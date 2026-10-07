using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static bool IsInventoryOpen { get; private set; }

    [Header("References")]
    [Space(5)]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Audio")]
    [Space(5)]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip closeSFX;

    private void Start()
    {
        if (inputHandler == null)
        {
            inputHandler = FindFirstObjectByType<InputHandler>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        CloseInventory();
    }

    private void Update()
    {
        if (inputHandler == null) return;

        if (inputHandler.inventory)
        {
            ToggleInventory();
            inputHandler.inventory = false;
        }

        if (IsInventoryOpen && inputHandler.back)
        {
            HandleBackInput();
            inputHandler.back = false;
        }
    }

    public void ToggleInventory()
    {
        if (PauseManager.IsGamePaused) return;

        if (IsInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        if (inventoryPanel == null || PauseManager.IsGamePaused) return;

        IsInventoryOpen = true;
        inventoryPanel.SetActive(true);

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseInventory()
    {
        PlayCloseSFX();

        IsInventoryOpen = false;

        if (inventoryManager != null)
        {
            inventoryManager.DeselectAll();
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        if (!PauseManager.IsGamePaused)
        {
            Time.timeScale = 1f;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void HandleBackInput()
    {
        if (inventoryManager != null && inventoryManager.HasSelectedItem)
        {
            inventoryManager.DeselectAll();
            return;
        }

        CloseInventory();
    }

    private void PlayCloseSFX()
    {
        if (audioSource != null && closeSFX != null && IsInventoryOpen)
        {
            audioSource.PlayOneShot(closeSFX);
        }
    }
}