using System;
using System.Reflection;
using UnityEngine;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.Compatibility
{
    /// <summary>
    /// RandomNpc Mod 对话气泡兼容性支持
    /// 通过反射订阅 RandomNpc 的对话气泡事件，并在第一人称模式下显示
    /// </summary>
    public class ScavDialogueBubbleCompatibility
    {
        private static ScavDialogueBubbleCompatibility _instance;
        private static EventInfo _dialogueBubbleEvent;
        private static MethodInfo _eventSubscribeMethod;
        private static MethodInfo _eventUnsubscribeMethod;
        private Delegate _cachedHandler; // 保存委托实例，用于取消订阅
        
        /// <summary>
        /// 对话气泡消息事件
        /// </summary>
        public static event Action<string> OnDialogueBubbleReceived;
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static ScavDialogueBubbleCompatibility Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ScavDialogueBubbleCompatibility();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// 是否可用（RandomNpc 程序集存在）
        /// </summary>
        public bool IsAvailable { get; private set; }
        
        private ScavDialogueBubbleCompatibility()
        {
            IsInitialized = false;
            IsAvailable = false;
        }
        
        /// <summary>
        /// 初始化兼容性支持（延迟初始化，在游戏加载后调用）
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            
            try
            {
                // 查找 RandomNpc 程序集
                Assembly randomNpcAssembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == "RandomNpc")
                    {
                        randomNpcAssembly = assembly;
                        break;
                    }
                }
                
                if (randomNpcAssembly == null)
                {
                    FPLogger.Log("RandomNpc 程序集未找到，跳过对话气泡兼容性支持");
                    IsAvailable = false;
                    IsInitialized = true;
                    return;
                }
                
                // 获取 ScavDialogueBubbleEventBroadcaster 类型
                Type broadcasterType = randomNpcAssembly.GetType("RandomNpc.ScavDialogueBubbleEventBroadcaster");
                if (broadcasterType == null)
                {
                    FPLogger.LogWarning("未找到 ScavDialogueBubbleEventBroadcaster 类型");
                    IsAvailable = false;
                    IsInitialized = true;
                    return;
                }
                
                // 获取事件
                _dialogueBubbleEvent = broadcasterType.GetEvent("OnDialogueBubbleShown");
                if (_dialogueBubbleEvent == null)
                {
                    FPLogger.LogWarning("未找到 OnDialogueBubbleShown 事件");
                    IsAvailable = false;
                    IsInitialized = true;
                    return;
                }
                
                // 获取事件参数类型
                Type eventArgsType = randomNpcAssembly.GetType("RandomNpc.DialogueBubbleEventArgs");
                if (eventArgsType == null)
                {
                    FPLogger.LogWarning("未找到 DialogueBubbleEventArgs 类型");
                    IsAvailable = false;
                    IsInitialized = true;
                    return;
                }
                
                // 获取处理方法的反射信息
                MethodInfo handlerMethod = typeof(ScavDialogueBubbleCompatibility)
                    .GetMethod("OnDialogueBubbleReceivedInternal", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (handlerMethod == null)
                {
                    FPLogger.LogError("未找到 OnDialogueBubbleReceivedInternal 方法");
                    IsAvailable = false;
                    IsInitialized = true;
                    return;
                }
                
                // 创建委托并保存
                _cachedHandler = Delegate.CreateDelegate(_dialogueBubbleEvent.EventHandlerType, this, handlerMethod);
                
                // 获取订阅和取消订阅方法
                _eventSubscribeMethod = broadcasterType.GetMethod("add_OnDialogueBubbleShown");
                _eventUnsubscribeMethod = broadcasterType.GetMethod("remove_OnDialogueBubbleShown");
                
                if (_eventSubscribeMethod == null || _eventUnsubscribeMethod == null)
                {
                    FPLogger.LogError("未找到事件订阅/取消订阅方法");
                    IsAvailable = false;
                    IsInitialized = true;
                    return;
                }
                
                // 订阅事件（静态事件，传入 null）
                _eventSubscribeMethod.Invoke(null, new object[] { _cachedHandler });
                
                IsAvailable = true;
                IsInitialized = true;
                FPLogger.Log("已成功订阅 RandomNpc 对话气泡事件");
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "初始化 RandomNpc 对话气泡兼容性支持失败");
                IsAvailable = false;
                IsInitialized = true;
            }
        }
        
        /// <summary>
        /// 清理资源（取消订阅事件）
        /// </summary>
        public void Cleanup()
        {
            if (!IsAvailable || _eventUnsubscribeMethod == null || _cachedHandler == null)
            {
                return;
            }
            
            try
            {
                // 取消订阅事件（使用保存的委托实例）
                _eventUnsubscribeMethod.Invoke(null, new object[] { _cachedHandler });
                _cachedHandler = null;
                FPLogger.Log("已取消订阅 RandomNpc 对话气泡事件");
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "取消订阅 RandomNpc 对话气泡事件失败");
            }
            
            IsAvailable = false;
        }
        
        /// <summary>
        /// 内部事件处理方法（通过反射调用）
        /// </summary>
        private void OnDialogueBubbleReceivedInternal(object eventArgs)
        {
            try
            {
                if (eventArgs == null)
                {
                    return;
                }
                
                // 获取事件参数类型
                Type eventArgsType = eventArgs.GetType();
                
                // 获取 Message 属性
                PropertyInfo messageProperty = eventArgsType.GetProperty("Message");
                if (messageProperty == null)
                {
                    FPLogger.LogWarning("未找到 DialogueBubbleEventArgs.Message 属性");
                    return;
                }
                
                // 获取消息内容
                string message = messageProperty.GetValue(eventArgs) as string;
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }
                
                // 触发事件
                OnDialogueBubbleReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "处理对话气泡事件时出错");
            }
        }
    }
}

