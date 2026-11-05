using WarpToolkit.WinForms.Containers.Wizard;

namespace DotnetUp.WinFormsApp;

internal sealed record SetupOptions(string Channel, string Mode, bool OnPath, bool Migrate, bool InstallSdk);

internal sealed partial class SetupWizard : Form
{
    private readonly List<(string Title, Control Content)> _pages = [];
    private int _pageIndex;
    private readonly ComboBox _channel = new() { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly CheckBox _onPath = new() { Text = "Add dotnetup to PATH (opt-in)", AutoSize = true };
    private readonly CheckBox _migrate = new() { Text = "Migrate matching system SDKs into dotnetup", AutoSize = true };
    private readonly CheckBox _install = new() { Text = "Install an initial SDK", Checked = true, AutoSize = true };
    private readonly TextBox _review = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };


    public SetupWizard(string defaultChannel, Font font, bool alreadyInstalled)
    {
        InitializeComponent();
        Font = font;
        TableLayoutPanel welcome = Page();

        welcome.Controls.Add(Paragraph("This wizard installs and configures the public-preview dotnetup tool.\r\n"
            + "All commands run visibly in the main window's terminal. Nothing runs until you finish.\r\n\r\n"
            + (alreadyInstalled ? "An existing dotnetup executable was found; downloading will be skipped.\r\n\r\n"
                : "Download command (executed in PowerShell):\r\n" + DotnetUpCommands.Bootstrap + "\r\n\r\n")
            + "The Microsoft installer downloads and executes a remote PowerShell script.\r\n"
            + "The default is LTS with no PATH changes. Your Visual Studio / system SDKs remain untouched.\r\n"
            + "You can cancel now and reopen this wizard from Tools."));

        AddPage("Welcome to dotnetup", welcome);

        TableLayoutPanel settings = Page();
        _channel.Items.AddRange(SdkChannels.Named.Cast<object>().ToArray());
        _channel.SelectedItem = defaultChannel;

        _mode.Items.AddRange(
            ["none - managed dotnet only (recommended)"
            , "shell - PowerShell (pwsh) profile"
            , "everywhere - system PATH (advanced)"]);

        _mode.SelectedIndex = 0;

        settings.Controls.Add(Paragraph("Initial SDK channel:"));
        settings.Controls.Add(_channel);
        settings.Controls.Add(_install);
        settings.Controls.Add(Paragraph("How applications find the managed .NET installation:"));
        settings.Controls.Add(_mode);
        settings.Controls.Add(_onPath);
        settings.Controls.Add(_migrate);

        settings.Controls.Add(Paragraph("WARNING: everywhere requires elevation and changes system PATH for all users.\r\n"
            + "It can override Visual Studio and other machine-wide SDKs. Migration is strongly advised.\r\n"
            + "Existing terminals do not automatically inherit environment changes.\r\n"
            + "Daily SDKs are unvalidated and may be unsigned."));

        AddPage("Choose channel and environment", settings);

        TableLayoutPanel review = Page();
        review.RowStyles.Clear();
        review.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        review.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        review.Controls.Add(Paragraph("Review the plan, then Finish to execute it in the hosted terminal."));
        review.Controls.Add(_review);
        AddPage("Review and install", review);
        nextButton.Click += (_, _) =>
        {
            if (_pageIndex == _pages.Count - 1)
            {
                DialogResult = DialogResult.OK;
                return;
            }

            UpdateReview(alreadyInstalled);
            ShowPage(_pageIndex + 1);
        };

        UpdateReview(alreadyInstalled);
        ShowPage(0);
    }

    public SetupOptions GetOptions() => new(_channel.SelectedItem?.ToString() ?? "lts",
        _mode.SelectedIndex switch { 1 => "shell", 2 => "everywhere", _ => "none" },
        _onPath.Checked, _migrate.Checked, _install.Checked);

    internal static bool IsRequired(AppPreferences preferences, string? executable) =>
        !preferences.SetupCompleted && executable is null;

    public async Task<WizardDialogResult> ShowSetupAsync(IWin32Window owner) =>
        await ShowDialogAsync(owner) == DialogResult.OK ? WizardDialogResult.Finished : WizardDialogResult.Cancelled;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.None)
        {
            DialogResult = DialogResult.Cancel;
        }

        base.OnFormClosing(e);
    }

    private void AddPage(string title, Control content)
    {
        _pages.Add((title, content));
        content.Visible = false;
        pageHost.Controls.Add(content);
    }

    private void ShowPage(int index)
    {
        _pages[_pageIndex].Content.Visible = false;
        _pageIndex = index;
        titleLabel.Text = _pages[index].Title;
        _pages[index].Content.Visible = true;
        _pages[index].Content.BringToFront();
        backButton.Enabled = index > 0;
        nextButton.Text = index == _pages.Count - 1 ? "&Finish" : "&Next";
        nextButton.AccessibleName = index == _pages.Count - 1 ? "Finish setup" : "Next setup page";
        ActiveControl = nextButton;
    }

    private void Back_Click(object? sender, EventArgs e) => ShowPage(_pageIndex - 1);

    private void UpdateReview(bool alreadyInstalled)
    {
        SetupOptions options = GetOptions();
        string executable = SystemInventory.FindDotnetUp() ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnetup", "dotnetup.exe");
        List<string> commands = [];
        if (!alreadyInstalled)
        {
            commands.Add(DotnetUpCommands.Bootstrap);
        }

        commands.AddRange(SetupCommands(executable, options).Select(command => command.Render(ShellKind.PowerShell)));
        _review.Text = string.Join(Environment.NewLine + Environment.NewLine, commands)
            + "\r\n\r\nThe app's default channel is not changed. Setup is saved only after all commands succeed.";
    }

    internal static IReadOnlyList<ShellCommand> SetupCommands(string executable, SetupOptions options)
    {
        SdkChannels.Validate(options.Channel);
        if (options.Mode is not ("none" or "shell" or "everywhere"))
        {
            throw new ArgumentException("Unsupported access mode.");
        }

        List<string> environment = ["env", "set", options.Mode, "--dotnetup-on-path", options.OnPath ? "true" : "false"];
        if (options.Mode == "shell")
        {
            environment.AddRange(["--shell", "pwsh"]);
        }

        List<ShellCommand> commands = [new(executable, ["--version"]), new(executable, environment)];
        if (options.InstallSdk)
        {
            commands.AddRange(DotnetUpCommands.Plan(executable,
                new SdkTaskOptions(false, options.Channel, Migrate: options.Migrate)));

            commands.Add(new ShellCommand(executable, ["dotnet", "--", "--version"]));
        }

        commands.Add(new ShellCommand(executable, ["env", "show"]));
        commands.Add(new ShellCommand(executable, ["list"]));

        return commands;
    }

    private static TableLayoutPanel Page()
    {
        TableLayoutPanel page = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            AutoScroll = true
        };

        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return page;
    }

    private static Label Paragraph(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Dock = DockStyle.Top,
        Margin = new Padding(3, 8, 3, 8)
    };

}
