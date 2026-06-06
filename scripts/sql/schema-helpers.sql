-- ═══════════════════════════════════════════════════════════════
-- EZURA FURNITURE DATABASE SCHEMA
-- SQL Server / Azure SQL
-- Run after: dotnet ef database update
-- ═══════════════════════════════════════════════════════════════

-- This file is for reference. Use EF Core migrations in production:
--   cd src/Ezura.Web
--   dotnet ef migrations add InitialCreate --project ../Ezura.Infrastructure
--   dotnet ef database update

-- ── Stored Procedure: Monthly Revenue Summary ────────────────────
CREATE OR ALTER PROCEDURE sp_GetMonthlyRevenueSummary
    @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Year = ISNULL(@Year, YEAR(GETUTCDATE()));

    SELECT
        MONTH(CreatedAt)           AS MonthNumber,
        DATENAME(MONTH, CreatedAt) AS MonthName,
        COUNT(*)                   AS OrderCount,
        SUM(TotalAmount)           AS TotalRevenue,
        SUM(DepositAmount)         AS TotalDeposits,
        SUM(RemainingAmount)       AS TotalOutstanding,
        AVG(TotalAmount)           AS AverageOrderValue
    FROM Orders
    WHERE
        YEAR(CreatedAt) = @Year
        AND IsDeleted   = 0
        AND Status NOT IN (8, 9) -- Cancelled, Refunded
    GROUP BY MONTH(CreatedAt), DATENAME(MONTH, CreatedAt)
    ORDER BY MonthNumber;
END
GO

-- ── Stored Procedure: Customer Lifetime Value ────────────────────
CREATE OR ALTER PROCEDURE sp_GetCustomerLifetimeValue
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.Id,
        u.FirstName + ' ' + u.LastName  AS FullName,
        u.Email,
        COUNT(o.Id)                      AS TotalOrders,
        SUM(o.TotalAmount)               AS LifetimeValue,
        SUM(o.DepositAmount)             AS TotalPaid,
        SUM(o.RemainingAmount)           AS TotalOutstanding,
        MIN(o.CreatedAt)                 AS FirstOrderDate,
        MAX(o.CreatedAt)                 AS LastOrderDate
    FROM ezura_users u
    LEFT JOIN Orders o ON o.UserId = u.Id
        AND o.IsDeleted = 0
        AND o.Status NOT IN (8, 9)
    WHERE (@UserId IS NULL OR u.Id = @UserId)
    GROUP BY u.Id, u.FirstName, u.LastName, u.Email
    ORDER BY LifetimeValue DESC;
END
GO

-- ── Stored Procedure: Low Stock Report ──────────────────────────
CREATE OR ALTER PROCEDURE sp_GetLowStockReport
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ii.Id,
        ii.Name,
        ii.Category,
        ii.Unit,
        ii.CurrentStock,
        ii.MinimumStock,
        ii.CurrentStock - ii.MinimumStock AS StockDeficit,
        s.Name AS SupplierName,
        s.Email AS SupplierEmail,
        s.Phone AS SupplierPhone
    FROM InventoryItems ii
    LEFT JOIN Suppliers s ON s.Id = ii.SupplierId
    WHERE
        ii.IsActive   = 1
        AND ii.IsDeleted = 0
        AND ii.CurrentStock <= ii.MinimumStock
    ORDER BY (ii.CurrentStock - ii.MinimumStock);
END
GO

-- ── Stored Procedure: Outstanding Payments ───────────────────────
CREATE OR ALTER PROCEDURE sp_GetOutstandingPayments
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.Id            AS OrderId,
        o.OrderNumber,
        o.CustomerName,
        o.CustomerEmail,
        o.CustomerPhone,
        o.TotalAmount,
        o.DepositAmount,
        o.RemainingAmount,
        o.Status,
        o.CreatedAt,
        DATEDIFF(DAY, o.CreatedAt, GETUTCDATE()) AS DaysSinceOrder
    FROM Orders o
    WHERE
        o.IsDeleted       = 0
        AND o.RemainingAmount > 0
        AND o.Status NOT IN (8, 9) -- Not Cancelled/Refunded
    ORDER BY o.RemainingAmount DESC;
END
GO

-- ── View: Dashboard Summary ──────────────────────────────────────
CREATE OR ALTER VIEW vw_DashboardSummary AS
SELECT
    (SELECT COUNT(*) FROM Orders WHERE IsDeleted=0 AND Status NOT IN(8,9)) AS TotalOrders,
    (SELECT COUNT(*) FROM Orders WHERE IsDeleted=0 AND Status=0)           AS PendingOrders,
    (SELECT SUM(TotalAmount) FROM Orders WHERE IsDeleted=0 AND Status NOT IN(8,9)) AS TotalRevenue,
    (SELECT SUM(RemainingAmount) FROM Orders WHERE IsDeleted=0 AND Status NOT IN(8,9)) AS OutstandingBalance,
    (SELECT COUNT(*) FROM Products WHERE IsDeleted=0 AND IsAvailable=1)    AS ActiveProducts,
    (SELECT COUNT(*) FROM Products WHERE IsDeleted=0 AND StockQuantity<=LowStockThreshold) AS LowStockProducts,
    (SELECT COUNT(*) FROM ezura_users WHERE IsActive=1)                    AS TotalCustomers,
    (SELECT COUNT(*) FROM CustomRequests WHERE IsDeleted=0 AND Status=0)   AS PendingRequests;
GO

-- ── Index suggestions for query performance ─────────────────────
-- (EF Core creates most of these; these are additional analytics indexes)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Orders_CreatedAt_Status')
    CREATE NONCLUSTERED INDEX IX_Orders_CreatedAt_Status
    ON Orders(CreatedAt DESC, Status)
    INCLUDE (TotalAmount, DepositAmount, RemainingAmount, CustomerId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Payments_OrderId_Type')
    CREATE NONCLUSTERED INDEX IX_Payments_OrderId_Type
    ON Payments(OrderId, Type)
    INCLUDE (Amount, Status, CreatedAt);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_AuditLogs_CreatedAt')
    CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAt
    ON AuditLogs(CreatedAt DESC)
    INCLUDE (UserId, Action, EntityType, IsSuccess);
GO

PRINT 'Ezura DB schema helpers created successfully.';
