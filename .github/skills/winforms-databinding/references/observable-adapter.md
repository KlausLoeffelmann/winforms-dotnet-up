# One-way ObservableCollection adapter

## Contents

- [Contract](#contract)
- [Implementation](#implementation)
- [Use and tests](#use-and-tests)

## Contract

This is an **ordered snapshot mirror**, not a two-way structural adapter. Create/use/dispose on the WinForms UI thread. All source collection and item notifications must occur there. Commands modify the source; Add/Remove/Replace/Move/Reset rebuild the mirror in current source order, including duplicate item references.

The binding side cannot add/remove/replace/clear entries. Item properties can be edited when mutable/notifying; property changes affect the shared objects. Structural refresh emits Reset and may reset currency/selection; preserve selection by identity/key at the view boundary if needed. Use a more elaborate incremental adapter only when measured scale/currency requirements justify it.

The **owning adapter** implements IDisposable; BindingList itself has no Dispose override. Detach views before disposal. This intentionally simple O(n) refresh favors complete semantics over a partially implemented incremental adapter.

## Implementation

```csharp
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

/// <summary>Mirrors source order with read-only binding-side structure.</summary>
public sealed class ObservableBindingAdapter<T> : IDisposable where T : class
{
    private ObservableCollection<T>? _source;
    private readonly MirrorList _mirror = new();
    private readonly int _threadId = Environment.CurrentManagedThreadId;

    /// <summary>Subscribes to a source owned by the current UI thread.</summary>
    public ObservableBindingAdapter(ObservableCollection<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _mirror.Reload(source.ToArray());
        source.CollectionChanged += SourceChanged;
    }

    /// <summary>Gets the structurally read-only binding list.</summary>
    public BindingList<T> List
    {
        get
        {
            CheckThread();
            ObjectDisposedException.ThrowIf(_source is null, this);

            return _mirror;
        }
    }

    private void CheckThread()
    {
        if (Environment.CurrentManagedThreadId != _threadId)
        {
            throw new InvalidOperationException("Source and adapter must stay on the owning UI thread.");
        }
    }

    private void SourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CheckThread();
        _mirror.Reload(_source!.ToArray());
    }

    /// <summary>Unsubscribes and releases mirrored item references.</summary>
    public void Dispose()
    {
        CheckThread();
        if (_source is null)
        {
            return;
        }

        _source.CollectionChanged -= SourceChanged;
        _source = null;
        _mirror.Reload(Array.Empty<T>());
    }

    /// <summary>Allows structural changes only while refreshing the mirror.</summary>
    private sealed class MirrorList : BindingList<T>
    {
        private bool _refreshing;

        internal MirrorList()
        {
            AllowNew = false;
            AllowRemove = false;
        }

        internal void Reload(T[] items)
        {
            _refreshing = true;
            RaiseListChangedEvents = false;
            try
            {
                Clear();
                foreach (T item in items)
                {
                    Add(item);
                }
            }
            finally
            {
                _refreshing = false;
                RaiseListChangedEvents = true;
                ResetBindings();
            }
        }

        private void CheckMutation()
        {
            if (!_refreshing)
            {
                throw new NotSupportedException("Modify the source collection, not the binding mirror.");
            }
        }

        protected override void InsertItem(int index, T item)
        {
            CheckMutation();
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            CheckMutation();
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, T item)
        {
            CheckMutation();
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            CheckMutation();
            base.ClearItems();
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged
                && e.NewIndex >= 0 && e.NewIndex < Count)
            {
                T changed = this[e.NewIndex];
                for (int index = 0; index < Count; index++)
                {
                    if (index != e.NewIndex && ReferenceEquals(this[index], changed))
                    {
                        base.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));

                        return;
                    }
                }
            }

            base.OnListChanged(e);
        }
    }
}
```

Duplicate references use Reset on item changes because BindingList item notification can otherwise identify only one occurrence. As with structural resets, preserve currency explicitly when needed.

## Use and tests

Bind `adapter.List` through BindingSource; disable grid AllowUserToAddRows/AllowUserToDeleteRows. Add/remove commands operate on ObservableCollection. Replacing a ViewModel collection requires disposing the old adapter and subscribing to the new one.

Test insert-at-index, remove, replace, move, reset/clear, duplicate items, notifying item edits, rejected binding-side mutations, source replacement, disposal and off-thread mutation. An off-thread source change is a caller contract violation: the event is synchronous after source mutation, so the guard reports failure rather than undoing the source.
