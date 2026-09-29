using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator; // 자식 캐릭터의 애니메이터

    private CharacterController characterController;

    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
    private static readonly int PickUpState = Animator.StringToHash("Base Layer.PickUp");
    private static readonly int InteractionState = Animator.StringToHash("Base Layer.Interaction");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>(); // 씬 연결이 비었을 때만 탐색
        }
    }

    private void LateUpdate()
    {
        if (characterAnimator == null)
        {
            return;
        }

        Vector3 velocity = characterController.velocity; // 실제로 움직인 속도
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        characterAnimator.SetBool(Moving, horizontalVelocity.sqrMagnitude > 0.01f);
        characterAnimator.SetBool(Grounded, characterController.isGrounded);
        characterAnimator.SetFloat(VerticalSpeed, velocity.y);
    }

    public void PlayPickup()
    {
        PlayOnce(PickUpState);
    }

    public void PlayInteraction()
    {
        PlayOnce(InteractionState);
    }

    private void PlayOnce(int stateHash)
    {
        if (characterAnimator == null)
        {
            return;
        }

        characterAnimator.CrossFadeInFixedTime(stateHash, 0.08f, 0, 0f); // 성공한 동작만 처음부터 재생
    }
}
