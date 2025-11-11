using UnityEngine;
using System;
using System.Collections.Generic;

public enum NodeRole { Host, Client }

public class NetSession : MonoBehaviour
{
    [Header("Role & IDs")]
    public NodeRole role = NodeRole.Host; // 한 대만 Host
    public string myId = "hmd-01";
    public string sessionCode = "000000";

    [Header("Refs")]
    public OscTransport transport;

    // === 내부 상태 ===
    int _seq;
    float _hbTimer;
    const float HeartbeatInterval = 2f;
    readonly Dictionary<string, long> _lastSeen = new(); // id -> ts

    // === T0 동기화 (PressFSM이 사용) ===
    private float sessionStartTime = -1f;
    public float SessionTime => sessionStartTime < 0 ? 0f : Time.time - sessionStartTime;

    void Start()
    {
        if (transport == null) transport = GetComponent<OscTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetSession] OscTransport 컴포넌트가 없습니다!");
            return;
        }

        transport.OnRawMessage += HandleRaw;
        if (role == NodeRole.Client) SendJoin();
    }

    void Update()
    {
        _hbTimer += Time.deltaTime;
        if (_hbTimer >= HeartbeatInterval)
        {
            _hbTimer = 0;
            SendPing();
        }
    }

    void HandleRaw(string json, System.Net.IPEndPoint remote)
    {
        var msg = OscMessage.FromJson(json);
        if (msg == null || string.IsNullOrEmpty(msg.path)) return;

        _lastSeen[msg.id] = msg.ts;

        switch (msg.path)
        {
            case "/session/join":
                if (role == NodeRole.Host && msg.reason == sessionCode)
                {
                    SendReady(msg.id);
                }
                break;

            case "/session/ready":
                if (role == NodeRole.Host)
                {
                    Debug.Log($"[NetSession] Client ready: {msg.id}");
                    if (AllReady()) BroadcastStart();
                }
                break;

            case "/session/start":
                sessionStartTime = Time.time; // T0 설정
                Debug.Log($"[NetSession] 게임 시작 수신! T0 = {sessionStartTime:F2}");
                GameSignals.SessionStart?.Invoke();
                break;

            case "/net/ping":
                if (role == NodeRole.Host) SendPong(msg.id);
                break;

            case "/ppe/select":
                if (role == NodeRole.Host)
                {
                    int correct = UnityEngine.Random.Range(0, 4); // 실제 채점 로직으로 교체
                    int bonus = correct == 3 ? 20 : correct == 2 ? 5 : correct == 1 ? 0 : -5;
                    SendPpeConfirm(msg.id, correct, bonus);
                    if (AllPpeDone()) BroadcastStart();
                }
                break;

            case "/emergency/stop":
                Broadcast(json); // 즉시 모든 클라에 전파
                GameSignals.EmergencyStop?.Invoke(msg.id, msg.reason);
                Debug.Log($"[NetSession] 비상정지 브로드캐스트: {msg.id}");
                break;

            case "/pose":
                // 포즈 처리 (아바타 동기화)
                break;
        }
    }

    bool AllReady()
    {
        int alive = 0;
        long now = OscMessage.NowMs();
        foreach (var kv in _lastSeen)
            if (now - kv.Value < 10000) alive++;
        return alive >= 3;
    }

    bool AllPpeDone() => AllReady(); // 실제 PPE 집계 로직으로 교체 예정

    void BroadcastStart()
    {
        sessionStartTime = Time.time;
        var m = NewMsg("/session/start");
        Broadcast(m);
        Debug.Log($"[NetSession] 게임 시작 브로드캐스트! T0 = {sessionStartTime:F2}");
    }

    void SendJoin()
    {
        var m = NewMsg("/session/join");
        m.reason = sessionCode;
        Send(m);
    }

    void SendReady(string who)
    {
        var m = NewMsg("/session/ready");
        m.id = who;
        Broadcast(m);
    }

    void SendPing()
    {
        var m = NewMsg("/net/ping");
        Send(m);
    }

    void SendPong(string toId)
    {
        var m = NewMsg("/net/pong");
        m.reason = toId;
        Send(m);
    }

    public void SendPose(Vector3 p, Quaternion q)
    {
        var m = NewMsg("/pose");
        m.px = p.x; m.py = p.y; m.pz = p.z;
        m.qx = q.x; m.qy = q.y; m.qz = q.z; m.qw = q.w;
        Send(m);
    }

    public void SendPpeSelect(string[] picks)
    {
        var m = NewMsg("/ppe/select");
        m.picks = picks;
        Send(m);
    }

    void SendPpeConfirm(string targetId, int correct, int bonus)
    {
        var m = NewMsg("/ppe/confirm");
        m.id = targetId;
        m.correctCount = correct;
        m.bonus = bonus;
        Broadcast(m);
    }

    // === 헬퍼 ===
    OscMessage NewMsg(string path)
    {
        return new OscMessage
        {
            path = path,
            id = myId,
            ts = OscMessage.NowMs(),
            seq = ++_seq
        };
    }

    void Send(OscMessage m) => transport.SendJson(OscMessage.ToJson(m));
    void Broadcast(OscMessage m) => transport.SendJson(OscMessage.ToJson(m));
    void Broadcast(string json) => transport.SendJson(json);

    // === 테스트용 ===
    [ContextMenu("테스트: 게임 강제 시작")]
    public void TEST_START_GAME()
    {
        if (role == NodeRole.Host) BroadcastStart();
    }

    [ContextMenu("테스트: 비상정지 브로드캐스트")]
    public void TEST_EMERGENCY()
    {
        var m = NewMsg("/emergency/stop");
        m.reason = "테스트";
        Broadcast(m);
    }
}