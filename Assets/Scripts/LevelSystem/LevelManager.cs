using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public const string CurrentLevelPrefKey = "CurrentLevel";

    [Header("Configuration")]
    [Tooltip("Reference to the production LevelDatabase ScriptableObject containing handmade levels.")]
    [SerializeField] private LevelDatabase levelDatabase;

    [Tooltip("Optional reference database containing reference progression (Levels 1-45).")]
    [SerializeField] private LevelDatabase referenceLevelDatabase;

    [Tooltip("When true, gameplay progression loads from ReferenceLevelDatabase instead of the production LevelDatabase.")]
    [SerializeField] private bool useReferenceLevelDatabase = false;

    [Tooltip("Should levels loop once the player completes the last configured level?")]
    [SerializeField] private bool loopWhenExceeded = false;

    [Header("Debug Settings")]
    [Tooltip("Override the level loaded on Start for testing purposes without changing PlayerPrefs.")]
    [SerializeField] private bool overrideLevelForTesting = false;
    [SerializeField] private int testLevelNumber = 1;

    [Header("Runtime State")]
    [SerializeField] private int currentLevelNumber;
    [SerializeField] private Level activeLevel;

    public Level ActiveLevel => activeLevel;
    public int CurrentLevelNumber => currentLevelNumber;
    public bool UseReferenceLevelDatabase
    {
        get => useReferenceLevelDatabase;
        set => useReferenceLevelDatabase = value;
    }

    public LevelDatabase ReferenceDatabase => referenceLevelDatabase;
    public LevelDatabase ActiveDatabase => (useReferenceLevelDatabase && referenceLevelDatabase != null) ? referenceLevelDatabase : levelDatabase;
    public LevelDatabase Database => ActiveDatabase;

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

#if UNITY_EDITOR
        if (referenceLevelDatabase == null)
        {
            referenceLevelDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Data/ReferenceLevelDatabase.asset");
        }
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
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
    /// Loads a specific level by its 1-based index from the active LevelDatabase.
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        var db = ActiveDatabase;
        if (db == null)
        {
            Debug.LogError("[LevelManager] LevelDatabase reference is missing! Please assign it in the Inspector.", this);
            return;
        }

        int maxLevels = db.LevelCount > 0 ? db.LevelCount : 45;
        currentLevelNumber = Mathf.Clamp(levelNumber, 1, maxLevels);
        activeLevel = db.GetLevel(currentLevelNumber, loopWhenExceeded);

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
    /// From Level 45, remains on Level 45.
    private const string HighestCompletedLevelPrefKey = "HighestCompletedLevel";

    /// <summary>
    /// Highest level number completed by the player in campaign progression.
    /// Replaying a level does not decrement or artificially inflate this value.
    /// </summary>
    public int HighestCompletedLevel => PlayerPrefs.GetInt(HighestCompletedLevelPrefKey, 0);

    /// <summary>
    /// Records completion of a level. Persists HighestCompletedLevel if levelNumber is higher.
    /// Returns true if this is a first-time completion of this level.
    /// </summary>
    public bool RecordLevelCompleted(int levelNumber)
    {
        int prevHighest = HighestCompletedLevel;
        if (levelNumber > prevHighest)
        {
            PlayerPrefs.SetInt(HighestCompletedLevelPrefKey, levelNumber);
            PlayerPrefs.Save();
            Debug.Log($"[LevelManager] New campaign milestone reached! HighestCompletedLevel={levelNumber} (previously {prevHighest})");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Increments the current level in PlayerPrefs and loads it.
    /// Defines a graceful campaign completion state when exceeding the final level.
    /// </summary>
    public void AdvanceToNextLevel()
    {
        var db = ActiveDatabase;
        int maxLevels = (db != null && db.LevelCount > 0) ? db.LevelCount : 45;

        if (currentLevelNumber >= maxLevels)
        {
            Debug.Log($"[LevelManager] Campaign Completed! Final Level {currentLevelNumber} finished. Gracefully handling completion state.");
            if (loopWhenExceeded)
            {
                currentLevelNumber = 1;
                PlayerPrefs.SetInt(CurrentLevelPrefKey, currentLevelNumber);
                PlayerPrefs.Save();
                LoadLevel(currentLevelNumber);
            }
            else
            {
                // Remain cleanly on final level
                currentLevelNumber = maxLevels;
                PlayerPrefs.SetInt(CurrentLevelPrefKey, currentLevelNumber);
                PlayerPrefs.Save();
                LoadLevel(currentLevelNumber);
            }
            return;
        }

        currentLevelNumber = Mathf.Min(maxLevels, currentLevelNumber + 1);
        PlayerPrefs.SetInt(CurrentLevelPrefKey, currentLevelNumber);
        PlayerPrefs.Save();

        LoadLevel(currentLevelNumber);
    }

    /// <summary>
    /// Loads the previous level (clamped to Level 1 minimum).
    /// From Level 1, remains on Level 1.
    /// </summary>
    public void LoadPreviousLevel()
    {
        if (currentLevelNumber <= 1)
        {
            currentLevelNumber = 1;
            Debug.Log("[LevelManager] Already at first Level 1. Remaining on current level.");
            return;
        }

        currentLevelNumber = Mathf.Max(1, currentLevelNumber - 1);
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
        PlayerPrefs.DeleteKey(HighestCompletedLevelPrefKey);
        PlayerPrefs.Save();
        currentLevelNumber = 1;
        Debug.Log("[LevelManager] PlayerPrefs level progress reset to Level 1.");
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Slither Ants Jam/Progression/Reset Progression to Level 1")]
    public static void ResetProgressionEditorMenu()
    {
        PlayerPrefs.SetInt(CurrentLevelPrefKey, 1);
        PlayerPrefs.DeleteKey(HighestCompletedLevelPrefKey);
        PlayerPrefs.Save();
        Debug.Log("[LevelManager] PlayerPrefs level progression reset to Level 1.");
    }
#endif
}
