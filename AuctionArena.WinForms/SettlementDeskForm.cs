using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    // Màn đối soát sau phiên dành cho nhân viên sàn; không chỉnh sửa hóa đơn hay quyền mua.
    public sealed class SettlementDeskForm : Form
    {
        readonly int actor;
        readonly bool canReviewBids;
        readonly SettlementDeskService service = new SettlementDeskService();
        readonly ComboBox sessions = new ComboBox
        {
            Left = 20, Top = 54, Width = 490, DropDownStyle = ComboBoxStyle.DropDownList, 
            BackColor = Color.White, ForeColor = Color.Black
        };
        readonly DataGridView winners = Theme.Grid(20, 140, 1110, 235);
        readonly DataGridView lots = Theme.Grid(20, 422, 540, 220);
        readonly DataGridView candidates = Theme.Grid(578, 422, 552, 220);
        readonly Label summary = new Label
        {
            Left = 20, Top = 101, Width = 1100, Height = 30, 
            ForeColor = Theme.Accent, Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        readonly Label message = new Label
        {
            Left = 20, Top = 672, Width = 1110, Height = 52, 
            ForeColor = Theme.Muted
        };
        readonly Button review;
        bool initializing;
        public SettlementDeskForm(int userId, bool canReview)
        {
            actor = userId;
            canReviewBids = canReview;
            Text = "Đối soát kết quả đấu giá trực tiếp";
            Width = 1170;
            Height = 800;
            MinimumSize = new Size(1040, 720);
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Controls.Add(Theme.Label("Phiên đã kết thúc", 20, 20, 350));
            Controls.Add(sessions);
            var refresh = Theme.Button("Tải lại", 532, 49, 132);
            Controls.Add(refresh);
            refresh.Click+=(s, e) => LoadWinners();
            Controls.Add(summary);
            winners.Top = 146;
            winners.Height = 225;
            winners.Anchor = AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top;
            Controls.Add(winners);
            Controls.Add(Theme.Label("Lot khách đã thắng", 20, 387, 450));
            Controls.Add(Theme.Label("Giá kế tiếp (chỉ xem xét khi quá hạn)", 578, 387, 545));
            lots.Anchor = AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom;
            candidates.Anchor = AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top|AnchorStyles.Bottom;
            Controls.Add(lots);
            Controls.Add(candidates);
            review = Theme.Button("Tra giá kế tiếp", 790, 49, 175);
            review.Enabled = canReviewBids;
            Controls.Add(review);
            review.Click+=(s, e) => ReviewCandidates();
            var invoice = Theme.Button("Mở màn thu ngân", 978, 49, 162);
            Controls.Add(invoice);
            invoice.Click+=(s, e) => Theme.Safe(() => 
            {
                var rights = new AccountService().Permissions(actor);
                PermissionFlags flags;
                if (!rights.TryGetValue("INVOICES", out flags) || !flags.View)
                throw new BusinessException("Tài khoản không có quyền mở hóa đơn.");
                using (var form = new FinanceInvoicesForm(actor, flags)) form.ShowDialog(this);
                LoadWinners();
            });
            Controls.Add(message);
            message.Text = "Không tự động chuyển quyền mua hoặc điều chỉnh tiền cọc. " +
            "Ứng viên trả giá kế tiếp chỉ để quản lý kiểm tra.";
            sessions.SelectedIndexChanged+=(s, e) => 
            {
                if (!initializing)LoadWinners();
            };
            winners.SelectionChanged+=(s, e) => LoadWonLots();
            Shown+=(s, e) => LoadSessions();
        }
        private void LoadSessions()
        {
            Theme.Safe(() => 
            {
                initializing = true;
                var options = service.ClosedSessions(actor);
                options.Insert(0, new SettlementSessionOption
                {
                    SessionId = 0, Display = "Tất cả phiên đã kết thúc"
                });
                sessions.DisplayMember = "Display";
                sessions.ValueMember = "SessionId";
                sessions.DataSource = options;
                initializing = false;
                LoadWinners();
            });
        }
        private void LoadWinners()
        {
            Theme.Safe(() => 
            {
                var selected = sessions.SelectedItem as SettlementSessionOption;
                int? id = selected == null || selected.SessionId == 0 ? (int?)null : selected.SessionId;
                var data = service.Winners(actor, id);
                winners.DataSource = data;
                SetColumn(winners, "SessionId", "", false);
                SetColumn(winners, "CustomerId", "", false);
                SetColumn(winners, "InvoiceId", "", false);
                SetColumn(winners, "Session", "Phiên");
                SetColumn(winners, "CustomerCode", "Mã khách");
                SetColumn(winners, "Customer", "Khách hàng");
                SetColumn(winners, "Paddle", "Thẻ paddle");
                SetColumn(winners, "LotsWon", "Số lot thắng");
                SetColumn(winners, "HammerTotal", "Tổng giá búa");
                SetColumn(winners, "InvoiceCode", "Mã hóa đơn");
                SetColumn(winners, "PaymentStatus", "Trạng thái");
                SetColumn(winners, "Remaining", "Còn phải thu");
                SetColumn(winners, "DueAt", "Hạn thanh toán");
                MoneyColumn(winners, "HammerTotal"); MoneyColumn(winners, "Remaining");
                summary.Text = string.Format("{0} khách thắng  |  {1} chưa có hóa đơn  |  {2} hóa đơn có công nợ  |  Tổng công nợ: {3:N0} đ", 
                data.Count, data.Count(x => !x.InvoiceId.HasValue), 
                data.Count(x => x.InvoiceId.HasValue && x.Remaining>0), 
                data.Sum(x => x.Remaining));
                LoadWonLots();
            });
        }
        private void LoadWonLots()
        {
            if (winners.CurrentRow == null || !(winners.CurrentRow.DataBoundItem is SettlementWinnerRow))
            {
                lots.DataSource = null;
                candidates.DataSource = null;
                return;
            }
            Theme.Safe(() => 
            {
                var winner = (SettlementWinnerRow)winners.CurrentRow.DataBoundItem;
                lots.DataSource = service.WonLots(actor, winner.SessionId, winner.CustomerId);
                SetColumn(lots, "LotId", "Mã lot");
                SetColumn(lots, "LotNumber", "Số lot");
                SetColumn(lots, "ItemName", "Hiện vật");
                SetColumn(lots, "HammerPrice", "Giá búa");
                SetColumn(lots, "State", "Trạng thái");
                MoneyColumn(lots, "HammerPrice");
                candidates.DataSource = null;
                message.Text = winner.InvoiceId.HasValue
                ? "Đã có hóa đơn: "+winner.InvoiceCode+". Chọn lot và bấm Tra giá kế tiếp nếu hóa đơn quá hạn."
                : "Khách thắng chưa được lập hóa đơn. Chọn Mở màn thu ngân để xử lý.";
            });
        }
        private void ReviewCandidates()
        {
            Theme.Safe(() => 
            {
                if (!canReviewBids)
                {
                    throw new BusinessException("Bạn không có quyền xem lịch sử trả giá.");
                }
                if (lots.CurrentRow == null || !(lots.CurrentRow.DataBoundItem is SettlementLotRow))
                throw new BusinessException("Hãy chọn lot đã bán trước.");
                var lot = (SettlementLotRow)lots.CurrentRow.DataBoundItem;
                var data = service.ReviewNextBidders(actor, lot.LotId);
                candidates.DataSource = data;
                SetColumn(candidates, "Paddle", "Paddle");
                SetColumn(candidates, "Customer", "Khách");
                SetColumn(candidates, "BidAmount", "Giá trả");
                SetColumn(candidates, "BidAt", "Thời điểm");
                SetColumn(candidates, "Review", "Cảnh báo");
                MoneyColumn(candidates, "BidAmount");
                message.Text = data.Count == 0 ? "Không có lượt giá kế tiếp hợp lệ để đối chiếu." :
                "Danh sách chỉ có tính tham khảo. Chưa cho chuyển quyền mua vì cần đảo chứng từ và quyết toán cọc.";
            });
        }
        private static void SetColumn(DataGridView view, string property, string title, bool visible = true)
        {
            if (!view.Columns.Contains(property))
            {
                return;
            }
            view.Columns[property].Visible = visible;
            if (visible)view.Columns[property].HeaderText = title;
        }
        private static void MoneyColumn(DataGridView view, string property)
        {
            if (view.Columns.Contains(property))view.Columns[property].DefaultCellStyle.Format = "N0";
        }
    }
}
