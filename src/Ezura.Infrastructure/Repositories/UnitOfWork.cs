using Ezura.Core.Entities;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ezura.Infrastructure.Repositories;

/// <summary>
/// Unit of Work pattern implementation.
/// Coordinates multiple repositories under a single database transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly EzuraDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy-initialized repositories
    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private IOrderRepository? _orders;
    private IPaymentRepository? _payments;
    private IInventoryRepository? _inventory;
    private ICustomerRepository? _customers;
    private IShipmentRepository? _shipments;
    private ICustomRequestRepository? _customRequests;
    private IPortfolioRepository? _portfolio;
    private IReviewRepository? _reviews;
    private INotificationRepository? _notifications;
    private IAuditRepository? _auditLogs;
    private ICurrencyRepository? _currencies;
    private ICartRepository? _carts;
    private IWishlistRepository? _wishlists;

    public UnitOfWork(EzuraDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products =>
        _products ??= new ProductRepository(_context);

    public ICategoryRepository Categories =>
        _categories ??= new CategoryRepository(_context);

    public IOrderRepository Orders =>
        _orders ??= new OrderRepository(_context);

    public IPaymentRepository Payments =>
        _payments ??= new PaymentRepository(_context);

    public IInventoryRepository Inventory =>
        _inventory ??= new InventoryRepository(_context);

    public ICustomerRepository Customers =>
        _customers ??= new CustomerRepository(_context);

    public IShipmentRepository Shipments =>
        _shipments ??= new ShipmentRepository(_context);

    public ICustomRequestRepository CustomRequests =>
        _customRequests ??= new CustomRequestRepository(_context);

    public IPortfolioRepository Portfolio =>
        _portfolio ??= new PortfolioRepository(_context);

    public IReviewRepository Reviews =>
        _reviews ??= new ReviewRepository(_context);

    public INotificationRepository Notifications =>
        _notifications ??= new NotificationRepository(_context);

    public IAuditRepository AuditLogs =>
        _auditLogs ??= new AuditRepository(_context);

    public ICurrencyRepository Currencies =>
        _currencies ??= new CurrencyRepository(_context);

    public ICartRepository Carts =>
        _carts ??= new CartRepository(_context);

    public IWishlistRepository Wishlists =>
        _wishlists ??= new WishlistRepository(_context);

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync() =>
        _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

// ── Remaining repository stubs (follow same pattern as ProductRepository) ──────

public class CategoryRepository : Repository<Core.Entities.Category>, ICategoryRepository
{
    public CategoryRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<Core.Entities.Category?> GetBySlugAsync(string slug) =>
        _dbSet.FirstOrDefaultAsync(c => c.Slug == slug);

    public Task<IEnumerable<Core.Entities.Category>> GetActiveCategoriesAsync() =>
        _dbSet.Where(c => c.IsActive)
              .OrderBy(c => c.SortOrder)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.Category>)t.Result);

    public Task<IEnumerable<Core.Entities.Category>> GetWithProductCountAsync() =>
        _dbSet.Include(c => c.Products)
              .Where(c => c.IsActive)
              .OrderBy(c => c.SortOrder)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.Category>)t.Result);
}

public class PaymentRepository : Repository<Core.Entities.Payment>, IPaymentRepository
{
    public PaymentRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<IEnumerable<Core.Entities.Payment>> GetByOrderIdAsync(int orderId) =>
        _dbSet.Where(p => p.OrderId == orderId)
              .OrderByDescending(p => p.CreatedAt)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.Payment>)t.Result);

    public async Task<decimal> GetTotalCollectedAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet.Where(p => p.Status == Core.Enums.PaymentStatus.FullyPaid);
        if (from.HasValue) query = query.Where(p => p.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(p => p.CreatedAt <= to.Value);
        return await query.SumAsync(p => p.Amount);
    }
}

public class InventoryRepository : Repository<Core.Entities.InventoryItem>, IInventoryRepository
{
    public InventoryRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<IEnumerable<Core.Entities.InventoryItem>> GetLowStockItemsAsync() =>
        _dbSet.Include(i => i.Supplier)
              .Where(i => i.CurrentStock <= i.MinimumStock && i.IsActive)
              .OrderBy(i => i.CurrentStock)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.InventoryItem>)t.Result);

    public Task<IEnumerable<Core.Entities.InventoryMovement>> GetMovementsAsync(int itemId, int count = 20) =>
        _context.InventoryMovements
                .Where(m => m.InventoryItemId == itemId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<Core.Entities.InventoryMovement>)t.Result);

    public async Task AddMovementAsync(Core.Entities.InventoryMovement movement)
    {
        await _context.InventoryMovements.AddAsync(movement);
    }
}

public class CustomerRepository : Repository<Core.Entities.ApplicationUser>, ICustomerRepository
{
    public CustomerRepository(EzuraDbContext ctx) : base(ctx) { }

    public async Task<(IEnumerable<Core.Entities.ApplicationUser> Customers, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));

        var total = await query.CountAsync();
        var customers = await query.OrderByDescending(u => u.CreatedAt)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();
        return (customers, total);
    }

    public Task<IEnumerable<Core.Entities.ApplicationUser>> GetTopCustomersAsync(int count = 10) =>
        _dbSet.Include(u => u.Orders)
              .OrderByDescending(u => u.Orders.Where(o => o.Status != Core.Enums.OrderStatus.Cancelled)
                                              .Sum(o => o.TotalAmount))
              .Take(count)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.ApplicationUser>)t.Result);

    public async Task<decimal> GetCustomerLifetimeValueAsync(string userId) =>
        await _context.Orders
                      .Where(o => o.UserId == userId &&
                                  o.Status != Core.Enums.OrderStatus.Cancelled)
                      .SumAsync(o => o.TotalAmount);
}

public class ShipmentRepository : Repository<Core.Entities.Shipment>, IShipmentRepository
{
    public ShipmentRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<Core.Entities.Shipment?> GetByOrderIdAsync(int orderId) =>
        _dbSet.Include(s => s.TrackingHistory)
              .FirstOrDefaultAsync(s => s.OrderId == orderId);

    public Task<Core.Entities.Shipment?> GetWithTrackingAsync(int id) =>
        _dbSet.Include(s => s.TrackingHistory.OrderByDescending(t => t.EventTime))
              .FirstOrDefaultAsync(s => s.Id == id);
}

public class CustomRequestRepository : Repository<Core.Entities.CustomRequest>, ICustomRequestRepository
{
    public CustomRequestRepository(EzuraDbContext ctx) : base(ctx) { }

    public async Task<(IEnumerable<Core.Entities.CustomRequest> Requests, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Core.Enums.CustomRequestStatus? status = null)
    {
        var query = _dbSet.Include(r => r.Images).AsQueryable();
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync();
        return (items, total);
    }
}

public class PortfolioRepository : Repository<Core.Entities.PortfolioProject>, IPortfolioRepository
{
    public PortfolioRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<Core.Entities.PortfolioProject?> GetBySlugAsync(string slug) =>
        _dbSet.Include(p => p.Images).FirstOrDefaultAsync(p => p.Slug == slug);

    public Task<IEnumerable<Core.Entities.PortfolioProject>> GetFeaturedAsync(int count = 6) =>
        _dbSet.Include(p => p.Images.Where(i => i.SortOrder == 0))
              .Where(p => p.IsFeatured && p.IsPublished)
              .OrderBy(p => p.SortOrder)
              .Take(count)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.PortfolioProject>)t.Result);

    public Task<Core.Entities.PortfolioProject?> GetWithImagesAsync(int id) =>
        _dbSet.Include(p => p.Images.OrderBy(i => i.SortOrder))
              .FirstOrDefaultAsync(p => p.Id == id);
}

public class ReviewRepository : Repository<Core.Entities.Review>, IReviewRepository
{
    public ReviewRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<IEnumerable<Core.Entities.Review>> GetByProductIdAsync(int productId, bool approvedOnly = true) =>
        _dbSet.Include(r => r.User)
              .Where(r => r.ProductId == productId && (!approvedOnly || r.IsApproved))
              .OrderByDescending(r => r.CreatedAt)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.Review>)t.Result);

    public async Task<double> GetAverageRatingAsync(int productId) =>
        await _dbSet.Where(r => r.ProductId == productId && r.IsApproved)
                    .AverageAsync(r => (double?)r.Rating) ?? 0;
}

public class NotificationRepository : Repository<Core.Entities.Notification>, INotificationRepository
{
    public NotificationRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<IEnumerable<Core.Entities.Notification>> GetByUserIdAsync(string userId, bool unreadOnly = false) =>
        _dbSet.Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead))
              .OrderByDescending(n => n.CreatedAt)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.Notification>)t.Result);

    public Task<int> GetUnreadCountAsync(string userId) =>
        _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _dbSet.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in notifications) n.IsRead = true;
    }
}

public class AuditRepository : Repository<Core.Entities.AuditLog>, IAuditRepository
{
    public AuditRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<IEnumerable<Core.Entities.AuditLog>> GetByUserAsync(string userId, int count = 50) =>
        _dbSet.Where(a => a.UserId == userId)
              .OrderByDescending(a => a.CreatedAt)
              .Take(count)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.AuditLog>)t.Result);

    public Task<IEnumerable<Core.Entities.AuditLog>> GetRecentAsync(int count = 100) =>
        _dbSet.OrderByDescending(a => a.CreatedAt)
              .Take(count)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.AuditLog>)t.Result);
}

public class CurrencyRepository : Repository<Core.Entities.CurrencyRate>, ICurrencyRepository
{
    public CurrencyRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<Core.Entities.CurrencyRate?> GetRateAsync(string toCurrency) =>
        _dbSet.FirstOrDefaultAsync(c => c.ToCurrency == toCurrency && c.IsActive);

    public Task<IEnumerable<Core.Entities.CurrencyRate>> GetAllActiveAsync() =>
        _dbSet.Where(c => c.IsActive)
              .OrderBy(c => c.ToCurrency)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.CurrencyRate>)t.Result);
}

public class CartRepository : Repository<Core.Entities.Cart>, ICartRepository
{
    public CartRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<Core.Entities.Cart?> GetByUserIdAsync(string userId) =>
        _dbSet.Include(c => c.Items).ThenInclude(i => i.Product)
              .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
              .FirstOrDefaultAsync(c => c.UserId == userId);

    public Task<Core.Entities.Cart?> GetBySessionIdAsync(string sessionId) =>
        _dbSet.Include(c => c.Items).ThenInclude(i => i.Product)
              .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
              .FirstOrDefaultAsync(c => c.SessionId == sessionId);

    public Task<Core.Entities.Cart?> GetWithItemsAsync(string? userId, string? sessionId) =>
        _dbSet.Include(c => c.Items).ThenInclude(i => i.Product)
              .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
              .FirstOrDefaultAsync(c =>
                  (userId != null && c.UserId == userId) ||
                  (sessionId != null && c.SessionId == sessionId));
}

public class WishlistRepository : Repository<Core.Entities.Wishlist>, IWishlistRepository
{
    public WishlistRepository(EzuraDbContext ctx) : base(ctx) { }

    public Task<IEnumerable<Core.Entities.Wishlist>> GetByUserIdAsync(string userId) =>
        _dbSet.Include(w => w.Product).ThenInclude(p => p.Images.Where(i => i.IsPrimary))
              .Where(w => w.UserId == userId)
              .ToListAsync()
              .ContinueWith(t => (IEnumerable<Core.Entities.Wishlist>)t.Result);

    public Task<bool> IsInWishlistAsync(string userId, int productId) =>
        _dbSet.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
}
