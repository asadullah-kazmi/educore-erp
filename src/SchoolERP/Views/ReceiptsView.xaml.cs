using System.Windows;
using System.Windows.Controls;
using SchoolERP.Models;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class ReceiptsView : UserControl
    {
        public ReceiptsView()
        {
            InitializeComponent();
            DataContext = new ReceiptsViewModel();
        }

        private void ViewReceipt_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is FeeReceipt receipt)
            {
                new FeeReceiptWindow(receipt)
                {
                    Owner = Window.GetWindow(this)
                }.ShowDialog();
            }
        }
    }
}
