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
using CoinbaseExchange.NET.Utilities;

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

        private static decimal sharedCurrentRealtimePrice;

        private static decimal sharedCurrentBufferedPrice; 

        private static decimal sharedCurrentAveragePrice;
        private static decimal sharedMaxBuy;
        private static decimal sharedMaxSell;

        private static decimal sharedBuySellAmount;

        private static bool sharedBuyOrderFilled;
        private static bool sharedSellOrderFilled;

        private static bool sharedWaitingSellFill;
        private static bool sharedWaitingBuyFill;

        private static bool sharedWaitingBuyOrSell;

        private static int sharedSmaSlices;
        private static int sharedSmaTimeInterval;


        private static bool sharedStartBuyingSelling;

        private static decimal sharedPriceBuffer;

        private static DateTime sharedLastTickerUpdateTime;
        private static DateTime sharedLastBuySellTime;

        private static DateTime sharedLastCrossTime;

        private static double sharedWaitTimeAfterCrossInMin;

        MovingAverage sharedSma;

        CBAuthenticationContainer myAuth;
        MyOrderBook myOrderBook;
        TickerClient LtcTickerClient;
        LogWindow logWindow;

        public MainWindow()
        {
            InitializeComponent();

            logWindow = new LogWindow();
            logWindow.Show();


            initListView();

            sharedBuySellAmount = 0.01m;//3.0m;

            sharedSmaTimeInterval = 3;
            sharedSmaSlices = 40;

            sharedPriceBuffer = 0.05m;

            sharedCurrentRealtimePrice = 0;
            sharedCurrentBufferedPrice = 0;
            sharedCurrentAveragePrice = 0;

            sharedBuyOrderFilled = false;
            sharedSellOrderFilled = false;

            sharedWaitingSellFill = false;
            sharedWaitingBuyFill= false;
            sharedWaitingBuyOrSell = false;

            sharedWaitTimeAfterCrossInMin = sharedSmaTimeInterval; //buy sell every time interval

            sharedMaxBuy = 5;
            sharedMaxSell = 5;

            sharedStartBuyingSelling = false;

            sharedLastCrossTime = DateTime.UtcNow.ToLocalTime();

            LtcTickerClient = new TickerClient("LTC-USD");
            LtcTickerClient.PriceUpdated += LtcTickerClient_Update;

            myAuth = new CBAuthenticationContainer(myKey, myPassphrase, mySecret);
            myOrderBook = new MyOrderBook(myAuth, "LTC-USD");
            myOrderBook.OrderUpdateEvent += fillUpdateHandler;

            sharedSma = new MovingAverage(ref LtcTickerClient, sharedSmaTimeInterval, sharedSmaSlices);
            sharedSma.MovingAverageUpdated += SmaUpdateEventHandler;




            txtSmaSlices.Text = sharedSmaSlices.ToString();
            txtSmaTimeInterval.Text = sharedSmaTimeInterval.ToString();

            lblBuySellBuffer.Content = sharedPriceBuffer.ToString();
            lblBuySellAmount.Content = sharedBuySellAmount; 

            
        }





        public void fillUpdateHandler(object sender, EventArgs args)
        {

            var filledOrder = ((OrderUpdateEventArgs)args);

            Logger.WriteLog(string.Format("{0} order ({1}) filled @{2}", filledOrder.side, filledOrder.OrderId, filledOrder.filledAtPrice));

            //MessageBox.Show(string.Format("{0} order ({1}) filled @{2} ", filledOrder.side, filledOrder.OrderId, filledOrder.filledAtPrice));

            if (filledOrder.side == "buy")
            {
                sharedBuyOrderFilled = true;
                sharedSellOrderFilled = false;

                sharedWaitingBuyFill = false;
                sharedWaitingSellFill = true;
            }
            else if (filledOrder.side == "sell")
            {
                sharedSellOrderFilled = true;
                sharedBuyOrderFilled = false;

                sharedWaitingSellFill = false;
                sharedWaitingBuyFill = true;
            }

            sharedWaitingBuyOrSell = false;

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

            sharedLastTickerUpdateTime = DateTime.UtcNow.ToLocalTime();

            sharedCurrentRealtimePrice = tickerData.RealTimePrice;

            //Logger.WriteLog(CurrentRealtimePrice + "-" + CurrentBufferedPrice);

            this.Dispatcher.Invoke(() =>
            {
                lblCurPrice.Content = tickerData.RealTimePrice.ToString();
                lblTickUpdate1.Content = sharedLastTickerUpdateTime.ToLongTimeString();

                if (sharedCurrentRealtimePrice - sharedCurrentAveragePrice >= 0)
                {
                    lblCurPrice.Foreground = Brushes.Green;
                }
                else
                {
                    lblCurPrice.Foreground = Brushes.Red;
                }
            }
            );

            if (sharedStartBuyingSelling)
            {
               if ((sharedLastTickerUpdateTime - sharedLastBuySellTime).TotalMilliseconds < 1000)
                {
                    //Logger.WriteLog("price skipped: " + CurrentRealtimePrice);
                    return;
                }

                sharedCurrentBufferedPrice = sharedCurrentRealtimePrice;

                var secSinceLastCrosss = (DateTime.UtcNow.ToLocalTime() - sharedLastCrossTime).TotalSeconds;
                if (secSinceLastCrosss < (sharedWaitTimeAfterCrossInMin * 60))
                {
                    //if the last time prices crossed is < 2 min do nothing
                    Logger.WriteLog(string.Format("Waiting {0} sec before placing any new order", Math.Round((sharedWaitTimeAfterCrossInMin * 60) - secSinceLastCrosss)));
                    return;
                }


                if (!sharedWaitingBuyOrSell)
                {
                    sharedLastBuySellTime = DateTime.UtcNow.ToLocalTime();

                    buysell();
                }
                else
                {
                    Logger.WriteLog("Buysell already in progress");
                }
            }


        }

        async void buysell()
        {


            if (sharedWaitingBuyOrSell)
            {
                Logger.WriteLog("this should never print");
                return;
            }

            decimal curPriceDiff = sharedCurrentBufferedPrice - sharedCurrentAveragePrice;

            //if (curPriceDiff <= priceBuffer) //below average: sell
            if (sharedCurrentBufferedPrice <= (sharedCurrentAveragePrice - sharedPriceBuffer))
            {
                if (!sharedSellOrderFilled) //if not already sold
                {
                    sharedSellOrderFilled = true;
                    sharedBuyOrderFilled = false;

                    if (sharedMaxSell > 0)
                    {
                        sharedMaxSell = 0;
                        sharedMaxBuy = 5;

                        sharedWaitingSellFill = true;
                        sharedWaitingBuyOrSell = true;

                        sharedLastCrossTime = DateTime.UtcNow.ToLocalTime();
                        try
                        {
                            await myOrderBook.PlaceNewLimitOrder("sell", "LTC-USD", sharedBuySellAmount.ToString(), sharedCurrentBufferedPrice.ToString(), true);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog("error selling: " + ex.Message);
                            //throw;
                        }
                        

                    }


                }

            }


            //if (curPriceDiff >= priceBuffer) //above average: buy
            if (sharedCurrentBufferedPrice >= (sharedCurrentAveragePrice + sharedPriceBuffer))
            {
                if (!sharedBuyOrderFilled) //if not already bought
                {

                    sharedSellOrderFilled = false;
                    sharedBuyOrderFilled = true;

                    if (sharedMaxBuy > 0)
                    {
                        sharedMaxSell = 5;
                        sharedMaxBuy = 0;

                        sharedWaitingBuyFill = true;
                        sharedWaitingBuyOrSell = true;

                        sharedLastCrossTime = DateTime.UtcNow.ToLocalTime();

                        try
                        {
                            await myOrderBook.PlaceNewLimitOrder("buy", "LTC-USD", sharedBuySellAmount.ToString(), sharedCurrentBufferedPrice.ToString(), true);

                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog("Error buying: " + ex.Message);
                            //throw;
                        }

                    }

                }

            }



        }

        private void btnStartBySelling_Click(object sender, RoutedEventArgs e)
        {
            sharedLastBuySellTime = DateTime.UtcNow.ToLocalTime();
            sharedLastCrossTime = DateTime.UtcNow.ToLocalTime().AddSeconds(-1 * sharedWaitTimeAfterCrossInMin * 60);

            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;

            sharedBuyOrderFilled = true;
            sharedSellOrderFilled = false;

            sharedMaxBuy = 0;
            sharedMaxSell = 5;

            sharedStartBuyingSelling = true;
        }


        private void btnStartByBuying_Click(object sender, RoutedEventArgs e)
        {
            sharedLastBuySellTime = DateTime.UtcNow.ToLocalTime();
            sharedLastCrossTime = DateTime.UtcNow.ToLocalTime().AddSeconds(-1 * sharedWaitTimeAfterCrossInMin * 60);

            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;

            sharedBuyOrderFilled = false;
            sharedSellOrderFilled = true;

            sharedMaxBuy = 5;
            sharedMaxSell = 0;

            sharedStartBuyingSelling = true;
        }

        private void SmaUpdateEventHandler(object sender, EventArgs args)
        {

            var currentSmaData = (MAUpdateEventArgs)args;
            decimal newSmaPrice = currentSmaData.CurrentMAPrice;

            sharedCurrentAveragePrice = newSmaPrice;

            //update the buy / sell buffer 
            var x = this.Dispatcher.Invoke(()=> chkUseSd.IsChecked);
            if (x == true)
                updateBuySellBuffer(currentSmaData.CurrentSd);

            this.Dispatcher.Invoke(() => 
                {
                    lblSma.SetValue(ContentProperty, newSmaPrice);
                    lblUpdatedTime.Content = DateTime.UtcNow.ToLocalTime().ToLongTimeString();
                    lblSmaValue.Content = "SMA-" + sharedSmaSlices.ToString() + " (" + sharedSmaTimeInterval.ToString() + " min)";
                    lblSd.Content = currentSmaData.CurrentSd;
                    //txtPriceBuffer.Text = currentSmaData.CurrentSd.ToString();
                }
            );
            Logger.WriteLog(string.Format("SMA updated: {0}",newSmaPrice));
            //lblSma.Content = newSmaPrice.ToString();
            //Logger.WriteLog(DateTime.UtcNow.ToString());
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

            //    Logger.WriteLog(ex.Message);
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

            //    System.Diagnostics.Logger.WriteLog(ex.Message);
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
                    Logger.WriteLog("Additional data needs to be downloaded if not already downloaded");
                    //MessageBox.Show("max amount of data points available is 200 at the moment");
                    //return;
                }



                bool errorOccured = false;

                try
                {
                    sharedSma.updateValues(timeInt, slices) ;
                    sharedSmaSlices = slices;
                    sharedSmaTimeInterval = timeInt;
                    sharedWaitTimeAfterCrossInMin = sharedSmaTimeInterval;
                }
                catch (Exception)
                {
                    errorOccured = true;
                    MessageBox.Show("Error occured  in sma calculations. Please check the numbers. default values used");

                    sharedSma.updateValues(3, 40);
                    sharedSmaSlices = 40;
                    sharedSmaTimeInterval = 3;
                    sharedWaitTimeAfterCrossInMin = sharedSmaTimeInterval;
                    //throw;
                }

                if (!errorOccured)
                {
                    var updateMsg = string.Format("SMA parameters updated. Time interval: {0}, Slices: {1}", timeInt, slices);
                    Logger.WriteLog(updateMsg);
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
                Logger.WriteLog(msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("invalid decimal value for price buffer");
            }
        }

        public void updateBuySellBuffer(decimal newPriceBuffer)
        {



            decimal tempValue = 0;
            bool x;
            x = Dispatcher.Invoke(() => chkUseInverse.IsChecked.Value);

            if (x == null)
                x = false;

            if (x == true)
                tempValue = 1 / (newPriceBuffer * (10*100));
            else
                tempValue = newPriceBuffer;

            sharedPriceBuffer = Math.Round( tempValue,3);
            Dispatcher.Invoke(() => lblBuySellBuffer.Content = sharedPriceBuffer.ToString());
            //lblBuySellBuffer.Content = sharedPriceBuffer.ToString();

        }


        public void updateBuySellAmount(decimal amount)
        {
            if (amount <= 0)
            {
                Logger.WriteLog("buy sell amount cannot be less than or equal to 0");
                return;
            }

            sharedBuySellAmount = amount;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            decimal tradeAmount = 0.1m; //default 

            try
            {
                tradeAmount = Convert.ToDecimal(txtBuySellAmount.Text);

                if (tradeAmount <= 0.01m) 
                {
                    MessageBox.Show("buy sell amount canot be less than 0.01");
                    return;
                }

                updateBuySellAmount(tradeAmount);
                lblBuySellAmount.Content = tradeAmount.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show("invalid buy sell amount");

            }

        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {
            if (logWindow != null)
            {
                logWindow.Close();
                logWindow = null;
                logWindow = new LogWindow();
                logWindow.Show();
                //logWindow.Closed += LogWindow_Closed;
            }
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
