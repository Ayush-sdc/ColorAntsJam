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
    Black
}

public static class AntColorPalette
{
    public static Color GetColor(AntColor antColor)
    {
        return antColor switch
        {
            AntColor.Blue => new Color(0.18f, 0.55f, 1f),
            AntColor.Green => new Color(0.22f, 0.82f, 0.35f),
            AntColor.Purple => new Color(0.68f, 0.28f, 0.95f),
            AntColor.Yellow => new Color(1f, 0.82f, 0.12f),
            AntColor.Red => new Color(0.95f, 0.24f, 0.24f),
            AntColor.Orange => new Color(1f, 0.55f, 0.12f),
            AntColor.Cyan => new Color(0.12f, 0.85f, 0.95f),
            AntColor.Pink => new Color(1f, 0.38f, 0.72f),
            AntColor.White => new Color(0.94f, 0.94f, 0.96f),
            AntColor.Black => new Color(0.20f, 0.20f, 0.22f),
            _ => Color.white
        };
    }
}
