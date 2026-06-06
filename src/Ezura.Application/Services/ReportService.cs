using ClosedXML.Excel;
using Ezura.Core.DTOs;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ezura.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUnitOfWork uow, ILogger<ReportService> logger)
    {
        _uow = uow; _logger = logger;
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime from, DateTime to)
    {
        var totalRevenue  = await _uow.Orders.GetTotalRevenueAsync(from, to);
        var totalDeposits = await _uow.Orders.GetTotalDepositsAsync(from, to);
        var outstanding   = await _uow.Orders.GetOutstandingBalancesAsync();
        var (_, totalCount) = await _uow.Orders.GetPagedAsync(1, int.MaxValue, null, null, from, to);

        return new RevenueReportDto
        {
            FromDate = from, ToDate = to,
            TotalRevenue = totalRevenue, TotalDeposits = totalDeposits,
            TotalOutstanding = outstanding, TotalOrders = totalCount,
            AverageOrderValue = totalCount > 0 ? totalRevenue / totalCount : 0
        };
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to)
    {
        var (orders, total) = await _uow.Orders.GetPagedAsync(1, int.MaxValue, null, null, from, to);
        var orderList = orders.ToList();

        return new SalesReportDto
        {
            FromDate = from, ToDate = to,
            TotalOrdersCreated = total,
            TotalOrdersCompleted = orderList.Count(o => o.Status == Core.Enums.OrderStatus.Completed),
            TotalOrdersCancelled = orderList.Count(o => o.Status == Core.Enums.OrderStatus.Cancelled)
        };
    }

    public async Task<CustomerReportDto> GetCustomerReportAsync(DateTime from, DateTime to)
    {
        var (customers, total) = await _uow.Customers.GetPagedAsync(1, int.MaxValue);
        var topCustomers = await _uow.Customers.GetTopCustomersAsync(10);

        return new CustomerReportDto
        {
            FromDate = from, ToDate = to,
            TotalCustomers = total,
            NewCustomers = customers.Count(c => c.CreatedAt >= from && c.CreatedAt <= to),
            ActiveCustomers = customers.Count(c => c.IsActive),
            TopCustomers = topCustomers.Select(c => new TopCustomerDto
            {
                Id = c.Id, Name = c.FullName, Email = c.Email ?? "",
                TotalOrders = c.Orders?.Count ?? 0,
                TotalSpent = c.Orders?.Where(o => o.Status != Core.Enums.OrderStatus.Cancelled)
                                       .Sum(o => o.TotalAmount) ?? 0
            }).ToList()
        };
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync()
    {
        var items    = (await _uow.Inventory.GetAllAsync()).ToList();
        var lowStock = (await _uow.Inventory.GetLowStockItemsAsync()).ToList();

        return new InventoryReportDto
        {
            TotalItems = items.Count,
            LowStockItems = lowStock.Count,
            OutOfStockItems = items.Count(i => i.CurrentStock <= 0),
            TotalInventoryValue = items.Sum(i => i.CurrentStock * (i.UnitCost ?? 0)),
            LowStockList = lowStock.Select(i => new InventoryItemDto
            {
                Id = i.Id, Name = i.Name, Unit = i.Unit,
                CurrentStock = i.CurrentStock, MinimumStock = i.MinimumStock,
                IsLowStock = true
            }).ToList()
        };
    }

    public async Task<byte[]> ExportToExcelAsync(string reportType, DateTime from, DateTime to)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Report");

        // Header styling
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#111111");
        ws.Row(1).Style.Font.FontColor = XLColor.FromHtml("#C9A84C");

        switch (reportType.ToLower())
        {
            case "revenue":
            case "orders":
                await WriteOrdersSheet(ws, from, to);
                break;
            default:
                await WriteOrdersSheet(ws, from, to);
                break;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportToCsvAsync(string reportType, DateTime from, DateTime to)
    {
        var (orders, _) = await _uow.Orders.GetPagedAsync(1, int.MaxValue, null, null, from, to);
        var sb = new StringBuilder();
        sb.AppendLine("Order Number,Customer,Email,Phone,Total,Deposit,Outstanding,Status,Date");

        foreach (var o in orders)
        {
            sb.AppendLine($"\"{o.OrderNumber}\",\"{o.CustomerName}\",\"{o.CustomerEmail}\"," +
                          $"\"{o.CustomerPhone}\",{o.TotalAmount},{o.DepositAmount}," +
                          $"{o.RemainingAmount},{o.StatusName},{o.CreatedAt:yyyy-MM-dd}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportToPdfAsync(string reportType, DateTime from, DateTime to)
    {
        // PDF generation using QuestPDF
        // Full implementation requires QuestPDF document builder
        // Returning placeholder byte array - implement with QuestPDF in production
        var report = await GetRevenueReportAsync(from, to);
        var content = $"EZURA REVENUE REPORT\n" +
                      $"Period: {from:dd MMM yyyy} - {to:dd MMM yyyy}\n\n" +
                      $"Total Revenue: EGP {report.TotalRevenue:N2}\n" +
                      $"Total Deposits: EGP {report.TotalDeposits:N2}\n" +
                      $"Outstanding: EGP {report.TotalOutstanding:N2}\n" +
                      $"Total Orders: {report.TotalOrders}\n" +
                      $"Average Order Value: EGP {report.AverageOrderValue:N2}\n";
        return Encoding.UTF8.GetBytes(content);
    }

    private async Task WriteOrdersSheet(IXLWorksheet ws, DateTime from, DateTime to)
    {
        var headers = new[] {
            "Order Number", "Customer Name", "Email", "Phone",
            "Sub Total", "Tax", "Shipping", "Total",
            "Deposit", "Outstanding", "Status", "Production", "Date"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var (orders, _) = await _uow.Orders.GetPagedAsync(1, int.MaxValue, null, null, from, to);
        int row = 2;
        foreach (var o in orders)
        {
            ws.Cell(row, 1).Value  = o.OrderNumber;
            ws.Cell(row, 2).Value  = o.CustomerName;
            ws.Cell(row, 3).Value  = o.CustomerEmail;
            ws.Cell(row, 4).Value  = o.CustomerPhone;
            ws.Cell(row, 5).Value  = (double)o.SubTotal;
            ws.Cell(row, 6).Value  = (double)o.TaxAmount;
            ws.Cell(row, 7).Value  = (double)o.ShippingCost;
            ws.Cell(row, 8).Value  = (double)o.TotalAmount;
            ws.Cell(row, 9).Value  = (double)o.DepositAmount;
            ws.Cell(row, 10).Value = (double)o.RemainingAmount;
            ws.Cell(row, 11).Value = o.StatusName;
            ws.Cell(row, 12).Value = o.ProductionStatusName;
            ws.Cell(row, 13).Value = o.CreatedAt.ToString("dd/MM/yyyy");
            row++;
        }

        // Currency format for money columns
        var moneyStyle = ws.Style.NumberFormat;
        foreach (int col in new[] { 5, 6, 7, 8, 9, 10 })
            ws.Column(col).Style.NumberFormat.Format = "#,##0.00";
    }
}
