using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SchoolERP.Data;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class AttendanceView : UserControl
    {
        public AttendanceView()
        {
            InitializeComponent();
            DataContext = new AttendanceViewModel();
        }

        private async void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AttendanceDataGrid.SelectedItem is TeacherAttendanceRowViewModel selected)
            {
                var repo = new StaffRepository();
                var teacher = await repo.GetStaffByIdAsync(selected.TeacherID);
                var window = new StaffDetailWindow(StaffViewModel.FromModel(teacher))
                {
                    Owner = Application.Current.MainWindow
                };
                window.ShowDialog();
            }
        }
    }
}
