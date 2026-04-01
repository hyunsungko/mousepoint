using System.Windows;
using System.Windows.Interop;

namespace MousePoint.Input;

/// <summary>
/// RegisterHotKey 기반 글로벌 핫키.
/// F9: 활성/비활성 토글
/// Ctrl+Shift+1/2/3: 키보드 폴백 (사이드버튼 없는 마우스용)
/// </summary>
internal sealed class GlobalKeyboardHook : IDisposable
{
    private const int HOTKEY_F9 = 1;
    private const int HOTKEY_CTRL_SHIFT_1 = 2;
    private const int HOTKEY_CTRL_SHIFT_2 = 3;
    private const int HOTKEY_CTRL_SHIFT_3 = 4;

    private readonly IntPtr _hwnd;
    private readonly HwndSource _hwndSource;
    private bool _disposed;

    public event Action? F9Pressed;
    public event Action? CtrlShift1Pressed;  // 레이저
    public event Action? CtrlShift2Pressed;  // 형광펜
    public event Action? CtrlShift3Pressed;  // 비활성

    public GlobalKeyboardHook(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;
        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource.AddHook(WndProc);

        NativeMethods.RegisterHotKey(_hwnd, HOTKEY_F9, 0, NativeMethods.VK_F9);
        NativeMethods.RegisterHotKey(_hwnd, HOTKEY_CTRL_SHIFT_1,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, 0x31); // '1'
        NativeMethods.RegisterHotKey(_hwnd, HOTKEY_CTRL_SHIFT_2,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, 0x32); // '2'
        NativeMethods.RegisterHotKey(_hwnd, HOTKEY_CTRL_SHIFT_3,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, 0x33); // '3'
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HOTKEY_F9:
                    F9Pressed?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_CTRL_SHIFT_1:
                    CtrlShift1Pressed?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_CTRL_SHIFT_2:
                    CtrlShift2Pressed?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_CTRL_SHIFT_3:
                    CtrlShift3Pressed?.Invoke();
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_F9);
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_CTRL_SHIFT_1);
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_CTRL_SHIFT_2);
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_CTRL_SHIFT_3);
        _hwndSource.RemoveHook(WndProc);
    }
}
