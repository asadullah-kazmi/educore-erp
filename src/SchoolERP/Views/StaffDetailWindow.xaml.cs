using System.Windows;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class StaffDetailWindow : Window
    {
        public StaffDetailWindow(StaffViewModel staff)
        {
            DataContext = staff;
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
