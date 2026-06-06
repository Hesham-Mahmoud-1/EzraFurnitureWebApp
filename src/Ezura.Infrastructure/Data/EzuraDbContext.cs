using Ezura.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ezura.Infrastructure.Data;

/// <summary>
/// Main application DbContext. Inherits IdentityDbContext for ASP.NET Identity integration.
/// Implements global query filters for soft-delete and configures all entity relationships.
/// </summary>
public class EzuraDbContext : IdentityDbContext<ApplicationUser>
{
    public EzuraDbContext(DbContextOptions<EzuraDbContext> options) : base(options) { }

    // Products
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    // Orders
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    // Payments
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    // Shipping
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();

    // Inventory
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    // Customer engagement
    public DbSet<CustomRequest> CustomRequests => Set<CustomRequest>();
    public DbSet<CustomRequestImage> CustomRequestImages => Set<CustomRequestImage>();
    public DbSet<PortfolioProject> PortfolioProjects => Set<PortfolioProject>();
    public DbSet<PortfolioImage> PortfolioImages => Set<PortfolioImage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    // System
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();
    public DbSet<HomepageSection> HomepageSections => Set<HomepageSection>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from the current assembly
        builder.ApplyConfigurationsFromAssembly(typeof(EzuraDbContext).Assembly);

        // ── Global soft-delete filters ──────────────────────────────────────────
        builder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<InventoryItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CustomRequest>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PortfolioProject>().HasQueryFilter(e => !e.IsDeleted);

        // ── Precision for money columns ─────────────────────────────────────────
        builder.Entity<Product>(e =>
        {
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
        });

        builder.Entity<Order>(e =>
        {
            e.Property(p => p.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(p => p.TaxAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.ShippingCost).HasColumnType("decimal(18,2)");
            e.Property(p => p.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.DepositAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.RemainingAmount).HasColumnType("decimal(18,2)");
        });

        builder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        });

        builder.Entity<CurrencyRate>(e =>
        {
            e.Property(p => p.Rate).HasColumnType("decimal(18,6)");
        });

        // ── Indexes for performance ─────────────────────────────────────────────
        builder.Entity<Product>()
            .HasIndex(p => p.Slug).IsUnique();
        builder.Entity<Product>()
            .HasIndex(p => p.CategoryId);
        builder.Entity<Product>()
            .HasIndex(p => p.IsAvailable);

        builder.Entity<Category>()
            .HasIndex(c => c.Slug).IsUnique();

        builder.Entity<Order>()
            .HasIndex(o => o.OrderNumber).IsUnique();
        builder.Entity<Order>()
            .HasIndex(o => o.UserId);
        builder.Entity<Order>()
            .HasIndex(o => o.Status);
        builder.Entity<Order>()
            .HasIndex(o => o.CreatedAt);

        builder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);
        builder.Entity<AuditLog>()
            .HasIndex(a => a.CreatedAt);

        builder.Entity<Notification>()
            .HasIndex(n => n.UserId);
        builder.Entity<Notification>()
            .HasIndex(n => n.IsRead);

        builder.Entity<Wishlist>()
            .HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();

        // ── Rename Identity tables to snake_case with prefix ───────────────────
        builder.Entity<ApplicationUser>().ToTable("ezura_users");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("ezura_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("ezura_user_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("ezura_user_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("ezura_role_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("ezura_user_logins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("ezura_user_tokens");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
