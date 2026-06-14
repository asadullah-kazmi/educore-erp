using System.Windows.Controls;
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
    }
}
