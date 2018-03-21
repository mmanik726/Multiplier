using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoinbaseExchange.NET.Data;
using CoinbaseExchange.NET.Endpoints.PublicData;

using CoinbaseExchange.NET.Utilities;
using System.Threading;


namespace Simulator
{
    class Program
    {



        static void Main(string[] args)
        {

            Logger.Logupdated += (object sender, LoggerEventArgs largs) => { Console.WriteLine(largs.LogMessage); };

            int lastCommonInterval = 0;
            int lastBigSma = 0;
            int lastSmallSma = 0;

            Simulator S = null;

            while (true)
            {
                try
                {
                    Console.WriteLine("Enter simulation start date:");
                    var inDt = Console.ReadLine();
                    DateTime dt;
                    DateTime.TryParse(inDt, out dt);

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
                        S = new Simulator(inputCommonInterval, inputBigSmaLen, inputSmallSmaLen);
                    }
                    else
                    {
                        if (!(lastCommonInterval == inputCommonInterval && lastBigSma == inputBigSmaLen && lastSmallSma == inputSmallSmaLen))
                        {
                            S.Dispose();
                            S = null;
                            S = new Simulator(inputCommonInterval, inputBigSmaLen, inputSmallSmaLen);
                        }
                    }

                    S.Calculate(dt, inputSmaLen);

                    lastCommonInterval = inputCommonInterval;
                    lastBigSma = inputBigSmaLen;
                    lastSmallSma = inputSmallSmaLen;

                }
                catch (Exception)
                {
                    Console.WriteLine("invalid input / error in calc");
                }

            }


        }

    }






    class Simulator : IDisposable
    {
        MovingAverage SmallSma;
        MovingAverage BigSma;
        private int COMMON_INTERVAL;

        private int LARGE_SMA_LEN;
        private int SMALL_SMA_LEN;


        private IEnumerable<DataPoint> SmaDiff;

        private int SignalLen; 

        public Simulator(int CommonInterval = 30, int LargeSmaLen = 100, int SmallSmaLen = 35)
        {
            var productName = "LTC-USD";
            TickerClient ticker = new TickerClient(productName);


            COMMON_INTERVAL = CommonInterval;
            LARGE_SMA_LEN = LargeSmaLen;
            SMALL_SMA_LEN = SmallSmaLen;


            BigSma = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, LARGE_SMA_LEN);
            SmallSma = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, SMALL_SMA_LEN);


            var largeSmaDataPoints = BigSma.SmaDataPoints;
            var smallSmaDataPoints = SmallSma.SmaDataPoints;


            //var x = BigSma.SmaDataPts_Candle;
            //var y = SmallSma.SmaDataPts_Candle;

            SmaDiff =
                from i in
                Enumerable.Range(0, Math.Max(largeSmaDataPoints.Count, smallSmaDataPoints.Count))
                select new DataPoint
                {
                    diff = smallSmaDataPoints.ElementAtOrDefault(i) - largeSmaDataPoints.ElementAtOrDefault(i),
                    ActualPrice = MovingAverage.SharedRawExchangeData.ElementAt(i * COMMON_INTERVAL).Close,
                    dt = MovingAverage.SharedRawExchangeData.ElementAt(i * COMMON_INTERVAL).Time

                    //////BigSma.SmaDataPts_Candle is equal to SmallSma.SmaDataPts_Candle since common interval is the same 
                    ////ActualPrice = BigSma.SmaDataPts_Candle.ElementAtOrDefault(i).Close,
                    ////dt = BigSma.SmaDataPts_Candle.ElementAtOrDefault(i).Time

                };

        }


        private class DateLst
        {
            public int groupNo { get; set; }
            public DateTime start { get; set; }
            public DateTime end { get; set; }

        }

        public async void Calculate(DateTime simStartDate, int inputSmaOfMacdLen = 2)
        {

            SignalLen = inputSmaOfMacdLen;
            //var bigSmaOfMacd = smaDiff.Select(d=>d.diff).ToList().SMA(L_SIGNAL_LEN);
            //var smallSmaOfMacd = smaDiff.Select(d=>d.diff).ToList().SMA(S_SIGNAL_LEN);

            Console.WriteLine("Calculating buy / sell actions...");

            var timePeriod = Math.Floor((DateTime.Now - simStartDate).TotalHours / 24) + 1;

            //////List<DateLst> dLst = new List<DateLst>();


            //////var endTime = simStartDate.AddHours(24);

            //////for (int i = 0; i < timePeriod; i++)
            //////{
            //////    dLst.Add(new DateLst
            //////    {
            //////        groupNo = i * 10000,
            //////        start = simStartDate,
            //////        end = endTime
            //////    });

            //////    simStartDate = endTime;
            //////    endTime = endTime.AddHours(24);
            //////}



            //////List<CrossData> allCrossings = new List<CrossData>();

            //////Object addLock = new object();

            //////Parallel.ForEach(dLst, item => 
            //////{
            //////    var res = Utilities.Getcrossings(SmaDiff, item.start, item.end, inputSmaOfMacdLen, item.groupNo);
            //////    lock (addLock)
            //////    {
            //////        allCrossings.AddRange(res.Result);
            //////    }

            //////});


            


            


            //Parallel.For(0, Convert.ToInt32(timePeriod),
            //                   index =>
            //                   {
            //                       var res = Utilities.Getcrossings(SmaDiff, simStartDate, endTime, inputSmaOfMacdLen);
            //                       res.Wait();
            //                       allCrossings.AddRange(res.Result);
            //                       simStartDate = endTime;
            //                       endTime = endTime.AddHours(24);
            //                   });





            ////Object addLock = new object();

            ////for (int i = 0; i < timePeriod; i++)
            ////{

            ////    await Task.Run(() =>
            ////    {
            ////        var res = Utilities.Getcrossings(SmaDiff, simStartDate, endTime, inputSmaOfMacdLen);
            ////        //res.Wait();

            ////        lock (addLock)
            ////        {
            ////            allCrossings.AddRange(res.Result);
            ////        }


            ////    });



            ////    simStartDate = endTime;
            ////    endTime = endTime.AddHours(24);

            ////}

            //System.Threading.Thread.Sleep(5 * 1 * 1000);

            //allCrossings.OrderBy((d) => d.dt);

            var allCrossings = Utilities.Getcrossings(SmaDiff, simStartDate, DateTime.Now, inputSmaOfMacdLen);
            Console.WriteLine("done");
            CalculatePl_Compounding(allCrossings.Result);


        }

        public void Dispose()
        {
            //throw new NotImplementedException();
            BigSma?.Dispose();
            SmallSma?.Dispose();

        }

        void CalculatePl_Compounding(List<CrossData> allCrossings)
        {
            allCrossings = allCrossings.OrderByDescending((d)=>d.dt).ToList();

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

            Console.WriteLine("Time\t\t\taction\t\tPrice\t\tFee\t\tSize\tPL\t\tBalance");

            var plList = new List<decimal>();

            var buyAtPrice = 0.0m;
            var sellAtPrice = 0.0m;

            var buyFee = 0.0m;
            var sellFee = 0.0m;

            const decimal BUFFER = 0.60m;//1.50m;//1.0m; //0.75m;


            const decimal STOP_LOSS_PERCENTAGE = 0.02m;

            for (int i = allCrossings.Count() - 1; i >= 0; i--)
            {
                var cross = allCrossings[i];

                if (cross.Action == "buy")
                {
                    buyAtPrice = cross.CrossingPrice + BUFFER;

                    buyFee = (USDbalance) * FEE_PERCENTAGE;
                    curProdSize = (USDbalance - buyFee) / buyAtPrice;
                    totalFee += buyFee;

                    USDbalance = USDbalance - (curProdSize * buyAtPrice) - buyFee;

                    Console.WriteLine(
                        cross.dt.ToString() + "\t"
                        + cross.Action + "\t\t"
                        + Math.Round(buyAtPrice, 2).ToString() + "\t\t"
                        + Math.Round(buyFee, 2).ToString() + "\t\t"
                        + Math.Round(curProdSize, 2).ToString() + "\t\t\t"
                        + Math.Round(USDbalance, 2).ToString());
                }

                if (cross.Action == "sell")
                {
                    sellAtPrice = cross.CrossingPrice - BUFFER;

                    var stopLossPrice = buyAtPrice - (buyAtPrice * STOP_LOSS_PERCENTAGE);
                    if (sellAtPrice < stopLossPrice)
                    {
                        sellAtPrice = stopLossPrice - BUFFER;
                    }

                    sellFee = (curProdSize * sellAtPrice) * FEE_PERCENTAGE;
                    USDbalance = USDbalance + (curProdSize * sellAtPrice) - sellFee;

                    totalFee += sellFee;



                    var netpl = ((sellAtPrice - buyAtPrice) * curProdSize ) - (buyFee + sellFee);

                    Console.WriteLine(
                        cross.dt.ToString() + "\t"
                        + cross.Action + "\t\t"
                        + Math.Round(sellAtPrice, 2).ToString() + "\t\t"
                        + Math.Round(sellFee, 2).ToString() + "\t\t"
                        + Math.Round(curProdSize, 2).ToString() + "\t"
                        + Math.Round(netpl, 2).ToString() + "\t\t"
                        + Math.Round(USDbalance, 2).ToString());

                    plList.Add(netpl);
                }
            }


            Console.WriteLine("\nFrom: " + allCrossings.Last().dt.ToString() + " To; " + allCrossings.First().dt.ToString());
            Console.WriteLine("Interval: " + COMMON_INTERVAL.ToString());
            Console.WriteLine("Big sma: " + LARGE_SMA_LEN.ToString());
            Console.WriteLine("Small sma; " + SMALL_SMA_LEN.ToString());
            Console.WriteLine("sma of macd; " + SignalLen.ToString());
            Console.WriteLine("\nTotal Trades: " + allCrossings.Count());
            Console.WriteLine("Profit/Loss: " + Math.Round(plList.Sum(), 2).ToString());
            Console.WriteLine("Total Fees: " + Math.Round(totalFee, 2).ToString());
            Console.WriteLine("Biggest Profit: " + Math.Round(plList.Max(), 2).ToString());
            Console.WriteLine("Biggest Loss: " + Math.Round(plList.Min(), 2).ToString());
            Console.WriteLine("Avg. PL / Trade: " + Math.Round(plList.Average(), 2).ToString());

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



    public class DataPoint
    {
        public double diff { get; set; }
        public decimal ActualPrice { get; set; }
        public DateTime dt { get; set; }
    }



}
