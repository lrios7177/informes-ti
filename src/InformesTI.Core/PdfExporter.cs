using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using MigraDoc.DocumentObjectModel.Tables;

namespace InformesTI.Core;

public static class PdfExporter
{
    private static readonly Color Ink = Color.FromRgb(19, 36, 58);
    private static readonly Color Accent = Color.FromRgb(0, 118, 141);
    private static readonly Color Muted = Color.FromRgb(76, 101, 124);
    private static readonly Color Line = Color.FromRgb(218, 229, 238);
    private const double ContentWidth = 17.6;
    private static readonly object FontLock = new();
    private static bool _initialized;
    private static void EnsureFonts()
    {
        lock (FontLock)
        {
            if (_initialized) return;
            GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
            _initialized = true;
        }
    }

    public static void ValidatePhoto(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var image = XImage.FromStream(stream);
        if (image.PixelWidth > 16000 || image.PixelHeight > 16000 || (long)image.PixelWidth * image.PixelHeight > 60_000_000)
            throw new InvalidDataException("La fotografía supera 60 megapíxeles o 16.000 píxeles por lado. Reduce su tamaño.");
    }

    public static void Export(Report report, string destination)
    {
        report.Validate();
        EnsureFonts();
        var doc = new Document();
        doc.Info.Title = $"{report.Number} — {report.Title}";
        doc.Info.Author = report.Technician;
        doc.Info.Subject = "Informe de mantenimiento técnico";
        var normal = doc.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Open Sans";
        normal.Font.Size = 9.5;
        normal.Font.Color = Ink;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);
        var heading = doc.Styles[StyleNames.Heading1]!;
        heading.Font.Size = 12;
        heading.Font.Bold = true;
        heading.Font.Color = Ink;
        heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(13);
        heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(7);
        heading.ParagraphFormat.KeepWithNext = true;
        var section = doc.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(2.0);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.HeaderDistance = Unit.FromCentimeter(0.8);
        section.PageSetup.FooterDistance = Unit.FromCentimeter(0.8);
        section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(1.7);
        var header = section.Headers.Primary.AddParagraph($"INFORMES TI    /    DOCUMENTACIÓN TÉCNICA");
        header.Format.Font.Size = 8;
        header.Format.Font.Bold = true;
        header.Format.Font.Color = Accent;
        header.Format.TabStops.AddTabStop(Unit.FromCentimeter(ContentWidth), TabAlignment.Right);
        header.AddText("\t" + report.Number);
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Font.Size = 7;
        footer.Format.Font.Color = Muted;
        footer.Format.Borders.Top.Width = Unit.FromPoint(0.5);
        footer.Format.Borders.Top.Color = Line;
        footer.Format.TabStops.AddTabStop(Unit.FromCentimeter(ContentWidth), TabAlignment.Right);
        footer.AddText("Desarrollado por Ing. Luis Ríos, MBA\tPágina ");
        footer.AddPageField();
        footer.AddText(" de ");
        footer.AddNumPagesField();
        var banner = NewTable(section, ContentWidth);
        var bannerCell = banner.AddRow().Cells[0];
        bannerCell.Shading.Color = Ink;
        bannerCell.VerticalAlignment = VerticalAlignment.Center;
        var bannerText = bannerCell.AddParagraph("INFORME DE MANTENIMIENTO");
        bannerText.Format.Font.Color = Colors.White;
        bannerText.Format.Font.Size = 9;
        bannerText.Format.Font.Bold = true;
        bannerText.Format.SpaceBefore = bannerText.Format.SpaceAfter = Unit.FromPoint(9);
        bannerText.Format.TabStops.AddTabStop(Unit.FromCentimeter(ContentWidth - 0.6), TabAlignment.Right);
        bannerText.AddText("\t" + report.Number);
        var titleParagraph = section.AddParagraph(report.Title);
        titleParagraph.Format.Font.Size = 22;
        titleParagraph.Format.Font.Bold = true;
        titleParagraph.Format.SpaceBefore = Unit.FromPoint(14);
        titleParagraph.Format.SpaceAfter = Unit.FromPoint(12);
        titleParagraph.Format.KeepWithNext = true;
        Add(section, "Empresa", report.Company);
        Add(section, "Ubicación", report.Location);
        Add(section, "Técnico responsable", report.Technician);
        SectionTitle(section, "01", "Fechas y tiempos");
        var times = NewTable(section, ContentWidth / 3, ContentWidth / 3, ContentWidth / 3);
        var timeRow = times.AddRow();
        var stages = new[] {
            ("INICIO", report.StartedAt), ("FIN DEL TRABAJO FÍSICO", report.PhysicalEndedAt),
            ("FIN DE RECUPERACIÓN", report.RecoveryEndedAt) };
        for (var i = 0; i < stages.Length; i++)
        {
            var label = timeRow.Cells[i].AddParagraph(stages[i].Item1);
            label.Format.Font.Size = 7;
            label.Format.Font.Bold = true;
            label.Format.Font.Color = Muted;
            timeRow.Cells[i].AddParagraph(stages[i].Item2.ToString("dd/MM/yyyy HH:mm"));
        }
        var metrics = NewTable(section, ContentWidth / 3, ContentWidth / 3, ContentWidth / 3);
        var metricRow = metrics.AddRow();
        var durations = new[] { ("TRABAJO FÍSICO", report.PhysicalDuration),
            ("RECUPERACIÓN POSTERIOR", report.RecoveryDuration), ("TIEMPO TOTAL", report.TotalDuration) };
        for (var i = 0; i < durations.Length; i++)
        {
            var cell = metricRow.Cells[i];
            cell.Shading.Color = i == 2 ? Color.FromRgb(219, 243, 247) : Color.FromRgb(239, 245, 249);
            cell.Borders.Color = Colors.White;
            cell.Borders.Width = Unit.FromPoint(3);
            var label = cell.AddParagraph(durations[i].Item1);
            label.Format.SpaceBefore = Unit.FromPoint(7);
            label.Format.Font.Size = 7;
            label.Format.Font.Bold = true;
            label.Format.Font.Color = Muted;
            var value = cell.AddParagraph(EmailComposer.Duration(durations[i].Item2));
            value.Format.Font.Size = 16;
            value.Format.Font.Bold = true;
            value.Format.Font.Color = Accent;
            value.Format.SpaceAfter = Unit.FromPoint(9);
        }
        var number = 2;
        foreach (var (title, text) in new[] {
            ("Actividades realizadas", report.Activities), ("Equipos intervenidos", report.Equipment),
            ("Incidencias", report.Incidents), ("Resultados", report.Results), ("Pendientes y recomendaciones", report.Pending) })
        {
            SectionTitle(section, (number++).ToString("00"), title);
            AddMultiline(section, EmailComposer.Value(text));
        }
        for (var i = 0; i < report.Photos.Count; i++)
        {
            var photo = report.Photos[i];
            ValidatePhoto(photo.Content);
            section.AddPageBreak();
            var eyebrow = section.AddParagraph("EVIDENCIAS / ANEXOS FOTOGRÁFICOS");
            eyebrow.Format.Font.Color = Accent;
            eyebrow.Format.Font.Size = 8;
            eyebrow.Format.Font.Bold = true;
            SectionTitle(section, (i + 1).ToString("00"), $"Anexo fotográfico · {i + 1} de {report.Photos.Count}");
            // MigraDoc admite imágenes base64, sin archivos temporales del cliente.
            using var stream = new MemoryStream(photo.Content, false);
            using var source = XImage.FromStream(stream);
            var ratio = (double)source.PixelWidth / source.PixelHeight;
            var width = Math.Min(ContentWidth - 0.6, 17.0 * ratio);
            var frame = NewTable(section, ContentWidth);
            frame.TopPadding = frame.BottomPadding = Unit.FromCentimeter(0.3);
            frame.Borders.Width = Unit.FromPoint(0.6);
            frame.Borders.Color = Line;
            var frameCell = frame.AddRow().Cells[0];
            frameCell.Shading.Color = Color.FromRgb(242, 247, 250);
            var image = frameCell.AddImage("base64:" + Convert.ToBase64String(photo.Content));
            image.LockAspectRatio = true;
            image.Width = Unit.FromCentimeter(width);
            image.Top = MigraDoc.DocumentObjectModel.Shapes.ShapePosition.Top;
            var caption = section.AddParagraph($"Fotografía {i + 1}: {photo.FileName}");
            caption.Format.SpaceBefore = Unit.FromPoint(10);
            caption.Format.Font.Bold = true;
            caption.Format.KeepWithNext = true;
            AddMultiline(section, EmailComposer.Value(photo.Description));
        }
        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        using var pdf = renderer.PdfDocument;
        foreach (var page in pdf.Pages)
        {
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(19, 36, 58)), 0, 0, page.Width.Point, 5);
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(0, 164, 190)), 0, 0, page.Width.Point * 0.32, 5);
        }
        AtomicFile.Write(destination, output => pdf.Save(output, closeStream: false));
    }

    private static void Add(Section section, string label, string value)
    {
        var p = section.AddParagraph();
        p.AddFormattedText(label + ": ", TextFormat.Bold).Font.Color = Muted;
        p.AddText(value);
    }
    private static Table NewTable(Section section, params double[] widths)
    {
        var table = section.AddTable();
        table.Rows.LeftIndent = Unit.FromPoint(0);
        table.LeftPadding = table.RightPadding = Unit.FromCentimeter(0.3);
        table.Format.SpaceAfter = Unit.FromPoint(6);
        foreach (var width in widths) table.AddColumn(Unit.FromCentimeter(width));
        return table;
    }
    private static void SectionTitle(Section section, string number, string title)
    {
        var heading = section.AddParagraph();
        heading.Style = StyleNames.Heading1;
        heading.AddFormattedText(number + "   ", TextFormat.Bold).Font.Color = Accent;
        heading.AddText(title);
        heading.Format.Borders.Bottom.Width = Unit.FromPoint(0.4);
        heading.Format.Borders.Bottom.Color = Line;
    }
    private static void AddMultiline(Section section, string value)
    {
        foreach (var line in value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            section.AddParagraph(line);
    }

    private sealed class EmbeddedFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) =>
            new(bold ? "OpenSans-Bold" : "OpenSans-Regular", false, italic);
        public byte[] GetFont(string faceName)
        {
            using var stream = typeof(PdfExporter).Assembly.GetManifestResourceStream($"InformesTI.Core.Fonts.{faceName}.ttf")
                ?? throw new InvalidOperationException("No se encontró la fuente del PDF.");
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return copy.ToArray();
        }
    }
}
