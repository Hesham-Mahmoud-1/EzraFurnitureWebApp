using Ezura.Core.Entities;
using Ezura.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ezura.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context     = services.GetRequiredService<EzuraDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedCurrenciesAsync(context);
        await SeedCategoriesAsync(context);
        await SeedHomepageSectionsAsync(context);
        await SeedSampleProductsAsync(context);
        await SeedTestimonialsAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles =
        [
            "SuperAdmin", "Manager", "SalesEmployee",
            "ProductionEmployee", "ShippingEmployee",
            "CustomerSupport", "Customer"
        ];
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "admin@ezura.com";
        if (await userManager.FindByEmailAsync(email) != null) return;

        var admin = new ApplicationUser
        {
            UserName = email, Email = email,
            FirstName = "Ezura", LastName = "Admin",
            EmailConfirmed = true, IsActive = true
        };
        var result = await userManager.CreateAsync(admin, "Admin@Ezura1!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "SuperAdmin");
    }

    private static async Task SeedCurrenciesAsync(EzuraDbContext ctx)
    {
        if (await ctx.CurrencyRates.AnyAsync()) return;
        await ctx.CurrencyRates.AddRangeAsync(new[]
        {
            new CurrencyRate { ToCurrency="USD", CurrencyName="US Dollar",     Symbol="$",    Rate=0.0210m },
            new CurrencyRate { ToCurrency="EUR", CurrencyName="Euro",           Symbol="€",    Rate=0.0193m },
            new CurrencyRate { ToCurrency="GBP", CurrencyName="British Pound",  Symbol="£",    Rate=0.0165m },
            new CurrencyRate { ToCurrency="SAR", CurrencyName="Saudi Riyal",    Symbol="﷼",   Rate=0.0788m },
            new CurrencyRate { ToCurrency="AED", CurrencyName="UAE Dirham",     Symbol="د.إ", Rate=0.0772m }
        });
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(EzuraDbContext ctx)
    {
        if (await ctx.Categories.AnyAsync()) return;
        await ctx.Categories.AddRangeAsync(new[]
        {
            new Category { Name="Drafting Tables",       Slug="drafting-tables",       SortOrder=1, IsActive=true },
            new Category { Name="Office Desks",          Slug="office-desks",          SortOrder=2, IsActive=true },
            new Category { Name="Engineer Workstations", Slug="engineer-workstations", SortOrder=3, IsActive=true },
            new Category { Name="Designer Furniture",    Slug="designer-furniture",    SortOrder=4, IsActive=true },
            new Category { Name="Custom Orders",         Slug="custom-orders",         SortOrder=5, IsActive=true }
        });
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedHomepageSectionsAsync(EzuraDbContext ctx)
    {
        if (await ctx.HomepageSections.AnyAsync()) return;
        await ctx.HomepageSections.AddRangeAsync(new[]
        {
            new HomepageSection
            {
                Key="hero", Title="Crafted for Visionaries",
                Subtitle="Premium handcrafted furniture for architects, engineers & designers",
                Content="Each piece is designed and built by hand in Cairo, Egypt.",
                ButtonText="Explore Collection", ButtonUrl="/products",
                IsActive=true, SortOrder=1
            },
            new HomepageSection
            {
                Key="story", Title="The Ezura Story",
                Subtitle="Born from a passion for precision and beauty",
                Content="Founded by an architect who couldn't find furniture that met professional standards.",
                IsActive=true, SortOrder=2
            }
        });
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedSampleProductsAsync(EzuraDbContext ctx)
    {
        if (await ctx.Products.AnyAsync()) return;

        var cats = await ctx.Categories.ToListAsync();
        var drafting = cats.FirstOrDefault(c => c.Slug == "drafting-tables");
        var office   = cats.FirstOrDefault(c => c.Slug == "office-desks");
        if (drafting == null || office == null) return;

        await ctx.Products.AddRangeAsync(new[]
        {
            new Product
            {
                Name="Architect Pro Drafting Table",
                Slug="architect-pro-drafting-table",
                ShortDescription="Professional A0 drafting table with adjustable angle and height",
                Description="The Architect Pro is our flagship drafting table, built for serious professionals. Features full A0 surface, precision angle adjustment from 0–90°, integrated tool tray, and cable management.",
                Price=12500m, CategoryId=drafting.Id,
                MaterialType="Beech wood & steel",
                WidthCm=120, HeightCm=80, DepthCm=90,
                Color="Natural Oak", FinishType="Matte lacquer",
                StockQuantity=5, LowStockThreshold=2,
                IsFeatured=true, IsAvailable=true,
                Sku="EZ-DT-001", Tags="drafting,architect,professional"
            },
            new Product
            {
                Name="Executive Corner Desk",
                Slug="executive-corner-desk",
                ShortDescription="L-shaped executive desk with hidden cable management",
                Description="Commanding presence meets functional elegance. Solid walnut surface, integrated charging stations, premium soft-close drawers.",
                Price=18000m, DiscountPrice=15500m, CategoryId=office.Id,
                MaterialType="Solid walnut & black steel",
                WidthCm=200, HeightCm=75, DepthCm=160,
                Color="Dark Walnut", FinishType="Oil finish",
                StockQuantity=3, LowStockThreshold=1,
                IsFeatured=true, IsAvailable=true,
                Sku="EZ-OD-001", Tags="executive,office,walnut"
            },
            new Product
            {
                Name="Engineer Workstation Pro",
                Slug="engineer-workstation-pro",
                ShortDescription="Heavy-duty workstation for engineering professionals",
                Description="Built to handle the demands of engineering work. Massive surface area, steel frame, integrated power outlets, and monitor arm mounts.",
                Price=22000m, CategoryId=cats.First(c=>c.Slug=="engineer-workstations").Id,
                MaterialType="Steel frame & MDF top",
                WidthCm=180, HeightCm=75, DepthCm=90,
                Color="Matte Black", FinishType="Powder coat",
                StockQuantity=4, LowStockThreshold=2,
                IsFeatured=true, IsAvailable=true,
                Sku="EZ-WS-001", Tags="engineer,workstation,heavy-duty"
            }
        });
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedTestimonialsAsync(EzuraDbContext ctx)
    {
        if (await ctx.Testimonials.AnyAsync()) return;
        await ctx.Testimonials.AddRangeAsync(new[]
        {
            new Testimonial
            {
                CustomerName="Ahmed Hassan", CustomerTitle="Senior Architect",
                Content="The drafting table completely transformed my studio. The craftsmanship is extraordinary.",
                Rating=5, IsActive=true, SortOrder=1
            },
            new Testimonial
            {
                CustomerName="Sara Ibrahim", CustomerTitle="Interior Designer",
                Content="Three pieces ordered, each one exceeds expectations. The custom desk fits my space perfectly.",
                Rating=5, IsActive=true, SortOrder=2
            },
            new Testimonial
            {
                CustomerName="Mohamed Kamal", CustomerTitle="Civil Engineer",
                Content="Professional grade workstation, delivered on time. The quality justifies every penny.",
                Rating=5, IsActive=true, SortOrder=3
            }
        });
        await ctx.SaveChangesAsync();
    }
}
