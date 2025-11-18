using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// 턴제 프레스 작업 시스템 관리
/// Player 1 → Player 2 → Player 3 순서로 작업
/// </summary>
public class TurnManager : MonoBehaviourPun
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn Settings")]
    public int currentPlayerTurn = 0; // 0, 1, 2 (Player 1, 2, 3)

    [Header("Zone References")]
    public Transform waitZone;
    public Transform workZone;

    [Header("UI")]
    public TextMeshProUGUI currentTurnText;
    public GameObject turnNotificationUI;
    public TextMeshProUGUI turnNotificationText;

    // 플레이어별 작업 완료 여부
    private bool[] playerCompleted = new bool[GameManager.MAX_PLAYERS];

    // 플레이어별 성공 카운트
    private int[] playerSuccessCount = new int[GameManager.MAX_PLAYERS];

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

    /// <summary>
    /// 턴제 시스템 시작
    /// </summary>
    public void StartTurnSystem()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[TurnManager] 턴 시스템은 마스터 클라이언트만 시작할 수 있습니다.");
            return;
        }

        Debug.Log("[TurnManager] 턴제 시스템 시작");

        // 초기화
        currentPlayerTurn = 0;
        for (int i = 0; i < GameManager.MAX_PLAYERS; i++)
        {
            playerCompleted[i] = false;
            playerSuccessCount[i] = 0;
        }

        // 첫 번째 플레이어 턴 시작
        photonView.RPC("RPC_StartTurn", RpcTarget.All, currentPlayerTurn);
    }

    [PunRPC]
    void RPC_StartTurn(int playerIndex)
    {
        currentPlayerTurn = playerIndex;

        Debug.Log($"[TurnManager] Player {playerIndex + 1}의 턴 시작");

        // UI 업데이트
        UpdateTurnUI();

        // 현재 턴 플레이어에게 알림
        int localPlayerIndex = GameManager.Instance.GetLocalPlayerIndex();
        if (localPlayerIndex == currentPlayerTurn)
        {
            ShowTurnNotification("당신의 차례입니다!", Color.green);

            // 플레이어를 작업 구역으로 이동 (선택사항)
            // MovePlayerToZone(workZone);
        }
        else
        {
            ShowTurnNotification($"Player {currentPlayerTurn + 1}의 차례입니다. 대기해주세요.", Color.yellow);

            // 플레이어를 대기 구역으로 이동 (선택사항)
            // MovePlayerToZone(waitZone);
        }

        // ProductSpawner에게 제품 소재 생성 신호
        ProductSpawner spawner = FindObjectOfType<ProductSpawner>();
        if (spawner != null && PhotonNetwork.IsMasterClient)
        {
            spawner.SpawnProductMaterial(currentPlayerTurn);
        }
    }

    /// <summary>
    /// 턴 UI 업데이트
    /// </summary>
    void UpdateTurnUI()
    {
        if (currentTurnText != null)
        {
            currentTurnText.text = $"현재 차례: Player {currentPlayerTurn + 1}";
        }
    }

    /// <summary>
    /// 턴 알림 표시
    /// </summary>
    void ShowTurnNotification(string message, Color color)
    {
        if (turnNotificationUI != null && turnNotificationText != null)
        {
            turnNotificationUI.SetActive(true);
            turnNotificationText.text = message;
            turnNotificationText.color = color;

            // 3초 후 자동 숨김
            Invoke(nameof(HideTurnNotification), 3f);
        }
    }

    void HideTurnNotification()
    {
        if (turnNotificationUI != null)
        {
            turnNotificationUI.SetActive(false);
        }
    }

    /// <summary>
    /// 플레이어가 제품을 성공적으로 제작했을 때 호출
    /// </summary>
    public void OnProductSuccess(int playerIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        playerSuccessCount[playerIndex]++;

        Debug.Log($"[TurnManager] Player {playerIndex + 1} 성공 카운트: {playerSuccessCount[playerIndex]}/3");

        // RPC로 모든 클라이언트에 동기화
        photonView.RPC("RPC_UpdateSuccessCount", RpcTarget.All, playerIndex, playerSuccessCount[playerIndex]);

        // 3회 성공 시 플레이어 작업 완료
        if (playerSuccessCount[playerIndex] >= 3)
        {
            OnPlayerComplete(playerIndex);
        }
    }

    [PunRPC]
    void RPC_UpdateSuccessCount(int playerIndex, int count)
    {
        playerSuccessCount[playerIndex] = count;

        // UI 업데이트 (예: ProductionSystem에서 처리)
        ProductionSystem productionSystem = FindObjectOfType<ProductionSystem>();
        if (productionSystem != null)
        {
            productionSystem.UpdateSuccessUI(playerIndex, count);
        }
    }

    /// <summary>
    /// 플레이어 작업 완료 처리
    /// </summary>
    void OnPlayerComplete(int playerIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[TurnManager] Player {playerIndex + 1} 작업 완료!");

        playerCompleted[playerIndex] = true;

        // RPC로 모든 클라이언트에 알림
        photonView.RPC("RPC_PlayerCompleted", RpcTarget.All, playerIndex);

        // 작업 점수 추가 (완료 시 +200점)
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.AddWorkScore(playerIndex, 200);
        }

        // 다음 플레이어 턴으로 전환
        if (currentPlayerTurn < GameManager.MAX_PLAYERS - 1)
        {
            currentPlayerTurn++;
            photonView.RPC("RPC_StartTurn", RpcTarget.All, currentPlayerTurn);
        }
        else
        {
            // 모든 플레이어 작업 완료
            AllPlayersCompleted();
        }
    }

    [PunRPC]
    void RPC_PlayerCompleted(int playerIndex)
    {
        Debug.Log($"[TurnManager] Player {playerIndex + 1} 작업 완료 알림");

        int localPlayerIndex = GameManager.Instance.GetLocalPlayerIndex();
        if (localPlayerIndex == playerIndex)
        {
            ShowTurnNotification("작업 완료! 다음 플레이어 차례입니다.", Color.cyan);
        }
    }

    /// <summary>
    /// 모든 플레이어 작업 완료
    /// </summary>
    void AllPlayersCompleted()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("[TurnManager] 모든 플레이어 작업 완료! Phase 8 (결과)로 전환");

        // Phase 8 (결과)로 전환
        GameManager.Instance.ChangePhase(GameManager.GamePhase.Result);
    }

    /// <summary>
    /// 현재 턴 플레이어인지 확인
    /// </summary>
    public bool IsMyTurn()
    {
        int localPlayerIndex = GameManager.Instance.GetLocalPlayerIndex();
        return localPlayerIndex == currentPlayerTurn;
    }

    /// <summary>
    /// 플레이어를 특정 구역으로 이동 (선택사항) - VR 버전
    /// </summary>
    void MovePlayerToZone(Transform zone)
    {
        VRPlayerController localPlayer = FindLocalPlayer();
        if (localPlayer != null && zone != null)
        {
            localPlayer.transform.position = zone.position;
        }
    }

    VRPlayerController FindLocalPlayer()
    {
        VRPlayerController[] players = FindObjectsOfType<VRPlayerController>();
        foreach (VRPlayerController player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return player;
            }
        }
        return null;
    }
}
