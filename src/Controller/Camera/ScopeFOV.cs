using UnityEngine;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 倍镜FOV缩放系统字段
        private float scopeFovMultiplier = 1f;
        private int currentScopeTypeID = -1;
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
            FPLogger.Log("[UpdateScopeFOV] called");
            if (gun == null || gun.Item == null || mainCamera == null)
            {
                FPLogger.Log("[UpdateScopeFOV] gun/camera null, returning");
                return;
            }

            try
            {
                var slot = gun.Item.Slots?.GetSlot("Scope");
                if (slot == null || slot.Content == null)
                {
                    FPLogger.Log("[UpdateScopeFOV] no scope, setting target to 1");
                    if (currentScopeTypeID != -1)
                    {
                        targetScopeFovMultiplier = 1f;
                        currentScopeTypeID = -1;
                        // 没有倍镜时恢复准星
                        RestoreAdsCrosshair();
                    }
                    return;
                }

                int scopeTypeID = slot.Content.TypeID;
                FPLogger.Log($"[UpdateScopeFOV] scopeTypeID={scopeTypeID}, currentScopeTypeID={currentScopeTypeID}");

                if (scopeTypeID != currentScopeTypeID)
                {
                    FPLogger.Log("[UpdateScopeFOV] scope type changed");

                    if (currentScopeTypeID == -1)
                    {
                        if (Mathf.Abs(currentScopeFovMultiplier - 1f) > FOV_MULTIPLIER_THRESHOLD)
                        {
                            baseFov = mainCamera.fieldOfView / currentScopeFovMultiplier;
                        }
                        else
                        {
                            baseFov = mainCamera.fieldOfView;
                        }
                    }

                    float magnification = GetScopeMagnification(scopeTypeID);
                    FPLogger.Log($"[UpdateScopeFOV] magnification={magnification}");

                    if (magnification > 0f)
                    {
                        targetScopeFovMultiplier = 1f / magnification;
                        currentScopeTypeID = scopeTypeID;
                        FPLogger.Log($"[UpdateScopeFOV] calling UpdateAdsCrosshair with scopeTypeID={scopeTypeID}");
                        UpdateAdsCrosshair(scopeTypeID);
                    }
                    else
                    {
                        // 未知倍镜：不缩放FOV，但应用准星隐藏
                        targetScopeFovMultiplier = 1f;
                        currentScopeTypeID = scopeTypeID;
                        FPLogger.Log($"[UpdateScopeFOV] unknown magnification, calling UpdateAdsCrosshair with scopeTypeID={scopeTypeID} to hide frame parts");
                        UpdateAdsCrosshair(scopeTypeID);
                    }
                }
                else
                {
                    FPLogger.Log("[UpdateScopeFOV] scope type unchanged");
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.Log($"[UpdateScopeFOV] exception: {ex.Message}");
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
                    float targetMultiplier = 1f;
                    currentScopeFovMultiplier = Mathf.Lerp(1f, targetMultiplier, adsValue);
                    mainCamera.fieldOfView = baseFov * currentScopeFovMultiplier;
                    return;
                }

                float magnification = GetScopeMagnification(currentScopeTypeID);
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

        private float GetScopeMagnification(int typeID)
        {
            // 原版倍镜
            switch (typeID)
            {
                case 570: // 数字瞄具 Lv4
                case 571: // 全息瞄具 Lv4
                case 572: // 快速瞄具 Lv4
                case 573: // 红点瞄具 Lv4
                    return 1.2f;
                case 574: // 2倍镜
                    return 2f;
                case 568: // 4倍镜 Lv4
                    return 4f;
                case 569: // 8倍镜 Lv4
                    return 8f;
                // 以下为特殊倍镜（如智慧核心系列），可根据需要补充倍率
                // 若不确定倍率，返回 0 会触发未知分支，只隐藏准星而不缩放FOV
                default:
                    return 0f; // 未知倍镜，由调用者处理
            }
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