using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Endpoints.PublicData;
using CoinbaseExchange.NET.Utilities;

namespace CoinbaseExchange.NET.Data
{

    public class MAUpdateEventArgs : EventArgs
    {
        public decimal CurrentMAPrice { get; set; }
        public int CurrentSlices { get; set; }
        public decimal CurrentSd { get; set; }
        public int CurrentTimeInterval { get; set; }
    }


    public class MovingAverage
    {
        private int SLICES;
        private int TIME_INTERVAL;

        private string Product;

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

        //private bool ForceRedownloadData;


        private static bool isBusyCalculatingSMA; 

        public MovingAverage(ref TickerClient tickerClient, string ProductName, int timeInterValInMin = 3, int smaSlices = 40)
        {
            //var a = tickerClient.CurrentPrice;

            //Logger.WriteLog("Starting moving avg calculations");
            isBusyCalculatingSMA = false;

            Product = ProductName;

            isBusyUpdatingMA = false;

            //ForceRedownloadData = false;

            sharedRawExchangeData = new List<CandleData>();

            TIME_INTERVAL = timeInterValInMin;

            SLICES = smaSlices;

            TickerPriceClient = tickerClient;

            Init(timeInterValInMin);


        }

        async void Init(int updateInterval)
        {

            if (isBusyCalculatingSMA)
            {
                return;
            }

            isBusyCalculatingSMA = true;

            
            try
            {
                MADataPoints = await getMaData();
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(1000);
                Logger.WriteLog("Error in sma calculation, retrying in 1 sec... : " + ex.Message + " innerExMsg: " + ex.InnerException.Message);
                isBusyCalculatingSMA = false;
                Init(updateInterval); // retry
                return;
                //throw new Exception("SMAInitError");
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

            NotifyListener(new MAUpdateEventArgs
            {
                CurrentMAPrice = CurrentSMAPrice,
                CurrentSd = Math.Round(currentSandardDeviation, 4),
                CurrentSlices = SLICES,
                CurrentTimeInterval = TIME_INTERVAL
            });

            isBusyCalculatingSMA = false;

        }

        async Task<List<CandleData>> getMaData()
        {
            var z = await Task.Factory.StartNew(() => GetSmaData());
            z.Wait();
            return z.Result;
        }

        public async Task<bool> updateValues(int timeIntInMin = 3, int newSlices = 40, bool forceRedownload = false)
        {

            //if (forceRedownload)
            //{
            //    ForceRedownloadData = true;
            //}

            TIME_INTERVAL = timeIntInMin;
            SLICES = newSlices;

            Init(timeIntInMin);

            return true; //done 
        }

        private void UpdateSMA(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Logger.WriteLog("Updating SMA");

            //const int SLICES = 40;
            if (isBusyUpdatingMA)
            {
                Logger.WriteLog("Already busy updating sma");
                return;
            }

            isBusyUpdatingMA = true;

            var maDtPtsRemoveIndex = MADataPoints.Count - 1;

            //var rawDtPtsRemoveIndex = sharedRawExchangeData.Count - 1;

            if (maDtPtsRemoveIndex != -1)
                MADataPoints.RemoveAt(maDtPtsRemoveIndex);

            //if (rawDtPtsRemoveIndex != -1)
            //    sharedRawExchangeData.RemoveAt(rawDtPtsRemoveIndex);

            var newDataPoint = new CandleData { Close = TickerPriceClient.CurrentPrice, Time = DateTime.UtcNow.ToLocalTime() };
            MADataPoints.Insert(0, newDataPoint); //insert at top

            //sharedRawExchangeData.Insert(0, newDataPoint);

            var itemsInSlice = MADataPoints.Take(SLICES); //MADataPoints.OrderBy((c) => c.Time).ToList().Take(SLICES);
            itemsInSlice.OrderBy((c) => c.Time).ToList().ForEach((t) => Logger.WriteLog(t.Time + "\t" + t.Close));
            var itemsInSLiceAvg = itemsInSlice.Average((d) => d.Close);

            //MADataPoints.ForEach((t) => Logger.WriteLog(t.Time + "\t" + t.Close));

            CurrentSMAPrice = itemsInSLiceAvg;


            //itemsInSlice.ToList().ForEach((d) => { Math.Pow((Convert.ToDouble(d.Close) - Convert.ToDouble(itemsInSLiceAvg)), 2); });

            List<double> itemsInSliceDbl = itemsInSlice.Select(candle => (double)candle.Close).ToList();
            var sdDouble = CalculateStdDev(itemsInSliceDbl);

            isBusyUpdatingMA = false;

            //updateOccured(CurrentSMA, NotifyListener);


            NotifyListener(new MAUpdateEventArgs
            {
                CurrentMAPrice = CurrentSMAPrice,
                CurrentSd = Math.Round(currentSandardDeviation, 4),
                CurrentSlices = SLICES,
                CurrentTimeInterval = TIME_INTERVAL
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

            Logger.WriteLog("Additional data is being downloaded ");

            int days = 6; //36 hours data in 1 min interval in total

            HistoricPrices historicData = new HistoricPrices();

            var startDt = firstDataPointDateTime.AddMinutes(-1).AddHours(-6);
            var endDt = firstDataPointDateTime.AddMinutes(-1);

            List<CandleData> extraData = new List<CandleData>();

            try
            {
                for (int i = 0; i < days; i++)
                {
                    //var x = Task.Delay(200);

                    System.Threading.Thread.Sleep(300);
                    var temp = await historicData.GetPrices(
                        product: Product,
                        granularity: "60",
                        startTime: startDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        endTime: endDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));

                    startDt = temp.Last().Time.AddMinutes(-1).AddHours(-6);
                    endDt = temp.Last().Time.AddMinutes(-1);

                    extraData.AddRange(temp);
                }
            }
            catch (Exception ex)
            {
                var msg = "Error downloading additional data: " + ex.Message;
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    msg = msg + "\n" + ex.Message;
                }
                Logger.WriteLog(msg);
                return false;
                throw ex;
            }



            sharedRawExchangeData.AddRange(extraData);


            //sharedRawExchangeData.ForEach((d) => Logger.WriteLog(d.Time + "\t" + d.Close));

            //sharedRawExchangeData = sharedRawExchangeData.OrderByDescending((d)=>d.Time).ToList();

            Logger.WriteLog("done downloading additional data ");

            return true;
        }

        private void NotifyListener(MAUpdateEventArgs args)
        {
            if (MovingAverageUpdated != null)
                MovingAverageUpdated(this, args);

        }

        private async Task<List<CandleData>> GetSmaData()
        {
            //const int TIME_INTERVAL = 3;
            //const int SLICES = 40;


            //if (sharedRawExchangeData.Count() == 0 || ForceRedownloadData) //download initial data if there is no data already
            //{
            //    HistoricPrices historicData = new HistoricPrices();
            //    var temp = await historicData.GetPrices(product: Product, granularity: "60");


            //    sharedRawExchangeData.Clear(); //clear existing data
            //    sharedRawExchangeData = temp.ToList();

            //    if (ForceRedownloadData)
            //        if (TIME_INTERVAL * SLICES > 200)
            //            await DownloadAdditionalData();

            //    ForceRedownloadData = false;
            //}



                HistoricPrices historicData = new HistoricPrices();
                var temp = await historicData.GetPrices(product: Product, granularity: "60");
            
                sharedRawExchangeData.Clear(); //clear existing data
                sharedRawExchangeData = temp.ToList();

                if (TIME_INTERVAL * SLICES > 200)
                    await DownloadAdditionalData();




            lastDataPointDateTime = sharedRawExchangeData.First().Time;
            firstDataPointDateTime = sharedRawExchangeData.Last().Time;

            //exchangeData.ToList().ForEach((a) => Logger.WriteLog(a.Time + "\t" + a.Close));

            var intervalData = sharedRawExchangeData.Where((candleData, i) => i % TIME_INTERVAL == 0).ToList();// select every third item in list ie select data from every x min 

            var takeCount = intervalData.Count - (intervalData.Count() % SLICES);
            var requiredIntervalData = intervalData.Take(takeCount).ToList();


            List<CandleData> sma = new List<CandleData>();


            var groupCount = requiredIntervalData.Count() / SLICES;

            //requiredIntervalData.ForEach((a) => Logger.WriteLog(a.Time + "\t"+ a.Close));

            //for (int i = 0; i < groupCount; i++)
            for (int i = 0; i <= groupCount - 1; i++)
            {
                var itemsInSlice = requiredIntervalData.Skip(i * SLICES).Take(SLICES);
                //itemsInSlice.ToList().ForEach((t) => Logger.WriteLog(t.Time + "\t" + t.Close));
                var itemsInSLiceAvg = itemsInSlice.Average((d) => d.Close);
                //Logger.WriteLog("slice avg: " + itemsInSLiceAvg.ToString());
                sma.Add(new CandleData { Time = itemsInSlice.First().Time, Close = itemsInSLiceAvg });
            }

            ////exchangeData.OrderBy(w => w.Time).ToList().ForEach((l) => Logger.WriteLog(l.Time + "\t" + l.Close));
            //Debug.Write("\n\n");

            //requiredIntervalData.ForEach((l) => Logger.WriteLog(l.Time + "\t" + l.Close));
            //Debug.Write("\n\n");

            //sma.ForEach((l) => Logger.WriteLog(l.Time + "\t" + l.Close));

            CurrentSMAPrice = sma.First().Close;


            //List<CandleData> priceDataPoints = new List<CandleData>();

            var maDataList = requiredIntervalData.OrderByDescending((c) => c.Time).Take(SLICES).ToList();

            List<double> itemsInSliceDbl = maDataList.Select(candle => (double)candle.Close).ToList();
            var sdDouble = CalculateStdDev(itemsInSliceDbl);

            currentSandardDeviation = Convert.ToDecimal(sdDouble);

            //return sma;
            return maDataList; 
        }


    }
}
