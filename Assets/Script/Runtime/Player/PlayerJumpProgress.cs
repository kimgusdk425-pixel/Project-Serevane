using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerJumpProgress : MonoBehaviour
{
    [SerializeField, Min(0f)] private float bonusPerFragment = 0.4f; // 조각 하나가 올리는 점프 높이
    private int collectedFragments; // 현재 실행 중의 성장 기록: 위치 재시작으로 초기화하지 않음

    public int CollectedFragments => collectedFragments;
    public float JumpBonus => collectedFragments * Mathf.Max(0f, bonusPerFragment);
    public event Action FragmentCollected; // 복원과 구분되는 실제 강화 획득
    public event Action Changed; // UI에 변경만 알리고 UI를 직접 다루지 않음

    public void RestoreFragments(int count)
    {
        collectedFragments = Mathf.Max(0, count);
        Changed?.Invoke();
    }

    public bool TryCollectFragment()
    {
        if (collectedFragments == int.MaxValue) return false; // 정수 범위 초과만 방지
        collectedFragments++;
        Changed?.Invoke(); // 수치가 바뀐 뒤 알림
        FragmentCollected?.Invoke();
        return true;
    }
}
