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
            AvailableClasses = new ObservableCollection<string>();
            AvailableSections = new ObservableCollection<string>();
            
            LoadCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsBusy);
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            
            _ = LoadAsync();
        }

        public ObservableCollection<FeeReceipt> Receipts { get; }
        public ObservableCollection<string> AvailableClasses { get; }
        public ObservableCollection<string> AvailableSections { get; }
        public ICommand LoadCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        private string selectedClass;
        public string SelectedClass
        {
            get => selectedClass;
            set
            {
                if (SetProperty(ref selectedClass, value)) ApplySearch();
            }
        }

        private string selectedSection;
        public string SelectedSection
        {
            get => selectedSection;
            set
            {
                if (SetProperty(ref selectedSection, value)) ApplySearch();
            }
        }

        private DateTime? selectedDate;
        public DateTime? SelectedDate
        {
            get => selectedDate;
            set
            {
                if (SetProperty(ref selectedDate, value)) ApplySearch();
            }
        }

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

                var classes = loadedReceipts.Select(r => r.ClassName).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c).ToList();
                AvailableClasses.Clear();
                AvailableClasses.Add("All Classes");
                foreach (var c in classes) AvailableClasses.Add(c);
                if (string.IsNullOrEmpty(SelectedClass) || !AvailableClasses.Contains(SelectedClass)) SelectedClass = "All Classes";

                var sections = loadedReceipts.Select(r => r.Section).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
                AvailableSections.Clear();
                AvailableSections.Add("All Sections");
                foreach (var s in sections) AvailableSections.Add(s);
                if (string.IsNullOrEmpty(SelectedSection) || !AvailableSections.Contains(SelectedSection)) SelectedSection = "All Sections";

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
            var filtered = loadedReceipts.Where(receipt =>
            {
                bool matchesText = string.IsNullOrWhiteSpace(query) ||
                    Contains(receipt.ReceiptNumber, query) ||
                    Contains(receipt.StudentName, query) ||
                    Contains(receipt.RegistrationNo, query) ||
                    Contains(receipt.Details, query);

                bool matchesClass = SelectedClass == "All Classes" || string.IsNullOrEmpty(SelectedClass) || receipt.ClassName == SelectedClass;
                bool matchesSection = SelectedSection == "All Sections" || string.IsNullOrEmpty(SelectedSection) || receipt.Section == SelectedSection;
                bool matchesDate = !SelectedDate.HasValue || receipt.PaymentDate.Date == SelectedDate.Value.Date;

                return matchesText && matchesClass && matchesSection && matchesDate;
            });

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

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedClass = "All Classes";
            SelectedSection = "All Sections";
            SelectedDate = null;
        }
    }
}
