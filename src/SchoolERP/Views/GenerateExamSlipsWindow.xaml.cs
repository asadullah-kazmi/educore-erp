using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SchoolERP.Data;
using SchoolERP.Models;

namespace SchoolERP.Views
{
    public partial class GenerateExamSlipsWindow : Window
    {
        private readonly StudentRepository studentRepository;
        private readonly ExamSlipRepository examSlipRepository;

        public string SelectedExamType { get; private set; }
        public string SelectedFeeMonth { get; private set; }
        public string GenerationScope { get; private set; }
        public string SelectedClass { get; private set; }
        public string SelectedSection { get; private set; }
        public int? SelectedStudentId { get; private set; }

        public GenerateExamSlipsWindow()
        {
            InitializeComponent();
            studentRepository = new StudentRepository();
            examSlipRepository = new ExamSlipRepository();
            Loaded += GenerateExamSlipsWindow_Loaded;
        }

        private async void GenerateExamSlipsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var startDate = new DateTime(2025, 1, 1);
                for (var i = 0; i < 36; i++)
                {
                    CmbFeeMonth.Items.Add(startDate.AddMonths(i).ToString("MMM yyyy"));
                }
                CmbFeeMonth.SelectedItem = DateTime.Now.ToString("MMM yyyy");

                var classes = await studentRepository.GetAllClassesAsync();
                CmbClass.ItemsSource = classes;

                var sections = await examSlipRepository.GetSectionsAsync();
                CmbSection.ItemsSource = sections;

                var students = await studentRepository.GetAllStudentsAsync();
                CmbStudent.ItemsSource = students.Where(s => s.IsActive).Select(s => new {
                    Display = $"{s.RegistrationNo} - {s.Name}",
                    s.StudentID
                }).ToList();
                CmbStudent.DisplayMemberPath = "Display";
                CmbStudent.SelectedValuePath = "StudentID";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Error loading options: " + ex.Message;
            }
        }

        private void Scope_Checked(object sender, RoutedEventArgs e)
        {
            if (PanelTarget == null) return;

            PanelTarget.Visibility = Visibility.Collapsed;
            CmbClass.Visibility = Visibility.Collapsed;
            CmbSection.Visibility = Visibility.Collapsed;
            CmbStudent.Visibility = Visibility.Collapsed;

            if (RadioClass.IsChecked == true)
            {
                PanelTarget.Visibility = Visibility.Visible;
                LblTarget.Text = "Select Class";
                CmbClass.Visibility = Visibility.Visible;
            }
            else if (RadioSection.IsChecked == true)
            {
                PanelTarget.Visibility = Visibility.Visible;
                LblTarget.Text = "Select Section";
                CmbSection.Visibility = Visibility.Visible;
            }
            else if (RadioStudent.IsChecked == true)
            {
                PanelTarget.Visibility = Visibility.Visible;
                LblTarget.Text = "Select Student";
                CmbStudent.Visibility = Visibility.Visible;
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            SelectedExamType = CmbExamType.Text;
            SelectedFeeMonth = CmbFeeMonth.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(SelectedExamType))
            {
                TxtStatus.Text = "Please enter or select an Exam Type.";
                return;
            }

            if (RadioAll.IsChecked == true)
            {
                GenerationScope = "All";
            }
            else if (RadioClass.IsChecked == true)
            {
                GenerationScope = "Class";
                SelectedClass = CmbClass.SelectedValue as string;
                if (string.IsNullOrWhiteSpace(SelectedClass))
                {
                    TxtStatus.Text = "Please select a class.";
                    return;
                }
            }
            else if (RadioSection.IsChecked == true)
            {
                GenerationScope = "Section";
                SelectedSection = CmbSection.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(SelectedSection))
                {
                    TxtStatus.Text = "Please select a section.";
                    return;
                }
            }
            else if (RadioStudent.IsChecked == true)
            {
                GenerationScope = "Student";
                SelectedStudentId = CmbStudent.SelectedValue as int?;
                if (SelectedStudentId == null)
                {
                    TxtStatus.Text = "Please select a student.";
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
