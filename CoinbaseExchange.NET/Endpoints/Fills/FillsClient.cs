using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CoinbaseExchange.NET.Endpoints.MyOrders;
using System.Diagnostics;

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
            this.Side = jToken["size"].Value<string>();
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
        public List<Fill> Fills { get; }
        public FillEventArgs(List<Fill> fills)
        {
            this.Fills = fills;
        }
    }


    public class FillsClient : ExchangeClientBase
    {
        private static object _watchListLock = new object();
        public EventHandler FillUpdated;
        public List<MyOrder> FillWatchList { get; set; }

        private MyOrderBook myActiveOrderBook; 

        private static bool IsBusy_TrackIngOrder; 

        public FillsClient(CBAuthenticationContainer authenticationContainer, MyOrderBook orderBook) : base(authenticationContainer)
        {
            myActiveOrderBook = orderBook;
            FillWatchList = orderBook.MyChaseOrderList ;
            IsBusy_TrackIngOrder = false;
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
            var response = await this.GetResponse(request);
            var accountHistoryResponse = new FillResponse(response);
            return accountHistoryResponse;
        }

        public async Task<FillResponse> GetFillStatus(string orderId)
        {
            var endpoint = string.Format(@"/fills?order_id={0}", orderId);
            var request = new GetFillsRequest(endpoint);

            var response = await this.GetResponse(request);
            var orderStats = new FillResponse(response);

            return orderStats;
        }

        private void orderFilledEvent(string orderId)
        {
            if (FillUpdated != null)
            {
                FillUpdated(this, EventArgs.Empty);
            }

        }


        private async void TrackOrder()
        {


            if (IsBusy_TrackIngOrder)
                return;


            System.Diagnostics.Debug.WriteLine("Checking fill status...");

            if (FillWatchList.Count() > 0)
                IsBusy_TrackIngOrder = true;

            while (FillWatchList.Count() > 0)
            {

                System.Diagnostics.Debug.WriteLine(string.Format("Watching {0} order(s)", FillWatchList.Count()));

                //System.Diagnostics.Debug.WriteLine(FillWatchList.FirstOrDefault());

                FillWatchList.ForEach((x) => System.Diagnostics.Debug.WriteLine(x.OrderId));

                for (int i = 0; i < FillWatchList.Count; i++)
                {
                    if (FillWatchList.Count == 0)
                        break;

                    var orderStat = await GetFillStatus(FillWatchList.ElementAt(i).OrderId);
                    //orderStat.Fills.FirstOrDefault();

                    await Task.Delay(400);

                    if (orderStat.Fills.Count > 0)
                    {
                        //busy waiting
                        while(myActiveOrderBook.isUpdatingOrderList)
                            Debug.WriteLine("waiting for order list update lock release");

                        myActiveOrderBook.isUpdatingOrderList = true; //wait
                        FillWatchList.RemoveAll(x => x.OrderId == orderStat.Fills.FirstOrDefault().OrderId);
                        myActiveOrderBook.isUpdatingOrderList = false; //release

                        orderFilledEvent(orderStat.Fills.FirstOrDefault().OrderId);
                    }



                }


                await Task.Delay(1000); //check fills every 1 sec

            }

            IsBusy_TrackIngOrder = false;


            //return true;
        }


    }
}
