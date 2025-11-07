using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 受击特效模块
    /// 负责在第一人称模式下处理玩家受到伤害时的视觉反馈效果，
    /// 包括后处理效果（暗角、色差、镜头畸变等）和UI闪烁效果
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        /// <summary>
        /// 选项键：受击特效是否启用
        /// </summary>
        private const string HitFxEnableKey = "FirstPersonCamera_HitFx_Enable";
        
        /// <summary>
        /// 选项键：受击特效后处理强度（0.0-1.0）
        /// </summary>
        private const string HitFxPostStrengthKey = "FirstPersonCamera_HitFx_PostStrength";
        
        /// <summary>
        /// 选项键：受击特效UI强度（0.0-1.0）
        /// </summary>
        private const string HitFxUiStrengthKey = "FirstPersonCamera_HitFx_UiStrength";
        
        /// <summary>
        /// 选项键：受击特效显示方向指示（已移除功能，保留用于兼容性）
        /// </summary>
        private const string HitFxShowDirKey = "FirstPersonCamera_HitFx_ShowDirection";
        
        /// <summary>
        /// 后处理Volume优先级（确保在最高层显示）
        /// </summary>
        private const float HitFxVolumePriority = 99f;
        
        /// <summary>
        /// Canvas排序顺序（确保在最上层显示）
        /// </summary>
        private const int HitFxCanvasSortOrder = 5000;
        
        /// <summary>
        /// 默认伤害参考值（用于计算强度曲线）
        /// </summary>
        private const float DefaultDamageReference = 30f;
        
        /// <summary>
        /// 特效达到峰值的时间（秒）
        /// </summary>
        private const float HitFxPeakTime = 0.12f;
        
        /// <summary>
        /// 特效衰减时间（秒）
        /// </summary>
        private const float HitFxDecayTime = 0.55f;
        
        /// <summary>
        /// 暗角效果基础强度
        /// </summary>
        private const float VignetteBaseIntensity = 0.45f;
        
        /// <summary>
        /// 色差效果基础强度
        /// </summary>
        private const float ChromaticAberrationBaseIntensity = 0.35f;
        
        /// <summary>
        /// 镜头畸变效果基础强度（负值表示向内收缩）
        /// </summary>
        private const float LensDistortionBaseIntensity = -0.05f;
        
        /// <summary>
        /// 色彩调整饱和度变化基础强度（负值表示去饱和度）
        /// </summary>
        private const float ColorSaturationBaseIntensity = -10f;
        
        /// <summary>
        /// UI闪烁效果基础Alpha值
        /// </summary>
        private const float UiFlashBaseAlpha = 0.35f;
        
        /// <summary>
        /// 暗角颜色（红色调）
        /// </summary>
        private static readonly Color VignetteColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        
        /// <summary>
        /// UI闪烁颜色（红色调）
        /// </summary>
        private static readonly Color UiFlashColor = new Color(0.7f, 0.05f, 0.05f, 0f);
        
        /// <summary>
        /// 暗角平滑度
        /// </summary>
        private const float VignetteSmoothness = 0.8f;
        
        /// <summary>
        /// 默认后处理强度（如果选项加载失败）
        /// </summary>
        private const float DefaultPostStrength = 0.75f;
        
        /// <summary>
        /// 默认UI强度（如果选项加载失败）
        /// </summary>
        private const float DefaultUiStrength = 0.60f;
        
        /// <summary>
        /// 防止除零的最小值
        /// </summary>
        private const float Epsilon = 0.0001f;
        #endregion

        #region 私有字段
        /// <summary>
        /// 受击特效后处理Volume组件
        /// </summary>
        private Volume hitFxVolume;
        
        /// <summary>
        /// 受击特效后处理Volume配置文件
        /// </summary>
        private VolumeProfile hitFxProfile;
        
        /// <summary>
        /// 暗角效果组件
        /// </summary>
        private Vignette fxVignette;
        
        /// <summary>
        /// 色差效果组件
        /// </summary>
        private ChromaticAberration fxCA;
        
        /// <summary>
        /// 镜头畸变效果组件
        /// </summary>
        private LensDistortion fxLD;
        
        /// <summary>
        /// 色彩调整效果组件
        /// </summary>
        private ColorAdjustments fxColor;
        
        /// <summary>
        /// 受击特效UI Canvas组件
        /// </summary>
        private Canvas hitFxCanvas;
        
        /// <summary>
        /// 受击特效全屏闪烁图像
        /// </summary>
        private Image hitFlashImage;
        
        /// <summary>
        /// 当前运行的受击特效协程
        /// </summary>
        private Coroutine hitFxRoutine;
        #endregion

        #region 初始化方法
        /// <summary>
        /// 确保受击特效系统已初始化
        /// 如果尚未初始化，则创建后处理Volume和UI Canvas
        /// </summary>
        private void EnsureHitFxSetup()
        {
            // 初始化后处理Volume
            if (hitFxVolume == null)
            {
                InitializePostProcessingVolume();
            }

            // 初始化UI Canvas
            if (hitFxCanvas == null)
            {
                InitializeHitFxCanvas();
            }
        }

        /// <summary>
        /// 初始化后处理Volume和所有后处理效果组件
        /// </summary>
        private void InitializePostProcessingVolume()
        {
            // 创建Volume GameObject
            var volumeGO = new GameObject("FPC_HitFxVolume");
            volumeGO.transform.SetParent(this.transform, false);
            
            // 添加并配置Volume组件
            hitFxVolume = volumeGO.AddComponent<Volume>();
            hitFxVolume.isGlobal = true;
            hitFxVolume.priority = HitFxVolumePriority;
            
            // 创建Volume配置文件
            hitFxProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            hitFxVolume.sharedProfile = hitFxProfile;

            // 添加并配置暗角效果
            fxVignette = hitFxProfile.Add<Vignette>(true);
            fxVignette.active = true;
            fxVignette.intensity.Override(0f);
            fxVignette.smoothness.Override(VignetteSmoothness);
            fxVignette.color.Override(VignetteColor);

            // 添加并配置色差效果
            fxCA = hitFxProfile.Add<ChromaticAberration>(true);
            fxCA.active = true;
            fxCA.intensity.Override(0f);

            // 添加并配置镜头畸变效果
            fxLD = hitFxProfile.Add<LensDistortion>(true);
            fxLD.active = true;
            fxLD.intensity.Override(0f);
            fxLD.scale.Override(1f);

            // 添加并配置色彩调整效果
            fxColor = hitFxProfile.Add<ColorAdjustments>(true);
            fxColor.active = true;
            fxColor.saturation.Override(0f);
        }

        /// <summary>
        /// 初始化受击特效UI Canvas和全屏闪烁图像
        /// </summary>
        private void InitializeHitFxCanvas()
        {
            // 加载默认UI Sprite（如果尚未加载）
            if (uiDefaultSprite == null)
            {
                try
                {
                    uiDefaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
                }
                catch
                {
                    uiDefaultSprite = null;
                }
            }
            
            // 创建Canvas GameObject
            var canvasGO = new GameObject("FPC_HitFxCanvas");
            hitFxCanvas = canvasGO.AddComponent<Canvas>();
            hitFxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hitFxCanvas.sortingOrder = HitFxCanvasSortOrder;
            
            // 添加CanvasScaler以支持不同分辨率
            canvasGO.AddComponent<CanvasScaler>();
            
            // 添加GraphicRaycaster但禁用以避免阻挡输入
            var graphicRaycaster = canvasGO.AddComponent<GraphicRaycaster>();
            graphicRaycaster.enabled = false;

            // 创建全屏闪烁图像
            var flashGO = new GameObject("Flash");
            flashGO.transform.SetParent(canvasGO.transform, false);
            
            hitFlashImage = flashGO.AddComponent<Image>();
            if (uiDefaultSprite != null)
            {
                hitFlashImage.sprite = uiDefaultSprite;
            }
            hitFlashImage.type = Image.Type.Sliced;
            hitFlashImage.color = UiFlashColor;
            
            // 设置RectTransform为全屏
            var rectTransform = hitFlashImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // 注意：方向指示UI已根据要求移除
        }
        #endregion

        #region 选项加载方法
        /// <summary>
        /// 加载后处理强度选项
        /// </summary>
        /// <returns>后处理强度值（0.0-1.0），如果加载失败则返回默认值</returns>
        private float LoadHitFxPostStrength()
        {
            try
            {
                return Mathf.Clamp01(FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(HitFxPostStrengthKey, DefaultPostStrength));
            }
            catch
            {
                return DefaultPostStrength;
            }
        }

        /// <summary>
        /// 加载UI强度选项
        /// </summary>
        /// <returns>UI强度值（0.0-1.0），如果加载失败则返回默认值</returns>
        private float LoadHitFxUiStrength()
        {
            try
            {
                return Mathf.Clamp01(FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(HitFxUiStrengthKey, DefaultUiStrength));
            }
            catch
            {
                return DefaultUiStrength;
            }
        }

        /// <summary>
        /// 加载受击特效启用选项
        /// </summary>
        /// <returns>是否启用受击特效，如果加载失败则返回true（默认启用）</returns>
        private bool LoadHitFxEnable()
        {
            try
            {
                return FirstPersonCamera.Utilities.OptionsHelper.LoadInt(HitFxEnableKey, 1) == 1;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// 加载受击特效显示方向选项（已废弃，保留用于兼容性）
        /// </summary>
        /// <returns>是否显示方向指示，如果加载失败则返回true</returns>
        private bool LoadHitFxShowDir()
        {
            try
            {
                return FirstPersonCamera.Utilities.OptionsHelper.LoadInt(HitFxShowDirKey, 1) == 1;
            }
            catch
            {
                return true;
            }
        }
        #endregion

        #region 公共接口
        /// <summary>
        /// 触发受击特效
        /// 当玩家受到伤害时调用此方法以显示视觉反馈
        /// </summary>
        /// <param name="damage">受到的伤害值</param>
        /// <param name="damagePointWorld">伤害点世界坐标（可选，当前未使用）</param>
        public void OnHitEffect(float damage, Vector3? damagePointWorld)
        {
            // 仅在第一人称模式下处理
            if (!isFirstPersonMode) return;
            
            // 检查是否启用受击特效
            if (!LoadHitFxEnable()) return;
            
            // 确保系统已初始化
            EnsureHitFxSetup();

            // 计算伤害强度（使用平方根曲线软化大值）
            float clampedDamage = Mathf.Max(0f, damage);
            float postStrength = LoadHitFxPostStrength();
            float uiStrength = LoadHitFxUiStrength();
            
            // 使用平方根曲线计算强度因子，使大伤害值不会过度强烈
            float intensityFactor = Mathf.Clamp01(Mathf.Sqrt(clampedDamage / DefaultDamageReference));

            // 注意：方向指示功能已移除，保留参数用于兼容性
            float angleDeg = 0f;
            bool showDir = false;

            // 停止当前运行的协程（如果存在）并启动新的受击特效协程
            if (hitFxRoutine != null)
            {
                StopCoroutine(hitFxRoutine);
            }
            hitFxRoutine = StartCoroutine(CoHitFx(intensityFactor, postStrength, uiStrength, showDir, angleDeg));
            
            // 受击后重新应用部位隐藏，防止受击特效或其他机制导致隐藏的部位重新显示
            // 使用协程延迟一小段时间，确保在受击特效处理完成后再应用隐藏
            StartCoroutine(ReapplyObstructionHidingAfterHit());
        }
        
        /// <summary>
        /// 受击后重新应用部位隐藏的协程
        /// 延迟一小段时间后重新应用隐藏，确保隐藏的部位保持隐藏
        /// </summary>
        private IEnumerator ReapplyObstructionHidingAfterHit()
        {
            // 等待一帧，让受击特效和其他系统完成处理
            yield return null;
            
            // 再等待一小段时间，确保所有受击相关的渲染更新完成
            yield return new WaitForSeconds(0.1f);
            
            // 重新应用部位隐藏
            try
            {
                HideFirstPersonObstructions();
            }
            catch
            {
                // 处理失败，静默处理
            }
        }
        #endregion

        #region 特效协程
        /// <summary>
        /// 受击特效协程
        /// 在指定时间内播放后处理效果和UI闪烁动画
        /// </summary>
        /// <param name="intensityFactor">强度因子（0.0-1.0），基于伤害值计算</param>
        /// <param name="postStrength">后处理强度（0.0-1.0），来自选项设置</param>
        /// <param name="uiStrength">UI强度（0.0-1.0），来自选项设置</param>
        /// <param name="showDir">是否显示方向指示（已废弃）</param>
        /// <param name="angleDeg">伤害方向角度（已废弃）</param>
        private IEnumerator CoHitFx(float intensityFactor, float postStrength, float uiStrength, bool showDir, float angleDeg)
        {
            float elapsedTime = 0f;
            float totalDuration = HitFxPeakTime + HitFxDecayTime;

            // 初始化UI闪烁为完全透明
            if (hitFlashImage != null)
            {
                var color = hitFlashImage.color;
                color.a = 0f;
                hitFlashImage.color = color;
            }

            // 动画循环：从峰值到衰减
            while (elapsedTime < totalDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                
                // 计算当前时间点的动画进度（0.0-1.0）
                // 在峰值时间内线性增长，在衰减时间内线性减少
                float animationProgress = 0f;
                if (elapsedTime <= HitFxPeakTime)
                {
                    // 峰值阶段：从0增长到1
                    animationProgress = elapsedTime / Mathf.Max(Epsilon, HitFxPeakTime);
                }
                else
                {
                    // 衰减阶段：从1减少到0
                    float decayProgress = (elapsedTime - HitFxPeakTime) / Mathf.Max(Epsilon, HitFxDecayTime);
                    animationProgress = Mathf.Clamp01(1f - decayProgress);
                }

                // 应用后处理效果
                ApplyPostProcessingEffects(postStrength, intensityFactor, animationProgress);

                // 应用UI闪烁效果
                ApplyUiFlashEffect(uiStrength, intensityFactor, animationProgress);

                yield return null;
            }

            // 动画结束，清除所有效果
            ClearAllEffects();
        }

        /// <summary>
        /// 应用后处理效果
        /// </summary>
        /// <param name="postStrength">后处理强度</param>
        /// <param name="intensityFactor">强度因子</param>
        /// <param name="animationProgress">动画进度</param>
        private void ApplyPostProcessingEffects(float postStrength, float intensityFactor, float animationProgress)
        {
            float combinedIntensity = postStrength * intensityFactor * animationProgress;

            // 应用暗角效果
            if (fxVignette != null)
            {
                fxVignette.intensity.value = VignetteBaseIntensity * combinedIntensity;
            }

            // 应用色差效果
            if (fxCA != null)
            {
                fxCA.intensity.value = ChromaticAberrationBaseIntensity * combinedIntensity;
            }

            // 应用镜头畸变效果（负值表示向内收缩）
            if (fxLD != null)
            {
                fxLD.intensity.value = LensDistortionBaseIntensity * combinedIntensity;
            }

            // 应用色彩调整效果（降低饱和度）
            if (fxColor != null)
            {
                fxColor.saturation.value = ColorSaturationBaseIntensity * combinedIntensity;
            }
        }

        /// <summary>
        /// 应用UI闪烁效果
        /// </summary>
        /// <param name="uiStrength">UI强度</param>
        /// <param name="intensityFactor">强度因子</param>
        /// <param name="animationProgress">动画进度</param>
        private void ApplyUiFlashEffect(float uiStrength, float intensityFactor, float animationProgress)
        {
            if (hitFlashImage != null)
            {
                var color = hitFlashImage.color;
                color.a = UiFlashBaseAlpha * uiStrength * intensityFactor * animationProgress;
                hitFlashImage.color = color;
            }
        }

        /// <summary>
        /// 清除所有受击特效
        /// 将后处理效果和UI闪烁重置为初始状态
        /// </summary>
        private void ClearAllEffects()
        {
            // 清除后处理效果
            if (fxVignette != null)
            {
                fxVignette.intensity.value = 0f;
            }
            if (fxCA != null)
            {
                fxCA.intensity.value = 0f;
            }
            if (fxLD != null)
            {
                fxLD.intensity.value = 0f;
            }
            if (fxColor != null)
            {
                fxColor.saturation.value = 0f;
            }

            // 清除UI闪烁
            if (hitFlashImage != null)
            {
                var color = hitFlashImage.color;
                color.a = 0f;
                hitFlashImage.color = color;
            }
        }
        #endregion
    }
}
