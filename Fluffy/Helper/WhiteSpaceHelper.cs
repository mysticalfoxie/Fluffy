namespace Fluffy.Helper;

public static class WhiteSpaceHelper
{
    public static string TrimUnicode(this string text)
    {
        return new string(text
            .Where(c => c != '\u00AD' && !char.IsControl(c))
            .ToArray());
    }
}