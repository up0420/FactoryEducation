using UnityEngine;
using System.Collections;

public class PlayerSync : MonoBehaviour
{

    [Header("Network Settings")]
    public bool isLocal = false; // �� Inspector���� '��'�� ��� üũ
    public string playerId = "Player_1"; // (�ӽ�) ���� ID
    public float syncRate = 0.05f; // 0.05��(20Hz)���� ��ġ ����
    public float lerpSpeed = 10f;  // (Ÿ��) ��ġ ���� �ӵ�

    [Header("Component References")]
    public GameObject vrCameraRig;    // 1��Ī ī�޶�/�� (XRRig)
    public GameObject avatarBodyMesh; // 3��Ī �ƹ�Ÿ �� (HumanM_BodyMesh)

    // ���� 1. �߰��� �κ� (��� ���� ��Ƽ���� �迭) ����
    [Header("Color Options")]
    // Inspector���� ���⿡ ������, �Ķ��� �� ��� ��Ƽ������ �̸� �־�Ӵϴ�.
    public Material[] allPlayerMaterials; // [0]=Red, [1]=Blue, [2]=Green ...
    // ����

    // --- ���� ���� ---
    private NetSession1 netSession; // �߾� ������
    private Vector3 targetPos;
    private Quaternion targetRot;

    // --- ���� �÷��̾�� ��ũ��Ʈ ���� ---
    private NewPlayerMover localMover;
    private CharacterController controller;


    void Start()
    {
        // �߾� ������(NetSession)�� ������ ã��
        netSession = FindObjectOfType<NetSession1>();
        if (netSession == null)
        {
            Debug.LogError("���� NetSession ������Ʈ�� �����ϴ�!");
            return;
        }

        // NetSession�� ���� ���
        netSession.RegisterPlayer(playerId, this);

        // ���� ��ũ��Ʈ�� ��������
        localMover = GetComponent<NewPlayerMover>();
        controller = GetComponent<CharacterController>();

        // 1��Ī / 3��Ī ��ȯ
        if (isLocal)
        {
            // '��' (1��Ī)
            if (vrCameraRig) vrCameraRig.SetActive(true);
            if (avatarBodyMesh) avatarBodyMesh.SetActive(false);
            if (localMover) localMover.enabled = true;
            if (controller) controller.enabled = true;

            // �� ��ġ ���� ���� ����
            StartCoroutine(SendPoseLoop());
        }
        else
        {
            // 'Ÿ��' (3��Ī)
            if (vrCameraRig) vrCameraRig.SetActive(false);
            if (avatarBodyMesh) avatarBodyMesh.SetActive(true);
            if (localMover) localMover.enabled = false;
            if (controller) controller.enabled = false;
            // (���� ������ NetSession1�� ��ȣ�� �� �� SetAvatarColor �Լ��� ó��)
            // (�׽�Ʈ��) 'Ÿ��' �ƹ�Ÿ��� ������ 0�� ��Ƽ����(������)�� ����
            SetAvatarColor(0);
        }
    }

    // 'Ÿ��'�� ��쿡�� �����
    void Update()
    {
        if (!isLocal)
        {
            // ���ŵ� targetPos/Rot�� ���� �ε巴�� �̵�
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lerpSpeed);
        }
    }

    // '��'�� ��쿡�� �����
    IEnumerator SendPoseLoop()
    {
        while (true)
        {
            // XRRig ���� ���� ī�޶�(��) ��ġ�� ã��
            Transform eyeCamera = vrCameraRig.transform.Find("Camera");
            // (���� OVRCameraRig��� ��δ� "TrackingSpace/CenterEyeAnchor")

            if (eyeCamera != null)
            {
                // NetSession�� ���� �� ��ġ/ȸ�� ���� ����
                netSession.SendPose(eyeCamera.position, eyeCamera.rotation);
            }

            yield return new WaitForSeconds(syncRate);
        }
    }

    // NetSession�� ȣ�����ִ� �Լ� (Ÿ���� ��ġ ����)
    public void OnPoseUpdate(Vector3 pos, Quaternion rot)
    {
        if (!isLocal)
        {
            targetPos = pos;
            targetRot = rot;
        }
    }

    // ���� 2. �߰��� �κ� (NetSession1�� ȣ���� ���� ���� �Լ�) ����
    /// <summary>
    /// NetSession1�κ��� ���� ���� �ε����� �ƹ�Ÿ ��Ƽ������ �����մϴ�.
    /// </summary>
    /// <param name="colorIndex">allPlayerMaterials �迭�� �ε���</param>
    public void SetAvatarColor(int colorIndex)
    {
        if (isLocal) return; // '��' �ڽſ��Դ� �������� ���� (�� �ƹ�Ÿ�� ��������)

        if (avatarBodyMesh != null && allPlayerMaterials != null && allPlayerMaterials.Length > colorIndex && colorIndex >= 0)
        {
            // �ƹ�Ÿ ���� �������� ã���ϴ�. (SkinnedMeshRenderer�� ���� ����)
            Renderer avatarRenderer = avatarBodyMesh.GetComponentInChildren<Renderer>();
            if (avatarRenderer != null)
            {
                // �迭���� �ùٸ� ��Ƽ������ ã�� �����մϴ�.
                avatarRenderer.material = allPlayerMaterials[colorIndex];
                Debug.Log($"[{playerId}] �ƹ�Ÿ ���� ����: {allPlayerMaterials[colorIndex].name}");
            }
        }
    }
    // ����
}