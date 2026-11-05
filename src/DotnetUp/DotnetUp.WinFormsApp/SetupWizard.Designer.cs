#nullable enable

namespace DotnetUp.WinFormsApp;

partial class SetupWizard
{
    private void InitializeComponent()
    {
        TableLayoutPanel root = new TableLayoutPanel();
        titleLabel = new Label();
        pageHost = new Panel();
        TableLayoutPanel footer = new TableLayoutPanel();
        TableLayoutPanel buttons = new TableLayoutPanel();
        backButton = new Button();
        nextButton = new Button();
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
        root.RowCount = 3;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleLabel.AutoSize = true;
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Margin = new Padding(3, 3, 3, 12);
        titleLabel.TabStop = false;
        pageHost.Dock = DockStyle.Fill;
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
        buttons.ColumnCount = 3;
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        buttons.RowCount = 1;
        buttons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        backButton.Text = "&Back";
        backButton.AutoSize = true;
        backButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        backButton.Dock = DockStyle.Fill;
        backButton.Padding = new Padding(15, 5, 10, 5);
        backButton.Margin = new Padding(5);
        backButton.AccessibleName = "Previous setup page";
        backButton.Click += Back_Click;
        nextButton.Text = "&Next";
        nextButton.AutoSize = true;
        nextButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        nextButton.Dock = DockStyle.Fill;
        nextButton.Padding = new Padding(15, 5, 10, 5);
        nextButton.Margin = new Padding(5);
        cancel.Text = "&Cancel";
        cancel.AutoSize = true;
        cancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancel.Dock = DockStyle.Fill;
        cancel.Padding = new Padding(15, 5, 10, 5);
        cancel.Margin = new Padding(5);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.CausesValidation = false;
        buttons.Controls.Add(backButton, 0, 0);
        buttons.Controls.Add(nextButton, 1, 0);
        buttons.Controls.Add(cancel, 2, 0);
        footer.Controls.Add(buttons, 1, 0);
        root.Controls.Add(titleLabel, 0, 0);
        root.Controls.Add(pageHost, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(850, 620);
        MinimumSize = new Size(700, 500);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Set up dotnetup";
        MinimizeBox = false;
        MaximizeBox = false;
        AcceptButton = nextButton;
        CancelButton = cancel;
        buttons.ResumeLayout(false);
        buttons.PerformLayout();
        footer.ResumeLayout(false);
        footer.PerformLayout();
        root.ResumeLayout(false);
        root.PerformLayout();
        ResumeLayout(false);
    }

    private Label titleLabel = null!;
    private Panel pageHost = null!;
    private Button backButton = null!;
    private Button nextButton = null!;
}
