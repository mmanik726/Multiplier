using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
        public List<String> FillWatchList { get; set; }

        public FillsClient(CBAuthenticationContainer authenticationContainer) : base(authenticationContainer)
        {
            FillWatchList = new List<string>();
            startTracker();
        }

        async void startTracker()
        {
            //await Task.Factory.StartNew(() => FillTracker(onFillUpdate));
            await Task.Run(() => FillTracker(onFillUpdate));

        }

        public async void AddOrderToWatchList(string orderId)
        {
            lock (_watchListLock)
            {
                this.FillWatchList.Add(orderId);
            }

            //var result = await FillTracker(onFillUpdate);
        }

        public async void RemoveFromOrderWatchList(string orderId)
        {
            lock (_watchListLock)
            {

                this.FillWatchList.RemoveAll(x => x == orderId);

                //if (this.FillWatchList.Contains(orderId))
                //    this.FillWatchList.RemoveAt(FillWatchList.IndexOf(orderId));
            }

            //var result = await FillTracker(onFillUpdate);
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

        private void onFillUpdate(List<Fill> fillList)
        {
            //do other stuff with received data before calling 
            //attached subscriber method

            FillEventArgs fillEvntArgs = new FillEventArgs(fillList);

            FillUpdated(this, fillEvntArgs);
        }


        private async void FillTracker(Action<List<Fill>> onFillReqUpdated)
        {

            //if (this.FillWatchList.Count() == 0)
            //{
            //    return false;
            //}


            if (onFillReqUpdated == null)
                throw new ArgumentNullException("onFillReqUpdated", "fill tracker callback must not be null.");


            System.Diagnostics.Debug.WriteLine("Fill tracker started");

            while (true)
            {
                if (this.FillWatchList.Count() == 0)
                    continue;

                System.Diagnostics.Debug.WriteLine(string.Format("Watching {0} order(s)", FillWatchList.Count()));

                //System.Diagnostics.Debug.WriteLine(FillWatchList.FirstOrDefault());

                FillWatchList.ForEach((x) => System.Diagnostics.Debug.WriteLine(x));

                for (int i = 0; i < FillWatchList.Count; i++)
                {
                    var orderStat = await GetFillStatus(FillWatchList.ElementAt(i));
                    //orderStat.Fills.FirstOrDefault();

                    if (orderStat.Fills.Count > 0)
                    {
                        lock (_watchListLock)
                        {
                            this.FillWatchList.RemoveAll(x => x == orderStat.Fills.FirstOrDefault().OrderId);
                        }

                        onFillReqUpdated(orderStat.Fills);
                    }



                    await Task.Delay(500);
                }


                await Task.Delay(1000); //check fills every 1 sec

            }




            //return true;
        }


    }
}
