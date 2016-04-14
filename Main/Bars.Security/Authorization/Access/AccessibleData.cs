using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authorization.Access
{
    public enum AccessibleDataStrategy
    {
        Between = 1,
        In = 2
    }

    /// <summary>
    /// Ограничение по доступу к данным
    /// </summary>
    public class AccessibleData
    {
        /// <summary>
        /// Уникальный идентификатор
        /// </summary>
        protected internal int AccessibleDataId { get; set; }

        /// <summary>
        /// Имя поля, по которому производится ограничение
        /// </summary>
        public string FieldName { get; protected internal set; }

        /// <summary>
        /// Имя ограничения
        /// </summary>
        public string Name { get; protected internal set; }

        /// <summary>
        /// Стратегия ограничения по данным
        /// </summary>
        public AccessibleDataStrategy Strategy { get; protected internal set; }

        /// <summary>
        /// Значения диапазона
        /// </summary>
        public IEnumerable<string> Range { get; protected internal set; }

        /// <summary>
        /// Ограничение по доступу к данным
        /// </summary>
        /// <param name="FieldName">Имя поля таблицы, по которому строится ограничение</param>
        /// <param name="strategy">Стратегия ограничения</param>
        /// <param name="values">Значения диапазона</param>
        protected internal AccessibleData(string Name, string FieldName, AccessibleDataStrategy strategy, params string[] values)
        {
            this.Name = Name;
            this.FieldName = FieldName;
            this.Strategy = strategy;
            this.Range = values.AsEnumerable();
        }

        public override string ToString()
        {
            switch (Strategy)
            {
                case AccessibleDataStrategy.Between:
                    return string.Format("{0} BETWEEN {1} AND {2}", FieldName, Range.First(), Range.Last());
                case AccessibleDataStrategy.In:
                    return string.Format("{0} IN ({1})", FieldName, string.Join(", ", Range));
            }
            throw new InvalidOperationException();
        }
    }
}
