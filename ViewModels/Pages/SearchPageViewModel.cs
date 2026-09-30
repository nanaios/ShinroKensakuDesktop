using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Models.Data;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	public partial class SearchPageViewModel : ObservableObject
	{
		private List<SearchResultData>? searchResults;
		private int pageIndex;
		private int pageCount;
        [ObservableProperty] public partial string StatusText { get; set; } = "条件を指定して検索してください。";
        [ObservableProperty] public partial bool IsBusy { get; set; }
        partial void OnIsBusyChanged(bool value) => ExportCsvCommand.NotifyCanExecuteChanged();

		[ObservableProperty]
		public partial string? CurrentSearchQuery { get; set; } = "なし";
		[ObservableProperty]
		public partial ExamMethodData? ExamMethodComboBoxSelectedItem { get; set; } = null;
		[ObservableProperty]
		public partial DepartmentData? DepartmentComboBoxSelectedItem { get; set; } = null;
		[ObservableProperty]
		public partial string? SearchTargetName { get; set; } = null;
		[ObservableProperty]
		public partial string? SelectedYear { get; set; } = null;
		[ObservableProperty]
		public partial ObservableCollection<SearchResultData> SearchResultList { get; set; } = new ( );
		[ObservableProperty]
		public partial string? SearchResultCountText { get; set; } = null;
		[ObservableProperty]
		public partial string? SearchResultPageIndexText { get; set; } = null;
		[ObservableProperty]
		public partial int ResultVisibleCountLimit { get; set; } = 50;

		public ObservableCollection<ExamMethodData> ExamMethodComboBoxItems { get; } = [ ];
		public ObservableCollection<DepartmentData> DepartmentComboBoxItems { get; } = [ ];

		partial void OnExamMethodComboBoxSelectedItemChanged ( ExamMethodData? oldValue, ExamMethodData? newValue )
		{
			UpdateSearchQuery ( );
		}
		partial void OnDepartmentComboBoxSelectedItemChanged ( DepartmentData? oldValue, DepartmentData? newValue )
		{
			UpdateSearchQuery ( );
		}
		partial void OnSelectedYearChanged ( string? oldValue, string? newValue )
		{
			UpdateSearchQuery ( );
		}

		public async Task LoadExamMethodComboBoxItems ( )
		{
			if (ExamMethodComboBoxItems.Count > 0) return;
            var items = await SearchPageComboBoxSourceProvider.GetExamMethodComboBoxSource();
			foreach ( var item in items )
			{
				ExamMethodComboBoxItems.Add ( item );
			}
		}

		public async Task LoadDepartmentComboBoxItems ( )
		{
			if (DepartmentComboBoxItems.Count > 0) return;
            var items = await SearchPageComboBoxSourceProvider.GetDepartmentComboBoxSource();
			foreach ( var item in items )
			{
				DepartmentComboBoxItems.Add ( item );
			}
		}

		[RelayCommand]
		public void ClearExamMethodComboBoxSelectedItem ( )
		{
			ExamMethodComboBoxSelectedItem = null;
		}
		[RelayCommand]
		public void ClearDepartmentComboBoxSelectedItem ( )
		{
			DepartmentComboBoxSelectedItem = null;
		}
		[RelayCommand]
		public void ClearSelectedYear ( )
		{
			SelectedYear = null;
		}
        [RelayCommand]
        public async Task ExecuteSearch()
        {
            IsBusy = true;
            searchResults = null;
            ShowPage();
            StatusText = "検索しています…";
            try
            {
                if (!string.IsNullOrWhiteSpace(SelectedYear) && (!int.TryParse(SelectedYear, out var year) || year < 1901 || year > 2155))
                {
                    StatusText = "年度は1901～2155の数字で入力してください。";
                    return;
                }
                searchResults = await SearchPageDataGridSourceProvider.GetDataGridSource(SearchTargetName, SelectedYear, ExamMethodComboBoxSelectedItem?.Id, DepartmentComboBoxSelectedItem?.Id);
                FirstPage();
                StatusText = searchResults.Count == 0 ? "条件に一致する受験記録はありません。" : $"検索結果：{searchResults.Count:N0}件。CSV保存は全ページが対象です。";
            }
            catch (Exception)
            {
                StatusText = "検索できませんでした。データベースの接続を確認して再検索してください。";
            }
            finally { IsBusy = false; }
        }

        private bool CanExportCsv() => !IsBusy && searchResults is { Count: > 0 };

        [RelayCommand(CanExecute = nameof(CanExportCsv))]
        private async Task ExportCsvAsync()
        {
            var snapshot = searchResults?.ToArray();
            if (snapshot is not { Length: > 0 }) return;
            var dialog = new SaveFileDialog
            {
                Filter = "CSVファイル (*.csv)|*.csv", DefaultExt = ".csv", AddExtension = true,
                FileName = $"進路検索結果_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                await File.WriteAllTextAsync(dialog.FileName, SearchResultCsv.Create(snapshot), new UTF8Encoding(true));
                StatusText = $"検索結果{snapshot.Length:N0}件をCSVに保存しました。";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                StatusText = "CSVを保存できませんでした。ファイルが開かれていないか、保存先を確認してください。";
            }
        }
		[RelayCommand]
		public void NextPage ( )
		{
			if ( pageIndex + 1 >= pageCount ) return;
			pageIndex++;
			ShowPage ( );
		}
		[RelayCommand]
		public void PreviousPage ( )
		{
			if ( pageIndex - 1 < 0 ) return;
			pageIndex--;
			ShowPage ( );
		}
		[RelayCommand]
		public void FirstPage ( )
		{
			pageIndex = 0;
			ShowPage ( );
		}
		partial void OnResultVisibleCountLimitChanged(int value) => FirstPage();
        private void ShowPage ( )
		{
            if (searchResults == null || searchResults.Count == 0)
            {
                SearchResultList = new();
                pageIndex = pageCount = 0;
                SearchResultCountText = "0件";
                SearchResultPageIndexText = "ページ 0/0";
                return;
            }
            var limit = Math.Clamp(ResultVisibleCountLimit, 1, 1000);
            if (ResultVisibleCountLimit != limit)
            {
                ResultVisibleCountLimit = limit;
                return; // The property change refreshes the page once with the normalized value.
            }
			// Replace the page in one notification instead of issuing one per record.
			pageCount = ( int ) Math.Ceiling ( ( double ) searchResults.Count / ResultVisibleCountLimit );
			pageIndex = Math.Clamp(pageIndex, 0, pageCount - 1);
            var count = Math.Min ( ResultVisibleCountLimit, searchResults.Count - pageIndex * ResultVisibleCountLimit );
			int start = pageIndex * ResultVisibleCountLimit;
            SearchResultList = new(searchResults.GetRange(start, count));
			SearchResultCountText = $"{start + 1}-{start + count}件目を表示中";
			SearchResultPageIndexText = $"ページ {pageIndex + 1}/{pageCount}";
		}
		private void UpdateSearchQuery ( )
		{
			string searchQuery = string.Empty;

			if ( ExamMethodComboBoxSelectedItem != null )
			{
				searchQuery += $" 受験方法: {ExamMethodComboBoxSelectedItem.Name}";
			}
			if ( DepartmentComboBoxSelectedItem != null )
			{
				searchQuery += $" 学科: {DepartmentComboBoxSelectedItem.Name} ( {DepartmentComboBoxSelectedItem.LongName} )";
			}
			if ( SelectedYear != null )
			{
				searchQuery += $" 年度: {SelectedYear}年";
			}

			CurrentSearchQuery = string.IsNullOrWhiteSpace ( searchQuery ) ? "なし" : searchQuery.Trim ( );
		}
	}
}
