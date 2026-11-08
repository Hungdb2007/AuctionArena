namespace AuctionArena.WinForms
{
    partial class FirstAdminForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTieuDe;
        private System.Windows.Forms.Label lblTaiKhoan;
        private System.Windows.Forms.Label lblHoTen;
        private System.Windows.Forms.Label lblMatKhau;
        private System.Windows.Forms.Label lblXacNhan;
        private System.Windows.Forms.TextBox txtTaiKhoan;
        private System.Windows.Forms.TextBox txtHoTen;
        private System.Windows.Forms.TextBox txtMatKhau;
        private System.Windows.Forms.TextBox txtXacNhan;
        private System.Windows.Forms.Button btnTao;
        private System.Windows.Forms.Button btnDong;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblTieuDe = new System.Windows.Forms.Label();
            this.lblTaiKhoan = new System.Windows.Forms.Label();
            this.lblHoTen = new System.Windows.Forms.Label();
            this.lblMatKhau = new System.Windows.Forms.Label();
            this.lblXacNhan = new System.Windows.Forms.Label();
            this.txtTaiKhoan = new System.Windows.Forms.TextBox();
            this.txtHoTen = new System.Windows.Forms.TextBox();
            this.txtMatKhau = new System.Windows.Forms.TextBox();
            this.txtXacNhan = new System.Windows.Forms.TextBox();
            this.btnTao = new System.Windows.Forms.Button();
            this.btnDong = new System.Windows.Forms.Button();
            this.SuspendLayout();

            this.lblTieuDe.Location = new System.Drawing.Point(25, 18);
            this.lblTieuDe.Size = new System.Drawing.Size(450, 30);
            this.lblTieuDe.Text = "TẠO TÀI KHOẢN QUẢN TRỊ ĐẦU TIÊN";
            this.lblTieuDe.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);

            this.lblTaiKhoan.Location = new System.Drawing.Point(25, 67);
            this.lblTaiKhoan.Size = new System.Drawing.Size(450, 27);
            this.lblTaiKhoan.Text = "Tên đăng nhập";
            this.txtTaiKhoan.Location = new System.Drawing.Point(25, 95);
            this.txtTaiKhoan.Name = "txtTaiKhoan";
            this.txtTaiKhoan.Size = new System.Drawing.Size(430, 25);
            this.txtTaiKhoan.MaxLength = 60;
            this.txtTaiKhoan.Text = "admin";
            this.txtTaiKhoan.TabIndex = 0;

            this.lblHoTen.Location = new System.Drawing.Point(25, 138);
            this.lblHoTen.Size = new System.Drawing.Size(450, 27);
            this.lblHoTen.Text = "Họ tên người quản trị";
            this.txtHoTen.Location = new System.Drawing.Point(25, 166);
            this.txtHoTen.Name = "txtHoTen";
            this.txtHoTen.Size = new System.Drawing.Size(430, 25);
            this.txtHoTen.MaxLength = 120;
            this.txtHoTen.TabIndex = 1;

            this.lblMatKhau.Location = new System.Drawing.Point(25, 209);
            this.lblMatKhau.Size = new System.Drawing.Size(450, 27);
            this.lblMatKhau.Text = "Mật khẩu";
            this.txtMatKhau.Location = new System.Drawing.Point(25, 237);
            this.txtMatKhau.Name = "txtMatKhau";
            this.txtMatKhau.Size = new System.Drawing.Size(430, 25);
            this.txtMatKhau.MaxLength = 128;
            this.txtMatKhau.UseSystemPasswordChar = true;
            this.txtMatKhau.TabIndex = 2;

            this.lblXacNhan.Location = new System.Drawing.Point(25, 280);
            this.lblXacNhan.Size = new System.Drawing.Size(450, 27);
            this.lblXacNhan.Text = "Nhập lại mật khẩu";
            this.txtXacNhan.Location = new System.Drawing.Point(25, 308);
            this.txtXacNhan.Name = "txtXacNhan";
            this.txtXacNhan.Size = new System.Drawing.Size(430, 25);
            this.txtXacNhan.MaxLength = 128;
            this.txtXacNhan.UseSystemPasswordChar = true;
            this.txtXacNhan.TabIndex = 3;

            this.btnTao.Location = new System.Drawing.Point(25, 370);
            this.btnTao.Name = "btnTao";
            this.btnTao.Size = new System.Drawing.Size(270, 38);
            this.btnTao.Text = "Tạo tài khoản quản trị";
            this.btnTao.TabIndex = 4;
            this.btnTao.Click += new System.EventHandler(this.BtnTao_Click);
            this.btnDong.Location = new System.Drawing.Point(320, 370);
            this.btnDong.Name = "btnDong";
            this.btnDong.Size = new System.Drawing.Size(135, 38);
            this.btnDong.Text = "Đóng";
            this.btnDong.TabIndex = 5;
            this.btnDong.Click += new System.EventHandler(this.BtnDong_Click);

            this.AcceptButton = this.btnTao;
            this.CancelButton = this.btnDong;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(494, 456);
            this.Controls.Add(this.lblTieuDe);
            this.Controls.Add(this.lblTaiKhoan);
            this.Controls.Add(this.txtTaiKhoan);
            this.Controls.Add(this.lblHoTen);
            this.Controls.Add(this.txtHoTen);
            this.Controls.Add(this.lblMatKhau);
            this.Controls.Add(this.txtMatKhau);
            this.Controls.Add(this.lblXacNhan);
            this.Controls.Add(this.txtXacNhan);
            this.Controls.Add(this.btnTao);
            this.Controls.Add(this.btnDong);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FirstAdminForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tạo quản trị viên đầu tiên";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
