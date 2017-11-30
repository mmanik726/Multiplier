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
        public decimal CiBuffer { get; set; }
    }


    public class MovingAverage : IDisposable
    {
        private int SLICES;
        private int TIME_INTERVAL;

        private string Product;

        private bool isBusyUpdatingMA;
        private DateTime firstDataPointDateTime;
        private DateTime lastDataPointDateTime;

        private static List<CandleData> sharedRawExchangeData;
        private static bool isBusyDownloadingData;


        public TickerClient TickerPriceClient { get; set; }

        private List<CandleData> MADataPoints;

        public EventHandler<MAUpdateEventArgs> MovingAverageUpdatedEvent;

        public decimal CurrentSMAPrice;
        public decimal currentSandardDeviation ;

        public decimal CurrentConfidenceIntervalBuffer;

        public System.Timers.Timer aTimer;

        //private bool ForceRedownloadData;

        


        private bool isBusyCalculatingSMA; 

        public MovingAverage(ref TickerClient tickerClient, string ProductName, int timeInterValInMin = 3, int smaSlices = 40)
        {
            //var a = tickerClient.CurrentPrice;

            //Logger.WriteLog("Starting moving avg calculations");
            isBusyCalculatingSMA = false;

            Product = ProductName;

            isBusyUpdatingMA = false;

            //ForceRedownloadData = false;

            if (sharedRawExchangeData == null)
                sharedRawExchangeData = new List<CandleData>();

            TIME_INTERVAL = timeInterValInMin;

            SLICES = smaSlices;

            TickerPriceClient = tickerClient;

            Init(timeInterValInMin);
            //Task.Run(() => Init(timeInterValInMin)).Wait();


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
                //MADataPoints = await getMaData();
                MADataPoints = getMaData().Result;
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
                CiBuffer = Math.Round(CurrentConfidenceIntervalBuffer, 4),
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
            //itemsInSlice.OrderBy((c) => c.Time).ToList().ForEach((t) => Logger.WriteLog(t.Time + "\t" + t.Close));
            var itemsInSLiceAvg = itemsInSlice.Average((d) => d.Close);

            //MADataPoints.ForEach((t) => Logger.WriteLog(t.Time + "\t" + t.Close));

            CurrentSMAPrice = itemsInSLiceAvg;


            //itemsInSlice.ToList().ForEach((d) => { Math.Pow((Convert.ToDouble(d.Close) - Convert.ToDouble(itemsInSLiceAvg)), 2); });

            List<double> itemsInSliceDbl = itemsInSlice.Select(candle => (double)candle.Close).ToList();
            var sdDouble = CalculateStdDev(itemsInSliceDbl);

            var ConfidenceInterval = CalculateConfidenceInterval(sdDouble, SLICES);

            isBusyUpdatingMA = false;

            //updateOccured(CurrentSMA, NotifyListener);


            NotifyListener(new MAUpdateEventArgs
            {
                CurrentMAPrice = CurrentSMAPrice,
                CurrentSd = Math.Round(currentSandardDeviation, 4),
                CiBuffer = Math.Round(CurrentConfidenceIntervalBuffer, 4),
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
                //ret = Math.Sqrt((sum) / (values.Count() - 1)); //sd of sample
                ret = Math.Sqrt((sum) / (values.Count())); //sd of population 
            }
            return ret;
        }

        private double CalculateConfidenceInterval(double sd, int n)
        {
            double zValue = 2.576D; //1.96D; //D is for double. z value of 1.96 indicates a 95% confidence level. 2.576 -> 99%

            double ci = zValue * (sd / Math.Sqrt(n));

            CurrentConfidenceIntervalBuffer = Convert.ToDecimal(ci); //set property value
            return ci;
        }

        public async Task<bool> DownloadAdditionalData()
        {

            while (isBusyUpdatingMA)
                Console.WriteLine("Waiting for sma update to finish before dowanloading additional data...");

            Logger.WriteLog("Additional data is being downloaded ");

            //int days = 25; //36 hours data in 1 min interval in total

            int days = 35; //36 hours data in 1 min interval in total

            HistoricPrices historicData = new HistoricPrices();

            var startDt = firstDataPointDateTime.AddMinutes(-1).AddHours(-6);
            var endDt = firstDataPointDateTime.AddMinutes(-1);

            List<CandleData> extraData = new List<CandleData>();

            var doneDownloadingExtraData = false;

            Int32 delayTime = 400;

            while (!doneDownloadingExtraData)
            {
                try
                {
                    for (int i = 0; i < days; i++)
                    {
                        //var x = Task.Delay(200);

                        System.Threading.Thread.Sleep(delayTime);
                        var temp = await historicData.GetPrices(
                            product: Product,
                            granularity: "60",
                            startTime: startDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            endTime: endDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));

                        startDt = temp.Last().Time.AddMinutes(-1).AddHours(-6);
                        endDt = temp.Last().Time.AddMinutes(-1);

                        extraData.AddRange(temp);
                    }

                    doneDownloadingExtraData = true;
                }
                catch (Exception ex)
                {
                    delayTime += 200; //increase the delay time 
                    var msg = "Error downloading additional data, retrying with " + delayTime.ToString() + " ms delay. (" + ex.Message + ")";
                    while (ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                        msg = msg + "\n" + ex.Message;
                    }
                    Logger.WriteLog(msg);
                    //return false;
                    //throw ex;

                    //reset vars;
                    doneDownloadingExtraData = false;
                    startDt = firstDataPointDateTime.AddMinutes(-1).AddHours(-6);
                    endDt = firstDataPointDateTime.AddMinutes(-1);
                    extraData.Clear();  //clear incomplete donwloaded data
                    

                }

            }

            sharedRawExchangeData.AddRange(extraData);


            //sharedRawExchangeData.ForEach((d) => Logger.WriteLog(d.Time + "\t" + d.Close));

            //sharedRawExchangeData = sharedRawExchangeData.OrderByDescending((d)=>d.Time).ToList();

            Logger.WriteLog("done downloading additional data ");

            return true;
        }

        private void NotifyListener(MAUpdateEventArgs args)
        {
            if (MovingAverageUpdatedEvent != null)
                MovingAverageUpdatedEvent(this, args);

        }


        public async Task<bool> RefreshDataFromServer()
        {
            while (isBusyDownloadingData)
            {
                Logger.WriteLog("waiting for download to finish before refresh operation");
                System.Threading.Thread.Sleep(100);
            }

            var result = downloadData();
            result.Wait();

            return true;
        }

        private async Task<bool> downloadData()
        {
            isBusyDownloadingData = true; 

            HistoricPrices historicData = new HistoricPrices();
            var temp = historicData.GetPrices(product: Product, granularity: "60");
            temp.Wait();

            sharedRawExchangeData.Clear(); //clear existing data
            sharedRawExchangeData = temp.Result.ToList();


            lastDataPointDateTime = sharedRawExchangeData.First().Time;
            firstDataPointDateTime = sharedRawExchangeData.Last().Time;

            //Logger.WriteLog("Before downloading additional data");

            if (TIME_INTERVAL * SLICES > 10) //download extra data anyways 
            {
                var res = DownloadAdditionalData();
                res.Wait();
            }

            sharedRawExchangeData = sharedRawExchangeData.OrderByDescending((x) => x.Time).ToList();

            isBusyDownloadingData = false;

            return true;
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



            //////HistoricPrices historicData = new HistoricPrices();
            //////var temp = historicData.GetPrices(product: Product, granularity: "60");
            //////temp.Wait();

            //////sharedRawExchangeData.Clear(); //clear existing data
            //////sharedRawExchangeData = temp.Result.ToList();


            //////lastDataPointDateTime = sharedRawExchangeData.First().Time;
            //////firstDataPointDateTime = sharedRawExchangeData.Last().Time;

            ////////Logger.WriteLog("Before downloading additional data");

            //////if (TIME_INTERVAL * SLICES > 200)
            //////{
            //////    var res = await DownloadAdditionalData();
            //////    if (res)
            //////    {
            //////    }

            //////}

            ////////Logger.WriteLog("after downloading additional data");




            ////////exchangeData.ToList().ForEach((a) => Logger.WriteLog(a.Time + "\t" + a.Close));

            //////sharedRawExchangeData = sharedRawExchangeData.OrderByDescending((x) => x.Time).ToList();


            //wait while data is being donwloaded by any instance of MovingVerage 

            while (isBusyDownloadingData)
            {
                //Logger.WriteLog("Waiting for data download to finish");
                System.Threading.Thread.Sleep(1000);
            }

            if (sharedRawExchangeData.Count == 0) //no data in shared exchnage data points
            {
                isBusyDownloadingData = true;
                Logger.WriteLog("Downloading data from server... please wait...");
                var result = downloadData();
                result.Wait(); //wait for down load to finish
                Logger.WriteLog("Done downloading data");
                isBusyDownloadingData = false ;
            }


            //sharedRawExchangeData.ForEach((a) => System.Diagnostics.Debug.WriteLine(a.Time + "\t"+ a.Close));


            var intervalData = sharedRawExchangeData.Where((candleData, i) => i % TIME_INTERVAL == 0).ToList();// select every third item in list ie select data from every x min 

            //intervalData.ForEach((l) => Logger.WriteLog(l.Time + "\t" + l.Close));

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

            var ConfidenceInterval = CalculateConfidenceInterval(sdDouble, SLICES);

            currentSandardDeviation = Convert.ToDecimal(sdDouble);

            //return sma;
            return maDataList; 
        }

        public void Dispose()
        {
            try
            {
                Logger.WriteLog(string.Format("disposing the {0} min sma timer ", this.aTimer.Interval / (60000)));
                this.aTimer.Stop();
                this.aTimer.Close();
                this.aTimer = null;
            }
            catch (Exception)
            {
                Logger.WriteLog("error disposing the timer, continuing");
                //throw;
            }
            finally
            {
                this.aTimer = null;
                sharedRawExchangeData = null;

            }

        }
    }
}
