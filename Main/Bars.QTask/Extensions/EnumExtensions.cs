using Bars.QTask.Tasks;
using STCLINE.KP50.Global;
using System;
using System.ComponentModel;
using System.Linq;

namespace Bars.QTask.Tasks
{
    /// <summary>
    /// Расширения enum
    /// </summary>
    public static class EnumExtentions
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
                    GetCustomAttributes(typeof(DescriptionAttribute), false).
                    SingleOrDefault() as DescriptionAttribute;
                return attribute.Description;
            }
            catch (Exception ex) { MonitorLog.WriteException("Can't load description of type " + enumerationValue, ex); }
            return string.Empty;
        }
    }
}
