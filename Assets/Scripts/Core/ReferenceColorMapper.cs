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

    /// <summary>
    /// Converts a raw reference ColorID integer to a runtime AntColor enum safely without direct casting.
    /// </summary>
    public static AntColor ToAntColor(int rawColorId)
    {
        if (customIdToColorMap.TryGetValue(rawColorId, out var mappedColor))
        {
            return mappedColor;
        }

        // Standard canonical ColorID mappings (preserves Level 1-3 rendering)
        return rawColorId switch
        {
            0 => AntColor.Blue,
            1 => AntColor.Green,
            2 => AntColor.Purple,
            3 => AntColor.Yellow,
            4 => AntColor.Red,
            5 => AntColor.Orange,
            6 => AntColor.Cyan,
            7 => AntColor.Pink,
            8 => AntColor.White,
            9 => AntColor.Black,
            10 => AntColor.Brown,
            _ => (AntColor)(Mathf.Abs(rawColorId) % 11)
        };
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
