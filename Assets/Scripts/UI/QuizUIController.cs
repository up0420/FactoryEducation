using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 퀴즈 UI 컨트롤러
/// Standby, Question, Result 화면 관리
/// </summary>
public class QuizUIController : MonoBehaviour
{
    [Header("Canvas Panels")]
    public GameObject standbyPanel;      // "Game Start" 화면
    public GameObject questionPanel;     // 문제 화면
    public GameObject resultPanel;       // 결과 화면

    [Header("Standby UI")]
    public Button startButton;           // Start 버튼
    public TextMeshProUGUI standbyText;  // "당신의 차례입니다" 텍스트

    [Header("Question UI")]
    public TextMeshProUGUI questionText; // 문제 텍스트
    public TextMeshProUGUI timerText;    // 남은 시간 표시
    public QuizButton oButton;           // O 버튼
    public QuizButton xButton;           // X 버튼
    public TextMeshProUGUI feedbackText; // [추가] 선택 확인용 텍스트 (예: "선택: O")

    [Header("Result UI")]
    public TextMeshProUGUI resultText;   // "정답입니다!" / "오답입니다!" (문제별 결과)
    public TextMeshProUGUI explanationText; // [추가] 정답 설명 텍스트
    public GameObject scoreboardPanel;   // 최종 점수판 패널
    public TextMeshProUGUI finalScoreText; // [추가] 최종 점수 텍스트 (예: "Score: 3 / 3")
    public TextMeshProUGUI finalRankText;  // [추가] 최종 등급 텍스트 (예: "Perfect!")
    public Button restartButton;           // [추가] 재시작 버튼
    public Button exitButton;              // [추가] 종료 버튼

    // [추가] VR 입력 감지용 변수
    private UnityEngine.XR.InputDevice leftController;
    private UnityEngine.XR.InputDevice rightController;

    void Start()
    {
        // Start 버튼 이벤트 연결
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        // [추가] 재시작/종료 버튼 이벤트 연결
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => 
            {
                if (QuizManager.Instance != null) QuizManager.Instance.RestartQuiz();
            });
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => 
            {
                if (QuizManager.Instance != null) QuizManager.Instance.ExitGame();
            });
        }

        // 초기 상태: 모든 패널 숨김
        ShowStandby(false);
    }

    void Update()
    {
        // 1. 컨트롤러 연결 확인 (매 프레임 체크는 비효율적이지만 안전함)
        if (!leftController.isValid || !rightController.isValid)
        {
            leftController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            rightController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        }

        // 2. 타이머 업데이트
        if (questionPanel != null && questionPanel.activeSelf)
        {
            if (QuizManager.Instance != null)
            {
                float remainingTime = QuizManager.Instance.GetRemainingTime();
                UpdateTimer(remainingTime);
            }
        }

        // 3. VR 입력 처리 (트리거 버튼)
        HandleVRInput();
    }

    /// <summary>
    /// VR 컨트롤러 입력 처리
    /// </summary>
    void HandleVRInput()
    {
        bool rightTrigger = false;
        bool leftTrigger = false;

        // 트리거 입력 감지 (0.5 이상 눌리면 true)
        if (rightController.isValid) rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out rightTrigger);
        if (leftController.isValid) leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out leftTrigger);

        // A. 대기 화면: 오른손 트리거로 시작
        if (standbyPanel.activeSelf && rightTrigger)
        {
            if (startButton != null && startButton.interactable)
            {
                OnStartButtonClicked();
            }
        }

        // B. 문제 화면: 오른손=O, 왼손=X
        if (questionPanel.activeSelf)
        {
            if (rightTrigger)
            {
                QuizManager.Instance.SelectAnswer(true); // O 선택
            }
            else if (leftTrigger)
            {
                QuizManager.Instance.SelectAnswer(false); // X 선택
            }
        }
    }

    /// <summary>
    /// 선택된 답안 강조 표시 (QuizManager에서 호출)
    /// </summary>
    public void HighlightAnswer(bool isO)
    {
        // 시각적 피드백: 선택된 버튼은 색깔 변경, 나머지는 원래대로
        if (oButton != null) oButton.SetHighlight(isO);     // O버튼: true면 하이라이트
        if (xButton != null) xButton.SetHighlight(!isO);    // X버튼: false면 하이라이트 (즉 X선택시)

        // [추가] 텍스트 피드백
        if (feedbackText != null)
        {
            feedbackText.text = isO ? "Selected: O" : "Selected: X";
            feedbackText.color = isO ? Color.green : Color.red; // O는 초록, X는 빨강
        }
    }

    /// <summary>
    /// Standby 화면 표시
    /// </summary>
    public void ShowStandby(bool isMyTurn)
    {
        SetActivePanel(standbyPanel);

        if (standbyText != null)
        {
            // [수정] 테스트를 위해 텍스트 변경
            standbyText.text = "테스트 모드: [Start]를 누르세요.";
        }

        // [수정] 테스트를 위해 무조건 활성화
        if (startButton != null)
        {
            startButton.interactable = true; 
        }
    }

    /// <summary>
    /// Question 화면 표시
    /// </summary>
    public void ShowQuestion(string question)
    {
        SetActivePanel(questionPanel);

        if (questionText != null)
        {
            questionText.text = question;
        }

        // [추가] 피드백 텍스트 초기화
        if (feedbackText != null)
        {
            feedbackText.text = "Please select an answer...";
            feedbackText.color = Color.white;
        }

        // 버튼 초기화
        if (oButton != null) oButton.ResetVisual();
        if (xButton != null) xButton.ResetVisual();

        // 버튼 활성화 (내 턴인 경우만)
        EnableButtons(true);
    }

    /// <summary>
    /// 타이머 업데이트
    /// </summary>
    public void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            timerText.text = $"남은 시간: {Mathf.CeilToInt(time)}초";

            // 시간이 3초 이하면 빨간색
            if (time <= 3f)
            {
                timerText.color = Color.red;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Result 화면 표시 (문제별 결과)
    /// </summary>
    public void ShowResult(bool isCorrect, string explanation = "")
    {
        SetActivePanel(resultPanel);

        if (resultText != null)
        {
            resultText.text = isCorrect ? "정답입니다!" : "오답입니다!";
            resultText.color = isCorrect ? Color.green : Color.red;
        }

        // [추가] 설명 표시
        if (explanationText != null)
        {
            explanationText.text = explanation;
        }
    }

    /// <summary>
    /// 최종 결과 화면 표시 (점수판)
    /// </summary>
    /// <summary>
    /// 최종 결과 화면 표시 (점수판)
    /// isIntermediate: 중간 결과인지 여부 (true면 버튼 숨김)
    /// </summary>
    public void ShowFinalResult(int score, int totalQuestions, bool isIntermediate = false)
    {
        // 모든 패널 끄고 결과 패널만 켜기
        SetActivePanel(resultPanel);
        
        // 1. 점수판 패널 활성화 (만약 별도로 있다면)
        if (scoreboardPanel != null) scoreboardPanel.SetActive(true);

        // 2. 점수 표시
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Score: {score} / {totalQuestions}";
        }

        // 3. 등급 및 메시지 결정
        string rankMessage = "";
        Color rankColor = Color.white;

        if (score == totalQuestions) // 만점
        {
            rankMessage = "Perfect!\n(Safety Master)";
            rankColor = Color.green;
        }
        else if (score >= totalQuestions - 1) // 1개 틀림 (2/3)
        {
            rankMessage = "Pass\n(Qualified)";
            rankColor = Color.yellow;
        }
        else // 나머지 (Fail)
        {
            rankMessage = "Fail\n(Retraining Needed)";
            rankColor = Color.red;
        }

        // 4. 등급 표시
        if (finalRankText != null)
        {
            finalRankText.text = rankMessage;
            finalRankText.color = rankColor;
        }
        
        // 기존 문제별 결과 텍스트는 숨김
        if (resultText != null) resultText.gameObject.SetActive(false);

        // [추가] 재시작/종료 버튼 활성화 여부
        if (restartButton != null) restartButton.gameObject.SetActive(!isIntermediate);
        if (exitButton != null) exitButton.gameObject.SetActive(!isIntermediate);
    }

    /// <summary>
    /// O/X 버튼 활성화/비활성화
    /// </summary>
    public void EnableButtons(bool enabled)
    {
        if (oButton != null) oButton.SetInteractable(enabled);
        if (xButton != null) xButton.SetInteractable(enabled);
    }

    /// <summary>
    /// Start 버튼 클릭 시
    /// </summary>
    void OnStartButtonClicked()
    {
        Debug.Log("[QuizUIController] Start 버튼 클릭!");

        if (QuizManager.Instance != null)
        {
            QuizManager.Instance.StartQuestion();
        }
    }

    /// <summary>
    /// 활성 패널 전환
    /// </summary>
    void SetActivePanel(GameObject activePanel)
    {
        if (standbyPanel != null) standbyPanel.SetActive(standbyPanel == activePanel);
        if (questionPanel != null) questionPanel.SetActive(questionPanel == activePanel);
        if (resultPanel != null) resultPanel.SetActive(resultPanel == activePanel);
    }

    /// <summary>
    /// 전체 UI 숨기기
    /// </summary>
    public void HideAll()
    {
        if (standbyPanel != null) standbyPanel.SetActive(false);
        if (questionPanel != null) questionPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }
}
