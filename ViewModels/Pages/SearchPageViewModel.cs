using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Models.Data;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace ShinroKensakuDesktop.ViewModels.Pages;

public partial class SearchPageViewModel : ObservableObject
{
    private readonly Func<SearchCriteria, CancellationToken, Task<List<SearchResultData>>> loadResults;
    private readonly Func<Task<List<ExamMethodData>>> loadMethods;
    private readonly Func<Task<List<DepartmentData>>> loadDepartments;
    private readonly SemaphoreSlim methodGate = new(1, 1);
    private readonly SemaphoreSlim departmentGate = new(1, 1);
    private bool methodsLoaded, departmentsLoaded;
    private int conditionsVersion = DatabaseSettings.Version;
    private List<SearchResultData>? searchResults;
    private SearchCriteria? lastCriteria;
    private int lastConnectionVersion;

    public SearchPageViewModel(
        Func<SearchCriteria, CancellationToken, Task<List<SearchResultData>>>? load = null,
        Func<Task<List<ExamMethodData>>>? methods = null,
        Func<Task<List<DepartmentData>>>? departments = null)
    {
        loadResults = load ?? SearchPageDataGridSourceProvider.GetDataGridSource;
        loadMethods = methods ?? SearchPageComboBoxSourceProvider.GetExamMethodComboBoxSource;
        loadDepartments = departments ?? SearchPageComboBoxSourceProvider.GetDepartmentComboBoxSource;
    }

    [ObservableProperty] public partial string StatusText { get; set; } = "条件を指定して検索してください。";
    [ObservableProperty] public partial string ConditionStatusText { get; set; } = "";
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial bool IsExporting { get; set; }
    [ObservableProperty] public partial bool IsLoadingConditions { get; set; }
    [ObservableProperty] public partial string CurrentSearchQuery { get; set; } = "すべての受験記録";
    [ObservableProperty] public partial string LastSearchSummary { get; set; } = "まだ検索していません";
    [ObservableProperty] public partial ExamMethodData? ExamMethodComboBoxSelectedItem { get; set; }
    [ObservableProperty] public partial DepartmentData? DepartmentComboBoxSelectedItem { get; set; }
    [ObservableProperty] public partial string? SearchTargetName { get; set; }
    [ObservableProperty] public partial string? SelectedYear { get; set; }
    [ObservableProperty] public partial ObservableCollection<SearchResultData> SearchResultList { get; set; } = new();
    [ObservableProperty] public partial string SearchResultCountText { get; set; } = "0件";

    public ObservableCollection<ExamMethodData> ExamMethodComboBoxItems { get; } = [];
    public ObservableCollection<DepartmentData> DepartmentComboBoxItems { get; } = [];
    public bool IsLoading => IsBusy || IsLoadingConditions || IsExporting;
    public bool IsSearchDirty
    {
        get
        {
            if (lastCriteria == null) return false;
            if (lastConnectionVersion != DatabaseSettings.Version) return true;
            try { return CurrentCriteria() != lastCriteria; }
            catch (ArgumentException) { return true; }
        }
    }
    public bool HasNoResults => !IsBusy && SearchResultList.Count == 0;
    public string EmptyStateText => lastCriteria == null ? "条件を指定して検索してください。" : "条件に一致する受験記録はありません。";
    public bool CanEditConditions => !IsBusy && !IsExporting;

    private SearchCriteria CurrentCriteria() => SearchCriteria.Create(SearchTargetName, SelectedYear,
        ExamMethodComboBoxSelectedItem?.Id, DepartmentComboBoxSelectedItem?.Id);

    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnIsExportingChanged(bool value) => NotifyState();
    partial void OnIsLoadingConditionsChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLoading));
        ReloadConditionsCommand.NotifyCanExecuteChanged();
    }
    partial void OnExamMethodComboBoxSelectedItemChanged(ExamMethodData? value) => UpdateSearchQuery();
    partial void OnDepartmentComboBoxSelectedItemChanged(DepartmentData? value) => UpdateSearchQuery();
    partial void OnSelectedYearChanged(string? value) => UpdateSearchQuery();
    partial void OnSearchTargetNameChanged(string? value) => UpdateSearchQuery();
    partial void OnSearchResultListChanged(ObservableCollection<SearchResultData> value) => OnPropertyChanged(nameof(HasNoResults));

    private void NotifyState()
    {
        ExportCsvCommand.NotifyCanExecuteChanged();
        ExecuteSearchCommand.NotifyCanExecuteChanged();
        ClearAllConditionsCommand.NotifyCanExecuteChanged();
        ReloadConditionsCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(CanEditConditions));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(IsSearchDirty));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    private void CheckConnectionVersion()
    {
        if (conditionsVersion == DatabaseSettings.Version) return;
        conditionsVersion = DatabaseSettings.Version;
        methodsLoaded = departmentsLoaded = false;
        ExamMethodComboBoxItems.Clear();
        DepartmentComboBoxItems.Clear();
        ExamMethodComboBoxSelectedItem = null;
        DepartmentComboBoxSelectedItem = null;
        NotifyState();
    }

    [RelayCommand]
    public async Task LoadConditionsAsync()
    {
        IsLoadingConditions = true;
        ConditionStatusText = "";
        try
        {
            CheckConnectionVersion();
            await Task.WhenAll(LoadExamMethodComboBoxItems(), LoadDepartmentComboBoxItems());
        }
        catch (Exception)
        {
            ConditionStatusText = "検索条件を取得できませんでした。設定の接続を確認し、条件を再読み込みしてください。";
        }
        finally { IsLoadingConditions = false; }
    }

    private bool CanReloadConditions() => !IsLoadingConditions && CanEditConditions;

    [RelayCommand(CanExecute = nameof(CanReloadConditions))]
    public async Task ReloadConditionsAsync()
    {
        if (!CanReloadConditions()) return;
        methodsLoaded = departmentsLoaded = false;
        await LoadConditionsAsync();
    }

    public async Task LoadExamMethodComboBoxItems()
    {
        await methodGate.WaitAsync();
        try
        {
            if (methodsLoaded) return;
            var items = await loadMethods();
            string? selectedId = ExamMethodComboBoxSelectedItem?.Id;
            ExamMethodComboBoxItems.Clear();
            foreach (var item in items) ExamMethodComboBoxItems.Add(item);
            ExamMethodComboBoxSelectedItem = items.FirstOrDefault(x => x.Id == selectedId);
            methodsLoaded = true;
        }
        finally { methodGate.Release(); }
    }

    public async Task LoadDepartmentComboBoxItems()
    {
        await departmentGate.WaitAsync();
        try
        {
            if (departmentsLoaded) return;
            var items = await loadDepartments();
            sbyte? selectedId = DepartmentComboBoxSelectedItem?.Id;
            DepartmentComboBoxItems.Clear();
            foreach (var item in items) DepartmentComboBoxItems.Add(item);
            DepartmentComboBoxSelectedItem = items.FirstOrDefault(x => x.Id == selectedId);
            departmentsLoaded = true;
        }
        finally { departmentGate.Release(); }
    }

    [RelayCommand] public void ClearExamMethodComboBoxSelectedItem() => ExamMethodComboBoxSelectedItem = null;
    [RelayCommand] public void ClearDepartmentComboBoxSelectedItem() => DepartmentComboBoxSelectedItem = null;
    [RelayCommand] public void ClearSelectedYear() => SelectedYear = null;

    [RelayCommand(CanExecute = nameof(CanSearch))]
    public void ClearAllConditions()
    {
        SearchTargetName = null;
        SelectedYear = null;
        ExamMethodComboBoxSelectedItem = null;
        DepartmentComboBoxSelectedItem = null;
    }

    private bool CanSearch() => !IsBusy && !IsExporting;

    [RelayCommand(CanExecute = nameof(CanSearch), IncludeCancelCommand = true)]
    public async Task ExecuteSearch(CancellationToken cancellationToken)
    {
        if (!CanSearch()) return;
        SearchCriteria criteria;
        try { criteria = CurrentCriteria(); }
        catch (ArgumentException)
        {
            StatusText = "年度は1901～2155の数字で入力してください。";
            return;
        }
        string summary = CurrentSearchQuery;
        int connectionVersion = DatabaseSettings.Version;
        IsBusy = true;
        searchResults = null;
        lastCriteria = null;
        LastSearchSummary = "検索しています…";
        ShowResults();
        StatusText = "検索しています…";
        try
        {
            var loaded = await loadResults(criteria, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            searchResults = loaded;
            lastCriteria = criteria;
            lastConnectionVersion = connectionVersion;
            LastSearchSummary = "表示中：" + summary;
            ShowResults();
            StatusText = loaded.Count == 0 ? "条件に一致する受験記録はありません。" : "検索完了";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LastSearchSummary = "検索を中断しました";
            StatusText = "検索を中断しました。条件を指定して再検索できます。";
        }
        catch (Exception)
        {
            LastSearchSummary = "検索結果を取得できませんでした";
            StatusText = "検索できませんでした。設定画面で接続を確認し、再検索してください。";
        }
        finally { IsBusy = false; }
    }

    private bool CanExportCsv() => !IsBusy && !IsExporting && !IsSearchDirty && searchResults is { Count: > 0 };

    [RelayCommand(CanExecute = nameof(CanExportCsv))]
    private async Task ExportCsvAsync()
    {
        if (!CanExportCsv()) return;
        SearchResultData[] snapshot = searchResults!.ToArray();
        SaveFileDialog dialog = new()
        {
            Filter = "CSVファイル (*.csv)|*.csv", DefaultExt = ".csv", AddExtension = true,
            FileName = $"進路検索結果_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dialog.ShowDialog() != true) return;
        IsExporting = true;
        try
        {
            await Task.Run(async () =>
            {
                string temporary = dialog.FileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await File.WriteAllTextAsync(temporary, SearchResultCsv.Create(snapshot), new UTF8Encoding(true));
                    File.Move(temporary, dialog.FileName, overwrite: true);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            });
            StatusText = $"検索結果{snapshot.Length:N0}件をCSVに保存しました。";
        }
        catch (Exception)
        {
            StatusText = "CSVを保存できませんでした。ファイルが開かれていないか、保存先を確認してください。";
        }
        finally { IsExporting = false; }
    }

    private void ShowResults()
    {
        SearchResultList = new(searchResults ?? []);
        SearchResultCountText = $"{SearchResultList.Count:N0}件";
        NotifyState();
    }

    private void UpdateSearchQuery()
    {
        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(SearchTargetName)) parts.Add($"進路先：{SearchTargetName.Trim()}");
        if (!string.IsNullOrWhiteSpace(SelectedYear)) parts.Add($"年度：{SelectedYear.Trim()}年");
        if (ExamMethodComboBoxSelectedItem != null) parts.Add($"受験方法：{ExamMethodComboBoxSelectedItem.Name}");
        if (DepartmentComboBoxSelectedItem != null) parts.Add($"クラス：{DepartmentComboBoxSelectedItem.Name}");
        CurrentSearchQuery = parts.Count == 0 ? "すべての受験記録" : string.Join(" ／ ", parts);
        OnPropertyChanged(nameof(IsSearchDirty));
        ExportCsvCommand.NotifyCanExecuteChanged();
    }
}
