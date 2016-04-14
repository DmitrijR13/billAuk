using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.Utility
{
    public static class DataTypesUtility
    {
        /// <summary>
        /// Возвращает true, если строка состоит только из цифр
        /// </summary>
        /// <param name="val">Строка</param>
        /// <returns>True - только цифры, false - иначе</returns>
        public static bool IsDigits(string val)
        {
            var regDigits = new Regex(@"\d+");
            var regNotDigits = new Regex(@"\D+");
            return regDigits.IsMatch(val) && !regNotDigits.IsMatch(val);
        }
    }
}
