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
        private List<SchoolERP.Models.Class> allClasses;
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
                allClasses = await studentRepository.GetAllClassesAsync().ConfigureAwait(true);
                filteredStudents = new ObservableCollection<Student>(allStudents);
                ComboStudent.ItemsSource = filteredStudents;
                ComboClass.ItemsSource = allClasses;

                if (editingFee != null)
                {
                    TxtTitle.Text = "Edit Fee Record";
                    ScopePanel.Visibility = Visibility.Collapsed;
                    ClassPanel.Visibility = Visibility.Collapsed;
                    SectionPanel.Visibility = Visibility.Collapsed;
                    StudentPanel.Visibility = Visibility.Visible;
                    ComboStudent.SelectedItem = allStudents.FirstOrDefault(s => s.StudentID == editingFee.StudentID);
                    ComboMonth.SelectedItem = editingFee.Month;
                    TxtFeeType.Text = editingFee.FeeType;

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
                    TxtTitle.Text = "Generate Fee";
                    ComboScope.SelectedIndex = 0;
                    ComboSection.SelectedIndex = 0;
                    ComboMonth.SelectedItem = DateTime.Now.ToString("MMM yyyy");

                    foreach (ComboBoxItem item in ComboStatus.Items)
                    {
                        if (string.Equals(item.Content.ToString(), "Due", StringComparison.OrdinalIgnoreCase))
                        {
                            ComboStatus.SelectedItem = item;
                            break;
                        }
                    }

                    TxtFeeType.Text = "Exam Fee";
                    DpPaymentDate.SelectedDate = null;
                    UpdateScopeVisibility();
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

        private void TxtFeeType_TextChanged(object sender, TextChangedEventArgs e)
        {
            AutoFillAmountAsync();
        }

        private void ComboScope_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateScopeVisibility();
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
            if (ComboStudent == null || TxtFeeType == null || TxtAmount == null) return;

            var student = ComboStudent.SelectedItem as Student;
            var feeType = TxtFeeType.Text;

            if (student != null && string.Equals(feeType?.Trim(), "Monthly Tuition", StringComparison.OrdinalIgnoreCase))
            {
                TxtAmount.Text = student.MonthlyFee.ToString("F0");
            }
        }

        private void UpdateScopeVisibility()
        {
            if (ScopePanel == null || ClassPanel == null || SectionPanel == null || StudentPanel == null) return;

            var scope = GetSelectedScope();
            ClassPanel.Visibility = string.Equals(scope, "Specific Class", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
            SectionPanel.Visibility = string.Equals(scope, "Specific Class", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
            StudentPanel.Visibility = string.Equals(scope, "Individual Student", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private string GetSelectedScope()
        {
            var scopeItem = ComboScope?.SelectedItem as ComboBoxItem;
            return scopeItem?.Content?.ToString() ?? "All Students";
        }

        private Student GetSelectedStudent()
        {
            if (ComboStudent.SelectedItem is Student selected)
            {
                return selected;
            }

            var text = ComboStudent.Text?.Trim();
            if (string.IsNullOrEmpty(text) || allStudents == null)
            {
                return null;
            }

            var exactMatch = allStudents.FirstOrDefault(s =>
                string.Equals(s.Name, text, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.RegistrationNo, text, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                return exactMatch;
            }

            var matches = allStudents.Where(s =>
                (!string.IsNullOrEmpty(s.Name) && s.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(s.RegistrationNo) && s.RegistrationNo.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            return matches.Count == 1 ? matches[0] : null;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudents = GetTargetStudents();
            if (selectedStudents.Count == 0)
            {
                MessageBox.Show("Please select at least one student.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var month = ComboMonth.SelectedItem as string;
            if (string.IsNullOrEmpty(month))
            {
                MessageBox.Show("Please select a month.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var feeType = TxtFeeType.Text?.Trim();
            if (string.IsNullOrEmpty(feeType))
            {
                MessageBox.Show("Please enter a fee name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                    var inserted = await feeRepository.AddFeesForStudentsAsync(
                        selectedStudents.Select(s => s.StudentID),
                        month,
                        feeType,
                        amount,
                        status,
                        paymentDate).ConfigureAwait(true);

                    if (inserted == 0)
                    {
                        MessageBox.Show("Fee records already exist for the selected students, month, and fee name.", "Generate Fee", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    MessageBox.Show($"Generated {inserted} fee record(s).", "Generate Fee", MessageBoxButton.OK, MessageBoxImage.Information);
                    success = true;
                }
                else
                {
                    var student = selectedStudents[0];
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

        private List<Student> GetTargetStudents()
        {
            if (editingFee != null)
            {
                var student = GetSelectedStudent();
                return student == null ? new List<Student>() : new List<Student> { student };
            }

            var scope = GetSelectedScope();
            if (string.Equals(scope, "All Students", StringComparison.OrdinalIgnoreCase))
            {
                return allStudents?.ToList() ?? new List<Student>();
            }

            if (string.Equals(scope, "Specific Class", StringComparison.OrdinalIgnoreCase))
            {
                if (!(ComboClass.SelectedItem is SchoolERP.Models.Class selectedClass))
                {
                    MessageBox.Show("Please select a class.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return new List<Student>();
                }

                var students = allStudents
                    .Where(s => s.ClassID == selectedClass.ClassID)
                    .ToList();

                var section = GetSelectedSection();
                if (section != null)
                {
                    students = students
                        .Where(s => string.Equals((s.Section ?? string.Empty).Trim(), section, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                return students;
            }

            var selectedStudent = GetSelectedStudent();
            return selectedStudent == null ? new List<Student>() : new List<Student> { selectedStudent };
        }

        private string GetSelectedSection()
        {
            var item = ComboSection.SelectedItem as ComboBoxItem;
            var section = item?.Content?.ToString()?.Trim();
            return string.IsNullOrEmpty(section) ||
                   string.Equals(section, "All Sections", StringComparison.OrdinalIgnoreCase)
                ? null
                : section;
        }

        private async Task<bool?> FindAndMarkPaidAsync(int studentId, string month, string feeType, decimal amount, DateTime? paymentDate)
        {
            var dueFees = await feeRepository.GetAllFeesAsync(studentId: studentId, month: month?.Trim(), status: "Due").ConfigureAwait(true);
            var fee = dueFees.FirstOrDefault(f => string.Equals(f.FeeType?.Trim(), feeType?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (fee == null)
            {
                MessageBox.Show("No due record found for this student, month, and fee type.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            fee.Amount = amount;
            fee.Status = "Paid";
            fee.PaymentDate = paymentDate;
            return await feeRepository.UpdateFeeAsync(fee).ConfigureAwait(true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
