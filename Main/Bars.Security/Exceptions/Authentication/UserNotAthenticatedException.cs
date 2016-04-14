using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Exceptions.Authentication
{
    /// <summary>
    /// Исключение, возникающее, если пользователь не прошел аутентификацию или его сессия истекла
    /// </summary>
    public class UserNotAthenticatedException : Exception
    {

    }
}
