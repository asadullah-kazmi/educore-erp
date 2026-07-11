using System.Windows;
using SchoolERP.Models;
using SchoolERP.Services;

namespace SchoolERP.Views
{
    public partial class ExamSlipDetailWindow : Window
    {
        private readonly ExamSlip slip;

        public ExamSlipDetailWindow(ExamSlip examSlip)
        {
            InitializeComponent();
            slip = examSlip;
            DataContext = examSlip;
            Loaded += ExamSlipDetailWindow_Loaded;
        }

        private void ExamSlipDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;
            MaxHeight = workArea.Height - 40;
            MaxWidth = workArea.Width - 40;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (slip != null)
            {
                ReportPrintService.PrintExamSlips(new[] { slip }, slip.TermName, slip.FeeMonth);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
