-- Kiểm tra kết nối, cấu trúc và dữ liệu chứng từ.
-- Chỉ đọc dữ liệu. Các dòng báo lỗi hoặc cảnh báo cần được đối chiếu riêng.

-- Đọc tên server và instance trước khi kiểm tra các bảng.
USE [master];
GO
SELECT
    CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)) AS [ServerName],
    CAST(SERVERPROPERTY('MachineName') AS nvarchar(128)) AS [MachineName],
    CAST(SERVERPROPERTY('InstanceName') AS nvarchar(128)) AS [InstanceName],
    CAST(SERVERPROPERTY('MachineName') AS nvarchar(128)) +
        CASE WHEN SERVERPROPERTY('InstanceName') IS NULL THEN N''
             ELSE N'\' + CAST(SERVERPROPERTY('InstanceName') AS nvarchar(128)) END AS [TenServerNenNhap],
    DB_ID(N'AuctionArenaDB') AS [AuctionArenaDbId];

IF DB_ID(N'AuctionArenaDB') IS NULL
    PRINT N'KHONG CO AuctionArenaDB tren ket noi SSMS nay. Kiem tra lai ten SERVER/INSTANCE.';
ELSE IF OBJECT_ID(N'AuctionArenaDB.dbo.Users', N'U') IS NULL
    PRINT N'CO database nhung KHONG TIM THAY dbo.Users. Kiem tra script khoi tao.';
ELSE
BEGIN
    PRINT N'Da tim thay database va dbo.Users. Kiem tra so luong tai khoan:';
    EXEC(N'SELECT COUNT(*) AS SoUser FROM [AuctionArenaDB].[dbo].[Users];');
END;
GO

IF DB_ID(N'AuctionArenaDB') IS NULL
BEGIN
    PRINT N'Chua co database AuctionArenaDB. Dung 01_TaoDatabase.sql neu muon cai moi.';
END
ELSE
BEGIN
    PRINT N'==== Kiem tra cau truc va so ban ghi ====';
DECLARE @CheckSql NVARCHAR(MAX);
    SET @CheckSql = CAST(N'USE [AuctionArenaDB];
    -- Danh sách bảng và cột mà ứng dụng sử dụng khi mở các màn hình.
    -- Chỉ đọc metadata, không thay đổi dữ liệu.
    -- So sánh với các cột được kiểm tra khi khởi động ứng dụng.
    SET NOCOUNT ON;
    DECLARE @YeuCau TABLE (TenBang SYSNAME, TenCot SYSNAME, Nhom VARCHAR(12), ScriptGoiY NVARCHAR(50));
    INSERT @YeuCau (TenBang, TenCot, Nhom, ScriptGoiY)
    VALUES
        (N''Users'', N''UserId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Users'', N''HashIterations'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Users'', N''RoleId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Users'', N''Email'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Users'', N''Phone'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Users'', N''LockReason'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Users'', N''LastLoginAt'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Users'', N''PasswordChangedAt'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Users'', N''MustChangePassword'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Users'', N''FailedPasswordAttempts'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Users'', N''LockedUntil'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Roles'', N''RoleId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Roles'', N''RoleCode'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Roles'', N''Description'', N''NenTang'', N''02_DongBoCauTruc.sql''),
        (N''Customers'', N''CustomerId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Consignors'', N''ConsignorId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Categories'', N''CategoryId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Items'', N''ItemId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''ConsignmentContracts'', N''ContractId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''StockReceiptDetails'', N''ReceiptId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''AuctionLots'', N''LotId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''AuctionLots'', N''RowVersion'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Registrations'', N''RegistrationId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Registrations'', N''DepositApplied'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Invoices'', N''InvoiceId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Invoices'', N''BuyerFeeRate'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''InvoiceDetails'', N''InvoiceId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''SellerPayouts'', N''PayoutId'', N''NenTang'', N''01_TaoDatabase.sql''),
        (N''Customers'', N''Address'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Customers'', N''PhotoPath'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''BankName'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''BankAccount'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''SellerFeeRate'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Categories'', N''CategoryCode'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Categories'', N''Description'', N''NghiepVu'', N''02_D' AS NVARCHAR(MAX))
        + N'ongBoCauTruc.sql''),
        (N''Items'', N''Origin'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''ConditionDescription'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''WarehouseLocation'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''ConsignmentContracts'', N''SellerFeeRate'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''ConsignmentContracts'', N''ExpiresAt'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockReceiptDetails'', N''ConditionAtReceipt'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockReceiptDetails'', N''LocationAtReceipt'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''AuctionLots'', N''SellerFeeRateLocked'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''AuctionLots'', N''DepositRequired'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''SellerPayouts'', N''PaymentMethod'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''SellerPayouts'', N''TransferReference'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''ConsignorReturns'', N''ReturnId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''ConsignorReturns'', N''ItemId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''ConsignorReturns'', N''LotId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''InvoiceAdjustments'', N''AdjustmentId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''InvoiceAdjustments'', N''InvoiceId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''InvoiceAdjustments'', N''RegistrationId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''DepositRefundObligations'', N''ObligationId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''DepositRefundObligations'', N''RegistrationId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''DepositRefundObligations'', N''DueAmount'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''DepositRefundObligations'', N''ObligationStatus'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''SellerPayoutOrders'', N''OrderId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''SellerPayoutOrders'', N''LotId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''SellerPayoutOrders'', N''OrderStatus'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''InvoiceReviewCases'', N''ReviewId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''InvoiceReviewCases'', N''InvoiceId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''InvoiceReviewCases'', N''ReviewStatus'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Payments'', N''PaymentId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Payments'', N''TransactionReference'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockIssues'', N''IssueId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockIssues'', N''WorkflowStatus'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockIssues'', N''DraftPayload'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockIssues'', N''RecipientDocument'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockIssues'', N''RecipientRelation'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockIssues'', N''DeliveryMethod'', N''NghiepVu'', N''02_DongBoCauTr'
        + N'uc.sql''),
        (N''StockIssueDetails'', N''IssueDetailId'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockIssueDetails'', N''ConditionAtDelivery'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''StockIssueDetails'', N''LocationAtIssue'', N''NghiepVu'', N''02_DongBoCauTruc.sql''),
        (N''Customers'', N''DateOfBirth'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Customers'', N''Gender'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Customers'', N''CustomerStatus'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Customers'', N''BlacklistReason'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Customers'', N''Notes'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Customers'', N''PhotoData'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''ConsignorType'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''RepresentativeName'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''Email'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''BankAccountHolder'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''ConsignorStatus'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Consignors'', N''Notes'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Categories'', N''SortOrder'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Categories'', N''IsActive'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''Era'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''Dimensions'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''Material'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''EstimatePriceHigh'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''AppraisalNotes'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''AppraisedByUserId'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''Items'', N''AppraisedAt'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''CustomerTierHistory'', N''HistoryId'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''CustomerTierHistory'', N''CustomerId'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''CustomerTierHistory'', N''OldTier'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''CustomerTierHistory'', N''NewTier'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''CustomerTierHistory'', N''ChangedByUserId'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''CustomerTierHistory'', N''ChangedAt'', N''HoSo'', N''02_DongBoCauTruc.sql''),
        (N''StockReceipts'', N''DeliveredBy'', N''NhapKho'', N''02_DongBoCauTruc.sql'');
    SELECT @@SERVERNAME AS SQLServer, DB_NAME() AS DatabaseName;
    SELECT y.Nhom, N''dbo.'' + y.TenBang AS Bang, y.TenCot AS Cot,
           CASE WHEN OBJECT_ID(N''dbo.'' + y.TenBang, N''U'') IS NULL THEN N''THIEU BANG''
                WHEN COL_LENGTH(N''dbo.'' + y.TenBang, y.TenCot) IS NULL THEN N''THIEU COT''
                ELSE N''DA CO'' END AS TrangThai,
           y.ScriptGoiY AS ScriptThamKhao
    FROM @YeuCau y
    WHERE OBJECT_ID(N''dbo.'' + y.TenBang, N''U'') IS NULL
       OR COL_LENGTH(N''dbo.'' + y.TenBang, y.TenCot) IS NULL
    ORDER BY y.Nhom, y.TenBang, y.TenCot;
    SELECT COUNT(*) AS TongSoBangCotCan'
        + N'KiemTra,
           SUM(CASE WHEN OBJECT_ID(N''dbo.'' + TenBang, N''U'') IS NULL
                          OR COL_LENGTH(N''dbo.'' + TenBang, TenCot) IS NULL THEN 1 ELSE 0 END) AS SoMucConThieu
    FROM @YeuCau;
    -- Kết quả này chỉ xác nhận cấu trúc dữ liệu, không thay cho kiểm thử nghiệp vụ.
    
    -- Thong ke so ban ghi theo metadata, khong thay doi du lieu.
    SELECT s.name AS TenSchema, t.name AS TenBang,
           SUM(CASE WHEN p.index_id IN (0,1) THEN p.[rows] ELSE 0 END) AS SoBanGhiUocTinh
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id=t.schema_id
    LEFT JOIN sys.partitions p ON p.object_id=t.object_id
    GROUP BY s.name,t.name
    ORDER BY s.name,t.name;
    ';
    EXEC sys.sp_executesql @CheckSql;
    
    IF OBJECT_ID(N'AuctionArenaDB.dbo.InvoiceAdjustments',N'U') IS NOT NULL
       AND OBJECT_ID(N'AuctionArenaDB.dbo.ConsignorReturns',N'U') IS NOT NULL
       AND COL_LENGTH(N'AuctionArenaDB.dbo.AuctionLots',N'SellerFeeRateLocked') IS NOT NULL
       AND COL_LENGTH(N'AuctionArenaDB.dbo.ConsignmentContracts',N'ExpiresAt') IS NOT NULL
       AND COL_LENGTH(N'AuctionArenaDB.dbo.StockIssues',N'IssueId') IS NOT NULL
       AND COL_LENGTH(N'AuctionArenaDB.dbo.Payments',N'PaymentType') IS NOT NULL
    BEGIN
        PRINT N'==== Kiem tra nghiep vu (chi doc) ==== ';
        DECLARE @AuditSql NVARCHAR(MAX);
        SET @AuditSql = CAST(N'-- BAO CAO KIEM TRA TOAN VEN - KHONG CHINH SUA DU LIEU.
        -- Các khoản tiền và trạng thái chứng từ cần được kiểm tra trên bản sao dữ liệu.
        -- Chỉ đọc số dư và chứng từ để tìm sai lệch.
        USE [AuctionArenaDB];
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        
        -- Không đối soát khi thiếu những bảng liên quan đến chứng từ.
        IF OBJECT_ID(N''dbo.InvoiceAdjustments'',N''U'') IS NULL OR
           OBJECT_ID(N''dbo.ConsignorReturns'',N''U'') IS NULL OR
           COL_LENGTH(''dbo.AuctionLots'',''SellerFeeRateLocked'') IS NULL OR
           COL_LENGTH(''dbo.AuctionLots'',''DepositRequired'') IS NULL OR
           COL_LENGTH(''dbo.ConsignmentContracts'',''ExpiresAt'') IS NULL
        BEGIN
            RAISERROR(N''Thieu cau truc du lieu can thiet de kiem tra chung tu.'',16,1);
            RETURN;
        END;
        
        CREATE TABLE #Findings
        (
            LevelName VARCHAR(8) NOT NULL,
            CheckCode VARCHAR(60) NOT NULL,
            BadRows INT NOT NULL,
            Explanation NVARCHAR(400) NOT NULL
        );
        INSERT #Findings
        SELECT ''ERROR'',''DEPOSIT_NEGATIVE_BALANCE'',COUNT(*),N''Số dư cọc âm hoặc các khoản sử dụng vượt tiền đã thu.''
        FROM dbo.Registrations
        WHERE DepositPaid<DepositApplied+DepositRefunded+DepositForfeited OR
              DepositPaid<0 OR DepositRequired<0 OR DepositApplied<0 OR DepositRefunded<0 OR DepositForfeited<0;
        INSERT #Findings
        SELECT ''ERROR'',''INVOICE_TOTAL_MISMATCH'',COUNT(*),N''Tổng phải thu lệch công thức giá búa + phí mua - VIP + VAT - cọc.''
        FROM dbo.Invoices
        WHERE TotalDue<>HammerSubtotal+BuyerFee-VipDiscount+VatAmount-DepositApplied;
        INSERT #Findings
        SELECT ''ERROR'',''INVOICE_PAID_MISMATCH'',COUNT(*),N''PaidAmount ngoài giới hạn hoặc trạng thái Paid/PartiallyPaid không khớp.''
        FROM dbo.Invoices
        WHERE PaidAmount<0 OR PaidAmount>TotalDue OR
              (InvoiceStatus=''Paid'' AND PaidAmount<>TotalDue) OR
              (InvoiceStatus=''PartiallyPaid'' AND (PaidAmount<=0 OR PaidAmount>=TotalDue)) OR
              (InvoiceStatus=''Unpaid'' AND (PaidAmount<>0 OR TotalDue<=0)) OR
              (InvoiceStatus=''Overdue'' AND PaidAmount>=TotalDue);
        INSERT #Findings
        SELECT ''ERROR'',''INVOICE_DETAILS_MISSING'',COUNT(*),N''Hóa đơn hoạt động thiếu dòng lot.''
        FROM dbo.Invoices i
        WHERE i.InvoiceStatus<>''Cancelled'' AND NOT EXISTS
              (SELECT 1 FROM dbo.InvoiceDetails d WHERE d.InvoiceId=i.InvoiceId);
        INSERT #Findings
        SELECT ''ERROR'',''INVOICE_HAMMER_MISMATCH'',COUNT(*),N''Giá búa tổng hóa đơn không khớp chi tiết.''
        FROM dbo.Invoices i
        WHERE EXISTS(SELECT 1 FROM dbo.InvoiceDetails d WHERE d.InvoiceId=i.InvoiceId)
        AND i.HammerSubtotal<>(SELECT SUM(d.HammerPrice) FROM dbo.InvoiceDetails d WHERE d.InvoiceId=i.InvoiceId);
        INSERT #Findings
        SELECT ''ERROR'',''ACTIVE_INVOICE_LOT_MISMATCH'',COUNT(*),N''Lot trong hóa đơn hoạt động không còn So' AS NVARCHAR(MAX))
        + N'ld, sai người thắng hoặc phiên.''
        FROM dbo.InvoiceDetails d
        JOIN dbo.Invoices i ON i.InvoiceId=d.InvoiceId
        JOIN dbo.AuctionLots l ON l.LotId=d.LotId
        WHERE i.InvoiceStatus<>''Cancelled'' AND
              (l.LotStatus<>''Sold'' OR l.WinningCustomerId<>i.CustomerId OR l.SessionId<>i.SessionId OR l.HammerPrice<>d.HammerPrice);
        INSERT #Findings
        SELECT ''ERROR'',''SOLD_LOT_INCOMPLETE'',COUNT(*),N''Lot Sold thiếu người thắng/giá búa hợp lệ.''
        FROM dbo.AuctionLots
        WHERE LotStatus=''Sold'' AND (WinningCustomerId IS NULL OR HammerPrice IS NULL OR HammerPrice<ReservePrice);
        INSERT #Findings
        SELECT ''ERROR'',''STOCK_ISSUE_UNPAID'',COUNT(*),N''Có chứng từ xuất hàng khi hóa đơn chưa Paid.''
        FROM dbo.StockIssues si JOIN dbo.Invoices i ON si.InvoiceId=i.InvoiceId
        WHERE i.InvoiceStatus<>''Paid'';
        INSERT #Findings
        SELECT ''ERROR'',''PAYOUT_UNPAID'',COUNT(*),N''Có chứng từ chi chủ hàng cho lot không có hóa đơn đã thanh toán đủ.''
        FROM dbo.SellerPayouts p
        WHERE NOT EXISTS(
            SELECT 1 FROM dbo.InvoiceDetails d JOIN dbo.Invoices i ON i.InvoiceId=d.InvoiceId
            WHERE d.LotId=p.LotId AND i.InvoiceStatus=''Paid'');
        INSERT #Findings
        SELECT ''ERROR'',''PAYOUT_CALC_MISMATCH'',COUNT(*),N''Phí bán hoặc thực chi không khớp giá búa và tỷ lệ đã lưu.''
        FROM dbo.SellerPayouts
        WHERE SellerFee<>ROUND(HammerPrice*SellerFeeRate,2) OR PayoutAmount<>HammerPrice-SellerFee;
        INSERT #Findings
        SELECT ''ERROR'',''RETURN_HAS_STOCK_ISSUE'',COUNT(*),N''Hiện vật đã trả chủ hàng nhưng vẫn có phiếu xuất cho người mua.''
        FROM dbo.ConsignorReturns r
        WHERE EXISTS(SELECT 1 FROM dbo.StockIssueDetails d WHERE d.ItemId=r.ItemId);
        INSERT #Findings
        SELECT ''ERROR'',''RETURN_BAD_STATUS'',COUNT(*),N''Hiện vật đã trả chủ hàng nhưng trạng thái khác Returned.''
        FROM dbo.ConsignorReturns r JOIN dbo.Items i ON i.ItemId=r.ItemId
        WHERE i.ItemStatus<>''Returned'';
        INSERT #Findings
        SELECT ''ERROR'',''ADJUSTMENT_BAD_INVOICE'',COUNT(*),N''Có điều chỉnh cọc nhưng hóa đơn chưa Cancelled.''
        FROM dbo.InvoiceAdjustments a JOIN dbo.Invoices i ON a.InvoiceId=i.InvoiceId
        WHERE i.InvoiceStatus<>''Cancelled'';
        INSERT #Findings
        SELECT ''WARN'',''SELLER_FEE_UNCONFIRMED'',COUNT(*),N''Lot Sold chưa chốt tỷ lệ phí bán; kế toán phải xác nhận trước khi chi.''
        FROM dbo.AuctionLots l
        WHERE l.LotStatus=''Sold'' AND l.SellerFeeRateLocked IS NULL AND
              NOT EXISTS (SELECT 1 FROM dbo.SellerPayouts p WHERE p.LotId=l.LotId);
        INSERT #Findings
        SELECT ''WARN'',''CANCELLED_INVOICE_WITH_CASH_PAYMENT'',COUNT(*),N''Hóa đơn hủy đã có khoản thu Invoice: phải kiểm tra chứng từ đảo tiền thủ công.''
        FROM dbo.Invoices i
        WHERE i.InvoiceStatus=''Cancelled'' AND EXISTS
              (SELECT 1 FROM dbo.Payments p WHERE p.InvoiceId=i.InvoiceId AND p.PaymentType=''Invoice'');
        INSERT #'
        + N'Findings
        SELECT ''WARN'',''PAYMENT_WITHOUT_METHOD'',COUNT(*),N''Chứng từ chi chủ hàng lịch sử chưa có phương thức chi.''
        FROM dbo.SellerPayouts WHERE PaymentMethod IS NULL;
        
        
        -- Đối chiếu số dư ghi trên chứng từ với sổ phiếu tiền thực tế.
        -- Không điều chỉnh tự động: mọi sai lệch cần kế toán kiểm tra nguồn phát sinh.
        INSERT #Findings
        SELECT ''ERROR'',''INVOICE_RECEIPTS_MISMATCH'',COUNT(*),
               N''PaidAmount trên hóa đơn không bằng tổng phiếu thu Invoice (không tính chuyển cọc).''
        FROM dbo.Invoices i
        WHERE i.PaidAmount<>COALESCE((SELECT SUM(p.Amount) FROM dbo.Payments p
                    WHERE p.InvoiceId=i.InvoiceId AND p.PaymentType=''Invoice''),0);
        INSERT #Findings
        SELECT ''ERROR'',''REGISTRATION_COLLECTION_MISMATCH'',COUNT(*),
               N''DepositPaid khác tổng phiếu thu cọc Deposit theo đăng ký.''
        FROM dbo.Registrations r
        WHERE r.DepositPaid<>COALESCE((SELECT SUM(p.Amount) FROM dbo.Payments p
                    WHERE p.RegistrationId=r.RegistrationId AND p.PaymentType=''Deposit''),0);
        INSERT #Findings
        SELECT ''ERROR'',''REGISTRATION_REFUND_MISMATCH'',COUNT(*),
               N''DepositRefunded khác tổng phiếu hoàn Refund theo đăng ký.''
        FROM dbo.Registrations r
        WHERE r.DepositRefunded<>COALESCE((SELECT SUM(p.Amount) FROM dbo.Payments p
                    WHERE p.RegistrationId=r.RegistrationId AND p.PaymentType=''Refund''),0);
        INSERT #Findings
        SELECT ''ERROR'',''REGISTRATION_FORFEIT_MISMATCH'',COUNT(*),
               N''DepositForfeited khác tổng chứng từ tịch thu Forfeit.''
        FROM dbo.Registrations r
        WHERE r.DepositForfeited<>COALESCE((SELECT SUM(p.Amount) FROM dbo.Payments p
                    WHERE p.RegistrationId=r.RegistrationId AND p.PaymentType=''Forfeit''),0);
        INSERT #Findings
        SELECT ''ERROR'',''REGISTRATION_APPLIED_MISMATCH'',COUNT(*),
               N''Cọc đã áp dụng khác tổng phiếu chuyển cọc trừ chứng từ đảo chuyển cọc.''
        FROM dbo.Registrations r
        WHERE r.DepositApplied<>COALESCE((SELECT SUM(p.Amount) FROM dbo.Payments p
                   WHERE p.RegistrationId=r.RegistrationId AND p.PaymentType=''DepositTransfer''),0)
               -COALESCE((SELECT SUM(a.Amount) FROM dbo.InvoiceAdjustments a
                   WHERE a.RegistrationId=r.RegistrationId AND a.AdjustmentType=''DepositTransferForfeit''),0);
        INSERT #Findings
        SELECT ''ERROR'',''INVOICE_TRANSFER_MISMATCH'',COUNT(*),
               N''DepositApplied lịch sử trên hóa đơn khác phiếu chuyển cọc liên kết.''
        FROM dbo.Invoices i
        WHERE i.DepositApplied<>COALESCE((SELECT SUM(p.Amount) FROM dbo.Payments p
                   WHERE p.InvoiceId=i.InvoiceId AND p.PaymentType=''DepositTransfer''),0);
        INSERT #Findings
        SELECT ''ERROR'',''PAYMENT_LINK_MISMATCH'',COUNT(*),
               N''Phiếu tiền thiếu tham chiếu bắt buộc hoặc invoice/registration sai khách và phiên.''
   '
        + N'     FROM dbo.Payments p
        LEFT JOIN dbo.Invoices i ON p.InvoiceId=i.InvoiceId
        LEFT JOIN dbo.Registrations r ON p.RegistrationId=r.RegistrationId
        WHERE (p.PaymentType IN (''Deposit'',''Refund'',''Forfeit'') AND p.RegistrationId IS NULL) OR
              (p.PaymentType=''Invoice'' AND p.InvoiceId IS NULL) OR
              (p.PaymentType=''DepositTransfer'' AND (p.InvoiceId IS NULL OR p.RegistrationId IS NULL)) OR
              (p.InvoiceId IS NOT NULL AND p.RegistrationId IS NOT NULL AND
               (i.CustomerId<>r.CustomerId OR i.SessionId<>r.SessionId));
        INSERT #Findings
        SELECT ''ERROR'',''PAYOUT_WRONG_OWNER'',COUNT(*),
               N''Chứng từ chi ghi chủ hàng khác chủ thực tế của hiện vật.''
        FROM dbo.SellerPayouts p
        JOIN dbo.AuctionLots l ON p.LotId=l.LotId
        JOIN dbo.Items it ON l.ItemId=it.ItemId
        WHERE p.ConsignorId<>it.ConsignorId;
        INSERT #Findings
        SELECT ''ERROR'',''PAYOUT_LOCKED_RATE_MISMATCH'',COUNT(*),
               N''Phí người bán đã chi không khớp tỷ lệ phí chốt theo lot.''
        FROM dbo.SellerPayouts p
        JOIN dbo.AuctionLots l ON p.LotId=l.LotId
        WHERE l.SellerFeeRateLocked IS NOT NULL AND p.SellerFeeRate<>l.SellerFeeRateLocked;
        -- Chứng từ có thể giao từng phần; đối chiếu phiếu xuất trước khi kết luận sai lệch.
        INSERT #Findings
        SELECT ''WARN'',''ISSUE_ITEM_COUNT_MISMATCH'',COUNT(*),
               N''Phiếu xuất có số hiện vật khác số lot của hóa đơn; kiểm tra chứng từ xuất từng phần.''
        FROM dbo.StockIssues si
        WHERE (SELECT COUNT(*) FROM dbo.StockIssueDetails sd WHERE sd.IssueId=si.IssueId)
              <>(SELECT COUNT(*) FROM dbo.InvoiceDetails d WHERE d.InvoiceId=si.InvoiceId);
        
        -- Xác minh hồ sơ và phân tách chủ hàng - người mua trong đấu giá trực tiếp.
        INSERT #Findings
        SELECT ''WARN'',''CUSTOMER_REGISTRATION_DOC_MISSING'',COUNT(*),
               N''Khách đã đăng ký phiên nhưng thiếu giấy tờ định danh hoặc số điện thoại; cần hoàn thiện hồ sơ.''
        FROM dbo.Registrations r JOIN dbo.Customers c ON c.CustomerId=r.CustomerId
        WHERE NULLIF(LTRIM(RTRIM(c.IdentityNumber)),'''') IS NULL OR
              NULLIF(LTRIM(RTRIM(c.Phone)),'''') IS NULL;
        INSERT #Findings
        SELECT ''ERROR'',''ACTIVE_LOT_CONSIGNOR_DOC_MISSING'',COUNT(*),
               N''Lot đang chuẩn bị/mở có chủ hàng chưa có số giấy tờ/đăng ký doanh nghiệp, không xác minh được việc tự trả giá.''
        FROM dbo.AuctionLots l JOIN dbo.Items i ON i.ItemId=l.ItemId
        JOIN dbo.Consignors c ON c.ConsignorId=i.ConsignorId
        WHERE l.LotStatus IN (''Planned'',''Open'') AND NULLIF(LTRIM(RTRIM(c.IdentityNumber)),'''') IS NULL;
        INSERT #Findings
        SELECT ''ERROR'',''OWNER_SELF_BID'',COUNT(*),
               N''Có lượt bid mà định danh người trả trùng chủ hàng của chính hiện vật.''
        FROM dbo.Bids b JOIN dbo.AuctionLots l ON l.LotId=b.LotId
        JOIN dbo.Items i ON i.ItemId=l.ItemId JOIN dbo.Consignors o O'
        + N'N o.ConsignorId=i.ConsignorId
        JOIN dbo.Customers c ON c.CustomerId=b.CustomerId
        WHERE NULLIF(LTRIM(RTRIM(o.IdentityNumber)),'''') IS NOT NULL AND
              LTRIM(RTRIM(o.IdentityNumber))=LTRIM(RTRIM(c.IdentityNumber));
        INSERT #Findings
        SELECT ''ERROR'',''DUPLICATE_IDENTITY_PADDLES'',COUNT(*),
               N''Cùng giấy tờ định danh được cấp nhiều paddle trong một phiên; cần rà soát hồ sơ và lịch sử trả giá.''
        FROM (SELECT r.SessionId,LTRIM(RTRIM(c.IdentityNumber)) AS IdentityNumber
              FROM dbo.Registrations r JOIN dbo.Customers c ON c.CustomerId=r.CustomerId
              WHERE NULLIF(LTRIM(RTRIM(c.IdentityNumber)),'''') IS NOT NULL
              GROUP BY r.SessionId,LTRIM(RTRIM(c.IdentityNumber)) HAVING COUNT(*)>1) duplicated;
        INSERT #Findings
        SELECT ''WARN'',''OVERDUE_PARTIALLY_PAID'',COUNT(*),
               N''Hóa đơn quá hạn đã thu một phần: cần kế toán đối soát, không tự động tịch thu cọc.''
        FROM dbo.Invoices i
        WHERE i.InvoiceStatus IN (''Unpaid'',''PartiallyPaid'',''Overdue'') AND i.PaidAmount>0 AND
              i.PaidAmount<i.TotalDue AND i.DueAt<SYSDATETIME();
        
        SELECT LevelName AS [Muc_do],CheckCode AS [Kiem_tra],BadRows AS [So_dong_sai],
               Explanation AS [Giai_thich]
        FROM #Findings
        WHERE BadRows > 0 -- Chi hien muc co van de, khong liet ke cac kiem tra da dat (0 dong).
        ORDER BY CASE WHEN LevelName=''ERROR'' THEN 0 ELSE 1 END,CheckCode;
        DECLARE @errorCount INT=(SELECT COALESCE(SUM(BadRows),0) FROM #Findings WHERE LevelName=''ERROR'');
        DECLARE @warningCount INT=(SELECT COALESCE(SUM(BadRows),0) FROM #Findings WHERE LevelName=''WARN'');
        PRINT N''Tổng dòng lỗi: ''+CONVERT(NVARCHAR(20),@errorCount)+N''; cảnh báo: ''+CONVERT(NVARCHAR(20),@warningCount);
        -- Tổng hợp số chứng từ có dữ liệu không khớp.
        -- Các chênh lệch cần đối chiếu chứng từ trước khi điều chỉnh.
        SELECT CASE WHEN @errorCount>0 THEN N''KHONG DAT - CAN DOI SOAT DU LIEU''
                    WHEN @warningCount>0 THEN N''CHUA CO LOI NGHIEM TRONG - CON CANH BAO''
                    ELSE N''DAT KIEM TRA DOC DU LIEU'' END AS [Ket_luan],
               @errorCount AS [Tong_dong_loi], @warningCount AS [Tong_dong_canh_bao];
        IF @errorCount>0
            PRINT N''CO LOI NGHIEP VU: xem bang Ket_qua phia tren, doi soat chung tu goc truoc khi sua.'';
        ELSE
            PRINT N''Khong phat hien loi nghiem trong qua cac muc doi soat du lieu. Xem cac canh bao neu co.'';
        ';
        EXEC sys.sp_executesql @AuditSql;
    END
    ELSE
        PRINT N'Cau truc du lieu chua du de doi soat. Xem danh sach thieu cot va huong dan trong SQL/02_DongBoCauTruc.sql.';
END;
GO
