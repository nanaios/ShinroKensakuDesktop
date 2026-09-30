using System.Data;
using System.IO;
using MySql.Data.MySqlClient;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Models.Data;
using ShinroKensakuDesktop.ViewModels.Pages;
using Wpf.Ui.Appearance;

internal static class ReviewChecks
{
    public static async Task RunAsync(Action<bool, string> check, bool database)
    {
        check(SearchCriteria.Create("  学校  ", " 2026 ", " A ", null) == new SearchCriteria("学校", 2026, "A", null), "search normalizes surrounding spaces");
        check(SearchCriteria.Create("  ", "  ", null, null) == new SearchCriteria(null, null, null, null), "blank conditions do not filter data");
        foreach (string year in new[] { "1900", "2156", "+2026", "２０２６", "2026 OR 1=1", "2147483648" })
        {
            bool rejected = false;
            try { SearchCriteria.Create(null, year, null, null); }
            catch (ArgumentException) { rejected = true; }
            check(rejected, "invalid year rejected before query: " + year);
        }
        check(SearchCriteria.EscapeLike("100%_!高校") == "100!%!_!!高校", "LIKE metacharacters are literal");
        check(SearchPageDataGridSourceProvider.FormatResult(null) == "未確定", "null object result is undecided");
        DataTable fixture = new();
        foreach (var name in new[] { "year", "jyukenbi", "shinro_name", "gakubu", "gakka", "course", "syuusyokusakinai_kubun", "jyukenhouhou_name", "g_name", "sei", "cls_name", "gouhi" })
            fixture.Columns.Add(name, typeof(object));
        DataRow row = fixture.NewRow();
        row["year"] = 2026;
        var mapped = SearchPageDataGridSourceProvider.MapRow(row);
        check(mapped.Year == 2026 && mapped.Jyukenbi == null && mapped.Result == "未確定", "row mapping accepts null date and numeric year types");
        row["year"] = DBNull.Value;
        check(SearchPageDataGridSourceProvider.MapRow(row).Year == null, "missing year is not fabricated");
        check(!SearchResultCsv.Create([mapped]).Contains("0001/01/01"), "CSV preserves missing dates");
        var input = new System.Windows.Controls.TextBox { MaxLength = 4 };
        var numeric = new ShinroKensakuDesktop.Views.Behaviors.NumericOnlyBehavior();
        numeric.Attach(input);
        var paste = new System.Windows.DataObjectPastingEventArgs(new System.Windows.DataObject(System.Windows.DataFormats.Text, "20261234"), false, System.Windows.DataFormats.Text)
            { RoutedEvent = System.Windows.DataObject.PastingEvent };
        input.RaiseEvent(paste);
        check(paste.CommandCancelled, "oversized year paste is rejected instead of truncated");
        numeric.Detach();

        TaskCompletionSource<List<SearchResultData>> completion = new();
        SearchCriteria? captured = null;
        var search = new SearchPageViewModel((criteria, token) => { captured = criteria; return completion.Task.WaitAsync(token); });
        search.SelectedYear = "2026";
        search.SearchTargetName = "大学";
        var running = search.ExecuteSearchCommand.ExecuteAsync(null);
        check(search.IsBusy && !search.CanEditConditions && !search.ExportCsvCommand.CanExecute(null), "pending search locks controls and export");
        search.SearchTargetName = "専門学校";
        completion.SetResult([new() { Year = 2026, Shinro_name = "fixture" }]);
        await running;
        check(captured?.Target == "大学" && search.LastSearchSummary.Contains("大学") && search.IsSearchDirty, "pending search keeps its captured criteria");
        check(!search.ExportCsvCommand.CanExecute(null), "changed conditions disable mismatched CSV export");
        search.SearchTargetName = "大学";
        check(!search.IsSearchDirty && search.ExportCsvCommand.CanExecute(null), "restoring matching conditions enables export");
        search.ClearAllConditionsCommand.Execute(null);
        check(search.SelectedYear == null && search.SearchTargetName == null && search.IsSearchDirty, "clear all conditions marks existing results stale");
        search.SelectedYear = "invalid";
        await search.ExecuteSearchCommand.ExecuteAsync(null);
        check(search.SearchResultList.Count == 1 && !search.ExportCsvCommand.CanExecute(null), "invalid input preserves identified results without exporting them");

        var cancelSearch = new SearchPageViewModel(async (_, token) => { await Task.Delay(5000, token); return []; });
        running = cancelSearch.ExecuteSearchCommand.ExecuteAsync(null);
        cancelSearch.ExecuteSearchCancelCommand.Execute(null);
        await running;
        check(!cancelSearch.IsBusy && cancelSearch.StatusText.Contains("中断") && cancelSearch.SearchResultList.Count == 0, "cancel search clears busy state and results");
        var failingSearch = new SearchPageViewModel((_, _) => Task.FromException<List<SearchResultData>>(new IOException("private error details")));
        await failingSearch.ExecuteSearchCommand.ExecuteAsync(null);
        check(!failingSearch.IsBusy && !failingSearch.StatusText.Contains("private") && !failingSearch.ExportCsvCommand.CanExecute(null), "search failures show no internal error details");

        int methodCalls = 0;
        TaskCompletionSource<List<ExamMethodData>> methodCompletion = new();
        var lookups = new SearchPageViewModel(methods: () => { methodCalls++; return methodCompletion.Task; }, departments: () => Task.FromResult(new List<DepartmentData>()));
        var first = lookups.LoadExamMethodComboBoxItems();
        var second = lookups.LoadExamMethodComboBoxItems();
        methodCompletion.SetResult([new("A", "一般"), new("B", "推薦")]);
        await Task.WhenAll(first, second);
        check(methodCalls == 1 && lookups.ExamMethodComboBoxItems.Count == 2, "concurrent lookup loading does not duplicate choices");
        lookups.ExamMethodComboBoxSelectedItem = lookups.ExamMethodComboBoxItems[0];
        await lookups.ReloadConditionsCommand.ExecuteAsync(null);
        check(methodCalls == 2 && lookups.ExamMethodComboBoxSelectedItem?.Id == "A", "lookup reload fetches again and preserves selection");
        methodCompletion = new();
        var refreshing = lookups.ReloadConditionsCommand.ExecuteAsync(null);
        lookups.ExamMethodComboBoxSelectedItem = lookups.ExamMethodComboBoxItems[1];
        methodCompletion.SetResult([new("A", "一般"), new("B", "推薦")]);
        await refreshing;
        check(lookups.ExamMethodComboBoxSelectedItem?.Id == "B", "lookup refresh preserves selection made while loading");
        int emptyCalls = 0;
        var emptyLookups = new SearchPageViewModel(methods: () => { emptyCalls++; return Task.FromResult(new List<ExamMethodData>()); });
        await emptyLookups.LoadExamMethodComboBoxItems();
        await emptyLookups.LoadExamMethodComboBoxItems();
        check(emptyCalls == 1, "empty lookup results are cached");
        var failingLookups = new SearchPageViewModel(methods: () => Task.FromException<List<ExamMethodData>>(new IOException()), departments: () => Task.FromResult(new List<DepartmentData>()));
        await failingLookups.LoadConditionsCommand.ExecuteAsync(null);
        check(!failingLookups.IsLoadingConditions && failingLookups.ConditionStatusText.Contains("再読み込み") && failingLookups.StatusText.Contains("条件を指定"), "lookup errors do not overwrite search status");

        string directory = Path.Combine(Path.GetTempPath(), "kaken-review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string encrypted = Path.Combine(directory, "database.bin");
            const string example = "Server=localhost;Database=review;User ID=review;Password=test-only";
            DatabaseSettings.WriteProtected(encrypted, example);
            check(new MySqlConnectionStringBuilder(DatabaseSettings.ReadProtected(encrypted)).Password == "test-only", "encrypted connection settings round trip");
            check(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(encrypted)).Contains("test-only"), "stored connection has no plaintext password");
            bool missing = false;
            try { DatabaseSettings.ReadProtected(Path.Combine(directory, "missing.bin")); }
            catch (InvalidOperationException) { missing = true; }
            check(missing, "missing config requires explicit setup");
            string? original = Environment.GetEnvironmentVariable(DatabaseSettings.EnvironmentVariable);
            try
            {
                Environment.SetEnvironmentVariable(DatabaseSettings.EnvironmentVariable, example);
                check(DatabaseSettings.UsesEnvironment && new MySqlConnectionStringBuilder(DatabaseSettings.LoadConnectionString()).Database == "review", "environment overrides local database config");
            }
            finally { Environment.SetEnvironmentVariable(DatabaseSettings.EnvironmentVariable, original); }

            string preferences = Path.Combine(directory, "preferences.json");
            check(UserPreferences.LoadTheme(preferences) == ApplicationTheme.Unknown, "new users default to system theme");
            File.WriteAllText(preferences, "invalid json");
            check(UserPreferences.LoadTheme(preferences) == ApplicationTheme.Unknown, "corrupt theme preference has a safe default");
            foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Unknown })
            {
                UserPreferences.SaveTheme(theme, preferences);
                check(UserPreferences.LoadTheme(preferences) == theme, "theme preference round trip: " + theme);
            }
            using var settings = new SettingsPageViewModel(preferences);
            settings.CurrentApplicationTheme = ApplicationTheme.Light;
            settings.CurrentApplicationTheme = ApplicationTheme.Dark;
            check(ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark && UserPreferences.LoadTheme(preferences) == ApplicationTheme.Dark, "selected theme applies and persists");
            settings.CurrentApplicationTheme = ApplicationTheme.Unknown;
            check(settings.CurrentApplicationTheme == ApplicationTheme.Unknown && UserPreferences.LoadTheme(preferences) == ApplicationTheme.Unknown, "system theme remains a user preference without recursion");
            settings.DatabasePort = "0";
            check(!await settings.SaveDatabaseAsync("") && settings.DatabaseStatusText.Contains("65535"), "database settings reject invalid port before connecting");
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
        }
        finally { Directory.Delete(directory, recursive: true); }

        if (!database) return;
        var match = await MySQLCommand.Query("SELECT @value LIKE @pattern ESCAPE '!' AS matched",
            new MySqlParameter("@value", "100%_!高校"), new MySqlParameter("@pattern", "%" + SearchCriteria.EscapeLike("100%_!") + "%"));
        check(Convert.ToInt32(match.Rows[0][0]) == 1, "MySQL literal pattern matches percent underscore and escape character");
        var noMatch = await MySQLCommand.Query("SELECT @value LIKE @pattern ESCAPE '!' AS matched",
            new MySqlParameter("@value", "100AB高校"), new MySqlParameter("@pattern", "%" + SearchCriteria.EscapeLike("100%_") + "%"));
        check(Convert.ToInt32(noMatch.Rows[0][0]) == 0, "MySQL wildcard characters cannot broaden literal search");
        int ticks = 0;
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        timer.Tick += (_, _) => ticks++;
        timer.Start();
        await MySQLCommand.Query("SELECT SLEEP(0.4) AS wait_result");
        timer.Stop();
        check(ticks > 2, "database reading leaves WPF dispatcher responsive");
        using var cancellation = new CancellationTokenSource(100);
        bool cancelled = false;
        try { await MySQLCommand.Query("SELECT SLEEP(2) AS wait_result", cancellation.Token); }
        catch (OperationCanceledException) { cancelled = true; }
        check(cancelled, "database query observes cancellation");
        var health = await MySQLCommand.Query("SELECT 1 AS value");
        check(Convert.ToInt32(health.Rows[0][0]) == 1, "database remains usable after cancellation");
    }
}
