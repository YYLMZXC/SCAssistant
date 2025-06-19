namespace SCAssistant
{
    partial class DownloadListForm
    {
       
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem openFolderMenuItem;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code


        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.listView1 = new System.Windows.Forms.ListView(); // ✅ 必须先初始化
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openFolderMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
        this.openFolderMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";

            // 
            // openFolderMenuItem
            // 
            this.openFolderMenuItem.Name = "openFolderMenuItem";
            this.openFolderMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openFolderMenuItem.Text = "打开文件夹";
            this.openFolderMenuItem.Click += new System.EventHandler(this.openFolderMenuItem_Click);

            // 
            // listView1
            // 
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.FullRowSelect = true;
            this.listView1.Location = new System.Drawing.Point(0, 0);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(600, 400);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.ContextMenuStrip = this.contextMenuStrip1; // ✅ 在 listView 初始化之后设置
            this.listView1.DoubleClick += new System.EventHandler(this.listView1_DoubleClick);

            // 
            // DownloadListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.listView1);
            this.Name = "DownloadListForm";
            this.Text = "下载文件列表";
            this.Load += new System.EventHandler(this.DownloadListForm_Load);
            this.ResumeLayout(false);
        }

        #endregion
    }
}