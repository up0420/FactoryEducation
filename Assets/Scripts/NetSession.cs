using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NodeRole { Host, Client }

public class NetSession : MonoBehaviour
{
    [Header("Role & IDs")]
    public NodeRole role = NodeRole.Host; // 한 대만 Host, 나머지 Client
    public string myId = "hmd-01";
    public string sessionCode = "000000";

    [Header("Refs")]
    // 이 클래스가 있다고 가정
    public OscTransport transport;
    public RemotePlayerManager remotePlayerManager;

    int _seq;
    float _hbTimer;
    const float HeartbeatInterval = 2f;

    // 개선된 참가자 관리 시스템
    readonly Dictionary<string, PlayerInfo> _players = new Dictionary<string, PlayerInfo>();

    // 호스트 전용: 준비 완료 상태를 저장
    readonly HashSet<string> _readyClients = new();
    // 호스트 전용: PPE 완료 상태를 추적
    readonly HashSet<string> _ppeDoneClients = new();

    void Start()
    {
        if (transport == null) transport = GetComponent<OscTransport>();
        if (remotePlayerManager == null) remotePlayerManager = GetComponent<RemotePlayerManager>();
        transport.OnRawMessage += HandleRaw;

        // [통합 로직 추가] EmergencyStop이 로컬 발동 시 네트워크에 전송하도록 구독
        EmergencyStop.OnEmergencyStopActivatedLocally += SendEmergencyStop;

        if (role == NodeRole.Client) SendJoin();
    }

    void OnDestroy()
    {
        // [통합 로직 추가] 구독 해제
        EmergencyStop.OnEmergencyStopActivatedLocally -= SendEmergencyStop;
        if (transport != null) transport.OnRawMessage -= HandleRaw;
    }

    void Update()
    {
        _hbTimer += Time.deltaTime;
        if (_hbTimer >= HeartbeatInterval)
        {
            _hbTimer = 0;
            SendPing();
            // 정기적으로 끊긴 플레이어 정리
            CleanupDisconnectedPlayers();
        }
        // 호스트는 일정 시간 이상 활동 없는 클라이언트 목록을 정리할 수도 있음
        // if (role == NodeRole.Host) CleanupInactiveClients();
    }

    /// <summary>
    /// 로컬에서 비상 정지 발동 시, 네트워크로 브로드캐스트합니다.
    /// </summary>
    /// <param name="sourceId">비상 정지를 건 플레이어 ID</param>
    public void SendEmergencyStop(string sourceId)
    {
        var m = NewMsg("/emergency/stop");
        // sourceId는 NewMsg에서 이미 myId로 설정되므로 reason에 로깅용으로 남김
        m.reason = "LocalTriggered";
        Broadcast(m);
    }


    void HandleRaw(string json, System.Net.IPEndPoint remote)
    {
        // [개선] JSON 파싱 실패 시 예외 처리 추가
        OscMessage msg = null;
        try
        {
            msg = OscMessage.FromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Invalid OSC message JSON: {json}. Error: {e.Message}");
            return;
        }

        if (msg == null || string.IsNullOrEmpty(msg.path)) return;


        // ping/pong 메시지를 제외한 모든 메시지에 대해 마지막 활동 시간 업데이트
        if (msg.path != "/net/ping" && msg.path != "/net/pong")
        {
            _lastSeen[msg.id] = msg.ts;
        }

        switch (msg.path)
        {
            case "/session/join":
                if (role == NodeRole.Host)
                {
                    // 간단 검증(코드 일치만)

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
                else if (role == NodeRole.Client)
                {
                    Debug.Log("Session Ready signal received.");
                }
                break;

            case "/session/start":
                Debug.Log("START received - Invoking GameSignals.SessionStart");
                // [통합 로직] 게임 시작 훅 -> PressFSM.StartPressSequence()를 호출하도록 연결됨
                GameSignals.SessionStart?.Invoke();
                break;

            case "/net/ping":
                // 호스트는 pong으로 회신(선택). 클라이언트의 생존 시간은 여기서 업데이트됨.
                if (role == NodeRole.Host)
                {
                    SendPong(msg.id);
                }
                break;

            case "/net/pong":
                // 클라이언트는 Host의 생존 시간 업데이트
                if (role == NodeRole.Client)
                {
                    _lastSeen[msg.id] = msg.ts;
                }
                break;

            case "/ppe/select":
                if (role == NodeRole.Host)
                {
                    // [통합 로직] Interlocks 업데이트 (msg.picks[0]=gate, [1]=hand, [2]=dual 가정)
                    if (msg.picks != null && msg.picks.Length >= 3)
                    {
                        Interlocks.UpdatePlayer(msg.id, int.Parse(msg.picks[0]), int.Parse(msg.picks[1]), int.Parse(msg.picks[2]));
                    }

                    int correct = UnityEngine.Random.Range(0, 4); // 데모 채점
                    int bonus = (correct == 3) ? 20 : (correct == 2) ? 5 : (correct == 1) ? 0 : -5;
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

    void BroadcastStart()
    {
        Debug.Log("Host broadcasts /session/start");
        var m = NewMsg("/session/start");
        Broadcast(m);
    }

    void SendJoin()
    {
        var m = NewMsg("/session/join");
        m.reason = sessionCode; // reason 필드에 세션코드 전송(간단화)
        Send(m);
    }

    void SendReady(string who)
    {
        var m = NewMsg("/session/ready");
        // m.id = who; // 이 메시지는 브로드캐스트로 처리
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
        // m.reason = toId; 
        Send(m);
    }

    public void SendPose(Vector3 p, Quaternion q)
    {
        var m = NewMsg("/pose");
        m.px = p.x; m.py = p.y; m.pz = p.z;
        m.qx = q.x; m.qy = q.y; m.qz = q.z; m.qw = q.w;
        Send(m);
    }

    /// <summary>
    /// 클라이언트가 자신의 인터록 상태(센서)를 호스트에게 전송합니다.
    /// </summary>
    public void SendInterlockStatus(int gate, int hand, int dual)
    {
        var m = NewMsg("/interlock/status");
        m.ints = new int[] { gate, hand, dual };
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

    // Helpers
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
}

/// <summary>
/// 네트워크 메시지 구조체 (간단화를 위해 JSON 직렬화 가능하도록 public 필드 사용)
/// </summary>
[System.Serializable]
public class OscMessage
{
    public string path;
    public string id; // 발신자 ID
    public long ts; // 타임스탬프 (ms)
    public int seq; // 순서 번호

    // 일반 데이터 필드
    public string reason; // 범용 문자열 데이터 (예: sessionCode, emergency stop 이유)

    // 포즈 데이터
    public float px, py, pz;
    public float qx, qy, qz, qw;

    // PPE 데이터
    public string[] picks; // 선택 항목 (예: "1", "0", "1")
    public int correctCount; // 호스트->클라이언트 (채점 결과)
    public int bonus; // 호스트->클라이언트 (보너스 점수)

    // 기타 데이터
    public int[] ints; // 범용 정수 배열 (예: Interlocks 상태)

    public static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static string ToJson(OscMessage m) => JsonUtility.ToJson(m);
    public static OscMessage FromJson(string json)
    {
        try
        {
            return JsonUtility.FromJson<OscMessage>(json);
        }
        catch
        {
            // JsonUtility.FromJson<T>은 디시리얼라이즈 실패 시 null을 반환하지 않고 예외 발생
            return null;
        }
    }
}

// 이 클래스는 Unity 프로젝트에서 사용 가능한 실제 OscTransport 컴포넌트를 가정합니다.
// Unity 밖에서는 이 클래스가 없으므로 주석 처리합니다.
/*
public class OscTransport : MonoBehaviour
{
    public event Action<string, System.Net.IPEndPoint> OnRawMessage;
    
    public void SendJson(string json)
    {
        // 실제 OSC/UDP 전송 로직
        Debug.Log($"Sending: {json}");
    }
}
*/