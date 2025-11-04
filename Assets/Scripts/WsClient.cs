using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;   // Package Manager로 설치

public class WsClient : MonoBehaviour {
    public string url = "ws://192.168.0.10:8080/ws"; // Host의 WS 주소
    WebSocket ws;
    public event Action<string> OnMessage;

    async void OnEnable(){
        ws = new WebSocket(url);
        ws.OnMessage += (bytes)=> OnMessage?.Invoke(Encoding.UTF8.GetString(bytes));
        ws.OnClose   += (code)=> Debug.Log("WS closed:"+code);
        ws.OnError   += (err)=> Debug.LogWarning("WS error:"+err);
        await ws.Connect();
    }
    void Update(){ ws?.DispatchMessageQueue(); }
    async void OnDisable(){ if(ws!=null) await ws.Close(); }

    public async void SendJson(string json){
        if(ws!=null && ws.State==WebSocketState.Open)
            await ws.SendText(json);
    }
}
