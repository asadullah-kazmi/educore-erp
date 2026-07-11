using System;
using System.Windows;
using System.Windows.Controls;
using SchoolERP.Models;

namespace SchoolERP.Views
{
    public class FeeReceiptViewModel
    {
        public FeeReceiptViewModel(FeeReceipt receipt)
        {
            ReceiptNumber = receipt.ReceiptNumber;
            StudentName = receipt.StudentName;
            RegistrationNo = receipt.RegistrationNo;
            ClassName = receipt.ClassName;
            Section = receipt.Section;
            Details = receipt.Details;
            PaidAmount = receipt.AmountPaid;
            Balance = receipt.BalanceAfter;
            ReceiptDate = receipt.PaymentDate;
        }

        public string ReceiptNumber { get; }
        public string StudentName { get; }
        public string RegistrationNo { get; }
        public string ClassName { get; }
        public string Section { get; }
        public string Details { get; }
        public decimal PaidAmount { get; }
        public decimal Balance { get; }
        public DateTime ReceiptDate { get; }
    }

    public partial class FeeReceiptWindow : Window
    {
        public FeeReceiptWindow(FeeReceipt receipt)
        {
            DataContext = new FeeReceiptViewModel(receipt);
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
