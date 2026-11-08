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
    public sealed class ProjectorRoomForm : Form
    {
        readonly AuctionManagementService manager = new AuctionManagementService();
        readonly int actor, lotId;
        readonly Label message = new Label
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Accent, 
            Font = new Font("Segoe UI", 25, FontStyle.Bold)
        };
        readonly Timer timer = new Timer
        {
            Interval = 2000
        };
        public ProjectorRoomForm(int actor, int lotId)
        {
            this.actor = actor;
            this.lotId = lotId;
            Text = "Bảng giá công khai · F11 toàn màn hình";
            Width = 1000;
            Height = 600;
            BackColor = Theme.Bg;
            Controls.Add(message);
            KeyPreview = true;
            KeyDown+=(s, e) => 
            {
                if (e.KeyCode == Keys.F11)
                {
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    WindowState = FormWindowState.Normal;
                    FormBorderStyle = FormBorderStyle.Sizable;
                }
            };
            timer.Tick+=(s, e) => RefreshBoard();
            Shown+=(s, e) => 
            {
                RefreshBoard();
                timer.Start();
            };
            FormClosed+=(s, e) => 
            {
                timer.Stop();
                timer.Dispose();
            };
        }
        void RefreshBoard()
        {
            try
            {
                var lot = manager.LotSnapshot(actor, lotId);
                var lead = lot.LeadingCustomerId.HasValue?manager.PaddlesForLot(actor, lotId).FirstOrDefault(x => x.CustomerId == lot.LeadingCustomerId.Value):null;
                var remaining = lot.ClosesAt-DateTime.Now;
                message.Text = "AUCTION ARENA\nLOT #"+lot.LotNumber+"\n\n"+lot.CurrentPrice.ToString("N0")+" đ\n\n"+
                "THẺ DẪN ĐẦU: "+(lead == null?"—":lead.PaddleNumber.ToString())+"\nBƯỚC GIÁ: "+lot.BidIncrement.ToString("N0")+
                " đ\nCÒN: "+(remaining.TotalSeconds<=0?"Hết giờ":remaining.ToString(@"hh\:mm\:ss"));
            }
            catch (Exception ex)
            {
                timer.Stop();
                message.Text = "Không tải được bảng giá: "+ex.Message;
            }
        }
    }
}
