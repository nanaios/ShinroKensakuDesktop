namespace ShinroKensakuDesktop.Models
{
	/* Each kind owns one colour and one icon across every page (see App.xaml and ThemeColors). */
	public enum StatKind
	{
		Total,
		Study,
		Work
	}

	public enum ChangeDirection
	{
		None,
		Up,
		Down,
		Flat
	}

	public enum NoticeSeverity
	{
		None,
		Info,
		Success,
		Warning,
		Error
	}

	/* A KPI tile: the value with its unit, an optional share, and a change against a reference value. */
	public sealed record StatItem ( StatKind Kind, string Label, string Value )
	{
		public string? Before { get; init; }
		public string? Share { get; init; }
		public string? Note { get; init; }
		public ChangeDirection Direction { get; init; }
		public string? Change { get; init; }
		public string? ChangeNote { get; init; }
		public bool IsPlaceholder { get; init; }

		public static StatItem Placeholder ( StatKind kind, string label ) => new(kind, label, "") { IsPlaceholder = true };

		public static ChangeDirection DirectionOf ( int difference ) => difference switch
		{
			> 0 => ChangeDirection.Up,
			< 0 => ChangeDirection.Down,
			_ => ChangeDirection.Flat
		};

		/* The sign is carried by the arrow, so the number is shown without it. */
		public static string ChangeOf ( int difference ) => difference == 0 ? "±0" : Math.Abs ( difference ).ToString ( "N0" );
	}

	/* Ratio is the bar length against the largest row on the same card, so bars compare across groups. */
	public sealed record BreakdownRow ( StatKind Kind, string Label, string Value, string Share, double Ratio );

	public sealed record BreakdownGroup ( StatKind Kind, string Title, string Total, IReadOnlyList<BreakdownRow> Rows );

	/* Only the first, last and current years are labelled, so narrow cards never crowd the axis. */
	public sealed record YearBar ( int Year, int Count, double Ratio, bool IsCurrent, bool ShowLabel = true )
	{
		public string Label => Year.ToString ( );
		public string ValueText => Count.ToString ( "N0" );
		public string Tip => $"{Year}年度 {Count:N0}件";
	}

	/* Bar ratios are against the largest absolute change, so the diverging bars share one scale. */
	public sealed record MethodComparisonRow ( StatKind Kind, string Label, int Baseline, int Current, double Ratio )
	{
		public int Difference => Current - Baseline;
		public ChangeDirection Direction => StatItem.DirectionOf ( Difference );
		public string ChangeText => StatItem.ChangeOf ( Difference );
		public double IncreaseRatio => Difference > 0 ? Ratio : 0;
		public double DecreaseRatio => Difference < 0 ? Ratio : 0;
		public string RateText => YearComparison.Rate ( Baseline, Current );
		public string? RateTip => Baseline == 0 ? YearComparison.ZeroBaselineTip : null;
	}
}
