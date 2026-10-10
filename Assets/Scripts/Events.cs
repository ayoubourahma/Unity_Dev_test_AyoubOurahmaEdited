using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Events : MonoBehaviour
{
   public void Replay()
    {
        Time.timeScale = 1f;
       SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
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

        Debug.LogError("No next gameplay scene or MainMenu is available in Build Settings.", this);
    }
}
