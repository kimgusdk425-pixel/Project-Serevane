using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimedCollectionChallenge : MonoBehaviour
{
    public event System.Action Completed; // 보상은 별도 컴포넌트가 성공 신호를 받아 처리
    [SerializeField, Min(1f)] private float timeLimit = 20f; // 도전 제한 시간
    [SerializeField, Min(0.1f)] private float resultDisplaySeconds = 3f; // 결과 문구 표시 시간
    [SerializeField] private TMP_Text statusText; // 화면 위쪽 진행 안내

    private readonly List<TimedCollectionPickup> coins = new List<TimedCollectionPickup>();
    private TimedCollectionPickup starter;
    private double endsAt; // UI와 접촉 판정이 함께 사용하는 종료 시각
    private float resultTimeLeft;
    private float retryAvailableAt;
    private int collectedCount;
    private int lastShownSeconds = -1;
    private int lastShownCount = -1;
    private bool running;
    private bool completed;
    private bool awaitingRetry; // 실패 후 안내를 재도전할 때까지 유지

    private void Awake()
    {
        // 자식에서 시작 아이템 하나와 보너스 코인들을 찾아 한 묶음으로 관리합니다.
        foreach (TimedCollectionPickup pickup in GetComponentsInChildren<TimedCollectionPickup>(true))
        {
            if (pickup.IsStarter)
            {
                if (starter != null)
                {
                    Debug.LogError("시간제 수집 도전에는 시작 아이템이 하나만 필요합니다.", this);
                    enabled = false;
                    return;
                }

                starter = pickup;
            }
            else
            {
                coins.Add(pickup);
            }
        }

        if (starter == null || coins.Count == 0 || statusText == null)
        {
            Debug.LogError("시간제 수집 도전의 시작 아이템, 코인 또는 UI 연결을 확인하세요.", this);
            enabled = false;
            return;
        }

        // 시작 전에는 코인과 진행 안내를 감춰 발견 순서를 분명히 합니다.
        foreach (TimedCollectionPickup coin in coins)
        {
            coin.gameObject.SetActive(false);
        }

        statusText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (running)
        {
            if (Time.timeAsDouble >= endsAt)
            {
                Fail();
            }
            else
            {
                ShowProgress();
            }
        }
        else if (!awaitingRetry && resultTimeLeft > 0f)
        {
            resultTimeLeft -= Time.deltaTime;
            if (resultTimeLeft <= 0f)
            {
                statusText.gameObject.SetActive(false);
            }
        }
    }

    public void TryStart()
    {
        if (!enabled || running || completed || Time.time < retryAvailableAt)
        {
            return; // 실패 직후의 자동 재접촉과 중복 시작을 막음
        }

        running = true;
        awaitingRetry = false;
        endsAt = Time.timeAsDouble + Mathf.Max(1f, timeLimit); // 기존 scaled time과 일시정지 규칙 유지
        resultTimeLeft = 0f;
        collectedCount = 0;
        lastShownSeconds = -1;
        lastShownCount = -1;
        starter.gameObject.SetActive(false);

        foreach (TimedCollectionPickup coin in coins)
        {
            coin.gameObject.SetActive(true);
        }

        statusText.gameObject.SetActive(true);
        ShowProgress();
    }

    public void Collect(TimedCollectionPickup coin)
    {
        if (!isActiveAndEnabled || !running) return;
        if (Time.timeAsDouble >= endsAt)
        {
            Fail(); // Update보다 접촉이 먼저 와도 만료 후 성공으로 바뀌지 않음
            return;
        }
        if (coin == null || !coins.Contains(coin) || !coin.gameObject.activeSelf)
        {
            return; // 시작 전이거나 이미 먹은 코인은 세지 않음
        }

        coin.gameObject.SetActive(false);
        collectedCount++;

        if (collectedCount == coins.Count)
        {
            running = false;
            completed = true;
            ShowResult("SUCCESS!");
            Completed?.Invoke(); // 최초 성공에서만 알림: 재시도로 보상 복제 방지
        }
        else
        {
            ShowProgress();
        }
    }

    private void Fail()
    {
        running = false;
        awaitingRetry = true;
        retryAvailableAt = Time.time + 0.5f; // 제자리에서 즉시 다시 시작되지 않도록 잠시 잠금

        foreach (TimedCollectionPickup coin in coins)
        {
            coin.gameObject.SetActive(false);
        }

        starter.gameObject.SetActive(true); // 다시 닿으면 재도전 가능
        ShowResult("TIME UP\nSTEP AWAY AND TOUCH AGAIN"); // 나갔다 다시 닿아야 하는 규칙을 명시
    }

    private void ShowProgress()
    {
        int shownSeconds = Mathf.CeilToInt((float)System.Math.Max(0d, endsAt - Time.timeAsDouble));
        if (shownSeconds == lastShownSeconds && collectedCount == lastShownCount)
        {
            return; // 숫자가 그대로면 TMP 글자를 다시 그리지 않음
        }

        lastShownSeconds = shownSeconds;
        lastShownCount = collectedCount;
        statusText.text = $"BONUS  {shownSeconds}s     {collectedCount}/{coins.Count}";
    }

    private void ShowResult(string message)
    {
        statusText.text = message;
        statusText.gameObject.SetActive(true);
        resultTimeLeft = resultDisplaySeconds;
    }
}
