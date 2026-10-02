using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class AsyncLoader : MonoBehaviour
{
    [Header("Menu Screens")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject mainMenu;

    [Header("Slider")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TextMeshProUGUI percentageTxt;

    [SerializeField] MenuMusicManager menuMusicManager;

    public void LoadLevelButton(int levelIndex)
    {
        if (menuMusicManager != null)
        {
            menuMusicManager.FadeOutAndStop(1f);
        }

        mainMenu.SetActive(false);
        loadingScreen.SetActive(true);

        StartCoroutine(LoadLevelAsync(levelIndex));
    }

    IEnumerator LoadLevelAsync(int levelIndex)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(levelIndex);

        while (!loadOperation.isDone)
        {
            float progressValue = Mathf.Clamp01(loadOperation.progress / 0.9f);

            loadingSlider.value = progressValue;
            percentageTxt.text = Mathf.RoundToInt(progressValue * 100f) + "%";

            yield return null;
        }
    }
}