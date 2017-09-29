using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using CoinbaseExchange.NET.Endpoints.PublicData;
using CoinbaseExchange.NET.Endpoints.OrderBook;
using CoinbaseExchange.NET.Endpoints.MyOrders;
using System.Reflection;
using CoinbaseExchange.NET.Core;
using CoinbaseExchange.NET.Endpoints.Fills;
using System.Diagnostics;
using CoinbaseExchange.NET.Data;
namespace Multiplier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private const string myPassphrase = "n6yci6u4i0g";
        private const string myKey = "b006d82554b495e227b9e7a1251ad745";
        private const string mySecret = "NhAb9pmbZaY9cPb2+eXOGWIILje7iFe/nF+dV9n6FOxazl6Kje2/03GuSiQYTsj3a/smh92m/lrvfu7kYkxQMg==";

        private static decimal CurrentRealtimePrice;

        private static decimal CurrentBufferedPrice; 

        private static decimal CurrentAveragePrice;
        private static decimal MaxBuy;
        private static decimal MaxSell;

        private static bool buyOrderFilled;
        private static bool sellOrderFilled;

        private static bool waitingSellFill;
        private static bool waitingBuyFill;

        private static bool waitingBuyOrSell;

        private static Int16 SmaSlices;
        private static Int16 SmaTimeInterval;


        private static bool startBuyingSelling;

        private static decimal priceBuffer;

        private static DateTime LastTickerUpdateTime;
        private static DateTime LastBuySellTime;

        private static DateTime LastCrossTime;

        private static double waitTimeAfterCrossInMin; 

        CBAuthenticationContainer myAuth;
        MyOrderBook myOrderBook;
        TickerClient LtcTickerClient;
        public MainWindow()
        {
            InitializeComponent();


            SmaTimeInterval = 3;
            SmaSlices = 40;

            priceBuffer = 0.01m;

            CurrentRealtimePrice = 0;
            CurrentBufferedPrice = 0;
            CurrentAveragePrice = 0;

            buyOrderFilled = false;
            sellOrderFilled = false;

            waitingSellFill = false;
            waitingBuyFill= false;
            waitingBuyOrSell = false;

            waitTimeAfterCrossInMin = 2.0;

            MaxBuy = 5;
            MaxSell = 5;

            startBuyingSelling = false;

            LastCrossTime = DateTime.UtcNow.ToLocalTime();

            LtcTickerClient = new TickerClient("LTC-USD");
            LtcTickerClient.PriceUpdated += LtcTickerClient_Update;

            myAuth = new CBAuthenticationContainer(myKey, myPassphrase, mySecret);
            myOrderBook = new MyOrderBook(myAuth, "LTC-USD");
            myOrderBook.OrderUpdateEvent += fillUpdateHandler;

            MovingAverage sma = new MovingAverage(ref LtcTickerClient, SmaTimeInterval, SmaSlices);
            sma.MovingAverageUpdated += SmaUpdateEventHandler;

        }





        public void fillUpdateHandler(object sender, EventArgs args)
        {

            var filledOrder = ((OrderUpdateEventArgs)args);

            Debug.WriteLine(string.Format("{0} order ({1}) filled @{2} {3}", filledOrder.side, filledOrder.OrderId, filledOrder.filledAtPrice, DateTime.UtcNow.ToLocalTime().ToString()));

            //MessageBox.Show(string.Format("{0} order ({1}) filled @{2} ", filledOrder.side, filledOrder.OrderId, filledOrder.filledAtPrice));

            if (filledOrder.side == "buy")
            {
                buyOrderFilled = true;
                sellOrderFilled = false;

                waitingBuyFill = false;
                waitingSellFill = true;
            }
            else if (filledOrder.side == "sell")
            {
                sellOrderFilled = true;
                buyOrderFilled = false;

                waitingSellFill = false;
                waitingBuyFill = true;
            }

            waitingBuyOrSell = false;

        }

        public void OrderUpdateHandler(object sender, EventArgs args)
        {

            var orderUpdate = ((OrderUpdateEventArgs)args);
            MessageBox.Show(orderUpdate.Message);
        }


        public void LtcTickerClient_Update(object sender, EventArgs args)
        {
            var tickerData = (TickerMessage)args;
            //var z = a.Sells.FirstOrDefault();
            //Console.WriteLine("Update: price {0}", tickerData.CurrentPrice);

            LastTickerUpdateTime = DateTime.UtcNow.ToLocalTime();

            CurrentRealtimePrice = tickerData.RealTimePrice;

            //Debug.WriteLine(CurrentRealtimePrice + "-" + CurrentBufferedPrice);

            this.Dispatcher.Invoke(() =>
            {
                lblCurPrice.Content = tickerData.RealTimePrice.ToString();
                lblTickUpdate1.Content = LastTickerUpdateTime.ToLongTimeString();

                if (CurrentRealtimePrice - CurrentAveragePrice >= 0)
                {
                    lblCurPrice.Foreground = Brushes.Green;
                }
                else
                {
                    lblCurPrice.Foreground = Brushes.Red;
                }
            }
            );

            if (startBuyingSelling)
            {
               if ((LastTickerUpdateTime - LastBuySellTime).TotalMilliseconds < 1000)
                {
                    //Debug.WriteLine("price skipped: " + CurrentRealtimePrice);
                    return;
                }

                CurrentBufferedPrice = CurrentRealtimePrice;

                var secSinceLastCrosss = (DateTime.UtcNow.ToLocalTime() - LastCrossTime).TotalSeconds;
                if (secSinceLastCrosss < (waitTimeAfterCrossInMin * 60))
                {
                    //if the last time prices crossed is < 2 min do nothing
                    Debug.WriteLine(string.Format("Waiting {0} sec before placing any new order", Math.Round((waitTimeAfterCrossInMin * 60) - secSinceLastCrosss)));
                    return;
                }


                if (!waitingBuyOrSell)
                {
                    LastBuySellTime = DateTime.UtcNow.ToLocalTime();

                    buysell();
                }
                else
                {
                    Debug.WriteLine("Buysell already in progress");
                }
            }


        }

        async void buysell()
        {


            if (waitingBuyOrSell)
            {
                Debug.WriteLine("this should never print");
                return;
            }

            decimal curPriceDiff = CurrentBufferedPrice - CurrentAveragePrice;

            if (curPriceDiff <= priceBuffer) //below average: sell
            {
                if (!sellOrderFilled) //if not already sold
                {
                    sellOrderFilled = true;
                    buyOrderFilled = false;

                    if (MaxSell > 0)
                    {
                        MaxSell = 0;
                        MaxBuy = 5;

                        waitingSellFill = true;
                        waitingBuyOrSell = true;

                        LastCrossTime = DateTime.UtcNow.ToLocalTime();

                        await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", "0.01", CurrentBufferedPrice.ToString(), true);

                    }


                }

            }

            if (curPriceDiff >= priceBuffer) //above average: buy
            {
                if (!buyOrderFilled) //if not already bought
                {

                    sellOrderFilled = false;
                    buyOrderFilled = true;

                    if (MaxBuy > 0)
                    {
                        MaxSell = 5;
                        MaxBuy = 0;

                        waitingBuyFill = true;
                        waitingBuyOrSell = true;

                        LastCrossTime = DateTime.UtcNow.ToLocalTime();

                        await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", CurrentBufferedPrice.ToString(), true);

                    }

                }

            }



        }

        private void btnStartBySelling_Click(object sender, RoutedEventArgs e)
        {
            LastBuySellTime = DateTime.UtcNow.ToLocalTime();
            LastCrossTime = DateTime.UtcNow.ToLocalTime().AddSeconds(-1 * waitTimeAfterCrossInMin * 60);

            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;

            buyOrderFilled = true;
            sellOrderFilled = false;

            MaxBuy = 0;
            MaxSell = 5;

            startBuyingSelling = true;
        }


        private void btnStartByBuying_Click(object sender, RoutedEventArgs e)
        {
            LastBuySellTime = DateTime.UtcNow.ToLocalTime();
            LastCrossTime = DateTime.UtcNow.ToLocalTime().AddSeconds(-1 * waitTimeAfterCrossInMin * 60);

            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;

            buyOrderFilled = false;
            sellOrderFilled = true;

            MaxBuy = 5;
            MaxSell = 0;

            startBuyingSelling = true;
        }

        private void SmaUpdateEventHandler(object sender, EventArgs args)
        {

            var tickerData = (MAUpdateEventArgs)args;
            decimal newSmaPrice = tickerData.CurrentMAPrice;

            CurrentAveragePrice = newSmaPrice; 

            this.Dispatcher.Invoke(() => 
                {
                    lblSma.SetValue(ContentProperty, newSmaPrice);
                    lblUpdatedTime.Content = DateTime.UtcNow.ToLocalTime().ToLongTimeString();
                    lblSmaValue.Content = "SMA-" + SmaSlices.ToString();
                }
            );
            
            //lblSma.Content = newSmaPrice.ToString();
            //Debug.WriteLine(DateTime.UtcNow.ToString());
        }


        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {


            try
            {
                //await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", "0.01", "80.00", true);
                await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", CurrentRealtimePrice.ToString(), true);
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex.Message);
            }


        }



        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {
                await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", "0.01", CurrentRealtimePrice.ToString(), true);
                //await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", "10.00", true);
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

    }
}
