namespace Bars.KP50.Utils
{
    using System;

    /// <summary>
    /// Класс исключительной ситуации, которая происходит при отсутствии атрибута в контейнере
    /// </summary>
    public class AttributeNotFoundInContainerException : Exception
    {
        private const string AttributeNotFoundFmt = "Атрибут '{0}' не найден в контейнере типа {1}";

        /// <summary>
        /// Создает новый экземпляр класса <see cref="AttributeNotFoundInContainerException"/>
        /// </summary>
        /// <param name="attributeType">Тип атрибута</param>
        /// <param name="containerType">Тип контейнера</param>
        public AttributeNotFoundInContainerException(Type attributeType, Type containerType)
            :base(AttributeNotFoundFmt.FormatUsing(attributeType.GetTypeUid(), containerType.GetTypeUid()))
        {
            
        }
    }
}