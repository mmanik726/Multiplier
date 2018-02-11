//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Shapes;


////using LiveCharts;
////using LiveCharts.Wpf;


//namespace Multiplier
//{
//    /// <summary>
//    /// Interaction logic for GraphWindow.xaml
//    /// </summary>
//    public partial class GraphWindow : Window
//    {
//        //public SeriesCollection SeriesCollection { get; set; }
//        public string[] Labels { get; set; }
//        public Func<double, string> YFormatter { get; set; }

//        public GraphWindow()
//        {
//            InitializeComponent();
//        }


//        //public void ShowData(List<double> dataPts)
//        //{

//        //    SeriesCollection = new SeriesCollection
//        //    {
//        //        new LineSeries
//        //        {
//        //            Title = "Series 1",
//        //            //Values = new ChartValues<double> { 0 }
//        //            Values = new ChartValues<double> (dataPts)
//        //        },

//        //    };

//        //    //Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May" };
//        //    YFormatter = value => value.ToString("C"); //to currency


//        //    MyChart.DisableAnimations = true;

//        //    MyChart.Series = SeriesCollection;

//        //    var yAxis = new Axis();
//        //    yAxis.Title = "Sales";
//        //    yAxis.LabelFormatter = YFormatter;

//        //    //MyChart.AxisY.Add(new Axis() { Title = "sales", LabelFormatter = YFormatter});
//        //    MyChart.AxisY.Add(yAxis);



//        //    //var xAxis = new Axis();
//        //    //xAxis.Name = "Month";
//        //    //xAxis.Labels = Labels;
//        //    ////xAxis.MinValue = 25;
//        //    //xAxis.MaxValue = 25;
//        //    //MyChart.AxisX.Add(xAxis);



//        //    //////modifying any series values will also animate and update the chart
//        //    //Random rnd = new Random();
//        //    ////double Value = rnd.Next(1, 50);

//        //    //Task.Factory.StartNew(() =>
//        //    //{

//        //    //    for (int i = 0; i < 50; i++)
//        //    //    {
//        //    //        double Value = rnd.Next(1, 50);
//        //    //        this.SeriesCollection[0].Values.Add(Value);
//        //    //        System.Threading.Thread.Sleep(200);


//        //    //    }


//        //    //});

//        //    //MyChart.DataContext = this;
//        //    //DataContext = this;

//        //}

//    }





//}
