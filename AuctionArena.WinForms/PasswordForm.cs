using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;

namespace AuctionArena.WinForms
{
    public sealed partial class PasswordForm : Form
    {
        private int userId;

        // Constructor không có tham số giúp Visual Studio mở màn hình thiết kế.
        public PasswordForm()
        {
            InitializeComponent();
            Theme.Apply(this);
            btnLuu.BackColor = Theme.Primary;
            btnLuu.ForeColor = Color.White;
            txtCu.TextChanged += (sender, e) => errors.SetError(txtCu, "");
            txtXacNhan.TextChanged += (sender, e) => errors.SetError(txtXacNhan, "");
        }

        public PasswordForm(int userId) : this()
        {
            this.userId = userId;
            txtUsername.Text = new AccountService().OwnUsername(userId);
        }

        public bool Succeeded { get; private set; }

        private void TxtMoi_TextChanged(object sender, EventArgs e)
        {
            errors.SetError(txtMoi, "");
            CapNhatDoManh();
        }

        private void ShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtCu.UseSystemPasswordChar = !chkHienCu.Checked;
            txtMoi.UseSystemPasswordChar = !chkHienMoi.Checked;
            txtXacNhan.UseSystemPasswordChar = !chkHienXacNhan.Checked;
        }

        private void BtnHuy_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void CapNhatDoManh()
        {
            string password = txtMoi.Text;
            int score = 0;
            if (password.Length >= 8) score++;
            if (password.Any(char.IsLower)) score++;
            if (password.Any(char.IsUpper)) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(c => !char.IsLetterOrDigit(c))) score++;
            if (score <= 2)
            {
                lblDoManh.Text = "Độ mạnh: Yếu";
                lblDoManh.ForeColor = Color.Firebrick;
            }
            else if (score <= 4)
            {
                lblDoManh.Text = "Độ mạnh: Trung bình";
                lblDoManh.ForeColor = Color.DarkGoldenrod;
            }
            else
            {
                lblDoManh.Text = "Độ mạnh: Mạnh";
                lblDoManh.ForeColor = Color.ForestGreen;
            }
        }
        private void BaoLoi(TextBox input, string message)
        {
            errors.SetError(input, message);
            input.Focus();
        }
        private bool DuLieuHopLe()
        {
            errors.Clear();
            if (string.IsNullOrWhiteSpace(txtCu.Text))
            {
                BaoLoi(txtCu, "Nhập mật khẩu hiện tại.");
                return false;
            }
            if (!AccountService.StrongPassword(txtMoi.Text))
            {
                BaoLoi(txtMoi, "Ít nhất 8 ký tự, chữ hoa, chữ thường và số.");
                return false;
            }
            if (txtMoi.Text == txtCu.Text)
            {
                BaoLoi(txtMoi, "Mật khẩu mới phải khác mật khẩu hiện tại.");
                return false;
            }
            if (txtMoi.Text != txtXacNhan.Text)
            {
                BaoLoi(txtXacNhan, "Mật khẩu xác nhận chưa khớp.");
                return false;
            }
            return true;
        }
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!DuLieuHopLe())
            {
                return;
            }
            try
            {
                new AccountService().ChangePassword(userId, txtCu.Text, txtMoi.Text);
                Succeeded = true;
                DialogResult = DialogResult.OK;
                MessageBox.Show("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
                Close();
            }
            catch (Exception ex)
            {
                BaoLoi(txtCu, ex.Message);
                MessageBox.Show(ex.Message, "Đổi mật khẩu", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
