using Ezura.Core.Enums;

namespace Ezura.Core.Entities;

// ============================================================
// PRODUCT DOMAIN
// ============================================================

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
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
    public int StockQuantity { get; set; } = 0;
    public int LowStockThreshold { get; set; } = 3;
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsCustomizable { get; set; } = false;
    public string? Sku { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int ViewCount { get; set; } = 0;
    public int SortOrder { get; set; } = 0;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? Tags { get; set; }

    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public decimal EffectivePrice => DiscountPrice.HasValue && DiscountPrice < Price ? DiscountPrice.Value : Price;
    public bool IsInStock => StockQuantity > 0;
    public bool IsLowStock => StockQuantity <= LowStockThreshold && StockQuantity > 0;
}

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; } = false;
    public int SortOrder { get; set; } = 0;

    public virtual Product Product { get; set; } = null!;
}

// ============================================================
// ORDER DOMAIN
// ============================================================

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public string? ShippingPostalCode { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Currency { get; set; } = "EGP";
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public ProductionStatus ProductionStatus { get; set; } = ProductionStatus.NotStarted;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? CancellationReason { get; set; }
    public string? DesignerId { get; set; }

    // Computed display properties
    public string StatusName => Status.ToString();
    public string ProductionStatusName => ProductionStatus.ToString();

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual Shipment? Shipment { get; set; }
    public virtual Invoice? Invoice { get; set; }
    public virtual ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
}

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? CustomizationNotes { get; set; }
    public string? Specifications { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }
    public OrderStatus PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public string ChangedById { get; set; } = string.Empty;

    public virtual Order Order { get; set; } = null!;
}

// ============================================================
// PAYMENT DOMAIN
// ============================================================

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public string TransactionId { get; set; } = Guid.NewGuid().ToString("N")[..16].ToUpper();
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }

    public virtual Order Order { get; set; } = null!;
}

public class Invoice : BaseEntity
{
    public int OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsPaid { get; set; } = false;
    public string? PdfUrl { get; set; }
    public string? Notes { get; set; }

    public virtual Order Order { get; set; } = null!;
}

// ============================================================
// SHIPPING DOMAIN
// ============================================================

public class Shipment : BaseEntity
{
    public int OrderId { get; set; }
    public string? ShippingCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal ShippingCost { get; set; }
    public ShippingStatus Status { get; set; } = ShippingStatus.Pending;
    public DateTime? ShippedDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public string? ShippingLabel { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual ICollection<ShipmentTracking> TrackingHistory { get; set; } = new List<ShipmentTracking>();
}

public class ShipmentTracking : BaseEntity
{
    public int ShipmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public DateTime EventTime { get; set; } = DateTime.UtcNow;

    public virtual Shipment Shipment { get; set; } = null!;
}

// ============================================================
// INVENTORY DOMAIN
// ============================================================

public class InventoryItem : BaseEntity
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
    public bool IsActive { get; set; } = true;

    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();
}

public class InventoryMovement : BaseEntity
{
    public int InventoryItemId { get; set; }
    public int? ProductId { get; set; }
    public MovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public string RecordedById { get; set; } = string.Empty;

    public virtual InventoryItem InventoryItem { get; set; } = null!;
    public virtual Product? Product { get; set; }
}

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? TotalPurchases { get; set; }

    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}

// ============================================================
// CUSTOM REQUEST DOMAIN
// ============================================================

public class CustomRequest : BaseEntity
{
    public string? UserId { get; set; }
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
    public CustomRequestStatus Status { get; set; } = CustomRequestStatus.Pending;
    public decimal? QuotedPrice { get; set; }
    public string? AdminNotes { get; set; }
    public string? AssignedToId { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<CustomRequestImage> Images { get; set; } = new List<CustomRequestImage>();
}

public class CustomRequestImage : BaseEntity
{
    public int CustomRequestId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Description { get; set; }

    public virtual CustomRequest CustomRequest { get; set; } = null!;
}

// ============================================================
// PORTFOLIO DOMAIN
// ============================================================

public class PortfolioProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ClientName { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsFeatured { get; set; } = false;
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public virtual ICollection<PortfolioImage> Images { get; set; } = new List<PortfolioImage>();
}

public class PortfolioImage : BaseEntity
{
    public int ProjectId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsBeforeImage { get; set; } = false;
    public bool IsAfterImage { get; set; } = false;
    public int SortOrder { get; set; } = 0;

    public virtual PortfolioProject Project { get; set; } = null!;
}

// ============================================================
// CUSTOMER ENGAGEMENT DOMAIN
// ============================================================

public class Review : BaseEntity
{
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public bool IsApproved { get; set; } = false;
    public bool IsVerifiedPurchase { get; set; } = false;

    public virtual Product Product { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}

public class Testimonial : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTitle { get; set; }
    public string? CustomerImageUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

public class Wishlist : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int ProductId { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public class Cart : BaseEntity
{
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? CustomizationNotes { get; set; }
    public string? Height { get; set; }
    public string? Width { get; set; }
    public string? Color { get; set; }

    public virtual Cart Cart { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

// ============================================================
// NOTIFICATION DOMAIN
// ============================================================

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public string? ActionUrl { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}

// ============================================================
// AUDIT & SECURITY DOMAIN
// ============================================================

public class AuditLog : BaseEntity
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestUrl { get; set; }
    public string? HttpMethod { get; set; }
    public int? ResponseCode { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }

    public virtual ApplicationUser? User { get; set; }
}

public class LoginHistory : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Device { get; set; }
    public string? Browser { get; set; }
    public string? Location { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public DateTime? LogoutTime { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}

// ============================================================
// CURRENCY DOMAIN
// ============================================================

public class CurrencyRate : BaseEntity
{
    public string FromCurrency { get; set; } = "EGP";
    public string ToCurrency { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

// ============================================================
// HOMEPAGE CONTENT DOMAIN
// ============================================================

public class HomepageSection : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public string? BackgroundUrl { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}
