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
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine($"PASS {name}");
    }

    [STAThread]
    private static int Main(string[] args)
    {
        var app = new App();
        app.InitializeComponent();
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

    private static async Task RunAsync(string[] args)
    {
        Check(SearchPageDataGridSourceProvider.FormatResult(true) == "合格", "boolean pass result");
        Check(SearchPageDataGridSourceProvider.FormatResult(0) == "不合格", "numeric fail result");
        Check(SearchPageDataGridSourceProvider.FormatResult(DBNull.Value) == "未確定", "null result");
        var csv = SearchResultCsv.Create([new SearchResultData { Year = 2026, Jyukenbi = new(2026, 9, 29), Shinro_name = "日本語,\"大学\"\r\n学部", G_name = " =1+1" }]);
        Check(csv.Contains("\"日本語,\"\"大学\"\"\r\n学部\""), "CSV quotes commas and newlines");
        Check(csv.Contains("\"' =1+1\""), "CSV formula neutralization");
        Check(SearchResultCsv.Create([]).Split("\r\n").Length == 2, "CSV empty header");
        var search = new SearchPageViewModel();
        search.FirstPage();
        Check(search.SearchResultPageIndexText == "ページ 0/0" && !search.ExportCsvCommand.CanExecute(null), "empty paging and export");
        search.SelectedYear = "invalid";
        await search.ExecuteSearchCommand.ExecuteAsync(null);
        Check(search.StatusText.Contains("1901") && !search.IsBusy, "invalid year handled");
        var provider = new TestPageProvider();
        var navigation = new NavigationService(provider);
        var dashboard = new DashboardPageViewModel(navigation, search);
        var dashboardPage = new DashBoardPage(dashboard);
        var searchPage = new SearchPage(search);
        provider.Page = searchPage;
        var control = new Wpf.Ui.Controls.NavigationView();
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
            Check(!dashboard.IsBusy && dashboard.StatusText.Contains("最終更新"), "dashboard live refresh");
            Check(dashboard.SelectedYear == groups.Max(x => x.Year), "latest registered year selected");
            search.SelectedYear = dashboard.SelectedYear.ToString();
            search.ResultVisibleCountLimit = 0;
            await search.ExecuteSearchCommand.ExecuteAsync(null);
            Check(search.ResultVisibleCountLimit == 1 && search.SearchResultList.Count == 1, "zero page size clamped");
            var selected = await SearchPageDataGridSourceProvider.GetDataGridSource(null, search.SelectedYear, null, null);
            Check(selected.Count == groups.Where(x => x.Year == dashboard.SelectedYear).Sum(x => x.Count), "dashboard year agrees with search");
            var fullCsv = SearchResultCsv.Create(selected);
            Check(fullCsv.Split("\r\n").Length == selected.Count + 2, "CSV includes all result pages");
            search.NextPage();
            Check(search.SearchResultPageIndexText!.StartsWith("ページ 2/"), "next page");
            search.SearchTargetName = "' OR 1=1 -- no such school";
            await search.ExecuteSearchCommand.ExecuteAsync(null);
            Check(search.SearchResultList.Count == 0 && search.SearchResultPageIndexText == "ページ 0/0", "quoted input safe and stale page cleared");
            Check(!search.ExportCsvCommand.CanExecute(null), "empty search disables export");
            await search.LoadExamMethodComboBoxItems();
            var methods = search.ExamMethodComboBoxItems.Count;
            await search.LoadExamMethodComboBoxItems();
            Check(methods == search.ExamMethodComboBoxItems.Count, "lookup loading is idempotent");
            await dashboard.OpenSearchCommand.ExecuteAsync(null);
            Check(search.SearchTargetName == null && search.SearchResultList.Count > 0 && search.SelectedYear == dashboard.SelectedYear.ToString(), "dashboard navigates and searches selected year");
            Console.WriteLine($"Verified {groups.Sum(x => x.Count)} records across {dashboard.Years.Count} years (no personal data logged).");
        }
                if (args.Contains("--render"))
        {
            var body = (FrameworkElement)dashboardPage.Content!;
            dashboardPage.Content = null;
            body.DataContext = dashboard;
            var previewHost = new System.Windows.Controls.Border { Background = System.Windows.Media.Brushes.White, Child = body };
            var content = (FrameworkElement)previewHost;
            content.Measure(new Size(1100, 950));
            content.Arrange(new Rect(0, 0, 1100, 950));
            content.UpdateLayout();
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(1100, 950, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            await Task.Delay(300);
            bitmap.Render(content);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using var file = System.IO.File.Create(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kaken-dashboard-preview.png"));
            encoder.Save(file);
            Console.WriteLine("Dashboard preview rendered.");
        }
        host.Close();
        Console.WriteLine($"Passed {checks} checks.");
    }
}

internal sealed class TestPageProvider : Wpf.Ui.Abstractions.INavigationViewPageProvider
{
    public SearchPage? Page { get; set; }
    public object? GetPage(Type pageType) => pageType == typeof(SearchPage) ? Page : null;
}


