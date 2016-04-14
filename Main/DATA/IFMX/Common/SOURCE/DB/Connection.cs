using System.Data;
using IBM.Data.Informix;
using Npgsql;
namespace STCLINE.KP50.DataBase
{
    public class DBManager
    {
        // создание новой команды в подключении
        static public IDbCommand newDbCommand(string sqlString, IDbConnection connectionID, IDbTransaction transactionID = null)
        {
            // какое подключение передали, то такую команду и возвращаем
#if PG
            if (connectionID.GetType() == typeof(NpgsqlConnection))
            {
                return new NpgsqlCommand(sqlString, (NpgsqlConnection)connectionID, (NpgsqlTransaction)transactionID);
            }
#else
            if (connectionID.GetType() == typeof(IfxConnection))
            {
                return new IfxCommand(sqlString, (IfxConnection)connectionID, (IfxTransaction)transactionID);
            }
#endif
            else
                return null;
        }

        // добавление параметра в команду
        static public IDbDataParameter addDbCommandParameter(IDbCommand command, string parameterName)
        {
            IDbDataParameter param = command.CreateParameter();
            param.ParameterName = parameterName;
            command.Parameters.Add(param);
            return param;
        }

        // добавление параметра в команду
        static public IDbDataParameter addDbCommandParameter(IDbCommand command, string parameterName, DbType parameterType)
        {
            IDbDataParameter param = addDbCommandParameter(command, parameterName);
            if (param != null)
            {
                param.DbType = parameterType;
            }
            return param;
        }

        // добавление параметра в команду
        static public IDbDataParameter addDbCommandFixedLengthParameter(IDbCommand command, string parameterName, DbType parameterType, int size)
        {
            IDbDataParameter param = addDbCommandParameter(command, parameterName, parameterType);
            if (param != null)
            {
                param.Size = size;
            }
            return param;
        }

        // добавление параметра в команду
        static public IDbDataParameter addDbCommandDecimalParameter(IDbCommand command, string parameterName, DbType parameterType, byte precision, byte scale)
        {
            IDbDataParameter param = addDbCommandParameter(command, parameterName, parameterType);
            if (param != null)
            {
                param.Precision = precision;
                param.Scale = scale;
            }
            return param;
        }

        // добавление параметра в команду
        static public IDbDataParameter addDbCommandParameter(IDbCommand command, string parameterName, object value)
        {
            IDbDataParameter param = addDbCommandParameter(command, parameterName);
            if (param != null)
            {
                param.Value = value;
            }
            return param;
        }

        // добавление параметра в команду
        static public IDbDataParameter addDbCommandParameter(IDbCommand command, string parameterName, DbType parameterType, object value)
        {
            IDbDataParameter param = addDbCommandParameter(command, parameterName, parameterType);
            if (param != null)
            {
                param.Value = value;
            }
            return param;
        }

        // создание нового подключения по строке соединения
        static public IDbConnection newDbConnection(string connectionString)
        {
            // пока жестко возвращаем подключение к informix
            // потом будем думать как разделять подключения
#if PG
            //connectionString = "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=websmr;Preload Reader=true;";
            return new NpgsqlConnection(connectionString);
#else
            // TODO: убрать это?!
            connectionString = connectionString.Replace("UID", "User ID");
            connectionString = connectionString.Replace("Pwd", "Password");
            IfxConnectionStringBuilder conn = new IfxConnectionStringBuilder(connectionString);
            return new IfxConnection(conn.ToString());
#endif
        }

        // получить информацию о сервере подключения
        // (т.к. нет в стандартном интерфейсе IDbConnection)
        static public string getServer(IDbConnection connectionID)
        {
            // какое подключение передали, то такой сервер и возвращаем
#if PG
            if (connectionID.GetType() == typeof(NpgsqlConnection))
            {
                //return (connectionID as NpgsqlConnection).Host;
                //TODO: для Postgresql сервер будет определять потом, это например будет год и месяц и он будет подставляться в конце имени схемы
                return "";
            }
#else
            if (connectionID.GetType() == typeof(IfxConnection))
            {
                return (connectionID as IfxConnection).Server;
            }
#endif
            else
                return null;
        }

        static public string getDbName(string connectionString)
        {
#if PG
            //connectionString = "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=websmr;Preload Reader=true;";
            NpgsqlConnectionStringBuilder conn = new NpgsqlConnectionStringBuilder(connectionString);
#else
            // TODO: убрать это?!
            connectionString = connectionString.Replace("UID", "User ID");
            connectionString = connectionString.Replace("Pwd", "Password");
            IfxConnectionStringBuilder conn = new IfxConnectionStringBuilder(connectionString);
#endif
            return conn.Database;
        }
    }
}