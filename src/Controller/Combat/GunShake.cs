using UnityEngine;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 枪械抖动字段
        private Vector3 gunShakeRotation;           // 累积的旋转抖动（欧拉角）
        private Vector3 gunShakeVelocity;            // 用于 SmoothDamp 的速度缓存

        // 通用抖动参数（所有非狙击枪通用）
        private float gunShakeSmoothTime = 0.2f;     // 平滑时间（越大回正越慢，运动越柔）
        private float gunShakeIntensity = 0.35f;      // 基础强度系数
        private float gunShakeAdsReduction = 0.17f;   // 瞄准时的强度衰减系数

        // 压枪抵抗相关
        private float recoilResistanceFactor = 0.35f;    // 当前压枪抵抗因子，1=无抵抗，0=完全抵抗
        private float targetResistanceFactor = 1f;    // 目标抵抗因子
        private float resistanceLerpSpeed = 30f;       // 因子平滑速度
        #endregion

        #region 枪械抖动方法
        /// <summary>
        /// 设置压枪抵抗强度，由外部调用（基于鼠标垂直移动）
        /// </summary>
        /// <param name="mouseDeltaY">当前帧鼠标Y增量（正为向下）</param>
        public void SetRecoilResistance(float mouseDeltaY)
        {
            if (mouseDeltaY > 0.1f)
            {
                // 向下移动越多，抵抗越强，因子越小
                // 使用反比例映射：移动速度越快，因子越小
                float resistance = Mathf.Clamp01(1f - mouseDeltaY * 0.6f); // 系数可调
                targetResistanceFactor = resistance;
            }
            else
            {
                targetResistanceFactor = 1f;
            }
        }

        private void ApplyGunShake(float verticalRecoil, float horizontalRecoil, float adsValue, ItemAgent_Gun gun)
        {
            // 排除狙击枪（根据 TypeID 判断，请根据实际游戏修改）
            if (IsSniperRifle(gun)) return;
            if (adsValue <= 0f) return; // 未瞄准无抖动

            // 平滑更新抵抗因子
            recoilResistanceFactor = Mathf.Lerp(recoilResistanceFactor, targetResistanceFactor, resistanceLerpSpeed * Time.unscaledDeltaTime);

            float adsMultiplier = Mathf.Lerp(1f, gunShakeAdsReduction, adsValue);

            // 计算抖动目标值，强度乘以抵抗因子
            float intensity = gunShakeIntensity * recoilResistanceFactor;

            float targetPitch = (verticalRecoil > 0 ? -1f : 1f) * Mathf.Abs(verticalRecoil) * 0.8f * intensity * adsMultiplier;
            float targetYaw   = (horizontalRecoil > 0 ? 1f : -1f) * Mathf.Abs(horizontalRecoil) * 1.0f * intensity * adsMultiplier;
            float targetRoll  = (Mathf.Abs(verticalRecoil) + Mathf.Abs(horizontalRecoil)) * 0.3f * Random.Range(0.5f, 1.5f) * intensity * adsMultiplier;

            Vector3 targetPulse = new Vector3(targetPitch, targetYaw, targetRoll);
            gunShakeRotation += targetPulse; // 累积脉冲
        }

        private bool IsSniperRifle(ItemAgent_Gun gun)
        {
            if (gun == null || gun.Item == null) return false;
            int typeID = gun.Item.TypeID;
            // 请根据实际游戏中的狙击枪 TypeID 修改此列表
            return typeID == 568 || typeID == 569 || typeID == 12031;
        }

        private void UpdateGunShake()
        {
            if (mainCharacter == null) return;

            var gun = mainCharacter.GetGun();
            if (gun == null || gun.transform == null)
            {
                gunShakeRotation = Vector3.zero;
                return;
            }

            // 未瞄准时强制归零
            if (gun.AdsValue <= 0f)
            {
                gunShakeRotation = Vector3.zero;
                return;
            }

            // 所有非狙击枪使用相同的平滑时间
            float smoothTime = gunShakeSmoothTime;

            // 使用 SmoothDamp 平滑回归零
            gunShakeRotation = Vector3.SmoothDamp(gunShakeRotation, Vector3.zero, ref gunShakeVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            // 应用旋转
            gun.transform.localRotation = Quaternion.Euler(gunShakeRotation) * gun.transform.localRotation;
        }
        #endregion
    }
}