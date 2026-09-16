using TMPro;
using UnityEngine;

public class ObjectiveUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject completedCheckmark;

    public void Setup(ObjectiveSO objectiveDef)
    {
        if (titleText != null)
        {
            titleText.text = objectiveDef.Title;
        }
        SetCompleted(false);
    }

    public void SetCompleted(bool isCompleted)
    {
        if (completedCheckmark != null)
        {
            completedCheckmark.SetActive(isCompleted);
        }
    }
}