using System.Data;
using System.Windows;
using System.Windows.Threading;
using ShinroKensakuDesktop;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Models.Data;
using ShinroKensakuDesktop.ViewModels.Pages;
using ShinroKensakuDesktop.Views.Pages;
using Wpf.Ui;

internal static class Program
{
    private static int checks;
    private static readonly BindingErrors bindingErrors = new();
    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            Console.Error.WriteLine("FAIL check: " + name);
            throw new InvalidOperationException(name);
        }
        checks++;
        Console.WriteLine($"PASS {name}");
    }

    [STAThread]
    private static int Main(string[] args)
    {
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Error;
        var app = new App();
        app.InitializeComponent();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var frame = new DispatcherFrame();
        var exitCode = 0;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        _ = RunAsync(args).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Connection errors may contain credentials or schema details: do not print them.
                Console.Error.WriteLine("FAIL " + task.Exception!.GetBaseException().GetType().Name);
                Console.Error.WriteLine(task.Exception.GetBaseException().StackTrace);
                exitCode = 1;
            }
            app.Dispatcher.Invoke(() => frame.Continue = false);
        });
        Dispatcher.PushFrame(frame);
        return exitCode;
    }

    private static IEnumerable<T> VisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var nested in VisualChildren<T>(child)) yield return nested;
        }
    }

    private static async Task RunAsync(string[] args)
    {
        await ReviewChecks.RunAsync(Check, args.Contains("--database"));
        Check(SearchPageDataGridSourceProvider.FormatResult(true) == "合格", "boolean pass result");
        Check(SearchPageDataGridSourceProvider.FormatResult(0) == "不合格", "numeric fail result");
        Check(SearchPageDataGridSourceProvider.FormatResult(DBNull.Value) == "未確定", "null result");
        var csv = SearchResultCsv.Create([new SearchResultData { Year = 2026, Jyukenbi = new(2026, 9, 29), Shinro_name = "日本語,\"大学\"\r\n学部", G_name = " =1+1" }]);
        Check(csv.Contains("\"日本語,\"\"大学\"\"\r\n学部\""), "CSV quotes commas and newlines");
        Check(csv.Contains("\"' =1+1\""), "CSV formula neutralization");
        Check(SearchResultCsv.Create([]).Split("\r\n").Length == 2, "CSV empty header");
        var search = args.Contains("--database") ? new SearchPageViewModel() : new SearchPageViewModel(
            methods: () => Task.FromResult(new List<ExamMethodData>()),
            departments: () => Task.FromResult(new List<DepartmentData>()),
            years: () => Task.FromResult(new List<int>()));
        Check(search.SearchResultCountText == "0件" && !search.ExportCsvCommand.CanExecute(null), "empty results and export");
        search.SelectedYear = "invalid";
        await search.ExecuteSearchCommand.ExecuteAsync(null);
        Check(search.StatusText.Contains("1901") && !search.IsBusy, "invalid year handled");
        var provider = new TestPageProvider();
        var navigation = new NavigationService(provider);
        var dashboard = new DashboardPageViewModel(navigation, search);
        var dashboardPage = new DashBoardPage(dashboard);
        List<DashboardGroup> fixture = [new(2024, "進学", "推薦", 10), new(2026, "進学", "推薦", 15), new(2026, "就職", "一般", 5)];
        bool failLoad = false;
        var analytics = new AnalyticsPageViewModel(navigation, search, () => failLoad
            ? Task.FromException<List<DashboardGroup>>(new InvalidOperationException()) : Task.FromResult(fixture));
        var analyticsPage = new AnalyticsPage(analytics);
        var comparisonPage = new ComparisonPage(analytics);
        Check(analyticsPage.Content != null && comparisonPage.Content != null, "new WPF pages load");
        await analytics.RefreshCommand.ExecuteAsync(null);
        Check(analytics.SelectedYear == 2026 && analytics.TotalText == "20", "charts default to latest registered year");
        Check(analytics.RangeStartYear == 2024 && analytics.RangeEndYear == 2026 && analytics.RangeTotalText == "30", "range defaults to all registered years");
        var kindTrend = analytics.KindTrendChart;
        Check(kindTrend.Labels.SequenceEqual(new[] { "2024", "2026" }) && kindTrend.Keys.SequenceEqual(new[] { 2024, 2026 }), "trend ordered chronologically with missing year");
        Check(kindTrend.Series.Select(x => x.Name).SequenceEqual(new[] { "合計", "進学", "就職" }) && kindTrend.Series[0].IsReference, "kind trend has total reference line and both kinds");
        Check(kindTrend.Series[1].Slot == 0 && kindTrend.Series[2].Slot == 1, "進学 and 就職 keep the two reserved colours");
        Check(kindTrend.Series[0].Values.SequenceEqual(new[] { 10, 20 }) && kindTrend.Series[2].Values.SequenceEqual(new[] { 0, 5 }), "kind trend fills absent kind with zero");
        Check(analytics.MethodTrendChart.Series.Sum(x => x.Total) == 25 && analytics.MethodTrendChart.Series.All(x => x.Slot >= ChartData.FirstMethodSlot), "method trend draws 進学 methods only, never in a kind colour");
        Check(analytics.MethodBreakdown.Sum(x => x.Rows.Count) == 2 && analytics.MethodBreakdown.Select(x => x.Title).SequenceEqual(new[] { "進学", "就職" }), "method breakdown grouped under each kind");
        Check(analytics.MethodBreakdown[0].Rows[0] is { Label: "推薦", Value: "25", Share: "83%", Ratio: 1 } && analytics.MethodBreakdown[1].Rows[0] is { Label: "一般", Kind: StatKind.Work, Share: "17%" }, "method rows carry count, share and bar ratio");
        Check(analytics.RangeStats[0].Note == "年平均 15件" && analytics.RangeStats[1].Share == "83%" && analytics.RangeStats[2].Share == "17%", "range totals show yearly average and shares");
        Check(analytics.Comparisons[0].Difference == 10 && analytics.Comparisons[2].RateText == "—", "comparison handles increases and zero baseline");
        int recommendSlot = analytics.MethodTrendChart.Series.Single(x => x.Name == "推薦").Slot;
        analytics.RangeEndYear = 2024;
        Check(analytics.RangeYears.SequenceEqual(new[] { 2024 }) && analytics.RangeTotalText == "10" && analytics.RangeWorkText == "0", "narrowed range updates totals");
        Check(analytics.MethodTrendChart.Series.Single().Slot == recommendSlot, "method colour slot survives range change");
        Check(!analytics.IsAllRange && analytics.RangeStats[0].Note == null, "custom range clears the preset state");
        analytics.RangeStartYear = 2026;
        Check(analytics.RangeStartYear == 2026 && analytics.RangeEndYear == 2026 && analytics.RangeYears.SequenceEqual(new[] { 2026 }), "start after end moves end");
        analytics.RangeEndYear = 2024;
        Check(analytics.RangeStartYear == 2024 && analytics.RangeEndYear == 2024, "end before start moves start");
        analytics.ShowAllYearsCommand.Execute(null);
        Check(analytics.RangeYears.Count == 2 && analytics.IsAllRange && !analytics.IsRecentRange, "all years preset is shown as selected");
        analytics.RangeStartYear = 2026;
        analytics.SelectedYear = 2024;
        Check(analytics.TotalText == "10" && analytics.RangeTotalText == "20", "comparison year and chart range are independent");
        await analytics.RefreshCommand.ExecuteAsync(null);
        Check(analytics.SelectedYear == 2024, "analytics preserves selected year on refresh");
        Check(analytics.RangeStartYear == 2026 && analytics.RangeEndYear == 2026, "analytics preserves year range on refresh");
        Check(analytics.Comparisons.All(x => x.Difference == 0), "same year comparison");
        analytics.BaselineYear = 2026;
        Check(analytics.Comparisons[0].Difference == -10, "comparison handles decreases");
        failLoad = true;
        await analytics.RefreshCommand.ExecuteAsync(null);
        Check(!analytics.KindTrendChart.HasData && analytics.MethodBreakdown.Count == 0 && analytics.RangeTotalText == "—" && analytics.Comparisons.Count == 0 && analytics.TotalText == "—" && !analytics.OpenSearchCommand.CanExecute(null), "failed refresh clears stale charts and comparisons");
        Check(analytics.HasError && analytics.ErrorText.Contains("接続") && analytics.UpdatedText == "" && !analytics.IsEmpty && analytics.SummaryStats.All(x => x.Value == "—"), "failed refresh raises an error notice instead of stale numbers");
        failLoad = false;
        fixture = [];
        await analytics.RefreshCommand.ExecuteAsync(null);
        Check(analytics.SelectedYear == null && analytics.RangeStartYear == null && !analytics.KindTrendChart.HasData && analytics.Comparisons.Count == 0, "empty database handled");
        Check(analytics.IsEmpty && !analytics.HasError && analytics.SummaryStats.Concat(analytics.RangeStats).All(x => !x.IsPlaceholder), "empty database shows the empty notice, not an error or placeholders");
        List<DashboardGroup> manyMethods = Enumerable.Range(1, 9).Select(i => new DashboardGroup(2026, "進学", $"方法{i}", 100 - i)).ToList();
        var folded = ChartData.MethodTrend(manyMethods, [2026]);
        Check(folded.Series.Count == ChartData.MaxMethods + 1 && folded.Series[^1].Name == ChartData.Other && folded.Series[^1].Slot == ChartData.OtherSlot && folded.Series[^1].Total == 91 + 92 + 93 && folded.Series.Select(x => x.Slot).Distinct().Count() == folded.Series.Count, "methods beyond palette fold into a grey その他");
        Check(ChartData.MethodTrend(manyMethods.Take(6), [2026]).Series.All(x => x.Name != ChartData.Other) && ChartData.MethodTrend(manyMethods.Take(7), [2026]).Series.Any(x => x.Name == ChartData.Other), "six methods fit without folding");
        Check(ChartData.Percent(1, 300) == "1%未満" && ChartData.Percent(0, 0) == "—" && ChartData.Percent(1, 3) == "33%", "shares are rounded and never read as zero");
        Check(DashboardDataProvider.DepartmentOf("情報工学科3年") == "情報工学科" && DashboardDataProvider.DepartmentOf("科学科２年") == "科学科" && DashboardDataProvider.DepartmentOf(null) == DashboardDataProvider.Unassigned, "department derived from class name without grade");
        List<DashboardGroup> departmentFixture =
        [
            new(2025, "進学", "推薦", 10, "情報工学科", 13), new(2025, "就職", "一般", 4, "情報工学科", 7),
            new(2026, "進学", "一般", 6, "建築工学科", 16), new(2026, "進学", "推薦", 3, "情報工学科", 13),
        ];
        var byDepartment = new AnalyticsPageViewModel(navigation, search, () => Task.FromResult(departmentFixture));
        await byDepartment.RefreshCommand.ExecuteAsync(null);
        Check(byDepartment.Departments.SequenceEqual(new[] { AnalyticsPageViewModel.AllDepartments, "情報工学科", "建築工学科" }) && byDepartment.RangeTotalText == "23", "departments listed in class order with all selected");
        int generalSlot = byDepartment.MethodTrendChart.Series.Single(x => x.Name == "一般").Slot;
        byDepartment.SelectedDepartment = "情報工学科";
        Check(byDepartment.RangeTotalText == "17" && byDepartment.RangeWorkText == "4" && byDepartment.KindTrendChart.Series[0].Values.SequenceEqual(new[] { 14, 3 }), "department filters totals and trend");
        Check(byDepartment.MethodBreakdown.Select(x => x.Title).SequenceEqual(new[] { "進学", "就職" }) && byDepartment.RangeStats[0].Value == "17", "department filters method breakdown and totals");
        byDepartment.SelectedDepartment = "建築工学科";
        Check(byDepartment.MethodTrendChart.Series.Single().Slot == generalSlot && byDepartment.KindTrendChart.Series[0].Values.SequenceEqual(new[] { 0, 6 }), "department keeps method colours and year axis");
        await byDepartment.RefreshCommand.ExecuteAsync(null);
        Check(byDepartment.SelectedDepartment == "建築工学科" && byDepartment.RangeTotalText == "6", "department preserved on refresh");
        departmentFixture = departmentFixture.Where(x => x.Department != "建築工学科").ToList();
        await byDepartment.RefreshCommand.ExecuteAsync(null);
        Check(byDepartment.SelectedDepartment == AnalyticsPageViewModel.AllDepartments && byDepartment.RangeTotalText == "17", "vanished department falls back to all");
        List<DashboardGroup> summaryFixture =
        [
            new(2016, "進学", "指定校推薦", 90), new(2018, "進学", "指定校推薦", 100),
            new(2019, "進学", "指定校推薦", 230), new(2019, "進学", "総合型選抜", 25), new(2019, "就職", "就職", 233),
            new(2020, "進学", "指定校推薦", 217), new(2020, "進学", "総合型選抜", 28), new(2020, "就職", "就職", 237),
        ];
        var summary = new DashboardPageViewModel(navigation, search, () => Task.FromResult(summaryFixture));
        await summary.RefreshCommand.ExecuteAsync(null);
        Check(summary.YearTitle == "2020年度" && summary.IsLatestYear && summary.UpdatedText.EndsWith("更新"), "dashboard names the latest year in its header");
        Check(summary.SummaryStats[0] is { Label: "延べ受験数", Value: "482", Direction: ChangeDirection.Down, Change: "6", ChangeNote: "前年度 488", Share: null }, "total compared with the previous year");
        Check(summary.SummaryStats[1] is { Kind: StatKind.Study, Value: "245", Share: "51%", Direction: ChangeDirection.Down, Change: "10" } && summary.SummaryStats[2] is { Kind: StatKind.Work, Value: "237", Share: "49%", Direction: ChangeDirection.Up, Change: "4" }, "kinds carry their share and change");
        Check(summary.MethodGroups.Count == 2 && summary.MethodGroups[0] is { Kind: StatKind.Study, Total: "245" } && summary.MethodGroups[0].Rows.Select(x => x.Label).SequenceEqual(new[] { "指定校推薦", "総合型選抜" }) && summary.MethodGroups[1].Rows.Single() is { Label: "就職", Ratio: 1 }, "dashboard methods grouped by kind with comparable bars");
        Check(summary.YearBars.Select(x => x.Year).SequenceEqual(new[] { 2016, 2018, 2019, 2020 }) && summary.YearBars[^1].IsCurrent && summary.YearBars.Count(x => x.ShowLabel) == 2 && summary.YearBars[2].Ratio == 1 && summary.YearBars[^1].Ratio < 1,"year bars run oldest to newest with the current year marked");
        var compare = new AnalyticsPageViewModel(navigation, search, () => Task.FromResult(summaryFixture));
        await compare.RefreshCommand.ExecuteAsync(null);
        Check(compare.BaselineYear == 2019 && compare.ComparisonStats[0] is { Before: "488 →", Value: "482", Direction: ChangeDirection.Down, Change: "6", ChangeNote: "（-1.2%）" }, "comparison cards show both years and the change");
        Check(compare.MethodComparisons.Select(x => x.Label).SequenceEqual(new[] { "指定校推薦", "総合型選抜", "就職" }) && compare.MethodComparisons[0] is { Difference: -13, DecreaseRatio: 1, IncreaseRatio: 0 } && Math.Abs(compare.MethodComparisons[1].IncreaseRatio - 3 / 13.0) < 1e-9, "method changes share one diverging scale");
        Check(compare.SelectedSearchText == "2020年度の明細" && compare.BaselineSearchText == "2019年度の明細" && !compare.IsSameYear, "both years link to their records");
        compare.SwapYearsCommand.Execute(null);
        Check(compare.BaselineYear == 2020 && compare.SelectedYear == 2019 && compare.ComparisonStats[0].Direction == ChangeDirection.Up, "years swap in place");
        compare.SelectedYear = 2018;
        Check(compare.SummaryStats[0].ChangeNote == "2016年度 90", "a gap in registered years names the year compared");
        compare.BaselineYear = 2018;
        Check(compare.IsSameYear && compare.ComparisonStats.All(x => x.Change == "±0"), "same year comparison is flagged");
        compare.ShowRecentYearsCommand.Execute(null);
        Check(compare.IsAllRange && !compare.IsRecentRange && compare.RangeYears.Count == 4, "with five or fewer years the recent preset is the whole period");
        List<DashboardGroup> longFixture = Enumerable.Range(2010, 11).Select(y => new DashboardGroup(y, "進学", "一般", 10)).ToList();
        var longRange = new AnalyticsPageViewModel(navigation, search, () => Task.FromResult(longFixture));
        await longRange.RefreshCommand.ExecuteAsync(null);
        longRange.ShowRecentYearsCommand.Execute(null);
        Check(longRange.IsRecentRange && !longRange.IsAllRange && longRange.RangeYears.SequenceEqual(new[] { 2016, 2017, 2018, 2019, 2020 }), "recent preset is shown as selected");
        var searchPage = new SearchPage(search);
        provider.Page = searchPage;
        var control = new Wpf.Ui.Controls.NavigationView { VerticalContentAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
        control.SetPageProviderService(provider);
        control.MenuItems.Add(new Wpf.Ui.Controls.NavigationViewItem { Content = "検索", TargetPageType = typeof(SearchPage) });
        var host = new Window { Content = control, Width = 1200, Height = 1000, ShowActivated = false, ShowInTaskbar = false, Opacity = 0 };
        host.Show();
        control.UpdateLayout();
        control.Measure(new Size(1200, 1000));
        control.Arrange(new Rect(0, 0, 1200, 1000));
        control.ApplyTemplate();
        navigation.SetNavigationControl(control);
        Check(dashboardPage.Content != null && searchPage.Content != null, "WPF pages load resources and bindings");
        if (args.Contains("--database"))
        {
            var groups = await DashboardDataProvider.LoadAsync();
            var raw = await MySQLCommand.Query("SELECT (SELECT COUNT(*) FROM shingakukekkaTbl) + (SELECT COUNT(*) FROM syuusyokukekkaTbl) AS total");
            Check(groups.Sum(x => x.Count) == Convert.ToInt32(raw.Rows[0]["total"]), "dashboard total matches raw tables");
            await dashboard.RefreshCommand.ExecuteAsync(null);
            Check(!dashboard.IsBusy && dashboard.UpdatedText.EndsWith("更新") && !dashboard.HasError, "dashboard live refresh");
            Check(dashboard.SelectedYear == groups.Max(x => x.Year), "latest registered year selected");
            search.SelectedYear = dashboard.SelectedYear.ToString();
            await search.ExecuteSearchCommand.ExecuteAsync(null);
            var selected = await SearchPageDataGridSourceProvider.GetDataGridSource(null, search.SelectedYear, null, null);
            Check(selected.Count == groups.Where(x => x.Year == dashboard.SelectedYear).Sum(x => x.Count), "dashboard year agrees with search");
            Check(search.SearchResultList.Count == selected.Count, "all matching year records displayed");
            var fullCsv = SearchResultCsv.Create(selected);
            Check(fullCsv.Split("\r\n").Length == selected.Count + 2, "CSV includes all result pages");
            search.SearchTargetName = "' OR 1=1 -- no such school";
            await search.ExecuteSearchCommand.ExecuteAsync(null);
            Check(search.SearchResultList.Count == 0 && search.SearchResultCountText == "0件", "quoted input safe and stale page cleared");
            Check(!search.ExportCsvCommand.CanExecute(null), "empty search disables export");
            await search.LoadExamMethodComboBoxItems();
            var methods = search.ExamMethodComboBoxItems.Count;
            await search.LoadExamMethodComboBoxItems();
            Check(methods == search.ExamMethodComboBoxItems.Count, "lookup loading is idempotent");
            await dashboard.OpenSearchCommand.ExecuteAsync(null);
            Check(search.SearchTargetName == null && search.SearchResultList.Count > 0 && search.SelectedYear == dashboard.SelectedYear.ToString(), "dashboard navigates and searches selected year");
            search.SelectedYear = null;
            await search.ExecuteSearchCommand.ExecuteAsync(null);
            Check(search.SearchResultList.Count == groups.Sum(x => x.Count) && search.SearchResultList.Count > 1000, "all database records displayed without 1000 row cap");
            Check(search.SearchResultList.Count(x => x.Kind == "進学") == groups.Where(x => x.Kind == "進学").Sum(x => x.Count) && search.SearchResultList.All(x => x.Kind is "進学" or "就職"), "search kind column agrees with the dashboard");
            await search.LoadConditionsAsync();
            foreach (var department in groups.Select(x => x.Department).Distinct())
            {
                search.ClearAllConditions();
                search.SelectedDepartmentName = department;
                await search.ExecuteSearchCommand.ExecuteAsync(null);
                Check(search.SearchResultList.Count == groups.Where(x => x.Department == department).Sum(x => x.Count), "department search agrees with the graph (no personal data logged)");
            }
            search.ClearAllConditions();
            Console.WriteLine($"Verified {groups.Sum(x => x.Count)} records across {dashboard.Years.Count} years (no personal data logged).");
        }
        fixture = [new(2024, "進学", "推薦", 10), new(2026, "進学", "推薦", 15), new(2026, "就職", "一般", 5)];
        await analytics.RefreshCommand.ExecuteAsync(null);
        provider.Pages[typeof(AnalyticsPage)] = analyticsPage;
        provider.Pages[typeof(ComparisonPage)] = comparisonPage;
        control.MenuItems.Add(new Wpf.Ui.Controls.NavigationViewItem { Content = "進路グラフ", TargetPageType = typeof(AnalyticsPage) });
        control.MenuItems.Add(new Wpf.Ui.Controls.NavigationViewItem { Content = "年度比較", TargetPageType = typeof(ComparisonPage) });
        dashboard.OpenAnalyticsCommand.Execute(null);
        Check(control.SelectedItem?.TargetPageType == typeof(AnalyticsPage), "dashboard opens analytics");
        dashboard.OpenComparisonCommand.Execute(null);
        Check(control.SelectedItem?.TargetPageType == typeof(ComparisonPage), "dashboard opens comparison");
        navigation.Navigate(typeof(SearchPage));
        Check(ShinroKensakuDesktop.Views.Controls.SeriesChartView.SeriesColor(99, false) == ShinroKensakuDesktop.Views.Controls.SeriesChartView.SeriesColor(7, false), "palette slot never cycles past the last colour");
        Check(ShinroKensakuDesktop.Views.Controls.SeriesChartView.SeriesColor(ChartData.OtherSlot, false) == "#8A8A8A", "その他 is drawn in the neutral grey");
        host.Width = 900;
        host.Height = 600;
        control.Measure(new Size(850, 500));
        control.Arrange(new Rect(0, 0, 850, 500));
        navigation.Navigate(typeof(AnalyticsPage));
        await Task.Delay(300);
        host.UpdateLayout();
        await Task.Delay(300);
        host.UpdateLayout();
        var graphScroll = (System.Windows.Controls.ScrollViewer)analyticsPage.FindName("ChartScroll");
        Check(graphScroll.ScrollableHeight > 0 && graphScroll.ActualHeight < 600, "graph has bounded scroll viewport in navigation host");
        var chartViews = VisualChildren<ShinroKensakuDesktop.Views.Controls.SeriesChartView>(analyticsPage).ToList();
        Check(chartViews.Count == 2 && chartViews.All(x => x.Chart.Visibility == Visibility.Visible), "two line charts rendered with data");
        Check(chartViews[0].Chart.Series.Count() == 3 && chartViews.All(v => v.Chart.Series.All(x => x is LiveChartsCore.SkiaSharpView.LineSeries<double>)), "line series built from data");
        Check(chartViews[0].Legend.Children.Count == 3 && System.Windows.Automation.AutomationProperties.GetName((UIElement)chartViews[0].Legend.Children[1]) == "進学 15", "legend sits above the plot with each latest value");
        Check(VisualChildren<System.Windows.Controls.ItemsControl>(analyticsPage).Any(x => x.Name == "MethodBreakdownList" && x.Items.Count == 2), "method counts listed under their kinds");
        Check(System.Windows.Automation.AutomationProperties.GetName(chartViews[0]).Contains("2026年度 20件"), "chart exposes its numbers to screen readers");
        graphScroll.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, -120) { RoutedEvent = System.Windows.Input.Mouse.MouseWheelEvent });
        host.UpdateLayout();
        Check(graphScroll.VerticalOffset > 0, "mouse wheel scrolls graph page");
        graphScroll.ScrollToBottom();
        await Task.Delay(100);
        host.UpdateLayout();
        var lastCard = (FrameworkElement)analyticsPage.FindName("MethodBreakdownCard");
        var bottom = lastCard.TransformToAncestor(graphScroll).Transform(new Point(0, lastCard.ActualHeight));
        Check(Math.Abs(graphScroll.VerticalOffset - graphScroll.ScrollableHeight) < 1 && bottom.Y <= graphScroll.ActualHeight && bottom.Y > 0, "last card reachable at page bottom");
        graphScroll.ScrollToTop();
        navigation.Navigate(typeof(ComparisonPage));
        await Task.Delay(100);
        host.UpdateLayout();
        Check(((System.Windows.Controls.ScrollViewer)comparisonPage.Content!).ActualHeight < 600, "comparison viewport bounded");
        navigation.Navigate(typeof(AnalyticsPage));
        await Task.Delay(100);
        host.UpdateLayout();
        Check(graphScroll.ScrollableHeight > 0, "graph scroll survives page navigation");
        navigation.Navigate(typeof(SearchPage));
        if (args.Contains("--render"))
        {
            foreach (var (page, name) in new[] { (dashboardPage as System.Windows.Controls.Page, "dashboard"), (analyticsPage, "analytics"), (comparisonPage, "comparison") })
            {
                var body = (FrameworkElement)page.Content!;
                page.Content = null;
                System.Windows.Data.BindingOperations.ClearBinding(body, FrameworkElement.HeightProperty);
                body.DataContext = page.DataContext;
                var content = new System.Windows.Controls.Border { Background = System.Windows.Media.Brushes.White, Child = body };
                content.Measure(new Size(1100, 1400));
                content.Arrange(new Rect(0, 0, 1100, 1400));
                content.UpdateLayout();
                await Task.Delay(300);
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(1100, 1400, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(content);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using var file = System.IO.File.Create(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kaken-{name}-preview.png"));
                encoder.Save(file);
                Console.WriteLine($"{name} preview rendered.");
            }
        }
        navigation.Navigate(typeof(SearchPage));
        search.SearchResultList = new(Enumerable.Range(0, 10000).Select(i => new SearchResultData { Year = 2026, Shinro_name = $"Test {i}" }));
        host.Width = 900;
        host.Height = 600;
        control.Measure(new Size(850, 500));
        control.Arrange(new Rect(0, 0, 850, 500));
        await Task.Delay(100);
        host.UpdateLayout();
        var resultGrid = (System.Windows.Controls.DataGrid)searchPage.FindName("ResultGrid");
        var searchLayout = (FrameworkElement)searchPage.FindName("SearchLayout");
        await Task.Delay(300);
        host.UpdateLayout();
        await host.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        host.UpdateLayout();
        var realized = VisualChildren<System.Windows.Controls.DataGridRow>(resultGrid).Count();
        Check(realized > 0 && realized < 100, "10000 records realize fewer than 100 rows");
        Check(searchLayout.ActualHeight < 600 && resultGrid.ActualHeight > 100, "search layout fits small viewport");
        var outerScroll = VisualChildren<Wpf.Ui.Controls.DynamicScrollViewer>(control).First();
        Check(outerScroll.ScrollableHeight < 1, "no outer page scrolling around result grid");
        var gridScroll = VisualChildren<System.Windows.Controls.ScrollViewer>(resultGrid).First();
        Check(gridScroll.ScrollableHeight > 0 && gridScroll.ScrollableWidth > 0, "result grid scrolls vertically and horizontally");
        gridScroll.ScrollToBottom();
        await Task.Delay(100);
        host.UpdateLayout();
        Check(gridScroll.VerticalOffset > 0 && VisualChildren<System.Windows.Controls.DataGridRow>(resultGrid).Count() < 100, "scrolling recycles result rows");
        gridScroll.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, 120) { RoutedEvent = System.Windows.Input.Mouse.MouseWheelEvent });
        host.UpdateLayout();
        Check(gridScroll.VerticalOffset < gridScroll.ScrollableHeight && outerScroll.VerticalOffset == 0, "wheel scrolls only results");
        Check(VisualChildren<System.Windows.Controls.ComboBox>(searchPage).Count() == 4 && VisualChildren<System.Windows.Controls.Expander>(searchPage).Count() == 0, "search conditions are always visible");
        Check(resultGrid.HeadersVisibility == System.Windows.Controls.DataGridHeadersVisibility.Column && resultGrid.Columns.Count == 13 && resultGrid.Columns.Count(x => x.Visibility != Visibility.Visible) == 2, "result headers line up with cells and optional columns start hidden");
        gridScroll.ScrollToBottom();
        await Task.Delay(100);
        host.UpdateLayout();
        await Task.Delay(200);
        host.UpdateLayout();
        Check(resultGrid.ItemContainerGenerator.ContainerFromIndex(9999) != null, "last of 10000 records reachable");
        Console.WriteLine($"Realized {realized} rows for 10000 records.");
        if (args.Contains("--render"))
        {
            gridScroll.ScrollToTop();
            host.UpdateLayout();
            var preview = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(control.ActualWidth), (int)Math.Ceiling(control.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            preview.Render(control);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(preview));
            using var file = System.IO.File.Create(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kaken-search-preview.png"));
            encoder.Save(file);
        }
        host.Close();
        await ReviewUiChecks.RunAsync(Check, args.Contains("--render"));
        Check(bindingErrors.Count == 0, "WPF pages have no binding errors");
        Console.WriteLine($"Passed {checks} checks.");
    }
}

internal sealed class BindingErrors : System.Diagnostics.TraceListener
{
    public int Count { get; private set; }
    public override void Write(string? message) { }
    public override void WriteLine(string? message) { }
    public override void TraceEvent(System.Diagnostics.TraceEventCache? eventCache, string source,
        System.Diagnostics.TraceEventType eventType, int id, string? message)
    {
        if (eventType is System.Diagnostics.TraceEventType.Error or System.Diagnostics.TraceEventType.Critical) Count++;
    }
    public override void TraceEvent(System.Diagnostics.TraceEventCache? eventCache, string source,
        System.Diagnostics.TraceEventType eventType, int id, string? format, params object?[]? args) =>
        TraceEvent(eventCache, source, eventType, id, format);
}

internal sealed class TestPageProvider : Wpf.Ui.Abstractions.INavigationViewPageProvider
{
    public Dictionary<Type, object> Pages { get; } = [];
    public SearchPage? Page { get; set; }
    public object? GetPage(Type pageType) => pageType == typeof(SearchPage) ? Page : Pages.GetValueOrDefault(pageType);
}
