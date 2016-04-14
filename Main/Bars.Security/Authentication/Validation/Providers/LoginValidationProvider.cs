using Bars.Security.Authentication.Attributes;
using Bars.Security.Authentication.Configuration;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Validation.Providers
{
    /// <summary>
    /// Валидирует логин пользователя
    /// </summary>
    [ValidationStrategyType(ValidationStrategy.Login)]
    internal class LoginValidationProvider : DataBaseHead, IValidationStrategy<string>
    {
        /// <summary>
        /// Валидирует логин пользователя
        /// </summary>
        /// <param name="ValidObject">Объект вылидации</param>
        /// <returns>NULL, если валидация успешно пройдена</returns>
        public Exception Validate(string ValidObject)
        {
            var t = new Returns(true);
            var conn = GetConnection(Constants.cons_Kernel);
            if (!DBManager.OpenDb(conn, true).result)
            {
                throw new Exception("Ошибка при открытии соединения в процедуре " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name);
            }
            var count = (long)ExecScalar(conn,
                string.Format("SELECT COUNT(*) FROM {0} WHERE UPPER({1}) = '{2}'",
                    DatabaseAuthenticationConfiguration.Instance.DbTable, "Login", ValidObject.ToUpper()),
                out t, false);
            return (count > 0) ? new Exception("Пользователь с таким логином уже существует") : null;
        }

        /// <summary>
        /// Валидирует логин пользователя
        /// </summary>
        /// <param name="ValidObject">Объект вылидации</param>
        /// <returns>NULL, если валидация успешно пройдена</returns>
        public Exception Validate(object ValidObject)
        {
            return Validate(ValidObject as string);
        }
    }
}
