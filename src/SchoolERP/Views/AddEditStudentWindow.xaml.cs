using System.Windows;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class AddEditStudentWindow : Window
    {
        private readonly AddEditStudentViewModel viewModel;

        public AddEditStudentWindow(int? studentId)
        {
            InitializeComponent();
            viewModel = new AddEditStudentViewModel(studentId);
            viewModel.RequestClose += success =>
            {
                DialogResult = success;
                Close();
            };
            DataContext = viewModel;
        }
    }
}
