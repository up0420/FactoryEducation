using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class OscTransport : MonoBehaviour {
    [Header("UDP Ports")]
    public int listenPort = 9000;   // 내 수신 포트
    public int sendPort = 9000;     // 상대 수신 포트(상대가 9000 수신한다고 가정)

    [Header("Addresses")]
    public string hostIp = "192.168.0.10"; // 호스트 IP (호스트 자신은 무시)
    public bool broadcast = false;         // true면 브로드캐스트(255.255.255.255)

    UdpClient _listener;
    UdpClient _sender;
    IPEndPoint _listenEp;
    IPEndPoint _sendEp;
    Thread _rxThread;
    volatile bool _running;

    // 이벤트: 수신 메시지를 상위(NetSession 등)로 전달
    public Action<string, IPEndPoint> OnRawMessage;

    void Awake() {
        _listenEp = new IPEndPoint(IPAddress.Any, listenPort);
        _listener = new UdpClient(_listenEp);
        _sender = new UdpClient();
        _sender.EnableBroadcast = true;
        _sendEp = broadcast
            ? new IPEndPoint(IPAddress.Broadcast, sendPort)
            : new IPEndPoint(IPAddress.Parse(hostIp), sendPort);
    }

    void OnEnable() {
        _running = true;
        _rxThread = new Thread(ReceiveLoop) { IsBackground = true };
        _rxThread.Start();
    }

    void OnDisable() {
        _running = false;
        try { _listener?.Close(); } catch {}
        try { _sender?.Close(); } catch {}
        if (_rxThread != null && _rxThread.IsAlive) _rxThread.Join(100);
    }

    void ReceiveLoop() {
        while (_running) {
            try {
                var remote = new IPEndPoint(IPAddress.Any, 0);
                var data = _listener.Receive(ref remote);
                var s = Encoding.UTF8.GetString(data);
                OnRawMessage?.Invoke(s, remote);
            } catch (Exception) { /* socket closed or transient */ }
        }
    }

    public void SendJson(string json) {
        var bytes = Encoding.UTF8.GetBytes(json);
        _sender.Send(bytes, bytes.Length, _sendEp);
    }
}
