namespace Bars.KP50.Utils
{
    using System;

    /// <summary>
    /// Класс исключительной ситуации, которая происходит при попытке добавить повторяющийся тип атрибута в контейнер
    /// </summary>
    public class AttributeTypeExistsInContainerException : Exception
    {
        private const string AttributeTypeExistsFmt = "Атрибут '{0}' уже присутствует в контейнере типа {1}";

        public AttributeTypeExistsInContainerException(Type attributeType, Type containerType)
            :base(AttributeTypeExistsFmt.FormatUsing(attributeType.GetTypeUid(), containerType.GetTypeUid()))
        {
            
        }
    }
}