using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Data;
using CoinbaseExchange.NET.Endpoints.PublicData;
using System.Threading;
using CoinbaseExchange.NET;
using CoinbaseExchange.NET.Utilities;

namespace Simulator
{
    class Simulator2 : IDisposable
    {

        MovingAverage BasePrices_MA;
        MovingAverage SmallSma_MA;
        MovingAverage BigSma_MA;

        static Random _random = new Random();

        static object _TriedListLock = new object();

        private int COMMON_INTERVAL;
        private int LARGE_SMA_LEN;
        private int SMALL_SMA_LEN;
        private int BASE_SMA_LEN;

        static MyWindow _GraphingWindow;
        
        static HashSet<IntervalData> _TriedIntervalList = new HashSet<IntervalData>();

        static string ProductName = "LTC-USD";


        public List<SmaData> ActualPriceList;
        public List<SmaData> Price;
        public List<SmaData> BigSma;
        public List<SmaData> SmallSma;

        internal MyWindow GraphWindow;


        public static void Start()
        {

            Console.WriteLine("Starting Simulator 2");

            ShowGraphingForm();

            while (true)
            {
                var am = "";
                while (!(am == "a" || am == "m"))
                {
                    Console.WriteLine("Enter m for manual a for automatic");
                    am = Console.ReadLine();
                }


                if (am == "m")
                {
                    //Simulator2.ManualSimulate2();
                    Simulator2.ManualSimulate2();
                }
                else
                {
                    //Simulator2.AutoSimulate2();
                    Simulator2.AutoSimulate2();
                }

            }
        }

            private static void ShowGraphingForm()
        { 

            Thread thread = new Thread(() =>
            {

                _GraphingWindow = new MyWindow();
                System.Windows.Application _wpfApplication = new System.Windows.Application();
                _wpfApplication.Run(_GraphingWindow);

            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

        }

        public Simulator2(ref TickerClient ticker, string productName, int CommonInterval = 30, int LargeSmaLen = 50, int SmallSmaLen = 12, int BasePriceSma = 10, bool downloadLatestData = false)
        {
            COMMON_INTERVAL = CommonInterval;
            LARGE_SMA_LEN = LargeSmaLen;
            SMALL_SMA_LEN = SmallSmaLen;

            BASE_SMA_LEN = BasePriceSma;



            BasePrices_MA = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, BASE_SMA_LEN, 10, downloadLatestData, false);

            //BigSma_MA = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, LARGE_SMA_LEN, 10, downloadLatestData, false);
            //SmallSma_MA = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, SMALL_SMA_LEN, 10, downloadLatestData, false);



            var smaDtPoints_BasePrice = getSmaWithDataPts(BasePrices_MA, BASE_SMA_LEN);

            //var t = smaDtPoints_BasePrice.Select(d=>d.SmaValue)

            var SmaDtPoints_Big = getSmaWithDataPts(smaDtPoints_BasePrice, LARGE_SMA_LEN);

            var SmaDtPoints_Small = getSmaWithDataPts(smaDtPoints_BasePrice, SMALL_SMA_LEN);


            var maxdate = DateTime.Now;

            if (SmaDtPoints_Big.First().Time > SmaDtPoints_Small.First().Time)
            {
                maxdate = SmaDtPoints_Big.First().Time;
            }
            else
            {
                maxdate = SmaDtPoints_Small.First().Time;
            }

            if (maxdate < smaDtPoints_BasePrice.First().Time)
            {
                maxdate = smaDtPoints_BasePrice.First().Time;
            }


            var firstCommonDate = maxdate; //SmaDtPoints_Big.First().Time;


            ActualPriceList = BasePrices_MA.SmaDataPts_Candle.Where(c => c.Time >= firstCommonDate).Select(d => new SmaData { ActualPrice = d.Close, SmaValue = (double)d.Close, Time = d.Time }).ToList();

            Price = smaDtPoints_BasePrice.Where(d => d.Time >= firstCommonDate).ToList();

            BigSma = SmaDtPoints_Big.Where(d => d.Time >= firstCommonDate).ToList();

            SmallSma = SmaDtPoints_Small.Where(d => d.Time >= firstCommonDate).ToList();



        }


        private List<SmaData> getSmaWithDataPts(List<SmaData> inputMA, int Input_SMA_LEN)
        {

            var smaDtPtsReversed = inputMA.OrderBy((d) => d.Time);

            //var smaPoints = smaDtPtsReversed.Select((d) => d.SmaValue).ToList().SMA(Input_SMA_LEN).ToList();
            var smaPoints = smaDtPtsReversed.Select((d) => d.SmaValue).ToList().EMA(Input_SMA_LEN).ToList();

            var requiredSmaDtPts = smaDtPtsReversed.Skip(Input_SMA_LEN - 1).ToList();


            var smaWithDataPts = smaPoints.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = requiredSmaDtPts.ElementAt(i).ActualPrice,
                Time = requiredSmaDtPts.ElementAt(i).Time
            }).ToList();

            return smaWithDataPts;
        }

        private List<SmaData> getSmaWithDataPts(MovingAverage inputMA, int Input_SMA_LEN)
        {

            var smaDtPtsReversed = inputMA.SmaDataPts_Candle.OrderBy((d) => d.Time);

            //var smaPoints = smaDtPtsReversed.Select((d) => (double)d.Close).ToList().SMA(Input_SMA_LEN).ToList();

            var smaPoints = smaDtPtsReversed.Select((d) => (double)d.Close).ToList().EMA(Input_SMA_LEN).ToList();

            var requiredSmaDtPts = smaDtPtsReversed.Skip(Input_SMA_LEN - 1).ToList();


            var smaWithDataPts = smaPoints.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = requiredSmaDtPts.ElementAt(i).Close,
                Time = requiredSmaDtPts.ElementAt(i).Time
            }).ToList();

            return smaWithDataPts;
        }

        public double Calculate(DateTime simStartDate, DateTime simEndDate, bool printTrades = true, bool renderGraph = false)
        {
            ActualPriceList = ActualPriceList.Where(p => (p.Time >= simStartDate) && (p.Time < simEndDate)).ToList();
            Price = Price.Where(p => (p.Time >= simStartDate) && (p.Time < simEndDate)).ToList();
            BigSma = BigSma.Where(p => (p.Time >= simStartDate) && (p.Time < simEndDate)).ToList();
            SmallSma = SmallSma.Where(p => (p.Time >= simStartDate) && (p.Time < simEndDate)).ToList();

            var allBuys = Price.Where((d, i) => (d.SmaValue > BigSma.ElementAt(i).SmaValue) && d.SmaValue > SmallSma.ElementAt(i).SmaValue).ToList();

            var allSells = Price.Where((d, i) => (d.SmaValue < SmallSma.ElementAt(i).SmaValue)).ToList();


            //var allBuys = Price.Where((d, i) => ((double)d.ActualPrice > BigSma.ElementAt(i).SmaValue) && d.SmaValue > SmallSma.ElementAt(i).SmaValue).ToList();

            //var allSells = Price.Where((d, i) => ((double)d.ActualPrice < SmallSma.ElementAt(i).SmaValue)).ToList();

            Utilities crossCalc = new Utilities();

            var strtTimer2 = DateTime.Now;

            var crossList = crossCalc.Getcrossings_Linq(allBuys, allSells);

            var timeTaken2 = (DateTime.Now - strtTimer2).Milliseconds;
            Console.WriteLine("linq time taken (ms): " + timeTaken2.ToString());


            var pl = CalculatePl_Compounding(ref crossList, printTrades);




            if (renderGraph)
            {
                //List<List<SmaData>> allSeries = new List<List<SmaData>>();
                List<SeriesDetails> allSereis2 = new List<SeriesDetails>();

                //allSeries.Add( Price);
                //allSeries.Add(BigSma);
                //allSeries.Add(SmallSma);

                allSereis2.Add(new SeriesDetails { DataPoints = ActualPriceList, SereiesName = "Actual Price" });

                allSereis2.Add(new SeriesDetails { DataPoints = Price, SereiesName = "Price" });
                allSereis2.Add(new SeriesDetails { DataPoints = BigSma, SereiesName = "Big_Sma" });
                allSereis2.Add(new SeriesDetails { DataPoints = SmallSma, SereiesName = "Small_Sma" });

                GraphWindow.DrawSeries(allSereis2, crossList);
            }


            return pl;

        }

        public static void AutoSimulate2()
        {

            _TriedIntervalList.Clear();

            TickerClient Ticker = new TickerClient(ProductName);

            //wait for ticker to get ready
            Thread.Sleep(2 * 1000);



            DateTime dtStart = DateTime.MinValue;

            while (dtStart == DateTime.MinValue)
            {
                Console.WriteLine("Enter simulation start date:");
                var inDt = Console.ReadLine();
                DateTime.TryParse(inDt, out dtStart);
            }


            DateTime autoStartDate = dtStart;// new DateTime(2017, 10, 1);

            //DateTime autoEndDate = new DateTime(2018, 2, 13);//DateTime.Now;


            DateTime dtEnd = DateTime.MinValue;

            while (dtEnd == DateTime.MinValue)
            {
                Console.WriteLine("Enter simulation end date, enter n to use todays date:");
                var inDt = Console.ReadLine();

                if (inDt == "n")
                {
                    dtEnd = DateTime.Now;
                    break;
                }
                DateTime.TryParse(inDt, out dtEnd);
            }


            DateTime autoEndDate = dtEnd; //DateTime.Now;


            List<ResultData> allCombinedResultList = new List<ResultData>();




            var startTime = DateTime.Now;

            int simCount = 0;


            int Each_Sim_Count = 50;//50;

            while (simCount == 0)
            {
                Console.WriteLine("Enter number of simulation count, " + Each_Sim_Count + " minimun");
                simCount = Convert.ToInt32(Console.ReadLine());
            }


            List<Task> allSimTasks = new List<Task>();


            var eachBatchCount = 1000;


            var lastBatchCompletionTime = DateTime.Now;

            Int32 SLEEP_TIME_SEC = 30 * 1000;


            for (int batch = eachBatchCount; batch < simCount + eachBatchCount; batch += eachBatchCount)
            {


                Console.WriteLine("starting batch: " + (batch - eachBatchCount) + " - " + batch);

                if (batch > eachBatchCount)
                    Thread.Sleep(SLEEP_TIME_SEC);

                var threadCount = Math.Ceiling((eachBatchCount / (double)Each_Sim_Count));


                for (int i = 0; i < threadCount; i++)
                {
                    var curTask = Task.Factory.StartNew(() =>
                    {
                        var returnedResult = RunSim_ThreadSafe(ref Ticker, autoStartDate, autoEndDate, Each_Sim_Count);
                        allCombinedResultList.AddRange(returnedResult);
                    });

                    allSimTasks.Add(curTask);
                }



                Task.WaitAll(allSimTasks.ToArray());


                if (batch >= eachBatchCount)
                {
                    var timeTakenLastBatch = (DateTime.Now - lastBatchCompletionTime).TotalMinutes;
                    Console.WriteLine("Time taken to complete last batch (min): " + timeTakenLastBatch);

                    var expectedCompletionTime = (timeTakenLastBatch + ((SLEEP_TIME_SEC / 1000) / 60)) * ((simCount - batch) / eachBatchCount);
                    Console.WriteLine("Expected completion time: " + expectedCompletionTime + " min, " + DateTime.Now.AddMinutes(expectedCompletionTime));
                    lastBatchCompletionTime = DateTime.Now;
                }
            }











            Console.WriteLine("\nTop 5 profit results\n");

            IntervalData bestValues = null;

            for (int i = 0; i < 5; i++)
            {
                var best = allCombinedResultList.Where(d => d.Pl == allCombinedResultList.Max(a => a.Pl)).First();

                if (bestValues == null)
                    bestValues = best.intervals;

                var resMsg = "best result: " + i + "\n"
                    + "PL: " + best.Pl + "\n"
                    + "Interval: " + best.intervals.interval + "\n"
                    + "Base price sma: " + best.intervals.basePriceSmaLen + "\n"
                    + "big sma: " + best.intervals.bigSmaLen + "\n"
                    + "small sma: " + best.intervals.smallSmaLen + "\n";


                Logger.WriteLog("\n" + resMsg);

                allCombinedResultList.Remove(best);

            }



            var S = new Simulator2(ref Ticker, ProductName, bestValues.interval, bestValues.bigSmaLen, bestValues.smallSmaLen, bestValues.basePriceSmaLen, false);
            S.GraphWindow = _GraphingWindow;
            S.Calculate(autoStartDate, autoEndDate, true, true);



            //Console.WriteLine("\nTop 5 loss results\n");
            //for (int i = 0; i < 5; i++)
            //{
            //    var best = allCombinedResultList.Where(d => d.Pl == allCombinedResultList.Min(a => a.Pl)).First();
            //    var resMsg = "best result: " + i + "\n"
            //        + "PL: " + best.Pl + "\n"
            //        + "Interval: " + best.intervals.interval + "\n"
            //        + "Base price sma: " + best.intervals.basePriceSmaLen + "\n"
            //        + "big sma: " + best.intervals.bigSmaLen + "\n"
            //        + "small sma: " + best.intervals.smallSmaLen + "\n";


            //    Logger.WriteLog("\n" + resMsg);

            //    allCombinedResultList.Remove(best);

            //}


            Console.WriteLine("time taken for " + allSimTasks.Count() * Each_Sim_Count + " iterations (min): " + (DateTime.Now - startTime).TotalMinutes);

            //Console.ReadLine();


        }


        private static List<ResultData> RunSim_ThreadSafe(ref TickerClient inputTicker, DateTime startDt, DateTime endDt, int inputSimCount)
        {
            int lastCommonInterval = 0;
            int lastBigSma = 0;
            int lastSmallSma = 0;

            Simulator2 S = null;


            TickerClient Ticker = inputTicker;


            DateTime autoStartDate = startDt;


            DateTime autoEndDate = endDt;


            List<ResultData> resultList = new List<ResultData>();


            int simCount = inputSimCount;


            //Parallel.For(0, simCount, i =>
            for (int i = 0; i < simCount; i++)
            {

                IntervalData interval = null;
                interval = getIntervalData();
                try
                {

                    if (S == null)
                    {
                        S = new Simulator2(ref Ticker, ProductName, interval.interval, interval.bigSmaLen, interval.smallSmaLen, interval.basePriceSmaLen, true);
                        S.GraphWindow = _GraphingWindow;
                    }
                    else
                    {
                        if (!(lastCommonInterval == interval.interval && lastBigSma == interval.bigSmaLen && lastSmallSma == interval.smallSmaLen))
                        {
                            S.Dispose();
                            S = null;
                            S = new Simulator2(ref Ticker, ProductName, interval.interval, interval.bigSmaLen, interval.smallSmaLen, interval.basePriceSmaLen);
                            S.GraphWindow = _GraphingWindow;
                        }
                    }

                    var plResult = S.Calculate(autoStartDate, autoEndDate, false, true);

                    resultList.Add(new ResultData { Pl = plResult, intervals = interval });


                    lastCommonInterval = interval.interval;
                    lastBigSma = interval.bigSmaLen;
                    lastSmallSma = interval.smallSmaLen;

                }
                catch (Exception ex)
                {
                    Console.WriteLine("invalid input / error in calc");
                }

            }

            return resultList;
        }

        static IntervalData getIntervalData()
        {
            var newInterval = new IntervalData
            {
                interval = _random.Next(10, 200),
                bigSmaLen = _random.Next(90, 200),
                smallSmaLen = _random.Next(5, 100),
                basePriceSmaLen = _random.Next(1, 5)
            };


            lock (_TriedListLock)
            {
                var beforeAdding = _TriedIntervalList.Count();

                _TriedIntervalList.Add(newInterval);

                var afterAdding = _TriedIntervalList.Count();

                while (beforeAdding == afterAdding)
                {
                    newInterval = new IntervalData
                    {
                        interval = _random.Next(10, 200),
                        bigSmaLen = _random.Next(90, 200),
                        smallSmaLen = _random.Next(5, 100),
                        basePriceSmaLen = _random.Next(1, 5)
                    };


                    beforeAdding = _TriedIntervalList.Count();

                    _TriedIntervalList.Add(newInterval);

                    afterAdding = _TriedIntervalList.Count();

                }
            }


            return newInterval;
        }


        public static void ManualSimulate2()
        {
            int lastCommonInterval = 0;
            int lastBigSma = 0;
            int lastSmallSma = 0;

            Simulator2 S = null;

            var ProductName = "LTC-USD";
            TickerClient Ticker = new TickerClient(ProductName);

            //wait for ticker to get ready
            Thread.Sleep(2 * 1000);



            try
            {




                Console.WriteLine("Enter simulation start date:");
                var inDt = Console.ReadLine();
                DateTime dt;
                DateTime.TryParse(inDt, out dt);

                Console.WriteLine("Enter Common time interval in minutes");
                var inputCommonInterval = Convert.ToInt16(Console.ReadLine());


                Console.WriteLine("Enter base price sma len: ");
                var basePriceSma = Convert.ToInt16(Console.ReadLine());

                Console.WriteLine("Enter big sma length");
                var inputBigSmaLen = Convert.ToInt16(Console.ReadLine());

                Console.WriteLine("Enter small sma length");
                var inputSmallSmaLen = Convert.ToInt16(Console.ReadLine());

                //Console.WriteLine("Enter signal len: ");
                //var inputSmaLen = Convert.ToInt16(Console.ReadLine());




                if (S == null)
                {
                    S = new Simulator2(ref Ticker, ProductName, inputCommonInterval, inputBigSmaLen, inputSmallSmaLen, basePriceSma, true);
                    S.GraphWindow = _GraphingWindow;
                }
                else
                {
                    if (!(lastCommonInterval == inputCommonInterval && lastBigSma == inputBigSmaLen && lastSmallSma == inputSmallSmaLen))
                    {
                        S.Dispose();
                        S = null;
                        S = new Simulator2(ref Ticker, ProductName, inputCommonInterval, inputBigSmaLen, inputSmallSmaLen, basePriceSma);
                        S.GraphWindow = _GraphingWindow;
                    }
                }

                S.Calculate(dt, DateTime.Now, true, true);

                lastCommonInterval = inputCommonInterval;
                lastBigSma = inputBigSmaLen;
                lastSmallSma = inputSmallSmaLen;

            }
            catch (Exception ex)
            {
                Console.WriteLine("invalid input / error in calc: " + ex.Message);
            }



        }


        double CalculatePl_Compounding(ref List<CrossData> allCrossings, bool printTrade = true)
        {
            if (allCrossings.Count() == 0)
            {
                Console.WriteLine("No crossing data!");
                return 0;
            }

            allCrossings = allCrossings.OrderByDescending((d) => d.dt).ToList();

            decimal curProdSize = 0;
            decimal USDbalance = 8000;
            const decimal FEE_PERCENTAGE = 0.003m;
            decimal totalFee = 0.0m;

            if (allCrossings.First().Action == "buy")
            {
                allCrossings.RemoveAt(allCrossings.IndexOf(allCrossings.First()));
            }

            if (allCrossings.Last().Action == "sell")
            {
                allCrossings.RemoveAt(allCrossings.IndexOf(allCrossings.Last()));
            }

            StringBuilder trans = new StringBuilder();

            StringBuilder transCsv = new StringBuilder();

            if (printTrade)
            {
                //Console.WriteLine("Time\t\t\taction\t\tPrice\t\tFee\t\tSize\tPL\t\tBalance");
                trans.AppendLine("Time\t\t\taction\t\tPrice\t\tFee\t\tSize\tPL\t\t\tBalance");
                transCsv.AppendLine("Time\tAction\tPrice\tFee\tSize\tPL\tBalance");
            }


            var plList = new List<decimal>();

            var buyAtPrice = 0.0m;
            var sellAtPrice = 0.0m;

            var buyFee = 0.0m;
            var sellFee = 0.0m;

            const decimal BUFFER = 0.30m;//1.20m;//1.50m;//1.0m; //0.75m;


            const decimal STOP_LOSS_PERCENTAGE = 0.02m;

            var lastAction = "";

            int stopLossSale = 0;



            for (int i = allCrossings.Count() - 1; i >= 0; i--)
            {
                var cross = allCrossings[i];

                if (lastAction == cross.Action)
                {
                    Console.WriteLine("\n\t\t\t<<<- Potential error here ->>>\n");
                }

                if (cross.Action == "buy")
                {
                    buyAtPrice = cross.CrossingPrice + BUFFER;
                    cross.BufferedCrossingPrice = (double)buyAtPrice;

                    buyFee = (USDbalance) * FEE_PERCENTAGE;
                    curProdSize = (USDbalance - buyFee) / buyAtPrice;
                    totalFee += buyFee;

                    USDbalance = USDbalance - (curProdSize * buyAtPrice) - buyFee;

                    cross.CalculatedBalance = Convert.ToDouble(USDbalance);

                    if (printTrade)
                    {
                        var buyMsg = cross.dt.ToString(@"yyyy-MM-dd HH:mm:ss") + "\t"
                            + cross.Action + ((cross.comment != null) ? "_M" : "") + "\t\t"
                            + Math.Round(buyAtPrice, 2).ToString() + "\t\t"
                            + Math.Round(buyFee, 2).ToString() + "\t\t"
                            + Math.Round(curProdSize, 2).ToString() + "\t\t\t"
                            + Math.Round(USDbalance, 2).ToString();
                        //Console.WriteLine(buyMsg);
                        trans.AppendLine(buyMsg);


                        var buyMsgCsv = cross.dt.ToString(@"yyyy-MM-dd HH:mm:ss") + "\t"
                            + cross.Action + ((cross.comment != null) ? "_M" : "") + "\t"
                            + Math.Round(buyAtPrice, 2).ToString() + "\t"
                            + Math.Round(buyFee, 2).ToString() + "\t"
                            + Math.Round(curProdSize, 2).ToString() + "\t"
                            + Math.Round(USDbalance, 2).ToString();
                        //Console.WriteLine(buyMsg);
                        transCsv.AppendLine(buyMsgCsv);
                    }

                    lastAction = "buy";
                }

                if (cross.Action == "sell")
                {
                    bool isStopLossSale = false;
                    sellAtPrice = cross.CrossingPrice - BUFFER;
                    cross.BufferedCrossingPrice = (double)sellAtPrice;

                    var stopLossPrice = buyAtPrice - (buyAtPrice * STOP_LOSS_PERCENTAGE);
                    if (sellAtPrice < stopLossPrice)
                    {
                        sellAtPrice = stopLossPrice - BUFFER;
                        cross.BufferedCrossingPrice = (double)sellAtPrice;
                        stopLossSale++;
                        isStopLossSale = true;
                    }

                    sellFee = (curProdSize * sellAtPrice) * FEE_PERCENTAGE;
                    USDbalance = USDbalance + (curProdSize * sellAtPrice) - sellFee;

                    totalFee += sellFee;



                    var netpl = ((sellAtPrice - buyAtPrice) * curProdSize) - (buyFee + sellFee);


                    cross.CalculatedBalance = Math.Round(Convert.ToDouble(USDbalance), 2);
                    cross.CalculatedNetPL = Math.Round(Convert.ToDouble(netpl), 2);

                    var sellingPrice = Math.Round(sellAtPrice, 2).ToString();

                    if (isStopLossSale)
                    {
                        sellingPrice = sellingPrice + "*";
                    }

                    if (printTrade)
                    {
                        var sellMsg = cross.dt.ToString(@"yyyy-MM-dd HH:mm:ss") + "\t"
                            + cross.Action + ((cross.comment != null) ? "_M" : "") + "\t\t"
                            + sellingPrice + "\t\t"
                            + Math.Round(sellFee, 2).ToString() + "\t\t"
                            + Math.Round(curProdSize, 2).ToString() + "\t"
                            + Math.Round(netpl, 2).ToString() + "\t\t"
                            + Math.Round(USDbalance, 2).ToString();
                        trans.AppendLine(sellMsg);


                        var sellMsgCsv = cross.dt.ToString(@"yyyy-MM-dd HH:mm:ss") + "\t"
                            + cross.Action + ((cross.comment != null) ? "_M" : "") + "\t"
                            + sellingPrice + "\t"
                            + Math.Round(sellFee, 2).ToString() + "\t"
                            + Math.Round(curProdSize, 2).ToString() + "\t"
                            + Math.Round(netpl, 2).ToString() + "\t"
                            + Math.Round(USDbalance, 2).ToString();
                        transCsv.AppendLine(sellMsgCsv);
                    }


                    plList.Add(netpl);
                    lastAction = "sell";
                }
            }


            Console.WriteLine("\n");

            var msg = "";
            msg = "" +
                "\nFrom: " + allCrossings.Last().dt.Date.ToString("yyyy-MMM-dd") + " To: " + allCrossings.First().dt.Date.ToString("yyyy-MMM-dd") + "\n" +
                "Interval: " + COMMON_INTERVAL.ToString() + "\n" +
                "Big sma: " + LARGE_SMA_LEN.ToString() + "\n" +
                "Small sma; " + SMALL_SMA_LEN.ToString() + "\n" +
                "Price Base sma: " + BASE_SMA_LEN.ToString() + "\n" +
                "\nTotal Trades: " + allCrossings.Count() + "\n" +
                "Profit/Loss: " + Math.Round(plList.Sum(), 2).ToString() + "\n" +
                "Total Fees: " + Math.Round(totalFee, 2).ToString() + "\n" +
                "Biggest Profit: " + Math.Round(plList.Max(), 2).ToString() + "\n" +
                "Biggest Loss: " + Math.Round(plList.Min(), 2).ToString() + "\n" +
                "Stop Loss Count: " + stopLossSale.ToString() + "\n" +
                "Avg. PL / Trade: " + Math.Round(plList.Average(), 2).ToString() + "\n\n";

            trans.AppendLine(msg);

            transCsv.AppendLine(msg);

            Logger.WriteLog("\n" + trans.ToString());

            //Logger.WriteLog("\n" + transCsv.ToString());

            //Console.WriteLine("\nFrom: " + allCrossings.Last().dt.Date.ToString("yyyy-MMM-dd") + " To: " + allCrossings.First().dt.Date.ToString("yyyy-MMM-dd"));
            //Console.WriteLine("Interval: " + COMMON_INTERVAL.ToString());
            //Console.WriteLine("Big sma: " + LARGE_SMA_LEN.ToString());
            //Console.WriteLine("Small sma; " + SMALL_SMA_LEN.ToString());
            //Console.WriteLine("sma of macd: " + SignalLen.ToString());
            //Console.WriteLine("\nTotal Trades: " + allCrossings.Count());
            //Console.WriteLine("Profit/Loss: " + Math.Round(plList.Sum(), 2).ToString());
            //Console.WriteLine("Total Fees: " + Math.Round(totalFee, 2).ToString());
            //Console.WriteLine("Biggest Profit: " + Math.Round(plList.Max(), 2).ToString());
            //Console.WriteLine("Biggest Loss: " + Math.Round(plList.Min(), 2).ToString());
            //Console.WriteLine("Avg. PL / Trade: " + Math.Round(plList.Average(), 2).ToString() + "\n\n");

            return (double)plList.Sum();

        }

        public void Dispose()
        {
            //throw new NotImplementedException();
            BasePrices_MA?.Dispose();
            SmallSma_MA?.Dispose();
            BigSma_MA?.Dispose();
        }
    }
}
