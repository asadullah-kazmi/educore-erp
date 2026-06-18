using System.Windows;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class SalaryHistoryWindow : Window
    {
        public SalaryHistoryWindow(int teacherId, string teacherName, bool isAdmin)
        {
            DataContext = new SalaryHistoryViewModel(teacherId, teacherName, isAdmin);
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
