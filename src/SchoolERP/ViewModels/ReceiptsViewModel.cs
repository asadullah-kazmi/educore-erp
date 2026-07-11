using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class ReceiptsViewModel : ObservableObject
    {
        private readonly ReceiptRepository repository = new ReceiptRepository();
        private readonly List<FeeReceipt> loadedReceipts = new List<FeeReceipt>();
        private string searchText;
        private string statusMessage;
        private bool isBusy;

        public ReceiptsViewModel()
        {
            Receipts = new ObservableCollection<FeeReceipt>();
            LoadCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsBusy);
            _ = LoadAsync();
        }

        public ObservableCollection<FeeReceipt> Receipts { get; }
        public ICommand LoadCommand { get; }

        public string SearchText
        {
            get => searchText;
            set
            {
                if (SetProperty(ref searchText, value))
                {
                    ApplySearch();
                }
            }
        }

        public string StatusMessage
        {
            get => statusMessage;
            set => SetProperty(ref statusMessage, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                if (SetProperty(ref isBusy, value))
                {
                    (LoadCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                loadedReceipts.Clear();
                loadedReceipts.AddRange(await repository.GetAllAsync().ConfigureAwait(true));
                ApplySearch();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading receipts: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplySearch()
        {
            var query = (SearchText ?? string.Empty).Trim();
            var filtered = string.IsNullOrWhiteSpace(query)
                ? loadedReceipts
                : loadedReceipts.Where(receipt =>
                    Contains(receipt.ReceiptNumber, query) ||
                    Contains(receipt.StudentName, query) ||
                    Contains(receipt.RegistrationNo, query) ||
                    Contains(receipt.ClassName, query) ||
                    Contains(receipt.Section, query) ||
                    Contains(receipt.PaymentDateDisplay, query) ||
                    Contains(receipt.Details, query));

            Receipts.Clear();
            foreach (var receipt in filtered)
            {
                Receipts.Add(receipt);
            }

            StatusMessage = string.IsNullOrWhiteSpace(query)
                ? "Showing " + Receipts.Count + " receipts."
                : "Showing " + Receipts.Count + " of " + loadedReceipts.Count + " receipts.";
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
