using SelectUnknown;
using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using SelectUnknown.LogManagement;

public static class TrayHelper
{
    private static NotifyIcon _notifyIcon;

    // 初始化托盘
    public static void InitTray(string tooltip, string iconPath)
    {
        if (_notifyIcon != null) return;  // 确保只初始化一次

        _notifyIcon = new NotifyIcon
        {
            Icon = new System.Drawing.Icon(iconPath),  // 设置托盘图标
            Visible = true,  // 显示托盘图标
            Text = tooltip   // 设置托盘提示文本
        };

        // 为托盘图标添加右键菜单
        var contextMenu = new ContextMenuStrip();

        var configItem = new ToolStripMenuItem("配置菜单", null, ConfigMenuItemClick);
        contextMenu.Items.Add(configItem);

        var logItem = new ToolStripMenuItem("打开日志", null, LogMenuItemClick);
        contextMenu.Items.Add(logItem);

        var exitItem = new ToolStripMenuItem("退出应用", null, ExitMenuItemClick);
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // 托盘图标的双击事件
        _notifyIcon.DoubleClick += (sender, args) =>
        {
            
        };
    }

    private static void LogMenuItemClick(object? sender, EventArgs e)
    {
        LogHelper.Log("用户通过托盘菜单打开了日志文件所在目录", LogLevel.Info);
        Process.Start("explorer.exe", $"/select,\"{LogHelper.logFilePath}\"");
    }

    private static void ConfigMenuItemClick(object? sender, EventArgs e)
    {
        var configWindow = System.Windows.Application.Current.MainWindow;
        configWindow.Show();
        configWindow.Activate();
        LogHelper.Log("用户通过托盘菜单打开了配置窗口", LogLevel.Info);
    }

    // 托盘右键菜单的退出操作
    private static void ExitMenuItemClick(object? sender, EventArgs e)
    {
        Cleanup(); 
        LogHelper.Log("托盘图标已被清理，应用程序即将退出", LogLevel.Info);
        Environment.Exit(0);  // 退出应用程序
    }

    // 清理托盘
    public static void Cleanup()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        LogHelper.Log("托盘图标已被清理", LogLevel.Debug);
    }
}
