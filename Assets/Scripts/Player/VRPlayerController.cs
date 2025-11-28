using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

/// <summary>
/// VR 플레이어 컨트롤러
/// XR Rig 기반, 컨트롤러 조이스틱으로 이동
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class VRPlayerController : MonoBehaviourPun
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.0f;
    public float gravity = -9.81f;
    public float turnSpeed = 60.0f;

    [Header("VR References")]
    public XRNode leftControllerNode = XRNode.LeftHand;
    public XRNode rightControllerNode = XRNode.RightHand;
    public Transform cameraTransform; // XR Rig의 Main Camera

    [Header("Movement Lock")]
    public bool isMovementLocked = false;

    private CharacterController characterController;
    private Vector3 velocity;
    private InputDevice leftController;
    private InputDevice rightController;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        // 로컬 플레이어가 아니면 비활성화
        if (!photonView.IsMine)
        {
            // 원격 플레이어는 이동 처리 안 함
            enabled = false;

            // 원격 플레이어의 Camera Rig는 비활성화 (내 화면에서 안 보이게)
            Transform cameraRig = transform.Find("[BuildingBlock] Camera Rig");
            if (cameraRig != null)
            {
                // 카메라와 컨트롤러는 비활성화 (아바타 몸통만 보이게)
                Camera remoteCamera = cameraRig.GetComponentInChildren<Camera>();
                if (remoteCamera != null)
                {
                    remoteCamera.gameObject.SetActive(false);
                }
            }
            return;
        }

        // 로컬 플레이어: 자신의 아바타 모델 숨기기
        Transform avatarBody = transform.Find("AvatarBody");
        if (avatarBody != null)
        {
            avatarBody.gameObject.SetActive(false);
            Debug.Log("[VRPlayerController] 로컬 플레이어 아바타 숨김");
        }

        // VR 컨트롤러 초기화
        InitializeControllers();

        // 카메라 찾기 (XR Rig의 Main Camera)
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // 컨트롤러 재연결 확인
        if (!leftController.isValid || !rightController.isValid)
        {
            InitializeControllers();
        }

        HandleMovement();
        HandleRotation();
        ApplyGravity();
    }

    void InitializeControllers()
    {
        leftController = InputDevices.GetDeviceAtXRNode(leftControllerNode);
        rightController = InputDevices.GetDeviceAtXRNode(rightControllerNode);

        Debug.Log($"[VRPlayerController] Left Controller: {leftController.name}, Valid: {leftController.isValid}");
        Debug.Log($"[VRPlayerController] Right Controller: {rightController.name}, Valid: {rightController.isValid}");
    }

    void HandleMovement()
    {
        if (isMovementLocked) return;

        // 왼쪽 컨트롤러 조이스틱으로 이동
        Vector2 leftStickInput = Vector2.zero;
        if (leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftStickInput))
        {
            // 카메라 방향 기준으로 이동
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Y축 제거 (수평 이동만)
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * leftStickInput.y + right * leftStickInput.x);
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
        }
    }

    void HandleRotation()
    {
        if (isMovementLocked) return;

        // 오른쪽 컨트롤러 조이스틱으로 회전 (스냅 턴)
        Vector2 rightStickInput = Vector2.zero;
        if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStickInput))
        {
            // 좌우 스냅 턴 (30도씩)
            if (Mathf.Abs(rightStickInput.x) > 0.7f)
            {
                float turnAmount = Mathf.Sign(rightStickInput.x) * turnSpeed * Time.deltaTime;
                transform.Rotate(0, turnAmount, 0);
            }
        }
    }

    void ApplyGravity()
    {
        // CharacterController가 활성화되어 있을 때만 실행
        if (characterController == null || !characterController.enabled)
        {
            return;
        }

        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// 이동 잠금/해제
    /// </summary>
    public void SetMovementLock(bool locked)
    {
        isMovementLocked = locked;
        Debug.Log($"[VRPlayerController] 이동 {(locked ? "잠김" : "해제")}");
    }
}
