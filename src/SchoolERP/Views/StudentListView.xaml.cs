using System.Windows.Controls;
using System.Windows.Input;
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

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((DataGrid)sender).SelectedItem is StudentViewModel student)
            {
                ((StudentListViewModel)DataContext).ViewStudentDetailCommand.Execute(student);
            }
        }
    }
}
