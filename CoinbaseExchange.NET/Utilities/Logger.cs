using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CoinbaseExchange.NET.Utilities
{
    public class LoggerEventArgs : EventArgs
    {
        public string LogMessage { get; set; }
    }
    
    //shared logger class 

    public class Logger
    {
        //public STATIC eventhandler 
        public static EventHandler<LoggerEventArgs> Logupdated;

        private static string filePath;
        private static string fileName;
        private static string fileNamePath;

        private static Logger LoggerInstance;


        private static void InitLogger(string logFileName = "")
        {

            filePath = AppDomain.CurrentDomain.BaseDirectory;//System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (logFileName == "")
                fileName = "Multiplier_Log.txt";
            else
                fileName = logFileName;

            fileNamePath = filePath +  fileName;


            string existingLogContent = ""; 

            if (File.Exists(fileNamePath))
            {
                WriteLog("Multiplier Logging started\n\n");

                try
                {
                    existingLogContent = File.ReadAllText(fileNamePath);
                    onLogUpdate(existingLogContent);
                }
                catch (Exception ex)
                {
                    onLogUpdate("Error loading log file: " + ex.Message);
                    //throw;
                }

            }
            else
            {
                WriteLog("Multiplier Logging started\n\n");
            }

        }


        public static void WriteLog(string message, string fileName = "")
        {

            if (LoggerInstance == null)
            {
                LoggerInstance = new Logger();
                InitLogger(fileName);
            }

            //default action is to write to file located in same dir as app
            // write to log

            var curDateTime = DateTime.UtcNow.ToLocalTime();

            var dt = curDateTime.ToShortDateString() + " " + curDateTime.ToLongTimeString();

            string logMsg = dt + "\t" + message + "\n";


            try
            {
                File.AppendAllText(fileNamePath, logMsg);
                Debug.WriteLine(logMsg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error writing to log file {0}\n {1}", fileNamePath, ex.Message));
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


    }
}
