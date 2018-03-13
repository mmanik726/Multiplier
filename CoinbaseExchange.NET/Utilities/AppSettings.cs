using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

using System.Security.Cryptography;
using System.Timers;

namespace CoinbaseExchange.NET.Utilities //Multiplier
{
    public class AppSettings
    {
        private static string _SettingsNamePath;
        private static string _appVersion;
        private static string _ReadJson;
        private static AppSettings _AppSettingsInstance;
        private static byte[] _SettingsFileHash;

        public  static EventHandler MajorSettingsChangEvent;

        private static string _TimeInterval; 

        private static Timer aTimer;

        private static void InnitSettings()
        {
            _appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _SettingsNamePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Mani_" + _appVersion + "_Settings.json";

            if (!File.Exists(_SettingsNamePath))
            {
                try
                {
                    CreateNew();
                }
                catch (Exception)
                {
                    Logger.WriteLog("cant create new settings file");
                    throw new Exception("CantCreateNewSettings");
                }

            }

            _ReadJson = "";

            try
            {
                ReadAllSettings();
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Cant read settings file: " + ex.Message);
                //throw;
            }


            if (aTimer != null) //timer already in place
            {
                aTimer.Elapsed -= CheckForNewSettings;
                aTimer.Stop();
                aTimer = null;
            }

            aTimer = new Timer();
            aTimer.Elapsed += CheckForNewSettings;
            aTimer.Interval = 1 * 60 * 1000; //every minute 
            aTimer.Enabled = true;
            aTimer.Start();

            CheckForNewSettings(null, null);


        }


        private static void CheckForNewSettings(object sender, ElapsedEventArgs e)
        {
            var md5Hash = MD5.Create();

            var oldHash = _SettingsFileHash;

            byte[] newHash;

            //var oldTimeInterval = _TimeInterval;

            try
            {
                //Logger.WriteLog("Checking for settings changes");
                using (FileStream fs = File.OpenRead(_SettingsNamePath))
                {
                    newHash = md5Hash.ComputeHash(fs);
                }

                if (oldHash == null)
                {
                    _SettingsFileHash = newHash;
                    return;
                }
                else
                {
                    //old and new has are not equal

                    var a = Convert.ToBase64String(oldHash);
                    var b = Convert.ToBase64String(newHash);


                    if (a != b)
                    {
                        Logger.WriteLog("New Settings detected");
                        //set to new hash



                        _SettingsFileHash = newHash;
                        Reloadsettings();

                        PrintFullSettings();



                        //MajorSettingsChangEvent?.Invoke(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Cant compute new hash value of settings file: " + ex.Message);
            }

        }


        public static bool SaveUpdateStrategySetting(string strategyName, string fieldName, string fieldValue)
        {

            if (_ReadJson == "")
                return false;


            try
            {
                JObject json = JObject.Parse(_ReadJson);

                //var sm 

                var strategySetting = from p in json["Settings"]["Strategies"]
                                      where p["StrategyName"].ToString() == strategyName
                                      select p;

                strategySetting.First()[fieldName] = fieldValue.ToString();

                //JArray strategiesArray = (JArray)json["Settings"]["Strategies"];
                //foreach (var curStrategy in strategiesArray.Where(obj => obj["StrategyName"].Value<String>() == strategyName))
                //{
                //    curStrategy[fieldName] = !string.IsNullOrEmpty(fieldValue.ToString()) ? fieldValue.ToString() : "";
                //}


                //var x = r.Where();

                //JToken s = setting;
                //setting [fieldName] = fieldValue.ToString();
                //var a = JObject.Parse(setting.ToString());
                File.WriteAllText(_SettingsNamePath, json.ToString());

                Logger.WriteLog("App settings file saved / updated: " + strategyName + "->" + fieldName + " = " + fieldValue);



                AppSettings.Reloadsettings();

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog("cant save / update setting: " + strategyName + "->" + fieldName + " = " + fieldValue);
                return false;
                //throw;
            }



            //return true;

        }




        public static bool SaveUpdateGeneralSetting(string fieldName, string fieldValue)
        {

            if (_ReadJson == "")
                return false;


            try
            {
                JObject json = JObject.Parse(_ReadJson);

                //var sm 

                var strategySetting = from p in json["Settings"]["GeneralSettings"]
                                      select p;

                strategySetting.First()[fieldName] = fieldValue.ToString();

                //JArray strategiesArray = (JArray)json["Settings"]["Strategies"];
                //foreach (var curStrategy in strategiesArray.Where(obj => obj["StrategyName"].Value<String>() == strategyName))
                //{
                //    curStrategy[fieldName] = !string.IsNullOrEmpty(fieldValue.ToString()) ? fieldValue.ToString() : "";
                //}


                //var x = r.Where();

                //JToken s = setting;
                //setting [fieldName] = fieldValue.ToString();
                //var a = JObject.Parse(setting.ToString());
                File.WriteAllText(_SettingsNamePath, json.ToString());

                Logger.WriteLog("App settings file saved / updated:" + fieldName  + " = " + fieldValue);



                AppSettings.Reloadsettings();

                return true;
            }
            catch (Exception ex)
            {
                return false;
                //throw;
            }



            //return true;

        }


        public static JArray GetStrategySettings(string StrategyName)
        {
            if (_AppSettingsInstance == null)
            {
                _AppSettingsInstance = new AppSettings();
                InnitSettings();
            }



            var json = JObject.Parse(_ReadJson);

            var settings = from p in json["Settings"]["Strategies"]
                           where p["StrategyName"].ToString() == StrategyName
                           select p;

            return new JArray(settings);
        }


        public static void Reloadsettings()
        {
            try
            {
                ReadAllSettings();
                Logger.WriteLog("settings reloaded");
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Cant read settings file: " + ex.Message);
                //throw;
            }

        }


        private static void ReadAllSettings()
        {

            try
            {
                _ReadJson = File.ReadAllText(_SettingsNamePath);
            }
            catch (Exception)
            {
                throw new Exception("CantReadSettingsFile");
            }
        }

        private static void CreateNew()
        {

            dynamic BlankAppSettings = new JObject();
            BlankAppSettings.AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(); //appVersion;


            var strat = new JObject { { "StrategyName", "sName" }, { "Value1", "val1" } };
            var stratArray = new JArray(strat);

            var genSetting = new JObject { { "setting1", "sValue1" }, { "setting2", "sValue2" } };
            var genSettingArray = new JArray(genSetting);

            //var settings = new JObject { { stratArray }, { genSettingArray } };

            BlankAppSettings.Settings = new JObject { { "Strategies", stratArray }, { "GeneralSettings", genSettingArray } };

            string appSettingsJSonStr = BlankAppSettings.ToString();
            //System.Diagnostics.Debug.Print(a);


            File.WriteAllText(_SettingsNamePath, appSettingsJSonStr);

        }



        public static List<StrategySettings> GetAllStrategies()
        {
            var settingsList = new List<StrategySettings>();


            if (_AppSettingsInstance == null)
            {
                _AppSettingsInstance = new AppSettings();
                InnitSettings();
            }

            var json = JObject.Parse(_ReadJson);

            var settings = from p in json["Settings"]["Strategies"]
                           select p;


            foreach (var ss in settings)
            {
                try
                {
                    dynamic dSettingsVal = ss;

                    var s = new StrategySettings
                    {
                        StrategyName = dSettingsVal.StrategyName,
                        last_buy_price = dSettingsVal.last_buy_price,
                        last_sell_price = dSettingsVal.last_sell_price,
                        stop_loss_percent = dSettingsVal.stop_loss_percent,
                        time_interval = dSettingsVal.time_interval,
                        fast_sma = dSettingsVal.fast_sma,
                        slow_sma = dSettingsVal.slow_sma,
                        signal = dSettingsVal.signal,
                        my_sma = dSettingsVal.my_sma,
                        use_two_sma = Convert.ToBoolean(dSettingsVal.use_two_sma),
                        use_ema = Convert.ToBoolean(dSettingsVal.use_ema)
                    };

                    settingsList.Add(s);

                }
                catch (Exception)
                {
                    Logger.WriteLog("Cant read settings json");
                    throw new Exception("SettingReadError");
                }
            }


            return settingsList;

        }

        public static void PrintFullSettings()
        {
            if (_ReadJson != "")
            {
                Logger.WriteLog(_ReadJson);
            }
        }

        public static void PrintStrategySetting(string sName)
        {
            Logger.WriteLog(GetStrategySettings(sName).ToString());
        }

        public static StrategySettings GetStrategySettings2(string StrategyName)
        {
            if (_AppSettingsInstance == null)
            {
                _AppSettingsInstance = new AppSettings();
                InnitSettings();
            }

            var json = JObject.Parse(_ReadJson);

            var settings = from p in json["Settings"]["Strategies"]
                           where p["StrategyName"].ToString() == StrategyName
                           select p;

            var ss = settings.First();


            if (ss.Count() == 0)
                throw new Exception("SeetingNotFoundError");

            try
            {
                dynamic dSettingsVal = settings.First();

                var s = new StrategySettings
                {
                    StrategyName = dSettingsVal.StrategyName,
                    last_buy_price = dSettingsVal.last_buy_price,
                    last_sell_price = dSettingsVal.last_sell_price,
                    stop_loss_percent = dSettingsVal.stop_loss_percent,
                    time_interval = dSettingsVal.time_interval,
                    fast_sma = dSettingsVal.fast_sma,
                    slow_sma = dSettingsVal.slow_sma,
                    signal = dSettingsVal.signal,
                    my_sma = dSettingsVal.my_sma,
                    use_two_sma = Convert.ToBoolean(dSettingsVal.use_two_sma),
                    use_ema = Convert.ToBoolean(dSettingsVal.use_ema)
                };

                return s;
            }
            catch (Exception)
            {
                Logger.WriteLog("Cant read settings json");
                throw new Exception("SettingReadError");
            }



            //var s = new StrategySettings
            //{
            //    StrategyName = ss["StrategyName"].Value<String>() ,
                
            //};



        }

        public static GeneralSettings GetGeneralSettings()
        {
            if (_AppSettingsInstance == null)
            {
                _AppSettingsInstance = new AppSettings();
                InnitSettings();
            }

            var json = JObject.Parse(_ReadJson);

            var settings = from p in json["Settings"]["GeneralSettings"]
                           select p;

            var ss = settings.First(); // first elemnt in array, return array 


            if (ss.Count() == 0)
                throw new Exception("SeetingNotFoundError");

            try
            {
                dynamic dSettingsVal = settings.First();

                var s = new GeneralSettings
                {
                    Strategy_in_use = dSettingsVal.Strategy_in_use,
                    price_inc_percent_for_using_smallmacd = Convert.ToDecimal(dSettingsVal.price_inc_percent_for_using_smallmacd),
                    already_bought = Convert.ToBoolean(dSettingsVal.already_bought),
                    already_sold = Convert.ToBoolean(dSettingsVal.already_sold)

                };

                return s;
            }
            catch (Exception)
            {
                Logger.WriteLog("Cant read settings json");
                throw new Exception("SettingReadError");
            }



            //var s = new StrategySettings
            //{
            //    StrategyName = ss["StrategyName"].Value<String>() ,

            //};



        }


    }


    public class StrategySettings
    {
        public StrategySettings()
        {
            //defaults
        }
        public string StrategyName { get; set; }
        public decimal last_buy_price { get; set; }
        public decimal last_sell_price { get; set; }
        public decimal stop_loss_percent { get; set; }
        public int time_interval { get; set; }
        public int fast_sma { get; set; }
        public int slow_sma { get; set; }
        public int signal { get; set; }
        public int my_sma { get; set; }
        public bool use_two_sma { get; set; }
        public bool use_ema { get; set; }

    }


    public class GeneralSettings
    {
        public GeneralSettings()
        {
            //default values 
        }
        public string Strategy_in_use { get; set; }

        public decimal price_inc_percent_for_using_smallmacd { get; set; }

        public bool already_bought { get; set; }
        public bool already_sold { get; set; }

        //public decimal last_buy_price { get; set; }
        //public decimal last_sell_price { get; set; }
        //public decimal stop_loss_percent { get; set; }
        //public int time_interval { get; set; }
        //public int fast_sma { get; set; }
        //public int slow_sma { get; set; }
        //public int signal { get; set; }
        //public int my_sma { get; set; }
        //public bool use_two_sma { get; set; }
        //public bool use_ema { get; set; }

    }



}





