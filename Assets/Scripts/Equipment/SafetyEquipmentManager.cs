using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// 보호구 선택 시스템 (Phase 3)
/// 5개 보호구 중 3개 선택 (정답: 안전화, 안전모, 안전장갑)
/// </summary>
public class SafetyEquipmentManager : MonoBehaviourPun
{
    [Header("Equipment Buttons")]
    public Button[] equipmentButtons; // 5개 버튼

    [Header("Equipment Names")]
    public string[] equipmentNames = new string[5]
    {
        "안전화",
        "안전모",
        "안전장갑",
        "더미1",
        "더미2"
    };

    [Header("Correct Answers (Indices)")]
    public int[] correctIndices = new int[] { 0, 1, 2 }; // 안전화(0), 안전모(1), 안전장갑(2)

    [Header("Scoring")]
    public int pointsPerCorrect = 100;
    public int pointsPerWrong = -50;

    [Header("UI")]
    public GameObject selectionUI;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI selectionCountText;

    // 플레이어별 선택 상태 (Key: PlayerID, Value: 선택한 인덱스 리스트)
    private Dictionary<int, List<int>> playerSelections = new Dictionary<int, List<int>>();

    // 로컬 플레이어의 선택
    private HashSet<int> mySelections = new HashSet<int>();

    // 플레이어별 점수
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();

    // 모든 플레이어가 선택 완료했는지 체크
    private int playersCompleted = 0;

    void Start()
    {
        // 버튼 이벤트 등록
        for (int i = 0; i < equipmentButtons.Length; i++)
        {
            int index = i;
            equipmentButtons[i].onClick.AddListener(() => OnEquipmentClick(index));
        }

        // 초기 상태: UI 비활성화
        if (selectionUI != null)
        {
            selectionUI.SetActive(false);
        }
    }

    /// <summary>
    /// 보호구 선택 시작
    /// </summary>
    public void StartSelection()
    {
        Debug.Log("[SafetyEquipmentManager] 보호구 선택 시작");

        // UI 활성화
        if (selectionUI != null)
        {
            selectionUI.SetActive(true);
        }

        // 안내 문구
        if (instructionText != null)
        {
            instructionText.text = "적절한 보호구 3개를 선택하세요";
        }

        UpdateSelectionCountUI();

        // 모든 플레이어 이동 가능 (VR 버전)
        VRPlayerController[] players = FindObjectsOfType<VRPlayerController>();
        foreach (VRPlayerController player in players)
        {
            player.SetMovementLock(false);
        }
    }

    /// <summary>
    /// 보호구 버튼 클릭 시 호출
    /// </summary>
    void OnEquipmentClick(int index)
    {
        if (mySelections.Count >= 3 && !mySelections.Contains(index))
        {
            Debug.Log("[SafetyEquipmentManager] 이미 3개를 선택했습니다.");
            return;
        }

        // 토글 선택
        if (mySelections.Contains(index))
        {
            mySelections.Remove(index);
            Debug.Log($"[SafetyEquipmentManager] 선택 해제: {equipmentNames[index]}");
        }
        else
        {
            mySelections.Add(index);
            Debug.Log($"[SafetyEquipmentManager] 선택: {equipmentNames[index]}");
        }

        // 버튼 색상 업데이트
        UpdateButtonVisuals();
        UpdateSelectionCountUI();

        // 3개 선택 완료 시 자동 제출
        if (mySelections.Count == 3)
        {
            SubmitSelection();
        }
    }

    /// <summary>
    /// 버튼 색상 업데이트
    /// </summary>
    void UpdateButtonVisuals()
    {
        for (int i = 0; i < equipmentButtons.Length; i++)
        {
            ColorBlock colors = equipmentButtons[i].colors;

            if (mySelections.Contains(i))
            {
                colors.normalColor = Color.green;
                colors.highlightedColor = Color.green * 0.8f;
            }
            else
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.gray;
            }

            equipmentButtons[i].colors = colors;
        }
    }

    /// <summary>
    /// 선택 개수 UI 업데이트
    /// </summary>
    void UpdateSelectionCountUI()
    {
        if (selectionCountText != null)
        {
            selectionCountText.text = $"선택: {mySelections.Count}/3";
        }
    }

    /// <summary>
    /// 선택 제출
    /// </summary>
    void SubmitSelection()
    {
        Debug.Log("[SafetyEquipmentManager] 선택 제출");

        // 버튼 비활성화
        foreach (Button btn in equipmentButtons)
        {
            btn.interactable = false;
        }

        // 점수 계산
        int score = CalculateScore();
        Debug.Log($"[SafetyEquipmentManager] 획득 점수: {score}");

        // ScoreManager에 점수 저장
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null)
        {
            int playerIndex = GameManager.Instance.GetLocalPlayerIndex();
            scoreManager.AddEquipmentScore(playerIndex, score);
        }

        // 마스터 클라이언트에게 완료 알림
        int localPlayerIndex = GameManager.Instance.GetLocalPlayerIndex();
        photonView.RPC("RPC_PlayerCompleted", RpcTarget.MasterClient, localPlayerIndex, score);
    }

    /// <summary>
    /// 점수 계산
    /// </summary>
    int CalculateScore()
    {
        int score = 0;
        HashSet<int> correctSet = new HashSet<int>(correctIndices);

        foreach (int selection in mySelections)
        {
            if (correctSet.Contains(selection))
            {
                score += pointsPerCorrect;
                Debug.Log($"[SafetyEquipmentManager] 정답: {equipmentNames[selection]} (+{pointsPerCorrect}점)");
            }
            else
            {
                score += pointsPerWrong;
                Debug.Log($"[SafetyEquipmentManager] 오답: {equipmentNames[selection]} ({pointsPerWrong}점)");
            }
        }

        return score;
    }

    [PunRPC]
    void RPC_PlayerCompleted(int playerIndex, int score)
    {
        Debug.Log($"[SafetyEquipmentManager] Player {playerIndex} 완료, 점수: {score}");

        playersCompleted++;

        // 모든 플레이어(현재 접속 인원) 완료 체크
        if (playersCompleted >= PhotonNetwork.CurrentRoom.PlayerCount && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[SafetyEquipmentManager] 모든 플레이어 선택 완료! Phase 4로 전환");

            // Phase 4 (프레스 작업)로 전환
            GameManager.Instance.ChangePhase(GameManager.GamePhase.PressWork);
        }
    }

    /// <summary>
    /// UI 숨기기
    /// </summary>
    public void HideSelectionUI()
    {
        if (selectionUI != null)
        {
            selectionUI.SetActive(false);
        }
    }
}
