using System.IO;
using UnityEditor;
using UnityEngine;

public static class PipeAssetSetup
{
    [MenuItem("Slither Ants/Setup Pipe Assets and Materials")]
    public static void SetupPipeAssets()
    {
        // 1. Configure Badge_Squircle texture importer as Sprite with 9-slice border
        string squirclePath = "Assets/Resources/Badge_Squircle.png";
        if (File.Exists(squirclePath))
        {
            var importer = AssetImporter.GetAtPath(squirclePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(24, 24, 24, 24);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                Debug.Log("[PipeAssetSetup] Configured Badge_Squircle.png as 9-slice Sprite.");
            }
        }

        // 2. Configure PipeConduit_Gradient texture importer
        string gradPath = "Assets/Art/Ants/Textures/PipeConduit_Gradient.png";
        if (File.Exists(gradPath))
        {
            var importer = AssetImporter.GetAtPath(gradPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapModeU = TextureWrapMode.Clamp;
                importer.wrapModeV = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
                Debug.Log("[PipeAssetSetup] Configured PipeConduit_Gradient.png texture wrapping.");
            }
        }

        // 3. Configure PipeShadow_Ribbon texture importer
        string shadowPath = "Assets/Art/Ants/Textures/PipeShadow_Ribbon.png";
        if (File.Exists(shadowPath))
        {
            var importer = AssetImporter.GetAtPath(shadowPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapModeU = TextureWrapMode.Clamp;
                importer.wrapModeV = TextureWrapMode.Repeat;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                Debug.Log("[PipeAssetSetup] Configured PipeShadow_Ribbon.png texture wrapping.");
            }
        }

        // 4. Create or update PipeConduit_URPLit.mat
        string matPath = "Assets/Art/Ants/Materials/PipeConduit_URPLit.mat";
        Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(uLitShader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = uLitShader;
        }

        Texture2D gradTex = AssetDatabase.LoadAssetAtPath<Texture2D>(gradPath);
        if (gradTex != null)
        {
            mat.SetTexture("_BaseMap", gradTex);
        }
        mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.88f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log("[PipeAssetSetup] Saved PipeConduit_URPLit.mat.");

        // 5. Assign to PipeVisualConfig.asset
        PipeVisualConfig config = PipeVisualConfig.Instance;
        if (config != null)
        {
            config.customPipeMaterial = mat;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[PipeAssetSetup] Assigned PipeConduit_URPLit.mat to PipeVisualConfig.");
        }
    }
}
