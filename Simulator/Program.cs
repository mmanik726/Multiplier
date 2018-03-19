using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoinbaseExchange.NET.Data;
using CoinbaseExchange.NET.Endpoints.PublicData;

using CoinbaseExchange.NET.Utilities;
namespace Simulator
{
    class Program
    {



        static void Main(string[] args)
        {

            Logger.Logupdated += (object sender, LoggerEventArgs largs) => { Console.WriteLine(largs.LogMessage); };
            

            var s = new Simulator();
            s.Start();


        }

    }






    class Simulator
    {
        MovingAverage SmallSma;
        MovingAverage BigSma;
        private const int COMMON_INTERVAL = 30;

        private const int LARGE_SMA_LEN = 100;
        private const int SMALL_SMA_LEN = 35;


        private int updateInterval;

        private const int L_SIGNAL_LEN = 10;
        private const int S_SIGNAL_LEN = 5;

        public Simulator()
        {
            var productName = "LTC-USD";
            TickerClient ticker = new TickerClient(productName);

            updateInterval = COMMON_INTERVAL; //update values every 1 min to keep sma data updated

            BigSma = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, LARGE_SMA_LEN);
            SmallSma = new MovingAverage(ref ticker, productName, COMMON_INTERVAL, SMALL_SMA_LEN);
        }


        public void Start()
        {

            var largeSmaDataPoints = BigSma.SmaDataPoints;
            var smallSmaDataPoints = SmallSma.SmaDataPoints;


            var smaDiff =
                from i in
                Enumerable.Range(0, Math.Max(largeSmaDataPoints.Count, smallSmaDataPoints.Count))
                select new DataPoint
                {
                    diff = smallSmaDataPoints.ElementAtOrDefault(i) - largeSmaDataPoints.ElementAtOrDefault(i),
                    ActualPrice = MovingAverage.SharedRawExchangeData.ElementAt(i * COMMON_INTERVAL).Close,
                    dt = MovingAverage.SharedRawExchangeData.ElementAt(i * COMMON_INTERVAL).Time
                };

            //var bigSmaOfMacd = smaDiff.Select(d=>d.diff).ToList().SMA(L_SIGNAL_LEN);
            //var smallSmaOfMacd = smaDiff.Select(d=>d.diff).ToList().SMA(S_SIGNAL_LEN);

            Console.WriteLine("Calculating buy / sell actions...");
            var res = Utilities.Getcrossings(smaDiff);
            Console.WriteLine("done");



            

            if (res.First().Action == "buy")
            {
                res.RemoveAt(res.IndexOf(res.First()));
            }

            if (res.Last().Action == "sell")
            {
                res.RemoveAt(res.IndexOf(res.Last()));
            }

            //Console.WriteLine("Time\t\t\taction\t\tActualPrice ");
            //foreach (var cross in res)
            //{
            //    Console.WriteLine(cross.dt.ToString() + "\t" + cross.Action + "\t\t" + cross.CrossingPrice.ToString());
            //}

            const int AMOUNT = 50;
            decimal pl = 0;
            const decimal FEE = 0.003m;
            decimal totalFee = 0.0m;

            Console.WriteLine("Time\t\t\taction\t\tPrice\t\tFee\t\tPL\t\tBalance");

            var plList = new List<decimal>();

            var curPl = 0.0m;


            for (int i = res.Count()-1 ; i >= 0; i--)
            {
                var cross = res[i];
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

            //foreach (var cross in res)
            //{
            //    var curFee = (AMOUNT * cross.CrossingPrice) * FEE;

            //    totalFee += curFee;
            //    if (cross.Action == "buy")
            //    {
            //        pl = pl - (AMOUNT * cross.CrossingPrice) - curFee;
            //        Console.WriteLine(cross.dt.ToString() + "\t"
            //            + cross.Action + "\t\t"
            //            + cross.CrossingPrice.ToString() + "\t\t\t"
            //            + curFee.ToString() + "\t\t"
            //            + pl.ToString());
            //    }

            //    if (cross.Action == "sell")
            //    {
            //        pl = pl + (AMOUNT * cross.CrossingPrice) - curFee;
            //        Console.WriteLine(cross.dt.ToString() + "\t"
            //            + cross.Action + "\t\t"
            //            + cross.CrossingPrice.ToString() + "\t\t\t"
            //            + curFee.ToString() + "\t\t"
            //            + pl.ToString());
            //    }

            //}

            Console.WriteLine("Total Trades: " + res.Count());
            Console.WriteLine("Profit/Loss: " + plList.Sum().ToString());
            Console.WriteLine("Total Fees: " + totalFee.ToString());
            Console.WriteLine("Biggest Profit: " + plList.Max().ToString());
            Console.WriteLine("Biggest Profit: " + plList.Min().ToString());

            //Console.WriteLine("Time\t\t\tSmaDiff\t\t\tActualPrice ");
            //for (int i = 0; i < 50; i++)
            //{
            //    var msg = smaDiff.ElementAt(i).dt.ToString() + "\t" + smaDiff.ElementAt(i).diff.ToString() + "\t" + smaDiff.ElementAt(i).ActualPrice.ToString();
            //    Console.WriteLine(msg);
            //}

            //Console.WriteLine(smaDiff.First().smaDiff + " : " + smaDiff.First().ActualPrice );




        }


    }



    public class DataPoint
    {
        public double diff { get; set; }
        public decimal ActualPrice { get; set; }
        public DateTime dt { get; set; }
    }



}
