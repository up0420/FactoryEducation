using UnityEngine;
using System.Collections;

public class PlayerSync : MonoBehaviour
{
    public bool isLocal = false; // 자기 자신인지?
    public string playerId;      // 유니크 ID
    public float syncRate = 0.05f; // 송신 주기 (20Hz)
    public float lerpSpeed = 10f;  // 보간 속도

    // --- 카메라/아바타 설정을 위해 새로 추가 ---
    [Header("Component References")]
    public GameObject vrCameraRig; // 1인칭 카메라/손 (자식 오브젝트)
    public GameObject avatarBodyMesh; // 3인칭 아바타 모델 (자식 오브젝트)
    // ------------------------------------------

    private Vector3 targetPos;
    private Quaternion targetRot;
    private NetSession net;

    void Start()
    {
        net = FindObjectOfType<NetSession>();

        // ★★★ 여기가 핵심입니다 ★★★
        if (isLocal)
        {
            // '나'일 경우
            vrCameraRig.SetActive(true);      // 1인칭 카메라(눈)와 손을 켠다.
            avatarBodyMesh.SetActive(false);  // 3인칭 아바타(몸)는 끈다.

            // NewPlayerMover 같은 로컬 이동 스크립트도 여기서 활성화
            // GetComponent<NewPlayerMover>().enabled = true;

            StartCoroutine(SendPoseLoop());   // 내 위치 정보 전송 시작
        }
        else
        {
            // '타인'일 경우
            vrCameraRig.SetActive(false);     // 타인의 1인칭 카메라와 손은 끈다.
            avatarBodyMesh.SetActive(true);   // 타인의 3인칭 아바타(몸)는 켠다.

            // 타인의 캐릭터는 내가 직접 조종하는게 아님
            // GetComponent<NewPlayerMover>().enabled = false;
            // GetComponent<CharacterController>().enabled = false; 
        }
        // ★★★ 여기까지 ★★★
    }

    void Update()
    {
        if (!isLocal)
        {
            // 원격 플레이어는 부드럽게 보간
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lerpSpeed);
        }
        // (로컬 플레이어의 Update는 NewPlayerMover가 담당)
    }

    IEnumerator SendPoseLoop()
    {
        while (true)
        {
            // 1인칭 카메라의 위치/회전값을 전송해야 함
            // 예시: Transform localCamera = vrCameraRig.transform.Find("CenterEyeAnchor");
            // net.SendPose(playerId, localCamera.position, localCamera.rotation);

            // 지금은 임시로 아바타 루트의 위치를 전송
            net.SendPose(playerId, transform.position, transform.rotation);

            yield return new WaitForSeconds(syncRate);
        }
    }

    public void OnPoseUpdate(Vector3 pos, Quaternion rot)
    {
        targetPos = pos;
        targetRot = rot;
    }
}