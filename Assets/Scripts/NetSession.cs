using UnityEngine;
using System.Collections.Generic;

public class NetSession : MonoBehaviour
{
    public Dictionary<string, PlayerSync> players = new Dictionary<string, PlayerSync>();

    // 송신 (자기 위치 전송)
    public void SendPose(string id, Vector3 pos, Quaternion rot)
    {
        // TODO: 실제로는 UDP/OSC 전송
        foreach (var kv in players)
        {
            if (kv.Key != id)
                kv.Value.OnPoseUpdate(pos, rot); // 로컬 테스트용
        }
    }

    // 수신 (원격 위치 갱신)
    public void ReceivePose(string id, Vector3 pos, Quaternion rot)
    {
        if (players.TryGetValue(id, out var player))
            player.OnPoseUpdate(pos, rot);
    }

    // 플레이어 등록
    public void RegisterPlayer(string id, PlayerSync player)
    {
        if (!players.ContainsKey(id))
            players.Add(id, player);
    }
}
