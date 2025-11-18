using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

/// <summary>
/// VR 손 상호작용 시스템
/// XR Grab Interactable을 사용한 양손 잡기
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class VRHandInteraction : MonoBehaviourPun
{
    [Header("VR Controllers")]
    public XRBaseController leftHandController;
    public XRBaseController rightHandController;

    [Header("Hand Transforms")]
    public Transform leftHandTransform;
    public Transform rightHandTransform;

    [Header("Grab Settings")]
    public float grabRange = 2.0f;
    public LayerMask interactableLayer;

    // 현재 잡고 있는 오브젝트
    private GameObject leftHandObject;
    private GameObject rightHandObject;

    // VR 컨트롤러 입력 디바이스
    private InputDevice leftController;
    private InputDevice rightController;

    void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        // VR 컨트롤러 초기화
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        Debug.Log("[VRHandInteraction] VR 손 상호작용 시스템 초기화");
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // 컨트롤러 재연결 확인
        if (!leftController.isValid || !rightController.isValid)
        {
            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }

        // 왼손 그립 버튼
        CheckGrabInput(leftController, ref leftHandObject, leftHandTransform, true);

        // 오른손 그립 버튼
        CheckGrabInput(rightController, ref rightHandObject, rightHandTransform, false);
    }

    void CheckGrabInput(InputDevice controller, ref GameObject handObject, Transform handTransform, bool isLeftHand)
    {
        if (!controller.isValid) return;

        // 그립 버튼 입력 확인
        bool gripButtonPressed = false;
        if (controller.TryGetFeatureValue(CommonUsages.gripButton, out gripButtonPressed))
        {
            if (gripButtonPressed)
            {
                // 그립 버튼을 누르고 있는 동안
                if (handObject == null)
                {
                    // 잡을 오브젝트 찾기
                    TryGrabObject(ref handObject, handTransform, isLeftHand);
                }
            }
            else
            {
                // 그립 버튼을 놓음
                if (handObject != null)
                {
                    ReleaseObject(ref handObject);
                }
            }
        }
    }

    void TryGrabObject(ref GameObject handObject, Transform handTransform, bool isLeftHand)
    {
        if (handTransform == null) return;

        // 손 위치에서 Raycast
        Ray ray = new Ray(handTransform.position, handTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabRange, interactableLayer))
        {
            // 상호작용 가능한 오브젝트인지 확인
            if (hit.collider.CompareTag("ProductMaterial") || hit.collider.CompareTag("Interactable"))
            {
                handObject = hit.collider.gameObject;

                // 물리 비활성화
                Rigidbody rb = handObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // 손에 부착
                handObject.transform.SetParent(handTransform);
                handObject.transform.localPosition = Vector3.zero;
                handObject.transform.localRotation = Quaternion.identity;

                Debug.Log($"[VRHandInteraction] {(isLeftHand ? "왼손" : "오른손")} - {handObject.name} 잡기 성공");

                // 네트워크 동기화
                PhotonView pv = handObject.GetComponent<PhotonView>();
                if (pv != null)
                {
                    photonView.RPC("RPC_GrabObject", RpcTarget.Others, pv.ViewID, isLeftHand);
                }
            }
        }
    }

    void ReleaseObject(ref GameObject handObject)
    {
        if (handObject == null) return;

        Debug.Log($"[VRHandInteraction] {handObject.name} 놓기");

        // 부모 해제
        handObject.transform.SetParent(null);

        // 물리 활성화
        Rigidbody rb = handObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;

            // 손의 속도를 오브젝트에 적용 (던지기)
            // rb.velocity = handController.GetVelocity();
            // rb.angularVelocity = handController.GetAngularVelocity();
        }

        // 네트워크 동기화
        PhotonView pv = handObject.GetComponent<PhotonView>();
        if (pv != null)
        {
            photonView.RPC("RPC_ReleaseObject", RpcTarget.Others, pv.ViewID);
        }

        handObject = null;
    }

    [PunRPC]
    void RPC_GrabObject(int viewID, bool isLeftHand)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView != null)
        {
            GameObject obj = targetView.gameObject;
            Transform hand = isLeftHand ? leftHandTransform : rightHandTransform;

            if (hand != null)
            {
                obj.transform.SetParent(hand);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
            }
        }
    }

    [PunRPC]
    void RPC_ReleaseObject(int viewID)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView != null)
        {
            GameObject obj = targetView.gameObject;
            obj.transform.SetParent(null);

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
    }

    /// <summary>
    /// 양손에 오브젝트를 들고 있는지 확인
    /// </summary>
    public bool IsBothHandsHolding()
    {
        return leftHandObject != null && rightHandObject != null;
    }

    /// <summary>
    /// 특정 손에 오브젝트를 들고 있는지 확인
    /// </summary>
    public bool IsHolding(bool leftHand)
    {
        return leftHand ? leftHandObject != null : rightHandObject != null;
    }

    /// <summary>
    /// 디버그: 레이 표시
    /// </summary>
    void OnDrawGizmos()
    {
        if (leftHandTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(leftHandTransform.position, leftHandTransform.forward * grabRange);
        }

        if (rightHandTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(rightHandTransform.position, rightHandTransform.forward * grabRange);
        }
    }
}
