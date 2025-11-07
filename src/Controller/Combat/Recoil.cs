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
        /// <summary>
        /// 累积的垂直后坐力（向上偏移）
        /// </summary>
        private float accumulatedRecoilV = 0f;
        
        /// <summary>
        /// 累积的水平后坐力（左右偏移）
        /// </summary>
        private float accumulatedRecoilH = 0f;
        
        /// <summary>
        /// 当前使用的武器（用于获取恢复属性）
        /// </summary>
        private ItemAgent_Gun currentRecoilGun = null;
        
        /// <summary>
        /// 水平后坐力模式计数器（用于创建模式化后坐力）
        /// </summary>
        private int horizontalRecoilPattern = 0;
        
        /// <summary>
        /// 上次水平后坐力方向（用于模式化）
        /// </summary>
        private float lastHorizontalDirection = 1f;
        
        /// <summary>
        /// 是否正在射击（用于控制后坐力累积和恢复）
        /// </summary>
        private bool isShooting = false;
        
        /// <summary>
        /// 最近几发子弹的垂直后坐力记录（用于回弹计算）
        /// </summary>
        private System.Collections.Generic.List<float> recentRecoilV = 
            new System.Collections.Generic.List<float>();
        
        /// <summary>
        /// 最近几发子弹的水平后坐力记录（用于回弹计算）
        /// </summary>
        private System.Collections.Generic.List<float> recentRecoilH = 
            new System.Collections.Generic.List<float>();
        
        /// <summary>
        /// 最大记录的后坐力数量（用于回弹计算）
        /// </summary>
        private const int MAX_RECENT_RECOIL = 3;
        
        /// <summary>
        /// 水平后坐力模式重置阈值（每N发重置模式）
        /// </summary>
        private const int HORIZONTAL_RECOIL_PATTERN_RESET = 10;
        
        /// <summary>
        /// 水平后坐力方向延续概率（70%延续，30%反转）
        /// </summary>
        private const float HORIZONTAL_DIRECTION_CONTINUE_CHANCE = 0.7f;
        
        /// <summary>
        /// 水平后坐力随机反转概率（10%完全反转）
        /// </summary>
        private const float HORIZONTAL_DIRECTION_RANDOM_CHANCE = 0.1f;
        
        /// <summary>
        /// 高斯随机数的标准差（用于水平后坐力分布）
        /// </summary>
        private const float GAUSSIAN_STD_DEV = 0.3f;
        
        /// <summary>
        /// 高斯因子应用到水平后坐力的基础倍数
        /// </summary>
        private const float GAUSSIAN_BASE_MULTIPLIER = 0.5f;
        
        /// <summary>
        /// 高斯因子应用到水平后坐力的范围倍数
        /// </summary>
        private const float GAUSSIAN_RANGE_MULTIPLIER = 0.5f;
        #endregion

        #region 后坐力应用逻辑
        /// <summary>
        /// 处理鼠标松开时的后坐力重置
        /// 当松开鼠标时，停止射击标记，并重置累积后坐力为最近几发子弹的后坐力总和
        /// </summary>
        private void HandleMouseReleaseRecoilReset()
        {
            if (!isShooting) return;
            
            isShooting = false;
            
            // 只保留最近几发子弹的后坐力用于回弹
            // 清除更早的后坐力记录
            float last3RecoilV = 0f;
            float last3RecoilH = 0f;
            
            // 计算最近几发子弹的后坐力总和
            for (int i = 0; i < recentRecoilV.Count; i++)
            {
                last3RecoilV += recentRecoilV[i];
                last3RecoilH += recentRecoilH[i];
            }
            
            // 重置累积后坐力为最近几发子弹的后坐力
            accumulatedRecoilV = last3RecoilV;
            accumulatedRecoilH = last3RecoilH;
            
            // 清空记录（已转移到累积后坐力中）
            recentRecoilV.Clear();
            recentRecoilH.Clear();
        }

        /// <summary>
        /// 开火回调：根据武器配置施加垂直和水平后坐力（使用重新设计的后坐力映射系统）
        /// </summary>
        /// <param name="agent">武器代理</param>
        private void OnCharacterShoot(DuckovItemAgent agent)
        {
            if (!isFirstPersonMode) return;
            if (agent == null) return;
            
            ItemAgent_Gun gun = agent as ItemAgent_Gun;
            if (gun == null) return;
            
            try
            {
                // 检查后坐力功能是否启用
                bool recoilEnabled = true;
                try
                {
                    recoilEnabled = FirstPersonCamera.Utilities.OptionsHelper.LoadInt(
                        FirstPersonOptionsUI.EnableRecoilKey, 1) == 1;
                }
                catch
                {
                    recoilEnabled = true;
                }
                
                if (!recoilEnabled) return;
                
                // 获取后坐力倍数和强度
                float recoilMult = 1f;
                try
                {
                    recoilMult = LevelManager.Rule.RecoilMultiplier;
                }
                catch
                {
                    recoilMult = 1f;
                }
                
                float strengthMult = 0.1f;
                try
                {
                    strengthMult = FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(
                        FirstPersonOptionsUI.RecoilStrengthKey, 0.1f);
                }
                catch
                {
                    strengthMult = 0.1f;
                }
                
                // 获取原版的后坐力数值（基础值）
                float origVMin = gun.RecoilVMin;
                float origVMax = gun.RecoilVMax;
                float origHMin = gun.RecoilHMin;
                float origHMax = gun.RecoilHMax;
                
                // 使用映射函数将原版数据转换为更合理的后坐力值
                float mappedVMin = MapRecoilValue(origVMin);
                float mappedVMax = MapRecoilValue(origVMax);
                float mappedHMin = MapRecoilValue(origHMin);
                float mappedHMax = MapRecoilValue(origHMax);
                
                // 应用缩放系数和控制力
                float v = UnityEngine.Random.Range(mappedVMin, mappedVMax) * 
                          gun.RecoilScaleV * 
                          (1f / Mathf.Max(0.01f, gun.CharacterRecoilControl)) * 
                          recoilMult * 
                          strengthMult;
                
                // 水平后坐力（现代FPS游戏设计思路）
                // 使用更平滑的随机分布，并添加轻微的模式化特征
                float hBase = UnityEngine.Random.Range(mappedHMin, mappedHMax) * 
                             gun.RecoilScaleH * 
                             (1f / Mathf.Max(0.01f, gun.CharacterRecoilControl)) * 
                             recoilMult * 
                             strengthMult;
                
                // 水平后坐力方向：使用更智能的分布
                // 1. 有轻微的模式化倾向（不是完全随机，而是有轻微的方向延续性）
                // 2. 使用高斯分布而不是均匀分布，使大多数情况更接近中心
                float hDirection;
                if (horizontalRecoilPattern == 0)
                {
                    // 第一发：完全随机
                    hDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                }
                else
                {
                    // 后续发：70%概率延续上次方向，30%概率反转（创建轻微模式）
                    float patternChance = UnityEngine.Random.value;
                    if (patternChance < HORIZONTAL_DIRECTION_CONTINUE_CHANCE)
                    {
                        // 延续上次方向
                        hDirection = lastHorizontalDirection;
                    }
                    else
                    {
                        // 反转方向
                        hDirection = -lastHorizontalDirection;
                    }
                    
                    // 添加少量随机性（±10%的方向变化）
                    if (UnityEngine.Random.value < HORIZONTAL_DIRECTION_RANDOM_CHANCE)
                    {
                        hDirection = -hDirection; // 10%概率完全反转
                    }
                }
                
                // 使用高斯分布调整大小，使大多数射击更接近中心值
                float gaussianFactor = Mathf.Clamp01(Mathf.Abs(GaussianRandom(0f, GAUSSIAN_STD_DEV)));
                hBase = hBase * (GAUSSIAN_BASE_MULTIPLIER + GAUSSIAN_RANGE_MULTIPLIER * gaussianFactor);
                
                float h = hBase * hDirection;
                
                // 更新模式状态
                horizontalRecoilPattern++;
                lastHorizontalDirection = hDirection;
                
                // 每10发重置模式，避免过度模式化
                if (horizontalRecoilPattern >= HORIZONTAL_RECOIL_PATTERN_RESET)
                {
                    horizontalRecoilPattern = 0;
                }
                
                // 如果切换了武器，重置累积后坐力
                if (currentRecoilGun != null && currentRecoilGun != gun)
                {
                    // 快速清除旧武器的后坐力，避免状态混乱
                    accumulatedRecoilV = 0f;
                    accumulatedRecoilH = 0f;
                    horizontalRecoilPattern = 0; // 重置水平模式
                    recentRecoilV.Clear(); // 清除最近几发子弹的记录
                    recentRecoilH.Clear();
                }
                
                // 记录当前武器（用于获取恢复属性）
                currentRecoilGun = gun;
                
                // 标记正在射击
                isShooting = true;
                
                // 记录最近几发子弹的后坐力（用于回弹）
                recentRecoilV.Add(v);
                recentRecoilH.Add(h);
                
                // 保持最多记录最近几发子弹
                if (recentRecoilV.Count > MAX_RECENT_RECOIL)
                {
                    recentRecoilV.RemoveAt(0); // 移除最旧的记录
                    recentRecoilH.RemoveAt(0);
                }
                
                // 累积所有后坐力（用于立即应用）
                accumulatedRecoilV += v;
                accumulatedRecoilH += h;
                
                // 立即应用后坐力：垂直影响pitch，水平影响yaw
                pitch = Mathf.Clamp(pitch - v, -89f, 89f);
                yaw += h;
            }
            catch
            {
                // 处理失败时静默处理，避免影响游戏运行
            }
        }

        /// <summary>
        /// 将原版的后坐力值映射到更合理的范围
        /// 原版数据范围很大（0-50+），大多数武器在40左右无法压枪
        /// 映射后保持相对差异，但压缩到合理可压的范围
        /// </summary>
        /// <param name="originalValue">原版的后坐力值</param>
        /// <returns>映射后的后坐力值</returns>
        private float MapRecoilValue(float originalValue)
        {
            // 使用分段映射函数，将原版数据压缩到合理范围
            // 映射策略：
            // 0-10: 线性映射到 0-2 (易压枪)
            // 10-20: 线性映射到 2-4 (正常)
            // 20-30: 使用平方根压缩映射到 4-6 (难压但可压)
            // 30+: 使用对数压缩，映射到 6-8 (高后坐力但依然可压)
            
            if (originalValue <= 0f) return 0f;
            
            if (originalValue <= 10f)
            {
                // 0-10 -> 0-2 (线性)
                return originalValue * 0.2f;
            }
            else if (originalValue <= 20f)
            {
                // 10-20 -> 2-4 (线性)
                return 2f + (originalValue - 10f) * 0.2f;
            }
            else if (originalValue <= 30f)
            {
                // 20-30 -> 4-6 (使用平方根压缩高值)
                float normalized = (originalValue - 20f) / 10f; // 0-1
                float compressed = Mathf.Sqrt(normalized); // 平方根压缩
                return 4f + compressed * 2f;
            }
            else if (originalValue <= 50f)
            {
                // 30-50 -> 6-8 (使用对数进一步压缩)
                float normalized = (originalValue - 30f) / 20f; // 0-1
                // 使用更平滑的压缩曲线
                float compressed = Mathf.Pow(normalized, 0.6f); // 幂函数压缩
                return 6f + compressed * 2f;
            }
            else
            {
                // 50+ -> 8-9 (极高后坐力，但依然可压)
                float excess = originalValue - 50f;
                float compressed = Mathf.Log(excess + 1f) / Mathf.Log(50f); // 对数压缩
                return 8f + Mathf.Clamp01(compressed) * 1f;
            }
        }
        
        /// <summary>
        /// 生成高斯分布的随机数（Box-Muller变换）
        /// </summary>
        /// <param name="mean">均值</param>
        /// <param name="stdDev">标准差</param>
        /// <returns>高斯分布的随机数</returns>
        private float GaussianRandom(float mean, float stdDev)
        {
            float u1 = 1f - UnityEngine.Random.value; // 避免0
            float u2 = 1f - UnityEngine.Random.value;
            float z0 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
            return mean + stdDev * z0;
        }
        #endregion
    }
}

