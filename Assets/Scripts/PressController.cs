using UnityEngine;
using System.Collections;

/// <summary>
/// 버튼 클릭에 따라 프레스 오브젝트를 위아래로 움직이는 스크립트입니다.
/// </summary>
public class PressController : MonoBehaviour
{
    [Header("움직일 오브젝트 설정")]
    [Tooltip("실제로 움직일 프레스 헤드 오브젝트 (Null_2)")]
    public Transform pressHead;

    [Header("프레스 움직임 설정")]
    [Tooltip("프레스가 '위' 상태일 때 초기 위치로부터의 Y축 오프셋입니다.")]
    public float upYOffset = 2.0f; // 예: 초기 위치보다 2 유닛 위
    [Tooltip("프레스가 '아래' 상태일 때 초기 위치로부터의 Y축 오프셋입니다.")]
    public float downYOffset = -2.0f; // 예: 초기 위치보다 2 유닛 아래

    [Tooltip("프레스가 움직이는 속도입니다 (초당 유닛).")]
    public float moveSpeed = 2.0f;

    [Header("반복 움직임 설정")]
    [Tooltip("프레스가 위아래로 반복 움직이는 총 시간(초)입니다.")]
    public float repeatDuration = 10.0f; // 10초 동안 반복

    [Tooltip("한 번 내려갔다 올라오는데 걸리는 시간(초)입니다.")]
    public float cycleTime = 2.0f; // 2초에 한 번씩 위아래

    private Vector3 _initialPosition;
    private Coroutine _currentMovementCoroutine;
    private bool _isRepeating = false;

    void Awake()
    {
        // pressHead가 설정되지 않았으면 자동으로 Null_2 찾기
        if (pressHead == null)
        {
            pressHead = transform.Find("Null_2");

            if (pressHead == null)
            {
                Debug.LogError("[PressController] Null_2 오브젝트를 찾을 수 없습니다! Inspector에서 Press Head를 수동으로 설정하세요.");
            }
            else
            {
                Debug.Log("[PressController] Null_2 오브젝트를 자동으로 찾았습니다.");
            }
        }

        // pressHead의 초기 위치를 저장합니다.
        if (pressHead != null)
        {
            _initialPosition = pressHead.position;
        }
    }

    /// <summary>
    /// 버튼을 누르면 프레스가 일정 시간 동안 위아래로 반복 움직입니다.
    /// VRButton의 OnButtonPressed 이벤트에 연결하세요.
    /// </summary>
    public void StartPressRepeat()
    {
        if (_isRepeating)
        {
            Debug.LogWarning("[PressController] 프레스가 이미 작동 중입니다.");
            return;
        }

        if (_currentMovementCoroutine != null)
        {
            StopCoroutine(_currentMovementCoroutine);
        }

        _currentMovementCoroutine = StartCoroutine(RepeatPressMovement());
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

    /// <summary>
    /// 프레스를 일정 시간 동안 위아래로 반복 움직입니다.
    /// 순서: 아래로 → 위로 반복
    /// </summary>
    private IEnumerator RepeatPressMovement()
    {
        if (pressHead == null)
        {
            Debug.LogError("[PressController] Press Head가 설정되지 않았습니다!");
            yield break;
        }

        _isRepeating = true;
        float elapsedTime = 0f;

        Debug.Log($"[PressController] 프레스 반복 시작! {repeatDuration}초 동안 작동합니다.");

        while (elapsedTime < repeatDuration)
        {
            // 1. 아래로 내려가기 (먼저!)
            Vector3 downPosition = _initialPosition + Vector3.up * downYOffset;
            yield return StartCoroutine(MoveToTarget(downPosition));

            // 잠시 대기
            yield return new WaitForSeconds(0.3f);

            // 2. 위로 올라가기
            Vector3 upPosition = _initialPosition + Vector3.up * upYOffset;
            yield return StartCoroutine(MoveToTarget(upPosition));

            // 잠시 대기
            yield return new WaitForSeconds(0.3f);

            elapsedTime += cycleTime;
        }

        // 작동 완료 후 초기 위치로 복귀
        yield return StartCoroutine(MoveToTarget(_initialPosition));

        _isRepeating = false;
        _currentMovementCoroutine = null;

        Debug.Log("[PressController] 프레스 반복 완료!");
    }

    public void StartMovement(Vector3 targetPosition)
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
        if (pressHead == null) yield break;

        Vector3 startPosition = pressHead.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float startTime = Time.time;

        // 목표 위치에 도달할 때까지 부드럽게 이동합니다.
        while (pressHead.position != targetPosition)
        {
            float distCovered = (Time.time - startTime) * moveSpeed;
            // journeyLength가 0이 되는 경우를 방지하기 위해 0으로 나누는 것을 피합니다.
            float fractionOfJourney = journeyLength > 0 ? distCovered / journeyLength : 1.0f;
            pressHead.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            yield return null; // 다음 프레임까지 대기
        }
        _currentMovementCoroutine = null;
    }
}