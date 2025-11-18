using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// 플레이어별 점수 관리 시스템
/// </summary>
public class ScoreManager : MonoBehaviourPun
{
    public static ScoreManager Instance { get; private set; }

    // 플레이어별 점수 데이터
    [System.Serializable]
    public class PlayerScore
    {
        public int playerIndex;
        public string playerName;
        public int equipmentScore;  // 보호구 선택 점수
        public int workScore;       // 프레스 작업 점수
        public int safetyPenalty;   // 안전 감점
        public int totalScore;      // 총점

        public PlayerScore(int index, string name)
        {
            playerIndex = index;
            playerName = name;
            equipmentScore = 0;
            workScore = 0;
            safetyPenalty = 0;
            totalScore = 0;
        }

        public void CalculateTotal()
        {
            totalScore = equipmentScore + workScore + safetyPenalty;
        }
    }

    // 3명의 플레이어 점수
    public Dictionary<int, PlayerScore> playerScores = new Dictionary<int, PlayerScore>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeScores();
    }

    void InitializeScores()
    {
        // 3명의 플레이어 점수 초기화
        for (int i = 0; i < GameManager.MAX_PLAYERS; i++)
        {
            string playerName = $"Player {i + 1}";
            playerScores[i] = new PlayerScore(i, playerName);
        }

        Debug.Log("[ScoreManager] 점수 시스템 초기화 완료");
    }

    /// <summary>
    /// 보호구 선택 점수 추가
    /// </summary>
    public void AddEquipmentScore(int playerIndex, int score)
    {
        if (playerScores.ContainsKey(playerIndex))
        {
            playerScores[playerIndex].equipmentScore = score;
            playerScores[playerIndex].CalculateTotal();

            Debug.Log($"[ScoreManager] Player {playerIndex} 보호구 점수: {score}");

            // 마스터 클라이언트에게 동기화
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_SyncScore", RpcTarget.Others, playerIndex, score, 0, 0);
            }
            else
            {
                photonView.RPC("RPC_SyncScore", RpcTarget.MasterClient, playerIndex, score, 0, 0);
            }
        }
    }

    /// <summary>
    /// 프레스 작업 점수 추가
    /// </summary>
    public void AddWorkScore(int playerIndex, int score)
    {
        if (playerScores.ContainsKey(playerIndex))
        {
            playerScores[playerIndex].workScore = score;
            playerScores[playerIndex].CalculateTotal();

            Debug.Log($"[ScoreManager] Player {playerIndex} 작업 점수: {score}");

            // 동기화
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_SyncScore", RpcTarget.Others, playerIndex, 0, score, 0);
            }
            else
            {
                photonView.RPC("RPC_SyncScore", RpcTarget.MasterClient, playerIndex, 0, score, 0);
            }
        }
    }

    /// <summary>
    /// 안전 감점 추가
    /// </summary>
    public void AddSafetyPenalty(int playerIndex, int penalty)
    {
        if (playerScores.ContainsKey(playerIndex))
        {
            playerScores[playerIndex].safetyPenalty += penalty;
            playerScores[playerIndex].CalculateTotal();

            Debug.Log($"[ScoreManager] Player {playerIndex} 안전 감점: {penalty}");

            // 동기화
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_SyncScore", RpcTarget.Others, playerIndex, 0, 0, penalty);
            }
            else
            {
                photonView.RPC("RPC_SyncScore", RpcTarget.MasterClient, playerIndex, 0, 0, penalty);
            }
        }
    }

    [PunRPC]
    void RPC_SyncScore(int playerIndex, int equipmentScore, int workScore, int safetyPenalty)
    {
        if (!playerScores.ContainsKey(playerIndex)) return;

        if (equipmentScore != 0)
        {
            playerScores[playerIndex].equipmentScore = equipmentScore;
        }

        if (workScore != 0)
        {
            playerScores[playerIndex].workScore = workScore;
        }

        if (safetyPenalty != 0)
        {
            playerScores[playerIndex].safetyPenalty += safetyPenalty;
        }

        playerScores[playerIndex].CalculateTotal();

        Debug.Log($"[ScoreManager] Player {playerIndex} 점수 동기화 완료");
    }

    /// <summary>
    /// 플레이어 점수 가져오기
    /// </summary>
    public PlayerScore GetPlayerScore(int playerIndex)
    {
        if (playerScores.ContainsKey(playerIndex))
        {
            return playerScores[playerIndex];
        }
        return null;
    }

    /// <summary>
    /// 모든 플레이어 점수 정렬 (총점 내림차순)
    /// </summary>
    public List<PlayerScore> GetSortedScores()
    {
        List<PlayerScore> sortedScores = new List<PlayerScore>(playerScores.Values);
        sortedScores.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
        return sortedScores;
    }
}
