using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Updating;

namespace UpdateTest
{
    public partial class Form1 : Form
    {
        // 配置信息
        private const string CurrentVersion = "1.0.0";
        private const string VersionFileUrl = "https://upper.freedash.top/Update/HXLVBMSTool/Version.txt";
        private const string UpdatePackageUrl = "https://upper.freedash.top/Update/HXLVBMSTool/HXLVBMSTool.zip";

        private UpdateChecker _updateChecker;

        public Form1()
        {
            InitializeComponent();
            
            // 初始化更新检查器
            _updateChecker = new UpdateChecker(CurrentVersion, VersionFileUrl);
            
            // 启动时自动检查更新
            CheckForUpdateOnStartup();
        }

        // 启动时检查更新
        private async void CheckForUpdateOnStartup()
        {
            try
            {
                // 延迟1秒检查，避免影响启动速度
                await Task.Delay(1000);

                // 使用专门的方法进行启动时检查
                bool shouldUpdate = await _updateChecker.CheckAndPromptForStartupAsync();

                if (shouldUpdate)
                {
                    // 用户选择更新，显示更新进度窗口
                    ShowUpdateProgress();
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不干扰用户
                Console.WriteLine($"启动时检查更新失败: {ex.Message}");
            }
        }

        // 手动检查更新按钮点击事件
        private async void btnCheckUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                // 禁用按钮防止重复点击
                btnCheckUpdate.Enabled = false;
                btnCheckUpdate.Text = "检查中...";

                // 先检查是否有更新（静默模式）
                bool hasUpdate = await _updateChecker.CheckForUpdateAsync();

                if (hasUpdate)
                {
                    // 有更新，显示提示框
                    bool shouldUpdate = await _updateChecker.CheckAndPromptForStartupAsync();

                    if (shouldUpdate)
                    {
                        // 用户选择更新，显示更新进度窗口
                        ShowUpdateProgress();
                    }
                    //else
                    //{
                    //    // 用户取消更新
                    //    MessageBox.Show("已取消更新，您可以在需要时再次检查更新。",
                    //        "更新已取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //}
                }
                else
                {
                    // 没有更新可用
                    MessageBox.Show($"当前版本 {CurrentVersion} 已是最新版本！\n无需更新。",
                        "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查更新失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 恢复按钮状态
                btnCheckUpdate.Enabled = true;
                btnCheckUpdate.Text = "检查更新";
            }
        }

        // 显示更新进度窗口
        private void ShowUpdateProgress()
        {
            UpdateForm updateForm = new UpdateForm(CurrentVersion, VersionFileUrl, UpdatePackageUrl);
            updateForm.ShowDialog();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // 显示当前版本
            txtb_CurrVer.Text = CurrentVersion;

            try
            {
                // 异步获取最新版本号
                await _updateChecker.CheckForUpdateAsync();
                txtb_LatestVer.Text = _updateChecker.LatestVersion ?? "获取失败";
            }
            catch
            {
                txtb_LatestVer.Text = "获取失败";
            }
        }
    }
}