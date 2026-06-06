using Ezura.Core.DTOs;
using Ezura.Core.Entities;
using Ezura.Core.Enums;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Core.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ezura.Infrastructure.Services;

// ============================================================
// EMAIL SERVICE
// ============================================================
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:FromName"] ?? "Ezura",
                _config["Email:FromEmail"] ?? "noreply@ezura.com"));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["Email:SmtpHost"] ?? "smtp.gmail.com",
                int.Parse(_config["Email:SmtpPort"] ?? "587"),
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public Task SendWelcomeEmailAsync(string email, string name) =>
        SendEmailAsync(email, "Welcome to Ezura Furniture", BuildWelcomeTemplate(name));

    public Task SendOrderConfirmationAsync(string email, string name, string orderNumber, decimal total) =>
        SendEmailAsync(email, $"Order Confirmed – #{orderNumber}",
            BuildOrderConfirmTemplate(name, orderNumber, total));

    public Task SendOrderStatusUpdateAsync(string email, string name, string orderNumber, string status) =>
        SendEmailAsync(email, $"Order Update – #{orderNumber}",
            BuildStatusUpdateTemplate(name, orderNumber, status));

    public Task SendPaymentReminderAsync(string email, string name, string orderNumber, decimal outstanding) =>
        SendEmailAsync(email, $"Payment Reminder – #{orderNumber}",
            BuildPaymentReminderTemplate(name, orderNumber, outstanding));

    public Task SendShippingUpdateAsync(string email, string name, string orderNumber, string trackingNumber) =>
        SendEmailAsync(email, $"Your Order is Shipped – #{orderNumber}",
            BuildShippingTemplate(name, orderNumber, trackingNumber));

    public Task SendPasswordResetAsync(string email, string resetLink) =>
        SendEmailAsync(email, "Reset Your Ezura Password",
            BuildPasswordResetTemplate(resetLink));

    private static string Wrap(string content) => $$"""
        <!DOCTYPE html><html><head><meta charset="utf-8">
        <style>
          body{font-family:'Georgia',serif;background:#0a0a0a;color:#f5f0eb;margin:0;padding:0}
          .w{max-width:600px;margin:40px auto;background:#111;border:1px solid #8B7355;border-radius:4px;overflow:hidden}
          .h{background:linear-gradient(135deg,#1a1a1a,#2a2014);padding:40px;text-align:center;border-bottom:2px solid #C9A84C}
          .h h1{color:#C9A84C;font-size:28px;letter-spacing:6px;margin:0;text-transform:uppercase}
          .c{padding:40px;line-height:1.8;color:#d4cfc9}
          .gold{color:#C9A84C}
          .btn{display:inline-block;background:#C9A84C;color:#0a0a0a;padding:14px 32px;text-decoration:none;font-weight:bold;letter-spacing:2px;text-transform:uppercase;margin:20px 0}
          .f{background:#0a0a0a;padding:20px 40px;text-align:center;font-size:12px;color:#666;border-top:1px solid #333}
        </style></head><body>
        <div class="w">
          <div class="h"><h1>EZURA</h1><p style="color:#8B7355;letter-spacing:2px;margin:8px 0 0">PREMIUM FURNITURE</p></div>
          <div class="c">{{content}}</div>
          <div class="f">&copy; {{DateTime.UtcNow.Year}} Ezura Furniture, Cairo, Egypt</div>
        </div></body></html>
        """;

    private static string BuildWelcomeTemplate(string name) =>
        Wrap($"<h2>Welcome, <span class='gold'>{name}</span></h2><p>Thank you for joining Ezura. Discover our premium handcrafted furniture.</p><a href='https://ezura.com/products' class='btn'>Explore Collection</a>");

    private static string BuildOrderConfirmTemplate(string name, string orderNumber, decimal total) =>
        Wrap($"<h2>Order Confirmed</h2><p>Dear <span class='gold'>{name}</span>,</p><p>Order <strong class='gold'>#{orderNumber}</strong> received. Total: <strong class='gold'>EGP {total:N2}</strong></p><a href='https://ezura.com/orders/{orderNumber}' class='btn'>Track Order</a>");

    private static string BuildStatusUpdateTemplate(string name, string orderNumber, string status) =>
        Wrap($"<h2>Order Update</h2><p>Dear <span class='gold'>{name}</span>,</p><p>Order <strong class='gold'>#{orderNumber}</strong> status: <strong class='gold'>{status}</strong></p><a href='https://ezura.com/orders/{orderNumber}' class='btn'>View Order</a>");

    private static string BuildPaymentReminderTemplate(string name, string orderNumber, decimal outstanding) =>
        Wrap($"<h2>Payment Reminder</h2><p>Dear <span class='gold'>{name}</span>,</p><p>Remaining balance on order <strong class='gold'>#{orderNumber}</strong>: <strong class='gold'>EGP {outstanding:N2}</strong></p><a href='https://ezura.com/orders/{orderNumber}' class='btn'>Pay Now</a>");

    private static string BuildShippingTemplate(string name, string orderNumber, string tracking) =>
        Wrap($"<h2>Your Order is on the Way</h2><p>Dear <span class='gold'>{name}</span>,</p><p>Order <strong class='gold'>#{orderNumber}</strong> dispatched. Tracking: <strong class='gold'>{tracking}</strong></p>");

    private static string BuildPasswordResetTemplate(string link) =>
        Wrap($"<h2>Reset Password</h2><p>Click the button below to reset your password. This link expires in 2 hours.</p><a href='{link}' class='btn'>Reset Password</a>");
}

// ============================================================
// AUDIT SERVICE
// ============================================================
public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork uow, ILogger<AuditService> logger)
    {
        _uow = uow; _logger = logger;
    }

    public async Task LogAsync(string action, string? userId = null, string? entityType = null,
        string? entityId = null, string? oldValues = null, string? newValues = null,
        string? ipAddress = null, string? userAgent = null, bool isSuccess = true, string? error = null)
    {
        try
        {
            var log = new AuditLog
            {
                Action = action, UserId = userId, EntityType = entityType,
                EntityId = entityId, OldValues = oldValues, NewValues = newValues,
                IpAddress = ipAddress, UserAgent = userAgent,
                IsSuccess = isSuccess, ErrorMessage = error
            };
            await _uow.AuditLogs.AddAsync(log);
            await _uow.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for {Action}", action);
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetRecentAsync(int count = 100) =>
        (await _uow.AuditLogs.GetRecentAsync(count)).Select(MapDto);

    public async Task<IEnumerable<AuditLogDto>> GetByUserAsync(string userId, int count = 50) =>
        (await _uow.AuditLogs.GetByUserAsync(userId, count)).Select(MapDto);

    private static AuditLogDto MapDto(AuditLog l) => new()
    {
        Id = l.Id, UserId = l.UserId, UserName = l.UserName, Action = l.Action,
        EntityType = l.EntityType, EntityId = l.EntityId, IpAddress = l.IpAddress,
        RequestUrl = l.RequestUrl, IsSuccess = l.IsSuccess, ErrorMessage = l.ErrorMessage,
        ExecutionTimeMs = l.ExecutionTimeMs, CreatedAt = l.CreatedAt
    };
}

// ============================================================
// NOTIFICATION SERVICE
// ============================================================
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork uow, ILogger<NotificationService> logger)
    {
        _uow = uow; _logger = logger;
    }

    public async Task SendAsync(string userId, string title, string message,
        NotificationType type, string? actionUrl = null)
    {
        try
        {
            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = userId, Title = title, Message = message,
                Type = type, ActionUrl = actionUrl
            });
            await _uow.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Notification send failed"); }
    }

    public Task SendToAdminsAsync(string title, string message, NotificationType type)
    {
        _logger.LogInformation("Admin alert — {Title}: {Message}", title, message);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<NotificationDto>> GetByUserAsync(string userId, bool unreadOnly = false)
    {
        var items = await _uow.Notifications.GetByUserIdAsync(userId, unreadOnly);
        return items.Select(n => new NotificationDto
        {
            Id = n.Id, Title = n.Title, Message = n.Message,
            Type = n.Type, IsRead = n.IsRead, ActionUrl = n.ActionUrl, CreatedAt = n.CreatedAt
        });
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _uow.Notifications.GetUnreadCountAsync(userId);

    public async Task MarkAsReadAsync(int id, string userId)
    {
        var n = await _uow.Notifications.GetByIdAsync(id);
        if (n != null && n.UserId == userId) { n.IsRead = true; await _uow.SaveChangesAsync(); }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _uow.Notifications.MarkAllAsReadAsync(userId);
        await _uow.SaveChangesAsync();
    }
}

// ============================================================
// CURRENCY SERVICE
// ============================================================
public class CurrencyService : ICurrencyService
{
    private readonly IUnitOfWork _uow;
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(IUnitOfWork uow, IHttpClientFactory http,
        IConfiguration config, ILogger<CurrencyService> logger)
    { _uow = uow; _http = http; _config = config; _logger = logger; }

    public async Task<decimal> ConvertAsync(decimal amount, string from, string to)
    {
        if (from == to) return amount;
        if (from == "EGP")
        {
            var rate = await _uow.Currencies.GetRateAsync(to);
            return rate != null ? Math.Round(amount * rate.Rate, 2) : amount;
        }
        var fromRate = await _uow.Currencies.GetRateAsync(from);
        if (fromRate == null) return amount;
        var inEgp = amount / fromRate.Rate;
        if (to == "EGP") return Math.Round(inEgp, 2);
        var toRate = await _uow.Currencies.GetRateAsync(to);
        return toRate != null ? Math.Round(inEgp * toRate.Rate, 2) : inEgp;
    }

    public async Task<IEnumerable<CurrencyDto>> GetSupportedCurrenciesAsync() =>
        (await _uow.Currencies.GetAllActiveAsync()).Select(r => new CurrencyDto
        {
            Code = r.ToCurrency, Name = r.CurrencyName,
            Symbol = r.Symbol, Rate = r.Rate, LastUpdated = r.LastUpdated
        });

    public async Task RefreshRatesAsync()
    {
        try
        {
            var client = _http.CreateClient("CurrencyApi");
            var json = await client.GetStringAsync("v4/latest/EGP");
            var data = JsonConvert.DeserializeObject<ExchangeApiResponse>(json);
            if (data?.Rates == null) return;

            var supported = new Dictionary<string, (string Name, string Symbol)>
            {
                ["USD"] = ("US Dollar", "$"), ["EUR"] = ("Euro", "€"),
                ["GBP"] = ("British Pound", "£"), ["SAR"] = ("Saudi Riyal", "﷼"),
                ["AED"] = ("UAE Dirham", "د.إ")
            };

            foreach (var (code, info) in supported)
            {
                if (!data.Rates.TryGetValue(code, out var rate)) continue;
                var existing = await _uow.Currencies.GetRateAsync(code);
                if (existing != null)
                {
                    existing.Rate = (decimal)rate;
                    existing.LastUpdated = DateTime.UtcNow;
                    _uow.Currencies.Update(existing);
                }
                else
                {
                    await _uow.Currencies.AddAsync(new CurrencyRate
                    {
                        ToCurrency = code, CurrencyName = info.Name,
                        Symbol = info.Symbol, Rate = (decimal)rate
                    });
                }
            }
            await _uow.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Currency refresh failed"); }
    }

    public string? DetectUserCurrency(string? ipAddress, string? acceptLanguage)
    {
        if (string.IsNullOrEmpty(acceptLanguage)) return "EGP";
        var lang = acceptLanguage.Split(',')[0].Trim().ToLower();
        return lang switch
        {
            var l when l.StartsWith("ar-ae") => "AED",
            var l when l.StartsWith("ar-sa") => "SAR",
            var l when l.StartsWith("en-gb") => "GBP",
            var l when l.StartsWith("de") || l.StartsWith("fr") || l.StartsWith("it") => "EUR",
            var l when l.StartsWith("en") => "USD",
            _ => "EGP"
        };
    }

    private class ExchangeApiResponse
    {
        [JsonProperty("rates")]
        public Dictionary<string, double>? Rates { get; set; }
    }
}

// ============================================================
// JWT SERVICE
// ============================================================
public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    public JwtService(IConfiguration config) { _config = config; }

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email,          user.Email ?? ""),
            new(ClaimTypes.Name,           user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:    _config["Jwt:Issuer"],
            audience:  _config["Jwt:Audience"],
            claims:    claims,
            expires:   DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "1440")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
