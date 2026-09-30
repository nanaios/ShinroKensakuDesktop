namespace ShinroKensakuDesktop.Models
{
	public record ChartBar ( string Label, int Count, double Maximum, string Color )
	{
		public string Caption => $"{Label}：{Count:N0} 件";
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
		public static List<ChartBar> Bars ( IEnumerable<DashboardSummary> values, string color = "#4F7DDE" )
		{
			List<DashboardSummary> rows = values.ToList ( );
			int maximum = Math.Max ( 1, rows.Select ( x => x.Count ).DefaultIfEmpty ( ).Max ( ) );
			return rows.Select ( x => new ChartBar ( x.Label, x.Count, maximum,
				x.Label == "就職" ? "#168577" : color ) ).ToList ( );
		}

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