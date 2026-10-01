using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerJumpProgress : MonoBehaviour
{
    [SerializeField, Min(0f)] private float bonusPerFragment = 0.4f; // 조각 하나가 올리는 점프 높이
    [SerializeField, Min(1)] private int maxFragments = 3; // 조각 세 개를 모으면 최대 강화
    private int collectedFragments; // 현재 실행 중의 성장 기록: 위치 재시작으로 초기화하지 않음

    public int CollectedFragments => collectedFragments;
    public int MaxFragments => Mathf.Max(1, maxFragments);
    public float JumpBonus => collectedFragments * Mathf.Max(0f, bonusPerFragment);
    public event Action Changed; // UI에 변경만 알리고 UI를 직접 다루지 않음

    public bool TryCollectFragment()
    {
        if (collectedFragments >= MaxFragments) return false; // 최대 단계에서는 소비하지 않음
        collectedFragments++;
        Changed?.Invoke(); // 수치가 바뀐 뒤 알림
        return true;
    }
}
