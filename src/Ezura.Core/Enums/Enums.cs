namespace Ezura.Core.Enums;

public enum OrderStatus
{
    Pending = 0,
    DepositReceived = 1,
    InDesign = 2,
    InProduction = 3,
    ReadyForShipping = 4,
    Shipped = 5,
    Delivered = 6,
    Completed = 7,
    Cancelled = 8,
    Refunded = 9
}

public enum ProductionStatus
{
    NotStarted = 0,
    DesignPhase = 1,
    MaterialSourcing = 2,
    InProgress = 3,
    QualityCheck = 4,
    Completed = 5,
    OnHold = 6
}

public enum PaymentStatus
{
    Pending = 0,
    PartiallyPaid = 1,
    FullyPaid = 2,
    Refunded = 3,
    Failed = 4,
    Cancelled = 5
}

public enum PaymentType
{
    Deposit = 0,
    FullPayment = 1,
    RemainingBalance = 2,
    Refund = 3,
    Adjustment = 4
}

public enum PaymentMethod
{
    CashOnDelivery = 0,
    BankTransfer = 1,
    CreditCard = 2,
    InstaPay = 3,
    Vodafone = 4,
    Orange = 5,
    Etisalat = 6,
    PayPal = 7,
    Other = 8
}

public enum ShippingStatus
{
    Pending = 0,
    Processing = 1,
    PickedUp = 2,
    InTransit = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Failed = 6,
    Returned = 7
}

public enum InventoryCategory
{
    WoodMaterial = 0,
    MetalMaterial = 1,
    FabricUpholstery = 2,
    Hardware = 3,
    Accessories = 4,
    Finishes = 5,
    Packaging = 6,
    Tools = 7,
    Other = 8
}

public enum MovementType
{
    Purchase = 0,
    Usage = 1,
    Return = 2,
    Adjustment = 3,
    Waste = 4,
    Transfer = 5
}

public enum CustomRequestStatus
{
    Pending = 0,
    UnderReview = 1,
    QuoteSent = 2,
    Accepted = 3,
    Rejected = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7
}

public enum NotificationType
{
    OrderCreated = 0,
    OrderStatusChanged = 1,
    PaymentReceived = 2,
    PaymentDue = 3,
    ShipmentUpdate = 4,
    LowStock = 5,
    CustomRequest = 6,
    SystemAlert = 7,
    Promotional = 8,
    Welcome = 9
}

public enum UserRole
{
    SuperAdmin = 0,
    Manager = 1,
    SalesEmployee = 2,
    ProductionEmployee = 3,
    ShippingEmployee = 4,
    CustomerSupport = 5,
    Customer = 6
}
