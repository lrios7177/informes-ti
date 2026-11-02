using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InformesTI.App;
using InformesTI.Core;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "InformesTI-WpfSmoke-" + Guid.NewGuid());
        try
        {
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            var window = new MainWindow(root) { ShowInTaskbar = false };
            window.Show(); window.UpdateLayout();
            T Find<T>(string name) where T : FrameworkElement => (T)window.FindName(name);
            Find<TextBox>("CompanyBox").Text = "Empresa ficticia de prueba WPF";
            Find<TextBox>("LocationBox").Text = "Laboratorio sintético";
            Find<DatePicker>("StartDate").SelectedDate = new DateTime(2026, 10, 30);
            Find<TextBox>("StartTime").Text = "23:00";
            Find<DatePicker>("PhysicalDate").SelectedDate = new DateTime(2026, 10, 31);
            Find<TextBox>("PhysicalTime").Text = "01:00";
            Find<DatePicker>("RecoveryDate").SelectedDate = new DateTime(2026, 10, 31);
            Find<TextBox>("RecoveryTime").Text = "03:30";
            Find<TextBox>("ActivitiesBox").Text = "Prueba sintética de guardado desde WPF";
            ApplicationCommands.Save.Execute(null, window);
            var repo = new ReportRepository(root);
            var summary = repo.List().Single(); var report = repo.Get(summary.Id)!;
            Check(report.PhysicalDuration == TimeSpan.FromHours(2), "Duración física incorrecta");
            Check(report.TotalDuration == TimeSpan.FromHours(4.5), "Duración total incorrecta");
            window.Close();
            report.Photos.Add(new() { FileName = "evidencia.png", Description = "Imagen sintética de prueba WPF", Content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "evidencia.png")) });
            repo.Save(report);
            window = new MainWindow(root) { ShowInTaskbar = false };
            window.Show(); window.UpdateLayout();
            Find<ListBox>("HistoryList").SelectedIndex = 0;
            Check(Find<TextBox>("CompanyBox").Text == report.Company, "No reabre la empresa");
            Check(Find<TextBox>("ActivitiesBox").Text == report.Activities, "No reabre las actividades");
            Check(Find<Image>("PhotoPreview").Source is not null, "No carga la fotografía desde SQLite");
            Check(Find<TextBox>("PhotoDescription").Text == report.Photos[0].Description, "No reabre la descripción");
            Find<TextBox>("ResultsBox").Text = "Edición sintética persistida";
            ApplicationCommands.Save.Execute(null, window);
            Check(repo.Get(report.Id)!.Results == "Edición sintética persistida", "No guarda la edición");
            window.Close(); app.Shutdown();
            Console.WriteLine("WPF OK: recursos, ventana, guardado, reapertura, fotografía y cruce de medianoche.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
