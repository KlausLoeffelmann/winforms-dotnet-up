namespace DotnetUp.WinFormsApp;

internal sealed partial class OptionsDialog : Form
{
    public OptionsDialog(AppPreferences preferences)
    {
        InitializeComponent();
        terminalPage.LoadPreferences(preferences);
        repositoryPage.LoadPreferences(preferences);
        tabs.AddTab("&Terminal and fonts", terminalPage);
        tabs.AddTab("&Solutions and projects", repositoryPage);
    }

    public void ApplyTo(AppPreferences preferences)
    {
        terminalPage.ApplyTo(preferences);
        repositoryPage.ApplyTo(preferences);
        preferences.Validate();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Size natural = new(Math.Max(terminalPage.GetPreferredSize(Size.Empty).Width,
            repositoryPage.GetPreferredSize(Size.Empty).Width),
            Math.Max(terminalPage.GetPreferredSize(Size.Empty).Height,
                repositoryPage.GetPreferredSize(Size.Empty).Height));
        int headerHeight = tabs.Controls.OfType<MenuStrip>().Single().Height;
        Size desired = new(natural.Width + root.Padding.Horizontal + tabs.Margin.Horizontal,
            natural.Height + headerHeight + footer.Height + root.Padding.Vertical + tabs.Margin.Vertical + footer.Margin.Vertical);
        Rectangle workingArea = Screen.FromControl(this).WorkingArea;
        Size chrome = Size - ClientSize;
        ClientSize = new Size(Math.Min(desired.Width, workingArea.Width * 3 / 4 - chrome.Width),
            Math.Min(desired.Height, workingArea.Height * 3 / 4 - chrome.Height));
    }
}
