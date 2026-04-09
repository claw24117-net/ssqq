using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DeliveryOrderReceiver;

public partial class App : Application
{
    public static string AppDataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DeliveryOrderReceiver-v3");

    protected override void OnStartup(StartupEventArgs e)
    {
        Directory.CreateDirectory(AppDataDir);
        Directory.CreateDirectory(Path.Combine(AppDataDir, "orders"));

        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;

        base.OnStartup(e);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        MessageBox.Show($"예기치 않은 오류:\n{e.Exception.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex) LogCrash(ex);
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var path = Path.Combine(AppDataDir, "crash.log");
            File.AppendAllText(path,
                $"[{DateTime.UtcNow:o}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
        }
        catch { /* swallow logger failures */ }
    }
}
