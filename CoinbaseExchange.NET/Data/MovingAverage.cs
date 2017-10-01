using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Endpoints.PublicData;

namespace CoinbaseExchange.NET.Data
{

    public class MAUpdateEventArgs : EventArgs
    {
        public decimal CurrentMAPrice { get; set; }
        public decimal CurrentSd { get; set; }
    }


    public class MovingAverage
    {
        private int SLICES;
        private int TIME_INTERVAL;

        public TickerClient TickerPriceClient { get; set; }

        private List<CandleData> MADataPoints;

        public EventHandler<MAUpdateEventArgs> MovingAverageUpdated;

        public decimal CurrentSMAPrice;
        public decimal currentSandardDeviation ;

        public static System.Timers.Timer aTimer; 

        public MovingAverage(ref TickerClient tickerClient, int timeInterValInMin = 3, int smaSlices = 40)
        {
            //var a = tickerClient.CurrentPrice;

            TIME_INTERVAL = timeInterValInMin;

            SLICES = smaSlices;

            TickerPriceClient = tickerClient;

            Init(timeInterValInMin);


        }

        async void Init(int updateInterval)
        {
            try
            {
                MADataPoints = await getMaData();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error in sma calculation: " + ex.Message);
                throw new Exception("SMAInitError");
            }

            if(aTimer != null) //timer already in place
            {
                aTimer.Elapsed -= UpdateSMA;
                aTimer.Stop();
                aTimer = null;
            }

            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += UpdateSMA;
            aTimer.Interval = updateInterval * 60 * 1000;
            aTimer.Enabled = true;
            aTimer.Start();
            
            NotifyListener(new MAUpdateEventArgs { CurrentMAPrice = CurrentSMAPrice, CurrentSd = currentSandardDeviation });
        }

        async Task<List<CandleData>> getMaData()
        {
            var z = await Task.Factory.StartNew(() => GetSmaData());
            return z.Result;
        }

        public void updateValues(int timeIntInMin = 3, int newSlices = 40)
        {

            TIME_INTERVAL = timeIntInMin;
            SLICES = newSlices;

            Init(timeIntInMin);

        }

        private void UpdateSMA(object sender, System.Timers.ElapsedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Updating SMA");

            //const int SLICES = 40;

            MADataPoints.RemoveAt(0);
            MADataPoints.Add(new CandleData { Close = TickerPriceClient.CurrentPrice, Time = DateTime.UtcNow.ToLocalTime()});

            var itemsInSlice = MADataPoints.Take(SLICES);
            itemsInSlice.ToList().ForEach((t) => System.Diagnostics.Debug.WriteLine(t.Time + "\t" + t.Close));
            var itemsInSLiceAvg = itemsInSlice.Average((d) => d.Close);

            //MADataPoints.ForEach((t) => System.Diagnostics.Debug.WriteLine(t.Time + "\t" + t.Close));

            CurrentSMAPrice = itemsInSLiceAvg;


            //itemsInSlice.ToList().ForEach((d) => { Math.Pow((Convert.ToDouble(d.Close) - Convert.ToDouble(itemsInSLiceAvg)), 2); });

            List<double> itemsInSliceDbl = itemsInSlice.Select(candle => (double)candle.Close).ToList();
            var sdDouble = CalculateStdDev(itemsInSliceDbl);

            //updateOccured(CurrentSMA, NotifyListener);

            NotifyListener(new MAUpdateEventArgs
            {
                CurrentMAPrice = CurrentSMAPrice,
                CurrentSd = Convert.ToDecimal(sdDouble)
            });

        }

        //void updateOccured(decimal price,  Action<MAUpdateEventArgs> runMethod)
        //{
        //    var arg = new MAUpdateEventArgs { CurrentMAPrice = price }; 
        //    runMethod(arg);
        //}
        private double CalculateStdDev(IEnumerable<double> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average      
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return ret;
        }

        private void NotifyListener(MAUpdateEventArgs args)
        {
            if (MovingAverageUpdated != null)
                MovingAverageUpdated(this, args);

        }

        public async Task<List<CandleData>> GetSmaData()
        {
            //const int TIME_INTERVAL = 3;
            //const int SLICES = 40;

            HistoricPrices historicData = new HistoricPrices();
            var exchangeData = await historicData.GetPrices(product: "LTC-USD", granularity: "60");

            var intervalData = exchangeData.Where((candleData, i) => i % TIME_INTERVAL == 0).ToList();// select every third item in list ie select data from every 3 min 

            var takeCount = intervalData.Count - (intervalData.Count() % SLICES);
            var requiredIntervalData = intervalData.Take(takeCount).ToList();


            List<CandleData> sma = new List<CandleData>();


            var groupCount = requiredIntervalData.Count() / SLICES;

            

            for (int i = 0; i < groupCount - 1; i++)
            {
                var itemsInSlice = requiredIntervalData.Skip(i * SLICES).Take(SLICES);
                //itemsInSlice.ToList().ForEach((t) => Debug.WriteLine(t.Time + "\t" + t.Close));
                var itemsInSLiceAvg = itemsInSlice.Average((d) => d.Close);
                //Debug.WriteLine("slice avg: " + itemsInSLiceAvg.ToString());
                sma.Add(new CandleData { Time = itemsInSlice.First().Time, Close = itemsInSLiceAvg });
            }

            ////exchangeData.OrderBy(w => w.Time).ToList().ForEach((l) => Debug.WriteLine(l.Time + "\t" + l.Close));
            //Debug.Write("\n\n");

            //requiredIntervalData.ForEach((l) => Debug.WriteLine(l.Time + "\t" + l.Close));
            //Debug.Write("\n\n");

            //sma.ForEach((l) => Debug.WriteLine(l.Time + "\t" + l.Close));

            CurrentSMAPrice = sma.First().Close;


            //List<CandleData> priceDataPoints = new List<CandleData>();

            var lst = requiredIntervalData.Take(SLICES).OrderBy((c)=>c.Time).ToList();

            List<double> itemsInSliceDbl = lst.Select(candle => (double)candle.Close).ToList();
            var sdDouble = CalculateStdDev(itemsInSliceDbl);

            currentSandardDeviation = Convert.ToDecimal(sdDouble);

            //return sma;
            return lst; 
        }


    }
}
