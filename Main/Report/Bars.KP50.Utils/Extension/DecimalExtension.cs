// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DecimalExtension.cs" company="">
//   
// </copyright>
// <summary>
//   The decimal extension.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Bars.KP50.Utils
{
    using System.Globalization;

    /// <summary>
    /// The decimal extension.
    /// </summary>
    public static class DecimalExtension
    {
        /// <summary>
        /// Возвращает целую часть числа
        /// </summary>
        /// <param name="value">
        /// Число 
        /// </param>
        /// <returns>
        /// Целая часть числа 
        /// </returns>
        public static decimal IntegerPart(this decimal value)
        {
            return decimal.Truncate(value);
        }

        /// <summary>
        /// Возвращает дробную часть числа
        /// </summary>
        /// <param name="value">
        /// Число 
        /// </param>
        /// <returns>
        /// Дробная часть числа 
        /// </returns>
        public static decimal Mantissa(this decimal value)
        {
            return value - decimal.Truncate(value);
        }

        /// <summary>
        /// Округлить число до указанного кол-ва знаков
        /// </summary>
        /// <param name="value">
        /// Число 
        /// </param>
        /// <param name="decimals">
        /// Кол-во знаков после запятой 
        /// </param>
        /// <returns>
        /// Округленное число 
        /// </returns>
        public static decimal RoundDecimal(this decimal value, int decimals)
        {
            return decimal.Round(value, decimals);
        }

        /// <summary>
        /// Округлить число до целого
        /// </summary>
        /// <param name="value">
        /// Число 
        /// </param>
        /// <returns>
        /// Округленное число 
        /// </returns>
        public static decimal RoundDecimal(this decimal value)
        {
            return decimal.Round(value, 0);
        }

        /// <summary>
        /// The to formated string.
        /// </summary>
        /// <param name="value">
        /// The value. 
        /// </param>
        /// <param name="splitter">
        /// The splitter. 
        /// </param>
        /// <returns>
        /// The to formated string. 
        /// </returns>
        public static string ToFormatedString(this decimal value, char splitter)
        {
            NumberFormatInfo formatInfo = new NumberFormatInfo
                {
                    NegativeSign = "-",
                    NumberNegativePattern = 1,
                    NumberGroupSizes = new int[3],
                    NumberGroupSeparator = string.Empty,
                    NumberDecimalSeparator = splitter.ToString(),
                    NumberDecimalDigits = 10
                };

            return value.ToString(formatInfo);
        }
    }
}