using Bars.Security.Authentication.Session;
using Bars.Security.Authorization.Access;
using Bars.Security.Authorization.Configuration;
using STCLINE.KP50.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Script.Serialization;

namespace Bars.Security.Authorization.Strategy
{
    /// <summary>
    /// Стратегия авторизации пользователя
    /// </summary>
    public abstract class AuthorizationStrategy : DataBaseHead, IAuthorizationStrategy
    {
        /// <summary>
        /// Авторизует действие пользователя
        /// </summary>
        /// <param name="user">Пользователь, для которого запрашивается авторизация</param>
        /// <param name="obj">Объект, для которого запрашивается авторизация</param>
        /// <param name="action">Действие над запрошенным объектом</param>
        /// <returns>TRUE, если действие над объектом разрешено указанному пользователю</returns>
        public abstract bool Authorize(User user, AccessibleObject obj, AccessibleAction action);

        /// <summary>
        /// Возвращает права, ограничивающие доступ к данным
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="obj">Объект доступа, к которому запрашиваются ограничения по данным</param>
        /// <returns>Ограничения по данным</returns>
        public virtual IEnumerable<AccessibleData> GetDataRestriction(User user, AccessibleObject obj)
        {
            var configuration = DatabaseAuthorizationConfiguration.Instance;
            var Query =
                " SELECT data.AccessibleDataId, data.AccessibleDataName, data.AccessibleDataField, " +
                "   data.AccessibleDataStrategy, data.AccessibleDataRange " +
                " FROM " + configuration.UserGroups + " groups " +
                " INNER JOIN " + configuration.AccessibleGroupsItems + " groupitems ON (groupitems.AccessibleGroupId = groups.AccessibleGroupId) " +
                " INNER JOIN " + configuration.GroupsAccesibleData + " groupdata ON (groupdata.AccessibleGroupItemsId = groupitems.AccessibleGroupItemsId) " +
                " INNER JOIN " + configuration.AccessibleData + " data ON (data.AccessibleDataId = groupdata.AccessibleDataId AND " +
                "   data.AccessibleDataId NOT IN ( " +
                "     SELECT userdata.AccessibleDataId " +
                "     FROM " + configuration.UsersAccesibleData + " userdata " +
                "     INNER JOIN " + configuration.UserAccess + " access ON (access.UserAccessId = userdata.UserAccessId) " +
                "     WHERE access.UserId = groups.UserId AND access.AccessibleObjectId = groupitems.AccessibleObjectId)) " +
                " WHERE groups.UserId = " + user.UserId + " AND groupitems.AccessibleObjectId = " + obj.AccessibleObjectId + " " +
                " UNION SELECT data.AccessibleDataId, data.AccessibleDataName, data.AccessibleDataField, " +
                "   data.AccessibleDataStrategy, data.AccessibleDataRange " +
                " FROM " + configuration.UsersAccesibleData + " userdata " +
                " INNER JOIN " + configuration.UserAccess + " access ON (access.UserAccessId = userdata.UserAccessId) " +
                " INNER JOIN " + configuration.AccessibleData + " data ON (data.AccessibleDataId = userdata.AccessibleDataId) " +
                " WHERE access.UserId = " + user.UserId + " AND access.AccessibleObjectId = " + obj.AccessibleObjectId + " ";

            var connection = GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
            if (!OpenDb(connection, true).result)
                throw new Exception("Ошибка при открытии соединения в процедуре " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name);
            IDataReader reader = null;
            ExecRead(connection, out reader, Query, false);
            if (!OpenDb(connection, true).result)
                throw new Exception("Ошибка при открытии соединения в процедуре " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name);
            while (reader.Read())
            {
                yield return new AccessibleData(
                    reader["AccessibleDataName"].ToString().TrimEnd(),
                    reader["AccessibleDataField"].ToString().TrimEnd(),
                    ((AccessibleDataStrategy)Convert.ToInt32(reader["AccessibleDataStrategy"])),
                    (new JavaScriptSerializer()).Deserialize<List<string>>(reader["AccessibleDataRange"].ToString().TrimEnd()).ToArray()) 
                    { AccessibleDataId = Convert.ToInt32(reader["AccessibleDataId"]) };
            }
        }

        /// <summary>
        /// Возвращает полный список родительских объектов доступа
        /// </summary>
        /// <param name="obj">Объект доступа</param>
        /// <returns>Список родительских объектов доступа</returns>
        protected IEnumerable<AccessibleObject> GetAccessibleObjectsTree(AccessibleObject obj)
        {
            var aobj = obj;
            var uri = new List<AccessibleObject>();
            do { uri.Add(aobj); aobj = aobj.Parent; }
            while (aobj != null);
            uri.Reverse();
            return uri.AsReadOnly();
        }

        /// <summary>
        /// Строит дерево из списка объектов доступа
        /// </summary>
        /// <param name="rules">Список объектов доступа</param>
        /// <param name="obj">Коренной объект доступа</param>
        /// <returns>Дерево объектов доступа</returns>
        protected AccessibleObjectsCollection BuildRulesTree(IEnumerable<KeyValuePair<int, AccessibleObject>> rules, AccessibleObject obj = null)
        {
            var objId = (obj == null) ? 0 : obj.AccessibleObjectId;
            var collection =
                 new AccessibleObjectsCollection(rules.Where(x => x.Key == objId).Select(x => x.Value).ToArray());
            foreach (var item in collection)
            {
                item.Parent = obj;
                var tree = BuildRulesTree(rules, item);
                if (tree.Any()) item.Objects = tree;
            }
            return collection;
        }

        /// <summary>
        /// Возвращает список объектов доступа пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns>Дерево объектов доступа</returns>
        protected AccessibleObjectsCollection GetUserRolesTree(User user)
        {
            var configuration = DatabaseAuthorizationConfiguration.Instance;
            var Query =
                " SELECT objects.AccessibleObjectId, objects.ParentObjectId, objects.Name, " +
                "   objects.DisplayName, BIT_OR(groupitems.Actions) AS Actions " +
                " FROM " + configuration.UserGroups + " groups " +
                " LEFT OUTER JOIN " + configuration.UserAccess + " access ON (access.UserId = groups.UserId) " +
                " INNER JOIN " + configuration.AccessibleGroupsItems + " groupitems ON ( " +
                "   groupitems.AccessibleGroupId = groups.AccessibleGroupId AND " +
                "   groupitems.AccessibleObjectId NOT IN (SELECT AccessibleObjectId FROM " + configuration.UserAccess + " WHERE UserId = groups.UserId)) " +
                " INNER JOIN " + configuration.AccessibleObjects + " objects ON (objects.AccessibleObjectId = groupitems.AccessibleObjectId) " +
                " WHERE groups.UserId = " + user.UserId + " " +
                " GROUP BY objects.AccessibleObjectId, objects.ParentObjectId, objects.Name, objects.DisplayName " +
                " UNION SELECT objects.AccessibleObjectId, objects.ParentObjectId, objects.Name, " +
                "   objects.DisplayName, access.Actions " +
                " FROM " + configuration.UserAccess + " access " +
                " INNER JOIN " + configuration.AccessibleObjects + " objects ON (objects.AccessibleObjectId = access.AccessibleObjectId) " +
                " WHERE access.UserId = " + user.UserId + " " +
                " ORDER BY AccessibleObjectId, ParentObjectId ";

            var objTree = new List<KeyValuePair<int, AccessibleObject>>();
            var connection = GetConnection(STCLINE.KP50.Global.Constants.cons_Kernel);
            if (!OpenDb(connection, true).result)
                throw new Exception("Ошибка при открытии соединения в процедуре " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name);
            IDataReader reader = null;
            ExecRead(connection, out reader, Query, false);
            while (reader.Read())
            {
                objTree.Add(new KeyValuePair<int, AccessibleObject>(
                    Convert.ToInt32(reader["ParentObjectId"]),
                    new AccessibleObject()
                    {
                        AccessibleObjectId = Convert.ToInt32(reader["AccessibleObjectId"]),
                        Actions = (AccessibleAction)Convert.ToInt32(reader["Actions"]),
                        DisplayName = reader["DisplayName"].ToString().TrimEnd(),
                        Name = reader["Name"].ToString().TrimEnd()
                    }));
            }

            return user.Roles = new AccessibleObjectsCollection(BuildRulesTree(objTree).ToArray());
        }

        /// <summary>
        /// Singleton constructor
        /// </summary>
        protected AuthorizationStrategy() :
            base()
        {

        }
    }
}
