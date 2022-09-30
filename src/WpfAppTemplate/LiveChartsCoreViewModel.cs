using LiveChartsCore;
using System.Collections.Generic;

namespace WpfAppTemplate;

public class LiveChartsCoreViewModel
{
    public List<ISeries> Series { get; set; } = new List<ISeries>();
}