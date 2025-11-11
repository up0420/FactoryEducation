using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpOscTransport : MonoBehaviour {
    public int port = 9000;
    public string hostIp = "192.168.0.10"; // Host의 수신IP
    public bool isHost = true;             // Host면 broadcast 송신 허용

    UdpClient rx, tx;
    IPEndPoint txEp;
    Thread rxThread;
    volatile bool running;

    public event Action<string, IPEndPoint> OnMessage;

    void Awake() {
        rx = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        tx = new UdpClient(); tx.EnableBroadcast = true;
        txEp = isHost ? new IPEndPoint(IPAddress.Broadcast, port)
                      : new IPEndPoint(IPAddress.Parse(hostIp), port);
    }
    void OnEnable() { running = true; rxThread = new Thread(Loop){IsBackground=true}; rxThread.Start(); }
    void OnDisable(){ running = false; try{rx.Close();}catch{} try{tx.Close();}catch{} }

    void Loop(){
        while(running){
            try{
                var rep = new IPEndPoint(IPAddress.Any,0);
                var data = rx.Receive(ref rep);
                OnMessage?.Invoke(Encoding.UTF8.GetString(data), rep);
            }catch{}
        }
    }
    public void SendJson(string json){
        var b = Encoding.UTF8.GetBytes(json);
        tx.Send(b, b.Length, txEp);
    }
}
