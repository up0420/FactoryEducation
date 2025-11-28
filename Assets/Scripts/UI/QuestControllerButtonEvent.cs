using UnityEngine;
using UnityEngine.Events; // UnityEvent 사용 시 필요

public class QuestControllerButtonEvent : MonoBehaviour
{
    [Header("트리거/그립 버튼 눌렀을 때 호출될 이벤트")]
    public UnityEvent onRightTriggerDown;
    public UnityEvent onRightGripDown;
    public UnityEvent onAButtonDown;

    public GameObject box;

    void Update()
    {
        // 1) 오른손 트리거(검지) 버튼 “딱 눌렀을 때 한 번”
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Debug.Log("Right Trigger Down!");
            //box.SetActive(false);
            onRightTriggerDown?.Invoke();
        }

        // 2) 오른손 그립(손잡이) 버튼
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            Debug.Log("Right Grip Down!");
            //box.SetActive(true);
            onRightGripDown?.Invoke();
        }

        // 3) 오른손 A 버튼
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Debug.Log("A Button Down!");
            onAButtonDown?.Invoke();
        }

        // 참고: 꾹 누르고 있는 동안 계속 체크하고 싶으면 GetDown 대신 Get 사용
        // if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)) { ... }
    }
}