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

    [Header("Retreat Settings")]
    [SerializeField] private float retreatRecoupTime = 4f;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource crawlSource;
    [SerializeField] private AudioSource voiceSource;
    public AudioSource alertSFX;

    [Header("Door Interaction")]
    [SerializeField] private LayerMask doorLayer;
    [SerializeField] private float doorInteractionDistance = 2f;
    [SerializeField] private float doorCheckHeightOffset = 1f;
    [SerializeField] private float doorSphereCastRadius = 0.5f;

    [Header("Ground Detection & Surface Alignment")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundRayDistance = 2f;
    [SerializeField] private float groundCheckHeightOffset = 0.5f;
    [SerializeField] private float surfaceAlignmentSpeed = 10f;

    [Header("Crawl Sound Settings")]
    [SerializeField] private AudioClip[] crawlClips;
    [SerializeField] private float minPitch = 0.85f;
    [SerializeField] private float maxPitch = 1.15f;
    [Range(0, 1)] public float crawlAudioVolume = 0.5f;

    [Header("Voice Sound Settings")]
    [SerializeField] private AudioClip[] breatheClips;
    [Range(0, 1)] public float breatheAudioVolume = 0.5f;

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

    private bool canSeePlayer;
    private bool hasDetectedPlayer;
    private Vector3 lastKnownPlayerPosition;

    private float currentSpeedPercent;
    private float targetSpeedPercent;

    private enum State { Patrol, Suspicious, Chase, Catch, Returning, Retreat }
    private State currentState;

    private bool isRetreating;
    private Camera mainCamera;

    public BackgroundMusicManager musicManager;
    public MultiAimConstraint headAim;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        agent.acceleration = 8f;
        agent.angularSpeed = 360f;
        agent.updateRotation = false;

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
        if (gameOver) return;

        if (currentState != State.Returning && currentState != State.Catch && currentState != State.Retreat)
        {
            UpdatePerception();
        }
        else if (currentState == State.Retreat)
        {
            currentTarget = null;
            canSeePlayer = false;
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
            AlignToSurface(transform.forward);
            return;
        }

        if (!isCatching && currentState != State.Retreat)
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
            case State.Retreat:
                break;
        }

        if (currentState == State.Chase || currentState == State.Retreat)
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
        CheckForDoors();

        if (!isCatching && !isAlerting)
            RotateTowardsMovementDirection();
    }

    private void UpdatePerception()
    {
        Transform closest = GetClosestSensorTarget();

        if (closest != null)
        {
            currentTarget = closest;
            canSeePlayer = true;
            hasDetectedPlayer = true;
            lastKnownPlayerPosition = currentTarget.position;
            loseSightTimer = loseSightDelay;
        }
        else
        {
            canSeePlayer = false;
            loseSightTimer -= Time.deltaTime;

            if (loseSightTimer <= 0)
            {
                hasDetectedPlayer = false;
                currentTarget = null;
            }
        }
    }

    private Transform GetClosestSensorTarget()
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

    private void HandleSuspicion()
    {
        if (canSeePlayer)
        {
            float lightModifier = GetTargetLightModifier();
            suspicion += (suspicionIncreaseRate * lightModifier) * Time.deltaTime;
        }
        else if (hasDetectedPlayer)
        {
            suspicion += (suspicionIncreaseRate * 0.5f) * Time.deltaTime;
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

    private float GetTargetLightModifier()
    {
        return 1.0f;
    }

    public void ReactToLightExposure()
    {
        if (currentState != State.Catch && !isRetreating)
        {
            TriggerRetreat();
        }
    }

    public void TriggerRetreat()
    {
        if (currentState == State.Catch || isRetreating) return;
        StartCoroutine(RetreatRoutine());
    }

    private IEnumerator RetreatRoutine()
    {
        isRetreating = true;
        ChangeState(State.Retreat);

        suspicion = 0f;
        hasAlerted = false;
        UpdateHeadAim(0f);

        Transform bestWaypoint = GetFurthestHiddenWaypoint();
        if (bestWaypoint != null)
        {
            agent.SetDestination(bestWaypoint.position);
        }

        while (agent.pathPending || agent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        UpdateAnimation(0f);
        yield return new WaitForSeconds(retreatRecoupTime);

        isRetreating = false;
        ChangeState(State.Patrol);
        GoToNextWaypoint();
    }

    private Transform GetFurthestHiddenWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return null;

        Transform bestPoint = null;
        float maxDistance = -1f;

        if (mainCamera == null) mainCamera = Camera.main;

        foreach (Transform wp in waypoints)
        {
            float dist = Vector3.Distance(transform.position, wp.position);

            bool isVisibleToPlayer = false;
            if (mainCamera != null)
            {
                Vector3 screenPoint = mainCamera.WorldToViewportPoint(wp.position);
                bool inFrustum = screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1;

                if (inFrustum)
                {
                    Vector3 dir = (wp.position - mainCamera.transform.position).normalized;
                    float checkDist = Vector3.Distance(mainCamera.transform.position, wp.position);
                    if (!Physics.Raycast(mainCamera.transform.position, dir, checkDist, groundLayers))
                    {
                        isVisibleToPlayer = true;
                    }
                }
            }

            if (!isVisibleToPlayer && dist > maxDistance)
            {
                maxDistance = dist;
                bestPoint = wp;
            }
        }

        if (bestPoint == null)
        {
            foreach (Transform wp in waypoints)
            {
                float dist = Vector3.Distance(transform.position, wp.position);
                if (dist > maxDistance)
                {
                    maxDistance = dist;
                    bestPoint = wp;
                }
            }
        }

        return bestPoint;
    }

    private void CheckForDoors()
    {
        if (agent.velocity.sqrMagnitude < 0.1f) return;

        Vector3 origin = transform.position + Vector3.up * doorCheckHeightOffset;
        Vector3 direction = agent.velocity.normalized;

        if (Physics.SphereCast(origin, doorSphereCastRadius, direction, out RaycastHit hit, doorInteractionDistance, doorLayer))
        {
            DoorScript door = hit.collider.GetComponentInParent<DoorScript>();
            if (door != null && !door.open)
            {
                door.ForceOpen();
            }
        }
    }

    void UpdateAnimation(float targetSpeed)
    {
        currentSpeedPercent = Mathf.MoveTowards(currentSpeedPercent, targetSpeed, acceleration * Time.deltaTime);
        animator.SetFloat("Speed", currentSpeedPercent);

        if (currentState == State.Chase || currentState == State.Retreat)
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

        isIdle = false;
        currentState = newState;

        switch (newState)
        {
            case State.Chase:
            case State.Retreat:
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

            Vector3 idleForward = targetLookRotation * Vector3.forward;
            AlignToSurface(idleForward);

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
        agent.SetDestination(lastKnownPlayerPosition);

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            if (!isIdle)
            {
                StartIdle();
            }
            else
            {
                idleTimer -= Time.deltaTime;
                Vector3 idleForward = targetLookRotation * Vector3.forward;
                AlignToSurface(idleForward);

                if (idleTimer <= 0f)
                {
                    isIdle = false;
                    suspicion = 0f;
                    ChangeState(State.Patrol);
                }
            }
        }
        else
        {
            isIdle = false;
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
        if (canSeePlayer)
        {
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

        AlignToSurface((lookPos - transform.position).normalized);

        CinemachineThirdPersonFollow follow =
            currentTarget.gameObject.GetComponentInChildren<CinemachineThirdPersonFollow>();

        if (follow != null)
        {
            follow.AvoidObstacles.Enabled = false;
        }

        animator.SetTrigger("Attack");
        StartCoroutine(RestartGame());
    }

    IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(restartDelay);
    }

    void StartIdle()
    {
        isIdle = true;
        idleTimer = idleTime;
        agent.ResetPath();

        float randomAngle = Random.Range(-lookAroundAngle, lookAroundAngle);
        targetLookRotation = Quaternion.Euler(0, transform.eulerAngles.y + randomAngle, 0);

        PlayBreatheSound();
    }

    void RotateTowardsMovementDirection()
    {
        Vector3 forwardDirection = agent.velocity.sqrMagnitude > 0.1f
            ? agent.velocity.normalized
            : transform.forward;

        AlignToSurface(forwardDirection);
    }

    private void AlignToSurface(Vector3 desiredForward)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * groundCheckHeightOffset;
        Vector3 groundNormal = Vector3.up;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayers))
        {
            groundNormal = hit.normal;
        }

        Vector3 projectedForward = Vector3.ProjectOnPlane(desiredForward, groundNormal).normalized;

        if (projectedForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, groundNormal);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * surfaceAlignmentSpeed
            );
        }
    }

    private void OnCrawl(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            PlayCrawlSound();
        }
    }

    private void PlayCrawlSound()
    {
        if (crawlClips != null && crawlClips.Length > 0 && crawlSource != null)
        {
            int index = Random.Range(0, crawlClips.Length);
            crawlSource.clip = crawlClips[index];
            crawlSource.volume = crawlAudioVolume;
            crawlSource.pitch = Random.Range(minPitch, maxPitch);
            crawlSource.Play();
        }
    }

    private void PlayBreatheSound()
    {
        if (breatheClips != null && breatheClips.Length > 0)
        {
            int index = Random.Range(0, breatheClips.Length);

            if (voiceSource != null)
            {
                voiceSource.spatialBlend = 1f; // Force 3D spatial blend
                voiceSource.PlayOneShot(breatheClips[index], breatheAudioVolume);
            }
        }
    }

    public bool IsChasing()
    {
        return currentState == State.Chase;
    }
}