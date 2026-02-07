using System;
using System.Collections.Generic;
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
using SelectUnknown.ConfigManagment;

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
        }

        private void UsingAndroidUserAgentCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            Config.UsingAndroidUserAgent = false;
        }
    }
}
