using UnityEngine;
using Photon.Pun;

/// <summary>
/// 제품 소재 생성 시스템
/// 플레이어 옆에 제품 소재를 생성 (최대 3회)
/// </summary>
public class ProductSpawner : MonoBehaviourPun
{
    [Header("Product Material Prefab")]
    public GameObject productMaterialPrefab;

    [Header("Spawn Positions")]
    public Transform[] spawnPositions; // 3개 플레이어별 생성 위치

    [Header("Spawn Offset")]
    public Vector3 spawnOffset = new Vector3(1.0f, 1.0f, 0.5f);

    // 플레이어별 생성 횟수
    private int[] spawnCounts = new int[GameManager.MAX_PLAYERS];

    void Start()
    {
        // 초기화
        for (int i = 0; i < GameManager.MAX_PLAYERS; i++)
        {
            spawnCounts[i] = 0;
        }
    }

    /// <summary>
    /// 특정 플레이어를 위한 제품 소재 생성
    /// </summary>
    public void SpawnProductMaterial(int playerIndex)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[ProductSpawner] 제품 소재 생성은 마스터 클라이언트만 가능합니다.");
            return;
        }

        if (playerIndex < 0 || playerIndex >= GameManager.MAX_PLAYERS)
        {
            Debug.LogError($"[ProductSpawner] 잘못된 플레이어 인덱스: {playerIndex}");
            return;
        }

        if (spawnCounts[playerIndex] >= 3)
        {
            Debug.LogWarning($"[ProductSpawner] Player {playerIndex + 1}은 이미 3회 생성 완료");
            return;
        }

        // 생성 위치 결정
        Vector3 spawnPos = GetSpawnPosition(playerIndex);

        // 제품 소재 생성 (Photon Network)
        GameObject productMaterial = PhotonNetwork.Instantiate(
            productMaterialPrefab.name,
            spawnPos,
            Quaternion.identity
        );

        // 태그 설정
        productMaterial.tag = "ProductMaterial";

        spawnCounts[playerIndex]++;

        Debug.Log($"[ProductSpawner] Player {playerIndex + 1} 제품 소재 생성 ({spawnCounts[playerIndex]}/3)");
    }

    /// <summary>
    /// 플레이어별 생성 위치 가져오기
    /// </summary>
    Vector3 GetSpawnPosition(int playerIndex)
    {
        // spawnPositions가 설정된 경우 사용
        if (spawnPositions != null && spawnPositions.Length > playerIndex && spawnPositions[playerIndex] != null)
        {
            return spawnPositions[playerIndex].position;
        }

        // 기본 위치: 플레이어 위치 기준
        VRPlayerController player = GetPlayerByIndex(playerIndex);
        if (player != null)
        {
            return player.transform.position + spawnOffset;
        }

        // 폴백: 원점 기준
        return Vector3.zero + new Vector3(playerIndex * 3.0f, 1.0f, 0f);
    }

    /// <summary>
    /// 플레이어 인덱스로 VRPlayerController 가져오기
    /// </summary>
    VRPlayerController GetPlayerByIndex(int playerIndex)
    {
        VRPlayerController[] players = FindObjectsOfType<VRPlayerController>();

        foreach (VRPlayerController player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null)
            {
                // Photon Player의 ActorNumber를 기준으로 인덱스 매칭
                int pvPlayerIndex = pv.Owner.ActorNumber - 1; // ActorNumber는 1부터 시작
                if (pvPlayerIndex == playerIndex)
                {
                    return player;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 플레이어의 생성 횟수 가져오기
    /// </summary>
    public int GetSpawnCount(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < GameManager.MAX_PLAYERS)
        {
            return spawnCounts[playerIndex];
        }
        return 0;
    }

    /// <summary>
    /// 생성 횟수 초기화 (디버그용)
    /// </summary>
    public void ResetSpawnCounts()
    {
        for (int i = 0; i < GameManager.MAX_PLAYERS; i++)
        {
            spawnCounts[i] = 0;
        }

        Debug.Log("[ProductSpawner] 생성 횟수 초기화");
    }
}
