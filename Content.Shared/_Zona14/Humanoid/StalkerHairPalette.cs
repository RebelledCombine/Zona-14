namespace Content.Shared._Zona14.Humanoid;

public static class StalkerHairPalette
{
    public const float MaxSaturation = 0.55f;
    public const float MinValue = 0.05f;
    // Cap brightness (HSV Value) so bright/light hair is impossible — doesn't fit the S.T.A.L.K.E.R. setting.
    public const float MaxValue = 0.70f;

    public static Color Clamp(Color color)
    {
        var hsv = Color.ToHsv(color);
        hsv.Y = Math.Clamp(hsv.Y, 0f, MaxSaturation);
        hsv.Z = Math.Clamp(hsv.Z, MinValue, MaxValue);
        return Color.FromHsv(hsv);
    }
}
