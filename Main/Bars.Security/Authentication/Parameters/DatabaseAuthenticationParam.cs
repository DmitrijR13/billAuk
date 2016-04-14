using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Parameters
{
    /// <summary>
    /// Провайдер аутентификации через базу данных
    /// </summary>
    public class DatabaseAuthenticationParam
    {
        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string User { get; internal set; }

        /// <summary>
        /// Хэпированный пароль
        /// </summary>
        public string Password { get; internal set; }
    }
}
