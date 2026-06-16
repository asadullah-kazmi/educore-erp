using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SchoolERP.Data;
using SchoolERP.Models;
using SchoolERP.Repositories;

namespace SchoolERP.Views
{
    public partial class AddEditFeeWindow : Window
    {
        private readonly StudentRepository studentRepository = new StudentRepository();
        private readonly FeeRepository feeRepository = new FeeRepository();
        private readonly FeeRecord editingFee;

        public AddEditFeeWindow(FeeRecord fee = null)
        {
            InitializeComponent();
            editingFee = fee;

            PopulateMonths();
            _ = LoadStudentsAndPrepopulateAsync();
        }

        private void PopulateMonths()
        {
            var months = new List<string>();
            DateTime start = new DateTime(2025, 1, 1);
            DateTime end = new DateTime(2026, 12, 1);
            for (DateTime m = start; m <= end; m = m.AddMonths(1))
            {
                months.Add(m.ToString("MMM yyyy"));
            }
            ComboMonth.ItemsSource = months;
        }

        private async Task LoadStudentsAndPrepopulateAsync()
        {
            try
            {
                var students = await studentRepository.GetAllStudentsAsync().ConfigureAwait(true);
                ComboStudent.ItemsSource = students;

                if (editingFee != null)
                {
                    TxtTitle.Text = "Edit Fee Record";
                    ComboStudent.SelectedItem = students.FirstOrDefault(s => s.StudentID == editingFee.StudentID);
                    ComboMonth.SelectedItem = editingFee.Month;

                    foreach (ComboBoxItem item in ComboFeeType.Items)
                    {
                        if (string.Equals(item.Content.ToString(), editingFee.FeeType, StringComparison.OrdinalIgnoreCase))
                        {
                            ComboFeeType.SelectedItem = item;
                            break;
                        }
                    }

                    TxtAmount.Text = editingFee.Amount.ToString("F2");

                    foreach (ComboBoxItem item in ComboStatus.Items)
                    {
                        if (string.Equals(item.Content.ToString(), editingFee.Status, StringComparison.OrdinalIgnoreCase))
                        {
                            ComboStatus.SelectedItem = item;
                            break;
                        }
                    }

                    DpPaymentDate.SelectedDate = editingFee.PaymentDate;
                }
                else
                {
                    TxtTitle.Text = "Add Fee Record";
                    ComboMonth.SelectedItem = DateTime.Now.ToString("MMM yyyy");

                    foreach (ComboBoxItem item in ComboStatus.Items)
                    {
                        if (string.Equals(item.Content.ToString(), "Due", StringComparison.OrdinalIgnoreCase))
                        {
                            ComboStatus.SelectedItem = item;
                            break;
                        }
                    }
                }

                UpdatePaymentDateEnableState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ComboStudent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await AutoFillAmountAsync();
        }

        private async void ComboFeeType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await AutoFillAmountAsync();
        }

        private void ComboStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePaymentDateEnableState();
        }

        private void UpdatePaymentDateEnableState()
        {
            if (ComboStatus == null || DpPaymentDate == null) return;

            var selectedItem = ComboStatus.SelectedItem as ComboBoxItem;
            bool isPaid = selectedItem != null && string.Equals(selectedItem.Content.ToString(), "Paid", StringComparison.OrdinalIgnoreCase);
            DpPaymentDate.IsEnabled = isPaid;
            if (!isPaid)
            {
                DpPaymentDate.SelectedDate = null;
            }
            else if (DpPaymentDate.SelectedDate == null)
            {
                DpPaymentDate.SelectedDate = DateTime.Today;
            }
        }

        private async Task AutoFillAmountAsync()
        {
            if (ComboStudent == null || ComboFeeType == null || TxtAmount == null) return;

            var student = ComboStudent.SelectedItem as Student;
            var feeTypeItem = ComboFeeType.SelectedItem as ComboBoxItem;

            if (student != null && feeTypeItem != null && string.Equals(feeTypeItem.Content.ToString(), "Monthly Tuition", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    decimal monthlyFee = await GetStudentMonthlyFeeAsync(student.StudentID).ConfigureAwait(true);
                    TxtAmount.Text = monthlyFee.ToString("F0");
                }
                catch (Exception)
                {
                    // Fail silently or default to empty/0
                }
            }
        }

        private async Task<decimal> GetStudentMonthlyFeeAsync(int studentId)
        {
            const string sql = @"
                SELECT MonthlyFee 
                FROM Classes 
                WHERE ClassID = (SELECT ClassID FROM Students WHERE StudentID = @id)";

            using (var connection = Database.GetConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", studentId);
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
            }
            return 0;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var student = ComboStudent.SelectedItem as Student;
            if (student == null)
            {
                MessageBox.Show("Please select a student.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var month = ComboMonth.SelectedItem as string;
            if (string.IsNullOrEmpty(month))
            {
                MessageBox.Show("Please select a month.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var feeTypeItem = ComboFeeType.SelectedItem as ComboBoxItem;
            if (feeTypeItem == null)
            {
                MessageBox.Show("Please select a fee type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var feeType = feeTypeItem.Content.ToString();

            if (!decimal.TryParse(TxtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var statusItem = ComboStatus.SelectedItem as ComboBoxItem;
            if (statusItem == null)
            {
                MessageBox.Show("Please select a status.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var status = statusItem.Content.ToString();

            DateTime? paymentDate = DpPaymentDate.SelectedDate;
            if (string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase) && paymentDate == null)
            {
                MessageBox.Show("Please select a payment date for paid fees.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool success;
                if (editingFee == null)
                {
                    var newFee = new FeeRecord
                    {
                        StudentID = student.StudentID,
                        Month = month,
                        FeeType = feeType,
                        Amount = amount,
                        Status = status,
                        PaymentDate = paymentDate
                    };
                    success = await feeRepository.AddFeeAsync(newFee).ConfigureAwait(true);
                }
                else
                {
                    editingFee.StudentID = student.StudentID;
                    editingFee.Month = month;
                    editingFee.FeeType = feeType;
                    editingFee.Amount = amount;
                    editingFee.Status = status;
                    editingFee.PaymentDate = paymentDate;
                    success = await feeRepository.UpdateFeeAsync(editingFee).ConfigureAwait(true);
                }

                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to save the fee record.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while saving: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
