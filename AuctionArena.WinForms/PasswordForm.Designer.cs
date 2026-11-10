namespace AuctionArena.WinForms
{
    partial class PasswordForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblCu;
        private System.Windows.Forms.Label lblMoi;
        private System.Windows.Forms.Label lblXacNhan;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtCu;
        private System.Windows.Forms.TextBox txtMoi;
        private System.Windows.Forms.TextBox txtXacNhan;
        private System.Windows.Forms.CheckBox chkHienCu;
        private System.Windows.Forms.CheckBox chkHienMoi;
        private System.Windows.Forms.CheckBox chkHienXacNhan;
        private System.Windows.Forms.Label lblDoManh;
        private System.Windows.Forms.Button btnLuu;
        private System.Windows.Forms.Button btnHuy;
        private System.Windows.Forms.ErrorProvider errors;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.txtCu = new System.Windows.Forms.TextBox();
            this.txtMoi = new System.Windows.Forms.TextBox();
            this.txtXacNhan = new System.Windows.Forms.TextBox();
            this.chkHienCu = new System.Windows.Forms.CheckBox();
            this.chkHienMoi = new System.Windows.Forms.CheckBox();
            this.chkHienXacNhan = new System.Windows.Forms.CheckBox();
            this.lblDoManh = new System.Windows.Forms.Label();
            this.btnLuu = new System.Windows.Forms.Button();
            this.btnHuy = new System.Windows.Forms.Button();
            this.errors = new System.Windows.Forms.ErrorProvider(this.components);
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblCu = new System.Windows.Forms.Label();
            this.lblMoi = new System.Windows.Forms.Label();
            this.lblXacNhan = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.errors)).BeginInit();
            this.SuspendLayout();

            this.lblUsername.Text = "Tên đăng nhập (chỉ đọc)";
            this.lblUsername.Location = new System.Drawing.Point(25, 15);
            this.lblUsername.Size = new System.Drawing.Size(400, 27);
            this.txtUsername.Location = new System.Drawing.Point(25, 42);
            this.txtUsername.Size = new System.Drawing.Size(390, 25);
            this.txtUsername.ReadOnly = true;
            this.txtUsername.Name = "txtUsername";

            this.lblCu.Text = "Mật khẩu hiện tại";
            this.lblCu.Location = new System.Drawing.Point(25, 82);
            this.lblCu.Size = new System.Drawing.Size(330, 27);
            this.txtCu.Location = new System.Drawing.Point(25, 110);
            this.txtCu.Size = new System.Drawing.Size(330, 25);
            this.txtCu.UseSystemPasswordChar = true;
            this.txtCu.Name = "txtCu";
            this.chkHienCu.Text = "Hiện";
            this.chkHienCu.Location = new System.Drawing.Point(370, 108);
            this.chkHienCu.Size = new System.Drawing.Size(75, 28);
            this.chkHienCu.CheckedChanged += new System.EventHandler(this.ShowPassword_CheckedChanged);

            this.lblMoi.Text = "Mật khẩu mới";
            this.lblMoi.Location = new System.Drawing.Point(25, 157);
            this.lblMoi.Size = new System.Drawing.Size(330, 27);
            this.txtMoi.Location = new System.Drawing.Point(25, 185);
            this.txtMoi.Size = new System.Drawing.Size(330, 25);
            this.txtMoi.UseSystemPasswordChar = true;
            this.txtMoi.Name = "txtMoi";
            this.txtMoi.TextChanged += new System.EventHandler(this.TxtMoi_TextChanged);
            this.chkHienMoi.Text = "Hiện";
            this.chkHienMoi.Location = new System.Drawing.Point(370, 183);
            this.chkHienMoi.Size = new System.Drawing.Size(75, 28);
            this.chkHienMoi.CheckedChanged += new System.EventHandler(this.ShowPassword_CheckedChanged);

            this.lblXacNhan.Text = "Nhập lại mật khẩu mới";
            this.lblXacNhan.Location = new System.Drawing.Point(25, 233);
            this.lblXacNhan.Size = new System.Drawing.Size(330, 27);
            this.txtXacNhan.Location = new System.Drawing.Point(25, 261);
            this.txtXacNhan.Size = new System.Drawing.Size(330, 25);
            this.txtXacNhan.UseSystemPasswordChar = true;
            this.txtXacNhan.Name = "txtXacNhan";
            this.chkHienXacNhan.Text = "Hiện";
            this.chkHienXacNhan.Location = new System.Drawing.Point(370, 259);
            this.chkHienXacNhan.Size = new System.Drawing.Size(75, 28);
            this.chkHienXacNhan.CheckedChanged += new System.EventHandler(this.ShowPassword_CheckedChanged);

            this.lblDoManh.Text = "Độ mạnh: chưa nhập";
            this.lblDoManh.Location = new System.Drawing.Point(25, 307);
            this.lblDoManh.Size = new System.Drawing.Size(405, 27);
            this.btnLuu.Text = "Đổi mật khẩu";
            this.btnLuu.Location = new System.Drawing.Point(25, 350);
            this.btnLuu.Size = new System.Drawing.Size(205, 38);
            this.btnLuu.Click += new System.EventHandler(this.BtnSave_Click);
            this.btnHuy.Text = "Hủy";
            this.btnHuy.Location = new System.Drawing.Point(250, 350);
            this.btnHuy.Size = new System.Drawing.Size(165, 38);
            this.btnHuy.Click += new System.EventHandler(this.BtnHuy_Click);

            this.errors.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.errors.ContainerControl = this;
            this.AcceptButton = this.btnLuu;
            this.CancelButton = this.btnHuy;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(469, 466);
            this.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                this.lblUsername, this.txtUsername, this.lblCu, this.txtCu, this.chkHienCu, 
                this.lblMoi, this.txtMoi, this.chkHienMoi, 
                this.lblXacNhan, this.txtXacNhan, this.chkHienXacNhan, 
                this.lblDoManh, this.btnLuu, this.btnHuy
            });
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "PasswordForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Đổi mật khẩu";
            ((System.ComponentModel.ISupportInitialize)(this.errors)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
