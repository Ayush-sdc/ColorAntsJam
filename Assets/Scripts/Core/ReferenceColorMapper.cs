using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ReferenceColorMapper provides a clean, decoupled translation layer between raw serialized reference ColorIDs
/// and runtime AntColor enum values, materials, and RGB colors.
/// 
/// This ensures raw reference ColorID integers are preserved losslessly in asset data without directly casting
/// to internal enum indices.
/// </summary>
public static class ReferenceColorMapper
{
    // Custom mapping dictionary for reference levels where ColorID mappings diverge
    private static readonly Dictionary<int, AntColor> customIdToColorMap = new Dictionary<int, AntColor>();

    /// <summary>
    /// Registers or overrides a mapping from a raw reference ColorID to an AntColor.
    /// </summary>
    public static void RegisterMapping(int rawColorId, AntColor mappedColor)
    {
        customIdToColorMap[rawColorId] = mappedColor;
    }

    /// <summary>
    /// Clears any registered custom color mappings.
    /// </summary>
    public static void ClearCustomMappings()
    {
        customIdToColorMap.Clear();
    }

    private static readonly HashSet<int> loggedColorIds = new HashSet<int>();

    /// <summary>
    /// Logs the color mapping resolution once per distinct ColorID.
    /// </summary>
    public static void LogColorMappingOnce(int rawColorId)
    {
        if (loggedColorIds.Add(rawColorId))
        {
            AntColor antColor = ToAntColor(rawColorId);
            Material mat = GetMaterial(rawColorId);
            string matName = mat != null ? mat.name : "null";
            Debug.Log($"[ColorMap] RawID={rawColorId} -> {antColor} -> Material={matName}");
        }
    }

    /// <summary>
    /// Converts a raw reference ColorID integer to a runtime AntColor enum safely without direct casting.
    /// </summary>
    public static AntColor ToAntColor(int rawColorId)
    {
        if (customIdToColorMap.TryGetValue(rawColorId, out var mappedColor))
        {
            return mappedColor;
        }

        // Authoritative mapping from Color Slither Jam reference data
        return rawColorId switch
        {
            0 => AntColor.SkyBlue,     // Sky Blue / Light Blue
            1 => AntColor.Lime,        // Lime Green
            2 => AntColor.Yellow,      // Yellow
            3 => AntColor.Purple,      // Purple
            4 => AntColor.Orange,      // Orange
            5 => AntColor.Pink,        // Pink / Magenta
            6 => AntColor.Indigo,      // Indigo / Deep Blue
            7 => AntColor.Green,       // Green
            8 => AntColor.Brown,       // Brown
            9 => AntColor.White,       // White
            10 => AntColor.Red,        // Red
            11 => AntColor.Turquoise,  // Turquoise / Aqua
            _ => LogUnknownColorId(rawColorId)
        };
    }

    private static AntColor LogUnknownColorId(int rawColorId)
    {
        Debug.LogWarning($"[ReferenceColorMapper] Unverified or unknown ColorID {rawColorId}! Falling back to White.");
        return AntColor.White;
    }

    /// <summary>
    /// Converts a raw reference ColorID directly to its runtime RGB visual Color.
    /// </summary>
    public static Color GetVisualColor(int rawColorId)
    {
        return AntColorPalette.GetColor(ToAntColor(rawColorId));
    }

    /// <summary>
    /// Converts a raw reference ColorID directly to its runtime Material.
    /// </summary>
    public static Material GetMaterial(int rawColorId)
    {
        return AntColorPalette.GetMaterial(ToAntColor(rawColorId));
    }

    /// <summary>
    /// Converts a raw reference ColorID directly to its runtime Texture.
    /// </summary>
    public static Texture2D GetTexture(int rawColorId)
    {
        return AntColorPalette.GetTexture(ToAntColor(rawColorId));
    }
}
