using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace DotnetUp.WinFormsApp;

public partial class MainForm
{
    private readonly List<Font> _toolbarFonts = [];

    private void InitializeToolbarSymbols()
    {
        SetSymbol(installButton, FluentSymbols.AllSymbols.Add);
        SetSymbol(updateButton, FluentSymbols.AllSymbols.Download);
        SetSymbol(updateSelectedButton, FluentSymbols.AllSymbols.Download);
        SetSymbol(refreshButton, FluentSymbols.AllSymbols.Refresh);
        SetSymbol(stopButton, FluentSymbols.AllSymbols.Stop);
        SetSymbol(workspaceInstallButton, FluentSymbols.AllSymbols.Add);
        SetSymbol(websiteButton, FluentSymbols.AllSymbols.Globe);
        SetSymbol(folderButton, FluentSymbols.AllSymbols.FolderOpen);
        SetSymbol(editorButton, FluentSymbols.AllSymbols.Edit);
        SetSymbol(codeButton, FluentSymbols.AllSymbols.Code);
        visualStudioButton.Image = RenderGlyph(FluentSymbols.AllSymbols.Project, Color.MediumPurple);
        foreach (ToolStripItem item in new ToolStripItem[] { workspaceInstallButton, websiteButton, folderButton,
            editorButton, visualStudioButton, codeButton })
        {
            item.DisplayStyle = ToolStripItemDisplayStyle.Image;
            item.AccessibleName = item.Text;
            item.ToolTipText = item.Text;
        }
    }

    private void SetSymbol(SymbolToolStripButton item, Enum glyph)
    {
        FontSymbol symbol = FontSymbol.FromFluentSymbol(glyph, toolbar.ImageScalingSize.Width);
        _toolbarFonts.Add(symbol.SymbolFont
            ?? throw new InvalidOperationException("The toolbar symbol did not provide its source font."));
        item.Symbol = symbol;
    }

    private void DisposeToolbarFonts()
    {
        foreach (Font font in _toolbarFonts)
        {
            font.Dispose();
        }

        _toolbarFonts.Clear();
        visualStudioButton.Image?.Dispose();
    }
}
