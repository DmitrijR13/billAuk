using Bars.Security.Authentication.Attributes;
using Bars.Security.Authorization.Access;
using Bars.Security.Security.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Session
{
    /// <summary>
    /// Описание пользователя
    /// </summary>
    public class User
    {
        /// <summary>
        /// Сессия пользователя
        /// </summary>
        [IgnoreOnRegister]
        public UserSession Session { get; protected internal set; }

        /// <summary>
        /// Уникальный идентификатор пользователя
        /// </summary>
        [IgnoreOnRegister]
        public long UserId { get; protected internal set; }

        /// <summary>
        /// Логин пользователя
        /// </summary>
        [ValidateOnRegister(ValidationStrategy.Login)]
        public string Login { get; protected internal set; }

        /// <summary>
        /// E-Mail пользователя
        /// </summary>
        [ValidateOnRegister(ValidationStrategy.EMail)]
        public string EMail { get; protected internal set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        [ValidateOnRegister(ValidationStrategy.User)]
        public string Name { get; protected internal set; }

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        [ValidateOnRegister(ValidationStrategy.Password)]
        public Password Password { get; protected internal set; }

        /// <summary>
        /// Доступные пользователю роли
        /// </summary>
        [IgnoreOnRegister]
        public AccessibleObjectsCollection Roles { get; protected internal set; }

        /// <summary>
        /// Аутентифицирует указанного пользователя в системе
        /// </summary>
        /// <param name="token">Токен сессии пользователя</param>
        /// <returns>Аутентифицированный пользователь</returns>
        public static implicit operator User(string token)
        {
            return AuthenticationAdapter.Instance.Authenticate(token).User;
        }
    }
}
