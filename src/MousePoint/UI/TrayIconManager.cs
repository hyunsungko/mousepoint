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
    private bool _disposed;

    /// <summary>
    /// 트레이 아이콘을 생성한다.
    /// </summary>
    /// <param name="onLaserSelected">레이저 포인터 메뉴 선택 시 콜백.</param>
    /// <param name="onHighlighterSelected">형광펜 메뉴 선택 시 콜백.</param>
    /// <param name="onExitClicked">종료 메뉴 선택 시 콜백.</param>
    public TrayIconManager(Action onLaserSelected, Action onHighlighterSelected, Action onExitClicked)
    {
        // 메뉴 항목 생성
        _laserMenuItem = new ToolStripMenuItem("레이저 포인터")
        {
            CheckOnClick = false
        };
        _laserMenuItem.Click += (_, _) => onLaserSelected();

        _highlighterMenuItem = new ToolStripMenuItem("형광펜")
        {
            CheckOnClick = false
        };
        _highlighterMenuItem.Click += (_, _) => onHighlighterSelected();

        var separatorItem = new ToolStripSeparator();

        var exitItem = new ToolStripMenuItem("종료");
        exitItem.Click += (_, _) => onExitClicked();

        // 컨텍스트 메뉴
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_laserMenuItem);
        contextMenu.Items.Add(_highlighterMenuItem);
        contextMenu.Items.Add(separatorItem);
        contextMenu.Items.Add(exitItem);

        // NotifyIcon 생성 (기본 시스템 아이콘 사용)
        _notifyIcon = new NotifyIcon
        {
            Text = "MousePoint",
            Icon = SystemIcons.Application,
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
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
