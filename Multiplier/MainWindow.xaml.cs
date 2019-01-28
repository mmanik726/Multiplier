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

using Newtonsoft.Json.Linq;


using CoinbaseExchange.NET.Endpoints.Funds;
using System.Threading;

namespace Multiplier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        GraphWindow _graphWindow;

        Manager ProductManager;

        private string myPassphrase; // = "n6yci6u4i0g";
        private string myKey; // = "b006d82554b495e227b9e7a1251ad745";
        private string mySecret; // = "NhAb9pmbZaY9cPb2+eXOGWIILje7iFe/nF+dV9n6FOxazl6Kje2/03GuSiQYTsj3a/smh92m/lrvfu7kYkxQMg==";
        LogWindow logWindow;

        private static decimal sharedCurrentLargeSmaPrice;


        private string SelectedProduct;

        private static bool AutoTradingOn;

        private bool LogAutoScrolling;

        private RadioButton rdBtnCurrentSelection; 

        public MainWindow()
        {
            

            InitializeComponent();

            _graphWindow = new GraphWindow();

            


            LogAutoScrolling = true;

            Logger.Logupdated += LogUpdatedHandler;
            txtMainLog.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            txtMainLog.Document.PageWidth = 1000;

            rdBtnCurrentSelection = getCurrentRdoBtnSelection();

            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Dispatcher.Invoke(()=> 
            {
                FrmMainWindow.Title = "Mani (" + appVersion.ToString() + ")"; 
            });

            //logWindow = new LogWindow();
            //logWindow.Show();
            InitListView();

            cmbProduct.Items.Add("BTC-USD");
            cmbProduct.Items.Add("ETH-USD");
            cmbProduct.Items.Add("LTC-USD");
            

            //btnSellAtNow.IsEnabled = false;
            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;

            
        }


        

        private void LogUpdatedHandler(object sender, EventArgs args)
        {
            var msg = (LoggerEventArgs)args;
            try
            {

                this.Dispatcher.Invoke(() => txtMainLog.AppendText(msg.LogMessage));

            }
            catch (Exception)
            {
                Console.WriteLine("error appending to text box");
                //throw;
            }

        }

        private void txtMainLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            // scroll it automatically
            if (LogAutoScrolling)
            {
                txtMainLog.ScrollToEnd();
            }
        }

        private void txtMainLog_GotFocus(object sender, RoutedEventArgs e)
        {
            LogAutoScrolling = false;
        }


        private void txtMainLog_LostFocus(object sender, RoutedEventArgs e)
        {
            LogAutoScrolling = true;
        }

        private void TickerDisConnectedEventHandler(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                lblWarning.Visibility = Visibility.Visible;
                lblWarning.Foreground = Brushes.Red;
                lblWarning.Content = "Warning: Realtime Data Offline"; 
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

            Dispatcher.Invoke(() => 
            {
                lblBuySellAmount.Content = curBuySellAmount.ToString();
            });

        }


        public void BuySellBufferChangedEventHandler(object sender, EventArgs args)
        {
            var data = (BuySellBufferUpdateArgs)args;
            var msg = "Buy Sell Price buffer updated to " + data.NewBuySellBuffer.ToString();
            //MessageBox.Show(msg);

            //this.Dispatcher.Invoke(() => 
            //{
            //    lblBuySellBuffer.Content = data.NewBuySellBuffer.ToString();
            //});
            

        }

        public void PriceBelowAverageEventHandler(object sender, EventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                btnStartByBuying.IsEnabled = false;
                //btnSellAtNow.IsEnabled = false;
            });
        }

        public void PriceAboveAverageEventHandler(object sender, EventArgs args)
        {
            //Dispatcher.Invoke(() =>
            //{

            //});
        }

        public void OrderFilledEventHandler(object sender, EventArgs args)
        {
            OrderUpdateEventArgs filledOrder = null;

            //if (args is ForcedOrderFilledEventArgs)
            //{
            //    filledOrder = (ForcedOrderFilledEventArgs)args;

            //    Dispatcher.Invoke(() => 
            //    {
            //        if (filledOrder.side != "UNKNOWN")
            //        {
            //            btnSellAtNow.IsEnabled = false;
            //            btnStartByBuying.IsEnabled = true;
            //        }

            //    });

            //}
            //else
            //{
            //    filledOrder = ((OrderUpdateEventArgs)args);
            //}

            filledOrder = ((OrderUpdateEventArgs)args);



            //MessageBox.Show(string.Format("{0} order ({1}) filled @{2} ", filledOrder.side, filledOrder.OrderId, filledOrder.filledAtPrice));

            Dispatcher.Invoke(() => btnBuyAtNow.IsEnabled = true);
            Dispatcher.Invoke(() => btnSellAtNow.IsEnabled = true);

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

        public void SmaLargeUpdateEventHandler(object sender, EventArgs args)
        {
            var currentSmaData = (MAUpdateEventArgs)args;
            decimal newSmaPrice = currentSmaData.CurrentMAPrice;

            //MessageBox.Show("handling sma update");
            

            sharedCurrentLargeSmaPrice = currentSmaData.CurrentMAPrice; 

            //var x = this.Dispatcher.Invoke(() => chkUseSd.IsChecked);
            //if (x == true)
            //{
            //    //LTCManager.UpdateBuySellBuffer(currentSmaData.CurrentSd);

            //    bool isUseInvers = (bool)Dispatcher.Invoke(() => chkUseInverse.IsChecked);

            //    ProductManager.UpdateBuySellBuffer(currentSmaData.CiBuffer, isUseInvers); //use the 95% confidence interval buffer
            //}

            this.Dispatcher.Invoke(() =>
            {
                lblSma.Content = Math.Round(newSmaPrice, 4);
                lblUpdatedTime.Content = DateTime.UtcNow.ToLocalTime().ToLongTimeString();
                lblSmaValue.Content = "SMA-" + currentSmaData.CurrentSlices.ToString() + " (" + currentSmaData.CurrentTimeInterval.ToString() + " min)";
                lblSd.Content = Math.Round(currentSmaData.CurrentSd, 4);
                btnUpdateSmaInterval.IsEnabled = true;
                //txtPriceBuffer.Text = currentSmaData.CurrentSd.ToString();
            });


        }

        //public void SmaSmallUpdateEventHandler(object sender, EventArgs args)
        //{
        //    //var currentSmaData = (MAUpdateEventArgs)args;
        //    //decimal newSmaPrice = currentSmaData.CurrentMAPrice;

        //    ////MessageBox.Show("handling sma update");
            

        //    //sharedCurrentLargeSmaPrice = currentSmaData.CurrentMAPrice; 

        //    ////var x = this.Dispatcher.Invoke(() => chkUseSd.IsChecked);
        //    ////if (x == true)
        //    ////{
        //    ////    //LTCManager.UpdateBuySellBuffer(currentSmaData.CurrentSd);

        //    ////    bool isUseInvers = (bool)Dispatcher.Invoke(() => chkUseInverse.IsChecked);

        //    ////    LTCManager.UpdateBuySellBuffer(currentSmaData.CiBuffer, isUseInvers); //use the 95% confidence interval buffer
        //    ////}

        //    //this.Dispatcher.Invoke(() =>
        //    //{
        //    //    lblSmaSmall.Content = Math.Round(newSmaPrice, 4);
        //    ////lblUpdatedTime.Content = DateTime.UtcNow.ToLocalTime().ToLongTimeString();
        //    ////lblSmaValue.Content = "SMA-" + currentSmaData.CurrentSlices.ToString() + " (" + currentSmaData.CurrentTimeInterval.ToString() + " min)";
        //    //    lblSmallSd.Content = Math.Round(currentSmaData.CurrentSd, 4);
        //    //    btnUpdateSmallSma.IsEnabled = true;
        //    //    //txtPriceBuffer.Text = currentSmaData.CurrentSd.ToString();
        //    //});


        //}


        public void FundsUpdatedHandler(object sender, EventArgs args)
        {
            AvailableFunds af = ((FundsEventArgs)args).AvaFunds ;

            this.Dispatcher.Invoke(()=> 
            {
                lblAvailableProduct.Content = af.AvailableProduct;
                lblAvailableUSD.Content = af.AvailableDollars;


                var maxProd = Math.Round(Math.Max(af.AvailableProduct, af.AvailableDollars / Convert.ToDecimal(lblCurPrice.Content)), 4);

                txtBuySellAmount.Text = maxProd.ToString();//af.AvailableProduct.ToString();
                Button_Click_1(null, null);
            });
        }


        public void TickerPriceUpdateEventHandler(object sender, EventArgs args)
        {
            var tickerData = (TickerMessage)args;

            this.Dispatcher.Invoke(() =>
            {
                lblCurPrice.Content = tickerData.RealTimePrice.ToString();
                var curLongTime = DateTime.UtcNow.ToLocalTime(); 
                lblTickUpdate1.Content = curLongTime.ToLongTimeString() + " " + curLongTime.Millisecond.ToString() ;

                if (tickerData.RealTimePrice - sharedCurrentLargeSmaPrice >= 0)
                {
                    lblCurPrice.Foreground = Brushes.Green;

                    //if (AutoTradingOn) 
                    //    btnSellAtNow.IsEnabled = true;
                }
                else
                {
                    //if (AutoTradingOn)
                    //    btnSellAtNow.IsEnabled = false;

                    lblCurPrice.Foreground = Brushes.Red;
                }
            });

        }


        public void AutoTradingStartedEventHandler(object sender,EventArgs args)
        {
            //auto trading started
            AutoTradingOn = true;

            Dispatcher.Invoke(()=> 
            {
                lblStatus.FontSize = 16;
                lblStatus.Foreground = Brushes.Orange; 
                lblStatus.Content = "Auto Trading ON";
            });

        }

        public void AutoTradingStoppedEventHandler(object sender, EventArgs args)
        {
            //auto trading started
            AutoTradingOn = false;

            Dispatcher.Invoke(() =>
            {
                lblStatus.FontSize = 16;
                lblStatus.Foreground = Brushes.Black;
                lblStatus.Content = "Auto Trading OFF";

                btnStartByBuying.IsEnabled = true;
                btnStartBySelling.IsEnabled = true;
                
            });
        }


        private void btnStartBySelling_Click(object sender, RoutedEventArgs e)
        {


            if (isNumeric(txtBuySellAmount.Text))
            {
                var amount = Math.Round(Convert.ToDecimal(txtBuySellAmount.Text), 4);
                if (MessageBox.Show("Is the buy/sell amount of " + amount.ToString() + " correct?", "Confirm start by selling", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {

                    ProductManager.StartTrading_BySelling();
                    btnStartByBuying.IsEnabled = false;
                    btnStartBySelling.IsEnabled = false;
                }
                else
                {
                    txtBuySellAmount.Focus();
                }
            }
            else
            {
                MessageBox.Show("Incorrect buy / sell amount");
                txtBuySellAmount.Focus();
            }


        }


        private bool isNumeric(string inputStr)
        {
            double myNum = 0;

            if (Double.TryParse(inputStr, out myNum))
            {
                return true;
            }
            else
            {
                // it is not a number
                return false;
            }
        }

        private void btnStartByBuying_Click(object sender, RoutedEventArgs e)
        {
            if (isNumeric(txtBuySellAmount.Text))
            {

                var amount = Math.Round(Convert.ToDecimal(txtBuySellAmount.Text), 4);
                if (MessageBox.Show("Is the buy/sell amount of " + amount.ToString() + " correct?", "Confirm start by buying", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ProductManager.StartTrading_ByBuying();
                    btnStartByBuying.IsEnabled = false;
                    btnStartBySelling.IsEnabled = false;
                }
                else
                {
                    txtBuySellAmount.Focus();
                }
            }
            else
            {
                MessageBox.Show("Incorrect buy / sell amount");
                txtBuySellAmount.Focus();
            }



        }


        private void CurrentActionChangedEventHandler(object sender, EventArgs args)
        {
            var curAction = (ActionChangedArgs)args;

            Dispatcher.Invoke(() => 
            {
                lblNextAction.Content = curAction.CurrentAction;
            });
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
                    await ProductManager.UpdateDisplaySmaParameters(timeInt, slices, true); 
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


            //btnUpdateSmallSma_Click(this, null);
        }

        private async void btnUpdateSmallSma_Click(object sender, RoutedEventArgs e)
        {

            //int timeInt = 3;
            //int slices = 20;

            //try
            //{
            //    timeInt = Convert.ToInt16(txtSmallSmaInterval.Text);
            //    slices = Convert.ToInt16(txtSmallSmaSlices.Text);

            //    if (timeInt < 1)
            //    {
            //        MessageBox.Show("time interval cannot be less than 1 min");
            //        return;
            //    }

            //    if (slices <= 1)
            //    {
            //        MessageBox.Show("slices cannot be 1 min or less than 1 min");
            //        return;
            //    }

            //    btnUpdateSmallSma.IsEnabled = false;

            //    try
            //    {
            //        await ProductManager.UpdateSmallSmaParameters(timeInt, slices, true);
            //    }
            //    catch (Exception)
            //    {
            //        var msg = "Error occured  in sma calculations. Please check the numbers. default values used";
            //        MessageBox.Show(msg);
            //        Logger.WriteLog(msg);
            //        //throw;
            //    }


            //}
            //catch (Exception)
            //{
            //    MessageBox.Show("invalid sma values");
            //}

        }

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    decimal buySellBuffer = 0.03m; //default 

        //    try
        //    {
        //        buySellBuffer = Convert.ToDecimal(txtPriceBuffer.Text);

        //        if (buySellBuffer < 0)
        //        {
        //            MessageBox.Show("price buffer cannot be less than 0");
        //            return;
        //        }

        //        ProductManager.UpdateBuySellBuffer(buySellBuffer, (bool)chkUseInverse.IsChecked);

        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("invalid decimal value for price buffer");
        //    }
        //}


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

                ProductManager.UpdateBuySellAmount(tradeAmount);
                lblBuySellAmount.Content = tradeAmount.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show("invalid buy sell amount");

            }

        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {
            //if (logWindow != null)
            //{
            //    logWindow.Close();
            //    logWindow = null;
            //    logWindow = new LogWindow();
            //    logWindow.Show();
            //    //logWindow.Closed += LogWindow_Closed;
            //}
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

            SelectedProduct = productSelected;

            //await this.Dispatcher.Invoke(() => startApp(productSelected));
            await Task.Run(() => { this.Dispatcher.Invoke(() => StartApp(productSelected)); });

            Dispatcher.Invoke(() => 
            {
                btnStartByBuying.IsEnabled = true;
                btnStartBySelling.IsEnabled = true;
            }); 
            //await Task.Run();

        }

        Task<bool> StartApp(string inputProduct)
        {

            var appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var settingsFile = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Mani_" + appVersion + "_Settings.json";

            if (!System.IO.File.Exists(settingsFile))
            {
                MessageBox.Show("Application strategy settings file cannot be found. Please make sure the json settings file exists in the application dir. " +
                    "A sample strategy json file can be found in the resource directory");

                return null;
            }
            
            
            //get user settings 
            var currentUser = Properties.Settings.Default.UserName;

            myPassphrase = Properties.Settings.Default.Passpharase;
            myKey = Properties.Settings.Default.Key;
            mySecret = Properties.Settings.Default.Secret;

            if (currentUser == "" ||
                myPassphrase == "" ||
                myKey == "" ||
                mySecret == "")
            {
                MessageBox.Show("User credentials are empty, please check config file and restart application");
                Environment.Exit(0); 
            }


            Dispatcher.Invoke(() => 
            {
                FrmMainWindow.Title = FrmMainWindow.Title + " User: " + currentUser + " " + inputProduct;  
            });

            Logger.WriteLog("\tCurrent user: " + currentUser + "\n");


            AutoTradingOn = false;

            ProductManager = new Manager(inputProduct, myPassphrase, myKey, mySecret);
            //LTCManager = new Manager("BTC-USD", myPassphrase, myKey, mySecret);

            ProductManager.BuySellAmountChangedEvent += BuySellAmountChangedEventHandler;
            ProductManager.BuySellBufferChangedEvent += BuySellBufferChangedEventHandler;
            ProductManager.OrderFilledEvent += OrderFilledEventHandler;
            //LTCManager.SmaParametersUpdatedEvent += SmaParametersUpdatedEventHandler;
            ProductManager.DisplaySmaUpdateEvent += SmaLargeUpdateEventHandler;
            //ProductManager.SmaSmallUpdateEvent += SmaSmallUpdateEventHandler;

            ProductManager.FundsUpdatedEvent += FundsUpdatedHandler;

            ProductManager.TickerPriceUpdateEvent += TickerPriceUpdateEventHandler;
            ProductManager.AutoTradingStartedEvent += AutoTradingStartedEventHandler;
            ProductManager.AutoTradingStoppedEvent += AutoTradingStoppedEventHandler;

            ProductManager.PriceAboveAverageEvent += PriceAboveAverageEventHandler;
            ProductManager.PriceBelowAverageEvent += PriceBelowAverageEventHandler;

            ProductManager.TickerConnectedEvent += TickerConnectedEventHandler;
            ProductManager.TickerDisConnectedEvent += TickerDisConnectedEventHandler;

            ProductManager.CurrentActionChangedEvent += CurrentActionChangedEventHandler;


            ProductManager.InitializeManager(inputProduct, GetIntervals());

            sharedCurrentLargeSmaPrice = 0;


            //ProductManager.UpdateBuySellAmount(0.01m);
            //ProductManager.UpdateBuySellBuffer(0.03m);

            return null;
        }

        public IntervalValues GetIntervals()
        {
            //IntervalValues intervals;

            var checkedButton = getCurrentRdoBtnSelection();

            IntervalValues intervalVals = null;

            if (Dispatcher.Invoke(()=>checkedButton.Name == "rdoBtn_5_3_1"))
            {
                intervalVals = new IntervalValues(5, 3, 1);
            }
            else if (Dispatcher.Invoke(() => checkedButton.Name == "rdoBtn_15_5_3"))
            {
                intervalVals = new IntervalValues(15, 5, 3);
            }
            else if (Dispatcher.Invoke(() => checkedButton.Name == "rdoBtn_30_15_5"))
            {
                intervalVals = new IntervalValues(30, 15, 5);
            }
            else
            {
                intervalVals = new IntervalValues(5, 3, 1); ;
            }

            ProductManager.SetCurrentIntervalValues(intervalVals);
            return intervalVals;
        }

        public RadioButton getCurrentRdoBtnSelection()
        {
            var checkedButton = Dispatcher.Invoke(()=> stkPannel.Children.OfType<RadioButton>().FirstOrDefault(r => (bool)r.IsChecked));
            return checkedButton;
        }



        private async void btnSellAtNow_Click(object sender, RoutedEventArgs e)
        {
            //btnSellAtNow.IsEnabled = false;
            //ProductManager.ForceSellAtNow();


            var msg = "";
            
            if ((bool)chkSellMarketOrder.IsChecked)
                msg = string.Format("confirm MARKET ORDER sell of "+ lblBuySellAmount.Content + " " + SelectedProduct + "?");
            else
                msg = string.Format("confirm limit sell of " + lblBuySellAmount.Content + " " + SelectedProduct + "?");

            var response = MessageBox.Show(msg, "Confirm sell", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (response == MessageBoxResult.No)
                return;

            if (ProductManager == null)
            {
                MessageBox.Show("Please select a product first");
                return;
            }

            var isMarketOrder = Dispatcher.Invoke(() => (bool)chkSellMarketOrder.IsChecked);

            await Task.Run(() =>
            {
                Dispatcher.Invoke(()=> btnSellAtNow.IsEnabled = false);
                ProductManager.ForceSellAtNow(isMarketOrder).Wait();

                //Task.Run(() => System.Threading.Thread.Sleep(1000)).ContinueWith((t) => t.Wait());
                //Dispatcher.Invoke(()=> btnSellAtNow.IsEnabled = true);
            });


        }

        private async void btnBuyAtNow_Click(object sender, RoutedEventArgs e)
        {
            //btnSellAtNow.IsEnabled = false;
            var msg = "";

            if ((bool)chkBuyMarketOrder.IsChecked)
                msg = string.Format("confirm MARKET ORDER buy of " + lblBuySellAmount.Content + " " + SelectedProduct + "?");
            else
                msg = string.Format("confirm limit buy of " + lblBuySellAmount.Content + " " + SelectedProduct + "?");

            var response = MessageBox.Show(msg, "Confirm buy", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (response == MessageBoxResult.No)
                return;

            if (ProductManager == null)
            {
                MessageBox.Show("Please select a product first");
                return;
            }

            var isMarketOrder = Dispatcher.Invoke(() => (bool)chkBuyMarketOrder.IsChecked);

            await Task.Run(() =>
            {
                Dispatcher.Invoke(() => btnBuyAtNow.IsEnabled = false);
                ProductManager.ForceBuyAtNow(isMarketOrder).Wait();

                //Task.Run(() => System.Threading.Thread.Sleep(1000)).ContinueWith((t) => t.Wait());
                //Dispatcher.Invoke(()=> btnSellAtNow.IsEnabled = true);
            });

        }

        private async void btnStopAndCancel_Click(object sender, RoutedEventArgs e)
        {

            if (ProductManager == null)
            {
                MessageBox.Show("Please select a product first");
                return;
            }

            //if (!AutoTradingOn)
            //{
            //    MessageBox.Show("Auto trading has not been started yet");
            //    return;
            //}

            Dispatcher.Invoke(() => btnStopAndCancel.IsEnabled = false);
            await ProductManager.StopAndCancel().ContinueWith((t) => t.Wait());
            Dispatcher.Invoke(() => btnStopAndCancel.IsEnabled = true);

            Dispatcher.Invoke(() => btnBuyAtNow.IsEnabled = true);
            Dispatcher.Invoke(() => btnSellAtNow.IsEnabled = true);

        }

        private async void rdoBtn_Clicked(object sender, RoutedEventArgs e)
        {
            var response = MessageBox.Show("Sure to change intervals?", "Change Intervals?", MessageBoxButton.YesNo);

            if (response == MessageBoxResult.Yes)
            {

                toggleRdoBtn(false);
                if (ProductManager != null)
                {
                    await Task.Run(() => 
                    {
                        var a = ProductManager.CreateUpdateStrategyInstance(GetIntervals()).Result;
                        //result.Wait();
                    }).ContinueWith((t)=>t.Wait());

                    //res.Wait();
                }
                toggleRdoBtn(true);

                rdBtnCurrentSelection = getCurrentRdoBtnSelection();
            }
            else
            {
                rdBtnCurrentSelection.IsChecked = true;
            }
        }

        private void toggleRdoBtn(bool state)
        {
            Dispatcher.Invoke(() => 
            {
                rdoBtn_15_5_3.IsEnabled = state;
                rdoBtn_30_15_5.IsEnabled = state;
                rdoBtn_5_3_1.IsEnabled = state;
            });
        }

        private void chkAvoidFees_Click(object sender, RoutedEventArgs e)
        {
            if (ProductManager == null)
            {
                MessageBox.Show("Please select product first");
                return;
            }

            if ((bool)chkAvoidFees.IsChecked)
            {
                ProductManager.setAvoidExFeesVar(true);
            }
            else
            {
                ProductManager.setAvoidExFeesVar(false);
            }

            if ((bool)chkBuyMarketOrder.IsChecked)
            {
                chkBuyMarketOrder.IsChecked = false;
            }
            if ((bool)chkSellMarketOrder.IsChecked)
            {
                chkSellMarketOrder.IsChecked = false;
            }
        }

        private void chkMarketOrder_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)chkAvoidFees.IsChecked)
            {
                chkAvoidFees.IsChecked = false;
                chkAvoidFees_Click(this, null);
            }
        }

        private void txtBuySellAmount_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isNumeric(txtBuySellAmount.Text))
            {
                Button_Click_1(null, null);
            }
            else
            {
                MessageBox.Show("Incorrect buy/sell amount");
                //txtBuySellAmount.Focus();
            }
        }

        private void btnUpdateFunds_Click(object sender, RoutedEventArgs e)
        {
            Logger.WriteLog("Updating funds");
            Task.Factory.StartNew(()=> ProductManager?.UpdateFunds());
        }

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{

        //    Task.Factory.StartNew(
        //        ()=>
        //        {
        //            ProductManager.TickerDisconnectedHandler(null, null);

        //            System.Threading.Thread.Sleep(500);

        //            ProductManager.TickerConnectedHandler(null, null);

        //        });

        //}

        private void btnShowGraph_Click(object sender, RoutedEventArgs e)
        {

            //Task.Run(()=> 
            //{

            //});

            if (ProductManager == null)
                return;
            
            _graphWindow = new GraphWindow();

            _graphWindow.Height = 900;
            _graphWindow.Width = 1500;
            _graphWindow.Show();

            Task.Run(()=> 
            {
                ProductManager.ShowGraph(_graphWindow);
            });

            //Thread thread = new Thread(() =>
            //{

            //    ProductManager.ShowGraph();


            //});
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.Start();

        }

        //private void btnShowGraph1_Click(object sender, RoutedEventArgs e)
        //{
        //    if (btnShowGraph1.Content.ToString() == "Hide Graph")
        //    {
        //        cefBrowser.Load("about:blank"); // = "";
        //        btnShowGraph1.Content = "Show Graph";
        //    }
        //    else
        //    {
        //        cefBrowser.Load(@"C:\Users\bobby\Source\Repos\Multiplier\Multiplier\Resources\TradeWidgetPage.html"); // = "";
        //        btnShowGraph1.Content = "Hide Graph";
        //    }
        //}

        //private void btnShowGraph2_Click(object sender, RoutedEventArgs e)
        //{
        //    if (btnShowGraph2.Content.ToString() == "Hide Graph")
        //    {
        //        cefBrowser1.Load("about:blank"); // = "";
        //        btnShowGraph2.Content = "Show Graph";
        //    }
        //    else
        //    {
        //        cefBrowser1.Load(@"C:\Users\bobby\Source\Repos\Multiplier\Multiplier\Resources\TradeWidgetPage.html"); // = "";
        //        btnShowGraph2.Content = "Hide Graph";
        //    }
        //}






        //private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        //{
        //    Dispatcher.Invoke(() => ProductManager.TickerDisconnectedHandler(this, EventArgs.Empty));
        //}

        //private void btnConnect_Click(object sender, RoutedEventArgs e)
        //{
        //    Dispatcher.Invoke(() => ProductManager.TickerConnectedHandler(this, EventArgs.Empty));
        //}
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
