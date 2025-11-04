using UnityEngine;
using System.Collections;

public class PlayerSync : MonoBehaviour
{
    public bool isLocal = false; // 자기 자신인지?
    public string playerId;      // 유니크 ID
    public float syncRate = 0.05f; // 송신 주기 (20Hz)
    public float lerpSpeed = 10f;  // 보간 속도

    private Vector3 targetPos;
    private Quaternion targetRot;
    private NetSession net; // 네트워크 세션 매니저 참조

    void Start()
    {
        net = FindObjectOfType<NetSession>();
        if (isLocal)
            StartCoroutine(SendPoseLoop());
    }

    void Update()
    {
        if (!isLocal)
        {
            // 원격 플레이어는 부드럽게 보간
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lerpSpeed);
        }
    }

    IEnumerator SendPoseLoop()
    {
        while (true)
        {
            if (net != null)
                net.SendPose(playerId, transform.position, transform.rotation);
            yield return new WaitForSeconds(syncRate);
        }
    }

    // 수신된 포즈 적용
    public void OnPoseUpdate(Vector3 pos, Quaternion rot)
    {
        targetPos = pos;
        targetRot = rot;
    }
}
