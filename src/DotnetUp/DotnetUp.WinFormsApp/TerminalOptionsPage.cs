namespace DotnetUp.WinFormsApp;

internal sealed partial class TerminalOptionsPage : UserControl
{
    private Font? _selectedUiFont;
    private Font? _selectedConsoleFont;

    public TerminalOptionsPage() => InitializeComponent();

    public void LoadPreferences(AppPreferences preferences)
    {
        _selectedUiFont?.Dispose();
        _selectedConsoleFont?.Dispose();
        _selectedUiFont = new Font(preferences.UiFontFamily, preferences.UiFontSize);
        _selectedConsoleFont = new Font(preferences.ConsoleFontFamily, preferences.ConsoleFontSize);
        shellChoice.SelectedIndex = (int)preferences.Shell;
        UpdateFontValues();
    }

    public void ApplyTo(AppPreferences preferences)
    {
        preferences.Shell = (ShellKind)shellChoice.SelectedIndex;
        preferences.UiFontFamily = _selectedUiFont!.FontFamily.Name;
        preferences.UiFontSize = _selectedUiFont.SizeInPoints;
        preferences.ConsoleFontFamily = _selectedConsoleFont!.FontFamily.Name;
        preferences.ConsoleFontSize = _selectedConsoleFont.SizeInPoints;
    }

    private void UiFont_Click(object? sender, EventArgs e) => ChooseFont(false);
    private void ConsoleFont_Click(object? sender, EventArgs e) => ChooseFont(true);

    private void ChooseFont(bool forConsole)
    {
        Font current = (forConsole ? _selectedConsoleFont : _selectedUiFont)
            ?? throw new InvalidOperationException("Load preferences before choosing a font.");
        using FontDialog dialog = new()
        {
            Font = current,
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

        Font selected = dialog.Font ?? throw new InvalidOperationException("The font dialog did not return a font.");
        if (forConsole)
        {
            _selectedConsoleFont?.Dispose();
            _selectedConsoleFont = (Font)selected.Clone();
        }
        else
        {
            _selectedUiFont?.Dispose();
            _selectedUiFont = (Font)selected.Clone();
        }

        UpdateFontValues();
    }

    private void UpdateFontValues()
    {
        uiFontValue.Text = $"{_selectedUiFont!.Name}, {_selectedUiFont.SizeInPoints:g} pt";
        consoleFontValue.Text = $"{_selectedConsoleFont!.Name}, {_selectedConsoleFont.SizeInPoints:g} pt";
    }

    public override Size GetPreferredSize(Size proposedSize) => fields.GetPreferredSize(proposedSize);
}
