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


        public static IEnumerable<SmaData> SMA_CD(this List<CandleData> source, int SmaInterval, int SmaLength)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (SmaLength <= 0) throw new ArgumentException("Invalid sample length");

            return SimpleMovingAverageImpl_CD(source, SmaInterval, SmaLength);
        }

        private static IEnumerable<SmaData> SimpleMovingAverageImpl_CD(IEnumerable<CandleData> source, int SmaInterval, int sampleLength)
        {
            var tempSourceCopy = source.Where((candleData, i) => i % SmaInterval == 0).ToList();
            Queue<CandleData> sample = new Queue<CandleData>(sampleLength);

            foreach (var d in tempSourceCopy)
            {
                if (sample.Count == sampleLength)
                {
                    sample.Dequeue();
                }
                sample.Enqueue(d);
                if (sample.Count == sampleLength)
                {
                    var lastItem = sample.Last();
                    var newSmaData = new SmaData {ActualPrice = lastItem.Close , Time = lastItem.Time };
                    newSmaData.SmaValue = (double)sample.Average(s => s.Close);
                    yield return newSmaData;
                }
                    

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
