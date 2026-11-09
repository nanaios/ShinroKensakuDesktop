using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Views.Controls
{
	/* An ⓘ mark for optional background only (definitions, how a value is derived). Rules and anything needed
	   to finish a task belong on screen instead. Shows its Text on hover or keyboard focus. */
	public sealed class InfoTip : Border
	{
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register (
			nameof(Text), typeof(string), typeof(InfoTip),
			new PropertyMetadata ( "", ( d, e ) => ( ( InfoTip ) d ).OnTextChanged ( ( string? ) e.NewValue ) ) );

		/* What the tip explains, so screen readers announce more than a bare "説明". */
		public static readonly DependencyProperty SubjectProperty = DependencyProperty.Register (
			nameof(Subject), typeof(string), typeof(InfoTip),
			new PropertyMetadata ( "", ( d, _ ) => ( ( InfoTip ) d ).UpdateName ( ) ) );

		private readonly System.Windows.Controls.TextBlock tipText = new( )
		{
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 360,
			LineHeight = 21
		};

		public InfoTip ( )
		{
			/* A transparent fill makes the whole circle hoverable, not only the glyph strokes. */
			Background = Brushes.Transparent;
			Width = 24;
			Height = 24;
			CornerRadius = new CornerRadius ( 12 );
			Margin = new Thickness ( 4, 0, 0, 0 );
			VerticalAlignment = VerticalAlignment.Center;
			Cursor = Cursors.Help;
			Focusable = true;
			SetResourceReference ( FocusVisualStyleProperty, "DefaultControlFocusVisualStyle" );
			SymbolIcon icon = new( )
			{
				Symbol = SymbolRegular.Info16,
				FontSize = 16,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			icon.SetResourceReference ( Control.ForegroundProperty, "TextFillColorSecondaryBrush" );
			Child = icon;
			ToolTip = new ToolTip { Content = tipText };
			ToolTipService.SetInitialShowDelay ( this, 150 );
			ToolTipService.SetShowDuration ( this, int.MaxValue );
			UpdateName ( );
		}

		public string Text
		{
			get => ( string ) GetValue ( TextProperty );
			set => SetValue ( TextProperty, value );
		}

		public string Subject
		{
			get => ( string ) GetValue ( SubjectProperty );
			set => SetValue ( SubjectProperty, value );
		}

		private void OnTextChanged ( string? text )
		{
			tipText.Text = text ?? "";
			AutomationProperties.SetHelpText ( this, tipText.Text );
		}

		private void UpdateName ( ) =>
			AutomationProperties.SetName ( this, string.IsNullOrEmpty ( Subject ) ? "説明" : Subject + "の説明" );
	}
}
