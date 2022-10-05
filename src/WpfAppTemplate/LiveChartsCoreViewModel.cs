using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace WpfAppTemplate;

public class LiveChartsCoreViewModel
{
    public LiveChartsCoreViewModel()
    {
        this.XAxes = new Axis[] { new Axis
        {
            Name = "X 轴",
            NamePaint = GetPaint(),
            MinLimit = 0,
            MaxLimit = 30
        }};
        this.YAxes = new Axis[] { new Axis
        {
            Name = "Y 轴",
            NamePaint = GetPaint(),
            MinLimit = 0,
            MaxLimit = 30
        }};
    }

    private IPaint<SkiaSharpDrawingContext> GetPaint()
    {
        return new SolidColorPaint { Color = SKColors.Red, FontFamily = LiveChartsSkiaSharp.MatchChar('汉') };
    }

    public List<ISeries> Series { get; set; } = new List<ISeries>();

    public Axis[] XAxes { get; set; }

    public Axis[] YAxes { get; set; }
}
