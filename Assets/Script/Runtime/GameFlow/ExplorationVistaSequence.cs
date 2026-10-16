using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ExplorationVistaSequence : MonoBehaviour
{
    [SerializeField] private LockedDoor entranceDoor;
    [SerializeField] private CameraFollow cameraFollow;
    private PlayerMovement activePlayer;
    private bool hasPlayed;
    public bool HasPlayed => hasPlayed;
    public bool IsPlaying => activePlayer != null;

    private void OnTriggerStay(Collider other)
    {
        if (hasPlayed || entranceDoor == null || !entranceDoor.IsOpen || cameraFollow == null) return;
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.IsControlEnabled || !player.GetComponent<CharacterController>().isGrounded) return;
        if (Vector3.Dot(player.transform.position - transform.position, transform.forward) < 0f) return;
        hasPlayed = true; // 문을 지난 뒤 지상에서 한 번만 재생
        activePlayer = player;
        StartCoroutine(Present());
    }

    private IEnumerator Present()
    {
        activePlayer.SetControlEnabled(false);
        try
        {
            yield return cameraFollow.PlayMapRevealView();
        }
        finally
        {
            Restore();
        }
    }

    private void Restore()
    {
        if (cameraFollow != null) cameraFollow.CancelMapRevealView();
        if (activePlayer != null) activePlayer.SetControlEnabled(true);
        activePlayer = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        Restore(); // 중단·씬 종료에도 조작 잠금을 남기지 않음
    }
}