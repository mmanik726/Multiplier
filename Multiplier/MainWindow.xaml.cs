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

namespace Multiplier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        


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

            StartWindow a = new StartWindow();
            a.Show();
            
            //var d = x.Where((f)=>f.ToString() == "Value1");
            //var s = x["Value1"].ToString();


            //AppSettings.CreateNew();
            //var filePath = System.IO.Path.GetFullPath(Assembly.GetExecutingAssembly().Location + @"\..\..\Resources\TradeWidgetPage.html");
            //cefBrowser.Address = @"https://www.tradingview.com"; //@"C:\Users\bobby\Source\Repos\Multiplier\Multiplier\Resources\TradeWidgetPage.html";//@".\TradeWidgetPage.html"; //Properties.Resources.TradeWidgetPage. //"https://github.com/cefsharp/CefSharp/wiki/Frequently-asked-questions";

            //cefBrowser.Address = @"C:\Users\bobby\Source\Repos\Multiplier\Multiplier\Resources\TradeWidgetPage.html";//@".\TradeWidgetPage.html"; //Properties.Resources.TradeWidgetPage. //"https://github.com/cefsharp/CefSharp/wiki/Frequently-asked-questions";
            //cefBrowser1.Address = @"C:\Users\bobby\Source\Repos\Multiplier\Multiplier\Resources\TradeWidgetPage.html";//@".\TradeWidgetPage.html"; //Properties.Resources.TradeWidgetPage. //"https://github.com/cefsharp/CefSharp/wiki/Frequently-asked-questions";

            //test

            //            List<double> dbl = new List<double>() {
            //22.27340d,
            //22.19400d,
            //22.08470d,
            //22.17410d,
            //22.18400d,
            //22.13440d,
            //22.23370d,
            //22.43230d,
            //22.24360d,
            //22.29330d,
            //22.15420d,
            //22.39260d,
            //22.38160d,
            //22.61090d,
            //23.35580d,
            //24.05190d,
            //23.75300d,
            //23.83240d,
            //23.95160d,
            //23.63380d,
            //23.82250d,
            //23.87220d,
            //23.65370d,
            //23.18700d,
            //23.09760d,
            //23.32600d,
            //22.68050d,
            //23.09760d,
            //22.40250d,
            //22.17250d
            //};
            //            var a = dbl.EMA(10).ToList();

            //a.ForEach((d) => Logger.WriteLog(d.ToString()));



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

            ProductManager.StartTrading_BySelling();

            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;
        }


        private void btnStartByBuying_Click(object sender, RoutedEventArgs e)
        {

            ProductManager.StartTrading_ByBuying();
            btnStartByBuying.IsEnabled = false;
            btnStartBySelling.IsEnabled = false;

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
                msg = string.Format("confirm MARKET ORDER sell?");
            else
                msg = string.Format("confirm limit sell?");

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
                msg = string.Format("confirm MARKET ORDER buy?");
            else
                msg = string.Format("confirm limit buy?");

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
