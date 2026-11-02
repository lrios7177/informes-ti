using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace InformesTI.App;

public partial class App : Application
{
    private Mutex? _instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _instance = new Mutex(true, "Local\\InformesTI-Desktop-v1", out var first);
        if (!first)
        {
            MessageBox.Show("Informes TI ya está abierto. Usa la ventana existente.", "Informes TI", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        var culture = CultureInfo.GetCultureInfo("es-PA");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("es-PA")));
        try { new MainWindow().Show(); }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo abrir Informes TI. Tus datos no se han eliminado.\n\n" + ex.Message,
                "Informes TI", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e) { _instance?.Dispose(); base.OnExit(e); }
}
