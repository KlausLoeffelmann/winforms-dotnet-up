namespace DotnetUp.WinFormsApp;

internal sealed partial class WorkspaceEditor : Form
{
    private readonly string _path;
    private byte[]? _original;
    private bool _busy;
    private bool _allowClose;

    public WorkspaceEditor(string path)
    {
        _path = path;
        InitializeComponent();
        Text = $"{Path.GetFileName(path)} - Internal editor";
    }

    private void Close_Click(object? sender, EventArgs e) => Close();

    public async Task LoadFileAsync()
    {
        _busy = true;
        editor.ReadOnly = true;
        saveItem.Enabled = false;
        try
        {
            byte[] content = await File.ReadAllBytesAsync(_path);
            using MemoryStream stream = new(content, writable: false);
            await editor.LoadDocumentAsync(stream);
            _original = content;
            editor.AutoFormatJson = false;
            editor.DocumentNewLineConvention = WarpToolkit.WinForms.DirectX.TextEditing.Persistence.TextEditorNewLineConvention.None;
            editor.ReadOnly = false;
            saveItem.Enabled = true;
            editorStatus.Text = _path;
        }
        finally
        {
            _busy = false;
        }
    }

    public async Task SaveFileAsync()
    {
        if (_busy || _original is null)
        {
            throw new InvalidOperationException("The document is not ready to save.");
        }

        _busy = true;
        editor.ReadOnly = true;
        saveItem.Enabled = false;
        try
        {
            editor.TextBoxElement.FlushPendingTyping();
            if (!(await File.ReadAllBytesAsync(_path)).SequenceEqual(_original))
            {
                throw new IOException("The file changed outside the editor. Your edits have not overwritten it. Copy them elsewhere and reopen the file.");
            }

            await editor.SaveDocumentAsync(_path);
            _original = await File.ReadAllBytesAsync(_path);
            editorStatus.Text = $"Saved {_path}";
        }
        finally
        {
            _busy = false;
            editor.ReadOnly = false;
            saveItem.Enabled = true;
        }
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            await LoadFileAsync();
        }
        catch (Exception ex)
        {
            editorStatus.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "Unable to load document", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void Save_Click(object? sender, EventArgs e)
    {
        try
        {
            await SaveFileAsync();
        }
        catch (Exception ex)
        {
            editorStatus.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "Unable to save document", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (_busy)
        {
            e.Cancel = true;
            editorStatus.Text = "Wait for the current load/save operation.";
            return;
        }

        editor.TextBoxElement.FlushPendingTyping();
        if (!_allowClose && editor.IsDocumentModified)
        {
            e.Cancel = true;
            DialogResult answer = MessageBox.Show(this, "Save changes before closing?", "Internal editor",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel)
            {
                return;
            }

            try
            {
                if (answer == DialogResult.Yes)
                {
                    await SaveFileAsync();
                }

                _allowClose = true;
                BeginInvoke(Close);
            }
            catch (Exception ex)
            {
                editorStatus.Text = ex.Message;
                MessageBox.Show(this, ex.Message, "Unable to save document", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return;
        }

        base.OnFormClosing(e);
    }
}
