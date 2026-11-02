using System.Text.Json.Serialization;
using System.ComponentModel;

namespace InformesTI.Core;

public sealed class Report
{
    public const string Author = "Ing. Luis Ríos, MBA";
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Company { get; set; } = "";
    public string Location { get; set; } = "";
    public string Technician { get; set; } = Author;
    public string Title { get; set; } = "Mantenimiento técnico";
    public DateTime StartedAt { get; set; } = DateTime.Today.AddHours(8);
    public DateTime PhysicalEndedAt { get; set; } = DateTime.Today.AddHours(9);
    public DateTime RecoveryEndedAt { get; set; } = DateTime.Today.AddHours(10);
    public string Activities { get; set; } = "";
    public string Equipment { get; set; } = "";
    public string Incidents { get; set; } = "";
    public string Results { get; set; } = "";
    public string Pending { get; set; } = "";
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    [JsonIgnore] public List<ReportPhoto> Photos { get; set; } = [];
    [JsonIgnore] public string Number => $"TI-{Id.ToString("N")[..8].ToUpperInvariant()}";
    [JsonIgnore] public TimeSpan PhysicalDuration => PhysicalEndedAt - StartedAt;
    [JsonIgnore] public TimeSpan RecoveryDuration => RecoveryEndedAt - PhysicalEndedAt;
    [JsonIgnore] public TimeSpan TotalDuration => RecoveryEndedAt - StartedAt;

    public void Validate()
    {
        if (Id == Guid.Empty) throw new InvalidDataException("El identificador del informe no es válido.");
        if (string.IsNullOrWhiteSpace(Company)) throw new InvalidDataException("Escribe la empresa.");
        if (string.IsNullOrWhiteSpace(Location)) throw new InvalidDataException("Escribe la ubicación.");
        if (string.IsNullOrWhiteSpace(Technician)) throw new InvalidDataException("Escribe el nombre del técnico.");
        if (string.IsNullOrWhiteSpace(Title)) throw new InvalidDataException("Escribe el título del informe.");
        if (PhysicalEndedAt < StartedAt)
            throw new InvalidDataException("El fin del trabajo físico debe ser posterior o igual al inicio. Si cruzaste medianoche, selecciona el día siguiente.");
        if (RecoveryEndedAt < PhysicalEndedAt)
            throw new InvalidDataException("El fin de recuperación debe ser posterior o igual al fin del trabajo físico.");
        string?[] fields = [Company, Location, Technician, Title, Activities, Equipment, Incidents, Results, Pending];
        if (fields.Any(f => f is null || f.Length > 100_000))
            throw new InvalidDataException("Un campo está vacío internamente o supera los 100.000 caracteres.");
        if (Photos.Count > 50 || Photos.Sum(p => (long)p.Content.Length) > 250L * 1024 * 1024)
            throw new InvalidDataException("El límite es 50 fotografías y 250 MB por informe.");
        if (Photos.Select(p => p.Id).Distinct().Count() != Photos.Count)
            throw new InvalidDataException("Hay fotografías duplicadas en el informe.");
        foreach (var photo in Photos) photo.Validate();
    }
}

public sealed class ReportPhoto : INotifyPropertyChanged
{
    private string _description = "";
    public event PropertyChangedEventHandler? PropertyChanged;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = "foto.jpg";
    public string Description
    {
        get => _description;
        set
        {
            if (_description == value) return;
            _description = value;
            PropertyChanged?.Invoke(this, new(nameof(Description)));
            PropertyChanged?.Invoke(this, new(nameof(Label)));
        }
    }
    public byte[] Content { get; set; } = [];
    [JsonIgnore] public string Label => string.IsNullOrWhiteSpace(Description) ? FileName : Description;

    public void Validate()
    {
        if (Id == Guid.Empty || string.IsNullOrWhiteSpace(FileName) || Description is null || Description.Length > 4000)
            throw new InvalidDataException("Los datos de una fotografía no son válidos (descripción: máximo 4.000 caracteres).");
        if (Content.Length is 0 or > 20 * 1024 * 1024)
            throw new InvalidDataException("Cada fotografía debe tener contenido y ocupar como máximo 20 MB.");
        var png = Content.Length >= 8 && Content.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
        var jpeg = Content.Length >= 3 && Content[0] == 255 && Content[1] == 216 && Content[2] == 255;
        if (!png && !jpeg) throw new InvalidDataException("Solo se admiten fotografías JPEG o PNG.");
    }
}

public sealed record ReportSummary(Guid Id, string Title, string Company, DateTime StartedAt, string Number)
{
    public string Label => $"{Title}\n{Company} · {StartedAt:dd/MM/yyyy} · {Number}";
}
