using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    [Header("UI References", order = 0)]
    [Space(5)]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private GameObject selectionBorder;
    [SerializeField] private Button slotButton;

    private SlotClass currentSlot;
    private InventoryManager inventoryManager;
    private int slotIndex;

    public void Setup(InventoryManager manager, int index)
    {
        inventoryManager = manager;
        slotIndex = index;

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    public void UpdateUI(SlotClass slot)
    {
        currentSlot = slot;

        if (slot == null || slot.Item == null)
        {
            itemIconImage.enabled = false;
            countText.text = string.Empty;
            return;
        }

        itemIconImage.enabled = true;
        itemIconImage.sprite = slot.Item.ItemIcon;

        if (slot.Item.IsStackable && slot.Quantity > 1)
        {
            countText.text = slot.Quantity.ToString();
        }
        else
        {
            countText.text = string.Empty;
        }
    }

    public void SetSelectedState(bool isSelected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.SetActive(isSelected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked();
    }

    public void OnSelect(BaseEventData eventData)
    {
        OnSlotClicked();
    }

    public void OnDeselect(BaseEventData eventData)
    {
    }

    private void OnSlotClicked()
    {
        inventoryManager.SelectSlot(slotIndex);
    }
}