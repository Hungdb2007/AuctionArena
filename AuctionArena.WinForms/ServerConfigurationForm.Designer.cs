namespace AuctionArena.WinForms
{
    partial class ServerConfigurationForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.Label lblDatabase;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox server;
        private System.Windows.Forms.TextBox database;
        private System.Windows.Forms.CheckBox integrated;
        private System.Windows.Forms.TextBox username;
        private System.Windows.Forms.TextBox password;
        private System.Windows.Forms.Button btnSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblServer = new System.Windows.Forms.Label();
            this.lblDatabase = new System.Windows.Forms.Label();
            this.lblUser = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.server = new System.Windows.Forms.TextBox();
            this.database = new System.Windows.Forms.TextBox();
            this.integrated = new System.Windows.Forms.CheckBox();
            this.username = new System.Windows.Forms.TextBox();
            this.password = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.SuspendLayout();

            this.lblServer.Text = "Tên Server";
            this.lblServer.Location = new System.Drawing.Point(25, 38);
            this.lblServer.Size = new System.Drawing.Size(140, 27);
            this.server.Name = "server";
            this.server.Location = new System.Drawing.Point(180, 35);
            this.server.Size = new System.Drawing.Size(350, 25);
            this.server.TabIndex = 0;

            this.lblDatabase.Text = "Cơ sở dữ liệu";
            this.lblDatabase.Location = new System.Drawing.Point(25, 88);
            this.lblDatabase.Size = new System.Drawing.Size(140, 27);
            this.database.Name = "database";
            this.database.Location = new System.Drawing.Point(180, 85);
            this.database.Size = new System.Drawing.Size(350, 25);
            this.database.TabIndex = 1;

            this.integrated.Text = "Xác thực Windows";
            this.integrated.Location = new System.Drawing.Point(180, 135);
            this.integrated.Size = new System.Drawing.Size(300, 26);
            this.integrated.Checked = true;
            this.integrated.Name = "integrated";
            this.integrated.TabIndex = 2;
            this.integrated.CheckedChanged += new System.EventHandler(this.Integrated_CheckedChanged);

            this.lblUser.Text = "Tài khoản SQL";
            this.lblUser.Location = new System.Drawing.Point(25, 188);
            this.lblUser.Size = new System.Drawing.Size(140, 27);
            this.username.Name = "username";
            this.username.Location = new System.Drawing.Point(180, 185);
            this.username.Size = new System.Drawing.Size(350, 25);
            this.username.TabIndex = 3;

            this.lblPassword.Text = "Mật khẩu SQL";
            this.lblPassword.Location = new System.Drawing.Point(25, 238);
            this.lblPassword.Size = new System.Drawing.Size(140, 27);
            this.password.Name = "password";
            this.password.Location = new System.Drawing.Point(180, 235);
            this.password.Size = new System.Drawing.Size(350, 25);
            this.password.TabIndex = 4;
            this.password.UseSystemPasswordChar = true;

            this.btnSave.Text = "Kiểm tra và lưu";
            this.btnSave.Name = "btnSave";
            this.btnSave.Location = new System.Drawing.Point(180, 285);
            this.btnSave.Size = new System.Drawing.Size(210, 38);
            this.btnSave.TabIndex = 5;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.AcceptButton = this.btnSave;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 341);
            this.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                this.lblServer, this.server, this.lblDatabase, this.database,
                this.integrated, this.lblUser, this.username,
                this.lblPassword, this.password, this.btnSave
            });
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "ServerConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cấu hình kết nối SQL Server";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
