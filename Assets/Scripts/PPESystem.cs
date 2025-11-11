using UnityEngine;
using UnityEngine.UI; // Button 사용
using System.Collections.Generic;

// 이 스크립트는 HUDController가 같이 있다고 가정
[RequireComponent(typeof(HUDController))]
public class PPESystem : MonoBehaviour
{
    [Header("PPE Candidates (5 Buttons)")]
    public Button[] ppeButtons;

    [Header("Scoring Rules")]
    public float timeLimit = 45.0f; // 시간 보너스 제한 (45초)

    [Header("Audio Cues")]
    public AudioClip selectSound;  // 선택 시 사운드
    public AudioClip correctSound; // 정답 사운드
    public AudioClip wrongSound;   // 오답 사운드
    private AudioSource audioSource;

    // --- 내부 변수 ---
    private HUDController hud;      // UI 피드백 담당
                                    // private NetSession1 netSession; // (A. 호스트) 역할 스크립트 참조

    private HashSet<int> selectedIndices = new HashSet<int>();
    private bool isLocked = false;  // 선택이 확정되었는지
    private float selectStartTime;  // 선택 시작 시간

    void Start()
    {
        hud = GetComponent<HUDController>();
        audioSource = gameObject.AddComponent<AudioSource>();

        // 씬에서 NetSession1 찾기 (A 역할)
        // netSession = FindObjectOfType<NetSession1>();

        for (int i = 0; i < ppeButtons.Length; i++)
        {
            int buttonIndex = i;
            ppeButtons[buttonIndex].onClick.AddListener(() => OnSelectButton(buttonIndex));
        }

        // (SafetyGate가 활성화되면 이 함수를 호출한다고 가정)
        // StartPPESelection(); 
    }

    // SafetyGate가 호출해주는 함수
    public void StartPPESelection()
    {
        this.gameObject.SetActive(true);
        selectStartTime = Time.time;
        hud.ShowToast("훈련 시작: 보호구 3개를 선택하세요.", Color.cyan);
    }

    void OnSelectButton(int index)
    {
        if (isLocked) return;

        if (selectedIndices.Contains(index))
        {
            selectedIndices.Remove(index);
        }
        else
        {
            if (selectedIndices.Count < 3)
            {
                selectedIndices.Add(index);
                PlaySound(selectSound);
            }
        }

        UpdateButtonVisuals();

        if (selectedIndices.Count == 3)
        {
            ConfirmSelection();
        }
    }

    void UpdateButtonVisuals()
    {
        for (int i = 0; i < ppeButtons.Length; i++)
        {
            var colors = ppeButtons[i].colors;
            colors.normalColor = selectedIndices.Contains(i) ? Color.green : Color.white;
            ppeButtons[i].colors = colors;
        }
    }

    // 선택 확정 및 호스트에게 전송
    void ConfirmSelection()
    {
        isLocked = true;
        float elapsed = Time.time - selectStartTime;

        hud.ShowToast("선택 완료. 호스트의 채점 결과를 기다립니다...", Color.white);

        // (A. 호스트) 역할:
        // 이 스크립트는 정답을 모릅니다.
        // 선택한 인덱스 목록(selectedIndices)과 소요 시간(elapsed)을
        // 호스트(NetSession1)에게 전송해야 합니다.

        // if (netSession != null)
        //     netSession.SendPPESelection(selectedIndices, elapsed);

        Debug.Log($"[PPESystem] 선택 확정. 호스트에게 전송: {string.Join(",", selectedIndices)} (소요시간: {elapsed}초)");

        // --- 테스트용 임시 코드 (나중에 삭제) ---
        // (A. 호스트)가 응답했다고 가정
        int testCorrectCount = 2; // (임시) 2개 정답
        int testTotalScore = 5;   // (임시) 5점 획득
        OnReceiveScoreResult(testCorrectCount, testTotalScore, false);
        // ------------------------------------
    }

    // (A. 호스트)가 채점 결과를 보내주면 이 함수를 호출
    public void OnReceiveScoreResult(int correctCount, int totalScore, bool allCorrect)
    {
        // 공유해주신 HUDController의 ShowResult 함수에 맞게 호출
        hud.ShowResult(correctCount, totalScore);

        if (allCorrect)
        {
            PlaySound(correctSound);
        }
        else
        {
            PlaySound(wrongSound);
        }

        Debug.Log("채점 완료. 다른 참가자 대기 중...");
    }

    // (A. 호스트)가 "모두 준비 완료, 게임 시작!" 신호를 보내면 호출
    public void OnReceiveGameStart()
    {
        hud.ShowToast("모든 참가자 준비 완료. 프레스 훈련을 시작합니다.", Color.cyan);
        // hud.HideResult(); // (HUDController에 이 함수가 없으므로 주석 처리)

        this.gameObject.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}