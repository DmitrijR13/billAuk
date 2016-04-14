using Bars.Security.Authentication.Attributes;
using Bars.Security.Authentication.Configuration;
using Bars.Security.Authentication.Session;
using Bars.Security.Authentication.Validation;
using Bars.Security.Exceptions.Authentication;
using Bars.Security.Security.Authentication;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using User = Bars.Security.Authentication.Session.User;

namespace Bars.Security.Authentication.Strategy
{
    /// <summary>
    /// Стратегия аутентификации пользователя
    /// </summary>
    public abstract class AuthenticationStrategy : DataBaseHead, IAuthenticationStrategy
    {
        /// <summary>
        /// Проверяет входные параметры для провайдера аутентификации
        /// </summary>
        /// <param name="AuthenticationParams">Параметры аутентификации</param>
        /// <returns>Параметр для инициализации провайдера</returns>
        public abstract object ValidateAuthenticationParams(params object[] AuthenticationParams);

        /// <summary>
        /// Аутентифицирует пользователя в системе
        /// </summary>
        /// <param name="session">Шоп рещарпэр ни ругалсо</param>
        /// <param name="AuthenticateParams">Параметры аутентификации провайдера</param>
        /// <returns>Сессия и данные аутентифицированного пользователя</returns>
        public abstract User Authenticate(UserSession session, object AuthenticateParam);

        /// <summary>
        /// Аутентифицирует пользователя в системе
        /// </summary>
        /// <param name="Token">Токен аунетификации пользователя</param>
        /// <returns>Сессия и данные аутентифицированного пользователя</returns>
        public virtual UserSession Authenticate(string Token, UserSession session = null)
        {
            GetUserData(session ?? (session = new UserSession() {TokenKey = Token}), string.Format("UserId = {0}", ValidateToken(Token)));
            return session;
        }

        /// <summary>
        /// Завершает текущую сессию пользователя
        /// </summary>
        /// <param name="session">Сессия пользователя для завершения</param>
        public void Logout(UserSession session)
        {
            session.Dispose();
        }

        /// <summary>
        /// Singleton constructor
        /// </summary>
        protected AuthenticationStrategy() { }

        /// <summary>
        /// Проверяет токен на валидность
        /// </summary>
        /// <param name="token">Токен пользователя</param>
        /// <returns>Уникальный идентификатор пользователя</returns>
        protected virtual long ValidateToken(string token) { return Token.ValidateToken(token); }

        /// <summary>
        /// Возвращает массив полей для заполнения
        /// </summary>
        /// <returns>Массив полей для заполнения</returns>
        protected virtual string[] GetUserColumns()
        {
            var cols = typeof(User).GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance).Select(x => x.Name);
            return cols.Where(x => x != "Session" && x != "Roles").ToArray();
        }

        /// <summary>
        /// Находит пользователя по заданному ксловию
        /// Генеирует DataMisalignedException, если подзапрос вернул более 1 пользователя
        /// Возвразает NULL, если пользователь не существует
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        /// <param name="WhereExpression">Условие поиска</param>
        /// <returns>Аутентифицированный пользователь</returns>
        protected virtual User GetUserData(UserSession session, string WhereExpression)
        {
            User user = null;
            var columns = GetUserColumns();
            var Query = string.Format(
                "SELECT {0} FROM {1} WHERE {2}",
                string.Join(", ", columns), DatabaseAuthenticationConfiguration.Instance.DbTable, WhereExpression);

            IDataReader result = null;
            var conn = GetConnection(Constants.cons_Kernel);
            if (!OpenDb(conn, true).result)
            {
                throw new DataException("Ошибка при открытии соединения в процедуре " +
                                    MethodBase.GetCurrentMethod().Name);
            }
            ExecRead(conn, out result, Query, false);

            using (result)
            {
                while (result.Read())
                {
                    var values = new object[columns.Count()];
                    result.GetValues(values);
                    if (user != null)
                        throw new DataMisalignedException("СУБД вернула больше 1 пользователя.");
                    user = new User();
                    foreach (var propery in columns)
                    {
                        var propertyinfo = typeof(User).GetProperty(propery, BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance);
                        var value = (values[Array.IndexOf(columns, propery)] is DBNull) ? null : values[Array.IndexOf(columns, propery)];

                        if (propertyinfo.PropertyType == typeof(Password))
                            propertyinfo.SetValue(user, new Password((value != null) ? value.ToString().TrimEnd() : null), null);
                        else if (propertyinfo.PropertyType == typeof(string))
                            propertyinfo.SetValue(user, (value != null) ? value.ToString().TrimEnd() : null, null);
                        else
                            propertyinfo.SetValue(user, value, null);
                    }
                    session.Authenticate(user);
                    user.Session = session;
                }
                result.Close();
            }
            return user;
        }

        /// <summary>
        /// Регистрирует нового пользователя в системе
        /// </summary>
        /// <param name="user">Пользователь системы</param>
        /// <returns>Зарегистрированный пользователь</returns>
        public virtual User RegisterUser(User user)
        {
            var identity = user.GetType().
                GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty)
                .First(x => x.Name != "Session" && 
                    x.GetCustomAttributes(typeof (IgnoreOnRegisterAttribute), false).Any());
            var properties = user.GetType().
                GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).
                Where(x => !x.GetCustomAttributes(typeof(IgnoreOnRegisterAttribute), false).Any());
            var exceptions = properties.Select(property =>
                ValidationProcessor.ValidateObject(
                    property.GetCustomAttributes(typeof (ValidateOnRegisterAttribute), false)
                        .Select(x => (x as ValidateOnRegisterAttribute).Strategy)
                        .FirstOrDefault(), property.GetValue(user, null))).Where(exc => exc != null).ToList();
            if (exceptions.Any())
                throw new AggregateException("В процессе выполнения проверок возникли ошибки", exceptions);

            var connection = GetConnection(Constants.cons_Kernel);
            var sheet = new Returns();
            var UserId = (int)ExecScalar(connection,
                string.Format("INSERT INTO {0} ({1}) VALUES ({2}) RETURNING {3}",
                    DatabaseAuthenticationConfiguration.Instance.DbTable,
                    string.Join(", ", properties.Select(x => x.Name).ToArray()),
                    string.Join(", ", properties.Select(x => (x.GetValue(user, null) != null) ? "'" + x.GetValue(user, null).ToString() + "'" : "NULL")),
                    identity.Name), out sheet, false);
            if (UserId <= 0) throw new UserNotAthenticatedException();
            user.UserId = UserId;
            return user;
        }

        /// <summary>
        /// Возвращает токен пользователя
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        /// <returns>Токен пользователя</returns>
        public virtual string GetSessionToken(UserSession session)
        {
            return Token.GetToken(session);
        }
    }
}
