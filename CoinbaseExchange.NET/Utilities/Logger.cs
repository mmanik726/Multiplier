using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using System.Timers;

namespace CoinbaseExchange.NET.Utilities
{
    public class LoggerEventArgs : EventArgs
    {
        public string LogMessage { get; set; }
    }
    
    //shared logger class 

    public class Logger : IDisposable
    {
        //public STATIC eventhandler 
        public static EventHandler<LoggerEventArgs> Logupdated;

        private static string _filePath;
        private static string _fileName;
        private static string _fileNamePath;

        private static Logger _LoggerInstance;

        public static Timer _aTimer;


        private static StringBuilder _AllText;
        private static int _LogMsgCount = 0;
        //private static Queue<string> writeQue;

        private static object _WriteLock = new object();

        private static void InitLogger(string logFileName = "")
        {
            
            _AllText = new StringBuilder();

            _filePath = AppDomain.CurrentDomain.BaseDirectory;//System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (logFileName == "")
            {
                var dateName = DateTime.UtcNow.ToLocalTime().ToString("yyyy_MMM_dd");
                _fileName = "Multiplier_Log_" + dateName + ".txt";
            }

            else
                _fileName = logFileName;

            _fileNamePath = _filePath +  _fileName;


            //string existingLogContent = ""; 

            if (File.Exists(_fileNamePath))
            {
                WriteLog("\n\n\tMultiplier Logging started\n\n");

                try
                {
                    var existingLogContent = File.ReadAllLines(_fileNamePath, Encoding.UTF8).ToList();
                    StringBuilder s = new StringBuilder();
                    if (existingLogContent.Count > 500)
                        existingLogContent.Skip(existingLogContent.Count() - 500).ToList().ForEach(a => s.AppendLine(a));
                    else
                        existingLogContent.ForEach(a => s.AppendLine(a));
                    onLogUpdate(s.ToString());
                }
                catch (Exception ex)
                {
                    onLogUpdate("Error loading log file: " + ex.Message);
                    //throw;
                }

            }
            else
            {
                WriteLog("\n\n\tMultiplier Logging started\n\n");
            }




            if (_aTimer != null) //timer already in place
            {
                _aTimer.Elapsed -= CheckLogFileName;
                _aTimer.Stop();
                _aTimer = null;
            }


            _aTimer = new Timer();
            _aTimer.Elapsed += CheckLogFileName;
            _aTimer.Interval = 30 * 60 * 1000; // every 30 min
            _aTimer.Enabled = true;
            _aTimer.Start();


            Logger.WriteLog("starting log file monitor");
            CheckLogFileName(null, null);


        }


        private static void CheckLogFileName(object sender, ElapsedEventArgs e)
        {
            var curDay = DateTime.UtcNow.ToLocalTime().ToString("yyyy_MMM_dd");

            if (!_fileName.Contains(curDay)) //if file name does not contains today date then rename file and create new one
            {
                Logger.WriteLog("End of log file " + _fileName);
                var dateName = DateTime.UtcNow.ToLocalTime().ToString("yyyy_MMM_dd");
                _fileNamePath = _filePath + "Multiplier_Log_" + dateName + ".txt";
            }

        }



        public static void WriteLog(string message, string fileName = "")
        {

            if (_LoggerInstance == null)
            {
                _LoggerInstance = new Logger();
                InitLogger(fileName);
            }

            //default action is to write to file located in same dir as app
            // write to log

            var curDateTime = DateTime.UtcNow.ToLocalTime();

            var dt = curDateTime.ToShortDateString() + " " + curDateTime.ToLongTimeString();

            string logMsg = dt + "\t" + message + "\n";



            //if (writeQue.Count > 0)
            //{
            //    writeQue.Enqueue(logMsg);
            //}
            

            try
            {
                //for (int i = 0; i < writeQue.Count; i++)
                //{
                //    File.AppendAllText(fileNamePath, logMsg);
                //    Debug.WriteLine(logMsg);

                //    writeQue.Dequeue();
                //}

                if (string.IsNullOrWhiteSpace(_fileNamePath))
                    return;



                //_AllText.Append(logMsg);
                //_LogMsgCount++;

                //lock (_WriteLock)
                //{
                //    if (_LogMsgCount > 100)
                //    {
                //        File.AppendAllText(_fileNamePath, _AllText.ToString());
                //        onLogUpdate(_AllText.ToString());

                //        _AllText.Clear();
                //        _LogMsgCount = 0;

                //    }
                //}

                lock (_WriteLock)
                {
                    File.AppendAllText(_fileNamePath, logMsg);
                }

                //onLogUpdate(_AllText.ToString());



            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error writing to log file {0}\n {1}", _fileNamePath, ex.Message));
                onLogUpdate(dt + " Error writing to log file");
                return;
                //throw;
            }


            //raise event
            onLogUpdate(logMsg);

        }

        static void onLogUpdate(string msg)
        {
            if (Logupdated != null)
            {
                LoggerEventArgs logArgs = new LoggerEventArgs
                {
                    LogMessage = msg
                };

                Logupdated(null, logArgs);
            }
        }


        public static void DumpLogToFile()
        {

            if (_LoggerInstance != null)
            {
                if (_LogMsgCount > 0)
                {
                    lock (_WriteLock)
                    {
                        File.AppendAllText(_fileNamePath, _AllText.ToString());
                        onLogUpdate(_AllText.ToString());

                        _AllText.Clear();
                        _LogMsgCount = 0;
                    }
                }
            }
            
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
            //write the last messages in buffer before exit
            if (_LogMsgCount > 0)
            {
                lock (_WriteLock)
                {
                    File.AppendAllText(_fileNamePath, _AllText.ToString());
                }
            }
        }
    }
}
