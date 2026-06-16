using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private List<Student> allStudents;
        private ObservableCollection<Student> filteredStudents;

        public AddEditFeeWindow(FeeRecord fee = null)
        {
            InitializeComponent();
            editingFee = fee;

            PopulateMonths();
            _ = LoadStudentsAndPrepopulateAsync();

            // Subscribe to the internal TextBox's TextChanged event
            ComboStudent.Loaded += (s, e) =>
            {
                var textBox = (TextBox)ComboStudent.Template.FindName("PART_EditableTextBox", ComboStudent);
                if (textBox != null)
                {
                    textBox.TextChanged += (ss, ee) => FilterStudents();
                }
            };
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
                allStudents = (await studentRepository.GetAllStudentsAsync().ConfigureAwait(true)).ToList();
                filteredStudents = new ObservableCollection<Student>(allStudents);
                ComboStudent.ItemsSource = filteredStudents;

                if (editingFee != null)
                {
                    TxtTitle.Text = "Edit Fee Record";
                    ComboStudent.SelectedItem = allStudents.FirstOrDefault(s => s.StudentID == editingFee.StudentID);
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

        private void FilterStudents()
        {
            if (allStudents == null) return;

            string searchText = ComboStudent.Text?.ToLower() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? allStudents
                : allStudents.Where(s =>
                    (!string.IsNullOrEmpty(s.Name) && s.Name.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(s.RegistrationNo) && s.RegistrationNo.ToLower().Contains(searchText))
                ).ToList();

            filteredStudents.Clear();
            foreach (var student in filtered)
            {
                filteredStudents.Add(student);
            }

            // Open dropdown if there's search text
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                ComboStudent.IsDropDownOpen = true;
            }
        }

        private void ComboStudent_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Filtering is handled by the internal TextBox's TextChanged event
        }

        private void ComboStudent_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            // Filtering is handled by the internal TextBox's TextChanged event
        }

        private void ComboStudent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AutoFillAmountAsync();
        }

        private void ComboFeeType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AutoFillAmountAsync();
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

        private void AutoFillAmountAsync()
        {
            if (ComboStudent == null || ComboFeeType == null || TxtAmount == null) return;

            var student = ComboStudent.SelectedItem as Student;
            var feeTypeItem = ComboFeeType.SelectedItem as ComboBoxItem;

            if (student != null && feeTypeItem != null && string.Equals(feeTypeItem.Content.ToString(), "Monthly Tuition", StringComparison.OrdinalIgnoreCase))
            {
                TxtAmount.Text = student.MonthlyFee.ToString("F0");
            }
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
