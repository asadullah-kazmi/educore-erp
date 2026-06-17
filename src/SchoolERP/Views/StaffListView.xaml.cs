using System.Windows.Controls;
using System.Windows.Input;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class StaffListView : UserControl
    {
        public StaffListView()
        {
            InitializeComponent();
            DataContext = new StaffListViewModel();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((DataGrid)sender).SelectedItem is StaffViewModel staff)
            {
                ((StaffListViewModel)DataContext).ViewStaffDetailCommand.Execute(staff);
            }
        }
    }
}
