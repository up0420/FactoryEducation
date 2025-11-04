using UnityEngine;

public class EmergencyStopDemo : MonoBehaviour {
    public NetSession net;

    public void TriggerEStop(string reason = "vibration") {
        var m = new OscMessage {
            path = "/emergency/stop",
            id = net != null ? net.myId : "unknown",
            ts = OscMessage.NowMs(),
            seq = 0,
            reason = reason
        };
        // UDP 브로드캐스트
        FindObjectOfType<OscTransport>()?.SendJson(OscMessage.ToJson(m));
        // ※ WS(TCP)도 병행 전송하려면 여기서 추가 구현
    }
}
