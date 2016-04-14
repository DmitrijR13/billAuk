using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bars.Security.Authentication.Session
{
    /// <summary>
    /// Сессии текущих пользователей системы
    /// </summary>
    public class Sessions
    {
        #region Singleton
        /// <summary>
        /// Singleton
        /// </summary>
        private sealed class SessionsCreator
        {
            /// <summary>
            /// Instance of object
            /// </summary>
            private static Sessions _instance = null;

            /// <summary>
            /// Создает или возвращает существующий объект
            /// </summary>
            public static Sessions Instance
            {
                get
                {
                    return _instance ?? (_instance = (Sessions)typeof(Sessions).
                        GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                        null, Type.EmptyTypes, null).Invoke(null));
                }
            }
        }

        /// <summary>
        /// Возвращает экземпляр созданного объекта
        /// </summary>
        public static Sessions Instance { get { return SessionsCreator.Instance; } }

        /// <summary>
        /// Базоный конструктор объекта
        /// </summary>
        private Sessions()
        {
            UserSessions = new Dictionary<string, UserSession>();
        }
        #endregion

        /// <summary>
        /// Словарь сессий пользователей
        /// </summary>
        private readonly Dictionary<string, UserSession> UserSessions;

        /// <summary>
        /// Возвращает сессию указанного пользователя
        /// </summary>
        /// <param name="AspSessionID">Уникальный идентификатор ASP сессии</param>
        /// <returns>Сессия пользователя или NULL, если ее не существует</returns>
        public UserSession this[string AspSessionID]
        {
            get
            {
                UserSession session = null;
                return (UserSessions.TryGetValue(AspSessionID, out session)) ? session : null;
            }
        }

        /// <summary>
        /// Проверяет существует ли сессия этого пользователя
        /// </summary>
        /// <param name="AspSessionID">Уникальный идентификатор ASP сессии</param>
        public bool Exists(string AspSessionID)
        {
            return UserSessions.ContainsKey(AspSessionID);
        }

        /// <summary>
        /// Регистрирует сессию в системе
        /// </summary>
        /// <param name="AspSessionId">Уникальный идентификатор ASP сессии</param>
        /// <param name="session">Сессия пользователя</param>
        internal void RegisterSession(string AspSessionId, UserSession session)
        {
            if (!Exists(AspSessionId))
                UserSessions.Add(AspSessionId, session);
        }

        /// <summary>
        /// Удаляет сессию пользователя
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        internal void DisposeSession(UserSession session)
        {
            if (UserSessions.ContainsValue(session))
                UserSessions.Remove(UserSessions.First(x => x.Value == session).Key);
        }
    }
}
