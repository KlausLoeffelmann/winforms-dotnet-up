# Normal bounds, monitor reachability and fullscreen

## Save and restore

Save at accepted closing: `Bounds` when normal, otherwise `RestoreBounds`; these preserve the last normal rectangle. Never restore minimized state at startup. Restore repaired normal bounds **before** maximized state. Settings persistence belongs to the application's selected storage policy, not mandatory generated Properties.Settings.

This complete regular-code helper repairs size/position against the nearest current monitor and captures fullscreen state before altering it:

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;

/// <summary>Repairs window placement and preserves fullscreen transitions.</summary>
public class PresentationWindow : Form
{
    private bool _fullscreen;
    private Rectangle _normalBounds;
    private FormWindowState _previousState;
    private FormBorderStyle _previousBorder;

    /// <summary>Gets normal bounds for persistence.</summary>
    public Rectangle SavedNormalBounds
        => WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

    /// <summary>Restores reachable normal bounds before restoring window state.</summary>
    public void RestoreWindow(Rectangle saved, FormWindowState state)
    {
        Rectangle area = Screen.FromRectangle(saved).WorkingArea;
        int width = Math.Clamp(saved.Width, 1, area.Width);
        int height = Math.Clamp(saved.Height, 1, area.Height);
        int x = Math.Clamp(saved.X, area.Left, area.Right - width);
        int y = Math.Clamp(saved.Y, area.Top, area.Bottom - height);
        StartPosition = FormStartPosition.Manual;
        WindowState = FormWindowState.Normal;
        Bounds = new Rectangle(x, y, width, height);
        WindowState = state == FormWindowState.Minimized ? FormWindowState.Normal : state;
    }

    /// <summary>Enters or exits application fullscreen.</summary>
    public void SetFullscreen(bool enabled)
    {
        if (_fullscreen == enabled)
        {
            return;
        }

        if (enabled)
        {
            _normalBounds = SavedNormalBounds;
            _previousState = WindowState;
            _previousBorder = FormBorderStyle;
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.FromControl(this).Bounds;
        }
        else
        {
            FormBorderStyle = _previousBorder;
            RestoreWindow(_normalBounds, _previousState);
        }

        _fullscreen = enabled;
    }

    /// <inheritdoc/>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F11 || (keyData == Keys.Escape && _fullscreen))
        {
            SetFullscreen(!_fullscreen);

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
```

The clamp prevents off-monitor placement; it is not a usable-content minimum policy. Add measured minima/adaptive overflow for the actual content, and retest when monitors or DPI change. Do not persist fullscreen borderless bounds as the normal window state.

On explicitly targeted .NET 11, the kiosk component can replace handwritten fullscreen/input/power management. Older-target helpers still need focus, exit and monitor tests; app fullscreen is not a Windows security kiosk boundary.
