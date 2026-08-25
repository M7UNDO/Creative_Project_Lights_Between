using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeBlurController : MonoBehaviour
{
    public static VolumeBlurController Instance { get; private set; }

    [Header("Volume References")]
    [SerializeField] private Volume defaultVolume;
    [SerializeField] private Volume uiBlurVolume;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.25f;

    private Coroutine activeTransition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (defaultVolume != null) defaultVolume.weight = 1f;
        if (uiBlurVolume != null) uiBlurVolume.weight = 0f;
    }

    public void SetMenuBlurState(bool isOpen)
    {
        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
        }

        activeTransition = StartCoroutine(TransitionVolumes(isOpen));
    }

    public void ToggleMenuBlur()
    {
        bool targetState = uiBlurVolume.weight < 0.5f;
        SetMenuBlurState(targetState);
    }

    private IEnumerator TransitionVolumes(bool enableBlur)
    {
        float startDefaultWeight = defaultVolume != null ? defaultVolume.weight : 1f;
        float startBlurWeight = uiBlurVolume != null ? uiBlurVolume.weight : 0f;

        float targetDefaultWeight = enableBlur ? 0f : 1f;
        float targetBlurWeight = enableBlur ? 1f : 0f;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            if (defaultVolume != null)
            {
                defaultVolume.weight = Mathf.Lerp(startDefaultWeight, targetDefaultWeight, t);
            }

            if (uiBlurVolume != null)
            {
                uiBlurVolume.weight = Mathf.Lerp(startBlurWeight, targetBlurWeight, t);
            }

            yield return null;
        }

        if (defaultVolume != null) defaultVolume.weight = targetDefaultWeight;
        if (uiBlurVolume != null) uiBlurVolume.weight = targetBlurWeight;

        activeTransition = null;
    }
}