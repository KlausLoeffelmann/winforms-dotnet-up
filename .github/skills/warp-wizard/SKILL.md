---
name: warp-wizard
description: >-
  Builds WARP first-run/setup wizards, adds pages through the supported factory,
  validates or blocks navigation and awaits completion/cancellation with the
  actual page events, result enums and DPI-aware dialog overloads.
metadata:
  source: "WARP src\\WarpToolkit.WinForms\\Containers\\Wizard"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Wizard pages and navigation

## Critical constraints

- WizardPage constructor is internal; call AddWizardPage, never new WizardPage.
- Result values are **Finished/Cancelled/None**, not Completed.
- Validation/leaving handlers must set Cancel=true to block navigation.
- Await wizard/navigation Tasks; do not block the UI.
- Show first-run setup only when required by actual prerequisites and saved state;
  an already usable installation must not be forced through setup merely because
  this app has no completion record. Keep an explicit manual setup command.
- Page/layout/Designer rules belong to `winforms-high-dpi-fluent-layout` and
  `winforms-designer-code`. Existing internal fixed-button implementation is not
  a recipe to copy; do not invent a public layout knob to replace it.

## Container contract

Package WarpToolkit.WinForms; namespace WarpToolkit.WinForms.Containers.Wizard.
`WizardContainer : ContainerControl` internally creates its UI and observes Pages.

| Member | Contract |
|---|---|
| AddWizardPage / RemoveWizardPage | WizardPage (string title, Control content) / bool (WizardPage) |
| Pages | ObservableCollection&lt;WizardPage&gt; getter |
| CurrentPage / CurrentPageIndex | WizardPage? getter / int private setter |
| PageCount | int getter |
| ScalingMode / WizardPageTitleFont | WizardContainerScalingMode / Font get/set |
| ShowWizardDialogAsync | Task&lt;WizardDialogResult&gt; (IWin32Window? owner=null) |
| Size overload | (IWin32Window? owner, Size? dialogSize, Size? minimumSize=null) |
| GoBackAsync / GoNextAsync / GoToPageAsync(int) | Task&lt;bool&gt; |
| CanGoBack / CanGoNext | bool |

Container events: CurrentPageChanged, PageCountChanged, PageTitleChanged,
PageTitleFontChanged, WizardFinished, WizardCancelled, WizardScalingModeChanged.
Scaling enum Automatic=0/Normal=1/Large=2; result Finished=0/Cancelled=1/None=2;
navigation Forward=0/Backward=1.

## Page contract and usage

WizardPage exposes Title (string get/set), Content (Control init),
NeedsSkipping (bool), ParentContainer, PageIndex, IsFirstPage and IsLastPage.
Events: PageEntering/Entered, PageLeaving/Left and PageValidating.
PageEntering uses WizardPageEventArgs; leaving uses CancelEventArgs-derived
WizardPageLeavingEventArgs with Direction; validation uses
WizardPageValidatingEventArgs with ErrorMessage.

```csharp
WizardContainer wizard = new();
WizardPage welcome = wizard.AddWizardPage("Welcome", welcomeView);
WizardPage account = wizard.AddWizardPage("Account", accountView);
account.PageValidating += OnAccountValidating;
WizardDialogResult result = await wizard.ShowWizardDialogAsync(owner);
if (result == WizardDialogResult.Finished)
{
    CommitAcceptedSettings();
}
```

This is a runtime host-method fragment; views and the named validation handler are
application members. They are normally separately Designer-created UserControls.
Set `e.Cancel` and optionally `e.ErrorMessage` in validation; inspect Direction in
leaving when backward navigation should be treated differently. Use NeedsSkipping
for optional pages, and do not assume container-level PageValidating/Completed.

The dialog computes/scales its size with DeviceDpi/96; Automatic uses larger buttons
above 120 DPI. Default title font is a derived bold font; custom fonts need an owner.
Test actual shipped wizard layout at target DPI rather than claiming it automatically
matches newly authored canonical modal layouts.

Fixed-height native wizard buttons can clip modern renderer metrics or large
fonts. Increasing the Form size alone does not fix a fixed-height footer. If the
consuming package has no supported adaptive footer API, prefer a library fix;
otherwise an explicitly app-owned Designer-compatible wizard dialog may use the
canonical modal footer. Preserve page navigation/review/cancel semantics and the
Finish-only commit gate. Do not reflect into private fields, reparent internal
library buttons or invent an unverified public layout property.

## Final checks

1. Pages use the supported factory and separately designed content.
2. Validation/cancellation and skipped/backward pages behave intentionally.
3. Accepted state commits only on Finished; dialog/controls/fonts have explicit lifetime.
