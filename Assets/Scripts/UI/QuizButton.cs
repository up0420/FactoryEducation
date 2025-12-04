using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.UI;

/// <summary>
/// 퀴즈용 UI 버튼 (O/X 선택)
/// XR Ray Interactor와 상호작용 가능
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class QuizButton : MonoBehaviour
{
    [Header("Button Type")]
    public bool isOButton = true; // true = O버튼, false = X버튼

    [Header("Visual Feedback")]
    public Image buttonImage; // UI Image 컴포넌트
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.cyan;

    [Header("Events")]
    public UnityEvent<bool> onButtonClicked; // bool = 선택된 답 (true=O, false=X)

    private XRSimpleInteractable interactable;
    private bool isInteractable = true;
    private bool isSelected = false;

    void Awake()
    {
        // XRSimpleInteractable 설정
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
        }

        // 이벤트 등록
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
    }

    void Start()
    {
        // 자동으로 Image 컴포넌트 찾기
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        ResetVisual();
    }

    /// <summary>
    /// 호버 시작
    /// </summary>
    void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (!isInteractable || isSelected) return;

        if (buttonImage != null)
        {
            buttonImage.color = hoverColor;
        }
    }

    /// <summary>
    /// 호버 종료
    /// </summary>
    void OnHoverExit(HoverExitEventArgs args)
    {
        if (!isInteractable || isSelected) return;

        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }

    /// <summary>
    /// 버튼 선택 (클릭)
    /// </summary>
    void OnSelectEnter(SelectEnterEventArgs args)
    {
        if (!isInteractable) return;

        Debug.Log($"[QuizButton] {(isOButton ? "O" : "X")} 버튼 클릭");

        // 선택 상태로 변경
        SetSelected(true);

        // 이벤트 발생
        onButtonClicked?.Invoke(isOButton);

        // QuizManager에게 알림
        if (QuizManager.Instance != null)
        {
            QuizManager.Instance.SelectAnswer(isOButton);
        }
    }

    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (buttonImage != null)
        {
            buttonImage.color = selected ? selectedColor : normalColor;
        }
    }

    /// <summary>
    /// 버튼 상호작용 활성화/비활성화
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        // XRSimpleInteractable 활성화/비활성화
        if (this.interactable != null)
        {
            this.interactable.enabled = interactable;
        }

        // 비활성화 시 색상 변경
        if (!interactable && buttonImage != null)
        {
            buttonImage.color = Color.gray;
        }
        else
        {
            ResetVisual();
        }
    }

    /// <summary>
    /// 버튼 초기화
    /// </summary>
    public void ResetVisual()
    {
        isSelected = false;
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }

    /// <summary>
    /// 외부에서 버튼 하이라이트 설정 (게임패드 조작용)
    /// </summary>
    public void SetHighlight(bool isSelected)
    {
        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    void OnDestroy()
    {
        // 이벤트 해제
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
            interactable.selectEntered.RemoveListener(OnSelectEnter);
        }
    }
}
