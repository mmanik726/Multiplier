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

namespace Multiplier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Loaded += MainWindow_Loaded1;
        }

        private void MainWindow_Loaded1(object sender, RoutedEventArgs e)
        {
            //MyBrowser.ObjectForScripting

            try
            {

                //var fileName = @"c$\Users\bobby\source\repos\CoinbaseExchange.NET-master\CoinbaseExchange.NET-master\Multiplier\Resources\" + "TradeWidgetPage.html"; //System.AppDomain.CurrentDomain.BaseDirectory + @"TradeWidgetPage.html";
                //MyBrowser.Navigate(new Uri("file://127.0.0.1/" + fileName));
                var file2 = @"C:\Users\bobby\source\repos\CoinbaseExchange.NET-master\CoinbaseExchange.NET-master\Multiplier\Resources\" + "TradeWidgetPage.html";
                System.Diagnostics.Process.Start(file2);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {



            //var myPassphrase = "";
            //var myKey = "";
            //var mySecret = "";


            testBook();



            //TickerClient LtcTickerClient = new TickerClient("LTC-USD");
            //LtcTickerClient.Update += LtcTickerClient_Update;

            //btnStart.IsEnabled = false;


        }


        public async void testBook()
        {


            var myPassphrase = "";
            var myKey = "";
            var mySecret = "";
            CBAuthenticationContainer myAuth = new CBAuthenticationContainer(myKey, myPassphrase, mySecret);

            MyOrderBook myOrderBook = new MyOrderBook(myAuth);

            //var x = await myOrderBook.ListAllOrders();
            //var delOrder = x[0].id;
            //var z = await myOrderBook.CancelSingleOrder(delOrder);
            //var z = await myOrderBook.CancelAllOrders();
            //await Task.Delay(1000000);
            var o = myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "1.0", "1.0"); 


        }

        public void LtcTickerClient_Update(object sender, EventArgs args)
        {
            var tickerData = (TickerMessage)args;
            //var z = a.Sells.FirstOrDefault();
            //Console.WriteLine("Update: price {0}", tickerData.CurrentPrice);

            lblCurPrice.Content = tickerData.RealTimePrice.ToString();

        }

    }
}
