namespace Bars.KP50.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;

    using Bars.KP50.Utils.Annotations;

    public static class TypeExtensions
    {
        public const string InvalidMultiPartNameChars = " +-*/^[]{}!\"\\%&()=?";
        public const string InvalidMemberNameChars = "." + InvalidMultiPartNameChars;

        public static string GetTypeUid(this Type type)
        {
            return "{0}.{1}, {2}".FormatUsing(type.Namespace, GetTypeName(type), type.Assembly.GetName().Name);
        }

        public static string GetTypeName(this Type type)
        {
            var names = new List<string>();
            while (type != null)
            {
                names.Add(type.Name);
                type = type.DeclaringType;
            }

            names.Reverse();

            return string.Join(".", names.ToArray());
        }

        public static IEnumerable<PropertyInfo> Properties(this Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return type.GetProperties(flags);
        }

        public static bool Is(this Type type, Type baseType, bool checkInheritance = true)
        {
            ArgumentChecker.NotNull(baseType, "baseType");

            if (type == baseType || baseType.IsAssignableFrom(type))
                return true;

            var isInterface = (baseType.IsInterface && type.GetInterface(baseType.FullName, true) != null);
            
            return checkInheritance ? isInterface || type.IsSubclassOf(baseType) : isInterface;
        }

        public static bool Is<TInterface>(this Type type, bool checkInheritance = true)
        {
            var interfaceType = typeof (TInterface);

            return type.Is(interfaceType, checkInheritance);
        }
        
        public static bool IsNot<T>(this Type type, bool checkInheritance = true)
        {
            return !type.Is<T>(checkInheritance);
        }

        public static bool IsGeneric<T>(this Type type)
        {
            var generics = type.GetGenericArguments();

            return type.IsGenericType && generics.Length == 1 && generics[0].Is<T>();
        }

        public static bool IsGeneric<T1, T2>(this Type type)
        {
            var generic = type;
            while (true)
            {
                if (generic == null)
                {
                    break;
                }

                if (generic.IsGenericType)
                {
                    break;
                }

                generic = generic.BaseType;
            }

            if (generic == null)
            {
                return false;
            }

            if (generic.Is<T1>())
            {
                var generics = generic.GetGenericArguments();
                return generics.Length == 1 && generics[0].Is<T2>();
            }

            return false;
        }

        public static bool IsEnumerable(this Type type)
        {
            return Is<IEnumerable>(type);
        }

        public static bool IsEnumerable<T>(this Type type)
        {
            return Is<IEnumerable<T>>(type);
        }

        public static bool IsList(this Type type)
        {
            return type.Is<IList>() || type.Name.StartsWithIgnoreCase("IList", "List");
        }

        public static bool IsCollection(this Type type)
        {
            return type.Is<ICollection>() || type.Name.StartsWithIgnoreCase("ICollection", "Collection") || type.IsList();
        }

        public static bool IsDictionary(this Type type)
        {
            return type.Is<IDictionary>() || type.Name.StartsWithIgnoreCase("IDictionary", "Dictionary");
        }

        public static bool IsNullable(this Type type)
        {
            ArgumentChecker.NotNull(type, "type");
            
            if (!type.IsValueType)
            {
                return false;
            }

            var generic = type.IsGenericType ? type.GetGenericTypeDefinition() : null;            

            return generic != null && generic == typeof(Nullable<>);            
        }

        public static int InheritanceLevel(this Type type)
        {
            if (type == null)
            {
                return Int32.MaxValue;
            }

            if (type.BaseType == null)
            {
                return 0;
            }

            return type.BaseType.InheritanceLevel() + 1;
        }

        public static T[] GetAttributes<T>(this ICustomAttributeProvider type, bool inherit)
            where T : Attribute
        {
            return type.Is<AttributesContainer>()
                       ? type.As<AttributesContainer>().GetAttributes<T>(inherit).ToArray()
                       : type.GetCustomAttributes(typeof (T), inherit).OfType<T>().ToArray();
        }

        public static T GetAttribute<T>(this ICustomAttributeProvider type, bool inherit)
            where T : Attribute
        {
            return type.GetAttributes<T>(inherit).FirstOrDefault();
        }

        public static string GetDisplayName(this ICustomAttributeProvider type, string defaultValue = "")
        {
            var result = type
                .GetAttribute<DisplayAttribute>(true)
                .Return(x => (string)x.Value);

            if (result.IsEmpty())
            {
                result = type
                .GetAttribute<DisplayNameAttribute>(true)
                .Return(x => x.DisplayName);
            }

            return result.Or(defaultValue);
        }

        public static bool HasAttribute<T>(this ICustomAttributeProvider type, bool inherit) 
            where T : Attribute
        {
            return GetAttributes<T>(type, inherit).IsNotEmpty();
        }

        public static bool HasNoAttribute<T>(this ICustomAttributeProvider type, bool inherit) 
            where T : Attribute
        {
            return !HasAttribute<T>(type, inherit);
        }
    }
}
