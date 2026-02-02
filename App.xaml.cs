using System.Configuration;
using System.Data;
using System.Windows;

namespace SelectUnknown
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static bool silentMode = false;
        protected override void OnStartup(StartupEventArgs e)
        {

            // 检查参数
            foreach (string arg in e.Args)
            {
                if (arg.Equals("--silent", StringComparison.OrdinalIgnoreCase))
                {
                    silentMode = true;
                    break;
                }
            }
        }
    }

}
