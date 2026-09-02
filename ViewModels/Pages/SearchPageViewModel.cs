using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Models.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	public partial class SearchPageViewModel : ObservableObject
	{
		private List<SearchResultData>? searchResults;
		private int pageIndex;
		private int pageCount;

		[ObservableProperty]
		public partial string? CurrentSearchQuery { get; set; } = null;
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
			var items = await SearchPageComboBoxSourceProvider.GetExamMethodComboBoxSource();
			foreach ( var item in items )
			{
				ExamMethodComboBoxItems.Add ( item );
			}
		}

		public async Task LoadDepartmentComboBoxItems ( )
		{
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
		public async Task ExecuteSearch ( )
		{
			SearchResultList.Clear ( );
			Debug.WriteLine ( $"SearchTargetName: {SearchTargetName}, SelectedYear: {SelectedYear}, ExamMethodComboBoxSelectedItem?.Id: {ExamMethodComboBoxSelectedItem?.Id}, DepartmentComboBoxSelectedItem?.Id: {DepartmentComboBoxSelectedItem?.Id}" );
			searchResults = await SearchPageDataGridSourceProvider.GetDataGridSource ( SearchTargetName, SelectedYear, ExamMethodComboBoxSelectedItem?.Id, DepartmentComboBoxSelectedItem?.Id );

			var count = Math.Min ( ResultVisibleCountLimit, searchResults.Count );
			pageIndex = 0;
			pageCount = ( int ) Math.Ceiling ( ( double ) searchResults.Count / ResultVisibleCountLimit );

			for ( int i = 0 ; i < count ; i++ )
			{
				SearchResultList.Add ( searchResults [ i ] );
			}
			SearchResultCountText = $"{count}/{searchResults.Count}件を表示中";
			SearchResultPageIndexText = $"ページ {pageIndex + 1}/{pageCount}";
		}
		[RelayCommand]
		public void NextPage ( )
		{
			if ( searchResults == null || searchResults.Count == 0 ) return;
			if ( pageIndex + 1 >= pageCount ) return;
			pageIndex++;

			SearchResultList.Clear ( );
			var count = Math.Min ( ResultVisibleCountLimit, searchResults.Count - pageIndex * ResultVisibleCountLimit );
			for ( int i = 0 ; i < count ; i++ )
			{
				SearchResultList.Add ( searchResults [ pageIndex * ResultVisibleCountLimit + i ] );
			}
			SearchResultCountText = $"{count}/{searchResults.Count}件を表示中";
			SearchResultPageIndexText = $"ページ {pageIndex + 1}/{pageCount}";
		}
		[RelayCommand]
		public void PreviousPage ( )
		{
			if ( searchResults == null || searchResults.Count == 0 ) return;
			if ( pageIndex - 1 < 0 ) return;
			pageIndex--;

			SearchResultList.Clear ( );
			var count = Math.Min ( ResultVisibleCountLimit, searchResults.Count - pageIndex * ResultVisibleCountLimit );
			for ( int i = 0 ; i < count ; i++ )
			{
				SearchResultList.Add ( searchResults [ pageIndex * ResultVisibleCountLimit + i ] );
			}
			SearchResultCountText = $"{count}/{searchResults.Count}件を表示中";
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