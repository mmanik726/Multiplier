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
using CoinbaseExchange.NET;



namespace Simulator
{

    public class Simulator1 : IDisposable
    {
        //MovingAverage SmallSma;
        //MovingAverage BigSma;
        private int COMMON_INTERVAL;

        private int LARGE_SMA_LEN;
        private int SMALL_SMA_LEN;


        public static List<CandleData> _RawData;

        private static bool LoadingData;

        private static object LoadingDataLock = new object();

        public List<SeriesDetails> CurResultsSeriesList = new List<SeriesDetails>();
        public List<CrossData> CurResultCrossList = new List<CrossData>();


        static DateTime _ActualInputStartDate = DateTime.Now;
        static DateTime _ActualInputEndDate = DateTime.Now;

        static MyWindow _GraphingWindow;

        static Random _random = new Random();

        private IEnumerable<SmaData> _SmaDiff;

        private int _SignalLen;

        public MyWindow GraphWindow;

        static object _TriedListLock = new object();

        static Object addLock = new object();


        static HashSet<IntervalData> _TriedIntervalList = new HashSet<IntervalData>();

        static string ProductName = "LTC-USD";


        public static void Start()
        {

            //ReadTriedIntervalList();

            //ShowGraphingForm();



            while (true)
            {
                var am = "";
                while (!(am == "a" || am == "m"))
                {
                    Console.WriteLine("Enter m for manual a for automatic");
                    am = Console.ReadLine();
                }

                bool useCompounding = true;
                //Console.WriteLine("Enter y for compounding n for non compounding");
                //var inputBool = Console.ReadLine();
                //if (inputBool == "y")
                //    useCompounding = true;
                //else
                //    useCompounding = false;

                if (am == "m")
                {
                    //Simulator2.ManualSimulate2();
                    ShowGraphingForm();

                    Simulator1.ManualSimulate(useCompounding);
                }
                else
                {
                    //Simulator2.AutoSimulate2();
                    //Simulator1.AutoSimulate_DateRange(useCompounding);
                    ShowGraphingForm();
                    Simulator1.AutoSimulate(useCompounding);
                }

            }

        }



        private static void ReadTriedIntervalList(DateTime simStart, DateTime simEnd)
        {

            var s = simStart.ToString("yyyy-MMM-dd");
            var e = simEnd.ToString("yyyy-MMM-dd");

            var fileNamePah  = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) 
                + @"\" + ProductName + "_TriedResultsList_S1_" + s + "_to_" + e +".json";

            Logger.WriteLog("Reading already tried list of intervals");

            if (File.Exists(fileNamePah))
            {
                try
                {
                    var lst = JsonConvert.DeserializeObject<List<ResultData>>(File.ReadAllText(fileNamePah));
                    Logger.WriteLog("Found " + lst.Count() + " tried records from " + s + " to " + e);
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

        private static void WriteTriedIntervalList(List<ResultData> triedList, DateTime simStart, DateTime simEnd)
        {

            var s = simStart.ToString("yyyy-MMM-dd");
            var e = simEnd.ToString("yyyy-MMM-dd");
            //
            var fileNamePah = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
                + @"\" + ProductName + "_TriedResultsList_S1_" + s + "_to_" + e + ".json";
            //var fileNamePah = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\" + ProductName + "_TriedResultsList_S1.json";

            Logger.WriteLog("Updating tried list of intervals");

            try
            {

                var previouslyTriedLst = new List<ResultData>();

                if (File.Exists(fileNamePah))
                {
                    previouslyTriedLst.AddRange(JsonConvert.DeserializeObject<List<ResultData>>(File.ReadAllText(fileNamePah)));
                }


                //public double Pl { get; set; }

                //public DateTime SimStartDate { get; set; }

                //public DateTime SimEndDate { get; set; }

                //public IntervalData intervals { get; set; }

                var temp = triedList.Select(a => new ResultData { Pl = a.Pl, SimStartDate = a.SimStartDate, SimEndDate = a.SimEndDate, intervals = a.intervals });

                previouslyTriedLst.AddRange(temp);


                previouslyTriedLst = previouslyTriedLst.OrderByDescending(a => a.Pl).ToList();

                string json = JsonConvert.SerializeObject(previouslyTriedLst, Formatting.Indented);

                //File.Copy(fileNamePah, Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\" + ProductName + "_TriedResultsList_S1_backup.json", true);
                //File.AppendAllText(fileNamePah, json);
                File.WriteAllText(fileNamePah, json);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("cant write to tried intervals list: " + fileNamePah + "\n" + ex.Message);
                //throw;
            }

        }



        private List<SmaData> getSmaLine(ref List<CandleData> rawData, int Interval, int smaLen)
        {
            var remainder = rawData.Count() % Interval;
            //            var tempExchangePriceDataSet = rawData.Skip(remainder - 1).ToList();
            var tempExchangePriceDataSet = rawData.Take(rawData.Count() - (remainder - 1)).ToList();


            var requiredIntervalData = tempExchangePriceDataSet.Where((candleData, i) => i % Interval == 0).ToList();// select every third item in list ie select data from every x min 



            var priceDataPointsDbl = requiredIntervalData.Select((d) => (double)d.Close).ToList(); //transfer candle data close values to pure list of doubles

            //StringBuilder test = new StringBuilder();
            //test.Clear();
            //requiredIntervalData.ForEach(d => test.AppendLine(d.Time + "\t" + d.Close));
            //File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Test.txt",
            //    test.ToString());


            var smaDataPtsList = priceDataPointsDbl.SMA(smaLen).ToList(); //return the continuous sma using the list of doubles (NOT candle data)

            //var smaDataPtsList = priceDataPointsDbl.EMA(smaLen).ToList(); //return the continuous sma using the list of doubles (NOT candle data)

            var requiredSmadtpts = requiredIntervalData.Skip(smaLen - 1).ToList();
            var smaWithDataPts = smaDataPtsList.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = requiredSmadtpts.ElementAt(i).Close,
                Time = requiredSmadtpts.ElementAt(i).Time
            }).ToList();



            //test.Clear();
            //smaWithDataPts.ForEach(d => test.AppendLine(d.Time + "\t" + d.SmaValue));
            //File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Test.txt",
            //    test.ToString());


            return smaWithDataPts;




        }


        public Simulator1(ref TickerClient ticker, string productName, int CommonInterval = 30, int LargeSmaLen = 100, int SmallSmaLen = 35, bool downloadLatestData = false)
        {

            //while (LoadingData)
            //{
            //    Thread.Sleep(100);
            //}

            lock (LoadingDataLock)
            {
                if (_RawData == null)
                {
                    LoadingData = true;

                    ExData a = new ExData(productName, true);
                    _RawData = a.RawExchangeData.OrderBy(d => d.Time).ToList();

                    LoadingData = false;

                }
            }




            
            COMMON_INTERVAL = CommonInterval;
            LARGE_SMA_LEN = LargeSmaLen;
            SMALL_SMA_LEN = SmallSmaLen;


            var biggerDate = DateTime.Now;


            var timeTaken = DateTime.Now;
            var LargeSmaLine = getSmaLine(ref _RawData, COMMON_INTERVAL, LARGE_SMA_LEN);


            //timeTaken = DateTime.Now;
            //var LargeSmaLine2 = _RawData.SMA_CD(COMMON_INTERVAL, LARGE_SMA_LEN).ToList();





            //StringBuilder test = new StringBuilder();
            //test.Clear();
            //LargeSmaLine2.ForEach(d => test.AppendLine(d.Time + "\t" + d.SmaValue));
            //File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Test.txt",
            //    test.ToString());



            var SmallSmaLine = getSmaLine(ref _RawData, COMMON_INTERVAL, SMALL_SMA_LEN);


            biggerDate = (LargeSmaLine.First().Time > SmallSmaLine.First().Time) ? LargeSmaLine.First().Time : SmallSmaLine.First().Time;

            //align data on same timeline 
            LargeSmaLine = LargeSmaLine.Where(d => d.Time >= biggerDate).ToList();
            SmallSmaLine = SmallSmaLine.Where(d => d.Time >= biggerDate).ToList();



            var smaD = LargeSmaLine.Zip(SmallSmaLine, (bd, sd) => sd.SmaValue - bd.SmaValue);


            _SmaDiff = smaD.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = LargeSmaLine.ElementAt(i).ActualPrice,
                Time = LargeSmaLine.ElementAt(i).Time
            });


        }


        static IntervalData getIntervalData(IntervalRange range)
        {
            var newInterval = new IntervalData
            {

                interval = _random.Next(range.interval_min, range.interval_max),
                bigSmaLen = _random.Next(range.bigSmaLen_min, range.bigSmaLen_max),
                smallSmaLen = _random.Next(range.smallSmaLen_min, range.smallSmaLen_max),
                SignalLen = _random.Next(range.SignalLen_min, range.SignalLen_max)


            };


            while (newInterval.bigSmaLen > newInterval.smallSmaLen)
            {
                newInterval = new IntervalData
                {

                    interval = _random.Next(range.interval_min, range.interval_max),
                    bigSmaLen = _random.Next(range.bigSmaLen_min, range.bigSmaLen_max),
                    smallSmaLen = _random.Next(range.smallSmaLen_min, range.smallSmaLen_max),
                    SignalLen = _random.Next(range.SignalLen_min, range.SignalLen_max)
                };
            }


            lock (_TriedListLock)
            {
                var beforeAdding = _TriedIntervalList.Count();

                _TriedIntervalList.Add(newInterval);

                var afterAdding = _TriedIntervalList.Count();

                while (beforeAdding == afterAdding)
                {

                    //Console.WriteLine("\t\t\t***");

                    Logger.WriteLog("looking for distinct interval");

                    newInterval = new IntervalData
                    {

                        interval = _random.Next(range.interval_min, range.interval_max),
                        bigSmaLen = _random.Next(range.bigSmaLen_min, range.bigSmaLen_min),
                        smallSmaLen = _random.Next(range.smallSmaLen_min, range.smallSmaLen_max),
                        SignalLen = _random.Next(range.SignalLen_min, range.SignalLen_max)

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

            if (_GraphingWindow == null)
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
        }
        public static void ManualSimulate(bool useCompoundingCalc = true)
        {
            int lastCommonInterval = 0;
            int lastBigSma = 0;
            int lastSmallSma = 0;

            Simulator1 S = null;

            var ProductName = "LTC-USD";
            TickerClient Ticker = new TickerClient(ProductName);

            //wait for ticker to get ready
            Thread.Sleep(1 * 1000);



            IntervalData intervalUsed = null;
            var lastUsedIntervalPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
                + @"\" + ProductName + "lastUsedInterval.json";


            try
            {



                Console.WriteLine("Enter simulation start date, or l to use last used interval details ");
                var tempInput = Console.ReadLine();

                if (tempInput == "l" && File.Exists(lastUsedIntervalPath))
                {
                    try
                    {
                        intervalUsed = JsonConvert.DeserializeObject<IntervalData>(File.ReadAllText(lastUsedIntervalPath));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error reading last used intervals file: " + ex.Message);
                    }

                }
                else
                {

                    if(tempInput == "l")
                    {
                        Console.WriteLine("Enter simulation start date");
                        tempInput = Console.ReadLine();
                    }

                    DateTime simStartDt;
                    DateTime.TryParse(tempInput, out simStartDt);

                    var simEndDt = DateTime.MinValue;
                    while (simEndDt == DateTime.MinValue)
                    {
                        Console.WriteLine("Enter simulation end date, enter n to use todays date:");
                        tempInput = Console.ReadLine();

                        if (tempInput == "n")
                        {
                            simEndDt = DateTime.Now;
                            break;
                        }
                        DateTime.TryParse(tempInput, out simEndDt);
                    }



                    Console.WriteLine("Enter Common time interval in minutes");
                    var inputCommonInterval = Convert.ToInt16(Console.ReadLine());

                    Console.WriteLine("Enter big sma length");
                    var inputBigSmaLen = Convert.ToInt16(Console.ReadLine());

                    Console.WriteLine("Enter small sma length");
                    var inputSmallSmaLen = Convert.ToInt16(Console.ReadLine());

                    Console.WriteLine("Enter signal len: ");
                    var inputSmaLen = Convert.ToInt16(Console.ReadLine());

                    intervalUsed = new IntervalData
                    {
                        SimStartDate = simStartDt,
                        SimEndDate = simEndDt,
                        interval = inputCommonInterval,
                        bigSmaLen = inputBigSmaLen,
                        smallSmaLen = inputSmallSmaLen,
                        SignalLen = inputSmaLen
                    };
                }




                try
                {
                    var serialisedInterval = JsonConvert.SerializeObject(intervalUsed, Formatting.Indented);

                    File.WriteAllText(lastUsedIntervalPath, serialisedInterval);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error writing last used intervals to file: " + ex.Message);
                }



                if (S == null)
                {
                    S = new Simulator1(ref Ticker, ProductName, intervalUsed.interval, intervalUsed.bigSmaLen, intervalUsed.smallSmaLen, true);
                    S.GraphWindow = _GraphingWindow;
                }
                else
                {
                    if (!(lastCommonInterval == intervalUsed.interval && lastBigSma == intervalUsed.bigSmaLen && lastSmallSma == intervalUsed.smallSmaLen))
                    {
                        S.Dispose();
                        S = null;
                        S = new Simulator1(ref Ticker, ProductName, intervalUsed.interval, intervalUsed.bigSmaLen, intervalUsed.smallSmaLen, false);
                        S.GraphWindow = _GraphingWindow;
                    }
                }

                _ActualInputStartDate = intervalUsed.SimStartDate;
                _ActualInputEndDate = intervalUsed.SimEndDate;

                S.Calculate(intervalUsed.SimStartDate, intervalUsed.SimEndDate, intervalUsed.SignalLen, true, true, useCompoundingCalc);

                lastCommonInterval = intervalUsed.interval;
                lastBigSma = intervalUsed.bigSmaLen;
                lastSmallSma = intervalUsed.smallSmaLen;

            }
            catch (Exception ex)
            {
                Console.WriteLine("invalid input / error in calc: " + ex.Message) ;
            }


        }


        private static List<ResultData> AutoSim1_ThreadSafe(ref TickerClient inputTicker, DateTime startDt, DateTime endDt, int inputSimCount, IntervalRange inputRndRange, bool useCompounding = true)
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

                IntervalData curIntervals = getIntervalData(inputRndRange);


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

                    var curPl = S.Calculate(autoStartDate, autoEndDate, curIntervals.SignalLen, false, false, useCompounding);

                    resultList.Add(new ResultData { Pl = curPl, intervals = curIntervals, SimStartDate = autoStartDate, SimEndDate = autoEndDate, intervalRange = inputRndRange });

                    lastCommonInterval = curIntervals.interval;
                    lastBigSma = curIntervals.bigSmaLen;
                    lastSmallSma = curIntervals.smallSmaLen;


                }
                catch (Exception ex)
                {
                    Console.WriteLine("invalid input / error in calc: " + ex.Message);
                }

            }

            return resultList;

        }

        public static void AutoSimulate(bool useCompoundingCalc = true)
        {
            //_TriedIntervalList.Clear();




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



            ReadTriedIntervalList(autoStartDate, autoEndDate);

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

            Int32 SLEEP_TIME_SEC = 15 * 1000;

            //Interval: 32
            //Big sma: 51
            //Small sma; 31
            //sma of macd: 21
            IntervalRange rndRange = new IntervalRange
            {
                //interval_min = 10,
                //interval_max = 60,

                //bigSmaLen_min = 30,
                //bigSmaLen_max = 200,

                //smallSmaLen_min = 15,
                //smallSmaLen_max = 60,

                //SignalLen_min = 2,
                //SignalLen_max = 30


                //interval_min = 150,
                //interval_max = 200,

                //bigSmaLen_min = 50,
                //bigSmaLen_max = 80,

                //smallSmaLen_min = 40,
                //smallSmaLen_max = 60,

                //SignalLen_min = 100,
                //SignalLen_max = 150


                interval_min = 10,
                interval_max = 200,

                bigSmaLen_min = 10,
                bigSmaLen_max = 300,

                smallSmaLen_min = 10,
                smallSmaLen_max = 300,

                SignalLen_min = 10,
                SignalLen_max = 300

            };


            Console.WriteLine("Using initial range: \n");
            rndRange.PrintRange();
            Logger.DumpLogToFile();

            _ActualInputStartDate = autoStartDate;
            _ActualInputEndDate = autoEndDate;

            double lastBest = 0;

            for (int batch = eachBatchCount; batch < simCount + eachBatchCount; batch += eachBatchCount)
            {


                Console.WriteLine("starting batch: " + (batch - eachBatchCount) + " - " + batch + " of " + simCount);

                if (batch > eachBatchCount)
                    Thread.Sleep(SLEEP_TIME_SEC);

                var threadCount = Math.Ceiling((eachBatchCount / (double)Each_Sim_Count));

                for (int i = 0; i < threadCount; i++)
                {
                    var curTask = Task.Factory.StartNew(() =>
                    {
                        var returnedResult = AutoSim1_ThreadSafe(ref Ticker, autoStartDate, autoEndDate, Each_Sim_Count, rndRange, useCompoundingCalc); //RunSim_ThreadSafe(ref Ticker, autoStartDate, autoEndDate, Each_Sim_Count);
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

                if (batch >= (simCount / 2))
                {

                    List<ResultData> topFiveResults = new List<ResultData>();

                    var sortedResults = allCombinedResultList.OrderByDescending(r => r.Pl).ToList();

                    var best_AfterHalf = sortedResults.First();

                    Logger.WriteLog("best result so far: " + best_AfterHalf.Pl);


                    if (lastBest == 0 || best_AfterHalf.Pl > lastBest)
                    {
                        if (sortedResults.Count >= 10)
                        {
                            topFiveResults.AddRange(sortedResults.Take(10));

                            Logger.WriteLog("using new range from top 10 results: \n");


                            rndRange = IntervalRange.GetaHalfRange(topFiveResults);

                            rndRange.PrintRange();

                        }
                        else
                        {
                            rndRange = IntervalRange.GetaHalfRange(topFiveResults);
                            //rndRange = IntervalRange.GetaHalfRange(best_AfterHalf.intervalRange, best_AfterHalf.intervals);
                            Logger.WriteLog("using new range: \n");
                            rndRange.PrintRange();
                        }

                    }



                    lastBest = best_AfterHalf.Pl;

                }

            }







            Console.WriteLine("\nTop 5 profit results\n");

            Logger.DumpLogToFile();

            IntervalData bestValues = null;

            //var sortedRes = allCombinedResultList.OrderByDescending(r => r.Pl).ToList();
            var sortedRes = allCombinedResultList; //allCombinedResultList.OrderByDescending(r => r.Pl).ToList();

            sortedRes = sortedRes.OrderByDescending(a => a.Pl).ToList();

            var triedListCopy = new List<ResultData>(allCombinedResultList);

            for (int i = 0; i < 5; i++)
            {
                var best = sortedRes.ElementAt(i); //allCombinedResultList.Where(d => d.Pl == allCombinedResultList.Max(a => a.Pl)).First();

                var resMsg = "best result: " + i + "\n"
                    + "PL: " + best.Pl + "\n"
                    + "Interval: " + best.intervals.interval + "\n"
                    + "Signal len: " + best.intervals.SignalLen + "\n"
                    + "big sma: " + best.intervals.bigSmaLen + "\n"
                    + "small sma: " + best.intervals.smallSmaLen + "\n";


                Logger.WriteLog("\n" + resMsg);

                //allCombinedResultList.Remove(best);

            }


            bestValues = sortedRes.First().intervals;






            var S = new Simulator1(ref Ticker, ProductName, bestValues.interval, bestValues.bigSmaLen, bestValues.smallSmaLen, true);
            S.GraphWindow = _GraphingWindow;
            S.Calculate(autoStartDate, autoEndDate, bestValues.SignalLen, true, true, useCompoundingCalc);



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

            WriteTriedIntervalList(triedListCopy, autoStartDate, autoEndDate);

        }

        private struct ToFrom
        {
            public DateTime From { get; set; }
            public DateTime To { get; set; }
        }

        public static void AutoSimulate_DateRange(bool useCompoundingCalc = true)
        {
            //_TriedIntervalList.Clear();




            //ShowGraphingForm();

            

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






            //int simCount = 0;
            //int Each_Sim_Count = 50;//50;

            //while (simCount == 0)
            //{
            //    Console.WriteLine("Enter number of simulation count for each date interval, " + Each_Sim_Count + " minimun");
            //    simCount = Convert.ToInt32(Console.ReadLine());
            //}



            List<ToFrom> startEndList = new List<ToFrom>();

            var s = autoStartDate;
            var DAYS_SLICE = 7;
            var now = DateTime.Now;

            startEndList.Clear();
            while (s < now )
            {
                startEndList.Add(new ToFrom {From = s, To = s.AddDays(DAYS_SLICE).AddMinutes(-1)});
                s = s.AddDays(DAYS_SLICE);
            }





            List<ResultData> allDateRangeResultList = new List<ResultData>();


            List<Task> dateRangeTaskList = new List<Task>();


            

            foreach (var dateInterval in startEndList)
            {

                var curThread = Task.Factory.StartNew(()=> 
                {
                    var currentDtRangeBestResult = DoIterations(dateInterval.From, dateInterval.To);
                    allDateRangeResultList.Add(currentDtRangeBestResult);
                });

                dateRangeTaskList.Add(curThread);


                if (dateRangeTaskList.Count > 2)
                {
                    Task.WaitAny(dateRangeTaskList.ToArray());

                    dateRangeTaskList.RemoveAll(t => t.IsCompleted);
                }


            }

            allDateRangeResultList = allDateRangeResultList.OrderByDescending(f => f.SimStartDate).ToList();


            Console.WriteLine("best results in each date range: ");



            var tempTicker = new TickerClient(ProductName);




            double TotalPl = 0.0;

            decimal LastUsdBalance = 0.0m;

            foreach (var result in allDateRangeResultList)
            {
                
                var resMsg = string.Format("start: {0} \t end: {1}\n", result.SimStartDate, result.SimEndDate) + "\n"
                    + "PL: " + result.Pl + "\n"
                    + "Interval: " + result.intervals.interval + "\n"
                    + "Signal len: " + result.intervals.SignalLen + "\n"
                    + "big sma: " + result.intervals.bigSmaLen + "\n"
                    + "small sma: " + result.intervals.smallSmaLen + "\n";

                Logger.WriteLog(resMsg);


                var bestValues = result.intervals;
                var S = new Simulator1(ref tempTicker, ProductName, bestValues.interval, bestValues.bigSmaLen, bestValues.smallSmaLen, true);
                S.GraphWindow = _GraphingWindow;

                if (LastUsdBalance == 0.0m)
                    LastUsdBalance = 8000;

                LastUsdBalance = (decimal)S.Calculate(result.SimStartDate, result.SimEndDate, bestValues.SignalLen, true, true, useCompoundingCalc, LastUsdBalance) + 8000;

                TotalPl += (double)LastUsdBalance - 8000;

            }

            Logger.WriteLog("Total PL: " + allDateRangeResultList.Sum(a => a.Pl));


        }


        private static ResultData DoIterations(DateTime StartDateTime, DateTime EndDateTime)
        {
            

            ReadTriedIntervalList(StartDateTime, EndDateTime);

            int SIM_Count = 4000;
            int Each_Sim_Count = 50;//50;


            bool useCompoundingCalc = true;

            List<Task> allSimTasks = new List<Task>();

            var eachBatchCount = 1000;

            var lastBatchCompletionTime = DateTime.Now;

            Int32 SLEEP_TIME_SEC = 15 * 1000;
            IntervalRange rndRange = new IntervalRange
            {
                interval_min = 10,
                interval_max = 60,

                bigSmaLen_min = 30,
                bigSmaLen_max = 200,

                smallSmaLen_min = 15,
                smallSmaLen_max = 60,

                SignalLen_min = 2,
                SignalLen_max = 30
            };


            Console.WriteLine("Using initial range: \n");
            rndRange.PrintRange();

            double lastBest = 0;

            List<ResultData> allCombinedResultList = new List<ResultData>();


            TickerClient Ticker = new TickerClient(ProductName);

            var startTime = DateTime.Now;

            for (int batch = eachBatchCount; batch < SIM_Count + eachBatchCount; batch += eachBatchCount)
            {

                Console.WriteLine("starting batch: " + (batch - eachBatchCount) + " - " + batch);
                Thread.Sleep(SLEEP_TIME_SEC);

                var threadCount = Math.Ceiling((eachBatchCount / (double)Each_Sim_Count));

                for (int i = 0; i < threadCount; i++)
                {
                    var curTask = Task.Factory.StartNew(() =>
                    {
                        var returnedResult = AutoSim1_ThreadSafe(ref Ticker, StartDateTime, EndDateTime, Each_Sim_Count, rndRange, useCompoundingCalc); //RunSim_ThreadSafe(ref Ticker, autoStartDate, autoEndDate, Each_Sim_Count);
                        allCombinedResultList.AddRange(returnedResult);
                    });

                    allSimTasks.Add(curTask);
                }

                Task.WaitAll(allSimTasks.ToArray());


                if (batch >= eachBatchCount)
                {
                    var timeTakenLastBatch = (DateTime.Now - lastBatchCompletionTime).TotalMinutes;
                    Console.WriteLine("Time taken to complete last batch (min): " + timeTakenLastBatch);

                    var expectedCompletionTime = (timeTakenLastBatch + ((SLEEP_TIME_SEC / 1000) / 60)) * ((SIM_Count - batch) / eachBatchCount);
                    Console.WriteLine("Expected completion time: " + expectedCompletionTime + " min, " + DateTime.Now.AddMinutes(expectedCompletionTime));
                    lastBatchCompletionTime = DateTime.Now;
                }

                if (batch >= (SIM_Count / 2))
                {

                    List<ResultData> topFiveResults = new List<ResultData>();

                    var sortedResults = allCombinedResultList.OrderByDescending(r => r.Pl).ToList();

                    var best_AfterHalf = sortedResults.First();

                    Logger.WriteLog("best result so far: " + best_AfterHalf.Pl);


                    if (lastBest == 0 || best_AfterHalf.Pl > lastBest)
                    {
                        if (sortedResults.Count >= 10)
                        {
                            topFiveResults.AddRange(sortedResults.Take(10));

                            Logger.WriteLog("using new range from top 10 results: \n");


                            rndRange = IntervalRange.GetaHalfRange(topFiveResults);

                            rndRange.PrintRange();

                        }
                        else
                        {
                            rndRange = IntervalRange.GetaHalfRange(best_AfterHalf.intervalRange, best_AfterHalf.intervals);
                            Logger.WriteLog("using new range: \n");
                            rndRange.PrintRange();
                        }

                    }



                    lastBest = best_AfterHalf.Pl;

                }

            }



            Console.WriteLine("\nTop 5 profit results\n");

            IntervalData bestValues = null;

            //var sortedRes = allCombinedResultList.OrderByDescending(r => r.Pl).ToList();
            var sortedRes = allCombinedResultList; //allCombinedResultList.OrderByDescending(r => r.Pl).ToList();

            sortedRes = sortedRes.OrderByDescending(a => a.Pl).ToList();

            var triedListCopy = new List<ResultData>(allCombinedResultList);

            for (int i = 0; i < 5; i++)
            {
                var best = sortedRes.ElementAt(i); //allCombinedResultList.Where(d => d.Pl == allCombinedResultList.Max(a => a.Pl)).First();

                var resMsg = "best result: " + i + "\n"
                    + "PL: " + best.Pl + "\n"
                    + "Interval: " + best.intervals.interval + "\n"
                    + "Signal len: " + best.intervals.SignalLen + "\n"
                    + "big sma: " + best.intervals.bigSmaLen + "\n"
                    + "small sma: " + best.intervals.smallSmaLen + "\n";


                Logger.WriteLog("\n" + resMsg);

                //allCombinedResultList.Remove(best);

            }


            bestValues = sortedRes.First().intervals;




            _ActualInputStartDate = StartDateTime;
            _ActualInputEndDate = EndDateTime;

            var S = new Simulator1(ref Ticker, ProductName, bestValues.interval, bestValues.bigSmaLen, bestValues.smallSmaLen, true);
            S.GraphWindow = _GraphingWindow;
            S.Calculate(StartDateTime, EndDateTime, bestValues.SignalLen, true, true, useCompoundingCalc);


            Console.WriteLine("time taken for " + allSimTasks.Count() * Each_Sim_Count + " iterations (min): " + (DateTime.Now - startTime).TotalMinutes);

            WriteTriedIntervalList(triedListCopy, StartDateTime, EndDateTime);

            return sortedRes.First();

        }



        public double Calculate(DateTime simStartDate, DateTime inputEndDate, int inputSignalLen = 2, 
            bool printTrades = true, bool renderGraph = false, bool useCompounding = true, decimal initialUsdAmount = 8000)
        {

            _SignalLen = inputSignalLen;


            var simEndTime = inputEndDate; //DateTime.Now;



            var signalLine1 = _SmaDiff.Select(d => d.SmaValue).ToList().SMA(inputSignalLen);

            //var signalLine1 = _SmaDiff.Select(d => d.SmaValue).ToList().EMA(inputSignalLen);

            var requiredSignalDtPts1 = _SmaDiff.Skip(inputSignalLen - 1).ToList();
            var SignalWithDataPtsBig = signalLine1.Select((d, i) => new SmaData
            {
                SmaValue = d,
                ActualPrice = requiredSignalDtPts1.ElementAt(i).ActualPrice,
                Time = requiredSignalDtPts1.ElementAt(i).Time
            }).ToList();







            //test for using double signla lines, one big and one small___________________________________________________________
            ////////////var signalLen2 = inputSignalLen;
            ////////////var signalLine2 = SmaDiff.Select(d => d.SmaValue).ToList().SMA(signalLen2);
            ////////////var requiredSignalDtPts2 = SmaDiff.Skip(signalLen2 - 1).ToList();
            ////////////var SignalWithDataPtsSmall = signalLine2.Select((d, i) => new SmaData
            ////////////{
            ////////////    SmaValue = d,
            ////////////    ActualPrice = requiredSignalDtPts2.ElementAt(i).ActualPrice,
            ////////////    Time = requiredSignalDtPts2.ElementAt(i).Time
            ////////////}).ToList();


            ////////////var largestDate = DateTime.Now;

            ////////////if (SignalWithDataPtsBig.First().Time >= SignalWithDataPtsSmall.First().Time)
            ////////////{
            ////////////    largestDate = SignalWithDataPtsBig.First().Time;
            ////////////}
            ////////////else
            ////////////{
            ////////////    largestDate = SignalWithDataPtsSmall.First().Time;
            ////////////}


            ////////////SignalWithDataPtsBig = SignalWithDataPtsBig.Where(f => f.Time >= largestDate).ToList();

            ////////////SignalWithDataPtsSmall = SignalWithDataPtsSmall.Where(f => f.Time >= largestDate).ToList();


            ////////////SmaDiff = SmaDiff.Where(f => f.Time >= largestDate).ToList();

            //test for using double signla lines, one big and one small___________________________________________________________

            ////this ensures the time lines are the same for signal and sma





            _SmaDiff = requiredSignalDtPts1; //SmaDiff.Skip(inputSmaOfMacdLen);




            List<CrossData> allCrossings_Linq = new List<CrossData>();

            var requiredMacdPtsLnq = _SmaDiff.Where((s => s.Time >= simStartDate && s.Time < simEndTime)).ToList();

            var requiredSignalPtsLnq_big = SignalWithDataPtsBig.Where((s => s.Time >= simStartDate && s.Time < simEndTime)).ToList();

            ////////////var requiredSignalPtsLnq_small = SignalWithDataPtsSmall.Where((s => s.Time >= simStartDate && s.Time < simEndTime)).ToList();

            ////////////var allBuys = requiredMacdPtsLnq.Where((d, i) => 
            ////////////(d.SmaValue > requiredSignalPtsLnq_big.ElementAt(i).SmaValue) || (d.SmaValue > requiredSignalPtsLnq_small.ElementAt(i).SmaValue)).ToList();

            ////////////var allSells = requiredMacdPtsLnq.Where((d, i) => d.SmaValue < requiredSignalPtsLnq_small.ElementAt(i).SmaValue).ToList();


            var allBuys = requiredMacdPtsLnq.Where((d, i) => d.SmaValue > requiredSignalPtsLnq_big.ElementAt(i).SmaValue).ToList();

            var allSells = requiredMacdPtsLnq.Where((d, i) => d.SmaValue < requiredSignalPtsLnq_big.ElementAt(i).SmaValue).ToList();




            //var allBuys2 = requiredMacdPtsLnq.Where((d, i) => d.SmaValue > requiredSignalPtsLnq_big.ElementAt(i).SmaValue).ToList();

            //var allSells2 = requiredMacdPtsLnq.Where((d, i) => d.SmaValue < requiredSignalPtsLnq_big.ElementAt(i).SmaValue).ToList();






            Utilities crossingCal = new Utilities(_RawData);
            //var Linqres = crossingCal.Getcrossings_Linq(requiredSmaDiffPtsLnq, requiredSignalPtsLnq, SmaDiff.First().Time, SmaDiff.Last().Time);
            var curLinqCrossres = crossingCal.Getcrossings_Linq(allBuys, allSells);
            allCrossings_Linq.AddRange(curLinqCrossres);



            //Console.WriteLine("\nLinq (press enter to continue):\n");
            //Console.ReadLine();


            var Pl = 0.0;
            if (useCompounding)
            {
                Pl = CalculatePl_Compounding(allCrossings_Linq, printTrades, initialUsdAmount);
            }
            else
            {
                Pl = CalculatePl_NonCompounding(allCrossings_Linq, printTrades);
            }
            


            if (renderGraph)
            {
                //List<List<SmaData>> allSeries = new List<List<SmaData>>();
                List<SeriesDetails> allSereis2 = new List<SeriesDetails>();

                //allSeries.Add( Price);
                //allSeries.Add(BigSma);
                //allSeries.Add(SmallSma);

                //allSereis2.Add(new SeriesDetails { series = ActualPriceList, SereiesName = "Actual Price" });
                //BigSma.SmaDataPts_Candle

                //enter moving average data to graph needed

                var lastAction = allCrossings_Linq.First();
                var firsAction = allCrossings_Linq.Last();

                if (lastAction.Action == "buy")
                    allCrossings_Linq.Last().BufferedCrossingPrice = (Double)allCrossings_Linq.Last().CrossingPrice;

                if (firsAction.Action == "sell")
                    allCrossings_Linq.First().BufferedCrossingPrice = (Double)allCrossings_Linq.First().CrossingPrice;

                Task.Run(()=>
                {
                    var PriceLine = requiredMacdPtsLnq.Select(p => new SmaData { ActualPrice = p.ActualPrice, Time = p.Time }).ToList();
                    allSereis2.Add(new SeriesDetails { DataPoints = PriceLine, SereiesName = "Price (" + COMMON_INTERVAL + " min)" });
                    allSereis2.Add(new SeriesDetails { DataPoints = requiredMacdPtsLnq, SereiesName = "macd (L:" + LARGE_SMA_LEN + " S:" + SMALL_SMA_LEN + ")" });
                    allSereis2.Add(new SeriesDetails { DataPoints = requiredSignalPtsLnq_big, SereiesName = "signal (" + inputSignalLen + ")" });
                    //allSereis2.Add(new SeriesDetails { series = SmallSma, SereiesName = "Small_Sma" });

                    CurResultsSeriesList.AddRange(allSereis2);

                    CurResultCrossList.AddRange(allCrossings_Linq);

                    if (GraphWindow != null)
                        GraphWindow.DrawSeriesSim1(allSereis2, allCrossings_Linq, Pl);
                });

                //var PriceLine = requiredMacdPtsLnq.Select(p => new SmaData { ActualPrice = p.ActualPrice, Time = p.Time }).ToList();
                //allSereis2.Add(new SeriesDetails { series = PriceLine, SereiesName = "Price" });
                //allSereis2.Add(new SeriesDetails { series = requiredMacdPtsLnq, SereiesName = "macd" });
                //allSereis2.Add(new SeriesDetails { series = requiredSignalPtsLnq_big, SereiesName = "signal" });
                ////allSereis2.Add(new SeriesDetails { series = SmallSma, SereiesName = "Small_Sma" });

                //CurResultsSeriesList.AddRange(allSereis2);

                //CurResultCrossList.AddRange(allCrossings_Linq);

                //if(GraphWindow != null)
                //    GraphWindow.DrawSeriesSim1(allSereis2, allCrossings_Linq);
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
            //BigSma?.Dispose();
            //SmallSma?.Dispose();

        }

        double CalculatePl_Compounding(List<CrossData> allCrossings, bool printTrade = true, decimal initialUsdBalance = 8000)
        {
            if (allCrossings.Count() <= 1)
            {
                Console.WriteLine("No crossing data!");
                return 0;
            }

            allCrossings = allCrossings.OrderByDescending((d) => d.dt).ToList();

            decimal curProdSize = 0;
            decimal USDbalance = initialUsdBalance;
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


            const decimal STOP_LOSS_PERCENTAGE = 0.0m;//0.02m;

            var lastAction = "";

            int stopLossSale = 0;



            //allCrossings.ForEach((a)=>System.Diagnostics.Debug.WriteLine(a.dt + "\t" + a.CrossingPrice + "\t" + a.Action ));

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
                            + cross.Action + ((cross.comment != null) ? "*" : "") + "\t\t\t"
                            + Math.Round(buyAtPrice, 2).ToString() + "\t\t"
                            + Math.Round(buyFee, 2).ToString() + "\t\t"
                            + Math.Round(curProdSize, 2).ToString() + "\t\t\t\t"
                            + Math.Round(USDbalance, 2).ToString();
                        //Console.WriteLine(buyMsg);
                        trans.AppendLine(buyMsg);


                        var buyMsgCsv = cross.dt.ToString(@"yyyy-MM-dd HH:mm:ss") + "\t"
                            + cross.Action + ((cross.comment != null) ? "*" : "") + "\t"
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
                        //sellAtPrice = stopLossPrice - BUFFER;
                        //cross.BufferedCrossingPrice = (double)sellAtPrice;
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

                    //if (isStopLossSale)
                    //{
                    //    sellingPrice = sellingPrice + "*";
                    //}


                    if (cross.comment == "STOP_LOSS_SALE")
                    {
                        sellingPrice = sellingPrice + "*";
                    }


                    if (printTrade)
                    {
                        var sellMsg = cross.dt.ToString(@"yyyy-MM-dd HH:mm:ss") + "\t"
                            + cross.Action + ((cross.comment != null) ? "" : "") + "\t\t"
                            + sellingPrice + "\t\t"
                            + Math.Round(sellFee, 2).ToString() + "\t\t"
                            + Math.Round(curProdSize, 2).ToString() + "\t"
                            + Math.Round(netpl, 2).ToString() + "\t\t"
                            + Math.Round(USDbalance, 2).ToString();
                        trans.AppendLine(sellMsg);


                        var sellMsgCsv = cross.dt.ToString(@"yyyy-MM-dd HH:mm:ss") + "\t"
                            + cross.Action + ((cross.comment != null) ? "" : "") + "\t"
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


            //Console.WriteLine("\n");

            var msg = "";
            msg = "" +
                "\nFrom: " + _ActualInputStartDate.Date.ToString("yyyy-MMM-dd") + " To: " + _ActualInputEndDate.Date.ToString("yyyy-MMM-dd") + "\n" +
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



        double CalculatePl_NonCompounding(List<CrossData> allCrossings, bool printTrade = true)
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

            const decimal AMOUNT = 50.0m;

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

                    buyFee = AMOUNT * buyAtPrice * FEE_PERCENTAGE;//(USDbalance) * FEE_PERCENTAGE;
                    curProdSize = AMOUNT; //(USDbalance - buyFee) / buyAtPrice;
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

                    sellFee = AMOUNT * sellAtPrice * FEE_PERCENTAGE; //(curProdSize * sellAtPrice) * FEE_PERCENTAGE;
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


            //Console.WriteLine("\n");

            var msg = "";
            msg = "" +
                "\nFrom: " + _ActualInputStartDate.Date.ToString("yyyy-MMM-dd") + " To: " + _ActualInputEndDate.Date.ToString("yyyy-MMM-dd") + "\n" +
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


            //"\nFrom: " + allCrossings.Last().dt.Date.ToString("yyyy-MMM-dd") + " To: " + allCrossings.First().dt.Date.ToString("yyyy-MMM-dd") + "\n" +

            trans.AppendLine(msg);

            transCsv.AppendLine(msg);

            Logger.WriteLog("\n Non compounding results:\n" + trans.ToString());

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


        //void CalculatePl_NonCompounding(List<CrossData> allCrossings)
        //{


        //    const int AMOUNT = 50;
        //    decimal pl = 0;
        //    const decimal FEE = 0.003m;
        //    decimal totalFee = 0.0m;

        //    if (allCrossings.First().Action == "buy")
        //    {
        //        allCrossings.RemoveAt(allCrossings.IndexOf(allCrossings.First()));
        //    }

        //    if (allCrossings.Last().Action == "sell")
        //    {
        //        allCrossings.RemoveAt(allCrossings.IndexOf(allCrossings.Last()));
        //    }

        //    Console.WriteLine("Time\t\t\taction\t\tPrice\t\tFee\t\tPL\t\tBalance");

        //    var plList = new List<decimal>();

        //    var curPl = 0.0m;


        //    for (int i = allCrossings.Count() - 1; i >= 0; i--)
        //    {
        //        var cross = allCrossings[i];
        //        var curFee = (AMOUNT * cross.CrossingPrice) * FEE;

        //        totalFee += curFee;
        //        if (cross.Action == "buy")
        //        {
        //            pl = pl - (AMOUNT * cross.CrossingPrice) + curFee;

        //            curPl = (AMOUNT * cross.CrossingPrice) + curFee;

        //            Console.WriteLine(cross.dt.ToString() + "\t"
        //                + cross.Action + "\t\t"
        //                + cross.CrossingPrice.ToString() + "\t\t"
        //                + Math.Round(curFee, 2).ToString() + "\t\t"
        //                + "\t\t"
        //                + pl.ToString());
        //        }

        //        if (cross.Action == "sell")
        //        {
        //            pl = pl + (AMOUNT * cross.CrossingPrice) - curFee;

        //            var netpl = ((AMOUNT * cross.CrossingPrice) - curFee) - curPl;

        //            Console.WriteLine(cross.dt.ToString() + "\t"
        //                + cross.Action + "\t\t"
        //                + cross.CrossingPrice.ToString() + "\t\t"
        //                + Math.Round(curFee, 2).ToString() + "\t\t"
        //                + Math.Round(netpl, 2).ToString() + "\t\t"
        //                + pl.ToString());

        //            plList.Add(netpl);
        //        }
        //    }



        //    Console.WriteLine("Total Trades: " + allCrossings.Count());
        //    Console.WriteLine("Profit/Loss: " + plList.Sum().ToString());
        //    Console.WriteLine("Total Fees: " + totalFee.ToString());
        //    Console.WriteLine("Biggest Profit: " + plList.Max().ToString());
        //    Console.WriteLine("Biggest Profit: " + plList.Min().ToString());

        //}


    }



}


