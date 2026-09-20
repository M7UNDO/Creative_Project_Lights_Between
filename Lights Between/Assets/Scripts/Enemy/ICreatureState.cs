/*using UnityEngine;
using UnityEngine.AI;

public interface ICreatureState
{
    void Enter();
    void Update();
    void Exit();
}

public class CreatureHiddenState : ICreatureState
{
    private CreatureAI ai;

    public CreatureHiddenState(CreatureAI ai)
    {
        this.ai = ai;
    }

    public void Enter()
    {
        if (ai.Agent != null)
        {
            ai.Agent.isStopped = true;
            ai.Agent.ResetPath();
        }

        ai.SetVisibility(false);

        if (ai.VoiceAudio != null)
        {
            ai.VoiceAudio.EnableWhispers(false);
        }

        if (ai.LightSensor != null)
        {
            ai.LightSensor.ResetLightStatus();
        }
    }

    public void Update() { }
    public void Exit() { }
}

public class CreatureDisabledState : ICreatureState
{
    private CreatureAI ai;

    public CreatureDisabledState(CreatureAI ai)
    {
        this.ai = ai;
    }

    public void Enter()
    {
        if (ai.Agent != null && ai.Agent.isActiveAndEnabled && ai.Agent.isOnNavMesh)
        {
            ai.Agent.isStopped = true;
            ai.Agent.ResetPath();
        }

        ai.SetVisibility(false);

        if (ai.VoiceAudio != null)
        {
            ai.VoiceAudio.EnableWhispers(false);
        }

        if (ai.LightSensor != null)
        {
            ai.LightSensor.ResetLightStatus();
        }
    }

    public void Update() { }
    public void Exit() { }
}

public class CreatureRevealState : ICreatureState
{
    private CreatureAI ai;
    private Transform revealPoint;

    public CreatureRevealState(CreatureAI ai, Transform revealPoint)
    {
        this.ai = ai;
        this.revealPoint = revealPoint;
    }

    public void Enter()
    {
        ai.WarpToPoint(revealPoint);
        ai.SetVisibility(true);

        if (ai.Agent != null)
        {
            ai.Agent.isStopped = true;
            ai.Agent.ResetPath();
        }

        if (ai.VoiceAudio != null)
        {
            ai.VoiceAudio.EnableWhispers(true);
        }
    }

    public void Update()
    {
        if (ai.LookAtPlayerWhenRevealed && ai.Player != null)
        {
            Vector3 direction = ai.Player.position - ai.Visuals.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                ai.Visuals.transform.rotation = Quaternion.Slerp(ai.Visuals.transform.rotation, targetRotation, Time.deltaTime * ai.LookAtPlayerSpeed);
            }
        }
    }

    public void Exit() { }
}

public class CreatureChaseState : ICreatureState
{
    private CreatureAI ai;

    public CreatureChaseState(CreatureAI ai)
    {
        this.ai = ai;
    }

    public void Enter()
    {
        ai.SetVisibility(true);

        if (ai.Agent != null)
        {
            ai.Agent.isStopped = false;
            ai.Agent.speed = ai.ChaseSpeed;
        }

        if (ai.VoiceAudio != null)
        {
            ai.VoiceAudio.EnableWhispers(false);
            ai.VoiceAudio.PlayChaseVoice();
        }
    }

    public void Update()
    {
        if (ai.Agent == null || ai.Player == null) return;

        ai.Agent.SetDestination(ai.Player.position);

        float distance = Vector3.Distance(ai.transform.position, ai.Player.position);
        if (distance <= ai.KillDistance)
        {
            FPSController playerController = ai.Player.GetComponent<FPSController>();
            PlayerKill playerKill = Object.FindFirstObjectByType<PlayerKill>(FindObjectsInactive.Include);

            if (playerController != null && playerKill != null)
            {
                playerKill.gameObject.SetActive(true);
                playerKill.ExecuteCatchSequence(playerController, ai.gameObject);
                ai.ChangeState(new CreatureDisabledState(ai));
            }
        }
    }

    public void Exit() { }
}

public class CreatureFleeState : ICreatureState
{
    private CreatureAI ai;
    private float stateTimer;
    private Vector3 fleeTarget;

    public CreatureFleeState(CreatureAI ai)
    {
        this.ai = ai;
    }

    public void Enter()
    {
        stateTimer = 0f;
        ai.SetVisibility(true);

        Vector3 away = (ai.transform.position - ai.Player.position).normalized;
        if (away.sqrMagnitude <= 0.01f) away = -ai.transform.forward;

        fleeTarget = ai.transform.position + away * ai.FleeDistance;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            fleeTarget = hit.position;
        }

        if (ai.Agent != null)
        {
            ai.Agent.isStopped = false;
            ai.Agent.speed = ai.FleeSpeed;
            ai.Agent.SetDestination(fleeTarget);
        }

        if (ai.VoiceAudio != null)
        {
            ai.VoiceAudio.EnableWhispers(false);
            ai.VoiceAudio.PlayLightReaction();
        }
    }

    public void Update()
    {
        stateTimer += Time.deltaTime;

        if (ai.Agent == null)
        {
            ai.Disappear();
            return;
        }

        float distance = Vector3.Distance(ai.transform.position, ai.Player.position);

        if (!ai.Agent.pathPending && ai.Agent.remainingDistance <= 0.8f)
        {
            ai.Disappear();
            return;
        }

        if (distance >= ai.DisappearDistance || stateTimer >= ai.FleeDuration)
        {
            ai.Disappear();
        }
    }

    public void Exit() { }
}

public enum SabotageTargetType
{
    LightSwitch,
    BreakerBox,
    Generator
}

public class CreatureDynamicState : ICreatureState
{
    private CreatureAI ai;
    private bool isSabotaging;

    private SabotageTargetType currentTargetType;
    private ElectricBoxBreaker targetBreaker;
    private GeneratorPowerSystem targetGenerator;

    public CreatureDynamicState(CreatureAI ai)
    {
        this.ai = ai;
    }

    public void Enter()
    {
        ai.SetVisibility(true);
        isSabotaging = false;

        if (ai.VoiceAudio != null)
        {
            ai.VoiceAudio.EnableWhispers(true);
        }
    }

    public void Update()
    {
        UpdateTension();
        EvaluateBehavior();
    }

    private void UpdateTension()
    {
        float darknessRatio = MapLightManager.Instance != null
            ? MapLightManager.Instance.DarknessPercentage
            : 0f;

        float dynamicAggression = ai.TensionIncreaseRate * (1f + darknessRatio);

        if (ai.GeneratorSystem != null && ai.GeneratorSystem.IsPowerOn)
        {
            ai.TensionLevel += dynamicAggression * Time.deltaTime;
        }
        else
        {
            ai.TensionLevel = Mathf.Max(0f, ai.TensionLevel - (ai.TensionIncreaseRate * 0.5f) * Time.deltaTime);
        }
    }

    private void EvaluateBehavior()
    {
        if (ai.TensionLevel >= ai.DirectHuntThreshold)
        {
            ai.ChangeState(new CreatureChaseState(ai));
            return;
        }

        if (!isSabotaging)
        {
            float darknessRatio = MapLightManager.Instance != null
                ? MapLightManager.Instance.DarknessPercentage
                : 0f;

            float sabotageChance = (0.01f + (darknessRatio * 0.05f)) * Time.deltaTime;

            if (Random.value < sabotageChance)
            {
                TrySelectSabotageTarget();
            }
        }

        if (isSabotaging)
        {
            ExecuteSabotage();
        }
        else
        {
            ExecuteStalk();
        }
    }

    private void TrySelectSabotageTarget()
    {
        int roll = Random.Range(0, 3);

        if (roll == 0 && ai.GeneratorSystem != null && ai.GeneratorSystem.IsPowerOn)
        {
            targetGenerator = ai.GeneratorSystem;
            currentTargetType = SabotageTargetType.Generator;
            isSabotaging = true;
            return;
        }

        if (roll == 1 && ai.BreakerBoxTransform != null)
        {
            ElectricBoxBreaker breaker = ai.BreakerBoxTransform.GetComponent<ElectricBoxBreaker>();
            if (breaker != null && breaker.IsGridActive)
            {
                targetBreaker = breaker;
                currentTargetType = SabotageTargetType.BreakerBox;
                isSabotaging = true;
                return;
            }
        }

        LightSwitch activeSwitch = FindRandomActiveLightSwitch();
        if (activeSwitch != null)
        {
            targetLightSwitch = activeSwitch;
            currentTargetType = SabotageTargetType.LightSwitch;
            isSabotaging = true;
        }
    }

    private LightSwitch FindRandomActiveLightSwitch()
    {
        LightSwitch[] switches = Object.FindObjectsByType<LightSwitch>(FindObjectsSortMode.None);
        System.Collections.Generic.List<LightSwitch> activeSwitches = new System.Collections.Generic.List<LightSwitch>();

        foreach (LightSwitch s in switches)
        {
            if (s != null && s.IsLightActive)
            {
                activeSwitches.Add(s);
            }
        }

        if (activeSwitches.Count == 0) return null;
        return activeSwitches[Random.Range(0, activeSwitches.Count)];
    }

    private void ExecuteSabotage()
    {
        Transform targetDestination = GetCurrentTargetTransform();

        if (targetDestination == null || ai.Agent == null)
        {
            ResetSabotageState();
            return;
        }

        ai.Agent.isStopped = false;
        ai.Agent.SetDestination(targetDestination.position);

        if (!ai.Agent.pathPending && ai.Agent.remainingDistance <= 1.5f)
        {
            PerformSabotageAction();
            ResetSabotageState();
            ai.Disappear();
        }
    }

    private Transform GetCurrentTargetTransform()
    {
        return currentTargetType switch
        {
            SabotageTargetType.Generator => targetGenerator != null ? targetGenerator.transform : null,
            SabotageTargetType.BreakerBox => targetBreaker != null ? targetBreaker.transform : null,
            SabotageTargetType.LightSwitch => targetLightSwitch != null ? targetLightSwitch.transform : null,
            _ => null
        };
    }

    private void PerformSabotageAction()
    {
        switch (currentTargetType)
        {
            case SabotageTargetType.Generator:
                if (targetGenerator != null && targetGenerator.IsPowerOn)
                {
                    targetGenerator.TurnGeneratorOff();
                }
                break;

            case SabotageTargetType.BreakerBox:
                if (targetBreaker != null)
                {
                    targetBreaker.TripBreaker();
                }
                break;

            case SabotageTargetType.LightSwitch:
                if (targetLightSwitch != null && targetLightSwitch.IsLightActive)
                {
                    targetLightSwitch.ToggleSwitch();
                }
                break;
        }
    }

    private void ResetSabotageState()
    {
        isSabotaging = false;
        targetGenerator = null;
        targetBreaker = null;
        targetLightSwitch = null;
    }

    private void ExecuteStalk()
    {
        if (ai.Agent == null || ai.Player == null) return;

        Vector3 stalkPosition = ai.Player.position - ai.Player.forward * ai.StalkDistance;
        if (NavMesh.SamplePosition(stalkPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            ai.Agent.isStopped = false;
            ai.Agent.SetDestination(hit.position);
        }
    }

    public void Exit() { }
}*/