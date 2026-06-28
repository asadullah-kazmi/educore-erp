using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SchoolERP.Models;

namespace SchoolERP.Views
{
    public partial class RecordFeePaymentWindow : Window
    {
        private readonly List<FeeRecord> fees;
        private readonly decimal totalPayable;

        public RecordFeePaymentWindow(IList<FeeRecord> feesToPay)
        {
            InitializeComponent();

            fees = (feesToPay ?? new List<FeeRecord>()).ToList();
            totalPayable = fees.Sum(f => f.Balance);

            FeeGrid.ItemsSource = fees;
            TxtTotalPayable.Text = $"Rs {totalPayable:N0}";
            DpPaymentDate.SelectedDate = DateTime.Today;
            TxtPaymentAmount.Text = totalPayable.ToString("F0");
            TxtPaymentAmount.IsEnabled = false;

            var firstFee = fees.FirstOrDefault();
            TxtStudent.Text = firstFee == null
                ? string.Empty
                : $"{firstFee.StudentName} ({firstFee.RegistrationNo}) - {firstFee.ClassName} {firstFee.Section}";
        }

        public decimal PaymentAmount { get; private set; }
        public DateTime PaymentDate { get; private set; }

        private void PaymentType_Checked(object sender, RoutedEventArgs e)
        {
            if (TxtPaymentAmount == null || RadioPartial == null)
            {
                return;
            }

            var isPartial = RadioPartial.IsChecked == true;
            TxtPaymentAmount.IsEnabled = isPartial;
            if (!isPartial)
            {
                TxtPaymentAmount.Text = totalPayable.ToString("F0");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtPaymentAmount.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid payment amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (amount > totalPayable)
            {
                MessageBox.Show("Payment amount cannot be greater than the total payable amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RadioPartial.IsChecked == true && amount >= totalPayable)
            {
                MessageBox.Show("For full balance payment, select Full fee.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DpPaymentDate.SelectedDate == null)
            {
                MessageBox.Show("Please select a payment date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PaymentAmount = amount;
            PaymentDate = DpPaymentDate.SelectedDate.Value;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
