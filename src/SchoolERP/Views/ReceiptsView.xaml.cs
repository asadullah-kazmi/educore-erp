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

        private async void ViewReceipt_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is FeeReceipt receipt)
            {
                if (receipt.IsFamily && receipt.FamilyReceiptID.HasValue)
                {
                    var familyReceipt = await new Data.FamilyReceiptRepository().GetByIdAsync(receipt.FamilyReceiptID.Value);
                    if (familyReceipt != null)
                    {
                        new FamilyFeeReceiptWindow(familyReceipt) { Owner = Window.GetWindow(this) }.ShowDialog();
                    }
                    return;
                }
                new FeeReceiptWindow(receipt)
                {
                    Owner = Window.GetWindow(this)
                }.ShowDialog();
            }
        }
    }
}
