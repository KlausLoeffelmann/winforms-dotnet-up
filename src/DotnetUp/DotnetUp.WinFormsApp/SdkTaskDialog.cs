namespace DotnetUp.WinFormsApp;

internal sealed partial class SdkTaskDialog : Form
{
    private readonly string _executable;
    private readonly ShellKind _shell;
    private readonly bool _update;
    private readonly RepositoryInfo? _repository;
    private readonly bool _dedicated;

    public SdkTaskDialog(string executable, ShellKind shell, string defaultChannel,
        bool update, RepositoryInfo? repository, string? installPath, bool dedicated = false)
    {
        _executable = executable;
        _shell = shell;
        _update = update;
        _repository = repository;
        _dedicated = dedicated;
        InitializeComponent();
        Text = dedicated ? "Install SDK dedicated to this solution / project" : update ? "Update existing SDKs" : "Install new Global SDK or runtime";
        channelChoice.Items.AddRange(SdkChannels.Named.Cast<object>().ToArray());
        channelChoice.Text = defaultChannel;
        componentChoice.SelectedIndex = 0;
        pathInput.Text = installPath ?? (dedicated ? Path.Combine(repository!.Path, ".dotnet") : "");
        componentChoice.Enabled = !update;
        overrideChannel.Visible = update;
        includeRuntimes.Visible = update;
        migrate.Visible = !update;
        useGlobalJson.Enabled = repository?.GlobalJson is not null && !update;
        useGlobalJson.Checked = useGlobalJson.Enabled;
        updateGlobalJson.Enabled = repository?.GlobalJson is not null;
        configureLocal.Visible = dedicated;
        configureLocal.Checked = dedicated && useGlobalJson.Enabled;
        scopeLabel.Text = repository is null ? "User-level managed installations (not Program Files)." : $"Workspace: {repository.Path}";
        if (dedicated)
        {
            migrate.Enabled = false;
            componentChoice.Enabled = false;
            updateGlobalJson.Enabled = false;
        }
        UpdatePreview();
    }

    public SdkTaskOptions GetOptions() => new(_update, channelChoice.Text.Trim(),
        string.IsNullOrWhiteSpace(pathInput.Text) ? null : pathInput.Text.Trim(), _repository?.Path,
        useGlobalJson.Checked, updateGlobalJson.Checked, migrate.Checked, includeRuntimes.Checked,
        overrideChannel.Checked, componentChoice.SelectedIndex switch
        {
            1 => "runtime",
            2 => "aspnetcore",
            3 => "windowsdesktop",
            _ => "sdk"
        });

    public WorkspaceSdkConfiguration? GetLocalConfiguration() =>
        configureLocal.Checked ? WorkspaceSdkConfiguration.Prepare(GetOptions()) : null;

    private void Input_Changed(object? sender, EventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        channelChoice.Enabled = !useGlobalJson.Checked && (!_update || overrideChannel.Checked);
        try
        {
            if (_dedicated && string.IsNullOrWhiteSpace(pathInput.Text))
            {
                throw new ArgumentException("Choose an explicit installation root for this workspace.");
            }

            preview.Text = string.Join(Environment.NewLine, DotnetUpCommands.Plan(_executable, GetOptions())
                .Select(command => command.Render(_shell)));
            if (configureLocal.Checked)
            {
                preview.Text += Environment.NewLine + "After installation: configure " + GetLocalConfiguration()!.Path
                    + " (sdk.version and sdk.paths; other values retained, JSON reformatted).";
            }
            runButton.Enabled = true;
        }
        catch (Exception ex) when (ex is ArgumentException or System.Text.Json.JsonException or IOException
            or UnauthorizedAccessException or InvalidOperationException)
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
