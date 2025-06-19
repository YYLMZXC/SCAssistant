using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;


namespace SCAssistant
{

    public class DownloadRecord
    {
        public string FileName { get; set; }
        public string Url { get; set; }
        public string LocalPath { get; set; } 
    }


    public partial class DownloadListForm : Form
    {

        private static readonly string HistoryFileName = Path.Combine(Application.StartupPath, "download_history.json");


        private List<DownloadRecord> downloadHistory = new List<DownloadRecord>();

        public DownloadListForm()
        {
            InitializeComponent();
            LoadDownloadHistory();
        }
        private void LoadDownloadHistory()
        {
            if (File.Exists(HistoryFileName))
            {
                try
                {
                    string json = File.ReadAllText(HistoryFileName);
                    downloadHistory = JsonConvert.DeserializeObject<List<DownloadRecord>>(json) ?? new List<DownloadRecord>();
                    foreach (var record in downloadHistory)
                    {
                        AddDownloadItem(record.FileName, record.Url, record.LocalPath, saveHistory: false);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("读取下载历史失败: " + ex.Message);
                    downloadHistory = new List<DownloadRecord>();
                }
            }
        }
        private void SaveDownloadHistory()
        {
            try
            {
                string json = JsonConvert.SerializeObject(downloadHistory);
                File.WriteAllText(HistoryFileName, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存下载历史失败: " + ex.Message);
            }
        }

        private void openFolderMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string localPath = listView1.SelectedItems[0].SubItems[2].Text;

                if (!string.IsNullOrWhiteSpace(localPath))
                {
                    if (File.Exists(localPath))
                    {
                        // 文件存在，打开文件所在目录并选中文件
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{localPath}\"");
                    }
                    else
                    {
                        // 文件不存在，尝试只打开文件夹（先获取文件夹路径）
                        string folderPath;
                        try
                        {
                            folderPath = Path.GetDirectoryName(localPath);
                        }
                        catch (ArgumentException)
                        {
                            folderPath = null;
                        }

                        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"\"{folderPath}\"");
                        }
                        else
                        {
                            MessageBox.Show("文件路径无效，无法打开文件夹。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("文件路径为空，无法打开文件夹。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void deleteRecordMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                // 提示确认删除
                var result = MessageBox.Show("确定要删除选中的下载记录吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    foreach (ListViewItem item in listView1.SelectedItems)
                    {
                        string url = item.SubItems[1].Text;
                        string localPath = item.SubItems.Count > 2 ? item.SubItems[2].Text : "";

                        // 从内存历史中移除对应项
                        var recordToRemove = downloadHistory.FirstOrDefault(r => r.Url == url && r.LocalPath == localPath);
                        if (recordToRemove != null)
                        {
                            downloadHistory.Remove(recordToRemove);
                        }

                        // 从界面移除选中项
                        listView1.Items.Remove(item);
                    }

                    // 保存更新后的历史
                    SaveDownloadHistory();
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的下载记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void DownloadListForm_Load(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            listView1.Columns.Add("文件名", 200);
            listView1.Columns.Add("下载地址", 400);
            listView1.Columns.Add("本地路径", 300); 
        }


        public void AddDownloadItem(string name, string url, string localPath = "", bool saveHistory = true)
        {
            var item = new ListViewItem(name);
            item.SubItems.Add(url);
            item.SubItems.Add(localPath); // 显示路径
            listView1.Items.Add(item);

            if (saveHistory && !downloadHistory.Any(r => r.Url == url && r.LocalPath == localPath))
            {
                downloadHistory.Add(new DownloadRecord { FileName = name, Url = url, LocalPath = localPath });
                SaveDownloadHistory();
            }

        }


        public void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string url = listView1.SelectedItems[0].SubItems[1].Text;
                string fileName = Path.GetFileName(url);

                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.FileName = fileName;
                    saveDialog.Filter = "所有文件 (*.*)|*.*";
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        string path = saveDialog.FileName;

                        WebClient client = new WebClient();
                        client.DownloadProgressChanged += (s, args) =>
                        {
                            this.Text = $"下载中: {args.ProgressPercentage}%";
                        };

                        client.DownloadFileCompleted += (s, args) =>
                        {
                            if (args.Error != null)
                                MessageBox.Show($"下载失败：{args.Error.Message}");
                            else
                                MessageBox.Show("下载完成！");
                            this.Text = "下载文件列表";
                        };

                        try
                        {
                            client.DownloadFileAsync(new Uri(url), path);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("下载失败：" + ex.Message);
                        }
                    }
                }
            }
        }
    }
}
