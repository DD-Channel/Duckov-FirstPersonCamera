using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using FirstPersonCamera.UI; 

namespace FirstPersonCamera.Utilities
{
    public static class FPLocalization
    {
        private static readonly Dictionary<SystemLanguage, string> languageFileMap = new Dictionary<SystemLanguage, string>
        {
            { SystemLanguage.Chinese, "zh-CN.json" },
            { SystemLanguage.ChineseSimplified, "zh-CN.json" },
            { SystemLanguage.ChineseTraditional, "zh-CN.json" },
            { SystemLanguage.English, "en-US.json" },
            { SystemLanguage.Japanese, "ja-JP.json" },
            { SystemLanguage.Korean, "ko-KR.json" },
            { SystemLanguage.Portuguese, "pt-BR.json" },
            { SystemLanguage.Russian, "ru-RU.json" },
            { SystemLanguage.German, "de-DE.json" },
        };

        private static Dictionary<string, string> currentDict = new Dictionary<string, string>();
        private static string localizationFolderPath;
        private static SystemLanguage currentLanguage = SystemLanguage.English;

        public static event Action OnLanguageChanged;
        public static string CurrentLanguageCode { get; private set; } = "en-US";

        public static void Initialize(string configDirectory)
        {
            Debug.Log("[FPLocalization] Initialize called.");

            string modRoot = GetModRootDirectory();
            string modLocalizationPath = Path.Combine(modRoot, "Localization");
            if (Directory.Exists(modLocalizationPath))
            {
                localizationFolderPath = modLocalizationPath;
                // Debug.Log($"[FPLocalization] Using Mod Localization folder: {localizationFolderPath}");
            }
            else
            {
                localizationFolderPath = Path.Combine(configDirectory, "Localization");
                if (!Directory.Exists(localizationFolderPath))
                    Directory.CreateDirectory(localizationFolderPath);
                // Debug.Log($"[FPLocalization] Using Config Localization folder: {localizationFolderPath}");
            }

            SubscribeToLanguageChange();

            SystemLanguage gameLang = GetCurrentGameLanguage();
            // Debug.Log($"[FPLocalization] Current game language detected: {gameLang}");
            SetLanguage(gameLang);
        }

        private static string GetModRootDirectory()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyPath);
        }

        private static void SubscribeToLanguageChange()
        {
            try
            {
                Type managerType = Type.GetType("SodaCraft.Localizations.LocalizationManager, SodaLocalization");
                if (managerType == null)
                {
                    Debug.LogError("[FPLocalization] Cannot find LocalizationManager type.");
                    return;
                }

                EventInfo eventInfo = managerType.GetEvent("OnSetLanguage");
                if (eventInfo == null)
                {
                    // Debug.LogError("[FPLocalization] OnSetLanguage event not found.");
                    return;
                }

                Action<SystemLanguage> handler = OnGameLanguageChanged;
                Delegate del = Delegate.CreateDelegate(eventInfo.EventHandlerType, handler.Target, handler.Method);
                eventInfo.AddEventHandler(null, del);
                // Debug.Log("[FPLocalization] Successfully subscribed to OnSetLanguage event.");
            }
            catch (Exception ex)
            {
                // Debug.LogError($"[FPLocalization] Failed to subscribe: {ex.Message}");
            }
        }

        private static SystemLanguage GetCurrentGameLanguage()
        {
            try
            {
                Type managerType = Type.GetType("SodaCraft.Localizations.LocalizationManager, SodaLocalization");
                if (managerType == null) return Application.systemLanguage;
                PropertyInfo prop = managerType.GetProperty("CurrentLanguage");
                if (prop != null)
                {
                    return (SystemLanguage)prop.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                // Debug.LogError($"[FPLocalization] GetCurrentGameLanguage error: {ex.Message}");
            }
            return Application.systemLanguage;
        }

        private static void OnGameLanguageChanged(SystemLanguage newLang)
        {
            // Debug.Log($"[FPLocalization] OnGameLanguageChanged called with {newLang}");
            SetLanguage(newLang);

            // 全局重建所有 Canvas 布局
            foreach (var canvas in GameObject.FindObjectsOfType<Canvas>())
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
            }

            // 强制刷新所有 LocalizedText 组件（备用）
            foreach (var lt in GameObject.FindObjectsOfType<LocalizedText>())
            {
                lt.UpdateText();
            }
        }

        public static void SetLanguage(SystemLanguage lang)
        {
            // Debug.Log($"[FPLocalization] SetLanguage({lang}) called. Current language was {currentLanguage}");
            if (currentLanguage == lang && currentDict.Count > 0)
            {
                // Debug.Log("[FPLocalization] Language unchanged and already loaded, skipping.");
                return;
            }

            currentLanguage = lang;

            if (!languageFileMap.TryGetValue(lang, out string fileName))
            {
                fileName = "en-US.json";
                currentLanguage = SystemLanguage.English;
                // Debug.Log($"[FPLocalization] No mapping for {lang}, falling back to English (en-US.json)");
            }

            CurrentLanguageCode = fileName.Replace(".json", "");
            LoadLanguageFile(fileName);
            // Debug.Log("[FPLocalization] Invoking OnLanguageChanged event.");
            OnLanguageChanged?.Invoke();
        }

        private static void LoadLanguageFile(string fileName)
        {
            string filePath = Path.Combine(localizationFolderPath, fileName);
            // Debug.Log($"[FPLocalization] Attempting to load language file: {filePath}");
            if (!File.Exists(filePath))
            {
                // Debug.LogWarning($"[FPLocalization] Language file not found: {filePath}, using fallback.");
                currentDict.Clear();
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    currentDict = dict;
                    // Debug.Log($"[FPLocalization] Loaded {currentDict.Count} entries from {fileName}");
                }
                else
                {
                    // Debug.LogError($"[FPLocalization] Failed to parse JSON from {fileName}");
                    currentDict.Clear();
                }
            }
            catch (Exception ex)
            {
                // Debug.LogError($"[FPLocalization] Error loading {fileName}: {ex.Message}");
                currentDict.Clear();
            }
        }

        public static string Get(string key, params object[] args)
        {
            if (currentDict.TryGetValue(key, out string value))
            {
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        return string.Format(value, args);
                    }
                    catch
                    {
                        return value;
                    }
                }
                return value;
            }
            return $"[{key}]";
        }
    }
}