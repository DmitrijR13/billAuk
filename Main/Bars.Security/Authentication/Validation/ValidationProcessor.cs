using Bars.Security.Authentication.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bars.Security.Authentication.Validation
{
    /// <summary>
    /// Валидирует объект, используя указанную стратегию
    /// </summary>
    internal static class ValidationProcessor
    {
        /// <summary>
        /// Валидирует объект, используя указанную стратегию
        /// </summary>
        /// <param name="strategy">Стратегия валидации</param>
        /// <param name="ValidObject">Объект валидации</param>
        /// <returns>NULL, если валидация прошла успешно</returns>
        internal static Exception ValidateObject(ValidationStrategy strategy, object ValidObject)
        {
            var Strategy = Assembly.GetExecutingAssembly().GetTypes().
                Where(x => x.GetCustomAttributes(typeof(ValidationStrategyTypeAttribute), false).
                    Where(y => (y as ValidationStrategyTypeAttribute).Strategy == strategy).Count() > 0).FirstOrDefault();
            if (Strategy != null)
                return (Strategy.GetConstructor(BindingFlags.Instance | BindingFlags.CreateInstance | BindingFlags.Public, null, Type.EmptyTypes, null).
                    Invoke(null) as IValidationStrategy).Validate(ValidObject);
            return null;
        }
    }
}
