using System.Windows;
using System.Windows.Controls;
using SchoolERP.Models;

namespace SchoolERP.Views
{
    public partial class FamilyFeeReceiptWindow : Window
    {
        public FamilyFeeReceiptWindow(FamilyFeeReceipt receipt)
        {
            InitializeComponent();
            DataContext = receipt;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true) dialog.PrintVisual(ReceiptContent, "Family Fee Receipt");
        }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
