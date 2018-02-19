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

namespace Multiplier
{
    class AppSettings
    {
        private static string _SettingsNamePath;
        private static string _appVersion;
        private static string _ReadJson;
        private static AppSettings _AppSettingsInstance;

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
                    throw new Exception("CantCreateNewSettings");
                }
                
            }

            _ReadJson = "";


            try
            {
                ReadAllSettings();
            }
            catch (Exception)
            {
                throw new Exception("CantReadSettingsFile");
            }

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


        private static void ReadAllSettings()
        {
            _ReadJson = File.ReadAllText(_SettingsNamePath);
        }

        private static void CreateNew()
        {

            dynamic BlankAppSettings = new JObject();
            BlankAppSettings.AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(); //appVersion;


            var strat = new JObject {{ "StrategyName" , "sName"}, { "Value1", "val1"}};
            var stratArray = new JArray(strat);

            var genSetting = new JObject { {"setting1", "sValue1"}, { "setting2", "sValue2" }};
            var genSettingArray = new JArray (genSetting);

            //var settings = new JObject { { stratArray }, { genSettingArray } };

            BlankAppSettings.Settings = new JObject { { "Strategies", stratArray }, { "GeneralSettings", genSettingArray } };

            string appSettingsJSonStr = BlankAppSettings.ToString();
            //System.Diagnostics.Debug.Print(a);


            File.WriteAllText(_SettingsNamePath, appSettingsJSonStr);

        }
    }
}
