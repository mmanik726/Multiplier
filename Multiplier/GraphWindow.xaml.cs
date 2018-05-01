using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;



using Simulator;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using CoinbaseExchange.NET.Data;


namespace Multiplier
{
    /// <summary>
    /// Interaction logic for GraphWindow.xaml
    /// </summary>
    public partial class GraphWindow : Window
    {


        OxyPlot.Wpf.PlotView _SmaPlotView;
        OxyPlot.Wpf.PlotView _PricePlotView;

        StackPanel _stkPanel;

        public GraphWindow()
        {
            InitializeComponent();

            _stkPanel = new StackPanel();


            _PricePlotView = new OxyPlot.Wpf.PlotView();
            _stkPanel.Children.Add(_PricePlotView);


            _SmaPlotView = new OxyPlot.Wpf.PlotView();
            _stkPanel.Children.Add(_SmaPlotView);

            _SmaPlotView.Margin = new Thickness(10);// (10, 171, 10, 10);
            _SmaPlotView.Height = 400; //MyWindow.Height / 2;

            _PricePlotView.Margin = new Thickness(10);//(10,10,10,153);
            _PricePlotView.Height = 400; // this.Height / 2;



            this.Content = _stkPanel;



            
        }



        //public class SmaData
        //{
        //    public double SmaValue { get; set; }
        //    public decimal ActualPrice { get; set; }
        //    public DateTime Time { get; set; }
        //}

        //public class SeriesDetails
        //{
        //    public List<SmaData> series { get; set; }
        //    public string SereiesName { get; set; }
        //}


        //public class CrossData
        //{
        //    public int sl;
        //    public DateTime dt { get; set; }
        //    public decimal CrossingPrice { get; set; }
        //    public double BufferedCrossingPrice { get; set; }
        //    public string Action { get; set; }
        //    public double cossDiff { get; set; }
        //    public double smaValue { get; set; }
        //    public string comment { get; set; }
        //    public double CalculatedBalance { get; set; }
        //    public double CalculatedNetPL { get; set; }
        //}


        public void DrawSeriesSim1(List<SeriesDetails> seriesList, List<CrossData> allCrossData = null)
        {



            var SeriesModel = new PlotModel
            {
                Title = "Trades"
            };

            foreach (var series in seriesList)
            {

                if (series.SereiesName == "Price")
                    continue;

                var seriesData = new LineSeries
                {
                    Title = series.SereiesName,
                    StrokeThickness = 1
                };
                var seriesDtpts = series.DataPoints.Select(s => new DataPoint(Axis.ToDouble(s.Time), s.SmaValue));
                seriesData.Points.AddRange(seriesDtpts);

                if (series.SereiesName == "Big_Sma")
                    seriesData.Color = OxyColor.FromRgb(224, 50, 15);
                if (series.SereiesName == "Small_Sma")
                    seriesData.Color = OxyColor.FromRgb(8, 150, 56);

                SeriesModel.Series.Add(seriesData);
            }


            SeriesModel.Axes.Add(new DateTimeAxis
            {
                MajorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                Position = AxisPosition.Bottom
                //Minimum = BuyScatterSeries.Points.First().X,
                //Maximum = BuyScatterSeries.Points.Last().X
            });



            var PriceModel = new PlotModel
            {
                Title = "Price"
            };

            if (allCrossData != null)
            {


                var priceLine = new LineSeries
                {
                    Title = "Price",
                    StrokeThickness = 1
                };

                var priceDt = seriesList.Where(a => a.SereiesName == "Price");
                if (priceDt.Count() > 0)
                {
                    var prices = priceDt.First().DataPoints.Select(s => new DataPoint(Axis.ToDouble(s.Time), (double)s.ActualPrice));
                    priceLine.Points.AddRange(prices);
                    PriceModel.Series.Add(priceLine);
                }





                var BuyScatterSeries = new ScatterSeries
                {
                    Title = "buy",
                    MarkerType = MarkerType.Circle
                };



                double BUFFER = 0.60;


                var buyZero = allCrossData.Where(a => a.Action == "buy").Where(b => b.BufferedCrossingPrice == 0);
                if (buyZero.Count() > 0)
                    buyZero.First().BufferedCrossingPrice = (double)buyZero.First().CrossingPrice + BUFFER;


                var sellZero = allCrossData.Where(a => a.Action == "sell").Where(b => b.BufferedCrossingPrice == 0);
                if (sellZero.Count() > 0)
                    sellZero.First().BufferedCrossingPrice = (double)sellZero.First().CrossingPrice - BUFFER;

                var buyPoints = allCrossData.Where(a => a.Action == "buy").Select((d) => new ScatterPoint(Axis.ToDouble(d.dt), Convert.ToDouble(d.BufferedCrossingPrice)));
                BuyScatterSeries.Points.AddRange(buyPoints);




                BuyScatterSeries.MarkerFill = OxyColor.FromRgb(224, 50, 15); //OxyColor.FromRgb(255, 0, 0);

                var SellScatterSeries = new ScatterSeries
                {
                    Title = "sell",
                    MarkerType = MarkerType.Circle
                };
                var SellPoints = allCrossData.Where(a => a.Action == "sell").Select((d) => new ScatterPoint(Axis.ToDouble(d.dt), Convert.ToDouble(d.BufferedCrossingPrice)));
                SellScatterSeries.Points.AddRange(SellPoints);
                SellScatterSeries.MarkerFill = OxyColor.FromRgb(8, 150, 56);// OxyColor.FromRgb(0, 255, 0);


                //PriceModel.Series.Add(BuyScatterSeries);
                PriceModel.Series.Add(SellScatterSeries);

                PriceModel.Axes.Add(new DateTimeAxis
                {
                    MajorGridlineThickness = 1,
                    MajorGridlineStyle = LineStyle.Solid,
                    Position = AxisPosition.Bottom
                });

            }

            //if (!resEvendHandlerSet)
            //{
            //    PriceModel.Axes[0].AxisChanged += MyWindow_Price_AxisChanged;

            //    SeriesModel.Axes[0].AxisChanged += MyWindow_Sma_AxisChanged;

            //    resEvendHandlerSet = true;
            //}


            Dispatcher.Invoke(() =>
            {


                _SmaPlotView.Height = 400; //MyWindow.Height / 2;

                _PricePlotView.Height = 400; // this.Height / 2;

                _SmaPlotView.Model = SeriesModel;
                _PricePlotView.Model = PriceModel;
            });
        }



    }





}
