namespace DotnetUp.WinFormsApp;

public partial class MainForm
{
    private IReadOnlyList<DetailRow> SelectedSdkRows => details.SelectedRows.Cast<DataGridViewRow>()
        .Select(row => row.DataBoundItem).OfType<DetailRow>().ToArray();

    private void UpdateCommandState()
    {
        bool ready = _terminal is { IsBusy: false } && !_workflowRunning && !_closing;
        IReadOnlyList<DetailRow> selection = SelectedSdkRows;
        bool canUpdate = ready && selection.Count > 0 && selection.All(SelectedSdkUpdates.CanUpdate);
        updateSelectedItem.Enabled = canUpdate;
        contextUpdateSelected.Enabled = canUpdate;
        updateSelectedButton.Enabled = canUpdate;
    }

    private async void UpdateSelected_Click(object? sender, EventArgs e)
    {
        DetailRow[] selection = SelectedSdkRows.ToArray();
        await RunWorkflowAsync(async () =>
        {
            IReadOnlyList<ShellCommand> commands = SelectedSdkUpdates.Plan(RequireDotnetUp(), selection);
            string preview = string.Join(Environment.NewLine, commands.Select(command => command.Render(_preferences.Shell)));
            if (MessageBox.Show(this,
                "Only the selected SDK requirements will be installed/advanced.\r\n"
                + "Installed exact versions target the latest SDK in the same major/minor line.\r\n"
                + "Existing pins and versions are retained. External SDKs are never modified.\r\n\r\n"
                + preview, "Update selected SDKs to latest", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
                await ExecutePlanAsync(commands);
            }
        });
    }
}
