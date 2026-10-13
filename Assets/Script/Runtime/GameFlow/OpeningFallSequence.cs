using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        float elapsed = 0f;
        while (!body.isGrounded && elapsed < 8f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (!body.isGrounded)
        {
            player.Respawn(); // 시작 착지를 놓쳤을 때 지정 체크포인트로 한 번 복구
            elapsed = 0f;
            while (!body.isGrounded && elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (!body.isGrounded)
            {
                Debug.LogWarning("OpeningFallSequence: 복구 착지가 실패하여 현재 구간을 다시 시작합니다.", this);
                string sceneName = SceneManager.GetActiveScene().name;
                if (Application.CanStreamedLevelBeLoaded(sceneName))
                    yield return SceneManager.LoadSceneAsync(sceneName);
                else
                    player.SetControlEnabled(true);
                yield break;
            }
        }
        yield return new WaitForSeconds(0.25f);
        player.SetControlEnabled(true); // 착지 뒤 이동 시작
    }
}
