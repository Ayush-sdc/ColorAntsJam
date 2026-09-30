using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntsJam.Progression
{
    [CreateAssetMenu(fileName = "FeatureProgressionConfig", menuName = "Slither Ants/Feature Progression Config")]
    public class FeatureProgressionConfig : ScriptableObject
    {
        private static FeatureProgressionConfig s_Instance;
        public static FeatureProgressionConfig Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<FeatureProgressionConfig>("FeatureProgressionConfig");
#if UNITY_EDITOR
                    if (s_Instance == null)
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:FeatureProgressionConfig");
                        if (guids != null && guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            s_Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<FeatureProgressionConfig>(path);
                        }
                    }
#endif
                }
                return s_Instance;
            }
        }

        [Header("Configured Features")]
        [SerializeField] private List<FeatureDefinition> features = new List<FeatureDefinition>();

        [Header("All Features Completed Presentation")]
        [SerializeField] private Sprite allFeaturesCompletedIcon;
        [SerializeField] private string allFeaturesCompletedTitle = "All Features Unlocked!";

        private const string TutorialAckPrefix = "TutorialSeen_";

        public IReadOnlyList<FeatureDefinition> Features => features;
        public Sprite AllFeaturesCompletedIcon => allFeaturesCompletedIcon;
        public string AllFeaturesCompletedTitle => allFeaturesCompletedTitle;

        /// <summary>
        /// Retrieves the upcoming feature not yet unlocked, based on the highest completed level count.
        /// If all features are unlocked, returns null.
        /// </summary>
        public FeatureDefinition GetUpcomingFeature(int completedLevels)
        {
            if (features == null || features.Count == 0) return null;

            for (int i = 0; i < features.Count; i++)
            {
                var feat = features[i];
                if (feat == null) continue;
                // A feature is considered "in progress" if completedLevels < feat.IntroductionLevel - 1
                // or if it has just completed to 100% on the milestone win screen.
                if (completedLevels < feat.IntroductionLevel)
                {
                    return feat;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates normalized progress (0.0 to 1.0) towards the upcoming feature.
        /// Independent of fill animation, strictly clamped between 0 and 1.
        /// Replaying a level does not inflate progress.
        /// </summary>
        public float CalculateProgress(int completedLevels, out FeatureDefinition upcomingFeature, out bool isAllUnlocked)
        {
            upcomingFeature = GetUpcomingFeature(completedLevels);

            if (upcomingFeature == null)
            {
                isAllUnlocked = true;
                return 1.0f;
            }

            isAllUnlocked = false;
            int startLvl = Mathf.Max(1, upcomingFeature.StartMilestoneLevel);
            int unlockLvl = Mathf.Max(startLvl + 1, upcomingFeature.IntroductionLevel);

            // Milestone calculation formula:
            // Progress = (completedLevels - (startLvl - 1)) / (unlockLvl - startLvl)
            // e.g. start=1, intro=7 -> range = 6. 
            // 0 completed -> 0/6 = 0%
            // 1 completed -> 1/6 = 16.7%
            // 6 completed -> 6/6 = 100% (milestone reached!)
            int intervalLength = unlockLvl - startLvl;
            int completedInInterval = Mathf.Clamp(completedLevels - (startLvl - 1), 0, intervalLength);

            float ratio = (float)completedInInterval / Mathf.Max(1, intervalLength);
            return Mathf.Clamp01(ratio);
        }

        /// <summary>
        /// Checks if a feature's mechanic has reached its unlock milestone.
        /// </summary>
        public bool IsFeatureUnlocked(string featureId, int currentOrCompletedLevel)
        {
            var feat = GetFeature(featureId);
            if (feat == null) return false;
            return currentOrCompletedLevel >= feat.IntroductionLevel;
        }

        public FeatureDefinition GetFeature(string featureId)
        {
            if (features == null) return null;
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i] != null && string.Equals(features[i].FeatureId, featureId, StringComparison.OrdinalIgnoreCase))
                {
                    return features[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Checks whether the tutorial for this feature was acknowledged by the player.
        /// Stored separately from level progression in PlayerPrefs.
        /// </summary>
        public bool IsTutorialAcknowledged(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return true;
            return PlayerPrefs.GetInt(TutorialAckPrefix + featureId, 0) == 1;
        }

        /// <summary>
        /// Marks a feature tutorial as acknowledged. Persisted in PlayerPrefs only when OK is tapped.
        /// </summary>
        public void AcknowledgeTutorial(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return;
            PlayerPrefs.SetInt(TutorialAckPrefix + featureId, 1);
            PlayerPrefs.Save();
            Debug.Log($"[Progression] Tutorial for feature '{featureId}' acknowledged and persisted.");
        }

        /// <summary>
        /// Returns all features that are unlocked for currentLevelNumber but have not yet been acknowledged.
        /// Queued sequentially so players never miss introductory context.
        /// </summary>
        public List<FeatureDefinition> GetPendingTutorials(int currentLevelNumber)
        {
            var pending = new List<FeatureDefinition>();
            if (features == null) return pending;

            for (int i = 0; i < features.Count; i++)
            {
                var feat = features[i];
                if (feat == null) continue;

                // Feature unlocked for this level or earlier, and not yet acknowledged
                if (currentLevelNumber >= feat.IntroductionLevel && !IsTutorialAcknowledged(feat.FeatureId))
                {
                    pending.Add(feat);
                }
            }

            // Sort by introduction level / display order
            pending.Sort((a, b) => a.IntroductionLevel.CompareTo(b.IntroductionLevel));
            return pending;
        }

        /// <summary>
        /// Resets all tutorial acknowledgement flags (for debugging / re-testing).
        /// </summary>
        public void ResetTutorialAcknowledgements()
        {
            if (features != null)
            {
                for (int i = 0; i < features.Count; i++)
                {
                    if (features[i] != null)
                    {
                        PlayerPrefs.DeleteKey(TutorialAckPrefix + features[i].FeatureId);
                    }
                }
                PlayerPrefs.Save();
                Debug.Log("[Progression] All tutorial acknowledgement flags reset.");
            }
        }

        public void SetFeaturesList(List<FeatureDefinition> list, Sprite completedIcon = null)
        {
            this.features = list;
            if (completedIcon != null)
            {
                this.allFeaturesCompletedIcon = completedIcon;
            }
        }
    }
}
