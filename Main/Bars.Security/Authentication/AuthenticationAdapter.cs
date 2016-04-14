using Bars.Security.Authentication.Session;
using Bars.Security.Authentication.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bars.Security.Authentication
{
    /// <summary>
    /// Модуль аутентификафии пользователя
    /// </summary>
    public class AuthenticationAdapter
    {
        /// <summary>
        /// Используемая стратения аутентификации
        /// </summary>
        public static Type UseStrategy { get; set; }

        /// <summary>
        /// Singleton
        /// </summary>
        private sealed class AuthenticationCoreCreator
        {
            // Instance of object
            private static AuthenticationAdapter instance = null;

            /// <summary>
            /// При необходимости создает и возвращает существующий объект
            /// </summary>
            public static AuthenticationAdapter Instance(Type T)
            {
                return instance ?? (instance = typeof(AuthenticationAdapter).
                    GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { T }, null).
                        Invoke(new object[] { 
                                T.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null).Invoke(null)
                            }) as AuthenticationAdapter);
            }
        }

        /// <summary>
        /// Модуль аутентификации
        /// </summary>
        /// <typeparam name="TStrategy">Тип провайдера аутентификации</typeparam>
        /// <returns>Возвращает модуль аутентификации, использующий указанный провайдер</returns>
        public static AuthenticationAdapter Instance { get { return AuthenticationCoreCreator.Instance(UseStrategy); } }

        /// <summary>
        /// Стратегия аутентификации
        /// </summary>
        private readonly IAuthenticationStrategy provider;

        /// <summary>
        /// Аутентифицирует указанного пользователя в системе
        /// </summary>
        /// <param name="AspSessionId">Уникальный идентификатор ASP сессии</param>
        /// <param name="AuthenticateParams">Параметры аутентификации</param>
        /// <returns>Аутентифицированный пользователь</returns>
        public User Authenticate(string AspSessionId, params object[] AuthenticateParams)
        {
            if (Sessions.Instance.Exists(AspSessionId) && Sessions.Instance[AspSessionId].IsAuthenticaed)
                return Sessions.Instance[AspSessionId].User;
            var InitializationParam = provider.ValidateAuthenticationParams(AuthenticateParams);
            var session = Sessions.Instance.Exists(AspSessionId) ?
                Sessions.Instance[AspSessionId] :
                new UserSession(AspSessionId);
            var user = provider.Authenticate(session, InitializationParam);
            Sessions.Instance.RegisterSession(AspSessionId, session);
            return user;
        }

        /// <summary>
        /// Аутентифицирует указанного пользователя в системе
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        /// <param name="Token">Токен сессии пользователя</param>
        /// <returns>Аутентифицированный пользователь</returns>
        public UserSession Authenticate(string Token, UserSession session = null)
        {
            return provider.Authenticate(Token, session);
        }

        /// <summary>
        /// Генерирует токен аутентификации пользователя
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        /// <returns>Токен аутентификации</returns>
        public string GetSessionToken(UserSession session)
        {
            return provider.GetSessionToken(session);
        }

        /// <summary>
        /// Модуль аутентификафии пользователя
        /// </summary>
        /// <param name="AuthenticationStrategy">Стратегия аутентификации пользователя</param>
        private AuthenticationAdapter(IAuthenticationStrategy AuthenticationStrategy)
        {
            if (AuthenticationStrategy == null)
                throw new NullReferenceException("Стратегия футентификации не может иметь значение null.");
            provider = AuthenticationStrategy;
        }

        /// <summary>
        /// Инифиализация компонента
        /// </summary>
        static AuthenticationAdapter()
        {
            UseStrategy = typeof(DatabaseAuthenticationStrategy);
        }
    }
}
