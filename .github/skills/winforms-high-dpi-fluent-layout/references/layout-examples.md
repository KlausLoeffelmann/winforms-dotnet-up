# Declarative adaptive layouts

## Contents

- [Standard modal footer](#standard-modal-footer)
- [Logical groups](#logical-groups)
- [Resizable viewport](#resizable-viewport)
- [Content-sized alternative](#content-sized-alternative)
- [Runtime minima](#runtime-minima)

## Standard modal footer

Complete .NET 10 example. Split the `InitializeComponent` method and fields into the Designer partial in an application; keep constructor/runtime logic in the main partial. The root is profile B, **not AutoSize**. Equal widths can round by one pixel without caps.

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;

#nullable disable
/// <summary>A customer-name dialog with a constrained root and adaptive footer.</summary>
public partial class CustomerDialog : Form
{
    private TableLayoutPanel _root;
    private TableLayoutPanel _outer;
    private TableLayoutPanel _buttons;
    private Label _caption;
    private TextBox _editor;
    private Button _save;
    private Button _cancel;

    /// <summary>Initializes the static dialog.</summary>
    public CustomerDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _root = new TableLayoutPanel();
        _outer = new TableLayoutPanel();
        _buttons = new TableLayoutPanel();
        _caption = new Label();
        _editor = new TextBox();
        _save = new Button();
        _cancel = new Button();
        SuspendLayout();
        _root.SuspendLayout();
        _outer.SuspendLayout();
        _buttons.SuspendLayout();

        _root.AutoSize = false;
        _root.Dock = DockStyle.Fill;
        _root.ColumnCount = 2;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _root.RowCount = 3;
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.Controls.Add(_caption, 0, 0);
        _root.Controls.Add(_editor, 1, 0);
        _root.Controls.Add(_outer, 0, 2);
        _root.SetColumnSpan(_outer, 2);

        _caption.AutoSize = true;
        _caption.Dock = DockStyle.Fill;
        _caption.TextAlign = ContentAlignment.MiddleLeft;
        _caption.Text = "Customer\nname:";
        _caption.TabIndex = 0;
        _editor.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _editor.AccessibleName = "Customer name";
        _editor.AccessibleDescription = "Enter the customer's full name";
        _editor.TabIndex = 1;

        _outer.AutoSize = true;
        _outer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _outer.Dock = DockStyle.Fill;
        _outer.ColumnCount = 2;
        _outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _outer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _outer.RowCount = 1;
        _outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _outer.Controls.Add(_buttons, 1, 0);

        _buttons.AutoSize = true;
        _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttons.Margin = new Padding(0, 20, 0, 0);
        _buttons.ColumnCount = 2;
        _buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _buttons.RowCount = 1;
        _buttons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _buttons.Controls.Add(_save, 0, 0);
        _buttons.Controls.Add(_cancel, 1, 0);

        _save.AutoSize = true;
        _save.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _save.Dock = DockStyle.Fill;
        _save.Padding = new Padding(15, 5, 10, 5);
        _save.Margin = new Padding(5, 5, 5, 5);
        _save.Text = "Save customer preferences\nand apply changes";
        _save.DialogResult = DialogResult.OK;
        _save.UseVisualStyleBackColor = true;
        _save.TabIndex = 0;
        _cancel.AutoSize = true;
        _cancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _cancel.Dock = DockStyle.Fill;
        _cancel.Padding = new Padding(15, 5, 10, 5);
        _cancel.Margin = new Padding(5, 5, 5, 5);
        _cancel.Text = "Cancel";
        _cancel.DialogResult = DialogResult.Cancel;
        _cancel.CausesValidation = false;
        _cancel.UseVisualStyleBackColor = true;
        _cancel.TabIndex = 1;

        AutoScaleMode = AutoScaleMode.Font;
        AcceptButton = _save;
        CancelButton = _cancel;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Customer";
        Controls.Add(_root);
        _buttons.ResumeLayout(false);
        _buttons.PerformLayout();
        _outer.ResumeLayout(false);
        _outer.PerformLayout();
        _root.ResumeLayout(false);
        _root.PerformLayout();
        ResumeLayout(false);
    }
}
#nullable restore
```

The positive result is appropriate only for this simple unvalidated example. A validation/persistence feature must unset button DialogResult and close the Form after successful save.

## Logical groups

Profile A/B: root group columns Percent100, 50/50, or equal thirds; group rows AutoSize. Place each GroupBox in its own cell, `AutoSize/GrowAndShrink/Dock Fill`; its nested table uses the same settings.

Inside a group, caption columns AutoSize and editor columns Percent—not all columns AutoSize. AutoSize content rows and horizontal-only editor anchoring preserve native single-line height. Optional final empty Percent spring lets groups in the same row absorb unequal content heights. The root's spring is separate.

Radio cluster: one GroupBox contains a single-column nested table; each option has its own AutoSize row and appropriate tab/mnemonic behavior. This retains mutual exclusion without a button FlowLayoutPanel.

## Resizable viewport

Profile C: root Dock Fill/AutoSize false, one Percent-width content column. Toolbar/status/prompt rows AutoSize; multiline editor, ListBox, TreeView or grid in dedicated occupied Percent row Dock Fill. A SplitContainer can occupy that row when user-adjustable pane proportions are required.

Do not use viewport preferred height as the sole minimum. Choose usable visible line/item counts for the task, measure with the current font, account for borders/scrollbars/headers and allow resizing. Field-only side panels retain their empty springs; they are not additional expanding viewports.

## Content-sized alternative

Profile D: whole Form and nested content containers AutoSize/GrowAndShrink with AutoSize real rows and no springs. Container-to-cell docking and honest preferred sizes keep the chain continuous. Root can be arranged to content-size rather than a constrained pinned footer. When standard bottom actions are required, switch to B; don't publish an AutoSize=true version of its root.

## Runtime minima

Designer starting Size/ClientSize is not a universal runtime minimum. In regular code, derive an initial desired size from measured content and a task-appropriate editor width/viewport line count. Convert client requirements to outer `MinimumSize` using `SizeFromClientSize`; constrain placement to the current working area.

If preferred content exceeds the working area, avoid setting an unreachable MinimumSize. Use an explicitly scrolling content host or responsive grouping, preserving reachable actions. Reevaluate after font/DPI/monitor changes, and test the actual host: `AutoScroll=true` alone is not proof that every docked/anchored child is reachable.
