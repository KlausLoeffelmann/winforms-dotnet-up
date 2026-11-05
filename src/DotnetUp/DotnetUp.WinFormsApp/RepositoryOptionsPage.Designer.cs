#nullable enable

namespace DotnetUp.WinFormsApp;

partial class RepositoryOptionsPage
{
    private void InitializeComponent()
    {
        fields = new TableLayoutPanel();
        Label depthLabel = new Label();
        depth = new NumericUpDown();
        Label note = new Label();
        ((System.ComponentModel.ISupportInitialize)depth).BeginInit();
        fields.SuspendLayout();
        SuspendLayout();
        fields.AutoSize = true;
        fields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        fields.Dock = DockStyle.Top;
        fields.Padding = new Padding(12);
        fields.ColumnCount = 2;
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        fields.RowCount = 2;
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        depthLabel.Text = "Maximum scan &depth:";
        depthLabel.AutoSize = true;
        depthLabel.Dock = DockStyle.Fill;
        depthLabel.TextAlign = ContentAlignment.MiddleLeft;
        depthLabel.Margin = new Padding(3, 8, 12, 8);
        depthLabel.TabIndex = 0;
        depth.Minimum = 0;
        depth.Maximum = 100;
        depth.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        depth.Margin = new Padding(3, 8, 3, 8);
        depth.AccessibleName = "Maximum repository scan depth";
        depth.AccessibleDescription = "0 means unlimited. 1 includes the scan root and its immediate subfolders.";
        depth.TabIndex = 1;
        note.Text = "0 = unlimited (the default).\r\n1 = scan the root and its immediate subfolders.\r\nLarger values include more nested folders, up to 100 levels.\r\n\r\nChanging depth resets discovery; expand a drive to scan again.\r\nBuild, dependency and linked folders are always skipped.";
        note.AutoSize = true;
        note.Dock = DockStyle.Fill;
        note.Margin = new Padding(3, 12, 3, 8);
        note.TabStop = false;
        fields.Controls.Add(depthLabel, 0, 0);
        fields.Controls.Add(depth, 1, 0);
        fields.Controls.Add(note, 0, 1);
        fields.SetColumnSpan(note, 2);
        Controls.Add(fields);
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Size = new Size(600, 300);
        ((System.ComponentModel.ISupportInitialize)depth).EndInit();
        fields.ResumeLayout(false);
        fields.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel fields = null!;
    private NumericUpDown depth = null!;
}
