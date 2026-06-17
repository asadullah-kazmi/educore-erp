using System.Collections.Generic;
using System.Windows;
using SchoolERP.Models;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class StudentDetailWindow : Window
    {
        public StudentDetailWindow(StudentViewModel student, List<FeeRecord> fees)
        {
            InitializeComponent();
            DataContext = new StudentDetailViewModel(student, fees);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
