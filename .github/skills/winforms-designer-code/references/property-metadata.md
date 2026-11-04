# Property metadata and Designer extensions

## Independent policies

Category/Description control consumer help; Browsable controls Properties visibility; EditorBrowsable controls IntelliSense; DesignerSerializationVisibility controls serialized content. They are not interchangeable.

Use Hidden for runtime-only or derived properties, Content for supported nested mutable content, and DefaultValue or Reset/ShouldSerialize for default suppression. Compatible independent attributes can combine; competing default policies should not. Hidden inherited members often need Browsable(false), EditorBrowsable(Never) and Hidden together.

Dynamic palette example below uses Color.Empty as an “ambient/default” sentinel, so a reset remains genuinely dynamic instead of capturing yesterday's theme. Palette policy itself is defined by rendering.

```csharp
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

/// <summary>Exposes a resettable ambient face-color property.</summary>
public class FacePanel : Panel
{
    private Color _faceColor = Color.Empty;

    /// <summary>Gets or sets a face color; resetting follows the active system role.</summary>
    [Category("Appearance")]
    [Description("Face color; reset to follow the current system control color.")]
    public Color FaceColor
    {
        get => _faceColor.IsEmpty ? SystemColors.Control : _faceColor;
        set
        {
            _faceColor = value;
            Invalidate();
        }
    }

    private bool ShouldSerializeFaceColor() => !_faceColor.IsEmpty;

    private void ResetFaceColor()
    {
        _faceColor = Color.Empty;
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnSystemColorsChanged(System.EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        Invalidate();
    }
}
```

ShouldSerialize/Reset are discovered by naming convention; use one default policy and test TypeDescriptor/Designer behavior for actual tooling. The example demonstrates serialization state, not a promise Panel automatically paints FaceColor.

A constant DefaultValue attribute does **not** initialize the property; initialize the backing storage consistently. Hiding a property from the grid without Hidden may still serialize it.

## Modern extension architecture

Basic attributes/converters do not require the entire Designer SDK. For actual modern Designer extension code, choose a compatible current `Microsoft.WinForms.Designer.SDK` version supported by the project/tooling; don't mandate a years-old preview.

Supported server-side serializers, type converters and noninteractive smart-tag actions can reside with control code where tooling supports it. Custom design-time UI needs the out-of-process architecture: VS client (.NET Framework side as required by supported tooling), .NET server/control designer, and shared communication layer, packaged/discovered correctly.

Do not assume one control assembly can host arbitrary interactive Designer UI. Escalate to that architecture when required; preserve client/server boundary, supported target versions, discovery and roundtrip testing.

Source: [WinForms Designer extensibility](https://github.com/microsoft/winforms-designer-extensibility).
