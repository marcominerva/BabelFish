using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BabelFish.Behaviors
{
    public class ScrollToBottomBehavior : DependencyObject, IBehavior
    {
        public DependencyObject AssociatedObject { get; private set; }

        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(object),
            typeof(ScrollToBottomBehavior),
            new PropertyMetadata(null, ItemsSourcePropertyChanged));

        private static void ItemsSourcePropertyChanged(object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as ScrollToBottomBehavior;
            if (behavior.AssociatedObject == null || e.NewValue == null) return;

            var collection = behavior.ItemsSource as INotifyCollectionChanged;
            if (collection != null)
            {
                collection.CollectionChanged += (s, args) =>
                {
                    var scrollViewer = behavior.AssociatedObject.GetFirstDescendantOfType<ScrollViewer>();
                    scrollViewer.UpdateLayout();
                    scrollViewer.Measure(scrollViewer.RenderSize);
                    scrollViewer.ChangeView(0, scrollViewer.ScrollableHeight, scrollViewer.ZoomFactor, false);
                };
            }
        }

        public void Attach(DependencyObject associatedObject)
        {
            var control = associatedObject as ListView;
            if (control == null)
                throw new ArgumentException(
                    "ScrollToBottomBehavior can be attached only to ListView.");

            AssociatedObject = associatedObject;
        }

        public void Detach()
        {
            AssociatedObject = null;
        }
    }
}
