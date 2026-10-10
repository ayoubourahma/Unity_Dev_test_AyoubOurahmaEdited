using System;
using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimeDebugPanel : MonoBehaviour
{
    private const int FrameSampleCapacity = 300;
    private const float MetricsRefreshInterval = 0.25f;

    private static RuntimeDebugPanel instance;
    private static bool constructingPersistentInstance;

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private readonly float[] frameSamples = new float[FrameSampleCapacity];
    private readonly float[] sortedFrameSamples = new float[FrameSampleCapacity];
    private readonly StringBuilder metricsBuilder = new StringBuilder(256);

    private InputAction toggleAction;
    private Canvas canvas;
    private GameObject panel;
    private TMP_Text metricsText;
    private TMP_Text statusText;
    private Button godModeButton;
    private TankHealth playerHealth;
    private int nextFrameSample;
    private int frameSampleCount;
    private int spawnCount;
    private float metricsRefreshTimer;
    private bool initialized;

    private void Awake()
    {
        if (constructingPersistentInstance)
        {
            return;
        }

        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;

        constructingPersistentInstance = true;
        GameObject persistentHost = new GameObject("Runtime Debug Panel");
        DontDestroyOnLoad(persistentHost);
        RuntimeDebugPanel persistentPanel = persistentHost.AddComponent<RuntimeDebugPanel>();
        constructingPersistentInstance = false;

        persistentPanel.enemyPrefab = enemyPrefab;
        persistentPanel.mainMenuSceneName = mainMenuSceneName;
        instance = persistentPanel;
        persistentPanel.Initialize();

        Destroy(this);
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        toggleAction = new InputAction("Toggle Debug Panel", InputActionType.Button);
        toggleAction.AddBinding("<Keyboard>/f3");
        toggleAction.AddBinding("<Keyboard>/backquote");
        toggleAction.AddBinding("<Gamepad>/select");

        toggleAction.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
        playerHealth = FindFirstObjectByType<TankHealth>();
        BuildPanel();
    }

    private void Update()
    {
        if (instance != this)
        {
            return;
        }

        if (toggleAction.WasPressedThisFrame() && panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }

        float frameTime = Mathf.Max(Time.unscaledDeltaTime, 0.000001f);
        frameSamples[nextFrameSample] = frameTime;
        nextFrameSample = (nextFrameSample + 1) % FrameSampleCapacity;
        frameSampleCount = Mathf.Min(frameSampleCount + 1, FrameSampleCapacity);

        if (panel == null || !panel.activeSelf)
        {
            return;
        }

        metricsRefreshTimer += Time.unscaledDeltaTime;
        if (metricsRefreshTimer >= MetricsRefreshInterval)
        {
            metricsRefreshTimer = 0f;
            RefreshMetrics(frameTime);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerHealth = FindFirstObjectByType<TankHealth>();
        Time.timeScale = 1f;

    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (toggleAction != null)
        {
            toggleAction.Disable();
            toggleAction.Dispose();
        }

        if (instance == this)
        {
            instance = null;
            Time.timeScale = 1f;
        }
    }

    private void BuildPanel()
    {
        GameObject canvasObject = new GameObject(
            "Debug Overlay Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Runtime Debug Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(430f, 462f);
        panel.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f, 0.94f);

        CreateLabel(panelRect, "DEBUG PANEL  [F3 / ` / View]", 20f, 18f, -14f, 394f, 30f);
        metricsText = CreateLabel(panelRect, "Collecting performance samples...", 17f, 18f, -52f, 394f, 132f);

        godModeButton = CreateButton(panelRect, "God Mode: OFF", 16f, -194f, 190f, ToggleGodMode);
        CreateButton(panelRect, "Spawn Enemy", 218f, -194f, 196f, SpawnEnemy);
        CreateButton(panelRect, "Complete / Skip Level", 16f, -242f, 398f, CompleteLevel);
        CreateButton(panelRect, "Slow 0.5x", 16f, -290f, 126f, () => SetGameSpeed(0.5f));
        CreateButton(panelRect, "Normal 1.0x", 152f, -290f, 126f, () => SetGameSpeed(1f));
        CreateButton(panelRect, "Fast 1.5x", 288f, -290f, 126f, () => SetGameSpeed(1.5f));
        statusText = CreateLabel(panelRect, "Ready", 14f, 18f, -344f, 394f, 96f);

        panel.transform.SetAsLastSibling();
        panel.SetActive(false);
    }

    private TMP_Text CreateLabel(
        RectTransform parent,
        string text,
        float fontSize,
        float x,
        float y,
        float width,
        float height)
    {
        GameObject labelObject = new GameObject(
            text,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform rect = (RectTransform)labelObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.text = text;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        return label;
    }

    private Button CreateButton(
        RectTransform parent,
        string label,
        float x,
        float y,
        float width,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(
            label,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, 38f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.13f, 0.26f, 0.38f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        GameAudioManager.RegisterButton(button);

        TMP_Text buttonLabel = CreateLabel(rect, label, 15f, 8f, -7f, width - 16f, 24f);
        buttonLabel.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private void RefreshMetrics(float currentFrameTime)
    {
        float totalFrameTime = 0f;
        int oldestSample = (nextFrameSample - frameSampleCount + FrameSampleCapacity) % FrameSampleCapacity;

        for (int i = 0; i < frameSampleCount; i++)
        {
            float sample = frameSamples[(oldestSample + i) % FrameSampleCapacity];
            sortedFrameSamples[i] = sample;
            totalFrameTime += sample;
        }

        Array.Sort(sortedFrameSamples, 0, frameSampleCount);

        int slowestSampleCount = Mathf.Max(1, Mathf.CeilToInt(frameSampleCount * 0.01f));
        float slowestFrameTimeTotal = 0f;
        for (int i = frameSampleCount - slowestSampleCount; i < frameSampleCount; i++)
        {
            slowestFrameTimeTotal += sortedFrameSamples[i];
        }

        float currentFps = 1f / currentFrameTime;
        float averageFps = frameSampleCount / totalFrameTime;
        float onePercentLow = slowestSampleCount / slowestFrameTimeTotal;
        float minimumFps = 1f / sortedFrameSamples[frameSampleCount - 1];
        float managedMemoryMb = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);

        metricsBuilder.Clear();
        metricsBuilder.Append("Current FPS: ").Append(currentFps.ToString("F0"))
            .Append("\nSmoothed average FPS: ").Append(averageFps.ToString("F0"))
            .Append("\n1% Low FPS: ").Append(onePercentLow.ToString("F0"))
            .Append("   Min FPS: ").Append(minimumFps.ToString("F0"))
            .Append("\nFrame time: ").Append((currentFrameTime * 1000f).ToString("F1")).Append(" ms")
            .Append("\nManaged heap: ").Append(managedMemoryMb.ToString("F1")).Append(" MB")
            .Append("\nActive enemies: ").Append(EnemyTankHealth.ActiveEnemyCount)
            .Append("   Active bullets: ").Append(Projectile.ActiveProjectileCount);
        metricsText.text = metricsBuilder.ToString();

        if (playerHealth != null && godModeButton != null)
        {
            godModeButton.GetComponentInChildren<TMP_Text>().text =
                playerHealth.IsGodMode ? "God Mode: ON" : "God Mode: OFF";
        }
    }

    private void ToggleGodMode()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<TankHealth>();
        }

        if (playerHealth == null)
        {
            SetStatus("No player tank is active.");
            return;
        }

        playerHealth.SetGodMode(!playerHealth.IsGodMode);
        SetStatus(playerHealth.IsGodMode ? "God Mode enabled." : "God Mode disabled.");
        RefreshMetrics(Mathf.Max(Time.unscaledDeltaTime, 0.000001f));
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            SetStatus("Assign an enemy prefab to RuntimeDebugPanel.");
            Debug.LogError("RuntimeDebugPanel needs an enemy prefab to spawn enemies.", this);
            return;
        }

        TankController player = FindFirstObjectByType<TankController>();
        if (player == null)
        {
            SetStatus("Cannot spawn: no player tank is active.");
            return;
        }

        float angle = spawnCount * 137.5f * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 8f;
        Vector3 spawnPosition = player.transform.position + offset;
        Vector3 facing = player.transform.position - spawnPosition;
        Quaternion spawnRotation = facing.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(facing)
            : player.transform.rotation;
        Instantiate(enemyPrefab, spawnPosition, spawnRotation);
        spawnCount++;
        SetStatus("Enemy spawned for QA.");
    }

    private void CompleteLevel()
    {
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int offset = 1; offset < sceneCount; offset++)
        {
            int buildIndex = (currentBuildIndex + offset) % sceneCount;
            string nextScene = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            string nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextScene);
            if (buildIndex != currentBuildIndex &&
                !string.Equals(nextSceneName, mainMenuSceneName, StringComparison.OrdinalIgnoreCase))
            {
                Time.timeScale = 1f;
                SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
                return;
            }
        }

        SetStatus("Level complete. Add the next gameplay scene after this one in Build Settings.");
    }

    private void SetGameSpeed(float timeScale)
    {
        Time.timeScale = timeScale;
        SetStatus($"Game speed set to {timeScale.ToString("F1")}x.");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
