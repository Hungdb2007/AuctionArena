using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using AuctionArena.Data;
using AuctionArena.Data.Models;
namespace AuctionArena.Services
{
    // Điều kiện mở phiên và xếp lô được kiểm tra tại Service.
    public sealed class AuctionManagementService : ServiceBase
    {
        public AuctionManagementService(Func<AuctionArenaDbContext> factory = null) : base(factory) //Khởi tạo AuctionManagementService. Nhận phụ thuộc/cấu hình ban đầ.
        {
        }
        public List<AuctionSessionEntity> Sessions(int actor, string keyword) // Lấy danh sách phiên.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "SESSIONS", "View");
                return db.AuctionSessions.AsNoTracking().Where(x => x.SessionCode.Contains(keyword) || x.SessionName.Contains(keyword))
                .OrderByDescending(x => x.StartsAt).ToList();
            }
        }
        public int SaveSession(int actor, int id, string code, string name, DateTime start, DateTime end) // Kiểm tra thời gian/trạng thái và lưu phiên
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length > 30 || string.IsNullOrWhiteSpace(name) || name.Length > 160 || end <= start)
            throw new BusinessException("Mã, tên hoặc thời gian phiên không hợp lệ.");
            using (var db = Factory()) using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "SESSIONS", id == 0 ? "Create" : "Edit");
                    if (db.AuctionSessions.Any(x => x.SessionCode == code && x.SessionId != id))
                    {
                        throw new BusinessException("Mã phiên đã tồn tại.");
                    }
                    AuctionSessionEntity entity;
                    if (id == 0)
                    {
                        entity = new AuctionSessionEntity
                        {
                            SessionStatus = "Draft"
                        };
                        db.AuctionSessions.Add(entity);
                    }
                    else
                    {
                        entity = db.AuctionSessions.Single(x => x.SessionId == id);
                        if (entity.SessionStatus != "Draft" && entity.SessionStatus != "Planned")
                        {
                            throw new BusinessException("Chỉ sửa phiên nháp hoặc đã lên lịch.");
                        }
                        if (db.AuctionLots.Any(x => x.SessionId == id && (x.OpensAt < start || x.ClosesAt > end)))
                        throw new BusinessException("Thời gian phiên phải bao trùm các lot.");
                    }
                    entity.SessionCode = code.Trim();
                    entity.SessionName = name.Trim();
                    entity.StartsAt = start;
                    entity.EndsAt = end;
                    db.SaveChanges();
                    Audit(db, actor, "SAVE_SESSION", "AuctionSessions", entity.SessionId, "Lưu phiên đấu giá");
                    db.SaveChanges();
                    tx.Commit();
                    return entity.SessionId;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Phiên chuyển từ Nháp sang Sắp diễn ra sau khi kiểm tra điều kiện.
        // Nhân bản chỉ sao chép thông tin phiên, không sao chép giao dịch.
        public int CloneSessionDraft(int actor, int sourceId, string code, string name, DateTime begins, DateTime ends)
        {//Tạo bản nháp dựa trên phiên( 76 - 109 ).
            if (string.IsNullOrWhiteSpace(code) || code.Trim().Length>30 || 
            string.IsNullOrWhiteSpace(name) || name.Trim().Length>160 || ends<=begins)
            throw new BusinessException("Mã, tên hoặc thời gian phiên mới không hợp lệ.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "SESSIONS", "Create");
                    var original = db.AuctionSessions.SingleOrDefault(x => x.SessionId == sourceId);
                    if (original == null)
                    {
                        throw new BusinessException("Không tìm thấy phiên nguồn.");
                    }
                    if (db.AuctionSessions.Any(x => x.SessionCode == code.Trim()))
                    throw new BusinessException("Mã phiên mới đã tồn tại.");
                    var copy = new AuctionSessionEntity
                    {
                        SessionCode = code.Trim(), SessionName = name.Trim(), 
                        StartsAt = begins, EndsAt = ends, SessionStatus = "Draft"
                    };
                    db.AuctionSessions.Add(copy);
                    db.SaveChanges();
                    Audit(db, actor, "CLONE_SESSION", "AuctionSessions", copy.SessionId, 
                    "Nhân bản lịch từ phiên #"+sourceId+"; không sao chép lot, đăng ký và tiền cọc.");
                    db.SaveChanges();
                    tx.Commit();
                    return copy.SessionId;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Sắp lại số lô theo thứ tự trong phiên chưa mở.
        public void RenumberPlannedLots(int actor, int sessionId) // Đánh lại số lô khi phiên còn lập kế hoạch.
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "LOTS", "Edit");
                    var session = db.AuctionSessions.SingleOrDefault(x => x.SessionId == sessionId);
                    if (session == null || (session.SessionStatus != "Draft" && session.SessionStatus != "Planned"))
                    throw new BusinessException("Chỉ đánh số lot trước phiên.");
                    var lots = db.AuctionLots.Where(x => x.SessionId == sessionId).OrderBy(x => x.LotNumber).ToList();
                    if (lots.Count == 0)
                    {
                        throw new BusinessException("Phiên chưa có lot.");
                    }
                    var ids = lots.Select(x => x.LotId).ToList();
                    if (lots.Any(x => x.LotStatus != "Planned") || db.Bids.Any(x => ids.Contains(x.LotId)) || 
                    db.Registrations.Any(x => x.SessionId == sessionId))
                    throw new BusinessException("Lot đã phát sinh giao dịch hoặc đăng ký; không thể đánh số lại.");
                    if (lots.Max(x => x.LotNumber)>int.MaxValue-lots.Count)
                    throw new BusinessException("Không còn khoảng số tạm an toàn.");
                    int offset = lots.Max(x => x.LotNumber)+1;
                    for (int i = 0; i<lots.Count; i++)
                    {
                        lots[i].LotNumber = offset+i;
                        db.SaveChanges();
                    }
                    for (int i = 0; i<lots.Count; i++)
                    {
                        lots[i].LotNumber = i+1;
                        db.SaveChanges();
                    }
                    Audit(db, actor, "RENUMBER_LOTS", "AuctionSessions", sessionId, 
                    "Đánh số liên tục "+lots.Count+" lot; không thay đổi giờ đấu.");
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
        public void ScheduleSession(int actor, int id) //Lập lịch phiên và các lô.
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "SESSIONS", "Edit");
                    var session = db.AuctionSessions.SingleOrDefault(x => x.SessionId == id);
                    if (session == null || session.SessionStatus != "Draft")
                    throw new BusinessException("Chỉ được duyệt lịch phiên đang ở trạng thái Nháp.");
                    if (session.EndsAt<=Now())
                    {
                        throw new BusinessException("Không duyệt lịch phiên đã hết giờ.");
                    }
                    if (!db.AuctionLots.Any(x => x.SessionId == id && x.LotStatus == "Planned"))
                    throw new BusinessException("Phiên chưa có lot. Hãy lập lot trước khi duyệt lịch.");
                    session.SessionStatus = "Planned";
                    session.ApprovedByUserId = actor;
                    Audit(db, actor, "SCHEDULE_SESSION", "AuctionSessions", id, "Duyệt lịch phiên – Sắp diễn ra");
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
        public void OpenSession(int actor, int id) // Kiểm tra điều kiện rồi mở phiên( 187-202 )
        {
            using (var db = Factory()) using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "SESSIONS", "Edit");
                    var s = db.AuctionSessions.Single(x => x.SessionId == id);
                    // Không mở phiên khi chủ hàng thiếu giấy tờ, vì không kiểm tra được việc tự trả giá.
                    var sellerIds = (from l in db.AuctionLots join i in db.Items on l.ItemId equals i.ItemId
                    where l.SessionId == id select i.ConsignorId).Distinct().ToList();
                    var sellerDocuments = db.Consignors.Where(x => sellerIds.Contains(x.ConsignorId))
                    .Select(x => x.IdentityNumber).ToList();
                    if (sellerDocuments.Any(x => string.IsNullOrWhiteSpace(x)))
                    throw new BusinessException("Một chủ hàng trong phiên chưa có giấy tờ định danh. Hãy hoàn tất hồ sơ trước khi mở phiên.");
                    if (s.SessionStatus != "Planned" || DateTime.Now < s.StartsAt || DateTime.Now >= s.EndsAt)
                    throw new BusinessException("Phiên chưa đến giờ mở hoặc không ở trạng thái chuẩn bị.");
                    if (!db.AuctionLots.Any(x => x.SessionId == id))
                    {
                        throw new BusinessException("Phiên chưa có lot.");
                    }
                    s.SessionStatus = "Open";
                    s.ApprovedByUserId = actor;
                    Audit(db, actor, "OPEN_SESSION", "AuctionSessions", id, "Mở phiên đấu giá");
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
        public List<AuctionLotEntity> Lots(int actor, int sessionId) //Lấy lô của phiên.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "LOTS", "View");
                return db.AuctionLots.AsNoTracking().Where(x => x.SessionId == sessionId).OrderBy(x => x.LotNumber).ToList();
            }
        }
        public int CreateLot(int actor, int sessionId, int itemId, int number, decimal startPrice, decimal reserve, decimal increment, DateTime open, DateTime close, decimal? depositRequired = null)
        {//Tạo lô từ hiện vật đủ điều kiện(229-299).
            ValidateMoneyOrZero(startPrice);
            ValidateMoneyOrZero(reserve);
            ValidateMoney(increment);
            if (depositRequired.HasValue) ValidateMoney(depositRequired.Value);
            if (number <= 0 || close <= open)
            throw new BusinessException("Số lot hoặc thời gian không hợp lệ.");
            using (var db = Factory()) using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "LOTS", "Create");
                    var session = db.AuctionSessions.Single(x => x.SessionId == sessionId);
                    if ((session.SessionStatus != "Draft" && session.SessionStatus != "Planned") || open < session.StartsAt || close > session.EndsAt)
                    throw new BusinessException("Chỉ thiết lập lot trong phiên chuẩn bị và trong thời gian phiên.");
                    if (db.Registrations.Any(x => x.SessionId == sessionId))
                    throw new BusinessException("Phiên đã cấp paddle/thu cọc. Không thêm lot mới vì có thể thay đổi mức cọc yêu cầu của khách.");
                    var item = db.Items.Single(x => x.ItemId == itemId);
                    if (item.ItemStatus != "InStock")
                    {
                        throw new BusinessException("Chỉ hiện vật trong kho được đưa vào lot.");
                    }
                    // Cần giấy tờ của bên bán để ngăn chủ hàng dùng hồ sơ khách trả giá chính tài sản của mình.
                    var seller = db.Consignors.Single(x => x.ConsignorId == item.ConsignorId);
                    if (string.IsNullOrWhiteSpace(seller.IdentityNumber))
                    throw new BusinessException("Chủ hàng chưa có số giấy tờ/đăng ký doanh nghiệp. Hãy bổ sung trước khi đưa tài sản vào phiên.");
                    if (db.AuctionLots.Any(x => x.SessionId == sessionId && (x.LotNumber == number || x.ItemId == itemId)) || 
                    db.AuctionLots.Any(x => x.ItemId == itemId && (x.LotStatus == "Open" || x.LotStatus == "Planned")))
                    throw new BusinessException("Số lot trùng hoặc hiện vật đang thuộc lot khác.");
                    var consignment = (from d in db.ConsignmentContractDetails
                    join c in db.ConsignmentContracts on d.ContractId equals c.ContractId
                    where d.ItemId == itemId
                    select new
                    {
                        c.ContractStatus, c.ExpiresAt, c.SellerFeeRate, c.ConsignorId
                    }).SingleOrDefault();
                    if (consignment == null || consignment.ContractStatus != "Active" || consignment.ConsignorId != item.ConsignorId)
                    throw new BusinessException("Hiện vật chưa có hợp đồng ký gửi còn hiệu lực với đúng chủ hàng.");
                    if (consignment.ExpiresAt.HasValue && close.Date>consignment.ExpiresAt.Value.Date)
                    throw new BusinessException("Lot kết thúc sau ngày hết hạn hợp đồng ký gửi. Hãy gia hạn hợp đồng trước.");
                    decimal effectiveDeposit = depositRequired ?? Math.Round(startPrice*RateSetting(db, "DepositRate"), 2, MidpointRounding.AwayFromZero);
                    if (effectiveDeposit<=0m)
                    throw new BusinessException("Mức cọc bằng 0. Cần nhập cọc riêng cho lot hoặc kiểm tra tỷ lệ cọc.");
                    var lot = new AuctionLotEntity
                    {
                        SessionId = sessionId, ItemId = itemId, LotNumber = number, StartingPrice = startPrice, 
                        ReservePrice = reserve, BidIncrement = increment, DepositRequired = effectiveDeposit, 
                        CurrentPrice = startPrice, OpensAt = open, ClosesAt = close, LotStatus = "Planned"
                    };
                    // Chốt tỷ lệ phí người bán tại thời điểm xếp lô, không lấy lại sau khi phiên chạy.
                    var ownerRate = db.Consignors.Where(x => x.ConsignorId == item.ConsignorId)
                    .Select(x => x.SellerFeeRate).SingleOrDefault();
                    decimal chosenRate = consignment.SellerFeeRate??ownerRate??RateSetting(db, "SellerFeeRate");
                    if (chosenRate<0m || chosenRate>1m)
                    throw new BusinessException("Phí người bán ngoài khoảng 0–100%, cần kiểm tra hợp đồng.");
                    lot.SellerFeeRateLocked = chosenRate;
                    db.AuctionLots.Add(lot);
                    db.SaveChanges();
                    Audit(db, actor, "CREATE_LOT", "AuctionLots", lot.LotId, "Thiết lập lot đấu giá");
                    db.SaveChanges();
                    tx.Commit();
                    return lot.LotId;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public sealed class NewLotRow
        {
            public int ItemId
            {
                get;
                set;
            }
            public int LotNumber
            {
                get;
                set;
            }
            public decimal StartingPrice
            {
                get;
                set;
            }
            public decimal ReservePrice
            {
                get;
                set;
            }
            public decimal BidIncrement
            {
                get;
                set;
            }
            public decimal? DepositRequired
            {
                get;
                set;
            }
            public DateTime OpensAt
            {
                get;
                set;
            }
            public DateTime ClosesAt
            {
                get;
                set;
            }
        }
        // Lưu các lô trong cùng transaction để lỗi ở một dòng không làm lưu dở.
        public int CreateLotsBatch(int actor, int sessionId, IList<NewLotRow> rows) // Tạo nhiều lô trong một giao dịch(344-410)
        {
            if (rows == null || rows.Count == 0 || rows.Count>100 || rows.Any(x => x == null) || 
            rows.Select(x => x.ItemId).Distinct().Count() != rows.Count || 
            rows.Select(x => x.LotNumber).Distinct().Count() != rows.Count)
            throw new BusinessException("Chọn tối đa 100 hiện vật, không trùng mã hiện vật hoặc số lot.");
            foreach (var row in rows)
            if (row.ItemId<=0 || row.LotNumber<=0 || row.BidIncrement<=0m || row.StartingPrice<0m || 
            row.ReservePrice<row.StartingPrice || row.ClosesAt<=row.OpensAt || 
            row.DepositRequired.HasValue && row.DepositRequired.Value<=0m)
            throw new BusinessException("Giá, số lot hoặc thời gian trong danh sách không hợp lệ.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "LOTS", "Create");
                    var session = db.AuctionSessions.SingleOrDefault(x => x.SessionId == sessionId);
                    if (session == null || (session.SessionStatus != "Draft" && session.SessionStatus != "Planned"))
                    throw new BusinessException("Chỉ tạo lot khi phiên chưa mở.");
                    if (db.Registrations.Any(x => x.SessionId == sessionId))
                    throw new BusinessException("Phiên đã có đăng ký/tiền cọc; không thêm lot hàng loạt.");
                    if (rows.Any(x => x.OpensAt<session.StartsAt || x.ClosesAt>session.EndsAt))
                    throw new BusinessException("Thời gian lot phải nằm trong thời gian phiên.");
                    var itemIds = rows.Select(x => x.ItemId).ToList();
                    var lotNumbers = rows.Select(x => x.LotNumber).ToList();
                    var items = db.Items.Where(x => itemIds.Contains(x.ItemId)).ToList();
                    if (items.Count != rows.Count || items.Any(x => x.ItemStatus != "InStock") || 
                    db.AuctionLots.Any(x => x.SessionId == sessionId && lotNumbers.Contains(x.LotNumber)) || 
                    db.AuctionLots.Any(x => itemIds.Contains(x.ItemId)))
                    throw new BusinessException("Có số lot trùng hoặc hiện vật không sẵn sàng.");
                    var contractItems = (from d in db.ConsignmentContractDetails
                    join c in db.ConsignmentContracts on d.ContractId equals c.ContractId
                    where itemIds.Contains(d.ItemId) && c.ContractStatus == "Active"
                    select d.ItemId).Distinct().ToList();
                    if (contractItems.Count != rows.Count)
                    throw new BusinessException("Hiện vật chưa có hợp đồng ký gửi còn hiệu lực.");
                    decimal fallback = RateSetting(db, "DepositRate");
                    foreach (var row in rows)
                    {
                        var item = items.Single(x => x.ItemId == row.ItemId);
                        decimal deposit = row.DepositRequired??Math.Round(row.StartingPrice*fallback, 2, MidpointRounding.AwayFromZero);
                        if (deposit<=0m)
                        {
                            throw new BusinessException("Tiền cọc lot phải lớn hơn 0.");
                        }
                        db.AuctionLots.Add(new AuctionLotEntity
                        {
                            SessionId = sessionId, ItemId = row.ItemId, 
                            LotNumber = row.LotNumber, StartingPrice = row.StartingPrice, ReservePrice = row.ReservePrice, 
                            BidIncrement = row.BidIncrement, DepositRequired = deposit, CurrentPrice = row.StartingPrice, 
                            OpensAt = row.OpensAt, ClosesAt = row.ClosesAt, LotStatus = "Planned"
                        });
                    }
                    db.SaveChanges();
                    Audit(db, actor, "CREATE_LOTS_BATCH", "AuctionSessions", sessionId, 
                    "Xếp hàng loạt "+rows.Count+" hiện vật, không phát sinh khách/cọc");
                    db.SaveChanges();
                    tx.Commit();
                    return rows.Count;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public void OpenLot(int actor, int lotId) //Mở lô khi thỏa điều kiện(411-446).
        {
            using (var db = Factory()) using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "LOTS", "Edit");
                    var lot = db.AuctionLots.Single(x => x.LotId == lotId);
                    var session = db.AuctionSessions.Single(x => x.SessionId == lot.SessionId);
                    if (session.SessionStatus != "Open" || lot.LotStatus != "Planned" || DateTime.Now < lot.OpensAt || DateTime.Now >= lot.ClosesAt)
                    throw new BusinessException("Lot không thể mở tại thời điểm hiện tại.");
                    if (db.Items.Single(x => x.ItemId == lot.ItemId).ItemStatus != "InStock")
                    throw new BusinessException("Hiện vật không còn trong kho.");
                    var contract = (from d in db.ConsignmentContractDetails
                    join c in db.ConsignmentContracts on d.ContractId equals c.ContractId
                    where d.ItemId == lot.ItemId
                    select new
                    {
                        c.ContractStatus, c.ExpiresAt
                    }).SingleOrDefault();
                    if (contract == null || contract.ContractStatus != "Active" || 
                    (contract.ExpiresAt.HasValue && Now().Date>contract.ExpiresAt.Value.Date))
                    throw new BusinessException("Hợp đồng ký gửi hết hạn hoặc không còn hiệu lực.");
                    lot.LotStatus = "Open";
                    db.Items.Single(x => x.ItemId == lot.ItemId).ItemStatus = "InAuction";
                    Audit(db, actor, "OPEN_LOT", "AuctionLots", lotId, "Mở lot");
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
        // Hủy phiên nháp chưa phát sinh đăng ký, bid hay hóa đơn; không xóa lịch sử.
        public void CancelDraftSession(int actor, int sessionId) //Hủy phiên nháp.
        {
            using (var db = Factory()) using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "SESSIONS", "Edit");
                    var session = db.AuctionSessions.Single(x => x.SessionId == sessionId);
                    if ((session.SessionStatus != "Draft" && session.SessionStatus != "Planned") || db.Registrations.Any(x => x.SessionId == sessionId) || 
                    db.Invoices.Any(x => x.SessionId == sessionId) || 
                    db.Bids.Any(x => db.AuctionLots.Any(l => l.LotId == x.LotId && l.SessionId == sessionId)))
                    throw new BusinessException("Chỉ được hủy phiên nháp chưa có đăng ký hoặc giao dịch.");
                    var lots = db.AuctionLots.Where(x => x.SessionId == sessionId).ToList();
                    if (lots.Any(x => x.LotStatus != "Planned"))
                    {
                        throw new BusinessException("Phiên chứa lot đã vận hành, không thể hủy.");
                    }
                    foreach (var lot in lots) lot.LotStatus = "Cancelled";
                    session.SessionStatus = "Cancelled";
                    Audit(db, actor, "CANCEL_SESSION", "AuctionSessions", sessionId, "Hủy phiên nháp an toàn");
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
        // Chỉ cần quyền xem lịch sử trả giá để tra cứu.
        public List<BidFilterOption> BidSessions(int actor) //Lấy phiên dùng lọc lịch sử bid (479-490).
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "BIDS", "View");
                return db.AuctionSessions.AsNoTracking().OrderByDescending(s => s.StartsAt)
                .Select(s => new BidFilterOption
                {
                    Id = s.SessionId, Name = s.SessionCode+" – "+s.SessionName
                }).ToList();
            }
        }
        public List<BidFilterOption> BidLots(int actor, int? sessionId)//Lấy lô dùng lọc lịch sử bid.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "BIDS", "View");
                var query = db.AuctionLots.AsNoTracking().AsQueryable();
                if (sessionId.HasValue)query = query.Where(x => x.SessionId == sessionId.Value);
                return query.OrderBy(x => x.SessionId).ThenBy(x => x.LotNumber)
                .Select(x => new
                {
                    x.LotId, x.LotNumber, x.SessionId
                }).ToList()
                .Select(x => new BidFilterOption
                {
                    Id = x.LotId, Name = "Lot "+x.LotNumber+" (phiên "+x.SessionId+")"
                }).ToList();
            }
        }
        public List<BidFilterOption> BidCustomers(int actor)//
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "BIDS", "View");
                return db.Customers.AsNoTracking().OrderBy(c => c.FullName)
                .Select(c => new BidFilterOption
                {
                    Id = c.CustomerId, Name = c.CustomerCode+" – "+c.FullName
                }).ToList();
            }
        }
        public List<RegistrationEntity> Registrations(int actor, int sessionId) //Truy vấn danh sách đăng ký..
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "REGISTRATIONS", "View");
                return db.Registrations.AsNoTracking().Where(x => x.SessionId == sessionId).OrderBy(x => x.PaddleNumber).ToList();
            }
        }
        public List<CustomerEntity> EligibleCustomers(int actor) //Lấy khách đủ điều kiện.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "REGISTRATIONS", "View");
                return db.Customers.AsNoTracking().Where(x => !x.IsBlacklisted && !x.HasOutstandingDebt).OrderBy(x => x.FullName).ToList();
            }
        }
        public List<ItemEntity> AvailableItems(int actor) //Lấy hiện vật có thể đưa vào lô.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "LOTS", "View");
                return db.Items.AsNoTracking().Where(x => x.ItemStatus == "InStock" && 
                !db.AuctionLots.Any(l => l.ItemId == x.ItemId && (l.LotStatus == "Planned" || l.LotStatus == "Open")))
                .OrderBy(x => x.ItemName).ToList();
            }
        }
        public List<BidView> BidHistory(int actor, int lotId)//Truy vấn lượt trả giá.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "BIDS", "View");
                return (from b in db.Bids join c in db.Customers on b.CustomerId equals c.CustomerId
                join r in db.Registrations on b.RegistrationId equals r.RegistrationId
                where b.LotId == lotId orderby b.BidAt descending, b.BidId descending
                select new BidView
                {
                    BidId = b.BidId, Time = b.BidAt, Customer = c.FullName, 
                    Paddle = r.PaddleNumber, Amount = b.Amount
                }).Take(150).ToList();
            }
        }
        // Danh sách thẻ tham gia hợp lệ cho chính lô đang mở, tránh nhập nhầm CustomerId.
        public List<PaddleBidOption> PaddlesForLot(int actor, int lotId) //Lấy số thẻ và khách tham gia lô.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "BIDS", "View");
                int sessionId = db.AuctionLots.Where(l => l.LotId == lotId).Select(l => l.SessionId).Single();
                return (from r in db.Registrations
                join c in db.Customers on r.CustomerId equals c.CustomerId
                where r.SessionId == sessionId && r.RegistrationStatus == "Active" && 
                r.DepositPaid >= r.DepositRequired && !c.IsBlacklisted && !c.HasOutstandingDebt
                orderby r.PaddleNumber
                select new PaddleBidOption
                {
                    CustomerId = c.CustomerId, 
                    PaddleNumber = r.PaddleNumber, CustomerName = c.FullName
                }).ToList();
            }
        }
        // Tra cứu lịch sử chỉ đọc, lọc theo phiên, lô, mã khách và khoảng ngày.
        public List<BidHistoryRow> SearchBidHistory(int actor, int? sessionId, int? lotId,  //Tìm lịch sử theo nhiều điều kiện(583-635).
        int? customerId, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            throw new BusinessException("Ngày bắt đầu không được sau ngày kết thúc.");
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "BIDS", "View");
                var query = from b in db.Bids
                join l in db.AuctionLots on b.LotId equals l.LotId
                join s in db.AuctionSessions on l.SessionId equals s.SessionId
                join c in db.Customers on b.CustomerId equals c.CustomerId
                join r in db.Registrations on b.RegistrationId equals r.RegistrationId
                select new BidHistoryRow
                {
                    BidId = b.BidId, SessionId = s.SessionId, 
                    SessionName = s.SessionName, LotId = l.LotId, LotNumber = l.LotNumber, 
                    CustomerId = c.CustomerId, CustomerName = c.FullName, Paddle = r.PaddleNumber, 
                    Amount = b.Amount, Time = b.BidAt, 
                    CurrentPrice = l.CurrentPrice, HammerPrice = l.HammerPrice, 
                    LeadingCustomerId = l.LeadingCustomerId, 
                    WinningCustomerId = l.WinningCustomerId, LotStatus = l.LotStatus
                };
                if (sessionId.HasValue) query = query.Where(x => x.SessionId == sessionId.Value);
                if (lotId.HasValue) query = query.Where(x => x.LotId == lotId.Value);
                if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
                if (fromDate.HasValue)
                {
                    DateTime start = fromDate.Value.Date;
                    query = query.Where(x => x.Time >= start);
                }
                if (toDate.HasValue)
                {
                    DateTime end = toDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.Time < end);
                }
                var result = query.OrderByDescending(x => x.Time).ThenByDescending(x => x.BidId).Take(1000).ToList();
                foreach (var row in result)
                {
                    // Chỉ lượt giá cuối cùng khớp giá hiện tại mới được đánh dấu dẫn đầu/trúng.
                    bool latest = row.Amount == row.CurrentPrice && row.LeadingCustomerId == row.CustomerId;
                    if (row.LotStatus == "Sold" && row.WinningCustomerId == row.CustomerId && 
                    row.HammerPrice == row.Amount && latest)
                    row.Result = "Trúng đấu giá";
                    else if (row.LotStatus == "Open" && latest)
                    row.Result = "Đang dẫn đầu";
                    else if (row.LotStatus == "ReserveNotMet" && latest)
                    row.Result = "Không đạt giá sàn";
                    else
                    row.Result = "Lượt giá trước";
                }
                return result;
            }
        }
        public AuctionLotEntity LotSnapshot(int actor, int lotId) //Đọc trạng thái/giá mới nhất của lô.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "BIDS", "View");
                return db.AuctionLots.AsNoTracking().Single(x => x.LotId == lotId);
            }
        }
    }
    public sealed class BidFilterOption
    {
        public int Id
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
    }
    public sealed class PaddleBidOption
    {
        public int CustomerId
        {
            get;
            set;
        }
        public int PaddleNumber
        {
            get;
            set;
        }
        public string CustomerName
        {
            get;
            set;
        }
        public override string ToString()    //Cung cấp nhãn hiển thị cho đối tượng lựa chọn
        {
            return "Thẻ " + PaddleNumber + " – " + CustomerName;
        }
    }
    public sealed class BidHistoryRow
    {
        public long BidId
        {
            get;
            set;
        }
        public int SessionId
        {
            get;
            set;
        }
        public string SessionName
        {
            get;
            set;
        }
        public int LotId
        {
            get;
            set;
        }
        public int LotNumber
        {
            get;
            set;
        }
        public int CustomerId
        {
            get;
            set;
        }
        public string CustomerName
        {
            get;
            set;
        }
        public int Paddle
        {
            get;
            set;
        }
        public decimal Amount
        {
            get;
            set;
        }
        public DateTime Time
        {
            get;
            set;
        }
        public string Result
        {
            get;
            set;
        }
        // Các trường tính toán nội bộ không đưa lên bảng hiển thị.
        [System.ComponentModel.Browsable(false)] public decimal CurrentPrice
        {
            get;
            set;
        }
        [System.ComponentModel.Browsable(false)] public decimal? HammerPrice
        {
            get;
            set;
        }
        [System.ComponentModel.Browsable(false)] public int? LeadingCustomerId
        {
            get;
            set;
        }
        [System.ComponentModel.Browsable(false)] public int? WinningCustomerId
        {
            get;
            set;
        }
        [System.ComponentModel.Browsable(false)] public string LotStatus
        {
            get;
            set;
        }
    }
    public sealed class BidView
    {
        public long BidId
        {
            get;
            set;
        }
        public DateTime Time
        {
            get;
            set;
        }
        public string Customer
        {
            get;
            set;
        }
        public int Paddle
        {
            get;
            set;
        }
        public decimal Amount
        {
            get;
            set;
        }
    }
}
