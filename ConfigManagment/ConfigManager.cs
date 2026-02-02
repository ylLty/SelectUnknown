using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using System.IO;
using SelectUnknown.LogManagement;

namespace SelectUnknown.ConfigManagment
{
    internal class ConfigManager
    {
        public static string configFilePath = GetConfigFilePath();
        public static string defaultConfigJson = GetConfigFilePath();
        public static void InitConfig()
        {
            configFilePath = GetConfigFilePath();
            if (File.Exists(configFilePath)) 
            { 
                LogHelper.Log("配置文件已存在，跳过创建默认配置文件", LogLevel.Info);
            }
            else
            {
                LogHelper.Log("配置文件不存在，创建默认配置文件", LogLevel.Info);
                CreateDefaultConfigFile(configFilePath);
            }
            ReadConfig();
        }
        private static void ReadConfig()
        {
            string configFilePath = GetConfigFilePath();
            if (File.Exists(configFilePath))
            {
                LogHelper.Log("读取配置文件内容", LogLevel.Info);

                var json = File.ReadAllText(configFilePath);

                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (dict == null) return;

                var properties = typeof(Config).GetProperties(
                    BindingFlags.Public | BindingFlags.Static
                );

                foreach (var prop in properties)
                {
                    if (!prop.CanWrite) continue;
                    if (!dict.TryGetValue(prop.Name, out var value)) continue;

                    object? convertedValue = value.Deserialize(prop.PropertyType);
                    prop.SetValue(null, convertedValue);
                }
            }
            else
            {
                LogHelper.Log("配置文件不存在，无法读取内容", LogLevel.Error);
                throw new FileNotFoundException("配置文件未找到", configFilePath);
            }
        }
        /// <summary>
        /// 保存配置到配置文件
        /// </summary>
        public static void SaveConfig()
        {
            LogHelper.Log("保存了一次配置文件");
            var dict = new Dictionary<string, object?>();

            var properties = typeof(Config).GetProperties(
                BindingFlags.Public | BindingFlags.Static
            );

            foreach (var prop in properties)
            {
                if (!prop.CanRead) continue;

                dict[prop.Name] = prop.GetValue(null);
            }

            var json = JsonSerializer.Serialize(
                dict,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath)!);
            File.WriteAllText(configFilePath, "");//先清空
            File.WriteAllText(configFilePath, json);
        }
        public static void ResetConfig()
        {
            string configFilePath = GetConfigFilePath();
            LogHelper.Log("重置配置文件，删除现有配置文件并创建默认配置文件", LogLevel.Info);
            if (File.Exists(configFilePath))
            {
                File.Delete(configFilePath);
                LogHelper.Log("现有配置文件删除成功", LogLevel.Info);
            }
            CreateDefaultConfigFile(configFilePath);
        }
        private static string GetConfigFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.json");
        }
        private static void CreateDefaultConfigFile(string configFilePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(configFilePath)))
            {
                LogHelper.Log("配置文件目录不存在，创建目录", LogLevel.Info);
                Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            }
            if(!File.Exists(configFilePath))
            {
                // 默认的配置文件内容
                string defaultConfigJson = GetDefaultConfigJson();
                File.WriteAllText(configFilePath, defaultConfigJson);
                LogHelper.Log("默认配置文件创建成功", LogLevel.Info);
            }
            else
            {
                LogHelper.Log("调用了 GetConfigFilePath() 创建配置文件，但是配置文件已存在，若是重置配置，需要先删除", LogLevel.Warn);
            }
        }
        /// <summary>
        /// 获取默认的配置 json 在软件初始化时调用
        /// </summary>
        /// <returns></returns>
        private static string GetDefaultConfigJson()
        {
            SaveConfig();// 初始化时调用就是默认配置
            return File.ReadAllText(configFilePath);
        }
    }
}
