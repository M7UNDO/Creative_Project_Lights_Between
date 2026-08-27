using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    [Header("References")]
    [SerializeField] private RectTransform target;
    [SerializeField] private Image leftBorder;
    [SerializeField] private Selectable selectable;

    [Header("Movement")]
    [SerializeField] private float hoverOffset = 10f;
    [SerializeField] private float lerpSpeed = 12f;

    [Header("Border")]
    [SerializeField] private Color borderColour = Color.white;

    [Header("UI SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip clickSFX;

    private float startPosX;
    private float targetPosX;

    private Color hiddenColour;
    private bool isSelected;

    // Optional fields initialized dynamically by the TabController if this is a tab
    private TabController associatedTabController;
    private int tabIndex = -1;

    private void Reset()
    {
        target = transform as RectTransform;
        selectable = GetComponent<Selectable>();
    }

    private void Awake()
    {
        target ??= transform as RectTransform;
        selectable ??= GetComponent<Selectable>();

        hiddenColour = borderColour;
        hiddenColour.a = 0f;

        // Safe check to find child image only if children exist and it wasn't manually assigned
        if (leftBorder == null && transform.childCount > 0)
        {
            leftBorder = transform.GetChild(0).GetComponent<Image>();
        }

        if (leftBorder)
            leftBorder.color = hiddenColour;
    }

    private void Start()
    {
        startPosX = target.anchoredPosition.x;
        targetPosX = startPosX;
    }

    /// <summary>
    /// Called automatically by the TabController script to register this button as an active tab element.
    /// Non-tab buttons will never call this, safely leaving the controller reference null.
    /// </summary>
    public void SetupTabController(TabController controller, int index)
    {
        associatedTabController = controller;
        tabIndex = index;
    }

    private void Update()
    {
        Vector2 currentPos = target.anchoredPosition;

        currentPos.x = Mathf.Lerp(
            currentPos.x,
            targetPosX,
            Time.unscaledDeltaTime * lerpSpeed
        );

        target.anchoredPosition = currentPos;

        if (leftBorder)
        {
            leftBorder.color = Color.Lerp(
                leftBorder.color,
                isSelected ? borderColour : hiddenColour,
                Time.unscaledDeltaTime * lerpSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!selectable || !selectable.interactable || !EventSystem.current)
            return;

        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleButtonActivation();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        targetPosX = startPosX + hoverOffset;

        PlaySFX(hoverSFX);

        // Optional: Only update tab text color if this button belongs to a TabController
        if (associatedTabController != null)
        {
            associatedTabController.UpdateTabTextHoverState(tabIndex, true);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        targetPosX = startPosX;

        // Optional: Only revert tab text color if this button belongs to a TabController
        if (associatedTabController != null)
        {
            associatedTabController.UpdateTabTextHoverState(tabIndex, false);
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        HandleButtonActivation();
    }

    private void HandleButtonActivation()
    {
        PlaySFX(clickSFX);

        // Only tell a TabController to swap pages if it exists
        if (associatedTabController != null)
        {
            associatedTabController.ActivateTab(tabIndex);
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
    }
}