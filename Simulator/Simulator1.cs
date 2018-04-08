using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoinbaseExchange.NET.Data;
using CoinbaseExchange.NET.Endpoints.PublicData;

using CoinbaseExchange.NET.Utilities;
using System.Threading;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Simulator
{

    class Simulator1 : IDisposable
    {
        MovingAverage SmallSma;
        MovingAverage BigSma;
        private int COMMON_INTERVAL;

        private int LARGE_SMA_LEN;
        private int SMALL_SMA_LEN;

        static MyWindow _GraphingWindow;

        static Random _random = new Random();

        private IEnumerable<SmaData> SmaDiff;

        private int _SignalLen;

        internal MyWindow GraphWindow;

        static object _TriedListLock = new object();

        static Object addLock = new object();


        static HashSet<IntervalData> _TriedIntervalList = new HashSet<IntervalData>();

        static string ProductName = "LTC-USD";


        public static void Start()
        {

            //ReadTriedIntervalList();

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
                    Simulator1.ManualSimulate();
                }
                else
                {
                    //Simulator2.AutoSimulate2();
                    Simulator1.AutoSimulate();
                }

            }

        }


        private static void ReadTriedIntervalList()
        {
            var fileNamePah  = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\" + ProductName + "_TriedResultsList.json";

            Logger.WriteLog("Reading already tried list of intervals");

            if (File.Exists(fileNamePah))
            {
                try
                {
                    var lst = JsonConvert.DeserializeObject<List<ResultData>>(File.ReadAllText(fileNamePah));

                    _TriedIntervalList.Clear();

                    lst.ForEach(a=>_TriedIntervalList.Add(a.intervals));
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("cant read tried intervals list: " + fileNamePah);
                    //throw;
                }
                
            }
        }

        private static void WriteTriedIntervalList(List<ResultData> triedList)
        {
            var fileNamePah = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\" + ProductName + "_TriedResultsList.json";

            Logger.WriteLog("Updating tried list of intervals");

            try
            {

                string json = JsonConvert.SerializeObject(triedList, Formatting.Indented);
                File.WriteAllText(fileNamePah, json);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("cant write to tried intervals list: " + fileNamePah);
                //throw;
            }

        }


        public Simulator1(ref TickerClient ticker, string productName, int CommonInterval = 30, int LargeSmaLen = 100, int SmallSmaLen = 35, bool downloadLatestData = false)
        {


            COMMON_INTERVAL = CommonInterval;
            LARGE_SMA_LEN = LargeSmaLen;
            SMALL_SMA_LEN = SmallSmaLen;


            BigSma = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, LARGE_SMA_LEN, 10, downloadLatestData, false);
            SmallSma = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, SMALL_SMA_LEN, 10, downloadLatestData, false);


            var largeSmaDataPoints = BigSma.SmaDataPoints;
            var smallSmaDataPoints = SmallSma.SmaDataPoints;

            //var x = BigSma.SmaDataPts_Candle;
            //var y = SmallSma.SmaDataPts_Candle;

            //SmaDiff =
            //    from i in
            //    Enumerable.Range(0, Math.Max(largeSmaDataPoints.Count, smallSmaDataPoints.Count))
            //    select new SmaData
            //    {
            //        SmaValue = smallSmaDataPoints.ElementAtOrDefault(i) - largeSmaDataPoints.ElementAtOrDefault(i),
            //        ActualPrice = MovingAverage.SharedRawExchangeData.ElementAt(i * COMMON_INTERVAL).Close,
            //        Time = MovingAverage.SharedRawExchangeData.ElementAt(i * COMMON_INTERVAL).Time

            //        //////BigSma.SmaDataPts_Candle is equal to SmallSma.SmaDataPts_Candle since common interval is the same 
            //        ////ActualPrice = BigSma.SmaDataPts_Candle.ElementAtOrDefault(i).Close,
            //        ////dt = BigSma.SmaDataPts_Candle.ElementAtOrDefault(i).Time

            //    };




            var smaDtPtsReversedBig = BigSma.SmaDataPts_Candle.OrderBy((d) => d.Time);

            var smaPointsBig = smaDtPtsReversedBig.Select((d) => (double)d.Close).ToList().SMA(LARGE_SMA_LEN).ToList();

            var requiredSmadtptsBig = smaDtPtsReversedBig.Skip(LARGE_SMA_LEN - 1).ToList();


            ////var cntr = smaDtPtsReversedBig.Count() - 1;
            ////var running = 0;

            ////while (cntr >= 0 )
            ////{


            ////    System.Diagnostics.Debug.Write(smaDtPtsReversedBig.ElementAt(cntr).Time.ToString() + "\t");
            ////    System.Diagnostics.Debug.Write(smaDtPtsReversedBig.ElementAt(cntr).Close + "\t");

            ////    if (running >= 100)
            ////    {
            ////        var innerCtr = cntr  + 1;
            ////        System.Diagnostics.Debug.Write(smaPointsBig.ElementAt(innerCtr));
            ////    }

            ////    System.Diagnostics.Debug.Write("\n");
            ////    running++;
            ////    cntr--;


            ////    if (running > 200)
            ////    {
            ////        break;
            ////    }
            ////}

            //System.Diagnostics.Debug.Write("\n");
            //System.Diagnostics.Debug.Write("\n");

            //for (int i = 0; i < 200; i++)
            //{
            //    System.Diagnostics.Debug.Write(smaDtPtsReversedBig.ElementAt(i).Time.ToString() + "\t");
            //    System.Diagnostics.Debug.Write(smaDtPtsReversedBig.ElementAt(i).Close + "\t");

            //    if (i >= LARGE_SMA_LEN - 1)
            //    {
            //        System.Diagnostics.Debug.Write(smaPointsBig.ElementAt(i - (LARGE_SMA_LEN - 1)));
            //        //continue;
            //    }
            //    System.Diagnostics.Debug.Write("\n");

            //}

            //Console.WriteLine(requiredSmadtpts.Count());

            var smaWithDataPtsBig = smaPointsBig.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = requiredSmadtptsBig.ElementAt(i).Close,
                Time = requiredSmadtptsBig.ElementAt(i).Time
            }).ToList();






            var smaDtPtsReversedSmall = SmallSma.SmaDataPts_Candle.OrderBy((d) => d.Time);
            var smaPointsSmall = smaDtPtsReversedSmall.Select((d) => (double)d.Close).ToList().SMA(SMALL_SMA_LEN).ToList();
            var requiredSmadtptsSmall = smaDtPtsReversedSmall.Skip(SMALL_SMA_LEN - 1).ToList();
            var smaWithDataPtsSmall = smaPointsSmall.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = requiredSmadtptsSmall.ElementAt(i).Close,
                Time = requiredSmadtptsSmall.ElementAt(i).Time
            }).ToList();



            //for (int i = 0; i < 150; i++)
            //{
            //    System.Diagnostics.Debug.WriteLine(smaWithDataPtsBig[i].Time.ToString() + "\t" + smaWithDataPtsBig[i].ActualPrice + "\t" + smaWithDataPtsBig[i].SmaValue);
            //}

            //System.Diagnostics.Debug.WriteLine("\n\n");

            //for (int i = 0; i < 150; i++)
            //{
            //    System.Diagnostics.Debug.WriteLine(smaWithDataPtsSmall[i].Time.ToString() + "\t" + smaWithDataPtsSmall[i].ActualPrice + "\t" + smaWithDataPtsSmall[i].SmaValue);
            //}


            DateTime bigSmaFirst = smaWithDataPtsBig.First().Time;

            var sameTime_smaWithDataPtsSmall = smaWithDataPtsSmall.Where(f => f.Time >= bigSmaFirst);

            //for (int i = 0; i < 100; i++)
            //{
            //    System.Diagnostics.Debug.WriteLine(sameTime_smaWithDataPtsSmall.ElementAt(i).Time + "\t" 
            //        + +sameTime_smaWithDataPtsSmall.ElementAt(i).ActualPrice + "\t"
            //        + sameTime_smaWithDataPtsSmall.ElementAt(i).SmaValue);
            //}


            var smaD = smaWithDataPtsBig.Zip(sameTime_smaWithDataPtsSmall, (bd, sd) => sd.SmaValue - bd.SmaValue);


            SmaDiff = smaD.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = smaWithDataPtsBig.ElementAt(i).ActualPrice,
                Time = smaWithDataPtsBig.ElementAt(i).Time
            });


            //for (int i = 0; i < 100; i++)
            //{
            //    System.Diagnostics.Debug.WriteLine(SmaDiff.ElementAt(i).Time + "\t"
            //        + +SmaDiff.ElementAt(i).ActualPrice + "\t"
            //        + SmaDiff.ElementAt(i).SmaValue);
            //}

        }


        //private class SmaDetails
        //{
        //    public double SmaValue { get; set; }
        //    public decimal ActualPrice { get; set; }
        //    public DateTime Time { get; set; }
        //}



        static IntervalData getIntervalData()
        {
            var newInterval = new IntervalData
            {
                //interval = _random.Next(20, 50),
                //bigSmaLen = _random.Next(80, 250),
                //smallSmaLen = _random.Next(25, 70),
                //SignalLen = _random.Next(2, 10)

                interval = _random.Next(15, 30),
                bigSmaLen = _random.Next(80, 150),
                smallSmaLen = _random.Next(55, 75),
                SignalLen = _random.Next(5, 10)

            };


            lock (_TriedListLock)
            {
                var beforeAdding = _TriedIntervalList.Count();

                _TriedIntervalList.Add(newInterval);

                var afterAdding = _TriedIntervalList.Count();

                while (beforeAdding == afterAdding)
                {

                    Console.WriteLine("\t\t\t***");
                    newInterval = new IntervalData
                    {
                        //interval = _random.Next(20, 50),
                        //bigSmaLen = _random.Next(80, 250),
                        //smallSmaLen = _random.Next(25, 70),
                        //SignalLen = _random.Next(2, 10)

                        interval = _random.Next(15, 30),
                        bigSmaLen = _random.Next(80, 150),
                        smallSmaLen = _random.Next(55, 75),
                        SignalLen = _random.Next(5, 10)
                    };


                    beforeAdding = _TriedIntervalList.Count();

                    _TriedIntervalList.Add(newInterval);

                    afterAdding = _TriedIntervalList.Count();

                }
            }


            return newInterval;
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
        public static void ManualSimulate()
        {
            int lastCommonInterval = 0;
            int lastBigSma = 0;
            int lastSmallSma = 0;

            Simulator1 S = null;

            var ProductName = "LTC-USD";
            TickerClient Ticker = new TickerClient(ProductName);

            //wait for ticker to get ready
            Thread.Sleep(2 * 1000);


            try
            {
                //////Console.WriteLine("Enter simulation start date:");
                //////var inDt = "2017/10/1";//Console.ReadLine();
                //////DateTime dt;
                //////DateTime.TryParse(inDt, out dt);

                //////Console.WriteLine("Enter Common time interval in minutes");
                //////var inputCommonInterval = 30; //Convert.ToInt16(Console.ReadLine());

                //////Console.WriteLine("Enter big sma length");
                //////var inputBigSmaLen = 100; //Convert.ToInt16(Console.ReadLine());

                //////Console.WriteLine("Enter small sma length");
                //////var inputSmallSmaLen = 35; //Convert.ToInt16(Console.ReadLine());

                //////Console.WriteLine("Enter signal len: ");
                //////var inputSmaLen = 5;

                //////var n = Console.ReadLine();



                Console.WriteLine("Enter simulation start date:");
                var inDt = Console.ReadLine();
                DateTime dt;
                DateTime.TryParse(inDt, out dt);



                var dtEnd = DateTime.MinValue;
                while (dtEnd == DateTime.MinValue)
                {
                    Console.WriteLine("Enter simulation end date, enter n to use todays date:");
                    inDt = Console.ReadLine();

                    if (inDt == "n")
                    {
                        dtEnd = DateTime.Now;
                        break;
                    }
                    DateTime.TryParse(inDt, out dtEnd);
                }


                DateTime autoEndDate = dtEnd; //DateTime.Now;


                Console.WriteLine("Enter Common time interval in minutes");
                var inputCommonInterval = Convert.ToInt16(Console.ReadLine());

                Console.WriteLine("Enter big sma length");
                var inputBigSmaLen = Convert.ToInt16(Console.ReadLine());

                Console.WriteLine("Enter small sma length");
                var inputSmallSmaLen = Convert.ToInt16(Console.ReadLine());

                Console.WriteLine("Enter signal len: ");
                var inputSmaLen = Convert.ToInt16(Console.ReadLine());



                if (S == null)
                {
                    S = new Simulator1(ref Ticker, ProductName, inputCommonInterval, inputBigSmaLen, inputSmallSmaLen, true);
                    S.GraphWindow = _GraphingWindow;
                }
                else
                {
                    if (!(lastCommonInterval == inputCommonInterval && lastBigSma == inputBigSmaLen && lastSmallSma == inputSmallSmaLen))
                    {
                        S.Dispose();
                        S = null;
                        S = new Simulator1(ref Ticker, ProductName, inputCommonInterval, inputBigSmaLen, inputSmallSmaLen, false);
                        S.GraphWindow = _GraphingWindow;
                    }
                }

                S.Calculate(dt, autoEndDate, inputSmaLen, true, true);

                lastCommonInterval = inputCommonInterval;
                lastBigSma = inputBigSmaLen;
                lastSmallSma = inputSmallSmaLen;

            }
            catch (Exception ex)
            {
                Console.WriteLine("invalid input / error in calc");
            }


        }


        private static List<ResultData> AutoSim1_ThreadSafe(ref TickerClient inputTicker, DateTime startDt, DateTime endDt, int inputSimCount)
        {

            int lastCommonInterval = 0;
            int lastBigSma = 0;
            int lastSmallSma = 0;

            Simulator1 S = null;


            TickerClient Ticker = inputTicker;

            //wait for ticker to get ready
            Thread.Sleep(2 * 1000);

            DateTime autoStartDate = startDt;//new DateTime(2018, 02, 17);

            DateTime autoEndDate = endDt; // DateTime.Now; //new DateTime(2018, 02, 17);

            //var autoInterval = 30; //Enumerable.Range(15, 60).Where(i => i % 5 == 0);//30;
            //var autoBigsmaLen = 100;
            //var autoSmallSmaLen = 35;
            //var autoSignalLen = 7;

            List<ResultData> resultList = new List<ResultData>();

            for (int i = 0; i < inputSimCount; i++)
            {

                IntervalData curIntervals = getIntervalData();


                try
                {

                    if (S == null)
                    {
                        S = new Simulator1(ref Ticker, ProductName, curIntervals.interval, curIntervals.bigSmaLen, curIntervals.smallSmaLen, true);
                        S.GraphWindow = _GraphingWindow;
                    }
                    else
                    {
                        if (!(lastCommonInterval == curIntervals.interval && lastBigSma == curIntervals.bigSmaLen && lastSmallSma == curIntervals.smallSmaLen))
                        {
                            S.Dispose();
                            S = null;
                            S = new Simulator1(ref Ticker, ProductName, curIntervals.interval, curIntervals.bigSmaLen, curIntervals.smallSmaLen);
                            S.GraphWindow = _GraphingWindow;
                        }
                    }

                    var curPl = S.Calculate(autoStartDate, autoEndDate, curIntervals.SignalLen, false, false);

                    resultList.Add(new ResultData { Pl = curPl, intervals = curIntervals, SimStartDate = autoStartDate, SimEndDate = autoEndDate });

                    lastCommonInterval = curIntervals.interval;
                    lastBigSma = curIntervals.bigSmaLen;
                    lastSmallSma = curIntervals.smallSmaLen;


                }
                catch (Exception)
                {
                    Console.WriteLine("invalid input / error in calc");
                }

            }

            return resultList;

        }

        public static void AutoSimulate()
        {
            //_TriedIntervalList.Clear();


            ReadTriedIntervalList();

            //ShowGraphingForm();

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

            var threadCount = Math.Ceiling((simCount / (double)Each_Sim_Count));

            for (int i = 0; i < threadCount; i++)
            {
                var curTask = Task.Factory.StartNew(() =>
                {
                    var returnedResult = AutoSim1_ThreadSafe(ref Ticker, autoStartDate, autoEndDate, Each_Sim_Count); //RunSim_ThreadSafe(ref Ticker, autoStartDate, autoEndDate, Each_Sim_Count);
                    allCombinedResultList.AddRange(returnedResult);
                });

                allSimTasks.Add(curTask);
            }

            Task.WaitAll(allSimTasks.ToArray());




            Console.WriteLine("\nTop 5 profit results\n");

            IntervalData bestValues = null;


            var triedListCopy = new List<ResultData>(allCombinedResultList);

            for (int i = 0; i < 5; i++)
            {
                var best = allCombinedResultList.Where(d => d.Pl == allCombinedResultList.Max(a => a.Pl)).First();

                if (bestValues == null)
                    bestValues = best.intervals;

                var resMsg = "best result: " + i + "\n"
                    + "PL: " + best.Pl + "\n"
                    + "Interval: " + best.intervals.interval + "\n"
                    + "Signal len: " + best.intervals.SignalLen + "\n"
                    + "big sma: " + best.intervals.bigSmaLen + "\n"
                    + "small sma: " + best.intervals.smallSmaLen + "\n";


                Logger.WriteLog("\n" + resMsg);

                allCombinedResultList.Remove(best);

            }



            var S = new Simulator1(ref Ticker, ProductName, bestValues.interval, bestValues.bigSmaLen, bestValues.smallSmaLen, true);
            S.GraphWindow = _GraphingWindow;
            S.Calculate(autoStartDate, autoEndDate, bestValues.SignalLen, true, true);



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

            WriteTriedIntervalList(triedListCopy);

        }



        internal double Calculate(DateTime simStartDate, DateTime inputEndDate, int inputSignalLen = 2, bool printTrades = true, bool renderGraph = false)
        {

            _SignalLen = inputSignalLen;


            var simEndTime = inputEndDate; //DateTime.Now;



            var signalLine = SmaDiff.Select(d => d.SmaValue).ToList().SMA(inputSignalLen);
            var requiredSignalDtPts = SmaDiff.Skip(inputSignalLen - 1).ToList();
            var SignalWithDataPtsBig = signalLine.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = requiredSignalDtPts.ElementAt(i).ActualPrice,
                Time = requiredSignalDtPts.ElementAt(i).Time
            }).ToList();


            //this ensures the time lines are the same for signal and sma
            SmaDiff = requiredSignalDtPts; //SmaDiff.Skip(inputSmaOfMacdLen);




            List<CrossData> allCrossings_Linq = new List<CrossData>();

            var requiredMacdPtsLnq = SmaDiff.Where((s => s.Time >= simStartDate && s.Time < simEndTime)).ToList();

            var requiredSignalPtsLnq = SignalWithDataPtsBig.Where((s => s.Time >= simStartDate && s.Time < simEndTime)).ToList();



            var allBuys = requiredMacdPtsLnq.Where((d, i) => d.SmaValue > requiredSignalPtsLnq.ElementAt(i).SmaValue).ToList();

            var allSells = requiredMacdPtsLnq.Where((d, i) => d.SmaValue < requiredSignalPtsLnq.ElementAt(i).SmaValue).ToList();


            Utilities crossingCal = new Utilities();
            //var Linqres = crossingCal.Getcrossings_Linq(requiredSmaDiffPtsLnq, requiredSignalPtsLnq, SmaDiff.First().Time, SmaDiff.Last().Time);
            var curLinqCrossres = crossingCal.Getcrossings_Linq(allBuys, allSells);
            allCrossings_Linq.AddRange(curLinqCrossres);



            //Console.WriteLine("\nLinq (press enter to continue):\n");
            //Console.ReadLine();


            var Pl = CalculatePl_Compounding(allCrossings_Linq, printTrades);


            if (renderGraph)
            {
                //List<List<SmaData>> allSeries = new List<List<SmaData>>();
                List<SeriesDetails> allSereis2 = new List<SeriesDetails>();

                //allSeries.Add( Price);
                //allSeries.Add(BigSma);
                //allSeries.Add(SmallSma);

                //allSereis2.Add(new SeriesDetails { series = ActualPriceList, SereiesName = "Actual Price" });


                var PriceLine = requiredMacdPtsLnq.Select(p => new SmaData { ActualPrice = p.ActualPrice, Time = p.Time }).ToList();
                allSereis2.Add(new SeriesDetails { series = PriceLine, SereiesName = "Price" });
                allSereis2.Add(new SeriesDetails { series = requiredMacdPtsLnq, SereiesName = "macd" });
                allSereis2.Add(new SeriesDetails { series = requiredSignalPtsLnq, SereiesName = "signal" });
                //allSereis2.Add(new SeriesDetails { series = SmallSma, SereiesName = "Small_Sma" });

                GraphWindow.DrawSeriesSim1(allSereis2, allCrossings_Linq);
            }


            return Pl;

            //show graph after calculations
            //ShowGraph(SmaDiff, SignalWithDataPtsBig, allCrossings_Parallel);

        }


        //public void Calculate(DateTime simStartDate, int inputSmaOfMacdLen = 2, bool printTrades = true)
        //{

        //    SignalLen = inputSmaOfMacdLen;
        //    //var bigSmaOfMacd = smaDiff.Select(d=>d.diff).ToList().SMA(L_SIGNAL_LEN);
        //    //var smallSmaOfMacd = smaDiff.Select(d=>d.diff).ToList().SMA(S_SIGNAL_LEN);

        //    Console.WriteLine("Calculating buy / sell actions...");


        //    var simStartTime = simStartDate; //new DateTime(2017, 10, 1);//.AddHours(19);//simStartDate;
        //    var simEndTime = DateTime.Now; //new DateTime(2017, 10, 5);//DateTime.Now;

        //    var timePeriod = Math.Floor((simEndTime - simStartTime).TotalHours / 24) + 1;

        //    List<DateLst> dLst = new List<DateLst>();


        //    var endTime = simStartTime.AddHours(24);

        //    for (int i = 0; i < timePeriod; i++)
        //    {
        //        dLst.Add(new DateLst
        //        {
        //            groupNo = i * 10000,
        //            start = simStartTime,
        //            end = endTime
        //        });

        //        simStartTime = endTime;
        //        endTime = endTime.AddHours(24);
        //    }





        //    //Object addLock = new object();


        //    var SmaOfMacd = SmaDiff.Select(d => d.SmaValue).ToList().SMA(inputSmaOfMacdLen);


        //    var requiredSignalDtPts = SmaDiff.Skip(inputSmaOfMacdLen - 1).ToList();


        //    //for (int i = 0; i < 200; i++)
        //    //{
        //    //    System.Diagnostics.Debug.Write(BigSma.SmaDataPts_Candle.ElementAt(i).Time.ToString() + "\t");
        //    //    System.Diagnostics.Debug.Write(BigSma.SmaDataPts_Candle.ElementAt(i).Close + "\t");

        //    //    if (i < LARGE_SMA_LEN - 1)
        //    //    {
        //    //        System.Diagnostics.Debug.Write("\n");
        //    //        continue;
        //    //    }

        //    //    System.Diagnostics.Debug.Write(smaPointsBig.ElementAt(i - LARGE_SMA_LEN + 1) + "\n");
        //    //}

        //    //Console.WriteLine(requiredSmadtpts.Count());

        //    var SignalWithDataPtsBig = SmaOfMacd.Select((d, i) => new SmaData
        //    {
        //        SmaValue = d,
        //        ActualPrice = requiredSignalDtPts.ElementAt(i).ActualPrice,
        //        Time = requiredSignalDtPts.ElementAt(i).Time
        //    }).ToList();


        //    //this ensures the time lines are the same for signal and sma
        //    SmaDiff = requiredSignalDtPts; //SmaDiff.Skip(inputSmaOfMacdLen);

        //    //for (int i = 0; i < 100; i++)
        //    //{
        //    //    System.Diagnostics.Debug.WriteLine(SignalWithDataPtsBig.ElementAt(i).Time + "\t"
        //    //        + SignalWithDataPtsBig.ElementAt(i).ActualPrice + "\t"
        //    //        + SignalWithDataPtsBig.ElementAt(i).SmaValue);
        //    //}

        //    //for (int i = 0; i < 50; i++)
        //    //{
        //    //    System.Diagnostics.Debug.Write(SmaDiff.ElementAt(i).Time.ToString() + "\t");
        //    //    System.Diagnostics.Debug.Write(SmaDiff.ElementAt(i).SmaValue + "\t");

        //    //    if (i < inputSmaOfMacdLen - 1)
        //    //    {
        //    //        System.Diagnostics.Debug.Write("\n");
        //    //        continue;
        //    //    }

        //    //    System.Diagnostics.Debug.Write(SmaOfMacd.ElementAt(i - inputSmaOfMacdLen + 1) + "\n");
        //    //}




        //    ////////for runnung sequestially 
        //    //////List<CrossData> allCrossings = new List<CrossData>();
        //    //////foreach (var item in dLst)
        //    //////{

        //    //////    Utilities crossingCalculator = new Utilities();

        //    //////    var requiredSmaDiffPts = SmaDiff.Where((s => s.Time >= item.start && s.Time < item.end));

        //    //////    var requiredSignalPts = SignalWithDataPtsBig.Where((s => s.Time >= item.start && s.Time < item.end));

        //    //////    var res = crossingCalculator.Getcrossings(requiredSmaDiffPts, requiredSignalPts, item.start, item.end, inputSmaOfMacdLen, item.groupNo);

        //    //////    //allCrossings.AddRange(res.Result);
        //    //////    lock (addLock)
        //    //////    {
        //    //////        allCrossings.AddRange(res);
        //    //////    }

        //    //////}

        //    //////allCrossings = allCrossings.OrderBy(s => s.dt).ToList();

        //    //for (int i = 0; i < allCrossings.Count(); i++)
        //    //{
        //    //    System.Diagnostics.Debug.WriteLine(allCrossings[i].dt + "\t" + allCrossings[i].Action + "\t" + allCrossings[i].CrossingPrice);
        //    //}

        //    //System.Diagnostics.Debug.WriteLine("");



        //    //show graph




        //    List<CrossData> allCrossings_Parallel = new List<CrossData>();

        //    var strtTimer = DateTime.Now;

        //    Parallel.ForEach(dLst, item =>
        //    {
        //        Utilities crossingCalculator = new Utilities();

        //        var requiredSmaDiffPts = SmaDiff.Where((s => s.Time >= item.start && s.Time < item.end));

        //        var requiredSignalPts = SignalWithDataPtsBig.Where((s => s.Time >= item.start && s.Time < item.end));

        //        var res = crossingCalculator.Getcrossings_Parallel(requiredSmaDiffPts, requiredSignalPts, item.start, item.end, inputSmaOfMacdLen, item.groupNo);

        //        //allCrossings.AddRange(res.Result);
        //        lock (addLock)
        //        {
        //            allCrossings_Parallel.AddRange(res);
        //        }

        //    });

        //    Utilities.EnterMissingActions(ref allCrossings_Parallel);

        //    var timeTaken = (DateTime.Now - strtTimer).Milliseconds;

        //    Console.WriteLine("parallel time taken (ms): " + timeTaken.ToString());


        //    List<CrossData> allCrossings_Linq = new List<CrossData>();
        //    Utilities crossingCal = new Utilities();


        //    var strtTimer2 = DateTime.Now;

        //    var requiredSmaDiffPtsLnq = SmaDiff.Where((s => s.Time >= simStartDate && s.Time < simEndTime));

        //    var requiredSignalPtsLnq = SignalWithDataPtsBig.Where((s => s.Time >= simStartDate && s.Time < simEndTime));

        //    var Linqres = crossingCal.Getcrossings_Linq(requiredSmaDiffPtsLnq, requiredSignalPtsLnq, SmaDiff.First().Time, SmaDiff.Last().Time);
        //    allCrossings_Linq.AddRange(Linqres);

        //    var timeTaken2 = (DateTime.Now - strtTimer2).Milliseconds;

        //    Console.WriteLine("linq time taken (ms): " + timeTaken2.ToString());

        //    //Utilities.EnterMissingActions(ref Linqres);

        //    //foreach (var item in dLst)
        //    //{
        //    //    Utilities crossingCalculator = new Utilities();

        //    //    var requiredSmaDiffPts = SmaDiff.Where((s => s.Time >= item.start && s.Time < item.end));

        //    //    var requiredSignalPts = SignalWithDataPtsBig.Where((s => s.Time >= item.start && s.Time < item.end));

        //    //    var res = crossingCalculator.Getcrossings_Linq(requiredSmaDiffPts, requiredSignalPts, item.start, item.end);

        //    //    //allCrossings.AddRange(res.Result);
        //    //    lock (addLock)
        //    //    {
        //    //        allCrossings_Linq.AddRange(res);
        //    //    }

        //    //};





        //    //for (int i = 0; i < allCrossings_Parallel.Count(); i++)
        //    //{
        //    //    System.Diagnostics.Debug.WriteLine(allCrossings_Parallel[i].dt + "\t" + allCrossings_Parallel[i].Action + "\t" + allCrossings_Parallel[i].CrossingPrice);
        //    //}


        //    //List<Task> allTasks = new List<Task>();
        //    //foreach (var item in dLst)
        //    //{

        //    //    allTasks.Add( Task.Run(()=> 
        //    //    { 
        //    //        Utilities crossingCalculator = new Utilities();

        //    //        var requiredSmaDiffPts = SmaDiff.Where((s => s.Time >= item.start && s.Time < item.end));

        //    //        var requiredSignalPts = SignalWithDataPtsBig.Where((s => s.Time >= item.start && s.Time < item.end));

        //    //        var res = crossingCalculator.Getcrossings(requiredSmaDiffPts, requiredSignalPts, item.start, item.end, inputSmaOfMacdLen, item.groupNo);

        //    //        //allCrossings.AddRange(res.Result);
        //    //        lock (addLock)
        //    //        {
        //    //            allCrossings.AddRange(res);
        //    //        }

        //    //    }));

        //    //}

        //    //Task.WaitAll(allTasks.ToArray());





        //    //////var start = dLst.First().start;
        //    //////var smaDiffDtRange = SmaDiff.Where(d => d.Time >= start);
        //    //////var reqSignals = SignalWithDataPtsBig.Where(d => d.Time >= start);

        //    //////Utilities crossingCalculator = new Utilities();
        //    //////var res = crossingCalculator.Getcrossings(smaDiffDtRange, reqSignals, start, DateTime.Now, inputSmaOfMacdLen, 100000);

        //    //////allCrossings.AddRange(res);




        //    Console.WriteLine("done");

        //    ////Console.WriteLine("\tserial");
        //    ////CalculatePl_Compounding(allCrossings, printTrades);

        //    //Console.WriteLine("\n\tparallel\n");
        //    CalculatePl_Compounding(allCrossings_Parallel, printTrades);


        //    Console.WriteLine("\nLinq (press enter to continue):\n");
        //    Console.ReadLine();
        //    CalculatePl_Compounding(allCrossings_Linq, printTrades);


        //    //show graph after calculations
        //    ShowGraph(SmaDiff, SignalWithDataPtsBig, allCrossings_Parallel);

        //}


        private void ShowGraph(IEnumerable<SmaData> smadifPts, IEnumerable<SmaData> signalPts, IEnumerable<CrossData> allCrosses)
        {
            GraphWindow.ShowData(smadifPts, signalPts, allCrosses);
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
            BigSma?.Dispose();
            SmallSma?.Dispose();

        }

        double CalculatePl_Compounding(List<CrossData> allCrossings, bool printTrade = true)
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
                trans.AppendLine("Time\t\t\t\taction\t\tPrice\t\tFee\t\t\tSize\tPL\t\t\tBalance");
                transCsv.AppendLine("Time\tAction\tPrice\tFee\tSize\tPL\tBalance");
            }


            var plList = new List<decimal>();

            var buyAtPrice = 0.0m;
            var sellAtPrice = 0.0m;

            var buyFee = 0.0m;
            var sellFee = 0.0m;

            const decimal BUFFER = 0.60m;//1.20m;//1.50m;//1.0m; //0.75m;


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

                    buyFee = (USDbalance) * FEE_PERCENTAGE;
                    curProdSize = (USDbalance - buyFee) / buyAtPrice;
                    totalFee += buyFee;

                    USDbalance = USDbalance - (curProdSize * buyAtPrice) - buyFee;

                    cross.CalculatedBalance = Convert.ToDouble(USDbalance);

                    if (printTrade)
                    {
                        var buyMsg = cross.dt.ToString(@"yyyy-MM-dd HH:mm:ss") + "\t"
                            + cross.Action + ((cross.comment != null) ? "_M" : "") + "\t\t\t"
                            + Math.Round(buyAtPrice, 2).ToString() + "\t\t"
                            + Math.Round(buyFee, 2).ToString() + "\t\t"
                            + Math.Round(curProdSize, 2).ToString() + "\t\t\t\t"
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

                    var stopLossPrice = buyAtPrice - (buyAtPrice * STOP_LOSS_PERCENTAGE);
                    if (sellAtPrice < stopLossPrice)
                    {
                        sellAtPrice = stopLossPrice - BUFFER;
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
                "sma of macd: " + _SignalLen.ToString() + "\n" +
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


        void CalculatePl_NonCompounding(List<CrossData> allCrossings)
        {


            const int AMOUNT = 50;
            decimal pl = 0;
            const decimal FEE = 0.003m;
            decimal totalFee = 0.0m;

            if (allCrossings.First().Action == "buy")
            {
                allCrossings.RemoveAt(allCrossings.IndexOf(allCrossings.First()));
            }

            if (allCrossings.Last().Action == "sell")
            {
                allCrossings.RemoveAt(allCrossings.IndexOf(allCrossings.Last()));
            }

            Console.WriteLine("Time\t\t\taction\t\tPrice\t\tFee\t\tPL\t\tBalance");

            var plList = new List<decimal>();

            var curPl = 0.0m;


            for (int i = allCrossings.Count() - 1; i >= 0; i--)
            {
                var cross = allCrossings[i];
                var curFee = (AMOUNT * cross.CrossingPrice) * FEE;

                totalFee += curFee;
                if (cross.Action == "buy")
                {
                    pl = pl - (AMOUNT * cross.CrossingPrice) + curFee;

                    curPl = (AMOUNT * cross.CrossingPrice) + curFee;

                    Console.WriteLine(cross.dt.ToString() + "\t"
                        + cross.Action + "\t\t"
                        + cross.CrossingPrice.ToString() + "\t\t"
                        + Math.Round(curFee, 2).ToString() + "\t\t"
                        + "\t\t"
                        + pl.ToString());
                }

                if (cross.Action == "sell")
                {
                    pl = pl + (AMOUNT * cross.CrossingPrice) - curFee;

                    var netpl = ((AMOUNT * cross.CrossingPrice) - curFee) - curPl;

                    Console.WriteLine(cross.dt.ToString() + "\t"
                        + cross.Action + "\t\t"
                        + cross.CrossingPrice.ToString() + "\t\t"
                        + Math.Round(curFee, 2).ToString() + "\t\t"
                        + Math.Round(netpl, 2).ToString() + "\t\t"
                        + pl.ToString());

                    plList.Add(netpl);
                }
            }



            Console.WriteLine("Total Trades: " + allCrossings.Count());
            Console.WriteLine("Profit/Loss: " + plList.Sum().ToString());
            Console.WriteLine("Total Fees: " + totalFee.ToString());
            Console.WriteLine("Biggest Profit: " + plList.Max().ToString());
            Console.WriteLine("Biggest Profit: " + plList.Min().ToString());

        }


    }
}


