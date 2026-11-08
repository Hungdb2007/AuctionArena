using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
    public sealed class AuctionRoomForm : Form
    {
        readonly int actor, lotId;
        readonly bool canBid, canClose;
        readonly AuctionManagementService manager = new AuctionManagementService();
        readonly Label price, clock, leader, status, name;
        readonly DataGridView grid;
        readonly TextBox paddle, bid;
        readonly Button place, close;
        readonly Timer refreshTimer = new Timer();
        AuctionLotEntity latest;
        List<PaddleBidOption> currentPaddles = new List<PaddleBidOption>();
        bool loading;
        ProjectorRoomForm projectorWindow;
        public AuctionRoomForm(int user, int id, bool allowedBid, bool allowedClose)
        {
            actor = user;
            lotId = id;
            canBid = allowedBid;
            canClose = allowedClose;
            Text = "Điều hành đấu giá trực tiếp – Lot "+id;
            Width = 1030;
            Height = 760;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            price = Theme.Label("Giá hiện tại", 25, 25, 940);
            price.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            leader = Theme.Label("Paddle đang dẫn đầu", 25, 100, 450);
            clock = Theme.Label("Thời gian", 520, 100, 450);
            status = Theme.Label("Trạng thái lot", 25, 135, 850);
            Controls.Add(price);
            Controls.Add(leader);
            Controls.Add(clock);
            Controls.Add(status);
            grid = Theme.Grid(25, 170, 970, 310);
            grid.Anchor = AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            Controls.Add(grid);
            paddle = Theme.Input(25, 533, 130);
            bid = Theme.Input(405, 533, 190);
            name = Theme.Label("Nhập số thẻ để nhận diện khách", 175, 535, 215);
            Controls.Add(name);
            Controls.Add(Theme.Label("Paddle khách giơ", 25, 504, 170));
            Controls.Add(Theme.Label("Giá khách trả", 405, 504, 200));
            Controls.Add(paddle);
            Controls.Add(bid);
            paddle.TextChanged+=(s, e) => Recognize(); //Recognize: Hiển thị khách tương ứng số thẻ.
            var next = Theme.Button("Giá kế tiếp", 620, 529, 158);
            Controls.Add(next);
            //Safe: Bắt lỗi thao tác giao diện và thông báo.; ToString: Cung cấp nhãn hiển thị cho đối tượng lựa chọn
            next.Click += (s, e) => Theme.Safe(() => 
            {
                if (latest == null)
                throw new BusinessException("Chưa tải lot.");
                // Khi chưa có người dẫn đầu: mức trả đầu tiên là giá khởi điểm.
                // Nếu có người dẫn đầu: cộng thêm một bước giá.
                decimal giaTiepTheo = latest.LeadingCustomerId.HasValue
                ? latest.CurrentPrice + latest.BidIncrement
                : latest.StartingPrice;
                bid.Text = giaTiepTheo.ToString("0.##");
            });
            place = Theme.Button("Ghi nhận bid", 790, 529, 190);
            place.Enabled = canBid;
            Controls.Add(place);
            //Safe: Bắt lỗi thao tác giao diện và thông báo.; FindPaddle: Tìm khách từ số thẻ nhập.; ToString: Cung cấp nhãn hiển thị cho đối tượng lựa chọn.        
            place.Click+=(s, e) => Theme.Safe(() => 
            {
                var option = FindPaddle();
                if (option == null)
                {
                    throw new BusinessException("Paddle không hợp lệ hoặc chưa đóng đủ cọc.");
                }
                decimal amount; if (!decimal.TryParse(bid.Text, out amount) || amount<=0)throw new BusinessException("Giá trả không hợp lệ.");
                string msg = "Ghi bid lot #"+lotId+"\nThẻ: "+option.PaddleNumber+" – "+option.CustomerName+
                "\nGiá: "+amount.ToString("N0")+" đ?";
                if (MessageBox.Show(msg, "Xác nhận giá đấu trực tiếp", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
                new AuctionService().Bid(actor, lotId, option.CustomerId, amount);
                LoadSnapshot(); Recognize();
            });
            close = Theme.Button("CHỐT LOT", 790, 600, 190);
            close.Enabled = canClose;
            Controls.Add(close);
            close.Click+=(s, e) => Theme.Safe(() => 
            {
                //Safe: Bắt lỗi thao tác giao diện và thông báo.; LoadSnapshot: Tải trạng thái lô bất đồng bộ và cập nhật giao diện.; LotSnapshot: Đọc trạng thái / giá mới nhất của lô.
                // Không dựa vào LoadSnapshot() async (có thể đang tải hoặc chưa hoàn tất).
                // Lấy ảnh chụp DB mới nhất ngay trước khi hiển thị xác nhận.
                var snapshot = manager.LotSnapshot(actor, lotId);
                if (snapshot.LotStatus != "Open")
                {
                    throw new BusinessException("Lot không còn đang mở.");
                }
                var winner = snapshot.LeadingCustomerId.HasValue ? manager.PaddlesForLot(actor, lotId)
                .FirstOrDefault(x => x.CustomerId == snapshot.LeadingCustomerId.Value):null;
                string result = snapshot.CurrentPrice>=snapshot.ReservePrice && snapshot.LeadingCustomerId.HasValue ? "Đạt giá sàn" : "Không đạt giá sàn";
                string question = "Chốt lot #"+lotId+"\nGiá búa dự kiến: "+snapshot.CurrentPrice.ToString("N0")+" đ\n"+
                "Paddle dẫn đầu: "+(winner != null?winner.PaddleNumber.ToString():"Không có")+"\nKết quả: "+result+"?";
                if (MessageBox.Show(question, "Xác nhận chốt", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    new AuctionService().CloseLot(actor, lotId, snapshot.RowVersion); LoadSnapshot();
                }
            });
            var projector = Theme.Button("Mở màn chiếu (F9)", 25, 600, 200); 

            Controls.Add(projector);
            projector.Click+=(s, e) => OpenProjector();//OpenProjector: Mở hoặc đưa màn chiếu lên trước.
            KeyPreview = true;
            KeyDown+=(s, e) => //OpenProjector: Mở hoặc đưa màn chiếu lên trước.
            {
                if (e.KeyCode == Keys.F9)
                {
                    OpenProjector();
                    e.Handled = true;
                }
            };
            refreshTimer.Interval = 2000;
            refreshTimer.Tick+=(s, e) => LoadSnapshot(); //LoadSnapshot: Tải trạng thái lô bất đồng bộ và cập nhật giao diện
            Shown +=(s, e) => //LoadSnapshot: Tải trạng thái lô bất đồng bộ và cập nhật giao diện
            {
                LoadSnapshot();
                refreshTimer.Start();
            };
            FormClosed+=(s, e) => //Dispose: Giải phóng component/tài nguyên khi đóng Form
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
                if (projectorWindow != null && !projectorWindow.IsDisposed)projectorWindow.Close();
            };
        }
        PaddleBidOption FindPaddle() //Tìm khách từ số thẻ nhập.
        {
            int value;
            if (!int.TryParse(paddle.Text, out value) || value<=0)
            {
                return null;
            }
            return currentPaddles.FirstOrDefault(x => x.PaddleNumber == value);
        }
        void Recognize()  //Hiển thị khách tương ứng số thẻ
        {
            Theme.Safe(() => 
            {
                var option = FindPaddle(); name.Text = option == null?"Paddle chưa đủ điều kiện":option.CustomerName;
            });
        }
        async void LoadSnapshot() //Tải trạng thái lô bất đồng bộ và cập nhật giao diện(154-204)
        {
            if (loading || IsDisposed)
            {
                return;
            }
            loading = true;
            try
            {
                // Truy vấn 3 nguồn trên luồng phụ, không giữ DbContext chung giữa các luồng.
                var data = await Task.Run(() => 
                {
                    var lot = manager.LotSnapshot(actor, lotId);
                    var paddles = manager.PaddlesForLot(actor, lotId);
                    var bids = manager.BidHistory(actor, lotId);
                    return new
                    {
                        lot, paddles, bids
                    };
                });
                if (IsDisposed)
                {
                    return;
                }
                latest = data.lot;
                currentPaddles = data.paddles;
                price.Text = "Giá hiện tại: "+latest.CurrentPrice.ToString("N0")+" đ";
                var option = latest.LeadingCustomerId.HasValue?
                currentPaddles.FirstOrDefault(x => x.CustomerId == latest.LeadingCustomerId.Value):null;
                leader.Text = "Paddle dẫn đầu: "+(option == null?"Chưa có":option.PaddleNumber.ToString());
                status.Text = "Trạng thái: "+latest.LotStatus+" · Bước giá: "+latest.BidIncrement.ToString("N0")+" đ";
                var seconds = (int)Math.Ceiling((latest.ClosesAt-DateTime.Now).TotalSeconds);
                clock.Text = "Còn: "+(seconds<=0?"Đã hết giờ":TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss"));
                place.Enabled = canBid && latest.LotStatus == "Open" && seconds>0;
                close.Enabled = canClose && latest.LotStatus == "Open";
                grid.DataSource = data.bids;
                Recognize();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    refreshTimer.Stop();
                    MessageBox.Show(ex.Message, "Lỗi cập nhật phòng đấu");
                }
            }
            finally
            {
                loading = false;
            }
        }
        void OpenProjector() //Mở hoặc đưa màn chiếu lên trước.
        {
            if (projectorWindow != null && !projectorWindow.IsDisposed)
            {
                projectorWindow.Activate();
                return;
            }
            projectorWindow = new ProjectorRoomForm(actor, lotId);
            projectorWindow.FormClosed+=(s, e) => projectorWindow = null; //Xử lý tương tác và cập nhật trạng thái/hiển thị trong khối sự kiện.
            if (Screen.AllScreens.Length>1)
            {
                projectorWindow.StartPosition = FormStartPosition.Manual;
                projectorWindow.Bounds = Screen.AllScreens[1].WorkingArea;
            }
            // Màn chiếu mở không chặn thao tác điều hành trên cửa sổ chính.
            projectorWindow.Show();
        }
    }
    // Chỉ chiếu giá, số thẻ và thời gian; không hiển thị thông tin cá nhân.
}
