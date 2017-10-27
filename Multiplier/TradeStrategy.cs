using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoinbaseExchange.NET.Utilities;

using CoinbaseExchange.NET.Data;

namespace Multiplier
{
    public abstract class TradeStrategyBase
    {
        internal ContextValues CurrentValues;

        public EventHandler CurrentActionChangedEvent;



        //temporary
        public virtual void updateSmallestSma(int interval, int slices) { }


        public TradeStrategyBase(ref ContextValues inputContextValues)
        {
            CurrentValues = inputContextValues;
        }

        public virtual void Trade() { }
        //void StopAll();

        internal virtual void SetNextActionTo_Sell()
        {
            CurrentValues.SellOrderFilled = false;
            CurrentValues.BuyOrderFilled = true;

            //MaxSell = 5;
            //MaxBuy = 0;

            CurrentValues.WaitingBuyFill = true;
            CurrentValues.WaitingBuyOrSell = true;

            SetCurrentAction("SELL");

            CurrentValues.LastLargeSmaCrossTime = DateTime.UtcNow.ToLocalTime();
            //CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime();
        }

        internal virtual void SetCurrentAction(string curAction)
        {
            CurrentValues.CurrentAction = curAction;

            CurrentActionChangedEvent?.Invoke(this, new ActionChangedArgs(curAction));
        }

        internal virtual void SetNextActionTo_Buy()
        {
            CurrentValues.SellOrderFilled = true;
            CurrentValues.BuyOrderFilled = false;

            //MaxSell = 0;
            //MaxBuy = 5;

            CurrentValues.WaitingSellFill = true;
            CurrentValues.WaitingBuyOrSell = true;

            SetCurrentAction("BUY");

            CurrentValues.LastLargeSmaCrossTime = DateTime.UtcNow.ToLocalTime();
            //CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime();

        }


        public virtual void StartTrading_BySelling()
        {

            //logic

            CurrentValues.LastBuySellTime = DateTime.UtcNow.ToLocalTime();

            CurrentValues.LastLargeSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin);
            //CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin);

            CurrentValues.BuyOrderFilled = true;
            CurrentValues.SellOrderFilled = false;

            CurrentValues.MaxBuy = 0;
            CurrentValues.MaxSell = 5;

            CurrentValues.StartAutoTrading = true;

            Logger.WriteLog("Auto trading started: waiting to sell");

            CurrentValues.UserStartedTrading = true;

            SetCurrentAction("SELL");


            //AutoTradingStartedEvent?.Invoke(this, EventArgs.Empty);

        }

        public virtual void StartTrading_ByBuying()
        {
            //logic

            CurrentValues.LastBuySellTime = DateTime.UtcNow.ToLocalTime();
            CurrentValues.LastLargeSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin);
            //CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin);


            CurrentValues.BuyOrderFilled = false;
            CurrentValues.SellOrderFilled = true;

            CurrentValues.MaxBuy = 5;
            CurrentValues.MaxSell = 0;

            CurrentValues.StartAutoTrading = true;

            Logger.WriteLog("Auto trading started: waiting to buy");

            CurrentValues.UserStartedTrading = true;

            SetCurrentAction("BUY");

            //AutoTradingStartedEvent?.Invoke(this, EventArgs.Empty);
        }


        public async Task<bool> CancelCurrentTradeAction(string action = "ALL")
        {
            //for each of the items in active order list cancel order

            CurrentValues.StartAutoTrading = false;

            await Task.Run(() =>
            {
                cancelActiveOrders(action);
            }).ContinueWith((t) => t.Wait());

            
            //var a = cancelActiveOrders(action);
            //a.Wait();
            


            //CurrentValues.StartAutoTrading = true;
            return true;
        }


        private async Task<bool> cancelActiveOrders(string action = "ALL")
        {
            for (int i = 0; i < CurrentValues.MyOrderBook.MyChaseOrderList.Count; i++)
            {
                //cancel each item

                if (action == "BUY" && CurrentValues.MyOrderBook.MyChaseOrderList.ElementAt(i).Side != "buy")
                    continue;

                if (action == "SELL" && CurrentValues.MyOrderBook.MyChaseOrderList.ElementAt(i).Side != "sell")
                    continue;

                
                var curOrderId = CurrentValues.MyOrderBook.MyChaseOrderList.ElementAt(i).OrderId;
                var curOrder = CurrentValues.MyOrderBook.MyChaseOrderList.ElementAt(i);

                try
                {
                    Logger.WriteLog(string.Format("trying to cancel {0} order ({1}) of {2} {3} @{4} ",
                        curOrder.Side, curOrder.OrderId, curOrder.ProductSize, curOrder.Productname, curOrder.UsdPrice));
                    var cancelledOrder = CurrentValues.MyOrderBook.CancelSingleOrder(curOrderId).Result;
                    if (cancelledOrder.Count() > 0)
                    {
                        Logger.WriteLog(string.Format("order {0} has been manually cancelled", curOrderId));
                        CurrentValues.MyOrderBook.RemoveFromOrderList(curOrderId);
                    }
                    else
                    {
                        Logger.WriteLog(string.Format("Order {0} could not be cancelled ", curOrderId));
                    }

                }
                catch (Exception ex)
                {

                    Logger.WriteLog("Error cancelling order id " + curOrderId + " " + ex.InnerException?.Message);
                }

                Logger.WriteLog(string.Format("removing order {0} from watch list", curOrderId)); 

                //CurrentValues.MyOrderBook.RemoveFromOrderList(curOrderId); 
            }

            return true;
        }




        internal virtual async void Buy()
        {
            CurrentValues.WaitingBuyOrSell = true;
            CurrentValues.WaitingSellFill = false;
            CurrentValues.WaitingBuyFill = true;

            SetNextActionTo_Sell();

            try
            {
                //limit order
                var buyResult = await CurrentValues.MyOrderBook.PlaceNewOrder("buy", CurrentValues.ProductName, CurrentValues.BuySellAmount.ToString(), CurrentValues.CurrentBufferedPrice.ToString(), true);

                //market order
                //var buyResult = await CurrentValues.MyOrderBook.PlaceNewOrder("buy", CurrentValues.ProductName, CurrentValues.BuySellAmount.ToString(), CurrentValues.CurrentBufferedPrice.ToString(), true, "market");

                //wait for the buy operation
                if (buyResult == null)
                    Logger.WriteLog("buy result is null");

                //test//await MyOrderBook.PlaceNewOrder("buy", ProductName, BuySellAmount.ToString(), (CurrentBufferedPrice - 10.00m).ToString(), true);
                Logger.WriteLog(string.Format("Order placed, waiting {0} min before placing any new order", CurrentValues.WaitTimeAfterLargeSmaCrossInMin));
            }
            catch (Exception ex)
            {
                var msg = ex.Message + "\n";
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    msg = msg + ex.Message;
                }

                Logger.WriteLog("Error buying: \n" + msg);
                Logger.WriteLog("Retrying to buy on next price tick...");
                SetNextActionTo_Buy();

                CurrentValues.WaitingBuyFill = false;
                CurrentValues.WaitingBuyOrSell = false; //set wait flag to false to place new order

                //simulate last cross time so buys immididtely instead of waiting. since error occured
                CurrentValues.LastLargeSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes((-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin) - 1);
                //CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes((-1 * CurrentValues.WaitTimeAfterSmallSmaCrossInMin) - 1);
            }

        }

        internal virtual async void Sell()
        {
            CurrentValues.WaitingBuyOrSell = true;
            CurrentValues.WaitingSellFill = true;
            CurrentValues.WaitingBuyFill = false;

            SetNextActionTo_Buy();

            try
            {
                //limit order
                var sellResult = await CurrentValues.MyOrderBook.PlaceNewOrder("sell", CurrentValues.ProductName, CurrentValues.BuySellAmount.ToString(), CurrentValues.CurrentBufferedPrice.ToString(), true);
                
                //market order 
                //var sellResult = await CurrentValues.MyOrderBook.PlaceNewOrder("sell", CurrentValues.ProductName, CurrentValues.BuySellAmount.ToString(), CurrentValues.CurrentBufferedPrice.ToString(), true, "market");

                //wait for the sell result
                if (sellResult == null)
                    Logger.WriteLog("Sell result is null");

                //test//await MyOrderBook.PlaceNewOrder("sell", ProductName, BuySellAmount.ToString(), (CurrentBufferedPrice + 10.00m).ToString(), true);
                Logger.WriteLog(string.Format("Order placed, Waiting {0} min before placing any new order", CurrentValues.WaitTimeAfterLargeSmaCrossInMin));
            }
            catch (Exception ex)
            {
                var msg = ex.Message + "\n";
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    msg = msg + ex.Message;
                }

                Logger.WriteLog("Error Selling: \n" + msg);
                Logger.WriteLog("Retrying to sell on next price tick...");
                SetNextActionTo_Sell();


                CurrentValues.WaitingBuyFill = false;
                CurrentValues.WaitingBuyOrSell = false; //set wait flag to false to place new order

                //simulate last cross time so sells immidiately instead of waiting. since error occured
                CurrentValues.LastLargeSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes((-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin) - 1);
                //CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes((-1 * CurrentValues.WaitTimeAfterSmallSmaCrossInMin) - 1);

            }
        }



    }



    //////class TradeStrategyA : TradeStrategyBase
    //////{



    //////    public TradeStrategyA(ref ContextValues inputContextValues) : base(ref inputContextValues)
    //////    {
    //////        //CurrentValues = inputContextValues;
    //////        SetCurrentAction("NOT_SET");
    //////    }


    //////    public override async void Trade()
    //////    {

    //////        decimal curPriceDiff = CurrentValues.CurrentBufferedPrice - CurrentValues.CurrentLargeSmaPrice;

    //////        //if (curPriceDiff <= priceBuffer) //below average: sell
    //////        if (CurrentValues.CurrentBufferedPrice <= (CurrentValues.CurrentLargeSmaPrice - CurrentValues.PriceBuffer))
    //////        {

    //////            if (!CurrentValues.SellOrderFilled) //if not already sold
    //////            {
    //////                if (!CurrentValues.WaitingBuyOrSell)
    //////                {
    //////                    CurrentValues.WaitingBuyOrSell = true;
    //////                    Sell();
    //////                }

    //////            }

    //////        }


    //////        //if (curPriceDiff >= priceBuffer) //above average: buy
    //////        if (CurrentValues.CurrentBufferedPrice >= (CurrentValues.CurrentLargeSmaPrice + CurrentValues.PriceBuffer))
    //////        {
    //////            if (!CurrentValues.BuyOrderFilled) //if not already bought
    //////            {
    //////                if (!CurrentValues.WaitingBuyOrSell)
    //////                {
    //////                    CurrentValues.WaitingBuyOrSell = true;
    //////                    Buy();
    //////                }
    //////            }

    //////        }


    //////    }

    //////    public void StopAll()
    //////    {
    //////        throw new NotImplementedException();
    //////    }
    //////}


    //////class TradeStrategyC : TradeStrategyBase
    //////{
    //////    public TradeStrategyC(ref ContextValues inputContextValues) : base(ref inputContextValues)
    //////    {
    //////        SetCurrentAction("NOT_SET");
    //////    }

    //////    public override async void Trade()
    //////    {
    //////        var sSma = CurrentValues.CurrentSmallSmaPrice;
    //////        var lSma = CurrentValues.CurrentLargeSmaPrice;
    //////        var curPrice = CurrentValues.CurrentBufferedPrice;
    //////        var priceBufferSmall = CurrentValues.PriceBuffer;
    //////        var priceBufferLarge = CurrentValues.PriceBuffer;


    //////        //sell  
    //////        if (sSma < (lSma - priceBufferLarge)) //if small sma < large sma
    //////        {
    //////            if (!CurrentValues.SellOrderFilled) //if not already sold
    //////            {
    //////                if (!CurrentValues.WaitingBuyOrSell)
    //////                {
    //////                    CurrentValues.WaitingBuyOrSell = true;
    //////                    Sell();
    //////                }
    //////            }
    //////        }



    //////        //buy  
    //////        if (sSma > (lSma + priceBufferLarge)) //if small sma > large sma 
    //////        {
    //////            if (!CurrentValues.BuyOrderFilled) //if not already bought
    //////            {
    //////                if (!CurrentValues.WaitingBuyOrSell)
    //////                {
    //////                    CurrentValues.WaitingBuyOrSell = true;
    //////                    Buy();
    //////                }
    //////            }
    //////        }

    //////    }

    //////}


    //////class TradeStrategyB : TradeStrategyBase
    //////{
    //////    private bool secondBuyAllowed; 

    //////    public TradeStrategyB(ref ContextValues inputContextValues) : base(ref inputContextValues)
    //////    {
    //////        SetCurrentAction("NOT_SET");
    //////        secondBuyAllowed = false;
    //////    }


    //////    public override async void Trade()
    //////    {

    //////        // smallSma -> sSma, lSma
    //////        //if bufferedPrice <= (sSma - priceBUffer)


    //////        var sSma = CurrentValues.CurrentSmallSmaPrice;
    //////        var lSma = CurrentValues.CurrentLargeSmaPrice;
    //////        var curPrice = CurrentValues.CurrentBufferedPrice;
    //////        var priceBufferSmall = CurrentValues.PriceBuffer;
    //////        var priceBufferLarge = CurrentValues.PriceBuffer;



    //////        if (curPrice > lSma)
    //////        {
    //////            if ((sSma < lSma)) //buy only when the small sma is below the large sma: indicates price going up
    //////            {

    //////                if (!CurrentValues.BuyOrderFilled) //if not already bought
    //////                {
    //////                    if (!CurrentValues.WaitingBuyOrSell)
    //////                    {
    //////                        CurrentValues.WaitingBuyOrSell = true;
    //////                        Buy();
    //////                    }
    //////                }

    //////            }

    //////            secondBuyAllowed = true; //allow for buying second time after initial sale if price keeps going up
    //////        }
    //////        else
    //////        {
    //////            secondBuyAllowed = false; //if price is below the large sma second buy is not allowed
    //////        }


    //////        if (curPrice > sSma && secondBuyAllowed == true) //buying the second time after the first sale based onn small sma
    //////        {
    //////            if (!CurrentValues.BuyOrderFilled) //if not already bought
    //////            {
    //////                if (!CurrentValues.WaitingBuyOrSell)
    //////                {
    //////                    CurrentValues.WaitingBuyOrSell = true;
    //////                    Buy();
    //////                }
    //////            }

    //////        }


    //////        if (curPrice < sSma) //if price is < the small sma
    //////        {

    //////            if (!CurrentValues.SellOrderFilled) //if not already sold
    //////            {
    //////                if (!CurrentValues.WaitingBuyOrSell)
    //////                {
    //////                    CurrentValues.WaitingBuyOrSell = true;
    //////                    Sell();
    //////                }
    //////            }
    //////        }


    //////    }


    //////}


    ////////a derivate of strategy B
    //////class TradeStrategyD : TradeStrategyBase
    //////{
    //////    private bool secondLevelBuyAllowed;
    //////    private bool thirdLevelBuyAllowed; 
    //////    private MovingAverage smallestSmaMa;
    //////    //private 
    //////    private decimal smallestSmaPrice;

    //////    public TradeStrategyD(ref ContextValues inputContextValues) : base(ref inputContextValues)
    //////    {
    //////        SetCurrentAction("NOT_SET");
    //////        secondLevelBuyAllowed = true; //assume time to buy, will get set to false 
    //////        thirdLevelBuyAllowed = false;

    //////        var smallestSmaInterval = inputContextValues.CurrentSmallSmaTimeInterval;
    //////        var smallestSmaSlices = Math.Round((inputContextValues.CurrentSmallSmaSlices / 2.0m), 0);

    //////        smallestSmaPrice = 0;

    //////        smallestSmaMa = new MovingAverage(ref inputContextValues.CurrentTicker, inputContextValues.ProductName,
    //////            smallestSmaInterval, Convert.ToInt32(smallestSmaSlices));

    //////        smallestSmaMa.MovingAverageUpdated += SmallestSmaUpdateHandler;


    //////    }


    //////    public override async void Trade()
    //////    {

    //////        // smallSma -> sSma, lSma
    //////        //if bufferedPrice <= (sSma - priceBUffer)


    //////        var mediumSmaPrice = CurrentValues.CurrentSmallSmaPrice;
    //////        var largestSmaPrice = CurrentValues.CurrentLargeSmaPrice;
    //////        var curPrice = CurrentValues.CurrentBufferedPrice;
    //////        var priceBufferSmall = CurrentValues.PriceBuffer;
    //////        var priceBufferLarge = CurrentValues.PriceBuffer;



    //////        ////set var
    //////        //if (curPrice > smallestSmaPrice)
    //////        //    thirdLevelBuyAllowed = true;
    //////        //else
    //////        //    thirdLevelBuyAllowed = false;


    //////        ////set vars
    //////        //if (curPrice > largestSmaPrice)
    //////        //    secondLevelBuyAllowed = true; //allow for buying second time after initial sale if price keeps going up
    //////        //else
    //////        //    secondLevelBuyAllowed = false; //if price is below the large sma second buy is not allowed



    //////        //if (curPrice >= largestSmaPrice)
    //////        //{
    //////        //    if ((mediumSmaPrice <= largestSmaPrice)) //buy only when the small sma is below the large sma: indicates price going up
    //////        //    {
    //////        //        if (!CurrentValues.BuyOrderFilled) //if not already bought
    //////        //        {
    //////        //            if (!CurrentValues.WaitingBuyOrSell)
    //////        //            {
    //////        //                CurrentValues.WaitingBuyOrSell = true;

    //////        //                Logger.WriteLog("Buying based on:\nCurPrice > Largest Sma\n\t Medium sma <= Largest Sma");

    //////        //                Buy();
    //////        //                //secondLevelBuyAllowed = true;
    //////        //            }
    //////        //        }

    //////        //    }
    //////        //}



    //////        //if (curPrice >= largestSmaPrice)
    //////        //{
    //////        //    if (curPrice >= mediumSmaPrice) //buying the second time after the first sale based on small sma
    //////        //    {
    //////        //        if (smallestSmaPrice <= mediumSmaPrice) //buy if the smallest sma is less than the 'medium' sma
    //////        //        {
    //////        //            if (!CurrentValues.BuyOrderFilled) //if not already bought
    //////        //            {
    //////        //                if (!CurrentValues.WaitingBuyOrSell)
    //////        //                {
    //////        //                    CurrentValues.WaitingBuyOrSell = true;

    //////        //                    Logger.WriteLog("Buying based on:\nCurPrice > Largest Sma\n\tCur Price >= Medimum sma \n\t\tSmalles sma <= medium Sma");

    //////        //                    Buy();
    //////        //                }
    //////        //            }
    //////        //        }
    //////        //    }
    //////        //}




    //////        if (curPrice >= largestSmaPrice)
    //////        {
    //////            if (smallestSmaPrice <= largestSmaPrice && mediumSmaPrice <= largestSmaPrice) //buying the second time after the first sale based on small sma
    //////            {
    //////                if (!CurrentValues.BuyOrderFilled) //if not already bought
    //////                {
    //////                    if (!CurrentValues.WaitingBuyOrSell)
    //////                    {
    //////                        CurrentValues.WaitingBuyOrSell = true;

    //////                        Logger.WriteLog("Buying based on OPTION1:\nCurPrice >= Largest Sma\n\tsmallestSmaPrice <= largestSmaPrice AND mediumSmaPrice <= largestSmaPrice");

    //////                        Buy();
    //////                    }
    //////                }
    //////            }
    //////        }


    //////        //special case occurs only when price is going up up and away :)
    //////        if (smallestSmaPrice > mediumSmaPrice)
    //////        {
    //////            if (mediumSmaPrice > largestSmaPrice) //buying the second time after the first sale based on small sma
    //////            {
    //////                if (curPrice >= smallestSmaPrice) //buy if the smallest sma is less than the 'medium' sma
    //////                {
    //////                    if (!CurrentValues.BuyOrderFilled) //if not already bought
    //////                    {
    //////                        if (!CurrentValues.WaitingBuyOrSell)
    //////                        {
    //////                            CurrentValues.WaitingBuyOrSell = true;

    //////                            Logger.WriteLog("Buying based on OPTION2:\nSmallest Sma > Medium Sma\n\tMedium Sma > Largest Sma\n\t\t Cur Price > smallest sma");
    //////                            Buy();
    //////                        }
    //////                    }
    //////                }
    //////            }
    //////        }



    //////        var smallestToMedGap = Math.Abs(smallestSmaPrice - mediumSmaPrice);
    //////        var threshold = smallestSmaPrice - (smallestToMedGap / 3);
    //////        if (curPrice <= mediumSmaPrice || curPrice <= threshold)  //if price is < the smallest sma or medium sma
    //////        {
    //////            if (!CurrentValues.SellOrderFilled) //if not already sold
    //////            {
    //////                if (!CurrentValues.WaitingBuyOrSell)
    //////                {
    //////                    CurrentValues.WaitingBuyOrSell = true;

    //////                    Logger.WriteLog("Selling based on:\nCur price <= Medium Sma OR threshold (less than smalles sma)\n\tMedium Sma > Largest Sma");
    //////                    Sell();
    //////                }
    //////            }
    //////        }


    //////    }


    //////    private void SmallestSmaUpdateHandler(object sender, EventArgs args)
    //////    {
    //////        var currentSmaData = (MAUpdateEventArgs)args;
    //////        decimal newSmaPrice = currentSmaData.CurrentMAPrice;

    //////        smallestSmaPrice = newSmaPrice;

    //////        Int32 curSmallesSmaSlices = currentSmaData.CurrentSlices;// InputSlices;
    //////        Int32 curSmallestSmaInterval = currentSmaData.CurrentTimeInterval; // InputTimerInterval;
    //////        //CurContextValues.WaitTimeAfterLargeSmaCrossInMin = CurContextValues.CurrentLargeSmaTimeInterval;
            
    //////        var msg = string.Format("Smallest SMA updated: {0} (Time interval: {1} Slices: {2})", newSmaPrice,
    //////            curSmallestSmaInterval, curSmallesSmaSlices);
    //////        Logger.WriteLog(msg);


    //////        //SmaLargeUpdateEvent?.Invoke(this, currentSmaData);
    //////    }

    //////    private void MediumSmaUpdatedHandler(object sender, EventArgs args)
    //////    {

    //////    }


    //////    //temporary solution
    //////    public override async void updateSmallestSma(int interval, int slices)
    //////    {
    //////        await smallestSmaMa.updateValues(interval, slices);
    //////    }

    //////}


    class TradeStrategyE : TradeStrategyBase
    {

        private decimal LastBuyAtPrice;
        private decimal LastSellAtPrice;

        SmaValues BigIntervalSmaValues; 

        SmaValues SmallIntervalSmaValues;

        public TradeStrategyE(ref ContextValues inputContextValues) : base(ref inputContextValues)
        {
            SetCurrentAction("NOT_SET");
            LastBuyAtPrice = 0;
            LastSellAtPrice = 0;

            BigIntervalSmaValues = new SmaValues(ref inputContextValues, 3, 60, 55, 28);
            SmallIntervalSmaValues = new SmaValues(ref inputContextValues, 1, 60, 55, 28);

        }



        public override async void Trade()
        {
            if (CurrentValues.WaitingBuyOrSell)
                return;


            BigIntervalSmaValues.DetermineBuySell();

            SmallIntervalSmaValues.DetermineBuySell();


            var curPrice = CurrentValues.CurrentBufferedPrice;

            if (!CurrentValues.BuyOrderFilled) // not bought yet
            {
                if (SmallIntervalSmaValues.Buy && BigIntervalSmaValues.Buy)
                {
                    TradeUsing(BigIntervalSmaValues);
                }

            }



            if (!CurrentValues.SellOrderFilled) // not sold yet
            {
                
                var priceDiffFromBuyTime = Math.Abs(curPrice - LastBuyAtPrice);
                if (priceDiffFromBuyTime == curPrice)
                    priceDiffFromBuyTime = 0;
                var priceDiffPercent = Math.Round((priceDiffFromBuyTime / curPrice) * 100, 4);

                if (priceDiffPercent > 20)
                {
                    TradeUsing(SmallIntervalSmaValues);
                }
                else
                {
                    TradeUsing(BigIntervalSmaValues);
                }

            }


        }


        private void TradeUsing(SmaValues inputSmaValues)
        {

            var curPrice = CurrentValues.CurrentBufferedPrice;

            if (inputSmaValues.Buy == true)
            {
                if (!CurrentValues.BuyOrderFilled) //if not already bought
                {
                    if (!CurrentValues.WaitingBuyOrSell)
                    {
                        CurrentValues.WaitingBuyOrSell = true;
                        Logger.WriteLog(inputSmaValues.BuyReason);
                        LastBuyAtPrice = curPrice;
                        Buy();
                    }
                }
            }


            if (inputSmaValues.Sell == true)
            {
                if (!CurrentValues.SellOrderFilled) //if not already sold
                {
                    if (!CurrentValues.WaitingBuyOrSell)
                    {
                        CurrentValues.WaitingBuyOrSell = true;

                        Logger.WriteLog(inputSmaValues.SellReason);
                        LastSellAtPrice = curPrice;
                        Sell();
                    }
                }
            }

        }


        class SmaValues
        {
            public decimal smallestSmaPrice;
            public decimal mediumSmaPrice;
            public decimal largestSmaPrice;

            public bool Sell;
            public bool Buy;

            public string BuyReason;
            public string SellReason;


            MovingAverage LargestMa;
            MovingAverage MediumMa;
            MovingAverage SmallestMa;

            ContextValues myContextValues;

            public SmaValues(ref ContextValues inputContextValues, 
                int CommonIntervalMin = 5, 
                int LargestSmaSlice = 60, 
                int MediumSmaSlice = 55, 
                int SmallestSmaSlice = 28)
            {

                myContextValues = inputContextValues;

                //int commonLargeIntervalMin = 5;
                smallestSmaPrice = 0;
                mediumSmaPrice = 0;
                largestSmaPrice = 0;

                BuyReason = "";
                SellReason = "";

                LargestMa = new MovingAverage(ref inputContextValues.CurrentTicker, inputContextValues.ProductName, CommonIntervalMin, LargestSmaSlice);
                MediumMa = new MovingAverage(ref inputContextValues.CurrentTicker, inputContextValues.ProductName, CommonIntervalMin, MediumSmaSlice);
                SmallestMa = new MovingAverage(ref inputContextValues.CurrentTicker, inputContextValues.ProductName, CommonIntervalMin, SmallestSmaSlice);

                LargestMa.MovingAverageUpdated += LargestSmaUpdatedHandler;
                MediumMa.MovingAverageUpdated += MediumSmaUpdatedHandler;
                SmallestMa.MovingAverageUpdated += SmallestSmaUpdateHandler;


                largestSmaPrice = LargestMa.CurrentSMAPrice;
                mediumSmaPrice = MediumMa.CurrentSMAPrice;
                smallestSmaPrice = SmallestMa.CurrentSMAPrice;

                Buy = false;
                Sell = false;


                //DetermineBuySell();
            }

            private void SmallestSmaUpdateHandler(object sender, EventArgs args)
            {
                var currentSmaData = (MAUpdateEventArgs)args;
                decimal newSmaPrice = currentSmaData.CurrentMAPrice;

                smallestSmaPrice = newSmaPrice;

                Logger.WriteLog(string.Format("Smallest SMA updated: {0} (Time interval: {1} Slices: {2})",
                    newSmaPrice, currentSmaData.CurrentTimeInterval, currentSmaData.CurrentSlices));
            }


            private void MediumSmaUpdatedHandler(object sender, EventArgs args)
            {
                var currentSmaData = (MAUpdateEventArgs)args;
                decimal newSmaPrice = currentSmaData.CurrentMAPrice;

                mediumSmaPrice = newSmaPrice;

                Logger.WriteLog(string.Format("Medium SMA updated: {0} (Time interval: {1} Slices: {2})",
                    newSmaPrice, currentSmaData.CurrentTimeInterval, currentSmaData.CurrentSlices));
            }


            private void LargestSmaUpdatedHandler(object sender, EventArgs args)
            {
                var currentSmaData = (MAUpdateEventArgs)args;
                decimal newSmaPrice = currentSmaData.CurrentMAPrice;

                largestSmaPrice = newSmaPrice;

                Logger.WriteLog(string.Format("Largest SMA updated: {0} (Time interval: {1} Slices: {2})",
                    newSmaPrice, currentSmaData.CurrentTimeInterval, currentSmaData.CurrentSlices));
            }

            internal void DetermineBuySell()
            {

                ResetBuySellFlags();

                var curPrice = myContextValues.CurrentBufferedPrice;


                if (curPrice >= largestSmaPrice)
                {
                    if (smallestSmaPrice <= largestSmaPrice && mediumSmaPrice <= largestSmaPrice) //buying the second time after the first sale based on small sma
                    {
                        if (!myContextValues.BuyOrderFilled) //if not already bought
                        {
                            BuyReason = "Buying based on OPTION1:\nCurPrice >= Largest Sma\n\tsmallestSmaPrice <= largestSmaPrice AND mediumSmaPrice <= largestSmaPrice";
                            SellReason = "";
                            SetBuyTrue();
                        }
                    }
                }


                //special case occurs only when price is going up up and away :)
                if (smallestSmaPrice > mediumSmaPrice)
                {
                    if (mediumSmaPrice > largestSmaPrice) //buying the second time after the first sale based on small sma
                    {
                        if (curPrice >= smallestSmaPrice) //buy if the smallest sma is less than the 'medium' sma
                        {
                            if (!myContextValues.BuyOrderFilled) //if not already bought
                            {
                                BuyReason = "Buying based on OPTION2:\nSmallest Sma > Medium Sma\n\tMedium Sma > Largest Sma\n\t\t Cur Price > smallest sma";
                                SellReason = "";
                                SetBuyTrue();
                            }
                        }
                    }
                }



                var smallestToMedGap = Math.Abs(smallestSmaPrice - mediumSmaPrice);
                var threshold = smallestSmaPrice - (smallestToMedGap / 10);
                if (curPrice <= mediumSmaPrice || curPrice <= threshold)  //if price is < the smallest sma or medium sma
                {
                    if (!myContextValues.SellOrderFilled)//if not already bought
                    {
                        SellReason = "Selling based on:\nCur price <= Medium Sma OR threshold (less than smalles sma)\n\tMedium Sma > Largest Sma";
                        BuyReason = "";
                        SetSellTrue();
                    }
                }

            }

            private void SetBuyTrue()
            {
                Buy = true;
                Sell = false;
            }


            private void SetSellTrue()
            {
                Buy = false;
                Sell = true;
            }

            private void ResetBuySellFlags()
            {
                Buy = false;
                Sell = false;
            }

        }

    }


}

