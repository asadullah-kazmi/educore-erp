using System.Windows;
using SchoolERP.ViewModels;

namespace SchoolERP
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
