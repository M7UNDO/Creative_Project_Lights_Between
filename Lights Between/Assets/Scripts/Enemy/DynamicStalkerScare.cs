using System.Collections;
using UnityEngine;

public enum ScareType
{
    PeekingCorner,      // Appears around corner, hides if player looks at it or after delay
    ObjectDisplacement, // Moves/swaps an object (e.g. Doll relocation)
    LightDisruption,    // Flickers/kills nearby lights temporarily
    ShadowPassing       // Crosses a doorway or hallway at distance
}

public class DynamicStalkerScare : MonoBehaviour
{
    [Header("Scare Configuration")]
    [SerializeField] private ScareType scareType = ScareType.PeekingCorner;
    [SerializeField] private ObjectiveSO requiredObjective;

    [Header("Target & Actor Setup")]
    [SerializeField] private GameObject stalkerActor;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float moveSpeed = 3f;

    [Header("Object Shift (Doll / Props)")]
    [SerializeField] private GameObject originalObject;
    [SerializeField] private GameObject displacedObject;

    [Header("Light Interference Settings")]
    [SerializeField] private Light[] targetLights;
    [SerializeField] private float flickerDuration = 2.5f;

    [Header("Audio SFX")]
    [SerializeField] private AudioSource scareAudioSource;
    [SerializeField] private AudioClip scareSFX;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !other.CompareTag("Player")) return;

        if (requiredObjective != null && ObjectiveManager.Instance != null)
        {
            if (ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) != ObjectiveStatus.Active &&
                ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) != ObjectiveStatus.Completed)
            {
                return;
            }
        }

        hasTriggered = true;
        ExecuteScare();
    }

    public void ExecuteScare()
    {
        switch (scareType)
        {
            case ScareType.PeekingCorner:
            case ScareType.ShadowPassing:
                StartCoroutine(ExecuteMovementScare());
                break;

            case ScareType.ObjectDisplacement:
                ExecuteObjectDisplacement();
                break;

            case ScareType.LightDisruption:
                StartCoroutine(ExecuteLightDisruption());
                break;
        }
    }

    private IEnumerator ExecuteMovementScare()
    {
        if (scareAudioSource != null && scareSFX != null)
        {
            scareAudioSource.PlayOneShot(scareSFX);
        }

        if (stalkerActor != null)
        {
            if (startPoint != null) stalkerActor.transform.position = startPoint.position;
            stalkerActor.SetActive(true);

            if (endPoint != null)
            {
                while (Vector3.Distance(stalkerActor.transform.position, endPoint.position) > 0.1f)
                {
                    stalkerActor.transform.position = Vector3.MoveTowards(
                        stalkerActor.transform.position,
                        endPoint.position,
                        moveSpeed * Time.deltaTime
                    );
                    yield return null;
                }
            }

            stalkerActor.SetActive(false);
        }
    }

    private void ExecuteObjectDisplacement()
    {
        if (originalObject != null) originalObject.SetActive(false);
        if (displacedObject != null) displacedObject.SetActive(true);

        if (scareAudioSource != null && scareSFX != null)
        {
            scareAudioSource.PlayOneShot(scareSFX);
        }
    }

    private IEnumerator ExecuteLightDisruption()
    {
        if (scareAudioSource != null && scareSFX != null)
        {
            scareAudioSource.PlayOneShot(scareSFX);
        }

        float timer = 0f;
        while (timer < flickerDuration)
        {
            timer += Time.deltaTime;
            foreach (var light in targetLights)
            {
                if (light != null)
                {
                    light.enabled = Random.value > 0.4f;
                }
            }
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }

        foreach (var light in targetLights)
        {
            if (light != null) light.enabled = true;
        }
    }
}