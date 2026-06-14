using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SchoolERP
{
    public partial class App : Application
    {
        static App()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.AppendAllText(path, DateTime.Now.ToString("u") + " - App type loaded early" + Environment.NewLine);
            }
            catch { }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            // Let the exception propagate to allow Windows to surface any dialog.
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception);
        }

        private static void LogException(Exception ex)
        {
            try
            {
                if (ex == null) return;
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.AppendAllText(path, DateTime.Now.ToString("u") + " - " + ex + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // swallow logging errors
            }
        }
    }
}
