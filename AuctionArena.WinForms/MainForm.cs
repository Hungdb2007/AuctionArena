using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    public sealed class MainForm : Form
    {
        UserInfo user;
        readonly AccountService account = new AccountService();
        readonly Panel side = new Panel(), content = new Panel();
        readonly TableLayoutPanel shell = new TableLayoutPanel();
        readonly Timer refreshTimer = new Timer
        {
            Interval = 60000
        };
        bool welcomed, loggedOut, logoutRequested, forceLogout;
        int periodIndex;
        Dictionary<string, PermissionFlags> permissions;
        public MainForm(UserInfo user)
        {
            this.user = user;
            Text = "Auction Arena | Quản lý sàn đấu giá";
            Width = 1280;
            Height = 820;
            MinimumSize = new Size(1024, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Theme.Apply(this);
            // TableLayoutPanel chia rõ sidebar / nội dung, tránh Dock Fill chồng lên Dock Left.
            shell.Dock = DockStyle.Fill;
            shell.Margin = Padding.Empty;
            shell.Padding = Padding.Empty;
            shell.ColumnCount = 2;
            shell.RowCount = 1;
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            side.Dock = DockStyle.Fill;
            side.AutoScroll = true;
            side.BackColor = Theme.Panel;
            side.Margin = Padding.Empty;
            content.Dock = DockStyle.Fill;
            content.BackColor = Theme.Bg;
            content.Margin = Padding.Empty;
            shell.Controls.Add(side, 0, 0);
            shell.Controls.Add(content, 1, 0);
            Controls.Add(shell);
            BuildMenu();
            Shown+=(s, e) => 
            {
                ShowDashboard();
                refreshTimer.Start();
            };
            refreshTimer.Tick+=(s, e) => 
            {
                if (content.Controls.Count>0 && content.Controls[0].Tag is string && 
                (string)content.Controls[0].Tag == "Dashboard")ShowDashboard();
            };
            FormClosing+=(s, e) => 
            {
                if (loggedOut || logoutRequested)
                {
                    return;
                }
                if (forceLogout)
                {
                    logoutRequested = true;
                    return;
                }
                if (!TryAuthorizeLogout())e.Cancel = true;
            };
            FormClosed+=(s, e) => 
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
                if (!loggedOut && user != null)
                {
                    try
                    {
                        account.Logout(user.UserId);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Audit logout: "+ex.Message);
                    }
                }
                loggedOut = true;
                permissions = null;
                user = null;
                content.Controls.Clear();
                side.Controls.Clear();
            };
        }
        void AddMenu(string title, Action show)
        {
            // Nút menu dùng màu nền sáng và chữ dễ đọc.
            int position = side.Controls.Count - 1;
            // Trừ dòng tiêu đề sidebar.
            var button = Theme.Button(title, 12, 67 + position * 46, 226);
            button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            button.BackColor = Theme.Panel;
            button.ForeColor = Theme.Text;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.FlatAppearance.BorderColor = Color.FromArgb(220, 229, 238);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(231, 240, 250);
            side.Controls.Add(button);
            button.Click += (s, e) => show();
        }
        // Chỉ hiển thị menu khi nhóm của tài khoản có quyền Xem.
        // Service vẫn kiểm tra quyền khi ghi dữ liệu, dù nút đã được ẩn ở giao diện.
        void BuildMenu()
        {
            var grants = account.Permissions(user.UserId);
            permissions = grants;
            side.Controls.Clear();
            var title = Theme.Label("AUCTION ARENA", 16, 15, 224);
            title.Height = 42;
            title.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            title.ForeColor = Theme.Primary;
            side.Controls.Add(title);
            AddMenu("Tổng quan", ShowDashboard);
            AddMenu("Đổi mật khẩu", () => 
            {
                using (var form = new PasswordForm(user.UserId))
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    forceLogout = true; Close(); return;
                }
            });
            if (grants.ContainsKey("USERS") && grants["USERS"].View)
            AddMenu("Quản lý người dùng", () => Open(new UsersForm(user.UserId, grants["USERS"])));
            if (grants.ContainsKey("ROLES") && grants["ROLES"].View)
            AddMenu("Quản lý nhóm quyền", () => Open(new RolesForm(user.UserId, grants["ROLES"])));
            if (grants.ContainsKey("CUSTOMERS") && grants["CUSTOMERS"].View)
            AddMenu("Khách hàng", () => Open(new CustomersForm(user.UserId, grants["CUSTOMERS"])));
            if (grants.ContainsKey("CONSIGNORS") && grants["CONSIGNORS"].View)
            AddMenu("Chủ hàng", () => Open(new ConsignorsForm(user.UserId, grants["CONSIGNORS"])));
            if (grants.ContainsKey("CATEGORIES") && grants["CATEGORIES"].View)
            AddMenu("Loại hiện vật", () => Open(new CategoriesForm(user.UserId, grants["CATEGORIES"])));
            if (grants.ContainsKey("ITEMS") && grants["ITEMS"].View)
            AddMenu("Hiện vật", () => Open(new ItemsForm(user.UserId, grants["ITEMS"])));
            if (grants.ContainsKey("CONTRACTS") && grants["CONTRACTS"].View)
            AddMenu("Hợp đồng ký gửi", () => Open(new ContractsForm(user.UserId, grants["CONTRACTS"])));
            if (grants.ContainsKey("STOCK_RECEIPTS") && grants["STOCK_RECEIPTS"].View)
            AddMenu("Phiếu nhập kho", () => Open(new StockReceiptsForm(user.UserId, grants["STOCK_RECEIPTS"])));
            if (grants.ContainsKey("STOCK_RECEIPTS") && grants["STOCK_RECEIPTS"].View)
            AddMenu("Tồn kho", () => Open(new InventoryForm(user.UserId)));
            if (grants.ContainsKey("STOCK_ISSUES") && grants["STOCK_ISSUES"].View)
            AddMenu("Trả hiện vật cho chủ hàng", () => Open(new ConsignorReturnsForm(user.UserId, grants["STOCK_ISSUES"])));
            // Quyền được kiểm tra lại ở từng Service khi thao tác.
            if (grants.ContainsKey("SESSIONS") && grants["SESSIONS"].View)
            {
                var lots = grants.ContainsKey("LOTS") ? grants["LOTS"] : new PermissionFlags();
                var registrations = grants.ContainsKey("REGISTRATIONS") ? grants["REGISTRATIONS"] : new PermissionFlags();
                var bids = grants.ContainsKey("BIDS") ? grants["BIDS"] : new PermissionFlags();
                AddMenu("Phiên / Phòng đấu giá", () => Open(new AuctionNavigationForm(user.UserId, grants["SESSIONS"], lots, registrations, bids)));
                AddMenu("Quản lý phiên", () => Open(new AuctionSessionsForm(user.UserId, grants["SESSIONS"])));
            }
            if (grants.ContainsKey("BIDS") && grants["BIDS"].View)
            AddMenu("Lịch sử trả giá", () => Open(new BidHistoryForm(user.UserId, grants["BIDS"])));
            if (grants.ContainsKey("INVOICES") && grants["INVOICES"].View)
            {
                AddMenu("Hóa đơn thanh toán", () => Open(new FinanceInvoicesForm(user.UserId, grants["INVOICES"])));
                bool canReviewBids = grants.ContainsKey("BIDS") && grants["BIDS"].View;
                AddMenu("Đối soát sau phiên", () => Open(new SettlementDeskForm(user.UserId, canReviewBids)));
            }
            if (grants.ContainsKey("STOCK_ISSUES") && grants["STOCK_ISSUES"].View)
            AddMenu("Xuất kho bàn giao", () => Open(new FinanceIssuesForm(user.UserId, grants["STOCK_ISSUES"])));
            if (grants.ContainsKey("PAYOUTS") && grants["PAYOUTS"].View)
            AddMenu("Chi trả chủ hàng", () => Open(new FinancePayoutsForm(user.UserId, grants["PAYOUTS"])));
            if (grants.ContainsKey("REPORTS") && grants["REPORTS"].View)
            AddMenu("Báo cáo doanh thu", () => Open(new ReportNavigationForm(user.UserId)));
            AddMenu("Đăng xuất", () => 
            {
                if (TryAuthorizeLogout())Close();
            });
        }
        // Trả kết quả true khi người dùng đã xác nhận; không khóa chức năng đăng xuất khi DB đang lỗi.
        bool TryAuthorizeLogout()
        {
            if (loggedOut || logoutRequested)
            {
                return true;
            }
            if (Application.OpenForms.Cast<Form>().Any(x => x != this && x.Visible && x != Owner))
            {
                MessageBox.Show("Hãy lưu hoặc hủy thay đổi và đóng cửa sổ đang thao tác trước khi đăng xuất.", 
                "Công việc chưa đóng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            int openLotCount = 0;
            try
            {
                if (Allowed(permissions, "BIDS") || Allowed(permissions, "SESSIONS"))
                openLotCount = new LogoutStatusService().OpenLots(user.UserId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Logout precheck: "+ex.Message);
            }
            var warning = openLotCount>0?
            "Hệ thống đang có "+openLotCount+" lô đang đấu giá. Hãy bàn giao phiên cho nhân viên khác nếu bạn đang điều hành.\n\n":"";
            if (MessageBox.Show(warning+"Bạn có muốn đăng xuất và trở về màn hình đăng nhập?", 
            "Xác nhận đăng xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return false;
            logoutRequested = true;
            refreshTimer.Stop();
            return true;
        }
        void Open(Form form)
        {
            refreshTimer.Stop();
            using (form)form.ShowDialog(this);
            if (!IsDisposed)
            {
                BuildMenu();
                ShowDashboard();
                refreshTimer.Start();
            }
        }
        // Tải số liệu tổng quan theo quyền truy cập và kỳ báo cáo.
        void ShowDashboard()
        {
            content.Controls.Clear();
            var list = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, 
                FlowDirection = FlowDirection.TopDown, Padding = new Padding(20, 22, 20, 22), BackColor = Theme.Bg, Tag = "Dashboard"
            };
            content.Controls.Add(list);
            var header = new Label
            {
                Height = 65, ForeColor = Theme.Text, Font = new Font("Segoe UI", 17, FontStyle.Bold), 
                TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 0, 10)
            };
            if (!welcomed)
            {
                header.Text = "Xin chào, "+user.FullName+" ("+account.OwnRoleName(user.UserId)+")";
                welcomed = true;
            }
            else header.Text = "TỔNG QUAN • "+account.OwnRoleName(user.UserId);
            list.Controls.Add(header);
            var options = new ComboBox
            {
                Width = 280, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Theme.Panel, ForeColor = Theme.Text, Margin = new Padding(0, 0, 0, 10)
            };
            options.Items.AddRange(new object[]
            {
                "Tháng này", "Quý này", "Năm nay"
            });
            options.SelectedIndex = periodIndex;
            options.SelectedIndexChanged+=(sender, args) => 
            {
                periodIndex = options.SelectedIndex;
                ShowDashboard();
            };
            list.Controls.Add(options);
            var refresh = Theme.Button("Làm mới số liệu", 0, 0, 190);
            refresh.Margin = new Padding(0, 0, 0, 10);
            refresh.Click+=(sender, args) => ShowDashboard();
            list.Controls.Add(refresh);
            var today = DateTime.Today;
            var begin = periodIndex == 0?new DateTime(today.Year, today.Month, 1):
            periodIndex == 1?new DateTime(today.Year, ((today.Month-1)/3)*3+1, 1):new DateTime(today.Year, 1, 1);
            var end = periodIndex == 0?begin.AddMonths(1):periodIndex == 1?begin.AddMonths(3):begin.AddYears(1);
            var cards = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false, 
                Height = 385, BackColor = Theme.Bg, Margin = new Padding(0, 0, 0, 12)
            };
            list.Controls.Add(cards);
            TabControl charts = null;
            FlowLayoutPanel recent = null, upcoming = null, tasks = null;
            Theme.Safe(() => 
            {
                var report = new DashboardPeriodService();
                var metric = report.Load(user.UserId, begin, end);
                var grants = permissions;
                var rows = new System.Collections.Generic.List<string>();
                if (Allowed(grants, "SESSIONS"))rows.Add("Phiên đang mở: "+metric.OpenSessions+"  •  Sắp diễn ra: "+metric.UpcomingSessions);
                if (Allowed(grants, "ITEMS"))rows.Add("Hiện vật Trong kho: "+metric.InStock);
                if (Allowed(grants, "LOTS"))rows.Add("Lot đã bán trong kỳ: "+metric.SoldLots);
                if (Allowed(grants, "REPORTS"))
                {
                    var previous = metric.PreviousRevenue;
                    var percent = previous == 0m?"Chưa có kỳ trước":((metric.Revenue-previous)/previous*100m).ToString("+0.##;-0.##;0")+"%";
                    rows.Add("Phí mua đã thu trong kỳ (không VAT): "+metric.Revenue.ToString("N0")+" đ  •  So với kỳ trước: "+percent);
                }
                if (Allowed(grants, "INVOICES"))rows.Add("Hóa đơn chưa thanh toán trong kỳ: "+metric.UnpaidInvoices+
                "  •  Công nợ: "+metric.UnpaidAmount.ToString("N0")+" đ");
                if (Allowed(grants, "PAYOUTS"))rows.Add("Lệnh chi chủ hàng chờ thực hiện: "+metric.PendingSellerPayout.ToString("N0")+" đ");
                if (Allowed(grants, "CUSTOMERS"))rows.Add("Khách mới trong kỳ: "+metric.NewCustomers);
                foreach (var line in rows)cards.Controls.Add(new Label
                {
                    Text = line, Height = 48, ForeColor = Theme.Text, BackColor = Theme.Panel, 
                    Font = new Font("Segoe UI", 11), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 10, 0), 
                    Margin = new Padding(0, 0, 0, 4)
                });
                cards.Height = Math.Max(60, rows.Count*53+8);
                if (Allowed(grants, "REPORTS"))
                {
                    charts = new TabControl
                    {
                        Height = 575, Margin = new Padding(0, 0, 0, 15)
                    };
                    var tab = new TabPage("Doanh thu 12 tháng / Loại hiện vật / Top 5 lot")
                    {
                        BackColor = Theme.Bg
                    };
                    charts.TabPages.Add(tab); tab.Controls.Add(new DashboardChartsControl(new DashboardChartsService().Load(user.UserId)));
                    list.Controls.Add(charts);
                }
                if (Allowed(grants, "SESSIONS"))
                {
                    upcoming = new FlowLayoutPanel
                    {
                        Height = 290, WrapContents = false, FlowDirection = FlowDirection.TopDown, 
                        BackColor = Theme.Panel, Margin = new Padding(0, 0, 0, 12)
                    };
                    upcoming.Controls.Add(new Label
                    {
                        Text = "PHIÊN SẮP DIỄN RA", Height = 32, ForeColor = Theme.Accent, 
                        Font = new Font("Segoe UI", 12, FontStyle.Bold)
                    });
                    var sessions = report.Upcoming(user.UserId);
                    if (sessions.Count == 0)upcoming.Controls.Add(new Label
                    {
                        Text = "Chưa có dữ liệu", ForeColor = Theme.Muted, Height = 30
                    });
                    foreach (var ss in sessions)
                    {
                        var item = ss; var btn = Theme.Button(item.StartsAt.ToString("dd/MM HH:mm")+" | "+item.SessionCode+" | "+item.SessionName, 0, 0, 675);
                        btn.BackColor = Theme.Panel; btn.ForeColor = Theme.Text;
                        btn.Click+=(sender, args) => Open(new AuctionSessionsForm(user.UserId, grants["SESSIONS"]));
                        upcoming.Controls.Add(btn);
                    }
                    list.Controls.Add(upcoming);
                }
                tasks = new FlowLayoutPanel
                {
                    Height = 200, WrapContents = false, FlowDirection = FlowDirection.TopDown, 
                    BackColor = Theme.Panel, Margin = new Padding(0, 0, 0, 12)
                };
                tasks.Controls.Add(new Label
                {
                    Text = "VIỆC CẦN LÀM", Height = 32, ForeColor = Theme.Accent, Font = new Font("Segoe UI", 12, FontStyle.Bold)
                });
                var pending = report.Tasks(user.UserId);
                foreach (var t in pending)
                {
                    var task = t;
                    var btn = Theme.Button(task.Group+": "+task.Count, 0, 0, 675);
                    btn.BackColor = Theme.Panel; btn.ForeColor = Theme.Text;
                    btn.Click+=(sender, args) => 
                    {
                        if (task.Group.Contains("thẩm định") && Allowed(grants, "ITEMS"))Open(new ItemsForm(user.UserId, grants["ITEMS"]));
                        else if (task.Group.Contains("Hóa đơn") && Allowed(grants, "INVOICES"))Open(new FinanceInvoicesForm(user.UserId, grants["INVOICES"]));
                        else if (Allowed(grants, "PAYOUTS"))Open(new FinancePayoutsForm(user.UserId, grants["PAYOUTS"]));
                    }; tasks.Controls.Add(btn);
                }
                if (pending.Count == 0)tasks.Controls.Add(new Label
                {
                    Text = "Chưa có dữ liệu", ForeColor = Theme.Muted, Height = 30
                });
                list.Controls.Add(tasks);
                recent = new FlowLayoutPanel
                {
                    Height = 390, FlowDirection = FlowDirection.TopDown, WrapContents = false, 
                    Margin = new Padding(0, 0, 0, 15), BackColor = Theme.Panel
                };
                recent.Controls.Add(new Label
                {
                    Text = "10 HOẠT ĐỘNG GẦN ĐÂY", Height = 35, ForeColor = Theme.Accent, 
                    Font = new Font("Segoe UI", 12, FontStyle.Bold)
                });
                var logs = new DashboardActivityService().MyLatest(user.UserId);
                if (logs.Count == 0)recent.Controls.Add(new Label
                {
                    Text = "Chưa có dữ liệu", Height = 30, ForeColor = Theme.Muted
                });
                foreach (var log in logs)recent.Controls.Add(new Label
                {
                    Text = log.At.ToString("dd/MM HH:mm")+" • "+log.Action+" • "+log.Description, 
                    Height = 29, ForeColor = Theme.Text, AutoEllipsis = true
                });
                list.Controls.Add(recent);
            });
            Action fit = () => 
            {
                if (list.IsDisposed)
                {
                    return;
                }
                int w = Math.Max(510, list.ClientSize.Width-list.Padding.Horizontal-30);
                header.Width = w;
                cards.Width = w;
                foreach (Control c in cards.Controls)c.Width = w-12;
                if (charts != null)charts.Width = w;
                foreach (var panel in new[]
                {
                    upcoming, tasks, recent
                })if (panel != null)
                {
                    panel.Width = w;
                    foreach (Control c in panel.Controls)c.Width = w-25;
                }
            };
            list.Resize+=(sender, args) => fit();
            fit();
        }
        bool Allowed(System.Collections.Generic.Dictionary<string, PermissionFlags> flags, string key)
        {
            return flags.ContainsKey(key) && flags[key].View;
        }
    }
}
