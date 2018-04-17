using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using CoinbaseExchange.NET.Data;
using CoinbaseExchange.NET.Utilities;

using Newtonsoft.Json.Linq;


namespace Multiplier
{
    internal class MacdStrategy : TradeStrategyBase //TradeStrategyE
    {

        //Macd myMacdStrategy_Large;
        Macd myMacdStrategy_Small;

        decimal percent_for_using_smallmacd;

        public MacdStrategy(ref ContextValues inputContextValues, IntervalValues intervalValues) : base(ref inputContextValues)
        {
            //myMacdStrategy_Large = new Macd(ref inputContextValues, "macd_large");

            myMacdStrategy_Small = new Macd(ref inputContextValues, "macd_small");

            var genSettings = AppSettings.GetGeneralSettings();

            percent_for_using_smallmacd = genSettings.price_inc_percent_for_using_smallmacd;

        }



        public override void Trade()
        {
            if (CurrentValues.WaitingBuyOrSell)
                return;


            var curPrice = CurrentValues.CurrentBufferedPrice;

            //var percentIncreaseSinceLastBuy = ((curPrice - myMacdStrategy_Large.LastBuyAtPrice) / myMacdStrategy_Large.LastBuyAtPrice) * 100;

            //if (percentIncreaseSinceLastBuy > percent_for_using_smallmacd)
            //{
            //    TradeUsing(myMacdStrategy_Small);
            //}
            //else
            //{
            //    TradeUsing(myMacdStrategy_Large);
            //}

            TradeUsing(myMacdStrategy_Small);


        }

        private void TradeUsing(Macd strategy)
        {
            if (!CurrentValues.BuyOrderFilled) // not bought yet
            {
                if (strategy.Buy)
                {
                    bool alreadyBought = AppSettings.GetGeneralSettings().already_bought;

                    if (alreadyBought)
                    {
                        Logger.WriteLog("Already bought detected in settings, setting next actino to Sell");
                        SetNextActionTo_Sell();

                        //simulate buy complete 
                        CurrentValues.WaitingBuyFill = false;
                        CurrentValues.WaitingBuyOrSell = false;
                        AppSettings.SaveUpdateGeneralSetting("already_bought", false.ToString());

                    }
                    else
                    {
                        Buy();
                    }

                    
                }

            }

            if (!CurrentValues.SellOrderFilled) // not sold yet
            {
                if (strategy.Sell)
                {

                    bool alreadySold = AppSettings.GetGeneralSettings().already_sold;

                    if (alreadySold)
                    {
                        Logger.WriteLog("Already sold detected in settings, setting next actino to Buy");
                        SetNextActionTo_Buy();

                        //simulate buy complete 
                        CurrentValues.WaitingSellFill = false;
                        CurrentValues.WaitingBuyOrSell = false;
                        AppSettings.SaveUpdateGeneralSetting("already_sold", false.ToString());

                    }
                    else
                    {
                        Sell();
                    }

                }

            }
        }



        public override void Dispose()
        {

            Logger.WriteLog("dipose method in macdStrategy class ");
            //throw new NotImplementedException();

            //dispose the existing timers, new ones get created in constructor
            //myMacdStrategy_Large?.Dispose();
            myMacdStrategy_Small?.Dispose();

        }


    }






    class Macd : IDisposable
    {
        MovingAverage SmallSma;
        MovingAverage BigSma;

        //only one instance per class object
        //public System.Timers.Timer aTimer;

        public System.Timers.Timer StopLossTimer;

        private int CommonINTERVAL; 
        private int updateInterval;

        public bool Buy;
        public bool Sell;

        public decimal LastBuyAtPrice;
        public decimal LastSellAtPrice;

        public bool StopLossInEffect;

        //private JArray settings;
        private StrategySettings settings;
        private ContextValues contextVals;

        private string _StategyName;

        private double stopLossCounter;

        public Macd(ref ContextValues inputContextValues, string macdStrategyName)
        {
            Buy = false;
            Sell = false;

            stopLossCounter = 0;

            StopLossInEffect = false;

            _StategyName = macdStrategyName;

            settings = AppSettings.GetStrategySettings2(_StategyName);

            

            if (settings == null)
            {
                throw new Exception("CantInitSettingsError");
            }

            Logger.WriteLog("macd settings found: ");
            AppSettings.PrintStrategySetting( settings.StrategyName );


            LastBuyAtPrice = settings.last_buy_price; //Convert.ToDecimal(settings[0]["last_buy_price"].ToString());
            LastSellAtPrice = settings.last_sell_price; //Convert.ToDecimal(settings[0]["last_sell_price"].ToString());

            CommonINTERVAL = settings.time_interval;//30;//30; //min
            int largeSmaLENGTH = settings.slow_sma;//100;
            int smallSmaLENGTH = settings.fast_sma;//15;
            updateInterval = CommonINTERVAL; //update values every 1 min to keep sma data updated

            BigSma = new MovingAverage(ref inputContextValues.CurrentTicker, inputContextValues.ProductName, CommonINTERVAL, largeSmaLENGTH);
            SmallSma = new MovingAverage(ref inputContextValues.CurrentTicker, inputContextValues.ProductName, CommonINTERVAL, smallSmaLENGTH);


            contextVals = inputContextValues;

            //if (aTimer != null) //timer already in place
            //{
            //    aTimer.Elapsed -= UpdateMacdValues;
            //    aTimer.Stop();
            //    aTimer = null;
            //}


            //aTimer = new System.Timers.Timer();
            //aTimer.Elapsed += UpdateMacdValues;
            //aTimer.Interval = updateInterval * 60 * 1000;
            //aTimer.Enabled = true;
            //aTimer.Start();



            BigSma.MovingAverageUpdatedEvent += UpdateMacdValues;


            UpdateMacdValues(this, null);





            
            if (StopLossTimer != null) //timer already in place
            {
                StopLossTimer.Elapsed -= DetermineStopLoss;
                StopLossTimer.Stop();
                StopLossTimer = null;
            }
            StopLossTimer = new System.Timers.Timer();
            StopLossTimer.Elapsed += DetermineStopLoss;
            StopLossTimer.Interval = 1 * 60 * 1000; //every minute check the stop loss condition
            StopLossTimer.Enabled = true;
            StopLossTimer.Start();


            DetermineStopLoss(this, null);

        }


        private void DetermineStopLoss(object sender, ElapsedEventArgs e)
        {
            settings = AppSettings.GetStrategySettings2(_StategyName); //AppSettings.GetStrategySettings("macd");
            LastBuyAtPrice = settings.last_buy_price; //Convert.ToDecimal(settings[0]["last_buy_price"].ToString());
            LastSellAtPrice = settings.last_sell_price; //Convert.ToDecimal(settings[0]["last_sell_price"].ToString());

            if (LastBuyAtPrice == 0)
                return;



            var stopLossPercent = settings.stop_loss_percent; //Convert.ToDecimal(settings[0]["stop_loss_percent"].ToString());

            var curPrice = contextVals.CurrentBufferedPrice;


            var curDiff = Math.Round(curPrice - LastBuyAtPrice, 4);


            var diffPercentage = Math.Round((curDiff / LastBuyAtPrice) * 100, 4);

            if (stopLossCounter % 3 == 0 || stopLossCounter == 0)
            {
                var msg = string.Format(_StategyName +  ": Price change since last buy: {0}-{1} = {2} ({3}%)",
                    Math.Round(curPrice, 4).ToString(), Math.Round(LastBuyAtPrice, 4).ToString(), curDiff.ToString(), diffPercentage.ToString());
                Logger.WriteLog(msg);
            }

            stopLossCounter += 1;

            if (diffPercentage <= Math.Abs(stopLossPercent) * -1)
            {
                StopLossInEffect = true;

                if (stopLossCounter % 2 == 0 || stopLossCounter == 0)
                {
                    Logger.WriteLog("Stop Loss in effect");
                }

                    Sell = true;
                Buy = false;
            }
            else
            {
                //Sell = false;
                //Buy = false;
                StopLossInEffect = false;
            }


        }



        private void UpdateMacdValues(object sender, EventArgs e)
        {
            Logger.WriteLog(String.Format("Udating {0} strategy values", _StategyName));


            //if (StopLossInEffect)
            //{
            //    Logger.WriteLog("Stop loss in effect");
            //    return;
            //}

            var largeSmaDataPoints = BigSma.SmaDataPoints;
            var smallSmaDataPoints = SmallSma.SmaDataPoints;

            
            

            //var emaTest = MovingAverage.SharedRawExchangeData.Select((d) => (double)d.Close).ToList().EMA(500);
            //var fst = emaTest.First();
            //var lst = emaTest.Last();

            var smaDiff =
                from i in
                Enumerable.Range(0, Math.Max(largeSmaDataPoints.Count, smallSmaDataPoints.Count))
                select smallSmaDataPoints.ElementAtOrDefault(i) - largeSmaDataPoints.ElementAtOrDefault(i);


            var curSmaDiff = Math.Round(smaDiff.First(), 4);

            settings = AppSettings.GetStrategySettings2(_StategyName);

            var largeSignal = settings.signal; //Convert.ToInt16(settings[0]["signal"].ToString());
            var smallSignal = settings.my_sma; //Convert.ToInt16(settings[0]["my_sma"].ToString());

            var L_SIGNAL_LEN = largeSignal;//14;
            var S_SIGNAL_LEN = smallSignal;//5;

            var bigSmaOfMacd = Math.Round(smaDiff.ToList().SMA(L_SIGNAL_LEN).First(), 4);
            var smallSmaOfMacd = Math.Round(smaDiff.ToList().SMA(S_SIGNAL_LEN).First(), 4);

            //todo:
            //Logger.WriteLog("current large ema " + MovingAverage.SharedRawExchangeData.Select((d) => (double)d.Close).ToList().EMA(100).First());
            //Logger.WriteLog("current small ema " + MovingAverage.SharedRawExchangeData.Select((d) => (double)d.Close).ToList().EMA(15).First()); //largeSmaDataPoints.EMA(15));

            Logger.WriteLog(string.Format("macd values: \n" +
                "\tcurent small sma: {0}\n" +
                "\tcurent large sma: {1}\n" +
                "\tsmall - large = macd: {2}\n" +
                "\tbig sma of macd: {3}\n" +
                "\tsmall sma of macd: {4}\n", Math.Round(smallSmaDataPoints.First(), 4), Math.Round(largeSmaDataPoints.First(), 4), curSmaDiff, bigSmaOfMacd, smallSmaOfMacd));


            var useBothSma = settings.use_two_sma; //Convert.ToBoolean(settings[0]["use_two_sma"].ToString());

            if (IsValuesToClose(curSmaDiff, smallSmaOfMacd))
            {
                Logger.WriteLog("values to close to each other, skipping buy / sell. \n" +
                    "macd: " + curSmaDiff.ToString() + " smallSmaMacd: " + smallSmaOfMacd.ToString());
                return;
            }

            if (useBothSma)
            {
                if (curSmaDiff > bigSmaOfMacd && curSmaDiff > smallSmaOfMacd)
                {

                    //if stop loss in effect -> no need to indicate to buy since stop loss already sold and graph suggests buy 
                    if (StopLossInEffect)
                    {
                        Logger.WriteLog("Stop Loss in effect (BUY = True), cant buy again now");
                        return;
                    }

                    //buy condition 
                    Logger.WriteLog("BUY = true");
                    Buy = true;
                    Sell = false;
                }
                else
                {

                    //if stop loss in effect (and graph saying sell)-> then turn it off and signal to sell if not already sold
                    if (StopLossInEffect)
                    {
                        StopLossInEffect = false;
                        //set last buy price (value will be overridden in buy func) to 0
                        //this stops dterimeStopLoss to return false and
                        //so that stop loss does not get set to on again
                        //AppSettings.SaveUpdateStrategySetting("macd", "last_buy_price", "0.01");

                        //update all strategies with last buy price to 0.01
                        var allStrategies = AppSettings.GetAllStrategies();
                        foreach (var s in allStrategies)
                        {
                            AppSettings.SaveUpdateStrategySetting(s.StrategyName, "last_buy_price", "0.01");
                        }

                        Logger.WriteLog("Stop Loss in effect (SELL = True), resetting stoploss flag to false.");

                        //reload the settings to get 0.00 for last_buy_price
                        //which disables stopLoss flag
                        AppSettings.Reloadsettings();
                        return;
                    }

                    //sell
                    Logger.WriteLog("Sell = true");
                    Sell = true;
                    Buy = false;

                }
            }
            else
            {
                if (curSmaDiff > smallSmaOfMacd)
                {

                    //if stop loss in effect -> no need to indicate to buy since stop loss already sold and graph suggests buy 
                    if (StopLossInEffect)
                    {
                        Logger.WriteLog("Stop Loss in effect, cant buy again now");
                        return;
                    }

                    //buy condition 
                    Buy = true;
                    Sell = false;
                }
                else
                {

                    //if stop loss in effect (and graph saying sell)-> then turn it off and signal to sell if not already sold
                    if (StopLossInEffect)
                    {
                        StopLossInEffect = false;
                        //set last buy price (value will be overridden in buy func) to 0
                        //this stops dterimeStopLoss to return false and
                        //so that stop loss does not get set to on again
                        //AppSettings.SaveUpdateStrategySetting("macd", "last_buy_price", "0.00");
                        var allStrategies = AppSettings.GetAllStrategies();
                        foreach (var s in allStrategies)
                        {
                            AppSettings.SaveUpdateStrategySetting(s.StrategyName, "last_buy_price", "0.01");
                        }
                        Logger.WriteLog("Stop Loss in effect, resetting stoploss flag to false.");

                        //reload the settings to get 0.00 for last_buy_price
                        //which disables stopLoss flag
                        AppSettings.Reloadsettings();
                        return;
                    }

                    //sell
                    Sell = true;
                    Buy = false;

                }
            }




            //throw new NotImplementedException();
        }


        private bool IsValuesToClose(double macd, double smallSmaMacd)
        {

            macd = Math.Round(macd, 2);
            smallSmaMacd = Math.Round(smallSmaMacd, 2);
            //largeSmaMacd = Math.Round(largeSmaMacd, 2);

            if (macd == smallSmaMacd)
            {
                return true;
            }

            //double difference = 0.00;

            //if (Math.Abs(smallSmaMacd) == 0)
            //{
            //    smallSmaMacd = 0.01;
            //}

            const double THRESHOLD = 0.03;

            //difference = Math.Abs((Math.Abs(macd * macd) - Math.Abs(smallSmaMacd * smallSmaMacd)) / Math.Abs(smallSmaMacd * smallSmaMacd)) * 100;
            double dist = Math.Abs(Math.Abs(macd) - Math.Abs(smallSmaMacd));



            if (dist <= THRESHOLD)
            {
                return true;
            }
            else
            {
                return false;
            }


        }


        public void Dispose()
        {


            if (StopLossTimer != null) //timer already in place
            {
                StopLossTimer.Elapsed -= DetermineStopLoss;
                StopLossTimer.Stop();
                StopLossTimer = null;
            }

            //if (aTimer != null) //timer already in place
            //{
            //    aTimer.Elapsed -= UpdateMacdValues;
            //    aTimer.Stop();
            //    aTimer = null;
            //}

            //throw new NotImplementedException();

        }
    }

}

