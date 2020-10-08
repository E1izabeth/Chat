using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Chat.Common;
using Chat.Interaction;
using Chat.Interaction.Xml;

namespace Chat.UI.Common
{
    public abstract class Util : DependencyObject
    {


        public static bool GetAutoScrollEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollEnabledProperty);
        }

        public static void SetAutoScrollEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollEnabledProperty, value);
        }

        // Using a DependencyProperty as the backing store for AutoScrollEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoScrollEnabledProperty =
            DependencyProperty.RegisterAttached("AutoScrollEnabled", typeof(bool), typeof(Util), new PropertyMetadata(false, (obj, ea) => {
                var oldValue = ea.OldValue;
                if (!(oldValue is bool))
                    oldValue = false;

                if (ea.NewValue is bool x && oldValue is bool y)
                {
                    if (obj is TextBoxBase tb)
                    {
                        if (x && !y)
                        {
                            tb.TextChanged += OnTextChanged;
                        }
                        else if (!x && y)
                        {
                            tb.TextChanged -= OnTextChanged;
                        }
                    }
                    else if (obj is ListView lv)
                    {
                        if (!_listViews.TryGetValue(lv, out var ctx))
                            _listViews.Add(lv, ctx = new ListViewItemsWatchingContext(lv));

                        ctx.OnAutoScrollEnabledChanged(x, y);
                    }
                }
            }));

        static Dictionary<ListView, ListViewItemsWatchingContext> _listViews = new Dictionary<ListView, ListViewItemsWatchingContext>();

        private static void OnTextChanged(object sender, TextChangedEventArgs ea)
        {
            if (sender is TextBoxBase tbb)
            {
                bool scrollToEnd = tbb.VerticalOffset + tbb.ViewportHeight == tbb.ExtentHeight;
                if (scrollToEnd)
                {
                    if (tbb is TextBox tb)
                        tb.CaretIndex = tb.Text.Length;
                    if (tbb is RichTextBox rtb && rtb.Document != null)
                        rtb.CaretPosition = rtb.Document.ContentEnd;

                    tbb.ScrollToEnd();
                }
            }
        }

        private class ListViewItemsWatchingContext
        {
            public ListView ListView { get; }

            public bool IsTrackingEnabled { get; set; }

            INotifyCollectionChanged _oldCollection = null;

            public ListViewItemsWatchingContext(ListView listView)
            {
                this.ListView = listView;

                var dpd = DependencyPropertyDescriptor.FromProperty(ListView.ItemsSourceProperty, typeof(ListView));

                if (dpd != null)
                {
                    dpd.AddValueChanged(this.ListView, this.OnItemsSourceChanged);
                }

            }

            void OnItemsSourceChanged(object sender, EventArgs ea)
            {
                var newCollection = this.ListView.ItemsSource as INotifyCollectionChanged;

                if (newCollection != null)
                {
                    if (this.IsTrackingEnabled)
                    {
                        if (_oldCollection != null)
                            _oldCollection.CollectionChanged -= this.OnCollectionChanged;

                        if (newCollection != null)
                            newCollection.CollectionChanged += this.OnCollectionChanged;
                    }
                }

                _oldCollection = newCollection;
            }

            void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs ea)
            {
                var border = VisualTreeHelper.GetChild(this.ListView, 0);
                ScrollViewer scrollView = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                if (scrollView != null)
                {
                    bool scrollToEnd = scrollView.VerticalOffset + scrollView.ViewportHeight == scrollView.ExtentHeight;
                    if (scrollToEnd)
                    {
                        scrollView.ScrollToEnd();
                    }
                }
            }

            public void OnAutoScrollEnabledChanged(bool newValue, bool oldValue)
            {
                var x = newValue;
                var y = oldValue;

                if (this.ListView.ItemsSource is INotifyCollectionChanged ncc)
                {
                    if (x && !y)
                    {
                        this.IsTrackingEnabled = true;
                        ncc.CollectionChanged += this.OnCollectionChanged;
                    }
                    else if (!x && y)
                    {
                        this.IsTrackingEnabled = false;
                        ncc.CollectionChanged -= this.OnCollectionChanged;
                    }
                }

                this.IsTrackingEnabled = x;
            }
        }

        public static void MsgBox(string message, string title = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title, button, image);
        }

        public static void ShowError(Exception ex)
        {
            System.Diagnostics.Debug.Print(ex.FormatExceptionOutputInfo());

            var w = new IndentedWriter("  ");

            void flattenRemoteError(ErrorInfoType info)
            {
                w.WriteLine(info.Message);
                w.Push();

                if (info.InnerError != null)
                    flattenRemoteError(info.InnerError);

                w.Pop();
            }

            void flattenError(Exception err)
            {
                if (err is AggregateException exs)
                {
                    foreach (var item in exs.InnerExceptions)
                        flattenError(item);
                }
                else
                {
                    w.WriteLine(err.Message);

                    w.Push();
                    if (err.InnerException != null)
                        flattenError(err.InnerException);

                    if (err is ChatInteractionException ex2)
                    {
                        System.Diagnostics.Debug.Print(ex2.Info.FormatErrorInfo());

                        flattenRemoteError(ex2.Info);
                    }
                    w.Pop();
                }
            }

            flattenError(ex);

            MessageBox.Show(w.GetContentAsString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }


    }
}
