using Ezura.Core.DTOs;
using Ezura.Core.Entities;
using Ezura.Core.Enums;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ezura.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager,SalesEmployee,ProductionEmployee,ShippingEmployee,CustomerSupport")]
public class DashboardController : Controller
{
    private readonly IOrderService _orders;
    private readonly IInventoryService _inventory;
    private readonly ICustomRequestService _customRequests;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _uow;

    public DashboardController(IOrderService orders, IInventoryService inventory,
        ICustomRequestService customRequests, INotificationService notifications,
        IUnitOfWork uow)
    {
        _orders = orders; _inventory = inventory;
        _customRequests = customRequests; _notifications = notifications;
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _orders.GetDashboardStatsAsync();
        var lowStock = await _inventory.GetLowStockAsync();
        ViewBag.LowStockItems = lowStock;
        ViewBag.TotalRegisteredUsers = await _uow.Customers.CountAsync();
        return View(stats);
    }

    [HttpGet]
    public async Task<IActionResult> StatsJson()
    {
        var stats = await _orders.GetDashboardStatsAsync();
        return Json(stats);
    }
}

// ============================================================
// ADMIN ORDERS
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager,SalesEmployee")]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) { _orders = orders; }

    public async Task<IActionResult> Index(
        int page = 1, OrderStatus? status = null,
        string? search = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var filter = new OrderFilterDto
        {
            Page = page, PageSize = 20, Status = status,
            Search = search, FromDate = fromDate, ToDate = toDate
        };
        var result = await _orders.GetPagedAsync(filter);
        ViewBag.CurrentStatus = status;
        ViewBag.Search = search;
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? notes)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _orders.UpdateStatusAsync(id, status, userId, notes);
        TempData["Success"] = "Order status updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduction(int id, ProductionStatus status)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _orders.UpdateProductionStatusAsync(id, status, userId);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(int id, [FromForm] RecordPaymentDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _orders.RecordPaymentAsync(id, dto, userId);
        TempData["Success"] = "Payment recorded.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Export(OrderFilterDto filter, string format = "excel")
    {
        var bytes = await _orders.ExportOrdersAsync(filter, format);
        var contentType = format switch
        {
            "pdf" => "application/pdf",
            "csv" => "text/csv",
            _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
        return File(bytes, contentType, $"orders-{DateTime.Now:yyyyMMdd}.{format}");
    }
}

// ============================================================
// ADMIN PRODUCTS
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager")]
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly IFileService _files;

    public ProductsController(IProductService products, IFileService files)
    {
        _products = products; _files = files;
    }

    public async Task<IActionResult> Index(int page = 1, string? search = null)
    {
        var result = await _products.GetPagedAsync(new ProductFilterDto
        {
            Page = page, PageSize = 20, Search = search
        });
        return View(result);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateProductDto dto,
        List<Microsoft.AspNetCore.Http.IFormFile> images)
    {
        if (!ModelState.IsValid) return View(dto);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var product = await _products.CreateAsync(dto, userId);

        // Handle image uploads
        foreach (var file in images.Take(10))
        {
            if (file.Length > 0)
            {
                await using var stream = file.OpenReadStream();
                await _files.UploadImageAsync(stream, file.FileName, $"products/{product.Id}");
            }
        }

        TempData["Success"] = $"Product '{product.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] UpdateProductDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _products.UpdateAsync(id, dto, userId);
        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _products.DeleteAsync(id, userId);
        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }
}

// ============================================================
// ADMIN CUSTOMERS
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager,SalesEmployee,CustomerSupport")]
public class CustomersController : Controller
{
    private readonly Ezura.Core.Interfaces.Repositories.IUnitOfWork _uow;

    public CustomersController(Ezura.Core.Interfaces.Repositories.IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IActionResult> Index(int page = 1, string? search = null)
    {
        var (customers, total) = await _uow.Customers.GetPagedAsync(page, 20, search);
        ViewBag.TotalCount = total;
        ViewBag.Page = page;
        return View(customers);
    }

    public async Task<IActionResult> Details(string id)
    {
        var customer = await _uow.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        var orders = await _uow.Orders.GetByUserIdAsync(id);
        ViewBag.Orders = orders;
        ViewBag.LifetimeValue = await _uow.Customers.GetCustomerLifetimeValueAsync(id);
        return View(customer);
    }
}

// ============================================================
// ADMIN INVENTORY
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager,ProductionEmployee")]
public class InventoryController : Controller
{
    private readonly IInventoryService _inventory;

    public InventoryController(IInventoryService inventory) { _inventory = inventory; }

    public async Task<IActionResult> Index()
    {
        var items = await _inventory.GetAllAsync();
        ViewBag.LowStock = await _inventory.GetLowStockAsync();
        return View(items);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateInventoryItemDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _inventory.CreateAsync(dto, userId);
        TempData["Success"] = "Inventory item created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _inventory.GetByIdAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordMovement(int id, [FromForm] RecordMovementDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _inventory.RecordMovementAsync(id, dto, userId);
        TempData["Success"] = "Movement recorded.";
        return RedirectToAction(nameof(Index));
    }
}

// ============================================================
// ADMIN CUSTOM REQUESTS
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager,SalesEmployee")]
public class CustomRequestsController : Controller
{
    private readonly ICustomRequestService _service;

    public CustomRequestsController(ICustomRequestService service) { _service = service; }

    public async Task<IActionResult> Index(int page = 1, CustomRequestStatus? status = null)
    {
        var result = await _service.GetPagedAsync(page, 20, status);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var request = await _service.GetByIdAsync(id);
        if (request == null) return NotFound();
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, CustomRequestStatus status, string? notes)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.UpdateStatusAsync(id, status, userId, notes);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendQuote(int id, decimal quotedPrice)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.SendQuoteAsync(id, quotedPrice, userId);
        TempData["Success"] = "Quote sent to customer.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

// ============================================================
// ADMIN REPORTS
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager")]
public class ReportsController : Controller
{
    private readonly IOrderService _orders;
    private readonly IReportService _reports;

    public ReportsController(IOrderService orders, IReportService reports)
    {
        _orders = orders;
        _reports = reports;
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> Revenue(DateTime? from, DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;
        var report = await _orders.GetRevenueReportAsync(fromDate, toDate);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportRevenue(DateTime from, DateTime to, string format = "excel")
    {
        var filter = new OrderFilterDto { FromDate = from, ToDate = to };
        var bytes = await _orders.ExportOrdersAsync(filter, format);
        return File(bytes, "application/octet-stream", $"revenue-report.{format}");
    }

    public async Task<IActionResult> Sales(DateTime? from, DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;
        var report = await _reports.GetSalesReportAsync(fromDate, toDate);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportSales(DateTime from, DateTime to, string format = "excel")
    {
        var bytes = format.ToLowerInvariant() switch
        {
            "excel" => await _reports.ExportToExcelAsync("sales", from, to),
            "pdf" => await _reports.ExportToPdfAsync("sales", from, to),
            _ => throw new ArgumentException("Unsupported format")
        };
        return File(bytes, "application/octet-stream", $"sales-report.{format}");
    }
}

// ============================================================
// ADMIN USERS
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _uow;

    public UsersController(UserManager<ApplicationUser> userManager, IUnitOfWork uow)
    {
        _userManager = userManager; _uow = uow;
    }

    public async Task<IActionResult> Index(int page = 1, string? search = null, string? role = null)
    {
        var query = _userManager.Users.AsQueryable();

        // Filter by admin roles if specified
        var adminRoles = new[] { "SuperAdmin", "Manager" };
        if (string.IsNullOrEmpty(role) || role == "admin")
            role = "admin"; // default: show only admin users

        if (role == "admin")
        {
            var adminUserIds = new List<string>();
            foreach (var r in adminRoles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(r);
                adminUserIds.AddRange(usersInRole.Select(u => u.Id));
            }
            query = query.Where(u => adminUserIds.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Email!.Contains(search) ||
                                     u.FirstName.Contains(search) ||
                                     u.LastName.Contains(search));

        var total = await query.CountAsync();
        var users = await query.OrderByDescending(u => u.CreatedAt)
                               .Skip((page - 1) * 20).Take(20)
                               .ToListAsync();

        // Load roles for each user
        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var u in users)
            userRoles[u.Id] = await _userManager.GetRolesAsync(u);

        ViewBag.TotalCount = total;
        ViewBag.Page = page;
        ViewBag.Search = search;
        ViewBag.RoleFilter = role;
        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    public IActionResult CreateAdmin() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdmin(CreateAdminDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);
            return View(dto);
        }

        await _userManager.AddToRoleAsync(user, "Manager");
        TempData["Success"] = $"Admin account '{dto.Email}' created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        ViewBag.Roles = await _userManager.GetRolesAsync(user);
        ViewBag.Orders = await _uow.Orders.GetByUserIdAsync(id);
        ViewBag.LifetimeValue = await _uow.Customers.GetCustomerLifetimeValueAsync(id);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = $"User {(user.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var current = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, current);
        await _userManager.AddToRoleAsync(user, role);
        TempData["Success"] = $"Role '{role}' assigned.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

// ============================================================
// ADMIN CATEGORIES
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager")]
public class CategoriesController : Controller
{
    private readonly IUnitOfWork _uow;

    public CategoriesController(IUnitOfWork uow) { _uow = uow; }

    public async Task<IActionResult> Index()
    {
        var categories = await _uow.Categories.GetWithProductCountAsync();
        return View(categories);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (!ModelState.IsValid) return View(category);
        category.Slug = category.Name.ToLower().Replace(" ", "-");
        await _uow.Categories.AddAsync(category);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"Category '{category.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _uow.Categories.GetByIdAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id) return NotFound();
        if (!ModelState.IsValid) return View(category);
        category.Slug = category.Name.ToLower().Replace(" ", "-");
        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync();
        TempData["Success"] = $"Category '{category.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _uow.Categories.GetByIdAsync(id);
        if (category == null) return NotFound();
        _uow.Categories.Remove(category);
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Index));
    }
}

// ============================================================
// ADMIN REVIEWS
// ============================================================
[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Manager")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews) { _reviews = reviews; }

    public async Task<IActionResult> Index()
    {
        var all = await _reviews.GetAllAsync();
        return View(all);
    }

    public async Task<IActionResult> Pending()
    {
        var pending = await _reviews.GetAllPendingAsync();
        return View("Index", pending);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        await _reviews.ApproveAsync(id);
        TempData["Success"] = "Review approved.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        await _reviews.RejectAsync(id);
        TempData["Success"] = "Review rejected and removed.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviews.DeleteAsync(id);
        TempData["Success"] = "Review deleted.";
        return RedirectToAction(nameof(Index));
    }
}
