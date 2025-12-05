using UnityEngine;

/// <summary>
/// 별가루 이펙트 컨트롤러
/// VRButton의 정답 프레스가 작동할 때 파티클 이펙트 재생
/// </summary>
public class StarEffectController : MonoBehaviour
{
    [Header("Particle Effect")]
    public ParticleSystem starEffect; // 별가루 파티클 시스템

    [Header("Effect Settings")]
    public bool autoFindParticle = true; // 자동으로 자식 ParticleSystem 찾기

    void Start()
    {
        // 파티클 시스템 자동 검색
        if (autoFindParticle && starEffect == null)
        {
            starEffect = GetComponentInChildren<ParticleSystem>();

            if (starEffect != null)
            {
                Debug.Log($"[StarEffectController] ParticleSystem 자동 검색 성공: {starEffect.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[StarEffectController] ParticleSystem을 찾을 수 없습니다!");
            }
        }

        // 초기에는 파티클 정지
        if (starEffect != null)
        {
            starEffect.Stop();
        }
    }

    /// <summary>
    /// 별가루 이펙트 재생 (VRButton에서 호출)
    /// </summary>
    public void PlayStarEffect()
    {
        if (starEffect == null)
        {
            Debug.LogWarning("[StarEffectController] ParticleSystem이 설정되지 않았습니다!");
            return;
        }

        Debug.Log("[StarEffectController] 별가루 이펙트 재생!");

        // 파티클 재생
        starEffect.Play();
    }

    /// <summary>
    /// 별가루 이펙트 정지
    /// </summary>
    public void StopStarEffect()
    {
        if (starEffect != null)
        {
            starEffect.Stop();
        }
    }

    /// <summary>
    /// 특정 위치에서 별가루 이펙트 재생
    /// </summary>
    public void PlayStarEffectAtPosition(Vector3 position)
    {
        if (starEffect == null)
        {
            Debug.LogWarning("[StarEffectController] ParticleSystem이 설정되지 않았습니다!");
            return;
        }

        // 이펙트 위치 변경
        starEffect.transform.position = position;

        // 파티클 재생
        starEffect.Play();

        Debug.Log($"[StarEffectController] 별가루 이펙트 재생 at {position}");
    }
}
