using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShinroKensakuDesktop.Models;
using ShinroKensakuDesktop.Models.Data;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	public partial class SearchPageViewModel : ObservableObject
	{
		/* Every selector starts with すべて, so "no filter" is a visible choice rather than an empty box. */
		public const string All = "すべて";
		public static readonly ExamMethodData AllMethods = new("", All);
		public static readonly DepartmentData AllClasses = new(sbyte.MinValue, All, "");

		private readonly SemaphoreSlim departmentGate = new(1, 1);
		private readonly Func<Task<List<DepartmentData>>> loadDepartments;
		private readonly Func<Task<List<ExamMethodData>>> loadMethods;
		private readonly Func<SearchCriteria, CancellationToken, Task<List<SearchResultData>>> loadResults;
		private readonly Func<Task<List<int>>> loadYears;
		private readonly SemaphoreSlim methodGate = new(1, 1);
		private readonly SemaphoreSlim yearGate = new(1, 1);
		private int conditionsVersion = DatabaseSettings.Version;
		private List<int> knownYears = [ ];
		private int lastConnectionVersion;
		private SearchCriteria? lastCriteria;
		private bool methodsLoaded, departmentsLoaded, yearsLoaded;
		private List<SearchResultData>? searchResults;

		public SearchPageViewModel (
			Func<SearchCriteria, CancellationToken, Task<List<SearchResultData>>>? load = null,
			Func<Task<List<ExamMethodData>>>? methods = null,
			Func<Task<List<DepartmentData>>>? departments = null,
			Func<Task<List<int>>>? years = null )
		{
			loadResults = load ?? SearchPageDataGridSourceProvider.GetDataGridSource;
			loadMethods = methods ?? SearchPageComboBoxSourceProvider.GetExamMethodComboBoxSource;
			loadDepartments = departments ?? SearchPageComboBoxSourceProvider.GetDepartmentComboBoxSource;
			loadYears = years ?? SearchPageComboBoxSourceProvider.GetYearComboBoxSource;
		}

		/* Raised after a CSV file is written, with a short message for a transient confirmation. */
		public event EventHandler<string>? Exported;

		[ ObservableProperty ] public partial string StatusText { get; set; } = "条件を指定して検索してください。";
		[ ObservableProperty ] public partial NoticeSeverity StatusSeverity { get; set; }
		[ ObservableProperty ] public partial bool IsConnectionError { get; set; }
		[ ObservableProperty ] public partial string ConditionStatusText { get; set; } = "";
		[ ObservableProperty ] public partial bool IsBusy { get; set; }
		[ ObservableProperty ] public partial bool IsExporting { get; set; }
		[ ObservableProperty ] public partial bool IsLoadingConditions { get; set; }
		[ ObservableProperty ] public partial string CurrentSearchQuery { get; set; } = "すべての受験記録";
		[ ObservableProperty ] public partial string LastSearchSummary { get; set; } = "";
		[ ObservableProperty ] public partial ExamMethodData? ExamMethodComboBoxSelectedItem { get; set; }
		[ ObservableProperty ] public partial DepartmentData? DepartmentComboBoxSelectedItem { get; set; }
		[ ObservableProperty ] public partial string? SearchTargetName { get; set; }
		[ ObservableProperty ] public partial string? SelectedYear { get; set; }

		/* A department (学科) name as the graph derives it; null means every department. */
		[ ObservableProperty ] public partial string? SelectedDepartmentName { get; set; }

		[ ObservableProperty ]
		public partial ObservableCollection<SearchResultData> SearchResultList { get; set; } = new( );

		[ ObservableProperty ] public partial string SearchResultCountText { get; set; } = "0件";
		[ ObservableProperty ] public partial List<string> YearOptions { get; set; } = [ All ];
		[ ObservableProperty ] public partial List<ExamMethodData> ExamMethodOptions { get; set; } = [ AllMethods ];
		[ ObservableProperty ] public partial List<string> DepartmentNameOptions { get; set; } = [ All ];
		[ ObservableProperty ] public partial List<DepartmentData> ClassOptions { get; set; } = [ AllClasses ];

		public ObservableCollection<ExamMethodData> ExamMethodComboBoxItems { get; } = [ ];
		public ObservableCollection<DepartmentData> DepartmentComboBoxItems { get; } = [ ];
		public bool IsLoading => IsBusy || IsLoadingConditions || IsExporting;
		public bool HasConditionError => ConditionStatusText.Length > 0;
		public bool HasLastSearch => lastCriteria != null;

		/* Selector values: すべて maps to null. A null from the control (its items being replaced) is ignored. */
		public string SelectedYearOption
		{
			get => string.IsNullOrWhiteSpace ( SelectedYear ) ? All : SelectedYear.Trim ( );
			set
			{
				if ( value != null )
				{
					SelectedYear = value == All ? null : value;
				}
			}
		}

		public ExamMethodData SelectedExamMethodOption
		{
			get => ExamMethodComboBoxSelectedItem ?? AllMethods;
			set
			{
				if ( value != null )
				{
					ExamMethodComboBoxSelectedItem = value == AllMethods ? null : value;
				}
			}
		}

		public string SelectedDepartmentNameOption
		{
			get => SelectedDepartmentName ?? All;
			set
			{
				if ( value != null )
				{
					SelectedDepartmentName = value == All ? null : value;
				}
			}
		}

		public DepartmentData SelectedClassOption
		{
			get => DepartmentComboBoxSelectedItem ?? AllClasses;
			set
			{
				if ( value != null )
				{
					DepartmentComboBoxSelectedItem = value == AllClasses ? null : value;
				}
			}
		}

		public bool IsSearchDirty
		{
			get
			{
				if ( lastCriteria == null )
				{
					return false;
				}

				if ( lastConnectionVersion != DatabaseSettings.Version )
				{
					return true;
				}

				try { return CurrentCriteria ( ) != lastCriteria; }
				catch ( ArgumentException ) { return true; }
			}
		}

		public string DirtyMessage => LastSearchSummary.Length == 0
			? "表示中の結果とCSVは、前の条件で検索したものです。"
			: $"表示中の結果とCSVは「{LastSearchSummary}」で検索したものです。";

		public bool HasNoResults => !IsBusy && SearchResultList.Count == 0;

		/* An error explains itself in the notice, so the empty table stays blank then. */
		public bool ShowEmptyState => HasNoResults && StatusSeverity != NoticeSeverity.Error;

		public string EmptyStateText => lastCriteria == null ? "進路先の名前か条件を指定して検索" : "条件に一致する受験記録はありません";
		public bool CanEditConditions => !IsBusy && !IsExporting;

		private SearchCriteria CurrentCriteria ( ) => SearchCriteria.Create ( SearchTargetName, SelectedYear,
			ExamMethodComboBoxSelectedItem?.Id, DepartmentComboBoxSelectedItem?.Id, SelectedDepartmentName,
			ClassesOf ( SelectedDepartmentName ) );

		private IEnumerable<sbyte> ClassesOf ( string? department ) => department == null
			? [ ]
			: DepartmentComboBoxItems.Where ( x => DashboardDataProvider.DepartmentOf ( x.LongName ) == department )
				.Select ( x => x.Id );

		partial void OnIsBusyChanged ( bool value ) => NotifyState ( );
		partial void OnIsExportingChanged ( bool value ) => NotifyState ( );
		partial void OnStatusSeverityChanged ( NoticeSeverity value ) => OnPropertyChanged ( nameof(ShowEmptyState) );
		partial void OnConditionStatusTextChanged ( string value ) => OnPropertyChanged ( nameof(HasConditionError) );
		partial void OnLastSearchSummaryChanged ( string value ) => OnPropertyChanged ( nameof(DirtyMessage) );

		partial void OnIsLoadingConditionsChanged ( bool value )
		{
			OnPropertyChanged ( nameof(IsLoading) );
			ReloadConditionsCommand.NotifyCanExecuteChanged ( );
		}

		partial void OnExamMethodComboBoxSelectedItemChanged ( ExamMethodData? value )
		{
			OnPropertyChanged ( nameof(SelectedExamMethodOption) );
			UpdateSearchQuery ( );
		}

		partial void OnDepartmentComboBoxSelectedItemChanged ( DepartmentData? value )
		{
			OnPropertyChanged ( nameof(SelectedClassOption) );
			UpdateSearchQuery ( );
		}

		partial void OnSelectedDepartmentNameChanged ( string? value )
		{
			UpdateDepartmentOptions ( );
			OnPropertyChanged ( nameof(SelectedDepartmentNameOption) );
			UpdateSearchQuery ( );
		}

		partial void OnSelectedYearChanged ( string? value )
		{
			UpdateYearOptions ( );
			OnPropertyChanged ( nameof(SelectedYearOption) );
			UpdateSearchQuery ( );
		}

		partial void OnSearchTargetNameChanged ( string? value ) => UpdateSearchQuery ( );

		partial void OnSearchResultListChanged ( ObservableCollection<SearchResultData> value )
		{
			OnPropertyChanged ( nameof(HasNoResults) );
			OnPropertyChanged ( nameof(ShowEmptyState) );
		}

		private void NotifyState ( )
		{
			ExportCsvCommand.NotifyCanExecuteChanged ( );
			ExecuteSearchCommand.NotifyCanExecuteChanged ( );
			ClearAllConditionsCommand.NotifyCanExecuteChanged ( );
			ReloadConditionsCommand.NotifyCanExecuteChanged ( );
			OnPropertyChanged ( nameof(IsLoading) );
			OnPropertyChanged ( nameof(CanEditConditions) );
			OnPropertyChanged ( nameof(HasNoResults) );
			OnPropertyChanged ( nameof(ShowEmptyState) );
			OnPropertyChanged ( nameof(IsSearchDirty) );
			OnPropertyChanged ( nameof(EmptyStateText) );
			OnPropertyChanged ( nameof(HasLastSearch) );
		}

		private void CheckConnectionVersion ( )
		{
			if ( conditionsVersion == DatabaseSettings.Version )
			{
				return;
			}

			conditionsVersion = DatabaseSettings.Version;
			methodsLoaded = departmentsLoaded = yearsLoaded = false;
			ExamMethodComboBoxItems.Clear ( );
			DepartmentComboBoxItems.Clear ( );
			ExamMethodComboBoxSelectedItem = null;
			DepartmentComboBoxSelectedItem = null;
			knownYears = [ ];
			ExamMethodOptions = [ AllMethods ];
			UpdateDepartmentOptions ( );
			UpdateYearOptions ( );
			NotifyState ( );
		}

		[ RelayCommand ]
		public async Task LoadConditionsAsync ( )
		{
			IsLoadingConditions = true;
			ConditionStatusText = "";
			try
			{
				CheckConnectionVersion ( );
				await Task.WhenAll ( LoadExamMethodComboBoxItems ( ), LoadDepartmentComboBoxItems ( ), LoadYearOptions ( ) );
			}
			catch ( Exception )
			{
				ConditionStatusText = "データベースの接続を確認して、再読み込みしてください。";
			}
			finally { IsLoadingConditions = false; }
		}

		private bool CanReloadConditions ( ) => !IsLoadingConditions && CanEditConditions;

		[ RelayCommand ( CanExecute = nameof(CanReloadConditions) ) ]
		public async Task ReloadConditionsAsync ( )
		{
			if ( !CanReloadConditions ( ) )
			{
				return;
			}

			methodsLoaded = departmentsLoaded = yearsLoaded = false;
			await LoadConditionsAsync ( );
		}

		public async Task LoadExamMethodComboBoxItems ( )
		{
			await methodGate.WaitAsync ( );
			try
			{
				if ( methodsLoaded )
				{
					return;
				}

				List<ExamMethodData> items = await loadMethods ( );
				string? selectedId = ExamMethodComboBoxSelectedItem?.Id;
				ExamMethodComboBoxItems.Clear ( );
				foreach ( ExamMethodData item in items )
				{
					ExamMethodComboBoxItems.Add ( item );
				}

				ExamMethodOptions = [ AllMethods, .. items ];
				ExamMethodComboBoxSelectedItem = items.FirstOrDefault ( x => x.Id == selectedId );
				OnPropertyChanged ( nameof(SelectedExamMethodOption) );
				methodsLoaded = true;
			}
			finally { methodGate.Release ( ); }
		}

		public async Task LoadDepartmentComboBoxItems ( )
		{
			await departmentGate.WaitAsync ( );
			try
			{
				if ( departmentsLoaded )
				{
					return;
				}

				List<DepartmentData> items = await loadDepartments ( );
				sbyte? selectedId = DepartmentComboBoxSelectedItem?.Id;
				DepartmentComboBoxItems.Clear ( );
				foreach ( DepartmentData item in items )
				{
					DepartmentComboBoxItems.Add ( item );
				}

				DepartmentComboBoxSelectedItem = items.FirstOrDefault ( x => x.Id == selectedId );
				UpdateDepartmentOptions ( );
				departmentsLoaded = true;
			}
			finally { departmentGate.Release ( ); }
		}

		private async Task LoadYearOptions ( )
		{
			await yearGate.WaitAsync ( );
			try
			{
				if ( yearsLoaded )
				{
					return;
				}

				knownYears = await loadYears ( );
				UpdateYearOptions ( );
				yearsLoaded = true;
			}
			finally { yearGate.Release ( ); }
		}

		/* A year set from elsewhere (a chart point) is shown even before, or without, the year list. */
		private void UpdateYearOptions ( )
		{
			IEnumerable<int> years = knownYears;
			if ( int.TryParse ( SelectedYear?.Trim ( ), NumberStyles.None, CultureInfo.InvariantCulture, out int year ) )
			{
				years = years.Append ( year );
			}

			YearOptions = [ All, .. years.Distinct ( ).OrderDescending ( ).Select ( x => x.ToString ( CultureInfo.InvariantCulture ) ) ];
			OnPropertyChanged ( nameof(SelectedYearOption) );
		}

		/* Departments in class order; classes narrow to the chosen department and a class outside it is dropped. */
		private void UpdateDepartmentOptions ( )
		{
			List<string> names = DepartmentComboBoxItems
				.GroupBy ( x => DashboardDataProvider.DepartmentOf ( x.LongName ) )
				.OrderBy ( x => x.Key == DashboardDataProvider.Unassigned ).ThenBy ( x => x.Min ( y => y.Id ) )
				.Select ( x => x.Key ).ToList ( );
			if ( SelectedDepartmentName != null && !names.Contains ( SelectedDepartmentName ) )
			{
				names.Add ( SelectedDepartmentName );
			}

			DepartmentNameOptions = [ All, .. names ];
			ClassOptions =
			[
				AllClasses, .. DepartmentComboBoxItems.Where ( x => SelectedDepartmentName == null ||
				                                                    DashboardDataProvider.DepartmentOf ( x.LongName ) ==
				                                                    SelectedDepartmentName )
			];
			if ( DepartmentComboBoxSelectedItem != null && !ClassOptions.Contains ( DepartmentComboBoxSelectedItem ) )
			{
				DepartmentComboBoxSelectedItem = null;
			}

			OnPropertyChanged ( nameof(SelectedDepartmentNameOption) );
			OnPropertyChanged ( nameof(SelectedClassOption) );
		}

		[ RelayCommand ( CanExecute = nameof(CanSearch) ) ]
		public void ClearAllConditions ( )
		{
			SearchTargetName = null;
			SelectedYear = null;
			ExamMethodComboBoxSelectedItem = null;
			SelectedDepartmentName = null;
			DepartmentComboBoxSelectedItem = null;
		}

		private bool CanSearch ( ) => !IsBusy && !IsExporting;

		private void SetStatus ( string text, NoticeSeverity severity, bool connection = false )
		{
			StatusText = text;
			IsConnectionError = connection;
			StatusSeverity = severity;
		}

		[ RelayCommand ( CanExecute = nameof(CanSearch), IncludeCancelCommand = true ) ]
		public async Task ExecuteSearch ( CancellationToken cancellationToken )
		{
			if ( !CanSearch ( ) )
			{
				return;
			}

			SearchCriteria criteria;
			try { criteria = CurrentCriteria ( ); }
			catch ( ArgumentException )
			{
				SetStatus ( "卒業年度は1901～2155の数字で入力してください。", NoticeSeverity.Error );
				return;
			}

			string summary = CurrentSearchQuery;
			int connectionVersion = DatabaseSettings.Version;
			IsBusy = true;
			searchResults = null;
			lastCriteria = null;
			LastSearchSummary = "";
			ShowResults ( );
			SetStatus ( "検索しています…", NoticeSeverity.None );
			try
			{
				List<SearchResultData> loaded = await loadResults ( criteria, cancellationToken );
				cancellationToken.ThrowIfCancellationRequested ( );
				searchResults = loaded;
				lastCriteria = criteria;
				lastConnectionVersion = connectionVersion;
				LastSearchSummary = summary;
				ShowResults ( );
				SetStatus ( loaded.Count == 0 ? "条件に一致する受験記録はありません。" : "検索完了", NoticeSeverity.None );
			}
			catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested )
			{
				SetStatus ( "検索を中断しました。", NoticeSeverity.Info );
			}
			catch ( Exception )
			{
				SetStatus ( "検索できませんでした。データベースの接続を確認して、もう一度検索してください。", NoticeSeverity.Error, true );
			}
			finally { IsBusy = false; }
		}

		private bool CanExportCsv ( ) => !IsBusy && !IsExporting && !IsSearchDirty && searchResults is { Count: > 0 };

		[ RelayCommand ( CanExecute = nameof(CanExportCsv) ) ]
		private async Task ExportCsvAsync ( )
		{
			if ( !CanExportCsv ( ) )
			{
				return;
			}

			SearchResultData [ ] snapshot = searchResults!.ToArray ( );
			SaveFileDialog dialog = new( )
			{
				Filter = "CSVファイル (*.csv)|*.csv",
				DefaultExt = ".csv",
				AddExtension = true,
				FileName = $"進路検索結果_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
			};
			if ( dialog.ShowDialog ( ) != true )
			{
				return;
			}

			IsExporting = true;
			try
			{
				await Task.Run ( async ( ) =>
				{
					string temporary = dialog.FileName + "." + Guid.NewGuid ( ).ToString ( "N" ) + ".tmp";
					try
					{
						await File.WriteAllTextAsync ( temporary, SearchResultCsv.Create ( snapshot ),
							new UTF8Encoding ( true ) );
						File.Move ( temporary, dialog.FileName, true );
					}
					finally
					{
						if ( File.Exists ( temporary ) )
						{
							File.Delete ( temporary );
						}
					}
				} );
				SetStatus ( $"検索結果{snapshot.Length:N0}件をCSVに保存しました。", NoticeSeverity.None );
				Exported?.Invoke ( this, $"{snapshot.Length:N0}件を{Path.GetFileName ( dialog.FileName )}に保存しました" );
			}
			catch ( Exception )
			{
				SetStatus ( "CSVを保存できませんでした。ファイルが開かれていないか、保存先を確認してください。", NoticeSeverity.Error );
			}
			finally { IsExporting = false; }
		}

		private void ShowResults ( )
		{
			SearchResultList = new ObservableCollection<SearchResultData> ( searchResults ?? [ ] );
			SearchResultCountText = $"{SearchResultList.Count:N0}件";
			NotifyState ( );
		}

		private void UpdateSearchQuery ( )
		{
			List<string> parts = [ ];
			if ( !string.IsNullOrWhiteSpace ( SearchTargetName ) )
			{
				parts.Add ( $"進路先：{SearchTargetName.Trim ( )}" );
			}

			if ( !string.IsNullOrWhiteSpace ( SelectedYear ) )
			{
				parts.Add ( $"卒業年度：{SelectedYear.Trim ( )}" );
			}

			if ( ExamMethodComboBoxSelectedItem != null )
			{
				parts.Add ( $"受験方法：{ExamMethodComboBoxSelectedItem.Name}" );
			}

			if ( SelectedDepartmentName != null )
			{
				parts.Add ( $"学科：{SelectedDepartmentName}" );
			}

			if ( DepartmentComboBoxSelectedItem != null )
			{
				parts.Add ( $"クラス：{DepartmentComboBoxSelectedItem.Name}" );
			}

			CurrentSearchQuery = parts.Count == 0 ? "すべての受験記録" : string.Join ( " ／ ", parts );
			OnPropertyChanged ( nameof(IsSearchDirty) );
			ExportCsvCommand.NotifyCanExecuteChanged ( );
		}
	}
}
