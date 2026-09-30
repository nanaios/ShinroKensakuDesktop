using ShinroKensakuDesktop.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ShinroKensakuDesktop.Views.Pages;

public partial class SearchPage : Page
{
    private bool loading;
    private void ResultGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scroller = FindScrollViewer(ResultGrid);
        if (scroller == null) return;
        if ((e.Delta > 0 && scroller.VerticalOffset <= 0) ||
            (e.Delta < 0 && scroller.VerticalOffset >= scroller.ScrollableHeight))
        {
            PageScroll.ScrollToVerticalOffset(PageScroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ScrollViewer viewer) return viewer;
            if (FindScrollViewer(child) is { } nested) return nested;
        }
        return null;
    }
    public SearchPage(SearchPageViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            // Page/frame boundaries prevent an ancestor binding from reaching the navigation viewport.
            for (DependencyObject? parent = this; parent != null; parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is Wpf.Ui.Controls.NavigationViewContentPresenter viewport)
                {
                    PageScroll.SetBinding(HeightProperty, new System.Windows.Data.Binding(nameof(ActualHeight)) { Source = viewport });
                    break;
                }
            }
            if (loading) return;
            loading = true;
            try
            {
                await viewModel.LoadExamMethodComboBoxItems();
                await viewModel.LoadDepartmentComboBoxItems();
            }
            catch (Exception)
            {
                viewModel.StatusText = "検索条件を取得できませんでした。接続を確認し、画面を開き直してください。";
            }
            finally { loading = false; }
        };
    }
}
