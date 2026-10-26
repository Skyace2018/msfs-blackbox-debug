// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Windows;

namespace MsfsInputBlackbox.Decoder;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, eventArgs) => DiagnosticLog.Write("viewer_ui_unhandled_exception", eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            DiagnosticLog.Write("viewer_process_unhandled_exception", eventArgs.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
            DiagnosticLog.Write("viewer_unobserved_task_exception", eventArgs.Exception);
        Exit += (_, _) => DiagnosticLog.Write("viewer_exit");
        DiagnosticLog.Write("viewer_start");
        ThemeManager.Apply(SettingsStore.Load().ThemeMode);
        if (e.Args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            Shutdown(SelfTest.Run() ? 0 : 1);
            return;
        }
        new MainWindow(e.Args.FirstOrDefault(File.Exists)).Show();
    }
}
