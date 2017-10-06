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
        public static object writeLock = new object();

        public LogWindow()
        {
            InitializeComponent();

            Logger.Logupdated += LogUpdatedHandler;
            txtLog.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            txtLog.Document.PageWidth = 1000;
        }

        private void LogUpdatedHandler(object sender, EventArgs args)
        {
            var msg = (LoggerEventArgs)args;
            try
            {

                this.Dispatcher.Invoke(() => txtLog.AppendText(msg.LogMessage)) ;

            }
            catch (Exception)
            {
                Console.WriteLine("error appending to text box");
                //throw;
            }
            
        }


        private void txtLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            // scroll it automatically
            txtLog.ScrollToEnd();
        }
    }
}
