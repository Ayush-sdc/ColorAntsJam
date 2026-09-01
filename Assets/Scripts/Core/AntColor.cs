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
}
