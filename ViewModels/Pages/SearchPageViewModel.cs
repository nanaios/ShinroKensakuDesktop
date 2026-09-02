using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Models.Data;
using System.Collections.ObjectModel;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	public partial class SearchPageViewModel : ObservableObject
	{
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
			var results = await SearchPageDataGridSourceProvider.GetDataGridSource(SearchTargetName, SelectedYear, ExamMethodComboBoxSelectedItem?.Id, DepartmentComboBoxSelectedItem?.Id );

			foreach ( var result in results )
			{
				SearchResultList.Add ( result );
			}
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