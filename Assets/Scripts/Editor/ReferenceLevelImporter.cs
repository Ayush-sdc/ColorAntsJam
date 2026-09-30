#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ReferenceLevelImporter
{
    private const string JsonFolder = "Assets/Data/ReferenceLevelJSON";
    private const string OutputFolder = "Assets/Data/ReferenceLevels";

    [Serializable]
    public class RawReferenceLevelJSON
    {
        public List<RawSnakeJSON> snakes = new List<RawSnakeJSON>();
        public List<RawCellInfoJSON> CellInfo = new List<RawCellInfoJSON>();
        public Vector2Int GridSize;
        public float Duration;
        public int LevelDifficulty;
        public int MaxCapacity;
        public List<Vector2Int> DisabledGridCells = new List<Vector2Int>();
    }

    [Serializable]
    public class RawSnakeJSON
    {
        public List<Vector2Int> Cells = new List<Vector2Int>();
        public int ColorID;
        public int HolderType;
        public bool IsConnected;
        public bool IsSecondColor;
        public int ConnectedIndex;
        public int SecondColor;
        public int PipeCount;
        public bool IsLocked;
        public int LockAmount;
    }

    [Serializable]
    public class RawCellInfoJSON
    {
        public bool IsForcedDisabledCell;
        public Vector2Int Position;
    }

    [MenuItem("Tools/Slither Ants Jam/Import Reference Levels")]
    public static void ImportReferenceLevels()
    {
        EnsureFolder(OutputFolder);
        EnsureJsonFilesExist();

        int importedCount = 0;
        int failedCount = 0;

        for (int i = 1; i <= 45; i++)
        {
            string levelName = $"Level_{i:000}";
            string jsonPath = $"{JsonFolder}/{levelName}.json";
            string assetPath = $"{OutputFolder}/{levelName}.asset";

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[ReferenceLevelImporter] Missing source JSON: {jsonPath}");
                failedCount++;
                continue;
            }

            string jsonText = File.ReadAllText(jsonPath);
            RawReferenceLevelJSON rawData = JsonUtility.FromJson<RawReferenceLevelJSON>(jsonText);

            if (rawData == null)
            {
                Debug.LogError($"[ReferenceLevelImporter] Failed to parse JSON for {levelName}");
                failedCount++;
                continue;
            }

            // Convert CellInfo list
            List<CellInfo> cellInfoList = new List<CellInfo>();
            if (rawData.CellInfo != null)
            {
                foreach (var c in rawData.CellInfo)
                {
                    cellInfoList.Add(new CellInfo(c.Position, c.IsForcedDisabledCell));
                }
            }

            // Convert raw snake records to AntQueueData (lossless representation)
            List<AntQueueData> queueList = new List<AntQueueData>();
            if (rawData.snakes != null)
            {
                for (int sIdx = 0; sIdx < rawData.snakes.Count; sIdx++)
                {
                    var s = rawData.snakes[sIdx];
                    AntQueueData queue = new AntQueueData(
                        $"Record_{sIdx}_{s.ColorID}",
                        s.ColorID,
                        s.HolderType,
                        s.Cells != null ? new List<Vector2Int>(s.Cells) : new List<Vector2Int>(),
                        s.IsConnected,
                        s.ConnectedIndex,
                        s.IsSecondColor,
                        s.SecondColor,
                        s.PipeCount,
                        s.IsLocked,
                        s.LockAmount
                    );
                    queueList.Add(queue);
                }
            }

            Level levelAsset = AssetDatabase.LoadAssetAtPath<Level>(assetPath);
            bool isNew = (levelAsset == null);

            if (isNew)
            {
                if (File.Exists(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                levelAsset = ScriptableObject.CreateInstance<Level>();
                levelAsset.name = levelName;
            }

            levelAsset.SetReferenceLevelData(
                i,
                $"Reference Level {i}",
                rawData.LevelDifficulty.ToString(),
                rawData.Duration,
                rawData.GridSize,
                rawData.MaxCapacity,
                rawData.DisabledGridCells,
                cellInfoList,
                queueList,
                difficultyInt: rawData.LevelDifficulty
            );

            if (isNew)
            {
                AssetDatabase.CreateAsset(levelAsset, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(levelAsset);
            }

            importedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ReferenceLevelImporter] Completed: {importedCount}/45 reference levels imported into {OutputFolder}. (Failed: {failedCount})");
    }

    [MenuItem("Tools/Slither Ants Jam/Export Reference Levels to JSON")]
    public static void ExportReferenceLevels()
    {
        EnsureFolder(JsonFolder);
        int exported = 0;

        for (int i = 1; i <= 45; i++)
        {
            string levelName = $"Level_{i:000}";
            string assetPath = $"{OutputFolder}/{levelName}.asset";
            string jsonPath = $"{JsonFolder}/{levelName}.json";

            Level level = AssetDatabase.LoadAssetAtPath<Level>(assetPath);
            if (level == null) continue;

            RawReferenceLevelJSON raw = new RawReferenceLevelJSON();
            raw.GridSize = level.GridSize;
            raw.Duration = level.RawDuration > 0f ? level.RawDuration : level.Duration;
            raw.LevelDifficulty = level.LevelDifficulty;
            raw.MaxCapacity = level.MaxCapacity;
            raw.DisabledGridCells = new List<Vector2Int>(level.DisabledGridCells);

            if (level.CellInfo != null)
            {
                foreach (var ci in level.CellInfo)
                {
                    raw.CellInfo.Add(new RawCellInfoJSON
                    {
                        Position = ci.Position,
                        IsForcedDisabledCell = ci.IsForcedDisabledCell
                    });
                }
            }

            if (level.AntQueues != null)
            {
                foreach (var q in level.AntQueues)
                {
                    raw.snakes.Add(new RawSnakeJSON
                    {
                        Cells = new List<Vector2Int>(q.Cells),
                        ColorID = q.ColorID,
                        HolderType = q.HolderType,
                        IsConnected = q.IsConnected,
                        ConnectedIndex = q.ConnectedIndex,
                        IsSecondColor = q.IsSecondColor,
                        SecondColor = q.SecondColor,
                        PipeCount = q.PipeCount,
                        IsLocked = q.IsLocked,
                        LockAmount = q.LockAmount
                    });
                }
            }

            string json = JsonUtility.ToJson(raw, true);
            File.WriteAllText(jsonPath, json);
            exported++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ReferenceLevelImporter] Exported {exported} reference levels to {JsonFolder}");
    }

    [MenuItem("Tools/Slither Ants Jam/Validate Reference Levels")]
    public static void ValidateReferenceLevels()
    {
        int passedLevels = 0;
        int failedLevels = 0;
        List<string> errorMessages = new List<string>();

        for (int i = 1; i <= 45; i++)
        {
            string levelName = $"Level_{i:000}";
            string jsonPath = $"{JsonFolder}/{levelName}.json";
            string assetPath = $"{OutputFolder}/{levelName}.asset";

            if (!File.Exists(jsonPath))
            {
                errorMessages.Add($"{levelName}: Missing source JSON file '{jsonPath}'");
                failedLevels++;
                continue;
            }

            Level levelAsset = AssetDatabase.LoadAssetAtPath<Level>(assetPath);
            if (levelAsset == null)
            {
                errorMessages.Add($"{levelName}: Missing imported asset '{assetPath}'");
                failedLevels++;
                continue;
            }

            string jsonText = File.ReadAllText(jsonPath);
            RawReferenceLevelJSON raw = JsonUtility.FromJson<RawReferenceLevelJSON>(jsonText);
            if (raw == null)
            {
                errorMessages.Add($"{levelName}: Failed to deserialize source JSON '{jsonPath}'");
                failedLevels++;
                continue;
            }

            bool levelPassed = true;

            // 1. GridSize
            if (levelAsset.GridSize != raw.GridSize)
            {
                errorMessages.Add($"{levelName} -> GridSize: Expected {raw.GridSize}, Actual {levelAsset.GridSize}");
                levelPassed = false;
            }

            // 2. Duration
            if (Mathf.Abs(levelAsset.Duration - raw.Duration) > 0.001f)
            {
                errorMessages.Add($"{levelName} -> Duration: Expected {raw.Duration}, Actual {levelAsset.Duration}");
                levelPassed = false;
            }

            // 3. Difficulty
            if (levelAsset.Difficulty != raw.LevelDifficulty.ToString())
            {
                errorMessages.Add($"{levelName} -> Difficulty: Expected {raw.LevelDifficulty}, Actual {levelAsset.Difficulty}");
                levelPassed = false;
            }

            // 4. Capacity
            if (levelAsset.Capacity != raw.MaxCapacity)
            {
                errorMessages.Add($"{levelName} -> Capacity: Expected {raw.MaxCapacity}, Actual {levelAsset.Capacity}");
                levelPassed = false;
            }

            // 5. DisabledGridCells
            int expectedDisabledCount = raw.DisabledGridCells != null ? raw.DisabledGridCells.Count : 0;
            int actualDisabledCount = levelAsset.DisabledGridCells != null ? levelAsset.DisabledGridCells.Count : 0;
            if (expectedDisabledCount != actualDisabledCount)
            {
                errorMessages.Add($"{levelName} -> DisabledGridCells count: Expected {expectedDisabledCount}, Actual {actualDisabledCount}");
                levelPassed = false;
            }
            else if (expectedDisabledCount > 0)
            {
                for (int d = 0; d < expectedDisabledCount; d++)
                {
                    if (levelAsset.DisabledGridCells[d] != raw.DisabledGridCells[d])
                    {
                        errorMessages.Add($"{levelName} -> DisabledGridCells[{d}]: Expected {raw.DisabledGridCells[d]}, Actual {levelAsset.DisabledGridCells[d]}");
                        levelPassed = false;
                    }
                }
            }

            // 6. CellInfo
            int expectedCellInfoCount = raw.CellInfo != null ? raw.CellInfo.Count : 0;
            int actualCellInfoCount = levelAsset.CellInfo != null ? levelAsset.CellInfo.Count : 0;
            if (expectedCellInfoCount != actualCellInfoCount)
            {
                errorMessages.Add($"{levelName} -> CellInfo count: Expected {expectedCellInfoCount}, Actual {actualCellInfoCount}");
                levelPassed = false;
            }
            else if (expectedCellInfoCount > 0)
            {
                for (int c = 0; c < expectedCellInfoCount; c++)
                {
                    if (levelAsset.CellInfo[c].Position != raw.CellInfo[c].Position ||
                        levelAsset.CellInfo[c].IsForcedDisabledCell != raw.CellInfo[c].IsForcedDisabledCell)
                    {
                        errorMessages.Add($"{levelName} -> CellInfo[{c}]: Expected (Pos={raw.CellInfo[c].Position}, Disabled={raw.CellInfo[c].IsForcedDisabledCell}), Actual (Pos={levelAsset.CellInfo[c].Position}, Disabled={levelAsset.CellInfo[c].IsForcedDisabledCell})");
                        levelPassed = false;
                    }
                }
            }

            // 7. Record count
            int expectedRecordCount = raw.snakes != null ? raw.snakes.Count : 0;
            int actualRecordCount = levelAsset.AntQueues != null ? levelAsset.AntQueues.Count : 0;
            if (expectedRecordCount != actualRecordCount)
            {
                errorMessages.Add($"{levelName} -> Record count: Expected {expectedRecordCount}, Actual {actualRecordCount}");
                levelPassed = false;
            }
            else if (expectedRecordCount > 0)
            {
                // 8. Field-by-field verification for every record
                for (int r = 0; r < expectedRecordCount; r++)
                {
                    var exp = raw.snakes[r];
                    var act = levelAsset.AntQueues[r];

                    // Cells
                    int expCellsCount = exp.Cells != null ? exp.Cells.Count : 0;
                    int actCellsCount = act.Cells != null ? act.Cells.Count : 0;
                    if (expCellsCount != actCellsCount)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> Cells count: Expected {expCellsCount}, Actual {actCellsCount}");
                        levelPassed = false;
                    }
                    else
                    {
                        for (int k = 0; k < expCellsCount; k++)
                        {
                            if (exp.Cells[k] != act.Cells[k])
                            {
                                errorMessages.Add($"{levelName} -> Record {r} -> Cells[{k}]: Expected {exp.Cells[k]}, Actual {act.Cells[k]}");
                                levelPassed = false;
                            }
                        }
                    }

                    // ColorID
                    if (act.ColorID != exp.ColorID)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> ColorID: Expected {exp.ColorID}, Actual {act.ColorID}");
                        levelPassed = false;
                    }

                    // HolderType
                    if (act.HolderType != exp.HolderType)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> HolderType: Expected {exp.HolderType}, Actual {act.HolderType}");
                        levelPassed = false;
                    }

                    // IsConnected
                    if (act.IsConnected != exp.IsConnected)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> IsConnected: Expected {exp.IsConnected}, Actual {act.IsConnected}");
                        levelPassed = false;
                    }

                    // ConnectedIndex
                    if (act.ConnectedIndex != exp.ConnectedIndex)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> ConnectedIndex: Expected {exp.ConnectedIndex}, Actual {act.ConnectedIndex}");
                        levelPassed = false;
                    }

                    // IsSecondColor
                    if (act.IsSecondColor != exp.IsSecondColor)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> IsSecondColor: Expected {exp.IsSecondColor}, Actual {act.IsSecondColor}");
                        levelPassed = false;
                    }

                    // SecondColor
                    if (act.SecondColor != exp.SecondColor)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> SecondColor: Expected {exp.SecondColor}, Actual {act.SecondColor}");
                        levelPassed = false;
                    }

                    // PipeCount
                    if (act.PipeCount != exp.PipeCount)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> PipeCount: Expected {exp.PipeCount}, Actual {act.PipeCount}");
                        levelPassed = false;
                    }

                    // IsLocked
                    if (act.IsLocked != exp.IsLocked)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> IsLocked: Expected {exp.IsLocked}, Actual {act.IsLocked}");
                        levelPassed = false;
                    }

                    // LockAmount
                    if (act.LockAmount != exp.LockAmount)
                    {
                        errorMessages.Add($"{levelName} -> Record {r} -> LockAmount: Expected {exp.LockAmount}, Actual {act.LockAmount}");
                        levelPassed = false;
                    }
                }
            }

            if (levelPassed)
            {
                Debug.Log($"Level {i:000}: PASS");
                passedLevels++;
            }
            else
            {
                Debug.LogError($"Level {i:000}: FAIL");
                failedLevels++;
            }
        }

        Debug.Log($"========================================");
        Debug.Log($"Final: {passedLevels}/45 PASS (Failed: {failedLevels})");
        Debug.Log($"========================================");
    }

    [MenuItem("Tools/Slither Ants Jam/Sanitize Main Scene")]
    public static void SanitizeMainScene()
    {
        string scenePath = "Assets/Scenes/main.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        int destroyedCount = 0;
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (var go in rootObjects)
        {
            if (go == null) continue;
            string name = go.name;
            if (name.Contains("(Clone)") || 
                name == "Nest FX" || 
                name.StartsWith("ant_dis") ||
                name.StartsWith("Ant_Record_") ||
                (name.StartsWith("Hole_") && go.transform.parent == null))
            {
                toDestroy.Add(go);
            }
            else if (name == "Ants" || name == "FoodBoxes")
            {
                for (int c = go.transform.childCount - 1; c >= 0; c--)
                {
                    toDestroy.Add(go.transform.GetChild(c).gameObject);
                }
            }
        }

        foreach (var go in toDestroy)
        {
            destroyedCount++;
            UnityEngine.Object.DestroyImmediate(go);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SceneSanitizer] Removed {destroyedCount} leaked runtime clones from {scenePath}. Saved: {saved}. Remaining root objects: {scene.rootCount}");
    }

    private static void EnsureJsonFilesExist()
    {
        EnsureFolder(JsonFolder);
        // Check if JSON files exist in JsonFolder
        bool allExist = true;
        for (int i = 1; i <= 45; i++)
        {
            if (!File.Exists($"{JsonFolder}/Level_{i:000}.json"))
            {
                allExist = false;
                break;
            }
        }

        if (allExist) return;

        // Try copying from ColorSlitherJam_Recovered if present
        string altSource = "C:/Users/ayush/Desktop/Unity Projects/ColorSlitherJam_Levels_1-45_FIXED/ColorSlitherJam_Levels_1-45_FIXED/Assets/ColorSlitherJam_Recovered/LevelJSON";
        if (Directory.Exists(altSource))
        {
            for (int i = 1; i <= 45; i++)
            {
                string src = $"{altSource}/Level_{i:000}.json";
                if (!File.Exists(src)) src = $"{altSource}/Level_{i:000}.asset";
                if (File.Exists(src))
                {
                    File.Copy(src, $"{JsonFolder}/Level_{i:000}.json", true);
                }
            }
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
