using System.Windows.Controls;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class FeesView : UserControl
    {
        public FeesView()
        {
            InitializeComponent();
            DataContext = new FeesViewModel();
        }
    }
}
