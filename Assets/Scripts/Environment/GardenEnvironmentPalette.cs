using UnityEngine;

public static class GardenEnvironmentPalette
{
    // Lawn grass tones
    public static readonly Color GrassGreen = new Color(0.42f, 0.76f, 0.38f);
    public static readonly Color GrassShadow = new Color(0.35f, 0.65f, 0.32f);

    // Stone patio & borders
    public static readonly Color PatioBase = new Color(0.90f, 0.88f, 0.84f);
    public static readonly Color PatioBorder = new Color(0.78f, 0.74f, 0.69f);
    public static readonly Color PatioGrout = new Color(0.68f, 0.64f, 0.60f);

    // Shrub & bush foliage
    public static readonly Color FoliageDark = new Color(0.24f, 0.58f, 0.28f);
    public static readonly Color FoliageMedium = new Color(0.36f, 0.72f, 0.34f);
    public static readonly Color FoliageLight = new Color(0.55f, 0.82f, 0.38f);

    // River pebbles
    public static readonly Color PebbleLight = new Color(0.82f, 0.80f, 0.78f);
    public static readonly Color PebbleDark = new Color(0.60f, 0.60f, 0.62f);

    // Garden flowers (friendly pastel pops matching ant colors)
    public static readonly Color FlowerRed = new Color(0.95f, 0.35f, 0.35f);
    public static readonly Color FlowerYellow = new Color(1.00f, 0.84f, 0.25f);
    public static readonly Color FlowerBlue = new Color(0.35f, 0.65f, 0.98f);
    public static readonly Color FlowerPurple = new Color(0.75f, 0.45f, 0.92f);
    public static readonly Color FlowerWhite = new Color(0.96f, 0.96f, 0.98f);

    public static Material CreateLitMaterial(Color color, float smoothness = 0.2f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader)
        {
            color = color
        };

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothness);
        }

        return mat;
    }
}
