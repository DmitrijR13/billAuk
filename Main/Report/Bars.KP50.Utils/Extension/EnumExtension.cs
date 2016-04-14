using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.KP50.Utils
{
    /// <summary>
    /// Расширения enum
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Возвращает значение аттрибута Description
        /// </summary>
        /// <typeparam name="T">enum type</typeparam>
        /// <param name="enumerationValue">enum value</param>
        /// <returns>Значение аттрибута Description</returns>
        public static string GetDescription<T>(this T enumerationValue)
            where T : struct
        {
            try
            {
                if (!enumerationValue.GetType().IsEnum) throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
                var attribute = typeof(T).GetMember(enumerationValue.ToString()).SingleOrDefault().
                    GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).
                    SingleOrDefault() as System.ComponentModel.DescriptionAttribute;
                return attribute.Description;
            }
            catch { return string.Empty; }
        }
    }
}
