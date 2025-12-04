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
    public GameObject leftHandControllerObj;  // [변경] GameObject로 변경 (드래그 앤 드롭 쉽도록)
    public GameObject rightHandControllerObj; // [변경] GameObject로 변경

    private XRBaseController leftHandController;
    private XRBaseController rightHandController;

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
        // [수정] PhotonView가 없으면 (LobbyCameraRig의 경우) 로컬 플레이어로 간주
        if (photonView != null && !photonView.IsMine)
        {
            enabled = false;
            return;
        }

        // VR 컨트롤러 초기화
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // [수정] GameObject에서 컨트롤러 컴포넌트 추출
        if (leftHandControllerObj != null) leftHandController = leftHandControllerObj.GetComponent<XRBaseController>();
        if (rightHandControllerObj != null) rightHandController = rightHandControllerObj.GetComponent<XRBaseController>();

        // [수정] 컨트롤러 참조가 비어있으면 자동으로 찾기 (무조건 실행 보장)
        if (leftHandController == null || rightHandController == null)
        {
            XRBaseController[] controllers = GetComponentsInChildren<XRBaseController>();
            foreach (var controller in controllers)
            {
                // 이름으로 왼손/오른손 구분
                string name = controller.gameObject.name.ToLower();
                if (name.Contains("left")) 
                {
                    leftHandController = controller;
                    leftHandControllerObj = controller.gameObject;
                }
                if (name.Contains("right")) 
                {
                    rightHandController = controller;
                    rightHandControllerObj = controller.gameObject;
                }
            }
        }

        // 리모컨(Ray Interactor) 기능 추가
        if (leftHandControllerObj != null) SetupRayInteractor(leftHandControllerObj);
        if (rightHandControllerObj != null) SetupRayInteractor(rightHandControllerObj);

        Debug.Log("[VRHandInteraction] VR 손 상호작용 시스템 초기화 (리모컨 기능 추가됨)");

        // [수정] 손 자체가 레이저를 막지 않도록 레이어 변경 (Ignore Raycast = 2)
        if (leftHandControllerObj != null) SetLayerRecursively(leftHandControllerObj, 2);
        if (rightHandControllerObj != null) SetLayerRecursively(rightHandControllerObj, 2);
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    /// <summary>
    /// 런타임에 Ray Interactor 및 시각화 컴포넌트 추가
    /// </summary>
    void SetupRayInteractor(GameObject controllerObj)
    {
        // 0. XRController (Input Provider) 확인 및 추가
        // Ray Interactor가 작동하려면 입력(클릭 등)을 처리할 컨트롤러 컴포넌트가 필수임
        XRBaseController xrController = controllerObj.GetComponent<XRBaseController>();
        if (xrController == null)
        {
            // DeviceBased 컨트롤러 추가 (VRPlayerController와 동일한 방식)
            UnityEngine.XR.Interaction.Toolkit.XRController deviceController = controllerObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRController>();
            
            // 왼손/오른손 노드 설정
            if (controllerObj.name.ToLower().Contains("left"))
                deviceController.controllerNode = XRNode.LeftHand;
            else
                deviceController.controllerNode = XRNode.RightHand;
                
            Debug.Log($"[VRHandInteraction] {controllerObj.name}에 XRController(DeviceBased) 자동 추가됨");
        }

        // 1. XRRayInteractor 추가
        UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor = controllerObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        if (rayInteractor == null)
        {
            rayInteractor = controllerObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        }

        // [수정] 이미 있어도 설정을 강제로 덮어씌움 (중요!)
        rayInteractor.enableUIInteraction = true; // UI 상호작용 활성화
        rayInteractor.maxRaycastDistance = 10.0f;
        rayInteractor.raycastMask = ~0; // 모든 레이어 인식 (UI + 3D 오브젝트)

        // 2. LineRenderer 추가 (시각화)
        LineRenderer lineRenderer = controllerObj.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = controllerObj.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.005f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // 기본 흰색 재질
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = new Color(1, 0, 0, 0); // 끝은 투명하게
            lineRenderer.useWorldSpace = true;
        }

        // 3. XRInteractorLineVisual 추가 (LineRenderer 제어)
        UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual lineVisual = controllerObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
        if (lineVisual == null)
        {
            lineVisual = controllerObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
            lineVisual.lineWidth = 0.005f;
            // lineVisual.validColorGradient = ... (기본값 사용)
        }
    }

    void Update()
    {
        // [수정] PhotonView가 없으면 로컬 플레이어로 간주
        if (photonView != null && !photonView.IsMine) return;

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
    /// 상호작용 활성화/비활성화
    /// </summary>
    public void SetInteractionEnabled(bool enabled)
    {
        // XRRayInteractor 활성화/비활성화
        if (leftHandController != null)
        {
            var ray = leftHandController.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            if (ray != null) ray.enabled = enabled;
        }
        if (rightHandController != null)
        {
            var ray = rightHandController.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            if (ray != null) ray.enabled = enabled;
        }

        // 스크립트 자체 활성화/비활성화 (Update 루프 정지)
        this.enabled = enabled;
        
        Debug.Log($"[VRHandInteraction] 상호작용 {(enabled ? "활성화" : "비활성화")}");
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
