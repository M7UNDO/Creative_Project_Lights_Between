using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Enemy References")]
    [SerializeField] private GameObject enemyObject;
    [SerializeField] private EnemyAI enemyAI;

    [Header("Kill Sequence References")]
    [SerializeField] private PlayerKill playerKillSequence;
    [SerializeField] private FPSController playerController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ResolveReferences();
    }

    public void ActivateEnemy()
    {
        if (enemyObject != null)
        {
            enemyObject.SetActive(true);
        }
    }

    public void DeactivateEnemy()
    {
        if (enemyObject != null)
        {
            enemyObject.SetActive(false);
        }
    }

    public void TriggerKillSequence()
    {
        ResolveReferences();

        if (playerKillSequence != null)
        {
            playerKillSequence.ExecuteCatchSequence(playerController, enemyObject);
        }
    }

    private void ResolveReferences()
    {
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<FPSController>();
        }

        if (enemyObject != null && enemyAI == null)
        {
            enemyAI = enemyObject.GetComponent<EnemyAI>();
        }
    }
}