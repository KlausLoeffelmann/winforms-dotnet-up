namespace DotnetUp.WinFormsApp;

internal sealed partial class RepositoryOptionsPage : UserControl
{
    public RepositoryOptionsPage() => InitializeComponent();

    public void LoadPreferences(AppPreferences preferences) => depth.Value = preferences.RepositoryScanDepth;

    public void ApplyTo(AppPreferences preferences) => preferences.RepositoryScanDepth = (int)depth.Value;

    public override Size GetPreferredSize(Size proposedSize) => fields.GetPreferredSize(proposedSize);
}
