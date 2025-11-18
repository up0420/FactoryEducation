using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// 결과 점수판 시스템 (Phase 8)
/// 플레이어별 점수 표시 및 순위 계산
/// </summary>
public class ScoreboardManager : MonoBehaviourPun
{
    [Header("Scoreboard UI")]
    public GameObject scoreboardUI;
    public Transform scoreboardContent; // 점수 항목들이 배치될 부모

    [Header("Score Entry Prefab")]
    public GameObject scoreEntryPrefab; // 점수 항목 프리팹

    [Header("Rank Colors")]
    public Color firstPlaceColor = Color.yellow;
    public Color secondPlaceColor = new Color(0.75f, 0.75f, 0.75f); // 은색
    public Color thirdPlaceColor = new Color(0.8f, 0.5f, 0.2f);     // 동색

    [Header("Buttons")]
    public Button restartButton;
    public Button exitButton;

    void Start()
    {
        // 초기 상태: 점수판 숨김
        if (scoreboardUI != null)
        {
            scoreboardUI.SetActive(false);
        }

        // 버튼 이벤트 등록
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClick);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClick);
        }
    }

    /// <summary>
    /// 점수판 표시
    /// </summary>
    public void ShowScoreboard()
    {
        Debug.Log("[ScoreboardManager] 점수판 표시");

        // UI 활성화
        if (scoreboardUI != null)
        {
            scoreboardUI.SetActive(true);
        }

        // 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 점수 데이터 가져오기
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogError("[ScoreboardManager] ScoreManager를 찾을 수 없습니다.");
            return;
        }

        // 정렬된 점수 리스트
        List<ScoreManager.PlayerScore> sortedScores = scoreManager.GetSortedScores();

        // 기존 점수 항목 제거
        ClearScoreboard();

        // 점수 항목 생성
        for (int i = 0; i < sortedScores.Count; i++)
        {
            CreateScoreEntry(sortedScores[i], i + 1);
        }
    }

    /// <summary>
    /// 기존 점수 항목 제거
    /// </summary>
    void ClearScoreboard()
    {
        if (scoreboardContent == null) return;

        foreach (Transform child in scoreboardContent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 점수 항목 생성
    /// </summary>
    void CreateScoreEntry(ScoreManager.PlayerScore playerScore, int rank)
    {
        if (scoreEntryPrefab == null || scoreboardContent == null)
        {
            Debug.LogError("[ScoreboardManager] scoreEntryPrefab 또는 scoreboardContent가 설정되지 않았습니다.");
            return;
        }

        // 점수 항목 인스턴스 생성
        GameObject entryObj = Instantiate(scoreEntryPrefab, scoreboardContent);

        // ScoreEntry 컴포넌트 가져오기 (또는 직접 UI 요소 설정)
        ScoreEntry entry = entryObj.GetComponent<ScoreEntry>();
        if (entry != null)
        {
            entry.SetScoreData(playerScore, rank, GetRankColor(rank));
        }
        else
        {
            // 프리팹에 ScoreEntry 컴포넌트가 없는 경우 직접 설정
            SetScoreEntryManually(entryObj, playerScore, rank);
        }
    }

    /// <summary>
    /// ScoreEntry 컴포넌트 없이 수동으로 UI 설정
    /// </summary>
    void SetScoreEntryManually(GameObject entryObj, ScoreManager.PlayerScore playerScore, int rank)
    {
        // 자식 TextMeshProUGUI 찾기 (이름 규칙에 따라 조정 필요)
        TextMeshProUGUI[] texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 6)
        {
            texts[0].text = $"#{rank}";                                    // 순위
            texts[1].text = playerScore.playerName;                         // 이름
            texts[2].text = playerScore.equipmentScore.ToString();          // 보호구 점수
            texts[3].text = playerScore.workScore.ToString();               // 작업 점수
            texts[4].text = playerScore.safetyPenalty.ToString();           // 안전 감점
            texts[5].text = playerScore.totalScore.ToString();              // 총점

            // 순위 색상 적용
            Color rankColor = GetRankColor(rank);
            texts[0].color = rankColor;
            texts[1].color = rankColor;
        }

        // 배경 색상 적용 (선택사항)
        Image background = entryObj.GetComponent<Image>();
        if (background != null)
        {
            background.color = GetRankColor(rank) * 0.3f; // 반투명 배경
        }
    }

    /// <summary>
    /// 순위별 색상 반환
    /// </summary>
    Color GetRankColor(int rank)
    {
        switch (rank)
        {
            case 1:
                return firstPlaceColor;
            case 2:
                return secondPlaceColor;
            case 3:
                return thirdPlaceColor;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// 다시 시작 버튼 클릭
    /// </summary>
    void OnRestartClick()
    {
        Debug.Log("[ScoreboardManager] 다시 시작");

        if (PhotonNetwork.IsMasterClient)
        {
            // 방 재시작 로직 (씬 리로드 등)
            PhotonNetwork.LoadLevel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// 종료 버튼 클릭
    /// </summary>
    void OnExitClick()
    {
        Debug.Log("[ScoreboardManager] 게임 종료");

        // 방 나가기
        PhotonNetwork.LeaveRoom();

        // 로비 씬으로 이동 (선택사항)
        // UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    /// <summary>
    /// 점수판 숨기기
    /// </summary>
    public void HideScoreboard()
    {
        if (scoreboardUI != null)
        {
            scoreboardUI.SetActive(false);
        }
    }
}

/// <summary>
/// 개별 점수 항목 UI 컴포넌트 (선택사항)
/// scoreEntryPrefab에 이 컴포넌트를 추가하면 더 편리하게 관리 가능
/// </summary>
public class ScoreEntry : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI equipmentScoreText;
    public TextMeshProUGUI workScoreText;
    public TextMeshProUGUI safetyPenaltyText;
    public TextMeshProUGUI totalScoreText;
    public Image backgroundImage;

    public void SetScoreData(ScoreManager.PlayerScore playerScore, int rank, Color rankColor)
    {
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
            rankText.color = rankColor;
        }

        if (playerNameText != null)
        {
            playerNameText.text = playerScore.playerName;
            playerNameText.color = rankColor;
        }

        if (equipmentScoreText != null)
        {
            equipmentScoreText.text = playerScore.equipmentScore.ToString();
        }

        if (workScoreText != null)
        {
            workScoreText.text = playerScore.workScore.ToString();
        }

        if (safetyPenaltyText != null)
        {
            safetyPenaltyText.text = playerScore.safetyPenalty.ToString();
        }

        if (totalScoreText != null)
        {
            totalScoreText.text = playerScore.totalScore.ToString();
            totalScoreText.color = rankColor;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = rankColor * 0.2f; // 반투명 배경
        }
    }
}
