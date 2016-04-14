using Bars.Security.Authorization.Configuration;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bars.Security.Authorization.Access
{
    /// <summary>
    /// Перечислитель объектов доступа и действий над ними в системе
    /// </summary>
    public class AccessibleRoles : DataBaseHead
    {
        /// <summary>
        /// Объекты доступа в системе
        /// </summary>
        public AccessibleObjectsCollection Objects { get; private set; }

        /// <summary>
        /// Singleton
        /// </summary>
        private sealed class RolesCreator
        {
            /// <summary>
            /// Сзоданный экземпляр объекта
            /// </summary>
            private static AccessibleRoles instance = null;

            /// <summary>
            /// При необходимости создает и возвращает созданный объект
            /// Вызывает приватный конструктор без параметров
            /// </summary>
            public static AccessibleRoles Instance
            {
                get
                {
                    return instance ?? (instance =
                        typeof(AccessibleRoles).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                        Type.EmptyTypes, null).Invoke(null) as AccessibleRoles);
                }
            }
        }

        /// <summary>
        /// Возвращает экземпляр объекта
        /// </summary>
        public static AccessibleRoles Instance { get { return RolesCreator.Instance; } }

        /// <summary>
        /// Singleton constructor
        /// </summary>
        private AccessibleRoles()
        {
            LoadSystemObjects();
        }

        /// <summary>
        /// Загружает объекты в систему
        /// </summary>
        private void LoadSystemObjects(AccessibleObject BaseObject = null)
        {
            var ParentObjectId = (BaseObject == null) ? "0" : BaseObject.AccessibleObjectId.ToString();
            var connection = GetConnection(Constants.cons_Kernel);
            if (!DBManager.OpenDb(connection, true).result)
            {
                throw new Exception("Ошибка при открытии соединения в процедуре " +
                                    System.Reflection.MethodBase.GetCurrentMethod().Name);
            }
            
            IDataReader reader = null;
            var r = ExecRead(connection, out reader,
                string.Format("SELECT AccessibleObjectId, Name, DisplayName FROM {0} WHERE ParentObjectId = {1}",
                    DatabaseAuthorizationConfiguration.Instance.AccessibleObjects, ParentObjectId), false);
            var lst = new List<AccessibleObject>();
            while (reader.Read())
            {
                lst.Add(new AccessibleObject()
                {
                    AccessibleObjectId = (Int32)reader["AccessibleObjectId"],
                    DisplayName = reader["DisplayName"].ToString().TrimEnd(),
                    Name = reader["Name"].ToString().TrimEnd(),
                    Parent = BaseObject
                });
            }
            reader.Dispose();
            if (lst.Count > 0)
            {
                if (BaseObject == null) Objects = new AccessibleObjectsCollection(lst.ToArray());
                else BaseObject.Objects = new AccessibleObjectsCollection(lst.ToArray());
                foreach (var obj in lst) LoadSystemObjects(obj);
            }
        }
    }
}
