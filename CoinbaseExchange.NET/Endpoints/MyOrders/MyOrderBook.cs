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
using CoinbaseExchange.NET.Utilities;
using System.Threading;
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

            ExchangeResponse genericResponse = null;

            try
            {
                genericResponse = await this.GetResponse(request);
            }
            catch (Exception)
            {
                throw new Exception("PlaceOrderError");
            }


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

            ExchangeResponse genericResponse = null;

            try
            {
                genericResponse = await this.GetResponse(new MyGetOrdersRequest(requestEndPoint));
            }
            catch (Exception)
            {
                throw new Exception("GetOpenOrderError");
            }

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
        public string ProductName { get; set; }
        public string OrderId { get; set; }
        public string fillSize { get; set; }
        public string side { get; set; }
        public string filledAtPrice { get; set; }
        public string Message { get; set; }
        public string fillFee { get; set; }
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

        private static int OrderRetriedCount;
        private static decimal OrderStartingPrice;

        private string ProductName; 

        private static DateTime lastTickTIme;  

        private static readonly object updateLock = new object();
        private static readonly object filledLock = new object();

        public bool isUpdatingOrderList; 


        private void NotifyOrderUpdateListener(OrderUpdateEventArgs message)
        {

            if (OrderUpdateEvent != null)
                OrderUpdateEvent(this, message);
        }


        public MyOrderBook (CBAuthenticationContainer authContainer, string product, ref TickerClient inputTickerClient) : base(authContainer)
        {
            isUpdatingOrderList = false;

            isBusyCancelAndReorder = false;

            OrderRetriedCount = 0;
            OrderStartingPrice = 0;


            ProductName = product;
            _auth = authContainer;
            orderList = new OrderList(_auth);

            MyChaseOrderList = new List<MyOrder>();

            lastTickTIme = DateTime.UtcNow;

            PriceTicker = inputTickerClient; //new TickerClient(product); //no need to create two different ticker, use one global ticker
            PriceTicker.PriceUpdated += PriceUpdateEventHandler;
            PriceTicker.TickerConnectedEvent += TickerConnectedEventHandler;
            PriceTicker.TickerDisconnectedEvent += TickerDisconnectedEventHandler;

            fillsClient = new FillsClient(_auth, this);
            fillsClient.FillUpdated += OrderFilledEventHandler;

        }

        private void TickerDisconnectedEventHandler (object sender, EventArgs args)
        {
            //remove all the items from the watch list

        }

        private void TickerConnectedEventHandler(object sender, EventArgs args)
        {
            //get all pending orders from server and update the watch list


            Logger.WriteLog("Syncing open orders with server, after reconnect");
            Logger.WriteLog("Orders is active order list before syncing:");

            printActiveOrderList();

            var allOpenOrders = GetAllOpenOrders();
            allOpenOrders.Wait();
            UpdateOrderListWithServer(allOpenOrders.Result);

            Logger.WriteLog("Orders is active order list after syncing:");
            printActiveOrderList();

        }

        void printActiveOrderList()
        {
            try
            {
                MyChaseOrderList.ForEach(x => Logger.WriteLog("\t" + x.OrderId));
            }
            catch (Exception)
            {
                Logger.WriteLog("Error enumerating list, list changed");
            }
        }


        private void UpdateOrderListWithServer(List<Order> orders)
        {
            //busy waiting
            while (isUpdatingOrderList)
                Logger.WriteLog("waiting for order list update lock release");

            var openServerOderList = orders.Select((x) => x.Id).ToList();

            isUpdatingOrderList = true;

            //remove all the items from the watch list that does not exists in the servers open oders list
            try
            {
                MyChaseOrderList.RemoveAll((item) => !openServerOderList.Contains(item.OrderId));
            }
            catch (Exception)
            {
                Logger.WriteLog("Error removing orders from active orderlist that are already filled in server after ticker reconnect");
                //throw;
            }
            isUpdatingOrderList = false;
        }


        public void OrderFilledEventHandler(object sender, EventArgs args)
        {
            FillEventArgs filledOrders = (FillEventArgs)args;

            Logger.WriteLog("Order filled");

            var filledOrder = filledOrders.filledOrder;



            //reset the order retry count
            OrderRetriedCount = 0;
            OrderStartingPrice = 0;


            //Logger.WriteLog(string.Format("Order id: {0} has been filled", filledOrder.OrderId));

            NotifyOrderUpdateListener(new OrderUpdateEventArgs
            {
                side = filledOrder.Side,
                filledAtPrice = filledOrder.Price,
                Message = string.Format("Order id: {0} has been filled", filledOrder.OrderId),
                OrderId = filledOrder.OrderId,
                fillFee = filledOrder.Fee,
                fillSize = filledOrder.Size,
                ProductName = filledOrder.ProductId

            });


        }

        public void PriceUpdateEventHandler(object sender, EventArgs args)
        {

            
            var curTime = DateTime.UtcNow;
            var timeSinceLastTick = (curTime - lastTickTIme).TotalMilliseconds;

            if (timeSinceLastTick < (4 * 1000)) //wait 5 sec before next price change. origianlly 3 sec
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
                decimal tempPrice = getAdjustedCurrentPrice(MyChaseOrderList.FirstOrDefault());


                if (MyChaseOrderList.FirstOrDefault().UsdAmount == tempPrice)
                {
                    return;
                }
            }


            if (isBusyCancelAndReorder)
            {
                Debug.Write("busy with cancel and reorder"); 
                return;
            }

            if (MyChaseOrderList.Count == 0)
            {
                return;
            }

            //await Task.Factory.StartNew(() => cancelAndReorder());
            isBusyCancelAndReorder = true;


            Task<bool> cancelAndReorderResult = Task.Run(() => CancelAndReorder());
            cancelAndReorderResult.Wait(); //wait for cancel and reorder to finish
            
        }


        private bool CancelAndReorder()
        {
            isBusyCancelAndReorder = true;

            //Logger.WriteLog("\t\t in cancel and reorder"); 

            for (int i = 0; i < MyChaseOrderList.Count; i++)
            {
                if (MyChaseOrderList.Count == 0)
                    break;

                MyOrder myCurrentOrder = MyChaseOrderList.FirstOrDefault();

                //cancell ALL the current orders
                List<string> cancelledOrder = new List<string>();


                //////problem code
                ////var temp = CancelSingleOrder(myCurrentOrder.OrderId);
                ////temp.Wait();
                ////cancelledOrder = temp.Result;



                //var temp = CancelSingleOrder(myCurrentOrder.OrderId).Result;
                //temp.Wait();
                try
                {
                    cancelledOrder = CancelSingleOrder(myCurrentOrder.OrderId).Result;
                }
                catch (Exception)
                {
                    Logger.WriteLog(string.Format("error cancelling order {0}, retrying...", myCurrentOrder.OrderId));
                    continue; //continue and try again to cancel and reorder
                    //throw;
                }

                if (cancelledOrder.Count() > 0)
                {
                    RemoveFromOrderList(cancelledOrder.FirstOrDefault());
                }



                Logger.WriteLog(string.Format("\t\tcancel {0} order: {1}", myCurrentOrder.Side, myCurrentOrder.OrderId));


                decimal adjustedPrice = 0;


                decimal curPriceDifference = 0;

                if (OrderStartingPrice > 0)
                    curPriceDifference = Math.Abs(OrderStartingPrice - PriceTicker.CurrentPrice);



                //OrderStartingPrice = adjustedPrice;

                var priceChangePercent = PriceTicker.CurrentPrice * 0.0025m;

                if (OrderRetriedCount <= 13 && curPriceDifference <= priceChangePercent) // retry a total of 15 times or while less than a "big jump"
                {
                    adjustedPrice = getAdjustedCurrentPrice(myCurrentOrder);
                    //place new order limit order
                    var orderAtNewPrice = PlaceNewOrder(
                        oSide: myCurrentOrder.Side,
                        oProdName: myCurrentOrder.Productname,
                        oSize: myCurrentOrder.ProductAmount.ToString(),
                        oPrice: adjustedPrice.ToString(),
                        chaseBestPrice: true,
                        orderType: "limit"
                        );

                    try
                    {
                        orderAtNewPrice.Wait();
                    }
                    catch (Exception)
                    {
                        Logger.WriteLog("error in cancel and reorder occured while placing new limit order, retrying...");
                        continue;
                    }
                }
                else
                {
                    if (curPriceDifference > priceChangePercent)
                        Logger.WriteLog("Price changed more than " + curPriceDifference + " Forcing market order");
                    adjustedPrice = getAdjustedCurrentPrice(myCurrentOrder, "market");
                    //place new market order
                    var orderAtNewPrice = PlaceNewOrder(
                        oSide: myCurrentOrder.Side,
                        oProdName: myCurrentOrder.Productname,
                        oSize: myCurrentOrder.ProductAmount.ToString(),
                        oPrice: adjustedPrice.ToString(),
                        chaseBestPrice: true,
                        orderType: "market"
                        );

                    try
                    {
                        orderAtNewPrice.Wait();
                    }
                    catch (Exception)
                    {
                        Logger.WriteLog("error in cancel and reorder occured while placing new market order, retrying...");
                        continue;
                    }
                }



            }


            OrderRetriedCount += 1;

            Logger.WriteLog("Order retried count: " + OrderRetriedCount);

            Logger.WriteLog("Orders in ActiveOrderList:");

            try
            {
                MyChaseOrderList.ForEach(x => Logger.WriteLog("\t" + x.OrderId));

            }
            catch (Exception)
            {
                Logger.WriteLog("Error enumerating order list, list changed");
            }
                
            isBusyCancelAndReorder = false;

            return true;
        }


        private void RemoveFromOrderList(string orderId)
        {
            //busy waiting
            while (isUpdatingOrderList)
                Logger.WriteLog("waiting for order list update lock release");

            isUpdatingOrderList = true;
            MyChaseOrderList.RemoveAll(x => x.OrderId == orderId);
            isUpdatingOrderList = false;
        }

        private void AddToOrderList(MyOrder order)
        {
            //busy waiting
            while (isUpdatingOrderList)
                Logger.WriteLog("waiting for lock release");

            isUpdatingOrderList = true;
            MyChaseOrderList.Add(order);
            isUpdatingOrderList = false;
        }

        public async Task<Order> PlaceNewOrder(string oSide, string oProdName,
            string oSize, string oPrice, bool chaseBestPrice, string orderType = "limit")
        {
            OrderPlacer limitOrder = new OrderPlacer(_auth);

            JObject orderBodyObj = new JObject();

            //orderBodyObj.Add(new JProperty("client_oid", ""));

            orderBodyObj.Add(new JProperty("type", orderType));
            orderBodyObj.Add(new JProperty("product_id", oProdName));
            orderBodyObj.Add(new JProperty("side", oSide));
            orderBodyObj.Add(new JProperty("price", oPrice));
            orderBodyObj.Add(new JProperty("size", oSize));

            if(orderType == "limit")
                orderBodyObj.Add(new JProperty("post_only", "T"));

            if (OrderStartingPrice == 0) //not yet set
                OrderStartingPrice = Convert.ToDecimal(oPrice);


            Logger.WriteLog(string.Format("placing new {0} {1} order @{2}", orderType, oSide, oPrice));
            var newOrder = await limitOrder.PlaceOrder(orderBodyObj);

            if (newOrder != null)
            {

                var myCurOrder = new MyOrder
                {
                    OrderId = newOrder.Id,
                    Productname = newOrder.Product_id,
                    OrderType = orderType,
                    Status = "OPEN",
                    UsdAmount = Convert.ToDecimal(newOrder.Price),
                    ProductAmount = Convert.ToDecimal(newOrder.Size),
                    Side = newOrder.Side,
                    ChaseBestPrice = chaseBestPrice
                };


                if (newOrder.Status != "rejected")
                {
                    AddToOrderList(myCurOrder);

                    if (chaseBestPrice && MyChaseOrderList.Count == 1)
                    {
                        fillsClient.startTracker();
                    }
                }
                else
                {
                    //wait before placing a new order
                    //Task.Delay(200);
                    Thread.Sleep(200);

                    Logger.WriteLog("Last order rejected, retrying..."); 
                    decimal adjustedPrice = getAdjustedCurrentPrice(myCurOrder);

                    var orderAtNewPrice = PlaceNewOrder(
                        oSide: myCurOrder.Side,
                        oProdName: myCurOrder.Productname,
                        oSize: myCurOrder.ProductAmount.ToString(),
                        oPrice: adjustedPrice.ToString(),
                        chaseBestPrice: true,
                        orderType: orderType
                        );
                }


            }

            return newOrder;
        }


        decimal getAdjustedCurrentPrice(MyOrder order, string otype = "limit" )
        {

            decimal curTmpPrice = 0;

            if (currentPrice == 0)
                curTmpPrice = PriceTicker.CurrentPrice;
            else
                curTmpPrice = currentPrice;


            if (otype == "market")
            {
                return curTmpPrice; 
            }


            //if (order.Side == "buy")
            //    return curTmpPrice - 10.00m; //m is for decimal
            //else
            //    return curTmpPrice + 10.00m;

            if (order.Side == "buy")
                return curTmpPrice - 0.01m; //m is for decimal
            else
                return curTmpPrice + 0.01m;

        }


        public async Task<List<String>> CancelAllOrders()
        {

            MyDeleteOrdersRequest request = new MyDeleteOrdersRequest("/orders");

            ExchangeResponse genericResponse = null;

            try
            {
                genericResponse = await this.GetResponse(request);
            }
            catch (Exception)
            {
                throw new Exception("CancelAllOrderError");
            }


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
                    Logger.WriteLog("cancel order permissions denied");
                    throw new Exception("PermissionDenied");
                }
                else
                {
                    Logger.WriteLog("cancel order error: " + genericResponse.ContentBody);
                    throw new Exception("CancelOrderError");
                }
            }

            return cancelledOrderList;
        }


        public async Task<List<String>> CancelSingleOrder(string orderId)
        {

            var endPoint = string.Format(@"/orders/{0}", orderId);
            MyDeleteOrdersRequest request = new MyDeleteOrdersRequest(endPoint);

            //var genericResponse = await this.GetResponse(request);

            ExchangeResponse genericResponse = null;

            try
            {
                genericResponse = await this.GetResponse(request);
            }
            catch (Exception)
            {
                throw new Exception("CancelSingleOrderError");
            }


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
                    Logger.WriteLog("cancel order permissions denied");
                    throw new Exception("PermissionDenied");
                }
                else if (genericResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Logger.WriteLog(string.Format("order {0} not found, may have been already filled or cancelled otherwise", orderId));
                    throw new Exception("OrderNotFound");
                }
                else
                {
                    Logger.WriteLog(string.Format("Error cancelling order {0}: ", orderId, genericResponse.StatusCode.ToString()));
                    throw new Exception("CancelOrderError");
                }
            }

            //if (cancelledOrder.Count() > 0)
            //{

            //    //busy waiting
            //    while (isUpdatingOrderList)
            //        Logger.WriteLog("waiting for order list update lock release");

            //    isUpdatingOrderList = true; //wait
            //    MyChaseOrderList.RemoveAll(x => x.OrderId == cancelledOrder.FirstOrDefault());
            //    isUpdatingOrderList = false; //release


            //    //fillsClient.RemoveFromOrderWatchList(cancelledOrder.FirstOrDefault());
            //}

            return cancelledOrder;
        }


        public async Task<List<Order>> GetAllOrders() 
        {
            //gets all pending order by default arguments 
            var requestEndPoint = string.Format(@"/orders");
            var allorders = await orderList.GetAllOpenOrders(requestEndPoint);

            //Task.Delay(100000);

            return allorders;
        }

        public async Task<List<Order>> GetAllOpenOrders()
        {
            Logger.WriteLog("Getting all open orders from server");
            var requestEndPoint = string.Format(@"/orders?status=open&product_id={0}", ProductName);
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
