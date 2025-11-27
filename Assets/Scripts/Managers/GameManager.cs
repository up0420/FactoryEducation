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

    [Header("Spawn Settings")]
    public Transform[] spawnPoints; // 3개의 스폰 포인트
    public GameObject vrPlayerPrefab; // VR 플레이어 프리팹 (Camera Rig)

    // 현재 접속된 플레이어 수
    private int currentPlayerCount = 0;
    private GameObject spawnedPlayer;
    private PhotonView photonView;

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

        // PhotonView 컴포넌트 가져오기
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("[GameManager] PhotonView 컴포넌트가 없습니다! GameManager에 PhotonView를 추가하세요.");
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
            MaxPlayers = 3, // 명시적으로 3명 설정
            IsVisible = true,
            IsOpen = true
        };

        Debug.Log($"[GameManager] 방 설정 - MaxPlayers: {roomOptions.MaxPlayers}");

        PhotonNetwork.JoinOrCreateRoom("SafetyTrainingRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[GameManager] 방 입장 완료. 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        Debug.Log($"[GameManager] 방 이름: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"[GameManager] 내 ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");

        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        UpdatePlayerCountUI();

        // 자동 시작 제거 - 수동 시작 버튼으로 변경
        // CheckAllPlayersReady();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[GameManager] 새 플레이어 입장: {newPlayer.NickName}");

        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        UpdatePlayerCountUI();

        // 자동 시작 제거
        // CheckAllPlayersReady();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[GameManager] 플레이어 퇴장: {otherPlayer.NickName}");

        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        UpdatePlayerCountUI();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[GameManager] 방 입장 실패! 코드: {returnCode}, 메시지: {message}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[GameManager] 방 생성 실패! 코드: {returnCode}, 메시지: {message}");
    }

    #endregion

    void UpdatePlayerCountUI()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"대기 중... {currentPlayerCount}/{MAX_PLAYERS}";
        }
    }

    /// <summary>
    /// 게임 시작 (아무나 호출 가능)
    /// </summary>
    public void StartGame()
    {
        if (currentPhase != GamePhase.Lobby)
        {
            Debug.LogWarning("[GameManager] 이미 게임이 시작되었습니다.");
            return;
        }

        Debug.Log("[GameManager] 게임 시작 버튼 클릭!");

        // 모든 클라이언트에게 게임 시작 신호 전송
        if (photonView != null)
        {
            photonView.RPC("RPC_StartGame", RpcTarget.All);
        }
        else
        {
            Debug.LogError("[GameManager] PhotonView가 null입니다! 게임 시작 실패");
        }
    }

    [PunRPC]
    void RPC_StartGame()
    {
        Debug.Log("[GameManager] 게임 시작!");
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
        else
        {
            Debug.LogError("[GameManager] VideoManager를 찾을 수 없습니다!");
        }
    }

    void CheckAllPlayersReady()
    {
        // 더 이상 사용하지 않음 - 수동 시작으로 변경
        if (currentPlayerCount == MAX_PLAYERS && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[GameManager] 모든 플레이어 준비 완료! Phase 2로 전환");

            // 모든 클라이언트에게 Phase 2 시작 신호 전송
            if (photonView != null)
            {
                photonView.RPC("StartPhase2", RpcTarget.All);
            }
            else
            {
                Debug.LogError("[GameManager] PhotonView가 null입니다! RPC 호출 실패");
            }
        }
    }

    /// <summary>
    /// 영상 종료 후 호출됨
    /// </summary>
    public void OnVideoFinished()
    {
        Debug.Log("[GameManager] 영상 종료 - 플레이어 스폰");

        // 영상 종료 후 프레스 앞으로 스폰
        if (photonView != null)
        {
            photonView.RPC("RPC_SpawnPlayers", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_SpawnPlayers()
    {
        Debug.Log("[GameManager] 플레이어 스폰 시작");
        SpawnVRPlayer();
    }

    [PunRPC]
    void StartPhase2()
    {
        Debug.Log("[GameManager] Phase 2: 영상 교육 시작");
        currentPhase = GamePhase.VideoEducation;

        // UI 전환
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);

        // 영상 재생 (스폰은 영상 후)
        VideoManager videoManager = FindObjectOfType<VideoManager>();
        if (videoManager != null)
        {
            videoManager.PlaySafetyVideo();
        }
    }

    /// <summary>
    /// VR 플레이어 스폰
    /// </summary>
    void SpawnVRPlayer()
    {
        if (spawnedPlayer != null)
        {
            Debug.LogWarning("[GameManager] 플레이어가 이미 스폰되었습니다.");
            return;
        }

        // 플레이어 번호에 따라 스폰 위치 결정 (1, 2, 3)
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[GameManager] 스폰 포인트가 설정되지 않았습니다!");
            return;
        }

        // 스폰 포인트 선택 (범위 체크)
        int spawnIndex = Mathf.Clamp(playerIndex, 0, spawnPoints.Length - 1);
        Transform spawnPoint = spawnPoints[spawnIndex];

        Debug.Log($"[GameManager] 플레이어 {playerIndex + 1} 스폰: {spawnPoint.position}");

        // VR 플레이어 스폰
        if (vrPlayerPrefab != null)
        {
            spawnedPlayer = PhotonNetwork.Instantiate(
                vrPlayerPrefab.name,
                spawnPoint.position,
                spawnPoint.rotation
            );
        }
        else
        {
            Debug.LogError("[GameManager] VR 플레이어 프리팹이 설정되지 않았습니다!");
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

        if (photonView != null)
        {
            photonView.RPC("SyncPhase", RpcTarget.All, (int)newPhase);
        }
        else
        {
            Debug.LogError("[GameManager] PhotonView가 null입니다! ChangePhase RPC 호출 실패");
        }
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
