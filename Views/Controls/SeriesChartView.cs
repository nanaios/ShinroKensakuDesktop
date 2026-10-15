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

namespace ShinroKensakuDesktop.Views.Controls
{
	public enum SeriesChartKind
	{
		Line,
		StackedBar
	}

	/* Maps SeriesChart data onto a LiveCharts chart and repaints it from the current WPF-UI theme resources. */
	public sealed class SeriesChartView : UserControl
	{
		public static readonly DependencyProperty DataProperty = DependencyProperty.Register (
			nameof(Data), typeof(SeriesChart), typeof(SeriesChartView),
			new PropertyMetadata ( SeriesChart.Empty, ( d, _ ) => ( ( SeriesChartView ) d ).RenderChart ( ) ) );

		public static readonly DependencyProperty KindProperty = DependencyProperty.Register (
			nameof(Kind), typeof(SeriesChartKind), typeof(SeriesChartView),
			new PropertyMetadata ( SeriesChartKind.Line, ( d, _ ) => ( ( SeriesChartView ) d ).RenderChart ( ) ) );

		public static readonly DependencyProperty PointCommandProperty = DependencyProperty.Register (
			nameof(PointCommand), typeof(ICommand), typeof(SeriesChartView),
			new PropertyMetadata ( null, ( d, _ ) => ( ( SeriesChartView ) d ).RenderChart ( ) ) );

		/* Validated categorical palette (light / dark steps of the same eight hues), assigned by slot, never cycled. */
		private static readonly string [ ] LightPalette =
			[ "#2A78D6", "#EB6834", "#1BAF7A", "#EDA100", "#E87BA4", "#008300", "#4A3AA7", "#E34948" ];

		private static readonly string [ ] DarkPalette =
			[ "#3987E5", "#D95926", "#199E70", "#C98500", "#D55181", "#008300", "#9085E9", "#E66767" ];

		private readonly CartesianChart chart = new( )
		{
			LegendPosition = LegendPosition.Bottom,
			LegendTextSize = 13,
			TooltipTextSize = 13,
			ZoomMode = ZoomAndPanMode.None,
			/* Let the card surface show through in every theme. */
			Background = Brushes.Transparent
		};

		private readonly TextBlock empty = new( )
		{
			Text = "選択した範囲に表示できるデータがありません。",
			Margin = new Thickness ( 0, 16, 0, 16 ),
			Visibility = Visibility.Collapsed
		};

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
			Grid layout = new( );
			layout.Children.Add ( chart );
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

		public SeriesChartKind Kind
		{
			get => ( SeriesChartKind ) GetValue ( KindProperty );
			set => SetValue ( KindProperty, value );
		}

		public ICommand? PointCommand
		{
			get => ( ICommand? ) GetValue ( PointCommandProperty );
			set => SetValue ( PointCommandProperty, value );
		}

		public CartesianChart Chart => chart;

		public bool IsDark => Luminance ( BrushColor ( "TextFillColorPrimaryBrush", Colors.Black ) ) > 0.5;

		public static string SeriesColor ( int slot, bool dark ) =>
			( dark ? DarkPalette : LightPalette ) [ Math.Clamp ( slot, 0, LightPalette.Length - 1 ) ];

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
			chart.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
			empty.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
			empty.Foreground = new SolidColorBrush ( BrushColor ( "TextFillColorSecondaryBrush", Colors.Gray ) );
			bool line = Kind == SeriesChartKind.Line;
			chart.Height = line ? 320 : Math.Max ( 160, ( data.Labels.Count * 44 ) + 96 );
			chart.FindingStrategy = line ? FindingStrategy.CompareOnlyX : FindingStrategy.CompareOnlyY;
			Cursor = line && PointCommand != null && data.Keys.Count > 0 ? Cursors.Hand : null;
			AutomationProperties.SetName ( this, Describe ( data ) );
			if ( !hasData )
			{
				chart.Series = [ ];
				return;
			}

			bool dark = IsDark;
			SKColor primary = Sk ( BrushColor ( "TextFillColorPrimaryBrush", Colors.Black ) );
			SKColor secondary = Sk ( BrushColor ( "TextFillColorSecondaryBrush", Colors.Gray ) );
			SKColor surface = Sk ( BrushColor ( "ApplicationBackgroundBrush", Colors.White ) );
			SKColor grid = secondary.WithAlpha ( 48 );
			chart.LegendTextPaint = new SolidColorPaint ( primary );
			chart.TooltipTextPaint = new SolidColorPaint ( primary );
			chart.TooltipBackgroundPaint = new SolidColorPaint ( surface );

			Axis categories = new( )
			{
				Labels = data.Labels.ToArray ( ),
				LabelsPaint = new SolidColorPaint ( secondary ),
				TextSize = 13,
				SeparatorsPaint = null,
				MinStep = 1,
				ForceStepToMin = true,
				/* Bar categories read top-down in ranked order. */
				IsInverted = !line
			};
			Axis values = new( )
			{
				MinLimit = 0,
				MinStep = 1,
				Labeler = value => value.ToString ( "N0" ),
				LabelsPaint = new SolidColorPaint ( secondary ),
				TextSize = 13,
				SeparatorsPaint = new SolidColorPaint ( grid ) { StrokeThickness = 1 }
			};
			chart.XAxes = [ line ? categories : values ];
			chart.YAxes = [ line ? values : categories ];
			chart.Series = data.Series.Select ( s => line ? LineSeries ( s, dark, secondary, surface ) : BarSeries ( s, data.Labels, dark, surface ) )
				.ToArray ( );
		}

		private static ISeries LineSeries ( ChartSeries source, bool dark, SKColor reference, SKColor surface )
		{
			SKColor color = source.IsReference ? reference : SKColor.Parse ( SeriesColor ( source.Slot, dark ) );
			return new LineSeries<int>
			{
				Name = source.Name,
				Values = source.Values.ToArray ( ),
				Fill = null,
				LineSmoothness = 0,
				Stroke = new SolidColorPaint ( color )
				{
					StrokeThickness = 2,
					/* The total is a reference line, so it is dashed rather than given a palette colour. */
					PathEffect = source.IsReference ? new DashEffect ( [ 6, 4 ] ) : null
				},
				GeometrySize = source.IsReference ? 7 : 9,
				GeometryStroke = new SolidColorPaint ( color ) { StrokeThickness = 2 },
				GeometryFill = new SolidColorPaint ( surface ),
				YToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue:N0} 件"
			};
		}

		/* Row tooltips take their header from the X formatter and each series line from the Y formatter. */
		private static ISeries BarSeries ( ChartSeries source, IReadOnlyList<string> labels, bool dark, SKColor surface ) =>
			new StackedRowSeries<int>
			{
				Name = source.Name,
				Values = source.Values.ToArray ( ),
				Fill = new SolidColorPaint ( SKColor.Parse ( SeriesColor ( source.Slot, dark ) ) ),
				/* A surface-coloured edge keeps a 2px gap between stacked segments. */
				Stroke = new SolidColorPaint ( surface ) { StrokeThickness = 2 },
				MaxBarWidth = 28,
				XToolTipLabelFormatter = point => point.Index < labels.Count ? labels [ point.Index ] : "",
				YToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue:N0} 件"
			};

		private void OnDataPointerDown ( IChartView sender, IEnumerable<ChartPoint> points )
		{
			ChartPoint? point = points.FirstOrDefault ( );
			IReadOnlyList<int> keys = Data?.Keys ?? [ ];
			if ( Kind != SeriesChartKind.Line || point == null || point.Index < 0 || point.Index >= keys.Count )
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
				data.Labels.Zip ( s.Values, ( label, value ) => $"{label} {value:N0}件" ) ) ) );

		private Color BrushColor ( string key, Color fallback ) =>
			TryFindResource ( key ) is SolidColorBrush brush ? brush.Color : fallback;

		private static SKColor Sk ( Color color ) => new(color.R, color.G, color.B, color.A);

		private static double Luminance ( Color color ) =>
			( ( 0.2126 * color.R ) + ( 0.7152 * color.G ) + ( 0.0722 * color.B ) ) / 255;
	}
}