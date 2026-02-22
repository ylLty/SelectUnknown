using SelectUnknown;
using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using SelectUnknown.LogManagement;
using SelectUnknown.Lens;

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

        var startItem = new ToolStripMenuItem("框索未知", null, StartMenuItemClick);
        contextMenu.Items.Add(startItem);

        var configItem = new ToolStripMenuItem("配置菜单", null, ConfigMenuItemClick);
        contextMenu.Items.Add(configItem);

        var logItem = new ToolStripMenuItem("打开日志", null, LogMenuItemClick);
        contextMenu.Items.Add(logItem);

        var exitItem = new ToolStripMenuItem("退出应用", null, ExitMenuItemClick);
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // 托盘图标的双击事件
        _notifyIcon.MouseDoubleClick += (sender, args) =>
        {
            //if (args.Button == MouseButtons.Left)
            //{
            //    var configWindow = System.Windows.Application.Current.MainWindow;
            //    configWindow.Show();
            //    configWindow.Activate();
            //    LogHelper.Log("用户通过左键双击托盘图标打开了配置窗口", LogLevel.Info);
            //}
            // 暂时弃用双击打开配置窗口，改为单击开始框索未知
        };
        _notifyIcon.MouseClick += (sender, args) =>
        {
            if (args.Button == MouseButtons.Left)
            {
                LensHelper.Start();
                LogHelper.Log("用户通过左键单击托盘图标点击开始了框索未知", LogLevel.Info);
            }
        };
    }

    private static void StartMenuItemClick(object? sender, EventArgs e)
    {
        LensHelper.Start();
        LogHelper.Log("用户通过托盘菜单开始了框索未知", LogLevel.Info);

    }

    private static void LogMenuItemClick(object? sender, EventArgs e)
    {
        LogHelper.OpenLogDirectory();
        LogHelper.Log("用户通过托盘菜单打开了日志文件所在目录", LogLevel.Info);
    }

    private static void ConfigMenuItemClick(object? sender, EventArgs e)
    {
        Main.OpenConfigWindow();
        LogHelper.Log("用户通过托盘菜单打开了配置窗口", LogLevel.Info);
    }

    // 托盘右键菜单的退出操作
    private static void ExitMenuItemClick(object? sender, EventArgs e)
    {
        Cleanup(); 
        OCRHelper.engine.Dispose();
        OCRHelper.client.Dispose();

        LogHelper.Log("托盘图标及文字识别引擎已被清理，应用程序即将退出", LogLevel.Info);
        
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
