using CommunityToolkit.Mvvm.ComponentModel;
using MySql.Data.MySqlClient;
using ShinroKensakuDesktop.Models;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using Wpf.Ui.Appearance;

namespace ShinroKensakuDesktop.ViewModels.Pages
{
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
			initializing = false;
			LoadDatabaseFields ( );
		}

		[ ObservableProperty ]
		public partial ApplicationTheme CurrentApplicationTheme { get; set; } = ApplicationTheme.Light;

		[ ObservableProperty ] public partial string ThemeStatusText { get; set; } = "";
		[ ObservableProperty ] public partial string DatabaseServer { get; set; } = "localhost";
		[ ObservableProperty ] public partial string DatabasePort { get; set; } = "3306";
		[ ObservableProperty ] public partial string DatabaseName { get; set; } = "";
		[ ObservableProperty ] public partial string DatabaseUser { get; set; } = "";
		[ ObservableProperty ] public partial string DatabaseStatusText { get; set; } = "接続情報を入力し、接続確認して保存してください。";
		[ ObservableProperty ] public partial bool IsTestingConnection { get; set; }
		public bool CanConfigureLocalConnection => !DatabaseSettings.UsesEnvironment && !IsTestingConnection;

		public string ConnectionSourceText => DatabaseSettings.UsesEnvironment
			? "環境変数で接続が指定されています。変更する場合は環境変数を更新してアプリを再起動してください。"
			: "接続設定は、このWindowsユーザーだけが復号できる形式で端末に保存します。";

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
				DatabaseStatusText = "保存済みの接続設定を読み込みました。";
			}
			catch ( Exception )
			{
				/* No credentials or exception details are displayed. */
			}
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
				DatabaseStatusText = "サーバー・データベース・ユーザー名を入力し、ポートは1～65535で指定してください。";
				return false;
			}

			IsTestingConnection = true;
			DatabaseStatusText = "接続を確認しています…";
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
				DatabaseStatusText = "接続確認して保存しました。各画面で再検索・再読み込みすると反映されます。";
				return true;
			}
			catch ( Exception )
			{
				DatabaseStatusText = "接続確認または保存に失敗しました。入力内容・MySQLの稼働・保存先の権限を確認してください。";
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
				if ( window?.IsLoaded == true )
				{
					SystemThemeWatcher.Watch ( window );
					watchingTheme = true;
				}
			}
			else
			{
				ApplicationThemeManager.Apply ( CurrentApplicationTheme );
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