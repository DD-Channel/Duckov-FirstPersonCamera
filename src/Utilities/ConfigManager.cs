using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.Utilities
{
    /// <summary>
    /// 配置数据容器（用于JSON序列化）
    /// </summary>
    [Serializable]
    public class ConfigDataContainer
    {
        public List<ConfigEntry> entries = new List<ConfigEntry>();
    }

    /// <summary>
    /// 配置条目
    /// </summary>
    [Serializable]
    public class ConfigEntry
    {
        public string key;
        public string value;
        public string type; // "int", "float", "bool", "string", "keycode"

        public ConfigEntry() { }

        public ConfigEntry(string key, object value)
        {
            this.key = key;
            this.type = GetTypeString(value);
            this.value = ConvertToString(value);
        }

        private string GetTypeString(object value)
        {
            if (value is int) return "int";
            if (value is float) return "float";
            if (value is bool) return "bool";
            if (value is KeyCode) return "keycode";
            return "string";
        }

        private string ConvertToString(object value)
        {
            if (value == null) return "";
            if (value is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is KeyCode kc) return ((int)kc).ToString(); // KeyCode保存为整数
            return value.ToString();
        }

        public object GetValue()
        {
            switch (type)
            {
                case "int":
                    return int.TryParse(value, out int i) ? i : 0;
                case "float":
                    return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f) ? f : 0f;
                case "bool":
                    return bool.TryParse(value, out bool b) ? b : false;
                case "keycode":
                    // 尝试解析为整数（新格式）
                    if (int.TryParse(value, out int k))
                    {
                        return (KeyCode)k;
                    }
                    // 兼容旧格式：尝试通过名称解析（如"F5", "Q"等）
                    if (System.Enum.TryParse<KeyCode>(value, true, out KeyCode parsedKey))
                    {
                        return parsedKey;
                    }
                    return KeyCode.None;
                default:
                    return value;
            }
        }
    }

    /// <summary>
    /// 第一人称相机配置管理器
    /// 负责将所有配置数据保存到独立的JSON文件中
    /// </summary>
    public static class ConfigManager
    {
        #region 私有字段
        /// <summary>
        /// 配置文件路径
        /// </summary>
        private static string configFilePath;

        /// <summary>
        /// 内存中的配置数据字典
        /// </summary>
        private static Dictionary<string, object> configData = new Dictionary<string, object>();

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private static bool initialized = false;

        /// <summary>
        /// 配置锁，用于线程安全
        /// </summary>
        private static readonly object configLock = new object();

        /// <summary>
        /// 当前值缓存，用于在加载失败时保持当前值
        /// </summary>
        private static Dictionary<string, object> currentValueCache = new Dictionary<string, object>();
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        private static void Initialize()
        {
            if (initialized) return;

            lock (configLock)
            {
                if (initialized) return;

                try
                {
                    // 使用用户指定的路径: C:\Users\Administrator\AppData\LocalLow\TeamSoda\Duckov\FirstPersonCamera
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string localLowPath = Path.Combine(userProfile, "AppData", "LocalLow");
                    string finalDir = Path.Combine(localLowPath, "TeamSoda", "Duckov", "FirstPersonCamera");
                    
                    // 兼容旧路径（用于迁移）
                    string legacyModsDir = Path.Combine(Application.dataPath, "Mods", "FirstPersonCamera", "Config");
                    string legacyPersistentDir = Path.Combine(Application.persistentDataPath, "FirstPersonCamera");
                    string legacyModsFile = Path.Combine(legacyModsDir, "config.json");
                    string legacyPersistentFile = Path.Combine(legacyPersistentDir, "config.json");

                    // 确保目标目录可写
                    if (!TryEnsureWritable(finalDir))
                    {
                        FPLogger.LogError("无法创建配置文件目录: " + finalDir);
                        finalDir = legacyPersistentDir;
                        TryEnsureWritable(finalDir); // 尽力创建备用目录
                    }

                    configFilePath = Path.Combine(finalDir, "config.json");

                    // 迁移旧配置：如果新路径不存在文件但旧路径存在，则复制
                    if (!File.Exists(configFilePath))
                    {
                        // 优先从Mods目录迁移
                        if (File.Exists(legacyModsFile))
                        {
                            try 
                            { 
                                Directory.CreateDirectory(finalDir); 
                                File.Copy(legacyModsFile, configFilePath, overwrite: false);
                                FPLogger.Log("已从Mods目录迁移配置文件");
                            }
                            catch (Exception ex) 
                            { 
                                FPLogger.LogWarning("从Mods目录迁移配置失败: {0}", ex.Message);
                            }
                        }
                        // 其次从PersistentDataPath迁移
                        else if (File.Exists(legacyPersistentFile) && !string.Equals(configFilePath, legacyPersistentFile, StringComparison.OrdinalIgnoreCase))
                        {
                            try 
                            { 
                                Directory.CreateDirectory(finalDir); 
                                File.Copy(legacyPersistentFile, configFilePath, overwrite: false);
                                FPLogger.Log("已从PersistentDataPath迁移配置文件");
                            }
                            catch (Exception ex) 
                            { 
                                FPLogger.LogWarning("从PersistentDataPath迁移配置失败: {0}", ex.Message);
                            }
                        }
                    }

                    // 加载现有配置
                    LoadConfig();
                    
                    FPLogger.Log("配置文件路径: {0}", configFilePath);
                    FPLogger.Log("已加载 {0} 个配置项", configData.Count);

                    initialized = true;
                }
                catch (Exception ex)
                {
                    FPLogger.LogError("配置管理器初始化失败: {0}", ex.Message);
                    configData = new Dictionary<string, object>();
                    
                    // 即使初始化失败，也尝试设置一个默认路径
                    if (string.IsNullOrEmpty(configFilePath))
                    {
                        try
                        {
                            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            string localLowPath = Path.Combine(userProfile, "AppData", "LocalLow");
                            string finalDir = Path.Combine(localLowPath, "TeamSoda", "Duckov", "FirstPersonCamera");
                            configFilePath = Path.Combine(finalDir, "config.json");
                            FPLogger.LogWarning("使用默认配置文件路径: {0}", configFilePath);
                        }
                        catch
                        {
                            // 如果连默认路径都设置失败，使用临时路径
                            configFilePath = Path.Combine(Application.persistentDataPath, "FirstPersonCamera", "config.json");
                            FPLogger.LogWarning("使用备用配置文件路径: {0}", configFilePath);
                        }
                    }
                    
                    initialized = true;
                }
            }
        }
        #endregion

        #region 配置加载和保存
        /// <summary>
        /// 从文件加载配置
        /// </summary>
        private static void LoadConfig()
        {
            if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
            {
                configData = new Dictionary<string, object>();
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(configFilePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    configData = new Dictionary<string, object>();
                    return;
                }

                configData = new Dictionary<string, object>();
                
                // 优先使用Newtonsoft.Json进行反序列化
                try
                {
                    ConfigDataContainer container = JsonConvert.DeserializeObject<ConfigDataContainer>(jsonContent);
                    if (container != null && container.entries != null && container.entries.Count > 0)
                    {
                        foreach (var entry in container.entries)
                        {
                            if (!string.IsNullOrEmpty(entry.key))
                            {
                                configData[entry.key] = entry.GetValue();
                            }
                        }
                        FPLogger.Log("使用Newtonsoft.Json加载了 {0} 个配置项", configData.Count);
                        return;
                    }
                }
                catch (Exception jsonEx)
                {
                    FPLogger.LogWarning("Newtonsoft.Json反序列化失败，尝试其他方法: {0}", jsonEx.Message);
                }
                
                // 尝试使用JsonUtility解析（兼容旧格式）
                try
                {
                    ConfigDataContainer container = JsonUtility.FromJson<ConfigDataContainer>(jsonContent);
                    if (container != null && container.entries != null && container.entries.Count > 0)
                    {
                        foreach (var entry in container.entries)
                        {
                            if (!string.IsNullOrEmpty(entry.key))
                            {
                                configData[entry.key] = entry.GetValue();
                            }
                        }
                        FPLogger.Log("使用JsonUtility加载了 {0} 个配置项", configData.Count);
                        return;
                    }
                }
                catch
                {
                    // JsonUtility解析失败，尝试手动解析
                }

                // 手动解析JSON（处理手动构建的JSON格式）
                try
                {
                    // 使用正则表达式或简单的字符串解析来提取entries
                    // 简化方法：直接查找所有 "key":"..." 的模式
                    int entryCount = 0;
                    int searchPos = 0;
                    
                    while (true)
                    {
                        // 查找 "key":" 模式
                        int keyPos = jsonContent.IndexOf("\"key\":\"", searchPos);
                        if (keyPos < 0) break;
                        
                        keyPos += 7; // "\"key\":\"".Length
                        int keyEnd = jsonContent.IndexOf("\"", keyPos);
                        if (keyEnd < 0) break;
                        
                        string key = UnescapeJsonString(jsonContent.Substring(keyPos, keyEnd - keyPos));
                        
                        // 查找对应的 "value":" 模式（在key之后）
                        int valuePos = jsonContent.IndexOf("\"value\":\"", keyEnd);
                        if (valuePos < 0) break;
                        
                        valuePos += 9; // "\"value\":\"".Length
                        int valueEnd = jsonContent.IndexOf("\"", valuePos);
                        if (valueEnd < 0) break;
                        
                        string valueStr = UnescapeJsonString(jsonContent.Substring(valuePos, valueEnd - valuePos));
                        
                        // 查找对应的 "type":" 模式（在value之后）
                        int typePos = jsonContent.IndexOf("\"type\":\"", valueEnd);
                        if (typePos < 0) break;
                        
                        typePos += 9; // "\"type\":\"".Length
                        int typeEnd = jsonContent.IndexOf("\"", typePos);
                        if (typeEnd < 0) break;
                        
                        string typeStr = jsonContent.Substring(typePos, typeEnd - typePos);
                        
                        // 创建ConfigEntry并解析值
                        var entry = new ConfigEntry();
                        entry.key = key;
                        entry.value = valueStr;
                        entry.type = typeStr;
                        
                        if (!string.IsNullOrEmpty(key))
                        {
                            configData[key] = entry.GetValue();
                            entryCount++;
                        }
                        
                        searchPos = typeEnd + 1;
                    }
                    
                    FPLogger.Log("手动解析JSON加载了 {0} 个配置项", entryCount);
                }
                catch (Exception parseEx)
                {
                    FPLogger.LogError("手动解析JSON失败: {0}", parseEx.Message);
                    // 如果手动解析也失败，尝试JsonUtility作为后备
                    try
                    {
                        ConfigDataContainer container = JsonUtility.FromJson<ConfigDataContainer>(jsonContent);
                        if (container != null && container.entries != null)
                        {
                            foreach (var entry in container.entries)
                            {
                                if (!string.IsNullOrEmpty(entry.key))
                                {
                                    configData[entry.key] = entry.GetValue();
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "加载配置文件失败");
                configData = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 查找匹配的大括号
        /// </summary>
        private static int FindMatchingBrace(string json, int startPos)
        {
            int braceCount = 0;
            for (int i = startPos; i < json.Length; i++)
            {
                if (json[i] == '{') braceCount++;
                if (json[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0) return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 解析单个配置条目JSON
        /// </summary>
        private static ConfigEntry ParseConfigEntry(string entryJson)
        {
            try
            {
                var entry = new ConfigEntry();
                
                // 提取key
                int keyStart = entryJson.IndexOf("\"key\":\"");
                if (keyStart >= 0)
                {
                    keyStart += 7; // "\"key\":\"".Length
                    int keyEnd = entryJson.IndexOf("\"", keyStart);
                    if (keyEnd > keyStart)
                    {
                        entry.key = UnescapeJsonString(entryJson.Substring(keyStart, keyEnd - keyStart));
                    }
                }
                
                // 提取value
                int valueStart = entryJson.IndexOf("\"value\":\"");
                if (valueStart >= 0)
                {
                    valueStart += 9; // "\"value\":\"".Length
                    int valueEnd = entryJson.IndexOf("\"", valueStart);
                    if (valueEnd > valueStart)
                    {
                        entry.value = UnescapeJsonString(entryJson.Substring(valueStart, valueEnd - valueStart));
                    }
                }
                
                // 提取type
                int typeStart = entryJson.IndexOf("\"type\":\"");
                if (typeStart >= 0)
                {
                    typeStart += 9; // "\"type\":\"".Length
                    int typeEnd = entryJson.IndexOf("\"", typeStart);
                    if (typeEnd > typeStart)
                    {
                        entry.type = entryJson.Substring(typeStart, typeEnd - typeStart);
                    }
                }
                
                return entry;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 反转义JSON字符串
        /// </summary>
        private static string UnescapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return str.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r");
        }

        /// <summary>
        /// 判断配置键是否是KeyCode类型的键
        /// </summary>
        private static bool IsKeyCodeKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return key.EndsWith("KeyCode", System.StringComparison.OrdinalIgnoreCase) ||
                   key == "FirstPersonCamera_ToggleKeyCode" ||
                   key == "FirstPersonCamera_PeekLeftKeyCode" ||
                   key == "FirstPersonCamera_PeekRightKeyCode";
        }

        /// <summary>
        /// 手动构建JSON（备用方法）
        /// </summary>
        private static string BuildJsonManually(ConfigDataContainer container)
        {
            var jsonBuilder = new System.Text.StringBuilder();
            jsonBuilder.AppendLine("{");
            jsonBuilder.AppendLine("    \"entries\": [");
            for (int i = 0; i < container.entries.Count; i++)
            {
                var entry = container.entries[i];
                // 转义JSON特殊字符
                string escapedKey = entry.key.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                string escapedValue = entry.value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                
                jsonBuilder.Append("        {");
                jsonBuilder.Append($"\"key\":\"{escapedKey}\",");
                jsonBuilder.Append($"\"value\":\"{escapedValue}\",");
                jsonBuilder.Append($"\"type\":\"{entry.type}\"");
                jsonBuilder.Append(i < container.entries.Count - 1 ? "},\n" : "}\n");
            }
            jsonBuilder.AppendLine("    ]");
            jsonBuilder.Append("}");
            return jsonBuilder.ToString();
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        private static void SaveConfig()
        {
            if (string.IsNullOrEmpty(configFilePath))
            {
                FPLogger.LogWarning("配置文件路径为空，无法保存");
                return;
            }

            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    FPLogger.Log("创建配置目录: {0}", directory);
                }

                // 转换为可序列化的容器
                ConfigDataContainer container = new ConfigDataContainer();
                // 确保在创建ConfigEntry时，KeyCode类型的值被正确转换为整数
                container.entries = configData.Select(kvp => 
                {
                    object valueToSave = kvp.Value;
                    
                    // 如果键是KeyCode类型的键，但值是字符串（从旧版本加载的），尝试解析为KeyCode
                    if (IsKeyCodeKey(kvp.Key) && valueToSave is string strValue)
                    {
                        if (int.TryParse(strValue, out int intKey))
                        {
                            valueToSave = (KeyCode)intKey;
                        }
                        else if (System.Enum.TryParse<KeyCode>(strValue, true, out KeyCode parsedKey))
                        {
                            valueToSave = parsedKey;
                        }
                    }
                    
                    // 如果值是KeyCode类型，确保保存为整数格式
                    if (valueToSave is KeyCode)
                    {
                        // KeyCode会被ConvertToString转换为整数
                        return new ConfigEntry(kvp.Key, valueToSave);
                    }
                    return new ConfigEntry(kvp.Key, valueToSave);
                }).ToList();

                // 检查是否有数据
                if (container.entries == null || container.entries.Count == 0)
                {
                    FPLogger.LogWarning("配置数据为空，跳过保存。configData.Count={0}", configData.Count);
                    return;
                }

                // 调试：输出配置数据信息
                FPLogger.Log("准备保存配置，条目数: {0}", container.entries.Count);
                
                string jsonContent;
                
                // 使用Newtonsoft.Json进行序列化
                try
                {
                    var settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    jsonContent = JsonConvert.SerializeObject(container, settings);
                    FPLogger.Log("使用Newtonsoft.Json序列化成功，JSON长度: {0} 字符", jsonContent.Length);
                }
                catch (Exception jsonEx)
                {
                    FPLogger.LogWarning("Newtonsoft.Json序列化失败，回退到手动构建: {0}", jsonEx.Message);
                    // 回退到手动构建JSON
                    jsonContent = BuildJsonManually(container);
                }

                // 写入文件
                File.WriteAllText(configFilePath, jsonContent, Encoding.UTF8);
                
                // 验证文件是否写入成功
                if (File.Exists(configFilePath))
                {
                    long fileSize = new FileInfo(configFilePath).Length;
                    if (fileSize > 0)
                    {
                        FPLogger.Log("配置文件保存成功: {0} (大小: {1} 字节)", configFilePath, fileSize);
                    }
                    else
                    {
                        FPLogger.LogError("配置文件已创建但大小为0");
                    }
                }
                else
                {
                    FPLogger.LogError("配置文件写入失败，文件不存在");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                FPLogger.LogException(ex, string.Format("保存配置文件失败（权限不足），路径: {0}", configFilePath));
            }
            catch (DirectoryNotFoundException ex)
            {
                FPLogger.LogException(ex, string.Format("保存配置文件失败（目录不存在），路径: {0}", configFilePath));
            }
            catch (IOException ex)
            {
                FPLogger.LogException(ex, string.Format("保存配置文件失败（IO错误），路径: {0}", configFilePath));
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, string.Format("保存配置文件失败，路径: {0}", configFilePath));
            }
        }
        #endregion

        #region 公共API
        /// <summary>
        /// 加载配置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值（仅在首次加载且配置不存在时使用）</param>
        /// <param name="keepCurrentOnError">加载失败时是否保持当前值（如果存在）</param>
        /// <returns>配置值</returns>
        public static T Load<T>(string key, T defaultValue, bool keepCurrentOnError = true)
        {
            Initialize();

            lock (configLock)
            {
                try
                {
                    // 如果配置中存在该键，返回配置值（优先使用从文件加载的值）
                    if (configData.TryGetValue(key, out object value))
                    {
                        // 转换类型
                        T convertedValue = ConvertValue<T>(value);
                        
                        // 更新当前值缓存
                        currentValueCache[key] = convertedValue;
                        
                        return convertedValue;
                    }

                    // 如果配置中不存在，但当前值缓存中存在，返回当前值（避免跳变）
                    if (keepCurrentOnError && currentValueCache.TryGetValue(key, out object cachedValue))
                    {
                        // 确保缓存值也被添加到configData中（用于完整保存）
                        if (!configData.ContainsKey(key))
                        {
                            configData[key] = cachedValue;
                        }
                        return ConvertValue<T>(cachedValue);
                    }

                    // 否则返回默认值
                    // 注意：不要将默认值添加到configData中，因为这会覆盖从文件加载的值
                    // 只在currentValueCache中记录，用于后续保存
                    currentValueCache[key] = defaultValue;
                    return defaultValue;
                }
                catch (Exception ex)
                {
                    FPLogger.LogException(ex, string.Format("加载配置键 '{0}' 失败", key));
                    
                    // 加载失败时，如果启用保持当前值，尝试从缓存获取
                    if (keepCurrentOnError && currentValueCache.TryGetValue(key, out object cachedValue))
                    {
                        try
                        {
                            return ConvertValue<T>(cachedValue);
                        }
                        catch
                        {
                            // 转换失败，返回默认值
                        }
                    }
                    
                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// 保存配置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        public static void Save<T>(string key, T value)
        {
            Initialize();

            lock (configLock)
            {
                try
                {
                    // 更新内存中的配置
                    configData[key] = value;
                    
                    // 更新当前值缓存（确保缓存和configData同步）
                    currentValueCache[key] = value;
                    
                    // 保存到文件（每次保存都立即写入，确保数据不丢失）
                    SaveConfig();
                }
                catch (Exception ex)
                {
                    FPLogger.LogException(ex, string.Format("保存配置键 '{0}' 失败", key));
                }
            }
        }

        /// <summary>
        /// 转换值类型
        /// </summary>
        private static T ConvertValue<T>(object value)
        {
            if (value == null)
                return default(T);

            if (value is T directValue)
                return directValue;

            // 类型转换
            try
            {
                if (typeof(T) == typeof(int))
                {
                    if (value is float f)
                        return (T)(object)Mathf.RoundToInt(f);
                    return (T)Convert.ChangeType(value, typeof(int));
                }
                else if (typeof(T) == typeof(float))
                {
                    if (value is int i)
                        return (T)(object)(float)i;
                    return (T)Convert.ChangeType(value, typeof(float), System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (typeof(T) == typeof(bool))
                {
                    if (value is int intVal)
                        return (T)(object)(intVal != 0);
                    if (value is float floatVal)
                        return (T)(object)(Mathf.Abs(floatVal) > 0.0001f);
                    return (T)Convert.ChangeType(value, typeof(bool));
                }
                else if (typeof(T) == typeof(KeyCode))
                {
                    if (value is int intVal)
                        return (T)(object)(KeyCode)intVal;
                    if (value is float floatVal)
                        return (T)(object)(KeyCode)Mathf.RoundToInt(floatVal);
                    return (T)Convert.ChangeType(value, typeof(KeyCode));
                }
                else
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        public static string GetConfigFilePath()
        {
            Initialize();
            return configFilePath;
        }

        /// <summary>
        /// 检查目录可写（尝试创建并写入临时文件）
        /// </summary>
        private static bool TryEnsureWritable(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".write_test");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 清除所有配置（谨慎使用）
        /// </summary>
        public static void ClearAll()
        {
            Initialize();

            lock (configLock)
            {
                configData.Clear();
                currentValueCache.Clear();
                SaveConfig();
            }
        }

        /// <summary>
        /// 强制保存所有当前配置（包括默认值和动态保存的数据）
        /// 用于在UI初始化完成后、游戏退出时等场景保存所有配置项
        /// </summary>
        public static void SaveAll()
        {
            Initialize();

            lock (configLock)
            {
                // 将所有缓存的值也添加到configData中（确保所有通过Load加载的默认值都被保存）
                foreach (var kvp in currentValueCache)
                {
                    if (!configData.ContainsKey(kvp.Key))
                    {
                        configData[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        // 如果configData中已有值，但缓存中的值更新，则使用缓存的值
                        // 这确保动态修改的值（如武器偏移）会被保存
                        configData[kvp.Key] = kvp.Value;
                    }
                }

                // 保存到文件
                SaveConfig();
                
                FPLogger.Log("SaveAll完成：保存了 {0} 个配置项", configData.Count);
            }
        }
        #endregion
    }
}

