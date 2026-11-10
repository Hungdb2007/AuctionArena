using AuctionArena.Data.Models;
using AuctionArena.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace AuctionArena.WinForms
{
    public sealed class BidHistoryForm : AuctionFormBase
    {
        readonly ComboBox sessions, lots, customers;
        readonly DateTimePicker from, to;
        readonly CheckBox useDate;
        readonly Timer autoRefresh = new Timer
        {
            Interval = 2000
        };
        readonly Label summary;
        readonly CheckBox auto;
        public BidHistoryForm(int actor, PermissionFlags flags):base("Lịch sử trả giá – chỉ đọc", actor, flags)
        {
            Height = 760;
            sessions = new ComboBox
            {
                Left = 20, Top = 470, Width = 295, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id"
            };
            lots = new ComboBox
            {
                Left = 330, Top = 470, Width = 265, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id"
            };
            customers = new ComboBox
            {
                Left = 610, Top = 470, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id"
            };
            Controls.Add(Theme.Label("Phiên", 20, 440));
            Controls.Add(Theme.Label("Lot", 330, 440));
            Controls.Add(Theme.Label("Khách hàng", 610, 440));
            Controls.Add(sessions);
            Controls.Add(lots);
            Controls.Add(customers);
            useDate = new CheckBox
            {
                Text = "Lọc theo ngày", Left = 20, Top = 540, Width = 160, ForeColor = Theme.Text
            };
            Controls.Add(useDate);
            from = new DateTimePicker
            {
                Left = 210, Top = 536, Width = 190, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1)
            };
            to = new DateTimePicker
            {
                Left = 425, Top = 536, Width = 190, Format = DateTimePickerFormat.Short
            };
            Controls.Add(from);
            Controls.Add(to);
            Action("Xem lịch sử", 650, 531, Flags.View, Reload);
            auto = new CheckBox
            {
                Text = "Tự làm mới 2 giây", Left = 20, Top = 598, Width = 210, 
                ForeColor = Theme.Text, Checked = true
            };
            Controls.Add(auto);
            auto.CheckedChanged+=(s, e) =>  //Xử lý tương tác và cập nhật trạng thái/hiển thị trong khối sự kiện
            {
                if (auto.Checked)autoRefresh.Start();
                else autoRefresh.Stop();
            };
            autoRefresh.Tick+=(s, e) => 
            {
                if (!IsDisposed && Visible)Reload();
            };
            FormClosed+=(s, e) => //Dispose: Giải phóng component/tài nguyên khi đóng Form
            {
                autoRefresh.Stop();
                autoRefresh.Dispose();
            };
            summary = Theme.Label("Tổng lượt: 0", 250, 596, 420);
            Controls.Add(summary);
            var report = Action("In biên bản lot", 680, 592, Flags.Print, () => 
            {
                var id = Selected(lots);
                if (!id.HasValue)
                {
                    throw new BusinessException("Chọn một lot trước khi in biên bản trả giá.");
                }
                ReportForm.OpenBidTranscript(this, Actor, id.Value);
            });
            report.Width = 205;
            var graph = Action("Biểu đồ trả giá", 680, 640, Flags.View, ShowPriceChart);
            graph.Width = 205;
            var openCustomer = Action("Mở hồ sơ khách", 450, 640, Flags.View, () => 
            {
                var row = Grid.CurrentRow == null?null:Grid.CurrentRow.DataBoundItem as BidHistoryRow;
                if (row == null)
                {
                    throw new BusinessException("Chọn một lượt giá của khách cần xem.");
                }
                var rights = new AccountService().Permissions(Actor);
                PermissionFlags customerRight;
                if (!rights.TryGetValue("CUSTOMERS", out customerRight) || !customerRight.View)
                throw new BusinessException("Tài khoản không có quyền xem hồ sơ khách hàng.");
                using (var f = new CustomersForm(Actor, customerRight, row.CustomerName))f.ShowDialog(this);
            });
            openCustomer.Width = 215;
            Shown+=(s, e) => Theme.Safe(() => // Safe: Bắt lỗi thao tác giao diện và thông báo.; AddAll: Thêm lựa chọn tất cả vào danh sách lọc.; BidSessions: Lấy phiên dùng lọc lịch sử bid.
            {
                sessions.DataSource = AddAll(Manager.BidSessions(Actor));
                customers.DataSource = AddAll(Manager.BidCustomers(Actor));
                LoadLots(); autoRefresh.Start();
            });
            sessions.SelectedIndexChanged+=(s, e) => Theme.Safe(LoadLots); //Safe: Bắt lỗi thao tác giao diện và thông báo
        }
        static System.Collections.Generic.List<BidFilterOption> AddAll(System.Collections.Generic.List<BidFilterOption> source) //Thêm lựa chọn tất cả vào danh sách lọc
        {
            source.Insert(0, new BidFilterOption
            {
                Id = 0, Name = "Tất cả"
            });
            return source;
        }
        int? Selected(ComboBox box) //Đọc giá trị hiện được chọn.
        {
            var value = box.SelectedItem as BidFilterOption;
            return value == null || value.Id<=0?(int?)null:value.Id;
        }
        void LoadLots() //Nạp các lô theo phiên đã chọn
        {
            lots.DataSource = AddAll(Manager.BidLots(Actor, Selected(sessions)));
        }
        void ShowPriceChart() //Dựng biểu đồ diễn biến giá
        {
            var id = Selected(lots);
            if (!id.HasValue)
            {
                throw new BusinessException("Chọn một lot để xem biểu đồ.");
            }
            var points = Manager.SearchBidHistory(Actor, Selected(sessions), id, Selected(customers), 
            useDate.Checked?(DateTime?)from.Value.Date:null, useDate.Checked?(DateTime?)to.Value.Date:null)
            .OrderBy(x => x.Time).ThenBy(x => x.BidId).ToList();
            if (points.Count == 0)
            {
                throw new BusinessException("Lot chưa có lượt trả giá.");
            }
            using (var dialog = new Form
            {
                Text = "Diễn biến giá lot #"+id.Value, Width = 900, Height = 540, 
                StartPosition = FormStartPosition.CenterParent
            })
            {
                Theme.Apply(dialog);
                var chart = new System.Windows.Forms.DataVisualization.Charting.Chart
                {
                    Dock = DockStyle.Fill
                };
                chart.ChartAreas.Add(new System.Windows.Forms.DataVisualization.Charting.ChartArea("Giá đấu"));
                var series = new System.Windows.Forms.DataVisualization.Charting.Series("Giá trả")
                {
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line, 
                    BorderWidth = 3, Color = Color.FromArgb(69, 214, 232)
                };
                foreach (var bid in points)series.Points.AddXY(bid.Time.ToString("HH:mm:ss"), bid.Amount);
                chart.Series.Add(series);
                dialog.Controls.Add(chart);
                dialog.ShowDialog(this);
            }
        }
        protected override void Reload() //Tải lại dữ liệu và cập nhật lưới.
        {
            Theme.Safe(() => 
            {
                var rows = Manager.SearchBidHistory(Actor, Selected(sessions), Selected(lots), Selected(customers), 
                useDate.Checked?(DateTime?)from.Value.Date:null, useDate.Checked?(DateTime?)to.Value.Date:null);
                Grid.DataSource = rows;
                summary.Text = "Lượt trả giá: "+rows.Count+"  |  Cao nhất: "+
                (rows.Count>0?rows.Max(x => x.Amount).ToString("N0")+" đ":"—");
            });
        }
    }
}
