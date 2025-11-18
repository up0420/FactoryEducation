using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 전체 흐름을 관리하는 메인 매니저
/// Photon PUN2를 사용한 멀티플레이어 로비 및 매칭 시스템
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public const int MAX_PLAYERS = 3;

    [Header("Game Phases")]
    public GamePhase currentPhase = GamePhase.Lobby;

    [Header("UI References")]
    public TextMeshProUGUI playerCountText;
    public GameObject lobbyUI;
    public GameObject gameUI;

    // 현재 접속된 플레이어 수
    private int currentPlayerCount = 0;

    public enum GamePhase
    {
        Lobby,              // 대기실
        VideoEducation,     // 영상 시청
        EquipmentSelection, // 보호구 선택
        PressWork,          // 프레스 작업
        Result              // 결과
    }

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Photon 서버 연결
        ConnectToPhoton();
    }

    void ConnectToPhoton()
    {
        Debug.Log("[GameManager] Photon 서버에 연결 중...");
        PhotonNetwork.ConnectUsingSettings();
    }

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("[GameManager] Photon 마스터 서버 연결 완료");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[GameManager] 로비 입장 완료");

        // 자동으로 방 생성 또는 참가
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = MAX_PLAYERS,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.JoinOrCreateRoom("SafetyTrainingRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[GameManager] 방 입장 완료. 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{MAX_PLAYERS}");

        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        UpdatePlayerCountUI();

        // 3명이 모두 접속했는지 확인
        CheckAllPlayersReady();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[GameManager] 새 플레이어 입장: {newPlayer.NickName}");

        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        UpdatePlayerCountUI();

        // 3명이 모두 접속했는지 확인
        CheckAllPlayersReady();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[GameManager] 플레이어 퇴장: {otherPlayer.NickName}");

        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        UpdatePlayerCountUI();
    }

    #endregion

    void UpdatePlayerCountUI()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"대기 중... {currentPlayerCount}/{MAX_PLAYERS}";
        }
    }

    void CheckAllPlayersReady()
    {
        if (currentPlayerCount == MAX_PLAYERS && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[GameManager] 모든 플레이어 준비 완료! Phase 2로 전환");

            // 모든 클라이언트에게 Phase 2 시작 신호 전송
            photonView.RPC("StartPhase2", RpcTarget.All);
        }
    }

    [PunRPC]
    void StartPhase2()
    {
        Debug.Log("[GameManager] Phase 2: 영상 교육 시작");
        currentPhase = GamePhase.VideoEducation;

        // UI 전환
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);

        // VideoManager에게 영상 재생 신호
        VideoManager videoManager = FindObjectOfType<VideoManager>();
        if (videoManager != null)
        {
            videoManager.PlaySafetyVideo();
        }
    }

    /// <summary>
    /// Phase 전환 (마스터 클라이언트만 호출)
    /// </summary>
    public void ChangePhase(GamePhase newPhase)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[GameManager] Phase 전환은 마스터 클라이언트만 가능합니다.");
            return;
        }

        photonView.RPC("SyncPhase", RpcTarget.All, (int)newPhase);
    }

    [PunRPC]
    void SyncPhase(int phaseIndex)
    {
        currentPhase = (GamePhase)phaseIndex;
        Debug.Log($"[GameManager] Phase 전환: {currentPhase}");

        // Phase에 따른 처리
        switch (currentPhase)
        {
            case GamePhase.EquipmentSelection:
                OnEquipmentSelectionPhase();
                break;
            case GamePhase.PressWork:
                OnPressWorkPhase();
                break;
            case GamePhase.Result:
                OnResultPhase();
                break;
        }
    }

    void OnEquipmentSelectionPhase()
    {
        Debug.Log("[GameManager] 보호구 선택 Phase 시작");

        SafetyEquipmentManager equipmentManager = FindObjectOfType<SafetyEquipmentManager>();
        if (equipmentManager != null)
        {
            equipmentManager.StartSelection();
        }
    }

    void OnPressWorkPhase()
    {
        Debug.Log("[GameManager] 프레스 작업 Phase 시작");

        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.StartTurnSystem();
        }
    }

    void OnResultPhase()
    {
        Debug.Log("[GameManager] 결과 Phase 시작");

        ScoreboardManager scoreboardManager = FindObjectOfType<ScoreboardManager>();
        if (scoreboardManager != null)
        {
            scoreboardManager.ShowScoreboard();
        }
    }

    /// <summary>
    /// 로컬 플레이어의 Photon 플레이어 인덱스 가져오기 (0, 1, 2)
    /// </summary>
    public int GetLocalPlayerIndex()
    {
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].IsLocal)
            {
                return i;
            }
        }
        return -1;
    }
}
