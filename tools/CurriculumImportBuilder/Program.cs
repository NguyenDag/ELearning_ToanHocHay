using System.IO.Compression;
using System.Text;

namespace CurriculumImportBuilder;

/// <summary>
/// Sinh bộ file import khung chương trình Toán 6 cho 3 bộ sách.
/// Với mỗi bộ sách: xuất 5 file CSV (Course / Nodes / Blocks / Flashcards / Resources)
/// và một workbook .xlsx cùng 5 sheet đó.
/// </summary>
internal static class Program
{
    private static readonly string[] NodeHeaders = ["NodeKey", "ParentKey", "NodeType", "Title", "OrderIndex", "IsFree", "DurationMinutes"];
    private static readonly string[] BlockHeaders = ["NodeKey", "OrderIndex", "BlockType", "ContentText", "ContentUrl", "MetadataJson"];
    private static readonly string[] CardHeaders = ["NodeKey", "DeckTitle", "CardOrder", "FrontText", "BackText", "Hint"];
    private static readonly string[] ResHeaders = ["NodeKey", "Title", "ResourceType", "ExternalUrl", "IsDownloadable", "OrderIndex"];
    private static readonly string[] CourseHeaders =
        ["Slug", "Title", "SubjectCode", "GradeCode", "FrameworkCode", "FrameworkName", "Publisher", "ListPrice", "VersionLabel", "Description"];

    private static int Main(string[] args)
    {
        var outDir = args.Length > 0
            ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "content-import"));

        Directory.CreateDirectory(outDir);

        var books = new[] { KnttBook.Build(), CtstBook.Build(), CanhDieuBook.Build() };

        foreach (var book in books)
        {
            var (nodes, blocks, cards, resources) = Flatten(book);
            var bookDir = Path.Combine(outDir, book.Slug);
            Directory.CreateDirectory(bookDir);

            WriteCsv(Path.Combine(bookDir, "course.csv"), CourseHeaders, new[] { CourseRow(book) });
            WriteCsv(Path.Combine(bookDir, "nodes.csv"), NodeHeaders, nodes);
            WriteCsv(Path.Combine(bookDir, "blocks.csv"), BlockHeaders, blocks);
            WriteCsv(Path.Combine(bookDir, "flashcards.csv"), CardHeaders, cards);
            WriteCsv(Path.Combine(bookDir, "resources.csv"), ResHeaders, resources);

            WriteXlsx(Path.Combine(outDir, book.Slug + ".xlsx"), new (string Name, string[] Headers, List<string[]> Rows)[]
            {
                ("Course", CourseHeaders, new List<string[]> { CourseRow(book) }),
                ("Nodes", NodeHeaders, nodes),
                ("Blocks", BlockHeaders, blocks),
                ("Flashcards", CardHeaders, cards),
                ("Resources", ResHeaders, resources),
            });

            Console.WriteLine($"{book.Slug,-32}  {nodes.Count,4} nodes  {blocks.Count,5} blocks  {cards.Count,4} cards  {resources.Count,3} resources");
        }

        Console.WriteLine($"\nĐã xuất vào: {outDir}");
        return 0;
    }

    private static string[] CourseRow(Book b) =>
    [
        b.Slug, b.Title, b.SubjectCode, b.GradeCode, b.FrameworkCode, b.FrameworkName, b.Publisher,
        b.ListPrice.ToString("0", System.Globalization.CultureInfo.InvariantCulture), b.VersionLabel, b.Description,
    ];

    private static (List<string[]> Nodes, List<string[]> Blocks, List<string[]> Cards, List<string[]> Resources) Flatten(Book book)
    {
        var nodes = new List<string[]>();
        var blocks = new List<string[]>();
        var cards = new List<string[]>();
        var resources = new List<string[]>();

        int chapterOrder = 1;
        foreach (var chapter in book.Chapters)
        {
            nodes.Add([chapter.Key, "", "Chapter", chapter.Title, chapterOrder.ToString(), Bool(chapter.IsFree), ""]);
            chapterOrder++;

            int lessonOrder = 1;
            foreach (var lesson in chapter.Lessons)
            {
                nodes.Add([lesson.Key, chapter.Key, "Lesson", lesson.Title, lessonOrder.ToString(),
                    Bool(lesson.IsFree || chapter.IsFree), lesson.DurationMinutes?.ToString() ?? ""]);
                lessonOrder++;

                int blockOrder = 1;
                foreach (var blk in lesson.Blocks)
                {
                    blocks.Add([lesson.Key, blockOrder.ToString(), blk.Type, blk.Text ?? "", blk.Url ?? "", blk.Meta ?? ""]);
                    blockOrder++;
                }

                int cardOrder = 1;
                foreach (var card in lesson.Cards)
                {
                    cards.Add([lesson.Key, "Từ khoá – " + StripPrefix(lesson.Title), cardOrder.ToString(),
                        card.Front, card.Back, card.Hint ?? ""]);
                    cardOrder++;
                }

                int resOrder = 1;
                foreach (var res in lesson.Resources)
                {
                    resources.Add([lesson.Key, res.Title, res.Type, res.Url, Bool(res.Downloadable), resOrder.ToString()]);
                    resOrder++;
                }
            }
        }

        return (nodes, blocks, cards, resources);
    }

    private static string Bool(bool v) => v ? "true" : "false";

    private static string StripPrefix(string title)
    {
        var idx = title.IndexOf(". ", StringComparison.Ordinal);
        return idx > 0 && idx < 12 ? title[(idx + 2)..] : title;
    }

    // ---------------- CSV ----------------

    private static void WriteCsv(string path, string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append('﻿'); // BOM để Excel đọc đúng UTF-8 tiếng Việt
        sb.AppendLine(string.Join(',', headers.Select(CsvField)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(',', row.Select(CsvField)));
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    private static string CsvField(string value)
    {
        value ??= "";
        var needsQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuote) return value;
        return '"' + value.Replace("\"", "\"\"") + '"';
    }

    // ---------------- XLSX (OpenXML tối thiểu, inline string) ----------------

    private static void WriteXlsx(string path, IReadOnlyList<(string Name, string[] Headers, List<string[]> Rows)> sheets)
    {
        if (File.Exists(path)) File.Delete(path);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);

        Entry(zip, "[Content_Types].xml", ContentTypes(sheets.Count));
        Entry(zip, "_rels/.rels",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
            """);
        Entry(zip, "xl/workbook.xml", Workbook(sheets));
        Entry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Count));

        for (int i = 0; i < sheets.Count; i++)
            Entry(zip, $"xl/worksheets/sheet{i + 1}.xml", Sheet(sheets[i].Headers, sheets[i].Rows));
    }

    private static void Entry(ZipArchive zip, string name, string content)
    {
        var e = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
        w.Write(content);
    }

    private static string ContentTypes(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""");
        for (int i = 1; i <= sheetCount; i++)
            sb.Append($"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""");
        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string Workbook(IReadOnlyList<(string Name, string[] Headers, List<string[]> Rows)> sheets)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>""");
        for (int i = 0; i < sheets.Count; i++)
            sb.Append($"""<sheet name="{XmlAttr(sheets[i].Name)}" sheetId="{i + 1}" r:id="rId{i + 1}"/>""");
        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string WorkbookRels(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
        for (int i = 1; i <= sheetCount; i++)
            sb.Append($"""<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml"/>""");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private static string Sheet(string[] headers, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
        AppendRow(sb, 1, headers);
        for (int r = 0; r < rows.Count; r++)
            AppendRow(sb, r + 2, rows[r]);
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, int rowNum, string[] cells)
    {
        sb.Append($"<row r=\"{rowNum}\">");
        for (int c = 0; c < cells.Length; c++)
        {
            var reference = ColumnName(c) + rowNum;
            sb.Append($"<c r=\"{reference}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{XmlText(cells[c])}</t></is></c>");
        }
        sb.Append("</row>");
    }

    private static string ColumnName(int index)
    {
        var sb = new StringBuilder();
        index++;
        while (index > 0)
        {
            index--;
            sb.Insert(0, (char)('A' + index % 26));
            index /= 26;
        }
        return sb.ToString();
    }

    private static string XmlText(string value)
    {
        value ??= "";
        var sb = new StringBuilder(value.Length + 16);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '\t' or '\n' or '\r': sb.Append(ch); break;
                default:
                    if (ch < 0x20) sb.Append(' ');
                    else sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string XmlAttr(string value) => XmlText(value).Replace("\"", "&quot;");
}
