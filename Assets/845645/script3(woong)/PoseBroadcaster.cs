using UnityEngine;

public class PoseBroadcaster : MonoBehaviour {
    public NetSession net;
    public Transform target; // HMD 또는 리그 루트
    public float sendHz = 25f;
    float _t, _interval;

    void Start() {
        if (net == null) net = FindObjectOfType<NetSession>();
        if (target == null) target = Camera.main?.transform;
        _interval = 1f / Mathf.Max(1f, sendHz);
    }

    void Update() {
        _t += Time.deltaTime;
        if (_t >= _interval && net != null && target != null) {
            _t = 0;
            net.SendPose(target.position, target.rotation);
        }
    }
}
