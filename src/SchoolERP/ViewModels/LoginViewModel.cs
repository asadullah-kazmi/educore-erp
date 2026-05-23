using System;
using System.Windows.Input;
using SchoolERP.Services;

namespace SchoolERP.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly AuthenticationService authenticationService;
        private string username;
        private string password;
        private string statusMessage;
        private bool isBusy;

        public event EventHandler LoginSucceeded;

        public LoginViewModel()
            : this(new AuthenticationService())
        {
        }

        public LoginViewModel(AuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            LoginCommand = new RelayCommand(_ => ExecuteLogin(), _ => !IsBusy);
        }

        public string Username
        {
            get => username;
            set => SetProperty(ref username, value);
        }

        public string Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }

        public string StatusMessage
        {
            get => statusMessage;
            set => SetProperty(ref statusMessage, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                if (SetProperty(ref isBusy, value))
                {
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand LoginCommand { get; }

        private void ExecuteLogin()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var result = authenticationService.Login(Username, Password);

                if (!result.Success)
                {
                    StatusMessage = result.ErrorMessage ?? "Login failed.";
                    return;
                }

                AppSession.Start(result);
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = "Unable to sign in: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
