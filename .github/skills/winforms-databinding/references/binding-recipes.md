# Binding recipes

## Contents

- [Notifying and validating item](#notifying-and-validating-item)
- [Static binding and save flow](#static-binding-and-save-flow)
- [Conversion and currency](#conversion-and-currency)
- [Designer datasource](#designer-datasource)
- [Grid performance](#grid-performance)

## Notifying and validating item

Complete reusable model. Validation feedback is separate from focus policy:

```csharp
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>Notifies name changes and provides binding-compatible validation.</summary>
public class Person : INotifyPropertyChanged, IDataErrorInfo
{
    private string _name = string.Empty;
    private int _age;

    /// <summary>Gets or sets the name.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets age.</summary>
    public int Age
    {
        get => _age;
        set
        {
            if (_age == value)
            {
                return;
            }

            _age = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Reports model errors.</summary>
    public string Error => this[nameof(Name)] + this[nameof(Age)];

    /// <summary>Reports an error for one property.</summary>
    public string this[string columnName] => columnName switch
    {
        nameof(Name) when string.IsNullOrWhiteSpace(Name) => "Name is required.",
        nameof(Age) when Age < 0 || Age > 120 => "Age must be between 0 and 120.",
        _ => string.Empty
    };

    /// <summary>Announces property changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raises a property notification.</summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

## Static binding and save flow

The following **declarative fragment**, not a complete Form, assumes existing fields/components and actual `Person`. Place statements in supported InitializeComponent order:

```csharp
components = new System.ComponentModel.Container();
personBindingSource = new System.Windows.Forms.BindingSource(components);
personBindingSource.DataSource = typeof(Person);
nameTextBox.DataBindings.Add(new System.Windows.Forms.Binding(
    "Text", personBindingSource, "Name", true,
    System.Windows.Forms.DataSourceUpdateMode.OnValidation));
errorProvider = new System.Windows.Forms.ErrorProvider(components);
errorProvider.ContainerControl = this;
errorProvider.DataSource = personBindingSource;
AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
```

Runtime sets actual `Person` or `BindingList<Person>`. Live-search uses OnPropertyChanged; calculated/read-only fields normally Never. Never still has initial/explicit-write caveats.

Save flow: call ValidateChildren under chosen focus policy; inspect BindingComplete conversion errors; write required focused bindings and EndEdit; verify Person.Error/domain rules; persist; then set Form.DialogResult. Don't set affirmative button DialogResult beforehand when save may fail. Cancel uses CausesValidation=false. ErrorProvider visuals do not themselves prevent invalid persistence.

## Conversion and currency

Format and Parse handlers belong in regular code, not lambda statements inside serialized UI. FormattingEnabled must be true. Format can display decimal currency/date, bool labels, enum display names and intentional rounding; Parse must use matching culture and report invalid input instead of silently retaining an unrelated value.

Master/detail: master BindingSource points at customers; detail BindingSource uses `DataSource=master`, `DataMember="Orders"` when that property exists. Bind each grid to its source. Detail then follows master currency; ensure order collections notify changes.

Combo/List: set DataSource, DisplayMember (display name), ValueMember (key). Bind SelectedValue to the actual model key; handle null/missing selection without an unchecked cast. Grid current-item selection comes from BindingSource.Current/CurrentChanged or CurrentRow.DataBoundItem; multiple selected rows require an explicit policy.

## Designer datasource

For supported VS object-data discovery, `Properties\DataSources\Person.datasource` contains:

```xml
<?xml version="1.0" encoding="utf-8"?>
<GenericObjectDataSource DisplayName="Person" Version="1.0"
    xmlns="urn:schemas-microsoft-com:xml-msdatasource">
  <TypeInfo>Example.Models.Person, Example.Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null</TypeInfo>
</GenericObjectDataSource>
```

Replace type/assembly/version/token with the built consuming assembly's actual metadata. Rebuild before discovery and reload supported VS tooling if needed. This XML does not give VS Code a WinForms Designer or restore unsupported modern typed-DataSet tooling.

## Grid performance

Explicit columns use AutoGenerateColumns=false and actual DataPropertyName. Use environment-aware Fill/AutoSize sizing rather than fixed 200/250-pixel examples. Evaluate paging/virtual mode using measured load and editing requirements.

Virtual mode requires total RowCount, CellValueNeeded backed by an indexed cache, CellValuePushed when editable, and correct add/delete/error/commit behavior. Bound and unbound virtual columns differ; don't flip VirtualMode on and assume a complete data layer. Avoid async I/O per paint-cell request; prefetch/cache outside paint and invalidate when available.

Bulk BindingList changes can set RaiseListChangedEvents=false, restore in finally and issue one ResetBindings, with selection/currency preservation where required. BindingSource.RaiseListChangedEvents suppression likewise needs final ResetBindings. SuspendBinding alone is not a universal complex-grid notification suppression technique.
