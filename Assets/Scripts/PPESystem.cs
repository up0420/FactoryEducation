using UnityEngine;
using UnityEngine.UI; // Button을 사용하기 위해 필요
using System.Collections.Generic;

// 이 스크립트는 OscTransport와 HUDController가 씬에 있다고 가정
[RequireComponent(typeof(OscTransport))]
[RequireComponent(typeof(HUDController))]
public class PPESystem : MonoBehaviour
{
    [Header("PPE Candidates (5 Buttons)")]
    // Inspector에서 5개의 UI 버튼을 순서대로 연결
    public Button[] ppeButtons;

    [Header("Scoring Rules")]
    public int correctBonus = 20;  // 3/3 정답 보너스
    public int timeBonus = 5;      // 시간 보너스
    public float timeLimit = 30.0f; // 시간 보너스 제한 (30초)

    // --- 내부 변수 ---
    private OscTransport transport; // 네트워크 전송 담당
    private HUDController hud;      // UI 피드백 담당

    private HashSet<int> selectedIndices = new HashSet<int>();
    // PPE 정답 세트 (0=헬멧, 1=보안경, 3=안전화) - 프로젝트 계획서 기준 예시
    private readonly HashSet<int> correctSet = new HashSet<int> { 0, 1, 3 };

    private bool isLocked = false;  // 선택이 확정되었는지
    private float selectStartTime;  // 선택 시작 시간

    void Start()
    {
        // 컴포넌트들을 가져옴
        transport = GetComponent<OscTransport>();
        hud = GetComponent<HUDController>();

        // 5개의 버튼에 각각 클릭 이벤트를 연결
        for (int i = 0; i < ppeButtons.Length; i++)
        {
            int buttonIndex = i; // (중요) 람다에서 클로저 문제를 피하기 위해 인덱스를 복사
            ppeButtons[buttonIndex].onClick.AddListener(() => OnSelectButton(buttonIndex));
        }

        // 선택 시작
        selectStartTime = Time.time;
        hud.ShowToast("훈련 시작: 보호구 3개를 선택하세요.", Color.cyan);
    }

    void OnSelectButton(int index)
    {
        if (isLocked) return; // 선택이 확정되면 아무것도 하지 않음

        if (selectedIndices.Contains(index))
        {
            // 이미 선택된 것을 다시 클릭: 선택 해제
            selectedIndices.Remove(index);
            hud.ShowToast($"선택 해제: {ppeButtons[index].name}", Color.gray);
        }
        else
        {
            // 3개 미만일 때만 새로 선택 가능
            if (selectedIndices.Count < 3)
            {
                selectedIndices.Add(index);
                hud.ShowToast($"선택: {ppeButtons[index].name}", Color.white);
            }
            else
            {
                hud.ShowToast("최대 3개까지 선택할 수 있습니다.", Color.yellow);
            }
        }

        UpdateButtonVisuals(); // 버튼 색상 업데이트

        // 3개를 모두 선택했다면 즉시 확정
        if (selectedIndices.Count == 3)
        {
            ConfirmSelection();
        }
    }

    void UpdateButtonVisuals()
    {
        // (간단한 예시) 선택된 버튼의 색상 변경
        for (int i = 0; i < ppeButtons.Length; i++)
        {
            var colors = ppeButtons[i].colors;
            if (selectedIndices.Contains(i))
                colors.normalColor = Color.green; // 선택됨
            else
                colors.normalColor = Color.white; // 선택 안 됨

            ppeButtons[i].colors = colors;
        }
    }

    void ConfirmSelection()
    {
        isLocked = true; // 선택 잠금
        float elapsed = Time.time - selectStartTime;

        // 1. 채점
        int correctCount = 0;
        foreach (int index in selectedIndices)
        {
            if (correctSet.Contains(index))
                correctCount++;
        }

        int score = 0;
        if (correctCount == 3) score += correctBonus;
        else if (correctCount == 2) score += 5;
        else if (correctCount == 0) score -= 5;

        // 2. 시간 보너스 채점
        if (elapsed < timeLimit)
        {
            score += timeBonus;
        }

        // 3. UI 피드백
        hud.ShowResult(correctCount, score);

        // 4. 네트워크로 결과 전송 (OscTransport 사용)
        // 전송할 데이터를 JSON 형태로 만듦 (간단한 예시)
        string jsonPayload = $"{{ \"type\": \"ppeResult\", \"correct\": {correctCount}, \"score\": {score} }}";

        // OscTransport의 SendJson 함수로 전송
        transport.SendJson(jsonPayload);

        Debug.Log($"[PPESystem] 결과 전송: {jsonPayload}");
    }
}