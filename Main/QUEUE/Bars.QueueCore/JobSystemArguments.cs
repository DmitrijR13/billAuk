using System.Security.Permissions;

namespace Bars.QueueCore
{
    using System;
    using System.Collections.Specialized;

    /// <summary>Класс для кправления очередью</summary>
    [Serializable]
    public class JobSystemArguments : MarshalByRefObject
    {
        private NameValueCollection _parameters;

        /// <summary>Идентификатор работы</summary>
        public int JobId { get; set; }

        /// <summary>
        /// Действие над задачей
        /// </summary>
        public JobActions Action { get; set; }

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="action"></param>
        public JobSystemArguments(int jobId, JobActions action)
        {
            JobId = jobId;
            Action = action;
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