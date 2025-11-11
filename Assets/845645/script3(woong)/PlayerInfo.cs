using UnityEngine;

/// <summary>
/// 각 플레이어의 상태 정보를 추적하는 클래스
/// </summary>
public class PlayerInfo {
    public string id;                    // 플레이어 ID (예: "hmd-01")
    public long lastSeen;                // 마지막으로 메시지를 받은 시간 (ms)
    public bool isReady;                 // 세션 준비 완료 여부
    public bool ppeDone;                 // PPE 선택 완료 여부
    public int ppeCorrect;               // PPE 정답 개수
    public int ppeBonus;                 // PPE 보너스 점수

    // 위치 정보
    public Vector3 position;
    public Quaternion rotation;
    public long lastPoseUpdate;          // 마지막 포즈 업데이트 시간

    // 아바타 오브젝트 참조
    public GameObject avatarObject;

    public PlayerInfo(string playerId) {
        id = playerId;
        lastSeen = OscMessage.NowMs();
        isReady = false;
        ppeDone = false;
        ppeCorrect = 0;
        ppeBonus = 0;
        position = Vector3.zero;
        rotation = Quaternion.identity;
        lastPoseUpdate = 0;
        avatarObject = null;
    }

    /// <summary>
    /// 플레이어가 살아있는지 확인 (10초 이내에 메시지를 받았는지)
    /// </summary>
    public bool IsAlive() {
        long now = OscMessage.NowMs();
        return (now - lastSeen) < 10000;
    }

    /// <summary>
    /// 플레이어 상태를 업데이트
    /// </summary>
    public void UpdateLastSeen() {
        lastSeen = OscMessage.NowMs();
    }

    /// <summary>
    /// 포즈 정보를 업데이트
    /// </summary>
    public void UpdatePose(Vector3 pos, Quaternion rot) {
        position = pos;
        rotation = rot;
        lastPoseUpdate = OscMessage.NowMs();
        UpdateLastSeen();
    }
}
