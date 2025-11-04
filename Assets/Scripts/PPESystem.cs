using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PPESystem : MonoBehaviour
{
    [Header("PPE Candidates (5 Buttons)")]
    public Button[] ppeButtons; // 헬멧, 보안경, 장갑, 안전화, 귀마개 순
    [Header("HUD Controller")]
    public HUDController hud;

    private HashSet<int> selected = new HashSet<int>();
    private readonly HashSet<int> correctSet = new HashSet<int> { 0, 1, 3 }; // 정답 예시
    private bool isLocked = false;
    private float selectStartTime;

    void Start()
    {
        for (int i = 0; i < ppeButtons.Length; i++)
        {
            int idx = i;
            ppeButtons[i].onClick.AddListener(() => OnSelect(idx));
        }

        selectStartTime = Time.time;
        hud.ShowToast("PPE 3개를 선택하세요", Color.cyan);
    }

    void OnSelect(int index)
    {
        if (isLocked) return;

        if (selected.Contains(index))
        {
            selected.Remove(index);
            hud.ShowToast($"선택 해제: {ppeButtons[index].name}", Color.gray);
        }
        else
        {
            if (selected.Count >= 3)
            {
                hud.ShowToast("이미 3개 선택 완료", Color.yellow);
                return;
            }

            selected.Add(index);
            hud.ShowToast($"선택: {ppeButtons[index].name}", Color.white);

            if (selected.Count == 3)
            {
                ConfirmSelection();
            }
        }

        UpdateButtonVisuals();
    }

    void ConfirmSelection()
    {
        isLocked = true;
        float elapsed = Time.time - selectStartTime;

        int correct = 0;
        foreach (var idx in selected)
        {
            if (correctSet.Contains(idx)) correct++;
        }

        int score = (correct == 3) ? 20 :
                    (correct == 2) ? 5 :
                    (correct == 1) ? 0 : -5;

        // 시간 보너스 예시: 30초 이내면 +5
        if (elapsed < 30f) score += 5;

        hud.ShowResult(correct, score);

        // (현재는 로컬용) 네트워크로 전송할 자리
        Debug.Log($"[PPE] 확정: {correct}/3 정답, 점수 {score}, 소요 {elapsed:F1}s");

        // 예: NetSession.Instance.Send("/ppe/confirm", payload);
    }

    void UpdateButtonVisuals()
    {
        for (int i = 0; i < ppeButtons.Length; i++)
        {
            var colors = ppeButtons[i].colors;
            colors.normalColor = selected.Contains(i) ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
            ppeButtons[i].colors = colors;
        }
    }
    // HUDController가 아직 없다면 임시 클래스 생성
    public class HUDController : MonoBehaviour
    {
        public void ShowToast(string msg, Color color)
        {
            Debug.Log($"[HUD] {msg}");
        }

        public void ShowResult(int correct, int score)
        {
            Debug.Log($"정답 {correct}/3, 점수 {score}");
        }
    }
}
