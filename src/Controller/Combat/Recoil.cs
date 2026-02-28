using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 后坐力应用系统模块
    /// 负责处理射击时的后坐力应用，包括后坐力值映射、高斯随机分布、水平后坐力模式化等
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 后坐力系统字段
        private float accumulatedRecoilV = 0f;
        private float accumulatedRecoilH = 0f;
        private ItemAgent_Gun currentRecoilGun = null;
        private int horizontalRecoilPattern = 0;
        private float lastHorizontalDirection = 1f;
        private bool isShooting = false;
        private System.Collections.Generic.List<float> recentRecoilV = new System.Collections.Generic.List<float>();
        private System.Collections.Generic.List<float> recentRecoilH = new System.Collections.Generic.List<float>();
        private const int MAX_RECENT_RECOIL = 3;
        private const int HORIZONTAL_RECOIL_PATTERN_RESET = 10;
        private const float HORIZONTAL_DIRECTION_CONTINUE_CHANCE = 0.7f;
        private const float HORIZONTAL_DIRECTION_RANDOM_CHANCE = 0.1f;
        private const float GAUSSIAN_STD_DEV = 0.3f;
        private const float GAUSSIAN_BASE_MULTIPLIER = 0.5f;
        private const float GAUSSIAN_RANGE_MULTIPLIER = 0.5f;
        #endregion

        #region 后坐力应用逻辑
        private void HandleMouseReleaseRecoilReset()
        {
            if (!isShooting) return;
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
                bool recoilEnabled = true;
                try { recoilEnabled = FirstPersonCamera.Utilities.OptionsHelper.LoadInt(FirstPersonOptionsUI.EnableRecoilKey, 1) == 1; } catch { recoilEnabled = true; }
                if (!recoilEnabled) return;

                float recoilMult = 1f;
                try { recoilMult = LevelManager.Rule.RecoilMultiplier; } catch { recoilMult = 1f; }

                float strengthMult = 0.1f;
                try { strengthMult = FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(FirstPersonOptionsUI.RecoilStrengthKey, 0.1f); } catch { strengthMult = 0.1f; }

                float origVMin = gun.RecoilVMin;
                float origVMax = gun.RecoilVMax;
                float origHMin = gun.RecoilHMin;
                float origHMax = gun.RecoilHMax;

                float mappedVMin = MapRecoilValue(origVMin);
                float mappedVMax = MapRecoilValue(origVMax);
                float mappedHMin = MapRecoilValue(origHMin);
                float mappedHMax = MapRecoilValue(origHMax);

                float v = Random.Range(mappedVMin, mappedVMax) * gun.RecoilScaleV * (1f / Mathf.Max(0.01f, gun.CharacterRecoilControl)) * recoilMult * strengthMult;

                float hBase = Random.Range(mappedHMin, mappedHMax) * gun.RecoilScaleH * (1f / Mathf.Max(0.01f, gun.CharacterRecoilControl)) * recoilMult * strengthMult;

                float hDirection;
                if (horizontalRecoilPattern == 0)
                    hDirection = Random.value > 0.5f ? 1f : -1f;
                else
                {
                    float patternChance = Random.value;
                    hDirection = patternChance < HORIZONTAL_DIRECTION_CONTINUE_CHANCE ? lastHorizontalDirection : -lastHorizontalDirection;
                    if (Random.value < HORIZONTAL_DIRECTION_RANDOM_CHANCE)
                        hDirection = -hDirection;
                }

                float gaussianFactor = Mathf.Clamp01(Mathf.Abs(GaussianRandom(0f, GAUSSIAN_STD_DEV)));
                hBase = hBase * (GAUSSIAN_BASE_MULTIPLIER + GAUSSIAN_RANGE_MULTIPLIER * gaussianFactor);
                float h = hBase * hDirection;

                horizontalRecoilPattern++;
                lastHorizontalDirection = hDirection;
                if (horizontalRecoilPattern >= HORIZONTAL_RECOIL_PATTERN_RESET)
                    horizontalRecoilPattern = 0;

                if (currentRecoilGun != null && currentRecoilGun != gun)
                {
                    accumulatedRecoilV = 0f;
                    accumulatedRecoilH = 0f;
                    horizontalRecoilPattern = 0;
                    recentRecoilV.Clear();
                    recentRecoilH.Clear();
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

                pitch = Mathf.Clamp(pitch - v, -89f, 89f);
                yaw += h;

                // 调用枪械抖动（前后位移）
                ApplyGunShake(v, h, gun.AdsValue, gun);
            }
            catch { }
        }

        private float MapRecoilValue(float originalValue)
        {
            if (originalValue <= 0f) return 0f;
            if (originalValue <= 10f) return originalValue * 0.2f;
            if (originalValue <= 20f) return 2f + (originalValue - 10f) * 0.2f;
            if (originalValue <= 30f)
            {
                float normalized = (originalValue - 20f) / 10f;
                float compressed1 = Mathf.Sqrt(normalized);
                return 4f + compressed1 * 2f;
            }
            if (originalValue <= 50f)
            {
                float normalized = (originalValue - 30f) / 20f;
                float compressed2 = Mathf.Pow(normalized, 0.6f);
                return 6f + compressed2 * 2f;
            }
            float excess = originalValue - 50f;
            float compressed3 = Mathf.Log(excess + 1f) / Mathf.Log(50f);
            return 8f + Mathf.Clamp01(compressed3) * 1f;
        }

        private float GaussianRandom(float mean, float stdDev)
        {
            float u1 = 1f - Random.value;
            float u2 = 1f - Random.value;
            float z0 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
            return mean + stdDev * z0;
        }
        #endregion
    }
}