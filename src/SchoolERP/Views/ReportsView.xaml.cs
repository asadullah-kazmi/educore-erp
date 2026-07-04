using System.Windows.Controls;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            DataContext = new ReportsViewModel();
            Loaded += ReportsView_Loaded;
        }

        private async void ReportsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ReportsViewModel viewModel)
            {
                await viewModel.InitializeAsync().ConfigureAwait(true);
            }
        }
    }
}
