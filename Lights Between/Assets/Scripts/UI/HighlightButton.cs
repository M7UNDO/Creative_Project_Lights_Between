using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HighlightButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler,
    IPointerClickHandler,
    ISubmitHandler
{
    [Header("References")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Selectable selectable;

    [Header("Normal Colours")]
    [SerializeField] private Color normalBackgroundColour = Color.white;
    [SerializeField] private Color normalTextColour = Color.black;

    [Header("Highlight Colours")]
    [SerializeField] private Color highlightBackgroundColour = Color.black;
    [SerializeField] private Color highlightTextColour = Color.white;

    [Header("UI SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip clickSFX;

    [Header("Input Icon")]
    public bool hasInputIcon;
    public Image inputIcon;

    private void Reset()
    {
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        selectable = GetComponent<Selectable>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (selectable == null)
            selectable = GetComponent<Selectable>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        ApplyNormalColours();

        if (hasInputIcon && inputIcon != null)
        {
            inputIcon.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ApplyNormalColours();

        if (hasInputIcon && inputIcon != null)
        {
            inputIcon.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable)
            return;

        // Syncs mouse hover with EventSystem selection.
        // This triggers OnDeselect on the previously selected button 
        // and OnSelect on this button automatically.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Optional: clear selection if you want hovering off the menu to remove focus entirely.
        // Leave empty if you prefer keeping the last hovered button focused.
    }

    public void OnSelect(BaseEventData eventData)
    {
        HandleHighlight();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HandleUnhighlight();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable)
            return;

        PlaySFX(clickSFX);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (selectable != null && !selectable.interactable)
            return;

        PlaySFX(clickSFX);
    }

    private void HandleHighlight()
    {
        if (selectable != null && !selectable.interactable)
            return;

        ApplyHighlightColours();
        PlaySFX(hoverSFX);

        if (hasInputIcon && inputIcon != null)
        {
            inputIcon.gameObject.SetActive(true);
        }
    }

    private void HandleUnhighlight()
    {
        ApplyNormalColours();

        if (hasInputIcon && inputIcon != null)
        {
            inputIcon.gameObject.SetActive(false);
        }
    }

    private void ApplyNormalColours()
    {
        if (buttonImage != null)
            buttonImage.color = normalBackgroundColour;

        if (buttonText != null)
            buttonText.color = normalTextColour;
    }

    private void ApplyHighlightColours()
    {
        if (buttonImage != null)
            buttonImage.color = highlightBackgroundColour;

        if (buttonText != null)
            buttonText.color = highlightTextColour;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}