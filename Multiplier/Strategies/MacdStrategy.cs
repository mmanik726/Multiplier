using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using CoinbaseExchange.NET.Data;
using CoinbaseExchange.NET.Utilities;
namespace Multiplier
{
    internal class MacdStrategy : TradeStrategyBase //TradeStrategyE
    {

        Macd myMacdStrategy;

        public MacdStrategy(ref ContextValues inputContextValues, IntervalValues intervalValues) : base(ref inputContextValues)
        {
            myMacdStrategy = new Macd(ref inputContextValues);
        }


        public override void Trade()
        {
            if (CurrentValues.WaitingBuyOrSell)
                return;


            var curPrice = CurrentValues.CurrentBufferedPrice;

            if (!CurrentValues.BuyOrderFilled) // not bought yet
            {
                //if (SmallIntervalSmaValues.Buy && BigIntervalSmaValues.Buy)
                if (myMacdStrategy.Buy)
                {

                    Buy();

                    ////////simulation
                    //////CurrentValues.WaitingBuyOrSell = true;
                    //////CurrentValues.WaitingSellFill = false;
                    //////CurrentValues.WaitingBuyFill = true;
                    //////SetNextActionTo_Sell();
                    //////Logger.WriteLog("\t\tsimulating Buying at " + curPrice);
                    //////CurrentValues.WaitingBuyOrSell = false;



                }

            }

            if (!CurrentValues.SellOrderFilled) // not sold yet
            {
                if (myMacdStrategy.Sell)
                {
                    Sell();

                    ////////simulation
                    //////CurrentValues.WaitingBuyOrSell = true;
                    //////CurrentValues.WaitingSellFill = true;
                    //////CurrentValues.WaitingBuyFill = false;
                    //////SetNextActionTo_Buy();
                    //////Logger.WriteLog("\t\tsimulating Selling at " + curPrice);
                    //////CurrentValues.WaitingBuyOrSell = false;
                }

            }
        }

    }


    class Macd
    {
        MovingAverage SmallSma;
        MovingAverage BigSma;

        public System.Timers.Timer aTimer;

        private int CommonINTERVAL; 
        private int updateInterval;

        public bool Buy;
        public bool Sell;

        public Macd(ref ContextValues inputContextValues)
        {
            Buy = false;
            Sell = false;

            CommonINTERVAL = 30;//30; //min
            int largeSmaLENGTH = 100;
            int smallSmaLENGTH = 15;
            updateInterval = CommonINTERVAL; //update values every 1 min to keep sma data updated

            BigSma = new MovingAverage(ref inputContextValues.CurrentTicker, inputContextValues.ProductName, CommonINTERVAL, largeSmaLENGTH);
            SmallSma = new MovingAverage(ref inputContextValues.CurrentTicker, inputContextValues.ProductName, CommonINTERVAL, smallSmaLENGTH);



            //// Create a thread
            //System.Threading.Thread newWindowThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            //{
            //    // Create and show the Window
            //    GraphWindow v = new GraphWindow();
            //    v.Show();
            //    v.ShowData(MovingAverage.SharedRawExchangeData.Select((d) => (double)d.Close).Take(1000).ToList());
            //    // Start the Dispatcher Processing
            //    System.Windows.Threading.Dispatcher.Run();
                
            //}));
            //// Set the apartment state
            //newWindowThread.SetApartmentState(System.Threading.ApartmentState.STA);
            //// Make the thread a background thread
            //newWindowThread.IsBackground = true;
            //// Start the thread
            //newWindowThread.Start();



            if (aTimer != null) //timer already in place
            {
                aTimer.Elapsed -= UpdateSMA;
                aTimer.Stop();
                aTimer = null;
            }


            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += UpdateSMA;
            aTimer.Interval = updateInterval * 60 * 1000;
            aTimer.Enabled = true;
            aTimer.Start();

            UpdateSMA(this, null);

        }

        private void UpdateSMA(object sender, ElapsedEventArgs e)
        {
            Logger.WriteLog("Udating MACD strategy values");

            var largeSmaDataPoints = BigSma.SmaDataPoints;
            var smallSmaDataPoints = SmallSma.SmaDataPoints;


            //var emaTest = MovingAverage.SharedRawExchangeData.Select((d) => (double)d.Close).ToList().EMA(500);
            //var fst = emaTest.First();
            //var lst = emaTest.Last();

            var smaDiff =
                from i in
                Enumerable.Range(0, Math.Max(largeSmaDataPoints.Count, smallSmaDataPoints.Count))
                select smallSmaDataPoints.ElementAtOrDefault(i) - largeSmaDataPoints.ElementAtOrDefault(i);


            var curSmaDiff = smaDiff.First();

            var L_SIGNAL_LEN = 14;
            var S_SIGNAL_LEN = 5;

            var bigSmaOfMacd =  smaDiff.ToList().SMA(L_SIGNAL_LEN).First();
            var smallSmaOfMacd = smaDiff.ToList().SMA(S_SIGNAL_LEN).First();


            Logger.WriteLog(string.Format("macd values: \n" + 
                "\tcurent small sma: {0}\n" +
                "\tcurent large sma: {1}\n" +
                "\tsmall - large = macd: {2}\n" +
                "\tbig sma of macd: {3}\n" +
                "\tsmall sma of macd: {4}\n", smallSmaDataPoints.First(), largeSmaDataPoints.First(), curSmaDiff, bigSmaOfMacd, smallSmaOfMacd));


            if (curSmaDiff > bigSmaOfMacd )
            {
                //buy condition 
                Buy = true;
                Sell = false;
            }
            else
            {
                //sell
                Sell = true;
                Buy = false;

            }




            //throw new NotImplementedException();
        }
    }

}

