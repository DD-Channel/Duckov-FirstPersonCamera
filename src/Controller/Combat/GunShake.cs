using UnityEngine;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 枪械抖动字段（按武器类别区分）

        private Vector3 gunShakeRotation;           // 累积的旋转抖动（欧拉角）
        private Vector3 gunShakeVelocity;            // 用于 SmoothDamp 的速度缓存

        // 新增：前后位移抖动
        private float gunShakeForward;               // 累积的前后位移
        private float gunShakeForwardVelocity;       // 位移平滑速度

        // 压枪抵抗相关（全局参数）
        private float recoilResistanceFactor = 0.35f;    // 当前压枪抵抗因子，1=无抵抗，0=完全抵抗
        private float targetResistanceFactor = 1f;       // 目标抵抗因子
        private float resistanceLerpSpeed = 30f;         // 因子平滑速度

        // 开镜完成阈值，只有当 adsValue 大于此值时，才允许应用抖动
        private const float ADS_COMPLETE_THRESHOLD = 0.2f;

        // ========== 各类武器抖动参数 ==========

        // 步枪
        private float rifleShakeSmoothTime = 0.12f;        // 旋转平滑时间
        private float rifleShakeIntensity = 0.1f;          // 旋转强度
        private float rifleShakeAdsReduction = 0.5f;       // 瞄准时旋转衰减
        private float rifleShakeForwardIntensity = 0.02f;  // 前后位移强度

        // 手枪/冲锋枪
        private float smgShakeSmoothTime = 0.12f;
        private float smgShakeIntensity = 0.2f;
        private float smgShakeAdsReduction = 0.3f;
        private float smgShakeForwardIntensity = 0.02f;

        // 霰弹枪
        private float shotgunShakeSmoothTime = 1.0f;
        private float shotgunShakeIntensity = 0.15f;
        private float shotgunShakeAdsReduction = 0.3f;
        private float shotgunShakeForwardIntensity = 0.03f;

        // 狙击枪
        private float sniperShakeSmoothTime = 0.1f;
        private float sniperShakeIntensity = 0.6f;
        private float sniperShakeAdsReduction = 1f;
        private float sniperShakeForwardIntensity = 0.02f;   // 狙击枪无位移

        // 精确步枪
        private float marksmanShakeSmoothTime = 0.15f;
        private float marksmanShakeIntensity = 0.2f;
        private float marksmanShakeAdsReduction = 0.6f;
        private float marksmanShakeForwardIntensity = 0.02f;

        // 未知武器
        private float unknownShakeSmoothTime = 0.8f;
        private float unknownShakeIntensity = 0.1f;
        private float unknownShakeAdsReduction = 0.5f;
        private float unknownShakeForwardIntensity = 0.02f;

        #endregion

        #region 枪械抖动方法

        public void SetRecoilResistance(float mouseDeltaY)
        {
            if (mouseDeltaY > 0.1f)
            {
                float resistance = Mathf.Clamp01(1f - mouseDeltaY * 0.6f);
                targetResistanceFactor = resistance;
            }
            else
            {
                targetResistanceFactor = 1f;
            }
        }

        private void ApplyGunShake(float verticalRecoil, float horizontalRecoil, float adsValue, ItemAgent_Gun gun)
        {
            // 只有开镜完成且瞄准中才产生新抖动
            if (adsValue < ADS_COMPLETE_THRESHOLD) return;

            WeaponCategory category = GetWeaponCategory(gun);
            float smoothTime, intensity, adsReduction, forwardIntensity;
            GetShakeParameters(category, out smoothTime, out intensity, out adsReduction, out forwardIntensity);

            if (intensity <= 0f && forwardIntensity <= 0f) return;

            // 平滑更新抵抗因子
            recoilResistanceFactor = Mathf.Lerp(recoilResistanceFactor, targetResistanceFactor, resistanceLerpSpeed * Time.unscaledDeltaTime);
            float adsMultiplier = Mathf.Lerp(1f, adsReduction, adsValue);
            float effectiveIntensity = intensity * recoilResistanceFactor;
            float effectiveForwardIntensity = forwardIntensity * recoilResistanceFactor;

            // 基于后坐力大小计算基础幅度
            float magnitude = (Mathf.Abs(verticalRecoil) + Mathf.Abs(horizontalRecoil)) * 0.5f;

            // 旋转抖动（方向与后坐力相关）
            float targetPitch = (verticalRecoil > 0 ? -1f : 1f) * Mathf.Abs(verticalRecoil) * 0.8f * effectiveIntensity * adsMultiplier;
            float targetYaw   = (horizontalRecoil > 0 ? 1f : -1f) * Mathf.Abs(horizontalRecoil) * 1.0f * effectiveIntensity * adsMultiplier;
            float targetRoll  = (Mathf.Abs(verticalRecoil) + Mathf.Abs(horizontalRecoil)) * 0.3f * Random.Range(0.5f, 1.5f) * effectiveIntensity * adsMultiplier;

            Vector3 targetPulse = new Vector3(targetPitch, targetYaw, targetRoll);
            gunShakeRotation += targetPulse;

            // 前后位移抖动（沿局部Z轴，方向随机）
            float randomForwardDir = Random.Range(-1f, 1f);
            float targetForward = randomForwardDir * magnitude * 0.5f * effectiveForwardIntensity * adsMultiplier;
            gunShakeForward += targetForward;
        }

        private void GetShakeParameters(WeaponCategory category, out float smoothTime, out float intensity, out float adsReduction, out float forwardIntensity)
        {
            switch (category)
            {
                case WeaponCategory.Rifle:
                    smoothTime = rifleShakeSmoothTime;
                    intensity = rifleShakeIntensity;
                    adsReduction = rifleShakeAdsReduction;
                    forwardIntensity = rifleShakeForwardIntensity;
                    break;
                case WeaponCategory.PistolSMG:
                    smoothTime = smgShakeSmoothTime;
                    intensity = smgShakeIntensity;
                    adsReduction = smgShakeAdsReduction;
                    forwardIntensity = smgShakeForwardIntensity;
                    break;
                case WeaponCategory.Shotgun:
                    smoothTime = shotgunShakeSmoothTime;
                    intensity = shotgunShakeIntensity;
                    adsReduction = shotgunShakeAdsReduction;
                    forwardIntensity = shotgunShakeForwardIntensity;
                    break;
                case WeaponCategory.Sniper:
                    smoothTime = sniperShakeSmoothTime;
                    intensity = sniperShakeIntensity;
                    adsReduction = sniperShakeAdsReduction;
                    forwardIntensity = sniperShakeForwardIntensity;
                    break;
                case WeaponCategory.MarksmanRifle:
                    smoothTime = marksmanShakeSmoothTime;
                    intensity = marksmanShakeIntensity;
                    adsReduction = marksmanShakeAdsReduction;
                    forwardIntensity = marksmanShakeForwardIntensity;
                    break;
                case WeaponCategory.Unknown:
                default:
                    smoothTime = unknownShakeSmoothTime;
                    intensity = unknownShakeIntensity;
                    adsReduction = unknownShakeAdsReduction;
                    forwardIntensity = unknownShakeForwardIntensity;
                    break;
            }
        }

        private void UpdateGunShake()
        {
            if (mainCharacter == null) return;

            var gun = mainCharacter.GetGun();
            if (gun == null || gun.transform == null)
            {
                ResetShake();
                return;
            }

            // 开镜未完成时，强制清零抖动，避免干扰
            if (gun.AdsValue < ADS_COMPLETE_THRESHOLD)
            {
                ResetShake();
                return;
            }

            WeaponCategory category = GetWeaponCategory(gun);
            float smoothTime = GetShakeSmoothTime(category);
            float intensity = GetShakeIntensity(category);
            float forwardIntensity = GetShakeForwardIntensity(category);

            // 如果旋转抖动被禁用，清零旋转
            if (smoothTime <= 0f || intensity <= 0f)
            {
                gunShakeRotation = Vector3.zero;
                gunShakeVelocity = Vector3.zero;
            }

            // 如果位移抖动被禁用，清零位移
            if (forwardIntensity <= 0f)
            {
                gunShakeForward = 0f;
                gunShakeForwardVelocity = 0f;
            }

            // 平滑回归零（旋转）
            if (gunShakeRotation != Vector3.zero)
            {
                gunShakeRotation = Vector3.SmoothDamp(gunShakeRotation, Vector3.zero, ref gunShakeVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                if (gunShakeRotation.sqrMagnitude < 0.0001f)
                {
                    gunShakeRotation = Vector3.zero;
                    gunShakeVelocity = Vector3.zero;
                }
            }

            // 平滑回归零（位移）
            if (gunShakeForward != 0f)
            {
                gunShakeForward = Mathf.SmoothDamp(gunShakeForward, 0f, ref gunShakeForwardVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(gunShakeForward) < 0.0001f)
                {
                    gunShakeForward = 0f;
                    gunShakeForwardVelocity = 0f;
                }
            }

            // 应用旋转
            if (gunShakeRotation != Vector3.zero)
            {
                gun.transform.localRotation = Quaternion.Euler(gunShakeRotation) * gun.transform.localRotation;
            }

            // 应用前后位移（叠加到 localPosition.z）
            if (gunShakeForward != 0f)
            {
                Vector3 pos = gun.transform.localPosition;
                pos.z += gunShakeForward;
                gun.transform.localPosition = pos;
            }
        }

        private void ResetShake()
        {
            gunShakeRotation = Vector3.zero;
            gunShakeForward = 0f;
            gunShakeVelocity = Vector3.zero;
            gunShakeForwardVelocity = 0f;
        }

        // 辅助方法：获取旋转平滑时间
        private float GetShakeSmoothTime(WeaponCategory category)
        {
            float smoothTime, intensity, adsReduction, forwardIntensity;
            GetShakeParameters(category, out smoothTime, out intensity, out adsReduction, out forwardIntensity);
            return smoothTime;
        }

        // 辅助方法：获取旋转强度
        private float GetShakeIntensity(WeaponCategory category)
        {
            float smoothTime, intensity, adsReduction, forwardIntensity;
            GetShakeParameters(category, out smoothTime, out intensity, out adsReduction, out forwardIntensity);
            return intensity;
        }

        // 辅助方法：获取前后位移强度
        private float GetShakeForwardIntensity(WeaponCategory category)
        {
            float smoothTime, intensity, adsReduction, forwardIntensity;
            GetShakeParameters(category, out smoothTime, out intensity, out adsReduction, out forwardIntensity);
            return forwardIntensity;
        }

        #endregion
    }
}