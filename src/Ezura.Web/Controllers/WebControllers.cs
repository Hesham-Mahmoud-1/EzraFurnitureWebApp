using Ezura.Core.DTOs;
using Ezura.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ezura.Web.Controllers;

// ============================================================
// HOME CONTROLLER
// ============================================================
public class HomeController : Controller
{
    private readonly IProductService _products;
    private readonly ICustomRequestService _customRequests;
    private readonly IReviewService _reviews;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IProductService products,
        ICustomRequestService customRequests,
        IReviewService reviews,
        ILogger<HomeController> logger)
    {
        _products = products;
        _customRequests = customRequests;
        _reviews = reviews;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _products.GetFeaturedAsync(8);
        ViewBag.FeaturedProducts = featured;
        ViewBag.Reviews = await _reviews.GetAllAsync(approvedOnly: true);
        return View();
    }

    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult Error() => View();

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult SetLanguage(string culture, string returnUrl = "/")
    {
        if (!string.IsNullOrEmpty(culture))
        {
            var cookie = new Microsoft.AspNetCore.Http.CookieOptions
            {
                Expires = DateTime.UtcNow.AddYears(1),
                IsEssential = true,
                Path = "/"
            };
            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(
                    new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                cookie);
        }
        return LocalRedirect(returnUrl);
    }
}

// ============================================================
// PRODUCTS CONTROLLER
// ============================================================
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly ICurrencyService _currency;
    private readonly IReviewService _reviews;

    public ProductsController(IProductService products, ICurrencyService currency, IReviewService reviews)
    {
        _products = products;
        _currency = currency;
        _reviews = reviews;
    }

    public async Task<IActionResult> Index(
        int page = 1, int pageSize = 12,
        int? categoryId = null, string? search = null,
        string? sortBy = null, decimal? minPrice = null, decimal? maxPrice = null)
    {
        var filter = new ProductFilterDto
        {
            Page = page, PageSize = pageSize, CategoryId = categoryId,
            Search = search, SortBy = sortBy, MinPrice = minPrice, MaxPrice = maxPrice
        };

        var result = await _products.GetPagedAsync(filter);
        ViewBag.Currencies = await _currency.GetSupportedCurrenciesAsync();
        return View(result);
    }

    public async Task<IActionResult> Details(string slug)
    {
        var product = await _products.GetBySlugAsync(slug);
        if (product == null) return NotFound();

        ViewBag.RelatedProducts = await _products.GetRelatedAsync(product.Id);
        ViewBag.Reviews = await _reviews.GetByProductIdAsync(product.Id, approvedOnly: true);
        return View(product);
    }

    public async Task<IActionResult> Search(string q)
    {
        var result = await _products.GetPagedAsync(new ProductFilterDto
        {
            Search = q, PageSize = 20
        });
        ViewBag.Query = q;
        return View("Index", result);
    }
}

// ============================================================
// CART CONTROLLER
// ============================================================
public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) { _cart = cart; }

    private string? UserId => User.Identity?.IsAuthenticated == true
        ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        : null;

    private string SessionId => HttpContext.Session.Id;

    public async Task<IActionResult> Index()
    {
        var cart = await _cart.GetCartAsync(UserId, SessionId);
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
    {
        try
        {
            var cart = await _cart.AddItemAsync(UserId, SessionId,
                request.ProductId, request.Quantity, request.Notes);
            return Json(new { success = true, itemCount = cart.ItemCount, subTotal = cart.SubTotal });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] UpdateCartRequest request)
    {
        var cart = await _cart.UpdateItemAsync(UserId, SessionId,
            request.CartItemId, request.Quantity);
        return Json(new { success = true, itemCount = cart.ItemCount, subTotal = cart.SubTotal });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateNotes([FromBody] UpdateCustomizationRequest request)
    {
        var cart = await _cart.UpdateNotesAsync(UserId, SessionId,
            request.CartItemId, request.Notes, request.Height, request.Width, request.Color);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var cart = await _cart.RemoveItemAsync(UserId, SessionId, cartItemId);
        return Json(new { success = true, itemCount = cart.ItemCount, subTotal = cart.SubTotal });
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        await _cart.ClearCartAsync(UserId, SessionId);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var cart = await _cart.GetCartAsync(UserId, SessionId);
        return Json(new { count = cart.ItemCount });
    }
}

// ============================================================
// CHECKOUT CONTROLLER
// ============================================================
[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly IOrderService _orders;

    public CheckoutController(ICartService cart, IOrderService orders)
    {
        _cart = cart; _orders = orders;
    }

    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    public async Task<IActionResult> Index()
    {
        var cart = await _cart.GetCartAsync(UserId, null);
        if (!cart.Items.Any()) return RedirectToAction("Index", "Cart");
        ViewBag.Cart = cart;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place([FromForm] CreateOrderDto dto)
    {
        if (!ModelState.IsValid) return View("Index");

        try
        {
            // Populate items from cart
            var cart = await _cart.GetCartAsync(UserId, null);
            dto.Items = cart.Items.Select(i => new CreateOrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                CustomizationNotes = i.CustomizationNotes
            }).ToList();

            var order = await _orders.CreateAsync(dto, UserId);
            await _cart.ClearCartAsync(UserId, null);

            return RedirectToAction("Confirmation", new { orderNumber = order.OrderNumber });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Index");
        }
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        var order = await _orders.GetByOrderNumberAsync(orderNumber);
        if (order == null || order.UserId != UserId) return NotFound();
        return View(order);
    }
}

// ============================================================
// ACCOUNT CONTROLLER
// ============================================================
public class AccountController : Controller
{
    private readonly Microsoft.AspNetCore.Identity.UserManager<Ezura.Core.Entities.ApplicationUser> _userManager;
    private readonly Microsoft.AspNetCore.Identity.SignInManager<Ezura.Core.Entities.ApplicationUser> _signInManager;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly ICartService _cart;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        Microsoft.AspNetCore.Identity.UserManager<Ezura.Core.Entities.ApplicationUser> userManager,
        Microsoft.AspNetCore.Identity.SignInManager<Ezura.Core.Entities.ApplicationUser> signInManager,
        IEmailService email, IAuditService audit, ICartService cart,
        ILogger<AccountController> logger)
    {
        _userManager = userManager; _signInManager = signInManager;
        _email = email; _audit = audit; _cart = cart; _logger = logger;
    }

    [HttpGet] public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            user.FailedLoginCount = 0;
            await _userManager.UpdateAsync(user);

            await _audit.LogAsync("Login", user.Id, ipAddress:
                HttpContext.Connection.RemoteIpAddress?.ToString());

            // Merge guest cart
            await _cart.MergeCartAsync(HttpContext.Session.Id, user.Id);

            return LocalRedirect(returnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Account locked. Try again in 15 minutes.");
            return View(model);
        }

        ModelState.AddModelError("", "Invalid credentials.");
        return View(model);
    }

    [HttpGet] public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new Ezura.Core.Entities.ApplicationUser
        {
            UserName = model.Email, Email = model.Email,
            FirstName = model.FirstName, LastName = model.LastName,
            PhoneNumber = model.Phone
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: false);
            await _email.SendWelcomeEmailAsync(model.Email, model.FirstName);
            await _audit.LogAsync("Register", user.Id);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _signInManager.SignOutAsync();
        await _audit.LogAsync("Logout", userId);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        var dto = new ProfileEditDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber ?? "",
            Address = user.Address,
            City = user.City,
            Country = user.Country,
            PostalCode = user.PostalCode
        };
        return View(dto);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileEditDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Address = dto.Address;
        user.City = dto.City;
        user.Country = dto.Country;
        user.PostalCode = dto.PostalCode;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var err in result.Errors)
            ModelState.AddModelError("", err.Description);

        return View(dto);
    }

    [Authorize]
    public async Task<IActionResult> Orders([FromServices] IOrderService orderService)
    {
        var userId = _userManager.GetUserId(User)!;
        var orders = await orderService.GetByUserAsync(userId);
        return View(orders);
    }

    public IActionResult AccessDenied() => View();
}

// ============================================================
// CUSTOM REQUEST CONTROLLER
// ============================================================
[Route("custom-request")]
public class CustomRequestController : Controller
{
    private readonly ICustomRequestService _service;
    private readonly IFileService _files;

    public CustomRequestController(ICustomRequestService service, IFileService files)
    {
        _service = service; _files = files;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost("submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitCustomRequestDto dto,
        List<Microsoft.AspNetCore.Http.IFormFile> attachments)
    {
        if (!ModelState.IsValid) return View("Index", dto);

        foreach (var file in attachments.Take(5))
        {
            if (file.Length > 0)
            {
                await using var stream = file.OpenReadStream();
                var url = await _files.UploadImageAsync(stream, file.FileName, "custom-requests");
                dto.UploadedImageUrls.Add(url);
            }
        }

        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        await _service.SubmitAsync(dto, userId);
        TempData["Success"] = "Your custom furniture request has been submitted. We'll contact you within 24 hours.";
        return RedirectToAction(nameof(Index));
    }
}

// ============================================================
// PORTFOLIO CONTROLLER
// ============================================================
public class PortfolioController : Controller
{
    private readonly Ezura.Core.Interfaces.Repositories.IUnitOfWork _uow;

    public PortfolioController(Ezura.Core.Interfaces.Repositories.IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _uow.Portfolio.GetFeaturedAsync(20);
        return View(projects);
    }

    public async Task<IActionResult> Details(string slug)
    {
        var project = await _uow.Portfolio.GetBySlugAsync(slug);
        if (project == null) return NotFound();
        return View(project);
    }
}

// ─── View Models ──────────────────────────────────────────────────────────────
public class LoginViewModel
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [System.ComponentModel.DataAnnotations.Required]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string LastName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Phone { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    [System.ComponentModel.DataAnnotations.MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ============================================================
// WISHLIST CONTROLLER
// ============================================================
[Authorize]
public class WishlistController : Controller
{
    private readonly Ezura.Core.Interfaces.Repositories.IUnitOfWork _uow;

    public WishlistController(Ezura.Core.Interfaces.Repositories.IUnitOfWork uow)
    {
        _uow = uow;
    }

    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    public async Task<IActionResult> Index()
    {
        var items = await _uow.Wishlists.GetByUserIdAsync(UserId);
        return View("~/Views/Account/Wishlist.cshtml", items);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var exists = await _uow.Wishlists.IsInWishlistAsync(UserId, id);
        if (exists)
        {
            var items = await _uow.Wishlists.FindAsync(w => w.UserId == UserId && w.ProductId == id);
            _uow.Wishlists.RemoveRange(items);
        }
        else
        {
            await _uow.Wishlists.AddAsync(new Ezura.Core.Entities.Wishlist
            {
                UserId = UserId,
                ProductId = id
            });
        }
        await _uow.SaveChangesAsync();
        return Json(new { success = true, added = !exists });
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int id)
    {
        var items = await _uow.Wishlists.FindAsync(w => w.UserId == UserId && w.ProductId == id);
        _uow.Wishlists.RemoveRange(items);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

public record AddToCartRequest(int ProductId, int Quantity, string? Notes);
public record UpdateCartRequest(int CartItemId, int Quantity);
public record UpdateCustomizationRequest(int CartItemId, string? Notes, string? Height, string? Width, string? Color);

// ============================================================
// REVIEW CONTROLLER
// ============================================================
[Authorize]
public class ReviewController : Controller
{
    private readonly IReviewService _reviews;

    public ReviewController(IReviewService reviews) { _reviews = reviews; }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitReviewDto dto)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid review data.");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var review = await _reviews.SubmitReviewAsync(userId, dto);

        TempData["Success"] = "Review submitted and awaiting approval.";
        return RedirectToAction("Details", "Products", new { slug = "" });
    }
}

public class ReviewViewModel
{
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
}

// ─── Accessor to avoid circular dependency ────────────────────────────────────
public interface IUnitOfWorkAccessor { }
