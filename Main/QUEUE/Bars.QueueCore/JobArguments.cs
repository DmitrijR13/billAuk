using System.Security.Permissions;

namespace Bars.QueueCore
{
    using System;
    using System.Collections.Specialized;

    /// <summary>Параметры запуска работы</summary>
    [Serializable]
    public class JobArguments : MarshalByRefObject
    {
        private NameValueCollection _parameters;

        /// <summary>Код работы</summary>
        public string Code { get; set; }

        /// <summary>Имя работы</summary>
        public string Name { get; set; }

        /// <summary>Идентификатор работы</summary>
        public int JobId { get; set; }

        /// <summary>Общие параметры вызова</summary>
        public NameValueCollection Parameters
        {
            get { return _parameters ?? (_parameters = new NameValueCollection()); }            
        }

        /// <summary>
        /// Для разрешения проблемы "object has been disconnected or does not exist at the server".
        /// Источник - http://stackoverflow.com/questions/5275839/inter-appdomain-communication-problem
        /// </summary>
        /// <returns></returns>
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}