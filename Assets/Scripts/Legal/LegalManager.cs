using System;
using UnityEngine;

namespace AntsJam.Legal
{
    public static class LegalManager
    {
        public const string AcceptedTermsVersionKey = "Legal_AcceptedTermsVersion";
        public const string AcceptedTermsTimestampKey = "Legal_AcceptedTermsTimestamp";
        public const string AcceptedPlayerIdKey = "Legal_AcceptedPlayerId";

        /// <summary>
        /// Returns true if the player has previously accepted the exact version of the terms configured.
        /// </summary>
        public static bool HasAcceptedCurrentTerms(LegalConfig config)
        {
            if (config == null) return true;
            if (string.IsNullOrEmpty(config.TermsOfServiceVersion)) return true;

            string savedVersion = PlayerPrefs.GetString(AcceptedTermsVersionKey, string.Empty);
            return !string.IsNullOrEmpty(savedVersion) && savedVersion == config.TermsOfServiceVersion;
        }

        /// <summary>
        /// Records explicit player acceptance of terms with configured version and ISO timestamp.
        /// </summary>
        public static void RecordTermsAcceptance(LegalConfig config)
        {
            if (config == null) return;

            string version = config.TermsOfServiceVersion;
            string timestamp = DateTime.UtcNow.ToString("o");

            PlayerPrefs.SetString(AcceptedTermsVersionKey, version);
            PlayerPrefs.SetString(AcceptedTermsTimestampKey, timestamp);

            if (config.IsPlayerIdAvailable)
            {
                PlayerPrefs.SetString(AcceptedPlayerIdKey, config.ConfiguredPlayerId);
            }

            PlayerPrefs.Save();
            Debug.Log($"[LegalManager] Terms accepted: Version={version}, Timestamp={timestamp}");
        }

        public static string GetAcceptedTermsVersion()
        {
            return PlayerPrefs.GetString(AcceptedTermsVersionKey, string.Empty);
        }

        public static string GetAcceptedTermsTimestamp()
        {
            return PlayerPrefs.GetString(AcceptedTermsTimestampKey, string.Empty);
        }

        public static void ResetTermsAcceptance()
        {
            PlayerPrefs.DeleteKey(AcceptedTermsVersionKey);
            PlayerPrefs.DeleteKey(AcceptedTermsTimestampKey);
            PlayerPrefs.DeleteKey(AcceptedPlayerIdKey);
            PlayerPrefs.Save();
            Debug.Log("[LegalManager] Terms acceptance reset.");
        }
    }
}
