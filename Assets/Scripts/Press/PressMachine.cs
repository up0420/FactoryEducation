using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// 프레스 기계 작동 시스템 (Phase 5)
/// 버튼 클릭 → 5초 카운트다운 → 3초 간격 반복 작동
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PressMachine : MonoBehaviourPun
{
    [Header("Press Parts")]
    public Transform pressHead;         // 프레스 상부 (하강하는 부분)
    public Transform pressBase;         // 프레스 하부 베드

    [Header("Press Movement")]
    public float upPosition = 2.0f;     // 상승 위치 (Y축 오프셋)
    public float downPosition = 0.2f;   // 하강 위치 (Y축 오프셋)
    public float moveSpeed = 1.5f;      // 이동 속도
    public float cycleInterval = 3.0f;  // 사이클 간격 (초)

    [Header("Countdown")]
    public float countdownTime = 5.0f;  // 카운트다운 시간

    [Header("UI")]
    public Button startButton;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI statusText;

    [Header("Safety Detection")]
    public BoxCollider pressZoneTrigger; // 프레스 내부 감지 영역

    // 프레스 상태
    public enum PressState
    {
        Idle,
        Countdown,
        Operating
    }

    public PressState currentState = PressState.Idle;

    private Vector3 initialHeadPosition;
    private Coroutine operatingCoroutine;
    private bool hasProductInside = false;
    private bool hasHandInside = false; // 안전 감지
    private GameObject currentProduct;

    void Awake()
    {
        if (pressHead != null)
        {
            initialHeadPosition = pressHead.localPosition;
        }

        // 버튼 이벤트 등록
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
        }

        // Trigger 설정
        if (pressZoneTrigger != null)
        {
            pressZoneTrigger.isTrigger = true;
        }
    }

    void Start()
    {
        UpdateStatusUI("대기 중");
        HideCountdown();
    }

    /// <summary>
    /// 시작 버튼 클릭 시 호출
    /// </summary>
    void OnStartButtonClick()
    {
        StartPressButton();
    }

    /// <summary>
    /// 프레스 시작 버튼 (VR 버튼에서 호출 가능)
    /// </summary>
    public void StartPressButton()
    {
        // 현재 턴 플레이어만 작동 가능
        if (!TurnManager.Instance.IsMyTurn())
        {
            Debug.LogWarning("[PressMachine] 현재 당신의 차례가 아닙니다.");
            return;
        }

        if (currentState != PressState.Idle)
        {
            Debug.LogWarning("[PressMachine] 프레스가 이미 작동 중입니다.");
            return;
        }

        // 마스터 클라이언트에게 시작 요청
        if (photonView != null)
        {
            photonView.RPC("RPC_StartCountdown", RpcTarget.All);
        }
        else
        {
            Debug.LogWarning("[PressMachine] PhotonView가 없어 시작 요청을 보낼 수 없습니다.");
        }
    }

    [PunRPC]
    void RPC_StartCountdown()
    {
        Debug.Log("[PressMachine] 카운트다운 시작");

        currentState = PressState.Countdown;
        StartCoroutine(CountdownCoroutine());
    }

    IEnumerator CountdownCoroutine()
    {
        UpdateStatusUI("카운트다운 중...");

        float timeRemaining = countdownTime;

        while (timeRemaining > 0)
        {
            ShowCountdown(Mathf.CeilToInt(timeRemaining));
            yield return new WaitForSeconds(1.0f);
            timeRemaining -= 1.0f;
        }

        HideCountdown();

        // 카운트다운 종료 후 프레스 작동 시작
        if (PhotonNetwork.IsMasterClient)
        {
            if (photonView != null)
            {
                photonView.RPC("RPC_StartPress", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void RPC_StartPress()
    {
        Debug.Log("[PressMachine] 프레스 작동 시작");

        currentState = PressState.Operating;
        UpdateStatusUI("작동 중");

        // 작동 코루틴 시작
        if (operatingCoroutine != null)
        {
            StopCoroutine(operatingCoroutine);
        }
        operatingCoroutine = StartCoroutine(OperatingCycle());
    }

    IEnumerator OperatingCycle()
    {
        while (currentState == PressState.Operating)
        {
            // 하강
            yield return StartCoroutine(MovePressHead(downPosition, "하강 중..."));

            // [추가] 하강 완료 시점에서 모델 교체 (프레스 효과)
            if (hasProductInside && currentProduct != null)
            {
                PressableObject pressable = currentProduct.GetComponent<PressableObject>();
                if (pressable != null)
                {
                    pressable.OnPressed();
                }
            }

            // 제품 체크 및 완성품 생성
            if (hasProductInside && currentProduct != null && PhotonNetwork.IsMasterClient)
            {
                CreateFinishedProduct();
            }

            // 안전 감지 체크
            if (hasHandInside)
            {
                OnHandDetected();
            }

            yield return new WaitForSeconds(0.5f);

            // 상승
            yield return StartCoroutine(MovePressHead(upPosition, "상승 중..."));

            // 사이클 간격 대기
            yield return new WaitForSeconds(cycleInterval);
        }
    }

    IEnumerator MovePressHead(float targetY, string statusMessage)
    {
        UpdateStatusUI(statusMessage);

        Vector3 startPos = pressHead.localPosition;
        Vector3 targetPos = new Vector3(initialHeadPosition.x, targetY, initialHeadPosition.z);

        float journeyLength = Mathf.Abs(targetPos.y - startPos.y);
        float startTime = Time.time;

        while (Vector3.Distance(pressHead.localPosition, targetPos) > 0.01f)
        {
            float distCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = journeyLength > 0 ? distCovered / journeyLength : 1.0f;

            pressHead.localPosition = Vector3.Lerp(startPos, targetPos, fractionOfJourney);

            yield return null;
        }

        pressHead.localPosition = targetPos;
    }

    /// <summary>
    /// 완성품 생성
    /// </summary>
    void CreateFinishedProduct()
    {
        Debug.Log("[PressMachine] 완성품 생성");

        // 현재 제품 파괴
        if (currentProduct != null)
        {
            PhotonNetwork.Destroy(currentProduct);
            currentProduct = null;
            hasProductInside = false;
        }

        // ProductionSystem에 성공 알림
        ProductionSystem productionSystem = FindObjectOfType<ProductionSystem>();
        if (productionSystem != null)
        {
            int currentPlayerIndex = TurnManager.Instance.currentPlayerTurn;
            productionSystem.OnProductComplete(currentPlayerIndex);
        }

        // 완성품 생성 (선택사항: 실제 완성품 오브젝트 배치)
        // SpawnFinishedProduct();
    }

    /// <summary>
    /// 손 감지 시 처리
    /// </summary>
    void OnHandDetected()
    {
        Debug.LogWarning("[PressMachine] 위험! 프레스 내부에 손이 감지되었습니다!");

        // 안전 감점
        int currentPlayerIndex = TurnManager.Instance.currentPlayerTurn;
        if (ScoreManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            ScoreManager.Instance.AddSafetyPenalty(currentPlayerIndex, -100);
        }

        // 경고 UI 표시
        UpdateStatusUI("경고! 안전 위반!");
    }

    /// <summary>
    /// 프레스 정지
    /// </summary>
    public void StopPress()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (photonView != null)
            {
                photonView.RPC("RPC_StopPress", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void RPC_StopPress()
    {
        Debug.Log("[PressMachine] 프레스 정지");

        currentState = PressState.Idle;

        if (operatingCoroutine != null)
        {
            StopCoroutine(operatingCoroutine);
            operatingCoroutine = null;
        }

        UpdateStatusUI("대기 중");
    }

    void OnTriggerEnter(Collider other)
    {
        // 제품 감지
        if (other.CompareTag("ProductMaterial"))
        {
            hasProductInside = true;
            currentProduct = other.gameObject;
            Debug.Log("[PressMachine] 제품 소재 감지");
        }

        // 손 감지 (플레이어 손 태그 필요)
        if (other.CompareTag("PlayerHand"))
        {
            hasHandInside = true;
            Debug.LogWarning("[PressMachine] 손 감지!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ProductMaterial"))
        {
            hasProductInside = false;
            currentProduct = null;
        }

        if (other.CompareTag("PlayerHand"))
        {
            hasHandInside = false;
        }
    }

    #region Quiz System Methods

    /// <summary>
    /// 퀴즈 시스템용: 강제 크러시 애니메이션 (안전 센서 무시)
    /// </summary>
    public void ForceCrush()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (photonView != null)
            {
                photonView.RPC("RPC_ForceCrush", RpcTarget.All);
            }
            else
            {
                Debug.LogWarning("[PressMachine] PhotonView가 없어 ForceCrush RPC를 호출할 수 없습니다.");
            }
        }
    }

    [PunRPC]
    void RPC_ForceCrush()
    {
        Debug.Log("[PressMachine] 강제 크러시 실행 (퀴즈용)");
        StartCoroutine(ForceCrushCoroutine());
    }

    IEnumerator ForceCrushCoroutine()
    {
        // 하강
        yield return StartCoroutine(MovePressHead(downPosition, "크러시!"));

        // [추가] 퀴즈용 크러시에서도 모델 교체 실행
        if (hasProductInside && currentProduct != null)
        {
            PressableObject pressable = currentProduct.GetComponent<PressableObject>();
            if (pressable != null)
            {
                pressable.OnPressed();
            }
        }

        // 0.5초 대기 (압착 효과)
        yield return new WaitForSeconds(0.5f);

        // 상승
        yield return StartCoroutine(MovePressHead(upPosition, "복귀 중"));

        Debug.Log("[PressMachine] 강제 크러시 완료");
    }

    /// <summary>
    /// 퀴즈 시스템용: 프레스 기계 초기화
    /// </summary>
    public void ResetMachine()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (photonView != null)
            {
                photonView.RPC("RPC_ResetMachine", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void RPC_ResetMachine()
    {
        Debug.Log("[PressMachine] 기계 초기화");

        currentState = PressState.Idle;

        if (operatingCoroutine != null)
        {
            StopCoroutine(operatingCoroutine);
            operatingCoroutine = null;
        }

        // 프레스 헤드를 상승 위치로 즉시 이동
        if (pressHead != null)
        {
            pressHead.localPosition = new Vector3(initialHeadPosition.x, upPosition, initialHeadPosition.z);
        }

        UpdateStatusUI("대기 중");
    }

    #endregion

    #region UI Methods

    void UpdateStatusUI(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    void ShowCountdown(int seconds)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = seconds.ToString();
        }
    }

    void HideCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    #endregion
}
