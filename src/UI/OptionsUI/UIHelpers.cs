using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPersonCamera.OptionsUI
{
    /// <summary>
    /// UI辅助方法
    /// 提供反射相关的辅助方法，用于访问和设置私有字段
    /// </summary>
    public static class UIHelpers
    {
        /// <summary>
        /// 设置对象的私有字段值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="value">要设置的值</param>
        public static void SetPrivateField(object obj, string fieldName, object value)
        {
            if (obj == null) return;
            
            var field = obj.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        /// <summary>
        /// 获取对象的私有字段值
        /// </summary>
        /// <typeparam name="T">字段类型</typeparam>
        /// <param name="obj">目标对象</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>字段值，如果未找到则返回null</returns>
        public static T GetPrivateField<T>(object obj, string fieldName) where T : class
        {
            if (obj == null) return null;
            
            var field = obj.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            return field != null ? field.GetValue(obj) as T : null;
        }

        /// <summary>
        /// 调用对象的私有方法
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="methodName">方法名</param>
        /// <param name="bindingFlags">绑定标志</param>
        /// <param name="parameters">方法参数</param>
        public static void InvokePrivateMethod(
            object obj,
            string methodName,
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance,
            params object[] parameters)
        {
            if (obj == null) return;
            
            var method = obj.GetType().GetMethod(methodName, bindingFlags);
            method?.Invoke(obj, parameters);
        }
    }
}

