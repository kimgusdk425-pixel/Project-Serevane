using TMPro;
using UnityEngine;

public class JumpProgressUI : MonoBehaviour
{
    [SerializeField] private PlayerJumpProgress progress;
    [SerializeField] private TMP_Text label;
    [SerializeField, Min(0f)] private float noticeDuration = 2.5f;
    private float noticeUntil;

    private void OnEnable()
    {
        if (progress != null) progress.Changed += OnProgressChanged;
        Refresh(); // 다시 표시할 때도 현재 수치로 복원
    }

    private void OnDisable()
    {
        if (progress != null) progress.Changed -= OnProgressChanged; // 중복 구독 방지
    }

    private void Update()
    {
        if (noticeUntil > 0f && Time.unscaledTime >= noticeUntil)
        {
            noticeUntil = 0f;
            Refresh();
        }
    }

    private void OnProgressChanged()
    {
        noticeUntil = Time.unscaledTime + noticeDuration; // 연출로 시간이 멈춰도 알림 시간은 진행
        Refresh();
    }

    private void Refresh()
    {
        if (label == null || progress == null) return;
        string notice = noticeUntil > Time.unscaledTime ? "JUMP UP!\n" : "LIGHT FRAGMENT\n";
        if (progress.CollectedFragments >= progress.MaxFragments) notice = "JUMP MAX\n";
        label.text = $"{notice}{progress.CollectedFragments}/{progress.MaxFragments}   Jump +{progress.JumpBonus:0.00}";
    }
}
