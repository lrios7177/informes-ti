using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using InformesTI.Core;
using Microsoft.Win32;

namespace InformesTI.App;

public partial class MainWindow : Window
{
    private readonly ReportRepository _repository;
    private Report _current = new();
    private bool _ready, _loading, _refreshingHistory, _loadingPhoto, _dirty, _saved;
    private ReportPhoto? _selectedPhoto;

    public MainWindow(string? dataDirectory = null)
    {
        _repository = new(dataDirectory ?? ReportRepository.DefaultDataDirectory);
        InitializeComponent();
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (_, _) => SaveCurrent()));
        _ready = true;
        ShowReport(new(), saved: false);
        RefreshHistory();
    }

    private bool Operation(Action action, string error)
    {
        try { Mouse.OverrideCursor = Cursors.Wait; action(); return true; }
        catch (Exception ex)
        {
            MessageBox.Show(this, error + "\n\n" + ex.Message, "Informes TI", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = error;
            return false;
        }
        finally { Mouse.OverrideCursor = null; }
    }

    private void ShowReport(Report report, bool saved)
    {
        _loading = true;
        try
        {
            _current = report; _saved = saved; _selectedPhoto = null;
            TitleBox.Text = report.Title; CompanyBox.Text = report.Company; LocationBox.Text = report.Location;
            TechnicianBox.Text = report.Technician;
            StartDate.SelectedDate = report.StartedAt.Date; StartTime.Text = report.StartedAt.ToString("HH:mm");
            PhysicalDate.SelectedDate = report.PhysicalEndedAt.Date; PhysicalTime.Text = report.PhysicalEndedAt.ToString("HH:mm");
            RecoveryDate.SelectedDate = report.RecoveryEndedAt.Date; RecoveryTime.Text = report.RecoveryEndedAt.ToString("HH:mm");
            ActivitiesBox.Text = report.Activities; EquipmentBox.Text = report.Equipment;
            IncidentsBox.Text = report.Incidents; ResultsBox.Text = report.Results; PendingBox.Text = report.Pending;
            ReportNumber.Text = report.Number;
            EmailBox.Clear();
            RefreshPhotos(report.Photos.Count > 0 ? 0 : -1);
            _dirty = false;
            SaveState.Text = saved ? $"Guardado · {report.UpdatedUtc.ToLocalTime():dd/MM/yyyy HH:mm}" : "Nuevo · Sin guardar";
            Title = "Informes TI";
            UpdateDurations();
        }
        finally { _loading = false; }
    }

    private static DateTime ReadTimestamp(DatePicker date, TextBox time, string stage)
    {
        if (date.SelectedDate is not DateTime day || !TimeOnly.TryParseExact(time.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hour))
            throw new InvalidDataException($"Revisa la fecha y hora de {stage}. La hora debe tener el formato HH:mm, por ejemplo 23:30.");
        // Fechas locales del trabajo, sin conversiones UTC que cambien el día.
        return DateTime.SpecifyKind(day.Date.Add(hour.ToTimeSpan()), DateTimeKind.Unspecified);
    }

    private Report ReadForm() => new()
    {
        Id = _current.Id, Title = TitleBox.Text.Trim(), Company = CompanyBox.Text.Trim(),
        Location = LocationBox.Text.Trim(), Technician = TechnicianBox.Text.Trim(),
        StartedAt = ReadTimestamp(StartDate, StartTime, "inicio"),
        PhysicalEndedAt = ReadTimestamp(PhysicalDate, PhysicalTime, "fin del trabajo físico"),
        RecoveryEndedAt = ReadTimestamp(RecoveryDate, RecoveryTime, "fin de recuperación"),
        Activities = ActivitiesBox.Text, Equipment = EquipmentBox.Text, Incidents = IncidentsBox.Text,
        Results = ResultsBox.Text, Pending = PendingBox.Text, Photos = _current.Photos, UpdatedUtc = _current.UpdatedUtc
    };

    private void MarkDirty()
    {
        if (!_ready || _loading) return;
        _dirty = true;
        SaveState.Text = "Cambios sin guardar";
        Title = "Informes TI · Cambios sin guardar";
        EmailBox.Clear();
    }

    private void Editor_Changed(object sender, TextChangedEventArgs e) { MarkDirty(); if (_ready) UpdateDurations(); }
    private void Date_Changed(object? sender, SelectionChangedEventArgs e) { MarkDirty(); if (_ready) UpdateDurations(); }
    private void UpdateDurations()
    {
        if (!_ready) return;
        try
        {
            var start = ReadTimestamp(StartDate, StartTime, "inicio");
            var physical = ReadTimestamp(PhysicalDate, PhysicalTime, "fin físico");
            var recovery = ReadTimestamp(RecoveryDate, RecoveryTime, "recuperación");
            DurationText.Text = physical < start || recovery < physical
                ? "Revisa el orden de las fechas. Para cruzar medianoche, cambia también el día."
                : $"Trabajo físico: {EmailComposer.Duration(physical - start)}   ·   Recuperación: {EmailComposer.Duration(recovery - physical)}   ·   Total: {EmailComposer.Duration(recovery - start)}";
        }
        catch (InvalidDataException) { DurationText.Text = "Completa las tres fechas y horas para calcular las duraciones."; }
    }

    private bool SaveCurrent() => Operation(() =>
    {
        var report = ReadForm();
        report.Validate();
        foreach (var photo in report.Photos) PdfExporter.ValidatePhoto(photo.Content);
        _repository.Save(report);
        _current = report; _saved = true; _dirty = false;
        SaveState.Text = $"Guardado · {report.UpdatedUtc.ToLocalTime():dd/MM/yyyy HH:mm}";
        Title = "Informes TI";
        RefreshHistory(report.Id);
        StatusText.Text = "Informe y fotografías guardados localmente.";
    }, "No se pudo guardar el informe.");

    private bool MayLeave()
    {
        if (!_dirty) return true;
        var choice = MessageBox.Show(this, "Hay cambios sin guardar. ¿Quieres guardarlos antes de continuar?",
            "Cambios pendientes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return choice == MessageBoxResult.No || (choice == MessageBoxResult.Yes && SaveCurrent());
    }

    private void RefreshHistory(Guid? select = null)
    {
        _refreshingHistory = true;
        try
        {
            var reports = _repository.List(SearchBox.Text.Trim());
            HistoryList.ItemsSource = reports;
            HistoryList.SelectedItem = reports.FirstOrDefault(r => r.Id == select);
            HistoryCount.Text = reports.Count == 0 ? "No hay informes para mostrar." : $"{reports.Count} informe(s)";
        }
        finally { _refreshingHistory = false; }
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (_ready) Operation(() => RefreshHistory(_saved ? _current.Id : null), "No se pudo consultar el historial."); }
    private void History_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || _refreshingHistory || HistoryList.SelectedItem is not ReportSummary summary || summary.Id == _current.Id) return;
        if (!MayLeave()) { RefreshHistory(_saved ? _current.Id : null); return; }
        Operation(() => {
            var report = _repository.Get(summary.Id) ?? throw new InvalidDataException("El informe ya no existe.");
            ShowReport(report, saved: true); RefreshHistory(report.Id);
            StatusText.Text = "Informe recuperado del historial.";
        }, "No se pudo abrir el informe.");
    }
    private void New_Click(object sender, RoutedEventArgs e)
    {
        if (!MayLeave()) return;
        ShowReport(new(), saved: false); RefreshHistory(); EditorTabs.SelectedIndex = 0;
        CompanyBox.Focus(); StatusText.Text = "Nuevo informe. Completa los campos y pulsa Guardar.";
    }
    private void Save_Click(object sender, RoutedEventArgs e) => SaveCurrent();
    private void Window_Closing(object? sender, CancelEventArgs e) { if (_ready && !MayLeave()) e.Cancel = true; }
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var message = _saved
            ? $"¿Eliminar definitivamente el informe {_current.Number} y sus fotografías? Esta acción no se puede deshacer."
            : "¿Descartar este informe nuevo y sus fotografías?";
        if (MessageBox.Show(this, message, "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        Operation(() => {
            if (_saved) _repository.Delete(_current.Id);
            ShowReport(new(), false); RefreshHistory();
            StatusText.Text = "Informe eliminado.";
        }, "No se pudo eliminar el informe.");
    }

    private void RefreshPhotos(int index)
    {
        _loadingPhoto = true;
        try
        {
            PhotosList.ItemsSource = null;
            PhotosList.ItemsSource = _current.Photos;
            PhotosList.SelectedIndex = index;
        }
        finally { _loadingPhoto = false; }
        DisplayPhoto();
    }
    private void Photo_Selected(object sender, SelectionChangedEventArgs e) { if (!_loadingPhoto) DisplayPhoto(); }
    private void DisplayPhoto()
    {
        _loadingPhoto = true;
        try
        {
            _selectedPhoto = PhotosList.SelectedItem as ReportPhoto;
            PhotoDescription.IsEnabled = _selectedPhoto is not null;
            PhotoDescription.Text = _selectedPhoto?.Description ?? "";
            PhotoPreview.Source = null;
            if (_selectedPhoto is null) return;
            Operation(() => {
                using var stream = new MemoryStream(_selectedPhoto.Content, writable: false);
                var bitmap = new BitmapImage();
                bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 1200; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
                PhotoPreview.Source = bitmap;
            }, "No se pudo mostrar la fotografía.");
        }
        finally { _loadingPhoto = false; }
    }
    private void PhotoDescription_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loadingPhoto || _selectedPhoto is null) return;
        _selectedPhoto.Description = PhotoDescription.Text; MarkDirty();
    }
    private void AddPhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Adjuntar fotografías", Filter = "Fotografías JPEG o PNG|*.jpg;*.jpeg;*.png", Multiselect = true, RestoreDirectory = true, InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
        if (dialog.ShowDialog(this) != true) return;
        Operation(() => {
            if (_current.Photos.Count + dialog.FileNames.Length > 50) throw new InvalidDataException("Solo se admiten 50 fotografías por informe.");
            var added = new List<ReportPhoto>();
            foreach (var file in dialog.FileNames)
            {
                if (new FileInfo(file).Length > 20 * 1024 * 1024) throw new InvalidDataException($"{Path.GetFileName(file)} supera 20 MB.");
                var photo = new ReportPhoto { FileName = Path.GetFileName(file), Content = File.ReadAllBytes(file) };
                photo.Validate(); PdfExporter.ValidatePhoto(photo.Content); added.Add(photo);
            }
            if (_current.Photos.Concat(added).Sum(p => (long)p.Content.Length) > 250L * 1024 * 1024)
                throw new InvalidDataException("Las fotografías del informe superan 250 MB.");
            var selected = _current.Photos.Count;
            _current.Photos.AddRange(added); MarkDirty(); RefreshPhotos(selected);
            StatusText.Text = "Fotografías adjuntadas. Pulsa Guardar para conservarlas.";
        }, "No se pudieron adjuntar las fotografías.");
    }
    private void MovePhoto(int delta)
    {
        var index = PhotosList.SelectedIndex; var target = index + delta;
        if (index < 0 || target < 0 || target >= _current.Photos.Count) return;
        (_current.Photos[index], _current.Photos[target]) = (_current.Photos[target], _current.Photos[index]);
        MarkDirty(); RefreshPhotos(target);
    }
    private void PhotoUp_Click(object sender, RoutedEventArgs e) => MovePhoto(-1);
    private void PhotoDown_Click(object sender, RoutedEventArgs e) => MovePhoto(1);
    private void RemovePhoto_Click(object sender, RoutedEventArgs e)
    {
        var i = PhotosList.SelectedIndex;
        if (i < 0 || MessageBox.Show(this, "¿Quitar esta fotografía del informe?", "Quitar fotografía", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        _current.Photos.RemoveAt(i); MarkDirty(); RefreshPhotos(Math.Min(i, _current.Photos.Count - 1));
    }

    private void Pdf_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveCurrent()) return;
        var dialog = new SaveFileDialog { Title = "Exportar informe PDF", Filter = "Documento PDF|*.pdf", FileName = _current.Number + ".pdf", DefaultExt = ".pdf", RestoreDirectory = true, InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
        if (dialog.ShowDialog(this) != true) return;
        if (Operation(() => PdfExporter.Export(_current, dialog.FileName), "No se pudo exportar el PDF."))
        {
            StatusText.Text = "PDF exportado correctamente.";
            if (MessageBox.Show(this, "PDF generado. ¿Quieres abrirlo con tu visor predeterminado?", "Exportación completada", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                Operation(() => Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true }), "No se pudo abrir el visor de PDF.");
        }
    }
    private void Email_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveCurrent()) return;
        EmailBox.Text = EmailComposer.Compose(_current); EditorTabs.SelectedIndex = 3;
        StatusText.Text = "Correo generado. Revisa el contenido antes de copiarlo.";
    }
    private void CopyEmail_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailBox.Text)) { Email_Click(sender, e); if (string.IsNullOrWhiteSpace(EmailBox.Text)) return; }
        if (Operation(() => Clipboard.SetText(EmailBox.Text), "No se pudo acceder al portapapeles. Inténtalo de nuevo."))
            StatusText.Text = "Asunto y mensaje copiados. No se ha enviado ningún correo.";
    }
    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        if (!MayLeave()) return;
        var dialog = new SaveFileDialog { Title = "Exportar respaldo completo", Filter = "Respaldo Informes TI|*.zip", FileName = $"InformesTI-respaldo-{DateTime.Now:yyyyMMdd-HHmm}.zip", DefaultExt = ".zip", RestoreDirectory = true, InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
        if (dialog.ShowDialog(this) == true && Operation(() => BackupService.Export(_repository, dialog.FileName), "No se pudo crear el respaldo."))
            StatusText.Text = "Respaldo exportado con todos los informes guardados y sus fotografías.";
    }
    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (!MayLeave()) return;
        var dialog = new OpenFileDialog { Title = "Importar respaldo completo", Filter = "Respaldo Informes TI|*.zip", RestoreDirectory = true, InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
        if (dialog.ShowDialog(this) != true) return;
        if (MessageBox.Show(this, "La importación reemplazará todo el historial actual; no combina historiales. Se conservará una copia automática del historial anterior en la carpeta de datos. ¿Continuar?", "Confirmar importación", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        Operation(() => {
            var previous = BackupService.Import(_repository, dialog.FileName);
            ShowReport(new(), false); SearchBox.Clear(); RefreshHistory();
            StatusText.Text = "Historial restaurado. Copia anterior conservada en la carpeta de datos.";
            MessageBox.Show(this, "Respaldo importado correctamente.\n\nCopia del historial anterior:\n" + previous,
                "Importación completada", MessageBoxButton.OK, MessageBoxImage.Information);
        }, "No se pudo importar el respaldo. El historial anterior se conserva.");
    }
    private void DataFolder_Click(object sender, RoutedEventArgs e) => Operation(() =>
        Process.Start(new ProcessStartInfo(ReportRepository.DefaultDataDirectory) { UseShellExecute = true }), "No se pudo abrir la carpeta de datos.");
}
