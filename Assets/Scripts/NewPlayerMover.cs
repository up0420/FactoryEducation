using UnityEngine;
using UnityEngine.InputSystem; // ★ Input System 사용을 위해 필수

// CharacterController가 항상 있도록 강제
[RequireComponent(typeof(CharacterController))]
public class NewPlayerMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float turnSpeed = 120f;

    // --- 비공개 변수 ---
    private CharacterController controller;
    private Vector2 moveInput;

    // 2단계에서 만든 Input Action C# 클래스
    private MyPlayerControls controls;

    // 1. 스크립트가 처음 로드될 때 실행 (초기화)
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new MyPlayerControls(); // Input Action을 준비
    }

    // 2. 오브젝트가 활성화될 때 실행
    private void OnEnable()
    {
        controls.Player.Move.Enable(); // "Move" 액션 입력을 받기 시작

        // "Move" 액션이 실행될 때(performed) OnMovePerformed 함수를 호출하도록 구독
        controls.Player.Move.performed += OnMovePerformed;
        // "Move" 액션이 취소될 때(canceled) OnMoveCanceled 함수를 호출하도록 구독
        controls.Player.Move.canceled += OnMoveCanceled;
    }

    // 3. 오브젝트가 비활성화될 때 실행 (정리)
    private void OnDisable()
    {
        controls.Player.Move.Disable(); // "Move" 액션 입력을 중지
        controls.Player.Move.performed -= OnMovePerformed;
        controls.Player.Move.canceled -= OnMoveCanceled;
    }

    // 4. "Move" 액션이 실행되었을 때 (키가 눌렸을 때)
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>(); // (x, y) 입력값을 읽어옴
        Debug.Log("Input Read: " + moveInput); // 콘솔에 입력값 출력 (확인용)
    }

    // 5. "Move" 액션이 취소되었을 때 (키에서 손을 뗐을 때)
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero; // 입력을 0으로 초기화 (멈춤)
    }

    // 6. 매 프레임마다 실행
    private void Update()
    {
        // 입력이 없으면(거의 0이면) 정지
        if (moveInput.sqrMagnitude < 0.01f)
            return;

        // 2D 입력(x, y)을 3D 이동 방향(x, 0, z)으로 변환
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);

        // 회전
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );

        // 이동
        Vector3 move = moveDirection * moveSpeed * Time.deltaTime;
        controller.Move(move);
    }
}