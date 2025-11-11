using UnityEngine;
using UnityEngine.Video; // Unity 비디오 플레이어 사용
using UnityEngine.Events; // Unity 이벤트 (예: OnVideoComplete) 사용

[RequireComponent(typeof(VideoPlayer))]
public class SafetyGate : MonoBehaviour
{
    // 영상 시청이 100% 완료되었을 때 호출할 이벤트
    public UnityEvent OnVideoCompleted;

    [Header("UI Control")]
    // 영상 시청 중에 활성화될 UI (예: 비디오 스크린)
    public GameObject videoUI;
    // 영상 시청 완료 후 활성화될 UI (예: PPE 선택 UI)
    public GameObject nextStepUI;

    private VideoPlayer videoPlayer;
    private bool hasCompleted = false;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        // 비디오 재생이 끝나면 OnVideoEnd 함수를 호출하도록 등록
        videoPlayer.loopPointReached += OnVideoEnd;

        // 초기 상태 설정
        if (videoUI) videoUI.SetActive(true);
        if (nextStepUI) nextStepUI.SetActive(false); // 다음 단계 UI는 숨김

        videoPlayer.Play();
    }

    // 비디오 재생이 끝까지 도달했을 때 호출됨
    void OnVideoEnd(VideoPlayer vp)
    {
        if (hasCompleted) return; // 중복 실행 방지
        hasCompleted = true;

        Debug.Log("안전수칙 영상 100% 시청 완료.");

        // 현재 UI 끄고, 다음 단계 UI 켜기
        if (videoUI) videoUI.SetActive(false);
        if (nextStepUI) nextStepUI.SetActive(true);

        // 등록된 OnVideoCompleted 이벤트를 실행
        // (예: PPESystem을 활성화시킴)
        OnVideoCompleted?.Invoke();

        // (네트워크) 호스트에게 내가 준비되었음을 알림
        // 예: FindObjectOfType<NetSession1>().SendReadyStatus("videoComplete");
    }

    // (옵션) 퀴즈 체크포인트용 함수 (영상 중간에 호출)
    public void ShowQuizCheckpoint()
    {
        videoPlayer.Pause();
        // quizUI.SetActive(true);
        Debug.Log("퀴즈 체크포인트! (일시 정지)");
    }
}