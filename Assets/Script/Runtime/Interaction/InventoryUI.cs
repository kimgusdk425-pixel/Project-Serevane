using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;

    private GameObject itemPanel;
    private TMP_Text itemText;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
        }

        BuildTemporaryItemPanel();
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.ItemChanged += Refresh;
            Refresh(inventory.CurrentItem);
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.ItemChanged -= Refresh;
        }
    }

    private void BuildTemporaryItemPanel()
    {
        // 기존 DemoEndCanvas 아래에 임시 UI를 코드로 생성합니다.
        // 나중에 아이콘 디자인이 정해지면 실제 UI 프리팹으로 교체할 수 있습니다.
        itemPanel = new GameObject("InventoryItemPanel", typeof(RectTransform), typeof(Image));
        itemPanel.transform.SetParent(transform, false);

        RectTransform panelRect = itemPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-24f, 24f);
        panelRect.sizeDelta = new Vector2(190f, 54f);

        Image background = itemPanel.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject textObject = new GameObject(
            "ItemText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(itemPanel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        itemText = textObject.GetComponent<TextMeshProUGUI>();
        itemText.alignment = TextAlignmentOptions.Center;
        itemText.fontSize = 20f;
        itemText.color = Color.white;
        itemText.text = "[ KEY ]";

        // 시작할 때는 아이템이 없으므로 패널을 숨깁니다.
        itemPanel.SetActive(false);
    }

    private void Refresh(ItemType currentItem)
    {
        if (itemPanel == null || itemText == null)
        {
            return;
        }

        bool hasItem = currentItem != ItemType.None;
        itemPanel.SetActive(hasItem);

        if (currentItem == ItemType.Key)
        {
            itemText.text = "[ KEY ]  열쇠";
        }
    }
}
