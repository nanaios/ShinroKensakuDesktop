using CommunityToolkit.Mvvm.ComponentModel;
using MySql.Data.MySqlClient;
using ShinroKensakuDesktop.Models;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
	/* Shown as a badge on the connection card: the state is read from its icon and colour plus one word. */
	public enum ConnectionStatus
	{
		NotConfigured,
		Saved,
		Testing,
		Connected,
		Failed,
		Environment
	}

	public partial class SettingsPageViewModel : ObservableObject, IDisposable
	{
		private readonly bool initializing = true;
		private readonly string? preferencesPath;
		private MySqlConnectionStringBuilder? storedConnection;
		private bool watchingTheme;
		private Window? window;

		public SettingsPageViewModel ( string? preferencesPath = null )
		{
			this.preferencesPath = preferencesPath;
			CurrentApplicationTheme = UserPreferences.LoadTheme ( preferencesPath );
			ReportLink = UserPreferences.LoadReportLink ( preferencesPath ) ?? "";
			initializing = false;
			LoadDatabaseFields ( );
		}

		/* The folder the 受験報告書 button opens; saved when the field is left, only if it is an https address. */
		[ ObservableProperty ] public partial string ReportLink { get; set; } = "";
		[ ObservableProperty ] public partial string ReportLinkError { get; set; } = "";
		public bool HasReportLinkError => ReportLinkError.Length > 0;
		public Uri? ReportUri => HasReportLinkError ? null : UserPreferences.ParseReportLink ( ReportLink );
		public bool HasReportUri => ReportUri != null;

		partial void OnReportLinkChanged ( string value )
		{
			if ( initializing )
			{
				return;
			}

			string text = value.Trim ( );
			if ( text.Length > 0 && UserPreferences.ParseReportLink ( text ) == null )
			{
				ReportLinkError = "https:// から始まるリンクを入力してください。";
			}
			else
			{
				try
				{
					UserPreferences.SaveReportLink ( text.Length == 0 ? null : text, preferencesPath );
					ReportLinkError = "";
				}
				catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException )
				{
					ReportLinkError = "リンクを保存できませんでした。保存先の権限を確認してください。";
				}
			}

			OnPropertyChanged ( nameof(ReportUri) );
			OnPropertyChanged ( nameof(HasReportUri) );
		}

		partial void OnReportLinkErrorChanged ( string value )
		{
			OnPropertyChanged ( nameof(HasReportLinkError) );
			OnPropertyChanged ( nameof(ReportUri) );
			OnPropertyChanged ( nameof(HasReportUri) );
		}

		[ ObservableProperty ]
		public partial ApplicationTheme CurrentApplicationTheme { get; set; } = ApplicationTheme.Light;

		[ ObservableProperty ] public partial string ThemeStatusText { get; set; } = "";
		[ ObservableProperty ] public partial string DatabaseServer { get; set; } = "localhost";
		[ ObservableProperty ] public partial string DatabasePort { get; set; } = "3306";
		[ ObservableProperty ] public partial string DatabaseName { get; set; } = "";
		[ ObservableProperty ] public partial string DatabaseUser { get; set; } = "";
		[ ObservableProperty ] public partial string DatabaseStatusText { get; set; } = "";
		[ ObservableProperty ] public partial NoticeSeverity DatabaseNoticeSeverity { get; set; }
		[ ObservableProperty ] public partial ConnectionStatus ConnectionState { get; set; } = ConnectionStatus.NotConfigured;
		[ ObservableProperty ] public partial string ConnectionTarget { get; set; } = "";
		[ ObservableProperty ] public partial bool IsTestingConnection { get; set; }
		public bool CanConfigureLocalConnection => !DatabaseSettings.UsesEnvironment && !IsTestingConnection;
		public bool UsesEnvironment => DatabaseSettings.UsesEnvironment;
		public bool HasThemeError => ThemeStatusText.Length > 0;

		public string ConnectionStateText => ConnectionState switch
		{
			ConnectionStatus.Saved => "保存済み",
			ConnectionStatus.Testing => "確認中",
			ConnectionStatus.Connected => "接続済み",
			ConnectionStatus.Failed => "接続できません",
			ConnectionStatus.Environment => "環境変数",
			_ => "未設定"
		};

		public string DatabaseNoticeTitle => DatabaseNoticeSeverity == NoticeSeverity.Success
			? "接続を確認して保存しました"
			: "保存できませんでした";

		partial void OnConnectionStateChanged ( ConnectionStatus value ) => OnPropertyChanged ( nameof(ConnectionStateText) );

		partial void OnDatabaseNoticeSeverityChanged ( NoticeSeverity value ) =>
			OnPropertyChanged ( nameof(DatabaseNoticeTitle) );
		partial void OnThemeStatusTextChanged ( string value ) => OnPropertyChanged ( nameof(HasThemeError) );

		public string ApplicationVersion => Assembly.GetExecutingAssembly ( ).GetName ( ).Version?.ToString ( ) ?? "不明";

		public void Dispose ( )
		{
			if ( window == null )
			{
				return;
			}

			window.Loaded -= OnWindowLoaded;
			window.Closing -= OnWindowClosing;
			StopWatchingTheme ( );
			window = null;
		}

		private void LoadDatabaseFields ( )
		{
			try
			{
				storedConnection = new MySqlConnectionStringBuilder ( DatabaseSettings.LoadConnectionString ( ) );
				DatabaseServer = storedConnection.Server;
				DatabasePort = storedConnection.Port.ToString ( );
				DatabaseName = storedConnection.Database;
				DatabaseUser = storedConnection.UserID;
				ConnectionTarget = Target ( storedConnection );
				ConnectionState = DatabaseSettings.UsesEnvironment ? ConnectionStatus.Environment : ConnectionStatus.Saved;
			}
			catch ( Exception )
			{
				/* No credentials or exception details are displayed. */
				ConnectionState = ConnectionStatus.NotConfigured;
			}
		}

		private static string Target ( MySqlConnectionStringBuilder builder ) =>
			$"{builder.Server}:{builder.Port} / {builder.Database}";

		private void SetDatabaseNotice ( string text, NoticeSeverity severity )
		{
			DatabaseStatusText = text;
			DatabaseNoticeSeverity = severity;
		}

		partial void OnIsTestingConnectionChanged ( bool value ) =>
			OnPropertyChanged ( nameof(CanConfigureLocalConnection) );

		public async Task<bool> SaveDatabaseAsync ( string password )
		{
			if ( !CanConfigureLocalConnection )
			{
				return false;
			}

			if ( string.IsNullOrWhiteSpace ( DatabaseServer ) || string.IsNullOrWhiteSpace ( DatabaseName ) ||
			     string.IsNullOrWhiteSpace ( DatabaseUser ) || !uint.TryParse ( DatabasePort, out uint port ) ||
			     port is 0 or > 65535 )
			{
				SetDatabaseNotice ( "サーバー・データベース名・ユーザー名を入力し、ポートは1～65535で指定してください。",
					NoticeSeverity.Error );
				return false;
			}

			IsTestingConnection = true;
			ConnectionState = ConnectionStatus.Testing;
			SetDatabaseNotice ( "", NoticeSeverity.None );
			try
			{
				MySqlConnectionStringBuilder builder = storedConnection == null
					? new MySqlConnectionStringBuilder ( )
					: new MySqlConnectionStringBuilder ( storedConnection.ConnectionString );
				builder.Server = DatabaseServer.Trim ( );
				builder.Port = port;
				builder.Database = DatabaseName.Trim ( );
				builder.UserID = DatabaseUser.Trim ( );
				if ( password.Length > 0 )
				{
					builder.Password = password;
				}

				builder.ConnectionTimeout = 15;
				builder.DefaultCommandTimeout = 30;
				await MySQLCommand.TestConnectionAsync ( builder.ConnectionString );
				DatabaseSettings.SaveConnectionString ( builder.ConnectionString );
				storedConnection = builder;
				ConnectionTarget = Target ( builder );
				ConnectionState = ConnectionStatus.Connected;
				SetDatabaseNotice ( "各画面で再読み込み・再検索すると、新しい接続先のデータが表示されます。", NoticeSeverity.Success );
				return true;
			}
			catch ( Exception )
			{
				ConnectionState = ConnectionStatus.Failed;
				SetDatabaseNotice ( "入力内容、MySQLが動いているか、保存先の権限を確認してください。", NoticeSeverity.Error );
				return false;
			}
			finally { IsTestingConnection = false; }
		}

		public void AttachWindow ( Window target )
		{
			if ( window == target )
			{
				return;
			}

			Dispose ( );
			window = target;
			target.Loaded += OnWindowLoaded;
			target.Closing += OnWindowClosing;
			if ( target.IsLoaded )
			{
				ApplyTheme ( );
			}
		}

		private void OnWindowLoaded ( object sender, RoutedEventArgs e ) => ApplyTheme ( );
		private void OnWindowClosing ( object? sender, CancelEventArgs e ) => StopWatchingTheme ( );

		partial void OnCurrentApplicationThemeChanged ( ApplicationTheme value )
		{
			ApplyTheme ( );
			if ( initializing )
			{
				return;
			}

			try
			{
				UserPreferences.SaveTheme ( value, preferencesPath );
				ThemeStatusText = "";
			}
			catch ( Exception )
			{
				ThemeStatusText = "テーマは変更しましたが、設定を保存できませんでした。";
			}
		}

		private void ApplyTheme ( )
		{
			StopWatchingTheme ( );
			if ( CurrentApplicationTheme == ApplicationTheme.Unknown )
			{
				ApplicationThemeManager.ApplySystemTheme ( false );
				/* ApplySystemTheme always applies Mica to the main window, so restore the opaque background. */
				if ( window?.IsLoaded == true )
				{
					WindowBackgroundManager.UpdateBackground ( window, ApplicationThemeManager.GetAppTheme ( ),
						WindowBackdropType.None );
					SystemThemeWatcher.Watch ( window, WindowBackdropType.None, false );
					watchingTheme = true;
				}
			}
			else
			{
				/* The window does not use a backdrop; the default (Mica) makes the background transparent. */
				ApplicationThemeManager.Apply ( CurrentApplicationTheme, WindowBackdropType.None );
			}
		}

		private void StopWatchingTheme ( )
		{
			if ( watchingTheme && window?.IsLoaded == true )
			{
				SystemThemeWatcher.UnWatch ( window );
			}

			watchingTheme = false;
		}
	}
}