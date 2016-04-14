using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Attributes
{
    /// <summary>
    /// Используемая стратегия валидации
    /// </summary>
    [Flags]
    internal enum ValidationStrategy
    {
        /// <summary>
        /// Не выполнять валидацию
        /// </summary>
        None = 0,

        /// <summary>
        /// Валидация логина
        /// </summary>
        Login = 1,

        /// <summary>
        /// Валидация имени
        /// </summary>
        User = 1 << 1,

        /// <summary>
        /// Валидация E-Mail
        /// </summary>
        EMail = 1 << 2,

        /// <summary>
        /// Валидация пароля
        /// </summary>
        Password = 1 << 3
    }

    /// <summary>
    /// Аттрибут валидации 
    /// </summary>
    internal class ValidateOnRegisterAttribute : Attribute
    {
        /// <summary>
        /// Стратегия валидации поля
        /// </summary>
        public ValidationStrategy Strategy { get; private set; }

        /// <summary>
        /// Аттрибут валидации 
        /// </summary>
        /// <param name="strategy">Стратегия валидации поля</param>
        public ValidateOnRegisterAttribute(ValidationStrategy strategy)
        {
            Strategy = strategy;
        }
    }
}
