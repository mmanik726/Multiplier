using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Endpoints.PublicData;


namespace CoinbaseExchange.NET.Data
{

    public static class MovingAverageExtensions
    {
        public static IEnumerable<double> SMA(this List<double> source, int sampleLength)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (sampleLength <= 0) throw new ArgumentException("Invalid sample length");

            return SimpleMovingAverageImpl(source, sampleLength);
        }

        private static IEnumerable<double> SimpleMovingAverageImpl(List<double> source, int sampleLength)
        {
            Queue<double> sample = new Queue<double>(sampleLength);

            foreach (var d in source)
            {
                if (sample.Count == sampleLength)
                {
                    sample.Dequeue();
                }
                sample.Enqueue(d);

                if (sample.Count == sampleLength)
                    yield return sample.Average();

            }
        }




        public static IEnumerable<double> EMA(this List<double> source, int sampleLength)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (sampleLength <= 0) throw new ArgumentException("Invalid sample length");

            

            return ExponentialMovingAverageImpl(source, sampleLength);
        }

        private static IEnumerable<double> ExponentialMovingAverageImpl(List<double> source, int length)
        {
            Queue<double> sample = new Queue<double>(length);


            double multipler = Math.Round(2d / (length + 1d),2);

            foreach (var d in source)
            {
                var lastEma = 0d;
                if (sample.Count == length)
                {
                    lastEma = sample.Average();
                    sample.Dequeue();
                }
                
                //if (sample.Count() > 0)
                //    lastEma = sample.Average();

                sample.Enqueue(d);




                if (sample.Count == length)
                {
                    double emaResult;

                    if (lastEma == 0)
                        emaResult = sample.Average();
                    else
                        emaResult = multipler * (d - lastEma) + lastEma;

                    System.Diagnostics.Debug.Print(d.ToString() + "\t" + emaResult.ToString());

                    //yield returns an intermediate value and saves to final result incrementally 
                    yield return emaResult;

                }

            }
        }



    }


    //public static class SimpleMovingAverageExtensions
    //{
    //    public static IEnumerable<double> SimpleMovingAverage(this List<CandleData> source, int sampleLength)
    //    {
    //        if (source == null) throw new ArgumentNullException("source");
    //        if (sampleLength <= 0) throw new ArgumentException("Invalid sample length");

    //        return SimpleMovingAverageImpl(source, sampleLength);
    //    }

    //    private static IEnumerable<double> SimpleMovingAverageImpl(List<CandleData> source, int sampleLength)
    //    {
    //        Queue<double> sample = new Queue<double>(sampleLength);

    //        foreach (var d in source)
    //        {
    //            if (sample.Count == sampleLength)
    //            {
    //                sample.Dequeue();
    //            }
    //            sample.Enqueue((double)d.Close);
    //            yield return sample.Average();
    //        }
    //    }

    //}


}
