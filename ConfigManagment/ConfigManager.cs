using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using System.IO;
using SelectUnknown.LogManagement;
using System.Windows;

namespace SelectUnknown.ConfigManagment
{
    internal class ConfigManager
    {
        public static string ConfigFilePath { get; private set; } = GetConfigFilePath();
        public static void InitConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                LogHelper.Log("配置文件已存在，跳过创建默认配置文件", LogLevel.Info);
            }
            else
            {
                LogHelper.Log("配置文件不存在，创建默认配置文件", LogLevel.Info);
                ResetConfig();
            }
            ReadConfig();
        }
        private static string GetConfigFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.json");
        }
        public static void SaveConfig()
        {
            string jsonString;
            jsonString = JsonSerializer.Serialize(Config.curConfig, new JsonSerializerOptions
            {
                WriteIndented = true // 格式化输出
            });
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
            File.WriteAllText(ConfigFilePath, "");//先清空
            File.WriteAllText(ConfigFilePath, jsonString);
        }
        public static void ReadConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                LogHelper.Log("配置文件在读取时未找到，尝试从现有值中恢复", LogLevel.Warn);
                SaveConfig();
            }
            string jsonString = File.ReadAllText(ConfigFilePath);
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                ResetConfig();
                LogHelper.Log("配置文件为空，已重置", LogLevel.Warn);
            }
            try
            {
                Config.curConfig = JsonSerializer.Deserialize<Config>(jsonString);
                LogHelper.Log("配置读取成功");
            }
            catch (Exception ex)
            {
                ResetConfig();
                System.Windows.Forms.Clipboard.SetText(jsonString);
                System.Windows.Forms.MessageBox.Show($"配置文件读取失败，因为 Json 文件存在问题，已为您重置配置\n错误的文本已复制到剪切板，以便您恢复原有配置：\n{jsonString}", "配置加载出错",MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Log($"配置文件读取失败，已重置\n错误信息：\n{ex}\n错误的 json 文本：\n{jsonString}", LogLevel.Warn);
            }
        }
        public static void ResetConfig()
        {
            Config newConfig = new Config();
            Config.curConfig = newConfig;
            SaveConfig();
            //ReadConfig(); 没必要
            LogHelper.Log("已重置配置");
        }
    }
    // 已弃用
    //internal class ConfigManager
    //{
    //    public static string configFilePath = GetConfigFilePath();
    //    public static string defaultConfigJson = GetConfigFilePath();
    //    public static void InitConfig()
    //    {
    //        configFilePath = GetConfigFilePath();
    //        if (File.Exists(configFilePath)) 
    //        { 
    //            LogHelper.Log("配置文件已存在，跳过创建默认配置文件", LogLevel.Info);
    //        }
    //        else
    //        {
    //            LogHelper.Log("配置文件不存在，创建默认配置文件", LogLevel.Info);
    //            CreateDefaultConfigFile(configFilePath);
    //        }
    //        ReadConfig();
    //    }
    //    public static void ReadConfig()
    //    {
    //        string configFilePath = GetConfigFilePath();
    //        if (File.Exists(configFilePath))
    //        {
    //            LogHelper.Log("读取配置文件内容", LogLevel.Info);

    //            string json = File.ReadAllText(configFilePath);
    //            if (string.IsNullOrWhiteSpace(json))
    //            {
    //                LogHelper.Log("配置文件内容为空，无法读取内容，将重新创建", LogLevel.Warn);
    //                ResetConfig();
    //                json = File.ReadAllText(configFilePath);// 重新读取
    //            }

    //            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
    //            if (dict == null) return;

    //            var properties = typeof(Config).GetProperties(
    //                BindingFlags.Public | BindingFlags.Static
    //            );

    //            foreach (var prop in properties)
    //            {
    //                if (!prop.CanWrite) continue;
    //                if (!dict.TryGetValue(prop.Name, out var value)) continue;

    //                object? convertedValue = value.Deserialize(prop.PropertyType);
    //                prop.SetValue(null, convertedValue);
    //            }
    //        }
    //        else
    //        {
    //            LogHelper.Log("配置文件不存在，无法读取内容，将重新创建", LogLevel.Warn);
    //            ResetConfig();
    //        }
    //    }
    //    /// <summary>
    //    /// 保存配置到配置文件
    //    /// </summary>
    //    public static void SaveConfig()
    //    {
    //        LogHelper.Log("保存了一次配置文件");
    //        var dict = new Dictionary<string, object?>();

    //        var properties = typeof(Config).GetProperties(
    //            BindingFlags.Public | BindingFlags.Static
    //        );

    //        foreach (var prop in properties)
    //        {
    //            if (!prop.CanRead) continue;

    //            dict[prop.Name] = prop.GetValue(null);
    //        }

    //        var json = JsonSerializer.Serialize(
    //            dict,
    //            new JsonSerializerOptions
    //            {
    //                WriteIndented = true
    //            }
    //        );

    //        Directory.CreateDirectory(Path.GetDirectoryName(configFilePath)!);
    //        File.WriteAllText(configFilePath, "");//先清空
    //        File.WriteAllText(configFilePath, json);
    //    }
    //    public static void ResetConfig()
    //    {
    //        string configFilePath = GetConfigFilePath();
    //        LogHelper.Log("重置配置文件，删除现有配置文件并创建默认配置文件", LogLevel.Info);
    //        if (File.Exists(configFilePath))
    //        {
    //            File.Delete(configFilePath);
    //            LogHelper.Log("现有配置文件删除成功", LogLevel.Info);
    //        }
    //        CreateDefaultConfigFile(configFilePath);
    //    }
    //    private static string GetConfigFilePath()
    //    {
    //        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.json");
    //    }
    //    private static void CreateDefaultConfigFile(string configFilePath)
    //    {
    //        if (!Directory.Exists(Path.GetDirectoryName(configFilePath)))
    //        {
    //            LogHelper.Log("配置文件目录不存在，创建目录", LogLevel.Info);
    //            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
    //        }
    //        if(!File.Exists(configFilePath))
    //        {
    //            // 默认的配置文件内容
    //            string defaultConfigJson = GetDefaultConfigJson();
    //            File.WriteAllText(configFilePath, defaultConfigJson);
    //            LogHelper.Log("默认配置文件创建成功", LogLevel.Info);
    //        }
    //        else
    //        {
    //            LogHelper.Log("调用了 GetConfigFilePath() 创建配置文件，但是配置文件已存在，若是重置配置，需要先删除", LogLevel.Warn);
    //        }
    //    }
    //    /// <summary>
    //    /// 获取默认的配置 json 在软件初始化时调用
    //    /// </summary>
    //    /// <returns></returns>
    //    private static string GetDefaultConfigJson()
    //    {
    //        SaveConfig();// 初始化时调用就是默认配置
    //        return File.ReadAllText(configFilePath);
    //    }
    //}
}
