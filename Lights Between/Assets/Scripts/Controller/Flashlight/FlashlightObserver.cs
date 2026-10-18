using System;
using UnityEngine;

public class FlashlightObserver : MonoBehaviour
{
    public static event Action<FlashlightObserver> OnObserverSpawned;
    public static event Action<FlashlightObserver> OnObserverDespawned;

    [Header("References")]
    [SerializeField] private FlashlightScript flashlight;
    [SerializeField] private Light spotLight;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform targetCreature;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask occlusionLayers;
    [SerializeField] private float playerViewMaxAngle = 45f;
    [SerializeField] private Vector3 raycastTargetOffset = new Vector3(0f, 1f, 0f);

    public bool IsObserving { get; private set; }

    private void Awake()
    {
        if (flashlight == null) flashlight = GetComponentInParent<FlashlightScript>();
        if (flashlight == null) flashlight = GetComponent<FlashlightScript>();
        if (spotLight == null) spotLight = GetComponent<Light>();
        if (spotLight == null) spotLight = GetComponentInChildren<Light>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        OnObserverSpawned?.Invoke(this);
    }

    private void OnDisable()
    {
        OnObserverDespawned?.Invoke(this);
    }

    private void Update()
    {
        if (targetCreature == null)
        {
            EnemyAI enemy = FindFirstObjectByType<EnemyAI>();
            if (enemy != null)
            {
                targetCreature = enemy.transform;
            }
        }

        IsObserving = EvaluateObservation(targetCreature);
    }

    public bool IsTargetObserved(Transform enemyTransform)
    {
        if (enemyTransform == targetCreature)
        {
            return IsObserving;
        }

        return EvaluateObservation(enemyTransform);
    }

    private bool EvaluateObservation(Transform target)
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (flashlight == null || spotLight == null || target == null || playerCamera == null) return false;

        if (flashlight.CurrentPower <= 0f || flashlight.requiresBatteryChange) return false;

        Vector3 rayOrigin = spotLight.transform.position + (spotLight.transform.forward * 0.2f);
        Vector3 targetPos = target.position + raycastTargetOffset;
        Vector3 dirToEnemy = (targetPos - rayOrigin).normalized;
        float distanceToEnemy = Vector3.Distance(rayOrigin, targetPos);

        if (distanceToEnemy > spotLight.range) return false;

        float spotAngleThreshold = spotLight.spotAngle * 0.5f;
        float angleToTorchForward = Vector3.Angle(spotLight.transform.forward, dirToEnemy);
        if (angleToTorchForward > spotAngleThreshold) return false;

        Vector3 screenPoint = playerCamera.WorldToViewportPoint(targetPos);
        bool inViewport = screenPoint.z > 0f && screenPoint.x >= 0.05f && screenPoint.x <= 0.95f && screenPoint.y >= 0.05f && screenPoint.y <= 0.95f;
        if (!inViewport) return false;

        Vector3 dirFromCam = (targetPos - playerCamera.transform.position).normalized;
        float angleFromCamForward = Vector3.Angle(playerCamera.transform.forward, dirFromCam);
        if (angleFromCamForward > playerViewMaxAngle) return false;

        if (Physics.Raycast(rayOrigin, dirToEnemy, out RaycastHit hit, distanceToEnemy, occlusionLayers, QueryTriggerInteraction.Ignore))
        {
            bool hitEnemy = hit.transform.IsChildOf(target) || hit.transform == target;
            Debug.DrawLine(rayOrigin, hit.point, hitEnemy ? Color.green : Color.red);

            if (!hitEnemy)
            {
                return false;
            }
        }

        return true;
    }
}