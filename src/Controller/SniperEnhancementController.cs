using System;
using System.Collections.Generic;
using System.Reflection;
using FirstPersonCamera.Utilities;
using ItemStatsSystem;
using UnityEngine;

namespace FirstPersonCamera
{
    public class SniperEnhancementController : MonoBehaviour
    {
        private FirstPersonCameraController fpsController;
        private bool enhanceEnabled;
        private ItemAgent_Gun currentGun;
        private bool isSniper;

        private float lastDistanceMult;
        private float lastSpeedMult;
        private float lastADSTimeMult;

        // 哈希字段名称
        private readonly string[] hashFieldNames = new string[]
        {
            "BulletDistanceHash",
            "BulletSpeedHash",
            "AdsTimeHash"
        };

        // 对应的 Stat 键（用于日志）
        private readonly string[] statKeys = new string[]
        {
            "BulletDistance",
            "BulletSpeed",
            "ADSTime"
        };

        // 存储每个 Stat 的原始 BaseValue
        private Dictionary<string, float> originalBaseValues = new Dictionary<string, float>();

        // 狙击枪 ID 列表（请根据实际游戏填写）
        private readonly HashSet<int> sniperIDs = new HashSet<int>
        {
            246,407,437,780,781,782,1480,1497,10111,10115,10142,10143,10146,95605,95604,95616,95805,95808
        };

        private void Awake()
        {
            fpsController = GetComponent<FirstPersonCameraController>();
            if (fpsController == null)
            {
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            LoadFromOptions();
            lastDistanceMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperDistanceMultiplierKey,
                OptionsUIConstants.SniperDistanceMultiplierDefault);
            lastSpeedMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperBulletSpeedMultiplierKey,
                OptionsUIConstants.SniperBulletSpeedMultiplierDefault);
            lastADSTimeMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperADSTimeMultiplierKey,
                OptionsUIConstants.SniperADSTimeMultiplierDefault);
        }

        private void OnDisable()
        {
            RestoreAll();
        }

        private void LoadFromOptions()
        {
            enhanceEnabled = OptionsHelper.LoadInt(OptionsUIConstants.SniperEnhanceEnabledKey, 0) == 1;
        }

        private void Update()
        {
            bool newEnhanceEnabled = OptionsHelper.LoadInt(OptionsUIConstants.SniperEnhanceEnabledKey, 0) == 1;
            if (newEnhanceEnabled != enhanceEnabled)
            {
                enhanceEnabled = newEnhanceEnabled;
                ApplyToCurrentGun();
            }

            var gun = fpsController.GetCurrentGun();
            if (gun != currentGun)
            {
                RestoreAll();
                currentGun = gun;
                CheckIfSniper();
                if (enhanceEnabled && isSniper)
                {
                    lastDistanceMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperDistanceMultiplierKey,
                        OptionsUIConstants.SniperDistanceMultiplierDefault);
                    lastSpeedMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperBulletSpeedMultiplierKey,
                        OptionsUIConstants.SniperBulletSpeedMultiplierDefault);
                    lastADSTimeMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperADSTimeMultiplierKey,
                        OptionsUIConstants.SniperADSTimeMultiplierDefault);
                    ApplyModifiers();
                }
            }

            if (enhanceEnabled && isSniper && currentGun != null)
            {
                float distanceMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperDistanceMultiplierKey,
                    OptionsUIConstants.SniperDistanceMultiplierDefault);
                float speedMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperBulletSpeedMultiplierKey,
                    OptionsUIConstants.SniperBulletSpeedMultiplierDefault);
                float adsTimeMult = OptionsHelper.LoadFloat(OptionsUIConstants.SniperADSTimeMultiplierKey,
                    OptionsUIConstants.SniperADSTimeMultiplierDefault);

                bool changed = false;
                if (Math.Abs(distanceMult - lastDistanceMult) > 0.001f)
                {
                    lastDistanceMult = distanceMult;
                    changed = true;
                }
                if (Math.Abs(speedMult - lastSpeedMult) > 0.001f)
                {
                    lastSpeedMult = speedMult;
                    changed = true;
                }
                if (Math.Abs(adsTimeMult - lastADSTimeMult) > 0.001f)
                {
                    lastADSTimeMult = adsTimeMult;
                    changed = true;
                }

                if (changed)
                {
                    ApplyModifiers();
                }
            }
        }

        private void CheckIfSniper()
        {
            isSniper = false;
            if (currentGun != null && currentGun.Item != null)
            {
                isSniper = sniperIDs.Contains(currentGun.Item.TypeID);
            }
        }

        private void ApplyToCurrentGun()
        {
            if (!enhanceEnabled || !isSniper || currentGun == null || currentGun.Item == null)
            {
                RestoreAll();
                return;
            }
            ApplyModifiers();
        }

        private void ApplyModifiers()
        {
            if (currentGun == null || currentGun.Item == null) return;

            float[] multipliers = new float[] { lastDistanceMult, lastSpeedMult, lastADSTimeMult };

            for (int i = 0; i < hashFieldNames.Length; i++)
            {
                string hashFieldName = hashFieldNames[i];
                float multiplier = multipliers[i];
                string statKey = statKeys[i];

                try
                {
                    int hash = GetHashFromStaticField(hashFieldName);
                    if (hash == 0)
                    {
                        FPLogger.LogWarning($"[狙击增强] 无法获取哈希字段 {hashFieldName}");
                        continue;
                    }

                    Stat stat = currentGun.Item.GetStat(hash);
                    if (stat == null)
                    {
                        FPLogger.LogWarning($"[狙击增强] 找不到统计属性 {statKey} (hash={hash})");
                        continue;
                    }

                    // 保存原始值（如果尚未保存）
                    if (!originalBaseValues.ContainsKey(statKey))
                    {
                        originalBaseValues[statKey] = stat.BaseValue;
                        FPLogger.Log($"[狙击增强] 保存原始 {statKey}: {originalBaseValues[statKey]}");
                    }

                    // 应用倍率
                    float newValue = originalBaseValues[statKey] * multiplier;
                    stat.BaseValue = newValue;
                    FPLogger.Log($"[狙击增强] 应用 {statKey}: {originalBaseValues[statKey]} * {multiplier} = {newValue}");
                }
                catch (Exception ex)
                {
                    FPLogger.LogException(ex, $"[狙击增强] 应用失败：{hashFieldName}");
                }
            }
        }

        private void RestoreAll()
        {
            if (currentGun == null || currentGun.Item == null) return;

            foreach (var kvp in originalBaseValues)
            {
                try
                {
                    string statKey = kvp.Key;
                    float originalValue = kvp.Value;

                    int hash = GetHashFromStatKey(statKey);
                    if (hash == 0) continue;

                    Stat stat = currentGun.Item.GetStat(hash);
                    if (stat != null)
                    {
                        stat.BaseValue = originalValue;
                        FPLogger.Log($"[狙击增强] 恢复 {statKey}: {originalValue}");
                    }
                }
                catch (Exception ex)
                {
                    FPLogger.LogException(ex, $"[狙击增强] 恢复属性失败：{kvp.Key}");
                }
            }
            originalBaseValues.Clear();
        }

        private int GetHashFromStaticField(string fieldName)
        {
            try
            {
                FieldInfo field = typeof(ItemAgent_Gun).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null && field.FieldType == typeof(int))
                {
                    int hash = (int)field.GetValue(null);
                    FPLogger.Log($"[狙击增强] 获取哈希 {fieldName} = {hash}");
                    return hash;
                }
                else
                {
                    FPLogger.LogWarning($"[狙击增强] 字段 {fieldName} 不存在或类型不正确");
                }
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, $"[狙击增强] 获取哈希字段 {fieldName} 失败");
            }
            return 0;
        }

        private int GetHashFromStatKey(string statKey)
        {
            if (statKey == "BulletDistance")
                return GetHashFromStaticField("BulletDistanceHash");
            if (statKey == "BulletSpeed")
                return GetHashFromStaticField("BulletSpeedHash");
            if (statKey == "ADSTime")
                return GetHashFromStaticField("AdsTimeHash");
            return 0;
        }
    }
}