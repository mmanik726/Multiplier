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
using System.Reflection;
//using CoinbaseExchange.NET.Data;

namespace Simulator
{
    class Program
    {






        static void Main(string[] args)
        {
            Logger.Logupdated += (object sender, LoggerEventArgs largs) => { Console.WriteLine(largs.LogMessage); };




            Simulator1.Start();
            

            //Simulator2.Start();

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


    public class IntervalRange
    {
        public int interval_min { get; set; }
        public int interval_max { get; set; }

        public int bigSmaLen_min { get; set; }
        public int bigSmaLen_max { get; set; }

        public int smallSmaLen_min { get; set; }
        public int smallSmaLen_max { get; set; }

        public int basePriceSmaLen_min { get; set; }
        public int basePriceSmaLen_max { get; set; }

        public int SignalLen_min { get; set; }
        public int SignalLen_max { get; set; }


        public void PrintRange()
        {
            foreach (var prop in typeof(IntervalRange).GetProperties())
            {
                Console.WriteLine("{0} = {1}", prop.Name, prop.GetValue(this, null));
            }
        }

        //public static IntervalRange operator / (IntervalRange interval, int a)
        //{
        //    var mid = interval.interval_max - interval.interval_min;
        //    var newMin_Interval = mid - (mid / a);
        //    var newMax_Interval = mid + (mid / a);

        //    mid = interval.bigSmaLen_max - interval.bigSmaLen_min;
        //    var newBigsmaMin = mid - (mid / a);
        //    var newBigsmaMax = mid + (mid / a);

        //    mid = interval.smallSmaLen_max - interval.smallSmaLen_min;
        //    var newSmallSmaLen_min = mid - (mid / a);
        //    var newSmallSmaLen_max = mid + (mid / a);

        //    mid = interval.SignalLen_max - interval.SignalLen_min;
        //    var newSignalLen_min = mid - (mid / a);
        //    var newSignalLen_max = mid + (mid / a);


        //    return new IntervalRange
        //    {

        //        interval_min = newMin_Interval,
        //        interval_max = newMax_Interval,

        //        bigSmaLen_min = newBigsmaMin,
        //        bigSmaLen_max = newBigsmaMax,

        //        smallSmaLen_min = newSmallSmaLen_min,
        //        smallSmaLen_max = newSmallSmaLen_max,

        //        SignalLen_min = newSignalLen_min,
        //        SignalLen_max = newSignalLen_max
        //    };
        //}


        public static IntervalRange GetaHalfRange(IntervalRange inputRange)
        {
            var mid = inputRange.interval_max - inputRange.interval_min;
            var newMin_Interval = mid - (mid / 2);
            var newMax_Interval = mid + (mid / 2);

            mid = inputRange.bigSmaLen_max - inputRange.bigSmaLen_min;
            var newBigsmaMin = mid - (mid / 2);
            var newBigsmaMax = mid + (mid / 2);

            mid = inputRange.smallSmaLen_max - inputRange.smallSmaLen_min;
            var newSmallSmaLen_min = mid - (mid / 2);
            var newSmallSmaLen_max = mid + (mid / 2);

            mid = inputRange.SignalLen_max - inputRange.SignalLen_min;
            var newSignalLen_min = mid - (mid / 2);
            var newSignalLen_max = mid + (mid / 2);


            return new IntervalRange
            {

                interval_min = newMin_Interval,
                interval_max = newMax_Interval,

                bigSmaLen_min = newBigsmaMin,
                bigSmaLen_max = newBigsmaMax,

                smallSmaLen_min = newSmallSmaLen_min,
                smallSmaLen_max = newSmallSmaLen_max,

                SignalLen_min = newSignalLen_min,
                SignalLen_max = newSignalLen_max
            };
        }




        public static IntervalRange GetaHalfRange(IntervalRange inputCurRange, IntervalData inputCurInterval)
        {
            var mid = (inputCurRange.interval_max - inputCurRange.interval_min) / 2;
            var curBest = inputCurInterval.interval;
            var newMin_Interval = curBest - (mid / 2);
            var newMax_Interval = curBest + (mid / 2);

            mid = (inputCurRange.bigSmaLen_max - inputCurRange.bigSmaLen_min) / 2;
            curBest = inputCurInterval.bigSmaLen;
            var newBigsmaMin = curBest - (mid / 2);
            var newBigsmaMax = curBest + (mid / 2);

            mid = (inputCurRange.smallSmaLen_max - inputCurRange.smallSmaLen_min) / 2;
            curBest = inputCurInterval.smallSmaLen;
            var newSmallSmaLen_min = curBest - (mid / 2);
            var newSmallSmaLen_max = curBest + (mid / 2);

            mid = (inputCurRange.SignalLen_max - inputCurRange.SignalLen_min) / 2;
            curBest = inputCurInterval.SignalLen;
            var newSignalLen_min = curBest - (mid / 2);
            var newSignalLen_max = curBest + (mid / 2);


            return new IntervalRange
            {

                interval_min = newMin_Interval,
                interval_max = newMax_Interval,

                bigSmaLen_min = newBigsmaMin,
                bigSmaLen_max = newBigsmaMax,

                smallSmaLen_min = newSmallSmaLen_min,
                smallSmaLen_max = newSmallSmaLen_max,

                SignalLen_min = newSignalLen_min,
                SignalLen_max = newSignalLen_max
            };
        }


    }

    public class ResultData
    {
        public double Pl { get; set; }

        public DateTime SimStartDate { get; set; }

        public DateTime SimEndDate { get; set; }

        public IntervalData intervals { get; set; }

        public IntervalRange intervalRange { get; set; }
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
