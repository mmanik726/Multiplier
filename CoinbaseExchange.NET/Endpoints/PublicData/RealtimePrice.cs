using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PublicData
{

    public class TickerRequest : ExchangeRequestBase
    {
        public TickerRequest(string product) : base("GET")
        {
            if (String.IsNullOrWhiteSpace(product))
                throw new ArgumentNullException("product");

            var urlFormat = String.Format("/products/{0}/ticker", product);
            this.RequestUrl = urlFormat;
        }
    }


    class RealtimePrice : ExchangeClientBase
    {

        public async Task<decimal> GetRealtimePrice(string product)
        {
            if (String.IsNullOrWhiteSpace(product))
                throw new ArgumentNullException("product");

            TickerRequest tickerRequest = new TickerRequest(product);

            ExchangeResponse response = await this.GetResponse(tickerRequest);

            decimal price = 0;

            if (response.IsSuccessStatusCode)
            {
                var jToken = JObject.Parse(response.ContentBody);
                var tokenPrice = jToken["price"];
                if (tokenPrice == null) return price;
                price = tokenPrice.Value<decimal>();

            }

            return price;
        }


    }


}
