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

        private static bool isBusyUpdatingMA;
        private DateTime firstDataPointDateTime;
        private DateTime lastDataPointDateTime;

        private List<CandleData> sharedRawExchangeData; 

        public TickerClient TickerPriceClient { get; set; }

        private List<CandleData> MADataPoints;

        public EventHandler<MAUpdateEventArgs> MovingAverageUpdated;

        public decimal CurrentSMAPrice;
        public decimal currentSandardDeviation ;

        public static System.Timers.Timer aTimer; 

        public MovingAverage(ref TickerClient tickerClient, int timeInterValInMin = 3, int smaSlices = 40)
        {
            //var a = tickerClient.CurrentPrice;

            isBusyUpdatingMA = false;

            sharedRawExchangeData = new List<CandleData>();

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
            
            NotifyListener(new MAUpdateEventArgs { CurrentMAPrice = CurrentSMAPrice, CurrentSd = Math.Round(currentSandardDeviation,3)});

            

        }

        async Task<List<CandleData>> getMaData()
        {
            var z = await Task.Factory.StartNew(() => GetSmaData());
            return z.Result;
        }

        public async void updateValues(int timeIntInMin = 3, int newSlices = 40)
        {


            if ((timeIntInMin * newSlices > 200) && sharedRawExchangeData.Count() < 500)
            {
                await DownloadAdditionalData();
            }

            TIME_INTERVAL = timeIntInMin;
            SLICES = newSlices;

            Init(timeIntInMin);

        }

        private void UpdateSMA(object sender, System.Timers.ElapsedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Updating SMA");

            //const int SLICES = 40;

            isBusyUpdatingMA = true;

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

            isBusyUpdatingMA = false;

            //updateOccured(CurrentSMA, NotifyListener);

            NotifyListener(new MAUpdateEventArgs
            {
                CurrentMAPrice = CurrentSMAPrice,
                CurrentSd = Math.Round(Convert.ToDecimal(sdDouble), 3)
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

        public async Task<bool> DownloadAdditionalData()
        {

            while (isBusyUpdatingMA)
                Console.WriteLine("Waiting for sma update to finish before dowanloading additional data...");

            System.Diagnostics.Debug.WriteLine("Additional data is being downloaded " + DateTime.UtcNow.ToString());

            int days = 5; //36 hours data in 1 min interval in total

            HistoricPrices historicData = new HistoricPrices();

            var startDt = firstDataPointDateTime.AddMinutes(-1).AddHours(-6);
            var endDt = firstDataPointDateTime.AddMinutes(-1);

            List<CandleData> extraData = new List<CandleData>();

            for (int i = 0; i < days; i++)
            {
                //var x = Task.Delay(200);

                System.Threading.Thread.Sleep(200);
                var temp = await historicData.GetPrices(
                    product: "LTC-USD",
                    granularity: "60",
                    startTime: startDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    endTime: endDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));

                startDt = temp.Last().Time.AddMinutes(-1).AddHours(-6);
                endDt = temp.Last().Time.AddMinutes(-1);

                extraData.AddRange(temp);
            }

            sharedRawExchangeData.AddRange(extraData);


            //sharedRawExchangeData.ForEach((d) => System.Diagnostics.Debug.WriteLine(d.Time + "\t" + d.Close));

            //sharedRawExchangeData = sharedRawExchangeData.OrderByDescending((d)=>d.Time).ToList();

            System.Diagnostics.Debug.WriteLine("done downloading additional data "+ DateTime.UtcNow.ToLocalTime());

            return true;
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


            if (sharedRawExchangeData.Count() == 0) //download initial data if there is no data already
            {
                HistoricPrices historicData = new HistoricPrices();
                var temp = await historicData.GetPrices(product: "LTC-USD", granularity: "60");
                sharedRawExchangeData = temp.ToList();
            }


            lastDataPointDateTime = sharedRawExchangeData.First().Time;
            firstDataPointDateTime = sharedRawExchangeData.Last().Time;

            //exchangeData.ToList().ForEach((a) => System.Diagnostics.Debug.WriteLine(a.Time + "\t" + a.Close));

            var intervalData = sharedRawExchangeData.Where((candleData, i) => i % TIME_INTERVAL == 0).ToList();// select every third item in list ie select data from every x min 

            var takeCount = intervalData.Count - (intervalData.Count() % SLICES);
            var requiredIntervalData = intervalData.Take(takeCount).ToList();


            List<CandleData> sma = new List<CandleData>();


            var groupCount = requiredIntervalData.Count() / SLICES;

            //requiredIntervalData.ForEach((a) => System.Diagnostics.Debug.WriteLine(a.Time + "\t"+ a.Close));

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

            var maDataList = requiredIntervalData.Take(SLICES).OrderBy((c)=>c.Time).ToList();

            List<double> itemsInSliceDbl = maDataList.Select(candle => (double)candle.Close).ToList();
            var sdDouble = CalculateStdDev(itemsInSliceDbl);

            currentSandardDeviation = Convert.ToDecimal(sdDouble);

            //return sma;
            return maDataList; 
        }


    }
}
