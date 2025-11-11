using UnityEngine;
using System.Collections.Generic;
using System.Net; // IPEndPoint를 사용하기 위해 필요

// OscTransport 스크립트가 반드시 같이 있어야 함
[RequireComponent(typeof(OscTransport))]
public class NetSession1 : MonoBehaviour
{
    // 씬에 있는 모든 플레이어 아바타(PlayerSync)의 목록
    // Key: 플레이어 ID (예: "Player_1"), Value: 해당 플레이어의 PlayerSync 스크립트
    public Dictionary<string, PlayerSync> playerList = new Dictionary<string, PlayerSync>();

    private OscTransport transport; // 네트워크 통신 담당

    void Awake()
    {
        // OscTransport 컴포넌트를 가져옴
        transport = GetComponent<OscTransport>();

        // OscTransport가 메시지를 받으면(OnRawMessage) -> NetSession이 처리(OnMessageReceived)
        transport.OnRawMessage += OnMessageReceived;
    }

    // OscTransport가 원시 메시지(JSON)를 받았을 때 호출됨
    private void OnMessageReceived(string jsonMessage, IPEndPoint remote)
    {
        // 이 곳에서 JSON 메시지를 분석(파싱)해서
        // "이건 위치 정보다", "이건 PPE 점수다" 등을 판단하고
        // 올바른 플레이어에게 전달해야 합니다.

        // (간단한 예시) JSON을 파싱해서 id와 pos, rot를 찾았다고 가정
        // if (messageType == "/pose")
        // {
        //     string id = "Player_2"; 
        //     Vector3 pos = new Vector3(1, 0, 0);
        //     Quaternion rot = Quaternion.identity;
        //     
        //     // 해당 플레이어를 찾아서 위치 갱신
        //     if (playerList.TryGetValue(id, out PlayerSync player))
        //     {
        //         player.OnPoseUpdate(pos, rot);
        //     }
        // }
    }

    // PlayerSync 스크립트가 "내 위치/회전 정보"를 보낼 때 호출
    public void SendPose(Vector3 pos, Quaternion rot)
    {
        // 이 정보를 JSON으로 만들어서 OscTransport로 전송
        // 예: string json = $"{{ \"type\": \"/pose\", \"pos\": ... }}";
        // transport.SendJson(json);

        // (지금은 로컬 테스트용)
        // Debug.Log($"Sending Pose: {pos}");
    }

    // PlayerSync 스크립트가 씬에 나타났을 때 자신을 등록하기 위해 호출
    public void RegisterPlayer(string id, PlayerSync player)
    {
        if (!playerList.ContainsKey(id))
        {
            playerList.Add(id, player);
            Debug.Log($"[NetSession] Player {id}가 등록되었습니다.");
        }
    }
}