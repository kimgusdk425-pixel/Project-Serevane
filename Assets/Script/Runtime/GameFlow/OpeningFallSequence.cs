using System.Collections;
using UnityEngine;

public class OpeningFallSequence : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Transform landingCheckpoint;

    private IEnumerator Start()
    {
        if (player == null || landingCheckpoint == null) yield break;
        player.SetRespawnPoint(landingCheckpoint.position, landingCheckpoint.rotation);
        player.SetControlEnabled(false); // 시작 낙하 중에는 중력만 적용
        CharacterController body = player.GetComponent<CharacterController>();
        while (!body.isGrounded) yield return null;
        yield return new WaitForSeconds(0.25f);
        player.SetControlEnabled(true); // 착지 뒤 이동 시작
    }
}
