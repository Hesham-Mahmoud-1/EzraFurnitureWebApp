using Ezura.Application.Services;
using Ezura.Core.Entities;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Core.Interfaces.Services;
using Ezura.Infrastructure.Data;
using Ezura.Infrastructure.Repositories;
using Ezura.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using AspNetCoreRateLimit;
using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Ezura.Infrastructure.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ────────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/ezura-.txt",
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30,
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Database ───────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<EzuraDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.EnableRetryOnFailure(3);
            sql.CommandTimeout(60);
        }
    )
);

// ── Identity ───────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase       = true;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers      = true;
    options.User.RequireUniqueEmail         = true;
    options.SignIn.RequireConfirmedEmail    = false;
})
.AddEntityFrameworkStores<EzuraDbContext>()
.AddDefaultTokenProviders();

// ── JWT ────────────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication()
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
        ClockSkew = TimeSpan.Zero
    };
    // Allow JWT from SignalR query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) &&
                ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

// ── Cookie (MVC) ───────────────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath          = "/Account/Login";
    options.LogoutPath         = "/Account/Logout";
    options.AccessDeniedPath   = "/Account/AccessDenied";
    options.ExpireTimeSpan     = TimeSpan.FromDays(7);
    options.SlidingExpiration  = true;
    options.Cookie.HttpOnly    = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // use Always in prod
    options.Cookie.SameSite    = SameSiteMode.Lax;
});

// ── Rate Limiting ──────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// ── MVC ────────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(o =>
        o.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore);
builder.Services.AddRazorPages();

// SignalR is built into ASP.NET Core — no extra package needed
builder.Services.AddSignalR();

// ── AutoMapper ─────────────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ── Repositories & Unit of Work ────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Application Services ───────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderService,         OrderService>();
builder.Services.AddScoped<IProductService,       ProductService>();
builder.Services.AddScoped<ICartService,          CartService>();
builder.Services.AddScoped<IInventoryService,     InventoryService>();
builder.Services.AddScoped<ICustomRequestService, CustomRequestService>();
builder.Services.AddScoped<ICurrencyService,      CurrencyService>();
builder.Services.AddScoped<INotificationService,  NotificationService>();
builder.Services.AddScoped<IEmailService,         EmailService>();
builder.Services.AddScoped<IAuditService,         AuditService>();
builder.Services.AddScoped<IReportService,        ReportService>();
builder.Services.AddScoped<IReviewService,        ReviewService>();
builder.Services.AddScoped<IFileService, Ezura.Web.Services.LocalFileService>();
builder.Services.AddScoped<IJwtService,           JwtService>();

// ── CORS ───────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("EzuraCors", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins")
                       .Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── Localization ───────────────────────────────────────────────────────────────
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// ── Anti-forgery ───────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName          = "X-CSRF-TOKEN";
});

// ── Session ────────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout               = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly           = true;
    options.Cookie.IsEssential        = true;
    options.Cookie.SecurePolicy       = CookieSecurePolicy.SameAsRequest;
});

// ── HttpClient for currency API ────────────────────────────────────────────────
builder.Services.AddHttpClient("CurrencyApi", client =>
    client.BaseAddress = new Uri("https://api.exchangerate-api.com"));

// ── Razor Pages ViewImports ────────────────────────────────────────────────────
builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation();

// ── Build ──────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Pipeline ───────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseIpRateLimiting();
app.UseRouting();
app.UseCors("EzuraCors");
app.UseRequestLocalization();
app.UseSession();
// Force session cookie to be established on every request
app.Use(async (context, next) =>
{
    if (!context.Session.Keys.Contains("_init"))
        context.Session.SetString("_init", "_");
    await next();
});
app.UseAuthentication();
app.UseAuthorization();

// ── Security headers ───────────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"]  = "nosniff";
    context.Response.Headers["X-Frame-Options"]         = "DENY";
    context.Response.Headers["X-XSS-Protection"]        = "1; mode=block";
    context.Response.Headers["Referrer-Policy"]         = "strict-origin-when-cross-origin";
    await next();
});

// ── Routes ────────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "product-search",
    pattern: "products/search",
    defaults: new { controller = "Products", action = "Search" });

app.MapControllerRoute(
    name: "product-details",
    pattern: "products/{slug}",
    defaults: new { controller = "Products", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHub<Ezura.Web.Hubs.NotificationHub>("/hubs/notifications");

// ── Seed ──────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EzuraDbContext>();
        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(services);
        Log.Information("Database migration and seeding completed");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration/seeding failed — check connection string");
    }
}

app.Run();
