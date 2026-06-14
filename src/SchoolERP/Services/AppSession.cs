using System;
using System.Collections.Generic;
using System.Linq;

namespace SchoolERP.Services
{
    public sealed class AppSession
    {
        private static readonly object SyncRoot = new object();

        public static AppSession Current { get; private set; }

        public static string CurrentRole => Current?.ResolveCurrentRole() ?? "Viewer";

        public int UserId { get; }
        public string Username { get; }
        public string FullName { get; }
        public IReadOnlyList<string> Roles { get; }
        public DateTime LoggedInAt { get; }

        public bool IsAuthenticated => UserId > 0;

        public bool HasRole(string role)
        {
            return Roles != null && Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        private string ResolveCurrentRole()
        {
            if (HasRole("Admin"))
            {
                return "Admin";
            }

            if (HasRole("Staff") || HasRole("Receptionist"))
            {
                return "Staff";
            }

            return "Viewer";
        }

        private AppSession(int userId, string username, string fullName, IReadOnlyList<string> roles)
        {
            UserId = userId;
            Username = username;
            FullName = fullName;
            Roles = roles ?? Array.Empty<string>();
            LoggedInAt = DateTime.Now;
        }

        public static AppSession Start(AuthResult authResult)
        {
            if (authResult == null || !authResult.Success || !authResult.UserId.HasValue)
            {
                throw new ArgumentException("A successful authentication result is required.", nameof(authResult));
            }

            var session = new AppSession(
                authResult.UserId.Value,
                authResult.Username,
                authResult.FullName,
                authResult.Roles ?? new List<string>());

            lock (SyncRoot)
            {
                Current = session;
            }

            return session;
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                Current = null;
            }
        }
    }
}
