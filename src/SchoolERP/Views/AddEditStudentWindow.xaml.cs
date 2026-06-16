using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private void MonthlyFeeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !decimal.TryParse(newText, out _);
        }
    }
}
