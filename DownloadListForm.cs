using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace SCAssistant
{
    public partial class DownloadListForm : Form
    {
        public DownloadListForm()
        {
            InitializeComponent();
        }

        public void DownloadListForm_Load(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            listView1.Columns.Add("文件名", 200);
            listView1.Columns.Add("下载地址", 400);

        }

        public void AddDownloadItem(string name, string url)
        {
            var item = new ListViewItem(name);
            item.SubItems.Add(url);
            listView1.Items.Add(item);
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
