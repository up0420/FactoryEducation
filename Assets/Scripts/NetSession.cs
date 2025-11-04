using System;
using System.Collections.Generic;
using UnityEngine;

public enum NodeRole { Host, Client }

public class NetSession : MonoBehaviour {
    [Header("Role & IDs")]
    public NodeRole role = NodeRole.Host; // 한 대만 Host, 나머지 Client
    public string myId = "hmd-01";
    public string sessionCode = "000000";

    [Header("Refs")]
    public OscTransport transport;

    int _seq;
    float _hbTimer;
    const float HeartbeatInterval = 2f;
    readonly Dictionary<string, long> _lastSeen = new(); // id -> ts

    void Start() {
        if (transport == null) transport = GetComponent<OscTransport>();
        transport.OnRawMessage += HandleRaw;
        if (role == NodeRole.Client) SendJoin();
    }

    void Update() {
        _hbTimer += Time.deltaTime;
        if (_hbTimer >= HeartbeatInterval) {
            _hbTimer = 0;
            SendPing();
        }
    }

    void HandleRaw(string json, System.Net.IPEndPoint remote) {
        var msg = OscMessage.FromJson(json);
        if (msg == null || string.IsNullOrEmpty(msg.path)) return;
        _lastSeen[msg.id] = msg.ts;

        switch (msg.path) {
            case "/session/join":
                if (role == NodeRole.Host) {
                    // 간단 검증(코드 일치만)
                    if (msg.reason == sessionCode) SendReady(msg.id);
                }
                break;

            case "/session/ready":
                if (role == NodeRole.Host) {
                    Debug.Log($"Client ready: {msg.id}");
                    // 3명 모두 준비되었는지 체크(샘플)
                    if (AllReady()) BroadcastStart();
                }
                break;

            case "/session/start":
                Debug.Log("START received");
                // 게임 시작 훅
                GameSignals.SessionStart?.Invoke();
                break;

            case "/net/ping":
                // host는 pong으로 회신(선택)
                if (role == NodeRole.Host) SendPong(msg.id);
                break;

            case "/ppe/select":
                if (role == NodeRole.Host) {
                    // TODO: 정답 채점 로직 → confirm 브로드캐스트
                    int correct = UnityEngine.Random.Range(0,4); // 데모
                    int bonus = (correct==3)?20:(correct==2)?5:(correct==1)?0:-5;
                    SendPpeConfirm(msg.id, correct, bonus);
                    if (AllPpeDone()) BroadcastStart(); // 3명 완료 시 시작
                }
                break;

            case "/emergency/stop":
                // 즉시 공유(호스트/클라 모두)
                Broadcast(json);
                GameSignals.EmergencyStop?.Invoke(msg.id, msg.reason);
                break;

            case "/pose":
                // 포즈 갱신(렌더러/아바타에 반영)
                break;
        }
    }

    bool AllReady() {
        // 실제로는 세션 참가자 목록 관리 필요. 데모: 최근 10초 내 ping이 3개 이상
        int alive = 0;
        long now = OscMessage.NowMs();
        foreach (var kv in _lastSeen) if (now - kv.Value < 10000) alive++;
        return alive >= 3;
    }

    bool AllPpeDone() {
        // TODO: PPE done 집계(플래그 관리). 데모: AllReady와 동일 취급
        return AllReady();
    }

    void BroadcastStart() {
        var m = NewMsg("/session/start");
        Broadcast(m);
    }

    void SendJoin() {
        var m = NewMsg("/session/join");
        m.reason = sessionCode; // reason 필드에 세션코드 전송(간단화)
        Send(m);
    }

    void SendReady(string who) {
        var m = NewMsg("/session/ready");
        m.id = who; // 대상 표시(로그용). 브로드캐스트로 처리해도 됨
        Broadcast(m);
    }

    void SendPing() {
        var m = NewMsg("/net/ping");
        Send(m);
    }

    void SendPong(string toId) {
        var m = NewMsg("/net/pong");
        m.reason = toId;
        Send(m);
    }

    public void SendPose(Vector3 p, Quaternion q) {
        var m = NewMsg("/pose");
        m.px = p.x; m.py = p.y; m.pz = p.z;
        m.qx = q.x; m.qy = q.y; m.qz = q.z; m.qw = q.w;
        Send(m);
    }

    public void SendPpeSelect(string[] picks) {
        var m = NewMsg("/ppe/select");
        m.picks = picks;
        Send(m);
    }

    void SendPpeConfirm(string targetId, int correct, int bonus) {
        var m = NewMsg("/ppe/confirm");
        m.id = targetId;
        m.correctCount = correct;
        m.bonus = bonus;
        Broadcast(m);
    }

    // Helpers
    OscMessage NewMsg(string path) {
        return new OscMessage {
            path = path,
            id = myId,
            ts = OscMessage.NowMs(),
            seq = ++_seq
        };
    }

    void Send(OscMessage m) => transport.SendJson(OscMessage.ToJson(m));
    void Broadcast(OscMessage m) => transport.SendJson(OscMessage.ToJson(m));
    void Broadcast(string json) => transport.SendJson(json);
}
