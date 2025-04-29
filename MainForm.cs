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
        private ChromiumWebBrowser browser;
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
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 释放资源
            Cef.Shutdown();
            base.OnFormClosed(e);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void button1_Click(object sender, EventArgs e)
        {
            browser.Load("https://www.schub.top/");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            browser.Load("https://www.scmod.cn/");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            browser.Load("http://xn--1kq052aeifw5v.top/");
        }
    }
}
