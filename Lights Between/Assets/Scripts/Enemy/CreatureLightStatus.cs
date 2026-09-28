using UnityEngine;

public class CreatureLightStatus : MonoBehaviour
{
    [Header("Exposure Settings")]
    [SerializeField] private float exposureThreshold = 100f;
    [SerializeField] private float exposureBuildRate = 50f;
    [SerializeField] private float exposureDecayRate = 20f;

    [Header("Current Status")]
    public bool isInLight;
    public float currentExposure;

    private int lightCount;
    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    private void Update()
    {
        if (isInLight)
        {
            currentExposure += exposureBuildRate * Time.deltaTime;
            if (currentExposure >= exposureThreshold)
            {
                currentExposure = 0f;
                if (enemyAI != null)
                {
                    enemyAI.TriggerRetreat();
                }
            }
        }
        else if (currentExposure > 0f)
        {
            currentExposure -= exposureDecayRate * Time.deltaTime;
            currentExposure = Mathf.Max(0f, currentExposure);
        }
    }

    public void EnterLight()
    {
        lightCount++;
        isInLight = true;
    }

    public void ExitLight()
    {
        lightCount--;

        if (lightCount <= 0)
        {
            lightCount = 0;
            isInLight = false;
        }
    }

    public void ResetLightStatus()
    {
        lightCount = 0;
        isInLight = false;
        currentExposure = 0f;
    }
}