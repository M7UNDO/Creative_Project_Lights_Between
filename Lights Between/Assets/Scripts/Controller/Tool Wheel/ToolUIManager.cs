using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolUIManager : MonoBehaviour
{
    [SerializeField] private Image toolIcon;
    [SerializeField] private TextMeshProUGUI toolInfoText;

    public void UpdateToolUI(ToolClass toolData, GameObject equippedToolObject, int quantity)
    {
        if (toolData == null)
        {
            toolIcon.enabled = false;
            toolInfoText.enabled = false;
            return;
        }

        toolIcon.enabled = true;
        toolInfoText.enabled = true;
        toolIcon.sprite = toolData.sprite;

        if (toolData.usesPower && equippedToolObject != null)
        {
            if (equippedToolObject.TryGetComponent<IToolPower>(out IToolPower toolPower))
            {
                int percentage = Mathf.FloorToInt(((float)toolPower.CurrentPower / toolPower.MaxPower) * 100f);
                toolInfoText.text = $"{percentage}%";
            }
        }
        else
        {
            toolInfoText.text = $"x{quantity}";
        }
    }
}