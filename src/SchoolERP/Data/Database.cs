using System;
using System.Data.SqlClient;

namespace SchoolERP.Data
{
    public static class Database
    {
        // Update the connection string in app config when ready
        public static string ConnectionString = "Server=.;Database=SchoolERP;Trusted_Connection=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}
