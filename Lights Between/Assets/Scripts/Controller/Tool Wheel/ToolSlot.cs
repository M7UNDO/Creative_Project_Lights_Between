using UnityEngine;
using UnityEngine.UI;

public class ToolSlot : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;

    [Header("Colours")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = Color.yellow;

    [SerializeField] private ToolWheelController wheelController;

    private ToolClass tool;

    public ToolClass Tool => tool;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (iconImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>();

            foreach (Image image in images)
            {
                if (image != backgroundImage)
                {
                    iconImage = image;
                    break;
                }
            }
        }

        if (wheelController == null)
        {
            wheelController = GetComponentInParent<ToolWheelController>();
        }

        SetHighlight(false);
    }

    public void Setup(ToolClass newTool)
    {
        tool = newTool;

        if (iconImage != null)
        {
            iconImage.sprite = newTool != null ? newTool.sprite : null;
            iconImage.enabled = newTool != null && newTool.sprite != null;
        }

        gameObject.SetActive(true);
        SetHighlight(false);
    }

    public void SetupAsUnequipSlot(Sprite unequipSprite)
    {
        tool = null;

        if (iconImage != null)
        {
            iconImage.sprite = unequipSprite;
            iconImage.enabled = unequipSprite != null;
        }

        gameObject.SetActive(true);
        SetHighlight(false);
    }

    public void ClearButKeepVisible()
    {
        tool = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        gameObject.SetActive(true);
        SetHighlight(false);
    }

    public void SetHighlight(bool state)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = state ? highlightedColor : normalColor;
        }
    }
}