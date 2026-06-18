using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class AddEditExpenseViewModel : ViewModelBase
    {
        private readonly ExpenseRepository repository = new ExpenseRepository();
        private readonly Expense existingExpense;
        private string category;
        private string amountText;
        private DateTime date;
        private string notes;
        private string categoryError;
        private string amountError;
        private bool isSaving;

        public AddEditExpenseViewModel(Expense expense = null)
        {
            existingExpense = expense;
            IsEditMode = expense != null;
            WindowTitle = IsEditMode ? "Edit Expense" : "Add Expense";

            CategorySuggestions = new ObservableCollection<string>();

            SaveCommand = new RelayCommand(_ => SaveAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

            if (IsEditMode)
            {
                Category = expense.Category;
                AmountText = expense.Amount.ToString("0.##");
                Date = expense.Date;
                Notes = expense.Notes;
            }
            else
            {
                Date = DateTime.Today;
            }

            _ = LoadCategorySuggestionsAsync();
        }

        public event Action<bool> RequestClose;

        public bool IsEditMode { get; }

        public string WindowTitle { get; }

        public string Category
        {
            get => category;
            set => SetProperty(ref category, value);
        }

        public string AmountText
        {
            get => amountText;
            set => SetProperty(ref amountText, value);
        }

        public DateTime Date
        {
            get => date;
            set => SetProperty(ref date, value);
        }

        public string Notes
        {
            get => notes;
            set => SetProperty(ref notes, value);
        }

        public ObservableCollection<string> CategorySuggestions { get; }

        public string CategoryError
        {
            get => categoryError;
            set => SetProperty(ref categoryError, value);
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

        private async Task LoadCategorySuggestionsAsync()
        {
            try
            {
                var categories = await repository.GetCategoriesAsync().ConfigureAwait(true);
                CategorySuggestions.Clear();
                foreach (var item in categories)
                {
                    CategorySuggestions.Add(item);
                }
            }
            catch
            {
                // Suggestions are optional; ignore load failures.
            }
        }

        private async void SaveAsync()
        {
            CategoryError = null;
            AmountError = null;

            if (string.IsNullOrWhiteSpace(Category))
            {
                CategoryError = "Category is required.";
            }

            decimal amount;
            if (!decimal.TryParse(AmountText, out amount) || amount <= 0)
            {
                AmountError = "Amount must be a number greater than zero.";
            }

            if (Date.Date > DateTime.Today.AddDays(1))
            {
                AmountError = "Date cannot be more than one day in the future.";
            }

            if (!string.IsNullOrEmpty(CategoryError) || !string.IsNullOrEmpty(AmountError))
            {
                return;
            }

            IsSaving = true;

            try
            {
                var expense = new Expense
                {
                    ExpenseID = IsEditMode ? existingExpense.ExpenseID : 0,
                    Category = Category.Trim(),
                    Amount = amount,
                    Date = Date,
                    Notes = Notes
                };

                bool success;
                if (IsEditMode)
                {
                    success = await repository.UpdateExpenseAsync(expense).ConfigureAwait(true);
                }
                else
                {
                    success = await repository.AddExpenseAsync(expense).ConfigureAwait(true);
                }

                if (success)
                {
                    RequestClose?.Invoke(true);
                }
                else
                {
                    CategoryError = "Unable to save. Please try again.";
                }
            }
            catch
            {
                CategoryError = "Unable to save. Please try again.";
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}
