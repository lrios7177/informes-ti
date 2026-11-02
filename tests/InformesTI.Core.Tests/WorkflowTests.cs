using System.IO.Compression;
using System.Text;
using InformesTI.Core;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Xunit;
using Microsoft.Data.Sqlite;

namespace InformesTI.Core.Tests;

public sealed class WorkflowTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "InformesTI-tests-" + Guid.NewGuid());
    public WorkflowTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private ReportRepository Repository(string name = "datos") => new(Path.Combine(_root, name));
    // Sin pooling: Dispose debe cerrar el archivo antes de limpiar el directorio
    // temporal. Windows no permite borrar una base con un handle del pool abierto.
    private static SqliteConnection InspectionConnection(string path, SqliteOpenMode mode = SqliteOpenMode.ReadWrite) =>
        new(new SqliteConnectionStringBuilder { DataSource = path, Mode = mode, Pooling = false }.ToString());
    private static Report Sample() => new()
    {
        Company = "Empresa ficticia de pruebas", Location = "Laboratorio de pruebas", Title = "Mantenimiento nocturno — prueba sintética",
        StartedAt = new(2026, 10, 30, 23, 15, 0), PhysicalEndedAt = new(2026, 10, 31, 1, 30, 0),
        RecoveryEndedAt = new(2026, 10, 31, 3, 45, 0), Activities = "Limpieza técnica\nVerificación de arranque",
        Equipment = "Equipo sintético A", Incidents = "Sin incidencias de prueba", Results = "Sistemas recuperados",
        Pending = "Revisión de prueba programada"
    };
    private static ReportPhoto Photo(string asset = "evidencia.png", string description = "Evidencia sintética") => new()
    {
        FileName = asset, Description = description,
        Content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", asset))
    };

    [Fact]
    public void SaveAndReopenPreservesAllFieldsAndIndependentPhotoBytes()
    {
        var repository = Repository(); var report = Sample();
        report.Photos.Add(Photo()); report.Photos.Add(Photo("evidencia.jpg", "Después de la intervención"));
        repository.Save(report);
        // Nueva instancia: la persistencia no depende de objetos ni rutas de origen.
        var reopened = Repository().Get(report.Id)!;
        Assert.Equal(report.Title, reopened.Title); Assert.Equal(report.Company, reopened.Company);
        Assert.Equal(report.Location, reopened.Location); Assert.Equal(Report.Author, reopened.Technician);
        Assert.Equal(report.StartedAt, reopened.StartedAt); Assert.Equal(report.PhysicalEndedAt, reopened.PhysicalEndedAt);
        Assert.Equal(report.RecoveryEndedAt, reopened.RecoveryEndedAt);
        Assert.Equal(report.Activities, reopened.Activities); Assert.Equal(report.Equipment, reopened.Equipment);
        Assert.Equal(report.Incidents, reopened.Incidents); Assert.Equal(report.Results, reopened.Results); Assert.Equal(report.Pending, reopened.Pending);
        Assert.Equal(report.Photos.Select(p => p.Id), reopened.Photos.Select(p => p.Id));
        Assert.Equal(report.Photos[0].Content, reopened.Photos[0].Content);
        Assert.Equal("Después de la intervención", reopened.Photos[1].Description);
    }

    [Fact]
    public void EditReorderAndRemovePhotosPersistAfterReopening()
    {
        var repository = Repository(); var report = Sample();
        var first = Photo(); var second = Photo("evidencia.jpg");
        report.Photos.AddRange([first, second]); repository.Save(report);
        report.Title = "Informe corregido"; second.Description = "Vista final";
        report.Photos = [second, first]; repository.Save(report);
        var reopened = Repository().Get(report.Id)!;
        Assert.Equal("Informe corregido", reopened.Title);
        Assert.Equal(second.Id, reopened.Photos[0].Id); Assert.Equal("Vista final", reopened.Photos[0].Description);
        reopened.Photos.RemoveAt(1); repository.Save(reopened);
        Assert.Single(Repository().Get(report.Id)!.Photos);
        Assert.Single(repository.List());
    }

    [Fact]
    public void PhysicalWorkAndRecoveryCrossMidnightWithDistinctDurations()
    {
        var r = Sample(); r.Validate();
        Assert.Equal(TimeSpan.FromMinutes(135), r.PhysicalDuration);
        Assert.Equal(TimeSpan.FromMinutes(135), r.RecoveryDuration);
        Assert.Equal(TimeSpan.FromMinutes(270), r.TotalDuration);
        r.RecoveryEndedAt = r.RecoveryEndedAt.AddDays(2);
        Assert.Equal("52 h 30 min", EmailComposer.Duration(r.TotalDuration));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(2, 1)]
    public void InvalidChronologyIsRejectedWithoutReplacingSavedData(int physicalHours, int recoveryHours)
    {
        var repo = Repository(); var r = Sample(); repo.Save(r);
        var original = r.PhysicalEndedAt;
        r.PhysicalEndedAt = r.StartedAt.AddHours(physicalHours);
        r.RecoveryEndedAt = r.StartedAt.AddHours(recoveryHours);
        Assert.Throws<InvalidDataException>(() => repo.Save(r));
        Assert.Equal(original, repo.Get(r.Id)!.PhysicalEndedAt);
    }

    [Fact]
    public void RecoveryCanFinishNextDayAfterPhysicalWorkOnFirstDay()
    {
        var r = Sample(); r.PhysicalEndedAt = r.StartedAt.AddMinutes(30);
        r.Validate(); Assert.Equal(TimeSpan.FromHours(4), r.RecoveryDuration);
    }

    [Fact]
    public void DeleteCascadesPhotosAndDoesNotDeleteOtherReports()
    {
        var repo = Repository(); var a = Sample(); var b = Sample();
        a.Photos.Add(Photo()); repo.Save(a); repo.Save(b); repo.Delete(a.Id);
        Assert.Null(repo.Get(a.Id)); Assert.NotNull(repo.Get(b.Id));
        using var db = InspectionConnection(repo.DatabasePath); db.Open();
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT COUNT(*) FROM Photos";
        Assert.Equal(0L, cmd.ExecuteScalar());
    }

    [Fact]
    public void SqlTransactionRollsBackWhenPhotoIdConflictsWithAnotherReport()
    {
        var repo = Repository(); var a = Sample(); var b = Sample();
        a.Photos.Add(Photo()); b.Photos.Add(Photo("evidencia.jpg")); repo.Save(a); repo.Save(b);
        var title = b.Title; var photoId = b.Photos[0].Id;
        b.Title = "No debe persistir"; b.Photos = [a.Photos[0]];
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => repo.Save(b));
        var loaded = repo.Get(b.Id)!;
        Assert.Equal(title, loaded.Title); Assert.Equal(photoId, Assert.Single(loaded.Photos).Id);
    }

    [Fact]
    public void SearchAndSqlParameterizationPreserveLiteralText()
    {
        var repo = Repository(); var r = Sample(); r.Company = "Prueba'); DROP TABLE Reports;--"; repo.Save(r);
        Assert.Single(repo.List("drop table")); Assert.Empty(repo.List("empresa ausente"));
        Assert.Equal(r.Company, repo.Get(r.Id)!.Company);
    }

    [Fact]
    public void MissingRequiredFieldsAndInvalidPhotoFormatsAreRejected()
    {
        var repo = Repository(); var r = Sample(); r.Company = "  ";
        Assert.Throws<InvalidDataException>(() => repo.Save(r)); Assert.Empty(repo.List());
        r.Company = "Prueba"; r.Photos.Add(new() { Content = Encoding.UTF8.GetBytes("archivo inválido") });
        Assert.Throws<InvalidDataException>(() => repo.Save(r));
    }

    [Fact]
    public void BackupRestoresWholeHistoryWithDescriptionsOrderAndPreservesPreviousHistory()
    {
        var source = Repository(); var r = Sample();
        r.Photos.Add(Photo("evidencia.jpg", "Primera fotografía")); r.Photos.Add(Photo()); source.Save(r);
        var zip = Path.Combine(_root, "respaldo.zip"); BackupService.Export(source, zip);
        var target = Repository("restaurado"); var old = Sample(); target.Save(old);
        var previous = BackupService.Import(target, zip);
        Assert.True(File.Exists(previous)); Assert.Null(target.Get(old.Id));
        var restored = new ReportRepository(Path.Combine(_root, "restaurado")).Get(r.Id)!;
        Assert.Equal(r.Photos.Select(p => p.Id), restored.Photos.Select(p => p.Id));
        Assert.Equal(r.Photos[0].Content, restored.Photos[0].Content);
        Assert.Equal("Primera fotografía", restored.Photos[0].Description);
        using var prior = InspectionConnection(previous, SqliteOpenMode.ReadOnly); prior.Open();
        using var cmd = prior.CreateCommand(); cmd.CommandText = "SELECT Id FROM Reports";
        Assert.Equal(old.Id.ToString(), cmd.ExecuteScalar());
    }

    [Fact]
    public void RepeatedBackupIsConsistentAndDoesNotModifyTheSource()
    {
        var repo = Repository(); var r = Sample(); repo.Save(r);
        var file = Path.Combine(_root, "respaldo.zip");
        BackupService.Export(repo, file); r.Results = "Actualizado"; repo.Save(r); BackupService.Export(repo, file);
        var target = Repository("destino"); BackupService.Import(target, file);
        Assert.Equal("Actualizado", target.Get(r.Id)!.Results); Assert.Single(repo.List());
    }

    [Theory]
    [InlineData("../informes.db")]
    [InlineData("informes.db")]
    public void MalformedBackupNeverReplacesCurrentHistory(string entryName)
    {
        var repo = Repository(); var report = Sample(); repo.Save(report);
        var zipPath = Path.Combine(_root, "malformado.zip");
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            using (var w = new StreamWriter(zip.CreateEntry("manifest.json").Open())) w.Write("{\"application\":\"InformesTI\",\"formatVersion\":1}");
            using var entry = new StreamWriter(zip.CreateEntry(entryName).Open()); entry.Write("Esto no es SQLite");
        }
        Assert.ThrowsAny<Exception>(() => BackupService.Import(repo, zipPath));
        Assert.NotNull(repo.Get(report.Id)); Assert.Single(repo.List());
    }

    [Fact]
    public void BackupWithInvalidChronologyIsRejectedBeforeReplacement()
    {
        var repo = Repository(); var r = Sample(); repo.Save(r);
        // Simula corrupción lógica en una base SQLite estructuralmente válida.
        using (var db = InspectionConnection(repo.DatabasePath))
        {
            db.Open(); using var cmd = db.CreateCommand();
            cmd.CommandText = "UPDATE Reports SET Payload=json_set(Payload,'$.RecoveryEndedAt','2020-01-01T00:00:00')"; cmd.ExecuteNonQuery();
        }
        var zip = Path.Combine(_root, "invalido.zip"); BackupService.Export(repo, zip);
        var target = Repository("destino"); var existing = Sample(); target.Save(existing);
        Assert.Throws<InvalidDataException>(() => BackupService.Import(target, zip));
        Assert.NotNull(target.Get(existing.Id));
    }

    [Fact]
    public void BackupWithoutPrimaryKeysAndForeignKeysCannotReplaceTheHistory()
    {
        var folder = Path.Combine(_root, "estructura-invalida"); Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "informes.db");
        var report = Sample();
        using (var db = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path, Pooling = false
        }.ToString()))
        {
            db.Open(); using var cmd = db.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE Reports (Id TEXT, Payload TEXT);
                CREATE TABLE Photos (Id TEXT, ReportId TEXT, SortOrder INTEGER, FileName TEXT, Description TEXT, Content BLOB);
                PRAGMA user_version=1;
                INSERT INTO Reports VALUES ($id,$payload);
                """;
            cmd.Parameters.AddWithValue("$id", report.Id.ToString());
            cmd.Parameters.AddWithValue("$payload", System.Text.Json.JsonSerializer.Serialize(report));
            cmd.ExecuteNonQuery();
        }
        var archive = Path.Combine(_root, "estructura-invalida.zip");
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
        {
            using (var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open()))
                writer.Write("{\"application\":\"InformesTI\",\"formatVersion\":1}");
            zip.CreateEntryFromFile(path, "informes.db");
        }
        var target = Repository(); var previous = Sample(); target.Save(previous);
        Assert.Throws<InvalidDataException>(() => BackupService.Import(target, archive));
        Assert.NotNull(target.Get(previous.Id));
        Assert.Null(target.Get(report.Id));
    }

    [Fact]
    public void PdfContainsSpanishTextDatesPageNumbersAndOrderedPhotoAnnexes()
    {
        var r = Sample(); r.Photos.Add(Photo("evidencia.png", "Evidencia inicial número uno"));
        r.Photos.Add(Photo("evidencia.jpg", "Evidencia final número dos"));
        var pdf = Path.Combine(_root, "informe.pdf"); PdfExporter.Export(r, pdf);
        using var document = PdfDocument.Open(pdf);
        var pages = document.GetPages().ToList();
        Assert.True(pages.Count >= 3);
        var text = string.Join("\n", pages.Select(p => ContentOrderTextExtractor.GetText(p)));
        foreach (var expected in new[] { r.Number, r.Company, "Ing. Luis Ríos, MBA", "Actividades realizadas", "Resultados", "Pendientes", "30/10/2026 23:15", "31/10/2026 03:45", "2 h 15 min", "Página 1 de", "Evidencia inicial número uno", "Evidencia final número dos" })
            Assert.Contains(expected, text);
        Assert.True(text.IndexOf("Evidencia inicial", StringComparison.Ordinal) < text.IndexOf("Evidencia final", StringComparison.Ordinal));
        Assert.True(pages.Sum(p => p.GetImages().Count()) >= 2);
    }

    [Fact]
    public void LongNarrativeFlowsAcrossPagesWithoutLosingLastLine()
    {
        var r = Sample(); r.Activities = string.Join('\n', Enumerable.Range(1, 160).Select(i => $"Actividad de prueba {i}: verificación técnica de sistemas."));
        var pdf = Path.Combine(_root, "largo.pdf"); PdfExporter.Export(r, pdf);
        using var doc = PdfDocument.Open(pdf);
        Assert.True(doc.NumberOfPages > 2);
        Assert.Contains("Actividad de prueba 160", string.Join('\n', doc.GetPages().Select(p => ContentOrderTextExtractor.GetText(p))));
    }

    [Fact]
    public void InvalidImageCannotOverwriteExistingPdf()
    {
        var r = Sample(); var photo = Photo(); photo.Content = photo.Content[..12]; r.Photos.Add(photo);
        var file = Path.Combine(_root, "existente.pdf"); File.WriteAllText(file, "PDF anterior");
        Assert.ThrowsAny<Exception>(() => PdfExporter.Export(r, file));
        Assert.Equal("PDF anterior", File.ReadAllText(file));
    }

    [Fact]
    public void EmailContainsSubjectSeparateCompletionTimesAndPendingWork()
    {
        var r = Sample(); var text = EmailComposer.Compose(r);
        Assert.StartsWith("Asunto:", text); Assert.Contains(r.Number, text);
        Assert.Contains("Fin del trabajo físico: 31/10/2026 01:30", text);
        Assert.Contains("Fin de recuperación de los sistemas: 31/10/2026 03:45", text);
        Assert.Contains(r.Results, text); Assert.Contains(r.Pending, text); Assert.Contains(Report.Author, text);
    }
}
