using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using System.IO;
using CoinbaseExchange.NET.Utilities;
using CoinbaseExchange.NET.Endpoints.PublicData;
namespace CoinbaseExchange.NET
{


    public class ExData
    {
        public String ProductName { get; set; }

        private int AmountOfDataToDownload;

        private DateTime firstDataPointDateTime;
        private DateTime lastDataPointDateTime;
        public List<CandleData> RawExchangeData;

        private Int32 delayTime = 500;
        //initialize database
        private string jsonDBNamePath;

        private int retriedCount; 

        public ExData(string productName)
        {
            ProductName = productName;
            retriedCount = 0;
            //check if data exists if so update accordingly
            jsonDBNamePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\" +ProductName + "_PriceDB.json";

            if (File.Exists(jsonDBNamePath))
            {
                Update();
            }
            else
            {
                CreateNew();
            }

        }

        private void CreateNew()
        {

            RawExchangeData = new List<CandleData>();

            while (RawExchangeData.Count == 0 && retriedCount <= 10)
            {
                retriedCount++;
                var resultOk = DownloadDataFromDateTillNow(true);//DownloadAdditionalData();
                if (resultOk == true)
                    break;
            }

            if (RawExchangeData.Count == 0)
            {
                throw new Exception("DataDownloadError");
            }

            WriteToFile();

        }

        private bool WriteToFile()
        {
            //write to file 
            try
            {
                RawExchangeData = RawExchangeData.OrderByDescending((x) => x.Time).ToList();
                File.WriteAllText(jsonDBNamePath, JsonConvert.SerializeObject(RawExchangeData, Formatting.Indented));
            }
            catch (Exception)
            {
                throw new Exception("FileWriteError");
            }

            return true;
        }


        private bool ReadFromFile()
        {
            try
            {
                RawExchangeData = JsonConvert.DeserializeObject<List<CandleData>>(File.ReadAllText(jsonDBNamePath));
            }
            catch (Exception)
            {
                throw new Exception("FileReadError");
            }

            return true;

        }

        //update database
        private void Update()
        {
            Logger.WriteLog("Updating price DB file " + jsonDBNamePath);

            ReadFromFile();

            //Logger.WriteLog(RawExchangeData.First().Time.ToShortDateString());
            //Logger.WriteLog(RawExchangeData.Last().Time.ToShortDateString());

            while (retriedCount <= 10)
            {
                retriedCount++;
                var resultOk = DownloadDataFromDateTillNow();//DownloadAdditionalData();
                if (resultOk == true)
                    break;
            }


            try
            {
                //create back up of originla file
                File.Copy(jsonDBNamePath, jsonDBNamePath + ".bak", true);

                WriteToFile();

                File.Copy(jsonDBNamePath, jsonDBNamePath + ".bak", true);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("File copy / write error: " + ex.Message);
                throw new Exception("FileWriteCopyError");
            }



        }


        private bool DownloadDataFromDateTillNow(bool createNewDB = false)
        {


            HistoricPrices historicData = new HistoricPrices();

            const int HOURS = 5;

            List<CandleData> extraData = new List<CandleData>();

            DateTime startDate = new DateTime();

            if (createNewDB)
                startDate = new DateTime(2017,06,1,0,0,0);
            else
                startDate = RawExchangeData.First().Time.AddMinutes(1);
            

            var endDate = DateTime.Now.AddMinutes(-1); //one min less of now

            var startDt = startDate; // from older date to newer date
            var endDt = startDt.AddHours(HOURS);


            do
            {
                if (startDt > endDt)
                    startDt = endDt;

                if (endDt > endDate)
                    endDt = endDate;

                try
                {
                    Logger.WriteLog(string.Format("downloading data: {0} - {1}", startDt.ToString(), endDt.ToString()));

                    var temp = historicData.GetPrices(
                                product: ProductName,
                                granularity: "60",
                                startTime: startDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                endTime: endDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")).Result;

                    startDt = temp.First().Time.AddMinutes(2); //most recent plus one min + one min to compensate
                    endDt = startDt.AddHours(HOURS); // start date plus 5 hours

                    //System.Diagnostics.Debug.Print("start:{0} end:{1}",startDt, endDt);

                    if (temp != null)
                    {
                        if (temp.Count() > 0)
                            extraData.AddRange(temp);
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    delayTime += 200;

                    Logger.WriteLog("Error downloading additional data, retrying with " + delayTime.ToString() + " ms delay. (" + ex.Message + ")");
                    return false;
                }




                System.Threading.Thread.Sleep(delayTime);

            } while (startDt <= endDate);

            extraData = extraData.OrderByDescending((d) => d.Time).ToList();
            //extraData.ForEach((d) => { System.Diagnostics.Debug.Print(d.Time.ToString()); });

            RawExchangeData.AddRange(extraData);


            Logger.WriteLog("done downloading additional data ");

            return true;
        }


        //void DownloadCompleteHandler(bool resultOk)
        //{
        //    if(resultOk)
        //    {



        //    }
        //    else
        //    {
        //        while(sharedRawExchangeData.Count == 0 && retriedCount <= 10)
        //        {
        //            retriedCount++;
        //            if (DownloadAdditionalData() == true)
        //                break;
        //        }
                    
        //    }

        //}





        //private bool DownloadAdditionalData()
        //{

        //    lastDataPointDateTime = DateTime.UtcNow; //sharedRawExchangeData.First().Time;
        //    firstDataPointDateTime = DateTime.UtcNow; //sharedRawExchangeData.Last().Time;

        //    //while (isBusyUpdatingMA)
        //    //    Console.WriteLine("Waiting for sma update to finish before dowanloading additional data...");

        //    Logger.WriteLog("Additional data is being downloaded ");

        //    //int days = 25; //36 hours data in 1 min interval in total



        //    int days = 1000; // default value if one is not provided 

        //    HistoricPrices historicData = new HistoricPrices();

        //    const int HOURS = -5;
        //    var startDt = firstDataPointDateTime.AddMinutes(-1).AddHours(HOURS);
        //    var endDt = firstDataPointDateTime.AddMinutes(-1);

        //    List<CandleData> extraData = new List<CandleData>();

        //    var doneDownloadingExtraData = false;

            

        //    while (!doneDownloadingExtraData)
        //    {
        //        try
        //        {
        //            for (int i = 0; i < days; i++)
        //            {
        //                //var x = Task.Delay(200);
        //                Logger.WriteLog(i + "/" + days);
        //                System.Threading.Thread.Sleep(delayTime);
        //                var temp = historicData.GetPrices(
        //                    product: ProductName,
        //                    granularity: "60",
        //                    startTime: startDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
        //                    endTime: endDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")).Result;

        //                startDt = temp.Last().Time.AddMinutes(-1).AddHours(HOURS);
        //                endDt = temp.Last().Time.AddMinutes(-1);

        //                extraData.AddRange(temp);
        //            }

        //            doneDownloadingExtraData = true;
        //        }
        //        catch (Exception ex)
        //        {
        //            delayTime += 200; //increase the delay time 
        //            var msg = "Error downloading additional data, retrying with " + delayTime.ToString() + " ms delay. (" + ex.Message + ")";
        //            while (ex.InnerException != null)
        //            {
        //                ex = ex.InnerException;
        //                msg = msg + "\n" + ex.Message;
        //            }
        //            Logger.WriteLog(msg);
        //            //return false;
        //            //throw ex;

        //            //reset vars;
        //            doneDownloadingExtraData = false;
        //            startDt = firstDataPointDateTime.AddMinutes(-1).AddHours(HOURS);
        //            endDt = firstDataPointDateTime.AddMinutes(-1);
        //            extraData.Clear();  //clear incomplete donwloaded data

        //            //completionHandler(false);
        //            return false;
        //        }

        //    }

        //    RawExchangeData.AddRange(extraData);


        //    //sharedRawExchangeData.ForEach((d) => Logger.WriteLog(d.Time + "\t" + d.Close));

        //    //sharedRawExchangeData = sharedRawExchangeData.OrderByDescending((d)=>d.Time).ToList();

        //    Logger.WriteLog("done downloading additional data ");

        //    //completionHandler(true);
        //    return true;
        //}


    }
}
