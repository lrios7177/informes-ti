using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace InformesTI.Core;

public sealed class ReportRepository
{
    public string DatabasePath { get; }
    public static string DefaultDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InformesTI");
    public ReportRepository(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        DatabasePath = Path.Combine(dataDirectory, "informes.db");
        using var db = Open();
        using var version = db.CreateCommand();
        version.CommandText = "PRAGMA user_version";
        var schema = Convert.ToInt32(version.ExecuteScalar());
        if (schema is not (0 or 1)) throw new InvalidDataException("Esta base de datos requiere una versión más reciente de Informes TI.");
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Reports (Id TEXT PRIMARY KEY, Payload TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Photos (
                Id TEXT PRIMARY KEY, ReportId TEXT NOT NULL REFERENCES Reports(Id) ON DELETE CASCADE,
                SortOrder INTEGER NOT NULL, FileName TEXT NOT NULL, Description TEXT NOT NULL,
                Content BLOB NOT NULL, UNIQUE(ReportId, SortOrder));
            PRAGMA user_version = 1;
            """;
        cmd.ExecuteNonQuery();
    }

    internal SqliteConnection Open()
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = DatabasePath, ForeignKeys = true, Pooling = false, DefaultTimeout = 10
        }.ToString());
        db.Open();
        return db;
    }

    public IReadOnlyList<ReportSummary> List(string search = "")
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Payload FROM Reports";
        using var reader = cmd.ExecuteReader();
        var reports = new List<ReportSummary>();
        while (reader.Read())
        {
            var r = Deserialize(reader.GetString(0));
            var text = $"{r.Title} {r.Company} {r.Location} {r.Technician} {r.Number}";
            if (text.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                reports.Add(new(r.Id, r.Title, r.Company, r.StartedAt, r.Number));
        }
        return reports.OrderByDescending(r => r.StartedAt).ToList();
    }

    internal static Report Deserialize(string payload) =>
        JsonSerializer.Deserialize<Report>(payload) ?? throw new InvalidDataException("Informe ilegible.");

    public Report? Get(Guid id)
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Payload FROM Reports WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        if (cmd.ExecuteScalar() is not string payload) return null;
        var report = Deserialize(payload);
        cmd.CommandText = "SELECT Id, FileName, Description, Content FROM Photos WHERE ReportId=$id ORDER BY SortOrder";
        using var photos = cmd.ExecuteReader();
        while (photos.Read()) report.Photos.Add(new() {
            Id = Guid.Parse(photos.GetString(0)), FileName = photos.GetString(1),
            Description = photos.GetString(2), Content = (byte[])photos[3]
        });
        return report;
    }

    public void Save(Report report)
    {
        report.Validate();
        using var db = Open();
        using var tx = db.BeginTransaction();
        using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        var updated = DateTime.UtcNow;
        var previous = report.UpdatedUtc;
        report.UpdatedUtc = updated;
        try
        {
            cmd.CommandText = "INSERT INTO Reports(Id,Payload) VALUES($id,$payload) ON CONFLICT(Id) DO UPDATE SET Payload=excluded.Payload";
            cmd.Parameters.AddWithValue("$id", report.Id.ToString());
            cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(report));
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DELETE FROM Photos WHERE ReportId=$id";
            cmd.ExecuteNonQuery();
            for (var i = 0; i < report.Photos.Count; i++)
            {
                var p = report.Photos[i];
                cmd.Parameters.Clear();
                cmd.CommandText = "INSERT INTO Photos VALUES($pid,$rid,$sort,$file,$description,$content)";
                cmd.Parameters.AddWithValue("$pid", p.Id.ToString());
                cmd.Parameters.AddWithValue("$rid", report.Id.ToString());
                cmd.Parameters.AddWithValue("$sort", i);
                cmd.Parameters.AddWithValue("$file", p.FileName);
                cmd.Parameters.AddWithValue("$description", p.Description);
                cmd.Parameters.AddWithValue("$content", p.Content);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { report.UpdatedUtc = previous; throw; }
    }

    public void Delete(Guid id)
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM Reports WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();
    }
}
