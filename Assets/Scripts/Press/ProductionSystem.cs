using UnityEngine;
using TMPro;
using Photon.Pun;

/// <summary>
/// 제품 제작 시스템 (Phase 6)
/// 플레이어별 성공 카운트 추적 (0/3, 1/3, 2/3, 3/3)
/// </summary>
public class ProductionSystem : MonoBehaviourPun
{
    [Header("UI")]
    public TextMeshProUGUI successCountText;

    // 플레이어별 성공 카운트 (로컬 표시용)
    private int[] playerSuccessCounts = new int[GameManager.MAX_PLAYERS];

    void Start()
    {
        UpdateSuccessUI(0, 0);
    }

    /// <summary>
    /// 제품 완성 시 호출 (PressMachine에서 호출)
    /// </summary>
    public void OnProductComplete(int playerIndex)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[ProductionSystem] 제품 완성은 마스터 클라이언트만 처리합니다.");
            return;
        }

        Debug.Log($"[ProductionSystem] Player {playerIndex + 1} 제품 완성");

        // TurnManager에 성공 알림
        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.OnProductSuccess(playerIndex);
        }

        // 새로운 제품 소재 생성 (3회 미만일 경우)
        if (playerSuccessCounts[playerIndex] < 3)
        {
            ProductSpawner spawner = FindObjectOfType<ProductSpawner>();
            if (spawner != null)
            {
                spawner.SpawnProductMaterial(playerIndex);
            }
        }
    }

    /// <summary>
    /// 성공 카운트 UI 업데이트
    /// </summary>
    public void UpdateSuccessUI(int playerIndex, int count)
    {
        playerSuccessCounts[playerIndex] = count;

        // 현재 턴 플레이어의 성공 카운트 표시
        int currentPlayerIndex = TurnManager.Instance.currentPlayerTurn;
        if (successCountText != null)
        {
            successCountText.text = $"완성: {playerSuccessCounts[currentPlayerIndex]}/3";
        }

        Debug.Log($"[ProductionSystem] Player {playerIndex + 1} 성공 카운트: {count}/3");
    }

    /// <summary>
    /// 플레이어 성공 카운트 가져오기
    /// </summary>
    public int GetPlayerSuccessCount(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < GameManager.MAX_PLAYERS)
        {
            return playerSuccessCounts[playerIndex];
        }
        return 0;
    }
}
