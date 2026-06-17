using System.Windows;
using SchoolERP.Models;

namespace SchoolERP.Views
{
    public partial class FeeDetailWindow : Window
    {
        public FeeDetailWindow(FeeRecord fee)
        {
            DataContext = fee;
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
