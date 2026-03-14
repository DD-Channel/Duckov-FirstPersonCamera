using UnityEngine;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 倍镜FOV缩放系统字段
        private float scopeFovMultiplier = 1f;
        private int currentScopeTypeID = -1; // 可以是倍镜ID，也可以是枪械ID（表示自带倍镜）
        private float baseFov = 70f;
        private float targetScopeFovMultiplier = 1f;
        private float currentScopeFovMultiplier = 1f;
        private const float FOV_MULTIPLIER_THRESHOLD = 0.001f;
        private const float LOW_MAGNIFICATION_THRESHOLD = 2f;
        private const float MEDIUM_MAGNIFICATION_THRESHOLD = 4f;
        private const float LOW_MAGNIFICATION_CURVE = 2f;
        private const float MEDIUM_MAGNIFICATION_CURVE = 1f;
        private const float HIGH_MAGNIFICATION_CURVE = 0.5f;
        #endregion

        #region 倍镜FOV管理
        private void UpdateScopeFOV(ItemAgent_Gun gun)
        {
            // FPLogger.Log("[UpdateScopeFOV] called");
            if (gun == null || gun.Item == null || mainCamera == null)
            {
                // FPLogger.Log("[UpdateScopeFOV] gun/camera null, returning");
                return;
            }

            try
            {
                // 1. 优先检查Scope插槽（外置倍镜）
                var slot = gun.Item.Slots?.GetSlot("Scope");
                int newScopeTypeID = -1;
                float magnification = 0f;

                if (slot != null && slot.Content != null)
                {
                    newScopeTypeID = slot.Content.TypeID;
                    magnification = GetScopeMagnification(newScopeTypeID);
                    if (magnification > 0f)
                    {
                        // 外置倍镜
                        targetScopeFovMultiplier = 1f / magnification;
                        if (currentScopeTypeID != newScopeTypeID)
                        {
                            currentScopeTypeID = newScopeTypeID;
                            // FPLogger.Log($"[UpdateScopeFOV] external scope, magnification={magnification}");
                            // 更新准星隐藏（由倍镜决定）
                            UpdateAdsCrosshair(newScopeTypeID);
                        }
                        return;
                    }
                }

                // 2. 如果没有外置倍镜或倍镜未知，检查枪械是否自带倍镜
                int gunTypeID = gun.Item.TypeID;
                magnification = GetGunIntegratedMagnification(gunTypeID);
                if (magnification > 0f)
                {
                    targetScopeFovMultiplier = 1f / magnification;
                    if (currentScopeTypeID != gunTypeID)
                    {
                        currentScopeTypeID = gunTypeID;
                        // FPLogger.Log($"[UpdateScopeFOV] gun integrated scope, magnification={magnification}");
                        // 调用准星隐藏，传入枪械ID（需在AdsCrosshair字典中配置该ID对应的准星规则）
                        UpdateAdsCrosshair(gunTypeID);
                    }
                    return;
                }

                // 3. 没有任何倍镜
                if (currentScopeTypeID != -1)
                {
                    targetScopeFovMultiplier = 1f;
                    currentScopeTypeID = -1;
                    RestoreAdsCrosshair();
                    // FPLogger.Log("[UpdateScopeFOV] no scope, resetting");
                }
            }
            catch (System.Exception ex)
            {
                // FPLogger.Log($"[UpdateScopeFOV] exception: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取枪械自带的放大倍数（通过 TypeID）
        /// </summary>
        private float GetGunIntegratedMagnification(int gunTypeID)
        {
            // 在此处添加需要处理的枪械 TypeID 和对应的放大倍数
            switch (gunTypeID)
            {
                case 1433:
                case 1521:
                case 1353:
                case 1354:
                case 916:
                case 917:
                case 914:
                case 1396:
                    return 1.2f;
                case 653:
                    return 2.5f;
                case 1480:
                case 10143:
                    return 4f;
                default:
                    return 0f; // 0 表示没有自带倍镜
            }
        }

        private void UpdateScopeFOVSmooth(ItemAgent_Gun gun)
        {
            if (mainCamera == null) return;
            try
            {
                float adsValue = 0f;
                if (gun != null)
                {
                    try { adsValue = gun.AdsValue; } catch { }
                }

                if (currentScopeTypeID == -1)
                {
                    currentScopeFovMultiplier = Mathf.Lerp(1f, 1f, adsValue);
                    mainCamera.fieldOfView = baseFov * currentScopeFovMultiplier;
                    return;
                }

                // 获取当前放大倍数（优先从倍镜映射，失败则从枪械自带映射）
                float magnification = GetScopeMagnification(currentScopeTypeID);
                if (magnification <= 0f)
                    magnification = GetGunIntegratedMagnification(currentScopeTypeID);

                if (magnification > 0f)
                {
                    float targetMultiplier = 1f / magnification;
                    float curveExponent = MEDIUM_MAGNIFICATION_CURVE;
                    if (magnification <= LOW_MAGNIFICATION_THRESHOLD)
                        curveExponent = LOW_MAGNIFICATION_CURVE;
                    else if (magnification <= MEDIUM_MAGNIFICATION_THRESHOLD)
                        curveExponent = MEDIUM_MAGNIFICATION_CURVE;
                    else
                        curveExponent = HIGH_MAGNIFICATION_CURVE;

                    float adjustedAdsValue = Mathf.Pow(adsValue, curveExponent);
                    currentScopeFovMultiplier = Mathf.Lerp(1f, targetMultiplier, adjustedAdsValue);
                    mainCamera.fieldOfView = baseFov * currentScopeFovMultiplier;
                    scopeFovMultiplier = currentScopeFovMultiplier;
                }
            }
            catch { }
        }

        public float GetScopeMagnification(int typeID)
        {
            // 原版倍镜
            switch (typeID)
            {
                case 570: // 数字瞄具 Lv4
                case 571: // 全息瞄具 Lv4
                case 572: // 快速瞄具 Lv4
                case 573: // 红点瞄具 Lv4
                case 10026:
                case 10027:
                case 10028:
                case 12028:
                    return 1.2f;
                case 574: // 2倍镜
                case 10029:
                case 12029:
                    return 2f;
                case 568: // 4倍镜 Lv4
                case 10030:
                case 10070:
                case 12030:
                    return 4f;
                case 569: // 8倍镜 Lv4
                case 10031:
                case 12031:
                case 12032:
                    return 8f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 获取当前倍镜的放大倍数（优先倍镜，其次枪械自带）
        /// </summary>
        public float GetCurrentMagnification()
        {
            if (currentScopeTypeID != -1)
            {
                float mag = GetScopeMagnification(currentScopeTypeID);
                if (mag > 0f) return mag;
                mag = GetGunIntegratedMagnification(currentScopeTypeID);
                if (mag > 0f) return mag;
            }
            return 1f;
        }

        private void RestoreBaseFOV()
        {
            if (mainCamera == null) return;
            try
            {
                if (currentScopeTypeID != -1)
                {
                    targetScopeFovMultiplier = 1f;
                    currentScopeTypeID = -1;
                }
            }
            catch { }
        }
        #endregion
    }
}