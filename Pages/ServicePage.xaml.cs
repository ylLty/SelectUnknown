using SelectUnknown.ConfigManagment;
using SelectUnknown.Lens;
using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;

namespace SelectUnknown.Pages
{
    /// <summary>
    /// ServicePage.xaml 的交互逻辑
    /// </summary>
    public partial class ServicePage : Page
    {
        public static ServicePage Instance = new ServicePage();
        public ServicePage()
        {
            InitializeComponent();
            SearchEngineSelect.Text = Config.SearchEngineName;
            SearchEngineSelect.SelectedItem = Config.SearchEngineName;
            UsingAndroidUserAgentCheck.IsChecked = Config.UsingAndroidUserAgent;
            LensEngineSelect.Text = Config.LensEngineName;
            OcrEngineSelect.Text = Config.OcrEngineName;
        }

        private void SearchEngineSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchEngineSelect.SelectedItem == null) return;
            string result = SearchEngineSelect.SelectedItem.ToString().Split(':')[1].Trim();
            Config.SearchEngineName = result;
            ConfigManager.SaveConfig();
            if (result == "夸克" && Config.UsingAndroidUserAgent == true)
            {
                Main.PopupMessageOnConfigWindow("启用安卓用户代理后，夸克\n搜索引擎可能无法正常使用。");
            }
        }

        private void UsingAndroidUserAgentCheck_Checked(object sender, RoutedEventArgs e)
        {
            Config.UsingAndroidUserAgent = true;
            if (SearchEngineSelect.SelectedItem == null) return;
            string result = SearchEngineSelect.SelectedItem.ToString().Split(':')[1].Trim();
            if (result == "夸克")
            {
                Main.PopupMessageOnConfigWindow("启用安卓用户代理后，夸克\n搜索引擎可能无法正常使用。");
            }
            if (Config.LensEngineName=="百度")
            {
                Main.PopupMessageOnConfigWindow("当前识图引擎不能\n使用安卓 UA 标识");
            }
        }

        private void UsingAndroidUserAgentCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            Config.UsingAndroidUserAgent = false;
        }

        private void LensEngineSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LensEngineSelect.SelectedItem == null) return;
            string result = LensEngineSelect.SelectedItem.ToString().Split(':')[1].Trim();
            Config.LensEngineName = result;
            ConfigManager.SaveConfig();
            if (result == "百度")
            {
                Main.PopupMessageOnConfigWindow("此识图引擎需要手动粘贴");
                if (Config.UsingAndroidUserAgent)
                {
                    Main.PopupMessageOnConfigWindow("此识图引擎不能\n使用安卓 UA 标识");
                }
            }
        }

        private void TranslateEngineSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TranslateEngineSelect.SelectedItem == null) return;
            string result = TranslateEngineSelect.SelectedItem.ToString().Split(':')[1].Trim();
            Config.TranslateEngineName = result;
            ConfigManager.SaveConfig();
        }

        private async void OcrEngineSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OcrEngineSelect.SelectedItem == null) return;
            string result = OcrEngineSelect.SelectedItem.ToString().Split(':')[1].Trim();


            Config.OcrEngineName = result;
            ConfigManager.SaveConfig();
            if (OCRHelper.IsPaddleOcrEngineReady && result == "PaddleOCR-json") return;

            while (true)
            {
                try
                {
                    await OCRHelper.InitOcr();
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is FileNotFoundException)
                    { 
                        DialogResult dialogResult = System.Windows.Forms.MessageBox.Show($"PaddleOCRJson 引擎未安装，是否前往下载？\n安装指导：\n1、下载 default_runtime.zip \n2、打开软件根目录中 PaddleOCR-json 文件夹（按下确定会一同打开）\n3、将 default_runtime.zip 中的所有文件解压于 PaddleOCR-json 文件夹中\n4、重新选择引擎", "引擎未安装", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                        if (System.Windows.Forms.DialogResult.Yes == dialogResult)
                        {
                            string enginePath = Path.GetDirectoryName(OCRHelper.GetOcrEnginePath());
                            Process.Start(new ProcessStartInfo()
                            {
                                FileName = enginePath,
                                UseShellExecute = true,
                                Verb = "open"
                            });
                            if (!string.IsNullOrEmpty(OCRHelper.DownloadUrl))
                                Main.OpenUrl(OCRHelper.DownloadUrl);
                            else
                                Main.PopupMessageOnConfigWindow("未找到下载链接，请前往\n项目主页查看下载方式");
                        }
                        OcrEngineSelect.Text = "Windows 内置";// 回退到 Windows 内置 OCR 引擎
                        OcrEngineSelect.SelectedIndex = 0;
                        Config.OcrEngineName = "Windows 内置";
                        continue;
                    }
                    LogHelper.Log($"OCR 引擎初始化失败，异常信息: {ex}", LogLevel.Error);
                    System.Windows.Forms.MessageBox.Show($"OCR 引擎初始化失败，异常信息: {ex.Message}\n请确保已正确安装 PaddleOCRJson 依赖，并尝试重新启动软件。若无法解决问题，请尝试切换 OCR 引擎", "OCR 初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }
    }
}
