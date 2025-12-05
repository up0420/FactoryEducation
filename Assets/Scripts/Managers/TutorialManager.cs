using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Tutorial 씬 매니저
/// - 트리거 버튼으로 튜토리얼 이미지 표시
/// - 5초 후 문 열림 애니메이션
/// - Onegiog 씬으로 전환
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI")]
    public GameObject tutorialImage; // Tutorial 이미지 오브젝트 (Canvas 하위의 Tutorial)
    public GameObject lobbyCanvas;   // LobbyCanvas 전체 오브젝트

    [Header("Door References")]
    public GameObject gatesDoorLeft;  // 왼쪽 문 (DoorOpen 스크립트 있음)
    public GameObject gatesDoorRight; // 오른쪽 문 (DoorOpen1 스크립트 있음)

    [Header("Tutorial Settings")]
    public float tutorialDisplayTime = 5f;    // 튜토리얼 표시 시간 (초)
    public float sceneTransitionDelay = 4f;   // 문 열린 후 씬 전환까지 대기 시간 (초)

    [Header("Scene Settings")]
    public string nextSceneName = "Onegiog";  // 다음 씬 이름

    // 내부 상태
    private bool isTutorialImageVisible = false; // 튜토리얼 이미지 표시 여부
    public bool isDoorOpening = false;          // 문이 열리는 중인지
    private bool isDoorOpened = false;           // 문이 완전히 열렸는지
    private bool hasTriggered = false;           // 트리거를 이미 눌렀는지 (중복 방지)

    // 씬 전환 타이머
    private bool isSceneTimerRunning = false;
    private float sceneTimer = 0f;

    // VR 컨트롤러 입력
    private InputDevice leftController;
    private InputDevice rightController;

    public bool Debug_bull;

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

        // 튜토리얼 이미지 초기 상태: 비활성화
        if (tutorialImage != null)
        {
            tutorialImage.SetActive(false); // 초기에는 숨김
            isTutorialImageVisible = false;
            Debug.Log("[TutorialManager] Tutorial 이미지 초기 상태: 비활성화");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] Tutorial Image가 설정되지 않았습니다!");
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

        // 아직 튜토리얼이 시작되지 않았을 때만 입력 받기
        if (!hasTriggered)
        {
            // 트리거 버튼 입력 감지
            HandleTriggerInput();

            // 디버그용 강제 시작
            if (Debug_bull)
            {
                Debug_bull = false;
                hasTriggered = true;
                StartCoroutine(TutorialSequence());
            }
        }

        // 씬 전환 타이머 업데이트
        UpdateSceneTimer();
        
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
        // 이미 시작되었으면 무시
        if (hasTriggered) return;

        hasTriggered = true;

        Debug.Log("[TutorialManager] 트리거 버튼 눌림! 튜토리얼 시작");

        // 튜토리얼 이미지 표시 → 5초 대기 → 문 열기
        StartCoroutine(TutorialSequence());
    }

    /// <summary>
    /// 튜토리얼 시퀀스 (이미지 표시 → 대기 → 문 열기)
    /// 문 열림 애니메이션과 씬 전환은 코루틴 없이 Update에서 처리
    /// </summary>
    IEnumerator TutorialSequence()
    {
        // 1. 튜토리얼 이미지 표시
        ShowTutorialImage();

        // 2. 5초 대기
        Debug.Log($"[TutorialManager] {tutorialDisplayTime}초 대기 중...");
        yield return new WaitForSeconds(tutorialDisplayTime);

        // 3. 튜토리얼 이미지 숨기기 + 로비 캔버스 비활성화
        HideTutorialImage();

        // 4. 문 열기 (코루틴 없음, 상태만 세팅 → Update에서 애니메이션)
        OpenDoors();
    }

    /// <summary>
    /// 튜토리얼 이미지 표시
    /// </summary>
    void ShowTutorialImage()
    {
        if (tutorialImage != null)
        {
            tutorialImage.SetActive(true);
            isTutorialImageVisible = true;
            Debug.Log("[TutorialManager] 튜토리얼 이미지 표시");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] Tutorial Image가 null입니다!");
        }
    }

    /// <summary>
    /// 튜토리얼 이미지 숨기기 + LobbyCanvas 비활성화
    /// </summary>
    void HideTutorialImage()
    {
        if (tutorialImage != null)
        {
            tutorialImage.SetActive(false);
            isTutorialImageVisible = false;
            Debug.Log("[TutorialManager] 튜토리얼 이미지 숨김");
        }

        // LobbyCanvas 전체 비활성화
        if (lobbyCanvas != null)
        {
            lobbyCanvas.SetActive(false);
            Debug.Log("[TutorialManager] LobbyCanvas 비활성화");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] LobbyCanvas가 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 문 열기 - DoorOpen과 DoorOpen1 스크립트의 Open bool을 true로 설정
    /// </summary>
    void OpenDoors()
    {
        Debug.Log($"[TutorialManager] OpenDoors() 호출됨!");
        Debug.Log($"[TutorialManager] gatesDoorLeft = {(gatesDoorLeft != null ? gatesDoorLeft.name : "NULL")}");
        Debug.Log($"[TutorialManager] gatesDoorRight = {(gatesDoorRight != null ? gatesDoorRight.name : "NULL")}");

        if (gatesDoorLeft == null || gatesDoorRight == null)
        {
            Debug.LogError("[TutorialManager] 문 Transform이 설정되지 않았습니다!");
            return;
        }

        // 이미 열리고 있거나 완전히 열린 상태면 재실행 방지
        if (isDoorOpening || isDoorOpened)
        {
            Debug.Log($"[TutorialManager] 이미 문이 열리는 중이거나 열린 상태: isDoorOpening={isDoorOpening}, isDoorOpened={isDoorOpened}");
            return;
        }

        Debug.Log("[TutorialManager] 문 열림 시작!");

        // 왼쪽 문의 DoorOpen 스크립트 찾아서 Open = true
        DoorOpen leftDoorScript = gatesDoorLeft.GetComponent<DoorOpen>();
        if (leftDoorScript != null)
        {
            leftDoorScript.Open = true;
            Debug.Log("[TutorialManager] 왼쪽 문 DoorOpen.Open = true 설정");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] 왼쪽 문에 DoorOpen 스크립트가 없습니다!");
        }

        // 오른쪽 문의 DoorOpen1 스크립트 찾아서 Open = true
        DoorOpen1 rightDoorScript = gatesDoorRight.GetComponent<DoorOpen1>();
        if (rightDoorScript != null)
        {
            rightDoorScript.Open = true;
            Debug.Log("[TutorialManager] 오른쪽 문 DoorOpen1.Open = true 설정");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] 오른쪽 문에 DoorOpen1 스크립트가 없습니다!");
        }

        isDoorOpening = true;
        isDoorOpened = true;

        // 씬 전환 타이머 시작
        isSceneTimerRunning = true;
        sceneTimer = 0f;

        Debug.Log($"[TutorialManager] 문 열림 완료! {sceneTransitionDelay}초 후 씬 전환");
    }


    /// <summary>
    /// 문이 열린 후 일정 시간 대기 후 씬 전환
    /// </summary>
    void UpdateSceneTimer()
    {
        if (!isSceneTimerRunning)
            return;

        sceneTimer += Time.deltaTime;

        if (sceneTimer >= sceneTransitionDelay)
        {
            isSceneTimerRunning = false;
            LoadNextScene();
        }
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
    /// 수동으로 튜토리얼 시작 (디버그용 또는 UI 버튼용)
    /// </summary>
    public void StartTutorial()
    {
        if (!hasTriggered)
        {
            OnTriggerPressed();
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