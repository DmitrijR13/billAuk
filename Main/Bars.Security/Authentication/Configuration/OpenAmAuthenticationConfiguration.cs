using Bars.Security.Extentions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authentication.Configuration
{
    /// <summary>
    /// Конфигурация параметров OpenAm для аутентификации
    /// </summary>
    public class OpenAmAuthenticationConfiguration : Singleton<OpenAmAuthenticationConfiguration>
    {
        /// <summary>
        /// Uri сервера аутентификации
        /// </summary>
        public Uri ServerUri { get; set; }

        /// <summary>
        /// Имя cookie OpenAm
        /// </summary>
        public string CookieName { get; set; }

        /// <summary>
        /// Конфигурация параметров OpenAm для аутентификации
        /// </summary>
        private OpenAmAuthenticationConfiguration()
        {
            ServerUri = new Uri("http://192.168.228.7:8080/openam");
            CookieName = "iPlanetDirectoryPro";
        }
    }
}
