using System.ComponentModel;
using System.IO;
using Forms = System.Windows.Forms;
using Wpf = System.Windows;

namespace PcView.App;

public partial class App : Wpf.Application
{
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PcView",
        "crash.log");

    private Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private bool _isExiting;

    protected override void OnStartup(Wpf.StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash("DispatcherUnhandled", args.Exception);
            args.Handled = false;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogCrash("DomainUnhandled", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogCrash("UnobservedTask", args.Exception);
            args.SetObserved();
        };

        base.OnStartup(e);
        ShutdownMode = Wpf.ShutdownMode.OnExplicitShutdown;

        _mainWindow = new MainWindow();
        _mainWindow.Closing += MainWindow_Closing;
        MainWindow = _mainWindow;
        _mainWindow.Show();
        _mainWindow.Activate();
        CreateTrayIcon();
    }

    protected override void OnExit(Wpf.ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open PcView", null, (_, _) => Dispatcher.Invoke(ShowMainWindow));
        menu.Items.Add("Scan", null, async (_, _) =>
        {
            await Dispatcher.InvokeAsync(ShowMainWindow);
            if (_mainWindow is not null)
            {
                await _mainWindow.RunScanAsync(refresh: false);
            }
        });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(() =>
        {
            _isExiting = true;
            Shutdown();
        }));

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "PcView",
            Icon = System.Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMainWindow);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Closing += MainWindow_Closing;
            MainWindow = _mainWindow;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = Wpf.WindowState.Normal;
        _mainWindow.Activate();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private static void LogCrash(string source, Exception? exception)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
            File.AppendAllText(
                CrashLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Crash logging must never trigger a secondary startup failure.
        }
    }
}
