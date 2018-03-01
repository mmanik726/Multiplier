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

namespace Multiplier
{
    /// <summary>
    /// Interaction logic for StartWindow.xaml
    /// </summary>
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();

            cmbBuySell.Items.Add("BUYING");
            cmbBuySell.Items.Add("SELLING");


        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void rdoIfPriceUP_Click(object sender, RoutedEventArgs e)
        {
            if (rdoIfPriceUP.IsChecked.Value)
            {
                txtUpPrice.IsEnabled = true;
                txtUpPrice.Text = "0";
            }
            else
            {
                txtUpPrice.IsEnabled = false;
                txtUpPrice.Text = "0";
            }
        }

        private void rdoIfPriceDOWN_Click(object sender, RoutedEventArgs e)
        {
            if (rdoIfPriceDOWN.IsChecked.Value)
            {
                txtDownPrice.IsEnabled = true;
                txtDownPrice.Text = "0";
            }
            else
            {
                txtDownPrice.IsEnabled = false;
                txtDownPrice.Text = "0";
            }
        }

        private void chkCondStart_Checked(object sender, RoutedEventArgs e)
        {
            if (chkCondStart.IsChecked.Value)
            {
                grpConditions.IsEnabled = true;
            }
            else
            {
                grpConditions.IsEnabled = false;
            }
        }


        public RadioButton getCurrentRdoBtnSelection()
        {
            var checkedButton = Dispatcher.Invoke(() => grdChoices.Children.OfType<RadioButton>().FirstOrDefault(r => (bool)r.IsChecked));
            //var checkedButton = grdChoices.Children.OfType<RadioButton>().FirstOrDefault(r => (bool)r.IsChecked);
            return checkedButton;
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

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {

            string selectedAction = "";




            if (chkCondStart.IsChecked.Value)
            {
                if (cmbBuySell.SelectedValue == null)
                {
                    MessageBox.Show("Select buy or sell");
                    return;
                }

                if (cmbBuySell.SelectedValue.ToString() == "BUYING")
                {
                    
                    if (getCurrentRdoBtnSelection().Name.ToLower() == "rdoIfPriceUP".ToLower())
                    {
                        if (isNumeric(txtUpPrice.Text))
                        {
                            selectedAction = "BUY@UP@" + txtUpPrice.Text;
                        }
                        else
                        {
                            MessageBox.Show("Invalid price value");
                            txtUpPrice.Focus();
                        }
                    }
                    else
                    {
                        if (isNumeric(txtDownPrice.Text))
                        {
                            selectedAction = "BUY@DOWN@" + txtDownPrice.Text;
                        }
                        else
                        {
                            MessageBox.Show("Invalid price value");
                            txtDownPrice.Focus();
                        }
                    }
                }
                else if (cmbBuySell.SelectedValue.ToString() == "SELLING")
                {
                    if (getCurrentRdoBtnSelection().Name.ToLower() == "rdoIfPriceUP".ToLower())
                    {
                        if (isNumeric(txtUpPrice.Text))
                        {
                            selectedAction = "SELL@UP@" + txtUpPrice.Text;
                        }
                        else
                        {
                            MessageBox.Show("Invalid price value");
                            txtUpPrice.Focus();
                        }
                    }
                    else
                    {
                        if (isNumeric(txtDownPrice.Text))
                        {
                            selectedAction = "SELL@DOWN@" + txtDownPrice.Text;
                        }
                        else
                        {
                            MessageBox.Show("Invalid price value");
                            txtDownPrice.Focus();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Select buy or sell");
                }
            }
            else
            {


                if (cmbBuySell.SelectedValue == null)
                {
                    MessageBox.Show("Select buy or sell");
                    return;
                }

                if (cmbBuySell.SelectedValue.ToString() == "BUYING")
                {
                    selectedAction = "BUY";
                }
                else if (cmbBuySell.SelectedValue.ToString() == "SELLING")
                {
                    selectedAction = "SELL";
                }
                else
                {
                    MessageBox.Show("Select buy or sell");
                }


            }

            if (selectedAction != "")
            {
                MessageBox.Show(selectedAction);
            }
            
        }
    }
}
