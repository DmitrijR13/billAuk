using Bars.Security.Authentication.Parameters;
using Bars.Security.Authentication.Session;
using Bars.Security.Exceptions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Strategy
{
    /// <summary>
    /// Стратегия аутентификации пользователя
    /// </summary>
    public class DatabaseAuthenticationStrategy : AuthenticationStrategy
    {
        /// <summary>
        /// Проверяет входные параметры для провайдера аутентификации
        /// </summary>
        /// <param name="AuthenticationParams">Параметры аутентификации</param>
        /// <returns>Параметр для инициализации провайдера</returns>
        public override object ValidateAuthenticationParams(params object[] AuthenticationParams)
        {
            var Args = AuthenticationParams.Cast<string>().ToArray();
            if (Args.Length != 2)
                throw new InvalidAuthenticationParamsException("Необходимо передать логин и пароль.");
            return new DatabaseAuthenticationParam { User = Args[0], Password = Args[1] };
        }

        /// <summary>
        /// Аутентифицирует пользователя в системе
        /// </summary>
        /// <param name="AuthenticateParams">Параметры аутентификации провайдера</param>
        /// <returns>Сессия и данные аутентифицированного пользователя</returns>
        public override User Authenticate(UserSession session, object AuthenticateParam)
        {
            var prm = ((DatabaseAuthenticationParam)AuthenticateParam);
            var user = GetUserData(session, string.Format("Login = '{0}'", prm.User));
            if (!user.Password.IsValid(prm.Password))
            {
                user.Session.Dispose();
                user = null;
            }
            return user;
        }

        /// <summary>
        /// Singleton constructor
        /// </summary>
        private DatabaseAuthenticationStrategy()
            : base()
        {

        }
    }
}
