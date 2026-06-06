using AutoMapper;
using ClosedXML.Excel;
using Ezura.Core.DTOs;
using Ezura.Core.Entities;
using Ezura.Core.Enums;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ezura.Application.Services;

/// <summary>
/// Core order management service.
/// Handles the full order lifecycle: creation, status transitions, payments, and reporting.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IUnitOfWork uow,
        IMapper mapper,
        INotificationService notifications,
        IEmailService email,
        IAuditService audit,
        ILogger<OrderService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _notifications = notifications;
        _email = email;
        _audit = audit;
        _logger = logger;
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var order = await _uow.Orders.GetWithDetailsAsync(id);
        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto?> GetByOrderNumberAsync(string orderNumber)
    {
        var order = await _uow.Orders.GetByOrderNumberAsync(orderNumber);
        return order == null ? null : MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetByUserAsync(string userId)
    {
        var orders = await _uow.Orders.GetByUserIdAsync(userId);
        return orders.Select(MapToDto);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(OrderFilterDto filter)
    {
        var (orders, total) = await _uow.Orders.GetPagedAsync(
            filter.Page, filter.PageSize, filter.Status,
            filter.Search, filter.FromDate, filter.ToDate);

        return new PagedResult<OrderDto>
        {
            Items = orders.Select(MapToDto),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, string userId)
    {
        await _uow.BeginTransactionAsync();

        try
        {
            // Validate products and calculate totals
            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} not found.");

                if (!product.IsAvailable || product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Product '{product.Name}' is not available in the requested quantity.");

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.EffectivePrice,
                    TotalPrice = product.EffectivePrice * item.Quantity,
                    CustomizationNotes = item.CustomizationNotes
                };
                orderItems.Add(orderItem);
                subTotal += orderItem.TotalPrice;

                // Reserve stock
                product.StockQuantity -= item.Quantity;
                _uow.Products.Update(product);
            }

            const decimal TAX_RATE = 0.14m; // 14% Egyptian VAT
            var taxAmount = subTotal * TAX_RATE;
            var total = subTotal + taxAmount + dto.ShippingCost;
            var remaining = total - dto.DepositAmount;

            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                UserId = userId,
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                CustomerPhone = dto.CustomerPhone,
                ShippingAddress = dto.ShippingAddress,
                ShippingCity = dto.ShippingCity,
                ShippingCountry = dto.ShippingCountry,
                ShippingPostalCode = dto.ShippingPostalCode,
                SubTotal = subTotal,
                TaxAmount = taxAmount,
                ShippingCost = dto.ShippingCost,
                TotalAmount = total,
                DepositAmount = dto.DepositAmount,
                RemainingAmount = remaining,
                Currency = dto.Currency,
                Notes = dto.Notes,
                Status = dto.DepositAmount > 0 ? OrderStatus.DepositReceived : OrderStatus.Pending,
                PaymentStatus = dto.DepositAmount >= total ? PaymentStatus.FullyPaid :
                                dto.DepositAmount > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Pending,
                Items = orderItems
            };

            await _uow.Orders.AddAsync(order);
            await _uow.SaveChangesAsync();

            // Record initial payment if deposit provided
            if (dto.DepositAmount > 0)
            {
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Amount = dto.DepositAmount,
                    Currency = dto.Currency,
                    Type = PaymentType.Deposit,
                    Method = dto.PaymentMethod,
                    Status = PaymentStatus.FullyPaid,
                    ProcessedAt = DateTime.UtcNow
                };
                await _uow.Payments.AddAsync(payment);
                await _uow.SaveChangesAsync();
            }

            // Create invoice
            var invoice = new Invoice
            {
                OrderId = order.Id,
                InvoiceNumber = $"INV-{order.OrderNumber}",
                IssuedDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                TotalAmount = total,
                PaidAmount = dto.DepositAmount,
                IsPaid = dto.DepositAmount >= total
            };
            await _uow.SaveChangesAsync();

            await _uow.CommitTransactionAsync();

            _logger.LogInformation("Order {OrderNumber} created for user {UserId}", order.OrderNumber, userId);

            // Fire-and-forget notifications
            _ = Task.Run(async () =>
            {
                await _notifications.SendAsync(userId, "Order Confirmed",
                    $"Your order #{order.OrderNumber} has been received.", NotificationType.OrderCreated,
                    $"/orders/{order.OrderNumber}");
                await _email.SendOrderConfirmationAsync(
                    dto.CustomerEmail, dto.CustomerName, order.OrderNumber, total);
            });

            await _audit.LogAsync("CreateOrder", userId, "Order", order.Id.ToString(),
                newValues: $"OrderNumber={order.OrderNumber}, Total={total}");

            return MapToDto(order);
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, OrderStatus status, string userId, string? notes = null)
    {
        var order = await _uow.Orders.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        var previousStatus = order.Status;
        order.Status = status;

        var history = new OrderStatusHistory
        {
            OrderId = id,
            PreviousStatus = previousStatus,
            NewStatus = status,
            Notes = notes,
            ChangedById = userId
        };
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Order {Id} status changed from {Previous} to {New} by {User}",
            id, previousStatus, status, userId);

        _ = Task.Run(async () =>
        {
            await _notifications.SendAsync(order.UserId, "Order Update",
                $"Order #{order.OrderNumber} status: {status}", NotificationType.OrderStatusChanged);
            await _email.SendOrderStatusUpdateAsync(
                order.CustomerEmail, order.CustomerName, order.OrderNumber, status.ToString());
        });

        await _audit.LogAsync("UpdateOrderStatus", userId, "Order", id.ToString(),
            oldValues: previousStatus.ToString(), newValues: status.ToString());

        return MapToDto(order);
    }

    public async Task<OrderDto> UpdateProductionStatusAsync(int id, ProductionStatus status, string userId)
    {
        var order = await _uow.Orders.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        order.ProductionStatus = status;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync();

        await _audit.LogAsync("UpdateProductionStatus", userId, "Order", id.ToString(),
            newValues: status.ToString());

        return MapToDto(order);
    }

    public async Task<OrderDto> RecordPaymentAsync(int id, RecordPaymentDto dto, string userId)
    {
        await _uow.BeginTransactionAsync();

        try
        {
            var order = await _uow.Orders.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Order {id} not found.");

            var payment = new Payment
            {
                OrderId = id,
                Amount = dto.Amount,
                Currency = order.Currency,
                Type = dto.Type,
                Method = dto.Method,
                Status = PaymentStatus.FullyPaid,
                Reference = dto.Reference,
                Notes = dto.Notes,
                ProcessedAt = DateTime.UtcNow,
                ProcessedBy = userId
            };
            await _uow.Payments.AddAsync(payment);

            order.DepositAmount += dto.Amount;
            order.RemainingAmount -= dto.Amount;
            order.PaymentStatus = order.RemainingAmount <= 0
                ? PaymentStatus.FullyPaid
                : PaymentStatus.PartiallyPaid;

            if (order.Status == OrderStatus.Pending && dto.Type == PaymentType.Deposit)
                order.Status = OrderStatus.DepositReceived;

            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();

            _ = Task.Run(() =>
                _notifications.SendAsync(order.UserId, "Payment Received",
                    $"Payment of {dto.Amount:N2} {order.Currency} received for #{order.OrderNumber}.",
                    NotificationType.PaymentReceived));

            await _audit.LogAsync("RecordPayment", userId, "Order", id.ToString(),
                newValues: $"Amount={dto.Amount}, Type={dto.Type}");

            return MapToDto(order);
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var yearStart = new DateTime(now.Year, 1, 1);

        var stats = new DashboardStatsDto
        {
            TodayRevenue = await _uow.Orders.GetTotalRevenueAsync(today, today.AddDays(1)),
            WeekRevenue = await _uow.Orders.GetTotalRevenueAsync(weekStart),
            MonthRevenue = await _uow.Orders.GetTotalRevenueAsync(monthStart),
            YearRevenue = await _uow.Orders.GetTotalRevenueAsync(yearStart),
            TotalRevenue = await _uow.Orders.GetTotalRevenueAsync(),
            TotalDeposits = await _uow.Orders.GetTotalDepositsAsync(),
            OutstandingPayments = await _uow.Orders.GetOutstandingBalancesAsync()
        };

        var statusCounts = await _uow.Orders.GetStatusCountsAsync();
        stats.TotalOrders = statusCounts.Values.Sum();
        stats.PendingOrders = statusCounts.GetValueOrDefault(OrderStatus.Pending, 0);
        stats.CompletedOrders = statusCounts.GetValueOrDefault(OrderStatus.Completed, 0);
        stats.OrderStatusDistribution = statusCounts.ToDictionary(k => k.Key.ToString(), v => v.Value);

        var dailyRevenue = await _uow.Orders.GetDailyRevenueAsync(30);
        stats.RevenueChart = dailyRevenue.Select(d => new RevenueDataPoint
        {
            Label = d.Date.ToString("MMM dd"),
            Revenue = d.Revenue
        }).ToList();

        return stats;
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime from, DateTime to)
    {
        var totalRevenue = await _uow.Orders.GetTotalRevenueAsync(from, to);
        var totalDeposits = await _uow.Orders.GetTotalDepositsAsync(from, to);
        var outstanding = await _uow.Orders.GetOutstandingBalancesAsync();
        var (orders, totalCount) = await _uow.Orders.GetPagedAsync(1, int.MaxValue, null, null, from, to);

        return new RevenueReportDto
        {
            FromDate = from,
            ToDate = to,
            TotalRevenue = totalRevenue,
            TotalDeposits = totalDeposits,
            TotalOutstanding = outstanding,
            TotalOrders = totalCount,
            AverageOrderValue = totalCount > 0 ? totalRevenue / totalCount : 0
        };
    }

    public async Task<byte[]> ExportOrdersAsync(OrderFilterDto filter, string format)
    {
        var (orders, _) = await _uow.Orders.GetPagedAsync(
            1, int.MaxValue, filter.Status, filter.Search,
            filter.FromDate, filter.ToDate);

        var dtos = orders.Select(MapToDto).ToList();

        return format.ToLowerInvariant() switch
        {
            "excel" => ExportToExcel(dtos),
            "pdf" => ExportToPdf(dtos),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private static byte[] ExportToExcel(List<OrderDto> orders)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Orders");

        var headers = new[] { "Order #", "Customer", "Email", "Phone", "City", "Total",
            "Deposit", "Balance", "Status", "Items", "Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        for (int r = 0; r < orders.Count; r++)
        {
            var o = orders[r];
            ws.Cell(r + 2, 1).Value = o.OrderNumber;
            ws.Cell(r + 2, 2).Value = o.CustomerName;
            ws.Cell(r + 2, 3).Value = o.CustomerEmail;
            ws.Cell(r + 2, 4).Value = o.CustomerPhone;
            ws.Cell(r + 2, 5).Value = o.ShippingCity;
            ws.Cell(r + 2, 6).Value = o.TotalAmount;
            ws.Cell(r + 2, 7).Value = o.DepositAmount;
            ws.Cell(r + 2, 8).Value = o.RemainingAmount;
            ws.Cell(r + 2, 9).Value = o.StatusName;
            ws.Cell(r + 2, 10).Value = o.Items?.Count ?? 0;
            ws.Cell(r + 2, 11).Value = o.CreatedAt.ToString("yyyy-MM-dd");
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] ExportToPdf(List<OrderDto> orders)
    {
        var total = orders.Sum(o => o.TotalAmount);
        var deposits = orders.Sum(o => o.DepositAmount);

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Text("Ezura — Revenue Report").SemiBold().FontSize(14);

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Order #").Bold();
                        h.Cell().Text("Customer").Bold();
                        h.Cell().Text("Email").Bold();
                        h.Cell().Text("City").Bold();
                        h.Cell().Text("Total").Bold();
                        h.Cell().Text("Deposit").Bold();
                        h.Cell().Text("Balance").Bold();
                        h.Cell().Text("Status").Bold();
                        h.Cell().Text("Items").Bold();
                        h.Cell().Text("Date").Bold();
                    });

                    foreach (var o in orders)
                    {
                        table.Cell().Text(o.OrderNumber);
                        table.Cell().Text(o.CustomerName);
                        table.Cell().Text(o.CustomerEmail);
                        table.Cell().Text(o.ShippingCity);
                        table.Cell().Text(o.TotalAmount.ToString("N2"));
                        table.Cell().Text(o.DepositAmount.ToString("N2"));
                        table.Cell().Text(o.RemainingAmount.ToString("N2"));
                        table.Cell().Text(o.StatusName);
                        table.Cell().Text((o.Items?.Count ?? 0).ToString());
                        table.Cell().Text(o.CreatedAt.ToString("yyyy-MM-dd"));
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span($"Total Orders: {orders.Count}  |  ").FontSize(9);
                    t.Span($"Revenue: {total:N2}  |  ").FontSize(9);
                    t.Span($"Deposits: {deposits:N2}").FontSize(9);
                });
            });
        }).GeneratePdf();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = Random.Shared.Next(100, 999);
        return $"EZ-{timestamp}-{random}";
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        UserId = o.UserId,
        CustomerName = o.CustomerName,
        CustomerEmail = o.CustomerEmail,
        CustomerPhone = o.CustomerPhone,
        ShippingAddress = o.ShippingAddress,
        ShippingCity = o.ShippingCity,
        ShippingCountry = o.ShippingCountry,
        SubTotal = o.SubTotal,
        TaxAmount = o.TaxAmount,
        ShippingCost = o.ShippingCost,
        DiscountAmount = o.DiscountAmount,
        TotalAmount = o.TotalAmount,
        DepositAmount = o.DepositAmount,
        RemainingAmount = o.RemainingAmount,
        Currency = o.Currency,
        Status = o.Status,
        StatusName = o.Status.ToString(),
        ProductionStatus = o.ProductionStatus,
        ProductionStatusName = o.ProductionStatus.ToString(),
        PaymentStatus = o.PaymentStatus,
        Notes = o.Notes,
        ExpectedCompletionDate = o.ExpectedCompletionDate,
        ActualDeliveryDate = o.ActualDeliveryDate,
        CreatedAt = o.CreatedAt,
        Items = o.Items?.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductThumbnail = i.Product?.ThumbnailUrl,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice,
            CustomizationNotes = i.CustomizationNotes
        }).ToList() ?? new(),
        Payments = o.Payments?.Select(p => new PaymentDto
        {
            Id = p.Id,
            TransactionId = p.TransactionId,
            Amount = p.Amount,
            Currency = p.Currency,
            Type = p.Type,
            TypeName = p.Type.ToString(),
            Method = p.Method,
            MethodName = p.Method.ToString(),
            Status = p.Status,
            Reference = p.Reference,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt
        }).ToList() ?? new()
    };
}
