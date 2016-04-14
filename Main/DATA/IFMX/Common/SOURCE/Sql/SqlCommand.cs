namespace STCLINE.KP50.DataBase
{
    using System.Collections.Generic;
    
    /// <summary>Sql команда</summary>
    public class SqlCommand
    {
        /// <summary>Sql запрос</summary>
        public string Sql { get; private set; }

        /// <summary>Строка подключения</summary>
        public string ConnectionString { get; private set; }

        /// <summary>Список параметров запроса</summary>
        public Dictionary<string, object> Parameters { get; private set; }

        /// <summary>Создать sql команду</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="connectionString">Строка подключения</param>
        /// <returns>Sql команда</returns>
        public static SqlCommand CreateCommand(string sql, string connectionString)
        {
            var sqlCommand = new SqlCommand
            {
                Sql = sql,
                ConnectionString = connectionString,
                Parameters = new Dictionary<string, object>()
            };
            return sqlCommand;
        }

        /// <summary>Добавить параметр запроса</summary>
        /// <typeparam name="TPar">Тип параметра</typeparam>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        public void AddParameter<TPar>(string key, TPar value)
        {
            if (Parameters.ContainsKey(key))
            {
                Parameters[key] = value;
            }
            else
            {
                Parameters.Add(key, value);
            }
        }
    }
}