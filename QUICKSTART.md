# EZURA — Quick Start Guide

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (NOT .NET 9)
- SQL Server LocalDB (included with Visual Studio) **or** SQL Server Express

> **Important:** You have .NET 9 SDK installed. The project targets .NET 8.
> Install .NET 8 SDK from the link above — both can coexist side-by-side.

## Step 1 — Check your SDK version
```powershell
dotnet --list-sdks
# You need 8.x.xxx listed. If not, install from https://dotnet.microsoft.com/download/dotnet/8
```

## Step 2 — Restore & build
```powershell
cd Ezura
dotnet restore Ezura.sln
dotnet build Ezura.sln
```

## Step 3 — Configure the database
Edit `src\Ezura.Web\appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EzuraDb;Trusted_Connection=True;TrustServerCertificate=true"
}
```
*For SQL Server Express, use:*
```
"Server=.\\SQLEXPRESS;Database=EzuraDb;Trusted_Connection=True;TrustServerCertificate=true"
```

## Step 4 — Run migrations
```powershell
cd src\Ezura.Web
dotnet ef migrations add InitialCreate --project ..\Ezura.Infrastructure --startup-project .
dotnet ef database update --project ..\Ezura.Infrastructure --startup-project .
```

## Step 5 — Run
```powershell
dotnet run
```
Open **http://localhost:5000**

## Default Admin Credentials
| Field | Value |
|-------|-------|
| Email | `admin@ezura.com` |
| Password | `Admin@Ezura1!` |
| URL | `/admin/dashboard` |

## Common Errors

### NETSDK1004 — Missing project.assets.json
Run `dotnet restore Ezura.sln` first, then retry.

### NU1101 — Package not found
Make sure `NuGet.config` is in the root folder and you have internet access.

### EF migration error — startup project
Always run EF commands **from** `src\Ezura.Web`:
```powershell
cd src\Ezura.Web
dotnet ef database update --project ..\Ezura.Infrastructure
```

### SDK version mismatch
The `global.json` file requests .NET 8. Either install .NET 8 SDK or
delete `global.json` if you want to use .NET 9 (minor changes may be needed).
