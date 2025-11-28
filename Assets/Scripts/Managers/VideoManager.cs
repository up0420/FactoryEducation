using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// 안전교육 영상 재생 관리
/// 마스터 클라이언트가 영상 제어
/// </summary>
public class VideoManager : MonoBehaviourPun
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;
    public GameObject videoCanvas; // VideoCanvas 전체를 참조

    [Header("Video Settings")]
    public string videoFileName = "SafetyEducation.mp4";

    private bool isVideoPlaying = false;

    void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // 비디오 플레이어 설정
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;

            // 영상 종료 이벤트 리스너
            videoPlayer.loopPointReached += OnVideoFinished;

            // RenderTexture 생성 및 할당
            if (videoDisplay != null)
            {
                RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
                videoPlayer.targetTexture = renderTexture;
                videoDisplay.texture = renderTexture;
            }
        }
    }

    void Start()
    {
        // 영상 파일 경로 설정
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Videos", videoFileName);
        if (videoPlayer != null)
        {
            videoPlayer.url = videoPath;
            Debug.Log($"[VideoManager] 영상 경로 설정: {videoPath}");
        }

        // 초기 상태: VideoCanvas 전체 비활성화
        if (videoCanvas != null)
        {
            videoCanvas.SetActive(false);
            Debug.Log("[VideoManager] VideoCanvas 초기 비활성화");
        }
        else if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 안전교육 영상 재생 (마스터 클라이언트만 호출)
    /// </summary>
    public void PlaySafetyVideo()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[VideoManager] 영상 재생은 마스터 클라이언트만 가능합니다.");
            return;
        }

        photonView.RPC("RPC_PlayVideo", RpcTarget.All);
    }

    [PunRPC]
    void RPC_PlayVideo()
    {
        Debug.Log("[VideoManager] 안전교육 영상 재생 시작");

        // 모든 플레이어 이동 잠금
        LockAllPlayers(true);

        // VideoCanvas 전체 활성화
        if (videoCanvas != null)
        {
            videoCanvas.SetActive(true);
            Debug.Log("[VideoManager] VideoCanvas 활성화");
        }
        else if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(true);
        }

        // 영상 재생
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            isVideoPlaying = true;
        }
    }

    /// <summary>
    /// 영상 종료 시 호출되는 콜백
    /// </summary>
    void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[VideoManager] 영상 재생 완료");

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_OnVideoEnd", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_OnVideoEnd()
    {
        Debug.Log("[VideoManager] 영상 종료 처리");

        isVideoPlaying = false;

        // VideoCanvas 전체 숨기기
        if (videoCanvas != null)
        {
            videoCanvas.SetActive(false);
            Debug.Log("[VideoManager] VideoCanvas 비활성화");
        }
        else if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
        }

        // 플레이어 이동 해제 (나중에 스폰 후 다시 설정)
        // LockAllPlayers(false);

        // GameManager에 영상 종료 알림 (플레이어 스폰)
        if (GameManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.OnVideoFinished();
        }

        // Phase 3 (보호구 선택)으로 전환 - 주석 처리 (스폰 후 진행)
        // if (PhotonNetwork.IsMasterClient)
        // {
        //     GameManager.Instance.ChangePhase(GameManager.GamePhase.EquipmentSelection);
        // }
    }

    /// <summary>
    /// 모든 플레이어의 이동 잠금/해제 (VR 버전)
    /// </summary>
    void LockAllPlayers(bool locked)
    {
        VRPlayerController[] players = FindObjectsOfType<VRPlayerController>();
        foreach (VRPlayerController player in players)
        {
            player.SetMovementLock(locked);
        }

        Debug.Log($"[VideoManager] 모든 플레이어 이동 {(locked ? "잠김" : "해제")}");
    }

    /// <summary>
    /// 영상 일시정지 (디버그용)
    /// </summary>
    public void PauseVideo()
    {
        if (videoPlayer != null && isVideoPlaying)
        {
            videoPlayer.Pause();
            Debug.Log("[VideoManager] 영상 일시정지");
        }
    }

    /// <summary>
    /// 영상 재개 (디버그용)
    /// </summary>
    public void ResumeVideo()
    {
        if (videoPlayer != null && isVideoPlaying)
        {
            videoPlayer.Play();
            Debug.Log("[VideoManager] 영상 재개");
        }
    }
}
