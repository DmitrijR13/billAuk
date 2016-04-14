namespace Bars.KP50.Utils
{
    using System;

    /// <summary>
    /// Аттрибут указывающий что элемент кода необходимо игнорировать
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    public class IgnoreAttribute : CustomValueAttribute
    {
        /// <summary>
        /// Строковый идентификатор контекста, в рамках которого необходимо игнорировать элемент
        /// </summary>
        public string Context { get; protected set; }

        public IgnoreAttribute(string context)
            :base("Ignore", true)
        {
            Context = context;
        }

        public IgnoreAttribute()
            :this(null)
        {
            
        }
    }
}