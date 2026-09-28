using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class UITextFlasher : MonoBehaviour
{
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private int flashCount = 3;

    private TMP_Text targetText;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        ValidateTextComponent();
    }

    private void ValidateTextComponent()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    public void PlayFlashEffect()
    {
        ValidateTextComponent();

        if (targetText == null) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        targetText.maxVisibleCharacters = 99999;
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(flashInterval);

        for (int i = 0; i < flashCount; i++)
        {
            if (targetText != null) targetText.maxVisibleCharacters = 0;
            yield return wait;
            if (targetText != null) targetText.maxVisibleCharacters = 99999;
            yield return wait;
        }

        flashCoroutine = null;
    }
}