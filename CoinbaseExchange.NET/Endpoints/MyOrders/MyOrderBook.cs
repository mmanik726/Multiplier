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
        public decimal price; 
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



    public class MyGetOrdersRequest : ExchangeRequestBase
    {
        public MyGetOrdersRequest(string endPoint): base("GET")
        {
            this.RequestUrl = String.Format(endPoint);
        }
    }

    public class MyPostOrdersRequest : ExchangeRequestBase
    {
        public MyPostOrdersRequest(string endPoint) : base("POST")
        {
            this.RequestUrl = String.Format(endPoint);
            this.RequestBody = "";
        }
    }

    public class MyDeleteOrdersRequest : ExchangeRequestBase
    {
        public MyDeleteOrdersRequest(string endPoint) : base("DELETE")
        {
            this.RequestUrl = String.Format(endPoint);
        }
    }

    class OrderPlacer: ExchangeClientBase 
    {
        public string customOrderId { get; set; } // client_oid
        public string size { get; set; }
        public string price { get; set; }
        public string side { get; set; }
        public string productName { get; set; }
        public string orderType { get; set; }

        public string selfTradeFlag { get; set; }

        public OrderPlacer(CBAuthenticationContainer authContainer) : base(authContainer)
        {
        }

        public async Task<Order> PlaceOrder(string endPoint)
        {

            MyPostOrdersRequest request = new MyPostOrdersRequest(endPoint);

            

            var genericResponse = await this.GetResponse(request);

            Order newOrderDetails = null; 

            if (genericResponse.IsSuccessStatusCode)
            {
                JObject obj = JObject.Parse(genericResponse.ContentBody);

                if (obj != null)
                {
                    newOrderDetails = new Order()
                    {
                        id = obj["id"].Value<string>(),
                        price = obj["price"].Value<decimal>(),
                        size = obj["size"].Value<decimal>(),
                        side = obj["side"].Value<string>(),
                        productId = obj["product_id"].Value<string>()
                    };
                }

            }
            else
            {
                throw new Exception("OrderUnsuccessfullError"); 
            }

            return newOrderDetails;

        }

    }





    public class OrderList : ExchangeClientBase
    {
        public string RequestEndpoint { get; set; }

        public OrderList(CBAuthenticationContainer authContainer) : base(authContainer)
        {
        }

        public async Task<List<Order>> GetAllOpenOrders(string endpoint)
        {
            var requestEndPoint = string.Format(endpoint);
            var genericResponse = await this.GetResponse(new MyGetOrdersRequest(requestEndPoint));

            var json = genericResponse.ContentBody;
            var orders_jArr = JArray.Parse(json);

            var allOrders = GetOrderFromOrderArray(orders_jArr);

            return allOrders;
        }


        private static List<Order> GetOrderFromOrderArray(JArray jArray)
        {
            List<Order> orders = new List<Order>();

            foreach (var obj in jArray)
            {
                orders.Add
                    (
                    new Order()
                    {
                        id = obj["id"].Value<string>(),
                        price = obj["price"].Value<decimal>(),
                        size = obj["size"].Value<decimal>(),
                        side = obj["side"].Value<string>(),
                        productId = obj["product_id"].Value<string>()
                    }
                    );

            }

            return orders;
        }

    }

    


    public class MyOrderBook : ExchangeClientBase
    {
        private CBAuthenticationContainer _auth { get; set; }
        private OrderList orderList; 
        public MyOrderBook (CBAuthenticationContainer authContainer) : base(authContainer)
        {

            _auth = authContainer;
            orderList = new OrderList(_auth);
        }


        public async Task<Order> PlaceNewLimitOrder(string oSide, string oProdName,
            string oSize, string oPrice)
        {
            OrderPlacer limitOrder = new OrderPlacer(_auth);
            
            var endPoint = string.Format(@"/orders");

            var newOrder = await limitOrder.PlaceOrder(endPoint); 

            return newOrder;
        }




        public bool CancelOrder()
        {
            return true;
        }

        public async Task<List<String>> CancelAllOrders()
        {

            MyDeleteOrdersRequest request = new MyDeleteOrdersRequest("/orders");

            var genericResponse = await this.GetResponse(request);

            List<string> cancelledOrderList = new List<string>();

            if (genericResponse.IsSuccessStatusCode)
            {
                var json = genericResponse.ContentBody;
                var orders_jArr = JArray.Parse(json).ToArray<JToken>();

                foreach (var obj in orders_jArr)
                {
                    cancelledOrderList.Add(obj.Value<string>());
                }
            }
            else
            {
                if (genericResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new Exception("PermissionDenied");
                }
                else
                {
                    throw new Exception("CancelOrderError");
                }
            }

            return cancelledOrderList;
        }


        public async Task<List<String>> CancelSingleOrder(string orderId)
        {

            var endPoint = string.Format(@"/orders/{0}", orderId);
            MyDeleteOrdersRequest request = new MyDeleteOrdersRequest(endPoint);

            var genericResponse = await this.GetResponse(request);

            List<string> cancelledOrderList = new List<string>();

            if (genericResponse.IsSuccessStatusCode)
            {
                var json = genericResponse.ContentBody;
                var orders_jArr = JArray.Parse(json).ToArray<JToken>();

                foreach (var obj in orders_jArr)
                {
                    cancelledOrderList.Add(obj.Value<string>());
                }
            }
            else
            {
                if (genericResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new Exception("PermissionDenied");
                }
                else
                {
                    throw new Exception("CancelOrderError");
                }
            }

            return cancelledOrderList;
        }


        public async Task<List<Order>> ListAllOrders() 
        {
            var requestEndPoint = string.Format(@"/orders");
            var allorders = await orderList.GetAllOpenOrders(requestEndPoint);

            //Task.Delay(100000);

            return allorders;
        }

        public async Task<List<Order>> ListAllOpenOrders()
        {
            var requestEndPoint = string.Format(@"/orders?status=open");
            var allorders = await orderList.GetAllOpenOrders(requestEndPoint);
            return allorders;
        }

        public async Task<List<Order>> ListAllPendingOrders()
        {
            var requestEndPoint = string.Format(@"/orders?status=pending");
            var allorders = await orderList.GetAllOpenOrders(requestEndPoint);
            return allorders;
        }

        public async Task<List<Order>> ListAllActiveOrders()
        {
            var requestEndPoint = string.Format(@"/orders?status=active");
            var allorders = await orderList.GetAllOpenOrders(requestEndPoint);
            return allorders;
        }

        public Order GetSingleOrder(string order)
        {
            return null;
        }

    }
}
