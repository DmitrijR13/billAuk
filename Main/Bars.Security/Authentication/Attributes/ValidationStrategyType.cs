using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Attributes
{
    /// <summary>
    /// Аттрибут провайдера валидации
    /// </summary>
    internal class ValidationStrategyTypeAttribute : Attribute
    {
        /// <summary>
        /// Стратегия валидации
        /// </summary>
        internal ValidationStrategy Strategy { get; private set; }

        /// <summary>
        /// Аттрибут провайдера валидации
        /// </summary>
        /// <param name="strategy">Стратегия валидации/param>
        internal ValidationStrategyTypeAttribute(ValidationStrategy strategy)
        {
            Strategy = strategy;
        }
    }
}
