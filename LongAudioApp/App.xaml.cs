using System.Windows;

namespace LongAudioApp;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AnalyticsService.Initialize();
        AnalyticsService.TrackEvent("app_launch");
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
