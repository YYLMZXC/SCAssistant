using System.Windows.Forms;

namespace SCAssistant
{
    partial class SetupForm
    {
        
        private System.ComponentModel.IContainer components = null;

      
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }




        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupForm));
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.metroLabel = new System.Windows.Forms.Label();
            this.metroLabelPath = new System.Windows.Forms.Label();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.nextButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.metroLabelProgress = new System.Windows.Forms.Label();
            this.metroLabelStatus = new System.Windows.Forms.Label();
            this.flowLayoutPanel.SuspendLayout();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Controls.Add(this.metroLabel);
            this.flowLayoutPanel.Controls.Add(this.metroLabelPath);
            this.flowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            this.flowLayoutPanel.Size = new System.Drawing.Size(600, 40);
            this.flowLayoutPanel.TabIndex = 0;
            // 
            // metroLabel
            // 
            this.metroLabel.Location = new System.Drawing.Point(3, 0);
            this.metroLabel.Name = "metroLabel";
            this.metroLabel.Size = new System.Drawing.Size(100, 20);
            this.metroLabel.TabIndex = 0;
            this.metroLabel.Text = "选择安装路径:";
            // 
            // metroLabelPath
            // 
            this.metroLabelPath.Location = new System.Drawing.Point(109, 0);
            this.metroLabelPath.Name = "metroLabelPath";
            this.metroLabelPath.Size = new System.Drawing.Size(400, 20);
            this.metroLabelPath.TabIndex = 1;
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.nextButton);
            this.panelButtons.Controls.Add(this.cancelButton);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(0, 350);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(600, 50);
            this.panelButtons.TabIndex = 1;
            // 
            // nextButton
            // 
            this.nextButton.Location = new System.Drawing.Point(450, 10);
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new System.Drawing.Size(75, 30);
            this.nextButton.TabIndex = 0;
            this.nextButton.Text = "下一步";
            this.nextButton.Click += new System.EventHandler(this.nextButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(535, 10);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 30);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "取消";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(10, 300);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(580, 23);
            this.progressBar.TabIndex = 2;
            // 
            // metroLabelProgress
            // 
            this.metroLabelProgress.Location = new System.Drawing.Point(10, 330);
            this.metroLabelProgress.Name = "metroLabelProgress";
            this.metroLabelProgress.Size = new System.Drawing.Size(580, 20);
            this.metroLabelProgress.TabIndex = 3;
            this.metroLabelProgress.Text = "准备安装...";
            // 
            // metroLabelStatus
            // 
            this.metroLabelStatus.Location = new System.Drawing.Point(10, 270);
            this.metroLabelStatus.Name = "metroLabelStatus";
            this.metroLabelStatus.Size = new System.Drawing.Size(580, 20);
            this.metroLabelStatus.TabIndex = 4;
            // 
            // SetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.flowLayoutPanel);
            this.Controls.Add(this.panelButtons);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.metroLabelProgress);
            this.Controls.Add(this.metroLabelStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SetupForm";
            this.Text = "生存战争助手 - 安装程序";
            this.Load += new System.EventHandler(this.SetupForm_Load);
            this.flowLayoutPanel.ResumeLayout(false);
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private FlowLayoutPanel flowLayoutPanel;
        private Label metroLabel;
        private Label metroLabelPath;
        private Panel panelButtons;
        private Button nextButton;
        private Button cancelButton;
        private ProgressBar progressBar;
        private Label metroLabelProgress;
        private Label metroLabelStatus;


    }
}