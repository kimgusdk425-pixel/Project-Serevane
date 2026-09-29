using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemInteraction : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerCarryController carryController;
    [SerializeField] private float interactionRange = 2.2f;

    private InputSystem_Actions inputActions;
    private PlayerAnimationDriver animationDriver;
    private readonly Collider[] interactionHits = new Collider[16];
    private bool interactWasHeld;
    private bool carryIsNearest;

    public string CurrentPrompt { get; private set; } = string.Empty;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        animationDriver = GetComponent<PlayerAnimationDriver>(); // 성공했을 때만 동작 재생

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (carryController == null)
        {
            carryController = GetComponent<PlayerCarryController>();
        }

        if (carryController != null)
        {
            // E 키는 이 컴포넌트 하나만 판단하게 합니다.
            // 그래야 상자와 열쇠를 같은 프레임에 동시에 처리하지 않습니다.
            carryController.SetDirectInputEnabled(false);
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }

    private void Update()
    {
        ItemInteractionTarget target = FindNearestTarget();
        float targetDistance = GetTargetDistance(target);
        float carryDistance = carryController == null
            ? float.MaxValue
            : carryController.GetNearestCarryableDistance();

        carryIsNearest = carryController != null && carryDistance < targetDistance;

        if (carryIsNearest)
        {
            CurrentPrompt = carryController.IsHolding
                ? "[ E ]  PUT DOWN"
                : "[ E ]  PICK UP";
        }
        else
        {
            CurrentPrompt = target == null
                ? string.Empty
                : target.GetInteractionPrompt(inventory);
        }

        bool interactHeld = inputActions.Player.Interact.IsPressed();

        // E 키를 직접 확인하는 보조 경로입니다.
        // 한 프레임 입력을 놓치지 않도록 눌림 상태를 기억합니다.
        if (Keyboard.current != null)
        {
            interactHeld |= Keyboard.current.eKey.isPressed;
        }

        bool interactPressed = interactHeld && !interactWasHeld;
        interactWasHeld = interactHeld;

        if (interactPressed && carryIsNearest)
        {
            CarryInteractionResult result = carryController.TryInteract();

            if (animationDriver != null)
            {
                if (result == CarryInteractionResult.PickedUp)
                {
                    animationDriver.PlayPickup();
                }
                else if (result == CarryInteractionResult.PutDown)
                {
                    animationDriver.PlayInteraction();
                }
            }
        }
        else if (target != null && interactPressed)
        {
            ItemInteractionResult result = target.Interact(inventory);

            if (animationDriver == null)
            {
                return; // 애니메이션이 없어도 아이템과 문 동작은 유지
            }

            if (result == ItemInteractionResult.ItemCollected)
            {
                animationDriver.PlayPickup();
            }
            else if (result == ItemInteractionResult.DoorOpened)
            {
                animationDriver.PlayInteraction();
            }
        }
    }

    private ItemInteractionTarget FindNearestTarget()
    {
        // 상자를 들고 있으면 손에 든 상자 놓기만 허용합니다.
        // 문이나 열쇠와 E 입력이 겹쳐 실행되는 것을 막습니다.
        if (carryController != null && carryController.IsHolding)
        {
            carryIsNearest = true;
            return null;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRange,
            interactionHits,
            ~0,
            QueryTriggerInteraction.Ignore);

        ItemInteractionTarget nearest = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            ItemInteractionTarget target =
                interactionHits[i].GetComponentInParent<ItemInteractionTarget>();

            if (target == null)
            {
                continue;
            }

            float distance = (target.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }

    private float GetTargetDistance(ItemInteractionTarget target)
    {
        if (target == null)
        {
            return float.MaxValue;
        }

        return (target.transform.position - transform.position).sqrMagnitude;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
