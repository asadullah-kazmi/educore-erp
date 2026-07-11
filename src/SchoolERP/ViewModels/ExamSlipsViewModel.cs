using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.Models;
using SchoolERP.Services;

namespace SchoolERP.ViewModels
{
    public class ExamSlipsViewModel : ObservableObject
    {
        private readonly ExamSlipRepository examSlipRepository = new ExamSlipRepository();
        private readonly StudentRepository studentRepository = new StudentRepository();

        private string selectedTerm;
        private string selectedFeeMonth;
        private string selectedClass = "All Classes";
        private string selectedSection = "All Sections";
        private ExamSlip selectedSlip;
        private string statusMessage;
        private bool isBusy;
        private int eligibleStudentCount;

        public ExamSlipsViewModel()
        {
            TermOptions = new ObservableCollection<string>
            {
                "1st Term",
                "2nd Term",
                "Final Term",
                "Mid Term",
                "Annual Exam"
            };

            MonthOptions = new ObservableCollection<string>();
            ClassOptions = new ObservableCollection<string>();
            SectionOptions = new ObservableCollection<string>();
            ExamSlips = new ObservableCollection<ExamSlip>();

            LoadCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsBusy);
            GenerateCommand = new RelayCommand(async _ => await GenerateAsync(), _ => !IsBusy && CanGenerate);
            PrintAllCommand = new RelayCommand(_ => PrintAll(), _ => ExamSlips.Count > 0);
            PrintSelectedCommand = new RelayCommand(_ => PrintSelected(), _ => SelectedSlip != null);

            InitializeOptions();
            _ = LoadLookupsAsync();
        }

        public ObservableCollection<string> TermOptions { get; }
        public ObservableCollection<string> MonthOptions { get; }
        public ObservableCollection<string> ClassOptions { get; }
        public ObservableCollection<string> SectionOptions { get; }
        public ObservableCollection<ExamSlip> ExamSlips { get; }

        public ICommand LoadCommand { get; }
        public ICommand GenerateCommand { get; }
        public ICommand PrintAllCommand { get; }
        public ICommand PrintSelectedCommand { get; }

        public string SelectedTerm
        {
            get => selectedTerm;
            set
            {
                if (SetProperty(ref selectedTerm, value))
                {
                    RefreshCommands();
                    _ = RefreshAsync();
                }
            }
        }

        public string SelectedFeeMonth
        {
            get => selectedFeeMonth;
            set
            {
                if (SetProperty(ref selectedFeeMonth, value))
                {
                    RefreshCommands();
                    _ = RefreshAsync();
                }
            }
        }

        public string SelectedClass
        {
            get => selectedClass;
            set
            {
                if (SetProperty(ref selectedClass, value))
                {
                    _ = RefreshAsync();
                }
            }
        }

        public string SelectedSection
        {
            get => selectedSection;
            set
            {
                if (SetProperty(ref selectedSection, value))
                {
                    _ = RefreshAsync();
                }
            }
        }

        public ExamSlip SelectedSlip
        {
            get => selectedSlip;
            set
            {
                if (SetProperty(ref selectedSlip, value))
                {
                    RefreshCommands();
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
                    RefreshCommands();
                }
            }
        }

        public int EligibleStudentCount
        {
            get => eligibleStudentCount;
            set => SetProperty(ref eligibleStudentCount, value);
        }

        public bool CanGenerate =>
            !string.IsNullOrWhiteSpace(SelectedTerm) &&
            !string.IsNullOrWhiteSpace(SelectedFeeMonth);

        private void InitializeOptions()
        {
            var startDate = new DateTime(2025, 1, 1);
            for (var i = 0; i < 36; i++)
            {
                MonthOptions.Add(startDate.AddMonths(i).ToString("MMM yyyy"));
            }

            SelectedTerm = TermOptions.FirstOrDefault();
            SelectedFeeMonth = DateTime.Now.ToString("MMM yyyy");
        }

        private async Task LoadLookupsAsync()
        {
            try
            {
                IsBusy = true;
                ClassOptions.Clear();
                ClassOptions.Add("All Classes");
                foreach (var schoolClass in await studentRepository.GetAllClassesAsync().ConfigureAwait(true))
                {
                    if (!string.IsNullOrWhiteSpace(schoolClass.ClassName))
                    {
                        ClassOptions.Add(schoolClass.ClassName);
                    }
                }

                SectionOptions.Clear();
                SectionOptions.Add("All Sections");
                foreach (var section in await examSlipRepository.GetSectionsAsync().ConfigureAwait(true))
                {
                    if (!string.IsNullOrWhiteSpace(section))
                    {
                        SectionOptions.Add(section);
                    }
                }

                SelectedClass = "All Classes";
                SelectedSection = "All Sections";
                await RefreshAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading exam slip filters: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadAsync()
        {
            await RefreshAsync().ConfigureAwait(true);
        }

        private async Task GenerateAsync()
        {
            try
            {
                IsBusy = true;
                var created = await examSlipRepository
                    .GenerateSlipsAsync(SelectedTerm, SelectedFeeMonth, SelectedClass, SelectedSection)
                    .ConfigureAwait(true);

                await RefreshAsync().ConfigureAwait(true);

                StatusMessage = created == 0
                    ? "No new slips generated. Either no eligible paid students were found or slips already exist."
                    : "Generated " + created + " exam slips for paid students.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error generating exam slips: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            if (!CanGenerate)
            {
                return;
            }

            try
            {
                var slips = await examSlipRepository
                    .GetSlipsAsync(SelectedTerm, SelectedFeeMonth, SelectedClass, SelectedSection)
                    .ConfigureAwait(true);

                ExamSlips.Clear();
                foreach (var slip in slips)
                {
                    ExamSlips.Add(slip);
                }

                EligibleStudentCount = await examSlipRepository
                    .GetEligibleStudentCountAsync(SelectedFeeMonth, SelectedClass, SelectedSection)
                    .ConfigureAwait(true);

                StatusMessage = "Showing " + ExamSlips.Count + " generated slips. Eligible paid students: " + EligibleStudentCount + ".";
                RefreshCommands();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading exam slips: " + ex.Message;
            }
        }

        private void PrintAll()
        {
            ReportPrintService.PrintExamSlips(ExamSlips.ToList(), SelectedTerm, SelectedFeeMonth);
        }

        private void PrintSelected()
        {
            if (SelectedSlip != null)
            {
                ReportPrintService.PrintExamSlips(new[] { SelectedSlip }, SelectedTerm, SelectedFeeMonth);
            }
        }

        private void RefreshCommands()
        {
            (LoadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PrintAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PrintSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CanGenerate));
        }
    }
}
