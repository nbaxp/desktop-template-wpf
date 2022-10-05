using System.Windows;
using LiveChartsCore;

namespace WpfAppTemplate;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // LiveCharts.Configure(o => o.AddDefaultMappers());
    }

}
