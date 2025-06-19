using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;


namespace SCAssistant
{

    public class DownloadRecord
    {
        public string FileName { get; set; }
        public string Url { get; set; }
    }

    public partial class DownloadListForm : Form
    {

        private const string HistoryFileName = "download_history.json";

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
                    // 读
                    downloadHistory = JsonConvert.DeserializeObject<List<DownloadRecord>>(json);
                    // 写
                    File.WriteAllText(HistoryFileName, JsonConvert.SerializeObject(downloadHistory));
                    foreach (var record in downloadHistory)
                    {
                        AddDownloadItem(record.FileName, record.Url, saveHistory: false);
                    }
                }
                catch
                {
                    // 读取或解析失败，忽略
                    downloadHistory = new List<DownloadRecord>();
                }
            }
        }
        private void SaveDownloadHistory()
        {
            try
            {
                // 序列化为字符串
                string json = JsonConvert.SerializeObject(downloadHistory);
                File.WriteAllText(HistoryFileName, json);
            }
            catch
            {
                // 保存失败，忽略或弹窗提示
            }
        }
        public void DownloadListForm_Load(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            listView1.Columns.Add("文件名", 200);
            listView1.Columns.Add("下载地址", 400);

        }

        public void AddDownloadItem(string name, string url, bool saveHistory = true)
        {
            var item = new ListViewItem(name);
            item.SubItems.Add(url);
            listView1.Items.Add(item);

            if (saveHistory)
            {
                downloadHistory.Add(new DownloadRecord { FileName = name, Url = url });
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
