using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Bars.Security.Authorization.Access
{
    /// <summary>
    /// Коллекция объектов доступа
    /// </summary>
    public class AccessibleObjectsCollection : IEnumerable<AccessibleObject>
    {
        /// <summary>
        /// Список объектов доступа коллекции
        /// </summary>
        private readonly ReadOnlyCollection<AccessibleObject> _objects;

        /// <summary>
        /// Родительский объект доступа
        /// </summary>
        public AccessibleObject Parent { get; protected internal set; }

        /// <summary>
        /// Возвращает объект доступа с указанным имением, если такой существует в коллекции
        /// </summary>
        /// <param name="Name">Имя объекта доступа</param>
        /// <returns>Объект доступа</returns>
        public AccessibleObject this[string Name] { get { return _objects.FirstOrDefault(x => string.Compare(x.Name, Name, true) == 0); } }

        /// <summary>
        /// Возвращает объект доступа, если такой существует в коллекции
        /// </summary>
        /// <param name="Obj">Искомый объект</param>
        /// <returns>Объект доступа</returns>
        public AccessibleObject this[AccessibleObject Obj] { get { return _objects.FirstOrDefault(x => x.Equals(Obj)); } }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An System.Collections.IEnumerator object that can be used to iterate through the collection.</returns>
        public IEnumerator<AccessibleObject> GetEnumerator()
        {
            return ((IEnumerable)this).GetEnumerator() as IEnumerator<AccessibleObject>;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An System.Collections.IEnumerator object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _objects.GetEnumerator();
        }

        /// <summary>
        /// Коллекция объектов доступа
        /// </summary>
        protected internal AccessibleObjectsCollection(params AccessibleObject[] Objects)
        {
            _objects = new ReadOnlyCollection<AccessibleObject>(Objects);
        }
    }
}
