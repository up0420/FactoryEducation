using UnityEngine;
using Photon.Pun;

/// <summary>
/// 퀴즈용 에셋 스포너
/// 프레스 기계 작업대에 정답/오답 에셋을 배치
/// </summary>
public class QuizAssetSpawner : MonoBehaviourPun
{
    [Header("Press Machines")]
    public PressMachine leftPressMachine;   // 왼쪽 프레스 기계
    public PressMachine rightPressMachine;  // 오른쪽 프레스 기계

    [Header("Spawn Points")]
    public Transform leftSpawnPoint;   // 왼쪽 기계 작업대 위치
    public Transform rightSpawnPoint;  // 오른쪽 기계 작업대 위치

    // 현재 생성된 에셋
    private GameObject leftAsset;
    private GameObject rightAsset;

    // 어느 쪽이 정답인지 추적
    private bool isLeftCorrect = false;

    /// <summary>
    /// 에셋 생성 (문제 출제 시 호출)
    /// </summary>
    public void SpawnAssets(QuizData quizData)
    {
        // 기존 에셋 제거
        ClearAssets();

        // 랜덤으로 좌우 배치 결정
        isLeftCorrect = Random.value > 0.5f;

        GameObject correctPrefab = quizData.correctAssetPrefab;
        GameObject incorrectPrefab = quizData.incorrectAssetPrefab;

        if (isLeftCorrect)
        {
            // 왼쪽 = 정답, 오른쪽 = 오답
            if (correctPrefab != null && leftSpawnPoint != null)
            {
                leftAsset = Instantiate(correctPrefab, leftSpawnPoint.position, leftSpawnPoint.rotation);
                Debug.Log($"[QuizAssetSpawner] 왼쪽에 정답 에셋 배치: {correctPrefab.name}");
            }

            if (incorrectPrefab != null && rightSpawnPoint != null)
            {
                rightAsset = Instantiate(incorrectPrefab, rightSpawnPoint.position, rightSpawnPoint.rotation);
                Debug.Log($"[QuizAssetSpawner] 오른쪽에 오답 에셋 배치: {incorrectPrefab.name}");
            }
        }
        else
        {
            // 왼쪽 = 오답, 오른쪽 = 정답
            if (incorrectPrefab != null && leftSpawnPoint != null)
            {
                leftAsset = Instantiate(incorrectPrefab, leftSpawnPoint.position, leftSpawnPoint.rotation);
                Debug.Log($"[QuizAssetSpawner] 왼쪽에 오답 에셋 배치: {incorrectPrefab.name}");
            }

            if (correctPrefab != null && rightSpawnPoint != null)
            {
                rightAsset = Instantiate(correctPrefab, rightSpawnPoint.position, rightSpawnPoint.rotation);
                Debug.Log($"[QuizAssetSpawner] 오른쪽에 정답 에셋 배치: {correctPrefab.name}");
            }
        }

        // [디버깅] 생성된 에셋에 스크립트가 제대로 붙어있는지 즉시 확인
        if (leftAsset != null && leftAsset.GetComponent<PressableObject>() == null)
            Debug.LogError($"[QuizAssetSpawner] 생성된 왼쪽 에셋({leftAsset.name})에 'PressableObject' 스크립트가 없습니다! 프리팹을 확인하세요.");
        
        if (rightAsset != null && rightAsset.GetComponent<PressableObject>() == null)
            Debug.LogError($"[QuizAssetSpawner] 생성된 오른쪽 에셋({rightAsset.name})에 'PressableObject' 스크립트가 없습니다! 프리팹을 확인하세요.");
    }

    /// <summary>
    /// 오답 에셋을 찍어 누르는 프레스 기계 작동
    /// </summary>
    /// <summary>
    /// 오답 에셋을 찍어 누르는 프레스 기계 작동
    /// </summary>
    public void CrushIncorrectAsset(bool playerAnswer)
    {
        // 플레이어 답과 실제 정답이 일치하는지 확인
        bool isCorrect = (playerAnswer == isLeftCorrect);

        try
        {
            if (isCorrect)
            {
                // 정답! -> 오답 쪽 기계가 작동
                if (isLeftCorrect)
                {
                    // 왼쪽이 정답이므로 오른쪽(오답) 기계 작동
                    if (rightPressMachine != null)
                    {
                        rightPressMachine.ForceCrush();
                    }
                }
                else
                {
                    // 오른쪽이 정답이므로 왼쪽(오답) 기계 작동
                    if (leftPressMachine != null)
                    {
                        leftPressMachine.ForceCrush();
                    }
                }
            }
            else
            {
                // 오답! -> 플레이어가 선택한 쪽을 찍음 (교육 효과)
                // 또는 여전히 오답 쪽을 찍을 수도 있음 (기획 결정 필요)
                // 현재는 항상 오답 에셋을 찍는 것으로 구현
                if (isLeftCorrect)
                {
                    if (rightPressMachine != null)
                    {
                        rightPressMachine.ForceCrush();
                    }
                }
                else
                {
                    if (leftPressMachine != null)
                    {
                        leftPressMachine.ForceCrush();
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QuizAssetSpawner] 프레스 기계 작동 중 오류 발생 (무시하고 진행): {e.Message}");
        }

        Debug.Log($"[QuizAssetSpawner] 크러시 실행 - 정답: {(isLeftCorrect ? "왼쪽" : "오른쪽")}, 플레이어 답: {(playerAnswer ? "왼쪽" : "오른쪽")}");

        // [추가] 물리 충돌이 실패할 경우를 대비해, 1.0초 뒤에 강제로 모델 교체 (Simulated Crush)
        GameObject targetAsset = isLeftCorrect ? rightAsset : leftAsset;
        if (targetAsset != null)
        {
            StartCoroutine(DelayedCrush(targetAsset, 1.0f));
        }
    }

    /// <summary>
    /// [추가] 프레스 기계가 내려오는 시간에 맞춰 모델을 강제로 교체
    /// </summary>
    System.Collections.IEnumerator DelayedCrush(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            PressableObject pressable = target.GetComponent<PressableObject>();
            if (pressable != null)
            {
                pressable.OnPressed();
                Debug.Log($"[QuizAssetSpawner] 강제 크러시(Simulated) 실행 완료: {target.name}");
            }
        }
    }

    /// <summary>
    /// [최후의 수단] 프레스 기계고 뭐고 다 무시하고, 그냥 강제로 모델을 바꿔버림
    /// </summary>
    public void ForceRevealResult(bool playerAnswer)
    {
        Debug.Log("[QuizAssetSpawner] 강제 정답 공개 실행 (ForceRevealResult)");

        // 1. 왼쪽 에셋 처리
        if (leftAsset != null)
        {
            bool isLeftTargetCorrect = isLeftCorrect; // 왼쪽이 정답인가?
            ProcessReveal(leftAsset, isLeftTargetCorrect);
        }

        // 2. 오른쪽 에셋 처리
        if (rightAsset != null)
        {
            bool isRightTargetCorrect = !isLeftCorrect; // 오른쪽이 정답인가?
            ProcessReveal(rightAsset, isRightTargetCorrect);
        }
    }

    private void ProcessReveal(GameObject asset, bool isCorrectAsset)
    {
        // 1단계: 스크립트로 시도
        PressableObject pressable = asset.GetComponent<PressableObject>();
        if (pressable != null)
        {
            pressable.Reveal();
            return;
        }

        // 2단계: 스크립트가 없으면 하이라키 구조로 직접 찾아서 끔/켬 (Fallback)
        Transform normal = asset.transform.Find("Normal_Model");
        Transform ghost = asset.transform.Find("Ghost_Model");
        if (ghost == null) ghost = asset.transform.Find("Safety_Model");
        if (ghost == null) ghost = asset.transform.Find("Ghost_Model2");
        if (ghost == null) ghost = asset.transform.Find("Helmet Variant"); // 헬멧용

        if (normal != null) normal.gameObject.SetActive(false);
        if (ghost != null) ghost.gameObject.SetActive(true);

        Debug.Log($"[QuizAssetSpawner] {asset.name} 강제 변환 완료 (스크립트 없이 직접 제어)");
    }

    /// <summary>
    /// 모든 에셋 제거
    /// </summary>
    public void ClearAssets()
    {
        if (leftAsset != null)
        {
            Destroy(leftAsset);
            leftAsset = null;
        }

        if (rightAsset != null)
        {
            Destroy(rightAsset);
            rightAsset = null;
        }

        Debug.Log("[QuizAssetSpawner] 에셋 제거 완료");
    }

    /// <summary>
    /// 프레스 기계 초기화
    /// </summary>
    public void ResetMachines()
    {
        if (leftPressMachine != null)
        {
            leftPressMachine.ResetMachine();
        }

        if (rightPressMachine != null)
        {
            rightPressMachine.ResetMachine();
        }
    }
}
