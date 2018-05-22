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

using Newtonsoft.Json;

using System.IO;

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


        //OxyPlot.Wpf.PlotView _SmaPlotView;
        //OxyPlot.Wpf.PlotView _PricePlotView;

        StackPanel _stkPanel;

        Simulator1 _Sim = null;

        bool resEvendHandlerSet = false;


        int lastCommonInterval = 0;
        int lastBigSma = 0;
        int lastSmallSma = 0;

        DateTime lastCalculated = DateTime.Now;

        public GraphWindow()
        {
            Cursor = Cursors.Wait;
            InitializeComponent();

            //_stkPanel = new StackPanel();


            //_PricePlotView = new OxyPlot.Wpf.PlotView();
            //_stkPanel.Children.Add(_PricePlotView);
            

            //_SmaPlotView = new OxyPlot.Wpf.PlotView();
            //_stkPanel.Children.Add(_SmaPlotView);

            //_SmaPlotView.Margin = new Thickness(10);// (10, 171, 10, 10);
            //_SmaPlotView.Height = 400; //MyWindow.Height / 2;

            //_PricePlotView.Margin = new Thickness(10);//(10,10,10,153);
            //_PricePlotView.Height = 400; // this.Height / 2;


            //_stkPanel.Width = 600;
            //this.Content = _stkPanel;




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


        //public void DrawSeriesSim1(List<SeriesDetails> seriesList, List<CrossData> allCrossData = null)
        //{



        //    var SeriesModel = new PlotModel
        //    {
        //        Title = "Trades"
        //    };

        //    foreach (var series in seriesList)
        //    {

        //        if (series.SereiesName == "Price")
        //            continue;

        //        var seriesData = new LineSeries
        //        {
        //            Title = series.SereiesName,
        //            StrokeThickness = 1
        //        };
        //        var seriesDtpts = series.DataPoints.Select(s => new DataPoint(Axis.ToDouble(s.Time), s.SmaValue));
        //        seriesData.Points.AddRange(seriesDtpts);

        //        if (series.SereiesName == "Big_Sma")
        //            seriesData.Color = OxyColor.FromRgb(224, 50, 15);
        //        if (series.SereiesName == "Small_Sma")
        //            seriesData.Color = OxyColor.FromRgb(8, 150, 56);

        //        SeriesModel.Series.Add(seriesData);
        //    }


        //    SeriesModel.Axes.Add(new DateTimeAxis
        //    {
        //        MajorGridlineThickness = 1,
        //        MajorGridlineStyle = LineStyle.Solid,
        //        Position = AxisPosition.Bottom
        //        //Minimum = BuyScatterSeries.Points.First().X,
        //        //Maximum = BuyScatterSeries.Points.Last().X
        //    });



        //    var PriceModel = new PlotModel
        //    {
        //        Title = "Price"
        //    };

        //    if (allCrossData != null)
        //    {


        //        var priceLine = new LineSeries
        //        {
        //            Title = "Price",
        //            StrokeThickness = 1
        //        };

        //        var priceDt = seriesList.Where(a => a.SereiesName == "Price");
        //        if (priceDt.Count() > 0)
        //        {
        //            var prices = priceDt.First().DataPoints.Select(s => new DataPoint(Axis.ToDouble(s.Time), (double)s.ActualPrice));
        //            priceLine.Points.AddRange(prices);
        //            PriceModel.Series.Add(priceLine);
        //        }





        //        var BuyScatterSeries = new ScatterSeries
        //        {
        //            Title = "buy",
        //            MarkerType = MarkerType.Circle
        //        };



        //        double BUFFER = 0.60;


        //        var buyZero = allCrossData.Where(a => a.Action == "buy").Where(b => b.BufferedCrossingPrice == 0);
        //        if (buyZero.Count() > 0)
        //            buyZero.First().BufferedCrossingPrice = (double)buyZero.First().CrossingPrice + BUFFER;


        //        var sellZero = allCrossData.Where(a => a.Action == "sell").Where(b => b.BufferedCrossingPrice == 0);
        //        if (sellZero.Count() > 0)
        //            sellZero.First().BufferedCrossingPrice = (double)sellZero.First().CrossingPrice - BUFFER;

        //        var buyPoints = allCrossData.Where(a => a.Action == "buy").Select((d) => new ScatterPoint(Axis.ToDouble(d.dt), Convert.ToDouble(d.BufferedCrossingPrice)));
        //        BuyScatterSeries.Points.AddRange(buyPoints);




        //        BuyScatterSeries.MarkerFill = OxyColor.FromRgb(224, 50, 15); //OxyColor.FromRgb(255, 0, 0);

        //        var SellScatterSeries = new ScatterSeries
        //        {
        //            Title = "sell",
        //            MarkerType = MarkerType.Circle
        //        };
        //        var SellPoints = allCrossData.Where(a => a.Action == "sell").Select((d) => new ScatterPoint(Axis.ToDouble(d.dt), Convert.ToDouble(d.BufferedCrossingPrice)));
        //        SellScatterSeries.Points.AddRange(SellPoints);
        //        SellScatterSeries.MarkerFill = OxyColor.FromRgb(8, 150, 56);// OxyColor.FromRgb(0, 255, 0);


        //        //PriceModel.Series.Add(BuyScatterSeries);
        //        PriceModel.Series.Add(SellScatterSeries);

        //        PriceModel.Axes.Add(new DateTimeAxis
        //        {
        //            MajorGridlineThickness = 1,
        //            MajorGridlineStyle = LineStyle.Solid,
        //            Position = AxisPosition.Bottom
        //        });

        //    }

        //    //if (!resEvendHandlerSet)
        //    //{
        //    //    PriceModel.Axes[0].AxisChanged += MyWindow_Price_AxisChanged;

        //    //    SeriesModel.Axes[0].AxisChanged += MyWindow_Sma_AxisChanged;

        //    //    resEvendHandlerSet = true;
        //    //}


        //    Dispatcher.Invoke(() =>
        //    {


        //        //_SmaPlotView.Height = 400; //MyWindow.Height / 2;

        //        //_PricePlotView.Height = 400; // this.Height / 2;

        //        _SmaPlotView.Model = SeriesModel;
        //        _PricePlotView.Model = PriceModel;
        //    });
        //}


        public void FillInitialVaues(DateTime startTime, DateTime endTime, int interval, int largeSma, int smallSma, int signal)
        {
            Dispatcher.Invoke(() =>
            {
                txtInterval.Text = interval.ToString();
                txtLargeSma.Text = largeSma.ToString();
                txtSmallSma.Text = smallSma.ToString();
                txtSignal.Text = signal.ToString();
                dtFrom.SelectedDate = startTime;
                dtTo.SelectedDate = endTime;


                var fileNamePah = @"C:\Users\bobby\Source\Repos\Multiplier\Simulator\bin\Debug";//System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);


                DirectoryInfo di = new DirectoryInfo(fileNamePah);
                var TriedResultfiles = di.GetFiles("LTC-USD_TriedResultsList_S1*.json").OrderByDescending(f => f.CreationTime).ToList(); 

                foreach (var file in TriedResultfiles)
                {
                    TriedFileList.Items.Add(file.Name);
                }


            });




        }

        public void DrawSeriesSim1(List<SeriesDetails> seriesList, List<CrossData> allCrossData = null, double Pl = 0.0)
        {



            var SeriesModel = new PlotModel
            {
                Title = "Signals"
            };

            foreach (var series in seriesList)
            {

                if (series.SereiesName.Contains("Price"))
                    continue;

                var seriesData = new LineSeries
                {
                    Title = series.SereiesName,
                    StrokeThickness = 1
                };
                var seriesDtpts = series.DataPoints.Select(s => new DataPoint(Axis.ToDouble(s.Time), s.SmaValue));
                seriesData.Points.AddRange(seriesDtpts);

                if (series.SereiesName.Contains("Big_Sma"))
                    seriesData.Color = OxyColor.FromRgb(224, 50, 15);
                if (series.SereiesName.Contains("Small_Sma"))
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


            var plStr = (Pl == 0.0) ? "" : Math.Round(Pl, 2).ToString();


            var PriceModel = new PlotModel
            {
                Title = "P/L: " + plStr
            };

            if (allCrossData != null)
            {

                var priceDt = seriesList.Where(a => a.SereiesName.Contains("Price"));

                var priceLine = new LineSeries
                {
                    Title = priceDt.First().SereiesName,
                    StrokeThickness = 1
                };

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


                PriceModel.Series.Add(BuyScatterSeries);
                PriceModel.Series.Add(SellScatterSeries);

                PriceModel.Axes.Add(new DateTimeAxis
                {
                    MajorGridlineThickness = 1,
                    MajorGridlineStyle = LineStyle.Solid,
                    Position = AxisPosition.Bottom
                });

            }

            if (!resEvendHandlerSet)
            {
                PriceModel.Axes[0].AxisChanged += MyWindow_Price_AxisChanged;

                SeriesModel.Axes[0].AxisChanged += MyWindow_Sma_AxisChanged;

                resEvendHandlerSet = true;
            }

            //Task.Run(()=> 
            //{
            //    Dispatcher.Invoke(() =>
            //    {


            //        _SmaPlotView.Height = 400; //MyWindow.Height / 2;

            //        _PricePlotView.Height = 400; // this.Height / 2;

            //        _SmaPlotView.Model = SeriesModel;
            //        _PricePlotView.Model = PriceModel;
            //    });
            //});



            //_SmaPlotView.Model.InvalidatePlot();

            //_SmaPlotView.
            //_SmaPlotView.InvalidatePlot();
            //_SmaPlotView.InvalidateVisual();

            Dispatcher.Invoke(() =>
            {
                //_SmaPlotView.ActualModel?.Series.Clear();// = SeriesModel;
                //_PricePlotView.ActualModel?.Series.Clear(); // = PriceModel;



                //_SmaPlotView.Model = null;
                //_PricePlotView.Model = null;//?.InvalidatePlot(true);


                _SmaPlotView.Model =  SeriesModel;
                _PricePlotView.Model = PriceModel;

                //_PricePlotView.ActualModel?.InvalidatePlot(true);
                //_SmaPlotView.ActualModel?.InvalidatePlot(true);

                //_SmaPlotView.Model.InvalidatePlot(false);
                //_PricePlotView.Model.InvalidatePlot(false);
                Cursor = Cursors.Arrow;
            });

            
        }


        private void MyWindow_Price_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            //throw new NotImplementedException();

            _SmaPlotView.Model.Axes[0].Maximum = _PricePlotView.Model.Axes[0].ActualMaximum;

            _SmaPlotView.Model.Axes[0].Minimum = _PricePlotView.Model.Axes[0].ActualMinimum;
            _SmaPlotView.InvalidatePlot(false);

        }

        private void MyWindow_Sma_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            _PricePlotView.Model.Axes[0].Maximum = _SmaPlotView.Model.Axes[0].ActualMaximum;

            _PricePlotView.Model.Axes[0].Minimum = _SmaPlotView.Model.Axes[0].ActualMinimum;

            _PricePlotView.InvalidatePlot(false);

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (!chkAutoCalc.IsChecked.Value)
            {
                Cursor = Cursors.Arrow;
                return;
            }

            if (Cursor == Cursors.Wait)
            {
                Cursor = Cursors.Arrow;
                return;
            }

            if ((DateTime.Now - lastCalculated).TotalMilliseconds < 500)
            {
                Cursor = Cursors.Arrow;
                return;
            }

            btnCalculate_Click(this, null);
            lastCalculated = DateTime.Now;

        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {

            //System.Windows.c
            Cursor = Cursors.Wait;
            DateTime? tempStartTime = dtFrom.SelectedDate;//new DateTime(2018, 1, 1);
            DateTime? tempEndTime = dtTo.SelectedDate;//new DateTime(2018, 5, 1);

            DateTime startTime = (tempStartTime == null)? new DateTime(2018, 1, 1): tempStartTime.Value;
            DateTime endTime = (tempStartTime == null) ? new DateTime(2018, 5, 1) : tempEndTime.Value;

            var ProductName = "LTC-USD";

            if (tempStartTime == null || tempEndTime == null) 
            {
                Cursor = Cursors.Arrow;
                return;
            }

           


            if (txtInterval.Text == "" ||
                txtLargeSma.Text == "" ||
                txtSmallSma.Text == "" ||
                txtSignal.Text == "" )
            {
                Cursor = Cursors.Arrow;
                return;
            }



            int interval = 0;//Convert.ToInt16(txtInterval.Text);//30;
            int bigSmaLen = 0; //Convert.ToInt16(txtLargeSma.Text); // 100;
            int smallSmaLen = 0; //Convert.ToInt16(txtSmallSma.Text); //50;
            int SignalLen = 0; //Convert.ToInt16(txtSignal.Text); //10;

            try
            {

                interval = Convert.ToInt16(txtInterval.Text);//30;
                bigSmaLen = Convert.ToInt16(txtLargeSma.Text); // 100;
                smallSmaLen = Convert.ToInt16(txtSmallSma.Text); //50;
                SignalLen = Convert.ToInt16(txtSignal.Text); //10;
            }
            catch (Exception)
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("invalid input");
                return;
            }



            if (interval <= 0 ||
                bigSmaLen <= 0 ||
                smallSmaLen <= 0 ||
                SignalLen <= 0)
            {
                Cursor = Cursors.Arrow;
                return;
            }



            if (_Sim == null)
            {
                _Sim = new Simulator1(ProductName, interval, bigSmaLen, smallSmaLen);
            }
            else
            {
                if (!(lastCommonInterval == interval && lastBigSma == bigSmaLen && lastSmallSma == smallSmaLen))
                {
                    _Sim.Dispose();
                    _Sim = null;
                    _Sim = new Simulator1(ProductName, interval, bigSmaLen, smallSmaLen);

                }
            }



            Task.Run(() =>
            {
                var pl = _Sim.Calculate(startTime, endTime, SignalLen, true, true, true);

                lastCommonInterval = interval;
                lastBigSma = bigSmaLen;
                lastSmallSma = smallSmaLen;


                DrawSeriesSim1(_Sim.CurResultsSeriesList, _Sim.CurResultCrossList, pl);
                Dispatcher.Invoke(() =>
                {
                    Cursor = Cursors.Arrow;// Mouse.OverrideCursor = Cursors.None;
                });
            });
        }


        private void HandleMouseWheelRoll(MouseWheelEventArgs e, TextBox textBox)
        {
            if (textBox.Text == "")
            {
                textBox.Text = "0";
            }
            else
            {
                try
                {
                    var val = Convert.ToInt16(textBox.Text);
                }
                catch (Exception)
                {
                    textBox.Text = "0";
                }
            }

            int curValue = Convert.ToInt16(textBox.Text);

            if (e.Delta > 0)
            {

                textBox.Text = (curValue + (e.Delta / 100)).ToString();
            }
            else
            {
                textBox.Text = (curValue + (e.Delta / 100)).ToString();
            }
        }

        private void txtInterval_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleMouseWheelRoll(e, txtInterval);
        }

        private void txtSmallSma_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleMouseWheelRoll(e, txtSmallSma);
        }

        private void txtLargeSma_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleMouseWheelRoll(e, txtLargeSma);
        }

        private void txtSignal_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleMouseWheelRoll(e, txtSignal);
        }

        private void TriedFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedFile = (String)TriedFileList.SelectedItem;
            var results = getTopResultsFromFile(selectedFile);

            ResulstList.Items.Clear();

            results.ForEach((a) => ResulstList.Items.Add(a));
        }

        //private void TriedFileList_Selected(object sender, RoutedEventArgs e)
        //{
        //    var selectedFile = (String)TriedFileList.SelectedItem;
        //    var results = getTopResultsFromFile(selectedFile);

        //    ResulstList.Items.Clear();

        //    results.ForEach((a)=> ResulstList.Items.Add(a));


        //}


        private List<string> getTopResultsFromFile(string fileName)
        {
            var resultList = new List<string>();

            string basePath = @"C:\Users\bobby\Source\Repos\Multiplier\Simulator\bin\Debug";

            var fileNamePath = basePath + @"\" + fileName;

            try
            {
                var lst = JsonConvert.DeserializeObject<List<ResultData>>(File.ReadAllText(fileNamePath));
                //Logger.WriteLog("Found " + lst.Count() + " tried records from " + s + " to " + e);
                resultList.Clear();


                lst = lst.Take(50).ToList();
                var sortedByIntervalResults = lst.OrderBy(r => r.intervals.interval).ToList();


                foreach (var result in sortedByIntervalResults)
                {
                    resultList.Add(
                        Math.Round(result.Pl, 2) + "," +
                        result.intervals.interval + "," +
                        result.SimStartDate.ToShortDateString() + "," +
                        result.SimEndDate.ToShortDateString() + "," +
                        result.intervals.bigSmaLen + "," +
                        result.intervals.smallSmaLen + "," +
                        result.intervals.SignalLen + "," );

                }


            }
            catch (Exception ex)
            {
                //Logger.WriteLog("cant read tried intervals list: " + fileNamePath);
                //throw;
            }

            return resultList;
        }

        private void ResulstList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var slectedItem = (String)ResulstList.SelectedItem;

            if (slectedItem == null)
                return;

            var values = slectedItem.Split(',');

            var interval = values[1];
            var simStartDate = values[2];
            var simEndDate = values[3];
            var bigSma = values[4];
            var smallSma = values[5];
            var signal = values[6];

            txtInterval.Text = interval;
            dtFrom.SelectedDate = Convert.ToDateTime(simStartDate);
            dtTo.SelectedDate = Convert.ToDateTime(simEndDate);
            txtLargeSma.Text = bigSma;
            txtSmallSma.Text = smallSma;
            txtSignal.Text = signal;

            btnCalculate_Click(this, null);

        }
    }





}
