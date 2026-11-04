#nullable enable

namespace DotnetUp.WinFormsApp;

partial class MainForm
{
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
        if (disposing)
        {
            _uiFont?.Dispose();
            _consoleFont?.Dispose();
        }
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        menu = new MenuStrip();
        ToolStripMenuItem fileMenu = new ToolStripMenuItem("&File");
        ToolStripMenuItem installItem = new ToolStripMenuItem("&Install new Global SDK...");
        ToolStripMenuItem updateItem = new ToolStripMenuItem("&Update existing SDKs...");
        ToolStripMenuItem exitItem = new ToolStripMenuItem("E&xit");
        ToolStripMenuItem uninstallItem = new ToolStripMenuItem("Uninstall selected &requirement...");
        channelMenu = new ToolStripMenuItem("&Channel");
        ToolStripMenuItem toolsMenu = new ToolStripMenuItem("&Tools");
        ToolStripMenuItem optionsItem = new ToolStripMenuItem("&Options...");
        ToolStripMenuItem restartItem = new ToolStripMenuItem("&Restart terminal");
        ToolStripMenuItem setupItem = new ToolStripMenuItem("&Setup wizard...");
        ToolStripMenuItem verifyItem = new ToolStripMenuItem("&Verify installations");
        ToolStripMenuItem environmentItem = new ToolStripMenuItem("Show &environment");
        ToolStripMenuItem infoItem = new ToolStripMenuItem("dotnetup &information");
        ToolStripMenuItem selfUpdateItem = new ToolStripMenuItem("Update &dotnetup...");
        ToolStripMenuItem rescanItem = new ToolStripMenuItem("Reset repository &discovery");
        ToolStripMenuItem repositoryMenu = new ToolStripMenuItem("&Repository");
        ToolStripMenuItem repoInstallItem = new ToolStripMenuItem("&Install SDK from global.json...");
        ToolStripMenuItem repoTerminalItem = new ToolStripMenuItem("Use repository in &terminal");
        ToolStripMenuItem repoFolderItem = new ToolStripMenuItem("Open &folder");
        ToolStripMenuItem repoJsonItem = new ToolStripMenuItem("Open &global.json");
        ContextMenuStrip repositoryContext = new ContextMenuStrip(components);
        ToolStripMenuItem contextInstall = new ToolStripMenuItem("Install SDK...");
        ToolStripMenuItem contextTerminal = new ToolStripMenuItem("Use repository in terminal");
        ToolStripMenuItem contextFolder = new ToolStripMenuItem("Open folder");
        ToolStripMenuItem contextJson = new ToolStripMenuItem("Open global.json");
        ContextMenuStrip detailContext = new ContextMenuStrip(components);
        ToolStripMenuItem contextUpdate = new ToolStripMenuItem("Update SDKs...");
        ToolStripMenuItem contextUninstall = new ToolStripMenuItem("Uninstall tracked requirement...");
        ToolStripMenuItem contextDetailFolder = new ToolStripMenuItem("Open folder");
        toolbar = new ToolStrip();
        ToolStripButton installButton = new ToolStripButton("Install SDK");
        ToolStripButton updateButton = new ToolStripButton("Update SDKs");
        ToolStripButton refreshButton = new ToolStripButton("Refresh");
        ToolStripButton stopButton = new ToolStripButton("Stop");
        status = new StatusStrip();
        selectionStatus = new ToolStripStatusLabel("System");
        channelStatus = new ToolStripStatusLabel("Channel: lts");
        messageStatus = new ToolStripStatusLabel("Ready");
        clockStatus = new ToolStripStatusLabel();
        navigationSplit = new SplitContainer();
        contentSplit = new SplitContainer();
        navigation = new TreeView();
        details = new WarpToolkit.WinForms.Specialized.WarpDataGridView();
        console = new WarpToolkit.WinForms.DirectX.Controls.D2DConsole();
        clockTimer = new System.Windows.Forms.Timer(components);
        ((System.ComponentModel.ISupportInitialize)navigationSplit).BeginInit();
        navigationSplit.Panel1.SuspendLayout();
        navigationSplit.Panel2.SuspendLayout();
        navigationSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)contentSplit).BeginInit();
        contentSplit.Panel1.SuspendLayout();
        contentSplit.Panel2.SuspendLayout();
        contentSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)details).BeginInit();
        SuspendLayout();
        installItem.Click += Install_Click;
        updateItem.Click += Update_Click;
        uninstallItem.Click += Uninstall_Click;
        exitItem.Click += Exit_Click;
        fileMenu.DropDownItems.AddRange(new ToolStripItem[] { installItem, updateItem, uninstallItem, new ToolStripSeparator(), exitItem });
        optionsItem.Click += Options_Click;
        toolsMenu.DropDownItems.Add(optionsItem);
        restartItem.Click += RestartTerminal_Click;
        toolsMenu.DropDownItems.Add(restartItem);
        setupItem.Click += Setup_Click;
        verifyItem.Click += Verify_Click;
        environmentItem.Click += Environment_Click;
        infoItem.Click += Info_Click;
        selfUpdateItem.Click += SelfUpdate_Click;
        rescanItem.Click += Rescan_Click;
        toolsMenu.DropDownItems.AddRange(new ToolStripItem[] { new ToolStripSeparator(), setupItem, verifyItem, environmentItem, infoItem, selfUpdateItem, rescanItem });
        repoInstallItem.Click += Install_Click;
        repoTerminalItem.Click += RepositoryTerminal_Click;
        repoFolderItem.Click += OpenFolder_Click;
        repoJsonItem.Click += OpenGlobalJson_Click;
        repositoryMenu.DropDownItems.AddRange(new ToolStripItem[] { repoInstallItem, repoTerminalItem, repoFolderItem, repoJsonItem });
        contextInstall.Click += Install_Click;
        contextTerminal.Click += RepositoryTerminal_Click;
        contextFolder.Click += OpenFolder_Click;
        contextJson.Click += OpenGlobalJson_Click;
        repositoryContext.Items.AddRange(new ToolStripItem[] { contextInstall, contextTerminal, contextFolder, contextJson });
        contextUpdate.Click += Update_Click;
        contextUninstall.Click += Uninstall_Click;
        contextDetailFolder.Click += OpenFolder_Click;
        detailContext.Items.AddRange(new ToolStripItem[] { contextUpdate, contextUninstall, contextDetailFolder });
        menu.Items.AddRange(new ToolStripItem[] { fileMenu, channelMenu, repositoryMenu, toolsMenu });
        menu.Dock = DockStyle.Top;
        installButton.Click += Install_Click;
        updateButton.Click += Update_Click;
        refreshButton.Click += Refresh_Click;
        stopButton.Click += Stop_Click;
        toolbar.Items.AddRange(new ToolStripItem[] { installButton, updateButton, new ToolStripSeparator(), refreshButton, stopButton });
        toolbar.Dock = DockStyle.Top;
        toolbar.GripStyle = ToolStripGripStyle.Hidden;
        status.Items.AddRange(new ToolStripItem[] { selectionStatus, channelStatus, messageStatus, clockStatus });
        status.ShowItemToolTips = true;
        selectionStatus.AutoSize = false;
        selectionStatus.Width = 240;
        selectionStatus.TextAlign = ContentAlignment.MiddleLeft;
        messageStatus.Spring = true;
        messageStatus.TextAlign = ContentAlignment.MiddleLeft;
        clockStatus.Alignment = ToolStripItemAlignment.Right;
        navigationSplit.Dock = DockStyle.Fill;
        navigationSplit.Size = new Size(1200, 720);
        navigationSplit.SplitterDistance = 270;
        navigationSplit.Panel1MinSize = 160;
        navigationSplit.Panel2MinSize = 400;
        navigationSplit.Panel1.Controls.Add(navigation);
        navigationSplit.Panel2.Controls.Add(contentSplit);
        contentSplit.Dock = DockStyle.Fill;
        contentSplit.Orientation = Orientation.Horizontal;
        contentSplit.Size = new Size(926, 720);
        contentSplit.SplitterDistance = 330;
        contentSplit.Panel1MinSize = 120;
        contentSplit.Panel2MinSize = 140;
        contentSplit.Panel1.Controls.Add(details);
        contentSplit.Panel2.Controls.Add(console);
        navigation.Dock = DockStyle.Fill;
        navigation.HideSelection = false;
        navigation.AccessibleName = "System and repositories";
        navigation.AfterSelect += Tree_AfterSelect;
        navigation.ContextMenuStrip = repositoryContext;
        navigation.NodeMouseClick += Navigation_NodeMouseClick;
        details.Dock = DockStyle.Fill;
        details.ReadOnly = true;
        details.AllowUserToAddRows = false;
        details.AllowUserToDeleteRows = false;
        details.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        details.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        details.MultiSelect = false;
        details.RowHeadersVisible = false;
        details.AccessibleName = "Selection details";
        details.SelectionChanged += Details_SelectionChanged;
        details.ContextMenuStrip = detailContext;
        details.CellMouseDown += Details_CellMouseDown;
        console.Dock = DockStyle.Fill;
        console.AccessibleName = "Hosted command terminal";
        clockTimer.Interval = 1000;
        clockTimer.Tick += ClockTimer_Tick;
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 800);
        MinimumSize = new Size(850, 580);
        Controls.Add(navigationSplit);
        Controls.Add(toolbar);
        Controls.Add(menu);
        Controls.Add(status);
        MainMenuStrip = menu;
        Name = "MainForm";
        Text = "dotnetup - SDK and Repository Manager";
        navigationSplit.Panel1.ResumeLayout(false);
        navigationSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)navigationSplit).EndInit();
        navigationSplit.ResumeLayout(false);
        contentSplit.Panel1.ResumeLayout(false);
        contentSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)contentSplit).EndInit();
        contentSplit.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)details).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private System.ComponentModel.IContainer? components;
    private MenuStrip menu = null!;
    private ToolStripMenuItem channelMenu = null!;
    private ToolStrip toolbar = null!;
    private StatusStrip status = null!;
    private ToolStripStatusLabel selectionStatus = null!;
    private ToolStripStatusLabel channelStatus = null!;
    private ToolStripStatusLabel messageStatus = null!;
    private ToolStripStatusLabel clockStatus = null!;
    private SplitContainer navigationSplit = null!;
    private SplitContainer contentSplit = null!;
    private TreeView navigation = null!;
    private WarpToolkit.WinForms.Specialized.WarpDataGridView details = null!;
    private WarpToolkit.WinForms.DirectX.Controls.D2DConsole console = null!;
    private System.Windows.Forms.Timer clockTimer = null!;
}
