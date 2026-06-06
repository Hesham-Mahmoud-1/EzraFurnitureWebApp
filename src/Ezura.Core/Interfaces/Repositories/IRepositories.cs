using System.Linq.Expressions;

namespace Ezura.Core.Interfaces.Repositories;

/// <summary>
/// Generic repository interface providing standard CRUD and query operations.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    IQueryable<T> Query();
}

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IOrderRepository Orders { get; }
    IPaymentRepository Payments { get; }
    IInventoryRepository Inventory { get; }
    ICustomerRepository Customers { get; }
    IShipmentRepository Shipments { get; }
    ICustomRequestRepository CustomRequests { get; }
    IPortfolioRepository Portfolio { get; }
    IReviewRepository Reviews { get; }
    INotificationRepository Notifications { get; }
    IAuditRepository AuditLogs { get; }
    ICurrencyRepository Currencies { get; }
    ICartRepository Carts { get; }
    IWishlistRepository Wishlists { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public interface IProductRepository : IRepository<Ezura.Core.Entities.Product>
{
    Task<Ezura.Core.Entities.Product?> GetBySlugAsync(string slug);
    Task<IEnumerable<Ezura.Core.Entities.Product>> GetFeaturedProductsAsync(int count = 8);
    Task<IEnumerable<Ezura.Core.Entities.Product>> GetByCategoryAsync(int categoryId);
    Task<(IEnumerable<Ezura.Core.Entities.Product> Products, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? categoryId = null, string? search = null,
        string? sortBy = null, decimal? minPrice = null, decimal? maxPrice = null);
    Task<IEnumerable<Ezura.Core.Entities.Product>> GetRelatedProductsAsync(int productId, int count = 4);
    Task<IEnumerable<Ezura.Core.Entities.Product>> GetLowStockProductsAsync();
    Task<Ezura.Core.Entities.Product?> GetWithImagesAsync(int id);
    Task<IEnumerable<Ezura.Core.Entities.Product>> GetTopSellingAsync(int count = 5);
}

public interface ICategoryRepository : IRepository<Ezura.Core.Entities.Category>
{
    Task<Ezura.Core.Entities.Category?> GetBySlugAsync(string slug);
    Task<IEnumerable<Ezura.Core.Entities.Category>> GetActiveCategoriesAsync();
    Task<IEnumerable<Ezura.Core.Entities.Category>> GetWithProductCountAsync();
}

public interface IOrderRepository : IRepository<Ezura.Core.Entities.Order>
{
    Task<Ezura.Core.Entities.Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<Ezura.Core.Entities.Order>> GetByUserIdAsync(string userId);
    Task<Ezura.Core.Entities.Order?> GetWithDetailsAsync(int id);
    Task<(IEnumerable<Ezura.Core.Entities.Order> Orders, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Ezura.Core.Enums.OrderStatus? status = null,
        string? search = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null);
    Task<decimal> GetTotalDepositsAsync(DateTime? from = null, DateTime? to = null);
    Task<decimal> GetOutstandingBalancesAsync();
    Task<IEnumerable<(DateTime Date, decimal Revenue)>> GetDailyRevenueAsync(int days = 30);
    Task<IEnumerable<(string Month, decimal Revenue)>> GetMonthlyRevenueAsync(int months = 12);
    Task<Dictionary<Ezura.Core.Enums.OrderStatus, int>> GetStatusCountsAsync();
}

public interface IPaymentRepository : IRepository<Ezura.Core.Entities.Payment>
{
    Task<IEnumerable<Ezura.Core.Entities.Payment>> GetByOrderIdAsync(int orderId);
    Task<decimal> GetTotalCollectedAsync(DateTime? from = null, DateTime? to = null);
}

public interface IInventoryRepository : IRepository<Ezura.Core.Entities.InventoryItem>
{
    Task<IEnumerable<Ezura.Core.Entities.InventoryItem>> GetLowStockItemsAsync();
    Task<IEnumerable<Ezura.Core.Entities.InventoryMovement>> GetMovementsAsync(int itemId, int count = 20);
    Task AddMovementAsync(Ezura.Core.Entities.InventoryMovement movement);
}

public interface ICustomerRepository : IRepository<Ezura.Core.Entities.ApplicationUser>
{
    Task<(IEnumerable<Ezura.Core.Entities.ApplicationUser> Customers, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search = null);
    Task<IEnumerable<Ezura.Core.Entities.ApplicationUser>> GetTopCustomersAsync(int count = 10);
    Task<decimal> GetCustomerLifetimeValueAsync(string userId);
}

public interface IShipmentRepository : IRepository<Ezura.Core.Entities.Shipment>
{
    Task<Ezura.Core.Entities.Shipment?> GetByOrderIdAsync(int orderId);
    Task<Ezura.Core.Entities.Shipment?> GetWithTrackingAsync(int id);
}

public interface ICustomRequestRepository : IRepository<Ezura.Core.Entities.CustomRequest>
{
    Task<(IEnumerable<Ezura.Core.Entities.CustomRequest> Requests, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Ezura.Core.Enums.CustomRequestStatus? status = null);
}

public interface IPortfolioRepository : IRepository<Ezura.Core.Entities.PortfolioProject>
{
    Task<Ezura.Core.Entities.PortfolioProject?> GetBySlugAsync(string slug);
    Task<IEnumerable<Ezura.Core.Entities.PortfolioProject>> GetFeaturedAsync(int count = 6);
    Task<Ezura.Core.Entities.PortfolioProject?> GetWithImagesAsync(int id);
}

public interface IReviewRepository : IRepository<Ezura.Core.Entities.Review>
{
    Task<IEnumerable<Ezura.Core.Entities.Review>> GetByProductIdAsync(int productId, bool approvedOnly = true);
    Task<double> GetAverageRatingAsync(int productId);
}

public interface INotificationRepository : IRepository<Ezura.Core.Entities.Notification>
{
    Task<IEnumerable<Ezura.Core.Entities.Notification>> GetByUserIdAsync(string userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAllAsReadAsync(string userId);
}

public interface IAuditRepository : IRepository<Ezura.Core.Entities.AuditLog>
{
    Task<IEnumerable<Ezura.Core.Entities.AuditLog>> GetByUserAsync(string userId, int count = 50);
    Task<IEnumerable<Ezura.Core.Entities.AuditLog>> GetRecentAsync(int count = 100);
}

public interface ICurrencyRepository : IRepository<Ezura.Core.Entities.CurrencyRate>
{
    Task<Ezura.Core.Entities.CurrencyRate?> GetRateAsync(string toCurrency);
    Task<IEnumerable<Ezura.Core.Entities.CurrencyRate>> GetAllActiveAsync();
}

public interface ICartRepository : IRepository<Ezura.Core.Entities.Cart>
{
    Task<Ezura.Core.Entities.Cart?> GetByUserIdAsync(string userId);
    Task<Ezura.Core.Entities.Cart?> GetBySessionIdAsync(string sessionId);
    Task<Ezura.Core.Entities.Cart?> GetWithItemsAsync(string? userId, string? sessionId);
}

public interface IWishlistRepository : IRepository<Ezura.Core.Entities.Wishlist>
{
    Task<IEnumerable<Ezura.Core.Entities.Wishlist>> GetByUserIdAsync(string userId);
    Task<bool> IsInWishlistAsync(string userId, int productId);
}
