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
    public static Color HolePitColor => new Color(0.155f, 0.198f, 0.235f);     // Recessed hole interior #27323C (sampled from Level_01.png)
    public static Color StudioBackground => new Color(0.843f, 0.835f, 0.816f); // Warm neutral gray #D7D5D0 (sampled from Level_01.png)
    public static Color StudioBackgroundTop => new Color(0.875f, 0.867f, 0.847f);
    public static Color StudioBackgroundBottom => new Color(0.815f, 0.807f, 0.788f);

    private static readonly Dictionary<AntColor, Material> materialCache = new Dictionary<AntColor, Material>();
    private static readonly Dictionary<AntColor, Material> holeRimMaterialCache = new Dictionary<AntColor, Material>();
    private static readonly Dictionary<AntColor, Material> holeBevelMaterialCache = new Dictionary<AntColor, Material>();
    private static Material holePitMaterial;
    private static Material eyeWhiteMaterial;
    private static Material eyePupilMaterial;
    private static Material legMaterial;
    private static Mesh workerMesh;
    private static Sprite shadowSprite;
    private static Material groundDotMaterial;

    /// <summary>
    /// Authoritative visual color palette sampled directly from Color Slither Jam reference screenshots (Level 4 & Level 5).
    /// Both ants and holder rims resolve their primary hue from this exact method.
    /// </summary>
    public static Color GetColor(AntColor antColor)
    {
        return antColor switch
        {
            AntColor.SkyBlue => new Color(0.071f, 0.596f, 0.902f),     // Raw 0: Light cyan sky blue #1298E6 (L5)
            AntColor.Lime => new Color(0.400f, 0.765f, 0.055f),        // Raw 1: Vibrant yellow-green lime #66C30E (L4)
            AntColor.Yellow => new Color(0.996f, 0.741f, 0.016f),      // Raw 2: Warm candy sunshine yellow #FEBD04 (L4/L5)
            AntColor.Purple => new Color(0.478f, 0.239f, 0.808f),      // Raw 3: Rich royal purple/violet #7A3DCE (Reference)
            AntColor.Orange => new Color(0.925f, 0.439f, 0.090f),      // Raw 4: Juicy bright orange #EC7017 (L4)
            AntColor.Pink => new Color(0.949f, 0.310f, 0.902f),        // Raw 5: Punchy candy magenta/pink #F24FE6 (L4/L5)
            AntColor.Indigo => new Color(0.255f, 0.353f, 0.804f),      // Raw 6: Deep royal/cobalt blue #415ACD (L4/L5)
            AntColor.Green => new Color(0.169f, 0.518f, 0.157f),       // Raw 7: Rich natural grass green #2B8428 (L5)
            AntColor.Brown => new Color(0.635f, 0.380f, 0.133f),       // Raw 8: Warm caramel chocolate brown #A26122 (L4/L5)
            AntColor.White => new Color(0.925f, 0.878f, 0.824f),       // Raw 9: Warm off-white cream #ECE0D2 (L4)
            AntColor.Red => new Color(0.910f, 0.247f, 0.251f),         // Raw 10: Poppy coral ruby red #E83F40 (L4)
            AntColor.Turquoise => new Color(0.031f, 0.922f, 0.757f),   // Raw 11: Bright aqua turquoise #08EBC1 (L4)
            AntColor.Blue => new Color(0.071f, 0.596f, 0.902f),        // Legacy alias for SkyBlue
            AntColor.Cyan => new Color(0.031f, 0.922f, 0.757f),        // Legacy alias for Turquoise
            AntColor.Black => new Color(0.24f, 0.26f, 0.30f),          // Raw 12: Charcoal black / dark graphite (L8/L10)
            _ => Color.white
        };
    }

    /// <summary>
    /// Computes the darker inner wall bevel color by sloping downward from the base hue toward the recessed pit neutral.
    /// </summary>
    public static Color GetInnerBevelColor(AntColor antColor)
    {
        return Color.Lerp(GetColor(antColor), HolePitColor, 0.35f);
    }

    /// <summary>
    /// Legacy texture getter preserved for backward compatibility. Ants now use clean URP Lit shaders.
    /// </summary>
    public static Texture2D GetTexture(AntColor color) => null;

    /// <summary>
    /// Clears cached runtime materials to allow live palette updates.
    /// </summary>
    public static void ClearCache()
    {
        materialCache.Clear();
        holeRimMaterialCache.Clear();
        holeBevelMaterialCache.Clear();
        holePitMaterial = null;
        eyeWhiteMaterial = null;
        eyePupilMaterial = null;
        legMaterial = null;
        groundDotMaterial = null;
    }

    /// <summary>
    /// Retrieves or creates the shared URP Lit ant material for a given AntColor.
    /// Smoothness = 0.72f, Metallic = 0f, Emission = OFF.
    /// </summary>
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

        // Smooth glossy plastic finish matching reference (Section 3: Metallic 0, Smoothness 0.65-0.80)
        if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", 0.72f);
        if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", 0.0f);

        materialCache[color] = newMat;
        return newMat;
    }

    /// <summary>
    /// Retrieves or creates the shared URP Lit holder rim material for a given AntColor.
    /// Uses the EXACT same primary hue as GetColor(color) with Smoothness = 0.65f, Metallic = 0f.
    /// </summary>
    public static Material GetHoleMaterial(AntColor color)
    {
        if (holeRimMaterialCache.TryGetValue(color, out var mat) && mat != null)
        {
            return mat;
        }

        Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material newMat = new Material(uLitShader);
        newMat.name = $"HoleRimMat_{color}";

        Color col = GetColor(color);
        newMat.color = col;
        if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", col);
        if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", 0.65f);
        if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", 0.0f);

        holeRimMaterialCache[color] = newMat;
        return newMat;
    }

    /// <summary>
    /// Retrieves or creates the shared URP Lit inner bevel material for a holder slot.
    /// Derived by Lerp(baseColor, HolePitColor, 0.35f).
    /// </summary>
    public static Material GetHoleBevelMaterial(AntColor color)
    {
        if (holeBevelMaterialCache.TryGetValue(color, out var mat) && mat != null)
        {
            return mat;
        }

        Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material newMat = new Material(uLitShader);
        newMat.name = $"HoleBevelMat_{color}";

        Color bevelCol = GetInnerBevelColor(color);
        newMat.color = bevelCol;
        if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", bevelCol);
        if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", 0.55f);
        if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", 0.0f);

        holeBevelMaterialCache[color] = newMat;
        return newMat;
    }

    /// <summary>
    /// Retrieves or creates the shared URP Lit recessed pit interior floor material.
    /// Dark blue-charcoal neutral #1F252B, Smoothness = 0.10f, Metallic = 0f.
    /// </summary>
    public static Material GetHolePitMaterial()
    {
        if (holePitMaterial == null)
        {
            Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            holePitMaterial = new Material(uLitShader);
            holePitMaterial.name = "HolePitFloorMat";

            holePitMaterial.color = HolePitColor;
            if (holePitMaterial.HasProperty("_BaseColor")) holePitMaterial.SetColor("_BaseColor", HolePitColor);
            if (holePitMaterial.HasProperty("_Smoothness")) holePitMaterial.SetFloat("_Smoothness", 0.10f);
            if (holePitMaterial.HasProperty("_Metallic")) holePitMaterial.SetFloat("_Metallic", 0.0f);
        }
        return holePitMaterial;
    }

    /// <summary>
    /// Shared glossy white eye sphere material.
    /// </summary>
    public static Material GetEyeWhiteMaterial()
    {
        if (eyeWhiteMaterial == null)
        {
            Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            eyeWhiteMaterial = new Material(uLitShader);
            eyeWhiteMaterial.name = "AntEyeWhiteMat";
            eyeWhiteMaterial.color = Color.white;
            if (eyeWhiteMaterial.HasProperty("_BaseColor")) eyeWhiteMaterial.SetColor("_BaseColor", Color.white);
            if (eyeWhiteMaterial.HasProperty("_Smoothness")) eyeWhiteMaterial.SetFloat("_Smoothness", 0.90f);
            if (eyeWhiteMaterial.HasProperty("_Metallic")) eyeWhiteMaterial.SetFloat("_Metallic", 0.0f);
        }
        return eyeWhiteMaterial;
    }

    /// <summary>
    /// Shared glossy dark pupil material.
    /// </summary>
    public static Material GetEyePupilMaterial()
    {
        if (eyePupilMaterial == null)
        {
            Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            eyePupilMaterial = new Material(uLitShader);
            eyePupilMaterial.name = "AntEyePupilMat";
            Color pupilCol = new Color(0.04f, 0.04f, 0.06f);
            eyePupilMaterial.color = pupilCol;
            if (eyePupilMaterial.HasProperty("_BaseColor")) eyePupilMaterial.SetColor("_BaseColor", pupilCol);
            if (eyePupilMaterial.HasProperty("_Smoothness")) eyePupilMaterial.SetFloat("_Smoothness", 0.95f);
            if (eyePupilMaterial.HasProperty("_Metallic")) eyePupilMaterial.SetFloat("_Metallic", 0.0f);
        }
        return eyePupilMaterial;
    }

    /// <summary>
    /// Shared dark graphite leg/antenna material.
    /// </summary>
    public static Material GetLegMaterial()
    {
        if (legMaterial == null)
        {
            Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            legMaterial = new Material(uLitShader);
            legMaterial.name = "AntLegMat";
            legMaterial.color = LegColor;
            if (legMaterial.HasProperty("_BaseColor")) legMaterial.SetColor("_BaseColor", LegColor);
            if (legMaterial.HasProperty("_Smoothness")) legMaterial.SetFloat("_Smoothness", 0.40f);
            if (legMaterial.HasProperty("_Metallic")) legMaterial.SetFloat("_Metallic", 0.0f);
        }
        return legMaterial;
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
}
