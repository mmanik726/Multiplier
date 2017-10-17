using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CoinbaseExchange.NET.Endpoints.MyOrders;
using System.Diagnostics;
using CoinbaseExchange.NET.Utilities;
using System.Threading;
namespace CoinbaseExchange.NET.Endpoints.Fills
{


    public class Fill
    {
        public string TradeId { get; set; }
        public string ProductId { get; set; }
        public string Price { get; set; }
        public string Size { get; set; }
        public string OrderId { get; set; }
        public DateTime Time { get; set; }
        public string Fee { get; set; }
        public bool Settled { get; set; }
        public string Side { get; set; }
        public string Liquidity { get; set; }
        public Fill(JToken jToken)
        {
            this.TradeId = jToken["trade_id"].Value<string>();
            this.ProductId = jToken["product_id"].Value<string>();
            this.Price = jToken["price"].Value<string>();
            this.Size = jToken["size"].Value<string>();
            this.OrderId = jToken["order_id"].Value<string>();
            this.Time = jToken["created_at"].Value<DateTime>();
            this.Fee = jToken["fee"].Value<string>();
            this.Settled = jToken["settled"].Value<bool>();
            this.Side = jToken["side"].Value<string>();
            this.Liquidity = jToken["liquidity"].Value<string>();

        }
    }


    public class GetFillsRequest : ExchangePageableRequestBase
    {
        public GetFillsRequest(string endpoint) : base("GET")
        {
            this.RequestUrl = endpoint;
        }
    }

    public class FillResponse : ExchangePageableResponseBase
    {
        public List<Fill> Fills { get; private set; }

        public FillResponse(ExchangeResponse response) : base(response)
        {
            if (!response.IsSuccessStatusCode)
                throw new Exception("FillRequestError");

            var json = response.ContentBody;
            var jArray = JArray.Parse(json);
            Fills = jArray.Select(elem => new Fill(elem)).ToList();
        }
    }

    public class FillEventArgs : EventArgs
    {
        public Fill filledOrder;
        
    }


    public class FillsClient : ExchangeClientBase
    {
        private static object _watchListLock = new object();
        public EventHandler FillUpdated;
        public List<MyOrder> FillWatchList { get; set; }

        private MyOrderBook myActiveOrderBook; 

        private static bool IsBusy_TrackIngOrder;

        public bool BusyCheckingOrder; 

        public FillsClient(CBAuthenticationContainer authenticationContainer, MyOrderBook orderBook) : base(authenticationContainer)
        {
            myActiveOrderBook = orderBook;
            FillWatchList = orderBook.MyChaseOrderList ;
            IsBusy_TrackIngOrder = false;
            BusyCheckingOrder = false;
            //startTracker();
        }

        public async void startTracker()
        {
            //await Task.Factory.StartNew(() => FillTracker(onFillUpdate));
            await Task.Run(() => TrackOrder());

        }


        public async Task<FillResponse> GetFills()
        {
            var endpoint = String.Format("/fills");
            var request = new GetFillsRequest(endpoint);

            ExchangeResponse response = null;
            try
            {
                response = await this.GetResponse(request);
            }
            catch (Exception)
            {
                throw new Exception("GetFillsError");
            }

            var accountHistoryResponse = new FillResponse(response);
            return accountHistoryResponse;
        }

        public async Task<FillResponse> GetFillStatus(string orderId)
        {
            var endpoint = string.Format(@"/fills?order_id={0}", orderId);
            var request = new GetFillsRequest(endpoint);


            ExchangeResponse response = null;
            try
            {
                response = await this.GetResponse(request);
            }
            catch (Exception)
            {
                throw new Exception("GetFillsStatusError");
            }


            var orderStats = new FillResponse(response);

            return orderStats;
        }

        private void orderFilledEvent(List<Fill> FillList)
        {
            var fillDetails = FillList.FirstOrDefault();
            var orderIndex = FillWatchList.FindIndex((o) => o.OrderId == fillDetails.OrderId);

            decimal totalFilled = 0;


            //test
            //var testFill = FillList.First();//new Fill(null);
            //testFill = FillList.First();
            ////testFill.
            //FillList.Add(testFill);



            //reset the filled size assuming all the split orders are retunred
            if (FillList.Count > 0)
            {
                FillWatchList[orderIndex].FilledSize = 0.00m; 
                foreach (var curFill in FillList)
                {
                    totalFilled += Convert.ToDecimal(curFill.Size);
                    FillWatchList[orderIndex].FilledSize = FillWatchList[orderIndex].FilledSize 
                        + Convert.ToDecimal(curFill.Size);
                }

            }
            else
            {
                //error condition where there is no data in filled order
                return;
            }




            //check if ordered size and fill size match, 
            //if it does then remove from watch list
            //else mark it was partially filled

            var majorityFillSize = FillWatchList[orderIndex].ProductSize * 1;//0.99m;

            if (FillWatchList[orderIndex].FilledSize == majorityFillSize) //if filled size is more than 90%
            {
                //BusyCheckingOrder = false;
                //myActiveOrderBook.RemoveFromOrderList(fillDetails.OrderId);

                FillWatchList.RemoveAll(x => x.OrderId == fillDetails.OrderId);

                Logger.WriteLog("Order filled with following sizes:");
                FillList.ForEach((f) => Logger.WriteLog(f.Size));

                fillDetails.Size = totalFilled.ToString();
                FillUpdated?.Invoke(this, new FillEventArgs { filledOrder = fillDetails });


            }
            else
            {
                //cancel the order here the first time and retry to fill unfilled amount in OrderBook

                var myCurrentOrder = FillWatchList[orderIndex];

                //when checking the same order after the first time
                if (FillWatchList[orderIndex].Status == "PARTIALLY_FILLED")
                {

                    Logger.WriteLog(string.Format("{0} order({1}) of {2} {3} filled partially with following sizes:",
                        fillDetails.Side, fillDetails.OrderId, FillWatchList[orderIndex].ProductSize,
                        fillDetails.ProductId));

                    FillList.ForEach((f) => Logger.WriteLog(f.Size));

                    try
                    {
                        Logger.WriteLog("Cancelling remainder of partially filled order: " + myCurrentOrder.OrderId);
                        var cancelledOrder = myActiveOrderBook.CancelSingleOrder(myCurrentOrder.OrderId).Result;
                        if (cancelledOrder.Count > 0)
                            FillWatchList[orderIndex].Status = "PARTIALLY_FILLED";
                    }
                    catch (Exception)
                    {
                        Logger.WriteLog("Error cancelling partially filled order " + myCurrentOrder.OrderId);
                    }


                }
            }


            //fillDetails.Size = totalFilled.ToString(); //modify the filled size with the total instead of the first size in list

            //BusyCheckingOrder = false;

            //notify only if fully filled?
            //FillUpdated?.Invoke(this, new FillEventArgs { filledOrder = fillDetails });

        }


        private void TrackOrder()
        {


            if (IsBusy_TrackIngOrder)
                return;


            Logger.WriteLog("Checking fill status...");

            if (FillWatchList.Count() > 0)
                IsBusy_TrackIngOrder = true;

            while (FillWatchList.Count() > 0)
            {

                //if ticker is offline then return



                Logger.WriteLog(string.Format("Watching {0} order(s)", FillWatchList.Count()));

                //Logger.WriteLog(FillWatchList.FirstOrDefault());
                try
                {
                    //list may change in the middle of operation
                    FillWatchList.ForEach((x) => Logger.WriteLog(string.Format("{0} -> {1} {2} {3} @{4}",
                        x.OrderId, x.Side, x.ProductSize, x.Productname, x.UsdPrice)));
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error writing watch list (" + ex.Message + ")");
                };


                if (myActiveOrderBook.BusyWithWithCancelReorder)
                    Logger.WriteLog("Waiting for cancel and reorder to finish");


                //wait until cancel and reorder is done in orderbook 
                int cancelReorderWaitCount = 0;
                while (myActiveOrderBook.BusyWithWithCancelReorder)
                {
                    Thread.Sleep(50);
                    //cancelReorderWaitCount += 1;
                }



                BusyCheckingOrder = true; 

                for (int i = 0; i < FillWatchList.Count; i++)
                {
                    if (FillWatchList.Count == 0)
                        break;

                    //var orderStat = await GetFillStatus(FillWatchList.ElementAt(i)?.OrderId);
                    var currentOrder = FillWatchList.ElementAt(i);

                    FillResponse orderStat = null;
                    try
                    {
                        orderStat = GetFillStatus(currentOrder?.OrderId).Result;
                    }
                    catch (Exception)
                    {
                        Logger.WriteLog("Error getting fill response");
                    }
                    

                    if (orderStat?.Fills.Count > 0)
                    {
                        //BusyCheckingOrder = false;
                        orderFilledEvent(orderStat.Fills);
                    }

                    Thread.Sleep(300);

                }

                BusyCheckingOrder = false;
                //await Task.Delay(1000); //check fills every 1 sec
                //Thread.Sleep(500);
            }

            IsBusy_TrackIngOrder = false;


            //return true;
        }


    }
}
