using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SchoolERP.Data;

namespace SchoolERP.ViewModels
{
    public class PaySalaryViewModel : ViewModelBase
    {
        private readonly SalaryRepository repository = new SalaryRepository();
        private readonly int teacherId;
        private string amountText;
        private DateTime paymentDate;
        private string notes;
        private string amountError;
        private bool isSaving;

        public PaySalaryViewModel(StaffSalaryRowViewModel staff, string month)
        {
            if (staff == null)
            {
                throw new ArgumentNullException(nameof(staff));
            }

            teacherId = staff.TeacherID;
            TeacherName = staff.Name;
            Designation = staff.Designation;
            BaseSalaryDisplay = staff.BaseSalaryDisplay;
            MonthDisplay = month;
            WindowTitle = "Pay Salary — " + month;
            AmountText = staff.BaseSalary.ToString("0.##");
            PaymentDate = DateTime.Today;

            SaveCommand = new RelayCommand(_ => SaveAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

        public event Action<bool> RequestClose;

        public string WindowTitle { get; }

        public string TeacherName { get; }

        public string Designation { get; }

        public string BaseSalaryDisplay { get; }

        public string MonthDisplay { get; }

        public string AmountText
        {
            get => amountText;
            set => SetProperty(ref amountText, value);
        }

        public DateTime PaymentDate
        {
            get => paymentDate;
            set => SetProperty(ref paymentDate, value);
        }

        public string Notes
        {
            get => notes;
            set => SetProperty(ref notes, value);
        }

        public string AmountError
        {
            get => amountError;
            set => SetProperty(ref amountError, value);
        }

        public bool IsSaving
        {
            get => isSaving;
            private set => SetProperty(ref isSaving, value);
        }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        private async void SaveAsync()
        {
            AmountError = null;

            decimal amount;
            if (!decimal.TryParse(AmountText, out amount) || amount <= 0)
            {
                AmountError = "Amount must be a number greater than zero.";
                return;
            }

            IsSaving = true;

            try
            {
                bool success = await repository.PaySalaryAsync(
                    teacherId,
                    amount,
                    PaymentDate,
                    Notes).ConfigureAwait(true);

                if (success)
                {
                    RequestClose?.Invoke(true);
                }
                else
                {
                    AmountError = "Unable to record salary payment. Please try again.";
                }
            }
            catch (Exception ex)
            {
                AmountError = "Save failed: " + ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}
