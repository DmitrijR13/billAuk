namespace Bars.KP50.Utils
{
    using System;

    /// <summary>
    /// Атрибут, указывающий наименование модуля, который необходимо загрузить перед модулем в текущей сборке
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = true, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        /// <summary>
        /// Создает новый экземпляр аттрибута
        /// </summary>
        /// <param name="dependsOn"></param>
        public DependsOnAttribute(string dependsOn)
            :base()
        {
            DependsOn = dependsOn;
        }

        /// <summary>
        /// Идентификатор модуля, от которого зависит модуль, определенный в сборке
        /// </summary>
        public string DependsOn { get; private set; }
    }
}