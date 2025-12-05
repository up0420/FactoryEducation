using UnityEngine;

/// <summary>
/// 프레스 기계에 눌렸을 때 모델을 교체하는 스크립트
/// </summary>
public class PressableObject : MonoBehaviour
{
    [Header("Model Settings")]
    public GameObject beforeModel; // 가공 전 모델 (처음에 보임)
    public GameObject afterModel;  // 가공 후 모델 (눌리면 보임)

    void Start()
    {
        // 시작할 때 초기화
        if (beforeModel != null) beforeModel.SetActive(true);
        if (afterModel != null) afterModel.SetActive(false);
    }

    /// <summary>
    /// 프레스에 눌렸을 때 호출 (PressMachine에서 호출함)
    /// </summary>
    public void OnPressed()
    {
        if (beforeModel != null) beforeModel.SetActive(false);
        if (afterModel != null) afterModel.SetActive(true);
        
        Debug.Log($"[PressableObject] {name} 모델 교체 완료!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Bottom")
        {
            if (beforeModel != null) beforeModel.SetActive(false);
            if (afterModel != null) afterModel.SetActive(true);
        }
    }
}
