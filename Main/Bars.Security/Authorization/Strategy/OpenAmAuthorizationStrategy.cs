using Bars.Security.Authentication.Configuration;
using Bars.Security.Authentication.Session;
using Bars.Security.Authorization.Access;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Bars.Security.Authorization.Strategy
{
    /// <summary>
    /// Стратегия авторизации пользователя
    /// </summary>
    public class OpenAmAuthorizationStrategy : AuthorizationStrategy
    {
        /// <summary>
        /// Стратегия авторизации пользователя
        /// </summary>
        private OpenAmAuthorizationStrategy() :
            base()
        {

        }

        /// <summary>
        /// Авторизует действие пользователя
        /// </summary>
        /// <param name="user">Пользователь, для которого запрашивается авторизация</param>
        /// <param name="obj">Объект, для которого запрашивается авторизация</param>
        /// <param name="action">Действие над запрошенным объектом</param>
        /// <returns>TRUE, если действие над объектом разрешено указанному пользователю</returns>
        public override bool Authorize(User user, AccessibleObject obj, AccessibleAction action)
        {
            var postData = new StringBuilder();
            postData.Append(string.Format("subjectid={0}", user.Session.Token));
            postData.Append(string.Format("&action={0}", action == AccessibleAction.Write ? "POST" : "GET"));
            postData.Append(string.Format("&uri=/Billing/{0}", string.Join("/", GetAccessibleObjectsTree(obj).Select(x => x.Name))));
            var postBytes = (new ASCIIEncoding()).GetBytes(postData.ToString());

            var request = HttpWebRequest.Create(new Uri(OpenAmAuthenticationConfiguration.Instance.ServerUri, "/openam/identity/authorize"));
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            var stream = request.GetRequestStream();
            stream.Write(postBytes, 0, postBytes.Length);
            stream.Flush();
            stream.Close();
            var responce = request.GetResponse();
            using (var res = new StreamReader(responce.GetResponseStream()))
            {
                var data = res.ReadToEnd();
                return data.Replace("\n", "").Split('=')[1] == "true";
            }
            throw new NotImplementedException();
        }
    }
}
