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

        private static decimal CurrentPrice;
        private static decimal CurrentAveragePrice;
        private decimal MaxBuy;
        private decimal MaxSell;

        private bool buyOrderFilled;
        private bool sellOrderFilled;

        private bool startBuyingSelling;

        CBAuthenticationContainer myAuth;
        MyOrderBook myOrderBook;
        TickerClient LtcTickerClient;
        public MainWindow()
        {
            InitializeComponent();

            CurrentPrice = 0;
            CurrentAveragePrice = 0;

            buyOrderFilled = false;
            sellOrderFilled = false;

            MaxBuy = 5;
            MaxSell = 5;

            startBuyingSelling = false;


            TestMethod();
            myAuth = new CBAuthenticationContainer(myKey, myPassphrase, mySecret);
            myOrderBook = new MyOrderBook(myAuth, "LTC-USD");
            myOrderBook.OrderUpdateEvent += fillUpdateHandler;

            MovingAverage sma = new MovingAverage(ref LtcTickerClient, 3, 40);
            sma.MovingAverageUpdated += SmaUpdateEventHandler;

        }





        public void fillUpdateHandler(object sender, EventArgs args)
        {

            var filledOrder = ((OrderUpdateEventArgs)args);

            MessageBox.Show(string.Format("{0} order ({1}) filled @{2} ", filledOrder.side, filledOrder.OrderId, filledOrder.filledAtPrice));

            if (filledOrder.side == "buy")
            {
                buyOrderFilled = true;
            }
            else if (filledOrder.side == "sell")
            {
                sellOrderFilled = true;
            }
            


        }

        public void OrderUpdateHandler(object sender, EventArgs args)
        {

            var orderUpdate = ((OrderUpdateEventArgs)args);
            MessageBox.Show(orderUpdate.Message);
        }

        public void TestMethod()
        {

            LtcTickerClient = new TickerClient("LTC-USD");
            LtcTickerClient.PriceUpdated += LtcTickerClient_Update;

            //MyOrderBook myOrderBook = new MyOrderBook(myAuth, "LTC-USD");

            //var x = await myOrderBook.GetAllOrders();
            //var delOrder = x[0].Id;

            //FillsClient fillClient = new FillsClient(myAuth);
            //fillClient.FillUpdated += fillUpdateHandler;

            //fillClient.addOrderToWatchList("42643496-1f08-442c-84e3-c9ecd7178f07");


            //var fillStat = fillClient.GetFillStatus(delOrder);

            //await Task.Delay(5000);
            //fillClient.addOrderToWatchList(delOrder);


            //await Task.Delay(5000);
            //fillClient.addOrderToWatchList("42643496-1f08-442c-84e3-c9ecd7178888");

            //await Task.Delay(5000);
            //fillClient.addOrderToWatchList("42643496-1f08-442c-84e3-c9ecd7178f07");

            //await Task.Delay(5000);
            //fillClient.removeFromOrderWatchList(delOrder);


            //var z = await myOrderBook.CancelSingleOrder(delOrder);
            //var z = await myOrderBook.CancelAllOrders();
            //await Task.Delay(1000000);
            //var o = myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "1.0", "1.0");

            //MessageBox.Show("Order placed, ID: " + o.Result.Id);




        }

        public void LtcTickerClient_Update(object sender, EventArgs args)
        {
            var tickerData = (TickerMessage)args;
            //var z = a.Sells.FirstOrDefault();
            //Console.WriteLine("Update: price {0}", tickerData.CurrentPrice);

            CurrentPrice = tickerData.RealTimePrice;
            this.Dispatcher.Invoke(() =>
            {
                lblCurPrice.Content = tickerData.RealTimePrice.ToString();
                lblTickUpdate1.Content = DateTime.UtcNow.ToLocalTime().ToLongTimeString();
            }
            );

            if(startBuyingSelling)
                buysell();

        }

        async void buysell()
        {

            decimal curPriceDiff = CurrentPrice - CurrentAveragePrice;

            if (curPriceDiff <= 0.02m) //below average: sell
            {
                if (!sellOrderFilled) //if not already sold
                {
                    if (MaxSell > 0)
                    {
                        await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", "0.01", "100.00", true);
                        MaxSell = 0;
                        MaxBuy = 5;
                    }

                    sellOrderFilled = true;
                    buyOrderFilled = false;
                }

            }

            if (curPriceDiff >= 0.02m) //above average: buy
            {
                if (!buyOrderFilled) //if not already bought
                {
                    if (MaxBuy > 0)
                    {
                        await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", "10.00", true);
                        MaxSell = 5;
                        MaxBuy = 0;
                    }

                    sellOrderFilled = false;
                    buyOrderFilled = true;
                }

            }



        }

        private void btnStartBySelling_Click(object sender, RoutedEventArgs e)
        {
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
                    lblUpdatedTime.Content = DateTime.UtcNow.ToLocalTime();
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
                await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", "10.00", true);
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
                await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", "0.01", "100.00", true);
                //await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", "10.00", true);
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

    }
}
