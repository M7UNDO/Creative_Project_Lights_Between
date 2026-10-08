using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolSlot : MonoBehaviour
{
    [Header("UI References")]
    [Space(5)]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Colours")]
    [Space(5)]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = Color.yellow;

    [Header("Controller")]
    [Space(5)]
    [SerializeField] private ToolWheelController wheelController;

    private ToolClass tool;
    private int quantity;

    public ToolClass Tool => tool;
    public int Quantity => quantity;

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

        if (countText == null)
        {
            countText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (wheelController == null)
        {
            wheelController = GetComponentInParent<ToolWheelController>();
        }

        SetHighlight(false);
    }

    public void Setup(ToolClass newTool, int itemQuantity)
    {
        tool = newTool;
        quantity = itemQuantity;

        if (iconImage != null)
        {
            iconImage.sprite = newTool != null ? newTool.sprite : null;
            iconImage.enabled = newTool != null && newTool.sprite != null;
        }

        UpdateCountText();

        gameObject.SetActive(true);
        SetHighlight(false);
    }

    public void SetupAsUnequipSlot(Sprite unequipSprite)
    {
        tool = null;
        quantity = 0;

        if (iconImage != null)
        {
            iconImage.sprite = unequipSprite;
            iconImage.enabled = unequipSprite != null;
        }

        if (countText != null)
        {
            countText.text = string.Empty;
        }

        gameObject.SetActive(true);
        SetHighlight(false);
    }

    public void ClearButKeepVisible()
    {
        tool = null;
        quantity = 0;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (countText != null)
        {
            countText.text = string.Empty;
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

    private void UpdateCountText()
    {
        if (countText == null) return;

        if (tool != null && quantity > 1)
        {
            countText.text = quantity.ToString();
        }
        else
        {
            countText.text = string.Empty;
        }
    }
}