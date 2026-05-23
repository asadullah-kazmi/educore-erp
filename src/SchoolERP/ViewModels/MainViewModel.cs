using SchoolERP.Services;

namespace SchoolERP.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private string welcomeMessage;

        public MainViewModel()
        {
            var session = AppSession.Current;

            if (session != null && session.IsAuthenticated)
            {
                var displayName = string.IsNullOrWhiteSpace(session.FullName) ? session.Username : session.FullName;
                var roles = session.Roles == null || session.Roles.Count == 0
                    ? string.Empty
                    : " (" + string.Join(", ", session.Roles) + ")";

                WelcomeMessage = "Welcome, " + displayName + roles;
            }
            else
            {
                WelcomeMessage = "Welcome to School ERP";
            }
        }

        public string WelcomeMessage
        {
            get => welcomeMessage;
            set => SetProperty(ref welcomeMessage, value);
        }
    }
}
