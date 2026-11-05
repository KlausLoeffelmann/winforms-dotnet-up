#nullable enable

namespace DotnetUp.WinFormsApp;

partial class TerminalOptionsPage
{
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _selectedUiFont?.Dispose();
            _selectedConsoleFont?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        fields = new TableLayoutPanel();
        Label shellLabel = new Label();
        shellChoice = new ComboBox();
        Label uiFontLabel = new Label();
        TableLayoutPanel uiPicker = new TableLayoutPanel();
        uiFontValue = new TextBox();
        Button uiBrowse = new Button();
        Label consoleFontLabel = new Label();
        TableLayoutPanel consolePicker = new TableLayoutPanel();
        consoleFontValue = new TextBox();
        Button consoleBrowse = new Button();
        Label note = new Label();
        fields.SuspendLayout();
        uiPicker.SuspendLayout();
        consolePicker.SuspendLayout();
        SuspendLayout();
        fields.AutoSize = true;
        fields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        fields.Dock = DockStyle.Top;
        fields.Padding = new Padding(12);
        fields.ColumnCount = 2;
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        fields.RowCount = 4;
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shellLabel.Text = "Hosted &shell:";
        shellLabel.AutoSize = true;
        shellLabel.Dock = DockStyle.Fill;
        shellLabel.TextAlign = ContentAlignment.MiddleLeft;
        shellLabel.Margin = new Padding(3, 8, 12, 8);
        shellLabel.TabIndex = 0;
        shellChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        shellChoice.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        shellChoice.Items.AddRange(new object[] { "PowerShell", "Command Prompt" });
        shellChoice.Margin = new Padding(3, 8, 3, 8);
        shellChoice.AccessibleName = "Hosted shell";
        shellChoice.TabIndex = 1;
        uiFontLabel.Text = "&Interface font:";
        uiFontLabel.AutoSize = true;
        uiFontLabel.Dock = DockStyle.Fill;
        uiFontLabel.TextAlign = ContentAlignment.MiddleLeft;
        uiFontLabel.Margin = new Padding(3, 8, 12, 8);
        uiFontLabel.TabIndex = 2;
        uiPicker.AutoSize = true;
        uiPicker.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiPicker.Dock = DockStyle.Fill;
        uiPicker.ColumnCount = 2;
        uiPicker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiPicker.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiPicker.RowCount = 1;
        uiPicker.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiPicker.Margin = new Padding(0, 8, 0, 8);
        uiPicker.TabIndex = 3;
        uiFontValue.ReadOnly = true;
        uiFontValue.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        uiFontValue.AccessibleName = "Interface font";
        uiFontValue.TabIndex = 0;
        uiBrowse.Text = "...";
        uiBrowse.AutoSize = true;
        uiBrowse.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiBrowse.Dock = DockStyle.Fill;
        uiBrowse.Padding = new Padding(10, 5, 10, 5);
        uiBrowse.AccessibleName = "Choose interface font";
        uiBrowse.TabIndex = 1;
        uiBrowse.Click += UiFont_Click;
        consoleFontLabel.Text = "Console &font:";
        consoleFontLabel.AutoSize = true;
        consoleFontLabel.Dock = DockStyle.Fill;
        consoleFontLabel.TextAlign = ContentAlignment.MiddleLeft;
        consoleFontLabel.Margin = new Padding(3, 8, 12, 8);
        consoleFontLabel.TabIndex = 4;
        consolePicker.AutoSize = true;
        consolePicker.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        consolePicker.Dock = DockStyle.Fill;
        consolePicker.ColumnCount = 2;
        consolePicker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        consolePicker.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        consolePicker.RowCount = 1;
        consolePicker.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        consolePicker.Margin = new Padding(0, 8, 0, 8);
        consolePicker.TabIndex = 5;
        consoleFontValue.ReadOnly = true;
        consoleFontValue.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        consoleFontValue.AccessibleName = "Console font";
        consoleFontValue.TabIndex = 0;
        consoleBrowse.Text = "...";
        consoleBrowse.AutoSize = true;
        consoleBrowse.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        consoleBrowse.Dock = DockStyle.Fill;
        consoleBrowse.Padding = new Padding(10, 5, 10, 5);
        consoleBrowse.AccessibleName = "Choose console font";
        consoleBrowse.TabIndex = 1;
        consoleBrowse.Click += ConsoleFont_Click;
        note.Text = "Changing the shell restarts the hosted terminal.\r\nFont changes apply when you choose OK.";
        note.AutoSize = true;
        note.Dock = DockStyle.Fill;
        note.Margin = new Padding(3, 12, 3, 8);
        note.TabStop = false;
        uiPicker.Controls.Add(uiFontValue, 0, 0);
        uiPicker.Controls.Add(uiBrowse, 1, 0);
        consolePicker.Controls.Add(consoleFontValue, 0, 0);
        consolePicker.Controls.Add(consoleBrowse, 1, 0);
        fields.Controls.Add(shellLabel, 0, 0);
        fields.Controls.Add(shellChoice, 1, 0);
        fields.Controls.Add(uiFontLabel, 0, 1);
        fields.Controls.Add(uiPicker, 1, 1);
        fields.Controls.Add(consoleFontLabel, 0, 2);
        fields.Controls.Add(consolePicker, 1, 2);
        fields.Controls.Add(note, 0, 3);
        fields.SetColumnSpan(note, 2);
        Controls.Add(fields);
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Size = new Size(600, 300);
        consolePicker.ResumeLayout(false);
        consolePicker.PerformLayout();
        uiPicker.ResumeLayout(false);
        uiPicker.PerformLayout();
        fields.ResumeLayout(false);
        fields.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel fields = null!;
    private ComboBox shellChoice = null!;
    private TextBox uiFontValue = null!;
    private TextBox consoleFontValue = null!;
}
