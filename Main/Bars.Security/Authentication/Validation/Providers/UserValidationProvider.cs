using Bars.Security.Authentication.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Validation.Providers
{
    /// <summary>
    /// Валидирует имя пользователя
    /// </summary>
    [ValidationStrategyType(ValidationStrategy.User)]
    internal class UserValidationProvider : IValidationStrategy<string>
    {
        /// <summary>
        /// Валидирует имя пользователя
        /// </summary>
        /// <param name="ValidObject">Объект вылидации</param>
        /// <returns>NULL, если валидация успешно пройдена</returns>
        public Exception Validate(string ValidObject)
        {
            return null;
        }

        /// <summary>
        /// Валидирует имя пользователя
        /// </summary>
        /// <param name="ValidObject">Объект вылидации</param>
        /// <returns>NULL, если валидация успешно пройдена</returns>
        public Exception Validate(object ValidObject)
        {
            return Validate(ValidObject as string);
        }
    }
}
