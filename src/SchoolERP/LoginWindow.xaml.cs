using System.Windows;
using SchoolERP.ViewModels;

namespace SchoolERP
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel viewModel;

        public LoginWindow()
        {
            InitializeComponent();

            viewModel = new LoginViewModel();
            viewModel.LoginSucceeded += ViewModel_LoginSucceeded;
            DataContext = viewModel;
        }

        private void ViewModel_LoginSucceeded(object sender, System.EventArgs e)
        {
            var dashboard = new MainWindow();
            if (Application.Current != null)
            {
                Application.Current.MainWindow = dashboard;
            }
            dashboard.Show();
            Close();
        }
    }
}
