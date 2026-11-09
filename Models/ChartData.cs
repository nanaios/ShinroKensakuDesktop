namespace ShinroKensakuDesktop.Models
{
	/* Slot is a fixed palette position chosen per entity, so narrowing the range never repaints a series.
	   Slots 0 and 1 are reserved for 進学 and 就職; a negative slot is the neutral grey of その他. */
	public record ChartSeries ( string Name, IReadOnlyList<int> Values, int Slot, bool IsReference = false )
	{
		public int Total => Values.Sum ( );
	}

	/* Keys hold the year behind each label so a clicked point can open that year's search. */
	public record SeriesChart ( IReadOnlyList<string> Labels, IReadOnlyList<ChartSeries> Series, IReadOnlyList<int> Keys )
	{
		public static SeriesChart Empty { get; } = new([ ], [ ], [ ]);
		public bool HasData => Labels.Count > 0 && Series.Any ( x => x.Total > 0 );
	}

	public record YearComparison ( string Label, int Baseline, int Current )
	{
		public const string ZeroBaselineTip = "基準年度が0件のため、増減率は算出しません。";
		public int Difference => Current - Baseline;
		public string DifferenceText => Difference.ToString ( "+#,0;-#,0;0" );
		public string RateText => Rate ( Baseline, Current );

		public static string Rate ( int baseline, int current ) =>
			baseline == 0 ? "—" : $"{( current - baseline ) * 100.0 / baseline:+0.0;-0.0;0.0}%";
	}

	public static class ChartData
	{
		public const string Other = "その他";
		public const string Study = "進学";
		public const string Work = "就職";

		/* Palette slots 2–7 are left for methods once blue and orange are reserved for the two kinds. They are handed
		   out green, yellow, pink, purple, red, dark green, so a few methods never get two greens. */
		public const int FirstMethodSlot = 2;
		public const int MaxMethods = 6;
		public const int OtherSlot = -1;
		private static readonly int [ ] MethodSlotOrder = [ 2, 3, 4, 6, 7, 5 ];

		public static readonly string [ ] Kinds = [ Study, Work ];

		public static StatKind KindOf ( string kind ) => kind switch
		{
			Study => StatKind.Study,
			Work => StatKind.Work,
			_ => StatKind.Total
		};

		public static SeriesChart KindTrend ( IEnumerable<DashboardGroup> groups, IReadOnlyList<int> years,
			string? department = null )
		{
			List<DashboardGroup> rows = Filter ( groups, years, department );
			List<ChartSeries> series =
			[
				new("合計", years.Select ( y => rows.Where ( x => x.Year == y ).Sum ( x => x.Count ) ).ToList ( ), 0, true),
				.. Kinds.Select ( ( kind, i ) => new ChartSeries ( kind,
					years.Select ( y => rows.Where ( x => x.Year == y && x.Kind == kind ).Sum ( x => x.Count ) ).ToList ( ),
					i ) )
			];
			return new SeriesChart ( YearLabels ( years ), series, years );
		}

		/* Only 進学 records: the 就職 method is the 就職 kind, which the kind trend already draws. */
		public static SeriesChart MethodTrend ( IEnumerable<DashboardGroup> groups, IReadOnlyList<int> years,
			string? department = null )
		{
			List<DashboardGroup> study = groups.Where ( x => x.Kind == Study ).ToList ( );
			Dictionary<string, int> slots = MethodSlots ( study );
			List<DashboardGroup> rows = Filter ( study, years, department );
			List<ChartSeries> series = rows.GroupBy ( x => Fold ( x.Method, slots ) )
				.Select ( g => new ChartSeries ( g.Key,
					years.Select ( y => g.Where ( x => x.Year == y ).Sum ( x => x.Count ) ).ToList ( ), slots [ g.Key ] ) )
				.OrderBy ( x => x.Slot < 0 ? int.MaxValue : x.Slot ).ToList ( );
			return new SeriesChart ( YearLabels ( years ), series, years );
		}

		/* Rows grouped under their kind; bar ratios share the largest method, shares are of the whole selection. */
		public static List<BreakdownGroup> MethodBreakdown ( IEnumerable<DashboardGroup> rows )
		{
			List<DashboardGroup> list = rows.ToList ( );
			int total = list.Sum ( x => x.Count );
			var methods = list.GroupBy ( x => ( x.Kind, x.Method ) )
				.Select ( g => ( g.Key.Kind, g.Key.Method, Count: g.Sum ( x => x.Count ) ) )
				.Where ( x => x.Count > 0 ).ToList ( );
			int max = methods.Count == 0 ? 0 : methods.Max ( x => x.Count );
			return Kinds.Select ( kind => ( kind, items: methods.Where ( x => x.Kind == kind )
					.OrderByDescending ( x => x.Count ).ThenBy ( x => x.Method, StringComparer.Ordinal ).ToList ( ) ) )
				.Where ( x => x.items.Count > 0 )
				.Select ( x => new BreakdownGroup ( KindOf ( x.kind ), x.kind, x.items.Sum ( y => y.Count ).ToString ( "N0" ),
					x.items.Select ( y => new BreakdownRow ( KindOf ( x.kind ), y.Method, y.Count.ToString ( "N0" ),
						Percent ( y.Count, total ), max == 0 ? 0 : ( double ) y.Count / max ) ).ToList ( ) ) )
				.ToList ( );
		}

		public static List<YearBar> YearBars ( IEnumerable<DashboardGroup> groups, int? current )
		{
			List<( int Year, int Count )> years = groups.GroupBy ( x => x.Year ).OrderBy ( x => x.Key )
				.Select ( x => ( x.Key, x.Sum ( y => y.Count ) ) ).ToList ( );
			int max = years.Count == 0 ? 0 : years.Max ( x => x.Count );
			return years.Select ( ( x, i ) => new YearBar ( x.Year, x.Count, max == 0 ? 0 : ( double ) x.Count / max,
				x.Year == current, i == 0 || i == years.Count - 1 || x.Year == current ) ).ToList ( );
		}

		public static string Percent ( int count, int total )
		{
			if ( total == 0 )
			{
				return "—";
			}

			double value = count * 100.0 / total;
			return value is > 0 and < 1 ? "1%未満" : $"{value:0}%";
		}

		/* Ranked over every year and department rather than the current filter, so colours stay with their method. */
		private static Dictionary<string, int> MethodSlots ( List<DashboardGroup> all )
		{
			List<string> ranked = all.GroupBy ( x => x.Method )
				.OrderByDescending ( x => x.Sum ( y => y.Count ) ).ThenBy ( x => x.Key, StringComparer.Ordinal )
				.Select ( x => x.Key ).ToList ( );
			Dictionary<string, int> slots = ranked.Take ( MaxMethods )
				.Select ( ( method, i ) => ( method, i ) ).ToDictionary ( x => x.method, x => MethodSlotOrder [ x.i ] );
			slots.TryAdd ( Other, OtherSlot );
			return slots;
		}

		/* A null department means every department. */
		public static List<DashboardGroup> Filter ( IEnumerable<DashboardGroup> groups, IReadOnlyCollection<int> years,
			string? department ) =>
			groups.Where ( x => years.Contains ( x.Year ) && ( department == null || x.Department == department ) )
				.ToList ( );

		private static string Fold ( string method, Dictionary<string, int> slots ) =>
			slots.ContainsKey ( method ) ? method : Other;

		/* The axis title carries 年度, so each tick is just the year. */
		private static List<string> YearLabels ( IEnumerable<int> years ) => years.Select ( y => y.ToString ( ) ).ToList ( );

		public static List<YearComparison> Compare ( IEnumerable<DashboardGroup> groups, int? baseline, int? current )
		{
			if ( !baseline.HasValue || !current.HasValue )
			{
				return [ ];
			}

			List<DashboardGroup> rows = groups.ToList ( );
			return new [ ] { "合計", Study, Work }.Select ( kind => new YearComparison ( kind,
					rows.Where ( x => x.Year == baseline && ( kind == "合計" || x.Kind == kind ) ).Sum ( x => x.Count ),
					rows.Where ( x => x.Year == current && ( kind == "合計" || x.Kind == kind ) ).Sum ( x => x.Count ) ) )
				.ToList ( );
		}

		/* Every method present in either year, 進学 before 就職, largest current count first. */
		public static List<MethodComparisonRow> CompareMethods ( IEnumerable<DashboardGroup> groups, int? baseline,
			int? current )
		{
			if ( !baseline.HasValue || !current.HasValue )
			{
				return [ ];
			}

			var methods = groups.Where ( x => x.Year == baseline || x.Year == current )
				.GroupBy ( x => ( x.Kind, x.Method ) )
				.Select ( g => ( g.Key.Kind, g.Key.Method,
					Baseline: g.Where ( x => x.Year == baseline ).Sum ( x => x.Count ),
					Current: g.Where ( x => x.Year == current ).Sum ( x => x.Count ) ) )
				.ToList ( );
			int max = methods.Count == 0 ? 0 : methods.Max ( x => Math.Abs ( x.Current - x.Baseline ) );
			return methods.OrderBy ( x => Array.IndexOf ( Kinds, x.Kind ) is var i && i < 0 ? int.MaxValue : i )
				.ThenByDescending ( x => x.Current ).ThenBy ( x => x.Method, StringComparer.Ordinal )
				.Select ( x => new MethodComparisonRow ( KindOf ( x.Kind ), x.Method, x.Baseline, x.Current,
					max == 0 ? 0 : ( double ) Math.Abs ( x.Current - x.Baseline ) / max ) )
				.ToList ( );
		}
	}
}
