using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IECGUI.Common.WPFExtensions.Behaviors
{
    public static class ListBoxSelectedItemsBehavior
    {
        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(ListBoxSelectedItemsBehavior));

        private static void SetIsUpdating(DependencyObject d, bool value) => d.SetValue(IsUpdatingProperty, value);
        private static bool GetIsUpdating(DependencyObject d) => (bool)(d.GetValue(IsUpdatingProperty) ?? false);

        private static readonly DependencyProperty CollectionChangedHandlerProperty =
            DependencyProperty.RegisterAttached("CollectionChangedHandler", typeof(NotifyCollectionChangedEventHandler), typeof(ListBoxSelectedItemsBehavior));

        private static void SetCollectionChangedHandler(DependencyObject d, NotifyCollectionChangedEventHandler? handler) => d.SetValue(CollectionChangedHandlerProperty, handler);
        private static NotifyCollectionChangedEventHandler? GetCollectionChangedHandler(DependencyObject d) => d.GetValue(CollectionChangedHandlerProperty) as NotifyCollectionChangedEventHandler;

        public static readonly DependencyProperty BindableSelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "BindableSelectedItems",
                typeof(IList),
                typeof(ListBoxSelectedItemsBehavior),
                new PropertyMetadata(null, OnBindableSelectedItemsChanged));

        public static void SetBindableSelectedItems(DependencyObject element, IList value)
        {
            element.SetValue(BindableSelectedItemsProperty, value);
        }

        public static IList GetBindableSelectedItems(DependencyObject element)
        {
            return (IList)element.GetValue(BindableSelectedItemsProperty);
        }

        private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox)
                return;

            // detach old collection handler
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                var oldHandler = GetCollectionChangedHandler(listBox);
                if (oldHandler != null)
                {
                    oldCollection.CollectionChanged -= oldHandler;
                }
            }

            // attach new collection handler
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                NotifyCollectionChangedEventHandler handler = (s, args) => SyncSelectedItems(listBox);
                SetCollectionChangedHandler(listBox, handler);
                newCollection.CollectionChanged += handler;
            }
            else
            {
                SetCollectionChangedHandler(listBox, null);
            }

            listBox.SelectionChanged -= ListBox_SelectionChanged;
            listBox.SelectionChanged += ListBox_SelectionChanged;

            SyncSelectedItems(listBox);
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            var bindableSelectedItems = GetBindableSelectedItems(listBox);
            if (bindableSelectedItems == null)
                return;

            if (GetIsUpdating(listBox))
                return;

            try
            {
                SetIsUpdating(listBox, true);

                // remove deselected items
                foreach (var item in e.RemovedItems)
                {
                    if (bindableSelectedItems.Contains(item))
                        bindableSelectedItems.Remove(item);
                }

                // add newly selected items
                foreach (var item in e.AddedItems)
                {
                    if (!bindableSelectedItems.Contains(item))
                        bindableSelectedItems.Add(item);
                }
            }
            finally
            {
                SetIsUpdating(listBox, false);
            }
        }

        private static void SyncSelectedItems(ListBox listBox)
        {
            var bindableSelectedItems = GetBindableSelectedItems(listBox);
            if (bindableSelectedItems == null)
                return;

            if (GetIsUpdating(listBox))
                return;

            try
            {
                SetIsUpdating(listBox, true);
                listBox.SelectedItems.Clear();
                foreach (var item in bindableSelectedItems.Cast<object>())
                {
                    if (listBox.Items.Contains(item))
                        listBox.SelectedItems.Add(item);
                }
            }
            finally
            {
                SetIsUpdating(listBox, false);
            }
        }
    }
}
