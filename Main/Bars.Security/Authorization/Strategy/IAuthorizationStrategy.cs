using Bars.Security.Authentication.Session;
using Bars.Security.Authorization.Access;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authorization.Strategy
{
    /// <summary>
    /// Стратегия авторизации пользователя
    /// </summary>
    public interface IAuthorizationStrategy
    {
        /// <summary>
        /// Авторизует действие пользователя
        /// </summary>
        /// <param name="user">Пользователь, для которого запрашивается авторизация</param>
        /// <param name="obj">Объект, для которого запрашивается авторизация</param>
        /// <param name="action">Действие над запрошенным объектом</param>
        /// <returns>TRUE, если действие над объектом разрешено указанному пользователю</returns>
        bool Authorize(User user, AccessibleObject obj, AccessibleAction action);

        /// <summary>
        /// Возвращает права, ограничивающие доступ к данным
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="obj">Объект доступа, к которому запрашиваются ограничения по данным</param>
        /// <returns>Ограничения по данным</returns>
        IEnumerable<AccessibleData> GetDataRestriction(User user, AccessibleObject obj);
    }
}
