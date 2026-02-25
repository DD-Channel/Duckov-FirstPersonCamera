using HarmonyLib;
using FirstPersonCamera;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁 - 视觉后坐力控制
    /// 在第一人称模式下，禁止武器自身的视觉后坐力位移，
    /// 避免与第一人称相机的ADS武器位置管理产生冲突
    /// 防止武器局部位姿被原游戏逻辑改回去
    /// </summary>
    [HarmonyPatch(typeof(ItemAgent_Gun))]
    internal static class RecoilPatch
    {
        #region Harmony补丁方法
        /// <summary>
        /// 开始视觉后坐力的前置补丁
        /// 在第一人称模式下阻止武器开始视觉后坐力动画
        /// </summary>
        /// <returns>如果返回false则跳过原方法，如果返回true则执行原方法</returns>
        [HarmonyPatch("StartVisualRecoil")]
        [HarmonyPrefix]
        private static bool BlockStartVisualRecoil()
        {
            var instance = FirstPersonCameraController.Instance;
            if (instance != null && instance.IsFirstPersonMode)
            {
                // 第一人称模式下跳过原方法，禁止视觉后坐力
                return false;
            }
            
            // 第三人称模式下保留原方法行为
            return true;
        }

        /// <summary>
        /// 更新视觉后坐力的前置补丁
        /// 在第一人称模式下阻止武器更新视觉后坐力，
        /// 防止每帧localPosition被原逻辑重设
        /// </summary>
        /// <returns>如果返回false则跳过原方法，如果返回true则执行原方法</returns>
        [HarmonyPatch("UpdateVisualRecoil")]
        [HarmonyPrefix]
        private static bool BlockUpdateVisualRecoil()
        {
            var instance = FirstPersonCameraController.Instance;
            if (instance != null && instance.IsFirstPersonMode)
            {
                // 第一人称模式下跳过原方法，防止每帧localPosition被重设
                return false;
            }
            
            // 第三人称模式下保留原方法行为
            return true;
        }
        #endregion
    }
}

