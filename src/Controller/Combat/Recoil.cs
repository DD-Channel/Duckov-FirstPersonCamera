using UnityEngine;
using FirstPersonCamera.Utilities;
using FirstPersonCamera.OptionsUI;
using System.Collections.Generic;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 后坐力系统字段
        private float accumulatedRecoilV = 0f;          // 累积的垂直后坐力，用于连续射击时的累加和回弹计算
        private float accumulatedRecoilH = 0f;          // 累积的水平后坐力，用于连续射击时的累加和回弹计算
        private ItemAgent_Gun currentRecoilGun = null;  // 当前正在使用的武器，用于检测武器切换时重置后坐力状态
        private int horizontalRecoilPattern = 0;         // 水平后坐力模式计数器，用于控制方向延续/反转的节奏
        private float lastHorizontalDirection = 1.2f;      // 上一次的水平后坐力方向（1=右，-1=左），用于方向延续逻辑
        private bool isShooting = false;                  // 是否正在射击（用于控制后坐力累积和恢复）
        private List<float> recentRecoilV = new List<float>(); // 最近几发子弹的垂直后坐力记录，用于松开鼠标时保留部分后坐力
        private List<float> recentRecoilH = new List<float>(); // 最近几发子弹的水平后坐力记录
        private const int MAX_RECENT_RECOIL = 3;          // 最大保留的最近子弹后坐力数量
        private const int HORIZONTAL_RECOIL_PATTERN_RESET = 10; // 水平后坐力模式重置的阈值，每N发重置模式计数器
        private const float HORIZONTAL_DIRECTION_CONTINUE_CHANCE = 0.7f; // 水平后坐力方向延续上次的概率
        private const float HORIZONTAL_DIRECTION_RANDOM_CHANCE = 0.5f;   // 水平后坐力方向完全反转的额外概率
        private const float GAUSSIAN_STD_DEV = 0.5f;      // 高斯随机分布的标准差
        private const float GAUSSIAN_BASE_MULTIPLIER = 1.35f; // 水平后坐力基础乘数
        private const float GAUSSIAN_RANGE_MULTIPLIER = 0.6f; // 高斯因子对水平后坐力大小的最大影响范围

        // ========== 霰弹枪专用后坐力乘数 ==========
        private float shotgunVerticalRecoilMultiplier = 6.0f;  // 霰弹枪垂直后坐力增强倍数
        private float shotgunHorizontalRecoilMultiplier = 2.0f; // 霰弹枪水平后坐力乘数

        // ========== 冲锋枪专用后坐力乘数 ==========
        private float smgVerticalRecoilMultiplier = 3f;      // 冲锋枪垂直后坐力增强倍数
        private float smgHorizontalRecoilMultiplier = 25f;    // 冲锋枪水平后坐力乘数

        // ========== 枪械类型识别 ==========
        // 冲锋枪 TypeID 列表（请根据实际游戏修改）
        private HashSet<int> smgTypeIDs = new HashSet<int>
        {
            // 示例：MP7 改进型 (10116), UZI 改进型 (10117), 野牛 改进型 (10118), Vector (10120)
            10116, 10117, 10118, 10120,
            // 添加其他冲锋枪 TypeID
        };

        // ========== 新增：后坐力恢复暂停标志 ==========
        private bool recoveryPaused = false; // 武器切换后暂停恢复，直到再次射击
        #endregion

        #region 后坐力应用逻辑
        private void HandleMouseReleaseRecoilReset()
        {
            if (!isShooting) return;

            // ========== 霰弹枪不自动回弹 ==========
            if (currentRecoilGun != null && currentRecoilGun.ShotCount > 1)
            {
                // 霰弹枪不重置后坐力，保持累积状态
                return;
            }

            isShooting = false;

            float last3RecoilV = 0f;
            float last3RecoilH = 0f;
            for (int i = 0; i < recentRecoilV.Count; i++)
            {
                last3RecoilV += recentRecoilV[i];
                last3RecoilH += recentRecoilH[i];
            }
            accumulatedRecoilV = last3RecoilV;
            accumulatedRecoilH = last3RecoilH;
            recentRecoilV.Clear();
            recentRecoilH.Clear();
        }

        private void OnCharacterShoot(DuckovItemAgent agent)
        {
            if (!isFirstPersonMode) return;
            if (agent == null) return;

            ItemAgent_Gun gun = agent as ItemAgent_Gun;
            if (gun == null) return;

            try
            {
                bool recoilEnabled = OptionsHelper.LoadInt(FirstPersonOptionsUI.EnableRecoilKey, 1) == 1;
                if (!recoilEnabled) return;

                float recoilMult = 1f;
                try { recoilMult = LevelManager.Rule.RecoilMultiplier; } catch { }

                float strengthMult = OptionsHelper.LoadFloat(FirstPersonOptionsUI.RecoilStrengthKey, 0.1f);

                // ========== 原版后坐力基础值（映射到合理范围） ==========
                float origVMin = gun.RecoilVMin;
                float origVMax = gun.RecoilVMax;
                float origHMin = gun.RecoilHMin;
                float origHMax = gun.RecoilHMax;

                float baseV = MapRecoilValue(UnityEngine.Random.Range(origVMin, origVMax));
                float baseH = MapRecoilValue(UnityEngine.Random.Range(origHMin, origHMax));

                // ========== 散布动态因子 ==========
                float currentScatter = gun.CurrentScatter;
                float defaultScatter = gun.DefaultScatter;
                float maxScatter = gun.MaxScatter;
                float scatterGrow = gun.ScatterGrow;

                // 当前散布相对于默认散布的比例（0~1）
                float scatterRatio = Mathf.Clamp01((currentScatter - defaultScatter) / Mathf.Max(0.01f, maxScatter - defaultScatter));

                // 散布因子：基础1，随着散布增大而增大（最大2倍）
                float scatterFactor = 1f + scatterRatio * 1f; // 可调系数

                // ========== 垂直后坐力计算 ==========
                float v = baseV * scatterFactor * gun.RecoilScaleV * (1f / Mathf.Max(0.01f, gun.CharacterRecoilControl)) * recoilMult * strengthMult;

                // ========== 水平后坐力方向 ==========
                float hDirection;
                if (horizontalRecoilPattern == 0)
                {
                    hDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                }
                else
                {
                    float patternChance = UnityEngine.Random.value;
                    if (patternChance < HORIZONTAL_DIRECTION_CONTINUE_CHANCE)
                    {
                        hDirection = lastHorizontalDirection;
                    }
                    else
                    {
                        hDirection = -lastHorizontalDirection;
                    }

                    if (UnityEngine.Random.value < HORIZONTAL_DIRECTION_RANDOM_CHANCE)
                    {
                        hDirection = -hDirection;
                    }
                }

                // 高斯随机调整大小
                float gaussianFactor = Mathf.Clamp01(Mathf.Abs(GaussianRandom(0f, GAUSSIAN_STD_DEV)));
                float hBase = baseH * (GAUSSIAN_BASE_MULTIPLIER + GAUSSIAN_RANGE_MULTIPLIER * gaussianFactor);

                // 水平后坐力也乘以散布因子和枪械水平缩放系数
                float h = hBase * scatterFactor * hDirection * gun.RecoilScaleH * (1f / Mathf.Max(0.01f, gun.CharacterRecoilControl)) * recoilMult * strengthMult;

                // ========== 根据瞄准状态调整后坐力强度 ==========
                float adsValue = gun.AdsValue;
                if (adsValue > 0f) // 瞄准中：增强1.5倍
                {
                    v *= 1.5f;
                    h *= 1.5f;
                }
                else // 未瞄准：不变
                {
                    v *= 1f;
                    h *= 1f;
                }

                // ========== 枪械类型特殊处理：增强后坐力 ==========
                int typeID = gun.Item.TypeID;
                bool isShotgun = (gun.ShotCount > 1); // 霰弹枪判断
                bool isSmg = smgTypeIDs.Contains(typeID); // 冲锋枪判断

                if (isShotgun)
                {
                    v *= shotgunVerticalRecoilMultiplier;
                    h *= shotgunHorizontalRecoilMultiplier;
                }
                else if (isSmg)
                {
                    v *= smgVerticalRecoilMultiplier;
                    h *= smgHorizontalRecoilMultiplier;
                }

                // 更新模式状态
                horizontalRecoilPattern++;
                lastHorizontalDirection = hDirection;
                if (horizontalRecoilPattern >= HORIZONTAL_RECOIL_PATTERN_RESET)
                    horizontalRecoilPattern = 0;

                // ========== 武器切换时不清零累积后坐力，但暂停恢复，并强制重建准星 ==========
                if (currentRecoilGun != null && currentRecoilGun != gun)
                {
                    // 仅重置模式计数器和最近记录，不清除 accumulatedRecoilV/H
                    horizontalRecoilPattern = 0;
                    recentRecoilV.Clear();
                    recentRecoilH.Clear();
                    recoveryPaused = true; // 切枪后暂停恢复

                    // ========== 强制重建准星（替代原来的ForceRefreshAdsCrosshair） ==========
                    ForceRecreateAdsMarker();
                }
                currentRecoilGun = gun;
                isShooting = true;

                recentRecoilV.Add(v);
                recentRecoilH.Add(h);
                if (recentRecoilV.Count > MAX_RECENT_RECOIL)
                {
                    recentRecoilV.RemoveAt(0);
                    recentRecoilH.RemoveAt(0);
                }

                accumulatedRecoilV += v;
                accumulatedRecoilH += h;

                // 立即应用后坐力
                pitch = Mathf.Clamp(pitch - v, -89f, 89f);
                yaw += h;

                // 触发枪械抖动
                ApplyGunShake(v, h, adsValue, gun);

                // ========== 每次射击后解除暂停（允许后续恢复） ==========
                recoveryPaused = false;
            }
            catch { }
        }

        /// <summary>
        /// 将原版后坐力值映射到更合理的范围（0~10之间）
        /// </summary>
        private float MapRecoilValue(float originalValue)
        {
            if (originalValue <= 0f) return 0f;

            if (originalValue <= 10f)
                return originalValue * 0.2f;          // 0-10 -> 0-2
            else if (originalValue <= 20f)
                return 2f + (originalValue - 10f) * 0.2f; // 10-20 -> 2-4
            else if (originalValue <= 30f)
            {
                float normalized = (originalValue - 20f) / 10f;
                float compressed = Mathf.Sqrt(normalized);
                return 4f + compressed * 2f;          // 20-30 -> 4-6
            }
            else if (originalValue <= 50f)
            {
                float normalized = (originalValue - 30f) / 20f;
                float compressed = Mathf.Pow(normalized, 0.6f);
                return 6f + compressed * 2f;          // 30-50 -> 6-8
            }
            else
            {
                float excess = originalValue - 50f;
                float compressed = Mathf.Log(excess + 1f) / Mathf.Log(50f);
                return 8f + Mathf.Clamp01(compressed) * 1f; // 50+ -> 8-9
            }
        }

        private float GaussianRandom(float mean, float stdDev)
        {
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = 1f - UnityEngine.Random.value;
            float z0 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
            return mean + stdDev * z0;
        }
        #endregion
    }
}