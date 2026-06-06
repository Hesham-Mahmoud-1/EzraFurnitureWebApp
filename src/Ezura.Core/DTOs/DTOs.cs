using Ezura.Core.Enums;

namespace Ezura.Core.DTOs;

// ============================================================
// GENERIC
// ============================================================

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

// ============================================================
// PRODUCT DTOs
// ============================================================

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Specifications { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? MaterialType { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Color { get; set; }
    public string? FinishType { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsCustomizable { get; set; }
    public string? Sku { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int ViewCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? Tags { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Specifications { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public int CategoryId { get; set; }
    public string? MaterialType { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Color { get; set; }
    public string? FinishType { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 3;
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsCustomizable { get; set; } = false;
    public string? Sku { get; set; }
    public string? Tags { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class UpdateProductDto : CreateProductDto
{
    public int Id { get; set; }
}

public class ProductFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsFeatured { get; set; }
}

// ============================================================
// ORDER DTOs
// ============================================================

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Currency { get; set; } = "EGP";
    public OrderStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public ProductionStatus ProductionStatus { get; set; }
    public string ProductionStatusName { get; set; } = string.Empty;
    public PaymentStatus PaymentStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
    public ShipmentDto? Shipment { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductThumbnail { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? CustomizationNotes { get; set; }
}

public class CreateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public string? ShippingPostalCode { get; set; }
    public string? Notes { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public string Currency { get; set; } = "EGP";
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? CustomizationNotes { get; set; }
}

public class RecordPaymentDto
{
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class OrderFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public OrderStatus? Status { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? UserId { get; set; }
}

// ============================================================
// PAYMENT DTOs
// ============================================================

public class PaymentDto
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public PaymentType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================
// INVENTORY DTOs
// ============================================================

public class InventoryItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InventoryCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = "piece";
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal? UnitCost { get; set; }
    public string? SupplierName { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInventoryItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InventoryCategory Category { get; set; }
    public string Unit { get; set; } = "piece";
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Sku { get; set; }
    public int? SupplierId { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class UpdateInventoryItemDto : CreateInventoryItemDto
{
    public int Id { get; set; }
}

public class RecordMovementDto
{
    public MovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class InventoryMovementDto
{
    public int Id { get; set; }
    public MovementType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public string RecordedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ============================================================
// CUSTOM REQUEST DTOs
// ============================================================

public class CustomRequestDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string FurnitureType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public string? PreferredMaterial { get; set; }
    public string? PreferredColor { get; set; }
    public string? BudgetRange { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public CustomRequestStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal? QuotedPrice { get; set; }
    public string? AdminNotes { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SubmitCustomRequestDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string FurnitureType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public string? PreferredMaterial { get; set; }
    public string? PreferredColor { get; set; }
    public string? BudgetRange { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public List<string> UploadedImageUrls { get; set; } = new();
}

// ============================================================
// SHIPMENT DTOs
// ============================================================

public class ShipmentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string? ShippingCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal ShippingCost { get; set; }
    public ShippingStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? ShippedDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<ShipmentTrackingDto> TrackingHistory { get; set; } = new();
}

public class ShipmentTrackingDto
{
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public DateTime EventTime { get; set; }
}

// ============================================================
// CURRENCY DTOs
// ============================================================

public class CurrencyDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime LastUpdated { get; set; }
}

// ============================================================
// CART DTOs
// ============================================================

public class CartDto
{
    public int Id { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public int ItemCount { get; set; }
}

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductThumbnail { get; set; }
    public string? ProductSlug { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? CustomizationNotes { get; set; }
    public string? Height { get; set; }
    public string? Width { get; set; }
    public string? Color { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
}

// ============================================================
// DASHBOARD & ANALYTICS DTOs
// ============================================================

public class DashboardStatsDto
{
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal YearRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal OutstandingPayments { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int PendingCustomRequests { get; set; }
    public List<RevenueDataPoint> RevenueChart { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public Dictionary<string, int> OrderStatusDistribution { get; set; } = new();
}

public class RevenueDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class TopProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int TotalSold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TopCustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class RevenueReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<RevenueDataPoint> DailyRevenue { get; set; } = new();
    public List<RevenueDataPoint> MonthlyRevenue { get; set; } = new();
}

public class SalesReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOrdersCreated { get; set; }
    public int TotalOrdersCompleted { get; set; }
    public int TotalOrdersCancelled { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
}

public class CustomerReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public decimal AverageLifetimeValue { get; set; }
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
}

public class InventoryReportDto
{
    public int TotalItems { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<InventoryItemDto> LowStockList { get; set; } = new();
}

// ============================================================
// NOTIFICATION DTOs
// ============================================================

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================
// ADMIN DTOs
// ============================================================

public class CreateAdminDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileEditDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
}

public class SubmitReviewDto
{
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================
// AUDIT DTOs
// ============================================================

public class AuditLogDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? RequestUrl { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
