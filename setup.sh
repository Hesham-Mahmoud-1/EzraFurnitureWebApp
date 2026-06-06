#!/usr/bin/env bash
set -e
echo "=== Ezura Furniture Setup ==="

echo "[1/4] Restoring packages..."
dotnet restore Ezura.sln

echo "[2/4] Building..."
dotnet build Ezura.sln -c Debug --no-restore

echo "[3/4] Creating migration..."
cd src/Ezura.Web
dotnet ef migrations add InitialCreate \
  --project ../Ezura.Infrastructure \
  --startup-project . \
  -o Data/Migrations || echo "Migration may exist, skipping"

echo "[4/4] Updating database..."
dotnet ef database update \
  --project ../Ezura.Infrastructure \
  --startup-project .

cd ../..
echo ""
echo "=== Setup Complete ==="
echo "Run:  cd src/Ezura.Web && dotnet run"
echo "Open: https://localhost:5001"
echo "Admin: admin@ezura.com / Admin@Ezura1!"
