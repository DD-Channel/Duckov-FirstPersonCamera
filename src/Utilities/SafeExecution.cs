using System;

namespace FirstPersonCamera.Utilities
{
    /// <summary>
    /// 安全执行辅助类
    /// 提供统一的安全执行方法，减少重复的try-catch代码
    /// </summary>
    public static class SafeExecution
    {
        /// <summary>
        /// 安全执行操作，忽略异常
        /// </summary>
        /// <param name="action">要执行的操作</param>
        public static void Execute(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch
            {
                // 静默处理异常
            }
        }

        /// <summary>
        /// 安全执行操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="defaultValue">默认值（如果执行失败）</param>
        /// <returns>执行结果，如果失败则返回默认值</returns>
        public static T Execute<T>(Func<T> func, T defaultValue = default(T))
        {
            try
            {
                return func != null ? func() : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 安全执行操作，带异常处理回调
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="onException">异常处理回调</param>
        public static void Execute(Action action, Action<Exception> onException)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        }
    }
}

