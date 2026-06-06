using Ezura.Core.DTOs;
using Ezura.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ezura.Web.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsApiController : ControllerBase
{
    private readonly IProductService _products;
    private readonly ICurrencyService _currency;

    public ProductsApiController(IProductService products, ICurrencyService currency)
    {
        _products = products; _currency = currency;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ProductFilterDto filter) =>
        Ok(await _products.GetPagedAsync(filter));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _products.GetByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> Featured([FromQuery] int count = 8) =>
        Ok(await _products.GetFeaturedAsync(count));

    [HttpGet("{id:int}/related")]
    public async Task<IActionResult> Related(int id, [FromQuery] int count = 4) =>
        Ok(await _products.GetRelatedAsync(id, count));

    [HttpGet("convert-price")]
    public async Task<IActionResult> ConvertPrice([FromQuery] decimal amount,
        [FromQuery] string toCurrency)
    {
        var converted = await _currency.ConvertAsync(amount, "EGP", toCurrency);
        return Ok(new { original = amount, converted, currency = toCurrency });
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrdersApiController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersApiController(IOrderService orders) { _orders = orders; }

    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    [HttpGet("my-orders")]
    public async Task<IActionResult> MyOrders() =>
        Ok(await _orders.GetByUserAsync(UserId));

    [HttpGet("{orderNumber}")]
    public async Task<IActionResult> GetByNumber(string orderNumber)
    {
        var order = await _orders.GetByOrderNumberAsync(orderNumber);
        if (order == null || order.UserId != UserId) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var order = await _orders.CreateAsync(dto, UserId);
            return CreatedAtAction(nameof(GetByNumber), new { orderNumber = order.OrderNumber }, order);
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CartApiController : ControllerBase
{
    private readonly ICartService _cart;

    public CartApiController(ICartService cart) { _cart = cart; }

    private string? UserId => User.Identity?.IsAuthenticated == true
        ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
    private string SessionId => HttpContext.Session.Id;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _cart.GetCartAsync(UserId, SessionId));

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest req)
    {
        try { return Ok(await _cart.AddItemAsync(UserId, SessionId, req.ProductId, req.Quantity, req.Notes)); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("update/{cartItemId:int}")]
    public async Task<IActionResult> Update(int cartItemId, [FromBody] int quantity) =>
        Ok(await _cart.UpdateItemAsync(UserId, SessionId, cartItemId, quantity));

    [HttpDelete("remove/{cartItemId:int}")]
    public async Task<IActionResult> Remove(int cartItemId) =>
        Ok(await _cart.RemoveItemAsync(UserId, SessionId, cartItemId));
}

[ApiController]
[Route("api/v1/currency")]
[Produces("application/json")]
public class CurrencyApiController : ControllerBase
{
    private readonly ICurrencyService _currency;

    public CurrencyApiController(ICurrencyService currency) { _currency = currency; }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _currency.GetSupportedCurrenciesAsync());

    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] ConvertRequest req)
    {
        var result = await _currency.ConvertAsync(req.Amount, req.From, req.To);
        return Ok(new { amount = req.Amount, from = req.From, to = req.To, result });
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsApiController : ControllerBase
{
    private readonly INotificationService _notifications;
    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    public NotificationsApiController(INotificationService notifications)
    { _notifications = notifications; }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool unreadOnly = false) =>
        Ok(await _notifications.GetByUserAsync(UserId, unreadOnly));

    [HttpGet("count")]
    public async Task<IActionResult> UnreadCount() =>
        Ok(new { count = await _notifications.GetUnreadCountAsync(UserId) });

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _notifications.MarkAsReadAsync(id, UserId);
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllAsReadAsync(UserId);
        return Ok();
    }
}

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "SuperAdmin,Manager")]
public class DashboardApiController : ControllerBase
{
    private readonly IOrderService _orders;

    public DashboardApiController(IOrderService orders) { _orders = orders; }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats() =>
        Ok(await _orders.GetDashboardStatsAsync());

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var report = await _orders.GetRevenueReportAsync(
            from ?? DateTime.UtcNow.AddMonths(-1),
            to ?? DateTime.UtcNow);
        return Ok(report);
    }
}

public record AddToCartRequest(int ProductId, int Quantity, string? Notes);
public record ConvertRequest(decimal Amount, string From, string To);
