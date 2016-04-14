using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace FormatLibrary
{
    /// <summary>
    /// Атрибуты класса формата загрузки
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AssembleAttribute : Attribute
    {
        /// <summary>
        /// Наименование формата
        /// </summary>
        public string FormatName { get; set; }
        /// <summary>
        /// Версия формата
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Имя при регистрации
        /// </summary>
        public string RegistrationName { get; set; }
        /// <summary>
        /// Тип и свойства класс
        /// </summary>
        public Type type { get; set; }
        /// <summary>
        /// Путь до загружаемого файла
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Наименование загружаемого файла
        /// </summary>
        public string FileName { get; set; }
    }

    /// <summary>
    /// Передаваемый тип
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Номер элемента 
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// Наименование файла
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Тип формата
        /// </summary>
        [ScriptIgnore]
        public Type type { get; set; }
        /// <summary>
        /// Наименование формата
        /// </summary>
        public string Format { get; set; }
        /// <summary>
        ///Версия формата
        /// </summary>
        public double Version { get; set; }
        /// <summary>
        /// Наименование формата при регистрации
        /// </summary>
        public string RegistrationName { get; set; }
        /// <summary>
        /// Идентификатор формата
        /// </summary>
        public int formatID { get; set; }
        /// <summary>
        /// Строка с результатом
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// Строка со статусом
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Статус в формате enum
        /// </summary>
        public Statuses StatusID { get; set; }
        /// <summary>
        /// Путь до файла
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Прогресс выполнения
        /// </summary>
        public decimal progress { get; set; }
        /// <summary>
        /// Ссылка на протокол
        /// </summary>
        public string link { get; set; }
        /// <summary>
        /// Полное наименование типа, необходимо для загрузки задача из файла
        /// </summary>
        public string TypeName { get; set; }

        public string programVersion { get; set; }
        public DateTime date { get; set; }
        public bool WithEndSymbol { get; set; }
    }

    /// <summary>
    /// Статусы проверщика
    /// </summary>
    public enum Statuses
    {
        [Description("Добавлена")]
        Added = 1,//Добавлена
        [Description("Выполняется")]
        Execute = 2,//Выполняется
        [Description("Остановлено")]
        Stopped = 3,//Остановлено
        [Description("Завершено")]
        Finished = 4,//Завершено
        [Description("Завершено с ошибкой(-ами)")]
        Error = 5//Завершено с ошибкой
    }

    /// <summary>
    /// Типы данных 
    /// </summary>
    public enum Types
    {
        Int,
        Numeric,
        NumericMoney,
        String,
        DateTime
    }

    /// <summary>
    /// Шаблон, необходимый для проверки форматов
    /// </summary>
    public class Template
    {
        public Template(string fieldName, Types type, int? minLength, int? maxLength, bool necessarily = true, int column = 0, string section = null, params string[] correctSymbols)
        {
            this.fieldName = fieldName;
            this.minLength = minLength;
            this.maxLength = maxLength;
            this.type = type;
            //this.mantissa = mantissa;
            this.necessarily = necessarily;
            this.correctSymbols = correctSymbols;
            this.section = section;
            this.column = column;
        }

        public static Dictionary<string, HashSet<string>> CreateSets(Dictionary<string, List<Template>> templateDict, Dictionary<string, List<string[]>> data, ref List<string> error)
        {
            var dict = new Dictionary<string, HashSet<string>>();
            foreach (var template in templateDict)
            {
                foreach (var t in template.Value)
                {
                    if (t.section == null || t.column == 0) continue;
                    if (!data.ContainsKey(t.section)) continue;
                    if (dict.ContainsKey(t.section + t.column)) continue;
                    var hash = new HashSet<string>();
                    foreach (var d in data[t.section])
                    {
                        try
                        {
                            if (!hash.Contains(d[t.column].Trim()))
                            {
                                hash.Add(d[t.column].Trim());
                            }
                            else
                            {
                                if (d[t.column].Trim().Length != 0)
                                    error.Add(
                                        "Обнаружена ошибка входных данных.Имеются дублирующиеся записи - " + t.fieldName + "; Значение:" +
                                        d[t.column].Trim());
                            }
                        }
                        catch
                        {
                        }
                    }
                    dict.Add(t.section + t.column, hash);
                }
            }
            return dict;
        }

        /// <summary>
        /// Наименование поля
        /// </summary>
        public string fieldName { get; set; }
        /// <summary>
        /// Тип данных, которым должно являться передаваемое поле
        /// </summary>
        public Types type { get; set; }
        /// <summary>
        /// Символы которые, могут встречаться во входящем объекте
        /// </summary>
        public string[] correctSymbols { get; set; }
        /// <summary>
        /// Минимальная длина(для чисел минимальное значение)
        /// </summary>
        public int? minLength { get; set; }
        /// <summary>
        /// Максимальная длина(для чисел максимальное значение)
        /// </summary>
        public int? maxLength { get; set; }
        /// <summary>
        /// Обязательность поля
        /// </summary>
        public bool necessarily { get; set; }
        /// <summary>
        /// Мантисса(в данный момент не используется)
        /// </summary>
        public int mantissa { get; set; }
        /// <summary>
        /// Порядковый номер зависимой колонки
        /// </summary>
        public int column { get; set; }
        /// <summary>
        /// Номер зависимой секции
        /// </summary>
        public string section { get; set; }

        /// <summary>
        /// Проверка объекта
        /// </summary>
        /// <param name="value">Значение объекта</param>
        /// <param name="rowNumber">Номер строки</param>
        /// <param name="section">Номер секции</param>
        /// <returns>Возвращает текст ошибки или null</returns>
        public string CheckValues(int number, string value, int rowNumber, string section, Dictionary<string, HashSet<string>> sets = null, Dictionary<string, List<string[]>> data = null)
        {
            if (value.Trim().Length == 0)
                return necessarily ? string.Format("Секция {2},cтрока {1}: Не заполнено обязательное поле:{0}", fieldName, rowNumber, section) : null;
            var newValue = value.Trim();
            correctSymbols.ToList().ForEach(s => newValue = newValue.Replace(s, ""));
            newValue = newValue.Replace("\"", "").Replace("\'", "");
            var customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            switch (type)
            {
                case Types.Int:
                    {
                        Int64 i_int;
                        if (!Int64.TryParse(newValue, out i_int))
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат. Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, section);
                        }
                        if (minLength.HasValue)
                            if (i_int < minLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (меньше {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, minLength, section);
                            }
                        if (maxLength.HasValue)
                            if (i_int > maxLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (превышает {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, maxLength, section);
                            }
                    } break;
                case Types.Numeric:
                    {
                        Decimal d_decimal;
                        newValue = newValue.Replace(",", ".");
                        if (!Decimal.TryParse(newValue, out d_decimal))
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат. Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, section);
                        }
                        if (Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString(CultureInfo.InvariantCulture).Length > 20)
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат(дробная часть превышает 20 знаков). Значение = {0}, имя поля: {2}",
                                value, rowNumber, fieldName, section);
                        }
                        if (minLength != null)
                            if (d_decimal < minLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (меньше {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, minLength, section);
                            }
                        if (maxLength != null)
                            if (d_decimal > maxLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (превышает {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, maxLength, section);
                            }
                    } break;
                case Types.NumericMoney:
                    {
                        Decimal d_decimal;
                        newValue = newValue.Replace(",", ".");
                        if (!Decimal.TryParse(newValue, out d_decimal))
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат. Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, section);
                        }
                        if (Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString(CultureInfo.InvariantCulture).Length > 4)
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат(дробная часть превышает 2 знака). Значение = {0}, имя поля: {2}",
                                value, rowNumber, fieldName, section);
                        }
                        if (minLength != null)
                            if (d_decimal < minLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (меньше {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, minLength, section);
                            }
                        if (maxLength != null)
                            if (d_decimal > maxLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (превышает {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, maxLength, section);
                            }
                    } break;
                case Types.String:
                    {
                        newValue = newValue.Replace("«", "\"").Replace("»", "\"").Replace("'", "\"");
                        if (newValue.Length > maxLength)
                        {
                            return string.Format("Секция {4},cтрока {1}: Длина текста превышает заданный формат ({3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, maxLength, section);
                        }
                    } break;
                case Types.DateTime:
                    {
                        DateTime date;
                        if (!DateTime.TryParse(newValue, out date))
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат. Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, section);
                        }
                    } break;
            }
            string l = null;
            try
            {
                if (section == "6" && fieldName == "Номер секции")
                {
                    decimal sum_insaldo, sum_nach, reval, real_charge, sum_money, sum_outsaldo;
                    if (data[section][rowNumber - Template.sectionLength - 1].Length < 27)
                    {
                        decimal.TryParse(data[section][rowNumber - Template.sectionLength - 1][13], NumberStyles.Any, CultureInfo.CurrentCulture, out reval);
                        real_charge = 0;
                    }
                    else
                    {
                        decimal.TryParse(data[section][rowNumber - Template.sectionLength - 1][28], NumberStyles.Any, CultureInfo.CurrentCulture, out reval);
                        decimal.TryParse(data[section][rowNumber - Template.sectionLength - 1][29], NumberStyles.Any, CultureInfo.CurrentCulture, out real_charge);
                    }
                    decimal.TryParse(data[section][rowNumber - Template.sectionLength - 1][4], NumberStyles.Any, CultureInfo.CurrentCulture, out sum_insaldo);
                    decimal.TryParse(data[section][rowNumber - Template.sectionLength - 1][12], NumberStyles.Any, CultureInfo.CurrentCulture, out sum_nach);
                    decimal.TryParse(data[section][rowNumber - Template.sectionLength - 1][20], NumberStyles.Any, CultureInfo.CurrentCulture, out sum_money);
                    decimal.TryParse(data[section][rowNumber - Template.sectionLength - 1][22], NumberStyles.Any, CultureInfo.CurrentCulture, out sum_outsaldo);
                    var sum = sum_insaldo + sum_nach + reval + real_charge - sum_money - sum_outsaldo;
                    if (sum != 0)
                        return string.Format("Секция {1},cтрока {0}: Ошибка, сумма не равна 0, Сумма: входящее сальдо + начисления + перерасчет + перекидка - оплата - сумма исходящего сальдо = {2}", rowNumber, section, sum);
                }
                if (column == 0 && this.section == null) return null;
                if (sets.ContainsKey(this.section + column))
                    if (!sets[this.section + column].Contains(value))
                        l = string.Format(
                            "Секция {0},cтрока {1}: Поле не связано не с одним значением. Значение = {2}, имя поля: {3}",
                            section, rowNumber, value, fieldName);
            }
            catch (Exception ex)
            {
            }
            return l;
        }

        public static int sectionLength;
        public static string[] FormatTypes =
        {
            "0.11",
            "0.9",
            "1.0",
            "1.2.1",
            "1.3.2",
            "1.3.4",
            "1.3.5",
            "1.3.6",
            "1.3.7",
            "1.3.8",
            "1.3.5.1",
            "1.3.5.2",
            "1.3.5.3"
        };

        public string GetTemplateFieldValue(int field, Dictionary<string, List<Template>> body, Dictionary<string, string> header, int number)
        {
            switch (field)
            {
                case 0:
                    {
                        return number.ToString();
                    }
                    break;
                case 1:
                    {
                        return fieldName;
                    } break;
                case 2:
                    {
                        switch (type)
                        {
                            case Types.Int:
                                {
                                    return "Целое";
                                }
                            case Types.Numeric:
                                {
                                    return "Число";
                                }
                            case Types.NumericMoney:
                                {
                                    return "Число в денежном формате";
                                }
                            case Types.String:
                                {
                                    return string.Format("Текст({0})", maxLength);
                                }
                            case Types.DateTime:
                                {
                                    return "Дата";
                                }
                        }
                    } break;
                case 3:
                    {
                        return necessarily ? "Да" : "Нет";
                    }
                case 4:
                    {
                        var str = new StringBuilder();
                        switch (type)
                        {
                            case Types.Int:
                            case Types.NumericMoney:
                            case Types.Numeric:
                                {
                                    if (minLength != null) str.AppendLine(string.Format("Минимальное значение больше или равно {0}.", minLength));
                                    if (maxLength != null) str.AppendLine(string.Format("Максимальное значение меньше или равно {0}.", maxLength));
                                } break;
                            case Types.String:
                                {
                                    if (minLength != null) str.AppendLine(string.Format("Минимальная длина больше или равна {0}.", minLength));
                                    if (maxLength != null) str.AppendLine(string.Format("Максимальная длина меньше или равна {0}.", maxLength));
                                } break;
                            case Types.DateTime:
                                break;
                        }
                        if (section != null)
                        {
                            str.AppendLine(string.Format("Поле связано с полем '{0}' секции '{1}'.", body[section][column].fieldName, header[section]));
                        }
                        return str.ToString();
                    } break;
            }
            return "";
        }
    }

    public class SumTemplate
    {
        protected string sectionNumber { get; set; }
        protected string sectionName { get; set; }
        protected int[] columnNumbers { get; set; }
        protected int groupColumn { get; set; }

        public SumTemplate(string sectionNumber, int[] columnNumbers, int groupColumn, string sectionName)
        {
            this.sectionNumber = sectionNumber;
            this.columnNumbers = columnNumbers;
            this.groupColumn = groupColumn;
            this.sectionName = sectionName;
        }

        public string Calculate(Dictionary<string, List
            <string[]>> data, Dictionary<string, List<Template>> template)
        {
            if (data == null) return null;
            var resStr = "";
            try
            {
                if (!data.ContainsKey(sectionNumber)) return null;
                var curTemp = template[sectionNumber];

                var dataSec = data[sectionNumber].GroupBy(x => x[groupColumn]).ToList();
                resStr += string.Format("Секция:'{0}'\r\n", sectionName);
                var mainSum = new Dictionary<int, decimal>();
                foreach (var elems in dataSec)
                {
                    try
                    {
                        foreach (var col in columnNumbers)
                        {
                            if (!mainSum.ContainsKey(col))
                                mainSum.Add(col, elems.ToList().Sum(x => x[col].Trim() != "" ? StrToDec(x[col].Replace(".", ",").Trim()) : 0));
                            else mainSum[col] += elems.ToList().Sum(x => x[col].Trim() != "" ? StrToDec(x[col].Replace(".", ",").Trim()) : 0);
                        }
                        if (data.ContainsKey("5"))
                            resStr += string.Format("Суммы по договору :'{0}'\r\n",
                                (data["5"] != null
                                    ? ((data["5"].First(x => x[1].Trim() == elems.Key).Any()
                                        ? data["5"].First(x => x[1].Trim() == elems.Key)[5] + "(" + elems.Key + ")"
                                        : "(" + elems.Key + ")"))
                                    : "(" + elems.Key + ")"));
                        resStr = columnNumbers.Aggregate(resStr,
                            (current, col) =>
                                current +
                                ("Сумма по колонке '" + curTemp[col].fieldName + "': " +
                                 elems.ToList()
                                     .Sum(x => x[col].Trim() != "" ? StrToDec(x[col].Replace(".", ",").Trim()) : 0) +
                                 "\r\n"));
                    }
                    catch
                    {
                    }
                }
                resStr += "\r\nОбщая сумма по договорам :\r\n";
                resStr = mainSum.Aggregate(resStr, (current, main) => current + ("Сумма по колонке '" + curTemp[main.Key].fieldName + "': " + main.Value + "\r\n"));
            }
            catch
            {
            }
            return resStr;
        }


        public decimal StrToDec(string str)
        {
            Decimal d_decimal;
            var newValue = str.Replace("\"", "").Replace("\'", "");
            if (!Decimal.TryParse(newValue, NumberStyles.Any, CultureInfo.GetCultureInfo("ru-RU"), out d_decimal))
                if (!Decimal.TryParse(newValue, NumberStyles.Any, CultureInfo.InstalledUICulture, out d_decimal))
                    if (!Decimal.TryParse(newValue, NumberStyles.Any, CultureInfo.CurrentCulture, out d_decimal))
                        throw new Exception(CultureInfo.CurrentCulture.DisplayName + ";" + CultureInfo.CurrentCulture.EnglishName + ";" + CultureInfo.CurrentCulture.NumberFormat + ";" + CultureInfo.CurrentCulture.CultureTypes);
            return d_decimal;
        }
    }

    public class Counters
    {
        protected string section { get; set; }
        protected int columnKod { get; set; }
        protected int columnDate { get; set; }
        protected int columnValue { get; set; }
        protected string sectionName { get; set; }
        public Counters(string sectionName, string section, int columnKod, int columnDate, int columnValue)
        {
            this.sectionName = sectionName;
            this.section = section;
            this.columnKod = columnKod;
            this.columnDate = columnDate;
            this.columnValue = columnValue;
        }
        public string CheckCounters(Dictionary<string, List<string[]>> data = null)
        {
            //режим выключен
            return null;
            if (data == null) return null;
            if (!data.ContainsKey(section)) return null;
            var counterVals = data[section].GroupBy(x => x[columnKod]).Where(g => g.Count() > 1)
              .Select(y => new { Element = y.Key, Counter = y.Count() })
              .ToList();
            foreach (var counter in counterVals)
            {
                var vals = data[section].Where(x => x[columnKod] == counter.Element).ToList();
                var maxDate = vals.Max(x => Convert.ToDateTime(x[columnDate]));
                var maxValue = vals.Max(x => x[columnValue]);
                var IndexOfMaxDate = 0;
                var IndexOfMaxValue = 0;
                for (var i = 0; i < vals.Count; i++)
                {
                    if (Convert.ToDateTime(vals[i][columnDate]) == maxDate)
                    {
                        IndexOfMaxDate = i;
                    }
                    if (vals[i][columnValue] == maxValue)
                    {
                        IndexOfMaxValue = i;
                    }
                }
                if (IndexOfMaxDate != IndexOfMaxValue) return string.Format("Ошибка: Переход через ноль, значения :{0},{1}\r\n", string.Join("|", vals[IndexOfMaxDate]), string.Join("|", vals[IndexOfMaxValue]));
            }
            return null;
        }
    }
}
