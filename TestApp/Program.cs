using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoinbaseExchange.NET.Core;
using CoinbaseExchange.NET.Endpoints.OrderBook;
namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {


            /*
             * 
            passphrase:
            kb4ou7pbmkk

            key:
            c8420d4178367af6929335a08da8ca90

            secret:
            zUF25y0GTGgpdR1LI2J4Pu6rjwQ/4RgqJVK03mR+T38ik+JyDwl1T3hI2p4GiED6Lm4KOUAgHnxl3+53ihRC4Q==
            */
            var myPassphrase = "";
            var myKey = "";
            var mySecret = "";

            CBAuthenticationContainer myAuth = new CBAuthenticationContainer(myKey, myPassphrase, mySecret);

            RealtimeOrderBookClient rtClient = new  RealtimeOrderBookClient(myAuth);

            rtClient.Updated += RtClient_Updated;

            //var book = rtClient.GetProductOrderBook("BTC-USD");


            //var res = rtClient.Buys;


            //Task.WaitAny();

            //System.Threading.Thread.Sleep(5000);

            while (true)
            {
                System.Threading.Thread.Sleep(60000);
                Console.WriteLine("Thread Sleeping...");
            }

        }

        private static void RtClient_Updated(object sender, EventArgs e)
        {
            var a = (RealtimeOrderBookClient)sender;
            //var z = a.Sells.FirstOrDefault();
            Console.WriteLine("Update: price {0}", a.CurrentPrice);
        }
    }
}
