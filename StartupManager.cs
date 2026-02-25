using Microsoft.Win32;
using SelectUnknown.ConfigManagment;
using System;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

public class StartupManager
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// 设置开机自启动
    /// </summary>
    /// <param name="appName">应用程序唯一名称</param>
    /// <param name="enable">是否启用</param>
    public static void SetStartup(string appName, bool enable)
    {
        try
        {
            string silentMode =  Config.curConfig.SilentStart? " --silent" : "";
            // 获取当前程序路径
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
            {
                if (key == null)
                {
                    MessageBox.Show("无法访问注册表！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (enable)
                {
                    // 添加到注册表
                    key.SetValue(appName, $"\"{exePath}\"" + silentMode);
                }
                else
                {
                    // 从注册表移除
                    if (key.GetValue(appName) != null)
                    {
                        key.DeleteValue(appName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"设置开机启动失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 检查是否已设置开机自启动
    /// </summary>
    public static bool IsStartupEnabled(string appName)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
            {
                if (key == null) return false;

                return key.GetValue(appName) != null;
            }
        }
        catch
        {
            return false;
        }
    }
}