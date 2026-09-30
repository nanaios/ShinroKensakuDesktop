using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Views.Behaviors;

// NavigationView wraps Pages in its own scroller. Bound the inner viewport so it
// receives wheel input and has a real scroll extent rather than infinite height.
public static class NavigationViewport
{
    public static readonly DependencyProperty FitProperty = DependencyProperty.RegisterAttached(
        "Fit", typeof(bool), typeof(NavigationViewport), new PropertyMetadata(false, OnFitChanged));
    public static bool GetFit(DependencyObject target) => (bool)target.GetValue(FitProperty);
    public static void SetFit(DependencyObject target, bool value) => target.SetValue(FitProperty, value);

    private static void OnFitChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not FrameworkElement viewer) return;
        if ((bool)e.NewValue) viewer.Loaded += OnLoaded;
        else
        {
            viewer.Loaded -= OnLoaded;
            BindingOperations.ClearBinding(viewer, FrameworkElement.HeightProperty);
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var viewer = (FrameworkElement)sender;
        viewer.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
            new Action(() => BindViewport(viewer)));
    }

    private static void BindViewport(FrameworkElement viewer)
    {
        if (!viewer.IsLoaded || !GetFit(viewer)) return;
        for (DependencyObject? parent = viewer; parent != null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is NavigationViewContentPresenter viewport)
            {
                viewer.SetBinding(FrameworkElement.HeightProperty,
                    new Binding(nameof(FrameworkElement.ActualHeight)) { Source = viewport });
                break;
            }
        }
    }
}
