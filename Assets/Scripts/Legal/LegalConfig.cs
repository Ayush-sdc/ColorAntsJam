using System;
using UnityEngine;

namespace AntsJam.Legal
{
    [CreateAssetMenu(fileName = "LegalConfig", menuName = "SlitherAnts/Legal/Legal Configuration", order = 10)]
    public class LegalConfig : ScriptableObject
    {
        private static LegalConfig instance;
        public static LegalConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<LegalConfig>("LegalConfig");
                    if (instance == null)
                    {
#if UNITY_EDITOR
                        string path = "Assets/Data/Legal/LegalConfig.asset";
                        instance = UnityEditor.AssetDatabase.LoadAssetAtPath<LegalConfig>(path);
#endif
                    }
                }
                return instance;
            }
        }

        [Header("Terms of Service")]
        [SerializeField] private string termsOfServiceVersion = "1.0.0";
        [SerializeField] private string termsOfServiceTitle = "Terms of Service";
        [TextArea(8, 20)]
        [SerializeField] private string termsOfServiceContent = @"COLOR ANTS JAM - TERMS OF SERVICE
Version 1.0.0

1. ACCEPTANCE OF TERMS
By accessing and playing Color Ants Jam, you agree to be bound by these Terms of Service. If you do not agree to these terms, please do not use the application.

2. LICENSE & GAMEPLAY
Color Ants Jam grants you a personal, non-exclusive, non-transferable, revocable license to use the game for personal, non-commercial entertainment purposes.

3. VIRTUAL ITEMS & PROGRESSION
Game progression, levels, scores, and any virtual elements are purely in-game features. They possess no real-world monetary value and cannot be exchanged for cash or real assets.

4. USER CONDUCT
You agree not to modify, reverse engineer, decompile, or exploit the game application, its assets, or its code.

5. DISCLAIMER & LIMITATION OF LIABILITY
The game is provided ""as is"" without warranty of any kind. Under no circumstances shall the creators be liable for any indirect, incidental, or consequential damages resulting from your use of the application.

6. CONTACT & INQUIRIES
For inquiries regarding these Terms of Service, please contact support at support@colorantsjam.game.";
        [SerializeField] private string termsOfServiceUrl = "";

        [Header("Privacy Policy")]
        [SerializeField] private string privacyPolicyVersion = "1.0.0";
        [SerializeField] private string privacyPolicyTitle = "Privacy Policy";
        [TextArea(8, 20)]
        [SerializeField] private string privacyPolicyContent = @"COLOR ANTS JAM - PRIVACY POLICY
Version 1.0.0

1. OVERVIEW
We respect your privacy and are committed to safeguarding player data. Color Ants Jam is designed as an offline-first puzzle game.

2. DATA COLLECTION & STORAGE
- Local Gameplay Data: Progression, completed levels, and settings (such as sounds and vibrations) are stored locally on your device via PlayerPrefs.
- Personal Information: Color Ants Jam does not collect your name, email, physical address, or phone number during normal offline gameplay.

3. ADVERTISING & THIRD-PARTY SERVICES
If third-party advertising or analytics services are integrated in future updates, player consent will be requested in compliance with applicable privacy regulations (such as GDPR and CCPA).

4. DATA REQUESTS & DELETION
You have the right to request information regarding your data or request deletion of any stored data. Please contact us via support@colorantsjam.game.

5. CHANGES TO THIS POLICY
We may update this Privacy Policy from time to time. Any material changes will be reflected with an updated version number and date.";
        [SerializeField] private string privacyPolicyUrl = "";

        [Header("Account Deletion & Data Request")]
        [SerializeField] private string accountDeletionTitle = "Account Deletion and Data Request";
        [TextArea(6, 16)]
        [SerializeField] private string accountDeletionContent = @"ACCOUNT DELETION & DATA REQUEST

Color Ants Jam stores your level progression, preferences, and completion records locally on your device.

HOW TO DELETE YOUR DATA:
1. Local Data: You can clear all game data at any time through your device settings: Settings > Apps > Color Ants Jam > Storage > Clear Data, or by uninstalling the application.
2. Inquiries & Remote Data: To request confirmation or deletion of any service-related records, contact support at:
support@colorantsjam.game

Please include your device platform and any details regarding your request. We process all verifiable requests promptly.";
        [SerializeField] private string accountDeletionContact = "support@colorantsjam.game";
        [SerializeField] private string accountDeletionUrl = "";

        [Header("Personalized Ads Service Integration")]
        [Tooltip("True if an actual Advertising SDK with Consent Management Platform is installed and active.")]
        [SerializeField] private bool isPersonalizedAdsAvailable = false;
        [SerializeField] private string personalizedAdsUnavailableReason = "Personalized advertising and tracking are currently disabled. No advertising SDK or consent provider is active.";

        [Header("Player Identifier Configuration")]
        [Tooltip("True only if a real authenticated player ID is available from a backend service.")]
        [SerializeField] private bool isPlayerIdAvailable = false;
        [SerializeField] private string configuredPlayerId = "";

        [Header("Welcome Overlay Copy")]
        [TextArea(3, 8)]
        [SerializeField] private string welcomeMessage = "Kindly take some time to review our Terms of Service and Privacy Policy. By clicking the \"ACCEPT\" button below, you agree to our Terms of Service and acknowledge that the Privacy Policy applies.";
        [SerializeField] private string welcomeUpdateNotice = "If you are a returning player, please note that these have been recently updated.";
        [Tooltip("Enable ONLY when legal terms were actually modified and returning players must be informed of the update.")]
        [SerializeField] private bool isTermsRecentlyUpdated = false;

        // Public Properties
        public string TermsOfServiceVersion => termsOfServiceVersion;
        public string TermsOfServiceTitle => termsOfServiceTitle;
        public string TermsOfServiceContent => termsOfServiceContent;
        public string TermsOfServiceUrl => termsOfServiceUrl;

        public string PrivacyPolicyVersion => privacyPolicyVersion;
        public string PrivacyPolicyTitle => privacyPolicyTitle;
        public string PrivacyPolicyContent => privacyPolicyContent;
        public string PrivacyPolicyUrl => privacyPolicyUrl;

        public string AccountDeletionTitle => accountDeletionTitle;
        public string AccountDeletionContent => accountDeletionContent;
        public string AccountDeletionContact => accountDeletionContact;
        public string AccountDeletionUrl => accountDeletionUrl;

        public bool IsPersonalizedAdsAvailable => isPersonalizedAdsAvailable;
        public string PersonalizedAdsUnavailableReason => personalizedAdsUnavailableReason;

        public bool IsPlayerIdAvailable => isPlayerIdAvailable && !string.IsNullOrEmpty(configuredPlayerId);
        public string ConfiguredPlayerId => configuredPlayerId;

        public string WelcomeMessage => welcomeMessage;
        public string WelcomeUpdateNotice => welcomeUpdateNotice;
        public bool IsTermsRecentlyUpdated => isTermsRecentlyUpdated;

        public void SetTermsRecentlyUpdated(bool updated) => isTermsRecentlyUpdated = updated;
        public void SetTermsVersion(string v) => termsOfServiceVersion = v;
        public void SetPlayerId(bool available, string id)
        {
            isPlayerIdAvailable = available;
            configuredPlayerId = id;
        }
    }
}
