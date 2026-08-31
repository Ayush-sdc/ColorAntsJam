using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string CurrentLevelPrefKey = "CurrentLevel";

    [Header("Configuration")]
    [Tooltip("Reference to the LevelDatabase ScriptableObject containing all levels.")]
    [SerializeField] private LevelDatabase levelDatabase;

    [Tooltip("Should levels loop once the player completes the last configured level?")]
    [SerializeField] private bool loopWhenExceeded = true;

    [Header("Debug Settings")]
    [Tooltip("Override the level loaded on Start for testing purposes without changing PlayerPrefs.")]
    [SerializeField] private bool overrideLevelForTesting = false;
    [SerializeField] private int testLevelNumber = 1;

    [Header("Runtime State")]
    [SerializeField] private int currentLevelNumber;
    [SerializeField] private Level activeLevel;

    public Level ActiveLevel => activeLevel;
    public int CurrentLevelNumber => currentLevelNumber;
    public LevelDatabase Database => levelDatabase;

    /// <summary>
    /// Event triggered whenever a level is loaded and initialized.
    /// Passes the loaded Level ScriptableObject data.
    /// </summary>
    public static event Action<Level> OnLevelLoaded;

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
        LoadCurrentLevel();
    }

    /// <summary>
    /// Reads the current level index from PlayerPrefs (or debug override) and loads the corresponding Level data.
    /// </summary>
    public void LoadCurrentLevel()
    {
        if (overrideLevelForTesting)
        {
            currentLevelNumber = testLevelNumber;
        }
        else
        {
            // Default to Level 1 if no PlayerPrefs key exists yet
            currentLevelNumber = PlayerPrefs.GetInt(CurrentLevelPrefKey, 1);
        }

        LoadLevel(currentLevelNumber);
    }

    /// <summary>
    /// Loads a specific level by its 1-based index from the LevelDatabase.
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        if (levelDatabase == null)
        {
            Debug.LogError("[LevelManager] LevelDatabase reference is missing! Please assign it in the Inspector.", this);
            return;
        }

        currentLevelNumber = levelNumber;
        activeLevel = levelDatabase.GetLevel(currentLevelNumber, loopWhenExceeded);

        if (activeLevel == null)
        {
            Debug.LogWarning($"[LevelManager] Failed to retrieve Level data for Level {currentLevelNumber}.", this);
            return;
        }

        Debug.Log($"[LevelManager] Loaded Level {currentLevelNumber}: '{activeLevel.LevelName}'.", this);

        // Notify any gameplay systems listening for level loading
        OnLevelLoaded?.Invoke(activeLevel);
    }

    /// <summary>
    /// Increments the current level in PlayerPrefs and loads it.
    /// </summary>
    public void AdvanceToNextLevel()
    {
        currentLevelNumber++;
        PlayerPrefs.SetInt(CurrentLevelPrefKey, currentLevelNumber);
        PlayerPrefs.Save();

        LoadLevel(currentLevelNumber);
    }

    /// <summary>
    /// Reloads the active level.
    /// </summary>
    public void RestartCurrentLevel()
    {
        LoadLevel(currentLevelNumber);
    }

    /// <summary>
    /// Resets PlayerPrefs progress back to Level 1.
    /// </summary>
    [ContextMenu("Reset Progress to Level 1")]
    public void ResetProgress()
    {
        PlayerPrefs.SetInt(CurrentLevelPrefKey, 1);
        PlayerPrefs.Save();
        currentLevelNumber = 1;
        Debug.Log("[LevelManager] PlayerPrefs level progress reset to Level 1.");
    }
}
