using SelectUnknown.ConfigManagment;
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

namespace SelectUnknown.Pages
{
    /// <summary>
    /// GeneralPage.xaml 的交互逻辑
    /// </summary>
    public partial class GeneralPage : Page
    {
        public static GeneralPage Instance = new GeneralPage();
        public GeneralPage()
        {
            InitializeComponent();
            IsStartUp.IsChecked = StartupManager.IsStartupEnabled(Main.APP_NAME);
            SilentStart.IsChecked = Config.SilentStart;
        }

        private void IsStartUp_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager.SetStartup(Main.APP_NAME, true);
        }

        private void IsStartUp_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager.SetStartup(Main.APP_NAME, false);
        }

        private void SilentStart_Checked(object sender, RoutedEventArgs e)
        {
            Config.SilentStart = true;
            StartupManager.SetStartup(Main.APP_NAME, (bool)IsStartUp.IsChecked);
        }

        private void SilentStart_Unchecked(object sender, RoutedEventArgs e)
        {

            Config.SilentStart = false;
            StartupManager.SetStartup(Main.APP_NAME, (bool)IsStartUp.IsChecked);
        }
    }
}
