using System.Windows;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class PaySalaryWindow : Window
    {
        public PaySalaryWindow(StaffSalaryRowViewModel staff, string month)
        {
            var viewModel = new PaySalaryViewModel(staff, month);
            viewModel.RequestClose += success =>
            {
                DialogResult = success;
                Close();
            };
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
