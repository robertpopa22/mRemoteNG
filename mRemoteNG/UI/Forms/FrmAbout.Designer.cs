namespace mRemoteNG.UI.Forms
{
    public partial class frmAbout
    {
        #region  Windows Form Designer generated code
        private void InitializeComponent()
        {
            pbLogo = new System.Windows.Forms.PictureBox();
            pnlBottom = new System.Windows.Forms.Panel();
            lblTitle = new Controls.MrngLabel();
            lblVersion = new Controls.MrngLabel();
            lblStats = new Controls.MrngLabel();
            lblStory = new Controls.MrngLabel();
            lblCopyright = new Controls.MrngLabel();
            lblLicense = new Controls.MrngLabel();
            lblForkHeader = new Controls.MrngLabel();
            llForkGitHub = new System.Windows.Forms.LinkLabel();
            llForkReleases = new System.Windows.Forms.LinkLabel();
            llForkChangelog = new System.Windows.Forms.LinkLabel();
            llDonate = new System.Windows.Forms.LinkLabel();
            lblMaintainedBy = new Controls.MrngLabel();
            lblMaintainer = new Controls.MrngLabel();
            llMaintainerWebsite = new System.Windows.Forms.LinkLabel();
            lblOriginalHeader = new Controls.MrngLabel();
            llLicense = new System.Windows.Forms.LinkLabel();
            llChangelog = new System.Windows.Forms.LinkLabel();
            llCredits = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)pbLogo).BeginInit();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            //
            // pbLogo
            //
            pbLogo.BackColor = System.Drawing.Color.FromArgb(52, 58, 64);
            pbLogo.BackgroundImage = Properties.Resources.Header_dark;
            pbLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            pbLogo.Dock = System.Windows.Forms.DockStyle.Top;
            pbLogo.Location = new System.Drawing.Point(0, 0);
            pbLogo.Name = "pbLogo";
            pbLogo.Size = new System.Drawing.Size(584, 120);
            pbLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            pbLogo.TabIndex = 1;
            pbLogo.TabStop = false;
            //
            // pnlBottom
            //
            pnlBottom.BackColor = System.Drawing.SystemColors.Control;
            pnlBottom.Controls.Add(lblTitle);
            pnlBottom.Controls.Add(lblVersion);
            pnlBottom.Controls.Add(lblStats);
            pnlBottom.Controls.Add(lblStory);
            pnlBottom.Controls.Add(lblCopyright);
            pnlBottom.Controls.Add(lblLicense);
            pnlBottom.Controls.Add(lblForkHeader);
            pnlBottom.Controls.Add(llForkGitHub);
            pnlBottom.Controls.Add(llForkReleases);
            pnlBottom.Controls.Add(llForkChangelog);
            pnlBottom.Controls.Add(llDonate);
            pnlBottom.Controls.Add(lblMaintainedBy);
            pnlBottom.Controls.Add(lblMaintainer);
            pnlBottom.Controls.Add(llMaintainerWebsite);
            pnlBottom.Controls.Add(lblOriginalHeader);
            pnlBottom.Controls.Add(llLicense);
            pnlBottom.Controls.Add(llChangelog);
            pnlBottom.Controls.Add(llCredits);
            pnlBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlBottom.ForeColor = System.Drawing.SystemColors.ControlText;
            pnlBottom.Location = new System.Drawing.Point(0, 120);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Size = new System.Drawing.Size(584, 380);
            pnlBottom.TabIndex = 1;
            //
            // lblTitle
            //
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.SystemColors.ControlText;
            lblTitle.Location = new System.Drawing.Point(6, 6);
            lblTitle.Name = "lblTitle";
            lblTitle.TabIndex = 0;
            lblTitle.Text = "mRemoteNG Community Edition";
            lblTitle.UseCompatibleTextRendering = true;
            //
            // lblVersion
            //
            lblVersion.AutoSize = true;
            lblVersion.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblVersion.ForeColor = System.Drawing.SystemColors.ControlText;
            lblVersion.Location = new System.Drawing.Point(6, 36);
            lblVersion.Name = "lblVersion";
            lblVersion.TabIndex = 1;
            lblVersion.Text = "Version";
            lblVersion.UseCompatibleTextRendering = true;
            //
            // lblStats
            //
            lblStats.AutoSize = true;
            lblStats.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            lblStats.ForeColor = System.Drawing.SystemColors.ControlText;
            lblStats.Location = new System.Drawing.Point(6, 58);
            lblStats.Name = "lblStats";
            lblStats.TabIndex = 2;
            lblStats.Text = "stats";
            lblStats.UseCompatibleTextRendering = true;
            //
            // lblStory
            //
            lblStory.AutoSize = false;
            lblStory.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblStory.ForeColor = System.Drawing.SystemColors.ControlText;
            lblStory.Location = new System.Drawing.Point(6, 84);
            lblStory.Name = "lblStory";
            lblStory.Size = new System.Drawing.Size(570, 92);
            lblStory.TabIndex = 3;
            lblStory.Text = "story";
            lblStory.UseCompatibleTextRendering = true;
            //
            // lblCopyright
            //
            lblCopyright.AutoSize = true;
            lblCopyright.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblCopyright.ForeColor = System.Drawing.SystemColors.ControlText;
            lblCopyright.Location = new System.Drawing.Point(6, 180);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.TabIndex = 4;
            lblCopyright.Text = "Copyright";
            lblCopyright.UseCompatibleTextRendering = true;
            //
            // lblLicense
            //
            lblLicense.AutoSize = true;
            lblLicense.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblLicense.ForeColor = System.Drawing.SystemColors.ControlText;
            lblLicense.Location = new System.Drawing.Point(6, 202);
            lblLicense.Name = "lblLicense";
            lblLicense.TabIndex = 5;
            lblLicense.Text = "License";
            lblLicense.UseCompatibleTextRendering = true;
            //
            // lblForkHeader
            //
            lblForkHeader.AutoSize = true;
            lblForkHeader.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            lblForkHeader.ForeColor = System.Drawing.SystemColors.ControlText;
            lblForkHeader.Location = new System.Drawing.Point(5, 240);
            lblForkHeader.Name = "lblForkHeader";
            lblForkHeader.TabIndex = 10;
            lblForkHeader.Text = "This Fork";
            //
            // llForkGitHub
            //
            llForkGitHub.AutoSize = true;
            llForkGitHub.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llForkGitHub.Location = new System.Drawing.Point(5, 261);
            llForkGitHub.Name = "llForkGitHub";
            llForkGitHub.TabIndex = 11;
            llForkGitHub.TabStop = true;
            llForkGitHub.Text = "GitHub Page";
            llForkGitHub.LinkClicked += llForkGitHub_LinkClicked;
            //
            // llForkReleases
            //
            llForkReleases.AutoSize = true;
            llForkReleases.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llForkReleases.Location = new System.Drawing.Point(95, 261);
            llForkReleases.Name = "llForkReleases";
            llForkReleases.TabIndex = 12;
            llForkReleases.TabStop = true;
            llForkReleases.Text = "Releases";
            llForkReleases.LinkClicked += llForkReleases_LinkClicked;
            //
            // llForkChangelog
            //
            llForkChangelog.AutoSize = true;
            llForkChangelog.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llForkChangelog.Location = new System.Drawing.Point(160, 261);
            llForkChangelog.Name = "llForkChangelog";
            llForkChangelog.TabIndex = 13;
            llForkChangelog.TabStop = true;
            llForkChangelog.Text = "Changelog";
            llForkChangelog.LinkClicked += llForkChangelog_LinkClicked;
            //
            // llDonate
            //
            llDonate.AutoSize = true;
            llDonate.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            llDonate.Location = new System.Drawing.Point(5, 283);
            llDonate.Name = "llDonate";
            llDonate.TabIndex = 14;
            llDonate.TabStop = true;
            llDonate.Text = "Support the Geseidl Association";
            llDonate.LinkClicked += llDonate_LinkClicked;
            //
            // lblMaintainedBy
            //
            lblMaintainedBy.AutoSize = true;
            lblMaintainedBy.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            lblMaintainedBy.ForeColor = System.Drawing.SystemColors.ControlText;
            lblMaintainedBy.Location = new System.Drawing.Point(5, 340);
            lblMaintainedBy.Name = "lblMaintainedBy";
            lblMaintainedBy.TabIndex = 15;
            lblMaintainedBy.Text = "Maintained by";
            //
            // lblMaintainer
            //
            lblMaintainer.AutoSize = true;
            lblMaintainer.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblMaintainer.ForeColor = System.Drawing.SystemColors.ControlText;
            lblMaintainer.Location = new System.Drawing.Point(110, 340);
            lblMaintainer.Name = "lblMaintainer";
            lblMaintainer.TabIndex = 16;
            lblMaintainer.Text = "Geseidl IT Solutions";
            //
            // llMaintainerWebsite
            //
            llMaintainerWebsite.AutoSize = true;
            llMaintainerWebsite.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llMaintainerWebsite.Location = new System.Drawing.Point(255, 340);
            llMaintainerWebsite.Name = "llMaintainerWebsite";
            llMaintainerWebsite.TabIndex = 17;
            llMaintainerWebsite.TabStop = true;
            llMaintainerWebsite.Text = "geseidl.ro/servicii-it";
            llMaintainerWebsite.LinkClicked += llMaintainerWebsite_LinkClicked;
            //
            // lblOriginalHeader
            //
            lblOriginalHeader.AutoSize = true;
            lblOriginalHeader.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            lblOriginalHeader.ForeColor = System.Drawing.SystemColors.ControlText;
            lblOriginalHeader.Location = new System.Drawing.Point(330, 240);
            lblOriginalHeader.Name = "lblOriginalHeader";
            lblOriginalHeader.TabIndex = 20;
            lblOriginalHeader.Text = "The Original Project";
            //
            // llLicense
            //
            llLicense.AutoSize = true;
            llLicense.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llLicense.Location = new System.Drawing.Point(330, 261);
            llLicense.Name = "llLicense";
            llLicense.TabIndex = 21;
            llLicense.TabStop = true;
            llLicense.Text = "License";
            llLicense.LinkClicked += llLicense_LinkClicked;
            //
            // llChangelog
            //
            llChangelog.AutoSize = true;
            llChangelog.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llChangelog.Location = new System.Drawing.Point(330, 283);
            llChangelog.Name = "llChangelog";
            llChangelog.TabIndex = 22;
            llChangelog.TabStop = true;
            llChangelog.Text = "Original Changelog";
            llChangelog.LinkClicked += llChangelog_LinkClicked;
            //
            // llCredits
            //
            llCredits.AutoSize = true;
            llCredits.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llCredits.Location = new System.Drawing.Point(330, 305);
            llCredits.Name = "llCredits";
            llCredits.TabIndex = 23;
            llCredits.TabStop = true;
            llCredits.Text = "Credits";
            llCredits.LinkClicked += llCredits_LinkClicked;
            //
            // frmAbout
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(584, 500);
            Controls.Add(pnlBottom);
            Controls.Add(pbLogo);
            Font = new System.Drawing.Font("Segoe UI", 8.25F);
            ForeColor = System.Drawing.SystemColors.ControlText;
            Name = "frmAbout";
            Text = "About";
            TabText = "About";
            ((System.ComponentModel.ISupportInitialize)pbLogo).EndInit();
            pnlBottom.ResumeLayout(false);
            pnlBottom.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        internal Controls.MrngLabel lblCopyright;
        internal Controls.MrngLabel lblTitle;
        internal Controls.MrngLabel lblVersion;
        internal Controls.MrngLabel lblStats;
        internal Controls.MrngLabel lblStory;
        internal Controls.MrngLabel lblLicense;
        internal System.Windows.Forms.Panel pnlBottom;
        internal System.Windows.Forms.PictureBox pbLogo;
        private System.Windows.Forms.LinkLabel llCredits;
        private System.Windows.Forms.LinkLabel llChangelog;
        private System.Windows.Forms.LinkLabel llLicense;
        private Controls.MrngLabel lblForkHeader;
        private System.Windows.Forms.LinkLabel llForkGitHub;
        private System.Windows.Forms.LinkLabel llForkReleases;
        private System.Windows.Forms.LinkLabel llForkChangelog;
        private System.Windows.Forms.LinkLabel llDonate;
        private Controls.MrngLabel lblMaintainedBy;
        private Controls.MrngLabel lblMaintainer;
        private System.Windows.Forms.LinkLabel llMaintainerWebsite;
        private Controls.MrngLabel lblOriginalHeader;
    }
}
