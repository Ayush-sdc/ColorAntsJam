using System;
using UnityEngine;

namespace AntsJam.Progression
{
    [Serializable]
    public class FeatureDefinition
    {
        [Header("Identity")]
        [Tooltip("Stable unique feature ID, e.g. 'linked_ants', 'stacked_ants', 'pipes', 'locked_ants'")]
        [SerializeField] private string featureId;

        [Tooltip("Display name shown in UI")]
        [SerializeField] private string displayName;

        [Header("Progression Milestones")]
        [Tooltip("Level number where this feature is introduced and unlocked")]
        [SerializeField] private int introductionLevel;

        [Tooltip("Level number where progression towards this feature starts")]
        [SerializeField] private int startMilestoneLevel;

        [Tooltip("Display sort order")]
        [SerializeField] private int displayOrder;

        [Header("Visual Assets")]
        [Tooltip("Preview icon showing cute ants with soft blue glow")]
        [SerializeField] private Sprite previewIcon;

        [Tooltip("Main tutorial illustration artwork")]
        [SerializeField] private Sprite tutorialArtwork;

        [Tooltip("Optional custom tutorial prefab")]
        [SerializeField] private GameObject tutorialPrefab;

        [Header("Tutorial Copy")]
        [SerializeField] private string tutorialTitle;
        [TextArea(2, 4)]
        [SerializeField] private string tutorialDescription;

        // Public Accessors
        public string FeatureId => featureId;
        public string DisplayName => displayName;
        public int IntroductionLevel => introductionLevel;
        public int StartMilestoneLevel => startMilestoneLevel;
        public int DisplayOrder => displayOrder;
        public Sprite PreviewIcon => previewIcon;
        public Sprite TutorialArtwork => tutorialArtwork;
        public GameObject TutorialPrefab => tutorialPrefab;
        public string TutorialTitle => tutorialTitle;
        public string TutorialDescription => tutorialDescription;

        public FeatureDefinition(
            string featureId,
            string displayName,
            int startMilestoneLevel,
            int introductionLevel,
            int displayOrder,
            string tutorialTitle,
            string tutorialDescription,
            Sprite previewIcon = null,
            Sprite tutorialArtwork = null,
            GameObject tutorialPrefab = null)
        {
            this.featureId = featureId;
            this.displayName = displayName;
            this.startMilestoneLevel = startMilestoneLevel;
            this.introductionLevel = introductionLevel;
            this.displayOrder = displayOrder;
            this.tutorialTitle = tutorialTitle;
            this.tutorialDescription = tutorialDescription;
            this.previewIcon = previewIcon;
            this.tutorialArtwork = tutorialArtwork;
            this.tutorialPrefab = tutorialPrefab;
        }

        public void SetAssets(Sprite preview, Sprite artwork, GameObject prefab = null)
        {
            this.previewIcon = preview;
            this.tutorialArtwork = artwork;
            this.tutorialPrefab = prefab;
        }
    }
}
