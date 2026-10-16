using System.Collections;
using UnityEngine;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("Camera & Focus Settings")]
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private float cameraTransitionSpeed = 5f;

    private FPSController playerController;
    private InspectableObject currentObject;

    private Vector3 originalCamPosition;
    private Quaternion originalCamRotation;

    private bool isInspecting;
    private bool isTransitioningCamera;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartInspection(InspectableObject inspectable)
    {
        if (isInspecting || isTransitioningCamera || inspectable == null) return;

        currentObject = inspectable;

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FPSController>();
        }

        if (playerController != null)
        {
            playerController.SetInspectionMode(true);
        }

        originalCamPosition = mainCameraTransform.position;
        originalCamRotation = mainCameraTransform.rotation;

        isInspecting = true;

        StartCoroutine(EnterInspectionRoutine());
    }

    private IEnumerator EnterInspectionRoutine()
    {
        isTransitioningCamera = true;

        Vector3 targetPos = currentObject.InspectCameraTransform.position;
        Quaternion targetRot = currentObject.InspectCameraTransform.rotation;

        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * cameraTransitionSpeed;
            mainCameraTransform.position = Vector3.Lerp(originalCamPosition, targetPos, elapsedTime);
            mainCameraTransform.rotation = Quaternion.Slerp(originalCamRotation, targetRot, elapsedTime);
            yield return null;
        }

        mainCameraTransform.position = targetPos;
        mainCameraTransform.rotation = targetRot;
        isTransitioningCamera = false;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartCustomLines(
                currentObject.InternalThoughts,
                OnThoughtsFinished,
                managePlayerMode: false
            );
        }
        else
        {
            OnThoughtsFinished();
        }
    }

    private void OnThoughtsFinished()
    {
        EndInspection();
    }

    private void EndInspection()
    {
        if (!isInspecting || isTransitioningCamera) return;
        StartCoroutine(ExitInspectionRoutine());
    }

    private IEnumerator ExitInspectionRoutine()
    {
        isTransitioningCamera = true;

        float elapsedTime = 0f;
        Vector3 startPos = mainCameraTransform.position;
        Quaternion startRot = mainCameraTransform.rotation;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * cameraTransitionSpeed;
            mainCameraTransform.position = Vector3.Lerp(startPos, originalCamPosition, elapsedTime);
            mainCameraTransform.rotation = Quaternion.Slerp(startRot, originalCamRotation, elapsedTime);
            yield return null;
        }

        mainCameraTransform.position = originalCamPosition;
        mainCameraTransform.rotation = originalCamRotation;

        isTransitioningCamera = false;
        isInspecting = false;

        if (playerController != null)
        {
            playerController.SetInspectionMode(false);
        }

        currentObject = null;
    }
}