using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using AuctionArena.Data.Models;

namespace AuctionArena.Data
{
    public class AuctionArenaDbContext : DbContext
    {
        // Tên chuỗi kết nối được lấy từ App.config; không lưu tên máy ở đây.
        public AuctionArenaDbContext() : base("name=AuctionArenaDB")
        {
            Database.SetInitializer<AuctionArenaDbContext>(null);
        }
        // Chỉ dùng để xác minh kết nối trước khi ghi App.config.
        public AuctionArenaDbContext(string connectionString) : base(connectionString)
        {
            Database.SetInitializer<AuctionArenaDbContext>(null);
        }
        public DbSet<RoleEntity> Roles { get; set; }
        public DbSet<PermissionEntity> Permissions { get; set; }
        public DbSet<RolePermissionEntity> RolePermissions { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<CustomerEntity> Customers { get; set; }
        public DbSet<CustomerTierHistoryEntity> CustomerTierHistory {get; set; }
        public DbSet<ConsignorEntity> Consignors { get; set; }
        public DbSet<CategorieEntity> Categories { get; set; }
        public DbSet<ItemEntity> Items { get; set; }
        public DbSet<ItemImageEntity> ItemImages { get; set; }
        public DbSet<ConsignmentContractEntity> ConsignmentContracts { get; set; }
        public DbSet<ConsignmentContractDetailEntity> ConsignmentContractDetails { get; set; }
        public DbSet<StockReceiptEntity> StockReceipts { get; set; }
        public DbSet<StockReceiptDetailEntity> StockReceiptDetails { get; set; }
        public DbSet<AuctionSessionEntity> AuctionSessions { get; set; }
        public DbSet<AuctionLotEntity> AuctionLots { get; set; }
        public DbSet<RegistrationEntity> Registrations { get; set; }
        public DbSet<DepositRefundObligationEntity> DepositRefundObligations { get; set; }
        public DbSet<BidEntity> Bids { get; set; }
        public DbSet<InvoiceEntity> Invoices { get; set; }
        public DbSet<InvoiceReviewCaseEntity> InvoiceReviewCases { get; set; }
        public DbSet<InvoiceDetailEntity> InvoiceDetails { get; set; }
        public DbSet<PaymentEntity> Payments { get; set; }
        public DbSet<StockIssueEntity> StockIssues { get; set; }
        public DbSet<ConsignorReturnEntity> ConsignorReturns { get; set; }
        public DbSet<InvoiceAdjustmentEntity> InvoiceAdjustments {get; set; }
        public DbSet<StockIssueDetailEntity> StockIssueDetails { get; set; }
        public DbSet<SellerPayoutEntity> SellerPayouts { get; set; }
        public DbSet<SellerPayoutOrderEntity> SellerPayoutOrders { get; set; }
        public DbSet<CustomerViolationEntity> CustomerViolations { get; set; }
        public DbSet<SystemSettingEntity> SystemSettings { get; set; }
        public DbSet<AuditLogEntity> AuditLogs { get; set; }
        // Chỉ đọc: danh sách cột thực tế trong database (INFORMATION_SCHEMA.COLUMNS).
        public DbSet<SchemaColumnEntity> SchemaColumns { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Entity<RolePermissionEntity>().HasKey(x => new { x.RoleId, x.PermissionId });
            modelBuilder.Entity<UserEntity>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            modelBuilder.Entity<UserEntity>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<CustomerEntity>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            modelBuilder.Entity<CustomerEntity>().Property(x => x.DateOfBirth).HasColumnType("datetime2");
            modelBuilder.Entity<CustomerTierHistoryEntity>().Property(x => x.ChangedAt).HasColumnType("datetime2");
            modelBuilder.Entity<ItemEntity>().Property(x => x.EstimatePriceHigh).HasPrecision(19, 2);
            modelBuilder.Entity<ItemEntity>().Property(x => x.AppraisedAt).HasColumnType("datetime2");
            modelBuilder.Entity<ConsignorEntity>().Property(x => x.SellerFeeRate).HasPrecision(9, 6);
            modelBuilder.Entity<ConsignmentContractEntity>().Property(x => x.SellerFeeRate).HasPrecision(9, 6);
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.SellerFeeRateLocked).HasPrecision(9, 6);
            modelBuilder.Entity<InvoiceAdjustmentEntity>().Property(x => x.Amount).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceAdjustmentEntity>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            modelBuilder.Entity<ConsignorReturnEntity>().Property(x => x.ReturnedAt).HasColumnType("datetime2");
            modelBuilder.Entity<ConsignmentContractEntity>().Property(x => x.ExpiresAt).HasColumnType("datetime2");
            modelBuilder.Entity<ItemEntity>().Property(x => x.EstimatePrice).HasPrecision(19, 2);
            modelBuilder.Entity<ItemEntity>().Property(x => x.AppraisedPrice).HasPrecision(19, 2);
            modelBuilder.Entity<ItemEntity>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            modelBuilder.Entity<ItemEntity>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<ConsignmentContractEntity>().Property(x => x.SignedAt).HasColumnType("datetime2");
            modelBuilder.Entity<StockReceiptEntity>().Property(x => x.ReceivedAt).HasColumnType("datetime2");
            modelBuilder.Entity<AuctionSessionEntity>().Property(x => x.StartsAt).HasColumnType("datetime2");
            modelBuilder.Entity<AuctionSessionEntity>().Property(x => x.EndsAt).HasColumnType("datetime2");
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.StartingPrice).HasPrecision(19, 2);
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.ReservePrice).HasPrecision(19, 2);
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.BidIncrement).HasPrecision(19, 2);
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.DepositRequired).HasPrecision(19, 2);
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.CurrentPrice).HasPrecision(19, 2);
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.HammerPrice).HasPrecision(19, 2);
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.OpensAt).HasColumnType("datetime2");
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.ClosesAt).HasColumnType("datetime2");
            modelBuilder.Entity<AuctionLotEntity>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<DepositRefundObligationEntity>().Property(x => x.DueAmount).HasPrecision(19, 2);
            modelBuilder.Entity<DepositRefundObligationEntity>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            modelBuilder.Entity<DepositRefundObligationEntity>().Property(x => x.SettledAt).HasColumnType("datetime2");
            modelBuilder.Entity<RegistrationEntity>().Property(x => x.DepositRequired).HasPrecision(19, 2);
            modelBuilder.Entity<RegistrationEntity>().Property(x => x.DepositPaid).HasPrecision(19, 2);
            modelBuilder.Entity<RegistrationEntity>().Property(x => x.DepositApplied).HasPrecision(19, 2);
            modelBuilder.Entity<RegistrationEntity>().Property(x => x.DepositRefunded).HasPrecision(19, 2);
            modelBuilder.Entity<RegistrationEntity>().Property(x => x.DepositForfeited).HasPrecision(19, 2);
            modelBuilder.Entity<RegistrationEntity>().Property(x => x.RegisteredAt).HasColumnType("datetime2");
            modelBuilder.Entity<RegistrationEntity>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<BidEntity>().Property(x => x.Amount).HasPrecision(19, 2);
            modelBuilder.Entity<BidEntity>().Property(x => x.BidAt).HasColumnType("datetime2");
            modelBuilder.Entity<InvoiceReviewCaseEntity>().Property(x => x.RemainingAtReview).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceReviewCaseEntity>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            modelBuilder.Entity<InvoiceReviewCaseEntity>().Property(x => x.ResolvedAt).HasColumnType("datetime2");
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.IssuedAt).HasColumnType("datetime2");
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.DueAt).HasColumnType("datetime2");
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.HammerSubtotal).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.BuyerFee).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.VipDiscount).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.VatAmount).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.DepositApplied).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.TotalDue).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.PaidAmount).HasPrecision(19, 2);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.BuyerFeeRate).HasPrecision(9, 6);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.VatRate).HasPrecision(9, 6);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.VipDiscountRate).HasPrecision(9, 6);
            modelBuilder.Entity<InvoiceEntity>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<InvoiceDetailEntity>().Property(x => x.HammerPrice).HasPrecision(19, 2);
            modelBuilder.Entity<PaymentEntity>().Property(x => x.Amount).HasPrecision(19, 2);
            modelBuilder.Entity<PaymentEntity>().Property(x => x.PaidAt).HasColumnType("datetime2");
            modelBuilder.Entity<StockIssueEntity>().Property(x => x.IssuedAt).HasColumnType("datetime2");
            modelBuilder.Entity<SellerPayoutOrderEntity>().Property(x => x.HammerPrice).HasPrecision(19, 2);
            modelBuilder.Entity<SellerPayoutOrderEntity>().Property(x => x.SellerFeeRate).HasPrecision(9, 6);
            modelBuilder.Entity<SellerPayoutOrderEntity>().Property(x => x.SellerFee).HasPrecision(19, 2);
            modelBuilder.Entity<SellerPayoutOrderEntity>().Property(x => x.PayoutAmount).HasPrecision(19, 2);
            modelBuilder.Entity<SellerPayoutOrderEntity>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            modelBuilder.Entity<SellerPayoutOrderEntity>().Property(x => x.PaidAt).HasColumnType("datetime2");
            modelBuilder.Entity<SellerPayoutEntity>().Property(x => x.HammerPrice).HasPrecision(19, 2);
            modelBuilder.Entity<SellerPayoutEntity>().Property(x => x.SellerFeeRate).HasPrecision(9, 6);
            modelBuilder.Entity<SellerPayoutEntity>().Property(x => x.SellerFee).HasPrecision(19, 2);
            modelBuilder.Entity<SellerPayoutEntity>().Property(x => x.PayoutAmount).HasPrecision(19, 2);
            modelBuilder.Entity<SellerPayoutEntity>().Property(x => x.PayoutAt).HasColumnType("datetime2");
            modelBuilder.Entity<CustomerViolationEntity>().Property(x => x.OccurredAt).HasColumnType("datetime2");
            modelBuilder.Entity<SystemSettingEntity>().Property(x => x.UpdatedAt).HasColumnType("datetime2");
            modelBuilder.Entity<AuditLogEntity>().Property(x => x.OccurredAt).HasColumnType("datetime2");
        }
    }
}
