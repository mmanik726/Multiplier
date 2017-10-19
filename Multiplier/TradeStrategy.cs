using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoinbaseExchange.NET.Utilities;

namespace Multiplier
{
    public abstract class TradeStrategyBase
    {
        internal ContextValues CurrentValues;

        public EventHandler CurrentActionChangedEvent;

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
            CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime();
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
            CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime();

        }


        public virtual void StartTrading_BySelling()
        {

            //logic

            CurrentValues.LastBuySellTime = DateTime.UtcNow.ToLocalTime();

            CurrentValues.LastLargeSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin);
            CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin);

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
            CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes(-1 * CurrentValues.WaitTimeAfterLargeSmaCrossInMin);


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


        public async void CancelCurrentTradeAction(string action = "ALL")
        {
            //for each of the items in active order list cancel order

            CurrentValues.StartAutoTrading = false;

            await Task.Run(() => 
            {
                for (int i = 0; i < CurrentValues.MyOrderBook.MyChaseOrderList.Count; i++)
                {
                    //cancel each item

                    if (action == "BUY" || CurrentValues.MyOrderBook.MyChaseOrderList.ElementAt(i).Side != "buy")
                        continue;

                    if (action == "SELL" || CurrentValues.MyOrderBook.MyChaseOrderList.ElementAt(i).Side != "sell")
                        continue;

                    var curOrder = CurrentValues.MyOrderBook.MyChaseOrderList.ElementAt(i).OrderId;

                    try
                    {
                        var cancelledOrder = CurrentValues.MyOrderBook.CancelSingleOrder(curOrder).Result;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("Error cancelling order id " + curOrder + ex.InnerException.Message);
                    }

                    CurrentValues.MyOrderBook.RemoveFromOrderList(curOrder);

                }
            });

            CurrentValues.StartAutoTrading = true;

        }







        internal virtual async void Buy()
        {
            CurrentValues.WaitingBuyOrSell = true;
            CurrentValues.WaitingSellFill = false;
            CurrentValues.WaitingBuyFill = true;

            SetNextActionTo_Sell();

            try
            {
                var buyResult = await CurrentValues.MyOrderBook.PlaceNewOrder("buy", CurrentValues.ProductName, CurrentValues.BuySellAmount.ToString(), CurrentValues.CurrentBufferedPrice.ToString(), true);

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
                CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes((-1 * CurrentValues.WaitTimeAfterSmallSmaCrossInMin) - 1);
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
                var sellResult = await CurrentValues.MyOrderBook.PlaceNewOrder("sell", CurrentValues.ProductName, CurrentValues.BuySellAmount.ToString(), CurrentValues.CurrentBufferedPrice.ToString(), true);

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
                CurrentValues.LastSmallSmaCrossTime = DateTime.UtcNow.ToLocalTime().AddMinutes((-1 * CurrentValues.WaitTimeAfterSmallSmaCrossInMin) - 1);

            }
        }



    }



    class TradeStrategyA : TradeStrategyBase
    {



        public TradeStrategyA(ref ContextValues inputContextValues) : base(ref inputContextValues)
        {
            //CurrentValues = inputContextValues;
            SetCurrentAction("NOT_SET");
        }


        public override async void Trade()
        {

                decimal curPriceDiff = CurrentValues.CurrentBufferedPrice - CurrentValues.CurrentLargeSmaPrice;

            //if (curPriceDiff <= priceBuffer) //below average: sell
            if (CurrentValues.CurrentBufferedPrice <= (CurrentValues.CurrentLargeSmaPrice - CurrentValues.PriceBuffer))
            {

                if (!CurrentValues.SellOrderFilled) //if not already sold
                {
                    if (!CurrentValues.WaitingBuyOrSell)
                    {
                        CurrentValues.WaitingBuyOrSell = true;
                        Sell();
                    }

                }

            }


            //if (curPriceDiff >= priceBuffer) //above average: buy
            if (CurrentValues.CurrentBufferedPrice >= (CurrentValues.CurrentLargeSmaPrice + CurrentValues.PriceBuffer))
            {
                if (!CurrentValues.BuyOrderFilled) //if not already bought
                {
                    if (!CurrentValues.WaitingBuyOrSell)
                    {
                        CurrentValues.WaitingBuyOrSell = true;
                        Buy();
                    }
                }

            }


        }

        public void StopAll()
        {
            throw new NotImplementedException();
        }
    }


    class TradeStrategyB : TradeStrategyBase
    {
        public TradeStrategyB(ref ContextValues inputContextValues) : base(ref inputContextValues)
        {
            SetCurrentAction("NOT_SET");
        }


        public override async void Trade()
        {

            // smallSma -> sSma, lSma
            //if bufferedPrice <= (sSma - priceBUffer)


            var sSma = CurrentValues.CurrentSmallSmaPrice;
            var lSma = CurrentValues.CurrentLargeSmaPrice;
            var curPrice = CurrentValues.CurrentBufferedPrice;
            var priceBufferSmall = CurrentValues.PriceBuffer;
            var priceBufferLarge = CurrentValues.PriceBuffer;


            //sell  
            if (curPrice <= (sSma - priceBufferSmall) || curPrice <= (lSma - priceBufferLarge)) //if price <= small sma or large sma
            {
                if (!CurrentValues.SellOrderFilled) //if not already sold
                {
                    if (!CurrentValues.WaitingBuyOrSell)
                    {
                        CurrentValues.WaitingBuyOrSell = true;
                        Sell();
                    }

                }

            }



            if (sSma <= (lSma + priceBufferSmall)) //if small sma higher than the large sma value
            {
                if (curPrice > (lSma + priceBufferLarge)) // and if the current price is higher than the 
                {

                    if (!CurrentValues.BuyOrderFilled) //if not already bought
                    {
                        if (!CurrentValues.WaitingBuyOrSell)
                        {
                            CurrentValues.WaitingBuyOrSell = true;
                            Buy();
                        }
                    }
                }
            }
            else //small sma is above large sma 
            {
                if (curPrice > (sSma + priceBufferSmall)) // and if current price is > small sma
                {

                    if (!CurrentValues.BuyOrderFilled && (sSma > lSma)) //and buyorder not filled and small sma is greater than large sma
                    {
                        if (!CurrentValues.WaitingBuyOrSell)
                        {
                            CurrentValues.WaitingBuyOrSell = true;
                            Buy();
                        }
                    }
                }
            }

        }


        public void StopAll()
        {
            throw new NotImplementedException();
        }
    }

}

