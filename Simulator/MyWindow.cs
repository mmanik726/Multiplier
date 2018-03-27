using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Add these namespaces
using System.Windows;
using System.Windows.Controls;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

//using OxyPlot.Wpf;
namespace Simulator
{
    //This will be your custom window class which is derieved    
    //from the base class Window.


    class MyWindow : Window
    {
        //public PlotModel SmaModel { get; private set; }

        public PlotModel PriceModel { get; private set; }
        //Declare some UI controls to be placed inside the
        //window.

        OxyPlot.Wpf.PlotView _SmaPlotView;
        OxyPlot.Wpf.PlotView _PricePlotView;

        //The controls can be placed only inside a panel.
        Grid _grid;

        public MyWindow()
        {
            //This is created just to show a reference , the 
            //below code can aswell be witten wihin this     
            //constructor.
            InitializeComponent();
            //Start();

        }


        public void ShowData(IEnumerable<SmaData> smadifPts, IEnumerable<SmaData> signalPts, IEnumerable<CrossData> allCrossData)
        {


            PriceModel = new PlotModel
            {
                Title = "Prices"
            };

            //var priceDt = smadifPts.Select(d => new DataPoint(Axis.ToDouble(d.Time), Convert.ToDouble(d.ActualPrice)));
            var priceDt = smadifPts.Select(d => new DataPoint(Axis.ToDouble(d.Time), Convert.ToDouble(d.ActualPrice)));

            var PriceSeries = new LineSeries
            {
                Title = "Price"
            };
            PriceSeries.Points.AddRange(priceDt);
            PriceModel.Series.Add(PriceSeries);




            var BuyScatterSeries = new ScatterSeries
            {
                Title = "buy",
                MarkerType = MarkerType.Circle
            };

            var buyPoints = allCrossData.Where(a => a.Action == "buy").Select((d) => new ScatterPoint(Axis.ToDouble(d.dt), Convert.ToDouble(d.CrossingPrice)));
            BuyScatterSeries.Points.AddRange(buyPoints);


            var SellScatterSeries = new ScatterSeries
            {
                Title = "sell",
                MarkerType = MarkerType.Circle
            };
            var SellPoints = allCrossData.Where(a => a.Action == "sell").Select((d) => new ScatterPoint(Axis.ToDouble(d.dt), Convert.ToDouble(d.CrossingPrice)));
            SellScatterSeries.Points.AddRange(SellPoints);



            PriceModel.Series.Add(BuyScatterSeries);
            PriceModel.Series.Add(SellScatterSeries);

            PriceModel.Axes.Add(new DateTimeAxis { MajorGridlineThickness = 1, Position = AxisPosition.Bottom, Minimum = PriceSeries.Points.First().X, Maximum = PriceSeries.Points.Last().X });
            PriceModel.Axes.Add(new LinearAxis { MajorGridlineThickness = 1, Position = AxisPosition.Left, Minimum = 20, Maximum = 400 });

            Dispatcher.Invoke(() => _PricePlotView.Model = PriceModel);



            return;


            //////SmaModel = new PlotModel
            //////{
            //////    Title = "Strategy 1",
            //////    PlotType = PlotType.Cartesian
            //////};

            //////var samdiffSeries = new LineSeries
            //////{
            //////    Title = "macd"
            //////};


            //////var dtpts1 = smadifPts.Select(d=> new DataPoint(Axis.ToDouble(d.Time) , d.SmaValue));
            //////samdiffSeries.Points.AddRange(dtpts1);



            //////var signalSeries = new LineSeries
            //////{
            //////    Title = "signal"
            //////};



            //////var signalDtpts = signalPts.Select(d => new DataPoint(Axis.ToDouble(d.Time), d.SmaValue));
            ////////var dtpts2 = signalPts.Select((d,i) => new DataPoint(i, d.SmaValue));
            //////signalSeries.Points.AddRange(signalDtpts);

            //////SmaModel.Axes.Add(new DateTimeAxis { MajorGridlineThickness = 1, Position = AxisPosition.Bottom, Minimum = signalSeries.Points.First().X, Maximum = signalSeries.Points.Last().X });
            //////SmaModel.Axes.Add(new LinearAxis { MajorGridlineThickness = 1, Position = AxisPosition.Left, Minimum = -10, Maximum = 10 });


            //////SmaModel.Series.Add(samdiffSeries);
            //////SmaModel.Series.Add(signalSeries);

            //////Dispatcher.Invoke(() => _SmaPlotView.Model = SmaModel);
            


        }



        void InitializeComponent()
        {


            //_SmaPlotView = new OxyPlot.Wpf.PlotView();

            _PricePlotView = new OxyPlot.Wpf.PlotView();
            //_SmaPlotView.Height = 100;
            //_oxyPV.Width = 200;

            //_searchButton = new Button { Height = 30, Width = 100, Content = "Search" };
            //_searchTextBox = new TextBox { Height = 30, Width = 100 };

            _grid = new Grid();

            //Add the controls inside the panel.
            //_panel.Children.Add(_searchButton);
            //_panel.Children.Add(_searchTextBox);

            _grid.Children.Add(_PricePlotView);
            //_grid.Children.Add(_SmaPlotView);

            //Set this panel as the content for this window.
            this.Content = _grid;
        }


        private void Start()
        {
            ////SmaModel = new PlotModel
            ////{
            ////    Title = "Example 1"
            ////};

            ////SmaModel.Series.Add(new FunctionSeries(Math.Cos, 0, 100, 0.1, "cos(x)"));



            //////var s1 = new LineSeries
            //////{
            //////    StrokeThickness = 0,
            //////    MarkerSize = 3,
            //////    MarkerStroke = OxyColors.ForestGreen,
            //////    MarkerType = MarkerType.Plus
            //////};

            //////foreach (var pt in Fern.Generate(2000))
            //////{
            //////    s1.Points.Add(new DataPoint(pt.X, -pt.Y));
            //////}




            ////// Create two line series (markers are hidden by default)
            ////var series1 = new LineSeries
            ////{
            ////    Title = "Series 1",
            ////    MarkerType = MarkerType.Circle
            ////};
            ////series1.Points.Add(new DataPoint(0, 0));
            ////series1.Points.Add(new DataPoint(10, 18));
            ////series1.Points.Add(new DataPoint(20, 12));
            ////series1.Points.Add(new DataPoint(30, 8));
            ////series1.Points.Add(new DataPoint(40, 15));

            ////var series2 = new LineSeries { Title = "Series 2", MarkerType = MarkerType.Square };
            ////series2.Points.Add(new DataPoint(0, 4));
            ////series2.Points.Add(new DataPoint(10, 12));
            ////series2.Points.Add(new DataPoint(20, 16));
            ////series2.Points.Add(new DataPoint(30, 25));
            ////series2.Points.Add(new DataPoint(40, 5));

            ////// Add the series to the plot model
            //////MyModel.Series.Add(series1);


            //////MyModel.Series.Add(series2);



            ////var xAxis = new LinearAxis();
            ////xAxis.Position = AxisPosition.Bottom;
            //////xAxis.Maximum = 5;

            ////SmaModel.Axes.Add(xAxis);


            ////_SmaPlotView.Model = SmaModel;



            ///////MyModel.InvalidatePlot(true);

            //////var x = oxyPV.MaxWidth = 5;


        }


    }
}
