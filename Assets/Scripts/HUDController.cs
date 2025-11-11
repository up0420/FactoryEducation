using UnityEngine;
using UnityEngine.UI; // Unity UI (Text, Image 등)를 사용하기 위해 필요
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    public Text toastText;     // 토스트 메시지를 표시할 UI 텍스트
    public Text resultText;    // 최종 점수 결과를 표시할 UI 텍스트
    public float toastDuration = 3.0f; // 메시지가 떠 있는 시간

    void Start()
    {
        // 시작할 때 텍스트를 숨김
        if (toastText) toastText.gameObject.SetActive(false);
        if (resultText) resultText.gameObject.SetActive(false);
    }

    // 간단한 메시지를 잠시 띄우는 함수
    public void ShowToast(string message, Color color)
    {
        if (!toastText) return; // 텍스트가 연결 안 됐으면 중단

        toastText.text = message;
        toastText.color = color;
        StopAllCoroutines(); // 이전에 떠 있던 코루틴이 있다면 중지
        StartCoroutine(ToastFadeRoutine());
    }

    private IEnumerator ToastFadeRoutine()
    {
        toastText.gameObject.SetActive(true);
        yield return new WaitForSeconds(toastDuration);
        toastText.gameObject.SetActive(false);
    }

    // 최종 결과 점수판을 띄우는 함수
    public void ShowResult(int correctCount, int totalScore)
    {
        if (!resultText) return; // 텍스트가 연결 안 됐으면 중단

        resultText.gameObject.SetActive(true);
        resultText.text = $"[훈련 결과]\n정답: {correctCount}/3\n획득 점수: {totalScore}점";
    }
}