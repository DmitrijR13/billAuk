using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Bars.Security.Authorization.Access
{
    /// <summary>
    /// Коллекция ограничений по доступу к данным объекта доступа
    /// </summary>
    public class AccessibleDataCollection : IEnumerable<AccessibleData>
    {
        /// <summary>
        /// Список ограничений по доступу к данным
        /// </summary>
        private ReadOnlyCollection<AccessibleData> _objects;

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A System.Collections.Generic.IEnumerator<T> that can be used to iterate through the collection.</returns>
        public IEnumerator<AccessibleData> GetEnumerator()
        {
            return ((IEnumerable)this).GetEnumerator() as IEnumerator<AccessibleData>;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An System.Collections.IEnumerator object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _objects.GetEnumerator();
        }

        /// <summary>
        /// Коллекция ограничений по доступу к данным объекта доступа
        /// </summary>
        /// <param name="Objects">Список ограничений по доступу к данным</param>
        protected internal AccessibleDataCollection(params AccessibleData[] Objects)
        {
            _objects = new ReadOnlyCollection<AccessibleData>(Objects);
        }
    }
}
