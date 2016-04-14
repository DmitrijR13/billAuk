namespace Bars.KP50.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Bars.KP50.Utils.Annotations;

    /// <summary>
    /// Контейнер атрибутов
    /// </summary>
    public class AttributesContainer : ICustomAttributeProvider
    {        
        private IList<Attribute> _attributes;

        /// <summary>
        /// Список атрибутов типа
        /// </summary>
        private readonly IList<Attribute> _typeAttributes = new List<Attribute>();

        /// <summary>
        /// Список атрибутов, размещенных в контейнере
        /// </summary>
        public virtual IList<Attribute> Attributes
        {
            get { return _attributes ?? (_attributes = new List<Attribute>()); }
            set { _attributes = value; }
        }

        /// <summary>
        /// Создает экземпляр класса <see cref="AttributesContainer"/>
        /// </summary>
        public AttributesContainer()
        {
            // считываем атрибуты типа
            GetType()
                .GetAttributes<Attribute>(true)
                .AddTo(_typeAttributes);
        }

        /// <summary>
        /// Получение списка атрибутов, имеющих одинаковый тип <see cref="T"/>, либо являющимися наследниками этого типа
        /// </summary>
        /// <typeparam name="T">Тип атрибута</typeparam>
        /// <returns></returns>
        [NotNull]
        public virtual IEnumerable<T> GetAttributes<T>(bool inherit = true)
            where T : Attribute
        {
            return (this as ICustomAttributeProvider)
                .GetCustomAttributes(typeof (T), inherit)
                .Select(x => x.CastAs<T>());
        }

        /// <summary>
        /// Получение типизированного атрибута.
        /// Попытка получить атрибут производится в два шага:
        /// 1. Проверка коллекции атрибутов <see cref="Attributes"/>
        /// 2. Проверка атрибутов текущего типа и его цепочки иерархии
        /// </summary>
        /// <typeparam name="T">Тип атрибута. Является наследником <see cref="CustomAttribute"/></typeparam>
        /// <param name="throwIfNotExists">
        /// <see cref="bool.True"/>, если необходимо выбросить исключение при отсутствии атрибута с таким типом
        /// <see cref="bool.False"/>, если допускается вернуть null
        /// </param>
        /// <returns>Атрибут заданного типа, либо null если такой атрибут не обнаружен</returns>
        [CanBeNull]
        public virtual T GetAttribute<T>(bool throwIfNotExists = false)
            where T : Attribute
        {
            var attribute = GetAttributes<T>().FirstOrDefault(x => x.GetType() == typeof (T));

            if (attribute == null && throwIfNotExists)
            {
                throw new AttributeNotFoundInContainerException(typeof (T), GetType());
            }

            return attribute.As<T>();
        }

        /// <summary>
        /// Добавление атрибута в контейнер
        /// </summary>
        /// <typeparam name="T">Тип добавляемого атрибута. Является наследником <see cref="CustomAttribute"/></typeparam>
        /// <param name="attribute">Экземпляр атрибута</param>
        /// <param name="throwIfExists">
        /// <see cref="bool.True"/>, если необходимо выбросить исключение при наличии атрибута с таким-же типом
        /// <see cref="bool.False"/>, если необходимо подменить значение атрибута с таким же типом
        /// </param>
        [NotNull]
        public virtual AttributesContainer SetAttribute<T>(T attribute, bool throwIfExists = false)
            where T : Attribute
        {
            var exists = GetAttributes<T>().FirstOrDefault(x => x.GetType() == typeof (T));
            if (exists != null)
            {
                if (throwIfExists)
                {
                    throw new AttributeTypeExistsInContainerException(typeof (T), GetType());
                }

                Attributes.Remove(exists);
            }

            Attributes.Add(attribute);

            return this;
        }

        object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit)
        {
//// ReSharper disable CoVariantArrayConversion
            return this
                .As<ICustomAttributeProvider>()
                .GetCustomAttributes(true)
                .Where(x => x.GetType().Return(t => inherit ? t.Is(attributeType) : t == attributeType))
                .ToArray();
//// ReSharper restore CoVariantArrayConversion
        }

        object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit)
        {
//// ReSharper disable CoVariantArrayConversion
            return Attributes.Concat(_typeAttributes).ToArray();
//// ReSharper restore CoVariantArrayConversion
        }

        bool ICustomAttributeProvider.IsDefined(Type attributeType, bool inherit)
        {
            return this
                .As<ICustomAttributeProvider>()
                .GetCustomAttributes(inherit)
                .Any(x => x.GetType().Return(t => inherit ? t.Is(attributeType) : t == attributeType));
        }
    }
}
