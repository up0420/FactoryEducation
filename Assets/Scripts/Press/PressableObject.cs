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
        // [자동 감지] Inspector에 할당하지 않았을 경우, 이름으로 찾아서 연결
        if (beforeModel == null)
        {
            Transform normal = transform.Find("Normal_Model");
            if (normal != null) beforeModel = normal.gameObject;
        }

        if (afterModel == null)
        {
            // Ghost_Model 또는 Safety_Model 찾기
            Transform ghost = transform.Find("Ghost_Model");
            if (ghost == null) ghost = transform.Find("Safety_Model");
            if (ghost == null) ghost = transform.Find("Ghost_Model2"); // 혹시 몰라 추가

            if (ghost != null) afterModel = ghost.gameObject;
        }

        // [디버깅] 연결 상태 확인
        if (beforeModel == null) Debug.LogError($"[PressableObject] '{name}'에 'Before Model'이 연결되지 않았습니다! (Normal_Model을 찾을 수 없음)");
        if (afterModel == null) Debug.LogError($"[PressableObject] '{name}'에 'After Model'이 연결되지 않았습니다! (Ghost_Model 또는 Safety_Model을 찾을 수 없음)");

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
        
        Debug.Log($"[PressableObject] {name} 모델 교체 완료! (Pressed)");
    }

    /// <summary>
    /// [추가] 물리적 충돌 없이 모델만 교체 (정답 공개용)
    /// </summary>
    public void Reveal()
    {
        if (beforeModel != null) beforeModel.SetActive(false);
        if (afterModel != null) afterModel.SetActive(true);

        Debug.Log($"[PressableObject] {name} 정답 공개 완료! (Reveal)");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Bottom")
        {
            if (beforeModel != null) beforeModel.SetActive(false);
            if (afterModel != null) afterModel.SetActive(true);

            // [추가] 5초 뒤에 원상복구
            StartCoroutine(ResetModel());
        }
    }

    // [추가] 5초 대기 후 모델 초기화 코루틴
    System.Collections.IEnumerator ResetModel()
    {
        yield return new WaitForSeconds(5.0f);

        if (beforeModel != null) beforeModel.SetActive(true);
        if (afterModel != null) afterModel.SetActive(false);

        Debug.Log($"[PressableObject] {name} 모델 원상복구 완료!");
    }
}
