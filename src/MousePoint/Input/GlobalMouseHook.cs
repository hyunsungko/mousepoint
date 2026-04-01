using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace MousePoint.Input;

/// <summary>
/// WH_MOUSE_LL 글로벌 마우스 훅 + watchdog 자동 재설치.
/// 훅 콜백은 최소 작업만 수행하고 Dispatcher로 분리.
/// </summary>
internal sealed class GlobalMouseHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelMouseProc _hookProc;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _watchdog;
    private DateTime _lastHookCallback = DateTime.UtcNow;
    private bool _disposed;

    public event Action<int, int>? MouseMoved;           // x, y (screen coords)
    public event Action<int, int>? LeftButtonDown;        // x, y
    public event Action<int, int>? LeftButtonUp;          // x, y
    public event Action<int>? XButtonDown;                // button number (1 or 2)
    public event Action<int>? XButtonUp;                  // button number (1 or 2)
    public event Action? HookLost;                        // watchdog 감지 시

    public bool IsHooked => _hookId != IntPtr.Zero;

    public GlobalMouseHook(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _hookProc = HookCallback;

        Install();

        _watchdog = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _watchdog.Tick += WatchdogTick;
        _watchdog.Start();
    }

    private void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule is null) return;

        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);
    }

    private void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private void WatchdogTick(object? sender, EventArgs e)
    {
        if (_hookId == IntPtr.Zero) return;

        // 5초 이상 콜백이 없으면 훅이 죽은 것으로 판단
        if ((DateTime.UtcNow - _lastHookCallback).TotalSeconds > 5)
        {
            Uninstall();
            Install();

            if (_hookId == IntPtr.Zero)
            {
                HookLost?.Invoke();
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            _lastHookCallback = DateTime.UtcNow;
            var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            int msg = (int)wParam;
            int x = hookStruct.pt.x;
            int y = hookStruct.pt.y;

            // 최소 작업만 수행 → Dispatcher로 분리
            switch (msg)
            {
                case NativeMethods.WM_MOUSEMOVE:
                    _dispatcher.BeginInvoke(() => MouseMoved?.Invoke(x, y));
                    break;

                case NativeMethods.WM_LBUTTONDOWN:
                    _dispatcher.BeginInvoke(() => LeftButtonDown?.Invoke(x, y));
                    break;

                case NativeMethods.WM_LBUTTONUP:
                    _dispatcher.BeginInvoke(() => LeftButtonUp?.Invoke(x, y));
                    break;

                case NativeMethods.WM_XBUTTONDOWN:
                    int xBtnDown = (int)((hookStruct.mouseData >> 16) & 0xFFFF);
                    _dispatcher.BeginInvoke(() => XButtonDown?.Invoke(xBtnDown));
                    break;

                case NativeMethods.WM_XBUTTONUP:
                    int xBtnUp = (int)((hookStruct.mouseData >> 16) & 0xFFFF);
                    _dispatcher.BeginInvoke(() => XButtonUp?.Invoke(xBtnUp));
                    break;
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _watchdog.Stop();
        Uninstall();
    }
}
