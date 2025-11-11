using UnityEngine;
using System.Collections;

public class PlayerSync : MonoBehaviour
{
    [Header("Network Settings")]
    public bool isLocal = false; // ★ Inspector에서 '나'일 경우 체크
    public string playerId = "Player_1"; // (임시) 고유 ID
    public float syncRate = 0.05f; // 0.05초(20Hz)마다 위치 전송
    public float lerpSpeed = 10f;  // (타인) 위치 보간 속도

    [Header("Component References")]
    public GameObject vrCameraRig;    // 1인칭 카메라/손 (XRRig)
    public GameObject avatarBodyMesh; // 3인칭 아바타 모델 (HumanM_BodyMesh)

    // --- 내부 변수 ---
    private NetSession1 netSession; // 중앙 관리자
    private Vector3 targetPos;
    private Quaternion targetRot;

    // --- 로컬 플레이어용 스크립트 참조 ---
    private NewPlayerMover localMover;
    private CharacterController controller;

    void Start()
    {
        // 중앙 관리자(NetSession)를 씬에서 찾음
        netSession = FindObjectOfType<NetSession1>();
        if (netSession == null)
        {
            Debug.LogError("씬에 NetSession 오브젝트가 없습니다!");
            return;
        }

        // NetSession에 나를 등록
        netSession.RegisterPlayer(playerId, this);

        // 로컬 스크립트들 가져오기
        localMover = GetComponent<NewPlayerMover>();
        controller = GetComponent<CharacterController>();

        // 1인칭 / 3인칭 전환
        if (isLocal)
        {
            // '나' (1인칭)
            if (vrCameraRig) vrCameraRig.SetActive(true);
            if (avatarBodyMesh) avatarBodyMesh.SetActive(false);
            if (localMover) localMover.enabled = true;
            if (controller) controller.enabled = true;

            // 내 위치 정보 전송 시작
            StartCoroutine(SendPoseLoop());
        }
        else
        {
            // '타인' (3인칭)
            if (vrCameraRig) vrCameraRig.SetActive(false);
            if (avatarBodyMesh) avatarBodyMesh.SetActive(true);
            if (localMover) localMover.enabled = false;
            if (controller) controller.enabled = false;
        }
    }

    // '타인'일 경우에만 실행됨
    void Update()
    {
        if (!isLocal)
        {
            // 수신된 targetPos/Rot를 향해 부드럽게 이동
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lerpSpeed);
        }
    }

    // '나'일 경우에만 실행됨
    IEnumerator SendPoseLoop()
    {
        while (true)
        {
            // XRRig 안의 실제 카메라(눈) 위치를 찾음
            Transform eyeCamera = vrCameraRig.transform.Find("Camera");
            // (만약 OVRCameraRig라면 경로는 "TrackingSpace/CenterEyeAnchor")

            if (eyeCamera != null)
            {
                // NetSession을 통해 내 위치/회전 정보 전송
                netSession.SendPose(eyeCamera.position, eyeCamera.rotation);
            }

            yield return new WaitForSeconds(syncRate);
        }
    }

    // NetSession이 호출해주는 함수 (타인의 위치 갱신)
    public void OnPoseUpdate(Vector3 pos, Quaternion rot)
    {
        if (!isLocal)
        {
            targetPos = pos;
            targetRot = rot;
        }
    }
}