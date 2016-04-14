using Bars.Security.Authentication.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Strategy
{
    /// <summary>
    /// Стратегия аутентификации пользователя
    /// </summary>
    public interface IAuthenticationStrategy
    {
        /// <summary>
        /// Проверяет входные параметры для провайдера аутентификации
        /// </summary>
        /// <param name="AuthenticationParams">Параметры аутентификации</param>
        /// <returns>Параметр для инициализации провайдера</returns>
        object ValidateAuthenticationParams(params object[] AuthenticationParams);

        /// <summary>
        /// Аутентифицирует пользователя в системе
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        /// <param name="AuthenticateParams">Параметры аутентификации провайдера</param>
        /// <returns>Сессия и данные аутентифицированного пользователя</returns>
        User Authenticate(UserSession session, object AuthenticateParam);

        /// <summary>
        /// Аутентифицирует пользователя в системе
        /// </summary>
        /// <param name="Token">Токен аутентификации пользователя</param>
        /// <returns>Сессия и данные аутентифицированного пользователя</returns>
        UserSession Authenticate(string Token, UserSession session = null);

        /// <summary>
        /// Завершает текущую сессию пользователя
        /// </summary>
        void Logout(UserSession session);

        /// <summary>
        /// Генерирует токен аутентификации пользователя
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        /// <returns>Токен аутентификации/returns>
        string GetSessionToken(UserSession session);

        /// <summary>
        /// Регистрирует нового пользователя в системе
        /// </summary>
        /// <param name="user">Пользователь системы</param>
        /// <returns>Зарегистрированный пользователь</returns>
        User RegisterUser(User user);
    }
}
