using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f; // 이동 속도
    [SerializeField] private float gravity = -9.81f; // 중력 값
    [SerializeField] private float jumpHeight = 1.2f; // 점프 높이
    [SerializeField] private float turnSpeed = 10f; // 회전 속도

    private CharacterController characterController; // 충돌+이동 담당
    private InputSystem_Actions inputActions; // 자동 발급 입력표
    private Camera mainCam; // 기준 카메라
    private float verticalVelocity; // 떨어지는 속도
    private Vector3 spawnPos; // 시작 위치

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new InputSystem_Actions(); // 입력표 생성
        mainCam = Camera.main; // 메인 카메라 자동 탐색
        spawnPos = transform.position; // 시작 위치 기억
    }

    private void OnEnable()
    {
        inputActions.Player.Enable(); // Player 맵 켜기
    }

    private void OnDisable()
    {
        inputActions.Player.Disable(); // Player 맵 끄기
    }

    private void OnDestroy()
    {
        inputActions.Dispose(); // 입력표 정리
    }

    private void Update()
    {
        Vector2 input = Vector2.ClampMagnitude(inputActions.Player.Move.ReadValue<Vector2>(), 1f); // 이동 입력
        Vector3 moveDir = ToCameraSpace(input); // 카메라 기준 방향
        Vector3 movement = moveDir * moveSpeed; // 평면 이동량

        if (moveDir.sqrMagnitude > 0.001f) // 입력 있을 때만
        {
            Quaternion look = Quaternion.LookRotation(moveDir); // 바라볼 방향
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime); // 부드럽게 회전
        }

        if (characterController.isGrounded && verticalVelocity < 0f) // 땅에 닿았으면
        {
            verticalVelocity = -2f; // 바닥에 붙이기
        }

        if (characterController.isGrounded && inputActions.Player.Jump.WasPressedThisFrame()) // 땅+Space
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // 높이→속도 변환
        }

        verticalVelocity += gravity * Time.deltaTime; // 낙하 가속

        movement.y = verticalVelocity; // 상하 속도 합치기
        characterController.Move(movement * Time.deltaTime); // 충돌 고려해 이동
    }

    public void Respawn()
    {
        characterController.enabled = false; // 충돌 잠시 해제
        transform.position = spawnPos; // 시작 위치로
        verticalVelocity = 0f; // 낙하 속도 초기화
        characterController.enabled = true; // 충돌 복구
    }

    private Vector3 ToCameraSpace(Vector2 input)    {
        Vector3 fwd = Vector3.forward; // 예비: 월드 앞
        Vector3 right = Vector3.right; // 예비: 월드 옆

        if (mainCam != null) // 카메라 있으면 기준 교체
        {
            fwd = mainCam.transform.forward; // 카메라 앞
            fwd.y = 0f; // 수평 투영
            fwd.Normalize(); // 길이 1
            right = mainCam.transform.right; // 카메라 옆
            right.y = 0f; // 수평 투영
            right.Normalize(); // 길이 1
        }

        return right * input.x + fwd * input.y; // 카메라 기준 합성
    }
}
