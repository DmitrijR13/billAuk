using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Exceptions.Security
{
    /// <summary>
    /// Исключение, возникающее, если токен аутентификации не прошел валидацию
    /// </summary>
    public class InvalidTokenException : Exception
    {
        /// <summary>
        /// Исключение, возникающее, если токен аутентификации не прошел валидацию
        /// </summary>
        public InvalidTokenException() :
            base()
        {

        }

        /// <summary>
        /// Исключение, возникающее, если токен аутентификации не прошел валидацию
        /// </summary>
        public InvalidTokenException(string Message) :
            base(Message)
        {

        }

        /// <summary>
        /// Исключение, возникающее, если токен аутентификации не прошел валидацию
        /// </summary>
        public InvalidTokenException(string Message, Exception InnerException) :
            base(Message, InnerException)
        {

        }
    }
}
