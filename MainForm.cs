using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
namespace SCAssistant
{
    public partial class MainForm : Form
    {
        public ChromiumWebBrowser browser;
        public DownloadHandler downloadHandler;
        private DownloadListForm downloadListForm;

        public MainForm()
        {
            InitializeComponent();

            CefSettings settings = new CefSettings();
            Cef.Initialize(settings);

            browser = new ChromiumWebBrowser("https://test.suancaixianyu.cn/");
            browser.LifeSpanHandler = new CustomLifeSpanHandler();
            browser.MenuHandler = new CustomContextMenuHandler();

            this.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;

            downloadHandler = new DownloadHandler(this);
            browser.DownloadHandler = downloadHandler;

            // 订阅下载创建事件
            downloadHandler.DownloadCreated += OnDownloadCreated;
        }
        private void OnDownloadCreated(string fileName, string url)
        {
            string fullPath = ""; // 临时变量

            // 显示下载窗体
            if (downloadListForm == null || downloadListForm.IsDisposed)
            {
                this.Invoke(new Action(() =>
                {
                    downloadListForm = new DownloadListForm();
                    downloadListForm.Show();
                }));
            }

            if (!downloadListForm.IsHandleCreated)
            {
                var _ = downloadListForm.Handle;
            }

    // 保存路径在下载确认时处理
    ((DownloadHandler)downloadHandler).OnDownloadConfirmed = (realPath) =>
    {
        fullPath = realPath;

        downloadListForm.Invoke(new Action(() =>
        {
            downloadListForm.AddDownloadItem(fileName, url, fullPath);
            if (!downloadListForm.Visible) downloadListForm.Show();
            downloadListForm.BringToFront();
        }));
         };
        }


        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 释放资源
            Cef.Shutdown();
            base.OnFormClosed(e);
        }
/*
        private void MainForm_Load(object sender, EventArgs e)
        {// 最大化窗口
         
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;}*/
         private void MainForm_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.Fixed3D; // 或者其他你想保留的边框样式
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = false; // 通常最大化窗口不需要置顶
        }

        

        public void button1_Click(object sender, EventArgs e)
        {
            browser.Load("https://test.suancaixianyu.cn/");

        }

        public void button2_Click(object sender, EventArgs e)
        {
            browser.Load("https://www.scmod.cn/");
        }

        public void button3_Click(object sender, EventArgs e)
        {
            browser.Load("https://scwz.top/");
        }
        private void button4_Click(object sender, EventArgs e)
        {
            browser.Load("https://web.schz.top/");
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            if (downloadListForm == null || downloadListForm.IsDisposed)
            {
                downloadListForm = new DownloadListForm();
            }

            downloadListForm.Show();
            downloadListForm.BringToFront();
        }



    }
}
