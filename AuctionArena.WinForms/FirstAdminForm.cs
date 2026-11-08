using System;
using System.Drawing;
using System.Windows.Forms;
using AuctionArena.Services;

namespace AuctionArena.WinForms
{
    public sealed partial class FirstAdminForm : Form
    {
        public FirstAdminForm()
        {
            InitializeComponent();
            Theme.Apply(this);
            btnTao.BackColor = Theme.Primary;
            btnTao.ForeColor = Color.White;
        }

        private void BtnTao_Click(object sender, EventArgs e)
        {
            Theme.Safe(() => 
            {
                if (txtMatKhau.Text != txtXacNhan.Text)
                {
                    throw new BusinessException("Mật khẩu xác nhận không khớp.");
                }

                // Service kiểm tra lại dữ liệu và lưu mật khẩu đã băm.
                var service = new InitialAdminService();
                service.Create(txtTaiKhoan.Text, txtHoTen.Text, txtMatKhau.Text);

                MessageBox.Show("Đã tạo Admin. Hãy đăng nhập bằng tài khoản vừa đặt.");
                DialogResult = DialogResult.OK;
                Close();
            });
        }

        private void BtnDong_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
