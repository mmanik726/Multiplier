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

        private static decimal BuySellAmount;

        private static bool buyOrderFilled;
        private static bool sellOrderFilled;

        private static bool waitingSellFill;
        private static bool waitingBuyFill;

        private static bool waitingBuyOrSell;

        private static int SmaSlices;
        private static int SmaTimeInterval;


        private static bool startBuyingSelling;

        private static decimal priceBuffer;

        private static DateTime LastTickerUpdateTime;
        private static DateTime LastBuySellTime;

        private static DateTime LastCrossTime;

        private static double waitTimeAfterCrossInMin;

        MovingAverage sma;

        CBAuthenticationContainer myAuth;
        MyOrderBook myOrderBook;
        TickerClient LtcTickerClient;
        public MainWindow()
        {
            InitializeComponent();
            initListView();

            BuySellAmount = 3.0m;//3.0m;

            SmaTimeInterval = 3;
            SmaSlices = 40;

            priceBuffer = 0.05m;

            CurrentRealtimePrice = 0;
            CurrentBufferedPrice = 0;
            CurrentAveragePrice = 0;

            buyOrderFilled = false;
            sellOrderFilled = false;

            waitingSellFill = false;
            waitingBuyFill= false;
            waitingBuyOrSell = false;

            waitTimeAfterCrossInMin = SmaTimeInterval; //buy sell every time interval

            MaxBuy = 5;
            MaxSell = 5;

            startBuyingSelling = false;

            LastCrossTime = DateTime.UtcNow.ToLocalTime();

            LtcTickerClient = new TickerClient("LTC-USD");
            LtcTickerClient.PriceUpdated += LtcTickerClient_Update;

            myAuth = new CBAuthenticationContainer(myKey, myPassphrase, mySecret);
            myOrderBook = new MyOrderBook(myAuth, "LTC-USD");
            myOrderBook.OrderUpdateEvent += fillUpdateHandler;

            sma = new MovingAverage(ref LtcTickerClient, SmaTimeInterval, SmaSlices);
            sma.MovingAverageUpdated += SmaUpdateEventHandler;




            txtSmaSlices.Text = SmaSlices.ToString();
            txtSmaTimeInterval.Text = SmaTimeInterval.ToString();

            lblBuySellBuffer.Content = priceBuffer.ToString();
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

            this.Dispatcher.Invoke(() => updateListView(filledOrder));
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

            //if (curPriceDiff <= priceBuffer) //below average: sell
            if (CurrentBufferedPrice <= (CurrentAveragePrice - priceBuffer))
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
                        try
                        {
                            await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", BuySellAmount.ToString(), CurrentBufferedPrice.ToString(), true);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("error selling: " + ex.Message);
                            //throw;
                        }
                        

                    }


                }

            }


            //if (curPriceDiff >= priceBuffer) //above average: buy
            if (CurrentBufferedPrice >= (CurrentAveragePrice + priceBuffer))
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

                        try
                        {
                            await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", BuySellAmount.ToString(), CurrentBufferedPrice.ToString(), true);

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error buying: " + ex.Message);
                            //throw;
                        }

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

            var currentSmaData = (MAUpdateEventArgs)args;
            decimal newSmaPrice = currentSmaData.CurrentMAPrice;

            CurrentAveragePrice = newSmaPrice; 

            this.Dispatcher.Invoke(() => 
                {
                    lblSma.SetValue(ContentProperty, newSmaPrice);
                    lblUpdatedTime.Content = DateTime.UtcNow.ToLocalTime().ToLongTimeString();
                    lblSmaValue.Content = "SMA-" + SmaSlices.ToString() + " (" + SmaTimeInterval.ToString() + " min)";
                    lblSd.Content = currentSmaData.CurrentSd; 
                }
            );
            Debug.WriteLine(string.Format("SMA updated: {0} {1}",newSmaPrice, DateTime.UtcNow.ToLocalTime().ToLongTimeString()));
            //lblSma.Content = newSmaPrice.ToString();
            //Debug.WriteLine(DateTime.UtcNow.ToString());
        }


        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {


            //try
            //{
            //    //await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", "0.01", "80.00", true);
            //    await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", CurrentRealtimePrice.ToString(), true);
            //}
            //catch (Exception ex)
            //{

            //    Debug.WriteLine(ex.Message);
            //}


        }



        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", "0.01", CurrentRealtimePrice.ToString(), true);
            //    //await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", "0.01", "10.00", true);
            //}
            //catch (Exception ex)
            //{

            //    System.Diagnostics.Debug.WriteLine(ex.Message);
            //}
        }

        void initListView()
        {
            // Add columns
            var grdView = new GridView();

            lstView.View = grdView;

            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Buy Time",
                DisplayMemberBinding = new Binding("buyTime")
            });

            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Buy price",
                DisplayMemberBinding = new Binding("buyPrice")
            });
            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Buy size",
                DisplayMemberBinding = new Binding("buySize")
            });
            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Buy fee",
                DisplayMemberBinding = new Binding("buyFee")
            });
            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Sell price",
                DisplayMemberBinding = new Binding("sellPrice")
            });
            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Sell size",
                DisplayMemberBinding = new Binding("sellSize")
            });
            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Sell fee",
                DisplayMemberBinding = new Binding("sellFee")
            });
            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Sell Time",
                DisplayMemberBinding = new Binding("sellTime")
            });
            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Total Fee",
                DisplayMemberBinding = new Binding("feeTotal")
            });

            grdView.Columns.Add(new GridViewColumn
            {
                Header = "Profit/Loss",
                DisplayMemberBinding = new Binding("pl")
            });
        }

        private void updateListView(OrderUpdateEventArgs fillData)
        {

            if (fillData.side == "buy")
            {
                RowItem rowData = new RowItem();

                rowData.buyPrice = fillData.filledAtPrice;
                rowData.buySize = fillData.fillSize;
                var dt = DateTime.UtcNow.ToLocalTime();
                rowData.buyTime = dt.ToShortDateString() + " " + dt.ToLongTimeString();
                rowData.buyFee = fillData.fillFee;

                lstView.Items.Add(rowData);
            }
            else
            {
                int lastItem = lstView.Items.Count -1;

                RowItem rowDataItem;

                if (lastItem >= 0) //no item in list 
                {
                    rowDataItem = (RowItem)(lstView.Items.GetItemAt(lastItem));
                }
                else
                {
                    rowDataItem = new RowItem
                    {
                        buyPrice = "0",
                        buyFee = "0",
                        buySize = "0",
                        buyTime = ""
                    };
                }

                

                rowDataItem.sellPrice = fillData.filledAtPrice;
                rowDataItem.sellSize = fillData.fillSize;
                var dt = DateTime.UtcNow.ToLocalTime();
                rowDataItem.sellTime = dt.ToShortDateString() + " " + dt.ToLongTimeString();
                rowDataItem.sellFee = fillData.fillFee;

                var feeTotal = (Convert.ToDecimal(rowDataItem.buyFee) + Convert.ToDecimal(rowDataItem.sellFee));

                rowDataItem.feeTotal = feeTotal.ToString();

                var diff = Convert.ToDecimal(rowDataItem.sellPrice) - Convert.ToDecimal(rowDataItem.buyPrice);
                var profitLoss = (diff * (Convert.ToDecimal(rowDataItem.sellSize))) - feeTotal; 

                rowDataItem.pl = (profitLoss).ToString();

                if (lastItem >= 0)
                {
                    lstView.Items.RemoveAt(lastItem);
                    lstView.Items.Insert(lastItem, rowDataItem);
                }
                else
                {
                    lstView.Items.Add(rowDataItem);
                }


            }



            // Populate list

            var gridView = (GridView)lstView.View;
            gridView.Columns.ToList().ForEach((c) => { c.Width = 0; c.Width = double.NaN; });

        }

        private async void btnUpdateSmaInterval_Click(object sender, RoutedEventArgs e)
        {

            int timeInt = 3;
            int slices = 40;

            try
            {
                timeInt = Convert.ToInt16(txtSmaTimeInterval.Text);
                slices = Convert.ToInt16(txtSmaSlices.Text);

                if (timeInt < 1)
                {
                    MessageBox.Show("time interval cannot be less than 1 min");
                    return;
                }

                if (slices < 1)
                {
                    MessageBox.Show("slices cannot be less than 1 min");
                    return;
                }

                if (slices * timeInt > 200)
                {
                    MessageBox.Show("max amount of data points available is 200 at the moment");
                    return;
                }



                bool errorOccured = false;

                try
                {
                    sma.updateValues(timeInt, slices) ;
                    SmaSlices = slices;
                    SmaTimeInterval = timeInt;
                    waitTimeAfterCrossInMin = SmaTimeInterval;
                }
                catch (Exception)
                {
                    errorOccured = true;
                    MessageBox.Show("Error occured  in sma calculations. Please check the numbers. default values used");

                    sma.updateValues(3, 40);
                    SmaSlices = 40;
                    SmaTimeInterval = 3;
                    waitTimeAfterCrossInMin = SmaTimeInterval;
                    //throw;
                }

                if (!errorOccured)
                {
                    var updateMsg = string.Format("SMA parameters updated. Time interval: {0}, Slices: {1}", timeInt, slices);
                    Debug.WriteLine(updateMsg);
                    MessageBox.Show(updateMsg);
                }


            }
            catch (Exception)
            {
                MessageBox.Show("invalid sma values"); 

                //throw;
            }



        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            decimal buySellBuffer = 0.03m; //default 

            try
            {
                buySellBuffer = Convert.ToDecimal(txtPriceBuffer.Text);

                if (buySellBuffer < 0)
                {
                    MessageBox.Show("price buffer cannot be less than 0");
                    return;
                }

                updateBuySellBuffer(buySellBuffer);
                var msg = "price buffer updated to " + 
                    buySellBuffer.ToString() + " at " 
                    + DateTime.UtcNow.ToLocalTime().ToShortDateString() + DateTime.UtcNow.ToLocalTime().ToLongTimeString();
                MessageBox.Show(msg);
                Debug.WriteLine(msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("invalid decimal value for price buffer");
            }
        }

        public void updateBuySellBuffer(decimal newPriceBuffer)
        {
            priceBuffer = newPriceBuffer;
            lblBuySellBuffer.Content = priceBuffer.ToString();
        }
    }



    class RowItem
    {
        public string buyPrice { get; set; }
        public string buySize { get; set; }
        public string buyFee { get; set; }
        public string buyTime { get; set; }

        public string sellPrice { get; set; }
        public string sellSize { get; set; }
        public string sellFee { get; set; }
        public string sellTime { get; set; }

        public string feeTotal { get; set; }
        public string pl { get; set; }

    }
}
