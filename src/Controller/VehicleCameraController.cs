using UnityEngine;
using Duckov;
using FirstPersonCamera;
using UnityEngine.InputSystem;
using System.Collections;
using FirstPersonCamera.Utilities;

public class VehicleCameraController : MonoBehaviour
{
    private FirstPersonCameraController fpsController;
    private Camera mainCamera;
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool isPossessing;
    private bool originalSaved = false;
    private CharacterMainControl currentVehicle;

    // 车辆第三人称参数
    private float vehicleDistance = 3.5f;
    private float vehicleYaw = 0f;
    private float vehiclePitch = 3f;
    private float vehicleMinPitch = -30f;
    private float vehicleMaxPitch = 60f;

    // 鼠标灵敏度
    public float mouseSensitivity = 3f;
    private bool useNewInputSystem;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        fpsController = FirstPersonCameraController.Instance;
        if (fpsController == null) return;

        useNewInputSystem = Keyboard.current != null && Mouse.current != null;

        LevelManager.OnControllingCharacterChanged += OnControllingCharacterChanged;
    }

    private void OnDestroy()
    {
        LevelManager.OnControllingCharacterChanged -= OnControllingCharacterChanged;
    }

    private bool EnsureCamera()
    {
        if (mainCamera != null) return true;

        if (fpsController != null)
            mainCamera = fpsController.MainCamera;

        if (mainCamera == null && GameCamera.Instance != null)
            mainCamera = GameCamera.Instance.renderCamera;

        if (mainCamera == null) return false;

        if (!originalSaved)
        {
            originalParent = mainCamera.transform.parent;
            originalLocalPos = mainCamera.transform.localPosition;
            originalLocalRot = mainCamera.transform.localRotation;
            originalSaved = true;
        }
        return true;
    }

    // 检测是否为自爆车：通过 AISpecialAttachment_BoomCar 组件
    private bool IsBoomCar(CharacterMainControl character)
    {
        if (character == null) return false;
        return character.GetComponentInChildren<AISpecialAttachment_BoomCar>() != null;
    }

    private void OnControllingCharacterChanged(CharacterMainControl newControlled)
    {
        if (fpsController == null) return;
        if (!EnsureCamera()) return;

        bool isMain = newControlled == CharacterMainControl.Main;

        if (isPossessing)
        {
            // 如果当前正在接管，当新角色是主角或不是自爆车时停止
            if (isMain || !IsBoomCar(newControlled))
            {
                StopPossessing();
            }
        }
        else
        {
            // 当前未接管，如果新角色不是主角且是自爆车，则开始接管
            if (!isMain && IsBoomCar(newControlled))
            {
                StartPossessingVehicle(newControlled);
            }
        }
    }

    private void StartPossessingVehicle(CharacterMainControl target)
    {
        fpsController.EnableCameraUpdate = false;
        isPossessing = true;
        currentVehicle = target;

        if (CharacterMainControl.Main != null && CharacterMainControl.Main.Health != null)
        {
            CharacterMainControl.Main.Health.SetInvincible(true);
        }

        mainCamera.transform.SetParent(null);
        vehicleYaw = 0f;
        vehiclePitch = 15f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        FPLogger.Log("开始接管自爆车视角");
    }

    private void StopPossessing()
    {
        isPossessing = false;
        currentVehicle = null;
        fpsController.EnableCameraUpdate = true;

        if (originalParent != null)
        {
            mainCamera.transform.SetParent(originalParent);
            mainCamera.transform.localPosition = originalLocalPos;
            mainCamera.transform.localRotation = originalLocalRot;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(DelayedInvincibleOff());

        FPLogger.Log("退出自爆车视角");
    }

    private IEnumerator DelayedInvincibleOff()
    {
        yield return new WaitForSeconds(0.1f);
        if (CharacterMainControl.Main != null && CharacterMainControl.Main.Health != null)
        {
            CharacterMainControl.Main.Health.SetInvincible(false);
        }
    }

    private void LateUpdate()
    {
        if (!isPossessing || currentVehicle == null || mainCamera == null)
            return;

        float mouseX = 0f, mouseY = 0f;
        try
        {
            if (useNewInputSystem)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    mouseX = delta.x;
                    mouseY = delta.y;
                }
            }
            else
            {
                mouseX = Input.GetAxis("Mouse X");
                mouseY = Input.GetAxis("Mouse Y");
            }
        }
        catch { }

        vehicleYaw += mouseX * mouseSensitivity * 0.1f;
        vehiclePitch -= mouseY * mouseSensitivity * 0.1f;
        vehiclePitch = Mathf.Clamp(vehiclePitch, vehicleMinPitch, vehicleMaxPitch);

        Quaternion rotation = Quaternion.Euler(vehiclePitch, vehicleYaw, 0f);
        Vector3 direction = rotation * Vector3.back;
        Vector3 targetPos = currentVehicle.transform.position + direction * vehicleDistance;

        // 恢复为直接设置相机位置和旋转（无平滑）
        mainCamera.transform.position = targetPos;
        mainCamera.transform.LookAt(currentVehicle.transform.position + Vector3.up * 1.5f);

        Vector3 moveInput = Vector3.zero;
        try
        {
            if (useNewInputSystem)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.wKey.isPressed) moveInput.z += 1f;
                    if (keyboard.sKey.isPressed) moveInput.z -= 1f;
                    if (keyboard.aKey.isPressed) moveInput.x -= 1f;
                    if (keyboard.dKey.isPressed) moveInput.x += 1f;
                }
            }
            else
            {
                moveInput.x = Input.GetAxis("Horizontal");
                moveInput.z = Input.GetAxis("Vertical");
            }
        }
        catch { }

        if (moveInput.magnitude > 0.1f)
        {
            moveInput = moveInput.normalized;
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDir = camForward * moveInput.z + camRight * moveInput.x;

            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            currentVehicle.transform.rotation = Quaternion.Slerp(currentVehicle.transform.rotation, targetRot, Time.deltaTime * 5f);

            float speed = 10f;
            if (currentVehicle.movementControl != null)
            {
                currentVehicle.SetForceMoveVelocity(moveDir * speed);
            }
            else
            {
                currentVehicle.transform.position += moveDir * speed * Time.deltaTime;
            }
        }
    }
}