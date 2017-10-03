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

        public TickerClient(string ProductName) : base()
        {
            //OnUpdated();
            isTryingToReconnect = false;
            updateLatestPrice(ProductName);
            Subscribe(ProductName, onTickerUpdateReceived);
        }


        private async void updateLatestPrice(string product)
        {
            RealtimePrice curPrice = new RealtimePrice();
            //error: stops here when http erros including no internet
            var price = await curPrice.GetRealtimePrice(product);

            TickerMessage tickerMsg = new TickerMessage(price);
            NotifyListener(tickerMsg);
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


        private static async void Subscribe(string product, Action<RealtimeMessage> onMessageReceived)
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

            //JObject jObj = new JObject(
            //    new JProperty(
            //        "type", "subscribe"),
            //    new JProperty(
            //        "product_ids", new JArray(
            //        "LTC-USD")),
            //    new JProperty(
            //        "channels", new JArray(
            //        "matches")));

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
                    var subscribeRequest = new ArraySegment<byte>(requestBytes);
                    var sendCancellationToken = new CancellationToken();
                    await webSocketClient.SendAsync(subscribeRequest, WebSocketMessageType.Text, true, sendCancellationToken);


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
                System.Diagnostics.Debug.WriteLine("Error in ticker websocket: " + ex.Message);
                //webSocketClient.Abort();
                webSocketClient = null;
            }

            //if (isTryingToReconnect)
            //{
            //    return;
            //}

            isTryingToReconnect = true;

            System.Diagnostics.Debug.WriteLine(string.Format("websocket ticker feed closed, retrying in 10 sec {0}", DateTime.UtcNow.ToLocalTime().ToString()));

            Task.Delay(10 * 1000);

            Subscribe(product, onMessageReceived);

            isTryingToReconnect = false;
        }


    }
}
