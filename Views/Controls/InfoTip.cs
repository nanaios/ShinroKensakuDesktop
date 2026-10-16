using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace ShinroKensakuDesktop.Views.Controls
{
	/* An ⓘ mark that shows its Text as a tooltip on hover or keyboard focus, so guidance stays out of the layout. */
	public sealed class InfoTip : Border
	{
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register (
			nameof(Text), typeof(string), typeof(InfoTip),
			new PropertyMetadata ( "", ( d, e ) => ( ( InfoTip ) d ).OnTextChanged ( ( string? ) e.NewValue ) ) );

		private readonly System.Windows.Controls.TextBlock tipText = new( )
		{
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 360
		};

		public InfoTip ( )
		{
			/* A transparent fill makes the whole circle hoverable, not only the glyph strokes. */
			Background = Brushes.Transparent;
			Width = 24;
			Height = 24;
			Margin = new Thickness ( 4, 0, 0, 0 );
			VerticalAlignment = VerticalAlignment.Center;
			Cursor = Cursors.Help;
			Focusable = true;
			FocusVisualStyle = null;
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
			AutomationProperties.SetName ( this, "説明" );
		}

		public string Text
		{
			get => ( string ) GetValue ( TextProperty );
			set => SetValue ( TextProperty, value );
		}

		private void OnTextChanged ( string? text )
		{
			tipText.Text = text ?? "";
			AutomationProperties.SetHelpText ( this, tipText.Text );
		}
	}
}
