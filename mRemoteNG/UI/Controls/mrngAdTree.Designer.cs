namespace mRemoteNG.UI.Controls
{
    partial class MrngAdTree
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tvActiveDirectory = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // tvActiveDirectory
            // 
            this.tvActiveDirectory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvActiveDirectory.Location = new System.Drawing.Point(0, 0);
            this.tvActiveDirectory.Name = "tvActiveDirectory";
            this.tvActiveDirectory.Size = new System.Drawing.Size(800, 450);
            this.tvActiveDirectory.TabIndex = 0;
            this.tvActiveDirectory.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.TvActiveDirectory_AfterExpand);
            this.tvActiveDirectory.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TvActiveDirectory_AfterSelect);
            //
            // AdTree
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tvActiveDirectory);
            this.Name = "AdTree";
            this.Size = new System.Drawing.Size(800, 450);
            this.Load += new System.EventHandler(this.AdTree_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView tvActiveDirectory;
    }
}