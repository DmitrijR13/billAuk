using Bars.Security.Authentication.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bars.Security.Authentication.Validation.Providers
{
    /// <summary>
    /// Стратегия валидации E-Mail
    /// </summary>
    [ValidationStrategyType(ValidationStrategy.EMail)]
    internal class EMailValidationProvider : IValidationStrategy<string>
    {
        /// <summary>
        /// Проверяет E-Mail
        /// </summary>
        /// <param name="ValidObject">E-Mail пользователя</param>
        /// <returns>NULL в случае успеха</returns>
        public Exception Validate(string ValidObject)
        {
            if (ValidObject == null) return null;
            var regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            var match = regex.Match(ValidObject);
            return (match.Success) ? null : new FormatException("E-Mail не прошел проверну на валидность.");
        }

        /// <summary>
        /// Проверяет E-Mail
        /// </summary>
        /// <param name="ValidObject">E-Mail пользователя</param>
        /// <returns>NULL в случае успеха</returns>
        public Exception Validate(object ValidObject)
        {
            return Validate(ValidObject as string);
        }
    }
}
