using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Exceptions.Authentication
{
    /// <summary>
    /// Исключение, возникающее при неправильных параметрах аутентификации
    /// </summary>
    public class InvalidAuthenticationParamsException : Exception
    {
        /// <summary>
        /// Исключение, возникающее при неправильных параметрах аутентификации
        /// </summary>
        public InvalidAuthenticationParamsException()
            : base()
        {

        }

        /// <summary>
        /// Исключение, возникающее при неправильных параметрах аутентификации
        /// </summary>
        public InvalidAuthenticationParamsException(string Message) :
            base(Message)
        {

        }
    }
}
