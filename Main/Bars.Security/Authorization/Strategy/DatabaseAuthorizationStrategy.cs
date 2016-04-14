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
    public class DatabaseAuthorizationStrategy : AuthorizationStrategy
    {
        /// <summary>
        /// Стратегия авторизации пользователя
        /// </summary>
        private DatabaseAuthorizationStrategy() :
            base()
        {

        }

        /// <summary>
        /// Авторизует действие пользователя
        /// </summary>
        /// <param name="user">Пользователь, для которого запрашивается авторизация</param>
        /// <param name="obj">Объект, для которого запрашивается авторизация</param>
        /// <param name="action">Действие над запрошенным объектом</param>
        /// <returns>TRUE, если действие над объектом разрешено указанному пользователю</returns>
        public override bool Authorize(User user, AccessibleObject obj, AccessibleAction action)
        {
            AccessibleObjectsCollection validate = user.Roles ?? GetUserRolesTree(user);
            var tree = GetAccessibleObjectsTree(obj);
            foreach (var role in tree)
            {
                if (validate[role] != null &&
                    validate[role].Actions.HasFlag(role == obj ? action : AccessibleAction.Execute))
                {
                    validate = validate[role].Objects;
                    if (role.Equals(obj)) return true;
                    continue;
                }
                return false;
            }
            return false;
        }
    }
}
