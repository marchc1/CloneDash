using System.Globalization;

namespace CloneDash.CustomAlbumsCompatibility.CustomAlbums;

public static class CustomAlbumsFormat {
    public static int ParseAsInt(string value)
        => int.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    public static float ParseAsFloat(string value)
        => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    public static decimal ParseAsDecimal(string value)
        => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    
    public static bool TryParseAsInt(string value, out int result)
        => int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    
    public static bool TryParseAsFloat(string value, out float result)
        => float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    
    public static bool TryParseAsDecimal(string value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    
    public static string ToStringInvariant(int value, string format = "")
        => value.ToString(format, CultureInfo.InvariantCulture);
    
    public static string ToStringInvariant(float value, string format = "")
        => value.ToString(format, CultureInfo.InvariantCulture);
    
    public static string ToStringInvariant(decimal value, string format = "")
        => value.ToString(format, CultureInfo.InvariantCulture);
}