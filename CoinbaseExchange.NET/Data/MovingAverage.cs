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

        public static List<CandleData> SharedRawExchangeData;
        private static bool isBusyDownloadingData;


        public TickerClient TickerPriceClient { get; set; }

        //private List<CandleData> MADataPoints;
        private List<double> PriceDataPtsForMa;

        public List<double> SmaDataPoints { get; set; }

        public List<CandleData> SmaDataPts_Candle { get; set; }

        public List<double> EmaDataPoints { get; set; }

        public EventHandler<MAUpdateEventArgs> MovingAverageUpdatedEvent;

        public decimal CurrentSMAPrice;
        public decimal currentSandardDeviation ;

        public decimal CurrentConfidenceIntervalBuffer;

        public System.Timers.Timer aTimer;

        //private bool ForceRedownloadData;

        private int AmountOfDataToDownload;

        private bool UpdateJasonDB;

        private bool clearSharedExDataOnDispose;

        private bool isBusyCalculatingSMA; 

        public MovingAverage(ref TickerClient tickerClient, string ProductName, int timeInterValInMin = 3, int smaSlices = 40, int serverDownloadAmount = 10, bool updateDb = true, bool clearDataOnDispose = true)
        {
            //var a = tickerClient.CurrentPrice;
            clearSharedExDataOnDispose = clearDataOnDispose;

            UpdateJasonDB = updateDb;

            AmountOfDataToDownload = serverDownloadAmount;

            //Logger.WriteLog("Starting moving avg calculations");
            isBusyCalculatingSMA = false;

            Product = ProductName;

            isBusyUpdatingMA = false;

            //ForceRedownloadData = false;

            if (SharedRawExchangeData == null)
                SharedRawExchangeData = new List<CandleData>();

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
                PriceDataPtsForMa = getMaData().Result;
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

        async Task<List<double>> getMaData()
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

            var maDtPtsRemoveIndex = PriceDataPtsForMa.Count - 1;


            if (maDtPtsRemoveIndex != -1)
                PriceDataPtsForMa.RemoveAt(maDtPtsRemoveIndex);

            var newDataPoint = (double)TickerPriceClient.CurrentPrice;

            PriceDataPtsForMa.Insert(0, newDataPoint); //insert at top


            ////var newDataPtCandle = new CandleData {Close = Convert.ToDecimal(newDataPoint), Time = DateTime.Now  };
            ////SharedRawExchangeData.Insert(0, newDataPtCandle);

            var itemsInSlice = PriceDataPtsForMa.Take(SLICES); 
            var itemsInSLiceAvg = itemsInSlice.Average((d) => d);


            CurrentSMAPrice = (decimal)itemsInSLiceAvg;

            //TODO:needs testing
            //var curEmaPrice = PriceDataPtsForMa.EMA(SLICES);


            SmaDataPoints.Insert(0, itemsInSLiceAvg);

            var sdDouble = CalculateStdDev(itemsInSlice);

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

            ExData DataManager = new ExData(Product, UpdateJasonDB);





            SharedRawExchangeData.AddRange(DataManager.RawExchangeData);



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

            SharedRawExchangeData.Clear(); //clear existing data

            //download to / update database
            var res = DownloadAdditionalData();
            res.Wait();

            isBusyDownloadingData = false;

            return true;
        }


        private async Task<List<double>> GetSmaData()
        {

            //wait while data is being donwloaded by any instance of MovingVerage 

            while (isBusyDownloadingData)
            {
                //Logger.WriteLog("Waiting for data download to finish");
                System.Threading.Thread.Sleep(1000);
            }

            if (SharedRawExchangeData.Count == 0) //no data in shared exchnage data points
            {
                isBusyDownloadingData = true;
                Logger.WriteLog("Downloading data from server... please wait...");
                var result = downloadData();
                result.Wait(); //wait for down load to finish
                Logger.WriteLog("Done downloading data");
                isBusyDownloadingData = false ;
            }


            //sharedRawExchangeData.ForEach((a) => System.Diagnostics.Debug.WriteLine(a.Time + "\t"+ a.Close));

            //sharedRawExchangeData = sharedRawExchangeData.Take(50).ToList();


            var remainder = SharedRawExchangeData.Count() % TIME_INTERVAL;

            //skip the first REMAINDER so that the latest data points are taken
            //as opposed to stripping of the latest data points and using old ones
            //this gurantess that the latest price is considered (latest data point is taken in calculations)
            var tempExchangePriceDataSet = SharedRawExchangeData.Take(SharedRawExchangeData.Count() - remainder).ToList();




            var intervalData = tempExchangePriceDataSet.Where((candleData, i) => i % TIME_INTERVAL == 0).ToList();// select every third item in list ie select data from every x min 

            //intervalData.ForEach((l) => Logger.WriteLog(l.Time + "\t" + l.Close));

            var takeCount = intervalData.Count - (intervalData.Count() % SLICES);
            var requiredIntervalData = intervalData.Take(takeCount).ToList();

            SmaDataPts_Candle = new List<CandleData>(requiredIntervalData);

            var priceDataPoints = requiredIntervalData.Select((d) => (double)d.Close).ToList(); //transfer candle data close values to pure list of doubles

            var smaDataPtsList = priceDataPoints.SMA(SLICES).ToList(); //return the continuous sma using the list of doubles (NOT candle data)

            //var emadataPtsList = priceDataPoints.EMA(SLICES).ToList();

            SmaDataPoints = new List<double>(smaDataPtsList);

            //EmaDataPoints = new List<double>(emadataPtsList);

            smaDataPtsList = smaDataPtsList.Where((data, i)=>i % SLICES == 0).ToList(); //take only sma datapoints that are every SLICE apart ie remove the intermediate values 

            Logger.WriteLog("Time: " + TIME_INTERVAL + " Len: " + SLICES);

            CurrentSMAPrice = (decimal)smaDataPtsList.First();

            
            

            var sdDouble = CalculateStdDev(smaDataPtsList);
            var ConfidenceInterval = CalculateConfidenceInterval(sdDouble, SLICES);
            currentSandardDeviation = Convert.ToDecimal(sdDouble);


            var pricesForMaCalc = priceDataPoints.Take(SLICES).ToList();
            return pricesForMaCalc; 
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

                if (clearSharedExDataOnDispose)
                {
                    SharedRawExchangeData = null;
                }
                

            }

        }
    }
}
