using System.Threading;
using System.Windows;

namespace MousePoint;

public partial class App : Application
{
    private const string MutexName = "Global\\MousePoint_SingleInstance_7A3B2C1D";
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("MousePoint가 이미 실행 중입니다.", "MousePoint",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _mutex?.ReleaseMutex(); }
        catch (ApplicationException) { /* Mutex was not owned — safe to ignore */ }
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
