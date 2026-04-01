# Changelog

All notable changes to MousePoint will be documented in this file.

## [0.2.0.0] - 2026-04-01

### Added
- Rectangle annotation tool: drag to draw semi-transparent rounded rectangles with auto fade-out
- Laser pointer 4-color presets (red, green, blue, yellow) with glow effect, cycled via XBUTTON2
- Highlighter thickness control via scroll wheel (3 levels: thin, medium, thick)
- Rectangle border thickness control via scroll wheel (3 levels: 2px, 4px, 8px)
- Ctrl+Shift+Q global exit shortcut
- Ctrl+Shift+4 for rectangle mode
- ESC to deactivate overlay (or quit during onboarding)
- Updated onboarding guide with all v0.2 controls (Korean + English)

### Changed
- Replaced AllowsTransparency with DWM hardware-accelerated transparency for dramatically smoother drawing
- F9 now hides/shows the overlay window (instead of click-through toggle)
- Laser pointer trail uses quadratic gradient opacity with thickness tapering
- Laser pointer center opacity reduced to 50% for softer appearance
- Douglas-Peucker point simplification on completed highlighter strokes
- FadeOutManager pauses during active drag to reduce Dispatcher contention

### Fixed
- Highlighter stutter when drawing 5+ rapid successive strokes (Issue #001)

## [0.1.0.0] - 2026-04-01

### Added
- Transparent overlay window covering all monitors
- Laser pointer with fade-out trail
- Highlighter with semi-transparent strokes and auto fade-out
- Mouse side button tool cycling (XBUTTON1) and color cycling (XBUTTON2)
- System tray icon with context menu
- Global hotkeys: F9 toggle, Ctrl+Shift+1/2/3 direct mode select
- First-run onboarding overlay
- DPI scaling support
- Single instance enforcement
