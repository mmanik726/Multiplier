using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Core;
using CoinbaseExchange.NET.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoinbaseExchange.NET.Endpoints.Funds
{
    class FundsRequest : ExchangeRequestBase
    {
        public FundsRequest(): base("GET")
        {
            RequestUrl = "/accounts";
        }

    }


    public class Funds : ExchangeClientBase
    {

        public String ProductName { get; set; }


        public Funds(CBAuthenticationContainer auth,string productName) : base(auth)
        {

            if (productName.Contains("-"))
            {
                ProductName = productName.Split('-').First();
            }
            else
            {
                ProductName = productName;
            }
            
        }

        public AvailableFunds GetAvailableFunds()
        {

            ExchangeResponse response = null;
            try
            {
                response = GetResponse(new FundsRequest()).Result;   
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Error gettings funds response: " + ex.Message);
                throw new Exception("ErrorGettingFunds") ;
            }

            //sample response
            //[
            //    {
            //        "id": "71452118-efc7-4cc4-8780-a5e22d4baa53",
            //        "currency": "BTC",
            //        "balance": "0.0000000000000000",
            //        "available": "0.0000000000000000",
            //        "hold": "0.0000000000000000",
            //        "profile_id": "75da88c5-05bf-4f54-bc85-5c775bd68254"
            //    },
            //    {
            //        "id": "e316cb9a-0808-4fd7-8914-97829c1925de",
            //        "currency": "USD",
            //        "balance": "80.2301373066930000",
            //        "available": "79.2266348066930000",
            //        "hold": "1.0035025000000000",
            //        "profile_id": "75da88c5-05bf-4f54-bc85-5c775bd68254"
            //    }
            //]

            AvailableFunds available = null;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var rawJson = response.ContentBody;

                    var parsedJson = JArray.Parse(rawJson);

                    var usd = from ca in parsedJson
                              where ca["currency"].Value<String>() == "USD"
                              select ca["available"].Value<decimal>();

                    

                    var productAmount = from b in parsedJson
                                        where b["currency"].Value<String>() == ProductName
                                        select b["available"].Value<decimal>();


                    available = new AvailableFunds
                    {
                        AvailableDollars =  usd.First(),
                        AvailableProduct = productAmount.First()
                    };


                    return available;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error parsing available funds json");
                    throw new Exception("JsonParseErrorFunds");
                }
            }
            else
            {
                //return null
                return available;
            }
         
            
        }
    }


    public class AvailableFunds
    {
        public Decimal AvailableProduct { get; set; }
        public Decimal AvailableDollars { get; set; }

    }

}
