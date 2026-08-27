using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShinroKensakuDesktop.Models;
using System.Collections.ObjectModel;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	public partial class SearchPageViewModel : ObservableObject
	{
		[ObservableProperty]
		public partial string? CurrentSearchQuery { get; set; } = null;
		[ObservableProperty]
		public partial string? ExamMethodComboBoxSelectedItem { get; set; } = null;
		[ObservableProperty]
		public partial string? DepartmentComboBoxSelectedItem { get; set; } = null;
		[ObservableProperty]
		public partial string? SearchTargetName { get; set; } = null;
		[ObservableProperty]
		public partial int? SelectedYear { get; set; } = null;

		public ObservableCollection<string> ExamMethodComboBoxItems { get; } = [ ];
		public ObservableCollection<string> DepartmentComboBoxItems { get; } = [ ];

		partial void OnExamMethodComboBoxSelectedItemChanged ( string? oldValue, string? newValue )
		{
			UpdateSearchQuery ( );
		}
		partial void OnDepartmentComboBoxSelectedItemChanged ( string? oldValue, string? newValue )
		{
			UpdateSearchQuery ( );
		}
		partial void OnSelectedYearChanged ( int? oldValue, int? newValue )
		{
			UpdateSearchQuery ( );
		}

		public async Task LoadExamMethodComboBoxItems ( )
		{
			var items = await SearchPageComboBoxSourceProvider.GetExamMethodComboBoxSource();
			foreach ( var item in items )
			{
				ExamMethodComboBoxItems.Add ( item.Name );
			}
		}

		public async Task LoadDepartmentComboBoxItems ( )
		{
			var items = await SearchPageComboBoxSourceProvider.GetDepartmentComboBoxSource();
			foreach ( var item in items )
			{
				var formatName = $"{item.Name} ({item.LongName})";
				DepartmentComboBoxItems.Add ( formatName );
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

		private void UpdateSearchQuery ( )
		{
			string searchQuery = string.Empty;

			if ( ExamMethodComboBoxSelectedItem != null )
			{
				searchQuery += $" 受験方法: {ExamMethodComboBoxSelectedItem}";
			}
			if ( DepartmentComboBoxSelectedItem != null )
			{
				searchQuery += $" 学科: {DepartmentComboBoxSelectedItem}";
			}
			if ( SelectedYear != null )
			{
				searchQuery += $" 年度: {SelectedYear}年";
			}

			CurrentSearchQuery = string.IsNullOrWhiteSpace ( searchQuery ) ? "なし" : searchQuery.Trim ( );
		}
	}
}