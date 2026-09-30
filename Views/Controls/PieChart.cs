using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ShinroKensakuDesktop.Models;

namespace ShinroKensakuDesktop.Views.Controls;

public sealed class PieChart : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable<ChartBar>), typeof(PieChart),
        new PropertyMetadata(null, (d, _) => ((PieChart)d).RenderChart()));

    public IEnumerable<ChartBar>? ItemsSource
    {
        get => (IEnumerable<ChartBar>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static readonly string[] Palette = ["#4F7DDE", "#168577", "#D98B27", "#9263C5", "#CC5777", "#3B9CAC", "#8A9638", "#A86B46", "#6776A6", "#B35EAB", "#628D70"];

    public static Geometry SliceGeometry(double start, double fraction)
    {
        if (fraction <= 0) return Geometry.Empty;
        if (fraction >= 1) return new EllipseGeometry(new Point(120, 120), 112, 112);
        Point At(double turn) => new(120 + 112 * Math.Cos(turn * 2 * Math.PI - Math.PI / 2),
            120 + 112 * Math.Sin(turn * 2 * Math.PI - Math.PI / 2));
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(120, 120), true, true);
            context.LineTo(At(start), true, false);
            context.ArcTo(At(start + fraction), new Size(112, 112), 0, fraction > 0.5,
                SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        return geometry;
    }

    private void RenderChart()
    {
        var rows = ItemsSource?.ToList() ?? [];
        var total = rows.Sum(x => (double)Math.Max(0, x.Count));
        var layout = new WrapPanel();
        var canvas = new Canvas { Width = 240, Height = 240, Margin = new Thickness(0, 8, 24, 8) };
        var legend = new StackPanel { Margin = new Thickness(0, 12, 0, 12), MaxWidth = 360 };
        layout.Children.Add(canvas);
        layout.Children.Add(legend);
        double start = 0;
        if (total == 0)
        {
            canvas.Children.Add(new Ellipse { Width = 224, Height = 224, Margin = new Thickness(8), Fill = Brushes.LightGray });
            legend.Children.Add(new TextBlock { Text = "データなし" });
        }
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var fraction = total == 0 ? 0 : Math.Max(0, row.Count) / total;
            var brush = (Brush)new BrushConverter().ConvertFromString(row.Label == "進学" ? Palette[0] : row.Label == "就職" ? Palette[1] : Palette[i % Palette.Length])!;
            brush.Freeze();
            var caption = $"{row.Label}  {row.Count:N0} 件（{fraction:P1}）";
            var slice = new Path { Data = SliceGeometry(start, fraction), Fill = brush, ToolTip = caption };
            System.Windows.Automation.AutomationProperties.SetName(slice, caption);
            canvas.Children.Add(slice);
            var entry = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            entry.Children.Add(new Border { Width = 12, Height = 12, Background = brush, Margin = new Thickness(0, 4, 10, 0), VerticalAlignment = VerticalAlignment.Top });
            entry.Children.Add(new TextBlock { Text = caption, TextWrapping = TextWrapping.Wrap });
            legend.Children.Add(entry);
            start += fraction;
        }
        Content = layout;
    }
}
