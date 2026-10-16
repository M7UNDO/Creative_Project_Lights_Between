using UnityEngine;
using UnityEngine.UI;

public class DrawingUIController : MonoBehaviour
{
    public static DrawingUIController Instance { get; private set; }

    [SerializeField] private InputHandler _input;
    [SerializeField] private FPSController playerController;
    private PlayerInteraction playerInteraction;

    [Header("UI References")]
    [SerializeField] private GameObject drawingPanelUI;
    [SerializeField] private Image portraitImageDisplay;
    [SerializeField] private Image landscapeImageDisplay;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openDrawingSound;
    [SerializeField] private AudioClip closeDrawingSound;

    public static bool isDrawingOpen { get; private set; }
    public bool IsDrawingOpen => isDrawingOpen;

    private InspectableObject pendingInspectable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (drawingPanelUI != null)
        {
            drawingPanelUI.SetActive(false);
        }
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FPSController>();
        }

        if (playerController != null)
        {
            playerInteraction = playerController.GetComponent<PlayerInteraction>();
        }
    }

    private void Update()
    {
        if (isDrawingOpen && _input != null && _input.close)
        {
            CloseDrawing();
            _input.close = false;
        }
    }

    public void OpenDrawing(Sprite drawingSprite, InspectableObject sourceObject, InspectableObject inspectableOnClose = null)
    {
        if (drawingPanelUI == null || drawingSprite == null) return;

        pendingInspectable = inspectableOnClose;

        bool usePortrait = sourceObject != null ? sourceObject.IsPortrait : true;
        if (sourceObject != null && sourceObject.IsLandscape)
        {
            usePortrait = false;
        }

        if (portraitImageDisplay != null)
        {
            portraitImageDisplay.gameObject.SetActive(usePortrait);
            if (usePortrait) portraitImageDisplay.sprite = drawingSprite;
        }

        if (landscapeImageDisplay != null)
        {
            landscapeImageDisplay.gameObject.SetActive(!usePortrait);
            if (!usePortrait) landscapeImageDisplay.sprite = drawingSprite;
        }

        drawingPanelUI.SetActive(true);
        isDrawingOpen = true;

        if (playerInteraction != null)
        {
            playerInteraction.EvaluateHUDState();
        }

        if (audioSource != null && openDrawingSound != null)
        {
            audioSource.PlayOneShot(openDrawingSound);
        }

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(true);
        }
    }

    public void CloseDrawing()
    {
        if (!isDrawingOpen) return;

        if (drawingPanelUI != null)
        {
            drawingPanelUI.SetActive(false);
        }

        isDrawingOpen = false;

        if (playerInteraction != null)
        {
            playerInteraction.EvaluateHUDState();
        }

        if (audioSource != null && closeDrawingSound != null)
        {
            audioSource.PlayOneShot(closeDrawingSound);
        }

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(false);
        }

        if (pendingInspectable != null)
        {
            InspectableObject target = pendingInspectable;
            pendingInspectable = null;

            if (InspectionManager.Instance != null)
            {
                InspectionManager.Instance.StartInspection(target);
            }
        }
    }
}