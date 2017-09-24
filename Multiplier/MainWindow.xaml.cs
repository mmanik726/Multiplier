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

        CBAuthenticationContainer myAuth;
        MyOrderBook myOrderBook;

        public MainWindow()
        {
            InitializeComponent();


            TestMethod();
            myAuth = new CBAuthenticationContainer(myKey, myPassphrase, mySecret);
            myOrderBook = new MyOrderBook(myAuth, "LTC-USD");

        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {



            TestMethod();



        }


        public void fillUpdateHandler(object sender, EventArgs args)
        {
            
            var filledOrderId = ((FillEventArgs)args).Fills.FirstOrDefault().OrderId;
            MessageBox.Show("order filled! " + filledOrderId);
        }

        public void OrderUpdateHandler(object sender, EventArgs args)
        {

            var orderUpdate = ((OrderUpdateEventArgs)args);
            MessageBox.Show(orderUpdate.Message);
        }

        public async void TestMethod()
        {


            CBAuthenticationContainer myAuth = new CBAuthenticationContainer(myKey, myPassphrase, mySecret);


            TickerClient LtcTickerClient = new TickerClient("LTC-USD");
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

            lblCurPrice.Content = tickerData.RealTimePrice.ToString();

        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {


            HistoricPrices h = new HistoricPrices();
            //h.testJson();
            var x = await h.GetPrices("LTC-USD");

            var data = from t in x
                       where t.Average > 52
                       select t;
            var tmp = (from t in x
                       select t.Average).Sum();

            var maxDate = (from md in x
                           select md.Time).Max();


            var minDate = (from md in x
                           select md.Time).Min();

            var avg = tmp / x.Count();
            MessageBox.Show("Average is " + avg.ToString() + "\nFrom: " + minDate.ToString() + 
                "\nTo: " + maxDate.ToString() + 
                "\n hours: " + (maxDate - minDate).Hours);
            
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {


            try
            {
                await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", "0.01", "80.00", true);
                //await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", "10.00", true);
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            

        }
    }
}
