using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using WarpToolkit.ComponentModel;
using WarpToolkit.WinForms.Extensions.UI;

namespace DotnetUp.WinFormsApp;

public partial class MainForm : Form, IServiceProvider
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly IUserSettingsService? _userSettingsService;
    private AppPreferences _preferences = new();
    private Font? _uiFont;
    private Font? _consoleFont;

    public MainForm()
    {
        InitializeComponent();
        InitializeToolbarSymbols();
        foreach (string channel in SdkChannels.Named)
        {
            ToolStripMenuItem item = new(channel) { Tag = channel };
            item.Click += Channel_Click;
            channelMenu.DropDownItems.Add(item);
        }
    }

    public MainForm(IServiceProvider serviceProvider) : this()
    {
        _serviceProvider = serviceProvider;
        _userSettingsService = serviceProvider.GetRequiredService<IUserSettingsService>();
    }

    object? IServiceProvider.GetService(Type serviceType) => _serviceProvider?.GetService(serviceType);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Bounds = _userSettingsService?.Get("SettingsKey_MainFormBounds",
            this.CenterToScreen(horizontalFillGrade: 85, verticalFillGrade: 85)) ?? Bounds;
        if (!Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(Bounds)))
        {
            CenterToScreen();
        }

        try
        {
            _preferences = JsonSerializer.Deserialize<AppPreferences>(
                _userSettingsService?.Get("Preferences", "{}") ?? "{}") ?? new();
            _preferences.Validate();
            ApplyPreferences();
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            _preferences = new();
            ApplyPreferences();
            ShowError($"Invalid saved preferences: {ex.Message}");
        }

        UpdateClock();
        clockTimer.Start();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_closing && _terminal is not null)
        {
            e.Cancel = true;
            if ((_terminal.IsBusy || _workflowRunning) && MessageBox.Show(this,
                "A command is running. Stop it and close the application?", "dotnetup",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            _closing = true;
            try
            {
                await CloseTerminalAsync();
                BeginInvoke(Close);
            }
            catch (Exception ex)
            {
                _closing = false;
                ShowError(ex.Message);
            }

            return;
        }

        base.OnFormClosing(e);
        if (e.Cancel)
        {
            return;
        }

        _userSettingsService?.Set("SettingsKey_MainFormBounds",
            WindowState == FormWindowState.Normal ? Bounds : RestoreBounds);
        _userSettingsService?.Set("Preferences", JsonSerializer.Serialize(_preferences));
        _userSettingsService?.Flush();
    }

    private void ApplyPreferences()
    {
        Font? previous = _uiFont;
        _uiFont = new Font(_preferences.UiFontFamily, _preferences.UiFontSize);
        Font = _uiFont;
        previous?.Dispose();
        previous = _consoleFont;
        _consoleFont = new Font(_preferences.ConsoleFontFamily, _preferences.ConsoleFontSize);
        console.Font = _consoleFont;
        previous?.Dispose();
        channelStatus.Text = $"Channel: {_preferences.Channel}";
        foreach (ToolStripMenuItem item in channelMenu.DropDownItems)
        {
            item.Checked = Equals(item.Tag, _preferences.Channel);
        }
    }

    private void Channel_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem { Tag: string channel })
        {
            _preferences.Channel = channel;
            channelStatus.Text = $"Channel: {channel}";
            foreach (ToolStripMenuItem item in channelMenu.DropDownItems)
            {
                item.Checked = Equals(item.Tag, channel);
            }
        }
    }

    private void Details_SelectionChanged(object? sender, EventArgs e) => UpdateSelectionStatus();

    private void UpdateSelectionStatus()
    {
        selectionStatus.Text = details.SelectedRows.Count > 0
            ? details.SelectedRows[0].DataBoundItem?.ToString() ?? navigation.SelectedNode?.Text ?? "System"
            : navigation.SelectedNode?.Text ?? "System";
    }

    private void ShowError(string message)
    {
        if (_closing || IsDisposed)
        {
            return;
        }

        messageStatus.Text = message;
        messageStatus.ToolTipText = message;
        messageStatus.ForeColor = Color.IndianRed;
    }

    private void SetStatus(string message)
    {
        if (_closing || IsDisposed)
        {
            return;
        }

        messageStatus.Text = message;
        messageStatus.ToolTipText = message;
        messageStatus.ForeColor = SystemColors.ControlText;
    }

    private void ClockTimer_Tick(object? sender, EventArgs e) => UpdateClock();
    private void UpdateClock() => clockStatus.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    private void Exit_Click(object? sender, EventArgs e) => Close();
    private async void Options_Click(object? sender, EventArgs e)
    {
        if (_terminal?.IsBusy == true || _workflowRunning || _scanCancellation is not null)
        {
            ShowError("Wait for or stop the current command or scan before changing options.");
            return;
        }

        using OptionsDialog dialog = new(_preferences) { Font = Font };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            ShellKind previousShell = _preferences.Shell;
            int previousDepth = _preferences.RepositoryScanDepth;
            dialog.ApplyTo(_preferences);
            ApplyPreferences();
            _userSettingsService?.Set("Preferences", JsonSerializer.Serialize(_preferences));
            _userSettingsService?.Flush();
            if (previousDepth != _preferences.RepositoryScanDepth)
            {
                Rescan_Click(this, EventArgs.Empty);
            }

            if (previousShell != _preferences.Shell && _terminal is not null)
            {
                try
                {
                    await _terminal.RestartAsync(_preferences.Shell);
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
        }
    }
}
