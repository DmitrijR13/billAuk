using Bars.Security.Extentions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Configuration
{
    /// <summary>
    /// Конфигурация параметров СУБД для аутентификации
    /// </summary>
    public class DatabaseAuthenticationConfiguration : Singleton<DatabaseAuthenticationConfiguration>
    {
        /// <summary>
        /// Схема для хранения данных в СУБД
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Таблица для хранения данных в СУБД
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// Имя таблицы в СУБД
        /// </summary>
        internal string DbTable { get { return string.Format("{0}.{1}", Schema, Table); } }

        /// <summary>
        /// Base singleton constructor
        /// </summary>
        private DatabaseAuthenticationConfiguration()
        {
            Schema = "Security";
            Table = "Users";
        }
    }
}
