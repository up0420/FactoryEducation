using System;
using UnityEngine;

[Serializable]
public class OscMessage {
    public string path;   // 예: "/pose", "/ppe/select", "/session/join"
    public string id;     // 기기/플레이어 ID (예: "hmd-01")
    public long ts;       // Unix ms 타임스탬프
    public int seq;       // 증가 시퀀스

    // 가변 페이로드(필요 키만 사용)
    public float px, py, pz;
    public float qx, qy, qz, qw;
    public string[] picks;      // PPE 선택
    public int correctCount;    // PPE 정답 수
    public int bonus;           // PPE 보너스
    public string reason;       // emergency 등 사유

    public static string ToJson(OscMessage m) => JsonUtility.ToJson(m);
    public static OscMessage FromJson(string s) => JsonUtility.FromJson<OscMessage>(s);
    public static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
