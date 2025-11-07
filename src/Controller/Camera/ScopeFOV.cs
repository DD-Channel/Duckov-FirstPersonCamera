using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 倍镜FOV缩放系统模块
    /// 负责处理倍镜的FOV缩放，包括平滑过渡和不同类型倍镜的缩放曲线
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 倍镜FOV缩放系统字段
        /// <summary>
        /// 当前倍镜的FOV缩放倍数（1/放大倍数）
        /// </summary>
        private float scopeFovMultiplier = 1f;
        
        /// <summary>
        /// 当前倍镜的TypeID（-1表示没有倍镜）
        /// </summary>
        private int currentScopeTypeID = -1;
        
        /// <summary>
        /// 基础FOV（用于倍镜缩放）
        /// </summary>
        private float baseFov = 70f;
        
        /// <summary>
        /// 目标FOV缩放倍数（用于平滑过渡）
        /// </summary>
        private float targetScopeFovMultiplier = 1f;
        
        /// <summary>
        /// 当前FOV缩放倍数（用于平滑过渡）
        /// </summary>
        private float currentScopeFovMultiplier = 1f;
        
        /// <summary>
        /// FOV缩放倍数变化阈值，小于此值认为没有变化
        /// </summary>
        private const float FOV_MULTIPLIER_THRESHOLD = 0.001f;
        
        /// <summary>
        /// 低倍镜阈值（≤2x），使用较慢的缩放曲线
        /// </summary>
        private const float LOW_MAGNIFICATION_THRESHOLD = 2f;
        
        /// <summary>
        /// 中倍镜阈值（≤4x），使用线性缩放曲线
        /// </summary>
        private const float MEDIUM_MAGNIFICATION_THRESHOLD = 4f;
        
        /// <summary>
        /// 低倍镜曲线指数（平方曲线，开始慢后面快）
        /// </summary>
        private const float LOW_MAGNIFICATION_CURVE = 2f;
        
        /// <summary>
        /// 中倍镜曲线指数（线性）
        /// </summary>
        private const float MEDIUM_MAGNIFICATION_CURVE = 1f;
        
        /// <summary>
        /// 高倍镜曲线指数（平方根曲线，开始快后面慢）
        /// </summary>
        private const float HIGH_MAGNIFICATION_CURVE = 0.5f;
        #endregion

        #region 倍镜FOV管理
        /// <summary>
        /// 更新倍镜FOV缩放（设置目标值）
        /// 根据倍镜TypeID设置相应的FOV缩放倍数，使用武器的瞄准速度进行平滑过渡
        /// </summary>
        /// <param name="gun">当前武器</param>
        private void UpdateScopeFOV(ItemAgent_Gun gun)
        {
            if (gun == null || gun.Item == null || mainCamera == null) return;
            
            try
            {
                // 获取Scope插槽
                var slot = gun.Item.Slots?.GetSlot("Scope");
                if (slot == null || slot.Content == null)
                {
                    // 没有倍镜，目标FOV恢复到1.0
                    if (currentScopeTypeID != -1)
                    {
                        targetScopeFovMultiplier = 1f;
                        currentScopeTypeID = -1;
                    }
                    return;
                }
                
                // 获取倍镜物品的TypeID
                int scopeTypeID = slot.Content.TypeID;
                
                // 如果倍镜变化了，更新目标FOV缩放倍数
                if (scopeTypeID != currentScopeTypeID)
                {
                    // 保存基础FOV（如果还没有保存或FOV已经改变）
                    // 如果当前FOV不是基础FOV，说明倍镜已经激活，需要从当前FOV反推基础FOV
                    if (currentScopeTypeID == -1)
                    {
                        // 如果currentScopeFovMultiplier还不是1，说明还在过渡中，需要反推
                        if (Mathf.Abs(currentScopeFovMultiplier - 1f) > FOV_MULTIPLIER_THRESHOLD)
                        {
                            baseFov = mainCamera.fieldOfView / currentScopeFovMultiplier;
                        }
                        else
                        {
                            baseFov = mainCamera.fieldOfView;
                        }
                    }
                    
                    // 根据倍镜TypeID设置目标FOV缩放倍数
                    float magnification = GetScopeMagnification(scopeTypeID);
                    
                    if (magnification > 0f)
                    {
                        targetScopeFovMultiplier = 1f / magnification;
                        currentScopeTypeID = scopeTypeID;
                    }
                    else
                    {
                        // 未知倍镜，恢复基础FOV
                        targetScopeFovMultiplier = 1f;
                        currentScopeTypeID = -1;
                    }
                }
            }
            catch
            {
                // 处理失败时静默处理
            }
        }
        
        /// <summary>
        /// 平滑更新FOV缩放（每帧调用）
        /// 根据枪械的AdsValue同步缩放，确保与举枪速度同步
        /// 根据倍镜类型调整速度：低倍镜慢一点，高倍镜快一点
        /// </summary>
        /// <param name="gun">当前武器</param>
        private void UpdateScopeFOVSmooth(ItemAgent_Gun gun)
        {
            if (mainCamera == null) return;
            
            try
            {
                // 获取枪械的AdsValue（0-1，表示举枪进度）
                float adsValue = 0f;
                if (gun != null)
                {
                    try
                    {
                        adsValue = gun.AdsValue;
                    }
                    catch
                    {
                        // 获取AdsValue失败时使用默认值
                    }
                }
                
                // 如果没有倍镜，FOV保持为1.0
                if (currentScopeTypeID == -1)
                {
                    // 直接使用AdsValue插值到基础FOV
                    float targetMultiplier = 1f;
                    currentScopeFovMultiplier = Mathf.Lerp(1f, targetMultiplier, adsValue);
                    mainCamera.fieldOfView = baseFov * currentScopeFovMultiplier;
                    return;
                }
                
                // 有倍镜时，根据AdsValue同步缩放
                // 获取倍镜的目标FOV缩放倍数
                float magnification = GetScopeMagnification(currentScopeTypeID);
                if (magnification > 0f)
                {
                    float targetMultiplier = 1f / magnification;
                    
                    // 根据倍镜类型调整缩放曲线
                    // 低倍镜（1.2x, 2x）：使用较慢的曲线（更大的exponent，开始慢后面快）
                    // 高倍镜（4x, 8x）：使用较快的曲线（更小的exponent，开始快后面慢）
                    float curveExponent = MEDIUM_MAGNIFICATION_CURVE; // 默认线性
                    if (magnification <= LOW_MAGNIFICATION_THRESHOLD)
                    {
                        // 低倍镜：使用较慢的曲线（平方），让缩放更慢更平滑
                        curveExponent = LOW_MAGNIFICATION_CURVE;
                    }
                    else if (magnification <= MEDIUM_MAGNIFICATION_THRESHOLD)
                    {
                        // 中倍镜：使用线性
                        curveExponent = MEDIUM_MAGNIFICATION_CURVE;
                    }
                    else
                    {
                        // 高倍镜：使用较快的曲线（平方根），让缩放更快
                        curveExponent = HIGH_MAGNIFICATION_CURVE;
                    }
                    
                    // 使用曲线调整adsValue，让不同倍镜有不同的缩放速度
                    float adjustedAdsValue = Mathf.Pow(adsValue, curveExponent);
                    
                    // 根据调整后的AdsValue插值FOV
                    // 从1.0（基础FOV）插值到targetMultiplier（倍镜FOV）
                    currentScopeFovMultiplier = Mathf.Lerp(1f, targetMultiplier, adjustedAdsValue);
                    
                    // 应用FOV缩放
                    mainCamera.fieldOfView = baseFov * currentScopeFovMultiplier;
                    
                    // 更新scopeFovMultiplier
                    scopeFovMultiplier = currentScopeFovMultiplier;
                }
            }
            catch
            {
                // 处理失败时静默处理
            }
        }
        
        /// <summary>
        /// 根据倍镜TypeID获取放大倍数
        /// </summary>
        /// <param name="typeID">倍镜的TypeID</param>
        /// <returns>放大倍数，如果未知则返回0</returns>
        private float GetScopeMagnification(int typeID)
        {
            // 根据用户提供的倍镜ID映射
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
                default:
                    return 0f; // 未知倍镜
            }
        }
        
        /// <summary>
        /// 恢复基础FOV（平滑过渡）
        /// </summary>
        private void RestoreBaseFOV()
        {
            if (mainCamera == null) return;
            
            try
            {
                // 如果正在使用倍镜，平滑恢复到基础FOV
                if (currentScopeTypeID != -1)
                {
                    targetScopeFovMultiplier = 1f;
                    currentScopeTypeID = -1;
                }
            }
            catch
            {
                // 处理失败时静默处理
            }
        }
        #endregion
    }
}

