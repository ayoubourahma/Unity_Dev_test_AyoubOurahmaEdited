using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelEndController : MonoBehaviour
{
    [SerializeField] private GameObject winPanel;

    private void Awake()
    {
        if (winPanel == null)
        {
            Debug.LogError("Assign the existing win panel to LevelEndController.", this);
            return;
        }

        winPanel.SetActive(false);
    }

    public static void ShowWin()
    {
        LevelEndController controller = FindFirstObjectByType<LevelEndController>();
        if (controller == null)
        {
            Debug.LogError("This gameplay scene has no LevelEndController.");
            return;
        }

        if (controller.winPanel == null)
        {
            Debug.LogError("Assign the existing win panel to LevelEndController.", controller);
            return;
        }

        controller.winPanel.SetActive(true);
        Time.timeScale = 0f;
        GameAudioManager.PlayWinLevel();
    }

    private void LoadNextLevel()
    {
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int buildIndex = currentBuildIndex + 1; buildIndex < sceneCount; buildIndex++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (!string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase))
            {
                Time.timeScale = 1f;
                SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
                return;
            }
        }

        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            return;
        }

        Debug.LogError("No next gameplay scene or MainMenu is available in Build Settings.");
    }
}
