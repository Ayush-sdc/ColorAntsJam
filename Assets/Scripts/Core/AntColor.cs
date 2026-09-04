using System.Collections.Generic;
using UnityEngine;

public enum AntColor
{
    Blue,
    Green,
    Purple,
    Yellow,
    Red,
    Orange,
    Cyan,
    Pink,
    White,
    Black,
    Brown
}

public static class AntColorPalette
{
    public static Color LegColor => new Color(0.20f, 0.22f, 0.26f);           // Dark graphite bead feet #333842
    public static Color HolePitColor => new Color(0.11f, 0.14f, 0.18f);       // Deep dark slot interior #1C232E
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
            AntColor.Yellow => new Color(1.0f, 0.72f, 0.05f),     // Warm candy yellow #FFB80D
            AntColor.Green => new Color(0.12f, 0.65f, 0.22f),     // Rich leaf green #1FA638
            AntColor.Blue => new Color(0.12f, 0.68f, 0.98f),      // Sky candy blue #1EAEEFA
            AntColor.Brown => new Color(0.55f, 0.32f, 0.17f),     // Milk chocolate brown #8C522B
            AntColor.Red => new Color(0.95f, 0.24f, 0.26f),       // Vibrant coral ruby red #F23D42
            AntColor.Purple => new Color(0.68f, 0.28f, 0.95f),
            AntColor.Orange => new Color(1.0f, 0.55f, 0.12f),
            AntColor.Cyan => new Color(0.12f, 0.85f, 0.95f),
            AntColor.Pink => new Color(1.0f, 0.38f, 0.72f),
            AntColor.White => new Color(0.95f, 0.95f, 0.96f),
            AntColor.Black => new Color(0.18f, 0.18f, 0.20f),
            _ => Color.white
        };
    }

    public static string GetTextureResourceName(AntColor antColor)
    {
        return antColor switch
        {
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

        Texture2D tex = GetTexture(color);
        if (tex != null)
        {
            newMat.mainTexture = tex;
            if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", tex);
            newMat.color = Color.white;
        }
        else
        {
            newMat.color = GetColor(color);
        }

        if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", 0.65f);
        if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", 0.05f);

        materialCache[color] = newMat;
        return newMat;
    }

    public static Mesh GetWorkerAntMesh()
    {
        if (workerMesh == null)
        {
            workerMesh = Resources.Load<Mesh>("Ants/Meshes/WorkerAntMesh");
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
    /// Smooth, neutral slate grey circular ground dot material matching the reference image.
    /// </summary>
    public static Material GetGroundDotMaterial()
    {
        if (groundDotMaterial == null)
        {
            Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            groundDotMaterial = new Material(uLitShader);
            groundDotMaterial.name = "GroundDotMat";
            groundDotMaterial.color = new Color(0.50f, 0.52f, 0.55f); // Smooth neutral grey
            if (groundDotMaterial.HasProperty("_Smoothness")) groundDotMaterial.SetFloat("_Smoothness", 0.05f);
            if (groundDotMaterial.HasProperty("_Metallic")) groundDotMaterial.SetFloat("_Metallic", 0f);
        }
        return groundDotMaterial;
    }
}
