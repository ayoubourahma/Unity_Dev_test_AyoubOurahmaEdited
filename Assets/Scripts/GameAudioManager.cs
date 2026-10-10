using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameAudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip winLevelClip;
    [SerializeField] private AudioClip loseLevelClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip tankShootingClip;

    private static GameAudioManager instance;
    private static bool creatingPersistentInstance;

    private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
    private AudioSource audioSource;
    private bool initialized;

    private void Awake()
    {
        if (creatingPersistentInstance)
        {
            return;
        }

        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        creatingPersistentInstance = true;
        GameObject persistentHost = new GameObject("Persistent Game Audio");
        DontDestroyOnLoad(persistentHost);
        GameAudioManager persistentManager = persistentHost.AddComponent<GameAudioManager>();
        creatingPersistentInstance = false;

        persistentManager.winLevelClip = winLevelClip;
        persistentManager.loseLevelClip = loseLevelClip;
        persistentManager.buttonClickClip = buttonClickClip;
        persistentManager.tankShootingClip = tankShootingClip;
        instance = persistentManager;
        persistentManager.Initialize();

        Destroy(this);
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterSceneButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        registeredButtons.Clear();
        RegisterSceneButtons();
    }

    private void RegisterSceneButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            RegisterButton(button);
        }
    }

    public static void RegisterButton(Button button)
    {
        if (instance == null || button == null || !instance.registeredButtons.Add(button))
        {
            return;
        }

        button.onClick.AddListener(instance.PlayButtonClick);
    }

    public static void PlayTankShot()
    {
        instance?.PlayOneShot(instance.tankShootingClip);
    }

    public static void PlayWinLevel()
    {
        instance?.PlayOneShot(instance.winLevelClip);
    }

    public static void PlayLoseLevel()
    {
        instance?.PlayOneShot(instance.loseLevelClip);
    }

    private void PlayButtonClick()
    {
        PlayOneShot(buttonClickClip);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }
}
