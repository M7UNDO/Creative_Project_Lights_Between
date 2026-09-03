using UnityEngine;
using UnityEngine.UI;

public class FlashlightUIScript : MonoBehaviour
{
    [Header("UI Containers & Components")]
    [SerializeField] private GameObject batteryUIPanel;
    [SerializeField] private Image[] batteryIcons = new Image[3];

    [Header("Color Settings")]
    [SerializeField] private Color fullBatteryColor = Color.white;
    [SerializeField] private Color emptyBatteryColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    private void Awake()
    {
        SetUIVisibility(false);
    }

    public void SetUIVisibility(bool visible)
    {
        if (batteryUIPanel != null)
        {
            batteryUIPanel.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    public void UpdateBatteryUI(int currentBatteryCount, bool isCurrentBatteryDepleted)
    {
        if (batteryIcons == null || batteryIcons.Length == 0) return;

        for (int i = 0; i < batteryIcons.Length; i++)
        {
            if (batteryIcons[i] == null) continue;

            if (i < currentBatteryCount)
            {
                batteryIcons[i].gameObject.SetActive(true);
                batteryIcons[i].color = (i == 0 && isCurrentBatteryDepleted) ? emptyBatteryColor : fullBatteryColor;
            }
            else
            {
                batteryIcons[i].gameObject.SetActive(false);
            }
        }
    }
}