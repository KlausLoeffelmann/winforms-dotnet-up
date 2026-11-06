#nullable enable

namespace DotnetUp.WinFormsApp;

partial class OptionsDialog
{
    private void InitializeComponent()
    {
        root = new TableLayoutPanel();
        tabs = new WarpToolkit.WinForms.Containers.FluentTabControl();
        footer = new TableLayoutPanel();
        buttons = new TableLayoutPanel();
        ok = new Button();
        cancel = new Button();
        terminalPage = new TerminalOptionsPage();
        repositoryPage = new RepositoryOptionsPage();
        root.SuspendLayout();
        footer.SuspendLayout();
        buttons.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(tabs, 0, 0);
        root.Controls.Add(footer, 0, 1);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Name = "root";
        root.Padding = new Padding(12);
        root.RowCount = 2;
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle());
        root.Size = new Size(640, 420);
        root.TabIndex = 0;
        // 
        // tabs
        // 
        tabs.AccessibleName = "Options categories";
        tabs.Dock = DockStyle.Fill;
        tabs.Location = new Point(15, 15);
        tabs.Name = "tabs";
        tabs.Size = new Size(610, 309);
        tabs.TabIndex = 0;
        // 
        // footer
        // 
        footer.AutoSize = true;
        footer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        footer.ColumnCount = 2;
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle());
        footer.Controls.Add(buttons, 1, 0);
        footer.Dock = DockStyle.Fill;
        footer.Location = new Point(15, 330);
        footer.Name = "footer";
        footer.RowCount = 1;
        footer.RowStyles.Add(new RowStyle());
        footer.Size = new Size(610, 75);
        footer.TabIndex = 1;
        // 
        // buttons
        // 
        buttons.AutoSize = true;
        buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttons.ColumnCount = 2;
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttons.Controls.Add(ok, 0, 0);
        buttons.Controls.Add(cancel, 1, 0);
        buttons.Location = new Point(394, 20);
        buttons.Margin = new Padding(0, 20, 0, 0);
        buttons.Name = "buttons";
        buttons.RowCount = 1;
        buttons.RowStyles.Add(new RowStyle());
        buttons.Size = new Size(216, 55);
        buttons.TabIndex = 0;
        // 
        // ok
        // 
        ok.AutoSize = true;
        ok.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ok.DialogResult = DialogResult.OK;
        ok.Dock = DockStyle.Fill;
        ok.Location = new Point(5, 5);
        ok.Margin = new Padding(5);
        ok.Name = "ok";
        ok.Padding = new Padding(15, 5, 10, 5);
        ok.Size = new Size(98, 45);
        ok.TabIndex = 0;
        ok.Text = "&OK";
        // 
        // cancel
        // 
        cancel.AutoSize = true;
        cancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancel.CausesValidation = false;
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Dock = DockStyle.Fill;
        cancel.Location = new Point(113, 5);
        cancel.Margin = new Padding(5);
        cancel.Name = "cancel";
        cancel.Padding = new Padding(15, 5, 10, 5);
        cancel.Size = new Size(98, 45);
        cancel.TabIndex = 1;
        cancel.Text = "&Cancel";
        // 
        // terminalPage
        // 
        terminalPage.AutoScroll = true;
        terminalPage.Location = new Point(0, 0);
        terminalPage.Name = "terminalPage";
        terminalPage.Size = new Size(600, 300);
        terminalPage.TabIndex = 0;
        // 
        // repositoryPage
        // 
        repositoryPage.AutoScroll = true;
        repositoryPage.Location = new Point(0, 0);
        repositoryPage.Name = "repositoryPage";
        repositoryPage.Size = new Size(600, 300);
        repositoryPage.TabIndex = 0;
        // 
        // OptionsDialog
        // 
        AcceptButton = ok;
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancel;
        ClientSize = new Size(640, 420);
        Controls.Add(root);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "OptionsDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Tools / Options";
        root.ResumeLayout(false);
        root.PerformLayout();
        footer.ResumeLayout(false);
        footer.PerformLayout();
        buttons.ResumeLayout(false);
        buttons.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel root = null!;
    private TableLayoutPanel footer = null!;
    private WarpToolkit.WinForms.Containers.FluentTabControl tabs = null!;
    private TerminalOptionsPage terminalPage = null!;
    private RepositoryOptionsPage repositoryPage = null!;
    private TableLayoutPanel buttons;
    private Button ok;
    private Button cancel;
}
