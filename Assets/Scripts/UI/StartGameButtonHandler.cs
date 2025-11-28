using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 버튼 클릭 이벤트 핸들러
/// StartGameButton에 연결하여 GameManager.StartGame()을 호출합니다
/// </summary>
public class StartGameButtonHandler : MonoBehaviour
{
    [Header("Button Reference")]
    public Button startButton;

    void Start()
    {
        // 버튼이 연결되지 않았으면 자동으로 찾기
        if (startButton == null)
        {
            startButton = GetComponent<Button>();
        }

        // 버튼 클릭 이벤트 연결
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            Debug.Log("[StartGameButtonHandler] 게임 시작 버튼 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError("[StartGameButtonHandler] Button 컴포넌트를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 게임 시작 버튼 클릭 시 호출되는 메서드 (Public으로 Inspector에서도 연결 가능)
    /// </summary>
    public void OnStartButtonClicked()
    {
        Debug.Log("[StartGameButtonHandler] 게임 시작 버튼 클릭됨!");

        // GameManager의 StartGame() 호출
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("[StartGameButtonHandler] GameManager.Instance를 찾을 수 없습니다!");
        }
    }

    void OnDestroy()
    {
        // 메모리 누수 방지: 이벤트 리스너 제거
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }
}
