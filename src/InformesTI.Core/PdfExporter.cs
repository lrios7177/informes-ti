using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Fonts;

namespace InformesTI.Core;

public static class PdfExporter
{
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
        normal.Font.Size = 10;
        normal.Font.Color = Colors.DarkSlateGray;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(7);
        var heading = doc.Styles[StyleNames.Heading1]!;
        heading.Font.Size = 14;
        heading.Font.Bold = true;
        heading.Font.Color = Color.FromRgb(21, 94, 117);
        heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(12);
        var section = doc.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(2.4);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(2.2);
        section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(2);
        var header = section.Headers.Primary.AddParagraph($"INFORMES TI  |  {report.Number}");
        header.Format.Font.Size = 9;
        header.Format.Font.Bold = true;
        header.Format.Font.Color = Color.FromRgb(21, 94, 117);
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.Format.Font.Size = 8;
        footer.AddText("Desarrollado por Ing. Luis Ríos, MBA  ·  Página ");
        footer.AddPageField();
        footer.AddText(" de ");
        footer.AddNumPagesField();
        var titleParagraph = section.AddParagraph(report.Title);
        titleParagraph.Format.Font.Size = 22;
        titleParagraph.Format.Font.Bold = true;
        titleParagraph.Format.SpaceAfter = Unit.FromPoint(14);
        Add(section, "Empresa", report.Company);
        Add(section, "Ubicación", report.Location);
        Add(section, "Técnico responsable", report.Technician);
        section.AddParagraph("Fechas y tiempos", StyleNames.Heading1);
        Add(section, "Inicio", report.StartedAt.ToString("dd/MM/yyyy HH:mm"));
        Add(section, "Fin del trabajo físico", report.PhysicalEndedAt.ToString("dd/MM/yyyy HH:mm"));
        Add(section, "Fin de recuperación de los sistemas", report.RecoveryEndedAt.ToString("dd/MM/yyyy HH:mm"));
        Add(section, "Duración del trabajo físico", EmailComposer.Duration(report.PhysicalDuration));
        Add(section, "Recuperación posterior", EmailComposer.Duration(report.RecoveryDuration));
        Add(section, "Tiempo total", EmailComposer.Duration(report.TotalDuration));
        foreach (var (title, text) in new[] {
            ("Actividades realizadas", report.Activities), ("Equipos intervenidos", report.Equipment),
            ("Incidencias", report.Incidents), ("Resultados", report.Results), ("Pendientes y recomendaciones", report.Pending) })
        {
            section.AddParagraph(title, StyleNames.Heading1);
            AddMultiline(section, EmailComposer.Value(text));
        }
        for (var i = 0; i < report.Photos.Count; i++)
        {
            var photo = report.Photos[i];
            ValidatePhoto(photo.Content);
            section.AddPageBreak();
            section.AddParagraph($"Anexo fotográfico · {i + 1} de {report.Photos.Count}", StyleNames.Heading1);
            // MigraDoc admite imágenes base64, sin archivos temporales del cliente.
            using var stream = new MemoryStream(photo.Content, false);
            using var source = XImage.FromStream(stream);
            var ratio = (double)source.PixelWidth / source.PixelHeight;
            var width = Math.Min(17.0, 18.0 * ratio);
            var image = section.AddImage("base64:" + Convert.ToBase64String(photo.Content));
            image.LockAspectRatio = true;
            image.Width = Unit.FromCentimeter(width);
            image.Top = MigraDoc.DocumentObjectModel.Shapes.ShapePosition.Top;
            section.AddParagraph($"Fotografía {i + 1}: {photo.FileName}").Format.Font.Bold = true;
            AddMultiline(section, EmailComposer.Value(photo.Description));
        }
        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        using var pdf = renderer.PdfDocument;
        AtomicFile.Write(destination, output => pdf.Save(output, closeStream: false));
    }

    private static void Add(Section section, string label, string value)
    {
        var p = section.AddParagraph();
        p.AddFormattedText(label + ": ", TextFormat.Bold);
        p.AddText(value);
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
