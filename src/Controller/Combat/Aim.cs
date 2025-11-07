using System.Reflection;
using UnityEngine;
using Duckov.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 瞄准和准星管理模块
    /// 负责处理瞄准点计算、准星UI更新、以及通过反射访问游戏内部输入系统
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 反射缓存字段
        /// <summary>
        /// 缓存的输入管理器实例（避免频繁查找）
        /// </summary>
        private InputManager cachedInputManager;
        
        /// <summary>
        /// 瞄准屏幕点字段（通过反射访问私有字段）
        /// </summary>
        private FieldInfo fiAimScreenPoint;
        
        /// <summary>
        /// 输入瞄准点字段（通过反射访问私有字段）
        /// </summary>
        private FieldInfo fiInputAimPoint;
        
        /// <summary>
        /// 瞄准鼠标位置缓存字段（通过反射访问私有字段）
        /// </summary>
        private FieldInfo fiAimMousePosCache;
        
        /// <summary>
        /// ADS瞄准标记器类型（通过反射获取）
        /// </summary>
        private System.Type adsAimMarkerType;
        
        /// <summary>
        /// 当前ADS瞄准标记器字段（通过反射访问）
        /// </summary>
        private FieldInfo fiCurrentAdsAimMarker;
        
        /// <summary>
        /// ADS跟随UI字段（通过反射访问）
        /// </summary>
        private FieldInfo fiAdsFollowUI;
        #endregion

        #region 瞄准状态缓存
        /// <summary>
        /// 上次计算的瞄准点（3D世界坐标）
        /// </summary>
        private Vector3 lastComputedAimPoint;
        
        /// <summary>
        /// 上次计算瞄准点的帧数（用于避免同帧重复计算）
        /// </summary>
        private int lastAimFrame = -1;
        
        /// <summary>
        /// 上次设置屏幕中心的帧数（用于避免同帧重复设置）
        /// </summary>
        private int lastCenterFrame = -1;
        
        /// <summary>
        /// 缓存的准星标记器组件（避免频繁查找）
        /// </summary>
        private AimMarker cachedAimMarker;
        
        /// <summary>
        /// 上次计算瞄准点的时间（用于时间间隔检查）
        /// </summary>
        private float lastAimComputeTime;
        
        /// <summary>
        /// 上次计算瞄准点时的旋转角度（用于角度变化检查）
        /// </summary>
        private float lastAimYaw, lastAimPitch;
        
        /// <summary>
        /// 上次计算瞄准点时的屏幕尺寸（用于屏幕尺寸变化检查）
        /// </summary>
        private int lastScreenW = -1, lastScreenH = -1;
        #endregion

        #region 常量定义
        /// <summary>
        /// 相机到瞄准点的最小距离（米），防止瞄准点太近导致计算异常
        /// </summary>
        private const float MIN_DISTANCE_FROM_CAMERA = 0.5f;
        
        /// <summary>
        /// 枪口到瞄准点的最小距离（米），确保子弹方向计算正常
        /// </summary>
        private const float MIN_DISTANCE_FROM_MUZZLE = 1f;
        
        /// <summary>
        /// 默认瞄准距离（米），当没有命中任何物体时使用
        /// </summary>
        private const float DEFAULT_AIM_DISTANCE = 50f;
        
        /// <summary>
        /// 最大瞄准距离（米），防止计算过远的点
        /// </summary>
        private const float MAX_AIM_DISTANCE = 4000f;
        
        /// <summary>
        /// 投影计算的最小阈值，避免除零错误
        /// </summary>
        private const float MIN_PROJECTION_MAGNITUDE = 0.01f;
        #endregion

        #region 反射缓存初始化
        /// <summary>
        /// 确保所有反射缓存已初始化
        /// 通过反射获取游戏内部输入系统和UI系统的私有字段，用于修改瞄准点
        /// </summary>
        private void EnsureCaches()
        {
            // 初始化输入管理器缓存
            if (cachedInputManager == null)
            {
                try
                {
                    cachedInputManager = LevelManager.Instance != null 
                        ? LevelManager.Instance.InputManager 
                        : null;
                }
                catch
                {
                    cachedInputManager = null;
                }
            }

            // 初始化输入管理器的反射字段
            if (cachedInputManager != null)
            {
                var imType = cachedInputManager.GetType();
                
                // 获取瞄准屏幕点字段（用于设置瞄准点位置）
                if (fiAimScreenPoint == null)
                {
                    fiAimScreenPoint = imType.GetField("aimScreenPoint", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }
                
                // 获取输入瞄准点字段（用于设置3D瞄准点）
                if (fiInputAimPoint == null)
                {
                    fiInputAimPoint = imType.GetField("inputAimPoint", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }
                
                // 获取瞄准鼠标位置缓存字段（用于同步鼠标位置）
                if (fiAimMousePosCache == null)
                {
                    fiAimMousePosCache = imType.GetField("_aimMousePosCache", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }

            // 初始化ADS（瞄准镜）相关的反射字段
            if (fiCurrentAdsAimMarker == null || fiAdsFollowUI == null || adsAimMarkerType == null)
            {
                try
                {
                    // 获取当前ADS瞄准标记器字段
                    fiCurrentAdsAimMarker = typeof(AimMarker).GetField("currentAdsAimMarker", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    // 获取ADS瞄准标记器类型
                    adsAimMarkerType = typeof(AimMarker).Assembly.GetType("ADSAimMarker");
                    
                    if (adsAimMarkerType != null)
                    {
                        // 获取ADS跟随UI字段
                        fiAdsFollowUI = adsAimMarkerType.GetField("followUI", 
                            BindingFlags.Public | BindingFlags.Instance);
                    }
                }
                catch
                {
                    // 反射失败时静默处理，不影响主要功能
                }
            }
        }
        #endregion

        #region 屏幕中心瞄准
        /// <summary>
        /// 将瞄准点强制设置到屏幕中心
        /// 在第一人称模式下，瞄准点应该始终在屏幕中心，不受鼠标位置影响
        /// </summary>
        private void SetAimScreenCenterOnly()
        {
            // 避免同帧重复设置
            if (lastCenterFrame == Time.frameCount) return;
            lastCenterFrame = Time.frameCount;

            // 确保反射缓存已初始化
            EnsureCaches();

            // 计算屏幕中心点
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (cachedInputManager != null)
            {
                try
                {
                    // 设置瞄准屏幕点（游戏内部使用）
                    if (fiAimScreenPoint != null)
                    {
                        fiAimScreenPoint.SetValue(cachedInputManager, center);
                    }

                    // 如果鼠标未锁定，同步设置鼠标位置
                    if (Cursor.lockState != CursorLockMode.Locked)
                    {
                        cachedInputManager.SetMousePosition(center);
                    }

                    // 更新鼠标位置缓存
                    if (fiAimMousePosCache != null)
                    {
                        fiAimMousePosCache.SetValue(cachedInputManager, center);
                    }
                }
                catch
                {
                    // 反射操作失败时静默处理
                }
            }
        }
        #endregion

        #region 瞄准和准星更新
        /// <summary>
        /// 确保当前帧的瞄准点已经更新
        /// </summary>
        private bool EnsureAimPoint(bool forceRecalculate, bool needsScreen, bool needsAngle, bool needsTime)
        {
            if (mainCamera == null)
            {
                return false;
            }

            if (!forceRecalculate && lastAimFrame == Time.frameCount)
            {
                return false;
            }

            if (!forceRecalculate && !(needsScreen || needsAngle || needsTime))
            {
                return false;
            }

            if (forceRecalculate && lastAimFrame == Time.frameCount)
            {
                return false;
            }

            var ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

            lastComputedAimPoint = ComputeAimPointFromRay(ray);
            lastAimFrame = Time.frameCount;
            lastAimComputeTime = Time.unscaledTime;
            lastAimYaw = yaw;
            lastAimPitch = pitch;
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;

            return true;
        }

        /// <summary>
        /// 获取最近计算出的瞄准点
        /// </summary>
        internal bool TryGetAimPoint(out Vector3 aimPoint, bool forceUpdate = false)
        {
            if (forceUpdate)
            {
                EnsureAimPoint(true, true, true, true);
            }

            aimPoint = lastComputedAimPoint;
            return lastAimFrame >= 0;
        }

        /// <summary>
        /// 获取当前用于准星的世界瞄准点
        /// </summary>
        public Vector3 GetCurrentAimWorldPoint()
        {
            if (mainCamera == null)
            {
                return lastComputedAimPoint;
            }

            if (lastAimFrame != Time.frameCount)
            {
                EnsureAimPoint(true, true, true, true);
            }

            return lastComputedAimPoint;
        }

        /// <summary>
        /// 更新瞄准点和准星UI
        /// 根据相机方向计算3D世界中的瞄准点，并更新准星UI位置
        /// </summary>
        private void UpdateAimAndCrosshair()
        {
            try
            {
                // 游戏暂停时跳过更新
                if (GameManager.Paused) return;

                // 确保反射缓存已初始化
                EnsureCaches();

                // 强制瞄准点保持在屏幕中心
                SetAimScreenCenterOnly();

                // 检查是否需要重新计算瞄准点
                bool needsScreen = (Screen.width != lastScreenW || Screen.height != lastScreenH);
                bool needsAngle = (Mathf.Abs(yaw - lastAimYaw) >= aimAngleThreshold || 
                                 Mathf.Abs(pitch - lastAimPitch) >= aimAngleThreshold);
                bool needsTime = (Time.unscaledTime - lastAimComputeTime) >= aimRecalcInterval;

                // 如果满足重新计算条件（屏幕尺寸变化、角度变化、或时间间隔到达）
                EnsureAimPoint(false, needsScreen, needsAngle, needsTime);

                // 更新角色瞄准点（用于子弹发射方向计算）
                if (mainCharacter != null)
                {
                    mainCharacter.SetAimPoint(lastComputedAimPoint);
                }

                // 更新输入管理器的瞄准点（通过反射）
                if (cachedInputManager != null && fiInputAimPoint != null)
                {
                    try
                    {
                        fiInputAimPoint.SetValue(cachedInputManager, lastComputedAimPoint);
                    }
                    catch
                    {
                        // 反射操作失败时静默处理
                    }
                }

                // 更新准星UI位置
                if (cachedAimMarker == null)
                {
                    cachedAimMarker = Object.FindObjectOfType<AimMarker>();
                }

                if (cachedAimMarker != null)
                {
                    // 节流准星UI更新，避免每帧布局开销
                    // 每N帧更新一次，或屏幕尺寸变化时立即更新
                    if ((Time.frameCount % aimMarkerUpdateEveryNFrames) == 0 || needsScreen)
                    {
                        var center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                        
                        // 设置准星位置到屏幕中心
                        cachedAimMarker.SetAimMarkerPosScreenSpace(center);
                        
                        try
                        {
                            // 同步更新ADS瞄准镜的跟随UI位置
                            if (fiCurrentAdsAimMarker != null && fiAdsFollowUI != null)
                            {
                                var ads = fiCurrentAdsAimMarker.GetValue(cachedAimMarker);
                                var followUI = fiAdsFollowUI.GetValue(ads) as RectTransform;
                                if (followUI != null)
                                {
                                    followUI.position = center;
                                }
                            }
                        }
                        catch
                        {
                            // ADS UI更新失败时静默处理
                        }
                    }
                }
            }
            catch
            {
                // 整体更新失败时静默处理，避免影响游戏运行
            }
        }
        #endregion

        #region 瞄准点计算
        /// <summary>
        /// 从射线计算瞄准点（3D世界坐标）
        /// 射线从相机位置发射，方向是相机forward，这个点就是准心在3D空间中指向的位置
        /// 原版游戏会计算：子弹方向 = 瞄准点 - 枪口位置
        /// 所以瞄准点应该准确反映准心指向的位置
        /// </summary>
        /// <param name="ray">从相机位置发射的射线，方向为相机forward</param>
        /// <returns>瞄准点在3D世界中的坐标</returns>
        private Vector3 ComputeAimPointFromRay(Ray ray)
        {
            // 获取角色和枪口信息
            Vector3 playerPos = Vector3.zero;
            Vector3 muzzlePos = Vector3.zero;
            bool hasMuzzle = false;
            
            try
            {
                var mc = mainCharacter != null ? mainCharacter : CharacterMainControl.Main;
                if (mc != null)
                {
                    playerPos = mc.transform.position;
                    var gun = mc.GetGun();
                    if (gun != null && gun.muzzle != null)
                    {
                        muzzlePos = gun.muzzle.position;
                        hasMuzzle = true;
                    }
                }
            }
            catch
            {
                // 获取角色信息失败时使用默认值
            }

            // 检查玩家是否有附近的半障碍物（如沙袋）
            // 如果有，子弹应该可以穿透，所以瞄准射线也应该忽略HalfObsticle层
            bool hasNearByHalfObsticle = false;
            LayerMask raycastLayerMask = GameplayDataSettings.Layers.damageReceiverLayerMask |
                                         GameplayDataSettings.Layers.wallLayerMask |
                                         GameplayDataSettings.Layers.groundLayerMask |
                                         GameplayDataSettings.Layers.fowBlockLayers |
                                         GameplayDataSettings.Layers.halfObsticleLayer;
            
            try
            {
                var mc = mainCharacter != null ? mainCharacter : CharacterMainControl.Main;
                if (mc != null)
                {
                    // 检查是否有附近的半障碍物
                    hasNearByHalfObsticle = mc.HasNearByHalfObsticle();
                    
                    // 如果有附近的半障碍物，需要从射线检测中排除HalfObsticle层
                    if (hasNearByHalfObsticle)
                    {
                        try
                        {
                            // 获取HalfObsticle层的索引
                            int halfObsticleLayer = LayerMask.NameToLayer("HalfObsticle");
                            if (halfObsticleLayer >= 0)
                            {
                                // 排除HalfObsticle层，使射线可以穿透半障碍物
                                raycastLayerMask &= (~(1 << halfObsticleLayer));
                            }
                        }
                        catch
                        {
                            // 层掩码设置失败时使用默认值
                        }
                    }
                }
            }
            catch
            {
                // 半障碍物检查失败时使用默认层掩码
            }
            
            // 执行射线检测，找到准心指向的点
            try
            {
                // 从相机位置发射射线，找到准心指向的点
                // 如果有附近的HalfObsticle，射线会穿透HalfObsticle层
                if (Physics.Raycast(ray, out var hit, raycastDistance, raycastLayerMask, 
                    QueryTriggerInteraction.Ignore))
                {
                    // 检查命中点到相机的距离
                    float distFromCamera = Vector3.Distance(hit.point, ray.origin);
                    
                    // 如果命中点太近（可能命中相机本身或很近的物体），沿着射线向前推远
                    if (distFromCamera < MIN_DISTANCE_FROM_CAMERA)
                    {
                        float t = MIN_DISTANCE_FROM_CAMERA;
                        t = Mathf.Clamp(t, MIN_DISTANCE_FROM_CAMERA, MAX_AIM_DISTANCE);
                        return ray.origin + ray.direction * t;
                    }
                    
                    // 如果有枪口，还需要确保瞄准点距离枪口足够远（用于子弹方向计算）
                    if (hasMuzzle)
                    {
                        float distToMuzzle = Vector3.Distance(hit.point, muzzlePos);
                        if (distToMuzzle < MIN_DISTANCE_FROM_MUZZLE)
                        {
                            // 沿着射线方向推远，确保距离枪口足够远
                            // 计算从枪口到射线的投影点
                            Vector3 toHit = hit.point - muzzlePos;
                            Vector3 proj = Vector3.Project(toHit, ray.direction);
                            
                            float t = MIN_DISTANCE_FROM_MUZZLE / ray.direction.magnitude;
                            if (proj.magnitude > MIN_PROJECTION_MAGNITUDE)
                            {
                                t = MIN_DISTANCE_FROM_MUZZLE / proj.magnitude;
                            }
                            
                            t = Mathf.Clamp(t, MIN_DISTANCE_FROM_MUZZLE / 100f, MAX_AIM_DISTANCE);
                            return muzzlePos + ray.direction.normalized * MIN_DISTANCE_FROM_MUZZLE;
                        }
                    }
                    
                    // 距离足够，返回命中点（这就是准心指向的点）
                    return hit.point;
                }
            }
            catch
            {
                // 射线检测失败时使用默认距离
            }

            // 没有命中任何物体：使用远距点
            // 从相机位置沿着forward方向延伸，确保距离足够远
            float defaultDistance = DEFAULT_AIM_DISTANCE;
            if (hasMuzzle)
            {
                // 确保至少距离枪口最小距离
                defaultDistance = Mathf.Max(MIN_DISTANCE_FROM_MUZZLE, defaultDistance);
            }
            defaultDistance = Mathf.Clamp(defaultDistance, MIN_DISTANCE_FROM_CAMERA, MAX_AIM_DISTANCE);
            return ray.origin + ray.direction * defaultDistance;
        }
        #endregion
    }
}


