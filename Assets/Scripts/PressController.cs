using UnityEngine;
using System.Collections;

/// <summary>
/// 버튼 클릭에 따라 프레스 오브젝트를 위아래로 움직이는 스크립트입니다.
/// </summary>
public class PressController : MonoBehaviour
{
    [Header("프레스 움직임 설정")]
    [Tooltip("프레스가 '위' 상태일 때 초기 위치로부터의 Y축 오프셋입니다.")]
    public float upYOffset = 2.0f; // 예: 초기 위치보다 2 유닛 위
    [Tooltip("프레스가 '아래' 상태일 때 초기 위치로부터의 Y축 오프셋입니다.")]
    public float downYOffset = 0.0f; // 예: 초기 위치 (0 유닛 오프셋)

    [Tooltip("프레스가 움직이는 속도입니다 (초당 유닛).")]
    public float moveSpeed = 1.0f;

    private Vector3 _initialPosition;
    private Coroutine _currentMovementCoroutine;

    void Awake()
    {
        // 오브젝트의 초기 위치를 저장합니다.
        _initialPosition = transform.position;
    }

    /// <summary>
    /// 프레스를 '위' 위치로 이동시킵니다.
    /// 이 메서드는 UI 버튼의 OnClick 이벤트에 할당할 수 있습니다.
    /// </summary>
    public void MovePressUp()
    {
        Vector3 targetPosition = _initialPosition + Vector3.up * upYOffset;
        StartMovement(targetPosition);
    }

    /// <summary>
    /// 프레스를 '아래' 위치로 이동시킵니다.
    /// 이 메서드는 UI 버튼의 OnClick 이벤트에 할당할 수 있습니다.
    /// </summary>
    public void MovePressDown()
    {
        Vector3 targetPosition = _initialPosition + Vector3.up * downYOffset;
        StartMovement(targetPosition);
    }

    private void StartMovement(Vector3 targetPosition)
    {
        // 이미 움직임이 진행 중이라면 중지하고 새로운 움직임을 시작합니다.
        if (_currentMovementCoroutine != null)
        {
            StopCoroutine(_currentMovementCoroutine);
        }
        _currentMovementCoroutine = StartCoroutine(MoveToTarget(targetPosition));
    }

    private IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float startTime = Time.time;

        // 목표 위치에 도달할 때까지 부드럽게 이동합니다.
        while (transform.position != targetPosition)
        {
            float distCovered = (Time.time - startTime) * moveSpeed;
            // journeyLength가 0이 되는 경우를 방지하기 위해 0으로 나누는 것을 피합니다.
            float fractionOfJourney = journeyLength > 0 ? distCovered / journeyLength : 1.0f;
            transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            yield return null; // 다음 프레임까지 대기
        }
        _currentMovementCoroutine = null;
    }
}