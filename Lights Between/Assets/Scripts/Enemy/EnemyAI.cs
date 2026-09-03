using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform waypointGroup;
    public AISensor sensor;

    [Header("Suspicion System")]
    public float suspicion = 0f;
    public float suspicionIncreaseRate = 25f;
    public float suspicionDecreaseRate = 10f;
    public float suspicionThreshold = 100f;

    [Header("Detection Memory")]
    public float loseSightDelay = 1.5f;
    private float loseSightTimer;

    [Header("Alert Settings")]
    public float alertDuration = 1.5f;
    [SerializeField] private float rigLerpSpeed = 5f;

    private bool isAlerting;
    private bool hasAlerted;

    [Header("Catch Settings")]
    public float catchRange = 2f;
    public float restartDelay = 2f;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 7f;
    public float maxChaseDistance = 25f;

    [Header("Animation Settings")]
    public float walkAnimationSpeed = 1f;
    public float runAnimationSpeed = 1f;
    public float acceleration = 10f;

    [Header("Idle Settings")]
    public float idleTime = 2f;
    public float lookAroundAngle = 60f;
    public float lookSpeed = 3f;

    /*[Header("UI")]
    public GameObject uiPrefab;
    public Transform uiParent;*/

    public AudioSource alertSFX;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundRayDistance = 1.5f;
    [SerializeField] private float groundCheckHeightOffset = 0.5f;


    [Header("Wood Footsteps")]
    [SerializeField] private AudioClip[] woodWalkFootstepClips;
    [SerializeField] private AudioClip[] woodRunFootstepClips;

    [Header("Tile Footsteps")]
    [SerializeField] private AudioClip[] tileWalkFootstepClips;
    [SerializeField] private AudioClip[] tileRunFootstepClips;

    [Header("Carpet Footsteps")]
    [SerializeField] private AudioClip[] carpetWalkFootstepClips;
    [SerializeField] private AudioClip[] carpetRunFootstepClips;

    [Header("Concrete Footsteps")]
    [SerializeField] private AudioClip[] concreteWalkFootstepClips;
    [SerializeField] private AudioClip[] concreteRunFootstepClips;

    [Header("Default Footsteps")]
    [SerializeField] private AudioClip[] defaultWalkFootstepClips;
    [SerializeField] private AudioClip[] defaultRunFootstepClips;


    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    private NavMeshAgent agent;
    private Transform currentTarget;

    private bool isCatching;
    private bool gameOver;

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Vector3 patrolStartPoint;

    private bool isIdle;
    private float idleTimer;
    private Quaternion targetLookRotation;

    private Vector3 lastKnownPlayerPosition;
    private float currentSpeedPercent;
    private float targetSpeedPercent;

    private enum State { Patrol, Suspicious, Chase, Catch, Returning }
    private State currentState;

    public BackgroundMusicManager musicManager;
    public MultiAimConstraint headAim;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.acceleration = 8f;
        agent.angularSpeed = 360f;

        if (waypointGroup != null && waypointGroup.childCount > 0)
        {
            waypoints = new Transform[waypointGroup.childCount];

            for (int i = 0; i < waypointGroup.childCount; i++)
                waypoints[i] = waypointGroup.GetChild(i);

            patrolStartPoint = waypoints[0].position;
        }
        else
        {
            waypoints = null;
        }

        ChangeState(State.Patrol);
        if (waypoints != null && waypoints.Length > 0)
        {
            GoToNextWaypoint();
        }

        UpdateHeadAim(0f);
    }

    void Update()
    {
        if (gameOver)
            return;

        if (currentState != State.Returning && currentState != State.Catch)
        {
            Transform detected = DetectPlayer();

            if (detected != null)
            {
                // Player is visible
                currentTarget = detected;
                lastKnownPlayerPosition = currentTarget.position;
                loseSightTimer = loseSightDelay; // Reset memory timer
            }
            else
            {
                // Player not visible
                loseSightTimer -= Time.deltaTime;

                if (loseSightTimer <= 0)
                {
                    currentTarget = null; // Forget player only after delay
                }
            }
        }
        else
        {
            currentTarget = null;
        }

        float distanceToPlayer = currentTarget ?
            Vector3.Distance(transform.position, currentTarget.position) :
            Mathf.Infinity;

        HandleSuspicion();

        if (isAlerting)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            UpdateAnimation(0f);
            return;
        }


        if (!isCatching)
        {
            if (distanceToPlayer <= catchRange && currentState == State.Chase)
            {
                ChangeState(State.Catch);
            }
            else if (suspicion >= suspicionThreshold && !hasAlerted && !isAlerting)
            {
                StartCoroutine(AlertBeforeChase());
            }
            else
            {

                State newState;

                if (suspicion >= suspicionThreshold)
                {
                    newState = State.Chase;
                }
                else if (suspicion > 0)
                {
                    newState = State.Suspicious;
                }
                else
                {
                    newState = State.Patrol;
                }

                if (newState != currentState)
                    ChangeState(newState);
            }
        }

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Suspicious:
                Suspicious();
                break;
            case State.Chase:
                ChasePlayer();
                break;
            case State.Catch:
                CatchPlayer();
                break;
            case State.Returning:
                ReturnToPatrol();
                break;
        }


        if (currentState == State.Chase)
        {
            targetSpeedPercent = 1f;
        }
        else if (agent.velocity.magnitude > 0.1f)
        {
            targetSpeedPercent = 0.5f;
        }
        else
        {
            targetSpeedPercent = 0f;
        }

        UpdateAnimation(targetSpeedPercent);

        if (!isCatching && !isAlerting)
            RotateTowardsMovementDirection();
    }

    void UpdateAnimation(float targetSpeed)
    {

        currentSpeedPercent = Mathf.MoveTowards(currentSpeedPercent, targetSpeed,
            acceleration * Time.deltaTime);


        animator.SetFloat("Speed", currentSpeedPercent);


        if (currentState == State.Chase)
        {

            animator.speed = runAnimationSpeed;
            agent.speed = runSpeed;
        }
        else if (agent.velocity.magnitude > 0.1f)
        {

            animator.speed = walkAnimationSpeed;
            agent.speed = walkSpeed;
        }
        else
        {

            animator.speed = 1f;
        }
    }

    public void UpdateHeadAim(float targetRigWeight)
    {
        if (headAim != null)
        {
            headAim.weight = Mathf.Lerp(headAim.weight, targetRigWeight, Time.deltaTime * rigLerpSpeed);
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;


        Debug.Log($"Changing state from {currentState} to {newState}");

        isIdle = false;
        currentState = newState;


        switch (newState)
        {
            case State.Chase:
                agent.isStopped = false;

                break;

            case State.Patrol:
            case State.Suspicious:
            case State.Returning:

                agent.isStopped = false;

                break;

            case State.Catch:

                agent.isStopped = true;

                break;
        }
    }

    IEnumerator AlertBeforeChase()
    {
        isAlerting = true;
        hasAlerted = true;


        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        UpdateAnimation(0f);
        UpdateHeadAim(1f);


        animator.SetTrigger("Alert");

        if (alertSFX != null)
            alertSFX.Play();

        yield return new WaitForSeconds(alertDuration);


        agent.isStopped = false;
        ChangeState(State.Chase);
        isAlerting = false;
    }

    Transform DetectPlayer()
    {
        if (sensor == null || sensor.Objects.Count == 0)
            return null;

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject obj in sensor.Objects)
        {
            float dist = Vector3.Distance(transform.position, obj.transform.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = obj.transform;
            }
        }

        return closest;
    }

    void HandleSuspicion()
    {
        if (currentTarget != null)
        {

            suspicion += suspicionIncreaseRate * Time.deltaTime;
        }
        else
        {

            suspicion -= suspicionDecreaseRate * Time.deltaTime;
        }

        suspicion = Mathf.Clamp(suspicion, 0, suspicionThreshold);

        if (suspicion <= suspicionThreshold * 0.25f)
        {
            hasAlerted = false;
            UpdateHeadAim(0f);
        }
    }

    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            UpdateAnimation(0f);
            return;
        }

        if (isIdle)
        {
            idleTimer -= Time.deltaTime;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetLookRotation,
                Time.deltaTime * lookSpeed
            );

            if (idleTimer <= 0f)
            {
                isIdle = false;
                GoToNextWaypoint();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            StartIdle();
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    void Suspicious()
    {
        isIdle = false;

        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            agent.SetDestination(lastKnownPlayerPosition);
        }
    }

    void ReturnToPatrol()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            suspicion = 0;
            hasAlerted = false;
            ChangeState(State.Patrol);
            GoToNextWaypoint();
        }
        else
        {
            agent.SetDestination(patrolStartPoint);
        }
    }

    void ChasePlayer()
    {
        if (currentTarget)
        {
            lastKnownPlayerPosition = currentTarget.position;
            agent.SetDestination(currentTarget.position);
            UpdateHeadAim(1f);
        }
        else
        {
            agent.SetDestination(lastKnownPlayerPosition);

            if (!agent.pathPending && agent.remainingDistance < 1f)
            {
                suspicion = suspicionThreshold * 0.5f;
                ChangeState(State.Suspicious);
                UpdateHeadAim(1f);
            }
        }
    }

    void CatchPlayer()
    {
        if (isCatching || currentTarget == null)
            return;

        isCatching = true;
        gameOver = true;

        agent.isStopped = true;
        agent.ResetPath();

        Vector3 lookPos = new Vector3(
            currentTarget.position.x,
            transform.position.y,
            currentTarget.position.z
        );

        transform.rotation = Quaternion.LookRotation(lookPos - transform.position);

        CinemachineThirdPersonFollow follow =
            currentTarget.gameObject.GetComponentInChildren<CinemachineThirdPersonFollow>();

        if (follow != null)
        {
            follow.AvoidObstacles.Enabled = false;
        }

        //DisableMovement
        //ThirdPersonController.SetMovement(false);

        animator.SetTrigger("Attack");

        /*if (musicManager != null && musicManager.missionFailedSFX != null)
        {
            musicManager.PlayCaughtBackgroundMusic();
        }*/

        StartCoroutine(RestartGame());
    }

    IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(restartDelay);

        /*if (gameOverUI != null)
        {
            gameOverUI.ShowGameOver();
        }*/
    }

    void StartIdle()
    {
        isIdle = true;
        idleTimer = idleTime;
        agent.ResetPath();

        float randomAngle = Random.Range(-lookAroundAngle, lookAroundAngle);
        targetLookRotation = Quaternion.Euler(0, transform.eulerAngles.y + randomAngle, 0);
    }

    void RotateTowardsMovementDirection()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }

    private AudioClip[] GetFootstepClips(bool isRunning)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * groundCheckHeightOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayers))
        {
            if (hit.collider.CompareTag("Wood"))
            {
                return isRunning ? woodRunFootstepClips : woodWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Tile"))
            {
                return isRunning ? tileRunFootstepClips : tileWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Carpet"))
            {
                return isRunning ? carpetRunFootstepClips : carpetWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Concrete"))
            {
                return isRunning ? concreteRunFootstepClips : concreteWalkFootstepClips;
            }

        }


        return isRunning ? defaultRunFootstepClips : defaultWalkFootstepClips;
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            PlayFootstep();
        }
    }

    private void PlayFootstep()
    {
        bool isRunning = currentState == State.Chase;
        AudioClip[] clips = GetFootstepClips(isRunning);

        if (clips != null && clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            AudioSource.PlayClipAtPoint(
                clips[index],
                transform.position,
                FootstepAudioVolume
            );
        }
    }

    public bool IsChasing()
    {
        return currentState == State.Chase;
    }
}