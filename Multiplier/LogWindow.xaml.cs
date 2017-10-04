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
using System.Windows.Shapes;
using CoinbaseExchange.NET.Utilities;
namespace Multiplier
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();

            Logger.Logupdated += LoggerUpdateEventHandler;
        }


        public void LoggerUpdateEventHandler(object sender, LoggerEventArgs args)
        {
            this.Dispatcher.Invoke(() =>
            {
                txtLog.AppendText(args.LogMessage); 
            });
            //Logger.WriteLog("Hello");
        }


        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    base.OnClosing(e);
        //    e.Cancel = true;
        //}
    }
}
