using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ToolWheelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSController fpsController;
    [SerializeField] private InputHandler input;
    [SerializeField] private GameObject hud;
    [SerializeField] private PlayerToolInventory inventory;
    [SerializeField] private ToolEquipmentManager equipmentManager;
    [SerializeField] private GameObject radialWheelUI;
    [SerializeField] private ToolSlot[] slots;
    [SerializeField] private TextMeshProUGUI centerItemNameText;

    [Header("Settings")]
    [SerializeField] private bool slowTimeWhenOpen = true;
    [SerializeField] private float slowedTimeScale = 0.2f;
    [SerializeField] private float inputDeadZone = 0.35f;
    [SerializeField] private float mouseDeadZone = 80f;

    [Header("Unequip Slot")]
    [SerializeField] private int unequipSlotIndex = 0;
    [SerializeField] private Sprite unequipIcon;

    private bool isWheelOpen;
    private int selectedIndex = -1;
    private int lastSelectedIndex = -1;

    [Header("SFX")]
    [SerializeField] private AudioSource toolWheelSource;
    public AudioClip SlotHover;
    public AudioClip SlotRelease;
    [SerializeField] private AudioClip wheelOpen;
    [SerializeField] private AudioClip wheelClose;

    private void Awake()
    {
        if (input == null) input = GetComponent<InputHandler>();
        if (fpsController == null) fpsController = GetComponent<FPSController>();
        if (inventory == null) inventory = GetComponent<PlayerToolInventory>();
        if (equipmentManager == null) equipmentManager = GetComponent<ToolEquipmentManager>();

        if (radialWheelUI != null)
        {
            radialWheelUI.SetActive(false);
        }

        UpdateCenterText(string.Empty);
    }

    private void Update()
    {
        if (input == null) return;

        if (input.wheel && !isWheelOpen)
        {
            OpenWheel();
            if (hud != null) hud.SetActive(false);
        }
        else if (!input.wheel && isWheelOpen)
        {
            CloseWheel();
            if (hud != null) hud.SetActive(true);
        }

        if (isWheelOpen)
        {
            UpdateSelection();
        }
    }

    private void OpenWheel()
    {
        isWheelOpen = true;
        selectedIndex = -1;
        lastSelectedIndex = -1;

        if (radialWheelUI != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(true);
            radialWheelUI.SetActive(true);

            if (toolWheelSource != null && wheelOpen != null)
            {
                toolWheelSource.PlayOneShot(wheelOpen);
            }
        }

        RefreshWheel();

       /* Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;/

        input.cursorInputForLook = false;*/
        input.blockLookInput = true;
        input.ClearLookInput();

        if (slowTimeWhenOpen)
        {
            Time.timeScale = slowedTimeScale;
        }
    }

    private void CloseWheel()
    {
        isWheelOpen = false;

        int indexToUse = lastSelectedIndex;

        if (indexToUse >= 0 && indexToUse < slots.Length)
        {
            ToolClass selectedTool = slots[indexToUse].Tool;

            if (selectedTool != null)
            {
                equipmentManager.EquipTool(selectedTool);
            }
            else
            {
                equipmentManager.UnequipTool();
            }
        }

        ClearHighlights();
        UpdateCenterText(string.Empty);

        if (radialWheelUI != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(false);
            radialWheelUI.SetActive(false);

            if (toolWheelSource != null)
            {
                if (SlotRelease != null && indexToUse >= 0)
                {
                    toolWheelSource.PlayOneShot(SlotRelease);
                }
                else if (wheelClose != null)
                {
                    toolWheelSource.PlayOneShot(wheelClose);
                }
            }
        }

        /*Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        input.cursorInputForLook = true;*/
        input.blockLookInput = false;
        input.ClearLookInput();

        if (slowTimeWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }

    private void RefreshWheel()
    {
        if (slots == null || slots.Length == 0) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i == unequipSlotIndex)
            {
                slots[i].SetupAsUnequipSlot(unequipIcon);
            }
            else
            {
                slots[i].ClearButKeepVisible();
            }
        }

        if (inventory == null) return;

        foreach (ToolClass tool in inventory.AcquiredTools)
        {
            if (tool == null) continue;

            int index = tool.fixedRadialSlotIndex;
            if (index >= 0 && index < slots.Length && index != unequipSlotIndex)
            {
                if (slots[index] != null)
                {
                    slots[index].Setup(tool);
                }
            }
        }
    }

    private void UpdateSelection()
    {
        if (slots == null || slots.Length == 0) return;

        Vector2 direction = GetSelectionDirection();

        if (direction.magnitude < inputDeadZone)
        {
            selectedIndex = -1;
            ClearHighlights();
            UpdateCenterText(string.Empty);
            return;
        }

        direction.Normalize();

        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        if (angle < 0f)
        {
            angle += 360f;
        }

        float anglePerSlot = 360f / slots.Length;
        int newSelectedIndex = Mathf.RoundToInt(angle / anglePerSlot) % slots.Length;

        if (newSelectedIndex != selectedIndex)
        {
            selectedIndex = newSelectedIndex;
            lastSelectedIndex = selectedIndex;

            ClearHighlights();
            slots[selectedIndex].SetHighlight(true);
            PlaySlotHoverSound();

            if (slots[selectedIndex].Tool != null)
            {
                UpdateCenterText(slots[selectedIndex].Tool.toolName);
            }
            else if (selectedIndex == unequipSlotIndex)
            {
                UpdateCenterText("Unequip");
            }
            else
            {
                UpdateCenterText(string.Empty);
            }
        }
    }

    private Vector2 GetSelectionDirection()
    {
        if (fpsController.IsCurrentDeviceMouse)
        {
            if (Mouse.current == null) return Vector2.zero;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mouseDirection = mousePos - screenCenter;

            if (mouseDirection.magnitude < mouseDeadZone)
            {
                return Vector2.zero;
            }

            return mouseDirection.normalized;
        }

        return input.wheelNavigate;
    }

    private void ClearHighlights()
    {
        if (slots == null) return;

        foreach (ToolSlot slot in slots)
        {
            if (slot != null)
            {
                slot.SetHighlight(false);
            }
        }
    }

    private void UpdateCenterText(string text)
    {
        if (centerItemNameText != null)
        {
            centerItemNameText.text = text;
        }
    }

    public void PlaySlotHoverSound()
    {
        if (toolWheelSource != null && SlotHover != null)
        {
            toolWheelSource.PlayOneShot(SlotHover);
        }
    }
}