using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SchoolERP.Models;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class FeesView : UserControl
    {
        public FeesView()
        {
            InitializeComponent();
            DataContext = new FeesViewModel();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((DataGrid)sender).SelectedItem is FeeRecord fee)
            {
                ((FeesViewModel)DataContext).ViewFeeDetailCommand.Execute(fee);
            }
        }

        private void ReceiptsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.Navigation.CurrentPage = "Receipts";
            }
        }
    }
}
