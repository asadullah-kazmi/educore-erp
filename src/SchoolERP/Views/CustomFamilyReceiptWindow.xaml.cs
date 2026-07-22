using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SchoolERP.Data;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class CustomFamilyReceiptWindow : Window
    {
        private readonly FamilyReceiptRepository repository = new FamilyReceiptRepository();
        public ObservableCollection<FamilyPaymentRowViewModel> Siblings { get; } = new ObservableCollection<FamilyPaymentRowViewModel>();

        public CustomFamilyReceiptWindow()
        {
            InitializeComponent();
            DataContext = this;
            PaymentDatePicker.SelectedDate = DateTime.Today;
        }

        private async void FindSiblings_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GuardianCnicTextBox.Text))
            {
                StatusText.Text = "Enter the guardian CNIC.";
                return;
            }

            try
            {
                StatusText.Text = "Loading siblings...";
                var rows = await repository.FindSiblingsAsync(GuardianCnicTextBox.Text).ConfigureAwait(true);
                Siblings.Clear();
                foreach (var row in rows)
                {
                    Siblings.Add(row);
                }
                StatusText.Text = rows.Count == 0 ? "No students were found with this guardian CNIC." : string.Empty;

                // Clear and re-distribute if there's already an amount
                DistributePayment();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Failed to find siblings: " + ex.Message;
            }
        }

        private void PaymentAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DistributePayment();
            UpdateSummary();
        }

        /// <summary>
        /// Auto-distribute the entered total amount across siblings,
        /// starting from the youngest class (lowest ClassID) first.
        /// Siblings are already sorted by ClassID ASC from the repository.
        /// </summary>
        private void DistributePayment()
        {
            decimal totalAmount = 0;
            if (!string.IsNullOrWhiteSpace(PaymentAmountTextBox.Text))
            {
                decimal.TryParse(PaymentAmountTextBox.Text.Trim(), out totalAmount);
            }

            if (totalAmount < 0) totalAmount = 0;

            var remaining = totalAmount;
            int paidCount = 0;
            int partialCount = 0;

            foreach (var sibling in Siblings)
            {
                if (remaining <= 0 || sibling.OutstandingBalance <= 0)
                {
                    sibling.AllocatedAmount = 0;
                }
                else if (remaining >= sibling.OutstandingBalance)
                {
                    sibling.AllocatedAmount = sibling.OutstandingBalance;
                    remaining -= sibling.OutstandingBalance;
                    paidCount++;
                }
                else
                {
                    sibling.AllocatedAmount = remaining;
                    remaining = 0;
                    partialCount++;
                }
            }

            // Update the hint text
            if (Siblings.Count == 0)
            {
                DistributionHintText.Text = "";
            }
            else if (totalAmount <= 0)
            {
                DistributionHintText.Text = "Enter an amount to see how it will be distributed across siblings.";
            }
            else
            {
                var totalOutstanding = Siblings.Sum(s => s.OutstandingBalance);
                if (totalAmount > totalOutstanding)
                {
                    DistributionHintText.Text = "⚠ Amount exceeds total outstanding (Rs " + totalOutstanding.ToString("N0") + "). Maximum payable is Rs " + totalOutstanding.ToString("N0") + ".";
                }
                else
                {
                    DistributionHintText.Text = paidCount + " fully paid, " + partialCount + " partial, " +
                        (Siblings.Count(s => s.OutstandingBalance > 0) - paidCount - partialCount) + " unpaid.";
                }
            }
        }

        private void UpdateSummary()
        {
            SiblingCountText.Text = Siblings.Count.ToString();
            TotalOutstandingText.Text = "Rs " + Siblings.Sum(x => x.OutstandingBalance).ToString("N0");
            TotalAllocatedText.Text = "Rs " + Siblings.Sum(x => x.AllocatedAmount).ToString("N0");
            TotalPendingText.Text = "Rs " + Siblings.Sum(x => x.PendingAmount).ToString("N0");
        }

        private async void GenerateReceipt_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (Siblings.Count == 0)
            {
                StatusText.Text = "Find siblings first by entering a guardian CNIC.";
                return;
            }

            if (!decimal.TryParse(PaymentAmountTextBox.Text, out var paymentAmount) || paymentAmount <= 0)
            {
                StatusText.Text = "Enter a valid payment amount greater than zero.";
                return;
            }

            var totalOutstanding = Siblings.Sum(s => s.OutstandingBalance);
            if (paymentAmount > totalOutstanding)
            {
                StatusText.Text = "Payment amount (Rs " + paymentAmount.ToString("N0") + ") exceeds total outstanding (Rs " + totalOutstanding.ToString("N0") + ").";
                return;
            }

            if (PaymentDatePicker.SelectedDate == null)
            {
                StatusText.Text = "Select a payment date.";
                return;
            }

            var siblingsWithPayment = Siblings.Where(x => x.AllocatedAmount > 0).ToList();
            if (siblingsWithPayment.Count == 0)
            {
                StatusText.Text = "No siblings have outstanding fees to pay.";
                return;
            }

            try
            {
                StatusText.Text = "Processing payment...";
                var receipt = await repository.CreateAsync(
                    GuardianCnicTextBox.Text,
                    PaymentDatePicker.SelectedDate.Value,
                    Siblings.ToList()
                ).ConfigureAwait(true);

                new FamilyFeeReceiptWindow(receipt) { Owner = this }.ShowDialog();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Failed to generate receipt: " + ex.Message;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
