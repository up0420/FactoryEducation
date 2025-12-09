using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 퀴즈 시스템 매니저
/// 턴 기반 OX 퀴즈 진행 및 타이머 관리
/// </summary>
public class QuizManager : MonoBehaviourPunCallbacks
{
    public static QuizManager Instance { get; private set; }

    [Header("Quiz Settings")]
    public List<QuizData> quizPool = new List<QuizData>(); // 퀴즈 데이터 리스트
    public int questionsPerPlayer = 5;   // 플레이어당 문제 수 (3 -> 5 변경)
    public float timePerQuestion = 10f;  // [복구] 문제당 제한 시간

    [Header("Timing Settings")]
    public float revealDelay = 1.0f;     // [추가] 정답 공개 대기 시간 (프레스 후)
    public float showResultDuration = 2.0f; // [추가] 결과 보여주는 시간 (공개 후)

    // ... (existing code) ...


    public QuizUIController uiController; // UI 컨트롤러 연결
    public QuizAssetSpawner assetSpawner; // 에셋 스포너 연결
    public Transform quizZone;            // 퀴즈 진행 위치 (플레이어 이동용)

    [Header("Polish (Sound & Effects)")]
    public AudioSource audioSource;       // 오디오 소스 컴포넌트
    public AudioClip correctSound;        // 정답 효과음 (딩동댕)
    public AudioClip wrongSound;          // 오답 효과음 (땡)
    public GameObject correctEffectPrefab;  // [변경] 정답 이펙트 프리팹 (Particle or VFX)

    // 턴 관리
    private int currentPlayerIndex = 0; // 현재 턴 플레이어 (0, 1, 2)
    private int currentQuestionIndex = 0; // 현재 문제 번호
    private List<QuizData> usedQuestions = new List<QuizData>(); // 사용된 문제

    // 타이머
    private float currentTimer = 0f;
    private bool isTimerRunning = false;

    // 플레이어 선택
    private bool? playerAnswer = null; // null = 미선택, true = O, false = X

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 타이머 카운트다운
        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                OnTimeUp();
            }
        }
    }

    /// <summary>
    /// 퀴즈 시스템 시작 (VideoManager 종료 후 호출)
    /// </summary>
    public void StartQuizSystem()
    {
        Debug.Log("[QuizManager] 퀴즈 시스템 시작");

        // [추가] 퀴즈 UI 강제 활성화 (평소엔 꺼져 있다가 이때 켜짐)
        if (uiController != null)
        {
            uiController.gameObject.SetActive(true);
            Debug.Log("[QuizManager] 퀴즈 UI 활성화됨");
        }

        // 초기화
        currentPlayerIndex = 0;
        currentQuestionIndex = 0;
        usedQuestions.Clear();

        // 플레이어 이동 (모든 플레이어를 퀴즈 존으로)
        TeleportAllPlayersToQuizZone();

        // 플레이어 이동 잠금 해제 (혹시 잠겨있을 경우 대비)
        UnlockAllPlayers();

        // 첫 번째 플레이어 턴 시작
        StartPlayerTurn(currentPlayerIndex);
    }

    /// <summary>
    /// 플레이어 턴 시작
    /// </summary>
    void StartPlayerTurn(int playerIndex)
    {
        Debug.Log($"[QuizManager] Player {playerIndex + 1}의 턴 시작");

        currentPlayerIndex = playerIndex;

        // UI 업데이트: Standby 화면 표시
        if (uiController != null)
        {
            int localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            bool isMyTurn = (localPlayerIndex == currentPlayerIndex);
            uiController.ShowStandby(isMyTurn);

            // VR 상호작용 권한 설정 (내 턴일 때만 레이저 활성화)
            SetPlayerInteraction(isMyTurn);
        }
    }

    /// <summary>
    /// 문제 시작 (Start 버튼 클릭 시 호출)
    /// </summary>
    public void StartQuestion()
    {
        // 사용되지 않은 문제 랜덤 추출
        QuizData selectedQuestion = GetRandomUnusedQuestion();
        if (selectedQuestion == null)
        {
            Debug.LogError("[QuizManager] 사용 가능한 문제가 없습니다!");
            return;
        }

        usedQuestions.Add(selectedQuestion);

        Debug.Log($"[QuizManager] 문제 출제: {selectedQuestion.questionText}");

        // 타이머 시작
        currentTimer = timePerQuestion;
        isTimerRunning = true;
        playerAnswer = null;

        // UI 업데이트: 문제 화면 표시
        if (uiController != null)
        {
            uiController.ShowQuestion(selectedQuestion.questionText);
        }

        // 에셋 스폰: 프레스 기계에 정답/오답 배치
        if (assetSpawner != null)
        {
            assetSpawner.SpawnAssets(selectedQuestion);
        }
    }

    /// <summary>
    /// 플레이어 답안 선택
    /// </summary>
    public void SelectAnswer(bool answer)
    {
        if (!isTimerRunning)
        {
            Debug.LogWarning("[QuizManager] 문제가 진행 중이 아닙니다.");
            return;
        }

        // 로컬 플레이어가 현재 턴인지 확인
        int localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        if (localPlayerIndex != currentPlayerIndex)
        {
            Debug.LogWarning("[QuizManager] 현재 턴이 아닙니다.");
            return;
        }

        playerAnswer = answer;
        Debug.Log($"[QuizManager] 답안 선택: {(answer ? "O" : "X")} (타이머 종료 시 채점)");

        // UI에 선택 상태 반영
        if (uiController != null)
        {
            uiController.HighlightAnswer(answer);
        }
    }

    /// <summary>
    /// 시간 종료
    /// </summary>


    /// <summary>
    /// 시간 종료 (Update에서 호출)
    /// </summary>
    void OnTimeUp()
    {
        Debug.Log("[QuizManager] Time Up!");
        isTimerRunning = false;

        // 결과 처리 시퀀스 시작
        StartCoroutine(ResultSequence());
    }

    /// <summary>
    /// 결과 처리 시퀀스 (정답 확인 -> 연출 -> 점수 -> 대기 -> 다음)
    /// </summary>
    System.Collections.IEnumerator ResultSequence()
    {
        // 1. 정답 확인
        bool isCorrect = false;
        string explanation = "";
        
        if (playerAnswer.HasValue && usedQuestions.Count > 0)
        {
            QuizData currentQ = usedQuestions[usedQuestions.Count - 1];
            isCorrect = (playerAnswer.Value == currentQ.correctAnswer);
            explanation = currentQ.explanation;
        }

        Debug.Log($"[QuizManager] 결과: {(isCorrect ? "정답" : "오답")}");

        // 점수 부여
        if (isCorrect)
        {
            ScoreManager.Instance.AddWorkScore(currentPlayerIndex, 10);
            if (correctSound != null) PlaySound(correctSound);
            if (correctEffectPrefab != null) PlayEffect(correctEffectPrefab);
        }
        else
        {
            if (wrongSound != null) PlaySound(wrongSound); // [수정] incorrectSound -> wrongSound
        }

        // 현재 점수 계산
        int correctCount = 0;
        var playerScore = ScoreManager.Instance.GetPlayerScore(currentPlayerIndex);
        if (playerScore != null)
        {
            correctCount = playerScore.workScore / 10;
        }

        // UI 결과 표시
        if (uiController != null)
        {
            // [수정] 인자 4개 전달 (isCorrect, explanation, correctCount, totalQuestions)
            uiController.ShowResult(isCorrect, explanation, correctCount, questionsPerPlayer);
        }

        // 2. 정답/오답 연출 (프레스 작동 및 공개)
        if (assetSpawner != null)
        {
            // [추가] 프레스 기계 작동 (애니메이션)
            if (playerAnswer.HasValue)
            {
                assetSpawner.CrushIncorrectAsset(playerAnswer.Value);
            }

            // [기존] 결과 강제 공개 (물음표 -> 실제 물건)
            // 참고: CrushIncorrectAsset 내부에서도 DelayedCrush가 있지만, 
            // ForceRevealResult는 양쪽 모두를 즉시 확실하게 공개하는 역할
            assetSpawner.ForceRevealResult(isCorrect);
        }

        // 3. 결과 보여주기 (3초 대기)
        yield return new WaitForSeconds(showResultDuration);

        // 4. 다음 문제로
        NextQuestion();
    }

    /// <summary>
    /// 다음 문제 또는 다음 턴
    /// </summary>
    void NextQuestion()
    {
        currentQuestionIndex++;

        // 현재 플레이어의 문제가 남았는지 확인
        if (currentQuestionIndex < questionsPerPlayer)
        {
            // 같은 플레이어의 다음 문제
            StartQuestion();
        }
        else
        {
            // [변경] 현재 플레이어 턴 종료 -> 중간 점수판 표시
            ShowIntermediateScoreboard();
        }
    }

    [Header("Delay Settings")]
    public float intermediateScoreDelay = 5f; // 중간 점수판 표시 시간 (Inspector에서 수정 가능)
    public float finalScoreDelay = 3f;        // 최종 점수판 대기 시간 (Inspector에서 수정 가능)

    /// <summary>
    /// [추가] 중간 점수판 표시 및 다음 턴 대기
    /// </summary>
    void ShowIntermediateScoreboard()
    {
        Debug.Log($"[QuizManager] Player {currentPlayerIndex + 1} Turn Ended. Showing Individual Result.");
        
        // Get current player's score
        var playerScore = ScoreManager.Instance.GetPlayerScore(currentPlayerIndex);
        int correctCount = 0;
        if (playerScore != null)
        {
            correctCount = playerScore.workScore / 10;
        }

        // Show Individual Result UI
        if (uiController != null)
        {
            uiController.ShowIndividualResult(currentPlayerIndex, correctCount, questionsPerPlayer);
        }

        // Wait (Inspector 설정 시간만큼 대기)
        Invoke(nameof(ProceedToNextTurn), intermediateScoreDelay);
    }

    /// <summary>
    /// [추가] 다음 플레이어 턴으로 실제로 이동
    /// </summary>
    void ProceedToNextTurn()
    {
        currentQuestionIndex = 0;
        currentPlayerIndex++;

        // [수정] 고정된 MAX_PLAYERS(3) 대신 실제 접속 인원수만큼만 진행
        int maxPlayers = GameManager.Instance != null ? GameManager.Instance.GetCurrentPlayerCount() : GameManager.MAX_PLAYERS;

        if (currentPlayerIndex < maxPlayers)
        {
            StartPlayerTurn(currentPlayerIndex);
        }
        else
        {
            // All players finished -> Show Final Scoreboard
            Debug.Log($"[QuizManager] All players finished. Showing Final Scoreboard in {finalScoreDelay}s.");
            Invoke(nameof(EndQuizSystem), finalScoreDelay);
        }
    }

    /// <summary>
    /// 퀴즈 시스템 종료
    /// </summary>
    void EndQuizSystem()
    {
        Debug.Log("[QuizManager] Showing Final Scoreboard!");

        // Collect all scores
        List<int> allScores = new List<int>();
        
        // [수정] 실제 접속한 플레이어 수만큼만 점수 수집
        int maxPlayers = GameManager.Instance != null ? GameManager.Instance.GetCurrentPlayerCount() : GameManager.MAX_PLAYERS;

        for (int i = 0; i < maxPlayers; i++)
        {
            var score = ScoreManager.Instance.GetPlayerScore(i);
            if (score != null)
            {
                allScores.Add(score.workScore);
            }
            else
            {
                allScores.Add(0);
            }
        }

        // Show Final Scoreboard UI
        if (uiController != null)
        {
            uiController.ShowFinalScoreboard(allScores);
        }
    }

    // [삭제됨] 재시작 기능 제거
    // public void RestartQuiz() { ... }

    /// <summary>
    /// 게임 종료 및 로비로 이동 (Exit 버튼)
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("[QuizManager] 게임 종료 -> 로비로 이동");
        
        // 방 나가기
        PhotonNetwork.LeaveRoom();
    }

    // Photon 콜백: 방을 나갔을 때
    public override void OnLeftRoom()
    {
        // 로비 씬으로 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby"); 
    }

    /// <summary>
    /// 사용되지 않은 랜덤 문제 가져오기
    /// </summary>
    QuizData GetRandomUnusedQuestion()
    {
        List<QuizData> availableQuestions = new List<QuizData>();

        foreach (QuizData q in quizPool)
        {
            if (!usedQuestions.Contains(q))
            {
                availableQuestions.Add(q);
            }
        }

        if (availableQuestions.Count == 0)
        {
            return null;
        }

        // [수정] 랜덤이 아닌 순서대로 가져오기 (0번 인덱스 = 리스트의 첫 번째 미사용 문제)
        return availableQuestions[0];
    }

    /// <summary>
    /// 현재 남은 시간 가져오기
    /// </summary>
    public float GetRemainingTime()
    {
        return currentTimer;
    }

    /// <summary>
    /// 현재 턴 플레이어 인덱스 가져오기
    /// </summary>
    public int GetCurrentPlayerIndex()
    {
        return currentPlayerIndex;
    }

    /// <summary>
    /// 모든 플레이어를 퀴즈 존으로 이동
    /// </summary>
    void TeleportAllPlayersToQuizZone()
    {
        Transform targetZone = quizZone;

        // QuizZone이 없으면 GameManager의 WorkPoint 사용
        if (targetZone == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.workPoint != null)
            {
                targetZone = GameManager.Instance.workPoint;
                Debug.Log("[QuizManager] QuizZone이 없어 GameManager.WorkPoint를 사용합니다.");
            }
            else
            {
                Debug.LogWarning("[QuizManager] 이동할 목표 지점(QuizZone 또는 WorkPoint)이 없습니다!");
                return;
            }
        }

        Debug.Log($"[QuizManager] 모든 플레이어를 {targetZone.name} 위치로 이동");

        // 로컬 플레이어 찾기
        VRPlayerController[] players = FindObjectsOfType<VRPlayerController>();
        foreach (VRPlayerController player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                // 위치 이동 (CharacterController가 있으면 비활성화 후 이동해야 함)
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = targetZone.position;
                player.transform.rotation = targetZone.rotation;

                if (cc != null) cc.enabled = true;

                Debug.Log("[QuizManager] 로컬 플레이어 이동 완료");
                break;
            }
        }
    }


    /// <summary>
    /// 로컬 플레이어의 VR 상호작용(레이저) 활성화/비활성화
    /// </summary>
    void SetPlayerInteraction(bool enabled)
    {
        VRHandInteraction handInteraction = FindObjectOfType<VRHandInteraction>();
        if (handInteraction != null)
        {
            handInteraction.SetInteractionEnabled(enabled);
            Debug.Log($"[QuizManager] VR 상호작용 설정: {enabled}");
        }
    }


    /// <summary>
    /// 모든 플레이어의 이동 잠금 해제
    /// </summary>
    void UnlockAllPlayers()
    {
        VRPlayerController[] players = FindObjectsOfType<VRPlayerController>();
        foreach (VRPlayerController player in players)
        {
            player.SetMovementLock(false);
        }
        Debug.Log("[QuizManager] 모든 플레이어 이동 잠금 해제");
    }

    // [추가] 사운드 재생 헬퍼
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // [추가] 이펙트 재생 헬퍼
    void PlayEffect(GameObject effectPrefab)
    {
        if (effectPrefab != null)
        {
            // 퀴즈 존 위치에 이펙트 생성
            Vector3 spawnPos = transform.position; // 기본값
            if (quizZone != null) spawnPos = quizZone.position + Vector3.up * 1.5f; // 약간 위쪽

            GameObject effect = Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect, 3f); // 3초 후 자동 파괴
        }
    }
}
