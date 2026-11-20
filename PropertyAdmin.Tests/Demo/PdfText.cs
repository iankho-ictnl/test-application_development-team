using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;

namespace PropertyAdmin.Tests.Demo;

/// <summary>
/// T-112: reads the text of a PDF with PDFsharp, the library the app already uses (D-009, D-017; rule 17: no new
/// package). The text in the content stream is glyph codes, so each code is mapped back through the font's ToUnicode
/// map. Words on the same baseline form one line (joined by a space, not after a hyphen), so a test can compare whole lines.
/// </summary>
public static partial class PdfText
{
    public static IReadOnlyList<string> Lines(byte[] pdf)
    {
        using var input = new MemoryStream(pdf);
        var document = PdfReader.Open(input, PdfDocumentOpenMode.Import);
        var lines = new List<string>();
        foreach (var page in document.Pages)
        {
            var maps = FontMaps(page);
            var line = new StringBuilder();
            FontMap? font = null;
            double y = 0, lineY = double.NaN;
            foreach (var op in ContentReader.ReadContent(page).OfType<COperator>())
            {
                switch (op.OpCode.OpCodeName)
                {
                    case OpCodeName.Tf:
                        font = op.Operands.Count > 0 && op.Operands[0] is CName name && maps.TryGetValue(name.Name, out var map) ? map : null;
                        break;
                    case OpCodeName.Tj:
                    case OpCodeName.QuoteSingle:
                    case OpCodeName.QuoteDouble:
                    case OpCodeName.TJ:
                        if (!double.IsNaN(lineY) && Math.Abs(y - lineY) > 0.5)
                            Flush(line, lines);
                        lineY = y;
                        var word = new StringBuilder();
                        foreach (var text in Strings(op.Operands))
                            word.Append(Decode(text, font));
                        var part = word.ToString().Trim();
                        if (part.Length == 0)
                            break;
                        if (line.Length > 0 && line[^1] != '-')
                            line.Append(' ');
                        line.Append(part);
                        break;
                    case OpCodeName.Tm:
                        y = Number(op.Operands, 5);
                        break;
                    case OpCodeName.Td:
                    case OpCodeName.TD:
                        y += Number(op.Operands, 1);
                        break;
                    case OpCodeName.BT:
                        y = 0;
                        break;
                }
            }
            Flush(line, lines);
        }
        return lines;
    }

    private static double Number(CSequence operands, int index) => index < operands.Count
        ? operands[index] switch { CReal r => r.Value, CInteger i => i.Value, _ => 0 }
        : 0;

    private static void Flush(StringBuilder line, List<string> lines)
    {
        var text = line.ToString().Trim();
        if (text.Length > 0)
            lines.Add(text);
        line.Clear();
    }

    private static IEnumerable<CString> Strings(CSequence operands)
    {
        foreach (var item in operands)
        {
            if (item is CString s)
                yield return s;
            else if (item is CSequence nested)
                foreach (var inner in Strings(nested))
                    yield return inner;
        }
    }

    private static string Decode(CString text, FontMap? font)
    {
        if (font is null)
            return text.Value;
        var result = new StringBuilder();
        var value = text.Value;
        for (var i = 0; i + font.CodeLength <= value.Length; i += font.CodeLength)
        {
            var code = font.CodeLength == 2 ? (value[i] << 8) | value[i + 1] : value[i];
            result.Append(font.Map.TryGetValue(code, out var unicode) ? unicode : "\uFFFD");
        }
        return result.ToString();
    }

    private static Dictionary<string, FontMap> FontMaps(PdfPage page)
    {
        var maps = new Dictionary<string, FontMap>();
        var fonts = page.Elements.GetDictionary("/Resources")?.Elements.GetDictionary("/Font");
        if (fonts is null)
            return maps;
        foreach (var key in fonts.Elements.Keys)
        {
            var font = fonts.Elements.GetDictionary(key);
            var cmap = font?.Elements.GetDictionary("/ToUnicode")?.Stream?.UnfilteredValue;
            if (font is null || cmap is null)
                continue;
            var codeLength = font.Elements.GetName("/Subtype") == "/Type0" ? 2 : 1;
            maps[key] = new FontMap(codeLength, ParseCMap(Encoding.ASCII.GetString(cmap)));
        }
        return maps;
    }

    private static Dictionary<int, string> ParseCMap(string cmap)
    {
        var map = new Dictionary<int, string>();
        foreach (Match block in BfChar().Matches(cmap))
            foreach (Match pair in HexPair().Matches(block.Groups[1].Value))
                map[Convert.ToInt32(pair.Groups[1].Value, 16)] = Unicode(pair.Groups[2].Value);
        foreach (Match block in BfRange().Matches(cmap))
            foreach (Match range in HexRange().Matches(block.Groups[1].Value))
            {
                var first = Convert.ToInt32(range.Groups[1].Value, 16);
                var last = Convert.ToInt32(range.Groups[2].Value, 16);
                var start = Convert.ToInt32(range.Groups[3].Value, 16);
                for (var code = first; code <= last; code++)
                    map[code] = char.ConvertFromUtf32(start + code - first);
            }
        return map;
    }

    private static string Unicode(string hex)
    {
        var text = new StringBuilder();
        for (var i = 0; i + 4 <= hex.Length; i += 4)
            text.Append((char)Convert.ToInt32(hex.Substring(i, 4), 16));
        return text.ToString();
    }

    private sealed record FontMap(int CodeLength, Dictionary<int, string> Map);

    [GeneratedRegex(@"beginbfchar(.*?)endbfchar", RegexOptions.Singleline)]
    private static partial Regex BfChar();

    [GeneratedRegex(@"beginbfrange(.*?)endbfrange", RegexOptions.Singleline)]
    private static partial Regex BfRange();

    [GeneratedRegex(@"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>")]
    private static partial Regex HexPair();

    [GeneratedRegex(@"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>")]
    private static partial Regex HexRange();
}
