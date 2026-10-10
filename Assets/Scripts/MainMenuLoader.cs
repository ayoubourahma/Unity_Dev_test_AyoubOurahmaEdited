using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuLoader : MonoBehaviour
{
    [SerializeField] private string levelSceneName = "Test";
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text loadingText;

    private bool isLoading;

    private void Awake()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }
    }

    public void Play()
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(levelSceneName))
        {
            Debug.LogError("Set a level scene name on the Main Menu Loader.", this);
            return;
        }

        if (mainMenuPanel == null || loadingPanel == null)
        {
            Debug.LogError("Assign the Main Menu Panel and Loading Panel on the Main Menu Loader.", this);
            return;
        }

        if (mainMenuPanel == loadingPanel)
        {
            Debug.LogError("The Main Menu Panel and Loading Panel must be different objects.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(levelSceneName))
        {
            Debug.LogError(
                $"Cannot load scene '{levelSceneName}'. Add it to Build Settings and check its name.",
                this
            );
            return;
        }

        isLoading = true;
        Time.timeScale = 1f;

        if (playButton != null)
        {
            playButton.interactable = false;
        }
        else
        {
            Debug.LogError("Assign the Play Button on the Main Menu Loader.", this);
        }

        mainMenuPanel.SetActive(false);
        loadingPanel.SetActive(true);

        if (loadingText != null)
        {
            loadingText.text = "Loading...";
            loadingText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Assign the loading text to show feedback while the level loads.", this);
        }

        StartCoroutine(LoadLevelAsync());
    }

    private IEnumerator LoadLevelAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(levelSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"Unity could not start loading scene '{levelSceneName}'.", this);
            isLoading = false;

            if (playButton != null)
            {
                playButton.interactable = true;
            }

            mainMenuPanel.SetActive(true);
            loadingPanel.SetActive(false);

            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(false);
            }

            yield break;
        }

        yield return operation;
    }
}
