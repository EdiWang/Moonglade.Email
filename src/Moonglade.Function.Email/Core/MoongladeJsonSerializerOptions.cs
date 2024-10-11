using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Moonglade.Function.Email.Core;

public static class MoongladeJsonSerializerOptions
{
    public static JsonSerializerOptions Default => new()
    {
        // https://en.wikipedia.org/wiki/CJK_Unified_Ideographs_(Unicode_block)
        Encoder = JavaScriptEncoder.Create(
            UnicodeRanges.BasicLatin,
            UnicodeRanges.CjkCompatibility,
            UnicodeRanges.CjkCompatibilityForms,
            UnicodeRanges.CjkCompatibilityIdeographs,
            UnicodeRanges.CjkRadicalsSupplement,
            UnicodeRanges.CjkStrokes,
            UnicodeRanges.CjkUnifiedIdeographs,
            UnicodeRanges.CjkUnifiedIdeographsExtensionA,
            UnicodeRanges.CjkSymbolsandPunctuation,
            UnicodeRanges.HalfwidthandFullwidthForms),
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}