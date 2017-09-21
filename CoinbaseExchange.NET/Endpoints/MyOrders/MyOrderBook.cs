using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using Newtonsoft.Json.Converters;
namespace CoinbaseExchange.NET.Endpoints.MyOrders
{
    public class Order
    {
        //sample
        /*
         * {{
          "id": "361f3f36-d11f-4e02-8f69-63e28abf04cb",
          "price": "1.00000000",
          "size": "1.00000000",
          "product_id": "LTC-USD",
          "side": "buy",
          "stp": "dc",
          "type": "limit",
          "time_in_force": "GTC",
          "post_only": false,
          "created_at": "2017-09-20T21:23:51.183885Z",
          "fill_fees": "0.0000000000000000",
          "filled_size": "0.00000000",
          "executed_value": "0.0000000000000000",
          "status": "pending",
          "settled": false
        }}
         * */
        public string Id { get; set; } 
        public string Price { get; set; }
        public string Size { get; set; } 
        public string Product_id { get; set; } 
        public string Side; 
        public string Stp { get; set; } 
        public string Type { get; set; } 
        public string Time_in_force { get; set; }
        public string Post_only; 
        public string Created_at; 
        public string Fill_fees; 
        public string Filled_size; 
        public string Executed_value; 
        public string Status; 
        public string Settled;
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
        public MyPostOrdersRequest(string endPoint, JObject messageBodyJson) : base("POST")
        {
            this.RequestUrl = String.Format(endPoint);
            this.RequestBody = messageBodyJson.ToString(); ;
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
        //public string customOrderId { get; set; } // client_oid
        //public string size { get; set; }
        //public string price { get; set; }
        //public string side { get; set; }
        //public string productName { get; set; }
        //public string orderType { get; set; }

        //public string selfTradeFlag { get; set; }

        public OrderPlacer(CBAuthenticationContainer authContainer) : base(authContainer)
        {
        }

        public async Task<Order> PlaceOrder(JObject messageBody)
        {
            var orderEndpoint = string.Format(@"/orders");

            MyPostOrdersRequest request = new MyPostOrdersRequest(orderEndpoint, messageBody);

            var genericResponse = await this.GetResponse(request);

            Order newOrderDetails = null; 

            if (genericResponse.IsSuccessStatusCode)
            {
                try
                {
                    newOrderDetails = JsonConvert.DeserializeObject<Order>(genericResponse.ContentBody);
                }
                catch (Exception)
                {

                    throw new Exception("JsonParseError");
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
            var allOrders = GetOrderListFromJson(json);

            return allOrders;
        }


        private static List<Order> GetOrderListFromJson(string jsonString)
        {

            JObject jsonObj = new JObject(jsonString);

            List<Order> orders = new List<Order>();

            foreach (var obj in jsonObj)
            {
                try
                {
                    orders.Add(JsonConvert.DeserializeObject<Order>(obj.ToString()));
                }
                catch (Exception)
                {

                    throw new Exception("JsonParseError");
                }
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

            JObject orderBodyObj = new JObject();

            //orderBodyObj.Add(new JProperty("client_oid", ""));

            orderBodyObj.Add(new JProperty("type", "limit"));
            orderBodyObj.Add(new JProperty("product_id", oProdName));
            orderBodyObj.Add(new JProperty("side", oSide));
            orderBodyObj.Add(new JProperty("price", oPrice));
            orderBodyObj.Add(new JProperty("size", oSize));
            //orderBodyObj.Add(new JProperty("", ""));
            //orderBodyObj.Add(new JProperty("", ""));
            //orderBodyObj.Add(new JProperty("", ""));

            var newOrder = await limitOrder.PlaceOrder(orderBodyObj); 

            return newOrder;
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

            List<string> cancelledOrder = new List<string>();

            if (genericResponse.IsSuccessStatusCode)
            {
                var json = genericResponse.ContentBody;
                var orders_jArr = JArray.Parse(json).ToArray<JToken>();

                foreach (var obj in orders_jArr)
                {
                    cancelledOrder.Add(obj.Value<string>());
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

            return cancelledOrder;
        }


        public async Task<List<Order>> GetAllOrders() 
        {
            var requestEndPoint = string.Format(@"/orders");
            var allorders = await orderList.GetAllOpenOrders(requestEndPoint);

            //Task.Delay(100000);

            return allorders;
        }

        public async Task<List<Order>> GetAllOpenOrders()
        {
            var requestEndPoint = string.Format(@"/orders?status=open");
            var allorders = await orderList.GetAllOpenOrders(requestEndPoint);
            return allorders;
        }

        public async Task<List<Order>> GetAllPendingOrders()
        {
            var requestEndPoint = string.Format(@"/orders?status=pending");
            var allorders = await orderList.GetAllOpenOrders(requestEndPoint);
            return allorders;
        }

        public async Task<List<Order>> GetAllActiveOrders()
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
