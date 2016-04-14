namespace Bars.KP50.Utils
{
    using System;

    /// <summary>
    /// Аттрибут, определяющий идентификатор модуля, расположенный сборке
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class ModuleIdAttribute : Attribute
    {
        /// <summary>
        /// Создает новый экземпляр аттрибута
        /// </summary>
        /// <param name="id"></param>
        public ModuleIdAttribute(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Идентификатор модуля, определенного в сборке
        /// </summary>
        public string Id { get; private set; }
    }
}