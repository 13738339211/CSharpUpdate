using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;

namespace Updating
{
    public partial class UpdateForm : Form
    {
        // 成员变量
        private readonly string _currentVersion;
        private readonly string _versionFileUrl;
        private readonly string _updatePackageUrl;
        private readonly string _downloadPath;

        private WebClient _webClient;
        private long _totalBytes;
        private long _receivedBytes;
        private DateTime _lastUpdateTime;

        public UpdateForm(string currentVersion, string versionFileUrl, string updatePackageUrl)
        {
            InitializeComponent();

            _currentVersion = currentVersion;
            _versionFileUrl = versionFileUrl;
            _updatePackageUrl = updatePackageUrl;
            _downloadPath = Path.Combine(Application.StartupPath, "update.ltk");

            // 窗体加载后立即开始下载
            this.Load += (s, e) => BeginDownload();
        }

        // 开始下载更新包
        private void BeginDownload()
        {
            lblStatus.Text = "正在下载更新...";
            progressBar.Value = 0;

            _webClient = new WebClient();
            _webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            _webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

            _receivedBytes = 0;
            _lastUpdateTime = DateTime.Now;

            _webClient.DownloadFileAsync(new Uri(_updatePackageUrl), _downloadPath);
        }

        // 下载进度变化事件
        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (_totalBytes == 0)
            {
                _totalBytes = e.TotalBytesToReceive;
            }

            _receivedBytes = e.BytesReceived;

            // 计算下载速度
            TimeSpan timeSpan = DateTime.Now - _lastUpdateTime;
            if (timeSpan.TotalSeconds >= 1)
            {
                double speed = _receivedBytes / timeSpan.TotalSeconds;
                string speedText = speed < 1024 ? $"{speed:F0} B/s" :
                    speed < 1024 * 1024 ? $"{speed / 1024:F1} KB/s" : $"{speed / (1024 * 1024):F1} MB/s";

                lblSpeed.Text = speedText;
                _lastUpdateTime = DateTime.Now;
            }

            // 更新进度条
            progressBar.Value = e.ProgressPercentage;
            lblProgress.Text = $"{e.ProgressPercentage}%";
        }

        // 下载完成事件
        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("下载已取消。", "更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show($"下载失败: {e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lblStatus.Text = "下载完成，正在解压...";
            progressBar.Value = 100;

            // 开始解压
            Task.Run(() => ExtractUpdatePackage());
        }

        // 解压更新包
        private void ExtractUpdatePackage()
        {
            try
            {
                using (ZipFile zip = new ZipFile(_downloadPath))
                {
                    // 尝试使用写死的密码，如果不需要密码就设为null
                    string fixedPassword = "Hx.123456"; // 你的写死密码
                    if (!string.IsNullOrEmpty(fixedPassword))
                    {
                        zip.Password = fixedPassword;
                    }

                    int totalEntries = (int)zip.Count;
                    int processedEntries = 0;

                    foreach (ZipEntry entry in zip)
                    {
                        if (entry.IsFile)
                        {
                            string extractPath = Path.Combine(Application.StartupPath, entry.Name);
                            string directory = Path.GetDirectoryName(extractPath);

                            if (!Directory.Exists(directory))
                                Directory.CreateDirectory(directory);

                            using (FileStream stream = new FileStream(extractPath, FileMode.Create))
                            using (Stream zipStream = zip.GetInputStream(entry))
                            {
                                zipStream.CopyTo(stream);
                            }

                            processedEntries++;
                            int progress = (int)((double)processedEntries / totalEntries * 100);

                            this.Invoke(new Action(() =>
                            {
                                lblStatus.Text = $"解压文件中... ({processedEntries}/{totalEntries})";
                                progressBar.Value = progress;
                            }));
                        }
                    }
                }

                this.Invoke(new Action(() =>
                {
                    lblStatus.Text = "更新完成，准备重启应用...";
                    PrepareRestart();
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    string errorMsg = "解压失败: " + ex.Message;
                    MessageBox.Show(errorMsg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        // 准备重启应用程序
        private void PrepareRestart()
        {
            try
            {
                string batchScript = Path.Combine(Application.StartupPath, "update_restart.bat");
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string currentExeName = Path.GetFileName(currentExe);

                using (StreamWriter writer = new StreamWriter(batchScript, false, Encoding.Default))
                {
                    writer.WriteLine("@echo off");

                    // 等待主程序退出
                    writer.WriteLine($"taskkill /f /im \"{currentExeName}\" > nul");
                    writer.WriteLine("timeout /t 2 /nobreak > nul");

                    // 删除旧文件并复制新文件
                    writer.WriteLine("del *.exe *.dll *.pdb *.config");
                    writer.WriteLine("robocopy \"HXLVBMSTool\" \".\" /move /e > nul");

                    // 启动新版本并自删除
                    writer.WriteLine($"start \"\" \"{currentExe}\"");
                    writer.WriteLine("del \"%~f0\"");
                }

                // 启动更新脚本
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batchScript,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                Process.Start(psi);

                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 取消按钮点击事件
        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_webClient != null && _webClient.IsBusy)
            {
                _webClient.CancelAsync();
            }
            Close();
        }
    }
}