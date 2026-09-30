using UnityEngine;

namespace AntsJam.Core
{
    public static class VibrationManager
    {
        private const string PrefKey = "VibrationEnabled";
        private static bool isEnabled = true;
        private static bool isInitialized = false;

        private static void EnsureInitialized()
        {
            if (!isInitialized)
            {
                isEnabled = PlayerPrefs.GetInt(PrefKey, 1) == 1;
                isInitialized = true;
            }
        }

        public static bool IsEnabled
        {
            get
            {
                EnsureInitialized();
                return isEnabled;
            }
            set
            {
                EnsureInitialized();
                isEnabled = value;
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void Toggle()
        {
            IsEnabled = !IsEnabled;
        }

        public static void Vibrate()
        {
            if (!IsEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
