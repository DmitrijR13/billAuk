using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authorization.Access
{
    /// <summary>
    /// Объект доступа пользователя
    /// </summary>
    public class AccessibleObject
    {
        /// <summary>
        /// Уникальный идентификатор объекта доступа
        /// </summary>
        public int AccessibleObjectId { get; protected internal set; }

        /// <summary>
        /// Родительский объект доступа
        /// </summary>
        public AccessibleObject Parent { get; protected internal set; }

        /// <summary>
        /// Имя объекта доступа
        /// </summary>
        public string Name { get; protected internal set; }

        /// <summary>
        /// Имя объекта для отображения пользователю
        /// </summary>
        public string DisplayName { get; protected internal set; }

        /// <summary>
        /// Дочерние объекты доступа
        /// </summary>
        public AccessibleObjectsCollection Objects { get; protected internal set; }

        /// <summary>
        /// Доступные действия над объектом
        /// </summary>
        public AccessibleAction Actions { get; protected internal set; }

        /// <summary>
        /// Determines whether the specified System.Object is equal to the current System.Object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified System.Object is equal to the current System.Object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            base.Equals(obj);
            return (obj != null &&
                obj is AccessibleObject &&
                AccessibleObjectId == (obj as AccessibleObject).AccessibleObjectId &&
                Name == (obj as AccessibleObject).Name &&
                DisplayName == (obj as AccessibleObject).DisplayName);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current System.Object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Объект доступа пользователя
        /// </summary>
        protected internal AccessibleObject()
        {
            
        }
    }
}
