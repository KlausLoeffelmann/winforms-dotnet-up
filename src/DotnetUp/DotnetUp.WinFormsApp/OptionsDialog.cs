namespace DotnetUp.WinFormsApp;

internal sealed partial class OptionsDialog : Form
{
    private Font _selectedUiFont;
    private Font _selectedConsoleFont;

    public OptionsDialog(AppPreferences preferences)
    {
        _selectedUiFont = new Font(preferences.UiFontFamily, preferences.UiFontSize);
        _selectedConsoleFont = new Font(preferences.ConsoleFontFamily, preferences.ConsoleFontSize);
        InitializeComponent();
        shellChoice.SelectedIndex = (int)preferences.Shell;
        UpdateFontLabels();
    }

    public void ApplyTo(AppPreferences preferences)
    {
        preferences.Shell = (ShellKind)shellChoice.SelectedIndex;
        preferences.UiFontFamily = _selectedUiFont.FontFamily.Name;
        preferences.UiFontSize = _selectedUiFont.SizeInPoints;
        preferences.ConsoleFontFamily = _selectedConsoleFont.FontFamily.Name;
        preferences.ConsoleFontSize = _selectedConsoleFont.SizeInPoints;
    }

    private void UiFont_Click(object? sender, EventArgs e) => ChooseFont(false);
    private void ConsoleFont_Click(object? sender, EventArgs e) => ChooseFont(true);

    private void ChooseFont(bool forConsole)
    {
        using FontDialog dialog = new()
        {
            Font = forConsole ? _selectedConsoleFont : _selectedUiFont,
            MinSize = 6,
            MaxSize = 36,
            FixedPitchOnly = forConsole,
            ShowEffects = false,
            FontMustExist = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (forConsole)
        {
            _selectedConsoleFont.Dispose();
            _selectedConsoleFont = (Font)dialog.Font.Clone();
        }
        else
        {
            _selectedUiFont.Dispose();
            _selectedUiFont = (Font)dialog.Font.Clone();
        }

        UpdateFontLabels();
    }

    private void UpdateFontLabels()
    {
        uiFontButton.Text = $"UI: {_selectedUiFont.Name}, {_selectedUiFont.SizeInPoints:g} pt...";
        consoleFontButton.Text = $"Console: {_selectedConsoleFont.Name}, {_selectedConsoleFont.SizeInPoints:g} pt...";
    }
}
