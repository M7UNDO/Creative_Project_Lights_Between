using System.Collections;
using UnityEngine;

public class ProceduralCrawlIK : MonoBehaviour
{
    [Header("Limb Transforms")]
    [SerializeField] private Transform[] feetTargets;
    [SerializeField] private Transform[] feetRayAnchors;

    [Header("Step Configuration")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float stepDistance = 0.6f;
    [SerializeField] private float stepHeight = 0.25f;
    [SerializeField] private float stepSpeed = 12f;
    [SerializeField] private float raycastDistance = 1.5f;

    [Header("Body Offset Settings")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private float bodyHeightOffset = 0.3f;
    [SerializeField] private float bodyLerpSpeed = 5f;

    private Vector3[] currentFootPositions;
    private Vector3[] nextFootPositions;
    private bool[] isLegMoving;

    private int legCount;

    void Start()
    {
        legCount = feetTargets.Length;
        currentFootPositions = new Vector3[legCount];
        nextFootPositions = new Vector3[legCount];
        isLegMoving = new bool[legCount];

        for (int i = 0; i < legCount; i++)
        {
            currentFootPositions[i] = feetTargets[i].position;
            nextFootPositions[i] = feetTargets[i].position;
        }
    }

    void Update()
    {
        UpdateFootPositions();
        AdjustBodyHeight();
    }

    private void UpdateFootPositions()
    {
        for (int i = 0; i < legCount; i++)
        {
            if (isLegMoving[i]) continue;

            if (CanLegMove(i))
            {
                Vector3 rayOrigin = feetRayAnchors[i].position;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers))
                {
                    float distanceToTarget = Vector3.Distance(currentFootPositions[i], hit.point);
                    if (distanceToTarget > stepDistance)
                    {
                        StartCoroutine(PerformStep(i, hit.point));
                    }
                }
            }
        }
    }

    private bool CanLegMove(int legIndex)
    {
        int diagonalOppositeIndex = (legIndex + 2) % legCount;
        return !isLegMoving[diagonalOppositeIndex];
    }

    private IEnumerator PerformStep(int legIndex, Vector3 targetPosition)
    {
        isLegMoving[legIndex] = true;
        Vector3 startPosition = currentFootPositions[legIndex];
        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * stepSpeed;

            Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            linearPosition.y += Mathf.Sin(progress * Mathf.PI) * stepHeight;

            currentFootPositions[legIndex] = linearPosition;
            feetTargets[legIndex].position = linearPosition;

            yield return null;
        }

        currentFootPositions[legIndex] = targetPosition;
        feetTargets[legIndex].position = targetPosition;
        isLegMoving[legIndex] = false;
    }

    private void AdjustBodyHeight()
    {
        if (bodyTransform == null) return;

        float averageFootY = 0f;
        for (int i = 0; i < legCount; i++)
        {
            averageFootY += feetTargets[i].position.y;
        }
        averageFootY /= legCount;

        Vector3 targetBodyPosition = bodyTransform.position;
        targetBodyPosition.y = averageFootY + bodyHeightOffset;

        bodyTransform.position = Vector3.Lerp(
            bodyTransform.position,
            targetBodyPosition,
            Time.deltaTime * bodyLerpSpeed
        );
    }
}