using Ezura.Core.DTOs;

namespace Ezura.Core.Interfaces.Services;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto?> GetBySlugAsync(string slug);
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductFilterDto filter);
    Task<IEnumerable<ProductDto>> GetFeaturedAsync(int count = 8);
    Task<IEnumerable<ProductDto>> GetRelatedAsync(int productId, int count = 4);
    Task<ProductDto> CreateAsync(CreateProductDto dto, string userId);
    Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, string userId);
    Task DeleteAsync(int id, string userId);
    Task<bool> UpdateStockAsync(int id, int quantity, string userId);
    Task IncrementViewCountAsync(int id);
}

public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(int id);
    Task<OrderDto?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<OrderDto>> GetByUserAsync(string userId);
    Task<PagedResult<OrderDto>> GetPagedAsync(OrderFilterDto filter);
    Task<OrderDto> CreateAsync(CreateOrderDto dto, string userId);
    Task<OrderDto> UpdateStatusAsync(int id, Ezura.Core.Enums.OrderStatus status, string userId, string? notes = null);
    Task<OrderDto> UpdateProductionStatusAsync(int id, Ezura.Core.Enums.ProductionStatus status, string userId);
    Task<OrderDto> RecordPaymentAsync(int id, RecordPaymentDto dto, string userId);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime from, DateTime to);
    Task<byte[]> ExportOrdersAsync(OrderFilterDto filter, string format);
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(string? userId, string? sessionId);
    Task<CartDto> AddItemAsync(string? userId, string? sessionId, int productId, int quantity, string? notes = null);
    Task<CartDto> UpdateItemAsync(string? userId, string? sessionId, int cartItemId, int quantity);
    Task<CartDto> RemoveItemAsync(string? userId, string? sessionId, int cartItemId);
    Task<CartDto> UpdateNotesAsync(string? userId, string? sessionId, int cartItemId, string? notes, string? height = null, string? width = null, string? color = null);
    Task ClearCartAsync(string? userId, string? sessionId);
    Task<CartDto> MergeCartAsync(string sessionId, string userId);
}

public interface IInventoryService
{
    Task<IEnumerable<InventoryItemDto>> GetAllAsync();
    Task<InventoryItemDto?> GetByIdAsync(int id);
    Task<IEnumerable<InventoryItemDto>> GetLowStockAsync();
    Task<InventoryItemDto> CreateAsync(CreateInventoryItemDto dto, string userId);
    Task<InventoryItemDto> UpdateAsync(int id, UpdateInventoryItemDto dto, string userId);
    Task RecordMovementAsync(int itemId, RecordMovementDto dto, string userId);
    Task<IEnumerable<InventoryMovementDto>> GetMovementsAsync(int itemId, int count = 20);
}

public interface ICustomRequestService
{
    Task<CustomRequestDto?> GetByIdAsync(int id);
    Task<PagedResult<CustomRequestDto>> GetPagedAsync(int page, int pageSize, Ezura.Core.Enums.CustomRequestStatus? status = null);
    Task<CustomRequestDto> SubmitAsync(SubmitCustomRequestDto dto, string? userId = null);
    Task<CustomRequestDto> UpdateStatusAsync(int id, Ezura.Core.Enums.CustomRequestStatus status, string userId, string? notes = null);
    Task<CustomRequestDto> SendQuoteAsync(int id, decimal quotedPrice, string userId);
}

public interface ICurrencyService
{
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
    Task<IEnumerable<CurrencyDto>> GetSupportedCurrenciesAsync();
    Task RefreshRatesAsync();
    string? DetectUserCurrency(string? ipAddress, string? acceptLanguage);
}

public interface INotificationService
{
    Task SendAsync(string userId, string title, string message, Ezura.Core.Enums.NotificationType type, string? actionUrl = null);
    Task SendToAdminsAsync(string title, string message, Ezura.Core.Enums.NotificationType type);
    Task<IEnumerable<NotificationDto>> GetByUserAsync(string userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(int id, string userId);
    Task MarkAllAsReadAsync(string userId);
}

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, string name);
    Task SendOrderConfirmationAsync(string email, string name, string orderNumber, decimal totalAmount);
    Task SendOrderStatusUpdateAsync(string email, string name, string orderNumber, string status);
    Task SendPaymentReminderAsync(string email, string name, string orderNumber, decimal outstanding);
    Task SendShippingUpdateAsync(string email, string name, string orderNumber, string trackingNumber);
    Task SendPasswordResetAsync(string email, string resetLink);
    Task SendEmailAsync(string to, string subject, string htmlBody);
}

public interface IAuditService
{
    Task LogAsync(string action, string? userId = null, string? entityType = null,
        string? entityId = null, string? oldValues = null, string? newValues = null,
        string? ipAddress = null, string? userAgent = null, bool isSuccess = true, string? error = null);
    Task<IEnumerable<AuditLogDto>> GetRecentAsync(int count = 100);
    Task<IEnumerable<AuditLogDto>> GetByUserAsync(string userId, int count = 50);
}

public interface IReportService
{
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime from, DateTime to);
    Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to);
    Task<CustomerReportDto> GetCustomerReportAsync(DateTime from, DateTime to);
    Task<InventoryReportDto> GetInventoryReportAsync();
    Task<byte[]> ExportToPdfAsync(string reportType, DateTime from, DateTime to);
    Task<byte[]> ExportToExcelAsync(string reportType, DateTime from, DateTime to);
    Task<byte[]> ExportToCsvAsync(string reportType, DateTime from, DateTime to);
}

public interface IReviewService
{
    Task<ReviewDto> SubmitReviewAsync(string userId, SubmitReviewDto dto);
    Task<IEnumerable<ReviewDto>> GetByProductIdAsync(int productId, bool approvedOnly = true);
    Task<IEnumerable<ReviewDto>> GetAllPendingAsync();
    Task<IEnumerable<ReviewDto>> GetAllAsync(bool approvedOnly = false);
    Task ApproveAsync(int reviewId);
    Task RejectAsync(int reviewId);
    Task DeleteAsync(int reviewId);
}

public interface IFileService
{
    Task<string> UploadImageAsync(Stream stream, string fileName, string folder);
    Task<string> UploadFileAsync(Stream stream, string fileName, string folder);
    Task DeleteFileAsync(string fileUrl);
    string GetPublicUrl(string relativePath);
}
