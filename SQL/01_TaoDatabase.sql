-- Tạo database và các bảng nghiệp vụ khi cài đặt trên máy mới.
-- Nếu AuctionArenaDB đã có dữ liệu, không chạy lại file này.
-- Chọn đúng SQL Server trước khi thực hiện trong SSMS.


SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
USE master;
GO
IF DB_ID(N'AuctionArenaDB') IS NOT NULL
BEGIN
    PRINT N'AuctionArenaDB đã tồn tại. Bỏ qua toàn bộ phần khởi tạo để bảo vệ dữ liệu.';
    SET NOEXEC ON;
END;
GO
CREATE DATABASE AuctionArenaDB;
GO
USE AuctionArenaDB;
GO
CREATE TABLE dbo.Roles (RoleId INT IDENTITY PRIMARY KEY, RoleCode VARCHAR(40) NOT NULL UNIQUE, RoleName NVARCHAR(100) NOT NULL UNIQUE, IsActive BIT NOT NULL DEFAULT 1);
CREATE TABLE dbo.Permissions (PermissionId INT IDENTITY PRIMARY KEY, PermissionCode VARCHAR(60) NOT NULL UNIQUE, PermissionName NVARCHAR(100) NOT NULL);
CREATE TABLE dbo.RolePermissions (RoleId INT NOT NULL REFERENCES dbo.Roles(RoleId), PermissionId INT NOT NULL REFERENCES dbo.Permissions(PermissionId), CanView BIT NOT NULL DEFAULT 0, CanCreate BIT NOT NULL DEFAULT 0, CanEdit BIT NOT NULL DEFAULT 0, CanDelete BIT NOT NULL DEFAULT 0, CanPrint BIT NOT NULL DEFAULT 0, CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId));
-- Tài khoản tham chiếu đến nhóm quyền; thông tin quyền được gán riêng ở bảng RolePermissions.
CREATE TABLE dbo.Users (UserId INT IDENTITY PRIMARY KEY, Username VARCHAR(60) NOT NULL UNIQUE, FullName NVARCHAR(120) NOT NULL, RoleId INT NOT NULL REFERENCES dbo.Roles(RoleId), PasswordHash VARCHAR(64) NOT NULL, PasswordSalt VARCHAR(32) NOT NULL, HashIterations INT NOT NULL DEFAULT 150000 CHECK (HashIterations>=100000), IsActive BIT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), RowVersion ROWVERSION);
CREATE TABLE dbo.Customers (CustomerId INT IDENTITY PRIMARY KEY, CustomerCode VARCHAR(30) NOT NULL UNIQUE, FullName NVARCHAR(120) NOT NULL, Phone VARCHAR(20) NULL, Email VARCHAR(150) NULL, IdentityNumber VARCHAR(30) NULL, MemberTier VARCHAR(12) NOT NULL DEFAULT 'Normal' CHECK (MemberTier IN ('Normal','VIP')), IsBlacklisted BIT NOT NULL DEFAULT 0, HasOutstandingDebt BIT NOT NULL DEFAULT 0, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.Consignors (ConsignorId INT IDENTITY PRIMARY KEY, ConsignorCode VARCHAR(30) NOT NULL UNIQUE, FullName NVARCHAR(120) NOT NULL, Phone VARCHAR(20), IdentityNumber VARCHAR(30), Address NVARCHAR(250));
CREATE TABLE dbo.Categories (CategoryId INT IDENTITY PRIMARY KEY, CategoryName NVARCHAR(100) NOT NULL UNIQUE);
CREATE TABLE dbo.Items (ItemId INT IDENTITY PRIMARY KEY, ItemCode VARCHAR(30) NOT NULL UNIQUE, ItemName NVARCHAR(160) NOT NULL, CategoryId INT NOT NULL REFERENCES dbo.Categories(CategoryId), ConsignorId INT NOT NULL REFERENCES dbo.Consignors(ConsignorId), Description NVARCHAR(1000), EstimatePrice DECIMAL(19,2) NOT NULL CHECK (EstimatePrice >= 0), AppraisedPrice DECIMAL(19,2) NULL CHECK (AppraisedPrice >= 0), ItemStatus VARCHAR(24) NOT NULL DEFAULT 'PendingAppraisal' CHECK (ItemStatus IN ('PendingAppraisal','InStock','InAuction','Sold','Delivered','Unsold','Returned')), CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), RowVersion ROWVERSION);
CREATE TABLE dbo.ItemImages (ItemImageId INT IDENTITY PRIMARY KEY, ItemId INT NOT NULL REFERENCES dbo.Items(ItemId), RelativePath NVARCHAR(500) NOT NULL, IsPrimary BIT NOT NULL DEFAULT 0);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ItemImages') AND name = N'UX_ItemImages_Primary')
    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_ItemImages_Primary ON dbo.ItemImages(ItemId) WHERE IsPrimary = 1';
CREATE TABLE dbo.ConsignmentContracts (ContractId INT IDENTITY PRIMARY KEY, ContractCode VARCHAR(30) NOT NULL UNIQUE, ConsignorId INT NOT NULL REFERENCES dbo.Consignors(ConsignorId), SignedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), ContractStatus VARCHAR(16) NOT NULL CHECK (ContractStatus IN ('Draft','Active','Closed','Cancelled')), Notes NVARCHAR(500));
CREATE TABLE dbo.ConsignmentContractDetails (ContractDetailId INT IDENTITY PRIMARY KEY, ContractId INT NOT NULL REFERENCES dbo.ConsignmentContracts(ContractId), ItemId INT NOT NULL REFERENCES dbo.Items(ItemId), CONSTRAINT UQ_ContractItem UNIQUE(ItemId));
CREATE TABLE dbo.StockReceipts (ReceiptId INT IDENTITY PRIMARY KEY, ReceiptCode VARCHAR(30) NOT NULL UNIQUE, ContractId INT NOT NULL REFERENCES dbo.ConsignmentContracts(ContractId), ReceivedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), ReceivedByUserId INT NOT NULL REFERENCES dbo.Users(UserId), Notes NVARCHAR(300));
CREATE TABLE dbo.StockReceiptDetails (ReceiptDetailId INT IDENTITY PRIMARY KEY, ReceiptId INT NOT NULL REFERENCES dbo.StockReceipts(ReceiptId), ItemId INT NOT NULL REFERENCES dbo.Items(ItemId), CONSTRAINT UQ_ReceiptItem UNIQUE(ItemId));
CREATE TABLE dbo.AuctionSessions (SessionId INT IDENTITY PRIMARY KEY, SessionCode VARCHAR(30) NOT NULL UNIQUE, SessionName NVARCHAR(160) NOT NULL, StartsAt DATETIME2 NOT NULL, EndsAt DATETIME2 NOT NULL, SessionStatus VARCHAR(16) NOT NULL CHECK (SessionStatus IN ('Planned','Open','Closed','Cancelled')), ApprovedByUserId INT NULL REFERENCES dbo.Users(UserId), CONSTRAINT CK_SessionDates CHECK (EndsAt > StartsAt));
CREATE TABLE dbo.AuctionLots (LotId INT IDENTITY PRIMARY KEY, SessionId INT NOT NULL REFERENCES dbo.AuctionSessions(SessionId), ItemId INT NOT NULL REFERENCES dbo.Items(ItemId), LotNumber INT NOT NULL CHECK (LotNumber > 0), StartingPrice DECIMAL(19,2) NOT NULL CHECK (StartingPrice >= 0), ReservePrice DECIMAL(19,2) NOT NULL CHECK (ReservePrice >= 0), BidIncrement DECIMAL(19,2) NOT NULL CHECK (BidIncrement > 0), CurrentPrice DECIMAL(19,2) NOT NULL CHECK (CurrentPrice >= 0), LeadingCustomerId INT NULL REFERENCES dbo.Customers(CustomerId), WinningCustomerId INT NULL REFERENCES dbo.Customers(CustomerId), HammerPrice DECIMAL(19,2) NULL CHECK(HammerPrice >= 0), OpensAt DATETIME2 NOT NULL, ClosesAt DATETIME2 NOT NULL, LotStatus VARCHAR(20) NOT NULL CHECK (LotStatus IN ('Planned','Open','Sold','ReserveNotMet','Cancelled','Defaulted')), RowVersion ROWVERSION, CONSTRAINT UQ_SessionLotNumber UNIQUE(SessionId,LotNumber), CONSTRAINT UQ_SessionItem UNIQUE(SessionId,ItemId), CONSTRAINT CK_LotTimes CHECK (ClosesAt > OpensAt), CONSTRAINT CK_LotWinning CHECK ((LotStatus <> 'Sold') OR (WinningCustomerId IS NOT NULL AND HammerPrice IS NOT NULL)));
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuctionLots') AND name = N'UX_ActiveItemLot')
    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_ActiveItemLot ON dbo.AuctionLots(ItemId) WHERE LotStatus IN (''Planned'',''Open'')';
CREATE TABLE dbo.Registrations (RegistrationId INT IDENTITY PRIMARY KEY, SessionId INT NOT NULL REFERENCES dbo.AuctionSessions(SessionId), CustomerId INT NOT NULL REFERENCES dbo.Customers(CustomerId), PaddleNumber INT NOT NULL CHECK(PaddleNumber > 0), DepositRequired DECIMAL(19,2) NOT NULL CHECK(DepositRequired >= 0), DepositPaid DECIMAL(19,2) NOT NULL DEFAULT 0 CHECK(DepositPaid >= 0), DepositApplied DECIMAL(19,2) NOT NULL DEFAULT 0 CHECK(DepositApplied >= 0), DepositRefunded DECIMAL(19,2) NOT NULL DEFAULT 0 CHECK(DepositRefunded >= 0), DepositForfeited DECIMAL(19,2) NOT NULL DEFAULT 0 CHECK(DepositForfeited >= 0), RegistrationStatus VARCHAR(16) NOT NULL CHECK(RegistrationStatus IN ('Pending','Active','Closed','Blocked')), RegisteredAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), RowVersion ROWVERSION, CONSTRAINT UQ_RegistrationCustomer UNIQUE(SessionId,CustomerId), CONSTRAINT UQ_RegistrationPaddle UNIQUE(SessionId,PaddleNumber), CONSTRAINT CK_DepositBalances CHECK(DepositPaid >= DepositApplied+DepositRefunded+DepositForfeited));
CREATE TABLE dbo.Bids (BidId BIGINT IDENTITY PRIMARY KEY, LotId INT NOT NULL REFERENCES dbo.AuctionLots(LotId), CustomerId INT NOT NULL REFERENCES dbo.Customers(CustomerId), RegistrationId INT NOT NULL REFERENCES dbo.Registrations(RegistrationId), Amount DECIMAL(19,2) NOT NULL CHECK(Amount > 0), BidAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), EnteredByUserId INT NOT NULL REFERENCES dbo.Users(UserId));
CREATE TABLE dbo.Invoices (InvoiceId INT IDENTITY PRIMARY KEY, InvoiceCode VARCHAR(30) NOT NULL UNIQUE, SessionId INT NOT NULL REFERENCES dbo.AuctionSessions(SessionId), CustomerId INT NOT NULL REFERENCES dbo.Customers(CustomerId), IssuedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), DueAt DATETIME2 NOT NULL, InvoiceStatus VARCHAR(16) NOT NULL CHECK (InvoiceStatus IN ('Unpaid','PartiallyPaid','Paid','Cancelled','Overdue')), HammerSubtotal DECIMAL(19,2) NOT NULL CHECK(HammerSubtotal >= 0), BuyerFee DECIMAL(19,2) NOT NULL CHECK(BuyerFee >= 0), VipDiscount DECIMAL(19,2) NOT NULL CHECK(VipDiscount >= 0), VatAmount DECIMAL(19,2) NOT NULL CHECK(VatAmount >= 0), DepositApplied DECIMAL(19,2) NOT NULL CHECK(DepositApplied >= 0), TotalDue DECIMAL(19,2) NOT NULL CHECK(TotalDue >= 0), PaidAmount DECIMAL(19,2) NOT NULL DEFAULT 0 CHECK(PaidAmount >= 0), BuyerFeeRate DECIMAL(9,6) NOT NULL, VatRate DECIMAL(9,6) NOT NULL, VipDiscountRate DECIMAL(9,6) NOT NULL, RowVersion ROWVERSION, CONSTRAINT UQ_InvoiceSessionCustomer UNIQUE(SessionId,CustomerId), CONSTRAINT CK_InvoiceTotal CHECK (TotalDue = HammerSubtotal + BuyerFee - VipDiscount + VatAmount - DepositApplied), CONSTRAINT CK_InvoicePaid CHECK (PaidAmount <= TotalDue));
CREATE TABLE dbo.InvoiceDetails (InvoiceDetailId INT IDENTITY PRIMARY KEY, InvoiceId INT NOT NULL REFERENCES dbo.Invoices(InvoiceId), LotId INT NOT NULL REFERENCES dbo.AuctionLots(LotId), HammerPrice DECIMAL(19,2) NOT NULL CHECK (HammerPrice >= 0), CONSTRAINT UQ_InvoicedLot UNIQUE(LotId));
CREATE TABLE dbo.Payments (PaymentId INT IDENTITY PRIMARY KEY, PaymentCode VARCHAR(30) NOT NULL UNIQUE, InvoiceId INT NULL REFERENCES dbo.Invoices(InvoiceId), RegistrationId INT NULL REFERENCES dbo.Registrations(RegistrationId), PaymentType VARCHAR(20) NOT NULL CHECK(PaymentType IN ('Deposit','Invoice','Refund','Forfeit','DepositTransfer')), Amount DECIMAL(19,2) NOT NULL CHECK (Amount>0), PaymentMethod VARCHAR(16) NOT NULL CHECK(PaymentMethod IN ('Cash','BankTransfer','Internal')), PaidAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), ReceivedByUserId INT NOT NULL REFERENCES dbo.Users(UserId), Notes NVARCHAR(300), CONSTRAINT CK_PaymentReference CHECK(InvoiceId IS NOT NULL OR RegistrationId IS NOT NULL));
CREATE TABLE dbo.StockIssues (IssueId INT IDENTITY PRIMARY KEY, IssueCode VARCHAR(30) NOT NULL UNIQUE, InvoiceId INT NOT NULL REFERENCES dbo.Invoices(InvoiceId), IssuedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), IssuedByUserId INT NOT NULL REFERENCES dbo.Users(UserId), RecipientName NVARCHAR(120) NOT NULL, Notes NVARCHAR(300));
CREATE TABLE dbo.StockIssueDetails (IssueDetailId INT IDENTITY PRIMARY KEY, IssueId INT NOT NULL REFERENCES dbo.StockIssues(IssueId), ItemId INT NOT NULL REFERENCES dbo.Items(ItemId), CONSTRAINT UQ_IssuedItem UNIQUE(ItemId));
CREATE TABLE dbo.SellerPayouts (PayoutId INT IDENTITY PRIMARY KEY, PayoutCode VARCHAR(30) NOT NULL UNIQUE, LotId INT NOT NULL UNIQUE REFERENCES dbo.AuctionLots(LotId), ConsignorId INT NOT NULL REFERENCES dbo.Consignors(ConsignorId), HammerPrice DECIMAL(19,2) NOT NULL CHECK(HammerPrice >= 0), SellerFeeRate DECIMAL(9,6) NOT NULL CHECK(SellerFeeRate BETWEEN 0 AND 1), SellerFee DECIMAL(19,2) NOT NULL CHECK(SellerFee >= 0), PayoutAmount DECIMAL(19,2) NOT NULL CHECK(PayoutAmount >= 0), PayoutAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), PaidByUserId INT NOT NULL REFERENCES dbo.Users(UserId), CONSTRAINT CK_PayoutFormula CHECK(PayoutAmount = HammerPrice - SellerFee));
CREATE TABLE dbo.CustomerViolations (ViolationId INT IDENTITY PRIMARY KEY, CustomerId INT NOT NULL REFERENCES dbo.Customers(CustomerId), SessionId INT NOT NULL REFERENCES dbo.AuctionSessions(SessionId), LotId INT NULL REFERENCES dbo.AuctionLots(LotId), ViolationType VARCHAR(24) NOT NULL CHECK(ViolationType IN ('NonPayment','BiddingMisconduct','Other')), Description NVARCHAR(300) NOT NULL, OccurredAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.SystemSettings (SettingKey VARCHAR(60) NOT NULL PRIMARY KEY, SettingValue NVARCHAR(200) NOT NULL, Description NVARCHAR(300), UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
CREATE TABLE dbo.AuditLogs (AuditLogId BIGINT IDENTITY PRIMARY KEY, UserId INT NULL REFERENCES dbo.Users(UserId), ActionCode VARCHAR(70) NOT NULL, EntityName VARCHAR(60) NULL, EntityId VARCHAR(60) NULL, Description NVARCHAR(1000) NULL, OccurredAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Bids') AND name = N'IX_Bids_Lot_Time')
    EXEC sys.sp_executesql N'CREATE INDEX IX_Bids_Lot_Time ON dbo.Bids(LotId,BidAt DESC) INCLUDE(Amount,CustomerId)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuctionLots') AND name = N'IX_Lots_Session_Status')
    EXEC sys.sp_executesql N'CREATE INDEX IX_Lots_Session_Status ON dbo.AuctionLots(SessionId,LotStatus)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuctionLots') AND name = N'IX_Lots_Item')
    EXEC sys.sp_executesql N'CREATE INDEX IX_Lots_Item ON dbo.AuctionLots(ItemId)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Items') AND name = N'IX_Items_Status_Category')
    EXEC sys.sp_executesql N'CREATE INDEX IX_Items_Status_Category ON dbo.Items(ItemStatus,CategoryId)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Invoices') AND name = N'IX_Invoices_IssuedAt_Status')
    EXEC sys.sp_executesql N'CREATE INDEX IX_Invoices_IssuedAt_Status ON dbo.Invoices(IssuedAt,InvoiceStatus)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Registrations') AND name = N'IX_Registrations_Customer')
    EXEC sys.sp_executesql N'CREATE INDEX IX_Registrations_Customer ON dbo.Registrations(CustomerId,SessionId)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments') AND name = N'IX_Payments_Invoice')
    EXEC sys.sp_executesql N'CREATE INDEX IX_Payments_Invoice ON dbo.Payments(InvoiceId,PaidAt)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments') AND name = N'IX_Payments_Registration')
    EXEC sys.sp_executesql N'CREATE INDEX IX_Payments_Registration ON dbo.Payments(RegistrationId,PaidAt)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditLogs') AND name = N'IX_AuditLogs_Time')
    EXEC sys.sp_executesql N'CREATE INDEX IX_AuditLogs_Time ON dbo.AuditLogs(OccurredAt DESC)';
GO
-- Khởi tạo nhóm quyền và chức năng, không tạo tài khoản đăng nhập mặc định.
INSERT dbo.Roles(RoleCode,RoleName) VALUES
('ADMIN',N'Quản trị hệ thống'),('MANAGER',N'Quản lý sàn'),('WAREHOUSE',N'Kho và thẩm định'),('AUCTIONEER',N'Đấu giá viên'),('CASHIER',N'Thu ngân - kế toán'),('RECEPTION',N'Lễ tân');
INSERT dbo.Permissions(PermissionCode,PermissionName) VALUES
('USERS',N'Quản lý người dùng'),('ROLES',N'Quản lý nhóm quyền'),('SETTINGS',N'Tham số hệ thống'),('AUDIT',N'Nhật ký hệ thống'),('CUSTOMERS',N'Khách hàng'),('CONSIGNORS',N'Chủ hàng'),('CATEGORIES',N'Loại hiện vật'),('ITEMS',N'Hiện vật'),('CONTRACTS',N'Hợp đồng ký gửi'),('STOCK_RECEIPTS',N'Phiếu nhập kho'),('STOCK_ISSUES',N'Phiếu xuất kho'),('SESSIONS',N'Phiên đấu giá'),('LOTS',N'Lot đấu giá'),('REGISTRATIONS',N'Đăng ký và đặt cọc'),('BIDS',N'Điều hành và trả giá'),('INVOICES',N'Hóa đơn thanh toán'),('PAYOUTS',N'Chi trả chủ hàng'),('REPORTS',N'Báo cáo');
INSERT dbo.RolePermissions(RoleId,PermissionId,CanView,CanCreate,CanEdit,CanDelete,CanPrint)
SELECT r.RoleId,p.PermissionId,
       1,
       CASE WHEN r.RoleCode='ADMIN' THEN 1 WHEN p.PermissionCode IN ('AUDIT','REPORTS') THEN 0 ELSE 1 END,
       CASE WHEN r.RoleCode='ADMIN' THEN 1 WHEN p.PermissionCode IN ('AUDIT','REPORTS') THEN 0 ELSE 1 END,
       CASE WHEN r.RoleCode='ADMIN' THEN 1 ELSE 0 END,
       CASE WHEN p.PermissionCode IN ('REPORTS','INVOICES','STOCK_ISSUES','CONTRACTS') THEN 1 ELSE CASE WHEN r.RoleCode='ADMIN' THEN 1 ELSE 0 END END
FROM dbo.Roles r CROSS JOIN dbo.Permissions p
WHERE r.RoleCode='ADMIN'
 OR (r.RoleCode='MANAGER' AND p.PermissionCode IN ('SETTINGS','AUDIT','CUSTOMERS','CONSIGNORS','CATEGORIES','ITEMS','CONTRACTS','STOCK_RECEIPTS','STOCK_ISSUES','SESSIONS','LOTS','REGISTRATIONS','BIDS','INVOICES','PAYOUTS','REPORTS'))
 OR (r.RoleCode='WAREHOUSE' AND p.PermissionCode IN ('CONSIGNORS','CATEGORIES','ITEMS','CONTRACTS','STOCK_RECEIPTS','STOCK_ISSUES'))
 OR (r.RoleCode='AUCTIONEER' AND p.PermissionCode IN ('SESSIONS','LOTS','REGISTRATIONS','BIDS','CUSTOMERS'))
 OR (r.RoleCode='CASHIER' AND p.PermissionCode IN ('CUSTOMERS','REGISTRATIONS','INVOICES','PAYOUTS','STOCK_ISSUES','REPORTS'))
 OR (r.RoleCode='RECEPTION' AND p.PermissionCode IN ('CUSTOMERS','SESSIONS','REGISTRATIONS'));
UPDATE rp SET CanCreate=0,CanEdit=0,CanDelete=0 FROM dbo.RolePermissions rp JOIN dbo.Permissions p ON p.PermissionId=rp.PermissionId JOIN dbo.Roles r ON r.RoleId=rp.RoleId WHERE r.RoleCode<>'ADMIN' AND p.PermissionCode IN ('AUDIT','REPORTS');
-- Giá trị phí và thời hạn mặc định được lưu trong bảng SystemSettings.
INSERT dbo.SystemSettings(SettingKey,SettingValue,Description) VALUES
('BuyerFeeRate',N'0.05',N'Phí người mua: 5% giá búa'),
('SellerFeeRate',N'0.08',N'Phí chủ hàng: 8% giá búa'),
('DepositRate',N'0.10',N'Tỷ lệ đặt cọc chuẩn'),
('VatRate',N'0.10',N'VAT trên phí dịch vụ ròng của người mua'),
('VipDiscountRate',N'0.10',N'VIP giảm 10% trên phí người mua'),
('AntiSnipingSeconds',N'30',N'Bid trong 30 giây cuối sẽ gia hạn'),
('PaymentDeadlineDays',N'3',N'Hạn thanh toán tính từ ngày kết thúc phiên');
GO
SELECT 'Users' AS TableName,COUNT(*) AS [RowCount] FROM dbo.Users
UNION ALL SELECT 'Roles',COUNT(*) FROM dbo.Roles
UNION ALL SELECT 'Customers',COUNT(*) FROM dbo.Customers
UNION ALL SELECT 'Consignors',COUNT(*) FROM dbo.Consignors
UNION ALL SELECT 'Items',COUNT(*) FROM dbo.Items
UNION ALL SELECT 'AuctionSessions',COUNT(*) FROM dbo.AuctionSessions
UNION ALL SELECT 'AuctionLots',COUNT(*) FROM dbo.AuctionLots
UNION ALL SELECT 'Invoices',COUNT(*) FROM dbo.Invoices;


GO



-- Các cột về hồ sơ, kho và phí ký gửi.
USE [AuctionArenaDB];
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Customers',N'U') IS NULL OR OBJECT_ID(N'dbo.AuctionLots',N'U') IS NULL OR
       OBJECT_ID(N'dbo.Registrations',N'U') IS NULL OR OBJECT_ID(N'dbo.InvoiceDetails',N'U') IS NULL OR
       OBJECT_ID(N'dbo.StockReceiptDetails',N'U') IS NULL OR OBJECT_ID(N'dbo.SellerPayouts',N'U') IS NULL OR
       COL_LENGTH('dbo.AuctionLots','RowVersion') IS NULL OR
       COL_LENGTH('dbo.Registrations','DepositApplied') IS NULL OR
       COL_LENGTH('dbo.Invoices','BuyerFeeRate') IS NULL
        THROW 51000, N'Cấu trúc cơ sở dữ liệu không tương thích với Auction Arena. Hãy kiểm tra tên server, database và các bảng bắt buộc.', 1;
    IF COL_LENGTH('dbo.Customers','Address') IS NULL EXEC(N'ALTER TABLE dbo.Customers ADD Address NVARCHAR(250) NULL');
    IF COL_LENGTH('dbo.Customers','PhotoPath') IS NULL EXEC(N'ALTER TABLE dbo.Customers ADD PhotoPath NVARCHAR(400) NULL');
    IF COL_LENGTH('dbo.Consignors','BankName') IS NULL EXEC(N'ALTER TABLE dbo.Consignors ADD BankName NVARCHAR(120) NULL');
    IF COL_LENGTH('dbo.Consignors','BankAccount') IS NULL EXEC(N'ALTER TABLE dbo.Consignors ADD BankAccount NVARCHAR(60) NULL');
    IF COL_LENGTH('dbo.Consignors','SellerFeeRate') IS NULL EXEC(N'ALTER TABLE dbo.Consignors ADD SellerFeeRate DECIMAL(9,6) NULL');
    IF COL_LENGTH('dbo.Categories','CategoryCode') IS NULL EXEC(N'ALTER TABLE dbo.Categories ADD CategoryCode VARCHAR(30) NULL');
    IF COL_LENGTH('dbo.Categories','Description') IS NULL EXEC(N'ALTER TABLE dbo.Categories ADD Description NVARCHAR(500) NULL');
    IF COL_LENGTH('dbo.Items','Origin') IS NULL EXEC(N'ALTER TABLE dbo.Items ADD Origin NVARCHAR(300) NULL');
    IF COL_LENGTH('dbo.Items','ConditionDescription') IS NULL EXEC(N'ALTER TABLE dbo.Items ADD ConditionDescription NVARCHAR(400) NULL');
    IF COL_LENGTH('dbo.Items','WarehouseLocation') IS NULL EXEC(N'ALTER TABLE dbo.Items ADD WarehouseLocation NVARCHAR(100) NULL');
    IF COL_LENGTH('dbo.ConsignmentContracts','SellerFeeRate') IS NULL EXEC(N'ALTER TABLE dbo.ConsignmentContracts ADD SellerFeeRate DECIMAL(9,6) NULL');
    IF COL_LENGTH('dbo.ConsignmentContracts','ExpiresAt') IS NULL EXEC(N'ALTER TABLE dbo.ConsignmentContracts ADD ExpiresAt DATETIME2 NULL');
    IF COL_LENGTH('dbo.StockReceiptDetails','ConditionAtReceipt') IS NULL EXEC(N'ALTER TABLE dbo.StockReceiptDetails ADD ConditionAtReceipt NVARCHAR(400) NULL');
    IF COL_LENGTH('dbo.StockReceiptDetails','LocationAtReceipt') IS NULL EXEC(N'ALTER TABLE dbo.StockReceiptDetails ADD LocationAtReceipt NVARCHAR(100) NULL');
    IF COL_LENGTH('dbo.AuctionLots','SellerFeeRateLocked') IS NULL EXEC(N'ALTER TABLE dbo.AuctionLots ADD SellerFeeRateLocked DECIMAL(9,6) NULL');
    IF COL_LENGTH('dbo.AuctionLots','DepositRequired') IS NULL EXEC(N'ALTER TABLE dbo.AuctionLots ADD DepositRequired DECIMAL(19,2) NULL');
    IF COL_LENGTH('dbo.SellerPayouts','PaymentMethod') IS NULL EXEC(N'ALTER TABLE dbo.SellerPayouts ADD PaymentMethod VARCHAR(16) NULL');
    IF OBJECT_ID(N'dbo.ConsignorReturns',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ConsignorReturns
        (ReturnId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
         ReturnCode VARCHAR(30) NOT NULL UNIQUE,
         LotId INT NOT NULL UNIQUE REFERENCES dbo.AuctionLots(LotId),
         ItemId INT NOT NULL REFERENCES dbo.Items(ItemId),
         ConsignorId INT NOT NULL REFERENCES dbo.Consignors(ConsignorId),
         RecipientName NVARCHAR(120) NOT NULL,
         Notes NVARCHAR(300) NULL,
         ReturnedAt DATETIME2 NOT NULL,
         ReturnedByUserId INT NOT NULL REFERENCES dbo.Users(UserId));
    END;
    IF OBJECT_ID(N'dbo.InvoiceAdjustments',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.InvoiceAdjustments
        (AdjustmentId INT IDENTITY(1,1) PRIMARY KEY,
         AdjustmentCode VARCHAR(30) NOT NULL UNIQUE,
         InvoiceId INT NOT NULL REFERENCES dbo.Invoices(InvoiceId),
         RegistrationId INT NOT NULL REFERENCES dbo.Registrations(RegistrationId),
         AdjustmentType VARCHAR(32) NOT NULL,
         Amount DECIMAL(19,2) NOT NULL CHECK (Amount>0),
         Reason NVARCHAR(300) NULL,
         CreatedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
         CreatedAt DATETIME2 NOT NULL,
         CONSTRAINT UQ_InvoiceAdjustmentType UNIQUE(InvoiceId,AdjustmentType));
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Categories') AND name = 'UX_Categories_CategoryCode_NotNull')
        EXEC(N'CREATE UNIQUE INDEX UX_Categories_CategoryCode_NotNull ON dbo.Categories(CategoryCode) WHERE CategoryCode IS NOT NULL');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Consignors') AND name='CK_Consignors_SellerFeeRate_0_1')
        EXEC(N'ALTER TABLE dbo.Consignors WITH CHECK ADD CONSTRAINT CK_Consignors_SellerFeeRate_0_1 CHECK(SellerFeeRate IS NULL OR SellerFeeRate BETWEEN 0 AND 1)');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.ConsignmentContracts') AND name='CK_Contracts_SellerFeeRate_0_1')
        EXEC(N'ALTER TABLE dbo.ConsignmentContracts WITH CHECK ADD CONSTRAINT CK_Contracts_SellerFeeRate_0_1 CHECK(SellerFeeRate IS NULL OR SellerFeeRate BETWEEN 0 AND 1)');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.AuctionLots') AND name='CK_Lots_SellerFeeRate_0_1')
        EXEC(N'ALTER TABLE dbo.AuctionLots WITH CHECK ADD CONSTRAINT CK_Lots_SellerFeeRate_0_1 CHECK(SellerFeeRateLocked IS NULL OR SellerFeeRateLocked BETWEEN 0 AND 1)');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.AuctionLots') AND name='CK_Lots_DepositRequired_Positive')
        EXEC(N'ALTER TABLE dbo.AuctionLots WITH CHECK ADD CONSTRAINT CK_Lots_DepositRequired_Positive CHECK(DepositRequired IS NULL OR DepositRequired>0)');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.SellerPayouts') AND name='CK_SellerPayouts_Method')
        EXEC(N'ALTER TABLE dbo.SellerPayouts WITH CHECK ADD CONSTRAINT CK_SellerPayouts_Method CHECK (PaymentMethod IS NULL OR PaymentMethod IN (''Cash'',''BankTransfer''))');
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ConsignorReturns') AND name='UX_ConsignorReturns_Item')
        EXEC(N'CREATE UNIQUE INDEX UX_ConsignorReturns_Item ON dbo.ConsignorReturns(ItemId)');
    COMMIT TRANSACTION;
    PRINT N'Cấu trúc hồ sơ, kho và phí ký gửi đã sẵn sàng.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
GO


-- Trạng thái phiên, nghĩa vụ hoàn cọc và lệnh chi chủ hàng.
USE [AuctionArenaDB];
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.ConsignmentContracts',N'U') IS NULL OR
       OBJECT_ID(N'dbo.StockReceiptDetails',N'U') IS NULL OR
       OBJECT_ID(N'dbo.AuctionSessions',N'U') IS NULL OR
       OBJECT_ID(N'dbo.SellerPayouts',N'U') IS NULL OR
       COL_LENGTH('dbo.Customers','Address') IS NULL OR
       COL_LENGTH('dbo.Items','WarehouseLocation') IS NULL OR
       COL_LENGTH('dbo.ConsignmentContracts','SellerFeeRate') IS NULL OR
       COL_LENGTH('dbo.SellerPayouts','PaymentMethod') IS NULL
       THROW 51020,N'Thiếu bảng hoặc cột bắt buộc cho phiên đấu giá. Hãy kiểm tra kết quả khởi tạo cơ sở dữ liệu.',1;

    DECLARE @checkName SYSNAME;
    DECLARE @dropSessionCheckSql NVARCHAR(MAX);
    DECLARE session_checks CURSOR LOCAL FAST_FORWARD FOR
      SELECT cc.name FROM sys.check_constraints AS cc
      WHERE cc.parent_object_id=OBJECT_ID(N'dbo.AuctionSessions')
        AND cc.definition LIKE '%SessionStatus%';
    OPEN session_checks;
    FETCH NEXT FROM session_checks INTO @checkName;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @dropSessionCheckSql = N'ALTER TABLE dbo.AuctionSessions DROP CONSTRAINT ' + QUOTENAME(@checkName);
        EXEC sys.sp_executesql @dropSessionCheckSql;
        FETCH NEXT FROM session_checks INTO @checkName;
    END;
    CLOSE session_checks;
    DEALLOCATE session_checks;
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id=OBJECT_ID(N'dbo.AuctionSessions')
          AND name='CK_AuctionSessions_Workflow_20261031')
        EXEC(N'ALTER TABLE dbo.AuctionSessions WITH CHECK ADD CONSTRAINT CK_AuctionSessions_Workflow_20261031
              CHECK (SessionStatus IN (''Draft'',''Planned'',''Open'',''Closed'',''Cancelled''))');

    IF OBJECT_ID(N'dbo.DepositRefundObligations',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DepositRefundObligations
        (ObligationId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
         SessionId INT NOT NULL REFERENCES dbo.AuctionSessions(SessionId),
         RegistrationId INT NOT NULL UNIQUE REFERENCES dbo.Registrations(RegistrationId),
         DueAmount DECIMAL(19,2) NOT NULL CHECK(DueAmount>0),
         ObligationStatus VARCHAR(16) NOT NULL
             CONSTRAINT CK_RefundObligationStatus_20261031 CHECK(ObligationStatus IN ('Pending','Settled')),
         CreatedAt DATETIME2 NOT NULL,
         SettledAt DATETIME2 NULL);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DepositRefundObligations') AND name = N'IX_RefundObligations_Session_Status')
            EXEC sys.sp_executesql N'CREATE INDEX IX_RefundObligations_Session_Status ON dbo.DepositRefundObligations(SessionId,ObligationStatus)';
    END;

    IF OBJECT_ID(N'dbo.SellerPayoutOrders',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SellerPayoutOrders
        (OrderId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
         LotId INT NOT NULL UNIQUE REFERENCES dbo.AuctionLots(LotId),
         ConsignorId INT NOT NULL REFERENCES dbo.Consignors(ConsignorId),
         HammerPrice DECIMAL(19,2) NOT NULL CHECK(HammerPrice>=0),
         SellerFeeRate DECIMAL(9,6) NOT NULL CHECK(SellerFeeRate BETWEEN 0 AND 1),
         SellerFee DECIMAL(19,2) NOT NULL CHECK(SellerFee>=0),
         PayoutAmount DECIMAL(19,2) NOT NULL CHECK(PayoutAmount>=0),
         OrderStatus VARCHAR(16) NOT NULL
             CONSTRAINT CK_SellerPayoutOrderStatus_20261031 CHECK(OrderStatus IN ('Pending','Paid')),
         CreatedAt DATETIME2 NOT NULL,
         CreatedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
         PaidAt DATETIME2 NULL,
         CONSTRAINT CK_SellerPayoutOrderFormula_20261031
             CHECK(PayoutAmount=HammerPrice-SellerFee));
    END;

    IF OBJECT_ID(N'dbo.InvoiceReviewCases',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.InvoiceReviewCases
        (ReviewId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
         InvoiceId INT NOT NULL REFERENCES dbo.Invoices(InvoiceId),
         RemainingAtReview DECIMAL(19,2) NOT NULL CHECK(RemainingAtReview>0),
         ReviewReason NVARCHAR(300) NOT NULL,
         ReviewStatus VARCHAR(16) NOT NULL
             CONSTRAINT CK_InvoiceReviewStatus_20261031 CHECK(ReviewStatus IN ('Open','Resolved')),
         CreatedAt DATETIME2 NOT NULL,
         CreatedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
         ResolvedAt DATETIME2 NULL);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InvoiceReviewCases') AND name = N'UX_InvoiceReviewCases_OneOpen_20261031')
            EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_InvoiceReviewCases_OneOpen_20261031 ON dbo.InvoiceReviewCases(InvoiceId) WHERE ReviewStatus=''Open''';
    END;
    COMMIT TRANSACTION;
    PRINT N'Cấu trúc phiên đấu giá và xử lý chứng từ đã sẵn sàng.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
GO


-- Lưu số tiền cần hoàn, lệnh chi và hóa đơn đang chờ đối soát.
USE [AuctionArenaDB];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
PRINT N'Server: ' + CONVERT(nvarchar(256),SERVERPROPERTY('ServerName'));
PRINT N'Database: ' + DB_NAME();
GO
BEGIN TRY
    BEGIN TRANSACTION;

    IF DB_NAME() <> N'AuctionArenaDB' OR
       OBJECT_ID(N'dbo.Registrations',N'U') IS NULL OR
       OBJECT_ID(N'dbo.AuctionSessions',N'U') IS NULL OR
       OBJECT_ID(N'dbo.AuctionLots',N'U') IS NULL OR
       OBJECT_ID(N'dbo.Consignors',N'U') IS NULL OR
       OBJECT_ID(N'dbo.Users',N'U') IS NULL OR
       OBJECT_ID(N'dbo.Invoices',N'U') IS NULL OR
       OBJECT_ID(N'dbo.SellerPayouts',N'U') IS NULL OR
       COL_LENGTH(N'dbo.Registrations', N'DepositApplied') IS NULL OR
       COL_LENGTH(N'dbo.Invoices', N'BuyerFeeRate') IS NULL OR
       COL_LENGTH(N'dbo.AuctionLots', N'RowVersion') IS NULL
       THROW 51040,N'Cau truc AuctionArenaDB khong tuong thich. Dung tao bang; kiem tra SQL Server va cac bang bat buoc.',1;

    IF OBJECT_ID(N'dbo.DepositRefundObligations',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DepositRefundObligations
        (
            ObligationId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            SessionId INT NOT NULL REFERENCES dbo.AuctionSessions(SessionId),
            RegistrationId INT NOT NULL UNIQUE REFERENCES dbo.Registrations(RegistrationId),
            DueAmount DECIMAL(19,2) NOT NULL CHECK (DueAmount > 0),
            ObligationStatus VARCHAR(16) NOT NULL
                CONSTRAINT CK_RefundObligationStatus_20261031 CHECK (ObligationStatus IN ('Pending','Settled')),
            CreatedAt DATETIME2 NOT NULL,
            SettledAt DATETIME2 NULL
        );
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DepositRefundObligations') AND name = N'IX_RefundObligations_Session_Status')
            EXEC sys.sp_executesql N'CREATE INDEX IX_RefundObligations_Session_Status ON dbo.DepositRefundObligations(SessionId, ObligationStatus)';
        PRINT N'Da tao: dbo.DepositRefundObligations';
    END
    ELSE PRINT N'Da ton tai: dbo.DepositRefundObligations (giu nguyen)';

    IF OBJECT_ID(N'dbo.SellerPayoutOrders',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SellerPayoutOrders
        (
            OrderId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            LotId INT NOT NULL UNIQUE REFERENCES dbo.AuctionLots(LotId),
            ConsignorId INT NOT NULL REFERENCES dbo.Consignors(ConsignorId),
            HammerPrice DECIMAL(19,2) NOT NULL CHECK (HammerPrice >= 0),
            SellerFeeRate DECIMAL(9,6) NOT NULL CHECK (SellerFeeRate BETWEEN 0 AND 1),
            SellerFee DECIMAL(19,2) NOT NULL CHECK (SellerFee >= 0),
            PayoutAmount DECIMAL(19,2) NOT NULL CHECK (PayoutAmount >= 0),
            OrderStatus VARCHAR(16) NOT NULL
                CONSTRAINT CK_SellerPayoutOrderStatus_20261031 CHECK (OrderStatus IN ('Pending','Paid')),
            CreatedAt DATETIME2 NOT NULL,
            CreatedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
            PaidAt DATETIME2 NULL,
            CONSTRAINT CK_SellerPayoutOrderFormula_20261031 CHECK (PayoutAmount = HammerPrice - SellerFee)
        );
        PRINT N'Da tao: dbo.SellerPayoutOrders';
    END
    ELSE PRINT N'Da ton tai: dbo.SellerPayoutOrders (giu nguyen)';

    IF OBJECT_ID(N'dbo.InvoiceReviewCases',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.InvoiceReviewCases
        (
            ReviewId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            InvoiceId INT NOT NULL REFERENCES dbo.Invoices(InvoiceId),
            RemainingAtReview DECIMAL(19,2) NOT NULL CHECK (RemainingAtReview > 0),
            ReviewReason NVARCHAR(300) NOT NULL,
            ReviewStatus VARCHAR(16) NOT NULL
                CONSTRAINT CK_InvoiceReviewStatus_20261031 CHECK (ReviewStatus IN ('Open','Resolved')),
            CreatedAt DATETIME2 NOT NULL,
            CreatedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
            ResolvedAt DATETIME2 NULL
        );
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InvoiceReviewCases') AND name = N'UX_InvoiceReviewCases_OneOpen_20261031')
            EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_InvoiceReviewCases_OneOpen_20261031 ON dbo.InvoiceReviewCases(InvoiceId) WHERE ReviewStatus=''Open''';
        PRINT N'Da tao: dbo.InvoiceReviewCases';
    END
    ELSE PRINT N'Da ton tai: dbo.InvoiceReviewCases (giu nguyen)';

    IF EXISTS (
       SELECT 1 FROM (VALUES
          (N'DepositRefundObligations',N'ObligationId'),
          (N'DepositRefundObligations',N'SessionId'),
          (N'DepositRefundObligations',N'RegistrationId'),
          (N'DepositRefundObligations',N'DueAmount'),
          (N'DepositRefundObligations',N'ObligationStatus'),
          (N'DepositRefundObligations',N'CreatedAt'),
          (N'DepositRefundObligations',N'SettledAt'),
          (N'SellerPayoutOrders',N'OrderId'),
          (N'SellerPayoutOrders',N'LotId'),
          (N'SellerPayoutOrders',N'ConsignorId'),
          (N'SellerPayoutOrders',N'HammerPrice'),
          (N'SellerPayoutOrders',N'SellerFeeRate'),
          (N'SellerPayoutOrders',N'SellerFee'),
          (N'SellerPayoutOrders',N'PayoutAmount'),
          (N'SellerPayoutOrders',N'OrderStatus'),
          (N'SellerPayoutOrders',N'CreatedAt'),
          (N'SellerPayoutOrders',N'CreatedByUserId'),
          (N'SellerPayoutOrders',N'PaidAt'),
          (N'InvoiceReviewCases',N'ReviewId'),
          (N'InvoiceReviewCases',N'InvoiceId'),
          (N'InvoiceReviewCases',N'RemainingAtReview'),
          (N'InvoiceReviewCases',N'ReviewReason'),
          (N'InvoiceReviewCases',N'ReviewStatus'),
          (N'InvoiceReviewCases',N'CreatedAt'),
          (N'InvoiceReviewCases',N'CreatedByUserId'),
          (N'InvoiceReviewCases',N'ResolvedAt')
       ) req(TableName,ColumnName)
       WHERE COL_LENGTH(N'dbo.'+req.TableName,req.ColumnName) IS NULL)
       THROW 51041,N'Bang da ton tai nhung thieu cot. Kiem tra cau truc va sao luu du lieu truoc khi xu ly.',1;

    COMMIT TRANSACTION;
    PRINT N'Cau truc xu ly chung tu da san sang.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
SELECT x.TableName AS Bang,
       CASE WHEN OBJECT_ID(N'dbo.'+x.TableName,N'U') IS NOT NULL THEN N'DA CO' ELSE N'THIEU' END AS TrangThai
FROM (VALUES (N'DepositRefundObligations'),(N'SellerPayoutOrders'),(N'InvoiceReviewCases')) x(TableName);
GO
GO


-- Các thông tin phục vụ khóa tài khoản, đổi mật khẩu và quản lý nhóm quyền.
USE [AuctionArenaDB];
GO
SET XACT_ABORT ON;
BEGIN TRY
  BEGIN TRANSACTION;
  IF OBJECT_ID(N'dbo.Users',N'U') IS NULL OR OBJECT_ID(N'dbo.Roles',N'U') IS NULL
     THROW 51104,N'Thiếu bảng Users/Roles; kiểm tra đúng server và database trước.',1;
  IF COL_LENGTH('dbo.Users','Email') IS NULL
    ALTER TABLE dbo.Users ADD Email NVARCHAR(150) NULL;
  IF COL_LENGTH('dbo.Users','Phone') IS NULL
    ALTER TABLE dbo.Users ADD Phone NVARCHAR(20) NULL;
  IF COL_LENGTH('dbo.Users','LockReason') IS NULL
    ALTER TABLE dbo.Users ADD LockReason NVARCHAR(250) NULL;
  IF COL_LENGTH('dbo.Users','LastLoginAt') IS NULL
    ALTER TABLE dbo.Users ADD LastLoginAt DATETIME2 NULL;
  IF COL_LENGTH('dbo.Users','PasswordChangedAt') IS NULL
    ALTER TABLE dbo.Users ADD PasswordChangedAt DATETIME2 NULL;
  IF COL_LENGTH('dbo.Users','MustChangePassword') IS NULL
    ALTER TABLE dbo.Users ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_AA_Users_ForceChange DEFAULT 0;
  IF COL_LENGTH('dbo.Users','FailedPasswordAttempts') IS NULL
    ALTER TABLE dbo.Users ADD FailedPasswordAttempts INT NOT NULL CONSTRAINT DF_AA_Users_Failed DEFAULT 0;
  IF COL_LENGTH('dbo.Users','LockedUntil') IS NULL
    ALTER TABLE dbo.Users ADD LockedUntil DATETIME2 NULL;
  IF COL_LENGTH('dbo.Roles','Description') IS NULL
    ALTER TABLE dbo.Roles ADD [Description] NVARCHAR(300) NULL;
  COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
SELECT N'Users' AS TableName, COUNT_BIG(*) AS Records FROM dbo.Users
UNION ALL SELECT N'Roles',COUNT_BIG(*) FROM dbo.Roles;
GO
GO


-- Thuộc tính hồ sơ khách hàng, chủ hàng và kết quả thẩm định hiện vật.
USE [AuctionArenaDB];
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Customers',N'U') IS NULL OR OBJECT_ID(N'dbo.Consignors',N'U') IS NULL
       OR OBJECT_ID(N'dbo.Categories',N'U') IS NULL OR OBJECT_ID(N'dbo.Items',N'U') IS NULL
       OR COL_LENGTH('dbo.Items','RowVersion') IS NULL
       THROW 51022,N'Cơ sở dữ liệu không tương thích hoặc thiếu bảng bắt buộc; dừng xử lý cấu trúc.',1;
    IF COL_LENGTH('dbo.Customers','DateOfBirth') IS NULL ALTER TABLE dbo.Customers ADD DateOfBirth DATETIME2 NULL;
    IF COL_LENGTH('dbo.Customers','Gender') IS NULL ALTER TABLE dbo.Customers ADD Gender VARCHAR(12) NULL;
    IF COL_LENGTH('dbo.Customers','CustomerStatus') IS NULL
        ALTER TABLE dbo.Customers ADD CustomerStatus VARCHAR(15) NOT NULL CONSTRAINT DF_AA_Customers_Status DEFAULT 'Active' WITH VALUES;
    IF COL_LENGTH('dbo.Customers','BlacklistReason') IS NULL ALTER TABLE dbo.Customers ADD BlacklistReason NVARCHAR(300) NULL;
    IF COL_LENGTH('dbo.Customers','Notes') IS NULL ALTER TABLE dbo.Customers ADD Notes NVARCHAR(500) NULL;
    IF COL_LENGTH('dbo.Customers','PhotoData') IS NULL ALTER TABLE dbo.Customers ADD PhotoData VARBINARY(MAX) NULL;
    IF COL_LENGTH('dbo.Consignors','ConsignorType') IS NULL
        ALTER TABLE dbo.Consignors ADD ConsignorType VARCHAR(20) NOT NULL CONSTRAINT DF_AA_Consignor_Type DEFAULT 'Individual' WITH VALUES;
    IF COL_LENGTH('dbo.Consignors','RepresentativeName') IS NULL ALTER TABLE dbo.Consignors ADD RepresentativeName NVARCHAR(120) NULL;
    IF COL_LENGTH('dbo.Consignors','Email') IS NULL ALTER TABLE dbo.Consignors ADD Email VARCHAR(150) NULL;
    IF COL_LENGTH('dbo.Consignors','BankAccountHolder') IS NULL ALTER TABLE dbo.Consignors ADD BankAccountHolder NVARCHAR(120) NULL;
    IF COL_LENGTH('dbo.Consignors','ConsignorStatus') IS NULL
        ALTER TABLE dbo.Consignors ADD ConsignorStatus VARCHAR(16) NOT NULL CONSTRAINT DF_AA_Consignor_Status DEFAULT 'Active' WITH VALUES;
    IF COL_LENGTH('dbo.Consignors','Notes') IS NULL ALTER TABLE dbo.Consignors ADD Notes NVARCHAR(500) NULL;
    IF COL_LENGTH('dbo.Categories','SortOrder') IS NULL
        ALTER TABLE dbo.Categories ADD SortOrder INT NOT NULL CONSTRAINT DF_AA_Category_Sort DEFAULT (0) WITH VALUES;
    IF COL_LENGTH('dbo.Categories','IsActive') IS NULL
        ALTER TABLE dbo.Categories ADD IsActive BIT NOT NULL CONSTRAINT DF_AA_Category_Active DEFAULT (1) WITH VALUES;
    IF COL_LENGTH('dbo.Items','Era') IS NULL ALTER TABLE dbo.Items ADD Era NVARCHAR(120) NULL;
    IF COL_LENGTH('dbo.Items','Dimensions') IS NULL ALTER TABLE dbo.Items ADD Dimensions NVARCHAR(120) NULL;
    IF COL_LENGTH('dbo.Items','Material') IS NULL ALTER TABLE dbo.Items ADD Material NVARCHAR(120) NULL;
    IF COL_LENGTH('dbo.Items','EstimatePriceHigh') IS NULL ALTER TABLE dbo.Items ADD EstimatePriceHigh DECIMAL(19,2) NULL;
    IF COL_LENGTH('dbo.Items','AppraisalNotes') IS NULL ALTER TABLE dbo.Items ADD AppraisalNotes NVARCHAR(400) NULL;
    IF COL_LENGTH('dbo.Items','AppraisedByUserId') IS NULL ALTER TABLE dbo.Items ADD AppraisedByUserId INT NULL;
    IF COL_LENGTH('dbo.Items','AppraisedAt') IS NULL ALTER TABLE dbo.Items ADD AppraisedAt DATETIME2 NULL;
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_AA_Items_Appraiser')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Items WITH CHECK ADD CONSTRAINT FK_AA_Items_Appraiser FOREIGN KEY(AppraisedByUserId) REFERENCES dbo.Users(UserId)';
    IF OBJECT_ID(N'dbo.CustomerTierHistory',N'U') IS NULL
        CREATE TABLE dbo.CustomerTierHistory
        (HistoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
         CustomerId INT NOT NULL REFERENCES dbo.Customers(CustomerId),
         OldTier VARCHAR(12) NULL, NewTier VARCHAR(12) NOT NULL,
         ChangedByUserId INT NOT NULL REFERENCES dbo.Users(UserId),
         ChangedAt DATETIME2 NOT NULL);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CustomerTierHistory') AND name = N'IX_AA_TierHistory_Customer')
            EXEC sys.sp_executesql N'CREATE INDEX IX_AA_TierHistory_Customer ON dbo.CustomerTierHistory(CustomerId,ChangedAt DESC)';
    DECLARE @old SYSNAME, @sql NVARCHAR(MAX);
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
        SELECT name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Customers')
          AND definition LIKE N'%MemberTier%';
    OPEN c;FETCH NEXT FROM c INTO @old;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @sql=N'ALTER TABLE dbo.Customers DROP CONSTRAINT '+QUOTENAME(@old);
        EXEC sp_executesql @sql;
        FETCH NEXT FROM c INTO @old;
    END;
    CLOSE c;DEALLOCATE c;
    ALTER TABLE dbo.Customers WITH CHECK ADD CONSTRAINT CK_AA_CustomerTier_Dot2
        CHECK(MemberTier IN ('Normal','Silver','Gold','VIP'));
    DECLARE c2 CURSOR LOCAL FAST_FORWARD FOR
        SELECT name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Items')
          AND definition LIKE N'%ItemStatus%';
    OPEN c2;FETCH NEXT FROM c2 INTO @old;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @sql=N'ALTER TABLE dbo.Items DROP CONSTRAINT '+QUOTENAME(@old);
        EXEC sp_executesql @sql;
        FETCH NEXT FROM c2 INTO @old;
    END;
    CLOSE c2;DEALLOCATE c2;
    ALTER TABLE dbo.Items WITH CHECK ADD CONSTRAINT CK_AA_ItemStatus_Dot2
        CHECK(ItemStatus IN ('PendingAppraisal','NeedInfo','Approved','Rejected','InStock','InAuction','Sold','Delivered','Unsold','Returned'));
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Items') AND name='CK_AA_Items_PriceRange_Dot2')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Items WITH CHECK ADD CONSTRAINT CK_AA_Items_PriceRange_Dot2 CHECK(EstimatePriceHigh IS NULL OR EstimatePriceHigh>=EstimatePrice)';
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Categories') AND name='CK_AA_Categories_Sort_Dot2')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Categories WITH CHECK ADD CONSTRAINT CK_AA_Categories_Sort_Dot2 CHECK(SortOrder>=0)';
    IF NOT EXISTS(SELECT 1 FROM dbo.SystemSettings WHERE SettingKey='SilverDiscountRate')
        INSERT dbo.SystemSettings(SettingKey,SettingValue,Description,UpdatedAt)
        VALUES('SilverDiscountRate',N'0',N'Giảm giá hạng Bạc; quản trị viên cấu hình',SYSUTCDATETIME());
    IF NOT EXISTS(SELECT 1 FROM dbo.SystemSettings WHERE SettingKey='GoldDiscountRate')
        INSERT dbo.SystemSettings(SettingKey,SettingValue,Description,UpdatedAt)
        VALUES('GoldDiscountRate',N'0',N'Giảm giá hạng Vàng; quản trị viên cấu hình',SYSUTCDATETIME());
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
SELECT 'Customers' AS TableName,COUNT_BIG(*) AS RowsSaved FROM dbo.Customers
UNION ALL SELECT 'Consignors',COUNT_BIG(*) FROM dbo.Consignors
UNION ALL SELECT 'Categories',COUNT_BIG(*) FROM dbo.Categories
UNION ALL SELECT 'Items',COUNT_BIG(*) FROM dbo.Items;
GO
GO


-- Tên người trực tiếp bàn giao hiện vật khi nhập kho.
USE [AuctionArenaDB];
GO
IF OBJECT_ID(N'dbo.StockReceipts',N'U') IS NULL
    THROW 51300,N'Thiếu bảng phiếu nhập kho. Hãy kiểm tra đúng server và cơ sở dữ liệu.',1;
GO
IF COL_LENGTH('dbo.StockReceipts','DeliveredBy') IS NULL
    ALTER TABLE dbo.StockReceipts ADD DeliveredBy NVARCHAR(160) NULL;
GO
SELECT COUNT_BIG(*) AS ExistingReceipts FROM dbo.StockReceipts;
GO
GO


-- Cấu trúc thanh toán và xuất kho.
USE [AuctionArenaDB];
GO
-- Chọn cơ sở dữ liệu Auction Arena trước khi chạy tệp này trong SSMS.
-- Chỉ bổ sung cấu trúc còn thiếu; các giá trị trên chứng từ hiện có được giữ nguyên.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
   OR COL_LENGTH(N'dbo.Users', N'HashIterations') IS NULL
   OR COL_LENGTH(N'dbo.Invoices', N'BuyerFeeRate') IS NULL
   OR COL_LENGTH(N'dbo.AuctionLots', N'RowVersion') IS NULL
   OR COL_LENGTH(N'dbo.Registrations', N'DepositApplied') IS NULL
   OR COL_LENGTH(N'dbo.Payments', N'PaymentId') IS NULL
   OR COL_LENGTH(N'dbo.SellerPayouts', N'PayoutId') IS NULL
   OR COL_LENGTH(N'dbo.StockIssues', N'IssueId') IS NULL
   OR COL_LENGTH(N'dbo.StockIssueDetails', N'IssueDetailId') IS NULL
   OR COL_LENGTH(N'dbo.SellerPayoutOrders', N'OrderStatus') IS NULL
    THROW 51400, N'Cơ sở dữ liệu không có cấu trúc nền tảng của Auction Arena. Hãy kiểm tra tên server và database.', 1;

DECLARE @RequiredColumns TABLE
(
    TableName SYSNAME,
    ColumnName SYSNAME,
    TypeName SYSNAME,
    MaxLength SMALLINT
);
INSERT @RequiredColumns (TableName, ColumnName, TypeName, MaxLength) VALUES
    (N'Payments', N'TransactionReference', N'nvarchar', 200),
    (N'SellerPayouts', N'TransferReference', N'nvarchar', 200),
    (N'StockIssues', N'WorkflowStatus', N'varchar', 16),
    (N'StockIssues', N'DraftPayload', N'nvarchar', -1),
    (N'StockIssues', N'RecipientDocument', N'nvarchar', 60),
    (N'StockIssues', N'RecipientRelation', N'nvarchar', 320),
    (N'StockIssues', N'DeliveryMethod', N'nvarchar', 40),
    (N'StockIssueDetails', N'ConditionAtDelivery', N'nvarchar', 800),
    (N'StockIssueDetails', N'LocationAtIssue', N'nvarchar', 200);

-- Cột đã có nhưng sai kiểu cần quản trị viên kiểm tra, không tự chuyển đổi dữ liệu.
IF EXISTS
(
    SELECT 1
    FROM @RequiredColumns r
    JOIN sys.columns c ON c.object_id = OBJECT_ID(N'dbo.' + r.TableName)
                      AND c.name = r.ColumnName
    WHERE TYPE_NAME(c.system_type_id) <> r.TypeName
       OR c.max_length <> r.MaxLength OR c.is_nullable = 0 OR c.is_computed = 1
)
    THROW 51401, N'Cột thanh toán hoặc xuất kho đã có nhưng không đúng kiểu dữ liệu. Hãy kiểm tra cấu trúc trước khi tiếp tục.', 1;

-- Database đã đủ cấu trúc chỉ cần quyền đọc, không yêu cầu quyền ALTER mỗi lần mở.
IF NOT EXISTS
   (SELECT 1 FROM @RequiredColumns WHERE COL_LENGTH(N'dbo.' + TableName, ColumnName) IS NULL)
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments') AND name = N'UX_AA_Payments_TransactionReference_D4' AND is_disabled = 0)
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SellerPayouts') AND name = N'UX_AA_Payouts_TransferReference_D4' AND is_disabled = 0)
   AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.StockIssues') AND name = N'CK_AA_StockIssues_DeliveryMethod_D4' AND is_disabled = 0 AND is_not_trusted = 0)
   AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.StockIssues') AND name = N'CK_AA_StockIssueStatus_D4' AND is_disabled = 0 AND is_not_trusted = 0)
   AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.SellerPayoutOrders') AND name = N'CK_AA_PayoutOrderStatus_D4' AND is_disabled = 0 AND is_not_trusted = 0)
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.SellerPayoutOrders') AND name = N'CK_SellerPayoutOrderStatus_20261031')
    RETURN;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult INT;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'AuctionArena.PaymentDeliverySchema',
        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 15000;
    IF @LockResult < 0
        THROW 51402, N'Cơ sở dữ liệu đang được chuẩn bị ở máy khác. Hãy mở lại ứng dụng sau ít phút.', 1;

    -- Kiểm tra lại sau khi có khóa để hai máy không cùng thêm một cột.
    IF COL_LENGTH(N'dbo.Payments', N'TransactionReference') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Payments ADD TransactionReference NVARCHAR(100) NULL';
    IF COL_LENGTH(N'dbo.SellerPayouts', N'TransferReference') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SellerPayouts ADD TransferReference NVARCHAR(100) NULL';
    IF COL_LENGTH(N'dbo.StockIssues', N'WorkflowStatus') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssues ADD WorkflowStatus VARCHAR(16) NULL';
    IF COL_LENGTH(N'dbo.StockIssues', N'DraftPayload') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssues ADD DraftPayload NVARCHAR(MAX) NULL';
    IF COL_LENGTH(N'dbo.StockIssues', N'RecipientDocument') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssues ADD RecipientDocument NVARCHAR(30) NULL';
    IF COL_LENGTH(N'dbo.StockIssues', N'RecipientRelation') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssues ADD RecipientRelation NVARCHAR(160) NULL';
    IF COL_LENGTH(N'dbo.StockIssues', N'DeliveryMethod') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssues ADD DeliveryMethod NVARCHAR(20) NULL';
    IF COL_LENGTH(N'dbo.StockIssueDetails', N'ConditionAtDelivery') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssueDetails ADD ConditionAtDelivery NVARCHAR(400) NULL';
    IF COL_LENGTH(N'dbo.StockIssueDetails', N'LocationAtIssue') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssueDetails ADD LocationAtIssue NVARCHAR(100) NULL';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments') AND name = N'UX_AA_Payments_TransactionReference_D4')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_AA_Payments_TransactionReference_D4 ON dbo.Payments(TransactionReference) WHERE TransactionReference IS NOT NULL';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SellerPayouts') AND name = N'UX_AA_Payouts_TransferReference_D4')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_AA_Payouts_TransferReference_D4 ON dbo.SellerPayouts(TransferReference) WHERE TransferReference IS NOT NULL';

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.StockIssues') AND name = N'CK_AA_StockIssues_DeliveryMethod_D4')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssues WITH CHECK ADD CONSTRAINT CK_AA_StockIssues_DeliveryMethod_D4 CHECK (DeliveryMethod IS NULL OR DeliveryMethod IN (''AtVenue'', ''Delivery''))';
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.StockIssues') AND name = N'CK_AA_StockIssueStatus_D4')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StockIssues WITH CHECK ADD CONSTRAINT CK_AA_StockIssueStatus_D4 CHECK (WorkflowStatus IS NULL OR WorkflowStatus IN (''Draft'', ''Confirmed'', ''Cancelled''))';

    -- Thay đúng ràng buộc trạng thái cũ, giữ các ràng buộc khác của lệnh chi.
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.SellerPayoutOrders') AND name = N'CK_SellerPayoutOrderStatus_20261031')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SellerPayoutOrders DROP CONSTRAINT CK_SellerPayoutOrderStatus_20261031';
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.SellerPayoutOrders') AND name = N'CK_AA_PayoutOrderStatus_D4')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SellerPayoutOrders WITH CHECK ADD CONSTRAINT CK_AA_PayoutOrderStatus_D4 CHECK (OrderStatus IN (''Pending'', ''Paid'', ''Cancelled''))';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id IN (OBJECT_ID(N'dbo.Payments'), OBJECT_ID(N'dbo.SellerPayouts')) AND name IN (N'UX_AA_Payments_TransactionReference_D4', N'UX_AA_Payouts_TransferReference_D4') AND is_disabled = 1)
       OR EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id IN (OBJECT_ID(N'dbo.StockIssues'), OBJECT_ID(N'dbo.SellerPayoutOrders')) AND name IN (N'CK_AA_StockIssues_DeliveryMethod_D4', N'CK_AA_StockIssueStatus_D4', N'CK_AA_PayoutOrderStatus_D4') AND (is_disabled = 1 OR is_not_trusted = 1))
        THROW 51403, N'Ràng buộc thanh toán hoặc xuất kho đang bị tắt. Hãy liên hệ quản trị viên để kiểm tra.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

GO
SET NOEXEC OFF;
GO
