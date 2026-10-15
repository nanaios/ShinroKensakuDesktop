namespace ShinroKensakuDesktop.Models
{
	/* Slot is a fixed palette position chosen per entity, so narrowing the range never repaints a series. */
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
		public int Difference => Current - Baseline;
		public string DifferenceText => Difference.ToString ( "+#,0;-#,0;0" ) + " 件";

		public string RateText =>
			Baseline == 0 ? "—（比較元が0件）" : $"{( Current - Baseline ) * 100.0 / Baseline:+0.0;-0.0;0.0}%";
	}

	public static class ChartData
	{
		public const string Other = "その他";

		/* Seven named methods plus "その他" keep every line in the validated eight-colour palette. */
		public const int MaxMethods = 7;

		public static readonly string [ ] Kinds = [ "進学", "就職" ];

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

		public static SeriesChart MethodTrend ( IEnumerable<DashboardGroup> groups, IReadOnlyList<int> years,
			string? department = null )
		{
			List<DashboardGroup> all = groups.ToList ( );
			Dictionary<string, int> slots = MethodSlots ( all );
			List<DashboardGroup> rows = Filter ( all, years, department );
			List<ChartSeries> series = rows.GroupBy ( x => Fold ( x.Method, slots ) )
				.Select ( g => new ChartSeries ( g.Key,
					years.Select ( y => g.Where ( x => x.Year == y ).Sum ( x => x.Count ) ).ToList ( ), slots [ g.Key ] ) )
				.OrderBy ( x => x.Slot ).ToList ( );
			return new SeriesChart ( YearLabels ( years ), series, years );
		}

		/* Methods are categories, so the range total per method is split into 進学 and 就職 segments. */
		public static SeriesChart MethodBreakdown ( IEnumerable<DashboardGroup> groups, IReadOnlyList<int> years,
			string? department = null )
		{
			List<DashboardGroup> all = groups.ToList ( );
			Dictionary<string, int> slots = MethodSlots ( all );
			List<DashboardGroup> rows = Filter ( all, years, department );
			List<string> methods = rows.GroupBy ( x => Fold ( x.Method, slots ) )
				.OrderBy ( x => x.Key == Other ).ThenByDescending ( x => x.Sum ( y => y.Count ) ).ThenBy ( x => x.Key )
				.Select ( x => x.Key ).ToList ( );
			List<ChartSeries> series = Kinds.Select ( ( kind, i ) => new ChartSeries ( kind,
				methods.Select ( m => rows.Where ( x => x.Kind == kind && Fold ( x.Method, slots ) == m ).Sum ( x => x.Count ) )
					.ToList ( ), i ) ).ToList ( );
			return new SeriesChart ( methods, series, [ ] );
		}

		/* Ranked over every year and department rather than the current filter, so colours stay with their method. */
		private static Dictionary<string, int> MethodSlots ( List<DashboardGroup> all )
		{
			List<string> ranked = all.GroupBy ( x => x.Method )
				.OrderByDescending ( x => x.Sum ( y => y.Count ) ).ThenBy ( x => x.Key, StringComparer.Ordinal )
				.Select ( x => x.Key ).ToList ( );
			bool folds = ranked.Count > MaxMethods + 1;
			Dictionary<string, int> slots = ranked.Take ( folds ? MaxMethods : ranked.Count )
				.Select ( ( method, i ) => ( method, i ) ).ToDictionary ( x => x.method, x => x.i );
			slots.TryAdd ( Other, folds ? MaxMethods : slots.Count );
			return slots;
		}

		/* A null department means every department. */
		public static List<DashboardGroup> Filter ( IEnumerable<DashboardGroup> groups, IReadOnlyCollection<int> years,
			string? department ) =>
			groups.Where ( x => years.Contains ( x.Year ) && ( department == null || x.Department == department ) )
				.ToList ( );

		private static string Fold ( string method, Dictionary<string, int> slots ) =>
			slots.ContainsKey ( method ) ? method : Other;

		private static List<string> YearLabels ( IEnumerable<int> years ) => years.Select ( y => $"{y}年度" ).ToList ( );

		public static List<YearComparison> Compare ( IEnumerable<DashboardGroup> groups, int? baseline, int? current )
		{
			if ( !baseline.HasValue || !current.HasValue )
			{
				return [ ];
			}

			List<DashboardGroup> rows = groups.ToList ( );
			return new [ ] { "合計", "進学", "就職" }.Select ( kind => new YearComparison ( kind,
					rows.Where ( x => x.Year == baseline && ( kind == "合計" || x.Kind == kind ) ).Sum ( x => x.Count ),
					rows.Where ( x => x.Year == current && ( kind == "合計" || x.Kind == kind ) ).Sum ( x => x.Count ) ) )
				.ToList ( );
		}
	}
}