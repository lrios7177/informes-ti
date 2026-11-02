using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace InformesTI.Core;

public static class BackupService
{
    private const long MaxDatabaseBytes = 1024L * 1024 * 1024;
    private const string Manifest = "{\"application\":\"InformesTI\",\"formatVersion\":1}";

    public static void Export(ReportRepository repository, string destination)
    {
        if (Path.GetFullPath(destination).Equals(Path.GetFullPath(repository.DatabasePath),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException("El respaldo no puede sobrescribir la base de datos.");
        var temp = Path.Combine(Path.GetTempPath(), "InformesTI-" + Guid.NewGuid());
        Directory.CreateDirectory(temp);
        try
        {
            var snapshot = Path.Combine(temp, "informes.db");
            using (var source = repository.Open())
            using (var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = snapshot, Pooling = false }.ToString()))
            {
                target.Open();
                source.BackupDatabase(target);
            }
            AtomicFile.Write(destination, output =>
            {
                using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
                using (var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open(), Encoding.UTF8))
                    writer.Write(Manifest);
                zip.CreateEntryFromFile(snapshot, "informes.db", CompressionLevel.Optimal);
            });
        }
        finally { Directory.Delete(temp, true); }
    }

    // Sustitución completa, con una copia automática del historial anterior.
    public static string Import(ReportRepository repository, string archive)
    {
        var temp = Path.Combine(Path.GetTempPath(), "InformesTI-" + Guid.NewGuid());
        Directory.CreateDirectory(temp);
        try
        {
            var candidate = Path.Combine(temp, "informes.db");
            using (var zip = ZipFile.OpenRead(archive))
            {
                if (zip.Entries.Count != 2 || zip.Entries.Count(e => e.FullName == "manifest.json") != 1 ||
                    zip.Entries.Count(e => e.FullName == "informes.db") != 1)
                    throw new InvalidDataException("Este archivo no es un respaldo válido de Informes TI.");
                var manifest = zip.GetEntry("manifest.json")!;
                if (manifest.Length > 1024) throw new InvalidDataException("Manifiesto demasiado grande.");
                using (var stream = manifest.Open())
                using (var json = JsonDocument.Parse(stream))
                {
                    var root = json.RootElement;
                    if (root.GetProperty("application").GetString() != "InformesTI" || root.GetProperty("formatVersion").GetInt32() != 1)
                        throw new InvalidDataException("El formato del respaldo no es compatible.");
                }
                var db = zip.GetEntry("informes.db")!;
                if (db.Length > MaxDatabaseBytes) throw new InvalidDataException("El respaldo supera el límite de 1 GB.");
                using var input = db.Open();
                using var output = File.Create(candidate);
                var buffer = new byte[81920];
                long total = 0;
                int count;
                while ((count = input.Read(buffer)) > 0)
                {
                    total += count;
                    if (total > MaxDatabaseBytes) throw new InvalidDataException("El respaldo supera el límite de 1 GB.");
                    output.Write(buffer, 0, count);
                }
            }
            ValidateSnapshot(candidate);
            var previous = repository.DatabasePath + ".antes-de-importar-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6];
            File.Copy(repository.DatabasePath, previous, overwrite: false);
            // Copiar primero al mismo volumen: la sustitución final es un rename atómico.
            var staging = repository.DatabasePath + ".restaurando-" + Guid.NewGuid();
            try
            {
                File.Copy(candidate, staging);
                File.Move(staging, repository.DatabasePath, overwrite: true);
            }
            finally { if (File.Exists(staging)) File.Delete(staging); }
            return previous;
        }
        finally { Directory.Delete(temp, true); }
    }

    private static void ValidateSnapshot(string path)
    {
        using (var db = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false
        }.ToString()))
        {
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check";
            if (!Equals(cmd.ExecuteScalar(), "ok")) throw new InvalidDataException("La base de datos del respaldo está dañada.");
            cmd.CommandText = "PRAGMA user_version";
            if (Convert.ToInt32(cmd.ExecuteScalar()) != 1) throw new InvalidDataException("Versión de respaldo no compatible.");
            // Un integrity_check correcto no garantiza que existan las claves y
            // restricciones que necesita Save. Validar antes de tocar el historial.
            var expected = new Dictionary<string, string> {
                ["Reports"] = ReportRepository.ReportsSchema, ["Photos"] = ReportRepository.PhotosSchema
            };
            cmd.CommandText = "SELECT name, sql FROM sqlite_master WHERE type='table' AND name IN ('Reports','Photos')";
            using (var schema = cmd.ExecuteReader())
            {
                var count = 0;
                while (schema.Read())
                {
                    var name = schema.GetString(0);
                    if (NormalizeSchema(schema.GetString(1)) != NormalizeSchema(expected[name]))
                        throw new InvalidDataException("La estructura del respaldo no es compatible. Se conserva el historial actual.");
                    count++;
                }
                if (count != expected.Count) throw new InvalidDataException("El respaldo no contiene todas las tablas necesarias.");
            }
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE (type='table' AND name NOT IN ('Reports','Photos')) OR type IN ('trigger','view')";
            if (Convert.ToInt32(cmd.ExecuteScalar()) != 0) throw new InvalidDataException("El respaldo contiene objetos no admitidos.");
            cmd.CommandText = "PRAGMA foreign_key_check";
            using (var reader = cmd.ExecuteReader())
                if (reader.Read()) throw new InvalidDataException("El respaldo contiene fotografías huérfanas.");
            cmd.CommandText = "SELECT COUNT(*) FROM Photos p LEFT JOIN Reports r ON p.ReportId=r.Id WHERE r.Id IS NULL";
            if (Convert.ToInt32(cmd.ExecuteScalar()) != 0) throw new InvalidDataException("El respaldo contiene fotografías huérfanas.");
        }
        // Validación de campos, fechas, fotos y correspondencia de identificadores.
        var repository = new ReportRepository(Path.GetDirectoryName(path)!);
        using var connection = repository.Open();
        using var ids = connection.CreateCommand();
        ids.CommandText = "SELECT Id FROM Reports";
        var keys = new List<Guid>();
        using (var reader = ids.ExecuteReader()) while (reader.Read()) keys.Add(Guid.Parse(reader.GetString(0)));
        foreach (var key in keys)
        {
            var r = repository.Get(key) ?? throw new InvalidDataException("Informe ausente en el respaldo.");
            if (r.Id != key) throw new InvalidDataException("Identificador de informe inconsistente.");
            r.Validate();
            foreach (var photo in r.Photos) PdfExporter.ValidatePhoto(photo.Content);
        }
    }

    private static string NormalizeSchema(string sql) =>
        string.Concat(sql.Where(c => !char.IsWhiteSpace(c))).TrimEnd(';').ToUpperInvariant();
}

internal static class AtomicFile
{
    public static void Write(string path, Action<Stream> write)
    {
        path = Path.GetFullPath(path);
        var temp = path + ".temporal-" + Guid.NewGuid();
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            { write(stream); stream.Flush(true); }
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
