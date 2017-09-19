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
        public EventHandler Update;
        //public decimal CurrentPrice;


        public TickerClient(string ProductName) : base()
        {
            //OnUpdated();
            updateLatestPrice(ProductName);
            Subscribe(ProductName, onTickerUpdateReceived);
        }


        private async void updateLatestPrice(string product)
        {
            RealtimePrice curPrice = new RealtimePrice();
            var price = await curPrice.GetRealtimePrice(product);

            TickerMessage tickerMsg = new TickerMessage(price);
            NotifyListener(tickerMsg);
        }

        private void NotifyListener(TickerMessage message)
        {

            if (Update != null)
                Update(this, message);
        }

        private void onTickerUpdateReceived(RealtimeMessage message)
        {
            

            if (message is RealtimeMatch)
            {
                TickerMessage priceData = new TickerMessage(message.Price); ;
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

            //String.Format(@"{{""type"": ""subscribe"",""product_id"": ""{0}""}}", product);


            //JObject jObj = new JObject(
            //    new JProperty(
            //        "type", "subscribe"),
            //    new JProperty(
            //        "product_ids", new JArray(
            //        "BTC-USD")),
            //    new JProperty(
            //        "channels", new JArray(
            //        "level2", "heartbeat", new JObject(
            //            new JProperty(
            //                "name", "ticker"), new JProperty(
            //                    "product_ids", new JArray(
            //                        "BTC-USD"))))));

            //JObject jObj = new JObject(
            //    new JProperty(
            //        "type", "subscribe"),
            //    new JProperty(
            //        "product_ids", new JArray(
            //        "BTC-USD")),
            //    new JProperty(
            //        "channels", new JArray(
            //        "heartbeat", new JObject(
            //            new JProperty(
            //                "name", "ticker"), new JProperty(
            //                    "product_ids", new JArray(
            //                        "BTC-USD"))))));


            //JObject jObj = new JObject(
            //    new JProperty(
            //        "type", "subscribe"),
            //    new JProperty(
            //        "product_ids", new JArray(
            //        "BTC-USD")),
            //    new JProperty(
            //        "channels", new JArray(
            //        "matches", "heartbeat", new JObject(
            //            new JProperty(
            //                "name", "ticker"), new JProperty(
            //                    "product_ids", new JArray(
            //                        "BTC-USD"))))));


            JObject jObj = new JObject(
                new JProperty(
                    "type", "subscribe"),
                new JProperty(
                    "product_ids", new JArray(
                    "LTC-USD")),
                new JProperty(
                    "channels", new JArray(
                    "matches")));

            Console.WriteLine(jObj.ToString());

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

                    switch (type)
                    {
                        case "received":
                            realtimeMessage = new RealtimeReceived(jToken);
                            break;
                        case "open":
                            realtimeMessage = new RealtimeOpen(jToken);
                            break;
                        case "done":
                            realtimeMessage = new RealtimeDone(jToken);
                            break;
                        case "match":
                            realtimeMessage = new RealtimeMatch(jToken);
                            break;
                        case "change":
                            realtimeMessage = new RealtimeChange(jToken);
                            break;
                        default:
                            break;
                    }

                    if (realtimeMessage == null)
                        continue;

                    onMessageReceived(realtimeMessage);
                }
            }
        }


    }
}
