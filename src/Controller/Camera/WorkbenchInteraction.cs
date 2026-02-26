using System.Collections;
using System.IO;
using System;
using UnityEngine;
using FirstPersonCamera.Utilities; // 如果需要 FPLogger

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        [Header("工作台交互调试")]
        [Tooltip("三个交互点的视角位置（可在 Inspector 中赋值）")]
        [SerializeField] private Transform workbenchViewPoint1;
        [SerializeField] private Transform workbenchViewPoint2;
        [SerializeField] private Transform workbenchViewPoint3;

        // 当前交互状态
        private bool isWorkbenchInteracting = false;
        private Coroutine workbenchTransitionCoroutine;

        // 日志路径
        private static readonly string WorkbenchLogPath = @"C:\temp\WorkbenchInteraction.log";

        // 用于保存玩家原始偏移（如果你需要恢复精确位置）
        private Vector3 preWorkbenchLocalPos;
        private Quaternion preWorkbenchLocalRot;

        // ========== 公共方法 ==========
        /// <summary>
        /// 开始与指定视角点的工作台交互
        /// </summary>
        /// <param name="viewPoint">工作台的视角 Transform</param>
        public void StartWorkbenchInteraction(Transform viewPoint)
        {
            if (!isFirstPersonMode)
            {
                LogToWorkbenchFile("[Start] 当前不在第一人称模式，忽略");
                return;
            }
            if (isWorkbenchInteracting)
            {
                LogToWorkbenchFile("[Start] 已在交互中，先结束当前交互");
                EndWorkbenchInteraction();
            }

            if (viewPoint == null)
            {
                LogToWorkbenchFile("[Start] 视角点为 null，无法开始");
                return;
            }

            LogToWorkbenchFile($"[Start] 开始交互，目标视角: {viewPoint.name}，位置: {viewPoint.position}，旋转: {viewPoint.rotation.eulerAngles}");

            // 启动协程
            if (workbenchTransitionCoroutine != null)
                StopCoroutine(workbenchTransitionCoroutine);
            workbenchTransitionCoroutine = StartCoroutine(TransitionToWorkbench(viewPoint));
        }

        /// <summary>
        /// 结束工作台交互，返回第一人称
        /// </summary>
        public void EndWorkbenchInteraction()
        {
            if (!isWorkbenchInteracting)
            {
                LogToWorkbenchFile("[End] 当前未在交互中，忽略");
                return;
            }

            LogToWorkbenchFile("[End] 结束交互，准备返回第一人称");

            if (workbenchTransitionCoroutine != null)
                StopCoroutine(workbenchTransitionCoroutine);
            workbenchTransitionCoroutine = StartCoroutine(TransitionFromWorkbench());
        }

        // ========== 私有方法 ==========
        private void LogToWorkbenchFile(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(WorkbenchLogPath));
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                File.AppendAllText(WorkbenchLogPath, line + Environment.NewLine);
            }
            catch { }
        }

        // 协程：进入工作台视角
        private IEnumerator TransitionToWorkbench(Transform targetView)
        {
            isWorkbenchInteracting = true;

            // 记录开始时的相机状态
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;
            Vector3 targetPos = targetView.position;
            Quaternion targetRot = targetView.rotation;

            LogToWorkbenchFile($"[Transition] 开始移动: 从 {startPos} / {startRot.eulerAngles} 到 {targetPos} / {targetRot.eulerAngles}");

            float duration = 1.0f; // 过渡时间 1 秒，可根据需要调整
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // 使用平滑曲线
                t = t * t * (3f - 2f * t); // smoothstep

                mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            // 确保最终位置准确
            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = targetRot;

            LogToWorkbenchFile("[Transition] 到达目标视角");
            workbenchTransitionCoroutine = null;
        }

        // 协程：返回第一人称
        private IEnumerator TransitionFromWorkbench()
        {
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;

            // 计算目标位置（复用现有的第一人称位置计算方法）
            Vector3 targetPos = GetFirstPersonTargetPosition();
            Quaternion targetRot = GetFirstPersonTargetRotation();

            LogToWorkbenchFile($"[Return] 从 {startPos} / {startRot.eulerAngles} 回到第一人称: {targetPos} / {targetRot.eulerAngles}");

            float duration = 1.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t); // smoothstep

                mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = targetRot;

            isWorkbenchInteracting = false;
            workbenchTransitionCoroutine = null;
            LogToWorkbenchFile("[Return] 已恢复第一人称");
        }

        // 辅助：获取第一人称相机的目标位置（复用你已有的逻辑）
        private Vector3 GetFirstPersonTargetPosition()
        {
            if (mainCharacter == null) return mainCamera.transform.position;

            Vector3 basePos;
            if (headSocket != null)
            {
                basePos = headSocket.position + Vector3.up * cameraHeightOffset;
            }
            else
            {
                basePos = mainCharacter.transform.position + Vector3.up * (1.7f + cameraHeightOffset);
            }

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 forward = rot * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

            basePos += forward * cameraForwardOffset + right * cameraRightOffset;
            basePos += right * peekOffset * peekMaxOffset;

            return basePos;
        }

        // 辅助：获取第一人称相机的目标旋转
        private Quaternion GetFirstPersonTargetRotation()
        {
            return Quaternion.Euler(pitch, yaw, -peekOffset * peekMaxRotation);
        }
    }
}