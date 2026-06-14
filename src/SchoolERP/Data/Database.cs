using System.Configuration;
using System.Data.SqlClient;

namespace SchoolERP.Data
{
    public static class Database
    {
        public static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["SchoolERP"].ConnectionString;

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}
