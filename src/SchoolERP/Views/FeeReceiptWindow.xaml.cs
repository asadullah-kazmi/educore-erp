using System;
using System.Windows;
using System.Windows.Controls;
using SchoolERP.Models;

namespace SchoolERP.Views
{
    public class FeeReceiptViewModel
    {
        public FeeReceiptViewModel(FeeRecord fee)
        {
            FeeID = fee.FeeID;
            StudentName = fee.StudentName;
            RegistrationNo = fee.RegistrationNo;
            ClassName = fee.ClassName;
            Month = fee.Month;
            FeeType = fee.FeeType;
            Amount = fee.Amount;
            Status = fee.Status;
            ReceiptDate = DateTime.Today;
        }

        public int FeeID { get; }
        public string StudentName { get; }
        public string RegistrationNo { get; }
        public string ClassName { get; }
        public string Month { get; }
        public string FeeType { get; }
        public decimal Amount { get; }
        public string Status { get; }
        public DateTime ReceiptDate { get; }
    }

    public partial class FeeReceiptWindow : Window
    {
        public FeeReceiptWindow(FeeRecord fee)
        {
            DataContext = new FeeReceiptViewModel(fee);
            InitializeComponent();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            printDialog.PrintVisual(ReceiptContent, "Fee Receipt");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
