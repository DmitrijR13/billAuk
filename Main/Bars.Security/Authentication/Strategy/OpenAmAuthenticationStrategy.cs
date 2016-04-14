using Bars.Security.Authentication.Configuration;
using Bars.Security.Authentication.Parameters;
using Bars.Security.Authentication.Session;
using Bars.Security.Exceptions.Authentication;
using Bars.Security.Security.Authentication;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;

namespace Bars.Security.Authentication.Strategy
{
    /// <summary>
    /// Стратегия аутентификации через OpenAm
    /// </summary>
    public class OpenAmAuthenticationStrategy : AuthenticationStrategy
    {
        /// <summary>
        /// Singleton constructor
        /// </summary>
        private OpenAmAuthenticationStrategy()
            : base()
        {

        }

        /// <summary>
        /// Проверяет входные параметры для провайдера аутентификации
        /// </summary>
        /// <param name="AuthenticationParams">Параметры аутентификации</param>
        /// <returns>Параметр для инициализации провайдера</returns>
        public override object ValidateAuthenticationParams(params object[] AuthenticationParams)
        {
            var AuthenticationParam = new OpenAmAuthenticationParam();
            var Token = (AuthenticationParams.FirstOrDefault(x => x is HttpCookieCollection) as HttpCookieCollection)
                [OpenAmAuthenticationConfiguration.Instance.CookieName].Value;
            var Headers = AuthenticationParams.FirstOrDefault(x => x is NameValueCollection) as NameValueCollection;
            if (Headers == null || string.IsNullOrWhiteSpace(Token)) throw new InvalidAuthenticationParamsException();
            var properties = typeof(OpenAmAuthenticationParam).
                GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).
                Select(item => item.Name).ToArray();
            foreach (var property in properties)
            {
                if (string.IsNullOrWhiteSpace(Headers[property]))
                    throw new InvalidAuthenticationParamsException();
                typeof(OpenAmAuthenticationParam).
                    GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty).
                    SetValue(AuthenticationParam, Headers[property], null);
            }
            AuthenticationParam.OpenAmToken = Token;
            return AuthenticationParam;
        }

        /// <summary>
        /// Аутентифицирует пользователя в системе
        /// </summary>
        /// <param name="AuthenticateParams">Параметры аутентификации провайдера</param>
        /// <returns>Сессия и данные аутентифицированного пользователя</returns>
        public override User Authenticate(UserSession session, object AuthenticateParam)
        {
            User user = null;
            var prm = AuthenticateParam as OpenAmAuthenticationParam;
            if (prm == null) throw new InvalidAuthenticationParamsException();
            if ((user = GetUserData(session, string.Format("Login = '{0}'", prm.Login))) == null)
                user = RegisterUser(new User() { Login = prm.Login, Password = Password.Empty });
            session.TokenKey = prm.OpenAmToken;
            return user;
        }

        /// <summary>
        /// Генерирует токен аутентификации пользователя
        /// </summary>
        /// <param name="session">Сессия пользователя</param>
        /// <returns>Токен аутентификации</returns>
        public override string GetSessionToken(UserSession session)
        {
            return session.TokenKey;
        }

        /// <summary>
        /// Проверяет токен на валидность
        /// </summary>
        /// <param name="Token">Токен пользователя</param>
        /// <returns>Уникальный идентификатор пользователя</returns>
        protected override long ValidateToken(string Token)
        {
            string ResponseData = null;
            var request = WebRequest.Create(
                new Uri(OpenAmAuthenticationConfiguration.Instance.ServerUri,
                    string.Format("/openam/identity/attributes?subjectid={0}&attributenames=uid", Token)));
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent = "Bars Group user agent";
            var response = request.GetResponse();
            using (var sResponce = new StreamReader(response.GetResponseStream()))
            {
                ResponseData = sResponce.ReadToEnd();
                response.Close();
            }

            string AttributeKey = null;
            string Login = string.Empty;
            foreach (var attribute in ResponseData.Split('\n').
                Where(x => x.Contains("userdetails.attribute")))
            {
                Login = string.Empty;
                var attr = attribute.Split('=');
                switch (attr[0])
                {
                    case "userdetails.attribute.name":
                        AttributeKey = attr[1];
                        break;
                    case "userdetails.attribute.value":
                        Login = attr[1];
                        break;
                }
                if (AttributeKey == "uid" && !string.IsNullOrWhiteSpace(Login)) break;
            }
            return GetUserData(new UserSession() {TokenKey = Token}, string.Format("Login = '{0}'", Login)).UserId;
        }
    }
}
