using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public enum ScareType
{
    PeekingCorner,
    ObjectDisplacement,
    LightDisruption,
    ShadowPassing
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

    [Header("Creature Animation")]
    [SerializeField] private Animator stalkerAnimator;
    [SerializeField] private string speedFloatName = "Speed";
    [SerializeField] private float idleSpeedPercent = 0.0f;
    [SerializeField] private float walkSpeedPercent = 0.5f;
    [SerializeField] private float runSpeedPercent = 1.0f;

    [Header("Creature Rigging")]
    [SerializeField] private MultiAimConstraint creatureHeadAim;
    [SerializeField] private float rigLerpSpeed = 5f;

    [Header("Peeking Setup")]
    [SerializeField] private float detectionDotThreshold = 0.75f;
    [SerializeField] private float maxPeekDuration = 5f;
    [SerializeField] private float spottedReactionDelay = 1.2f;
    [SerializeField] private float retreatSpeed = 5f;

    [Header("Occlusion & Line of Sight")]
    [SerializeField] private LayerMask occlusionMask = ~0;

    [Header("Object Shift")]
    [SerializeField] private GameObject originalObject;
    [SerializeField] private GameObject displacedObject;

    [Header("Light Interference Settings")]
    [SerializeField] private Light[] targetLights;
    [SerializeField] private float flickerDuration = 1.5f;
    [SerializeField] private float blackoutDuration = 2.0f;

    [Header("Audio SFX")]
    [SerializeField] private AudioSource scareAudioSource;
    [SerializeField] private AudioClip scareSFX;

    private bool hasTriggered;
    private Camera playerCamera;
    private Coroutine headAimCoroutine;

    private void Awake()
    {
        playerCamera = Camera.main;
        SetCreatureHeadAimWeightDirect(0f);
    }

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
                StartCoroutine(ExecutePeekingScare());
                break;

            case ScareType.ShadowPassing:
                StartCoroutine(ExecuteShadowPassing());
                break;

            case ScareType.ObjectDisplacement:
                ExecuteObjectDisplacement();
                break;

            case ScareType.LightDisruption:
                StartCoroutine(ExecuteLightDisruption());
                break;
        }
    }

    private bool HasLineOfSight(Vector3 targetPosition)
    {
        if (playerCamera == null) return false;

        Vector3 origin = playerCamera.transform.position;

        if (Physics.Linecast(origin, targetPosition, out RaycastHit hit, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            if (stalkerActor != null && hit.transform.IsChildOf(stalkerActor.transform)) return true;
            if (startPoint != null && hit.transform == startPoint) return true;

            return false;
        }

        return true;
    }

    private void SetCreatureHeadAimWeightDirect(float weight)
    {
        if (creatureHeadAim != null)
        {
            creatureHeadAim.weight = weight;
        }
    }

    private void LerpCreatureHeadAimWeight(float targetWeight)
    {
        if (headAimCoroutine != null)
        {
            StopCoroutine(headAimCoroutine);
        }
        headAimCoroutine = StartCoroutine(LerpHeadAimRoutine(targetWeight));
    }

    private IEnumerator LerpHeadAimRoutine(float targetWeight)
    {
        if (creatureHeadAim == null) yield break;

        while (Mathf.Abs(creatureHeadAim.weight - targetWeight) > 0.01f)
        {
            creatureHeadAim.weight = Mathf.Lerp(creatureHeadAim.weight, targetWeight, Time.deltaTime * rigLerpSpeed);
            yield return null;
        }

        creatureHeadAim.weight = targetWeight;
        headAimCoroutine = null;
    }

    private IEnumerator ExecutePeekingScare()
    {
        if (scareAudioSource != null && scareSFX != null)
        {
            scareAudioSource.PlayOneShot(scareSFX);
        }

        if (stalkerActor != null)
        {
            if (startPoint != null)
            {
                stalkerActor.transform.position = startPoint.position;
                stalkerActor.transform.rotation = startPoint.rotation;
            }
            stalkerActor.SetActive(true);

            if (stalkerAnimator != null)
            {
                stalkerAnimator.Rebind();
                stalkerAnimator.Update(0f);
                stalkerAnimator.SetFloat(speedFloatName, idleSpeedPercent);
            }

            LerpCreatureHeadAimWeight(1f);

            float timer = 0f;
            bool playerNoticed = false;

            while (timer < maxPeekDuration)
            {
                if (playerCamera != null)
                {
                    Vector3 directionToStalker = (stalkerActor.transform.position - playerCamera.transform.position).normalized;
                    float dotProduct = Vector3.Dot(playerCamera.transform.forward, directionToStalker);

                    if (dotProduct >= detectionDotThreshold)
                    {
                        if (HasLineOfSight(stalkerActor.transform.position))
                        {
                            playerNoticed = true;
                            break;
                        }
                    }
                }

                timer += Time.deltaTime;
                yield return null;
            }

            if (playerNoticed)
            {
                yield return new WaitForSeconds(spottedReactionDelay);
            }

            if (endPoint != null)
            {
                float currentRetreatSpeed = playerNoticed ? retreatSpeed : moveSpeed;
                float currentAnimSpeed = playerNoticed ? runSpeedPercent : walkSpeedPercent;

                if (stalkerAnimator != null)
                {
                    stalkerAnimator.SetFloat(speedFloatName, currentAnimSpeed);
                }

                LerpCreatureHeadAimWeight(0f);

                while (Vector3.Distance(stalkerActor.transform.position, endPoint.position) > 0.1f)
                {
                    stalkerActor.transform.position = Vector3.MoveTowards(
                        stalkerActor.transform.position,
                        endPoint.position,
                        currentRetreatSpeed * Time.deltaTime
                    );

                    Vector3 moveDir = (endPoint.position - stalkerActor.transform.position).normalized;
                    if (moveDir != Vector3.zero)
                    {
                        stalkerActor.transform.rotation = Quaternion.LookRotation(moveDir);
                    }

                    yield return null;
                }
            }

            SetCreatureHeadAimWeightDirect(0f);
            stalkerActor.SetActive(false);
        }
    }

    private IEnumerator ExecuteShadowPassing()
    {
        if (scareAudioSource != null && scareSFX != null)
        {
            scareAudioSource.PlayOneShot(scareSFX);
        }

        if (stalkerActor != null)
        {
            if (startPoint != null)
            {
                stalkerActor.transform.position = startPoint.position;
                stalkerActor.transform.rotation = startPoint.rotation;
            }
            stalkerActor.SetActive(true);

            SetCreatureHeadAimWeightDirect(0f);

            if (stalkerAnimator != null)
            {
                stalkerAnimator.Rebind();
                stalkerAnimator.Update(0f);
                stalkerAnimator.SetFloat(speedFloatName, runSpeedPercent);
            }

            if (endPoint != null)
            {
                while (Vector3.Distance(stalkerActor.transform.position, endPoint.position) > 0.1f)
                {
                    stalkerActor.transform.position = Vector3.MoveTowards(
                        stalkerActor.transform.position,
                        endPoint.position,
                        moveSpeed * Time.deltaTime
                    );

                    Vector3 moveDir = (endPoint.position - stalkerActor.transform.position).normalized;
                    if (moveDir != Vector3.zero)
                    {
                        stalkerActor.transform.rotation = Quaternion.LookRotation(moveDir);
                    }

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
            if (light != null) light.enabled = false;
        }

        yield return new WaitForSeconds(blackoutDuration);

        foreach (var light in targetLights)
        {
            if (light != null) light.enabled = true;
        }
    }
}