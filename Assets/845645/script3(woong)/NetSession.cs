using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NodeRole { Host, Client }

public class NetSession : MonoBehaviour {
    [Header("Role & IDs")]
    public NodeRole role = NodeRole.Host; // 한 대만 Host, 나머지 Client
    public string myId = "hmd-01";
    public string sessionCode = "000000";

    [Header("Refs")]
    public OscTransport transport;
    public RemotePlayerManager remotePlayerManager;

    int _seq;
    float _hbTimer;
    const float HeartbeatInterval = 2f;

    // 개선된 참가자 관리 시스템
    readonly Dictionary<string, PlayerInfo> _players = new Dictionary<string, PlayerInfo>();

    void Start() {
        if (transport == null) transport = GetComponent<OscTransport>();
        if (remotePlayerManager == null) remotePlayerManager = GetComponent<RemotePlayerManager>();
        transport.OnRawMessage += HandleRaw;
        if (role == NodeRole.Client) SendJoin();
    }

    void Update() {
        _hbTimer += Time.deltaTime;
        if (_hbTimer >= HeartbeatInterval) {
            _hbTimer = 0;
            SendPing();
            // 정기적으로 끊긴 플레이어 정리
            CleanupDisconnectedPlayers();
        }
    }

    void HandleRaw(string json, System.Net.IPEndPoint remote) {
        var msg = OscMessage.FromJson(json);
        if (msg == null || string.IsNullOrEmpty(msg.path)) return;

        // 자기 자신의 메시지는 무시
        if (msg.id == myId) return;

        // 플레이어 정보 업데이트 또는 생성
        if (!_players.ContainsKey(msg.id)) {
            _players[msg.id] = new PlayerInfo(msg.id);
            Debug.Log($"New player joined: {msg.id}");
        }
        _players[msg.id].UpdateLastSeen();

        switch (msg.path) {
            case "/session/join":
                if (role == NodeRole.Host) {
                    // 간단 검증(코드 일치만)
                    if (msg.reason == sessionCode) {
                        _players[msg.id].isReady = false; // 아직 준비 안됨
                        SendReady(msg.id);
                    }
                }
                break;

            case "/session/ready":
                // 클라이언트도 다른 클라이언트의 ready 상태를 받을 수 있음
                if (_players.ContainsKey(msg.id)) {
                    _players[msg.id].isReady = true;
                    Debug.Log($"Client ready: {msg.id}");
                }

                if (role == NodeRole.Host) {
                    // 3명 모두 준비되었는지 체크
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

                    // PPE 선택 완료로 표시
                    if (_players.ContainsKey(msg.id)) {
                        _players[msg.id].ppeDone = true;
                        _players[msg.id].ppeCorrect = correct;
                        _players[msg.id].ppeBonus = bonus;
                    }

                    if (AllPpeDone()) BroadcastStart(); // 3명 완료 시 시작
                }
                break;

            case "/ppe/confirm":
                // 클라이언트가 자신의 PPE 결과를 받음
                if (_players.ContainsKey(msg.id)) {
                    _players[msg.id].ppeDone = true;
                    _players[msg.id].ppeCorrect = msg.correctCount;
                    _players[msg.id].ppeBonus = msg.bonus;
                    Debug.Log($"PPE Confirmed for {msg.id}: {msg.correctCount} correct, {msg.bonus} bonus");
                }

                // PPE 결과 이벤트 발생 (UI 업데이트용)
                GameSignals.PpeConfirmed?.Invoke(msg.id, msg.correctCount, msg.bonus);
                break;

            case "/emergency/stop":
                // 즉시 공유(호스트/클라 모두)
                if (role == NodeRole.Host) {
                    Broadcast(json);
                }
                GameSignals.EmergencyStop?.Invoke(msg.id, msg.reason);
                break;

            case "/pose":
                // 포즈 갱신(렌더러/아바타에 반영)
                Vector3 pos = new Vector3(msg.px, msg.py, msg.pz);
                Quaternion rot = new Quaternion(msg.qx, msg.qy, msg.qz, msg.qw);

                if (_players.ContainsKey(msg.id)) {
                    _players[msg.id].UpdatePose(pos, rot);
                }

                // 아바타 매니저에 업데이트
                if (remotePlayerManager != null) {
                    remotePlayerManager.UpdatePlayerPose(msg.id, pos, rot);
                }
                break;
        }
    }

    bool AllReady() {
        // 살아있는 플레이어 중 모두 Ready 상태인지 확인
        var alivePlayers = _players.Values.Where(p => p.IsAlive()).ToList();

        // Host 포함 최소 3명이 있어야 함
        if (alivePlayers.Count < 2) return false; // Host + 최소 2명의 Client

        // 모든 살아있는 플레이어가 Ready 상태여야 함
        return alivePlayers.All(p => p.isReady);
    }

    bool AllPpeDone() {
        // 살아있는 플레이어 중 모두 PPE 완료 상태인지 확인
        var alivePlayers = _players.Values.Where(p => p.IsAlive()).ToList();

        // Host 포함 최소 3명이 있어야 함
        if (alivePlayers.Count < 2) return false; // Host + 최소 2명의 Client

        // 모든 살아있는 플레이어가 PPE 완료 상태여야 함
        return alivePlayers.All(p => p.ppeDone);
    }

    /// <summary>
    /// 특정 플레이어 정보 가져오기
    /// </summary>
    public PlayerInfo GetPlayer(string playerId) {
        return _players.TryGetValue(playerId, out PlayerInfo player) ? player : null;
    }

    /// <summary>
    /// 살아있는 모든 플레이어 목록 가져오기
    /// </summary>
    public List<PlayerInfo> GetAlivePlayers() {
        return _players.Values.Where(p => p.IsAlive()).ToList();
    }

    /// <summary>
    /// 연결이 끊긴 플레이어 정리
    /// </summary>
    public void CleanupDisconnectedPlayers() {
        var disconnected = _players.Where(kvp => !kvp.Value.IsAlive()).Select(kvp => kvp.Key).ToList();
        foreach (var playerId in disconnected) {
            Debug.Log($"Player disconnected: {playerId}");
            if (remotePlayerManager != null) {
                remotePlayerManager.RemoveAvatar(playerId);
            }
            _players.Remove(playerId);
        }
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
