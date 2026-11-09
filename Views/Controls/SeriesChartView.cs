using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.WPF;
using ShinroKensakuDesktop.Models;
using SkiaSharp;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace ShinroKensakuDesktop.Views.Controls
{
	/* A year line chart. The legend sits above the plot with each series' latest value, so the reader does not
	   travel to the bottom; hovering a legend entry emphasises that line. Repaints from the current theme. */
	public sealed class SeriesChartView : UserControl
	{
		public static readonly DependencyProperty DataProperty = DependencyProperty.Register (
			nameof(Data), typeof(SeriesChart), typeof(SeriesChartView),
			new PropertyMetadata ( SeriesChart.Empty, ( d, _ ) => ( ( SeriesChartView ) d ).RenderChart ( ) ) );

		public static readonly DependencyProperty PointCommandProperty = DependencyProperty.Register (
			nameof(PointCommand), typeof(ICommand), typeof(SeriesChartView),
			new PropertyMetadata ( null, ( d, _ ) => ( ( SeriesChartView ) d ).RenderChart ( ) ) );

		/* Plots each series as its share of the year's total of all series (they must partition the total). */
		public static readonly DependencyProperty ShowShareProperty = DependencyProperty.Register (
			nameof(ShowShare), typeof(bool), typeof(SeriesChartView),
			new PropertyMetadata ( false, ( d, _ ) => ( ( SeriesChartView ) d ).RenderChart ( ) ) );

		private readonly CartesianChart chart = new( )
		{
			LegendPosition = LegendPosition.Hidden,
			TooltipTextSize = 14,
			ZoomMode = ZoomAndPanMode.None,
			Height = 280,
			/* Let the card surface show through in every theme. */
			Background = Brushes.Transparent
		};

		private readonly List<( ChartSeries Source, LineSeries<double> Series, SKColor Color )> drawn = [ ];

		private readonly TextBlock empty = new( )
		{
			Text = "選択した範囲に表示できるデータがありません。",
			Margin = new Thickness ( 0, 16, 0, 16 ),
			Visibility = Visibility.Collapsed
		};

		private readonly StackPanel hint = new( )
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness ( 0, 4, 0, 0 )
		};

		private readonly WrapPanel legend = new( ) { HorizontalAlignment = HorizontalAlignment.Right };
		private readonly TextBlock unit = new( ) { FontSize = 12, VerticalAlignment = VerticalAlignment.Bottom };
		private readonly DockPanel header = new( ) { Margin = new Thickness ( 0, 0, 0, 4 ) };

		static SeriesChartView ( )
		{
			/* Skia's default face has no Japanese glyphs; start from the system face that does. */
			LiveCharts.Configure ( settings => settings.HasTextSettings ( new TextSettings
			{
				DefaultTypeface = SKFontManager.Default.MatchCharacter ( '年' ) ?? SKTypeface.Default
			} ) );
		}

		public SeriesChartView ( )
		{
			chart.DataPointerDown += OnDataPointerDown;
			header.Children.Add ( unit );
			DockPanel.SetDock ( unit, Dock.Left );
			header.Children.Add ( legend );
			SymbolIcon pointer = new( ) { Symbol = SymbolRegular.CursorClick20, FontSize = 16, Margin = new Thickness ( 0, 0, 4, 0 ) };
			pointer.SetResourceReference ( Control.ForegroundProperty, "TextFillColorSecondaryBrush" );
			TextBlock hintText = new( ) { Text = "点をクリックで、その年度の明細", FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
			hintText.SetResourceReference ( TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush" );
			hint.Children.Add ( pointer );
			hint.Children.Add ( hintText );
			unit.SetResourceReference ( TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush" );
			empty.SetResourceReference ( TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush" );

			Grid layout = new( );
			layout.RowDefinitions.Add ( new RowDefinition { Height = GridLength.Auto } );
			layout.RowDefinitions.Add ( new RowDefinition { Height = GridLength.Auto } );
			layout.RowDefinitions.Add ( new RowDefinition { Height = GridLength.Auto } );
			Grid.SetRow ( chart, 1 );
			Grid.SetRow ( hint, 2 );
			Grid.SetRowSpan ( empty, 3 );
			layout.Children.Add ( header );
			layout.Children.Add ( chart );
			layout.Children.Add ( hint );
			layout.Children.Add ( empty );
			Content = layout;
			/* Theme switches replace this resource, which repaints the chart through OnPropertyChanged. */
			SetResourceReference ( ForegroundProperty, "TextFillColorPrimaryBrush" );
		}

		public SeriesChart Data
		{
			get => ( SeriesChart ) GetValue ( DataProperty );
			set => SetValue ( DataProperty, value );
		}

		public ICommand? PointCommand
		{
			get => ( ICommand? ) GetValue ( PointCommandProperty );
			set => SetValue ( PointCommandProperty, value );
		}

		public bool ShowShare
		{
			get => ( bool ) GetValue ( ShowShareProperty );
			set => SetValue ( ShowShareProperty, value );
		}

		public CartesianChart Chart => chart;
		public Panel Legend => legend;

		public bool IsDark => Luminance ( BrushColor ( "TextFillColorPrimaryBrush", Colors.Black ) ) > 0.5;

		public static string SeriesColor ( int slot, bool dark ) => ThemeColors.SeriesColor ( slot, dark );

		protected override void OnPropertyChanged ( DependencyPropertyChangedEventArgs e )
		{
			base.OnPropertyChanged ( e );
			if ( e.Property == ForegroundProperty )
			{
				RenderChart ( );
			}
		}

		private void RenderChart ( )
		{
			SeriesChart data = Data ?? SeriesChart.Empty;
			bool hasData = data.HasData;
			bool clickable = PointCommand != null && data.Keys.Count > 0;
			chart.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
			header.Visibility = chart.Visibility;
			hint.Visibility = hasData && clickable ? Visibility.Visible : Visibility.Collapsed;
			empty.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
			chart.FindingStrategy = FindingStrategy.CompareOnlyX;
			Cursor = clickable ? Cursors.Hand : null;
			AutomationProperties.SetName ( this, Describe ( data ) );
			drawn.Clear ( );
			legend.Children.Clear ( );
			if ( !hasData )
			{
				chart.Series = [ ];
				return;
			}

			bool share = ShowShare;
			bool dark = IsDark;
			SKColor primary = Sk ( BrushColor ( "TextFillColorPrimaryBrush", Colors.Black ) );
			SKColor secondary = Sk ( BrushColor ( "TextFillColorSecondaryBrush", Colors.Gray ) );
			SKColor surface = Sk ( BrushColor ( "ApplicationBackgroundBrush", Colors.White ) );
			chart.TooltipTextPaint = new SolidColorPaint ( primary );
			chart.TooltipBackgroundPaint = new SolidColorPaint ( surface );
			unit.Text = share ? "%" : "件";

			chart.XAxes =
			[
				new Axis
				{
					Labels = data.Labels.ToArray ( ),
					LabelsPaint = new SolidColorPaint ( secondary ),
					TextSize = 12,
					SeparatorsPaint = null,
					MinStep = 1,
					ForceStepToMin = true
				}
			];
			chart.YAxes =
			[
				new Axis
				{
					MinLimit = 0,
					MinStep = 1,
					Labeler = value => share ? $"{value:0}%" : value.ToString ( "N0" ),
					LabelsPaint = new SolidColorPaint ( secondary ),
					TextSize = 12,
					SeparatorsPaint = new SolidColorPaint ( secondary.WithAlpha ( 48 ) ) { StrokeThickness = 1 }
				}
			];

			double [ ] totals = Enumerable.Range ( 0, data.Labels.Count )
				.Select ( i => ( double ) data.Series.Where ( s => !s.IsReference && i < s.Values.Count ).Sum ( s => s.Values [ i ] ) )
				.ToArray ( );
			foreach ( ChartSeries source in data.Series )
			{
				SKColor color = source.IsReference ? secondary : SKColor.Parse ( SeriesColor ( source.Slot, dark ) );
				double [ ] values = source.Values.Select ( ( v, i ) =>
					share ? totals [ i ] == 0 ? 0 : v * 100.0 / totals [ i ] : v ).ToArray ( );
				LineSeries<double> series = new( )
				{
					Name = source.Name,
					Values = values,
					Fill = null,
					LineSmoothness = 0,
					GeometrySize = source.IsReference ? 7 : 9,
					GeometryFill = new SolidColorPaint ( surface ),
					YToolTipLabelFormatter = point => share
						? $"{point.Coordinate.PrimaryValue:0.0}%（{source.Values [ point.Index ]:N0}件）"
						: $"{point.Coordinate.PrimaryValue:N0} 件"
				};
				drawn.Add ( ( source, series, color ) );
				legend.Children.Add ( LegendItem ( source, color, values.Length == 0 ? 0 : values [ ^1 ], share ) );
			}

			Highlight ( null );
			chart.Series = drawn.Select ( x => ( ISeries ) x.Series ).ToArray ( );
		}

		/* The legend entry carries the latest value, so the end of each line is read without the tooltip. */
		private FrameworkElement LegendItem ( ChartSeries source, SKColor color, double latest, bool share )
		{
			Line swatch = new( )
			{
				X1 = 0, X2 = 18, Y1 = 0, Y2 = 0,
				Stroke = new SolidColorBrush ( Color.FromArgb ( color.Alpha, color.Red, color.Green, color.Blue ) ),
				StrokeThickness = 3,
				StrokeDashArray = source.IsReference ? [ 1.5, 1 ] : null,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness ( 0, 0, 6, 0 )
			};
			string value = share ? $"{latest:0}%" : latest.ToString ( "N0" );
			StackPanel item = new( )
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness ( 16, 2, 0, 2 ),
				Background = Brushes.Transparent,
				Cursor = Cursors.Arrow
			};
			item.Children.Add ( swatch );
			item.Children.Add ( new TextBlock { Text = source.Name, VerticalAlignment = VerticalAlignment.Center } );
			item.Children.Add ( new TextBlock
			{
				Text = value,
				FontWeight = FontWeights.SemiBold,
				Margin = new Thickness ( 6, 0, 0, 0 ),
				VerticalAlignment = VerticalAlignment.Center
			} );
			item.MouseEnter += ( _, _ ) => Highlight ( source.Name );
			item.MouseLeave += ( _, _ ) => Highlight ( null );
			AutomationProperties.SetName ( item, $"{source.Name} {value}" );
			return item;
		}

		/* Every other line fades while one legend entry is hovered. */
		private void Highlight ( string? name )
		{
			foreach ( ( ChartSeries source, LineSeries<double> series, SKColor color ) in drawn )
			{
				bool focus = name == null || source.Name == name;
				SKColor shown = focus ? color : color.WithAlpha ( 56 );
				float thickness = name != null && focus ? 3 : 2;
				series.Stroke = new SolidColorPaint ( shown )
				{
					StrokeThickness = thickness,
					/* The total is a reference line, so it is dashed rather than given a palette colour. */
					PathEffect = source.IsReference ? new DashEffect ( [ 6, 4 ] ) : null
				};
				series.GeometryStroke = new SolidColorPaint ( shown ) { StrokeThickness = 2 };
			}
		}

		private void OnDataPointerDown ( IChartView sender, IEnumerable<ChartPoint> points )
		{
			ChartPoint? point = points.FirstOrDefault ( );
			IReadOnlyList<int> keys = Data?.Keys ?? [ ];
			if ( point == null || point.Index < 0 || point.Index >= keys.Count )
			{
				return;
			}

			int year = keys [ point.Index ];
			if ( PointCommand?.CanExecute ( year ) == true )
			{
				PointCommand.Execute ( year );
			}
		}

		/* Screen readers get the same numbers the chart draws. */
		private static string Describe ( SeriesChart data ) => !data.HasData
			? "表示できるデータがありません"
			: string.Join ( "。", data.Series.Select ( s => s.Name + "：" + string.Join ( "、",
				data.Labels.Zip ( s.Values, ( label, value ) => $"{label}年度 {value:N0}件" ) ) ) );

		private Color BrushColor ( string key, Color fallback ) =>
			TryFindResource ( key ) is SolidColorBrush brush ? brush.Color : fallback;

		private static SKColor Sk ( Color color ) => new(color.R, color.G, color.B, color.A);

		private static double Luminance ( Color color ) =>
			( ( 0.2126 * color.R ) + ( 0.7152 * color.G ) + ( 0.0722 * color.B ) ) / 255;
	}
}
