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
    }


    public class MovingAverage
    {
        public TickerClient TickerPriceClient { get; set; }

        private List<CandleData> MADataPoints;

        public EventHandler<MAUpdateEventArgs> MovingAverageUpdated;

        public decimal CurrentSMA;

        public MovingAverage(ref TickerClient tickerClient)
        {
            //var a = tickerClient.CurrentPrice;

            TickerPriceClient = tickerClient;

            Init();


        }

        async void Init()
        {
            MADataPoints = await getMaData();

            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += UpdateSMA;
            aTimer.Interval = 3 * 60 * 1000;
            aTimer.Enabled = true;
            aTimer.Start();
            NotifyListener(new MAUpdateEventArgs { CurrentMAPrice = CurrentSMA });
        }

        async Task<List<CandleData>> getMaData()
        {
            var z = await Task.Factory.StartNew(() => GetSmaData());
            return z.Result;
        }

        private void UpdateSMA(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Updating SMA");

            const int SLICES = 40;

            MADataPoints.RemoveAt(0);
            MADataPoints.Add(new CandleData { Close = TickerPriceClient.CurrentPrice, Time = DateTime.UtcNow.ToLocalTime()});

            var itemsInSlice = MADataPoints.Take(SLICES);
            //itemsInSlice.ToList().ForEach((t) => Debug.WriteLine(t.Time + "\t" + t.Close));
            var itemsInSLiceAvg = itemsInSlice.Average((d) => d.Close);

            MADataPoints.ForEach((t) => System.Diagnostics.Debug.WriteLine(t.Time + "\t" + t.Close));

            CurrentSMA = itemsInSLiceAvg;

            //updateOccured(CurrentSMA, NotifyListener);

            NotifyListener(new MAUpdateEventArgs { CurrentMAPrice = CurrentSMA });

        }

        //void updateOccured(decimal price,  Action<MAUpdateEventArgs> runMethod)
        //{
        //    var arg = new MAUpdateEventArgs { CurrentMAPrice = price }; 
        //    runMethod(arg);
        //}

        private void NotifyListener(MAUpdateEventArgs args)
        {
            if (MovingAverageUpdated != null)
                MovingAverageUpdated(this, args);

        }

        public async Task<List<CandleData>> GetSmaData()
        {
            const int TIME_INTERVAL = 3;
            const int SLICES = 40;

            HistoricPrices historicData = new HistoricPrices();
            var exchangeData = await historicData.GetPrices(product: "LTC-USD", granularity: "60");

            var intervalData = exchangeData.Where((candleData, i) => i % TIME_INTERVAL == 0).ToList();// select every third item in list ie select data from every 3 min 

            var takeCount = intervalData.Count - (intervalData.Count() % SLICES);
            var requiredIntervalData = intervalData.Take(takeCount).ToList();


            List<CandleData> sma = new List<CandleData>();


            var groupCount = requiredIntervalData.Count() / SLICES;

            

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

            CurrentSMA = sma.First().Close;

            //List<CandleData> priceDataPoints = new List<CandleData>();

            var lst = requiredIntervalData.Take(SLICES).OrderBy((c)=>c.Time).ToList(); 

            //priceDataPoints.AddRange(lst);

            //return sma;
            return lst; 
        }


    }
}
