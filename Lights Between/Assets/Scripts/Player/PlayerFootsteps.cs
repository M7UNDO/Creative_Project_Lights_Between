using UnityEngine;

[RequireComponent(typeof(FPSController))]
[RequireComponent(typeof(InputHandler))]
public class PlayerFootsteps : MonoBehaviour
{
    public bool playingFootsteps = false;

    [SerializeField] private float walkFootstepSpeed = 0.5f;
    [SerializeField] private float sprintFootstepSpeed = 0.3f;

    [Header("Walk Footstep Clips")]
    [SerializeField] private AudioClip[] woodWalkFootstepClips;
    [SerializeField] private AudioClip[] tileWalkFootstepClips;
    [SerializeField] private AudioClip[] carpetWalkFootstepClips;
    [SerializeField] private AudioClip[] defaultWalkFootstepClips;

    [Header("Run Footstep Clips")]
    [SerializeField] private AudioClip[] woodRunFootstepClips;
    [SerializeField] private AudioClip[] tileRunFootstepClips;
    [SerializeField] private AudioClip[] carpetRunFootstepClips;
    [SerializeField] private AudioClip[] defaultRunFootstepClips;

    [SerializeField] private AudioSource stepSFX;
    [SerializeField] private float groundRayDistance = 1.5f;
    [SerializeField] private LayerMask GroundLayers;

    private float _footstepTimer;
    private float currentFootstepSpeed;
    private FPSController _fpsController;
    private InputHandler _input;

    private void Start()
    {
        _fpsController = GetComponent<FPSController>();
        _input = GetComponent<InputHandler>();
    }

    private void Update()
    {
        if (_fpsController.isSeated || _fpsController.isInspecting || _fpsController.isDialogueActive || _fpsController.isInMiniGame || _fpsController.isJumpscareActive)
        {
            StopFootsteps();
            return;
        }

        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        bool isMoving = _input.move.sqrMagnitude > 0.01f;
        bool isSprinting = _input.sprint && isMoving;
        bool shouldPlayFootsteps = isMoving && _fpsController.Grounded;

        currentFootstepSpeed = isSprinting ? sprintFootstepSpeed : walkFootstepSpeed;

        if (shouldPlayFootsteps)
        {
            playingFootsteps = true;
            _footstepTimer -= Time.deltaTime;

            if (_footstepTimer <= 0f)
            {
                PlayFootstepsSFX(isSprinting);
                _footstepTimer = currentFootstepSpeed;
            }
        }
        else
        {
            playingFootsteps = false;

            if (!isMoving)
            {
                _footstepTimer = 0f;
            }
        }
    }

    public void StopFootsteps()
    {
        playingFootsteps = false;
        _footstepTimer = 0f;
    }

    private void PlayFootstepsSFX(bool isSprinting)
    {
        if (stepSFX == null) return;

        AudioClip[] chosenClips = GetFootstepClips(isSprinting);

        if (chosenClips == null || chosenClips.Length == 0)
            return;

        int chosenStep = Random.Range(0, chosenClips.Length);

        stepSFX.pitch = Random.Range(0.9f, 1.15f);
        stepSFX.PlayOneShot(chosenClips[chosenStep]);
    }

    private AudioClip[] GetFootstepClips(bool isSprinting)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundRayDistance, GroundLayers))
        {
            if (hit.collider.CompareTag("Wood"))
            {
                return isSprinting ? woodRunFootstepClips : woodWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Tile"))
            {
                return isSprinting ? tileRunFootstepClips : tileWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Carpet"))
            {
                return isSprinting ? carpetRunFootstepClips : carpetWalkFootstepClips;
            }
        }

        return isSprinting ? defaultRunFootstepClips : defaultWalkFootstepClips;
    }
}