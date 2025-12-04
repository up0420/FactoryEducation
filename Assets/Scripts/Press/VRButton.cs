using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Photon.Pun;

/// <summary>
/// VR에서 눌릴 수 있는 3D 버튼 (XR Interaction Toolkit 기반)
/// XRRayInteractor로 가리키고 Trigger/Grip 버튼을 누르면 작동
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
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

    private Vector3 originalPosition;
    private bool isPressed = false;
    private Material buttonMaterial;
    private XRSimpleInteractable interactable;

    void Awake()
    {
        // XRSimpleInteractable 컴포넌트 가져오기 또는 추가
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
        }

        // 이벤트 리스너 등록
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
    }

    void Start()
    {
        originalPosition = transform.localPosition;

        // 버튼 렌더러 자동으로 찾기
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<Renderer>();
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

        Debug.Log("[VRButton] XR Interaction Toolkit 기반 버튼 초기화 완료");
    }

    /// <summary>
    /// XR Ray Interactor가 버튼 위에 올라왔을 때
    /// </summary>
    void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (buttonMaterial != null && !isPressed)
        {
            buttonMaterial.color = hoverColor;
        }

        Debug.Log($"[VRButton] 버튼 호버링 시작 (Interactor: {args.interactorObject})");
    }

    /// <summary>
    /// XR Ray Interactor가 버튼에서 벗어났을 때
    /// </summary>
    void OnHoverExit(HoverExitEventArgs args)
    {
        if (buttonMaterial != null && !isPressed)
        {
            buttonMaterial.color = normalColor;
        }

        Debug.Log($"[VRButton] 버튼 호버링 종료 (Interactor: {args.interactorObject})");
    }

    /// <summary>
    /// XR Ray Interactor가 버튼을 선택(클릭)했을 때
    /// </summary>
    void OnSelectEnter(SelectEnterEventArgs args)
    {
        Debug.Log($"[VRButton] 버튼 선택됨 (Interactor: {args.interactorObject})");
        PressButton();
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
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("RPC_ButtonPressed", RpcTarget.Others); // 자기 자신은 이미 눌렀으니 Others만
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

    void OnDestroy()
    {
        // 이벤트 리스너 해제 (메모리 누수 방지)
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
            interactable.selectEntered.RemoveListener(OnSelectEnter);
        }
    }

    // ==================== 에디터 테스트용 기능 (마우스 클릭) ====================
    
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
