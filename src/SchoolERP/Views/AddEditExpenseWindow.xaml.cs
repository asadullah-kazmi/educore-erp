using System.Windows;
using SchoolERP.Models;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class AddEditExpenseWindow : Window
    {
        public AddEditExpenseWindow(Expense expense)
        {
            InitializeComponent();
            var viewModel = new AddEditExpenseViewModel(expense);
            viewModel.RequestClose += success =>
            {
                DialogResult = success;
                Close();
            };
            DataContext = viewModel;
        }
    }
}
