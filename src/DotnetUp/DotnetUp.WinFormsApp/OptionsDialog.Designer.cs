#nullable enable

namespace DotnetUp.WinFormsApp;

partial class OptionsDialog
{
    private void InitializeComponent()
    {
        root = new TableLayoutPanel();
        tabs = new WarpToolkit.WinForms.Containers.FluentTabControl();
        terminalPage = new TerminalOptionsPage();
        repositoryPage = new RepositoryOptionsPage();
        footer = new TableLayoutPanel();
        TableLayoutPanel buttons = new TableLayoutPanel();
        Button ok = new Button();
        Button cancel = new Button();
        root.SuspendLayout();
        footer.SuspendLayout();
        buttons.SuspendLayout();
        SuspendLayout();
        root.AutoSize = false;
        root.Dock = DockStyle.Fill;
        root.Padding = new Padding(12);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowCount = 2;
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabs.Dock = DockStyle.Fill;
        tabs.AccessibleName = "Options categories";
        footer.AutoSize = true;
        footer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        footer.Dock = DockStyle.Fill;
        footer.ColumnCount = 2;
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.RowCount = 1;
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        buttons.AutoSize = true;
        buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttons.Margin = new Padding(0, 20, 0, 0);
        buttons.ColumnCount = 2;
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttons.RowCount = 1;
        buttons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        ok.Text = "&OK";
        ok.AutoSize = true;
        ok.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ok.Dock = DockStyle.Fill;
        ok.Padding = new Padding(15, 5, 10, 5);
        ok.Margin = new Padding(5);
        ok.DialogResult = DialogResult.OK;
        cancel.Text = "&Cancel";
        cancel.AutoSize = true;
        cancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancel.Dock = DockStyle.Fill;
        cancel.Padding = new Padding(15, 5, 10, 5);
        cancel.Margin = new Padding(5);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.CausesValidation = false;
        buttons.Controls.Add(ok, 0, 0);
        buttons.Controls.Add(cancel, 1, 0);
        footer.Controls.Add(buttons, 1, 0);
        root.Controls.Add(tabs, 0, 0);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Tools / Options";
        MinimizeBox = false;
        MaximizeBox = false;
        AcceptButton = ok;
        CancelButton = cancel;
        buttons.ResumeLayout(false);
        buttons.PerformLayout();
        footer.ResumeLayout(false);
        footer.PerformLayout();
        root.ResumeLayout(false);
        root.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel root = null!;
    private TableLayoutPanel footer = null!;
    private WarpToolkit.WinForms.Containers.FluentTabControl tabs = null!;
    private TerminalOptionsPage terminalPage = null!;
    private RepositoryOptionsPage repositoryPage = null!;
}
