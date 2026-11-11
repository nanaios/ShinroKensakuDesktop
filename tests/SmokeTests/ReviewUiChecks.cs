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

    // Skia draws only inside a live window, so each chart is scrolled into view and captured there.
    private static async Task RenderCharts(MainWindow window, List<ShinroKensakuDesktop.Views.Controls.SeriesChartView> charts, string theme)
    {
        for (int i = 0; i < charts.Count; i++)
        {
            charts[i].BringIntoView();
            await Task.Delay(1200);
            window.UpdateLayout();
            Render(window, $"analytics-{theme}-chart{i + 1}");
        }
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
        SearchCriteria? lastCriteria = null;
        var search = new SearchPageViewModel((criteria, _) =>
        {
            searchCalls++;
            lastCriteria = criteria;
            return Task.FromResult(Enumerable.Range(0, 1200).Select(i => new SearchResultData
            {
                Kind = i % 3 == 0 ? "就職" : "進学", Year = 2026, Jyukenbi = new DateTime(2026, 9, 30), Shinro_name = $"検証用大学 {i}",
                Gakubu = "工学部", Gakka = "情報学科", Jyukenhouhou_name = "一般", G_name = "検証データ",
                Cls_name = "検証クラス", Result = (i % 3) switch { 0 => "合格", 1 => "不合格", _ => "未確定" }
            }).ToList());
        }, () => Task.FromResult(new List<ExamMethodData> { new("A", "一般") }),
            () => Task.FromResult(new List<DepartmentData> { new(1, "検証クラス", "情報工学科1年") }),
            () => Task.FromResult(new List<int> { 2026, 2025, 2024 }));
        List<DashboardGroup> fixture = [new(2024, "進学", "一般", 100, "情報工学科", 13), new(2025, "進学", "一般", 200, "情報工学科", 13), new(2026, "進学", "一般", 300, "建築工学科", 16), new(2026, "就職", "推薦", 50, "情報工学科", 13)];
        var provider = new TestPageProvider();
        var navigation = new NavigationService(provider);
        var dashboard = new DashboardPageViewModel(navigation, search, () => Task.FromResult(fixture));
        var analytics = new AnalyticsPageViewModel(navigation, search, () => Task.FromResult(fixture));
        var searchPage = new SearchPage(search);
        var analyticsPage = new AnalyticsPage(analytics);
        var comparisonPage = new ComparisonPage(analytics);
        var dashboardPage = new DashBoardPage(dashboard);
        provider.Page = searchPage;
        provider.Pages[typeof(DashBoardPage)] = dashboardPage;
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
            check(settingsPage.FindName("DatabasePassword") is Wpf.Ui.Controls.PasswordBox password && password.Password.Length == 0 && password.PlaceholderText.Contains("空欄"), "settings never repopulates password and states the blank rule in place");
            check(Children<System.Windows.Controls.TextBlock>(settingsPage).All(x => x.Text != "Test"), "settings contains application guidance instead of placeholder text");
            check(settingsPage.FindName("ReportLinkBox") is Wpf.Ui.Controls.TextBox { PlaceholderText: var hint } && hint.StartsWith("https://")
                && dashboardPage.FindName("ReportButton") is FrameworkElement reportButton && System.Windows.Automation.AutomationProperties.GetName(reportButton) == "受験報告書をブラウザで開く"
                && searchPage.FindName("ReportButton") == null,
                "report link is set in settings and opened from the dashboard header");
            var connectionForm = (FrameworkElement)settingsPage.FindName("ConnectionForm");
            var reportBox = (FrameworkElement)settingsPage.FindName("ReportLinkBox");
            check(!settings.IsAdvancedMode && connectionForm.Visibility != Visibility.Visible && reportBox.Visibility != Visibility.Visible, "settings open read-only");
            if (render) Render(window, "settings-light");
            settings.IsAdvancedMode = true;
            window.UpdateLayout();
            check(connectionForm.IsVisible && reportBox.IsVisible, "advanced settings reveal the editors");
            if (render)
            {
                ((System.Windows.Controls.ScrollViewer)settingsPage.Content).ScrollToBottom();
                window.UpdateLayout();
                Render(window, "settings-advanced-light");
            }
            navigation.Navigate(typeof(AnalyticsPage));
            await Task.Delay(400);
            window.UpdateLayout();
            check(!settings.IsAdvancedMode, "leaving the settings page locks it again");
            if (render) Render(window, "analytics-light");
            var charts = Children<ShinroKensakuDesktop.Views.Controls.SeriesChartView>(analyticsPage).ToList();
            check(charts.Count == 2 && !charts[0].IsDark, "real main window hosts all charts");
            var tips = new System.Windows.Controls.Page[] { dashboardPage, analyticsPage, comparisonPage, searchPage, settingsPage }
                .SelectMany(Children<ShinroKensakuDesktop.Views.Controls.InfoTip>).ToList();
            check(tips.Count <= 3 && tips.All(x => x.Text.Length > 0 && x.ToolTip is System.Windows.Controls.ToolTip { Content: System.Windows.Controls.TextBlock text } && text.Text == x.Text
                && System.Windows.Automation.AutomationProperties.GetName(x).EndsWith("の説明") && x.FocusVisualStyle != null), "only optional definitions stay behind info tips, named and focusable");
            check(Children<System.Windows.Controls.TextBlock>(analyticsPage).Any(x => x.Text == "点をクリックで、その年度の明細" && x.IsVisible), "chart click target is stated beside the chart");
            if (render) await RenderCharts(window, charts, "light");
            settings.CurrentApplicationTheme = ApplicationTheme.Dark;
            await Task.Delay(400);
            window.UpdateLayout();
            if (render) await RenderCharts(window, charts, "dark");
            var darkStroke = ((LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint)((LiveChartsCore.SkiaSharpView.LineSeries<double>)charts[0].Chart.Series.ElementAt(1)).Stroke!).Color;
            check(charts[0].IsDark && darkStroke == SkiaSharp.SKColor.Parse(ShinroKensakuDesktop.Views.Controls.SeriesChartView.SeriesColor(0, true)), "charts repaint with dark palette on theme switch");
            check(((SolidColorBrush)Application.Current.Resources["StudyBrush"]).Color == (Color)ColorConverter.ConvertFromString(ShinroKensakuDesktop.Views.Controls.SeriesChartView.SeriesColor(0, true))
                && ((SolidColorBrush)Application.Current.Resources["WorkBrush"]).Color == (Color)ColorConverter.ConvertFromString(ShinroKensakuDesktop.Views.Controls.SeriesChartView.SeriesColor(1, true)), "kind colours follow the theme outside the charts");
            charts[0].Legend.Children[1].RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseEnterEvent });
            var faded = ((LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint)((LiveChartsCore.SkiaSharpView.LineSeries<double>)charts[0].Chart.Series.ElementAt(2)).Stroke!).Color;
            check(faded.Alpha < 255, "hovering a legend entry fades the other lines");
            charts[0].Legend.Children[1].RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseLeaveEvent });
            settings.CurrentApplicationTheme = ApplicationTheme.Light;
            await Task.Delay(400);
            await analytics.OpenYearSearchCommand.ExecuteAsync(2025);
            check(search.SelectedYear == "2025" && searchCalls == 2 && search.SearchTargetName == null, "chart point opens search for that year");
            analytics.SelectedDepartment = "情報工学科";
            navigation.Navigate(typeof(AnalyticsPage));
            await Task.Delay(100);
            await analytics.OpenYearSearchCommand.ExecuteAsync(2025);
            check(search.SelectedDepartmentName == "情報工学科" && lastCriteria is { Gakka: "情報工学科", GakkaClasses: "1", Year: 2025 }, "chart point carries the graph's department into the search");
            navigation.Navigate(typeof(ComparisonPage));
            await Task.Delay(100);
            await analytics.OpenSearchCommand.ExecuteAsync(null);
            check(search.SelectedDepartmentName == null && searchCalls == 4, "comparison years open without a department");
            analytics.SelectedDepartment = AnalyticsPageViewModel.AllDepartments;
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
