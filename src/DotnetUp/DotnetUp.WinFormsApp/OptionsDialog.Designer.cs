#nullable enable

namespace DotnetUp.WinFormsApp;

partial class OptionsDialog
{
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _selectedUiFont.Dispose();
            _selectedConsoleFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        TableLayoutPanel root = new TableLayoutPanel();
        Label shellLabel = new Label();
        shellChoice = new ComboBox();
        uiFontButton = new Button();
        consoleFontButton = new Button();
        Label note = new Label();
        TableLayoutPanel footer = new TableLayoutPanel();
        TableLayoutPanel buttons = new TableLayoutPanel();
        Button ok = new Button();
        Button cancel = new Button();
        SuspendLayout();
        root.Dock = DockStyle.Fill;
        root.AutoScroll = true;
        root.Padding = new Padding(12);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowCount = 6;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shellLabel.Text = "Hosted &shell:";
        shellLabel.AutoSize = true;
        shellChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        shellChoice.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        shellChoice.Items.AddRange(new object[] { "PowerShell", "Command Prompt" });
        uiFontButton.AutoSize = true;
        uiFontButton.Dock = DockStyle.Fill;
        uiFontButton.Click += UiFont_Click;
        consoleFontButton.AutoSize = true;
        consoleFontButton.Dock = DockStyle.Fill;
        consoleFontButton.Click += ConsoleFont_Click;
        note.Text = "Changing the shell restarts the hosted terminal.\r\nFont changes apply when you choose OK.";
        note.AutoSize = true;
        note.Dock = DockStyle.Fill;
        footer.AutoSize = true;
        footer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        footer.Dock = DockStyle.Fill;
        footer.ColumnCount = 2;
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        buttons.AutoSize = true;
        buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttons.Margin = new Padding(0, 20, 0, 0);
        buttons.ColumnCount = 2;
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        ok.Text = "OK";
        ok.AutoSize = true;
        ok.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ok.Dock = DockStyle.Fill;
        ok.Padding = new Padding(15, 5, 10, 5);
        ok.Margin = new Padding(5);
        ok.DialogResult = DialogResult.OK;
        cancel.Text = "Cancel";
        cancel.AutoSize = true;
        cancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancel.Dock = DockStyle.Fill;
        cancel.Padding = new Padding(15, 5, 10, 5);
        cancel.Margin = new Padding(5);
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(ok, 0, 0);
        buttons.Controls.Add(cancel, 1, 0);
        footer.Controls.Add(buttons, 1, 0);
        root.Controls.Add(shellLabel, 0, 0);
        root.Controls.Add(shellChoice, 0, 1);
        root.Controls.Add(uiFontButton, 0, 2);
        root.Controls.Add(consoleFontButton, 0, 3);
        root.Controls.Add(note, 0, 4);
        root.Controls.Add(footer, 0, 5);
        Controls.Add(root);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(540, 330);
        MinimumSize = new Size(440, 350);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Tools / Options";
        MinimizeBox = false;
        MaximizeBox = false;
        AcceptButton = ok;
        CancelButton = cancel;
        ResumeLayout(false);
    }

    private ComboBox shellChoice = null!;
    private Button uiFontButton = null!;
    private Button consoleFontButton = null!;
}
