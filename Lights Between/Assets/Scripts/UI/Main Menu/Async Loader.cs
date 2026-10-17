using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class AsyncLoader : MonoBehaviour
{
    [Header("Menu Screens")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject mainMenu;

    [Header("Slider UI")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TextMeshProUGUI percentageTxt;

    [Header("Audio")]
    [SerializeField] private MenuMusicManager menuMusicManager;

    [SerializeField] private bool isMainMenu = false;

    public void LoadLevelButton(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[AsyncLoader] Invalid scene index: {levelIndex}. Check your Build Settings.");
            return;
        }

        StartCoroutine(LoadLevelRoutine(levelIndex));
    }

    public void LoadLevelButton(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[AsyncLoader] Scene name is null or empty.");
            return;
        }

        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    private IEnumerator LoadLevelRoutine(object sceneIdentifier)
    {
        if ((isMainMenu))
        {
            if (menuMusicManager != null)
            {
                menuMusicManager.FadeOutAndStop(1f);
            }
        }
        

        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }

        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        AsyncOperation loadOperation = sceneIdentifier is int index
            ? SceneManager.LoadSceneAsync(index)
            : SceneManager.LoadSceneAsync((string)sceneIdentifier);

        if (loadOperation == null)
        {
            Debug.LogError($"[AsyncLoader] Failed to start async load for: {sceneIdentifier}");
            yield break;
        }

        while (!loadOperation.isDone)
        {
            float progressValue = Mathf.Clamp01(loadOperation.progress / 0.9f);

            if (loadingSlider != null)
            {
                loadingSlider.value = progressValue;
            }

            if (percentageTxt != null)
            {
                percentageTxt.text = $"{Mathf.RoundToInt(progressValue * 100f)}%";
            }

            yield return null;
        }
    }
}