namespace DotnetUp.WinFormsApp;

internal sealed partial class SdkTaskDialog : Form
{
    private readonly string _executable;
    private readonly ShellKind _shell;
    private readonly bool _update;
    private readonly RepositoryInfo? _repository;

    public SdkTaskDialog(string executable, ShellKind shell, string defaultChannel,
        bool update, RepositoryInfo? repository, string? installPath)
    {
        _executable = executable;
        _shell = shell;
        _update = update;
        _repository = repository;
        InitializeComponent();
        Text = update ? "Update existing SDKs" : "Install new Global SDK or runtime";
        channelChoice.Items.AddRange(SdkChannels.Named.Cast<object>().ToArray());
        channelChoice.Text = defaultChannel;
        componentChoice.SelectedIndex = 0;
        pathInput.Text = installPath ?? "";
        componentChoice.Enabled = !update;
        overrideChannel.Visible = update;
        includeRuntimes.Visible = update;
        migrate.Visible = !update;
        useGlobalJson.Enabled = repository?.GlobalJson is not null && !update;
        useGlobalJson.Checked = useGlobalJson.Enabled;
        updateGlobalJson.Enabled = repository?.GlobalJson is not null;
        scopeLabel.Text = repository is null ? "User-level managed installations (not Program Files)." : $"Repository: {repository.Path}";
        UpdatePreview();
    }

    public SdkTaskOptions GetOptions() => new(_update, channelChoice.Text.Trim(),
        string.IsNullOrWhiteSpace(pathInput.Text) ? null : pathInput.Text.Trim(), _repository?.Path,
        useGlobalJson.Checked, updateGlobalJson.Checked, migrate.Checked, includeRuntimes.Checked,
        overrideChannel.Checked, componentChoice.SelectedIndex switch
        {
            1 => "runtime", 2 => "aspnetcore", 3 => "windowsdesktop", _ => "sdk"
        });

    private void Input_Changed(object? sender, EventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        channelChoice.Enabled = !useGlobalJson.Checked && (!_update || overrideChannel.Checked);
        try
        {
            preview.Text = string.Join(Environment.NewLine, DotnetUpCommands.Plan(_executable, GetOptions())
                .Select(command => command.Render(_shell)));
            runButton.Enabled = true;
        }
        catch (ArgumentException ex)
        {
            preview.Text = ex.Message;
            runButton.Enabled = false;
        }
    }

    private void Component_Changed(object? sender, EventArgs e)
    {
        useGlobalJson.Enabled = componentChoice.SelectedIndex == 0 && _repository?.GlobalJson is not null && !_update;
        if (!useGlobalJson.Enabled)
        {
            useGlobalJson.Checked = false;
        }

        updateGlobalJson.Enabled = componentChoice.SelectedIndex == 0 && _repository?.GlobalJson is not null;
        if (!updateGlobalJson.Enabled)
        {
            updateGlobalJson.Checked = false;
        }

        UpdatePreview();
    }

    private void Browse_Click(object? sender, EventArgs e)
    {
        using FolderBrowserDialog browser = new() { Description = "Select a dotnetup-managed installation root", UseDescriptionForTitle = true };
        if (browser.ShowDialog(this) == DialogResult.OK)
        {
            pathInput.Text = browser.SelectedPath;
        }
    }

    private void Run_Click(object? sender, EventArgs e)
    {
        SdkTaskOptions options = GetOptions();
        if (options.Channel is "daily" or "preview" && (!options.Update || options.OverrideUpdateChannel)
            && MessageBox.Show(this, "This installs prerelease software. Daily SDKs are unvalidated and may be unsigned.\r\nContinue?",
                "Prerelease channel", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        DialogResult = DialogResult.OK;
    }
}
