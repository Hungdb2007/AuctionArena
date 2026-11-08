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
    public sealed class AuctionNavigationForm : AuctionFormBase
    {
        readonly PermissionFlags lots, registrations, bids;
        public AuctionNavigationForm(int actor, PermissionFlags sessionPermission, PermissionFlags lotPermission, PermissionFlags registrationPermission, PermissionFlags bidPermission)
        :base("Chọn phiên đấu giá", actor, sessionPermission)
        {
            lots = lotPermission;
            registrations = registrationPermission;
            bids = bidPermission;
            Action("Thiết lập lot", 20, 460, lots.View, () => 
            {
                using (var f = new AuctionLotsForm(Actor, lots, SelectedId(Grid, "SessionId")))f.ShowDialog(this);
            });
            Action("Đăng ký / cọc", 215, 460, registrations.View, () => 
            {
                using (var f = new AuctionRegistrationsForm(Actor, registrations, SelectedId(Grid, "SessionId")))f.ShowDialog(this);
            });
            var lotChoice = new ComboBox
            {
                Left = 20, Top = 565, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, 
                DisplayMember = "LotNumber", ValueMember = "LotId"
            };
            Controls.Add(Theme.Label("Chọn lot trong phiên", 20, 535));
            Controls.Add(lotChoice);
            //Safe: Bắt lỗi thao tác giao diện và thông báo.; Lots: Lấy lô của phiên
            Grid.SelectionChanged+=(s, e) => Theme.Safe(() => 
            {
                if (Grid.CurrentRow == null || !lots.View)
                {
                    return;
                }
                var session = Grid.CurrentRow.DataBoundItem as AuctionSessionEntity;
                if (session != null)lotChoice.DataSource = Manager.Lots(Actor, session.SessionId);
            });
            Action("Vào phòng đấu", 355, 562, bids.View, () => 
            {
                if (lotChoice.SelectedValue == null)
                {
                    throw new BusinessException("Chưa chọn lot.");
                }
                using (var f = new AuctionRoomForm(Actor, Convert.ToInt32(lotChoice.SelectedValue), bids.Create, lots.Edit))f.ShowDialog(this);
            });
        }
        protected override void Reload() //Tải lại dữ liệu và cập nhật lưới
        {
            Theme.Safe(() => Grid.DataSource = Manager.Sessions(Actor, ""));
        }
    }
}
