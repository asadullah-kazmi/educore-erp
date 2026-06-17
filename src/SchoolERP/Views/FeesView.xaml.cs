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
    }
}
