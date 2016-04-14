// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Transliterator.cs" company="">
//   
// </copyright>
// <summary>
//   The transliterator type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Bars.KP50.Utils
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The transliterator type.
    /// </summary>
    public enum TransliteratorType
    {
        /// <summary>
        ///   The гост.
        /// </summary>
        ГОСТ,

        /// <summary>
        ///   The исо.
        /// </summary>
        ИСО,

        /// <summary>
        ///   The защищенный sql.
        /// </summary>
        ЗащищенныйSQL
    }

    /// <summary>
    /// The transliterator.
    /// </summary>
    public static class Transliterator
    {
        /// <summary>
        ///   The gost.
        /// </summary>
        private static readonly Dictionary<string, string> Gost = new Dictionary<string, string>(); // ГОСТ 16876-71

        /// <summary>
        ///   The iso.
        /// </summary>
        private static readonly Dictionary<string, string> Iso = new Dictionary<string, string>(); // ИСО 9-95

        /// <summary>
        ///   The safe sql.
        /// </summary>
        private static readonly Dictionary<string, string> SafeSql = new Dictionary<string, string>();

        // для генерации SQL

        /// <summary>
        ///   Initializes static members of the <see cref="Transliterator" /> class.
        /// </summary>
        static Transliterator()
        {
            Gost.Add("Є", "EH");
            Gost.Add("І", "I");
            Gost.Add("і", "i");
            Gost.Add("№", "#");
            Gost.Add("є", "eh");
            Gost.Add("А", "A");
            Gost.Add("Б", "B");
            Gost.Add("В", "V");
            Gost.Add("Г", "G");
            Gost.Add("Д", "D");
            Gost.Add("Е", "E");
            Gost.Add("Ё", "JO");
            Gost.Add("Ж", "ZH");
            Gost.Add("З", "Z");
            Gost.Add("И", "I");
            Gost.Add("Й", "JJ");
            Gost.Add("К", "K");
            Gost.Add("Л", "L");
            Gost.Add("М", "M");
            Gost.Add("Н", "N");
            Gost.Add("О", "O");
            Gost.Add("П", "P");
            Gost.Add("Р", "R");
            Gost.Add("С", "S");
            Gost.Add("Т", "T");
            Gost.Add("У", "U");
            Gost.Add("Ф", "F");
            Gost.Add("Х", "KH");
            Gost.Add("Ц", "C");
            Gost.Add("Ч", "CH");
            Gost.Add("Ш", "SH");
            Gost.Add("Щ", "SHH");
            Gost.Add("Ъ", "\"");
            Gost.Add("Ы", "Y");
            Gost.Add("Ь", "'");
            Gost.Add("Э", "EH");
            Gost.Add("Ю", "YU");
            Gost.Add("Я", "YA");
            Gost.Add("а", "a");
            Gost.Add("б", "b");
            Gost.Add("в", "v");
            Gost.Add("г", "g");
            Gost.Add("д", "d");
            Gost.Add("е", "e");
            Gost.Add("ё", "jo");
            Gost.Add("ж", "zh");
            Gost.Add("з", "z");
            Gost.Add("и", "i");
            Gost.Add("й", "jj");
            Gost.Add("к", "k");
            Gost.Add("л", "l");
            Gost.Add("м", "m");
            Gost.Add("н", "n");
            Gost.Add("о", "o");
            Gost.Add("п", "p");
            Gost.Add("р", "r");
            Gost.Add("с", "s");
            Gost.Add("т", "t");
            Gost.Add("у", "u");
            Gost.Add("ф", "f");
            Gost.Add("х", "kh");
            Gost.Add("ц", "c");
            Gost.Add("ч", "ch");
            Gost.Add("ш", "sh");
            Gost.Add("щ", "shh");
            Gost.Add("ъ", "\"");
            Gost.Add("ы", "y");
            Gost.Add("ь", "'");
            Gost.Add("э", "eh");
            Gost.Add("ю", "yu");
            Gost.Add("я", "ya");
            Gost.Add("«", string.Empty);
            Gost.Add("»", string.Empty);
            Gost.Add("—", "-");

            Iso.Add(" ", "_");
            Iso.Add("Є", "YE");
            Iso.Add("І", "I");
            Iso.Add("Ѓ", "G");
            Iso.Add("і", "i");

            // Iso.Add("№", "#");
            Iso.Add("№", string.Empty);
            Iso.Add("є", "ye");
            Iso.Add("ѓ", "g");
            Iso.Add("А", "A");
            Iso.Add("Б", "B");
            Iso.Add("В", "V");
            Iso.Add("Г", "G");
            Iso.Add("Д", "D");
            Iso.Add("Е", "E");
            Iso.Add("Ё", "YO");
            Iso.Add("Ж", "ZH");
            Iso.Add("З", "Z");
            Iso.Add("И", "I");
            Iso.Add("Й", "J");
            Iso.Add("К", "K");
            Iso.Add("Л", "L");
            Iso.Add("М", "M");
            Iso.Add("Н", "N");
            Iso.Add("О", "O");
            Iso.Add("П", "P");
            Iso.Add("Р", "R");
            Iso.Add("С", "S");
            Iso.Add("Т", "T");
            Iso.Add("У", "U");
            Iso.Add("Ф", "F");
            Iso.Add("Х", "X");
            Iso.Add("Ц", "C");
            Iso.Add("Ч", "CH");
            Iso.Add("Ш", "SH");
            Iso.Add("Щ", "SHH");
            Iso.Add("Ъ", "\"");
            Iso.Add("Ы", "Y");
            Iso.Add("Ь", "'");
            Iso.Add("Э", "E");
            Iso.Add("Ю", "YU");
            Iso.Add("Я", "YA");
            Iso.Add("а", "a");
            Iso.Add("б", "b");
            Iso.Add("в", "v");
            Iso.Add("г", "g");
            Iso.Add("д", "d");
            Iso.Add("е", "e");
            Iso.Add("ё", "yo");
            Iso.Add("ж", "zh");
            Iso.Add("з", "z");
            Iso.Add("и", "i");
            Iso.Add("й", "j");
            Iso.Add("к", "k");
            Iso.Add("л", "l");
            Iso.Add("м", "m");
            Iso.Add("н", "n");
            Iso.Add("о", "o");
            Iso.Add("п", "p");
            Iso.Add("р", "r");
            Iso.Add("с", "s");
            Iso.Add("т", "t");
            Iso.Add("у", "u");
            Iso.Add("ф", "f");
            Iso.Add("х", "x");
            Iso.Add("ц", "c");
            Iso.Add("ч", "ch");
            Iso.Add("ш", "sh");
            Iso.Add("щ", "shh");
            Iso.Add("ъ", "\"");
            Iso.Add("ы", "y");
            Iso.Add("ь", "'");
            Iso.Add("э", "e");
            Iso.Add("ю", "yu");
            Iso.Add("я", "ya");
            Iso.Add("«", string.Empty);
            Iso.Add("»", string.Empty);
            Iso.Add("—", "-");
            Iso.Add(",", ".");

            SafeSql.Add("Є", "YE");
            SafeSql.Add("І", "I");
            SafeSql.Add("Ѓ", "G");
            SafeSql.Add("і", "i");
            SafeSql.Add("№", "#");
            SafeSql.Add("є", "ye");
            SafeSql.Add("ѓ", "g");
            SafeSql.Add("А", "A");
            SafeSql.Add("Б", "B");
            SafeSql.Add("В", "V");
            SafeSql.Add("Г", "G");
            SafeSql.Add("Д", "D");
            SafeSql.Add("Е", "E");
            SafeSql.Add("Ё", "YO");
            SafeSql.Add("Ж", "ZH");
            SafeSql.Add("З", "Z");
            SafeSql.Add("И", "I");
            SafeSql.Add("Й", "J");
            SafeSql.Add("К", "K");
            SafeSql.Add("Л", "L");
            SafeSql.Add("М", "M");
            SafeSql.Add("Н", "N");
            SafeSql.Add("О", "O");
            SafeSql.Add("П", "P");
            SafeSql.Add("Р", "R");
            SafeSql.Add("С", "S");
            SafeSql.Add("Т", "T");
            SafeSql.Add("У", "U");
            SafeSql.Add("Ф", "F");
            SafeSql.Add("Х", "X");
            SafeSql.Add("Ц", "C");
            SafeSql.Add("Ч", "CH");
            SafeSql.Add("Ш", "SH");
            SafeSql.Add("Щ", "SHH");
            SafeSql.Add("Ъ", string.Empty);
            SafeSql.Add("Ы", "Y");
            SafeSql.Add("Ь", string.Empty);
            SafeSql.Add("Э", "E");
            SafeSql.Add("Ю", "YU");
            SafeSql.Add("Я", "YA");
            SafeSql.Add("а", "a");
            SafeSql.Add("б", "b");
            SafeSql.Add("в", "v");
            SafeSql.Add("г", "g");
            SafeSql.Add("д", "d");
            SafeSql.Add("е", "e");
            SafeSql.Add("ё", "yo");
            SafeSql.Add("ж", "zh");
            SafeSql.Add("з", "z");
            SafeSql.Add("и", "i");
            SafeSql.Add("й", "j");
            SafeSql.Add("к", "k");
            SafeSql.Add("л", "l");
            SafeSql.Add("м", "m");
            SafeSql.Add("н", "n");
            SafeSql.Add("о", "o");
            SafeSql.Add("п", "p");
            SafeSql.Add("р", "r");
            SafeSql.Add("с", "s");
            SafeSql.Add("т", "t");
            SafeSql.Add("у", "u");
            SafeSql.Add("ф", "f");
            SafeSql.Add("х", "x");
            SafeSql.Add("ц", "c");
            SafeSql.Add("ч", "ch");
            SafeSql.Add("ш", "sh");
            SafeSql.Add("щ", "shh");
            SafeSql.Add("ъ", string.Empty);
            SafeSql.Add("ы", "y");
            SafeSql.Add("ь", string.Empty);
            SafeSql.Add("э", "e");
            SafeSql.Add("ю", "yu");
            SafeSql.Add("я", "ya");
            SafeSql.Add("«", string.Empty);
            SafeSql.Add("»", string.Empty);
            SafeSql.Add("—", "-");
        }

        /// <summary>
        /// The lat to rus.
        /// </summary>
        /// <param name="текст">
        /// The текст. 
        /// </param>
        /// <returns>
        /// The lat to rus. 
        /// </returns>
        public static string LatToRus(string текст)
        {
            return LatToRus(текст, TransliteratorType.ИСО);
        }

        /// <summary>
        /// The lat to rus.
        /// </summary>
        /// <param name="текст">
        /// The текст. 
        /// </param>
        /// <param name="тип">
        /// The тип. 
        /// </param>
        /// <returns>
        /// The lat to rus. 
        /// </returns>
        public static string LatToRus(string текст, TransliteratorType тип)
        {
            string output = текст;
            Dictionary<string, string> tdict = ПолучитьСловарьСоответствия(тип);

            foreach (var key in tdict)
            {
                output = output.Replace(key.Value, key.Key);
            }

            return output;
        }

        /// <summary>
        /// The rus to lat.
        /// </summary>
        /// <param name="текст">
        /// The текст. 
        /// </param>
        /// <returns>
        /// The rus to lat. 
        /// </returns>
        public static string RusToLat(string текст)
        {
            return RusToLat(текст, TransliteratorType.ИСО);
        }

        /// <summary>
        /// The rus to lat.
        /// </summary>
        /// <param name="текст">
        /// The текст. 
        /// </param>
        /// <param name="тип">
        /// The тип. 
        /// </param>
        /// <returns>
        /// The rus to lat. 
        /// </returns>
        public static string RusToLat(string текст, TransliteratorType тип)
        {
            string output = текст;

            Dictionary<string, string> tdict = ПолучитьСловарьСоответствия(тип);

            foreach (var key in tdict)
            {
                output = output.Replace(key.Key, key.Value);
            }

            return output;
        }

        /// <summary>
        /// The получитьсловарьсоответствия.
        /// </summary>
        /// <param name="тип">
        /// The тип. 
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        private static Dictionary<string, string> ПолучитьСловарьСоответствия(TransliteratorType тип)
        {
            switch (тип)
            {
                case TransliteratorType.ГОСТ:
                    return Gost;
                case TransliteratorType.ИСО:
                    return Iso;
                case TransliteratorType.ЗащищенныйSQL:
                    return SafeSql;
                default:
                    throw new InvalidOperationException("Данный тип не поддерживается.");
            }
        }
    }
}