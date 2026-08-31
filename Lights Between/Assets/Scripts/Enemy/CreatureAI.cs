using System;
using UnityEngine;
using UnityEngine.AI;

public class CreatureAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject creatureVisuals;
    [SerializeField] private Collider creatureCollider;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private CreatureVoiceAudio creatureVoiceAudio;
    [SerializeField] private CreatureLightStatus creatureLightStatus;
    [SerializeField] private GeneratorPowerSystem generatorSystem;
    [SerializeField] private Transform breakerBoxTransform;

    [Header("Door Handling")]
    [SerializeField] private float doorCheckDistance = 2f;
    [SerializeField] private LayerMask doorLayer;

    [Header("Spawn & Attack Points")]
    [SerializeField] private Transform[] attackSpawnPoints;
    [SerializeField] private float minSpawnDistance = 5f;
    [SerializeField] private float maxSpawnDistance = 25f;
    [SerializeField] private float playerFOVAngle = 60f;

    [Header("Dynamic AI Settings")]
    [SerializeField] private float directHuntThreshold = 85f;
    [SerializeField] private float tensionIncreaseRate = 2f;
    [SerializeField] private float stalkDistance = 12f;

    [Header("Reveal Settings")]
    [SerializeField] private Transform currentRevealPoint;
    [SerializeField] private bool lookAtPlayerWhenRevealed = true;
    [SerializeField] private float lookAtPlayerSpeed = 6f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float killDistance = 1.4f;

    [Header("Light Reaction")]
    [SerializeField] private bool reactToLight = true;
    [SerializeField] private float lightReactionDelay = 0.2f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float fleeDistance = 8f;
    [SerializeField] private float fleeDuration = 1.5f;
    [SerializeField] private float disappearDistanceFromPlayer = 10f;

    [Header("Animation")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private float animationBlendSpeed = 8f;

    public event Action OnCreatureDisappeared;

    private ICreatureState currentState;
    private float animationSpeed;
    private float lightContactTimer;

    public float TensionLevel { get; set; }

    public Transform Player => player;
    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;
    public CreatureVoiceAudio VoiceAudio => creatureVoiceAudio;
    public CreatureLightStatus LightSensor => creatureLightStatus;
    public GeneratorPowerSystem GeneratorSystem => generatorSystem;
    public Transform BreakerBoxTransform => breakerBoxTransform;
    public GameObject Visuals => creatureVisuals;

    public float ChaseSpeed => chaseSpeed;
    public float KillDistance => killDistance;
    public float FleeSpeed => fleeSpeed;
    public float FleeDistance => fleeDistance;
    public float FleeDuration => fleeDuration;
    public float DisappearDistance => disappearDistanceFromPlayer;
    public float TensionIncreaseRate => tensionIncreaseRate;
    public float DirectHuntThreshold => directHuntThreshold;
    public float StalkDistance => stalkDistance;
    public bool LookAtPlayerWhenRevealed => lookAtPlayerWhenRevealed;
    public float LookAtPlayerSpeed => lookAtPlayerSpeed;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (creatureVoiceAudio == null) creatureVoiceAudio = GetComponent<CreatureVoiceAudio>();
        if (creatureLightStatus == null) creatureLightStatus = GetComponent<CreatureLightStatus>();
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main;
    }

    private void Start()
    {
        ChangeState(new CreatureHiddenState(this));
    }

    private void Update()
    {
        if (player == null) return;

        HandleLightReaction();
        CheckForDoorsAhead();
        currentState?.Update();
        UpdateAnimation();
    }

    private void CheckForDoorsAhead()
    {
        if (agent == null || !agent.hasPath || currentState is CreatureHiddenState || currentState is CreatureDisabledState)
            return;

        Vector3 moveDirection = agent.desiredVelocity.sqrMagnitude > 0.1f
            ? agent.desiredVelocity.normalized
            : transform.forward;

        Vector3 rayOrigin = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(rayOrigin, moveDirection, out RaycastHit hit, doorCheckDistance, doorLayer))
        {
            DoorScript door = hit.collider.GetComponentInParent<DoorScript>();
            if (door != null && !door.open)
            {
                door.ForceOpen();
            }
        }
    }

    public void ChangeState(ICreatureState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void SetLightReaction(bool state)
    {
        reactToLight = state;
        lightContactTimer = 0f;

        if (creatureLightStatus != null)
        {
            creatureLightStatus.ResetLightStatus();
        }
    }

    private void HandleLightReaction()
    {
        if (!reactToLight) return;
        if (creatureLightStatus == null) return;
        if (currentState is CreatureHiddenState) return;
        if (currentState is CreatureDisabledState) return;
        if (currentState is CreatureFleeState) return;

        if (creatureLightStatus.isInLight)
        {
            lightContactTimer += Time.deltaTime;
        }
        else
        {
            lightContactTimer = 0f;
        }

        if (lightContactTimer >= lightReactionDelay)
        {
            lightContactTimer = 0f;
            ChangeState(new CreatureFleeState(this));
        }
    }

    public Transform GetBestAttackPoint()
    {
        if (attackSpawnPoints == null || attackSpawnPoints.Length == 0 || player == null)
        {
            return null;
        }

        Transform bestPoint = null;
        float bestScore = float.MinValue;

        Vector3 viewDirection = playerCamera != null ? playerCamera.transform.forward : player.forward;

        foreach (Transform point in attackSpawnPoints)
        {
            if (point == null) continue;

            Vector3 toPoint = point.position - player.position;
            float distance = toPoint.magnitude;

            if (distance < minSpawnDistance || distance > maxSpawnDistance)
            {
                continue;
            }

            Vector3 directionToPoint = toPoint.normalized;
            float angle = Vector3.Angle(viewDirection, directionToPoint);

            bool isBehindPlayer = angle > (playerFOVAngle * 0.5f);

            float score = distance;
            if (isBehindPlayer)
            {
                score += 100f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = point;
            }
        }

        if (bestPoint == null)
        {
            float fallbackDistance = float.MaxValue;
            foreach (Transform point in attackSpawnPoints)
            {
                if (point == null) continue;
                float dist = Vector3.Distance(player.position, point.position);
                if (dist < fallbackDistance)
                {
                    fallbackDistance = dist;
                    bestPoint = point;
                }
            }
        }

        return bestPoint;
    }

    public void StartChaseFromBestPoint()
    {
        Transform bestPoint = GetBestAttackPoint();
        StartChaseFromPoint(bestPoint);
    }

    public void RevealAtPoint(Transform revealPoint)
    {
        if (currentState is CreatureDisabledState) return;
        if (revealPoint == null) return;

        currentRevealPoint = revealPoint;
        ChangeState(new CreatureRevealState(this, currentRevealPoint));
    }

    public void RevealAtCurrentPoint()
    {
        if (currentRevealPoint == null) return;
        RevealAtPoint(currentRevealPoint);
    }

    public void Disappear()
    {
        if (currentState is CreatureHiddenState) return;
        if (currentState is CreatureDisabledState) return;

        ChangeState(new CreatureHiddenState(this));
        OnCreatureDisappeared?.Invoke();
    }

    public void DisappearAndDisable()
    {
        if (currentState is CreatureDisabledState) return;

        ChangeState(new CreatureDisabledState(this));
        OnCreatureDisappeared?.Invoke();
    }

    public void StartChaseFromPoint(Transform chaseStartPoint)
    {
        if (currentState is CreatureDisabledState) return;

        if (chaseStartPoint != null)
        {
            WarpToPoint(chaseStartPoint);
        }

        ChangeState(new CreatureChaseState(this));
    }

    public void StartChaseFromCurrentPosition()
    {
        if (currentState is CreatureDisabledState) return;
        ChangeState(new CreatureChaseState(this));
    }

    public void StopChaseAndHide()
    {
        if (!(currentState is CreatureChaseState)) return;

        ChangeState(new CreatureHiddenState(this));
        OnCreatureDisappeared?.Invoke();
    }

    public void StartDynamicBehavior()
    {
        if (currentState is CreatureDisabledState) return;
        ChangeState(new CreatureDynamicState(this));
    }

    public void WarpToPoint(Transform point)
    {
        if (point == null) return;

        if (agent != null && agent.enabled)
        {
            agent.Warp(point.position);
        }
        else
        {
            transform.position = point.position;
        }

        transform.rotation = point.rotation;
    }

    public void SetVisibility(bool isVisible)
    {
        if (creatureVisuals != null) creatureVisuals.SetActive(isVisible);
        if (creatureCollider != null) creatureCollider.enabled = isVisible;
    }

    private void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        float targetSpeed = agent.velocity.magnitude;

        animationSpeed = Mathf.Lerp(
            animationSpeed,
            targetSpeed,
            Time.deltaTime * animationBlendSpeed
        );

        animator.SetFloat(speedParameter, animationSpeed);
    }
}