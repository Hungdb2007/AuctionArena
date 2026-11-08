using System;
using System.Drawing;
using System.Windows.Forms;
using AuctionArena.Services;

namespace AuctionArena.WinForms
{
    public sealed partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            Theme.Apply(this);

            lblTieuDe.ForeColor = Theme.Primary;
            btnDangNhap.BackColor = Theme.Primary;
            btnDangNhap.ForeColor = Color.White;
            ActiveControl = txtTaiKhoan;
        }

        private void BtnDangNhap_Click(object sender, EventArgs e)
        {
            btnDangNhap.Enabled = false;
            try
            {
                DangNhap();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Đăng nhập không thành công", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnDangNhap.Enabled = true;
            }
        }

        private void DangNhap()
        {
            var account = new AccountService();
            var user = account.Login(txtTaiKhoan.Text, txtMatKhau.Text);

            // Mật khẩu do quản trị viên cấp lại phải được đổi trước khi sử dụng.
            if (user.MustChangePassword)
            {
                MessageBox.Show("Bạn cần đổi mật khẩu tạm trước khi sử dụng ứng dụng.");
                using (var form = new PasswordForm(user.UserId))
                {
                    form.ShowDialog(this);
                }
                txtMatKhau.Clear();
                txtTaiKhoan.Focus();
                return;
            }

            Hide();
            try
            {
                using (var main = new MainForm(user))
                {
                    main.ShowDialog(this);
                }
            }
            finally
            {
                txtMatKhau.Clear();
                Show();
                txtTaiKhoan.Focus();
            }
        }
    }
}
