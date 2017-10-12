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
using static Multiplier.Manager;

namespace Multiplier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        Manager LTCManager; 

        private const string myPassphrase = "n6yci6u4i0g";
        private const string myKey = "b006d82554b495e227b9e7a1251ad745";
        private const string mySecret = "NhAb9pmbZaY9cPb2+eXOGWIILje7iFe/nF+dV9n6FOxazl6Kje2/03GuSiQYTsj3a/smh92m/lrvfu7kYkxQMg==";
        LogWindow logWindow;

        private static decimal sharedCurrentAveragePrice;


        private string SelectedProduct; 

        public MainWindow()
        {
            InitializeComponent();


            logWindow = new LogWindow();
            logWindow.Show();
            InitListView();

            cmbProduct.Items.Add("LTC-USD");
            cmbProduct.Items.Add("BTC-USD");

            //////LTCManager = new Manager("LTC-USD", myPassphrase, myKey, mySecret);
            ////////LTCManager = new Manager("BTC-USD", myPassphrase, myKey, mySecret);

            //////LTCManager.BuySellAmountChangedEvent += BuySellAmountChangedEventHandler;
            //////LTCManager.BuySellBufferChangedEvent += BuySellBufferChangedEventHandler;
            //////LTCManager.OrderFilledEvent += OrderFilledEventHandler;
            ////////LTCManager.SmaParametersUpdatedEvent += SmaParametersUpdatedEventHandler;
            //////LTCManager.SmaUpdateEvent += SmaUpdateEventHandler;
            //////LTCManager.TickerPriceUpdateEvent += TickerPriceUpdateEventHandler;
            //////LTCManager.AutoTradingStartedEvent += AutoTradingStartedEventHandler;

            //////LTCManager.TickerConnectedEvent += TickerConnectedEventHandler;
            //////LTCManager.TickerDisConnectedEvent += TickerDisConnectedEventHandler;

            //////LTCManager.InitializeManager(); 


            //////sharedCurrentAveragePrice = 0;



            //////LTCManager.UpdateBuySellAmount(0.01m);
            //////LTCManager.UpdateBuySellBuffer(0.03m);

            ////////lblBuySellBuffer.Content = sharedPriceBuffer.ToString();
            ////////lblBuySellAmount.Content = sharedBuySellAmount; 

            
        }

        private void TickerDisConnectedEventHandler(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                lblWarning.Visibility = Visibility.Visible;
                lblWarning.Foreground = Brushes.Red;
                lblWarning.Content = "Warning: Realtime Data Offline. Autotrading OFF"; 
            });

        }

        private void TickerConnectedEventHandler(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                lblWarning.Visibility = Visibility.Hidden;


                //btnStartByBuying.IsEnabled = false;
                //btnStartBySelling.IsEnabled = false;
                //lblWarning.Foreground = Brushes.Red;
                //lblWarning.Content = "Warning: Realtime Data Offline. Autotrading OFF";
            });

        }

        public void BuySellAmountChangedEventHandler(object sender, EventArgs args)
        {
            var curBuySellAmount = ((BuySellAmountUpdateArgs)args).NewBuySellAmount;

            var msg = "Buy sell amount changed to: " + curBuySellAmount.ToString();
            //MessageBox.Show(msg);

            lblBuySellAmount.Content = curBuySellAmount.ToString();

        }


        public void BuySellBufferChangedEventHandler(object sender, EventArgs args)
        {
            var data = (BuySellBufferUpdateArgs)args;
            var msg = "Buy Sell Price buffer updated to " + data.NewBuySellBuffer.ToString();
            //MessageBox.Show(msg);

            this.Dispatcher.Invoke(() => 
            {
                lblBuySellBuffer.Content = data.NewBuySellBuffer.ToString();
            });
            

        }

        public void OrderFilledEventHandler(object sender, EventArgs args)
        {
            var filledOrder = ((OrderUpdateEventArgs)args);


            //MessageBox.Show(string.Format("{0} order ({1}) filled @{2} ", filledOrder.side, filledOrder.OrderId, filledOrder.filledAtPrice));


            this.Dispatcher.Invoke(() => updateListView(filledOrder));
        }


        //public void SmaParametersUpdatedEventHandler(object sender, EventArgs args)
        //{

        //    var data = (SmaParamUpdateArgs)args;

        //    this.Dispatcher.Invoke(() => 
        //    {
        //        txtSmaSlices.Text = data.NewSlices.ToString();
        //        txtSmaTimeInterval.Text = data.NewTimeinterval.ToString();
        //    });



        //}

        public void SmaUpdateEventHandler(object sender, EventArgs args)
        {
            var currentSmaData = (MAUpdateEventArgs)args;
            decimal newSmaPrice = currentSmaData.CurrentMAPrice;

            //MessageBox.Show("handling sma update");
            

            sharedCurrentAveragePrice = currentSmaData.CurrentMAPrice; 

            var x = this.Dispatcher.Invoke(() => chkUseSd.IsChecked);
            if (x == true)
            {
                //LTCManager.UpdateBuySellBuffer(currentSmaData.CurrentSd);

                bool isUseInvers = (bool)Dispatcher.Invoke(() => chkUseInverse.IsChecked);

                LTCManager.UpdateBuySellBuffer(currentSmaData.CiBuffer, isUseInvers); //use the 95% confidence interval buffer
            }

            this.Dispatcher.Invoke(() =>
            {
                lblSma.SetValue(ContentProperty, newSmaPrice);
                lblUpdatedTime.Content = DateTime.UtcNow.ToLocalTime().ToLongTimeString();
                lblSmaValue.Content = "SMA-" + currentSmaData.CurrentSlices.ToString() + " (" + currentSmaData.CurrentTimeInterval.ToString() + " min)";
                lblSd.Content = currentSmaData.CurrentSd;
                btnUpdateSmaInterval.IsEnabled = true;
                //txtPriceBuffer.Text = currentSmaData.CurrentSd.ToString();
            });


        }

        public void TickerPriceUpdateEventHandler(object sender, EventArgs args)
        {
            var tickerData = (TickerMessage)args;

            this.Dispatcher.Invoke(() =>
            {
                lblCurPrice.Content = tickerData.RealTimePrice.ToString();
                lblTickUpdate1.Content = DateTime.UtcNow.ToLocalTime().ToLongTimeString();

                if (tickerData.RealTimePrice - sharedCurrentAveragePrice >= 0)
                {
                    lblCurPrice.Foreground = Brushes.Green;
                }
                else
                {
                    lblCurPrice.Foreground = Brushes.Red;
                }
            });

        }


        public void AutoTradingStartedEventHandler(object sender,EventArgs args)
        {
            //auto trading started
        }




        private void btnStartBySelling_Click(object sender, RoutedEventArgs e)
        {

            LTCManager.StartTrading_BySelling();

            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;
        }


        private void btnStartByBuying_Click(object sender, RoutedEventArgs e)
        {

            LTCManager.StartTrading_ByBuying();
            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;

        }





        void InitListView()
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

                if (slices <= 1)
                {
                    MessageBox.Show("slices cannot be 1 min or less than 1 min");
                    return;
                }

                if (slices * timeInt > 200)
                {
                    //Logger.WriteLog("Additional data needs to be downloaded if not already downloaded");
                    //MessageBox.Show("max amount of data points available is 200 at the moment");
                    //return;
                }

                btnUpdateSmaInterval.IsEnabled = false;

                try
                {
                    await LTCManager.UpdateSmaParameters(timeInt, slices, true); 
                }
                catch (Exception)
                {
                    var msg = "Error occured  in sma calculations. Please check the numbers. default values used";
                    MessageBox.Show(msg);
                    Logger.WriteLog(msg);
                    //throw;
                }


            }
            catch (Exception)
            {
                MessageBox.Show("invalid sma values"); 
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

                LTCManager.UpdateBuySellBuffer(buySellBuffer, (bool)chkUseInverse.IsChecked);

            }
            catch (Exception ex)
            {
                MessageBox.Show("invalid decimal value for price buffer");
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            decimal tradeAmount = 0.1m; //default 

            try
            {
                tradeAmount = Convert.ToDecimal(txtBuySellAmount.Text);

                if (tradeAmount < 0.001m) 
                {
                    MessageBox.Show("buy sell amount canot be less than 0.001");
                    return;
                }

                LTCManager.UpdateBuySellAmount(tradeAmount);
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

        private void Window_Closed(object sender, EventArgs e)
        {
            //MessageBox.Show("app closing");
            Logger.WriteLog("Multiplier exit");
            System.Environment.Exit(0);
        }

        private async void cmbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var productSelected = (cmbProduct.SelectedItem as string);
            cmbProduct.IsEnabled = false;


            //await this.Dispatcher.Invoke(() => startApp(productSelected));
            await Task.Run(() => { this.Dispatcher.Invoke(() => startApp(productSelected)); });
            //await Task.Run();

        }

        Task<bool> startApp(string inputProduct)
        {

            LTCManager = new Manager(inputProduct, myPassphrase, myKey, mySecret);
            //LTCManager = new Manager("BTC-USD", myPassphrase, myKey, mySecret);

            LTCManager.BuySellAmountChangedEvent += BuySellAmountChangedEventHandler;
            LTCManager.BuySellBufferChangedEvent += BuySellBufferChangedEventHandler;
            LTCManager.OrderFilledEvent += OrderFilledEventHandler;
            //LTCManager.SmaParametersUpdatedEvent += SmaParametersUpdatedEventHandler;
            LTCManager.SmaUpdateEvent += SmaUpdateEventHandler;
            LTCManager.TickerPriceUpdateEvent += TickerPriceUpdateEventHandler;
            LTCManager.AutoTradingStartedEvent += AutoTradingStartedEventHandler;

            LTCManager.TickerConnectedEvent += TickerConnectedEventHandler;
            LTCManager.TickerDisConnectedEvent += TickerDisConnectedEventHandler;

            LTCManager.InitializeManager();

            sharedCurrentAveragePrice = 0;


            LTCManager.UpdateBuySellAmount(0.01m);
            LTCManager.UpdateBuySellBuffer(0.03m);

            return null;
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
