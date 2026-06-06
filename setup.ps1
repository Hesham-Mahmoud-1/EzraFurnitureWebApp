# ═══════════════════════════════════════════════════════════
# EZURA FURNITURE — Setup Script
# Run from the Ezura\ root folder in PowerShell
# ═══════════════════════════════════════════════════════════

Write-Host "=== Ezura Furniture Setup ===" -ForegroundColor Cyan

# 1. Restore packages
Write-Host "`n[1/4] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore Ezura.sln
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Restore failed" -ForegroundColor Red; exit 1 }

# 2. Build to verify compilation
Write-Host "`n[2/4] Building solution..." -ForegroundColor Yellow
dotnet build Ezura.sln -c Debug --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Build failed" -ForegroundColor Red; exit 1 }

# 3. Create migration
Write-Host "`n[3/4] Creating EF Core migration..." -ForegroundColor Yellow
Set-Location src\Ezura.Web
dotnet ef migrations add InitialCreate --project ..\Ezura.Infrastructure --startup-project . -o Data\Migrations
if ($LASTEXITCODE -ne 0) {
    Write-Host "Migration may already exist, skipping..." -ForegroundColor DarkYellow
}

# 4. Apply migration
Write-Host "`n[4/4] Applying database migration..." -ForegroundColor Yellow
dotnet ef database update --project ..\Ezura.Infrastructure --startup-project .
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Migration failed — check connection string in appsettings.json" -ForegroundColor Red; exit 1 }

Set-Location ..\..

Write-Host "`n=== Setup Complete! ===" -ForegroundColor Green
Write-Host "Run:  cd src\Ezura.Web && dotnet run" -ForegroundColor Cyan
Write-Host "Open: https://localhost:5001" -ForegroundColor Cyan
Write-Host "Admin: admin@ezura.com / Admin@Ezura1!" -ForegroundColor Cyan
