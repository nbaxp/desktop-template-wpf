using HelixToolkit.Wpf;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Media.Media3D;
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
        private LiveChartsCoreViewModel vm = new LiveChartsCoreViewModel();

        public MainWindow()
        {
            InitializeComponent();
            LiveCharts2Init();
        }

        private void LiveCharts2Init()
        {
            // https://lvcharts.com/docs/wpf/2.0.0-beta.330/gallery
            // 折线图
            var lineSeries = new LineSeries<double>();
            lineSeries.Values = lineValues;
            vm.Series.Add(lineSeries);
            // 柱图
            var columnSeries = new ColumnSeries<double>();
            columnSeries.Values = lineValues;
            vm.Series.Add(columnSeries);
            // 数据源
            // this.lvc.EasingFunction = null;// 禁止动画
            this.lvc.DataContext = vm;
            // 定时器
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();
            // 模型
            //var file = "./test.obj";
            var file = "./test.stl";
            ModelReader reader = file.EndsWith(".obj")? new ObjReader():new StLReader();
            var model3d = reader.Read(file);
            this.camera.Position = new Point3D(model3d.Bounds.X + model3d.Bounds.SizeX / 2,
                model3d.Bounds.Y + model3d.Bounds.SizeY / 2, 100);
            //this.camera.LookDirection.
            this.model.Content = model3d;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var rand = new Random();
            for (int i = 0; i < lineValues.Count; i++)
            {
                lineValues[i] = rand.Next(0, 100);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
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

        private void position_Click(object sender, RoutedEventArgs e)
        {
            positionBtn.Content = camera.Position.ToString();
        }

        private void HelixViewport3D_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            
        }
    }
}