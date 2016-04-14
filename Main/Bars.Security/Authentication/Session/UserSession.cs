using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bars.Security.Authentication.Session
{
    /// <summary>
    /// Сессия пользователя
    /// </summary>
    public class UserSession : IDisposable
    {
        /// <summary>
        /// Внешний токен аутентификации
        /// </summary>
        protected internal string TokenKey = null;

        #region Session timeout
        /// <summary>
        /// Таймаут активности сессии
        /// </summary>
        public TimeSpan Timeout { get { return TimeSpan.FromMinutes(10); } }

        /// <summary>
        /// Таймаут проверки сессии на паследнее использование
        /// </summary>
        internal TimeSpan ValidationTimeout { get { return TimeSpan.FromMinutes(1); } }

        /// <summary>
        /// Задача отслеживания времени жизни сессии
        /// </summary>
        private Task SessionTask { get; set; }

        /// <summary>
        /// Источник токена отмены задачи
        /// </summary>
        private CancellationTokenSource SessionTaskCancellationToken { get; set; }

        /// <summary>
        /// Убивает сессию по истечению таймаута
        /// </summary>
        /// <param name="Token">Токен отмены задачи</param>
        private void ValidateSessionState(CancellationToken Token)
        {
            while (this != null && !IsClosed)
            {
                Token.ThrowIfCancellationRequested();
                if (DateTime.UtcNow - LastUserActivity < Timeout)
                    Thread.Sleep(ValidationTimeout);
                else Dispose();
            }
            SessionTaskCancellationToken = null;
            SessionTask = null;
        }
        #endregion

        /// <summary>
        /// Закрыта ли сессия
        /// </summary>
        public bool IsClosed { get; internal set; }

        /// <summary>
        /// Аутентифицирован ли пользователь
        /// </summary>
        public bool IsAuthenticaed { get; internal set; }

        /// <summary>
        /// Пользователь в системе
        /// </summary>
        private User _user = null;

        /// <summary>
        /// Уникальный идентификатор пользователя
        /// </summary>
        public long UserId { get; internal set; }

        /// <summary>
        /// Аутентифицированный в системе пользователь
        /// </summary>
        public User User
        {
            get
            {
                LastUserActivity = DateTime.UtcNow;
                return _user ?? (_user = AuthenticationAdapter.Instance.Authenticate(Token, this).User);
            }
        }

        /// <summary>
        /// Токен сессии
        /// </summary>
        public string Token
        {
            get
            {
                LastUserActivity = DateTime.UtcNow;
                return AuthenticationAdapter.Instance.GetSessionToken(this);
            }
        }

        /// <summary>
        /// Время последней кативности пользователя в UTC
        /// </summary>
        public DateTime LastUserActivity { get; internal set; }

        /// <summary>
        /// Impliment of IDisposable
        /// </summary>
        public void Dispose()
        {
            Sessions.Instance.DisposeSession(this);
            _user = null;
            UserId = -1;
            IsClosed = true;
            IsAuthenticaed = false;
        }

        /// <summary>
        /// Сессия пользователя
        /// </summary>
        /// <param name="AspSessionId">Уникальный идентификатор ASP сессии</param>
        protected internal UserSession(string AspSessionId)
        {
            SessionTaskCancellationToken = new CancellationTokenSource();
            SessionTask = new Task(() => ValidateSessionState(SessionTaskCancellationToken.Token), SessionTaskCancellationToken.Token);
            IsClosed = true;
            IsAuthenticaed = false;
            UserId = -1;
        }

        /// <summary>
        /// Сессия пользователя
        /// </summary>
        protected internal UserSession()
        {
            SessionTaskCancellationToken = new CancellationTokenSource();
            SessionTask = new Task(() => ValidateSessionState(SessionTaskCancellationToken.Token), SessionTaskCancellationToken.Token);
            IsClosed = true;
            IsAuthenticaed = false;
            UserId = -1;
        }

        /// <summary>
        /// Fill session
        /// </summary>
        /// <param name="user">Authenticcated user</param>
        internal void Authenticate(User user)
        {
            IsClosed = false;
            IsAuthenticaed = true;
            _user = user;
            UserId = user.UserId;
            LastUserActivity = DateTime.UtcNow;
            if (SessionTask.Status != TaskStatus.Running) SessionTask.Start();
        }

        /// <summary>
        /// Аутентифицирует указанного пользователя в системе
        /// </summary>
        /// <param name="token">Токен сессии пользователя</param>
        /// <returns>Аутентифицированный пользователь</returns>
        public static implicit operator UserSession(string token)
        {
            return AuthenticationAdapter.Instance.Authenticate(token);
        }
    }
}
