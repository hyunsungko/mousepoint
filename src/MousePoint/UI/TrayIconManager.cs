using System.Drawing;
using System.Windows.Forms;
using MousePoint.Core;

namespace MousePoint.UI;

/// <summary>
/// 시스템 트레이(알림 영역) 아이콘 관리자.
/// 우클릭 컨텍스트 메뉴를 통해 도구 전환 및 종료 기능을 제공한다.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _laserMenuItem;
    private readonly ToolStripMenuItem _highlighterMenuItem;
    private readonly ToolStripMenuItem _rectangleMenuItem;
    private bool _disposed;

    public TrayIconManager(Action onLaserSelected, Action onHighlighterSelected,
        Action onRectangleSelected, Action onExitClicked)
    {
        _laserMenuItem = new ToolStripMenuItem("레이저 포인터") { CheckOnClick = false };
        _laserMenuItem.Click += (_, _) => onLaserSelected();

        _highlighterMenuItem = new ToolStripMenuItem("형광펜") { CheckOnClick = false };
        _highlighterMenuItem.Click += (_, _) => onHighlighterSelected();

        _rectangleMenuItem = new ToolStripMenuItem("네모박스") { CheckOnClick = false };
        _rectangleMenuItem.Click += (_, _) => onRectangleSelected();

        var exitItem = new ToolStripMenuItem("종료");
        exitItem.Click += (_, _) => onExitClicked();

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_laserMenuItem);
        contextMenu.Items.Add(_highlighterMenuItem);
        contextMenu.Items.Add(_rectangleMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        // NotifyIcon 생성 (임베디드 리소스에서 앱 아이콘 로드)
        Icon trayIcon = SystemIcons.Application;
        try
        {
            using var stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MousePoint.app.ico");
            if (stream != null)
                trayIcon = new Icon(stream);
        }
        catch { /* 로드 실패 시 기본 아이콘 사용 */ }

        _notifyIcon = new NotifyIcon
        {
            Text = "MousePoint",
            Icon = trayIcon,
            ContextMenuStrip = contextMenu,
            Visible = true
        };
    }

    /// <summary>
    /// 현재 도구 모드에 따라 메뉴 체크 표시를 업데이트한다.
    /// </summary>
    public void UpdateState(ToolMode mode)
    {
        _laserMenuItem.Checked = mode == ToolMode.Laser;
        _highlighterMenuItem.Checked = mode == ToolMode.Highlighter;
        _rectangleMenuItem.Checked = mode == ToolMode.Rectangle;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
