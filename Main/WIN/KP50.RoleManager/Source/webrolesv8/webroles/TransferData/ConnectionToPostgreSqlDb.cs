using Npgsql;
namespace webroles
{
    class ConnectionToPostgreSqlDb
    {        
        public static NpgsqlConnection GetConnection ()
        {
            var npgStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = "linepg",
                Database = "webroles",
                UserName = "postgres",
                Password = "postgres",
                CommandTimeout = 50000
            };
            //var npgStringBuilder = new NpgsqlConnectionStringBuilder
            //{
            //    Host = "localhost",
            //    Database = "postgres",
            //    UserName = "postgres",
            //    Password = "postgres"
            //};
            return new NpgsqlConnection(npgStringBuilder.ConnectionString);
        }
    }
}
