using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using AuctionArena.Data;
using AuctionArena.Data.Models;
namespace AuctionArena.Services
{
    // Kiểm tra cấu trúc cơ sở dữ liệu bằng metadata SQL, không truy vấn Entity chưa có cột.
    // Chỉ đối chiếu cấu trúc sau bước chuẩn bị cơ sở dữ liệu khi khởi động.
    public sealed class SchemaReadinessService
    {
        public void Verify() //Đối chiếu bảng/cột bắt buộc và báo thiếu cấu trúc(14-305).
        {
            var coreColumns = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Users", new[]
                    {
                        "UserId", "HashIterations", "RoleId", "Email", "Phone", "LockReason", 
                        "LastLoginAt", "PasswordChangedAt", "MustChangePassword", "FailedPasswordAttempts", "LockedUntil"
                    }
                }, 
                {
                    "Roles", new[]
                    {
                        "RoleId", "RoleCode", "Description"
                    }
                }, 
                {
                    "Customers", new[]
                    {
                        "CustomerId"
                    }
                }, 
                {
                    "Consignors", new[]
                    {
                        "ConsignorId"
                    }
                }, 
                {
                    "Categories", new[]
                    {
                        "CategoryId"
                    }
                }, 
                {
                    "Items", new[]
                    {
                        "ItemId"
                    }
                }, 
                {
                    "ConsignmentContracts", new[]
                    {
                        "ContractId"
                    }
                }, 
                {
                    "StockReceiptDetails", new[]
                    {
                        "ReceiptId"
                    }
                }, 
                {
                    "AuctionLots", new[]
                    {
                        "LotId", "RowVersion"
                    }
                }, 
                {
                    "Registrations", new[]
                    {
                        "RegistrationId", "DepositApplied"
                    }
                }, 
                {
                    "Invoices", new[]
                    {
                        "InvoiceId", "BuyerFeeRate"
                    }
                }, 
                {
                    "InvoiceDetails", new[]
                    {
                        "InvoiceId"
                    }
                }, 
                {
                    "SellerPayouts", new[]
                    {
                        "PayoutId"
                    }
                }
            };
            var businessColumns = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Customers", new[]
                    {
                        "Address", "PhotoPath"
                    }
                }, 
                {
                    "Consignors", new[]
                    {
                        "BankName", "BankAccount", "SellerFeeRate"
                    }
                }, 
                {
                    "Categories", new[]
                    {
                        "CategoryCode", "Description"
                    }
                }, 
                {
                    "Items", new[]
                    {
                        "Origin", "ConditionDescription", "WarehouseLocation"
                    }
                }, 
                {
                    "ConsignmentContracts", new[]
                    {
                        "SellerFeeRate", "ExpiresAt"
                    }
                }, 
                {
                    "StockReceiptDetails", new[]
                    {
                        "ConditionAtReceipt", "LocationAtReceipt"
                    }
                }, 
                {
                    "AuctionLots", new[]
                    {
                        "SellerFeeRateLocked", "DepositRequired"
                    }
                }, 
                {
                    "SellerPayouts", new[]
                    {
                        "PaymentMethod", "TransferReference"
                    }
                }, 
                {
                    "ConsignorReturns", new[]
                    {
                        "ReturnId", "ItemId", "LotId"
                    }
                }, 
                {
                    "InvoiceAdjustments", new[]
                    {
                        "AdjustmentId", "InvoiceId", "RegistrationId"
                    }
                }, 
                {
                    "DepositRefundObligations", new[]
                    {
                        "ObligationId", "RegistrationId", "DueAmount", "ObligationStatus"
                    }
                }, 
                {
                    "SellerPayoutOrders", new[]
                    {
                        "OrderId", "LotId", "OrderStatus"
                    }
                }, 
                {
                    "InvoiceReviewCases", new[]
                    {
                        "ReviewId", "InvoiceId", "ReviewStatus"
                    }
                }, 
                {
                    "Payments", new[]
                    {
                        "PaymentId", "TransactionReference"
                    }
                }, 
                {
                    "StockIssues", new[]
                    {
                        "IssueId", "WorkflowStatus", "DraftPayload", "RecipientDocument", "RecipientRelation", "DeliveryMethod"
                    }
                }, 
                {
                    "StockIssueDetails", new[]
                    {
                        "IssueDetailId", "ConditionAtDelivery", "LocationAtIssue"
                    }
                }
            };
            // Bảng có thể tồn tại nhưng vẫn thiếu cột mà Entity Framework cần đọc.
            // Kiểm tra trước khi mở màn hình, tránh lỗi Entity Framework khó hiểu.
            var profileColumns = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Customers", new[] { "DateOfBirth", "Gender", "CustomerStatus", "BlacklistReason", "Notes", "PhotoData" } }, 
                { "Consignors", new[] { "ConsignorType", "RepresentativeName", "Email", "BankAccountHolder", "ConsignorStatus", "Notes" } }, 
                { "Categories", new[] { "SortOrder", "IsActive" } }, 
                { "Items", new[] { "Era", "Dimensions", "Material", "EstimatePriceHigh", "AppraisalNotes", "AppraisedByUserId", "AppraisedAt" } }, 
                { "CustomerTierHistory", new[] { "HistoryId", "CustomerId", "OldTier", "NewTier", "ChangedByUserId", "ChangedAt" } }
            };
            var receiptColumns = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "StockReceipts", new[] { "DeliveredBy" } }
            };
            try
            {
                using (var db = new AuctionArenaDbContext())
                {
                    var connection = db.Database.Connection;
                    // Đọc danh sách cột thực tế bằng LINQ, nhóm theo tên bảng.
                    var columns = db.SchemaColumns
                    .Where(c => c.TableSchema == "dbo")
                    .Select(c => new { c.TableName, c.ColumnName })
                    .ToList()
                    .GroupBy(c => c.TableName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => new HashSet<string>(g.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase),
                        StringComparer.OrdinalIgnoreCase);
                    var missingBase = Missing(columns, coreColumns);
                    if (missingBase.Count > 0)
                    throw new BusinessException("Cơ sở dữ liệu đang kết nối thiếu cấu trúc bắt buộc của Auction Arena. " +
                    "Hãy kiểm tra tên SQL Server và cơ sở dữ liệu.\n" +
                    "Server: " + connection.DataSource + "\nDatabase: " + connection.Database + "\n" +
                    "Thiếu các bảng/cột nền tảng: " + string.Join(", ", missingBase) + "\n" +
                    "Nếu thiếu các cột Users/Roles, sao lưu dữ liệu rồi chạy SQL/02_DongBoCauTruc.sql. " +
                    "KHÔNG chạy file tạo database mới lên dữ liệu hiện có.");
                    var missingBusiness = Missing(columns, businessColumns);
                    if (missingBusiness.Count > 0)
                    {
                        // Thông báo liệt kê đúng bảng hoặc cột thiếu để người quản trị kiểm tra.
                        var workflowTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        {
                            "dbo.DepositRefundObligations (thiếu bảng)", 
                            "dbo.SellerPayoutOrders (thiếu bảng)", 
                            "dbo.InvoiceReviewCases (thiếu bảng)"
                        };
                        bool onlyWorkflowTables = missingBusiness.All(x => workflowTables.Contains(x));
                        bool onlyPaymentAndDeliveryColumns = missingBusiness.All(x => 
                        x.StartsWith("Payments.TransactionReference") || 
                        x.StartsWith("SellerPayouts.TransferReference") || 
                        x.StartsWith("StockIssues.") || x.StartsWith("StockIssueDetails."));
                        string instructions = onlyPaymentAndDeliveryColumns
                        ? "Sao lưu cơ sở dữ liệu rồi chạy SQL/02_DongBoCauTruc.sql trên đúng instance."
                        : onlyWorkflowTables
                        ? "Thiếu bảng xử lý chứng từ. Sao lưu dữ liệu, mở SSMS và chạy tệp " +
                        "SQL/02_DongBoCauTruc.sql trên đúng instance. " +
                        "Không chạy SQL/01_TaoDatabase.sql trên DB đã có dữ liệu."
                        : "Sao lưu AuctionArenaDB, sau đó chạy SQL/02_DongBoCauTruc.sql. " +
                        "Không chạy SQL/01_TaoDatabase.sql trên DB đã có dữ liệu.";
                        throw new BusinessException("AuctionArenaDB còn thiếu bảng hoặc cột cần thiết.\n" +
                        "Server: " + connection.DataSource + "\nDatabase: " + connection.Database + "\n" +
                        "Bảng/cột còn thiếu (" + missingBusiness.Count + "): " +
                        string.Join(", ", missingBusiness) + "\n\n" + instructions);
                    }
                    // Các cột ở đây được dùng để đọc thông tin hồ sơ và chứng từ trên Form.
                    // Không được tự sửa DB tại đây, vì quản trị viên phải sao lưu trước.
                    var missingProfile = Missing(columns, profileColumns);
                    if (missingProfile.Count > 0)
                    {
                        throw new BusinessException(
                            "Dữ liệu Khách hàng / Chủ hàng / Hiện vật chưa có đủ các cột cần thiết.\n" +
                            "Server: " + connection.DataSource + "\nDatabase: " + connection.Database + "\n" +
                            "Thiếu " + missingProfile.Count + " bảng/cột: " + string.Join(", ", missingProfile) + "\n\n" +
                            "Cách sửa: Sao lưu AuctionArenaDB, mở SSMS và chạy " +
                            "SQL/02_DongBoCauTruc.sql. " +
                            "Nếu tệp SQL báo thiếu bảng bắt buộc, hãy kiểm tra lại tên server và cơ sở dữ liệu. " +
                            "KHÔNG chạy SQL/01_TaoDatabase.sql trên DB đã có dữ liệu."
                        );
                    }
                    var missingReceipt = Missing(columns, receiptColumns);
                    if (missingReceipt.Count > 0)
                    {
                        throw new BusinessException(
                            "Database còn thiếu cột cần thiết: " + string.Join(", ", missingReceipt) + "\n" +
                            "Sao lưu AuctionArenaDB và chạy SQL/02_DongBoCauTruc.sql " +
                            "trong SSMS trên đúng server " + connection.DataSource + "."
                        );
                    }
                }
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BusinessException("Không kiểm tra được cấu trúc AuctionArenaDB. " +
                "Hãy xác minh kết nối/quyền đọc SQL Server trong SSMS. Chi tiết: " +
                ex.GetBaseException().Message);
            }
        }
        private static List<string> Missing( //So sánh cấu trúc thực tế với cột bắt buộc.
        IDictionary<string, HashSet<string>> actual, 
        IDictionary<string, string[]> required)
        {
            var missing = new List<string>();
            foreach (var pair in required)
            {
                HashSet<string> columns;
                if (!actual.TryGetValue(pair.Key, out columns))
                {
                    missing.Add("dbo." + pair.Key + " (thiếu bảng)");
                    continue;
                }
                foreach (var column in pair.Value)
                if (!columns.Contains(column)) missing.Add(pair.Key + "." + column);
            }
            return missing;
        }
    }
    public sealed class InventoryRow
    {
        public int ItemId
        {
            get;
            set;
        }
        public string ItemCode
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public string Consignor
        {
            get;
            set;
        }
        public string Location
        {
            get;
            set;
        }
        public string Condition
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
    }
    public sealed class ReturnCandidateRow
    {
        public int LotId
        {
            get;
            set;
        }
        public int ItemId
        {
            get;
            set;
        }
        public string ItemCode
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public string Consignor
        {
            get;
            set;
        }
        public string SessionName
        {
            get;
            set;
        }
    }
    public sealed class ReturnRecordRow
    {
        public int ReturnId
        {
            get;
            set;
        }
        public string ReturnCode
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public string RecipientName
        {
            get;
            set;
        }
        public DateTime ReturnedAt
        {
            get;
            set;
        }
    }
    // Trả hiện vật cho chủ hàng phải có chứng từ riêng, không dùng phiếu xuất bán có InvoiceId.
    public sealed class WarehouseWorkflowService : ServiceBase
    {
        public WarehouseWorkflowService(Func<AuctionArenaDbContext> factory = null):base(factory)
        {
        }
        public List<InventoryRow> Inventory(int actor, string keyword, string status) //Lọc tồn kho
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_RECEIPTS", "View");
                var query = from i in db.Items.AsNoTracking()
                join o in db.Consignors on i.ConsignorId equals o.ConsignorId
                where db.StockReceiptDetails.Any(d => d.ItemId == i.ItemId) && 
                !db.StockIssueDetails.Any(d => d.ItemId == i.ItemId) && 
                !db.ConsignorReturns.Any(r => r.ItemId == i.ItemId) && 
                i.ItemStatus != "Returned" && i.ItemStatus != "Delivered"
                select new InventoryRow
                {
                    ItemId = i.ItemId, ItemCode = i.ItemCode, ItemName = i.ItemName, 
                    Consignor = o.FullName, Location = i.WarehouseLocation, 
                    Condition = i.ConditionDescription, Status = i.ItemStatus
                };
                if (!string.IsNullOrWhiteSpace(keyword))query = query.Where(x => x.ItemCode.Contains(keyword) || x.ItemName.Contains(keyword) || x.Consignor.Contains(keyword));
                if (!string.IsNullOrWhiteSpace(status))query = query.Where(x => x.Status == status);
                return query.OrderBy(x => x.ItemCode).ToList();
            }
        }
        public List<ReturnCandidateRow> Candidates(int actor) //Lấy hiện vật đủ điều kiện trả chủ hàng
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "View");
                return (from l in db.AuctionLots
                join s in db.AuctionSessions on l.SessionId equals s.SessionId
                join i in db.Items on l.ItemId equals i.ItemId
                join o in db.Consignors on i.ConsignorId equals o.ConsignorId
                where s.SessionStatus == "Closed" && l.LotStatus == "ReserveNotMet" && 
                (i.ItemStatus == "Unsold" || i.ItemStatus == "InStock") && 
                !db.ConsignorReturns.Any(r => r.ItemId == i.ItemId) && 
                !db.StockIssueDetails.Any(d => d.ItemId == i.ItemId) && 
                !db.InvoiceDetails.Any(d => d.LotId == l.LotId) && 
                !db.AuctionLots.Any(next => next.ItemId == i.ItemId && next.LotId != l.LotId && 
                (next.LotStatus == "Open" || next.LotStatus == "Planned" || next.LotStatus == "Sold"))
                select new ReturnCandidateRow
                {
                    LotId = l.LotId, ItemId = i.ItemId, 
                    ItemCode = i.ItemCode, ItemName = i.ItemName, Consignor = o.FullName, SessionName = s.SessionName
                })
                .OrderBy(x => x.ItemCode).ToList();
            }
        }
        public List<ReturnRecordRow> Returns(int actor) //Truy vấn chứng từ trả chủ hàng
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "View");
                return (from r in db.ConsignorReturns join i in db.Items on r.ItemId equals i.ItemId
                orderby r.ReturnedAt descending
                select new ReturnRecordRow
                {
                    ReturnId = r.ReturnId, ReturnCode = r.ReturnCode, 
                    ItemName = i.ItemName, RecipientName = r.RecipientName, ReturnedAt = r.ReturnedAt
                }).ToList();
            }
        }
        public int ReturnToConsignor(int actor, int lotId, string recipient, string notes) //Ghi trả hiện vật cho chủ và cập nhật trạng thái(490-541)
        {
            if (string.IsNullOrWhiteSpace(recipient) || recipient.Length>120)
            {
                throw new BusinessException("Cần nhập tên người thực nhận hiện vật.");
            }
            if (notes != null && notes.Length>300)
            {
                throw new BusinessException("Ghi chú bàn giao tối đa 300 ký tự.");
            }
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "STOCK_ISSUES", "Create");
                    var lot = db.AuctionLots.Single(x => x.LotId == lotId);
                    var session = db.AuctionSessions.Single(x => x.SessionId == lot.SessionId);
                    var item = db.Items.Single(x => x.ItemId == lot.ItemId);
                    if (session.SessionStatus != "Closed" || lot.LotStatus != "ReserveNotMet" || 
                    (item.ItemStatus != "Unsold" && item.ItemStatus != "InStock"))
                    throw new BusinessException("Chỉ trả hiện vật không đạt giá sàn khi phiên đã kết thúc.");
                    if (db.AuctionLots.Any(x => x.ItemId == item.ItemId && x.LotId != lotId && 
                    (x.LotStatus == "Planned" || x.LotStatus == "Open" || x.LotStatus == "Sold")) || 
                    db.InvoiceDetails.Any(d => d.LotId == lotId) || 
                    db.StockIssueDetails.Any(d => d.ItemId == item.ItemId) || 
                    db.ConsignorReturns.Any(r => r.ItemId == item.ItemId))
                    throw new BusinessException("Hiện vật đã giao dịch, lên phiên mới hoặc đã trả chủ hàng.");
                    var record = new ConsignorReturnEntity
                    {
                        ReturnCode = Code("TH"), LotId = lotId, ItemId = item.ItemId, 
                        ConsignorId = item.ConsignorId, RecipientName = recipient.Trim(), Notes = notes, 
                        ReturnedAt = Now(), ReturnedByUserId = actor
                    };
                    db.ConsignorReturns.Add(record);
                    item.ItemStatus = "Returned";
                    Audit(db, actor, "RETURN_TO_OWNER", "Items", item.ItemId, "Bàn giao hiện vật chưa bán cho "+recipient.Trim());
                    db.SaveChanges();
                    tx.Commit();
                    return record.ReturnId;
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Hiện vật đã thay đổi trên máy khác, hãy tải lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Đưa hàng không đạt giá sàn trở lại kho để xếp vào phiên khác; giữ nguyên lô và lịch sử bid.
        public void RequeueUnsold(int actor, int lotId) //Đưa hiện vật không bán được về luồng xử lý tiếp(543-570).
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "LOTS", "Edit");
                    var lot = db.AuctionLots.Single(x => x.LotId == lotId);
                    var session = db.AuctionSessions.Single(x => x.SessionId == lot.SessionId);
                    var item = db.Items.Single(x => x.ItemId == lot.ItemId);
                    if (session.SessionStatus != "Closed" || lot.LotStatus != "ReserveNotMet" || item.ItemStatus != "Unsold")
                    throw new BusinessException("Chỉ đưa hiện vật không bán được về kho sau khi kết thúc phiên.");
                    if (db.ConsignorReturns.Any(r => r.ItemId == item.ItemId) || 
                    db.AuctionLots.Any(x => x.ItemId == item.ItemId && x.LotId != lotId && 
                    (x.LotStatus == "Planned" || x.LotStatus == "Open" || x.LotStatus == "Sold")))
                    throw new BusinessException("Hiện vật đã được trả hoặc xếp vào phiên khác.");
                    item.ItemStatus = "InStock";
                    Audit(db, actor, "REQUEUE_UNSOLD", "Items", item.ItemId, "Trả hiện vật về kho chờ đấu giá lại");
                    db.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
    }
}
