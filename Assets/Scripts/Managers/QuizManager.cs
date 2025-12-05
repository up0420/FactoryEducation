using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.VFX; // Visual Effect Graph 사용

/// <summary>
/// 퀴즈 시스템 매니저 (통합 버전)
/// - 퀴즈 진행 (문제 출제, 타이머, 정답 확인)
/// - 플레이어 이동 및 권한 제어
/// - 사운드 및 이펙트 재생
/// - 외부 호출용 메서드 (Restart, Exit 등) 포함
/// </summary>
public class QuizManager : MonoBehaviourPunCallbacks
{
    public static QuizManager Instance { get; private set; }

    [Header("Quiz Settings")]
    public List<QuizData> quizPool = new List<QuizData>(); // 퀴즈 데이터 리스트
    public int questionsPerPlayer = 5;   // 플레이어당 문제 수
    public float timePerQuestion = 10f;  // 문제당 제한 시간

    [Header("References")]
    public QuizUIController uiController; // UI 컨트롤러 연결
    public QuizAssetSpawner assetSpawner; // 에셋 스포너 연결
    public Transform quizZone;            // 퀴즈 진행 위치 (플레이어 이동용)

    [Header("Polish (Sound & Effects)")]
    public AudioSource audioSource;       // 오디오 소스 컴포넌트
    public AudioClip correctSound;        // 정답 효과음 (딩동댕)
    public AudioClip wrongSound;          // 오답 효과음 (땡)

    [Tooltip("파티클 시스템(Legacy) 또는 Visual Effect(VFX Graph)가 붙은 오브젝트를 넣으세요.")]
    public GameObject correctEffect;      // 이펙트 오브젝트 (폭죽 등)

    // 내부 상태 변수
    private int currentPlayerIndex = 0;   // 현재 턴 플레이어 (0, 1, 2)
    private int currentQuestionIndex = 0; // 현재 문제 번호 (0 ~ questionsPerPlayer-1)
    private List<QuizData> usedQuestions = new List<QuizData>(); // 중복 출제 방지용
    private float currentTimer = 0f;
    private bool isTimerRunning = false;
    private bool? playerAnswer = null;    // null: 미선택, true: O, false: X

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
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
                OnTimeUp(); // 시간 종료 처리
            }
        }
    }

    // ========================================================================
    // 1. 퀴즈 시스템 시작 및 진행 로직
    // ========================================================================

    /// <summary>
    /// 퀴즈 시스템 전체 시작 (외부 호출용)
    /// </summary>
    public void StartQuizSystem()
    {
        Debug.Log("[QuizManager] 퀴즈 시스템 시작");

        if (uiController != null)
        {
            uiController.gameObject.SetActive(true);
        }

        currentPlayerIndex = 0;
        currentQuestionIndex = 0;
        usedQuestions.Clear();

        // 플레이어 이동 및 초기화
        TeleportAllPlayersToQuizZone();
        UnlockAllPlayers();

        // 첫 번째 플레이어 턴 시작
        StartPlayerTurn(currentPlayerIndex);
    }

    /// <summary>
    /// 특정 플레이어의 턴 시작
    /// </summary>
    void StartPlayerTurn(int playerIndex)
    {
        Debug.Log($"[QuizManager] Player {playerIndex + 1}의 턴 시작");
        currentPlayerIndex = playerIndex;

        if (uiController != null)
        {
            int localPlayerIndex = -1;
            if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
                localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

            bool isMyTurn = (localPlayerIndex == currentPlayerIndex);

            uiController.ShowStandby(isMyTurn);

            // 내 턴이면 상호작용 활성화
            SetPlayerInteraction(isMyTurn);
        }
    }

    /// <summary>
    /// 문제 출제 (Start 버튼 누르면 호출)
    /// </summary>
    public void StartQuestion()
    {
        QuizData selectedQuestion = GetRandomUnusedQuestion();
        if (selectedQuestion == null)
        {
            Debug.LogError("[QuizManager] 사용 가능한 문제가 없습니다! (Quiz Data 확인 필요)");
            return;
        }

        usedQuestions.Add(selectedQuestion);
        Debug.Log($"[QuizManager] 문제 출제: {selectedQuestion.questionText}");

        currentTimer = timePerQuestion;
        isTimerRunning = true;
        playerAnswer = null;

        // UI 및 에셋 갱신
        if (uiController != null) uiController.ShowQuestion(selectedQuestion.questionText);
        if (assetSpawner != null) assetSpawner.SpawnAssets(selectedQuestion);
    }

    /// <summary>
    /// 답안 선택 (UI나 컨트롤러에서 호출)
    /// </summary>
    public void SelectAnswer(bool answer)
    {
        if (!isTimerRunning) return;

        // 내 턴인지 확인
        int localPlayerIndex = -1;
        if (PhotonNetwork.IsConnected) localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        if (localPlayerIndex != currentPlayerIndex)
        {
            Debug.LogWarning("당신의 차례가 아닙니다.");
            return;
        }

        playerAnswer = answer;
        Debug.Log($"[QuizManager] 답안 선택: {(answer ? "O" : "X")}");

        if (uiController != null) uiController.HighlightAnswer(answer);
    }

    /// <summary>
    /// 타이머 종료 (채점 및 결과 처리)
    /// </summary>
    void OnTimeUp()
    {
        isTimerRunning = false;

        if (usedQuestions.Count == 0) return;
        QuizData currentQuestion = usedQuestions[usedQuestions.Count - 1];

        // 정답 확인
        bool isCorrect = (playerAnswer.HasValue && playerAnswer.Value == currentQuestion.correctAnswer);

        if (isCorrect)
        {
            Debug.Log("정답입니다!");
            // 점수 추가
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddWorkScore(currentPlayerIndex, 10);

            // 효과음 및 이펙트
            PlaySound(correctSound);
            PlayEffect(correctEffect);
        }
        else
        {
            Debug.Log("오답입니다.");
            PlaySound(wrongSound);
        }

        // 결과 UI 표시
        if (uiController != null)
            uiController.ShowResult(isCorrect, currentQuestion.explanation);

        // 오답 에셋 파괴 연출
        if (assetSpawner != null && playerAnswer.HasValue)
        {
            try { assetSpawner.CrushIncorrectAsset(playerAnswer.Value); } catch { }
        }

        // 3초 뒤 다음 단계로
        Invoke(nameof(NextQuestion), 3f);
    }

    /// <summary>
    /// 다음 문제 또는 턴 종료 처리
    /// </summary>
    void NextQuestion()
    {
        currentQuestionIndex++;

        // 아직 풀 문제가 남았다면 계속 진행
        if (currentQuestionIndex < questionsPerPlayer)
        {
            StartQuestion();
        }
        else
        {
            // 해당 플레이어의 할당량 끝 -> 중간 점수판 표시
            ShowIntermediateScoreboard();
        }
    }

    void ShowIntermediateScoreboard()
    {
        var playerScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetPlayerScore(currentPlayerIndex) : null;
        int correctCount = playerScore != null ? playerScore.workScore / 10 : 0;

        if (uiController != null)
        {
            uiController.ShowIndividualResult(currentPlayerIndex, correctCount, questionsPerPlayer);
        }

        // 5초 뒤 다음 사람에게 턴 넘기기
        Invoke(nameof(ProceedToNextTurn), 5f);
    }

    void ProceedToNextTurn()
    {
        // 다음 플레이어로 설정
        currentQuestionIndex = 0;
        currentPlayerIndex++;

        // 최대 인원수(3명) 체크
        int maxPlayers = (GameManager.Instance != null) ? GameManager.MAX_PLAYERS : 3;

        if (currentPlayerIndex < maxPlayers)
        {
            StartPlayerTurn(currentPlayerIndex);
        }
        else
        {
            // 모든 플레이어 종료 -> 최종 결과 화면
            Invoke(nameof(EndQuizSystem), 2f);
        }
    }

    void EndQuizSystem()
    {
        List<int> allScores = new List<int>();
        int maxPlayers = (GameManager.Instance != null) ? GameManager.MAX_PLAYERS : 3;

        for (int i = 0; i < maxPlayers; i++)
        {
            var score = ScoreManager.Instance != null ? ScoreManager.Instance.GetPlayerScore(i) : null;
            allScores.Add(score != null ? score.workScore : 0);
        }

        if (uiController != null)
            uiController.ShowFinalScoreboard(allScores);
    }

    // ========================================================================
    // 2. 외부 호출용 메서드 (UI 버튼 등에서 사용)
    // ========================================================================

    /// <summary>
    /// 퀴즈 재시작 (점수 리셋 후 처음부터)
    /// </summary>
    public void RestartQuiz()
    {
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScores();
        StartQuizSystem();
    }

    /// <summary>
    /// 게임 종료 (방 나가기)
    /// </summary>
    public void ExitGame()
    {
        PhotonNetwork.LeaveRoom();
        // 이후 OnLeftRoom() 콜백 등에서 씬 전환 처리 필요
    }

    /// <summary>
    /// 남은 시간 반환 (UI 표시용)
    /// </summary>
    public float GetRemainingTime()
    {
        return currentTimer;
    }

    // ========================================================================
    // 3. 내부 헬퍼 메서드 (누락되었던 기능들 구현)
    // ========================================================================

    /// <summary>
    /// 사용하지 않은 랜덤 문제 뽑기
    /// </summary>
    QuizData GetRandomUnusedQuestion()
    {
        List<QuizData> candidates = new List<QuizData>();
        foreach (var q in quizPool)
        {
            if (!usedQuestions.Contains(q)) candidates.Add(q);
        }

        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// 플레이어들을 퀴즈 구역으로 이동
    /// </summary>
    void TeleportAllPlayersToQuizZone()
    {
        if (quizZone == null) return;

        // 로컬 플레이어(나) 찾아서 이동
        var localPlayer = FindLocalVRPlayer();
        if (localPlayer != null)
        {
            // CharacterController 간섭 방지
            var cc = localPlayer.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            localPlayer.transform.position = quizZone.position;
            localPlayer.transform.rotation = quizZone.rotation;

            if (cc) cc.enabled = true;
        }
    }

    /// <summary>
    /// 모든 플레이어 이동 잠금 해제
    /// </summary>
    void UnlockAllPlayers()
    {
        // 로컬 플레이어만 제어하면 됨
        var localPlayer = FindLocalVRPlayer();
        if (localPlayer != null)
        {
            localPlayer.SetMovementLock(false);
        }
    }

    /// <summary>
    /// 로컬 플레이어 상호작용(레이저 등) 활성화/비활성화
    /// </summary>
    void SetPlayerInteraction(bool enabled)
    {
        var handInteraction = FindObjectOfType<VRHandInteraction>();
        if (handInteraction != null)
        {
            handInteraction.SetInteractionEnabled(enabled);
        }
    }

    /// <summary>
    /// 사운드 재생
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 이펙트(파티클/VFX) 재생
    /// </summary>
    void PlayEffect(GameObject effectObj)
    {
        if (effectObj == null) return;

        // 파티클 시스템(Legacy) 체크
        var particle = effectObj.GetComponent<ParticleSystem>();
        if (particle != null)
        {
            particle.Play();
            return;
        }

        // VFX Graph 체크
        var vfx = effectObj.GetComponent<VisualEffect>();
        if (vfx != null)
        {
            vfx.Play();
            return;
        }

        // 둘 다 아니면 그냥 활성화 (Setactive) 시도
        effectObj.SetActive(true);
    }

    /// <summary>
    /// 씬에서 로컬 VR 플레이어(VRPlayerController) 찾기
    /// </summary>
    VRPlayerController FindLocalVRPlayer()
    {
        var players = FindObjectsOfType<VRPlayerController>();
        foreach (var p in players)
        {
            // PhotonView가 내 것이거나, 없으면(싱글) 로컬로 간주
            if (p.photonView == null || p.photonView.IsMine)
                return p;
        }
        return null;
    }
}