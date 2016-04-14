namespace Bars.KP50.Utils.Annotations
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;

    public static class ArgumentChecker
    {
        private const string CSStreamIsEmpty = "Stream is empty.";
        private const string CSStreamIsEof = "Stream at the end.";
        private const string CSStreamIsNotReadable = "Stream is not readable.";
        private const string CSStreamIsNotWriteable = "Stream is not writeable.";

        private const string CSStringDoesNotMatchValidationExpression =
            "String is invalid and does not match validation expression.";

        private const string CSTypeValueCannotBeAbstract = "Type '{0}' is abstract. Abstract types is not allowed here.";
        private const string CSTypeValueIsNotGenericTypeDefinition = "Type '{0}' is not an generic type definition.";
        private const string CSTypeValueShouldBeAnInterface = "Type value should be an interface.";

        private const string CSTypeValueShouldInheritFrom =
            "Type '{0}' should inherit from '{1}'. Types with another base types is not allowed.";

        private const string CSTypeValueIsNotGenericTypeDefinitionAndNotInterface =
            "Type '{0}' is not an generic interface definition.";

        private const string CSTypeValueIsNotInterface =
    "Type '{0}' is not interface.";

        private const string CSValueCantBeNull = "Value can't be null.";
        private const string CSValueCantBeNullOrEmpty = "Value can't be null or empty string.";
        private const string CSValueCantBeWhitespace = "Value can't be whitespace string.";
        private const string CSValueIsNotSubclassOf = "Value is not an subclass of {0} type.";

        public static void IsType(object value, Type type, [InvokerParameterName] string parameterName)
        {
            NotNull(value, parameterName);
            NotNull(type, "type");

            var valueType = value is Type ? value as Type : value.GetType();

            if (!valueType.Is(type))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format("{0} must be of <{1}> type", parameterName, type.FullName));
            }
        }

        public static void IsType<TType>(object value, [InvokerParameterName] string parameterName)
        {
            IsType(value, typeof(TType), parameterName);
        }

        public static void NotAbstract(Type type, [InvokerParameterName] string parameterName)
        {
            NotNull(type, parameterName);
            if (type.IsAbstract)
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format("Type {0} of parameter {1} is abstract",
                                                                       type.FullName, parameterName));
            }
        }

        public static void IsEnum(Type type, [InvokerParameterName] string parameterName)
        {
            NotNull(type, parameterName);
            if (!type.IsEnum)
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format("Type of {0} is not Enumeration", parameterName));
            }
        }

        public static void NotNullAndSubclassOf(Type typeValue, Type baseType, string message,
                                                [InvokerParameterName] string parameterName)
        {
            if (typeValue == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(message, parameterName);
            }
            if (!typeValue.Is(baseType))
            {
                ExceptionHelper.Throw<ArgumentException>(message, parameterName);
            }
        }

        public static void NotNullAndSubclassOf(Type typeValue, Type baseType, [InvokerParameterName] string parameterName)
        {
            if (typeValue == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNull, parameterName);
            }
            if (!typeValue.Is(baseType))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        public static void NotNullAndSubclassOf(object instance, Type baseType, [InvokerParameterName] string parameterName)
        {
            if (instance == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNull, parameterName);
            }
            Type type = instance.GetType();
            if (!type.Is(baseType))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2208")]
        public static void NotNullAndSubclassOfGeneric(object instance, Type baseType,
                                                       [InvokerParameterName] string parameterName)
        {
            if (instance == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNull, parameterName);
            }
            Type type = instance.GetType();
            if (!isBaseGenericType(type, baseType))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2208")]
        public static void NotNullAndSubclassOfGeneric(Type instanceType, Type baseType,
                                                       [InvokerParameterName] string parameterName)
        {
            if (instanceType == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNull, parameterName);
            }
            Type type = instanceType;
            if (!isBaseGenericType(type, baseType))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        public static void NotNullAndImplementsGenericInterface(object instance, Type baseType,
                                                                [InvokerParameterName] string parameterName)
        {
            if (instance == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNull, parameterName);
            }
            Type type = instance.GetType();
            if (!isBaseGenericInterface(type, baseType))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        public static void NotNullAndImplementsGenericInterface(Type instanceType, Type baseType,
                                                                [InvokerParameterName] string parameterName)
        {
            if (instanceType == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNull, parameterName);
            }
            Type type = instanceType;
            if (!isBaseGenericInterface(type, baseType))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture, CSValueIsNotSubclassOf, baseType.FullName), parameterName);
            }
        }

        private static bool isBaseGenericType(Type type, Type baseType)
        {
            Type objectType = typeof(Object);
            while (type != objectType && type != null)
            {
                if (type == baseType)
                {
                    return (true);
                }
                if (baseType.IsGenericTypeDefinition && type.IsGenericType)
                {
                    Type definition = type.GetGenericTypeDefinition();
                    if (definition == baseType)
                    {
                        return (true);
                    }
                }
                if (type.Module == baseType.Module && (type.Namespace + type.Name) == (baseType.Namespace + baseType.Name) &&
                    (type.IsGenericType && baseType.IsGenericTypeDefinition))
                {
                    return (true);
                }
                type = type.BaseType;
            }
            return (false);
        }

        private static bool isBaseGenericInterface(Type type, Type baseType)
        {
            if (baseType.IsAssignableFrom(type))
            {
                return (true);
            }
            Type objectType = typeof(Object);
            while (type != objectType)
            {
                if (type == baseType)
                {
                    return (true);
                }
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (isBaseGenericType(interfaceType, baseType))
                    {
                        return (true);
                    }
                }
                if (type.Module == baseType.Module && (type.Namespace + type.Name) == (baseType.Namespace + baseType.Name) &&
                    (type.IsGenericType && baseType.IsGenericTypeDefinition))
                {
                    return (true);
                }
                type = type.BaseType;
            }
            return (false);
        }

        public static void NotNull(Object value, string message, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(message, parameterName);
            }
        }

        public static void IsBetween(int value, int left, int right, [InvokerParameterName] string parameterName)
        {
            if (value < left || value > right)
            {
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value <{0}> must be between <{1}> and <{2}>", value,
                                                                   left, right);
            }
        }

        public static void IsBetween(double value, double left, double right, [InvokerParameterName] string parameterName)
        {
            if (value < left || value > right)
            {
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value <{0}> must be between <{1}> and <{2}>", value,
                                                                   left, right);
            }
        }

        public static void IsBetween(decimal value, decimal left, decimal right, [InvokerParameterName] string parameterName)
        {
            if (value < left || value > right)
            {
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value <{0}> must be between <{1}> and <{2}>", value,
                                                                   left, right);
            }
        }

        public static void IsBetween(float value, float left, float right, [InvokerParameterName] string parameterName)
        {
            if (value < left || value > right)
            {
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value <{0}> must be between <{1}> and <{2}>", value,
                                                                   left, right);
            }
        }

        public static void NotLessThanZero(int value, [InvokerParameterName] string parameterName)
        {
            if (value < 0)
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of <{0}> cannot be less that zero.", parameterName);
        }

        public static void NotLessThanZeroOrZero(int value, [InvokerParameterName] string parameterName)
        {
            if (value <= 0)
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of <{0}> cannot be less that zero or zero.", parameterName);
        }

        public static void NotLessThanZero(long value, [InvokerParameterName] string parameterName)
        {
            if (value < 0L)
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of <{0}> cannot be less that zero.", parameterName);
        }

        public static void NotLessThanZeroOrZero(long value, [InvokerParameterName] string parameterName)
        {
            if (value <= 0L)
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of <{0}> cannot be less that zero or zero.", parameterName);
        }

        public static void NotLessThanZero(float value, [InvokerParameterName] string parameterName)
        {
            if (value < 0f)
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of <{0}> cannot be less that zero.", parameterName);
        }

        public static void NotLessThanZeroOrZero(float value, [InvokerParameterName] string parameterName)
        {
            if (value <= 0f)
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of <{0}> cannot be less that zero or zero.", parameterName);
        }

        public static void NotLessThanZero(double value, [InvokerParameterName] string parameterName)
        {
            if (value < 0f)
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of <{0}> cannot be less that zero.", parameterName);
        }

        public static void NotLessThanZeroOrZero(double value, [InvokerParameterName] string parameterName)
        {
            if (value <= 0f)
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of <{0}> cannot be less that zero or zero.", parameterName);
        }

        public static void InCollection(IDictionary collection, object value, [InvokerParameterName] string parameterName)
        {
            if (!collection.Contains(value))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format("Value <{0}> not in collection", value), parameterName);
            }
        }

        public static void InCollection(IEnumerable collection, object value, [InvokerParameterName] string parameterName)
        {
            var item = collection.OfType<object>().FirstOrDefault(o => o == value);
            if (item == null)
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format("Value <{0}> not in collection", value), parameterName);
            }
        }

        public static void NotInCollection(IDictionary collection, object value, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                return;
            }

            if (collection.Contains(value))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format("Value <{0}> already in collection", value), parameterName);
            }
        }

        public static void NotInCollection(IEnumerable collection, object value, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                return;
            }

            var item = collection.OfType<object>().FirstOrDefault(o => o == value);
            if (item != null)
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format("Value <{0}> already in collection", value), parameterName);
            }
        }

        [AssertionMethod]
        public static void NotNull([AssertionCondition(AssertionConditionType.IS_NOT_NULL)]Object value, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(parameterName, CSValueCantBeNull);
            }
        }

        public static void NotNull<T>(Object value, Expression<Func<T>> parameterExpression)
        {
            if (value == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(((MemberExpression)parameterExpression.Body).Member.Name, CSValueCantBeNull);
            }
        }

        public static void NotNullOrEmpty(String value, string message, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(message, parameterName);
            }
            if (value.Length == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(message, parameterName);
            }
        }

        public static void NotNullOrEmptyAndMatches(String value, string regex, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (value.Length == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (!Regex.IsMatch(value, regex))
            {
                ExceptionHelper.Throw<ArgumentException>(CSStringDoesNotMatchValidationExpression, parameterName);
            }
        }

        public static void NotNullOrEmptyAndMatches(String value, Regex regex, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (value.Length == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (!regex.IsMatch(value))
            {
                ExceptionHelper.Throw<ArgumentException>(CSStringDoesNotMatchValidationExpression, parameterName);
            }
        }

        public static void NotNullOrEmpty(String value, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (value.Length == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(CSValueCantBeNullOrEmpty, parameterName);
            }
        }

        public static void NotNullOrEmptyOrWhitespace(String value, [InvokerParameterName] string parameterName)
        {
            if (value == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNullOrEmpty, parameterName);
            }
            if (value.Length == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(CSValueCantBeNullOrEmpty, parameterName);
            }
            value = value.Trim();
            if (value.Length == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(CSValueCantBeWhitespace, parameterName);
            }
        }

        public static void NotNullAndLengthNotLessThan(Array array, int minimumLength, string message,
                                                       [InvokerParameterName] string parameterName)
        {
            if (array == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(message, parameterName);
            }
            if (array.Length < minimumLength)
            {
                ExceptionHelper.Throw<ArgumentException>(message, parameterName);
            }
        }

        public static void NotNullOrEmpty(Array collection, string message, [InvokerParameterName] string parameterName)
        {
            if (collection == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(message, parameterName);
            }

            if (collection.Cast<object>().Count() == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(message, parameterName);
            }
        }

        public static void NotNullOrEmpty(IEnumerable collection, [InvokerParameterName] string parameterName)
        {
            if (collection == null)
            {
                ExceptionHelper.Throw<ArgumentNullException>(CSValueCantBeNullOrEmpty, parameterName);
            }

            var isEmpty = !collection.Cast<object>().Any();

            if (isEmpty)
            {
                ExceptionHelper.Throw<ArgumentException>(CSValueCantBeNullOrEmpty, parameterName);
            }
        }

        public static void ValidEnumerationValue<TType>(TType value, [InvokerParameterName] string parameterName)
            where TType : struct
        {
            if (!Enum.IsDefined(value.GetType(), value))
            {
                ExceptionHelper.Throw<ArgumentOutOfRangeException>("Value of enumeration <{0}> is out of range.", parameterName);
            }
        }

        public static void NotNullWritableStream(Stream document, [InvokerParameterName] string parameterName)
        {
            NotNull(document, parameterName);
            if (!document.CanWrite)
            {
                ExceptionHelper.Throw<ArgumentException>(CSStreamIsNotWriteable, parameterName);
            }
        }

        public static void NotNullReadableNotEmptyStream(Stream document, [InvokerParameterName] string parameterName)
        {
            NotNull(document, parameterName);
            if (!document.CanRead)
            {
                ExceptionHelper.Throw<ArgumentException>(CSStreamIsNotReadable, parameterName);
            }
            if (document.Length == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(CSStreamIsEmpty, parameterName);
            }
            if ((document.Length - document.Position) == 0)
            {
                ExceptionHelper.Throw<ArgumentException>(CSStreamIsEof, parameterName);
            }
        }

        public static void NotNullInterface(Type type, [InvokerParameterName] string parameterName)
        {
            NotNull(type, parameterName);
            if (!type.IsInterface)
            {
                ExceptionHelper.Throw<ArgumentException>(CSTypeValueShouldBeAnInterface, parameterName);
            }
        }

        public static void NotNullInterface(Type type, string message, [InvokerParameterName] string parameterName)
        {
            NotNull(type, message, parameterName);
            if (!type.IsInterface)
            {
                ExceptionHelper.Throw<ArgumentException>(message, parameterName);
            }
        }

        public static void NotNullInterfaceAssignableTo(Type type, Type assignableTo,
                                                        [InvokerParameterName] string parameterName)
        {
            NotNull(type, parameterName);
            if (!type.IsInterface)
            {
                ExceptionHelper.Throw<ArgumentException>(CSTypeValueShouldBeAnInterface, parameterName);
            }
            if (!assignableTo.IsAssignableFrom(type))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture,
                                                          CSTypeValueShouldInheritFrom, type, assignableTo), parameterName);
            }
        }

        public static void NotNullAssignableTo(Object instance, Type assignableTo, [InvokerParameterName] string parameterName)
        {
            NotNull(instance, parameterName);
            Type type = instance.GetType();
            if (!assignableTo.IsAssignableFrom(type))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture,
                                                          CSTypeValueShouldInheritFrom, type, assignableTo), parameterName);
            }
        }

        public static void NotNullAssignableTo(Type instanceType, Type assignableTo,
                                               [InvokerParameterName] string parameterName)
        {
            NotNull(instanceType, parameterName);
            Type type = instanceType;
            if (!assignableTo.IsAssignableFrom(type))
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture,
                                                          CSTypeValueShouldInheritFrom, type, assignableTo), parameterName);
            }
        }

        public static void NotNullInterfaceAssignableTo(Type type, Type assignableFrom, string message,
                                                        [InvokerParameterName] string parameterName)
        {
            NotNull(type, message, parameterName);
            if (!type.IsInterface)
            {
                ExceptionHelper.Throw<ArgumentException>(message, parameterName);
            }
            if (!assignableFrom.IsAssignableFrom(type))
            {
                ExceptionHelper.Throw<ArgumentException>(message, parameterName);
            }
        }

        public static void NotNullNonAbstract(Type type, [InvokerParameterName] string parameterName)
        {
            NotNull(type, parameterName);
            if (type.IsAbstract)
            {
                ExceptionHelper.Throw<ArgumentException>(string.Format(CultureInfo.InvariantCulture, CSTypeValueCannotBeAbstract,
                                                          type), parameterName);
            }
        }

        public static void NotNullGenericTypeDefinition(Type type, [InvokerParameterName] string parameterName)
        {
            NotNull(type, "type");
            if (!type.IsGenericTypeDefinition)
            {
                ExceptionHelper.Throw<ArgumentException>(
                    string.Format(CultureInfo.InvariantCulture, CSTypeValueIsNotGenericTypeDefinition, type), parameterName);
            }
        }

        public static void NotNullGenericInterfaceDefinition(Type type, [InvokerParameterName] string parameterName)
        {
            NotNull(type, "type");
            if (!type.IsGenericTypeDefinition || !type.IsInterface)
            {
                ExceptionHelper.Throw<ArgumentException>(
                    string.Format(CultureInfo.InvariantCulture, CSTypeValueIsNotGenericTypeDefinitionAndNotInterface, type), parameterName);
            }
        }

        public static void NotNullAndIsInterface(Type type, [InvokerParameterName] string parameterName)
        {
            NotNull(type, parameterName);
            if (!type.IsInterface)
            {
                ExceptionHelper.Throw<ArgumentException>(
                    string.Format(CultureInfo.InvariantCulture, CSTypeValueIsNotInterface, type), parameterName);
            }
        }

        public static void NotEquals(object value, object anotherValue, [InvokerParameterName] string parameterName)            
        {
            NotNull(value, parameterName);

            if (value == anotherValue || value.Equals(anotherValue))
            {
                ExceptionHelper.Throw<ArgumentException>(
                    string.Format(CultureInfo.InvariantCulture, "Parameter {0} must not be equal to {1}", parameterName,
                                  anotherValue));
            }
        }
    }
}