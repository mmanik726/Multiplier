using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CoinbaseExchange.NET.Core;
using CoinbaseExchange.NET.Endpoints.OrderBook;
using CoinbaseExchange.NET.Utilities;

namespace CoinbaseExchange.NET.Endpoints.PublicData
{

    public class TickerMessage : EventArgs
    {
        public decimal RealTimePrice { get; }

        public TickerMessage(decimal price)
        {
            RealTimePrice = price;
        }
    }


    public class TickerClient : ExchangeClientBase
    {
        public EventHandler PriceUpdated;
        public decimal CurrentPrice;
        public static bool isTryingToReconnect;

        public static bool TickerClientConnected;

        public EventHandler TickerDisconnectedEvent;
        public EventHandler TickerConnectedEvent;

        private static bool tickerDisconnectNotified;
        private static bool tickerConnectedNotified;  

        public TickerClient(string ProductName) : base()
        {
            //OnUpdated();
            isTryingToReconnect = false;
            TickerClientConnected = false;

            tickerConnectedNotified = false;
            tickerDisconnectNotified = false;


            //updateLatestPrice(ProductName);

            updateLatestPrice(ProductName).Wait(); //wait till result 

            while (!TickerClientConnected)
            {
                updateLatestPrice(ProductName).Wait();

                Thread.Sleep(5 * 1000);

                if (!TickerClientConnected)
                {
                    Logger.WriteLog("Ticker client not ready / cannot connect to server... retrying in 5 sec");
                }
            }

            TickerConnectedEvent?.Invoke(this, EventArgs.Empty);
            tickerConnectedNotified = true;

            Subscribe(ProductName, onTickerUpdateReceived);
        }


        private async Task<bool> updateLatestPrice(string product)
        {

            //error: stops here when http erros including no internet
            decimal price = 0;

            try
            {
                RealtimePrice curPrice = new RealtimePrice();
                price = await curPrice.GetRealtimePrice(product);
                TickerClientConnected = true;
                CurrentPrice = price;
                NotifyListener(new TickerMessage(price));
                return true;
            }
            catch (Exception ex)
            {
                TickerClientConnected = false;
                Logger.WriteLog("Error updating latest price: " + ex.Message);
                return false;
                //throw ex; //new Exception("LatestPriceError");
            }



        }


        private void NotifyListener(TickerMessage message)
        {

            if (PriceUpdated != null)
                PriceUpdated(this, message);
        }

        private void onTickerUpdateReceived(RealtimeMessage message)
        {
            

            if (message is RealtimeMatch)
            {
                CurrentPrice = message.Price;
                TickerMessage priceData = new TickerMessage(message.Price);
                NotifyListener(priceData);
            }

        }


        private  async void Subscribe(string product, Action<RealtimeMessage> onMessageReceived)
        {
            if (String.IsNullOrWhiteSpace(product))
                throw new ArgumentNullException("product");

            if (onMessageReceived == null)
                throw new ArgumentNullException("onMessageReceived", "Message received callback must not be null.");
            JArray aj = new JArray();





            var uri = new Uri("wss://ws-feed.gdax.com");
            var webSocketClient = new ClientWebSocket();
            var cancellationToken = new CancellationToken();

            //jStr.Append()
            var requestString = string.Format("");


            JObject jObj = new JObject();
            jObj.Add(new JProperty("type", "subscribe"));
            jObj.Add(new JProperty("product_ids", new JArray(product)));
            jObj.Add(new JProperty("channels", new JArray("matches")));

            //Console.WriteLine(jObj.ToString());



            try
            {
                var requestBytes = UTF8Encoding.UTF8.GetBytes(jObj.ToString());
                await webSocketClient.ConnectAsync(uri, cancellationToken);

                if (webSocketClient.State == WebSocketState.Open)
                {
                    Logger.WriteLog("Websocket connected");
                    var subscribeRequest = new ArraySegment<byte>(requestBytes);
                    var sendCancellationToken = new CancellationToken();
                    await webSocketClient.SendAsync(subscribeRequest, WebSocketMessageType.Text, true, sendCancellationToken);

                    if (!tickerConnectedNotified)
                    {
                        tickerConnectedNotified = true;
                        TickerConnectedEvent?.Invoke(null, EventArgs.Empty);
                    }
                    TickerClientConnected = true;
                    tickerDisconnectNotified = false;

                    while (webSocketClient.State == WebSocketState.Open)
                    {
                        var receiveCancellationToken = new CancellationToken();
                        var receiveBuffer = new ArraySegment<byte>(new byte[1024 * 1024 * 1]); // 5MB buffer 1024 * 1024 * 5
                        var webSocketReceiveResult = await webSocketClient.ReceiveAsync(receiveBuffer, receiveCancellationToken);
                        if (webSocketReceiveResult.Count == 0) continue;

                        var jsonResponse = Encoding.UTF8.GetString(receiveBuffer.Array, 0, webSocketReceiveResult.Count);
                        //var jToken = JToken.Parse(jsonResponse);
                        var jToken = JObject.Parse(jsonResponse);

                        var typeToken = jToken["type"];
                        if (typeToken == null) continue;

                        var type = typeToken.Value<string>();
                        RealtimeMessage realtimeMessage = null;

                        //Console.WriteLine("MSG TYPE: {0}", type);

                        //look for only match messages

                        if (type == "match")
                        {
                            realtimeMessage = new RealtimeMatch(jToken);
                        }


                        if (realtimeMessage == null)
                            continue;



                        onMessageReceived(realtimeMessage);
                    }


                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Error in ticker websocket: " + ex.Message);
                //webSocketClient.Abort();
                webSocketClient = null;

                TickerClientConnected = false;

                Reconnect(product, onMessageReceived);

                tickerConnectedNotified = false;
                if (!tickerDisconnectNotified)
                {
                    tickerDisconnectNotified = true;
                    TickerDisconnectedEvent?.Invoke(null,EventArgs.Empty);
                }
            }

            Reconnect(product, onMessageReceived);


        }

        private  void Reconnect(string prodName, Action<RealtimeMessage> onMessageReceived)
        {

            if (isTryingToReconnect)
            {
                return;
            }

            isTryingToReconnect = true;

            Logger.WriteLog(string.Format("websocket ticker feed closed or cannot connect, retrying in 5 sec"));

            System.Threading.Thread.Sleep(5 * 1000);
            //Task.Delay(10 * 1000);

            Subscribe(prodName, onMessageReceived);

            isTryingToReconnect = false;

        }
    }
}
