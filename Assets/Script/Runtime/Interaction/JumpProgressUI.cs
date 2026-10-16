using TMPro;
using UnityEngine;

public class JumpProgressUI : MonoBehaviour
{
    [SerializeField] private PlayerJumpProgress progress;
    [SerializeField] private TMP_Text label;
    [SerializeField] private ExplorationSceneExit exitRequirement;
    [SerializeField, Min(0f)] private float noticeDuration = 2.5f;
    private float noticeUntil, pulseUntil;
    private bool milestoneNotice;
    private Color originalColor;
    private const float PulseSeconds = .55f;

    private void Awake()
    {
        if (label != null) originalColor = label.color;
    }

    private void OnEnable()
    {
        if (progress != null)
        {
            progress.Changed += Refresh;
            progress.FragmentCollected += OnFragmentCollected;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (progress != null)
        {
            progress.Changed -= Refresh;
            progress.FragmentCollected -= OnFragmentCollected;
        }
        noticeUntil = pulseUntil = 0f;
        if (label != null) label.color = originalColor;
    }

    private void Update()
    {
        if (pulseUntil > 0f && label != null)
        {
            float remaining = Mathf.Max(0f, pulseUntil - Time.unscaledTime);
            float strength = Mathf.Sin(Mathf.PI * remaining / PulseSeconds);
            Color gold = new Color(1f, .83f, .4f, originalColor.a);
            label.color = Color.Lerp(originalColor, gold, strength);
            if (remaining <= 0f) pulseUntil = 0f;
        }
        if (noticeUntil > 0f && Time.unscaledTime >= noticeUntil)
        {
            noticeUntil = 0f;
            Refresh();
        }
    }

    private void OnFragmentCollected()
    {
        int count = progress.CollectedFragments;
        milestoneNotice = count % 4 == 0 ||
            (exitRequirement != null && count == exitRequirement.RequiredFragments);
        noticeUntil = Time.unscaledTime + noticeDuration;
        if (milestoneNotice) pulseUntil = Time.unscaledTime + PulseSeconds;
        Refresh(); // 실제 수집에만 반응하며 씬 전환의 수치 복원에는 강조하지 않음
    }

    private void Refresh()
    {
        if (label == null || progress == null) return;
        bool showingNotice = noticeUntil > Time.unscaledTime;
        string bonus = showingNotice ? (milestoneNotice ? "도약 강화!" : "점프 강화!") : "점프";
        if (exitRequirement != null && exitRequirement.RequiredFragments > 0)
        {
            int required = exitRequirement.RequiredFragments;
            bool ready = progress.CollectedFragments >= required;
            string goal = ready ? "출구 준비 완료" : $"출구 준비 {progress.CollectedFragments}/{required}";
            label.text = $"{goal}\n강화 {progress.CollectedFragments}개\n{bonus} +{progress.JumpBonus:0.00}";
        }
        else
        {
            string notice = showingNotice ? bonus : "빛 조각";
            label.text = $"{notice}\n{progress.CollectedFragments}개   점프 +{progress.JumpBonus:0.00}";
        }
    }
}
