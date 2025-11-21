using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using Photon.Pun;

/// <summary>
/// VR에서 눌릴 수 있는 3D 버튼
/// VR 컨트롤러로 버튼을 가리키고 A버튼(Primary Button)을 누르면 작동
/// </summary>
public class VRButton : MonoBehaviourPun
{
    [Header("Button Settings")]
    public UnityEvent onButtonPressed;

    [Header("Visual Feedback")]
    public Renderer buttonRenderer;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color pressedColor = Color.green;

    [Header("Press Settings")]
    public float pressDistance = 0.1f;
    public float pressDuration = 0.5f;

    [Header("VR Controller Settings")]
    [Tooltip("VR 컨트롤러가 버튼을 가리킬 수 있는 최대 거리")]
    public float rayDistance = 10f;

    private Vector3 originalPosition;
    private bool isPressed = false;
    private Material buttonMaterial;
    private bool isHovering = false;

    // VR 컨트롤러 입력
    private InputDevice rightController;
    private InputDevice leftController;

    void Start()
    {
        originalPosition = transform.localPosition;

        // 버튼 렌더러 자동으로 찾기
        if (buttonRenderer == null)
        {
            // 1. 현재 GameObject에서 찾기
            buttonRenderer = GetComponent<Renderer>();

            // 2. 없으면 자식 오브젝트에서 찾기
            if (buttonRenderer == null)
            {
                buttonRenderer = GetComponentInChildren<Renderer>();
            }

            if (buttonRenderer != null)
            {
                Debug.Log($"[VRButton] 렌더러 자동 검색 성공: {buttonRenderer.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[VRButton] 렌더러를 찾을 수 없습니다. 색상 변경 기능이 비활성화됩니다.");
            }
        }

        // 버튼 머티리얼 가져오기
        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            buttonMaterial.color = normalColor;
        }

        // VR 컨트롤러 초기화
        InitializeControllers();
    }

    void Update()
    {
        // VR 컨트롤러 입력 체크
        CheckVRInput();
    }

    /// <summary>
    /// VR 컨트롤러 초기화
    /// </summary>
    void InitializeControllers()
    {
        var rightHandDevices = new System.Collections.Generic.List<InputDevice>();
        var leftHandDevices = new System.Collections.Generic.List<InputDevice>();

        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);

        if (rightHandDevices.Count > 0)
            rightController = rightHandDevices[0];

        if (leftHandDevices.Count > 0)
            leftController = leftHandDevices[0];
    }

    /// <summary>
    /// VR 컨트롤러 입력 체크 (Ray + A버튼)
    /// </summary>
    void CheckVRInput()
    {
        // 컨트롤러가 유효하지 않으면 다시 초기화
        if (!rightController.isValid || !leftController.isValid)
        {
            InitializeControllers();
        }

        // 오른손 컨트롤러 체크
        CheckControllerRaycast(rightController, XRNode.RightHand);

        // 왼손 컨트롤러 체크
        CheckControllerRaycast(leftController, XRNode.LeftHand);
    }

    /// <summary>
    /// 컨트롤러에서 Ray를 쏴서 버튼을 가리키고 있는지 체크
    /// </summary>
    void CheckControllerRaycast(InputDevice controller, XRNode handNode)
    {
        if (!controller.isValid) return;

        // 컨트롤러 위치와 방향 가져오기
        Vector3 controllerPosition;
        Quaternion controllerRotation;

        if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out controllerPosition) &&
            controller.TryGetFeatureValue(CommonUsages.deviceRotation, out controllerRotation))
        {
            // 컨트롤러에서 앞으로 Ray 발사
            Ray ray = new Ray(controllerPosition, controllerRotation * Vector3.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance))
            {
                // Ray가 이 버튼을 맞췄는지 확인
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    // 버튼에 호버링 중
                    if (!isHovering)
                    {
                        OnHoverEnter();
                    }

                    // A버튼 (Primary Button) 체크
                    bool primaryButtonPressed;
                    if (controller.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonPressed))
                    {
                        if (primaryButtonPressed && !isPressed)
                        {
                            PressButton();
                        }
                    }
                }
                else
                {
                    // 다른 오브젝트를 가리킴
                    if (isHovering)
                    {
                        OnHoverExit();
                    }
                }
            }
            else
            {
                // Ray가 아무것도 맞추지 못함
                if (isHovering)
                {
                    OnHoverExit();
                }
            }
        }
    }

    /// <summary>
    /// 버튼 위에 마우스/컨트롤러가 올라왔을 때
    /// </summary>
    void OnHoverEnter()
    {
        isHovering = true;

        if (buttonMaterial != null && !isPressed)
        {
            buttonMaterial.color = hoverColor;
        }

        Debug.Log("[VRButton] 버튼 호버링 시작");
    }

    /// <summary>
    /// 버튼에서 마우스/컨트롤러가 벗어났을 때
    /// </summary>
    void OnHoverExit()
    {
        isHovering = false;

        if (buttonMaterial != null && !isPressed)
        {
            buttonMaterial.color = normalColor;
        }

        Debug.Log("[VRButton] 버튼 호버링 종료");
    }

    /// <summary>
    /// VR 컨트롤러가 버튼에 닿았을 때
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // VR 손 또는 컨트롤러인지 확인
        if (other.CompareTag("PlayerHand") && !isPressed)
        {
            PressButton();
        }
    }

    /// <summary>
    /// 버튼 누르기
    /// </summary>
    void PressButton()
    {
        if (isPressed) return;

        isPressed = true;

        Debug.Log("[VRButton] 버튼 눌림!");

        // 시각적 피드백: 버튼 색상 변경
        if (buttonMaterial != null)
        {
            buttonMaterial.color = pressedColor;
        }

        // 시각적 피드백: 버튼 눌림 애니메이션
        transform.localPosition = originalPosition - new Vector3(0, 0, pressDistance);

        // 버튼 이벤트 실행
        onButtonPressed?.Invoke();

        // 마스터 클라이언트에게 알림 (네트워크 동기화)
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_ButtonPressed", RpcTarget.All);
        }

        // 일정 시간 후 버튼 복구
        Invoke(nameof(ResetButton), pressDuration);
    }

    [PunRPC]
    void RPC_ButtonPressed()
    {
        Debug.Log("[VRButton] 네트워크 버튼 눌림 동기화");

        // 원격 클라이언트에서도 시각적 피드백
        if (buttonMaterial != null)
        {
            buttonMaterial.color = pressedColor;
        }

        transform.localPosition = originalPosition - new Vector3(0, 0, pressDistance);

        Invoke(nameof(ResetButton), pressDuration);
    }

    /// <summary>
    /// 버튼 복구
    /// </summary>
    void ResetButton()
    {
        isPressed = false;

        // 색상 복구
        if (buttonMaterial != null)
        {
            buttonMaterial.color = normalColor;
        }

        // 위치 복구
        transform.localPosition = originalPosition;

        Debug.Log("[VRButton] 버튼 복구");
    }

    /// <summary>
    /// 마우스 hover (에디터 테스트용)
    /// </summary>
    void OnMouseEnter()
    {
        if (buttonMaterial != null)
        {
            buttonMaterial.color = hoverColor;
        }
    }

    void OnMouseExit()
    {
        if (buttonMaterial != null && !isPressed)
        {
            buttonMaterial.color = normalColor;
        }
    }

    /// <summary>
    /// 마우스 클릭 (에디터 테스트용)
    /// </summary>
    void OnMouseDown()
    {
        PressButton();
    }
}
