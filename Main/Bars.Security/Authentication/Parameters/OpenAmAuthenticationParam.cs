using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Parameters
{
    /// <summary>
    /// Параметр инициализации для провайдера OpenAm
    /// </summary>
    public class OpenAmAuthenticationParam
    {
        /// <summary>
        /// Токен пользователя OpenAm
        /// </summary>
        protected internal string OpenAmToken = null;

        /// <summary>
        /// Логин пользователя OpenAm
        /// </summary>
        public string Login { get; protected internal set; }
    }
}
