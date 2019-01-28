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



            //Simulator4.Start();

            Simulator1.Start();

            //Simulator2.Start();

            //Simulator3.Start();

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

        public DateTime SimStartDate { get; set; }
        public DateTime SimEndDate { get; set; }
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
            var msg = "";

            foreach (var prop in typeof(IntervalRange).GetProperties())
            {
                msg += string.Format("{0} = {1} \n", prop.Name, prop.GetValue(this, null));
            }

            Logger.WriteLog("\n" + msg);
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



        public static IntervalRange GetaHalfRange(List<ResultData> topResultList)
        {


            int BUFFER = 5;
            int TAKE_COUNT = 10;

            int temp = 0;
            var TopResultsTemp = topResultList.Take(TAKE_COUNT).ToList();


            temp = (int)TopResultsTemp.Select(a => a.intervals.interval).Average();
            var newMin_Interval = temp - BUFFER;
            var newMax_Interval = temp + BUFFER;


            temp = (int)TopResultsTemp.Select(a => a.intervals.bigSmaLen).Average();
            var newBigsmaMin = temp - BUFFER;
            var newBigsmaMax = temp + BUFFER;

            temp = (int)TopResultsTemp.Select(a => a.intervals.smallSmaLen).Average();
            var newSmallSmaLen_min = temp - BUFFER;
            var newSmallSmaLen_max = temp + BUFFER;

            temp = (int)TopResultsTemp.Select(a => a.intervals.SignalLen).Average();
            var newSignalLen_min = temp - BUFFER;
            var newSignalLen_max = temp + BUFFER;


            return new IntervalRange
            {

                interval_min = (newMin_Interval < 1) ? 1 : newMin_Interval,
                interval_max = (newMax_Interval < 1) ? 1 : newMax_Interval,

                bigSmaLen_min = (newBigsmaMin < 1) ? 1 : newBigsmaMin,
                bigSmaLen_max = (newBigsmaMax < 1) ? 1 : newBigsmaMax,

                smallSmaLen_min = (newSmallSmaLen_min < 1) ? 1 : newSmallSmaLen_min,
                smallSmaLen_max = (newSmallSmaLen_max < 1) ? 1 : newSmallSmaLen_max,

                SignalLen_min = (newSignalLen_min < 1) ? 1 : newSignalLen_min,
                SignalLen_max = (newSignalLen_max < 1) ? 1 : newSignalLen_max
            };
        }


        public static IntervalRange GetaHalfRange2(List<ResultData> topResultList)
        {

            //topResultList = topResultList.OrderBy(r => r.Pl).ToList();

            int constBuffer = 5; 

            var newMin_Interval = topResultList.Select(a => a.intervals.interval).Min() - constBuffer;
            var newMax_Interval = topResultList.Select(a => a.intervals.interval).Max() + constBuffer;


            var newBigsmaMin = topResultList.Select(a => a.intervals.bigSmaLen).Min() - constBuffer;
            var newBigsmaMax = topResultList.Select(a => a.intervals.bigSmaLen).Max() + constBuffer;


            var newSmallSmaLen_min = topResultList.Select(a => a.intervals.smallSmaLen).Min() - constBuffer;
            var newSmallSmaLen_max = topResultList.Select(a => a.intervals.smallSmaLen).Max() + constBuffer;


            var newSignalLen_min = topResultList.Select(a => a.intervals.SignalLen).Min() - constBuffer;
            var newSignalLen_max = topResultList.Select(a => a.intervals.SignalLen).Max() + constBuffer;


            return new IntervalRange
            {

                interval_min = (newMin_Interval < 1) ? 1 : newMin_Interval,
                interval_max = (newMax_Interval < 1) ? 1 : newMax_Interval,

                bigSmaLen_min = (newBigsmaMin < 1) ? 1 : newBigsmaMin,
                bigSmaLen_max = (newBigsmaMax < 1) ? 1 : newBigsmaMax,

                smallSmaLen_min = (newSmallSmaLen_min < 1) ? 1 : newSmallSmaLen_min,
                smallSmaLen_max = (newSmallSmaLen_max < 1) ? 1 : newSmallSmaLen_max,

                SignalLen_min = (newSignalLen_min < 1) ? 1 : newSignalLen_min,
                SignalLen_max = (newSignalLen_max < 1) ? 1 : newSignalLen_max
            };
        }




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

                interval_min = (newMin_Interval < 1)? 1 : newMin_Interval,
                interval_max = (newMax_Interval < 1) ? 1 : newMax_Interval,

                bigSmaLen_min = (newBigsmaMin < 1) ? 1 : newBigsmaMin,
                bigSmaLen_max = (newBigsmaMax < 1) ? 1 : newBigsmaMax,

                smallSmaLen_min = (newSmallSmaLen_min < 1) ? 1 : newSmallSmaLen_min,
                smallSmaLen_max = (newSmallSmaLen_max < 1) ? 1 : newSmallSmaLen_max,

                SignalLen_min = (newSignalLen_min < 1) ? 1 : newSignalLen_min,
                SignalLen_max = (newSignalLen_max < 1) ? 1 : newSignalLen_max
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
                interval_min = (newMin_Interval < 1) ? 1 : newMin_Interval,
                interval_max = (newMax_Interval < 1) ? 1 : newMax_Interval,

                bigSmaLen_min = (newBigsmaMin < 1) ? 1 : newBigsmaMin,
                bigSmaLen_max = (newBigsmaMax < 1) ? 1 : newBigsmaMax,

                smallSmaLen_min = (newSmallSmaLen_min < 1) ? 1 : newSmallSmaLen_min,
                smallSmaLen_max = (newSmallSmaLen_max < 1) ? 1 : newSmallSmaLen_max,

                SignalLen_min = (newSignalLen_min < 1) ? 1 : newSignalLen_min,
                SignalLen_max = (newSignalLen_max < 1) ? 1 : newSignalLen_max

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
        public List<SmaData> DataPoints { get; set; }
        public string SereiesName { get; set; }
    }






}
