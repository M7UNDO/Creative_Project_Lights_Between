using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerFearFeedback : MonoBehaviour
{
    [Header("Volume Reference")]
    [SerializeField] private Volume postProcessVolume;

    private ChromaticAberration chromaticAberration;
    private float originalIntensity;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            if (postProcessVolume.profile.TryGet(out chromaticAberration))
            {
                originalIntensity = chromaticAberration.intensity.value;
            }
        }
    }

    public void StartPulse(float targetIntensity, float speed)
    {
        if (chromaticAberration == null) return;

        StopPulse();
        pulseCoroutine = StartCoroutine(PulseRoutine(targetIntensity, speed));
    }

    public void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = originalIntensity;
        }
    }

    private IEnumerator PulseRoutine(float targetIntensity, float speed)
    {
        bool toggled = false;

        while (true)
        {
            float target = toggled ? targetIntensity : originalIntensity;
            while (Mathf.Abs(chromaticAberration.intensity.value - target) > 0.02f)
            {
                chromaticAberration.intensity.value = Mathf.MoveTowards(
                    chromaticAberration.intensity.value,
                    target,
                    speed * Time.deltaTime
                );
                yield return null;
            }

            toggled = !toggled;
            yield return new WaitForSeconds(0.05f);
        }
    }
}
