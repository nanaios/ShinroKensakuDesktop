using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Models.Data;
using ShinroKensakuDesktop.ViewModels.Pages;
using ShinroKensakuDesktop.ViewModels.Windows;
using ShinroKensakuDesktop.Views.Pages;
using ShinroKensakuDesktop.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

internal static class ReviewUiChecks
{
    private static Color Composite(Color color, Color background)
    {
        double alpha = color.A / 255.0;
        return Color.FromRgb((byte)Math.Round(color.R * alpha + background.R * (1 - alpha)),
            (byte)Math.Round(color.G * alpha + background.G * (1 - alpha)),
            (byte)Math.Round(color.B * alpha + background.B * (1 - alpha)));
    }

    private static double Luminance(Color color)
    {
        double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        return Linear(color.R) * 0.2126 + Linear(color.G) * 0.7152 + Linear(color.B) * 0.0722;
    }

    private static IEnumerable<T> Children<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var item in Children<T>(child)) yield return item;
        }
    }

    private static void Render(MainWindow window, string name)
    {
        var root = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var drawing = background.RenderOpen())
            drawing.DrawRectangle((Brush)window.FindResource("ApplicationBackgroundBrush"), null, new Rect(0, 0, bitmap.Width, bitmap.Height));
        bitmap.Render(background);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(Path.GetTempPath(), $"kaken-review-{name}.png"));
        encoder.Save(output);
    }

    public static async Task RunAsync(Action<bool, string> check, bool render)
    {
        string directory = Path.Combine(Path.GetTempPath(), "kaken-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        using var settings = new SettingsPageViewModel(Path.Combine(directory, "preferences.json"));
        settings.DatabaseServer = "localhost";
        settings.DatabaseUser = "検証用ユーザー";
        settings.DatabaseName = "検証用データベース";
        int searchCalls = 0;
        var search = new SearchPageViewModel((_, _) =>
        {
            searchCalls++;
            return Task.FromResult(Enumerable.Range(0, 1200).Select(i => new SearchResultData
            {
                Year = 2026, Jyukenbi = new DateTime(2026, 9, 30), Shinro_name = $"検証用大学 {i}",
                Gakubu = "工学部", Gakka = "情報学科", Jyukenhouhou_name = "一般", G_name = "検証データ",
                Cls_name = "検証クラス", Result = "合格"
            }).ToList());
        }, () => Task.FromResult(new List<ExamMethodData> { new("A", "一般") }),
            () => Task.FromResult(new List<DepartmentData> { new(1, "検証クラス", "検証学科") }));
        List<DashboardGroup> fixture = [new(2024, "進学", "一般", 100), new(2025, "進学", "一般", 200), new(2026, "進学", "一般", 300), new(2026, "就職", "推薦", 50)];
        var provider = new TestPageProvider();
        var navigation = new NavigationService(provider);
        var dashboard = new DashboardPageViewModel(navigation, search, () => Task.FromResult(fixture));
        var analytics = new AnalyticsPageViewModel(navigation, search, () => Task.FromResult(fixture));
        var searchPage = new SearchPage(search);
        var analyticsPage = new AnalyticsPage(analytics);
        var comparisonPage = new ComparisonPage(analytics);
        provider.Page = searchPage;
        provider.Pages[typeof(DashBoardPage)] = new DashBoardPage(dashboard);
        provider.Pages[typeof(AnalyticsPage)] = analyticsPage;
        provider.Pages[typeof(ComparisonPage)] = comparisonPage;
        provider.Pages[typeof(SettingsPage)] = new SettingsPage(settings);
        using var services = new ServiceCollection().AddSingleton<Wpf.Ui.Abstractions.INavigationViewPageProvider>(provider).BuildServiceProvider();
        var window = new MainWindow(new MainWindowViewModel(), navigation, services, settings)
        { ShowActivated = false, ShowInTaskbar = false, Opacity = 0 };
        try
        {
            window.Show();
            await Task.Delay(100);
            window.UpdateLayout();
            check(dashboard.SelectedYear == 2026 && dashboard.TotalText == "350", "real main window initializes dashboard through page provider");
            check(window.Width <= SystemParameters.WorkArea.Width && window.Height <= SystemParameters.WorkArea.Height, "initial window fits desktop work area");
            navigation.Navigate(typeof(AnalyticsPage));
            await Task.Delay(100);
            analytics.SelectedYear = 2025;
            navigation.Navigate(typeof(ComparisonPage));
            await Task.Delay(100);
            analytics.BaselineYear = 2024;
            await analytics.RefreshCommand.ExecuteAsync(null);
            window.UpdateLayout();
            check(analytics.SelectedYear == 2025 && analytics.BaselineYear == 2024, "bound year selectors preserve both years on refresh");
            check(analytics.Comparisons[0].Difference == 100, "bound comparison updates to selected years");
            if (render) Render(window, "comparison-light");
            navigation.Navigate(typeof(SearchPage));
            await Task.Delay(100);
            var box = (AutoSuggestBox)searchPage.FindName("SearchNameBox");
            box.SetCurrentValue(AutoSuggestBox.TextProperty, "検証用大学");
            box.RaiseEvent(new AutoSuggestBoxQuerySubmittedEventArgs(AutoSuggestBox.QuerySubmittedEvent, box) { QueryText = box.Text });
            await search.ExecuteSearchCommand.ExecutionTask!;
            check(searchCalls == 1 && search.SearchResultList.Count == 1200, "search box submission executes search once");
            check(searchPage.InputBindings.OfType<System.Windows.Input.KeyBinding>().Any(x => x.Key == System.Windows.Input.Key.Escape), "search page exposes Escape cancellation shortcut");
            window.Width = 900;
            window.Height = 600;
            Children<System.Windows.Controls.Expander>(searchPage).First().IsExpanded = true;
            var grid = (System.Windows.Controls.DataGrid)searchPage.FindName("ResultGrid");
            foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
            {
                settings.CurrentApplicationTheme = theme;
                await Task.Delay(400);
                window.UpdateLayout();
                var header = Children<DataGridColumnHeader>(grid).First(x => x.Content?.ToString() == "卒業年度");
                var background = Composite(((SolidColorBrush)header.Background).Color,
                    ((SolidColorBrush)window.FindResource("ApplicationBackgroundBrush")).Color);
                var foreground = Composite(((SolidColorBrush)header.Foreground).Color, background);
                double contrast = (Math.Max(Luminance(foreground), Luminance(background)) + 0.05) /
                    (Math.Min(Luminance(foreground), Luminance(background)) + 0.05);
                if (render) Render(window, "search-" + theme.ToString().ToLowerInvariant());
                check(contrast >= 4.5, "result headers contrast in " + theme + " theme");
                check(grid.ActualHeight > 100 && Children<DataGridRow>(grid).Count() < 100, "expanded search remains virtualized in real small window: " + theme);
            }
            settings.CurrentApplicationTheme = ApplicationTheme.Light;
            navigation.Navigate(typeof(SettingsPage));
            await Task.Delay(400);
            window.UpdateLayout();
            var settingsPage = (SettingsPage)provider.Pages[typeof(SettingsPage)];
            check(settingsPage.FindName("DatabasePassword") is System.Windows.Controls.PasswordBox password && password.Password.Length == 0, "settings never repopulates password into visible control");
            check(Children<System.Windows.Controls.TextBlock>(settingsPage).All(x => x.Text != "Test"), "settings contains application guidance instead of placeholder text");
            if (render) Render(window, "settings-light");
            navigation.Navigate(typeof(AnalyticsPage));
            await Task.Delay(400);
            window.UpdateLayout();
            if (render) Render(window, "analytics-light");
            check(Children<ShinroKensakuDesktop.Views.Controls.PieChart>(analyticsPage).Count() == 3, "real main window hosts all charts");
            settings.CurrentApplicationTheme = ApplicationTheme.Unknown;
        }
        finally
        {
            window.Close();
            settings.Dispose();
            check(true, "system theme watcher detaches safely when window closes");
            Directory.Delete(directory, recursive: true);
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
        }
    }
}
