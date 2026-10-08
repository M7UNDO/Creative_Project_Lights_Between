using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Settings")]
    [Space(5)]
    [SerializeField] private int maxCapacity = 12;

    [Header("References")]
    [Space(5)]
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private GameObject playerGameObject;

    [Header("UI References")]
    [Space(5)]
    [SerializeField] private Transform slotGridHolder;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI capacityCounterText;

    [Header("Input Prompt UI")]
    [Space(5)]
    [SerializeField] private GameObject actionPromptsContainer;
    [SerializeField] private TextMeshProUGUI usePromptText;

    private readonly List<SlotClass> slots = new List<SlotClass>();
    private readonly List<InventorySlotUI> slotUIComponents = new List<InventorySlotUI>();
    private int selectedIndex = -1;

    public bool HasSelectedItem => selectedIndex >= 0 && selectedIndex < slots.Count;

    private void Start()
    {
        RebuildUI();
    }

    private void Update()
    {
        if (inputHandler == null) return;

        if (inputHandler.use)
        {
            UseSelectedItem();
            inputHandler.use = false;
        }
    }

    public bool AddItem(ItemClass item, int quantity = 1)
    {
        if (item == null) return false;

        if (item.IsStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.Item == item && slot.Quantity < item.StackSize)
                {
                    int spaceAvailable = item.StackSize - slot.Quantity;
                    int amountToAdd = Mathf.Min(quantity, spaceAvailable);
                    slot.AddQuantity(amountToAdd);
                    quantity -= amountToAdd;

                    if (quantity <= 0)
                    {
                        RebuildUI();
                        return true;
                    }
                }
            }
        }

        while (quantity > 0 && slots.Count < maxCapacity)
        {
            int amountToAdd = Mathf.Min(quantity, item.StackSize);
            slots.Add(new SlotClass(item, amountToAdd));
            quantity -= amountToAdd;
        }

        RebuildUI();
        return quantity <= 0;
    }

    public int GetItemCount(ItemClass item)
    {
        if (item == null) return 0;

        int totalCount = 0;
        foreach (var slot in slots)
        {
            if (slot != null && slot.Item == item)
            {
                totalCount += slot.Quantity;
            }
        }

        return totalCount;
    }

    public bool RemoveItem(ItemClass item, int quantity = 1)
    {
        if (item == null) return false;

        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].Item == item)
            {
                if (slots[i].Quantity > quantity)
                {
                    slots[i].SubQuantity(quantity);
                    RebuildUI();
                    return true;
                }
                else
                {
                    quantity -= slots[i].Quantity;
                    slots.RemoveAt(i);
                    if (quantity <= 0)
                    {
                        RebuildUI();
                        return true;
                    }
                }
            }
        }

        RebuildUI();
        return false;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            DeselectAll();
            return;
        }

        selectedIndex = index;
        UpdateSelectionVisuals();
        UpdateDetailsPanel();
    }

    public void UseSelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= slots.Count) return;

        SlotClass selectedSlot = slots[selectedIndex];
        if (selectedSlot?.Item == null) return;

        if (selectedSlot.Item.CanUse(playerGameObject))
        {
            selectedSlot.Item.Use(playerGameObject);
            selectedSlot.SubQuantity(1);

            if (selectedSlot.Quantity <= 0)
            {
                slots.RemoveAt(selectedIndex);
                selectedIndex = Mathf.Clamp(selectedIndex, 0, slots.Count - 1);
            }

            RebuildUI();
        }
    }

    public void DeselectAll()
    {
        selectedIndex = -1;

        for (int i = 0; i < slotUIComponents.Count; i++)
        {
            slotUIComponents[i].SetSelectedState(false);
        }

        if (itemNameText != null) itemNameText.text = string.Empty;
        if (itemDescriptionText != null) itemDescriptionText.text = string.Empty;
        if (actionPromptsContainer != null) actionPromptsContainer.SetActive(false);
    }

    private void RebuildUI()
    {
        foreach (Transform child in slotGridHolder)
        {
            Destroy(child.gameObject);
        }

        slotUIComponents.Clear();

        for (int i = 0; i < slots.Count; i++)
        {
            GameObject instantiatedSlot = Instantiate(slotPrefab, slotGridHolder);
            InventorySlotUI slotUI = instantiatedSlot.GetComponent<InventorySlotUI>();
            slotUI.Setup(this, i);
            slotUI.UpdateUI(slots[i]);
            slotUIComponents.Add(slotUI);
        }

        if (capacityCounterText != null)
        {
            capacityCounterText.text = $"{slots.Count}/{maxCapacity}";
        }

        if (slots.Count == 0)
        {
            DeselectAll();
        }
        else if (selectedIndex >= slots.Count)
        {
            SelectSlot(slots.Count - 1);
        }
        else if (selectedIndex >= 0)
        {
            SelectSlot(selectedIndex);
        }
        else
        {
            DeselectAll();
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < slotUIComponents.Count; i++)
        {
            slotUIComponents[i].SetSelectedState(i == selectedIndex);
        }
    }

    private void UpdateDetailsPanel()
    {
        if (selectedIndex < 0 || selectedIndex >= slots.Count)
        {
            DeselectAll();
            return;
        }

        ItemClass currentItem = slots[selectedIndex].Item;

        if (itemNameText != null) itemNameText.text = currentItem.ItemName;
        if (itemDescriptionText != null) itemDescriptionText.text = currentItem.ItemDescription;

        if (actionPromptsContainer != null) actionPromptsContainer.SetActive(true);
        if (usePromptText != null) usePromptText.text = currentItem.UsePromptText;
    }
}