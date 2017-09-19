using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;

namespace CoinbaseExchange.NET.Endpoints.MyOrders
{
    public class Order
    {
        public string id; //: "68e6a28f-ae28-4788-8d4f-5ab4e5e5ae08",
        public decimal size; //"size": "1.00000000",
        public string productId; //"product_id": "BTC-USD",
        public string side; //"side": "buy",
        public string stpFlag; //"stp": "dc",
        public decimal funds;  //"funds": "9.9750623400000000",
        public decimal specifiedFunds; // "specified_funds": "10.0000000000000000",
        public string OrderType; // "type": "market",
        public bool PostOnly; //"post_only": false,
        public string OrderCreateTime; // "created_at": "2016-12-08T20:09:05.508883Z",
        public string OrderDoneAt; //"done_at": "2016-12-08T20:09:05.527Z",
        public string DoneReason; //"done_reason": "filled",
        public decimal fillFee; //"fill_fees": "0.0249376391550000",
        public decimal fillSize; //  "filled_size": "0.01291771",
        public decimal fillValue; // "executed_value": "9.9750556620000000",
        public string Status; //"status": "done",
        public bool settled; //"settled": true
    }



    public class MyOrderBookRequest : ExchangeRequestBase
    {
        public MyOrderBookRequest(): base("GET")
        {
            this.RequestUrl = String.Format("/orders");
        }
    }

    public class MyOrderBookResponse : ExchangeResponseBase
    {

        public MyOrderBookResponse(ExchangeResponse response) : base (response)
        {
            var json = response.ContentBody;
            var jObject = JObject.Parse(json);

            //var bids = jObject["bids"].Select(x => (JArray)x).ToArray();
            //var asks = jObject["asks"].Select(x => (JArray)x).ToArray();

            //Sequence = jObject["sequence"].Value<long>();

            //Sells = asks.Select(a => GetBidAskOrderFromJToken(a)).ToList();
            //Buys = bids.Select(b => GetBidAskOrderFromJToken(b)).ToList();
        }

        private Order GetOrderFromOrderArray(JArray jArray)
        {
            return new Order()
            {
                id = jArray[0].Value<string>(),
                size = jArray[1].Value<decimal>(),
                productId = jArray[2].Value<string>()
            };
        }

    }

    public class MyOrderBook : ExchangeClientBase
    {

        public MyOrderBook (CBAuthenticationContainer authContainer) : base(authContainer)
        {

        }

        public bool PlaceNewOrder()
        {
            return true;
        }


        public bool CancelOrder()
        {
            return true;
        }

        public bool CancelAllOrders()
        {
            return true;
        }

        public async Task<List<Order>> ListAllOpenOrders() 
        {

            var genericResponse = await  this.GetResponse(new MyOrderBookRequest());

            MyOrderBookResponse myOrderBookResponse = new MyOrderBookResponse(genericResponse);

            

            return null;
        }

        public Order GetSingleOrder(string order)
        {
            return null;
        }

    }
}
