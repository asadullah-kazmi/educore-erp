using System.Windows;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class SalaryHistoryWindow : Window
    {
        public SalaryHistoryWindow(int teacherId, string teacherName, bool isAdmin)
        {
            InitializeComponent();
            DataContext = new SalaryHistoryViewModel(teacherId, teacherName, isAdmin);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
