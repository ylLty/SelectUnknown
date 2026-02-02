using SelectUnknown.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace SelectUnknown
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            // 显示加载窗口
            var loadWindow = new LoadWindow();
            loadWindow.Show();
            // 开始加载
            Main.Init();
            AddHandler(UIElement.PreviewMouseDownEvent,
                  new MouseButtonEventHandler(GlobalPreviewMouseDown),
                  true);
            // 如果是静默启动模式，不显示配置窗口, 但是启动动画还是会有的
            if (App.silentMode)
            {
                App.silentMode = false;// 重置静默模式标志
                this.Closing += ConfigWindow_Closing;
                this.Close();
                //关闭加载窗口
                loadWindow.Close();
                return;
            }
            InitializeComponent();
            

            //加载完成，关闭加载窗口
            loadWindow.Close();

            this.Closing += ConfigWindow_Closing;
        }
        /// <summary>
        /// 监听窗口鼠标点击事件用于自动保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void GlobalPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Task.Run(async() =>
            {
                Thread.Sleep(300);// 等待配置项变化完成
                ConfigManagment.ConfigManager.SaveConfig();
            });
        }

        private void ConfigWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GeneralButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(GeneralPage.Instance);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(AboutPage.Instance);
        }
    }
}
