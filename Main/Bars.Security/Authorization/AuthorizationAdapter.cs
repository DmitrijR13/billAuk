using Bars.Security.Authentication.Session;
using Bars.Security.Authorization.Access;
using Bars.Security.Authorization.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bars.Security.Authorization
{
    /// <summary>
    /// Модуль авторизации пользователя
    /// </summary>
    public class AuthorizationAdapter
    {
        /// <summary>
        /// Используемая стратения авторизации
        /// </summary>
        public static Type UseStrategy { get; set; }

        /// <summary>
        /// Singleton
        /// </summary>
        private sealed class AuthenticationCoreCreator
        {
            /// <summary>
            /// Instance of object
            /// </summary>
            private static AuthorizationAdapter instance = null;

            /// <summary>
            /// При необходимости создает и возвращает существующий объект
            /// </summary>
            public static AuthorizationAdapter Instance(Type T)
            {
                return instance ?? (instance = typeof(AuthorizationAdapter).
                    GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { T }, null).
                        Invoke(new object[] { 
                                T.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null).Invoke(null)
                            }) as AuthorizationAdapter);
            }
        }

        /// <summary>
        /// Модуль авторизации
        /// </summary>
        /// <typeparam name="TStrategy">Тип провайдера аутентификации</typeparam>
        /// <returns>Возвращает модуль аутентификации, использующий указанный провайдер</returns>
        public static AuthorizationAdapter Instance { get { return AuthenticationCoreCreator.Instance(UseStrategy); } }

        /// <summary>
        /// Стратегия аутентификации
        /// </summary>
        private readonly IAuthorizationStrategy provider;

        /// <summary>
        /// Модуль аутентификафии пользователя
        /// </summary>
        /// <param name="AuthorizationStrategy">Стратегия аутентификации пользователя</param>
        private AuthorizationAdapter(IAuthorizationStrategy AuthorizationStrategy)
        {
            if (AuthorizationStrategy == null)
                throw new NullReferenceException("Стратегия футентификации не может иметь значение null.");
            provider = AuthorizationStrategy;
        }

        /// <summary>
        /// Инифиализация компонента
        /// </summary>
        static AuthorizationAdapter()
        {
            UseStrategy = typeof(DatabaseAuthorizationStrategy);
        }

        /// <summary>
        /// Авторизует действие пользователя
        /// </summary>
        /// <param name="user">Пользователь, для которого запрашивается авторизация</param>
        /// <param name="obj">Объект, для которого запрашивается авторизация</param>
        /// <param name="action">Действие над запрошенным объектом</param>
        /// <returns>TRUE, если действие над объектом разрешено указанному пользователю</returns>
        public bool Authorize(User user, AccessibleObject obj, AccessibleAction action)
        {
            return provider.Authorize(user, obj, action);
        }

        /// <summary>
        /// Возвращает права, ограничивающие доступ к данным
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="obj">Объект доступа, к которому запрашиваются ограничения по данным</param>
        /// <returns>Ограничения по данным</returns>
        public IEnumerable<AccessibleData> GetDataRestriction(User user, AccessibleObject obj)
        {
            return provider.GetDataRestriction(user, obj);
        }
    }
}
