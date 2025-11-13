using UnityEngine;
using System.Collections.Generic;
using System.Net; // IPEndPoint�� ����ϱ� ���� �ʿ�

// OscTransport ��ũ��Ʈ�� �ݵ�� ���� �־�� ��
[RequireComponent(typeof(OscTransport))]
public class NetSession1 : MonoBehaviour
{

    // ���� �ִ� ��� �÷��̾� �ƹ�Ÿ(PlayerSync)�� ���
    // Key: �÷��̾� ID (��: "Player_1"), Value: �ش� �÷��̾��� PlayerSync ��ũ��Ʈ
    public Dictionary<string, PlayerSync> playerList = new Dictionary<string, PlayerSync>();

    private OscTransport transport; // ��Ʈ��ũ ��� ���

    void Awake()
    {
        // OscTransport ������Ʈ�� ������
        transport = GetComponent<OscTransport>();

        // OscTransport�� �޽����� ������(OnRawMessage) -> NetSession�� ó��(OnMessageReceived)
        transport.OnRawMessage += OnMessageReceived;
    }
    [Header("Session Role")]
    public NodeRole role = NodeRole.Host; // �⺻���� Host�� ���� (�׽�Ʈ��)
    // ���� ������� ����

    
    // OscTransport�� ���� �޽���(JSON)�� �޾��� �� ȣ���
    private void OnMessageReceived(string jsonMessage, IPEndPoint remote)
    {
        // �� ������ JSON �޽����� �м�(�Ľ�)�ؼ�
        // "�̰� ��ġ ������", "�̰� PPE ������" ���� �Ǵ��ϰ�
        // �ùٸ� �÷��̾�� �����ؾ� �մϴ�.

        // (������ ����) JSON�� �Ľ��ؼ� id�� pos, rot�� ã�Ҵٰ� ����
        // if (messageType == "/pose")
        // {
        //     string id = "Player_2"; 
        //     Vector3 pos = new Vector3(1, 0, 0);
        //     Quaternion rot = Quaternion.identity;
        //     
        //     // �ش� �÷��̾ ã�Ƽ� ��ġ ����
        //     if (playerList.TryGetValue(id, out PlayerSync player))
        //     {
        //         player.OnPoseUpdate(pos, rot);
        //     }
        // }
    }

    // PlayerSync ��ũ��Ʈ�� "�� ��ġ/ȸ�� ����"�� ���� �� ȣ��
    public void SendPose(Vector3 pos, Quaternion rot)
    {
        // �� ������ JSON���� ���� OscTransport�� ����
        // ��: string json = $"{{ \"type\": \"/pose\", \"pos\": ... }}";
        // transport.SendJson(json);

        // (������ ���� �׽�Ʈ��)
        // Debug.Log($"Sending Pose: {pos}");
    }

    // PlayerSync ��ũ��Ʈ�� ���� ��Ÿ���� �� �ڽ��� ����ϱ� ���� ȣ��
    public void RegisterPlayer(string id, PlayerSync player)
    {
        if (!playerList.ContainsKey(id))
        {
            playerList.Add(id, player);
            Debug.Log($"[NetSession] Player {id}�� ��ϵǾ����ϴ�.");
        }
    }
}