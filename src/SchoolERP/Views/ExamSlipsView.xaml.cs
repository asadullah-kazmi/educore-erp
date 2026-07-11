using System.Windows;
using System.Windows.Controls;
using SchoolERP.Models;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class ExamSlipsView : UserControl
    {
        public ExamSlipsView()
        {
            InitializeComponent();
            DataContext = new ExamSlipsViewModel();
        }

        private void ViewSlip_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is ExamSlip slip)
            {
                var window = new ExamSlipDetailWindow(slip)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
        }
    }
}
