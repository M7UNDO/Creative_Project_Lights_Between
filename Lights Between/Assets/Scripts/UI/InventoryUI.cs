using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    public static bool IsInventoryOpen { get; private set; }

    [Header("References")]
    [Space(5)]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Pickup Notification Panel")]
    [Space(5)]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Image notificationIconImage;
    [SerializeField] private TextMeshProUGUI notificationNameText;

    [Header("Notification Settings")]
    [Space(5)]
    [SerializeField] private float notificationDisplayDuration = 2f;
    [SerializeField] private float notificationFadeDuration = 0.35f;

    [Header("Audio")]
    [Space(5)]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip closeSFX;

    private CanvasGroup notificationCanvasGroup;
    private readonly Queue<PickupData> notificationQueue = new Queue<PickupData>();
    private Coroutine activeNotificationCoroutine;

    private struct PickupData
    {
        public string ItemName;
        public Sprite ItemIcon;
        public int Quantity;

        public PickupData(string name, Sprite icon, int quantity)
        {
            ItemName = name;
            ItemIcon = icon;
            Quantity = quantity;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (notificationPanel != null)
        {
            if (!notificationPanel.TryGetComponent(out notificationCanvasGroup))
            {
                notificationCanvasGroup = notificationPanel.AddComponent<CanvasGroup>();
            }

            notificationCanvasGroup.alpha = 0f;
            notificationPanel.SetActive(false);
        }
    }

    private void Start()
    {
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

    public void ShowPickupNotification(string itemName, Sprite itemIcon, int quantity = 1)
    {
        notificationQueue.Enqueue(new PickupData(itemName, itemIcon, quantity));

        if (activeNotificationCoroutine == null)
        {
            activeNotificationCoroutine = StartCoroutine(ProcessNotificationQueue());
        }
    }

    public void ShowPickupNotification(ItemClass item, int quantity = 1)
    {
        if (item == null) return;
        ShowPickupNotification(item.ItemName, item.ItemIcon, quantity);
    }

    private IEnumerator ProcessNotificationQueue()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
        }

        while (notificationQueue.Count > 0)
        {
            PickupData currentPickup = notificationQueue.Dequeue();

            if (notificationNameText != null)
            {
                notificationNameText.text = currentPickup.ItemName;
            }

            if (notificationIconImage != null)
            {
                notificationIconImage.sprite = currentPickup.ItemIcon;
                notificationIconImage.enabled = currentPickup.ItemIcon != null;
            }

            yield return StartCoroutine(FadeNotificationCanvasGroup(0f, 1f, notificationFadeDuration));

            yield return new WaitForSecondsRealtime(notificationDisplayDuration);

            yield return StartCoroutine(FadeNotificationCanvasGroup(1f, 0f, notificationFadeDuration));
        }

        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }

        activeNotificationCoroutine = null;
    }

    private IEnumerator FadeNotificationCanvasGroup(float startAlpha, float targetAlpha, float duration)
    {
        if (notificationCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            notificationCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        notificationCanvasGroup.alpha = targetAlpha;
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

    public bool HasSelectedItem()
    {
        return inventoryManager != null && inventoryManager.HasSelectedItem;
    }

    public void DeselectItem()
    {
        if (inventoryManager != null)
        {
            inventoryManager.DeselectAll();
        }
    }

    private void PlayCloseSFX()
    {
        if (audioSource != null && closeSFX != null && IsInventoryOpen)
        {
            audioSource.PlayOneShot(closeSFX);
        }
    }
}