using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SlitherAnts.Editor
{
    public static class AppIconConfigurator
    {
        private const string OriginalSourcePath = "Assets/Art/AppIcon/colorantsjam.png";
        private const string AdaptiveBackgroundPath = "Assets/Art/AppIcon/AppIcon_Adaptive_Background.png";
        private const string AdaptiveForegroundPath = "Assets/Art/AppIcon/AppIcon_Adaptive_Foreground.png";
        private const string LegacyIconPath = "Assets/Art/AppIcon/AppIcon_Legacy.png";

        [MenuItem("Tools/Slither Ants Jam/Icons/Configure Android App Icons")]
        public static void ConfigureIcons()
        {
            Debug.Log("[AppIconConfigurator] Starting icon configuration...");

            // 1. Ensure texture import settings
            ConfigureTextureImporter(OriginalSourcePath);
            ConfigureTextureImporter(AdaptiveBackgroundPath);
            ConfigureTextureImporter(AdaptiveForegroundPath);
            ConfigureTextureImporter(LegacyIconPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Load textures
            var originalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(OriginalSourcePath);
            var bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveBackgroundPath);
            var fgTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveForegroundPath);
            var legacyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyIconPath);

            if (originalTex == null || bgTex == null || fgTex == null || legacyTex == null)
            {
                Debug.LogError("[AppIconConfigurator] One or more icon textures could not be loaded!");
                return;
            }

            // 3. Cache application identifier to guarantee preservation
            string currentAndroidAppId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            Debug.Log($"[AppIconConfigurator] Preserved Android App Identifier: {currentAndroidAppId}");

            // 4. Set Default Icons
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { legacyTex });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { legacyTex });

            // 5. Configure Android Platform Icons
            var androidTarget = NamedBuildTarget.Android;
            var supportedKinds = PlayerSettings.GetSupportedIconKinds(androidTarget);

            foreach (var kind in supportedKinds)
            {
                var icons = PlayerSettings.GetPlatformIcons(androidTarget, kind);
                string kindName = kind.ToString();
                Debug.Log($"[AppIconConfigurator] Configuring {kindName} ({icons.Length} slots)...");

                for (int i = 0; i < icons.Length; i++)
                {
                    var icon = icons[i];
                    if (kindName.Contains("Adaptive"))
                    {
                        // Layer 0 = Background (full bleed, no rounded corners)
                        // Layer 1 = Foreground (centered ant in safe area, transparent bg)
                        icon.SetTextures(new[] { bgTex, fgTex });
                        Debug.Log($"  Adaptive slot [{i}] {icon.width}x{icon.height}: Set Layer0=Background, Layer1=Foreground");
                    }
                    else if (kindName.Contains("Round"))
                    {
                        icon.SetTextures(new[] { legacyTex });
                        Debug.Log($"  Round slot [{i}] {icon.width}x{icon.height}: Set Layer0=Legacy");
                    }
                    else // Legacy
                    {
                        icon.SetTextures(new[] { legacyTex });
                        Debug.Log($"  Legacy slot [{i}] {icon.width}x{icon.height}: Set Layer0=Legacy");
                    }
                }

                PlayerSettings.SetPlatformIcons(androidTarget, kind, icons);
            }

            // 6. Guarantee Application Identifier was preserved
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != currentAndroidAppId)
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, currentAndroidAppId);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[AppIconConfigurator] Android App Icons configured successfully!");

            VerifyAppIconSetup();
        }

        [MenuItem("Tools/Slither Ants Jam/Icons/Verify Android App Icons")]
        public static bool VerifyAppIconSetup()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ANDROID APP ICON VERIFICATION REPORT ===");
            bool allPassed = true;

            // 1. Verify Asset Files Exist
            bool filesExist = File.Exists(OriginalSourcePath) &&
                              File.Exists(AdaptiveBackgroundPath) &&
                              File.Exists(AdaptiveForegroundPath) &&
                              File.Exists(LegacyIconPath);
            sb.AppendLine($"[1] Icon Asset Files Exist: {(filesExist ? "PASS" : "FAIL")}");
            if (!filesExist) allPassed = false;

            // 2. Verify Default Icons
            var defaultUnknown = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            var defaultAndroid = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Android);
            bool defaultAssigned = defaultUnknown.Length > 0 && defaultUnknown[0] != null &&
                                   defaultAndroid.Length > 0 && defaultAndroid[0] != null;
            sb.AppendLine($"[2] Default App Icon Assigned: {(defaultAssigned ? "PASS" : "FAIL")} (Unknown={defaultUnknown.Length}, Android={defaultAndroid.Length})");
            if (!defaultAssigned) allPassed = false;

            // 3. Verify Android Platform Icons
            var androidTarget = NamedBuildTarget.Android;
            var kinds = PlayerSettings.GetSupportedIconKinds(androidTarget);

            int totalSlotsChecked = 0;
            int totalAdaptiveLayersChecked = 0;

            foreach (var kind in kinds)
            {
                var icons = PlayerSettings.GetPlatformIcons(androidTarget, kind);
                string kindName = kind.ToString();

                bool kindValid = true;
                foreach (var icon in icons)
                {
                    totalSlotsChecked++;
                    var texs = icon.GetTextures();
                    if (kindName.Contains("Adaptive"))
                    {
                        if (texs.Length != 2 || texs[0] == null || texs[1] == null)
                        {
                            kindValid = false;
                        }
                        else
                        {
                            totalAdaptiveLayersChecked += 2;
                        }
                    }
                    else
                    {
                        if (texs.Length < 1 || texs[0] == null)
                        {
                            kindValid = false;
                        }
                    }
                }

                sb.AppendLine($"[3] {kindName} Icons ({icons.Length} slots): {(kindValid ? "PASS" : "FAIL")}");
                if (!kindValid) allPassed = false;
            }

            // 4. Verify Application Identifier Preserved
            string appId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            bool appIdPreserved = appId == "com.UnityTechnologies.com.unity.template.urpblank";
            sb.AppendLine($"[4] Application Identifier Preserved ({appId}): {(appIdPreserved ? "PASS" : "FAIL")}");
            if (!appIdPreserved) allPassed = false;

            // 5. Verification Summary
            sb.AppendLine($"Total Slots Verified: {totalSlotsChecked}, Adaptive Layers: {totalAdaptiveLayersChecked}");
            sb.AppendLine($"OVERALL ICON CONFIGURATION: {(allPassed ? "PASSED" : "FAILED")}");
            sb.AppendLine("============================================");

            if (allPassed)
                Debug.Log(sb.ToString());
            else
                Debug.LogError(sb.ToString());

            return allPassed;
        }

        private static void ConfigureTextureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool modified = false;
            if (!importer.isReadable) { importer.isReadable = true; modified = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; modified = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; modified = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; modified = true; }
            if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; modified = true; }
            if (importer.maxTextureSize < 1024) { importer.maxTextureSize = 2048; modified = true; }

            if (modified)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
