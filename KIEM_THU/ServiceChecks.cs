using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using AuctionArena.Data;
using AuctionArena.Data.Models;
using AuctionArena.Data.Security;
using AuctionArena.Services;

// Chỉ chạy trên database kiểm thử mới; không nhận AuctionArenaDB đang sử dụng.
public sealed class TestDbConfiguration : DbConfiguration
{
    public TestDbConfiguration()
    {
        SetProviderFactory("System.Data.SqlClient", SqlClientFactory.Instance);
        SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
    }
}
public static class ServiceChecks
{
    static string connection;
    static int passed, actor, restricted, customer, secondCustomer, openSession, closedSession, openLot;
    static int invoiceId, firstItem, secondItem;
    static AuctionArenaDbContext Db() { return new AuctionArenaDbContext(connection); }
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Denied(Action action)
    {
        try { action(); } catch (BusinessException) { return; }
        throw new Exception("Expected BusinessException was not raised.");
    }
    static void Test(string name, Action action)
    {
        action(); passed++; Console.WriteLine("PASS " + name);
    }
    public static int Main()
    {
        try
        {
            connection = Environment.GetEnvironmentVariable("AUCTIONARENA_TEST_CONNECTION");
            var options = new SqlConnectionStringBuilder(connection);
            if (!Regex.IsMatch(options.InitialCatalog, @"^AA_Test_[a-f0-9]{32}$"))
                throw new Exception("Only a fresh AA_Test_<32 hex> database is allowed.");
            DbConfiguration.SetConfiguration(new TestDbConfiguration());
            using (var db = Db())
                Assert(!db.Users.Any() && !db.Customers.Any(), "Test database must be empty.");
            Test("password_hash_and_salt", delegate {
                string salt = PasswordHasher.CreateSalt();
                string hash = PasswordHasher.Hash("TestOnlyAa123!", salt);
                Assert(salt.Length == 32 && hash.Length == 64, "Wrong hash format.");
                Assert(PasswordHasher.Verify("TestOnlyAa123!", salt, hash, 150000), "Password rejected.");
                Assert(!PasswordHasher.Verify("WrongAa123!", salt, hash, 150000), "Wrong password accepted.");
                Assert(PasswordHasher.Hash("TestOnlyAa123!", PasswordHasher.CreateSalt()) != hash, "Salt not applied.");
            });
            Test("initial_admin_and_login", delegate {
                new InitialAdminService(Db).Create("test_admin", "Test Administrator", "TestOnlyAa123!");
                actor = new AccountService(Db).Login("test_admin", "TestOnlyAa123!").UserId;
                Denied(delegate { new InitialAdminService(Db).Create("other_admin", "Other Admin", "TestOnlyAa123!"); });
            });
            Seed();
            Test("rbac_blocks_service_write", delegate {
                Denied(delegate { new AuctionService(Db).Bid(restricted, openLot, customer, 1000m); });
                using (var db = Db()) Assert(!db.Bids.Any(), "Unauthorized bid persisted.");
            });
            Test("bid_requires_full_deposit", delegate {
                Denied(delegate { new AuctionService(Db).Bid(actor, openLot, customer, 1000m); });
                using (var db = Db()) Assert(!db.Bids.Any(), "Unfunded bid persisted.");
            });
            Test("bid_price_and_leading_customer", delegate {
                using (var db = Db()) {
                    db.Registrations.Single(x => x.SessionId == openSession && x.CustomerId == customer).DepositPaid = 100m;
                    db.SaveChanges();
                }
                new AuctionService(Db).Bid(actor, openLot, customer, 1000m);
                Denied(delegate { new AuctionService(Db).Bid(actor, openLot, customer, 1100m); });
                Denied(delegate { new AuctionService(Db).Bid(actor, openLot, secondCustomer, 1050m); });
                new AuctionService(Db).Bid(actor, openLot, secondCustomer, 1100m);
                using (var db = Db()) {
                    Assert(db.Bids.Count() == 2, "Unexpected bid count.");
                    Assert(db.AuctionLots.Single(x => x.LotId == openLot).CurrentPrice == 1100m, "Price not updated.");
                }
            });
            Test("invoice_rolls_back_after_header_saved", delegate {
                using (var db = Db()) db.Database.ExecuteSqlCommand(
                    "CREATE TRIGGER dbo.AA_Test_FailDetail ON dbo.InvoiceDetails AFTER INSERT AS BEGIN THROW 51001, 'AA_TEST_DETAIL_FAILURE', 1; END");
                bool failed = false;
                try { new BillingService(Db).CreateInvoice(actor, closedSession, customer); }
                catch (Exception ex) { failed = ex.ToString().Contains("AA_TEST_DETAIL_FAILURE"); if (!failed) throw; }
                finally { using (var db = Db()) db.Database.ExecuteSqlCommand("DROP TRIGGER dbo.AA_Test_FailDetail"); }
                Assert(failed, "Injected detail failure did not occur.");
                using (var db = Db()) {
                    Assert(!db.Invoices.Any() && !db.InvoiceDetails.Any() && !db.Payments.Any(), "Partial invoice persisted.");
                    Assert(db.Registrations.Single(x => x.SessionId == closedSession).DepositApplied == 0m, "Deposit not rolled back.");
                }
            });
            Test("invoice_vip_fee_vat_and_deposit", delegate {
                invoiceId = new BillingService(Db).CreateInvoice(actor, closedSession, customer);
                using (var db = Db()) {
                    var invoice = db.Invoices.Single(x => x.InvoiceId == invoiceId);
                    Assert(invoice.HammerSubtotal == 300000m && invoice.BuyerFee == 15000m, "Wrong subtotal/fee.");
                    Assert(invoice.VipDiscount == 1500m && invoice.VatAmount == 1350m, "Wrong VIP/VAT.");
                    Assert(invoice.DepositApplied == 50000m && invoice.TotalDue == 264850m, "Wrong balance.");
                    Assert(db.InvoiceDetails.Count() == 2, "Missing invoice detail.");
                    Assert(db.Payments.Count(x => x.PaymentType == "DepositTransfer") == 1, "Missing deposit transfer.");
                }
                Denied(delegate { new BillingService(Db).CreateInvoice(actor, closedSession, customer); });
            });
            var lines = new List<SettlementService.DeliveryLine> {
                new SettlementService.DeliveryLine { ItemId = firstItem, Condition = "Tinh trang duoc kiem tra" }
            };
            Test("delivery_requires_paid_invoice", delegate {
                Denied(delegate { new SettlementService(Db).IssueItems(actor, invoiceId, "Test Recipient", lines, "123456789012", "Nguoi trung", "AtVenue"); });
                using (var db = Db()) Assert(!db.StockIssueDetails.Any(), "Unpaid item delivered.");
            });
            Test("payment_amount_and_bank_reference", delegate {
                Denied(delegate { new BillingService(Db).PayInvoice(actor, invoiceId, 264851m, "Cash"); });
                new BillingService(Db).PayInvoice(actor, invoiceId, 100000m, "BankTransfer", "AA-TEST-TRANSFER");
                Denied(delegate { new BillingService(Db).PayInvoice(actor, invoiceId, 100m, "BankTransfer", "AA-TEST-TRANSFER"); });
                new BillingService(Db).PayInvoice(actor, invoiceId, 164850m, "Cash");
                using (var db = Db()) Assert(db.Invoices.Single().InvoiceStatus == "Paid", "Invoice not paid.");
            });
            Test("partial_delivery_and_duplicate_prevention", delegate {
                new SettlementService(Db).IssueItems(actor, invoiceId, "Test Recipient", lines, "123456789012", "Nguoi trung", "AtVenue");
                using (var db = Db()) {
                    Assert(db.Items.Single(x => x.ItemId == firstItem).ItemStatus == "Delivered", "Delivered item not updated.");
                    Assert(db.Items.Single(x => x.ItemId == secondItem).ItemStatus == "Sold", "Unselected item changed.");
                }
                Denied(delegate { new SettlementService(Db).IssueItems(actor, invoiceId, "Test Recipient", lines, "123456789012", "Nguoi trung", "AtVenue"); });
                using (var db = Db()) Assert(db.StockIssues.Count() == 1 && db.StockIssueDetails.Count() == 1, "Duplicate issue persisted.");
            });
            Console.WriteLine("RESULT: " + passed + "/10 passed");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL after " + passed + " checks: " + ex); return 1; }
    }
    static void Seed()
    {
        using (var db = Db())
        {
            var role = new RoleEntity { RoleCode = "TEST_NONE", RoleName = "Test No Permissions", IsActive = true };
            db.Roles.Add(role); db.SaveChanges();
            string salt = PasswordHasher.CreateSalt();
            var user = new UserEntity { Username = "test_restricted", FullName = "Test Restricted", RoleId = role.RoleId,
                IsActive = true, CreatedAt = DateTime.UtcNow, PasswordSalt = salt,
                PasswordHash = PasswordHasher.Hash("TestOnlyAa123!", salt), HashIterations = 150000 };
            db.Users.Add(user);
            var a = new CustomerEntity { CustomerCode = "TEST_A", FullName = "Test A", IdentityNumber = "111111111111", MemberTier = "VIP", CustomerStatus = "Active", CreatedAt = DateTime.Now };
            var b = new CustomerEntity { CustomerCode = "TEST_B", FullName = "Test B", IdentityNumber = "222222222222", MemberTier = "Normal", CustomerStatus = "Active", CreatedAt = DateTime.Now };
            db.Customers.Add(a); db.Customers.Add(b);
            var owner = new ConsignorEntity { ConsignorCode = "TEST_OWNER", FullName = "Test Owner", IdentityNumber = "999999999999", ConsignorType = "Individual", ConsignorStatus = "Active" };
            var category = new CategorieEntity { CategoryName = "Test Category", IsActive = true };
            db.Consignors.Add(owner); db.Categories.Add(category);
            var open = new AuctionSessionEntity { SessionCode = "TEST_OPEN", SessionName = "Test Open", StartsAt = DateTime.Now.AddHours(-1), EndsAt = DateTime.Now.AddHours(1), SessionStatus = "Open" };
            var closed = new AuctionSessionEntity { SessionCode = "TEST_CLOSED", SessionName = "Test Closed", StartsAt = DateTime.Now.AddHours(-2), EndsAt = DateTime.Now.AddMinutes(-1), SessionStatus = "Closed" };
            db.AuctionSessions.Add(open); db.AuctionSessions.Add(closed); db.SaveChanges();
            actor = db.Users.Single(x => x.Username == "test_admin").UserId;
            restricted = user.UserId; customer = a.CustomerId; secondCustomer = b.CustomerId; openSession = open.SessionId; closedSession = closed.SessionId;
            for (int i = 0; i < 3; i++) {
                var item = new ItemEntity { ItemCode = "TEST_ITEM_" + i, ItemName = "Test Item " + i, CategoryId = category.CategoryId, ConsignorId = owner.ConsignorId,
                    EstimatePrice = 1000m, ItemStatus = i == 0 ? "InAuction" : "Sold", CreatedAt = DateTime.Now, WarehouseLocation = "Test Storage" };
                db.Items.Add(item); db.SaveChanges();
                var lot = new AuctionLotEntity { SessionId = i == 0 ? openSession : closedSession, ItemId = item.ItemId, LotNumber = i + 1, StartingPrice = 1000m, ReservePrice = 1000m,
                    BidIncrement = 100m, CurrentPrice = i == 0 ? 0m : 100000m * i,
                    WinningCustomerId = i == 0 ? (int?)null : customer, HammerPrice = i == 0 ? (decimal?)null : 100000m * i,
                    OpensAt = DateTime.Now.AddHours(-1), ClosesAt = i == 0 ? DateTime.Now.AddHours(1) : DateTime.Now.AddMinutes(-1), LotStatus = i == 0 ? "Open" : "Sold" };
                db.AuctionLots.Add(lot); db.SaveChanges();
                if (i == 0) openLot = lot.LotId; else if (i == 1) firstItem = item.ItemId; else secondItem = item.ItemId;
            }
            db.Registrations.Add(new RegistrationEntity { SessionId = openSession, CustomerId = customer, PaddleNumber = 1, DepositRequired = 100m, DepositPaid = 0m, RegistrationStatus = "Active", RegisteredAt = DateTime.Now });
            db.Registrations.Add(new RegistrationEntity { SessionId = openSession, CustomerId = secondCustomer, PaddleNumber = 2, DepositRequired = 100m, DepositPaid = 100m, RegistrationStatus = "Active", RegisteredAt = DateTime.Now });
            db.Registrations.Add(new RegistrationEntity { SessionId = closedSession, CustomerId = customer, PaddleNumber = 1, DepositRequired = 50000m, DepositPaid = 50000m, RegistrationStatus = "Active", RegisteredAt = DateTime.Now });
            db.SaveChanges();
        }
    }
}
