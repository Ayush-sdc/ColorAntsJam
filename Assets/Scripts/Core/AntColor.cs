using System.Collections.Generic;
using UnityEngine;

public enum AntColor
{
    // Legacy serialized values (preserved for backward compatibility)
    Blue = 0,
    Green = 1,
    Purple = 2,
    Yellow = 3,
    Red = 4,
    Orange = 5,
    Cyan = 6,
    Pink = 7,
    White = 8,
    Black = 9,
    Brown = 10,

    // Extended distinct reference colors
    SkyBlue = 11,
    Lime = 12,
    Indigo = 13,
    Turquoise = 14
}

public static class AntColorPalette
{
    public static Color LegColor => new Color(0.20f, 0.22f, 0.26f);           // Dark graphite bead feet #333842
    public static Color HolePitColor => Color.black;       // Deep dark slot interior #1C232E
    public static Color StudioBackground => new Color(0.87f, 0.86f, 0.84f);   // Clean light grey-beige studio backdrop #DEDCD6
    public static Color StudioBackgroundTop => new Color(0.91f, 0.90f, 0.88f);
    public static Color StudioBackgroundBottom => new Color(0.83f, 0.82f, 0.80f);

    private static readonly Dictionary<AntColor, Material> materialCache = new Dictionary<AntColor, Material>();
    private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private static Mesh workerMesh;
    private static Sprite shadowSprite;
    private static Material groundDotMaterial;

    public static Color GetColor(AntColor antColor)
    {
        return antColor switch
        {
            AntColor.SkyBlue => new Color(0.20f, 0.70f, 1.0f),     // Sky / Light blue #33B3FF
            AntColor.Lime => new Color(0.48f, 0.85f, 0.15f),        // Vibrant lime green #7BD926
            AntColor.Yellow => new Color(1.0f, 0.75f, 0.05f),      // Warm candy yellow #FFBF0D
            AntColor.Purple => new Color(0.58f, 0.22f, 0.88f),      // Rich vivid purple #9438E0
            AntColor.Orange => new Color(1.0f, 0.50f, 0.05f),      // Juicy citrus orange #FF800D
            AntColor.Pink => new Color(0.98f, 0.35f, 0.75f),        // Punchy candy pink/magenta #FA59BF
            AntColor.Indigo => new Color(0.18f, 0.28f, 0.85f),      // Deep royal blue #2E47D9
            AntColor.Green => new Color(0.12f, 0.65f, 0.22f),       // Rich forest/leaf green #1FA638
            AntColor.Brown => new Color(0.55f, 0.32f, 0.17f),       // Milk chocolate brown #8C522B
            AntColor.White => new Color(0.92f, 0.92f, 0.92f),       // Clean warm white #EBEBEB
            AntColor.Red => new Color(0.95f, 0.24f, 0.26f),         // Coral ruby red #F23D42
            AntColor.Turquoise => new Color(0.10f, 0.88f, 0.78f),   // Aqua / turquoise #1AE0C7
            AntColor.Blue => new Color(0.20f, 0.70f, 1.0f),
            AntColor.Cyan => new Color(0.10f, 0.88f, 0.78f),
            AntColor.Black => new Color(0.18f, 0.18f, 0.20f),
            _ => Color.white
        };
    }

    public static string GetTextureResourceName(AntColor antColor)
    {
        return antColor switch
        {
            AntColor.SkyBlue => "blue0",
            AntColor.Lime => "green0",
            AntColor.Indigo => "blue0",
            AntColor.Turquoise => "teal0",
            AntColor.Blue => "blue0",
            AntColor.Green => "green0",
            AntColor.Purple => "purple0",
            AntColor.Yellow => "yellow0",
            AntColor.Red => "red0",
            AntColor.Orange => "orange0",
            AntColor.Cyan => "cyan0",
            AntColor.Pink => "pink0",
            AntColor.White => "white0",
            AntColor.Black => "black0",
            AntColor.Brown => "brown0",
            _ => "ant_new_concept"
        };
    }

    public static Texture2D GetTexture(AntColor color)
    {
        string texName = GetTextureResourceName(color);
        if (textureCache.TryGetValue(texName, out var cachedTex) && cachedTex != null)
        {
            return cachedTex;
        }

        Texture2D loadedTex = Resources.Load<Texture2D>($"Ants/Textures/{texName}");
        if (loadedTex != null)
        {
            textureCache[texName] = loadedTex;
            return loadedTex;
        }

        return null;
    }

    public static Material GetMaterial(AntColor color)
    {
        if (materialCache.TryGetValue(color, out var mat) && mat != null)
        {
            return mat;
        }

        Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material newMat = new Material(uLitShader);
        newMat.name = $"AntMat_{color}";

        Color col = GetColor(color);
        newMat.color = col;
        if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", col);

        Texture2D tex = GetTexture(color);
        if (tex != null)
        {
            newMat.mainTexture = tex;
            if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", tex);
        }

        if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", 0.65f);
        if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", 0.05f);

        materialCache[color] = newMat;
        return newMat;
    }

    private static GameObject workerAntPrefab;

    public static GameObject GetWorkerAntPrefab()
    {
        if (workerAntPrefab == null)
        {
            workerAntPrefab = Resources.Load<GameObject>("Ants/Prefabs/WorkerAnt");
        }
        return workerAntPrefab;
    }

    public static Mesh GetWorkerAntMesh()
    {
        if (workerMesh == null)
        {
            GameObject prefab = GetWorkerAntPrefab();
            if (prefab != null)
            {
                MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>(true);
                if (mf != null && mf.sharedMesh != null)
                {
                    workerMesh = mf.sharedMesh;
                }
            }

            if (workerMesh == null)
            {
                workerMesh = Resources.Load<Mesh>("Ants/Meshes/WorkerAntMesh");
                if (workerMesh == null)
                {
                    Mesh[] all = Resources.LoadAll<Mesh>("Ants/Meshes/WorkerAntMesh");
                    if (all != null && all.Length > 0)
                    {
                        workerMesh = all[0];
                    }
                }
            }
        }
        return workerMesh;
    }

    public static Sprite GetShadowSprite()
    {
        if (shadowSprite == null)
        {
            Texture2D shadowTex = Resources.Load<Texture2D>("Ants/Textures/ant_shadowblack");
            if (shadowTex != null)
            {
                shadowSprite = Sprite.Create(shadowTex, new Rect(0, 0, shadowTex.width, shadowTex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return shadowSprite;
    }

    /// <summary>
    /// Subtle low-opacity dark/black ground dot material for initial ant body path markers (exactly 25% alpha).
    /// </summary>
    public static Material GetGroundDotMaterial()
    {
        if (groundDotMaterial == null)
        {
            Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            groundDotMaterial = new Material(uLitShader);
            groundDotMaterial.name = "GroundDotMat";
            Color darkCol = new Color(0.12f, 0.12f, 0.15f, 0.25f); // Exactly 25% alpha
            groundDotMaterial.color = darkCol;
            if (groundDotMaterial.HasProperty("_BaseColor")) groundDotMaterial.SetColor("_BaseColor", darkCol);

            // URP Transparency Configuration
            groundDotMaterial.SetFloat("_Surface", 1f); // 1 = Transparent
            groundDotMaterial.SetFloat("_Blend", 0f);   // 0 = Alpha blend
            groundDotMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            groundDotMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            groundDotMaterial.SetInt("_ZWrite", 0);
            groundDotMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            groundDotMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (groundDotMaterial.HasProperty("_Smoothness")) groundDotMaterial.SetFloat("_Smoothness", 0.0f);
            if (groundDotMaterial.HasProperty("_Metallic")) groundDotMaterial.SetFloat("_Metallic", 0f);
        }
        return groundDotMaterial;
    }

    private static readonly Dictionary<AntColor, Material> holeMaterialCache = new Dictionary<AntColor, Material>();

    public static Material GetHoleMaterial(AntColor color)
    {
        if (holeMaterialCache.TryGetValue(color, out var mat) && mat != null)
        {
            return mat;
        }

        Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material newMat = new Material(uLitShader);
        newMat.name = $"HoleRimMat_{color}";

        Color col = GetColor(color);
        newMat.color = col;
        if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", col);
        if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", 0.90f);
        if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", 0.05f);

        holeMaterialCache[color] = newMat;
        return newMat;
    }
}
