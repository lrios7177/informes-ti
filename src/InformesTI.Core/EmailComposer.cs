namespace InformesTI.Core;

public static class EmailComposer
{
    public static string Duration(TimeSpan duration) => $"{(int)duration.TotalHours} h {duration.Minutes:00} min";
    public static string Compose(Report r)
    {
        r.Validate();
        return $"""
            Asunto: Entrega de informe {r.Number} — {r.Title} — {r.Company}

            Estimados señores de {r.Company}:

            Comparto el informe de mantenimiento «{r.Title}», realizado en {r.Location}.

            Inicio: {r.StartedAt:dd/MM/yyyy HH:mm}
            Fin del trabajo físico: {r.PhysicalEndedAt:dd/MM/yyyy HH:mm}
            Fin de recuperación de los sistemas: {r.RecoveryEndedAt:dd/MM/yyyy HH:mm}
            Trabajo físico: {Duration(r.PhysicalDuration)}
            Recuperación posterior: {Duration(r.RecoveryDuration)}
            Tiempo total: {Duration(r.TotalDuration)}

            Actividades:
            {Value(r.Activities)}

            Equipos intervenidos:
            {Value(r.Equipment)}

            Incidencias:
            {Value(r.Incidents)}

            Resultados:
            {Value(r.Results)}

            Pendientes:
            {Value(r.Pending)}

            Fotografías en el informe: {r.Photos.Count}.
            El PDF se adjunta manualmente al preparar el correo.

            Quedo atento a sus comentarios.

            Atentamente,
            {r.Technician}
            """;
    }
    internal static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "No registrado." : value;
}
