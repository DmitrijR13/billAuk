namespace Bars.KP50.Utils
{
    using System;

    /// <summary>
    /// Базовый класс для создания дополнительных атрибутов
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]     
    public class CustomAttribute : Attribute
    {
        /// <summary>
        /// Идентификатор атрибута
        /// </summary>
        public string Uid
        {
            get { return GetType().GetTypeUid(); }
        }

        /// <summary>
        /// Идентификатор типа атрибута
        /// </summary>
        public override object TypeId
        {
            get { return Uid; }
        }
    }
}