using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCAssistant
{
    public partial class SetupForm : Form
    {
        private string installPath = @"C:\SCAssistant"; // 默认安装路径
        private bool isInstalling = false;
        public SetupForm()
        {
            InitializeComponent();
        }

        private void SetupForm_Load(object sender, EventArgs e)
        {
            // 设置安装路径为默认值
            metroLabelPath.Text = installPath;
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            if (!isInstalling)
            {
                // 打开文件夹浏览器对话框，选择安装路径
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "选择安装目录";
                    folderBrowserDialog.SelectedPath = installPath;

                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        installPath = folderBrowserDialog.SelectedPath;
                        metroLabelPath.Text = installPath;
                    }
                }
            }
            else
            {
                // 开始安装
                InstallApplication();
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void InstallApplication()
        {
            isInstalling = true;
            nextButton.Text = "安装";
            metroLabelProgress.Text = "正在安装...";
            progressBar.Value = 0;

            // 这里是模拟安装过程，实际应用中需要替换为真实的安装逻辑
            progressBar.Value = 25;
            metroLabelStatus.Text = "创建目录...";
            Directory.CreateDirectory(installPath);

            progressBar.Value = 50;
            metroLabelStatus.Text = "复制文件...";
            // 在这里添加复制文件的代码

            progressBar.Value = 75;
            metroLabelStatus.Text = "创建快捷方式...";
            // 在这里添加创建快捷方式的代码

            progressBar.Value = 100;
            metroLabelProgress.Text = "安装完成!";
            metroLabelStatus.Text = "点击完成按钮退出安装程序。";
            nextButton.Text = "完成";
            isInstalling = false;
        }
    }
}

