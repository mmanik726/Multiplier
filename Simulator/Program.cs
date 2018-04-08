using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoinbaseExchange.NET.Data;
using CoinbaseExchange.NET.Endpoints.PublicData;

using CoinbaseExchange.NET.Utilities;
using System.Threading;
using CoinbaseExchange.NET;

//using CoinbaseExchange.NET.Data;

namespace Simulator
{
    class Program
    {






        static void Main(string[] args)
        {
            //Logger.Logupdated += (object sender, LoggerEventArgs largs) => { Console.WriteLine(largs.LogMessage); };

            Simulator1.Start();

            //ManualSimulate();
                        
            //AutoSimulate();

            
        }









    }

    

    public class IntervalData
        {
            public int interval { get; set; }
            public int bigSmaLen { get; set; }
            public int smallSmaLen { get; set; }
            public int basePriceSmaLen { get; set; }
            public int SignalLen { get; set; }
            
        }

    public class ResultData
    {
        public double Pl { get; set; }

        public DateTime SimStartDate { get; set; }

        public DateTime SimEndDate { get; set; }

        public IntervalData intervals { get; set; }
    }

    public class SeriesDetails
    {
        public List<SmaData> series { get; set; }
        public string SereiesName { get; set; }
    }




    public class SmaData
    {
        public double SmaValue { get; set; }
        public decimal ActualPrice { get; set; }
        public DateTime Time { get; set; }
    }



}
