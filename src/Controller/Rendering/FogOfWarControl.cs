using System;
using UnityEngine;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 战争迷雾控制模块
    /// 注意：实际控制已由 Harmony 补丁 Patch_FogOfWarManager_Update 接管，
    /// 本模块仅负责订阅角色死亡事件，以便在必要时清理状态（ConditionalWeakTable 会自动管理）。
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 战争迷雾控制字段
        /// <summary>
        /// 是否已订阅角色死亡事件
        /// </summary>
        private bool subscribedToDeathEvent;
        #endregion

        #region 战争迷雾控制方法
        /// <summary>
        /// 初始化战争迷雾控制：订阅角色死亡事件
        /// </summary>
        private void InitializeFogOfWarControl()
        {
            if (subscribedToDeathEvent) return;

            try
            {
                LevelManager.OnMainCharacterDead += OnMainCharacterDead;
                subscribedToDeathEvent = true;
                FPLogger.Log("战争迷雾控制：已订阅角色死亡事件");
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "初始化战争迷雾控制失败");
            }
        }

        /// <summary>
        /// 更新战争迷雾状态（已被 Harmony 补丁替代，此方法保留为空以避免编译错误）
        /// </summary>
        private void UpdateFogOfWarState()
        {
            // Harmony 补丁已接管，无需任何操作
        }

        /// <summary>
        /// 清理战争迷雾控制：取消事件订阅
        /// </summary>
        private void CleanupFogOfWarControl()
        {
            if (!subscribedToDeathEvent) return;

            try
            {
                LevelManager.OnMainCharacterDead -= OnMainCharacterDead;
                subscribedToDeathEvent = false;
                FPLogger.Log("战争迷雾控制：已取消角色死亡事件订阅");
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "清理战争迷雾控制失败");
            }
        }

        /// <summary>
        /// 角色死亡事件处理：记录日志，Harmony 补丁的 ConditionalWeakTable 会自动清理关联条目
        /// </summary>
        private void OnMainCharacterDead(DamageInfo dmgInfo)
        {
            FPLogger.Log("角色死亡，战争迷雾管理器即将重建，ConditionalWeakTable 关联将自动失效");
        }
        #endregion
    }
}