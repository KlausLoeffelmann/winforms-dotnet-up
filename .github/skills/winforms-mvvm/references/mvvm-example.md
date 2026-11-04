# Coherent Toolkit and context example

## Contents

- [ViewModel](#viewmodel)
- [View context](#view-context)
- [Command binding](#command-binding)
- [Tests and lifecycle](#tests-and-lifecycle)

## ViewModel

Compile with a compatible pinned CommunityToolkit.Mvvm package (validated with 8.4.0). Generator annotations belong in the project compiling this partial type:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>Persists screen state without UI dependencies.</summary>
public interface IPersonService
{
    /// <summary>Saves a person.</summary>
    Task SaveAsync(string first, string last, int age, CancellationToken token);

    /// <summary>Processes a named action.</summary>
    Task ProcessAsync(string action, CancellationToken token);
}

/// <summary>Exposes notifying entry state and cancellable service commands.</summary>
public partial class PersonViewModel : ObservableObject
{
    private readonly IPersonService _service;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _firstName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _lastName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int _age;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Initializes injected persistence.</summary>
    public PersonViewModel(IPersonService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    /// <summary>Gets a dependent display value.</summary>
    public string FullName => $"{FirstName} {LastName}";

    private bool CanSave() => !string.IsNullOrWhiteSpace(FirstName) && Age >= 18;

    [RelayCommand(CanExecute = nameof(CanSave), IncludeCancelCommand = true)]
    private async Task SaveAsync(CancellationToken token)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await _service.SaveAsync(FirstName, LastName, Age, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task ProcessAsync(string action, CancellationToken token)
        => _service.ProcessAsync(action, token);
}
```

Save converts expected cancellation and service errors into explicit state; Process intentionally propagates failures for an observing caller. Choose one application error policy rather than assuming every async command exception is harmless.

## View context

This Control-derived host illustrates **runtime** context/subscription behavior without redefining a Form layout. Static composed UI in an application still belongs in Designer partials.

```csharp
using System;
using System.ComponentModel;
using System.Windows.Forms;

/// <summary>Rebinds inherited context and detaches old ViewModel subscriptions.</summary>
public class VmContextHost : Control
{
    private readonly BindingSource _source = new();
    private PersonViewModel? _viewModel;

    /// <summary>Gets the binding source for composed view controls.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BindingSource Source => _source;

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_source is null)
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= StateChanged;
        }

        _viewModel = DataContext as PersonViewModel;
        _source.DataSource = _viewModel is null ? typeof(PersonViewModel) : _viewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += StateChanged;
        }
    }

    private void StateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PersonViewModel.IsLoading))
        {
            Cursor = _viewModel?.IsLoading == true ? Cursors.WaitCursor : Cursors.Default;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= StateChanged;
            }

            _source.Dispose();
        }

        base.Dispose(disposing);
    }
}
```

Construct/rebind on UI; ViewModel notifications must remain UI-affine. Bind controls once, then replace the source, instead of clearing/readding on every context change. A no-context view should disable actions appropriately.

## Command binding

Declarative Designer fragment assuming actual fields and ViewModel type:

```csharp
viewModelBindingSource.DataSource = typeof(PersonViewModel);
saveButton.DataBindings.Add(new System.Windows.Forms.Binding(
    "Command", viewModelBindingSource, "SaveCommand", true));
approveButton.DataBindings.Add(new System.Windows.Forms.Binding(
    "Command", viewModelBindingSource, "ProcessCommand", true));
approveButton.CommandParameter = "Approve";
```

Regular code assigns actual instance after InitializeComponent. ToolStripItem supports analogous Command binding on modern targets. Property bindings use actual generated names; save must still perform the binding owner's required focused-field writeback/validation before persisting.

## Tests and lifecycle

Use a small fake IPersonService:
- Changes to FirstName/LastName notify FullName; age 17 versus 18 changes CanExecute.
- SaveCommand.ExecuteAsync forwards names/age/token once and always resets IsLoading.
- A fault exposes ErrorMessage; requested cancellation is not shown as a database error.
- Command parameter reaches ProcessAsync and its failures are observed.
- Replaced/null DataContext detaches prior view subscriptions.

Long async load flows use the async owner's lifecycle pattern; do not reference an undefined LoadCommand. Grid selection/collection adapters follow binding rules, with source-command structural changes and explicit disposal.
