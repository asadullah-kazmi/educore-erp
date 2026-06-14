using System.Windows;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class AddEditStaffWindow : Window
    {
        public AddEditStaffWindow(int? teacherId)
        {
            InitializeComponent();
            var viewModel = new AddEditStaffViewModel(teacherId);
            viewModel.RequestClose += success =>
            {
                DialogResult = success;
                Close();
            };
            DataContext = viewModel;
        }
    }
}
