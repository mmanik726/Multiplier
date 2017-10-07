﻿using CoinbaseExchange.NET.Core;
using CoinbaseExchange.NET.Data;
using CoinbaseExchange.NET.Endpoints.MyOrders;
using CoinbaseExchange.NET.Endpoints.PublicData;
using CoinbaseExchange.NET.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiplier
{



    class Manager
    {

        public string ProductName;

        private string MyPassphrase { get; set; }
        private string MyKey { get; set; }
        private string MySecret { get; set; }

        private decimal CurrentRealtimePrice { get; set; }

        private decimal CurrentBufferedPrice { get; set; }

        private decimal CurrentAveragePrice { get; set; }
        private decimal MaxBuy { get; set; }
        private decimal MaxSell { get; set; }

        private decimal BuySellAmount { get; set; }

        private bool BuyOrderFilled { get; set; }
        private bool SellOrderFilled { get; set; }

        private bool WaitingSellFill { get; set; }
        private bool WaitingBuyFill { get; set; }

        private bool WaitingBuyOrSell { get; set; }

        private int SmaSlices { get; set; }
        private int SmaTimeInterval { get; set; }


        private bool StartBuyingSelling { get; set; }

        private decimal PriceBuffer { get; set; }

        private DateTime LastTickerUpdateTime { get; set; }
        private DateTime LastBuySellTime { get; set; }

        private DateTime LastCrossTime { get; set; }

        private double WaitTimeAfterCrossInMin { get; set; }

        private MovingAverage Sma { get; set; }


        private CBAuthenticationContainer MyAuth { get; set; }
        private MyOrderBook MyOrderBook { get; set; }
        


        public EventHandler TickerPriceUpdateEvent;
        public EventHandler OrderFilledEvent;
        public EventHandler SmaUpdateEvent;
        public EventHandler BuySellBufferChangedEvent;
        public EventHandler BuySellAmountChangedEvent;
        public EventHandler<SmaParamUpdateArgs> SmaParametersUpdatedEvent;
        public EventHandler AutoTradingStartedEvent;

        public EventHandler TickerConnectedEvent;
        public EventHandler TickerDisConnectedEvent;

        public Manager(string inputProductName, string PassPhrase, string Key, string Secret)
        {

            ProductName = inputProductName;
            MyPassphrase = PassPhrase;
            MyKey = Key;
            MySecret = Secret;
            //InitializeManager();


        }

        public async void InitializeManager()
        {
            await Task.Factory.StartNew(() => InitManager(ProductName));
        }


        public void StartTrading_BySelling()
        {

            //logic

            LastBuySellTime = DateTime.UtcNow.ToLocalTime();
            LastCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * WaitTimeAfterCrossInMin);

            BuyOrderFilled = true;
            SellOrderFilled = false;

            MaxBuy = 0;
            MaxSell = 5;

            StartBuyingSelling = true;

            Logger.WriteLog("Auto trading started: waiting to sell");

            AutoTradingStartedEvent?.Invoke(this, EventArgs.Empty);

        }
        public void StartTrading_ByBuying()
        {
            //logic

            LastBuySellTime = DateTime.UtcNow.ToLocalTime();
            LastCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * WaitTimeAfterCrossInMin);


            BuyOrderFilled = false;
            SellOrderFilled = true;

            MaxBuy = 5;
            MaxSell = 0;

            StartBuyingSelling = true;

            Logger.WriteLog("Auto trading started: waiting to buy");

            AutoTradingStartedEvent?.Invoke(this, EventArgs.Empty);
        }


        private void InitManager(string ProductName)
        {


            //try to get ticker first
            TickerClient ProductTickerClient;

            try
            {
                Logger.WriteLog("Initializing ticker");
                ProductTickerClient = new TickerClient(ProductName);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Manager cant initialize ticker");
                return;
            }



            BuySellAmount = 0.01m;//default

            SmaTimeInterval = 3; //default
            SmaSlices = 40; //default

            PriceBuffer = 0.05m; //default

            CurrentRealtimePrice = 0;
            CurrentBufferedPrice = 0;
            CurrentAveragePrice = 0;

            BuyOrderFilled = false;
            SellOrderFilled = false;

            WaitingSellFill = false;
            WaitingBuyFill = false;
            WaitingBuyOrSell = false;

            WaitTimeAfterCrossInMin = SmaTimeInterval; //buy sell every time interval

            MaxBuy = 5;
            MaxSell = 5;

            StartBuyingSelling = false;

            LastCrossTime = DateTime.UtcNow.ToLocalTime();

            


            

            ProductTickerClient.PriceUpdated += ProductTickerClient_UpdateHandler;

            ////update ui with initial pice
            ProductTickerClient_UpdateHandler(this, new TickerMessage(ProductTickerClient.CurrentPrice));

            ProductTickerClient.TickerConnectedEvent += tickerConnectedHandler;
            ProductTickerClient.TickerDisconnectedEvent += tickerDisconnectedHandler;

            MyAuth = new CBAuthenticationContainer(MyKey, MyPassphrase, MySecret);
            MyOrderBook = new MyOrderBook(MyAuth, ProductName, ref ProductTickerClient);
            MyOrderBook.OrderUpdateEvent += FillUpdateEventHandler;

            Sma = new MovingAverage(ref ProductTickerClient, ProductName, SmaTimeInterval, SmaSlices);
            Sma.MovingAverageUpdated += SmaChangeEventHandler;

            Logger.WriteLog(string.Format("{0} manager started", ProductName));

        }

        private void tickerDisconnectedHandler(object sender, EventArgs e)
        {
            Logger.WriteLog("Ticker disconnected... pausing all buy / sell");

            StartBuyingSelling = false;
            TickerDisConnectedEvent?.Invoke(this, EventArgs.Empty);

        }

        private void tickerConnectedHandler(object sender, EventArgs args)
        {
            Logger.WriteLog("Ticker connected... resuming buy sell");

            
            Task.Run(()=> UpdateSmaParameters(SmaTimeInterval, SmaSlices, true)).Wait();
            System.Threading.Thread.Sleep(2 * 1000);
            StartBuyingSelling = true;

            TickerConnectedEvent?.Invoke(this, EventArgs.Empty);
        }


        private void FillUpdateEventHandler(object sender, EventArgs args)
        {

            var filledOrder = ((OrderUpdateEventArgs)args);

            Logger.WriteLog(string.Format("{0} order of {1} {2} ({3}) filled @{4}", filledOrder.side, filledOrder.fillSize, filledOrder.ProductName , filledOrder.OrderId, filledOrder.filledAtPrice));

            //MessageBox.Show(string.Format("{0} order ({1}) filled @{2} ", filledOrder.side, filledOrder.OrderId, filledOrder.filledAtPrice));

            if (filledOrder.side == "buy")
            {
                BuyOrderFilled = true;
                SellOrderFilled = false;

                WaitingBuyFill = false;
                WaitingSellFill = true;
            }
            else if (filledOrder.side == "sell")
            {
                SellOrderFilled = true;
                BuyOrderFilled = false;

                WaitingSellFill = false;
                WaitingBuyFill = true;
            }

            WaitingBuyOrSell = false;


            OrderFilledEvent?.Invoke(this, filledOrder);


            //this.Dispatcher.Invoke(() => updateListView(filledOrder));
        }


        private void ProductTickerClient_UpdateHandler(object sender, EventArgs args)
        {
            var tickerData = (TickerMessage)args;


            LastTickerUpdateTime = DateTime.UtcNow.ToLocalTime();

            CurrentRealtimePrice = tickerData.RealTimePrice;



            TickerPriceUpdateEvent?.Invoke(this, tickerData);


            if (StartBuyingSelling)
            {
                if ((LastTickerUpdateTime - LastBuySellTime).TotalMilliseconds < 1000)
                {
                    //Logger.WriteLog("price skipped: " + CurrentRealtimePrice);
                    return;
                }

                CurrentBufferedPrice = CurrentRealtimePrice;

                var secSinceLastCrosss = (DateTime.UtcNow.ToLocalTime() - LastCrossTime).TotalSeconds;
                if (secSinceLastCrosss < (WaitTimeAfterCrossInMin * 60))
                {
                    //if the last time prices crossed is < 2 min do nothing
                    //Logger.WriteLog(string.Format("Waiting {0} sec before placing any new order", Math.Round((sharedWaitTimeAfterCrossInMin * 60) - secSinceLastCrosss)));
                    return;
                }


                if (!WaitingBuyOrSell)
                {
                    LastBuySellTime = DateTime.UtcNow.ToLocalTime();

                    buysell();
                }
                else
                {
                    Logger.WriteLog("Buy/sell already in progress");
                }
            }


        }

        private async void buysell()
        {


            if (WaitingBuyOrSell)
            {
                Logger.WriteLog("this should never print");
                return;
            }

            decimal curPriceDiff = CurrentBufferedPrice - CurrentAveragePrice;

            //if (curPriceDiff <= priceBuffer) //below average: sell
            if (CurrentBufferedPrice <= (CurrentAveragePrice - PriceBuffer))
            {
                if (!SellOrderFilled) //if not already sold
                {



                    setNextActionTo_Buy();

                    try
                    {
                        await MyOrderBook.PlaceNewLimitOrder("sell", ProductName, BuySellAmount.ToString(), CurrentBufferedPrice.ToString(), true);
                        Logger.WriteLog(string.Format("Order placed, Waiting {0} min before placing any new order", WaitTimeAfterCrossInMin));
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("Error selling: \n" + ex.Message + "\n"
                            + ex.InnerException.Message + "\n"
                            + ex.InnerException.InnerException.Message);


                        Logger.WriteLog("Retrying to sell now...");
                        setNextActionTo_Sell();


                        WaitingBuyFill = false;
                        WaitingBuyOrSell = false; //set wait flag to false to place new order

                        //simulate last cross time so sells immidiately instead of waiting. since error occured
                        LastCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes((-1 * WaitTimeAfterCrossInMin) - 1);
                    }


                }

            }


            //if (curPriceDiff >= priceBuffer) //above average: buy
            if (CurrentBufferedPrice >= (CurrentAveragePrice + PriceBuffer))
            {
                if (!BuyOrderFilled) //if not already bought
                {


                    setNextActionTo_Sell();

                    try
                    {
                        await MyOrderBook.PlaceNewLimitOrder("buy", ProductName, BuySellAmount.ToString(), CurrentBufferedPrice.ToString(), true);
                        Logger.WriteLog(string.Format("Order placed, waiting {0} min before placing any new order", WaitTimeAfterCrossInMin));
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("Error buying: \n" + ex.Message + "\n" 
                            + ex.InnerException.Message + "\n"
                            + ex.InnerException.InnerException.Message);

                        Logger.WriteLog("Retrying to buy now...");
                        setNextActionTo_Buy();

                        WaitingBuyFill = false; 
                        WaitingBuyOrSell = false; //set wait flag to false to place new order

                        //simulate last cross time so buys immididtely instead of waiting. since error occured
                        LastCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes ((-1 * WaitTimeAfterCrossInMin) - 1);

                    }


                }

            }

        }



        private void setNextActionTo_Sell()
        {
            SellOrderFilled = false;
            BuyOrderFilled = true;

            //MaxSell = 5;
            //MaxBuy = 0;

            WaitingBuyFill = true;
            WaitingBuyOrSell = true;

            LastCrossTime = DateTime.UtcNow.ToLocalTime();
        }

        private void setNextActionTo_Buy()
        {
            SellOrderFilled = true;
            BuyOrderFilled = false;

            //MaxSell = 0;
            //MaxBuy = 5;

            WaitingSellFill = true;
            WaitingBuyOrSell = true;

            LastCrossTime = DateTime.UtcNow.ToLocalTime();

        }


        public void UpdateSmaParameters(int InputTimerInterval, int InputSlices, bool forceRedownload = false)
        {

            try
            {
                Sma.updateValues(InputTimerInterval, InputSlices, forceRedownload);
                SmaSlices = InputSlices;
                SmaTimeInterval = InputTimerInterval;
                WaitTimeAfterCrossInMin = SmaTimeInterval;

                SmaParametersUpdatedEvent?.Invoke(this, new SmaParamUpdateArgs { NewTimeinterval = SmaTimeInterval, NewSlices = SmaSlices });

                var msg = string.Format("New SMA values, Time interval: {0} Slices: {1}", InputTimerInterval.ToString(), InputSlices.ToString());
                Logger.WriteLog(msg);

            }
            catch (Exception ex)
            {
                Logger.WriteLog("Error occured while updating SMA parameters: " + ex.Message);
            }

        }


        private void SmaChangeEventHandler(object sender, EventArgs args)
        {

            var currentSmaData = (MAUpdateEventArgs)args;
            decimal newSmaPrice = currentSmaData.CurrentMAPrice;

            CurrentAveragePrice = newSmaPrice;

            Logger.WriteLog(string.Format("SMA updated: {0}", newSmaPrice));

            SmaUpdateEvent?.Invoke(this, currentSmaData);
        }


        public void UpdateBuySellBuffer(decimal newPriceBuffer)
        {

            PriceBuffer = Math.Round(newPriceBuffer, 4);

            var msg = "Buy Sell Price buffer updated to " + PriceBuffer.ToString();
            Logger.WriteLog(msg);

            BuySellBufferChangedEvent?.Invoke(this, new BuySellBufferUpdateArgs { NewBuySellBuffer = PriceBuffer });

            //Dispatcher.Invoke(() => lblBuySellBuffer.Content = sharedPriceBuffer.ToString());

        }


        public void UpdateBuySellAmount(decimal amount)
        {
            if (amount <= 0)
            {
                Logger.WriteLog("buy sell amount cannot be less than or equal to 0");
                return;
            }

            BuySellAmount = amount;


            var msg = "Buy sell amount changed to: " + BuySellAmount.ToString();
            Logger.WriteLog(msg);

            BuySellAmountChangedEvent?.Invoke(this, new BuySellAmountUpdateArgs { NewBuySellAmount = BuySellAmount });
        }

    }

    public class BuySellBufferUpdateArgs : EventArgs { public decimal NewBuySellBuffer { get; set; } }
    public class BuySellAmountUpdateArgs : EventArgs { public decimal NewBuySellAmount { get; set; } }

    public class SmaParamUpdateArgs : EventArgs
    {
        public int NewTimeinterval { get; set; }
        public decimal NewSlices { get; set; }
    }
}









