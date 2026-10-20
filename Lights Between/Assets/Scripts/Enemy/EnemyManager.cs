using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    public static bool IsGameOver { get; private set; }

    [Header("Enemy References")]
    [SerializeField] private GameObject enemyObject;
    [SerializeField] private EnemyAI enemyAI;

    [Header("Kill Sequence References")]
    [SerializeField] private PlayerKill playerKillSequence;
    [SerializeField] private FPSController playerController;

    [Header("Game Over & Reset Settings")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private float gameOverDelay = 2.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsGameOver = false;
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
        IsGameOver = true;
        ResolveReferences();

        if (playerKillSequence != null)
        {
            playerKillSequence.ExecuteCatchSequence(playerController, enemyObject);
        }

        StartCoroutine(ShowGameOverRoutine());
    }

    public void ShowGameOverScreen()
    {
        IsGameOver = true;

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(true);
        }

        if (playerKillSequence != null)
        {
            playerKillSequence.StopJumpscareAnimation();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        IsGameOver = false;

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(false);
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private IEnumerator ShowGameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);
        ShowGameOverScreen();
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

    public void LoadLevelInt(int levelIndex)
    {
        IsGameOver = false;
        SceneManager.LoadScene(levelIndex);
    }
}