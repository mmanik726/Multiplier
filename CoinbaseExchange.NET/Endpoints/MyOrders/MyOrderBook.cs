using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using Newtonsoft.Json.Converters;

using CoinbaseExchange.NET.Endpoints.PublicData;
using CoinbaseExchange.NET.Endpoints.Fills;

using System.Diagnostics;

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

                throw new Exception("OrderUnsuccessfullError: " + genericResponse.ContentBody); 
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

            //JObject jsonObj = new JObject(jsonString);

            var jsonArray = JArray.Parse(jsonString);

            List<Order> orders = new List<Order>();

            foreach (var obj in jsonArray)
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

    

    public class MyOrder
    {
        public string OrderId;
        public string Productname { get; set; }
        public string Status { get; set; }
        public decimal ProductAmount { get; set; }
        public decimal UsdAmount { get; set; }
        public string OrderType { get; set; }
        public bool ChaseBestPrice { get; set; }
        public string Side { get; set; }

        public MyOrder(string productName = "")
        {
            OrderId = "";
            Productname = productName;
            OrderType = "limit";
            Status = "";
            UsdAmount = 0;
            ProductAmount = 0;
            ChaseBestPrice = false;

        }
    }

    public class OrderUpdateEventArgs : EventArgs
    {

        public string OrderId { get; set; }
        public string Message { get; set; }
    }


    public class MyOrderBook : ExchangeClientBase
    {
        private CBAuthenticationContainer _auth { get; set; }
        private OrderList orderList;

        private TickerClient PriceTicker;
        private FillsClient fillsClient;

        //private List<MyOrder> MyActiveOrderList;
        public List<MyOrder> MyChaseOrderList;

        public EventHandler OrderUpdateEvent;

        private static bool isBusyCancelAndReorder;

        private static decimal currentPrice;

        private static DateTime lastTickTIme;  

        private static readonly object updateLock = new object();
        private static readonly object filledLock = new object();

        public bool isUpdatingOrderList; 


        private void NotifyOrderUpdateListener(OrderUpdateEventArgs message)
        {

            if (OrderUpdateEvent != null)
                OrderUpdateEvent(this, message);
        }


        public MyOrderBook (CBAuthenticationContainer authContainer, string product) : base(authContainer)
        {
            isUpdatingOrderList = false;

            isBusyCancelAndReorder = false;

            _auth = authContainer;
            orderList = new OrderList(_auth);

            MyChaseOrderList = new List<MyOrder>();

            lastTickTIme = DateTime.UtcNow;

            PriceTicker = new TickerClient(product);
            PriceTicker.PriceUpdated += PriceUpdateEventHandler;

            fillsClient = new FillsClient(_auth, this);
            fillsClient.FillUpdated += OrderFilledEventHandler;

        }

        public async void OrderFilledEventHandler(object sender, EventArgs args)
        {
            //FillEventArgs filledOrders = (FillEventArgs)args;

            Debug.WriteLine("Order filled");

            //var filledOrder = filledOrders.Fills.FirstOrDefault();


            //lock (filledLock)
            //{
            //    //remove the order from MyActiveOrderList
            //    MyChaseOrderList.RemoveAll(x => x.OrderId == filledOrder.OrderId);

            //    fillsClient.RemoveFromOrderWatchList(filledOrder.OrderId);
            //}


            //System.Diagnostics.Debug.WriteLine(string.Format("Order id: {0} has been filled", filledOrder.OrderId));

            //NotifyOrderUpdateListener(new OrderUpdateEventArgs
            //{
            //    Message = string.Format("Order id: {0} has been filled", filledOrder.OrderId),
            //    OrderId = filledOrder.OrderId
            //});
        }

        public async void PriceUpdateEventHandler(object sender, EventArgs args)
        {

            var curTime = DateTime.UtcNow;
            var timeSincLastTick = (curTime - lastTickTIme).TotalMilliseconds; 

            if (timeSincLastTick < 3000)
            {
                return;
            }
            else
            {
                lastTickTIme = DateTime.UtcNow;
            }

            var tickerMsg = (TickerMessage)args;

            var tickerPrice = tickerMsg.RealTimePrice;
            //currentPrice = tickerMsg.RealTimePrice;

            if (tickerPrice == currentPrice)
                return; // do nothing if price is the same 
            else
                currentPrice = tickerPrice;


            //check if the new order price is the same as the last placed order
            if (MyChaseOrderList.Count > 0 )
            {
                decimal tempPrice = 0;
                if (MyChaseOrderList.FirstOrDefault().Side == "buy")
                    tempPrice = currentPrice - 0.01m; //m is for decimal
                else
                    tempPrice = currentPrice + 0.01m;

                if (MyChaseOrderList.FirstOrDefault().UsdAmount == tempPrice)
                {
                    return;
                }
            }


            if (isBusyCancelAndReorder || MyChaseOrderList.Count == 0)
            {
                return;
            }

            //System.Diagnostics.Debug.WriteLine("Orders in ActiveOrderList:");
            //MyActiveOrderList.ForEach(x => System.Diagnostics.Debug.WriteLine("\t" + x.OrderId));

            //if (MyChaseOrderList.Count == 0)
            //{
            //    return;
            //}

            //await Task.Factory.StartNew(() => cancelAndReorder());
            await Task.Run(() => CancelAndReorder());
        }


        private void CancelAndReorder()
        {
            isBusyCancelAndReorder = true;

            Debug.WriteLine("\t\tcancel order: " + MyChaseOrderList.FirstOrDefault().OrderId);
            //Task.Delay(15000);

                
            MyOrder myCurrentOrder = MyChaseOrderList.FirstOrDefault();


            //cancell the current order
            List<string> cancelledOrder = new List<string>();
            try
            {
                var temp = CancelSingleOrder(myCurrentOrder.OrderId);
                temp.Wait();
                cancelledOrder = temp.Result;

                if (cancelledOrder.Count() > 0)
                {
                    //busy waiting
                    while (isUpdatingOrderList)
                        Debug.WriteLine("waiting for order list update lock release");

                    isUpdatingOrderList = true;
                    MyChaseOrderList.RemoveAll(x => x.OrderId == cancelledOrder.FirstOrDefault());
                    isUpdatingOrderList = false;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                //throw;
            }


            decimal adjustedPrice;
            if (myCurrentOrder.Side == "buy")
                adjustedPrice = currentPrice;// - 0.01m; //m is for decimal
            else
                adjustedPrice = currentPrice;// + 0.01m;



            //place new order
            var orderAtNewPrice = PlaceNewLimitOrder(
                oSide: myCurrentOrder.Side,
                oProdName: myCurrentOrder.Productname,
                oSize: myCurrentOrder.ProductAmount.ToString(),
                oPrice: adjustedPrice.ToString(),
                chaseBestPrice: true
                ).Result;


            if (orderAtNewPrice == null)
            {
                Debug.WriteLine("Order error ");
                //try again with new order
                isBusyCancelAndReorder = false;
                PriceUpdateEventHandler(this, EventArgs.Empty);
                return;
            }

            if (orderAtNewPrice.Status == "rejected")
            {
                Debug.WriteLine("Order rejected at " + adjustedPrice.ToString());
                //try again with new order
                isBusyCancelAndReorder = false;
                PriceUpdateEventHandler(this, EventArgs.Empty);
                return;
            }


            ////add the new order to order list
            ////busy waiting
            //while (isUpdatingOrderList)
            //    Debug.WriteLine("waiting for lock release");

            //isUpdatingOrderList = true;

            //MyChaseOrderList.Add(
            //    new MyOrder
            //    {
            //        OrderId = orderAtNewPrice.Id,
            //        Productname = orderAtNewPrice.Product_id,
            //        OrderType = "limit",
            //        Status = "OPEN",
            //        UsdAmount = Convert.ToDecimal(orderAtNewPrice.Price),
            //        ProductAmount = Convert.ToDecimal(orderAtNewPrice.Size),
            //        Side = orderAtNewPrice.Side,
            //        ChaseBestPrice = true
            //    });
            //isUpdatingOrderList = false;

            Debug.WriteLine("Orders in ActiveOrderList:");


            MyChaseOrderList.ForEach(x => Debug.WriteLine("\t" + x.OrderId));
                
            isBusyCancelAndReorder = false;

        }

        public async Task<Order> PlaceNewLimitOrder(string oSide, string oProdName,
            string oSize, string oPrice, bool chaseBestPrice)
        {
            OrderPlacer limitOrder = new OrderPlacer(_auth);

            JObject orderBodyObj = new JObject();

            //orderBodyObj.Add(new JProperty("client_oid", ""));

            orderBodyObj.Add(new JProperty("type", "limit"));
            orderBodyObj.Add(new JProperty("product_id", oProdName));
            orderBodyObj.Add(new JProperty("side", oSide));
            orderBodyObj.Add(new JProperty("price", oPrice));
            orderBodyObj.Add(new JProperty("size", oSize));
            //orderBodyObj.Add(new JProperty("post_only", "T"));


            var newOrder = await limitOrder.PlaceOrder(orderBodyObj);

            if (newOrder != null)
            {
                if (newOrder.Status != "rejected")
                {
                    //busy waiting
                    while (isUpdatingOrderList)
                        Debug.WriteLine("waiting for lock release");

                    isUpdatingOrderList = true;
                    MyChaseOrderList.Add(
                        new MyOrder
                        {
                            OrderId = newOrder.Id,
                            Productname = newOrder.Product_id,
                            OrderType = "limit",
                            Status = "OPEN",
                            UsdAmount = Convert.ToDecimal(newOrder.Price),
                            ProductAmount = Convert.ToDecimal(newOrder.Size),
                            Side = newOrder.Side,
                            ChaseBestPrice = chaseBestPrice

                        });
                    isUpdatingOrderList = false;

                    if (chaseBestPrice && MyChaseOrderList.Count == 1)
                    {
                        fillsClient.startTracker();
                    }
                }


            }

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

            //if (cancelledOrder.Count() > 0)
            //{

            //    //busy waiting
            //    while (isUpdatingOrderList)
            //        Debug.WriteLine("waiting for order list update lock release");

            //    isUpdatingOrderList = true; //wait
            //    MyChaseOrderList.RemoveAll(x => x.OrderId == cancelledOrder.FirstOrDefault());
            //    isUpdatingOrderList = false; //release


            //    //fillsClient.RemoveFromOrderWatchList(cancelledOrder.FirstOrDefault());
            //}

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
