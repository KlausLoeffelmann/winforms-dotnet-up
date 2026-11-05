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
        SetSymbol(refreshButton, FluentSymbols.AllSymbols.Refresh);
        SetSymbol(stopButton, FluentSymbols.AllSymbols.Stop);
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
    }
}
