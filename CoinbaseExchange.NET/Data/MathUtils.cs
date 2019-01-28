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


            double multipler = 2d / (length + 1d);

            var lastEma = double.MinValue;


            foreach (var curClose in source)
            {
                
                if (sample.Count == length)
                {
                    sample.Dequeue();
                }

                sample.Enqueue(curClose);


                if (sample.Count == length)
                {
                    double emaResult;

                    if (lastEma == double.MinValue)
                        emaResult = sample.Average();
                    else
                        emaResult = multipler * (curClose - lastEma) + lastEma;


                    //yield returns an intermediate value and saves to final result incrementally 
                    lastEma = emaResult;
                    yield return emaResult;

                }

            }
        }




        public static IEnumerable<SmaData> EMA_CD(this List<CandleData> source, int sampleLength)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (sampleLength <= 0) throw new ArgumentException("Invalid sample length");

            return ExponentialMovingAverageImpl_CD(source, sampleLength);
        }

        private static IEnumerable<SmaData> ExponentialMovingAverageImpl_CD(List<CandleData> source, int length)
        {
            Queue<CandleData> sample = new Queue<CandleData>(length);


            double multipler = 2d / (length + 1d);

            var lastEma = double.MinValue;


            foreach (var curCandle in source)
            {

                if (sample.Count == length)
                {
                    sample.Dequeue();
                }

                sample.Enqueue(curCandle);


                if (sample.Count == length)
                {
                    SmaData emaResult;

                    if (lastEma == double.MinValue)
                    {
                        var newSmaData = new SmaData { ActualPrice = curCandle.Close, Time = curCandle.Time };
                        newSmaData.SmaValue = (double)sample.Average(c => c.Close);
                        emaResult = newSmaData;

                    }
                    else
                    {
                        var newSmaData = new SmaData { ActualPrice = curCandle.Close, Time = curCandle.Time };
                        newSmaData.SmaValue = multipler * ((double)curCandle.Close - lastEma) + lastEma;
                        emaResult = newSmaData;

                    }


                    //yield returns an intermediate value and saves to final result incrementally 
                    lastEma = emaResult.SmaValue;
                    yield return emaResult;

                }

            }
        }



    }




}
