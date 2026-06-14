using System.Windows.Controls;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }
    }
}
