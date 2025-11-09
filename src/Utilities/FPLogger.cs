using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace FirstPersonCamera.Utilities
{
    /// <summary>
    /// FirstPersonCamera专用的日志记录器
    /// 将日志输出到独立的日志文件，避免与其他mod冲突
    /// </summary>
    public static class FPLogger
    {
        private static string logFilePath;
        private static bool initialized = false;
        private static readonly object lockObject = new object();
        private static StreamWriter logWriter;
        private static bool enableFileLogging = true;
        private static bool enableUnityLogging = true; // 同时输出到Unity控制台，方便调试

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        private static void Initialize()
        {
            if (initialized) return;

            lock (lockObject)
            {
                if (initialized) return;

                try
                {
                    // 使用与配置文件相同的目录结构
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string localLowPath = Path.Combine(userProfile, "AppData", "LocalLow");
                    string logDir = Path.Combine(localLowPath, "TeamSoda", "Duckov", "FirstPersonCamera");

                    // 确保日志目录存在
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }

                    // 创建日志文件名（带时间戳）
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string logFileName = $"FirstPersonCamera_{timestamp}.log";

                    // 如果文件已存在，添加序号
                    int fileIndex = 0;
                    string baseFileName = logFileName;
                    while (File.Exists(Path.Combine(logDir, logFileName)))
                    {
                        fileIndex++;
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(baseFileName);
                        string ext = Path.GetExtension(baseFileName);
                        logFileName = $"{nameWithoutExt}_{fileIndex}{ext}";
                    }

                    logFilePath = Path.Combine(logDir, logFileName);

                    // 打开日志文件流（追加模式）
                    FileStream fileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    logWriter = new StreamWriter(fileStream, Encoding.UTF8)
                    {
                        AutoFlush = true // 自动刷新，确保日志及时写入
                    };

                    // 写入日志文件头
                    WriteToFile($"[{GetTimestamp()}] ========== FirstPersonCamera Log Started ==========");
                    WriteToFile($"[{GetTimestamp()}] Log File: {logFilePath}");
                    WriteToFile($"[{GetTimestamp()}] Unity Version: {Application.unityVersion}");
                    WriteToFile($"[{GetTimestamp()}] Application Version: {Application.version}");
                    WriteToFile($"[{GetTimestamp()}] Platform: {Application.platform}");
                    WriteToFile($"[{GetTimestamp()}] ====================================================");
                    WriteToFile("");

                    initialized = true;

                    // 输出初始化信息到Unity控制台（如果启用）
                    if (enableUnityLogging)
                    {
                        Debug.Log($"[FirstPersonCamera] 日志系统已初始化，日志文件: {logFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    // 如果文件日志初始化失败，至少输出到Unity控制台
                    Debug.LogError($"[FirstPersonCamera] 日志系统初始化失败: {ex.Message}");
                    enableFileLogging = false;
                    initialized = true; // 标记为已初始化，避免重复尝试
                }
            }
        }

        /// <summary>
        /// 获取时间戳字符串
        /// </summary>
        private static string GetTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// 写入日志到文件
        /// </summary>
        private static void WriteToFile(string message)
        {
            if (!enableFileLogging || logWriter == null) return;

            try
            {
                lock (lockObject)
                {
                    logWriter.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                // 如果写入失败，尝试输出到Unity控制台
                try
                {
                    Debug.LogError($"[FirstPersonCamera] 写入日志文件失败: {ex.Message}");
                }
                catch { }
            }
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Log(string message)
        {
            Initialize();
            string logMessage = $"[{GetTimestamp()}] [INFO] {message}";
            
            if (enableFileLogging)
            {
                WriteToFile(logMessage);
            }
            
            if (enableUnityLogging)
            {
                Debug.Log($"[FirstPersonCamera] {message}");
            }
        }

        /// <summary>
        /// 记录信息日志（带格式）
        /// </summary>
        public static void Log(string format, params object[] args)
        {
            try
            {
                Log(string.Format(format, args));
            }
            catch
            {
                Log(format); // 如果格式化失败，直接输出原始格式
            }
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void LogWarning(string message)
        {
            Initialize();
            string logMessage = $"[{GetTimestamp()}] [WARN] {message}";
            
            if (enableFileLogging)
            {
                WriteToFile(logMessage);
            }
            
            if (enableUnityLogging)
            {
                Debug.LogWarning($"[FirstPersonCamera] {message}");
            }
        }

        /// <summary>
        /// 记录警告日志（带格式）
        /// </summary>
        public static void LogWarning(string format, params object[] args)
        {
            try
            {
                LogWarning(string.Format(format, args));
            }
            catch
            {
                LogWarning(format); // 如果格式化失败，直接输出原始格式
            }
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void LogError(string message)
        {
            Initialize();
            string logMessage = $"[{GetTimestamp()}] [ERROR] {message}";
            
            if (enableFileLogging)
            {
                WriteToFile(logMessage);
            }
            
            if (enableUnityLogging)
            {
                Debug.LogError($"[FirstPersonCamera] {message}");
            }
        }

        /// <summary>
        /// 记录错误日志（带格式）
        /// </summary>
        public static void LogError(string format, params object[] args)
        {
            try
            {
                LogError(string.Format(format, args));
            }
            catch
            {
                LogError(format); // 如果格式化失败，直接输出原始格式
            }
        }

        /// <summary>
        /// 记录异常日志
        /// </summary>
        public static void LogException(Exception exception, string context = "")
        {
            Initialize();
            string logMessage = $"[{GetTimestamp()}] [EXCEPTION] {context}";
            
            if (enableFileLogging)
            {
                WriteToFile(logMessage);
                WriteToFile($"Exception Type: {exception.GetType().Name}");
                WriteToFile($"Exception Message: {exception.Message}");
                WriteToFile($"Stack Trace: {exception.StackTrace}");
                
                // 如果有内部异常，也记录
                Exception innerEx = exception.InnerException;
                int depth = 0;
                while (innerEx != null && depth < 5) // 限制深度，避免无限递归
                {
                    WriteToFile($"Inner Exception [{depth}]: {innerEx.GetType().Name} - {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                    depth++;
                }
            }
            
            if (enableUnityLogging)
            {
                if (!string.IsNullOrEmpty(context))
                {
                    Debug.LogError($"[FirstPersonCamera] {context}: {exception}");
                }
                else
                {
                    Debug.LogError($"[FirstPersonCamera] {exception}");
                }
            }
        }

        /// <summary>
        /// 刷新日志缓冲区
        /// </summary>
        public static void Flush()
        {
            if (logWriter != null)
            {
                try
                {
                    lock (lockObject)
                    {
                        logWriter.Flush();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public static void Shutdown()
        {
            lock (lockObject)
            {
                if (logWriter != null)
                {
                    try
                    {
                        WriteToFile($"[{GetTimestamp()}] ========== FirstPersonCamera Log Ended ==========");
                        logWriter.Flush();
                        logWriter.Close();
                        logWriter.Dispose();
                        logWriter = null;
                    }
                    catch { }
                }
                initialized = false;
            }
        }

        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public static string GetLogFilePath()
        {
            Initialize();
            return logFilePath;
        }

        /// <summary>
        /// 设置是否启用文件日志（默认启用）
        /// </summary>
        public static void SetFileLoggingEnabled(bool enabled)
        {
            enableFileLogging = enabled;
        }

        /// <summary>
        /// 设置是否启用Unity控制台日志（默认启用）
        /// </summary>
        public static void SetUnityLoggingEnabled(bool enabled)
        {
            enableUnityLogging = enabled;
        }
    }
}

