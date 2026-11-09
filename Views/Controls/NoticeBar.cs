using ShinroKensakuDesktop.Models;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace ShinroKensakuDesktop.Views.Controls
{
	/* A status bar for states the user must notice or act on. Unlike the WPF UI InfoBar it hosts action
	   buttons as Content; the template in App.xaml gives each severity its own icon and colour, always with text. */
	public sealed class NoticeBar : ContentControl
	{
		public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register (
			nameof(Severity), typeof(NoticeSeverity), typeof(NoticeBar),
			new PropertyMetadata ( NoticeSeverity.None, ( d, e ) => ( ( NoticeBar ) d ).OnSeverityChanged ( ) ) );

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register (
			nameof(Title), typeof(string), typeof(NoticeBar), new PropertyMetadata ( "" ) );

		public static readonly DependencyProperty MessageProperty = DependencyProperty.Register (
			nameof(Message), typeof(string), typeof(NoticeBar), new PropertyMetadata ( "" ) );

		public NoticeBar ( )
		{
			Visibility = Visibility.Collapsed;
			Focusable = false;
			AutomationProperties.SetLiveSetting ( this, AutomationLiveSetting.Polite );
		}

		public NoticeSeverity Severity
		{
			get => ( NoticeSeverity ) GetValue ( SeverityProperty );
			set => SetValue ( SeverityProperty, value );
		}

		public string Title
		{
			get => ( string ) GetValue ( TitleProperty );
			set => SetValue ( TitleProperty, value );
		}

		public string Message
		{
			get => ( string ) GetValue ( MessageProperty );
			set => SetValue ( MessageProperty, value );
		}

		private void OnSeverityChanged ( )
		{
			SetCurrentValue ( VisibilityProperty, Severity == NoticeSeverity.None ? Visibility.Collapsed : Visibility.Visible );
			AutomationProperties.SetName ( this, Title );
		}
	}
}
