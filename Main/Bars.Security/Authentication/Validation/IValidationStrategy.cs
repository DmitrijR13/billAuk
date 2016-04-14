using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Validation
{
    /// <summary>
    /// Проверяет объект на валидность
    /// </summary>
    internal interface IValidationStrategy
    {
        /// <summary>
        /// Проверяет объект на валидность
        /// </summary>
        /// <param name="ValidObject">Объект для валидации</param>
        /// <returns>NULL, если объект прошел валидацию</returns>
        Exception Validate(object ValidObject);
    }

    /// <summary>
    /// Проверяет объект на валидность
    /// </summary>
    internal interface IValidationStrategy<T> : IValidationStrategy
    {
        /// <summary>
        /// Проверяет объект на валидность
        /// </summary>
        /// <param name="ValidObject">Объект для валидации</param>
        /// <returns>NULL, если объект прошел валидацию</returns>
        Exception Validate(T ValidObject);
    }
}
