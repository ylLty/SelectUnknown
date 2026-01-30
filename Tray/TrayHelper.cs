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

        var configItem = new ToolStripMenuItem("配置", null, ConfigMenuItemClick);
        contextMenu.Items.Add(configItem);

        var logItem = new ToolStripMenuItem("打开日志", null, LogMenuItemClick);
        contextMenu.Items.Add(logItem);

        var exitItem = new ToolStripMenuItem("退出", null, ExitMenuItemClick);
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // 托盘图标的双击事件
        _notifyIcon.DoubleClick += (sender, args) =>
        {
            
        };
    }

    private static void LogMenuItemClick(object? sender, EventArgs e)
    {
        Process.Start("explorer.exe", $"/select,\"{LogHelper.logFilePath}\"");
    }

    private static void ConfigMenuItemClick(object? sender, EventArgs e)
    {
        ConfigWindow configWindow = new ConfigWindow();
        configWindow.Show();
    }

    // 托盘右键菜单的退出操作
    private static void ExitMenuItemClick(object? sender, EventArgs e)
    {
        Cleanup();
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
    }
}
