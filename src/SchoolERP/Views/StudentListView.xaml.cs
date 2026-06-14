using System.Windows.Controls;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class StudentListView : UserControl
    {
        public StudentListView()
        {
            InitializeComponent();
            DataContext = new StudentListViewModel();
        }
    }
}
