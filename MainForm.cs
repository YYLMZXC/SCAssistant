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
        public Button settingsButton;
        public ChromiumWebBrowser browser;
        public DownloadHandler downloadHandler;
        public MainForm()
        {
            InitializeComponent();
            // 初始化 CEF
            CefSettings settings = new CefSettings();
            Cef.Initialize(settings);

            // 创建 ChromiumWebBrowser 控件
            browser = new ChromiumWebBrowser("https://www.schub.top/");
            this.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
            // 创建并设置下载处理器
            downloadHandler = new DownloadHandler(this);
            browser.DownloadHandler = downloadHandler;
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
            var form = new DownloadListForm();
            form.ShowDialog();
        }


    }
}
