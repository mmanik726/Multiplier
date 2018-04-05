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

using CoinbaseExchange.NET.Data;
//using OxyPlot.Wpf;
namespace Simulator
{
    //This will be your custom window class which is derieved    
    //from the base class Window.


    class MyWindow : Window
    {

        //Declare some UI controls to be placed inside the
        //window.

        OxyPlot.Wpf.PlotView _SmaPlotView;
        OxyPlot.Wpf.PlotView _PricePlotView;

        OxyPlot.Wpf.PlotView _PLPlotView;

        //The controls can be placed only inside a panel.
        StackPanel _stkPanel;

        public MyWindow()
        {
            //This is created just to show a reference , the 
            //below code can aswell be witten wihin this     
            //constructor.
            InitializeComponent();
            //Start();
            this.SizeChanged += MyWindow_SizeChanged;


            _PLPlotView.Margin = new Thickness(10);
            _PLPlotView.Height = 300;

            _SmaPlotView.Margin = new Thickness(10);// (10, 171, 10, 10);
            _SmaPlotView.Height = 290; //MyWindow.Height / 2;

            _PricePlotView.Margin = new Thickness(10);//(10,10,10,153);
            _PricePlotView.Height = 290; // this.Height / 2;





        }

        private void MyWindow_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            //throw new NotImplementedException();

            _PricePlotView.Model.Axes[0].Maximum = _SmaPlotView.Model.Axes[0].ActualMaximum;

            _PricePlotView.Model.Axes[0].Minimum = _SmaPlotView.Model.Axes[0].ActualMinimum;

            _PricePlotView.InvalidatePlot(false);


        }


        private void MyWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //throw new NotImplementedException();

            System.Diagnostics.Debug.WriteLine(this.ActualHeight);


            //_SmaPlotView.Margin = new Thickness(10, 0, 10, 10);
            //_SmaPlotView.Height = 700;// 150; //MyWindow.Height / 2;

            //_SmaPlotView.Margin = new Thickness(10, 10, 10, 10);
            //_SmaPlotView.Height = 500; // this.Height / 2;


        }




        public void DrawSeries(List<List<SmaData>> seriesList, List<CrossData> allCrossData = null)
        {

            var PlModel = new PlotModel
            {
                Title = "Series"
            };

            foreach (var series in seriesList)
            {
                var seriesData = new LineSeries
                {
                    Title = "",
                    StrokeThickness = 1
                };
                var BalanceDtpts = series.Select(s => new DataPoint(Axis.ToDouble(s.Time), s.SmaValue));
                seriesData.Points.AddRange(BalanceDtpts);
                PlModel.Series.Add(seriesData);
            }


            PlModel.Axes.Add(new DateTimeAxis
            {
                MajorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                Position = AxisPosition.Bottom
                //Minimum = BuyScatterSeries.Points.First().X,
                //Maximum = BuyScatterSeries.Points.Last().X
            });



            if (allCrossData != null)
            {
                var BuyScatterSeries = new ScatterSeries
                {
                    Title = "buy",
                    MarkerType = MarkerType.Circle
                };

                var buyPoints = allCrossData.Where(a => a.Action == "buy").Select((d) => new ScatterPoint(Axis.ToDouble(d.dt), Convert.ToDouble(d.BufferedCrossingPrice)));
                BuyScatterSeries.Points.AddRange(buyPoints);


                var SellScatterSeries = new ScatterSeries
                {
                    Title = "sell",
                    MarkerType = MarkerType.Circle
                };
                var SellPoints = allCrossData.Where(a => a.Action == "sell").Select((d) => new ScatterPoint(Axis.ToDouble(d.dt), Convert.ToDouble(d.BufferedCrossingPrice)));
                SellScatterSeries.Points.AddRange(SellPoints);



                PlModel.Series.Add(BuyScatterSeries);
                PlModel.Series.Add(SellScatterSeries);
            }


            Dispatcher.Invoke(() => _PLPlotView.Model = PlModel);
        }



        public void ShowData(IEnumerable<SmaData> smadifPts, IEnumerable<SmaData> signalPts, IEnumerable<CrossData> allCrossData)
        {


            var PlModel = new PlotModel
            {
                Title = "PL"
            };

            var BalanceSeries = new LineSeries
            {
                Title = "Balance(x10K)",
                StrokeThickness = 1
            };
            var BalanceDtpts = allCrossData.Where(c => c.Action == "sell").Select(s => new DataPoint(Axis.ToDouble(s.dt), s.CalculatedBalance / 10));
            BalanceSeries.Points.AddRange(BalanceDtpts);
            PlModel.Series.Add(BalanceSeries);

            var PlSeries = new LineSeries
            {
                Title = "PL",
                StrokeThickness = 1
            };
            var PlDtpts = allCrossData.Where(c => c.Action == "sell").Select(s => new DataPoint(Axis.ToDouble(s.dt), s.CalculatedNetPL));
            PlSeries.Points.AddRange(PlDtpts);
            PlModel.Series.Add(PlSeries);



            PlModel.Axes.Add(new DateTimeAxis
            {
                MajorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                Position = AxisPosition.Bottom,
                Minimum = BalanceSeries.Points.First().X,
                Maximum = BalanceSeries.Points.Last().X
            });

            PlModel.Axes.Add(new LinearAxis
            {
                MajorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                Position = AxisPosition.Left
            });

            Dispatcher.Invoke(() => _PLPlotView.Model = PlModel);


            var PriceModel = new PlotModel
            {
                Title = "Prices"
            };

            //var priceDt = smadifPts.Select(d => new DataPoint(Axis.ToDouble(d.Time), Convert.ToDouble(d.ActualPrice)));
            var priceDt = smadifPts.Select(d => new DataPoint(Axis.ToDouble(d.Time), Convert.ToDouble(d.ActualPrice)));

            var PriceSeries = new LineSeries
            {
                Title = "Price",
                StrokeThickness = 1
            };
            PriceSeries.Points.AddRange(priceDt);
            PriceModel.Series.Add(PriceSeries);



            //var SmaPrice_len100 = new LineSeries
            //{
            //    Title = "sma 100"
            //};
            //var smaDtpts100 = priceDt.Select((d) => d.Y).ToList().SMA(100).Select((s, i) => new DataPoint(priceDt.ElementAt(i).X, s));
            //SmaPrice_len100.Points.AddRange(smaDtpts100);
            //PriceModel.Series.Add(SmaPrice_len100);

            //var SmaPrice_len35 = new LineSeries
            //{
            //    Title = "sma 35"
            //};
            //var smaDtpts35 = priceDt.Select((d) => d.Y).ToList().SMA(35).Select((s, i) => new DataPoint(priceDt.ElementAt(i).X, s));
            //SmaPrice_len35.Points.AddRange(smaDtpts35);
            //PriceModel.Series.Add(SmaPrice_len35);


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

            PriceModel.Axes.Add(new DateTimeAxis
            {
                MajorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                Position = AxisPosition.Bottom,
                Minimum = BuyScatterSeries.Points.First().X,
                Maximum = BuyScatterSeries.Points.Last().X
            });
            PriceModel.Axes.Add(new LinearAxis
            {
                MajorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                Position = AxisPosition.Left

            });

            Dispatcher.Invoke(() => _PricePlotView.Model = PriceModel);





            //return;


            var SmaModel = new PlotModel
            {
                Title = "Strategy 1",
                PlotType = PlotType.Cartesian
            };




            var samdiffSeries = new LineSeries
            {
                Title = "macd",
                StrokeThickness = 1
            };


            var smaDataPts = smadifPts.Select(d => new DataPoint(Axis.ToDouble(d.Time), d.SmaValue));
            samdiffSeries.Points.AddRange(smaDataPts);



            var signalSeries = new LineSeries
            {
                Title = "signal",
                StrokeThickness = 1
            };


            var signalDtpts = signalPts.Select(d => new DataPoint(Axis.ToDouble(d.Time), d.SmaValue));
            //var dtpts2 = signalPts.Select((d,i) => new DataPoint(i, d.SmaValue));
            signalSeries.Points.AddRange(signalDtpts);




            SmaModel.Series.Add(samdiffSeries);
            SmaModel.Series.Add(signalSeries);


            var smaXAxis = new DateTimeAxis { MajorGridlineThickness = 1, Position = AxisPosition.Bottom, Minimum = BuyScatterSeries.Points.First().X, Maximum = BuyScatterSeries.Points.Last().X };

            smaXAxis.MajorGridlineStyle = LineStyle.Solid;

            smaXAxis.AxisChanged += MyWindow_AxisChanged;

            SmaModel.Axes.Add(smaXAxis);
            SmaModel.Axes.Add(new LinearAxis
            {
                MajorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                Position = AxisPosition.Left,
                Minimum = -10,
                Maximum = 10
            });

            Dispatcher.Invoke(() => _SmaPlotView.Model = SmaModel);







        }



        void InitializeComponent()
        {

            _stkPanel = new StackPanel();


            _PLPlotView = new OxyPlot.Wpf.PlotView();
            _stkPanel.Children.Add(_PLPlotView);

            _PricePlotView = new OxyPlot.Wpf.PlotView();
            _stkPanel.Children.Add(_PricePlotView);

            _SmaPlotView = new OxyPlot.Wpf.PlotView();
            _stkPanel.Children.Add(_SmaPlotView);




            //_grid.Children.Add(_SmaPlotView);

            //Set this panel as the content for this window.
            this.Content = _stkPanel;




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
