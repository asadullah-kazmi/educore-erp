using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SchoolERP.Controls;
using SchoolERP.Data;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class SalaryHistoryViewModel : ViewModelBase
    {
        private readonly SalaryRepository repository = new SalaryRepository();
        private readonly int teacherId;
        private decimal totalPaid;

        public SalaryHistoryViewModel(int teacherId, string teacherName, bool isAdmin)
        {
            this.teacherId = teacherId;
            TeacherName = teacherName;
            IsAdmin = isAdmin;
            Payments = new ObservableCollection<SalaryPayment>();

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            DeletePaymentCommand = new RelayCommand<SalaryPayment>(
                async payment => await DeletePaymentAsync(payment),
                _ => IsAdmin);

            _ = LoadAsync();
        }

        public ObservableCollection<SalaryPayment> Payments { get; }

        public string TeacherName { get; }

        public decimal TotalPaid
        {
            get => totalPaid;
            private set => SetProperty(ref totalPaid, value);
        }

        public string TotalPaidDisplay => TotalPaid.ToString("N0");

        public bool IsAdmin { get; }

        public ICommand LoadCommand { get; }

        public RelayCommand<SalaryPayment> DeletePaymentCommand { get; }

        private async Task LoadAsync()
        {
            try
            {
                var payments = await repository.GetPaymentHistoryAsync(teacherId).ConfigureAwait(true);
                Payments.Clear();
                foreach (var payment in payments)
                {
                    Payments.Add(payment);
                }

                TotalPaid = Payments.Sum(p => p.Amount);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load payment history: " + ex.Message,
                    "Salary History",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task DeletePaymentAsync(SalaryPayment payment)
        {
            if (payment == null || !IsAdmin)
            {
                return;
            }

            if (!ConfirmationDialog.Show(
                "Are you sure you want to delete this salary payment record?",
                "Confirm Delete"))
            {
                return;
            }

            try
            {
                bool success = await repository.DeletePaymentAsync(payment.SalaryPaymentID).ConfigureAwait(true);
                if (success)
                {
                    await LoadAsync().ConfigureAwait(true);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to delete payment record.",
                        "Salary History",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to delete payment record: " + ex.Message,
                    "Salary History",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
