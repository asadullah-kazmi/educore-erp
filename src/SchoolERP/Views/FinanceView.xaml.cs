using System.Windows.Controls;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class FinanceView : UserControl
    {
        public FinanceView()
        {
            InitializeComponent();
            DataContext = new FinanceViewModel();
        }
    }
}
