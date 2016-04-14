using Bars.Security.Authentication.Attributes;
using Bars.Security.Security.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Validation.Providers
{
    /// <summary>
    /// Валидирует пароль пользователя
    /// </summary>
    [ValidationStrategyType(ValidationStrategy.Password)]
    internal class PassworValidationProvider : IValidationStrategy<Password>
    {
        /// <summary>
        /// Валидирует пароль пользователя
        /// </summary>
        /// <param name="ValidObject">Объект вылидации</param>
        /// <returns>NULL, если валидация успешно пройдена</returns>
        public Exception Validate(Password ValidObject)
        {
            if (ValidObject == null) return new NullReferenceException("Пароль не может иметь значение NULL");
            if (ValidObject.ToString() != Password.Empty.ToString()) return new FormatException("Пароль не может быть пустым");
            return null;
        }

        /// <summary>
        /// Валидирует пароль пользователя
        /// </summary>
        /// <param name="ValidObject">Объект вылидации</param>
        /// <returns>NULL, если валидация успешно пройдена</returns>
        public Exception Validate(object ValidObject)
        {
            return Validate(ValidObject as Password);
        }
    }
}
