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

            browser = new ChromiumWebBrowser("https://www.schub.top/");
            this.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;

            downloadHandler = new DownloadHandler(this);
            browser.DownloadHandler = downloadHandler;

            // 订阅下载创建事件
            downloadHandler.DownloadCreated += OnDownloadCreated;
        }
        private void OnDownloadCreated(string fileName, string url)
        {
            // 跨线程安全调用窗体方法
            if (downloadListForm == null || downloadListForm.IsDisposed)
            {
                // UI线程创建窗体并显示
                this.Invoke(new Action(() =>
                {
                    downloadListForm = new DownloadListForm();
                    downloadListForm.Show();
                }));
            }

            // 下面确保添加项时窗体句柄已创建
            if (!downloadListForm.IsHandleCreated)
            {
                var _ = downloadListForm.Handle; // 强制创建句柄
            }

            // 通过 Invoke 添加下载项
            downloadListForm.Invoke(new Action(() =>
            {
                downloadListForm.AddDownloadItem(fileName, url);
                // 你也可以这里BringToFront保证窗体在前
                if (!downloadListForm.Visible)
                    downloadListForm.Show();
                downloadListForm.BringToFront();
            }));
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 释放资源
            Cef.Shutdown();
            base.OnFormClosed(e);
        }

        public void MainForm_Load(object sender, EventArgs e)
        {

        }
        public void button1_Click(object sender, EventArgs e)
        {
            browser.Load("https://www.schub.top/");
        }

        public void button2_Click(object sender, EventArgs e)
        {
            browser.Load("https://www.scmod.cn/");
        }

        public void button3_Click(object sender, EventArgs e)
        {
            browser.Load("http://xn--1kq052aeifw5v.top/");
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
