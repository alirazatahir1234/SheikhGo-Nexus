using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SheikhGoNexusERP.Application.DTOs;

namespace SheikhGoNexusERP.Infrastructure.Services;

public static class ReportExportHelper
{
    static ReportExportHelper()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static (byte[] Bytes, string ContentType, string FileName) ExportNamed(
        NamedReportDto report, string format)
    {
        var fmt = (format ?? "csv").Trim().ToLowerInvariant();
        var safe = Sanitize(report.Title);
        return fmt switch
        {
            "xlsx" or "excel" => (ToExcel(report), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{safe}.xlsx"),
            "pdf" => (ToPdf(report), "application/pdf", $"{safe}.pdf"),
            _ => (ToCsv(report), "text/csv", $"{safe}.csv")
        };
    }

    public static (byte[] Bytes, string ContentType, string FileName) ExportTable(
        string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, string format)
    {
        var fmt = (format ?? "csv").Trim().ToLowerInvariant();
        var safe = Sanitize(title);
        return fmt switch
        {
            "xlsx" or "excel" => (ToExcelTable(title, headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{safe}.xlsx"),
            "pdf" => (ToPdfTable(title, headers, rows), "application/pdf", $"{safe}.pdf"),
            _ => (ToCsvTable(headers, rows), "text/csv", $"{safe}.csv")
        };
    }

    private static string Sanitize(string title) =>
        string.Join("_", title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Replace(' ', '_');

    private static byte[] ToCsv(NamedReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Code,Name,Amount,Qty,Extra");
        foreach (var r in report.Rows)
        {
            sb.Append(Csv(r.Code)).Append(',')
                .Append(Csv(r.Name)).Append(',')
                .Append(r.Amount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(r.Qty?.ToString(CultureInfo.InvariantCulture) ?? "").Append(',')
                .AppendLine(Csv(r.Extra ?? ""));
        }
        sb.AppendLine($",Total,{report.GrandTotal.ToString(CultureInfo.InvariantCulture)},,");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] ToCsvTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Csv)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Csv)));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Csv(string v)
    {
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }

    private static byte[] ToExcel(NamedReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Report");
        ws.Cell(1, 1).Value = report.Title;
        ws.Cell(2, 1).Value = "Code";
        ws.Cell(2, 2).Value = "Name";
        ws.Cell(2, 3).Value = "Amount";
        ws.Cell(2, 4).Value = "Qty";
        ws.Cell(2, 5).Value = "Extra";
        var i = 3;
        foreach (var r in report.Rows)
        {
            ws.Cell(i, 1).Value = r.Code;
            ws.Cell(i, 2).Value = r.Name;
            ws.Cell(i, 3).Value = r.Amount;
            if (r.Qty is decimal q) ws.Cell(i, 4).Value = q;
            ws.Cell(i, 5).Value = r.Extra ?? "";
            i++;
        }
        ws.Cell(i, 2).Value = "Total";
        ws.Cell(i, 3).Value = report.GrandTotal;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] ToExcelTable(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Report");
        ws.Cell(1, 1).Value = title;
        for (var c = 0; c < headers.Count; c++)
            ws.Cell(2, c + 1).Value = headers[c];
        for (var r = 0; r < rows.Count; r++)
        for (var c = 0; c < rows[r].Count; c++)
            ws.Cell(r + 3, c + 1).Value = rows[r][c];
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] ToPdf(NamedReportDto report)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text(report.Title).SemiBold().FontSize(16);
                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(4);
                        c.RelativeColumn(2);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Text("Code").SemiBold();
                        h.Cell().Text("Name").SemiBold();
                        h.Cell().AlignRight().Text("Amount").SemiBold();
                    });
                    foreach (var r in report.Rows)
                    {
                        t.Cell().Text(r.Code);
                        t.Cell().Text(r.Name);
                        t.Cell().AlignRight().Text(r.Amount.ToString("N2"));
                    }
                });
                page.Footer().AlignRight().Text($"Total: {report.GrandTotal:N2}");
            });
        });
        return doc.GeneratePdf();
    }

    private static byte[] ToPdfTable(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text(title).SemiBold().FontSize(16);
                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        foreach (var _ in headers) c.RelativeColumn();
                    });
                    t.Header(h =>
                    {
                        foreach (var header in headers)
                            h.Cell().Text(header).SemiBold();
                    });
                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                            t.Cell().Text(cell);
                    }
                });
            });
        });
        return doc.GeneratePdf();
    }
}
