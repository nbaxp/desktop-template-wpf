using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Threading;

namespace WpfAppTemplate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<double> lineValues = new(new double[30]);
        private DispatcherTimer timer = new();
        private LiveChartsCoreViewModel chartViewModel = new LiveChartsCoreViewModel();

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            // https://lvcharts.com/docs/wpf/2.0.0-beta.330/gallery
            // 折线图
            var lineSeries = new LineSeries<double>();
            lineSeries.Values = lineValues;
            chartViewModel.Series.Add(lineSeries);

            // 柱图
            var columnSeries = new ColumnSeries<double>();
            columnSeries.Values = lineValues;
            chartViewModel.Series.Add(columnSeries);

            // 数据源
            // this.lvc.EasingFunction = null;// 禁止动画
            this.lvc.DataContext = chartViewModel;

            // 定时器
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();

            // 模型
            var file = "./test.obj";
            var dc = new MainViewModel();
            this.DataContext = dc;
            view.AddHandler(Element3D.MouseDown3DEvent, new RoutedEventHandler((s, e) =>
            {
                var arg = e as MouseDown3DEventArgs;
                if (arg.HitTestResult == null)
                {
                    return;
                }
                if (arg.HitTestResult.ModelHit is SceneNode node && node.Tag is AttachedNodeViewModel vm)
                {
                    vm.Selected = !vm.Selected;
                }
            }));
            dc.LoadFile(file);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            status.Header = DateTime.Now.ToString();
            var rand = new Random();
            for (int i = 0; i < lineValues.Count; i++)
            {
                lineValues[i] = rand.Next(0, 100);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using var sp = new SerialPort("COM1");
            sp.Open();
            sp.WriteLine("test");
            sp.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            timer.Stop();
            base.OnClosing(e);
        }
    }
}