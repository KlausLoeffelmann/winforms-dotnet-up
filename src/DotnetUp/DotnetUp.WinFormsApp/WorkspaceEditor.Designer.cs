#nullable enable

namespace DotnetUp.WinFormsApp;

partial class WorkspaceEditor
{
    private void InitializeComponent()
    {
        MenuStrip menu = new();
        ToolStripMenuItem file = new("&File");
        saveItem = new("&Save") { ShortcutKeys = Keys.Control | Keys.S, Enabled = false };
        ToolStripMenuItem close = new("&Close");
        editor = new() { Dock = DockStyle.Fill, AutoFormatJson = false, ReadOnly = true, ShowLineNumbers = true };
        StatusStrip status = new();
        editorStatus = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft, Text = "Loading..." };
        saveItem.Click += Save_Click;
        close.Click += Close_Click;
        file.DropDownItems.AddRange(new ToolStripItem[] { saveItem, close });
        menu.Items.Add(file);
        status.Items.Add(editorStatus);
        Controls.Add(editor);
        Controls.Add(menu);
        Controls.Add(status);
        MainMenuStrip = menu;
        AutoScaleMode = AutoScaleMode.Font;
        Size = new Size(950, 700);
        MinimumSize = new Size(650, 400);
        StartPosition = FormStartPosition.CenterParent;
    }

    private WarpToolkit.WinForms.DirectX.Controls.D2DTextEditor editor = null!;
    private ToolStripMenuItem saveItem = null!;
    private ToolStripStatusLabel editorStatus = null!;
}
