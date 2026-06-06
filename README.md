# EZURA FURNITURE — Complete Solution

## Project Overview
Full-stack ASP.NET Core 8 MVC application for **Ezura**, a premium Egyptian furniture brand.

---

## Solution Structure
```
Ezura/
├── src/
│   ├── Ezura.Core/              # Domain entities, interfaces, DTOs, enums
│   ├── Ezura.Infrastructure/    # EF Core, repositories, services, email, currency
│   ├── Ezura.Application/       # Business logic services, order/cart/inventory
│   └── Ezura.Web/               # MVC controllers, Razor views, API, SignalR
├── tests/
│   ├── Ezura.UnitTests/
│   └── Ezura.IntegrationTests/
└── scripts/
    └── sql/                     # Stored procedures, views, indexes
```

---

## Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server 2019+ or Azure SQL
- Visual Studio 2022 / VS Code / Rider

### 1. Clone & Configure
```bash
git clone <repo>
cd Ezura
```

Edit `src/Ezura.Web/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=EzuraDb;Trusted_Connection=True;TrustServerCertificate=true"
  },
  "Jwt": {
    "SecretKey": "YOUR-32-CHAR-SECRET-KEY-CHANGE-THIS"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "Username": "your@email.com",
    "Password": "your-app-password"
  }
}
```

### 2. Database Migration
```bash
cd src/Ezura.Web
dotnet ef migrations add InitialCreate --project ../Ezura.Infrastructure
dotnet ef database update
```

The app auto-seeds on first run:
- Default admin: `admin@ezura.com` / `Admin@123!`
- Roles: SuperAdmin, Manager, SalesEmployee, ProductionEmployee, ShippingEmployee, CustomerSupport, Customer
- Sample categories and products
- Currency rates

### 3. Run
```bash
dotnet run --project src/Ezura.Web
```

Open `https://localhost:5001`

---

## Admin Panel
Navigate to `/admin/dashboard`

| Role | Access |
|------|--------|
| SuperAdmin | Everything |
| Manager | Orders, Products, Customers, Reports, Inventory |
| SalesEmployee | Orders, Customers, Custom Requests |
| ProductionEmployee | Orders (production), Inventory |
| ShippingEmployee | Orders (shipping) |
| CustomerSupport | Orders (view), Customers |

---

## Key Features Implemented

### Public Website
- ✅ Luxury dark editorial design with gold accents
- ✅ Product catalog with filters, search, pagination
- ✅ Product details with gallery
- ✅ Shopping cart (session + user, auto-merge on login)
- ✅ Checkout with deposit/full payment support
- ✅ Custom furniture request with file uploads
- ✅ Portfolio section
- ✅ Customer accounts (register, login, orders, wishlist)
- ✅ Multi-currency (EGP, USD, EUR, GBP, SAR, AED)
- ✅ Real-time cart count updates
- ✅ Responsive mobile design

### Admin Panel
- ✅ Dashboard with live KPI cards + revenue charts
- ✅ Order management (full lifecycle, status tracking)
- ✅ Payment recording (deposits, balances, methods)
- ✅ Production status tracking
- ✅ Customer management with lifetime value
- ✅ Inventory with low-stock alerts
- ✅ Custom request management + quoting
- ✅ Report generation (revenue, sales, customers, inventory)
- ✅ Real-time notifications (SignalR)

### Security
- ✅ ASP.NET Identity with role-based authorization
- ✅ JWT for API endpoints
- ✅ Account lockout after 5 failed attempts
- ✅ Secure cookies (HttpOnly, SameSite=Strict, Secure)
- ✅ CSRF protection on all forms
- ✅ Security headers (CSP, X-Frame-Options, HSTS)
- ✅ Rate limiting (IP-based, per-endpoint)
- ✅ Audit logging for all admin actions
- ✅ Login history with IP/device tracking
- ✅ Soft-delete on all entities

### Architecture
- ✅ Clean Architecture (Core / Infrastructure / Application / Web)
- ✅ Repository Pattern + Unit of Work
- ✅ Service Layer
- ✅ Dependency Injection
- ✅ AutoMapper
- ✅ FluentValidation
- ✅ Serilog structured logging
- ✅ EF Core with SQL Server

---

## Production Deployment

### IIS
```xml
<!-- web.config -->
<aspNetCore processPath="dotnet" arguments=".\Ezura.Web.dll"
            stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY publish/ /app
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Ezura.Web.dll"]
```

```bash
dotnet publish src/Ezura.Web -c Release -o publish
docker build -t ezura .
docker run -p 80:80 ezura
```

### Environment Variables (production)
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<azure-sql-connection>
Jwt__SecretKey=<strong-random-32-char-key>
Email__Password=<smtp-password>
```

---

## API Reference

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/v1/products` | None | Paginated product list |
| `GET /api/v1/products/{id}` | None | Product details |
| `GET /api/v1/products/featured` | None | Featured products |
| `GET /api/v1/cart` | None | Get cart |
| `POST /api/v1/cart/add` | None | Add to cart |
| `GET /api/v1/orders/my-orders` | Customer | User's orders |
| `POST /api/v1/orders` | Customer | Create order |
| `GET /api/v1/currency` | None | Exchange rates |
| `GET /api/v1/admin/dashboard/stats` | Admin | Dashboard data |
| `GET /api/v1/admin/dashboard/revenue` | Admin | Revenue report |
| `GET /api/v1/notifications` | User | Notifications |

---

## Environment File Checklist

- [ ] `DefaultConnection` — SQL Server connection string
- [ ] `Jwt:SecretKey` — Minimum 32 characters, random
- [ ] `Email:*` — SMTP credentials
- [ ] `CurrencyApi:ApiKey` — From exchangerate-api.com (free tier available)
- [ ] `FileStorage:UploadPath` — Writeable directory

---

*Built with ❤️ for Ezura Furniture, Cairo, Egypt*
