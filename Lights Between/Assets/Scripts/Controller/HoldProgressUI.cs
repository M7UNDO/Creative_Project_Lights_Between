using UnityEngine;
using UnityEngine.UI;

public class HoldProgressUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private Image progressImage;

    private void Start()
    {
        SetProgress(0f);
    }

    public void Show()
    {
        /*if (progressRoot != null)
            progressRoot.SetActive(true);*/
        if (progressImage != null)
        {
            progressImage.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        /*if (progressRoot != null)
            progressRoot.SetActive(false);*/

        if(progressImage != null)
        {
            progressImage.gameObject.SetActive(false);
        }

        SetProgress(0f);
    }

    public void SetProgress(float value)
    {
        if (progressImage != null)
            progressImage.fillAmount = Mathf.Clamp01(value);
    }
}