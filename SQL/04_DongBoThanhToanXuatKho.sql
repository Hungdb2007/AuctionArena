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
