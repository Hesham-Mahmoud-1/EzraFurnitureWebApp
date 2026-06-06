using Ezura.Core.Entities;
using Ezura.Core.Enums;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ezura.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(EzuraDbContext context) : base(context) { }

    public async Task<Product?> GetBySlugAsync(string slug) =>
        await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 8) =>
        await _dbSet
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.Category)
            .Where(p => p.IsFeatured && p.IsAvailable)
            .OrderBy(p => p.SortOrder)
            .Take(count)
            .ToListAsync();

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId) =>
        await _dbSet
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Where(p => p.CategoryId == categoryId && p.IsAvailable)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? categoryId = null, string? search = null,
        string? sortBy = null, decimal? minPrice = null, decimal? maxPrice = null)
    {
        var query = _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)) ||
                (p.Tags != null && p.Tags.Contains(search)));

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var totalCount = await query.CountAsync();

        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "popular" => query.OrderByDescending(p => p.ViewCount),
            _ => query.OrderBy(p => p.SortOrder).ThenByDescending(p => p.CreatedAt)
        };

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    public async Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int count = 4)
    {
        var product = await _dbSet.FindAsync(productId);
        if (product == null) return Enumerable.Empty<Product>();

        return await _dbSet
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Where(p => p.CategoryId == product.CategoryId && p.Id != productId && p.IsAvailable)
            .OrderBy(p => Guid.NewGuid())
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync() =>
        await _dbSet
            .Where(p => p.StockQuantity <= p.LowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

    public async Task<Product?> GetWithImagesAsync(int id) =>
        await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Reviews.Where(r => r.IsApproved).OrderByDescending(r => r.CreatedAt).Take(10))
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Product>> GetTopSellingAsync(int count = 5) =>
        await _dbSet
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.OrderItems)
            .OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity))
            .Take(count)
            .ToListAsync();
}

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(EzuraDbContext context) : base(context) { }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber) =>
        await _dbSet
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payments)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId) =>
        await _dbSet
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<Order?> GetWithDetailsAsync(int id) =>
        await _dbSet
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .Include(o => o.Payments)
            .Include(o => o.Shipment).ThenInclude(s => s != null ? s.TrackingHistory : null)
            .Include(o => o.Invoice)
            .Include(o => o.StatusHistories)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedAsync(
        int page, int pageSize, OrderStatus? status = null,
        string? search = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _dbSet
            .Include(o => o.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                o.CustomerName.Contains(search) ||
                o.CustomerEmail.Contains(search) ||
                o.CustomerPhone.Contains(search));

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value.AddDays(1));

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet.Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded);

        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);

        return await query.SumAsync(o => o.TotalAmount);
    }

    public async Task<decimal> GetTotalDepositsAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Payments
            .Where(p => p.Type == PaymentType.Deposit && p.Status == PaymentStatus.FullyPaid);

        if (from.HasValue) query = query.Where(p => p.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(p => p.CreatedAt <= to.Value);

        return await query.SumAsync(p => p.Amount);
    }

    public async Task<decimal> GetOutstandingBalancesAsync() =>
        await _dbSet
            .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Completed)
            .SumAsync(o => o.RemainingAmount);

    public async Task<IEnumerable<(DateTime Date, decimal Revenue)>> GetDailyRevenueAsync(int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);
        var rows = await _dbSet
            .Where(o => o.CreatedAt >= fromDate &&
                        o.Status != OrderStatus.Cancelled &&
                        o.Status != OrderStatus.Refunded)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Date)
            .ToListAsync();
        return rows.Select(x => (x.Date, x.Revenue));
    }

    public async Task<IEnumerable<(string Month, decimal Revenue)>> GetMonthlyRevenueAsync(int months = 12)
    {
        var fromDate = DateTime.UtcNow.AddMonths(-months);
        var rows = await _dbSet
            .Where(o => o.CreatedAt >= fromDate &&
                        o.Status != OrderStatus.Cancelled &&
                        o.Status != OrderStatus.Refunded)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(x => x.Month)
            .ToListAsync();
        return rows.Select(x => (x.Month, x.Revenue));
    }

    public async Task<Dictionary<OrderStatus, int>> GetStatusCountsAsync() =>
        await _dbSet
            .GroupBy(o => o.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
}
