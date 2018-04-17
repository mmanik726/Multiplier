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

    public class SmaData
    {
        public double SmaValue { get; set; }
        public decimal ActualPrice { get; set; }
        public DateTime Time { get; set; }
    }

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

        private string missingDummyFileNamtPath; 

        private int retriedCount;

        private bool UpdateJsonDb;

        public ExData(string productName, bool updateDbToLatest = true)
        {
            UpdateJsonDb = updateDbToLatest;
            ProductName = productName;
            retriedCount = 0;
            //check if data exists if so update accordingly
            jsonDBNamePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\" + ProductName + "_PriceDB.json";

            missingDummyFileNamtPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\" + ProductName + "_MissingDummyAvgData.json";


            if (File.Exists(jsonDBNamePath))
            {
                ReadFromFile();

                if (UpdateJsonDb)
                {
                    Update();
                }

                firstDataPointDateTime = RawExchangeData.First().Time;
                lastDataPointDateTime = RawExchangeData.Last().Time;

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

            WriteToFile(jsonDBNamePath, RawExchangeData);

        }

        private bool WriteToFile(string fileNamePath, List<CandleData> CandleDataList)
        {
            //write to file 
            Logger.WriteLog("Writing to Json DB");
            try
            {
                var data = CandleDataList.OrderByDescending((x) => x.Time).ToList();
                File.WriteAllText(fileNamePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception)
            {
                throw new Exception("FileWriteError");
            }

            return true;
        }


        private bool ReadFromFile()
        {

            Logger.WriteLog("Reading Json DB for historic data");
            try
            {
                RawExchangeData = JsonConvert.DeserializeObject<List<CandleData>>(File.ReadAllText(jsonDBNamePath));

                //order the data right after reading from db so its consistent everywhere
                RawExchangeData = RawExchangeData.OrderByDescending(d => d.Time).ToList();
            }
            catch (Exception)
            {
                throw new Exception("FileReadError");
            }

            return true;

        }


        private bool IsDbCorrupt()
        {
            Logger.WriteLog("Checking for in consistencies in Json DB");

            var mDt = RawExchangeData;//.OrderByDescending((d) => d.Time).ToList();


            var mDt2 = new List<CandleData>(mDt).ToList();



            mDt2.RemoveAt(0);

            var comparedList = mDt.Zip(mDt2, (lstA, lstB) => new MissingData { Difference = (lstA.Time - lstB.Time), LastCandle = lstA, NextCandle = lstB });

            //var comparedList = mDt.Zip(mDt2, (lstA, lstB) => (lstA.Time - lstB.Time));



            var invalidData = comparedList.Where((a) => a.Difference > TimeSpan.FromMinutes(1));//ToList();


            var duplicateData = comparedList.Where((a) => a.Difference == TimeSpan.FromMinutes(0)).ToList();//ToList();

            if (duplicateData.Count() > 0)
            {
                Logger.WriteLog("There may be duplicate data in Json DB");
            }


            //FillMissingData(invalidData.ToList());
            if (invalidData.Count() > 0)
                FillMissingData_Dummy(invalidData.ToList());

            if (invalidData.Count() > 0)
            {
                Logger.WriteLog("Inconsistencies in database found! Fixed with dummy data:");

                //foreach (var d in invalidData)
                //{
                //    Logger.WriteLog(d.LastCandle.Time.ToString());
                //}

                return true;
            }

            return false;


        }


        private void FillMissingData_Dummy(List<MissingData> missingDataList)
        {

            List<CandleData> allMissingCandleList = new List<CandleData>();

            foreach (var missingData in missingDataList)
            {
                var endDt = missingData.LastCandle.Time.AddMinutes(-1); //data is in reverse order and so -1

                var startDt = missingData.NextCandle.Time.AddMinutes(1);//missingData.LastCandle.Time - missingData.Difference.Add(TimeSpan.FromMinutes(-1));

                if (missingData.Difference > TimeSpan.FromMinutes(2))
                {
                    Console.WriteLine((missingData.Difference.ToString() + " of data missing in sequesnce"));
                }

                //DownloadDataSegment(startDt, endDt);
                allMissingCandleList.AddRange( AddDummyData(startDt, endDt, missingData.LastCandle, missingData.NextCandle));
            }


            WriteToFile(missingDummyFileNamtPath , allMissingCandleList);

            RawExchangeData.AddRange(allMissingCandleList);

            RawExchangeData = RawExchangeData.OrderByDescending(d => d.Time).ToList();


            if (allMissingCandleList.Count > 0)
            {
                

                try
                {
                    WriteToFile(jsonDBNamePath, RawExchangeData);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("File copy / write error: " + ex.Message);
                    throw new Exception("FileWriteCopyError");
                }
            }



        }


        private List<CandleData> AddDummyData(DateTime startDate, DateTime endDate, CandleData lastCandle, CandleData nextCandle)
        {
            List<CandleData> missingCandleList = new List<CandleData>();

            var curDt = startDate;
            var dtEnd = endDate;

            DateTime curCandleTime;
            string avgLow = ((Convert.ToDouble(lastCandle.Low) + Convert.ToDouble(nextCandle.Low)) / 2).ToString();
            string avgHigh = ((Convert.ToDouble(lastCandle.High) + Convert.ToDouble(nextCandle.High)) / 2).ToString();
            decimal avgLocalAvg = (lastCandle.LocalAvg + nextCandle.LocalAvg) / 2;
            string avgOpen = ((Convert.ToDouble(lastCandle.Open) + Convert.ToDouble(nextCandle.Open)) / 2).ToString();
            decimal avgClose = (lastCandle.Close + nextCandle.Close) / 2;
            string avgVolume = ((Math.Round(Convert.ToDouble(lastCandle.Volume), 4) + Math.Round(Convert.ToDouble(nextCandle.Volume), 4))  / 2).ToString();



            while (curDt <= dtEnd)
            {
                curCandleTime = curDt;

                CandleData curDummyCandle = new CandleData
                {
                    Time = curCandleTime,
                    Low = avgLow,
                    High = avgHigh,
                    LocalAvg = avgLocalAvg,
                    Open = avgOpen,
                    Close = avgClose,
                    Volume = avgVolume
                };

                missingCandleList.Add(curDummyCandle);
                curDt = curDt.AddMinutes(1);
            }

            return missingCandleList;

        }


        private void FillMissingData(List<MissingData> missingDataList)
        {

            foreach (var missingData in missingDataList)
            {
                var endDt = missingData.LastCandle.Time.AddMinutes(-1); //data is in reverse order and so -1

                var startDt = missingData.LastCandle.Time - missingData.Difference.Add(TimeSpan.FromMinutes(-1));

                DownloadDataSegment(startDt, endDt);
            }

            //for (int i = 0; i < missingDataList.Count(); i++)
            //{

            //}

        }


        private void DownloadDataSegment(DateTime startDate, DateTime endDate)
        {

            const int HOURS = 5;

            List<CandleData> extraData = new List<CandleData>();


            var todownloadList = new List<StartEndTimes>();

            if ((endDate - startDate) > TimeSpan.FromHours(HOURS))
            {
                var t = (endDate - startDate).Hours % HOURS;

                for (int i = 0; i < t; i++)
                {
                    var newEnd = startDate.AddHours(HOURS);

                    todownloadList.Add(new StartEndTimes { Start = startDate, End = newEnd });

                    startDate = startDate.AddHours(HOURS).AddMinutes(1);

                    //todownloadList.Add
                }

                if ((endDate - startDate).Minutes > 0)
                {
                    todownloadList.Add(new StartEndTimes { Start = startDate, End = endDate });
                }


            }
            else
            {
                todownloadList.Add(new StartEndTimes { Start = startDate, End = endDate });
            }



            foreach (var segment in todownloadList)
            {

                var result = downloadSegment(segment);

                while (result == null)
                {
                    System.Threading.Thread.Sleep(delayTime);
                    result = downloadSegment(segment);
                }


                if (result.Count() > 0)
                    extraData.AddRange(result);

            }






        }

        private IEnumerable<CandleData> downloadSegment(StartEndTimes segment)
        {

            HistoricPrices historicData = new HistoricPrices();

            try
            {
                //Logger.WriteLog(string.Format("downloading data: {0} - {1}", segment.Start.ToString(), segment.End.ToString()));

                //var temp = historicData.GetPrices(
                //            product: ProductName,
                //            granularity: "60",
                //            startTime: segment.Start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                //            endTime: segment.End.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")).Result;


                var temp = historicData.GetPrices(
                            product: ProductName,
                            granularity: "60",
                            startTime: segment.Start.ToUniversalTime().ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                            endTime: segment.End.ToUniversalTime().ToString("s", System.Globalization.CultureInfo.InvariantCulture)).Result;


                //("s", System.Globalization.CultureInfo.InvariantCulture)

                if (temp != null)
                {
                    //if (temp.Count() > 0)
                    //    extraData.AddRange(temp);

                    return temp;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                delayTime += 200;

                Logger.WriteLog("Error downloading additional data, retrying with " + delayTime.ToString() + " ms delay. (" + ex.Message + ")");
                return null;
            }
        }


        //update database
        private void Update()
        {
            Logger.WriteLog("Updating price DB file " + jsonDBNamePath);

            //ReadFromFile();


            

            //if (UpdateJsonDb == false)
            //{
            //    return;
            //}

            //////if (IsDbCorrupt())
            //////{
            //////    //CreateNew();

            //////    //return;
            //////}

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
                //no back up really required, wast of space
                //File.Copy(jsonDBNamePath, jsonDBNamePath + ".bak", true);

                WriteToFile(jsonDBNamePath, RawExchangeData);

                //File.Copy(jsonDBNamePath, jsonDBNamePath + ".bak", true);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("File copy / write error: " + ex.Message);
                throw new Exception("FileWriteCopyError");
            }


            var dbDurrupt = IsDbCorrupt();

        }


        private bool DownloadDataFromDateTillNow(bool createNewDB = false)
        {


            HistoricPrices historicData = new HistoricPrices();

            const int HOURS = 5;

            List<CandleData> extraData = new List<CandleData>();

            DateTime startDate = new DateTime();

            if (createNewDB)
                startDate = DateTime.Now.AddYears(-1);//get all data from past year //new DateTime(2017,06,1,0,0,0);
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

            RawExchangeData = RawExchangeData.OrderByDescending(d => d.Time).ToList();

            Logger.WriteLog("done downloading additional data from server");

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


        private class StartEndTimes
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }

        private class MissingData
        {
            public TimeSpan Difference { get; set; }
            public CandleData LastCandle { get; set; }
            public CandleData NextCandle { get; set; }
        }



    }




}
