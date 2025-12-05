using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Tutorial 씬 매니저
/// - 트리거 버튼으로 튜토리얼 이미지 토글
/// - 문 열림 애니메이션
/// - Onegiog 씬으로 전환
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI")]
    public GameObject tutorialCanvas; // Tutorial 이미지 Canvas (LobbyCanvas)

    [Header("Door References")]
    public Transform gatesDoorLeft;  // 왼쪽 문
    public Transform gatesDoorRight; // 오른쪽 문

    [Header("Door Animation Settings")]
    public float doorOpenAngle = 90f;  // 문이 열리는 각도 (Y축 회전)
    public float doorOpenDuration = 2f; // 문 열리는 시간 (초)
    public float sceneTransitionDelay = 4f; // 씬 전환 대기 시간 (초)

    [Header("Scene Settings")]
    public string nextSceneName = "Onegiog"; // 다음 씬 이름

    // 내부 상태
    private bool isTutorialImageVisible = false; // 튜토리얼 이미지 표시 여부
    private bool isDoorOpening = false; // 문이 열리는 중인지
    private bool isDoorOpened = false; // 문이 완전히 열렸는지

    // VR 컨트롤러 입력
    private InputDevice leftController;
    private InputDevice rightController;

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // VR 컨트롤러 초기화
        InitializeControllers();

        // 튜토리얼 Canvas 초기 상태: 활성화 (튜토리얼 처음부터 보여줌)
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(true); // 초기에 보여줌
            isTutorialImageVisible = true; // 이미 표시된 상태
        }

        // 문 초기 상태 확인
        ValidateDoors();
    }

    void Update()
    {
        // 컨트롤러 재연결 확인
        if (!leftController.isValid || !rightController.isValid)
        {
            InitializeControllers();
        }

        // 트리거 버튼 입력 감지
        HandleTriggerInput();
    }

    /// <summary>
    /// VR 컨트롤러 초기화
    /// </summary>
    void InitializeControllers()
    {
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        Debug.Log($"[TutorialManager] Left Controller: {leftController.name}, Valid: {leftController.isValid}");
        Debug.Log($"[TutorialManager] Right Controller: {rightController.name}, Valid: {rightController.isValid}");
    }

    /// <summary>
    /// 트리거 버튼 입력 처리
    /// </summary>
    void HandleTriggerInput()
    {
        bool leftTrigger = false;
        bool rightTrigger = false;

        // 왼쪽 트리거 버튼
        if (leftController.TryGetFeatureValue(CommonUsages.triggerButton, out leftTrigger) && leftTrigger)
        {
            OnTriggerPressed();
        }

        // 오른쪽 트리거 버튼
        if (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out rightTrigger) && rightTrigger)
        {
            OnTriggerPressed();
        }
    }

    /// <summary>
    /// 트리거 버튼이 눌렸을 때 호출
    /// </summary>
    void OnTriggerPressed()
    {
        // 이미 문이 열리는 중이면 무시
        if (isDoorOpening || isDoorOpened) return;

        // 튜토리얼 이미지가 보이는 상태
        if (isTutorialImageVisible)
        {
            // 두 번째 트리거: 이미지 숨기고 문 열기
            HideTutorialImage();
            OpenDoors();
        }
        else
        {
            // 첫 번째 트리거: 이미지 표시
            ShowTutorialImage();
        }

        // 버튼 중복 입력 방지 (0.5초 대기)
        StartCoroutine(WaitForInputCooldown());
    }

    /// <summary>
    /// 튜토리얼 이미지 표시
    /// </summary>
    void ShowTutorialImage()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(true);
            isTutorialImageVisible = true;
            Debug.Log("[TutorialManager] 튜토리얼 이미지 표시");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] Tutorial Canvas가 null입니다!");
        }
    }

    /// <summary>
    /// 튜토리얼 이미지 숨기기
    /// </summary>
    void HideTutorialImage()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
            isTutorialImageVisible = false;
            Debug.Log("[TutorialManager] 튜토리얼 이미지 숨김");
        }
    }

    /// <summary>
    /// 문 열기 (코루틴 시작)
    /// </summary>
    void OpenDoors()
    {
        if (gatesDoorLeft == null || gatesDoorRight == null)
        {
            Debug.LogError("[TutorialManager] 문 Transform이 설정되지 않았습니다!");
            return;
        }

        Debug.Log("[TutorialManager] 문 열림 시작");
        isDoorOpening = true;
        StartCoroutine(OpenDoorsAnimation());
    }

    /// <summary>
    /// 문 열림 애니메이션 코루틴
    /// </summary>
    IEnumerator OpenDoorsAnimation()
    {
        // 초기 회전값 저장
        Quaternion leftStartRotation = gatesDoorLeft.rotation;
        Quaternion rightStartRotation = gatesDoorRight.rotation;

        // 목표 회전값 계산 (Y축 회전)
        // 왼쪽 문: -90도 회전 (안쪽으로)
        // 오른쪽 문: +90도 회전 (안쪽으로)
        Quaternion leftTargetRotation = leftStartRotation * Quaternion.Euler(0, -doorOpenAngle, 0);
        Quaternion rightTargetRotation = rightStartRotation * Quaternion.Euler(0, doorOpenAngle, 0);

        float elapsedTime = 0f;

        // 문 열림 애니메이션 (부드러운 회전)
        while (elapsedTime < doorOpenDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / doorOpenDuration;

            // EaseInOut 효과 (부드러운 시작 및 끝)
            t = Mathf.SmoothStep(0f, 1f, t);

            // 회전 보간
            gatesDoorLeft.rotation = Quaternion.Slerp(leftStartRotation, leftTargetRotation, t);
            gatesDoorRight.rotation = Quaternion.Slerp(rightStartRotation, rightTargetRotation, t);

            yield return null;
        }

        // 최종 회전값 적용 (정확한 각도)
        gatesDoorLeft.rotation = leftTargetRotation;
        gatesDoorRight.rotation = rightTargetRotation;

        Debug.Log("[TutorialManager] 문 열림 완료");
        isDoorOpening = false;
        isDoorOpened = true;

        // 씬 전환 대기
        yield return new WaitForSeconds(sceneTransitionDelay);

        // Onegiog 씬으로 전환
        LoadNextScene();
    }

    /// <summary>
    /// 다음 씬 로드 (Onegiog)
    /// </summary>
    void LoadNextScene()
    {
        Debug.Log($"[TutorialManager] {nextSceneName} 씬으로 전환");

        // Build Settings에 씬이 추가되어 있어야 함
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError($"[TutorialManager] '{nextSceneName}' 씬을 찾을 수 없습니다! Build Settings에 추가하세요.");
        }
    }

    /// <summary>
    /// 입력 쿨다운 (중복 입력 방지)
    /// </summary>
    IEnumerator WaitForInputCooldown()
    {
        enabled = false; // Update 비활성화
        yield return new WaitForSeconds(0.5f);
        enabled = true; // Update 재활성화
    }

    /// <summary>
    /// 문 오브젝트 유효성 검사
    /// </summary>
    void ValidateDoors()
    {
        if (gatesDoorLeft == null)
        {
            Debug.LogWarning("[TutorialManager] GatesDoorLeft가 설정되지 않았습니다!");
        }

        if (gatesDoorRight == null)
        {
            Debug.LogWarning("[TutorialManager] GatesDoorRight가 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 수동으로 문 열기 (디버그용)
    /// </summary>
    public void DebugOpenDoors()
    {
        if (!isDoorOpening && !isDoorOpened)
        {
            OpenDoors();
        }
    }

    /// <summary>
    /// 수동으로 씬 전환 (디버그용)
    /// </summary>
    public void DebugLoadNextScene()
    {
        LoadNextScene();
    }
}
