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
    public GameObject intermediateScorePanel;   // 최종 점수판 패널
    public TextMeshProUGUI finalScoreText; // [추가] 최종 점수 텍스트 (예: "Score: 3 / 3")
    public TextMeshProUGUI finalRankText;  // [추가] 최종 등급 텍스트 (예: "Perfect!")
    
    [Header("Buttons (Assign in Inspector)")]
    public Button confirmButton;           // [변경] 확인 버튼 (로비로 이동)

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

        // [변경] 확인 버튼 이벤트 연결 (게임 종료 -> 로비 이동)
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() => 
            {
                if (QuizManager.Instance != null) QuizManager.Instance.ExitGame();
            });
        }

        // 초기 상태: 모든 패널 숨김 (확실하게!)
        HideAll();
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
        HideAll();
        if (standbyPanel != null) standbyPanel.SetActive(true);

        if (standbyText != null)
        {
            // [수정] 테스트를 위해 텍스트 변경
            standbyText.text = "당신의 차례입니다. [Start] 버튼을 누르세요.";
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
        HideAll();
        if (questionPanel != null) questionPanel.SetActive(true);

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
        if (intermediateScorePanel != null) intermediateScorePanel.SetActive(false);
        if (finalScorePanel != null) finalScorePanel.SetActive(false);
    }
    [Header("Final Scoreboard UI")]
    public GameObject finalScorePanel;      // Final comprehensive scoreboard (White Canvas)
    public TextMeshProUGUI[] finalPlayerScores;  // Array for player scores (Player 1, 2, 3...)

    [Header("Message Settings")]
    public string correctMessage = "정답입니다!";
    public string incorrectMessage = "오답입니다!";

    /// <summary>
    /// Result 화면 표시 (문제별 결과)
    /// </summary>
    public void ShowResult(bool isCorrect, string explanation, int correctCount, int totalQuestions)
    {
        HideAll(); // 다른 패널 끄기
        if (resultPanel != null) resultPanel.SetActive(true);

        // 1. 결과 텍스트 (정답/오답) - 상단 큰 텍스트
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = isCorrect ? correctMessage : incorrectMessage;
            resultText.color = isCorrect ? Color.green : Color.red;
        }

        // 2. 중간 점수판 켜기 (점수 및 설명 표시용)
        if (intermediateScorePanel != null) intermediateScorePanel.SetActive(true);

        // 3. 점수 텍스트 업데이트 (예: Score: 3 / 5)
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Score: {correctCount} / {totalQuestions}";
        }

        // 4. 등급/메시지 텍스트 (패널 내부 텍스트)
        if (finalRankText != null)
        {
            finalRankText.gameObject.SetActive(true); // 무조건 켜기
            finalRankText.text = isCorrect ? correctMessage : incorrectMessage;
            finalRankText.color = isCorrect ? Color.green : Color.red;
        }

        // 5. 설명 텍스트 표시
        if (explanationText != null)
        {
            explanationText.text = explanation;
        }
        
        // 확인 버튼은 숨김 (문제별 결과이므로 자동 진행)
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Show Individual Result (Intermediate)
    /// </summary>
    public void ShowIndividualResult(int playerIndex, int correctCount, int totalQuestions)
    {
        // Turn off all other panels and show result panel
        HideAll();
        if (resultPanel != null) resultPanel.SetActive(true);
        
        // 1. Enable Scoreboard Panel
        if (intermediateScorePanel != null) intermediateScorePanel.SetActive(true);
        
        // [수정] 기존 결과 텍스트("오답입니다!" 등)가 겹치지 않도록 비활성화
        if (resultText != null)
        {
            resultText.gameObject.SetActive(false); 
        }

        // 2. Set Text
        int totalScore = correctCount * 10;
        
        // [참고] resultText 대신 별도의 텍스트를 쓰거나, resultText를 재활용하려면 위치가 겹치지 않아야 함.
        // 여기서는 scoreboardPanel 내부의 텍스트를 사용한다고 가정하거나, 
        // 만약 resultText를 재활용해야 한다면 위치를 조정해야 합니다.
        // 현재 코드 흐름상 resultText를 끄고 finalScoreText 등을 사용하는 것이 안전해 보입니다.

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Score: {totalScore}";
        }
        if (finalRankText != null)
        {
            // Reuse for "Correct Count"
            finalRankText.gameObject.SetActive(true);
            finalRankText.text = $"Correct: {correctCount}";
            finalRankText.color = Color.white; 
        }
        // [변경] 중간 점수판에서는 확인 버튼 숨김
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Show Final Comprehensive Scoreboard (Game End)
    /// </summary>
    public void ShowFinalScoreboard(List<int> allScores)
    {
        // Hide all other panels
        HideAll();
        
        // Show Final Scoreboard Panel
        if (finalScorePanel != null)
        {
            finalScorePanel.SetActive(true);
        }
        // Display scores for each player
        for (int i = 0; i < finalPlayerScores.Length; i++)
        {
            if (i < allScores.Count && finalPlayerScores[i] != null)
            {
                finalPlayerScores[i].text = $"Player {i + 1}: {allScores[i]} Points";
                finalPlayerScores[i].gameObject.SetActive(true);
            }
            else if (finalPlayerScores[i] != null)
            {
                finalPlayerScores[i].gameObject.SetActive(false);
            }
        }
        // [변경] 최종 화면에서만 확인 버튼 활성화
        if (confirmButton != null) 
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.transform.SetAsLastSibling(); // Bring to front
        }
    }
}
