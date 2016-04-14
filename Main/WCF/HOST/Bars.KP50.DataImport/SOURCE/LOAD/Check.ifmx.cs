using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Проверки полей секций на корректность типа (по формату обмена)
    /// </summary>
    public static class CheckType
    {
        /// <summary>
        /// Проверка на integer
        /// </summary>
        /// <param name="s_int"></param>
        /// <param name="not_null">Обязательность поля</param>
        /// <param name="low">Нижняя граница числа</param>
        /// <param name="high">Верхняя граница числа</param>
        /// <param name="ret">Ошибки</param>
        /// <returns>Число</returns>
        public static string CheckInt(string s_int, bool not_null, Int64? low, Int64? high, ref Returns ret)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();
            ret.text = temp;

            int i_int = 0;

            if (s_int == "")
            {
                if (not_null)
                {
                    ret.result = false;
                    ret.text = "Не заполнено обязательное числовое поле: ";
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (!int.TryParse(s_int, out i_int))
            {
                ret.result = false;
                ret.text = "Поле имеет неверный формат. Значение = " + s_int + ", имя поля: ";
                return "";
            }

            if (low.HasValue)
            {
                if (i_int < low.Value)
                {
                    ret.result = false;
                    ret.text = "Поле имеет неверное значение(меньше " + low.Value + "). Значение = " + i_int + ", имя поля: ";
                    return "";
                }
            }

            if (high.HasValue)
            {
                if (i_int > high.Value)
                {
                    ret.result = false;
                    ret.text = "Поле имеет неверное значение(превышает " + high.Value + "). Значение = " + i_int + ", имя поле: ";
                    return "";
                }
            }

            return i_int.ToString();
        }

        /// <summary>
        /// Проверка на integer
        /// </summary>
        /// <param name="valuesFromFile"></param>
        /// <param name="i">Номер поля</param>
        /// <param name="not_null">Обязательность поля</param>
        /// <param name="low">Нижняя граница числа</param>
        /// <param name="high">Верхняя граница числа</param>
        /// <param name="ret">Ошибки</param>
        /// <param name="fieldName"></param>
        /// <returns>Число</returns>
        public static string CheckInt2(DbValuesFromFile valuesFromFile, int i, bool not_null, Int64? low, Int64? high, ref Returns ret, string fieldName)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();
            ret.text = temp;

            Int64 i_int = 0;

            if (valuesFromFile.vals[i] == "")
            {
                if (not_null)
                {
                    ret.result = false;
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Не заполнено обязательное числовое поле: " + fieldName);
                    return "";
                }
                else
                {
                    return "null";
                }
            }

            CultureInfo provider;
            provider = CultureInfo.InvariantCulture;
            NumberStyles styles;
            styles = NumberStyles.Integer | NumberStyles.AllowDecimalPoint;
            if (!Int64.TryParse(valuesFromFile.vals[i], styles, provider, out i_int))
            {
                ret.result = false;
                //ret.text = "Поле имеет неверный формат. Значение = " + valuesFromFile.vals[i] + ", имя поля: ";
                valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат. Значение = " + valuesFromFile.vals[i] + ", имя поля: " + fieldName);
                return "";
            }

            if (low.HasValue)
            {
                if (i_int < low.Value)
                {
                    ret.result = false;
                    //ret.text = "Поле имеет неверное значение(меньше " + low.Value + "). Значение = " + i_int + ", имя поля: ";
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверное значение(меньше " + low.Value + "). Значение = " + i_int + ", имя поля: " + fieldName);
                    return "";
                }
            }

            if (high.HasValue)
            {
                if (i_int > high.Value)
                {
                    ret.result = false;
                    //ret.text = "Поле имеет неверное значение(превышает " + high.Value + "). Значение = " + i_int + ", имя поле: ";
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверное значение(превышает " + high.Value + "). Значение = " + i_int + ", имя поле: " + fieldName);
                    return "";
                }
            }

            return i_int.ToString();
        }


        
        /// <summary>
        /// Проверка на decimal
        /// </summary>
        /// <param name="s_decimal">Число в строковом представлении</param>
        /// <param name="not_null">Обязательность поля</param>
        /// <param name="is_money"></param>
        /// <param name="low">Нижняя граница числа</param>
        /// <param name="high">Верхняя граница числа</param>
        /// <param name="ret">Ошибки</param>
        /// <param name="is_gubkin"></param>
        /// <returns></returns>
        public static string CheckDecimal(string s_decimal, bool not_null, bool is_money, decimal? low, decimal? high, ref Returns ret, bool is_gubkin = false)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();

            ret.text = temp;

            s_decimal = s_decimal.Replace(",", ".");

            decimal d_decimal = 0;

            if (s_decimal == "")
            {
                if (not_null)
                {
                    ret.result = false;
                    ret.text = "Не заполнено обязательное числовое поле: ";
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (!Decimal.TryParse(s_decimal, out d_decimal))
            {
                if (is_gubkin)
                {
                    if (Decimal.TryParse(s_decimal.Replace("E-01", ""), out d_decimal))
                    {
                        d_decimal = d_decimal / 10;
                    }
                    else
                    {
                        if (Decimal.TryParse(s_decimal.Replace("E-02", ""), out d_decimal))
                        {
                            d_decimal = d_decimal / 100;
                        }
                        else
                        {
                            if (Decimal.TryParse(s_decimal.Replace("E-03", ""), out d_decimal))
                            {
                                d_decimal = d_decimal / 1000;
                            }
                            else
                            {
                                if (s_decimal.Contains("E-") || s_decimal.Contains("e-"))
                                {
                                    d_decimal = 0;
                                    return d_decimal.ToString();
                                }
                                else
                                {
                                    ret.result = false;
                                    ret.text = "Поле имеет неверный формат. Значение = " + s_decimal + ", имя поля: ";
                                    return "";

                                }
                            }
                        }
                    }
                }
                else
                {
                    if (s_decimal.Contains("E-") || s_decimal.Contains("e-"))
                    {
                        d_decimal = 0;
                    }
                    else
                    {
                        ret.result = false;
                        ret.text = "Поле имеет неверный формат. Значение = " + s_decimal + ", имя поля: ";
                        return "";
                    }
                }
            }


            if (is_money && Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString().Length > 4)
            {
                ret.result = false;
                ret.text = "Поле имеет неверный формат(дробная часть превышает 2 знака). Значение = " + s_decimal + ", имя поля: ";
                return "";
            }

            if (!is_money && Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString().Length > 20)
            {
                ret.result = false;
                ret.text = "Поле имеет неверный формат(дробная часть превышает 20 знаков). Значение = " + s_decimal + " имя поля: ";
                return "";
            }

            if (low.HasValue)
            {
                if (d_decimal < low.Value)
                {
                    ret.result = false;
                    ret.text = "Поле имеет неверное значение(меньше " + low.Value + "). Значение = " + d_decimal + ", имя поля: ";
                    return "";
                }
            }

            if (high.HasValue)
            {
                if (d_decimal > high.Value)
                {
                    ret.result = false;
                    ret.text = "Поле имеет неверное значение(превышает " + high.Value + "). Значение = " + d_decimal + ", имя поля: ";
                    return "";
                }
            }

            return d_decimal.ToString();
        }

        public static string CheckDecimal2(DbValuesFromFile valuesFromFile, int i, bool not_null, bool is_money, decimal? low, decimal? high, ref Returns ret,  string fieldName, bool is_gubkin = false)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();

            ret.text = temp;

            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace(",", ".");

            decimal d_decimal = 0;

            if (valuesFromFile.vals[i] == "")
            {
                if (not_null)
                {
                    ret.result = false;
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Не заполнено обязательное числовое поле: " + fieldName);
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (!Decimal.TryParse(valuesFromFile.vals[i], out d_decimal))
            {
                if (is_gubkin)
                {
                    if (Decimal.TryParse(valuesFromFile.vals[i].Replace("E-01", ""), out d_decimal))
                    {
                        d_decimal = d_decimal / 10;
                    }
                    else
                    {
                        if (Decimal.TryParse(valuesFromFile.vals[i].Replace("E-02", ""), out d_decimal))
                        {
                            d_decimal = d_decimal / 100;
                        }
                        else
                        {
                            if (Decimal.TryParse(valuesFromFile.vals[i].Replace("E-03", ""), out d_decimal))
                            {
                                d_decimal = d_decimal / 1000;
                            }
                            else
                            {
                                if (valuesFromFile.vals[i].Contains("E-") || valuesFromFile.vals[i].Contains("e-"))
                                {
                                    d_decimal = 0;
                                    return d_decimal.ToString();
                                }
                                else
                                {
                                    ret.result = false;
                                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат. Значение = " + valuesFromFile.vals[i] + ", имя поля: " + fieldName);
                                    return "";

                                }
                            }
                        }
                    }
                }
                else
                {
                    if (valuesFromFile.vals[i].Contains("E-") || valuesFromFile.vals[i].Contains("e-"))
                    {
                        d_decimal = 0;
                    }
                    else
                    {
                        ret.result = false;
                        valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат. Значение = " + valuesFromFile.vals[i] + ", имя поля: " + fieldName);
                        return "";
                    }
                }
            }


            if (is_money && Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString().Length > 4)
            {
                ret.result = false;
                valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат(дробная часть превышает 2 знака). Значение = " + valuesFromFile.vals[i] + ", имя поля: " + fieldName);
                return "";
            }

            if (!is_money && Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString().Length > 20)
            {
                ret.result = false;
               valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат(дробная часть превышает 20 знаков). Значение = " + valuesFromFile.vals[i] + " имя поля: " + fieldName);
                return "";
            }

            if (low.HasValue)
            {
                if (d_decimal < low.Value)
                {
                    ret.result = false;
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверное значение(меньше " + low.Value + "). Значение = " + d_decimal + ", имя поля: " + fieldName);
                    return "";
                }
            }

            if (high.HasValue)
            {
                if (d_decimal > high.Value)
                {
                    ret.result = false;
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверное значение(превышает " + high.Value + "). Значение = " + d_decimal + ", имя поля: " + fieldName);
                    return "";
                }
            }

            return d_decimal.ToString();
        }

        /// <summary>
        /// Проверка значения из файла
        /// </summary>
        /// <param name="valuesFromFile"></param>
        /// <param name="i"></param>
        /// <param name="not_null"></param>
        /// <param name="is_money"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <param name="ret"></param>
        /// <param name="fieldName"></param>
        /// <param name="is_gubkin"></param>
        /// <param name="is_sum_lgota">флаг, разрешающий загрузку отрацательной суммы льготы</param>
        /// <returns></returns>
        public static string CheckDecimal2(DbValuesFromFile valuesFromFile, int i, bool not_null, bool is_money, decimal? low, decimal? high, ref Returns ret, string fieldName, int is_sum_lgota, bool is_gubkin = false)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();

            ret.text = temp;

            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace(",", ".");

            decimal d_decimal = 0;

            if (valuesFromFile.vals[i] == "")
            {
                if (not_null)
                {
                    ret.result = false;
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Не заполнено обязательное числовое поле: " + fieldName);
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (!Decimal.TryParse(valuesFromFile.vals[i], out d_decimal))
            {
                if (is_gubkin)
                {
                    if (Decimal.TryParse(valuesFromFile.vals[i].Replace("E-01", ""), out d_decimal))
                    {
                        d_decimal = d_decimal / 10;
                    }
                    else
                    {
                        if (Decimal.TryParse(valuesFromFile.vals[i].Replace("E-02", ""), out d_decimal))
                        {
                            d_decimal = d_decimal / 100;
                        }
                        else
                        {
                            if (Decimal.TryParse(valuesFromFile.vals[i].Replace("E-03", ""), out d_decimal))
                            {
                                d_decimal = d_decimal / 1000;
                            }
                            else
                            {
                                if (valuesFromFile.vals[i].Contains("E-") || valuesFromFile.vals[i].Contains("e-"))
                                {
                                    d_decimal = 0;
                                    return d_decimal.ToString();
                                }
                                else
                                {
                                    ret.result = false;
                                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат. Значение = " + valuesFromFile.vals[i] + ", имя поля: " + fieldName);
                                    return "";

                                }
                            }
                        }
                    }
                }
                else
                {
                    if (valuesFromFile.vals[i].Contains("E-") || valuesFromFile.vals[i].Contains("e-"))
                    {
                        d_decimal = 0;
                    }
                    else
                    {
                        ret.result = false;
                        valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат. Значение = " + valuesFromFile.vals[i] + ", имя поля: " + fieldName);
                        return "";
                    }
                }
            }


            if (is_money && Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString().Length > 4)
            {
                ret.result = false;
                valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат(дробная часть превышает 2 знака). Значение = " + valuesFromFile.vals[i] + ", имя поля: " + fieldName);
                return "";
            }

            if (!is_money && Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString().Length > 20)
            {
                ret.result = false;
                valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат(дробная часть превышает 20 знаков). Значение = " + valuesFromFile.vals[i] + " имя поля: " + fieldName);
                return "";
            }

            if (low.HasValue)
            {
                if (d_decimal < low.Value)
                {
                    if (is_sum_lgota == 1)
                    {
                        MonitorLog.WriteLog("Данные загружены с предупреждением: " + valuesFromFile.rowNumber + "Поле имеет неверное значение(меньше " +
                                                  low.Value + "). Значение = " + d_decimal + ", имя поля: " + fieldName, MonitorLog.typelog.Info, true);
                        return d_decimal.ToString();
                    }               
                }
            }

            if (high.HasValue)
            {
                if (d_decimal > high.Value)
                {
                    ret.result = false;
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверное значение(превышает " + high.Value + "). Значение = " + d_decimal + ", имя поля: " + fieldName);
                    return "";
                }
            }

            return d_decimal.ToString();
        }

        /// <summary>
        /// Проверка на дату 
        /// </summary>
        /// <param name="s_date">Строковое представление даты</param>
        /// <param name="not_null">Обязательность поля</param>
        /// <param name="ret">Ошибки</param>
        /// <returns>Дата в строковом представлении</returns>
        public static string CheckDateTime(string s_date, bool not_null, ref Returns ret)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();
            ret.text = temp;
            DateTime d_date = new DateTime();

            if (s_date == "")
            {
                if (not_null)
                {
                    ret.result = false;
                    ret.text = "Не заполнено обязательное поле даты: ";
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (!DateTime.TryParse(s_date, out d_date))
            {
                ret.result = false;
                ret.text = "Поле имеет неверный формат. Значение = " + s_date + ", имя поля: ";
                return "";
            }


            return "\'" + d_date.ToShortDateString() + "\'";
        }

        
        /// <summary>
        /// Проверка на дату 
        /// </summary>
        /// <param name="valuesFromFile"></param>
        /// <param name="i">Номер поля</param>
        /// <param name="not_null">Обязательность поля</param>
        /// <param name="ret">Ошибки</param>
        /// <param name="fieldName">Название поля</param>
        /// <returns></returns>
        public static string CheckDateTime2(DbValuesFromFile valuesFromFile, int i,bool not_null, ref Returns ret, string fieldName)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();
            ret.text = temp;
            DateTime d_date = new DateTime();

            if (valuesFromFile.vals[i] == "")
            {
                if (not_null)
                {
                    ret.result = false;
                    //ret.text = "Не заполнено обязательное поле даты: ";
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Не заполнено обязательное поле даты: " + fieldName);
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (!DateTime.TryParse(valuesFromFile.vals[i], out d_date))
            {
                ret.result = false;
                //ret.text = "Поле имеет неверный формат. Значение = " + s_date + ", имя поля: ";
                valuesFromFile.err.Append(valuesFromFile.rowNumber + "Поле имеет неверный формат. Значение = " + valuesFromFile.vals[i] + ", имя поля: " + fieldName);
                return "";
            }


            return "\'" + d_date.ToShortDateString() + "\'";
        }


        /// <summary>
        /// Проверка текста
        /// </summary>
        /// <param name="s_text"></param>
        /// <param name="not_null">Обязательность поля</param>
        /// <param name="ln">Длина поля</param>
        /// <param name="ret">Ошибки</param>
        /// <returns></returns>
        public static string CheckText(string s_text, bool not_null, int ln, ref Returns ret)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();
            ret.text = temp;
           

            s_text = s_text.Replace(",", ".");
            s_text = s_text.Replace("«", "\"");
            s_text = s_text.Replace("»", "\"");
            s_text = s_text.Replace("'", "\"");
            s_text = s_text.Trim();

            if (s_text == "")
            {
                if (not_null)
                {
                    ret.result = false;
                    ret.text = "Не заполнено обязательное числовое поле: ";
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (s_text.Length > ln)
            {
                ret.result = false;
                ret.text = "Длина текста превышает заданный формат (" + ln + ") поля: ";
                return "";
            }

            return "\'" + s_text.Trim() + "\'";

        }

        /// <summary>
        /// Проверка текста
        /// </summary>
        /// <param name="valuesFromFile"></param>
        /// <param name="i">Номер поля</param>
        /// <param name="not_null">Обязательность поля</param>
        /// <param name="ln">Длина поля</param>
        /// <param name="ret">Ошибки</param>
        /// <param name="fieldName">Название поля</param>
        /// <returns></returns>
        public static string CheckText2(DbValuesFromFile valuesFromFile, int i, bool not_null, int ln, ref Returns ret, string fieldName)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();
            ret.text = temp;
             //string s_text = valuesFromFile.vals[i];
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace(",", ".");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("«", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("»", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("'", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("*", "\"");
            if(fieldName != "Дом")
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("/", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace(@"\", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Trim();

            if (valuesFromFile.vals[i] == "")
            {
                if (not_null)
                {
                    //ret.result = false;
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Не заполнено обязательное числовое поле: " + fieldName);
                    //ret.text = "Не заполнено обязательное числовое поле: ";
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (valuesFromFile.vals[i].Length > ln)
            {
                //ret.result = false;
                //ret.text = "Длина текста превышает заданный формат (" + ln + ") поля: ";
                valuesFromFile.err.Append(valuesFromFile.rowNumber + " Длина текста превышает заданный формат (" + ln + ") поля: " + fieldName);
                return "";
            }
            
            return "\'" + valuesFromFile.vals[i].Trim() + "\'";

        }
        /// <summary>
        /// Проверка текста
        /// </summary>
        /// <param name="valuesFromFile"></param>
        /// <param name="i">Номер поля</param>
        /// <param name="not_null">Обязательность поля</param>
        /// <param name="ln">Длина поля</param>
        /// <param name="ret">Ошибки</param>
        /// <param name="fieldName">Название поля</param>
        /// <returns></returns>
        /// надо функцию обобщить с тзр индивидуального поведения
        public static string CheckText3(DbValuesFromFile valuesFromFile, int i, bool not_null, int ln, ref Returns ret, string fieldName)
        {
            string temp = ret.text;
            ret = Utils.InitReturns();
            ret.text = temp;
            //string s_text = valuesFromFile.vals[i];
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace(",", ".");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("«", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("»", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("'", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("*", "\"");
           // valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace("/", "\"");
            //valuesFromFile.vals[i] = valuesFromFile.vals[i].Replace(@"\", "\"");
            valuesFromFile.vals[i] = valuesFromFile.vals[i].Trim();

            if (valuesFromFile.vals[i] == "")
            {
                if (not_null)
                {
                    //ret.result = false;
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Не заполнено обязательное числовое поле: " + fieldName);
                    //ret.text = "Не заполнено обязательное числовое поле: ";
                    return "";
                }
                else
                {
                    return "null";
                }
            }


            if (valuesFromFile.vals[i].Length > ln)
            {
                //ret.result = false;
                //ret.text = "Длина текста превышает заданный формат (" + ln + ") поля: ";
                valuesFromFile.err.Append(valuesFromFile.rowNumber + " Длина текста превышает заданный формат (" + ln + ") поля: " + fieldName);
                return "";
            }
            
            return "\'" + valuesFromFile.vals[i].Trim() + "\'";

        }
    

    }

    public class RelationParams
    {
        public int NzpFile;
        public string ChildTbl;
        public string ParentTbl;
        public string ChildFieldRelation;
        public string ParentFieldRelation;
        public string ChildFieldLog;
        public string ChildLogName;
        public string ChildRelationName;
        public string ErrMessage;
        public StringBuilder Err;

        public RelationParams(int nzp_file, StringBuilder err)
        {
            this.NzpFile = nzp_file;
            ChildTbl = "";
            ParentTbl = "";
            ChildFieldRelation = "";
            ParentFieldRelation = "";
            ChildFieldLog = "";
            ChildLogName = "";
            ChildRelationName = "";
            ErrMessage = "";
            this.Err = new StringBuilder();
            if(err!=null){this.Err = err;}
        }
    }   

    public class Check : DataBaseHeadServer
    {
        /// <summary>
        /// Функция проверки уникальности юр.лиц
        /// </summary>
        /// <param name="connDb"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CheckUniqUrlic(IDbConnection connDb, FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            string sql;

            try
            {
                //Вытаскиваем дубли во временную таблицу
                sql =
                    " SELECT urlic_id " +
                    " INTO TEMP t1 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic " +
                    " WHERE nzp_file = " + finder.nzp_file +
                    " GROUP BY 1 " +
                    " HAVING count(*) > 1 " +
                    " ORDER BY urlic_id";
                DBManager.ExecSQL(connDb, sql, true);

                //Получаем дополнительную информацию о дублях, выбираем минимальный id у дублей 
                sql =
                    " SELECT MIN(u.id) as id, u.urlic_id, u.urlic_name " +
                    " INTO TEMP t2 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic u, t1 t" +
                    " WHERE u.urlic_id = t.urlic_id " +
                    " AND u.nzp_file = " + finder.nzp_file +
                    " GROUP BY 2, 3";
                DBManager.ExecSQL(connDb, sql, true);

                //Проставляем признак банка(is_bank = 1) юр. лицам с кодами из таблицы t2
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic " +
                    " SET is_bank = 1 " +
                    " WHERE id IN(SELECT id FROM t2)";
                DBManager.ExecSQL(connDb, sql, true);

                //Удаляем дубль
                sql =
                    " DELETE FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic " +
                    " WHERE urlic_id IN(" +
                    "   SELECT urlic_id FROM t2)" +
                    " AND id NOT IN(" +
                    "       SELECT id FROM t2) " +
                    " AND nzp_file = " + finder.nzp_file;
                DBManager.ExecSQL(connDb, sql, true);
            }
            catch (Exception)
            {
                MonitorLog.WriteLog("Ошибка проверки уникальности юр.лиц" +
                                    Environment.NewLine, MonitorLog.typelog.Info, true);
            }
            finally
            {
                sql =
                    " DROP TABLE IF EXISTS t1";
                DBManager.ExecSQL(connDb, sql, false);

                sql =
                    " DROP TABLE IF EXISTS t2";
                DBManager.ExecSQL(connDb, sql, false);
            }

            return ret;
        }

        /// <summary>
        /// Функция проверки того, что ЮР лицо является управляющей компанией
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CheckIsUk(IDbConnection conn_db, FilesImported finder, StringBuilder err)
        {
            Returns ret = Utils.InitReturns();
            string sql;
            string urliclist = "";

            try
            {
                //проеверяем есть ли ЮР лица у которых не выставлен признак УК во 2й секции (при наличие соответствущего кода юр. лица у ЛС)
                sql =
                    " SELECT DISTINCT(fu.urlic_id) as urlic" +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " + Points.Pref +
                        DBManager.sUploadAliasRest + "file_urlic fu " +
                    " WHERE fk.nzp_file = " + finder.nzp_file +
                    " AND fk.nzp_file = fu.nzp_file " +
                    " AND fk.id_urlic_pass_dom Is NOT NULL " +
                    " AND fk.id_urlic_pass_dom = fu.urlic_id " +
                    " AND fu.is_area <> 1";
                var dt = ClassDBUtils.OpenSQL(sql, conn_db);

                foreach (DataRow rr in dt.GetData().Rows)
                {
                   urliclist += String.Concat(rr["urlic"], " ");
                }

                if (dt.GetData().Rows.Count > 0)
                {
                    err.Append("ЮР лицам (" + urliclist + ") не проставлен признак того, что они являются управляющими компаниями(секция 2, поле 18)." 
                        + Environment.NewLine);
                    return new Returns(false, "Юридическим лицам не проставлен признак того, что они являются управляющими компаниями", -1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке данных в CheckIsUk: " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            }

            return ret;
        }

        /// <summary>
        /// Проверка уникальности номеров документа жильцов Больше не будет использоваться
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CheckUniqPasp(IDbConnection conn_db, FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            string sql;

            try
            {
                sql =
                    " SELECT serij, nomer, count(*) " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec " +
                    " WHERE nzp_file = " + finder.nzp_file +
                    " GROUP BY 1,2 " +
                    " HAVING count(*) > 1";
                DataTable dt = ClassDBUtils.OpenSQL( sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                if (dt.Rows.Count > 0)
                {
                    sql ="Имеются жильцы с одинаковыми номерами документа, документы для таких жильцов не будут сохранены." +Environment.NewLine;
                   
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                }
                foreach (DataRow rr in dt.Rows)
                {
                    //Если дубли имеются - проставляем значение null для полей serij, nomer
                    sql =
                        " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec " +
                        " SET serij = null, nomer = null " +
                        " WHERE serij = trim('" + rr["serij"].ToString() + "')" +
                        " AND nomer = trim('" + rr["nomer"].ToString() + "')";
                    DBManager.ExecSQL(conn_db, null, sql, true);
                }                              
            }
            catch 
            {
                MonitorLog.WriteLog("Ошибка удаления дублей в паспортных данных " +
                        Environment.NewLine, MonitorLog.typelog.Info, true);
                //return new Returns(false, "Ошибка выполнения функции: CheckUniqPasp", -1);
            }
            return ret;
        }
        
        
        /// <summary>
        /// Проверка уникальности оплат
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="err"></param>
        public Returns CheckOplats(IDbConnection conn_db, FilesImported finder) 
        {
            Returns ret = Utils.InitReturns();
            string sql;
            string str;
            try
            {               
                sql =
                    " SELECT * " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_pack p " +
               //     Points.Pref + DBManager.sUploadAliasRest + "file_oplats o " +
                    " WHERE p.sum_plat <> ( " +
                    "   SELECT SUM(o.sum_oplat) " +
                    "   FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats o " +
                    "   WHERE o.nzp_file = p.nzp_file " +
                    "   AND o.nzp_pack = p.id) " +
                    " AND p.nzp_file = " + finder.nzp_file;

                int cnt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count;
                if (cnt > 0)
                {
                    str =
                        "Сумма оплат по пачке в таблице file_pack не соответствуют сумме в file_oplats, данные не загружены" +
                        Environment.NewLine;
                    MonitorLog.WriteLog(str, MonitorLog.typelog.Error, true);
                   
                    return new Returns(false, "Сумма оплат по пачке в таблице file_pack не соответствуют сумме в file_oplats", -1);
                }

                sql =
                    " SELECT * " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_pack p " +
               //     Points.Pref + DBManager.sUploadAliasRest + "file_oplats o " +
                    " WHERE p.kol_plat <> ( " +
                    "   SELECT COUNT(*) " +
                    "   FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats o " +
                    "   WHERE o.nzp_file = p.nzp_file " +
                    "   AND o.nzp_pack = p.id) " +
                    " AND p.nzp_file = " + finder.nzp_file;

               int count = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count;
                if (count > 0)
                {
                    str ="Количество оплат по пачке в таблице file_pack не соответствуют количеству квитанций в file_oplats ";
                   
                    MonitorLog.WriteLog(str + Environment.NewLine, MonitorLog.typelog.Error, true);
                    return new Returns(false, "Количество оплат по пачке в таблице file_pack не соответствуют количеству квитанций в file_oplats", -1);
                }
            }
            catch (Exception ex)
            {

                MonitorLog.WriteLog("Ошибка при проверке уникальности строк в функции CheckOplats: " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            return ret;
        }
        /// <summary>
        /// Проверка существования кода перекидки в справочнике
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public Returns CheckKodPerekidki(IDbConnection conn_db, FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            string sql;

            try
            {
                sql =
                    " SELECT * " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_perekidki p " +
                    " WHERE  NOT exists (" +
                    "  SELECT type_rcl " +
                    "  FROM " + finder.bank + "_kernel" + tableDelimiter + "s_typercl a where a.type_rcl=p.id_type)" +
                    "  AND p.nzp_file = " + finder.nzp_file ;

                int cnt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count;

                if (cnt > 0)
                {
                    sql = "Имеются коды типов перекидок в кол-ве: " + cnt + ", которых нет в базе, данные не загружены" + Environment.NewLine;
                  
                    MonitorLog.WriteLog(sql, MonitorLog.typelog.Error, true);
                    return new Returns(false, "Имеются коды типов перекидок, которых нет в базе", -1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке правильности типов перекидки в функции CheckKodPerekidki: " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            return ret;
        }
        /// <summary>
        /// Функция проверки уникальности информации о лицевых счетах, принадлежащих прибору учета
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CheckInfoPu(IDbConnection conn_db, FilesImported finder, StringBuilder err)
        {
            Returns ret = Utils.InitReturns();
            string sql;

            try
            {
                sql =
                    " SELECT p.id_pu " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_info_pu p " +
                    " WHERE not exists (" +
                    "  SELECT o.local_id " +
                    "  FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu o " +
                    "  WHERE o.type_pu = 2 or o.type_pu = 3" +
                    "  AND o.nzp_file = " + finder.nzp_file + " and p.id_pu=o.local_id )" +
                    " AND p.nzp_file = " + finder.nzp_file;

                int cnt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count;

                if (cnt > 0)
                {
                    err.Append("Имеются коды ПУ(34я секциия) в кол-ве: " + cnt +
                               ", которых нет в базе(9я секция), данные не загружены" + Environment.NewLine);
                    MonitorLog.WriteLog("Имеются коды ПУ(34я секция) в кол-ве: " + cnt + ", которых нет в базе, данные не загружены" + Environment.NewLine, MonitorLog.typelog.Error, true);
                    return new Returns(false, "Имеются коды ПУ(34я секция), которых нет в базе", -1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке уникальности строк в функции CheckInfoPu: " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            }

            try
            {
                sql =
                    " SELECT * " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_info_pu p " +
                    " WHERE not exists (" +
                    "  SELECT k.id " +
                    "  FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar k" +
                    "  WHERE k.nzp_file = " + finder.nzp_file + " and p.num_ls_pu=k.id )" +
                    " AND p.nzp_file = " + finder.nzp_file;

                int cnt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count;

                if (cnt > 0)
                {
                    err.Append("Имеются номера ЛС, относящиеся к ПУ(34я секция), в кол-ве: " + cnt + ", которых нет в базе(4я секция), данные не загружены" + Environment.NewLine);
                    MonitorLog.WriteLog("Имеются номера ЛС, относящиеся к ПУ, в кол-ве: " + cnt + ", которых нет в базе, данные не загружены" + Environment.NewLine, MonitorLog.typelog.Error, true);
                    return new Returns(false, "Имеются номера ЛС, относящиеся к ПУ, которых нет в базе", -1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке уникальности строк в функции CheckInfoPu: " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
                  
            return ret;
        }

        /// <summary>
        /// Проверка уникальности данных
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="err"></param>
        public void CheckUnique(IDbConnection conn_db, FilesImported finder, StringBuilder err)
        {
            Returns ret = Utils.InitReturns();
            try
            {

                #region 2 file_area
                ret = CheckOneUnique(conn_db, finder, err, "file_area", "id", " управляющие компании ");
                #endregion

                #region 2 file_urlic
                ret = CheckOneUnique(conn_db, finder, err, "file_urlic", "urlic_id", "юридические лица"," and is_bank=0 ");
                #endregion

                #region 3 file_dom
                ret = CheckOneUnique(conn_db, finder, err, "file_dom", "id", "дома");
                #endregion

                #region 4 file_kvar
                ret = CheckOneUnique(conn_db, finder, err, "file_kvar", "id", "квартиры");
                #endregion

                #region 5 file_supp
                ret = CheckOneUnique(conn_db, finder, err, "file_supp", "supp_id", "поставщики");
                #endregion

                #region 5 file_dog
                ret = CheckOneUnique(conn_db, finder, err, "file_dog", "dog_id", "договора на оказание ЖКУ");
                #endregion

                #region 9 file_odpu
                ret = CheckOneUnique(conn_db, finder, err, "file_odpu", "local_id", "ОДПУ");
                #endregion

                #region 11 file_ipu
                ret = CheckOneUnique(conn_db, finder, err, "file_ipu", "local_id", "ИПУ");
                #endregion

                #region 14 file_mo
                ret = CheckOneUnique(conn_db, finder, err, "file_mo", "id_mo", "МО");
                #endregion

                #region 16 file_typeparams
                ret = CheckOneUnique(conn_db, finder, err, "file_typeparams", "id_prm", "выгруженные параметры");
                #endregion

                #region 17 file_gaz
                ret = CheckOneUnique(conn_db, finder, err, "file_gaz", "id_prm", "выгруженные типы домов по газоснабжению");
                #endregion

                #region 18 file_voda
                ret = CheckOneUnique(conn_db, finder, err, "file_voda", "id_prm", "выгруженные типы домов по водоснабжению");
                #endregion

                #region 19 file_blag
                ret = CheckOneUnique(conn_db, finder, err, "file_blag", "id_prm", "выгруженные категории благоустройства");
                #endregion

                #region 24 file_typenedopost
                ret = CheckOneUnique(conn_db, finder, err, "file_typenedopost", "type_ned", "типы недопоставок");
                #endregion

                #region 26 file_pack
                ret = CheckOneUnique(conn_db, finder, err, "file_pack", "id", "пачки реестров");
                #endregion

                /*#region 33 file_raspr
                ret = CheckOneUnique(conn_db, finder, err, "file_raspr", "kod_oplat", "уникальный код оплаты");
                #endregion*/


            }
            catch
            {
                err.Append("Ошибка при проверке уникальности строк в функцие CheckUnique " + Environment.NewLine);
            }
        }

        /// <summary>
        /// Проверка уникальности одной таблицы
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="err"></param>
        /// <param name="table_name"></param>
        /// <param name="id_name"></param>
        /// <param name="errField"></param>
        /// <returns></returns>
        private Returns CheckOneUnique(IDbConnection conn_db, FilesImported finder, StringBuilder err, string table_name,string id_name, string errField )
        {
            Returns ret = Utils.InitReturns();
            
            ret = CheckOneUnique(conn_db, finder, err, table_name, id_name, errField, "");
             return ret;
        }
        // добавить параметр условие 
        private Returns CheckOneUnique(IDbConnection conn_db, FilesImported finder, StringBuilder err, string table_name, string id_name, string errField, string strwhere ="")
        {
            Returns ret = Utils.InitReturns();
            try
            {
                string sql;
#if PG
                sql = "set search_path to '" + Points.Pref + "_upload'";
#else
                sql =  "database " + Points.Pref + "_upload";
#endif
                ret = ExecSQL(conn_db, sql, true);
                sql = "drop table " + Points.Pref + DBManager.sUploadAliasRest + "t_unique";
                ret = ExecSQL(conn_db, sql, false);
                sql = "select " + id_name + " as id_name, count(*) as kol " +
#if PG
 " into unlogged t_unique " +
#else
#endif
 "from " + Points.Pref + DBManager.sUploadAliasRest + "" + table_name +
                    " where nzp_file = " + finder.nzp_file + strwhere+
                    " group by 1" +
                    " having count(*)>1 " +
#if PG
#else
                    " into temp t_unique "+
#endif
 "";
                ret = ExecSQL(conn_db, sql, true);
                sql = "select * from " + Points.Pref + DBManager.sUploadAliasRest + "t_unique";
                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                if (Convert.ToInt32(dt.resultData.Rows.Count) > 0)
                {
                    err.Append("Обнаружена ошибка входных данных. Имеются " + errField + " с одинаковым уникальным номером в количестве " + Convert.ToInt32(dt.resultData.Rows.Count) + "." + Environment.NewLine);
                    err.Append(String.Format("{0,30}|{1,30}|{2}", "Уникальный код", "Количество строк", Environment.NewLine));

                    foreach (DataRow rr in dt.GetData().Rows)
                    {
                        string testMePls = String.Format("{0,30}|{1,30}|{2}", rr["id_name"].ToString().Trim(), rr["kol"].ToString().Trim(), Environment.NewLine);
                        err.Append(testMePls);
                    }

                    if (table_name.Trim() == "file_kvar")
                    {
                        sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar set nzp_status = 1 where id in (select id_name from " + Points.Pref + DBManager.sUploadAliasRest + "t_unique) and nzp_file =" + finder.nzp_file;
                        ret = ExecSQL(conn_db, sql, true);
                    }
                }
            }
            catch (Exception ex)
            {
                err.Append("Ошибка при проверке уникальности строк в функции CheckOneUnique " + Environment.NewLine);
                MonitorLog.WriteLog("Ошибка функции CheckOneUnique: " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            return ret;
        }

        /// <summary>
        /// Функция проверки связности загружаемых таблиц
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public Returns CheckRelation(IDbConnection conn_db, FilesImported finder, StringBuilder err)
        
        {
            Returns ret = Utils.InitReturns();
            try 
            {
                #region 1. Выбираем квартиры без домов

                CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_dom", "id", "dom_id", "id", "id", "Номер ЛС", "Уникальный номер дома", Points.Pref.ToString(), "квартиры без домов");
                #endregion Выбираем квартиры без домов

                #region 2. Выбираем услуги без квартир

                CheckOneRelation(conn_db, finder, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_kvar", "id", "ls_id", "id", "nzp_serv", "Код услуги", "Номер ЛС", Points.Pref.ToString(), "услуги без квартир");
                #endregion Выбираем услуги без квартир

                #region 3. Выбираем жильцов без квартир

                CheckOneRelation(conn_db, finder, err, "file_gilec", "num_ls, id", "file_kvar", "id", "num_ls", "id", "nzp_gil", "Уникальный номер гражданина", "Номер ЛС", Points.Pref.ToString(), "жильцы без квартир");
                #endregion Выбираем жильцов без квартир

                #region 4. Выбираем ИПУ без квартир

                CheckOneRelation(conn_db, finder, err, "file_ipu", "local_id, id", "file_kvar", "id", "ls_id", "id", "num_cnt", "Заводской номер ПУ", "Номер ЛС", Points.Pref.ToString(), "ИПУ без квартир");
                #endregion Выбираем ИПУ без квартир

                #region 5. Выбираем показания ИПУ без ИПУ

                CheckOneRelation(conn_db, finder, err, "file_ipu_p", "id_ipu, id", "file_ipu", "local_id, id", "id_ipu", "local_id", "dat_uchet", "Дата показания", "уникальный код ПУ", Points.Pref.ToString(), "показания ИПУ без ИПУ");
                #endregion Выбираем пересчет ИПУ без ИПУ

                #region 6. Выбираем перерасчеты квартиры без квартир
                //Выбираем параметры квартиры без квартиры //??????????????????????????????????????

                //CheckOneRelation(conn_db, finder, err, "file_kvarp", "id", "file_kvar", "id", "nzp_kvar", "id", "reval_month", "Дата перерасчета", "Номер ЛС", Points.Pref.ToString(), "перерасчеты квартиры без квартир");

                CheckOneRelation(conn_db, finder, err, "file_kvarp", "id", "file_kvar", "id", "id", "id", "reval_month", "Дата перерасчета", "Номер ЛС", Points.Pref.ToString(), "перерасчеты квартиры без квартир");
                #endregion Выбираем перерасчеты квартиры без квартир

                //Выбираем параметры квартиры без квартиры

                #region 7. Выбираем услуги без единицы измерения

                CheckOneRelation(conn_db, finder, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "nzp_serv", "Код услуги", "Единица измерения", Points.Pref.ToString(), "услуги без единицы измерения");
                #endregion Выбираем услуги без единицы измерения

                #region 8. Выбираем дома без МО

                CheckOneRelation(conn_db, finder, err, "file_dom", "id", "file_mo", "id_mo, id", "mo_id", "id_mo", "id", "Уникальный код дома", "Уникальный код МО", Points.Pref.ToString(), "дома без МО");
                #endregion Выбираем дома без МО

                #region 9. Выбираем ОДПУ без дома

                string sql =
                " SELECT trim(local_id) as local_id FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu" +
                " WHERE dom_id = '-1' AND nzp_file = " + finder.nzp_file + " AND type_pu = 2";
                DataTable odpu = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                if (odpu.Rows.Count > 0)
                {
                }
                else
                {
                    CheckOneRelation(conn_db, finder, err, "file_odpu", "local_id, id", "file_dom", "id", "dom_id", "id",
                        "num_cnt", "Заводской номер ПУ", "Уникальный код дома", Points.Pref.ToString(), "ОДПУ без домов");
                }

                #endregion Выбираем общедомовые приборы учета без дома

                #region 10. Выбираем ОДПУ без единиц измерения

                CheckOneRelation(conn_db, finder, err, "file_odpu", "local_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "num_cnt", "Заводской номер ПУ", "Код единицы измерения", Points.Pref.ToString(), "ОДПУ без единиц измерения");
                #endregion Выбираем ОДПУ без единиц измерения

                #region 11. Выбираем оплаты без квартир

                CheckOneRelation(conn_db, finder, err, "file_oplats", "ls_id, id", "file_kvar", "id", "ls_id", "id", "numplat", "Номер платежного документа", "Номер ЛС", Points.Pref.ToString(), "оплаты без квартир");
                #endregion Выбираем оплаты без квартир

                #region 12. Выбираем параметры дома без дома

                //CheckOneRelation(conn_db, finder, err, "file_paramsdom", "id_dom, id", "file_dom", "id", "id_dom", "id", "id_prm", "Код параметра", "Уникальный код дома", Points.Pref.ToString(), "параметры дома без дома");
                CheckOneRelation(conn_db, finder, err, "file_paramsdom", "id_dom, id", "file_dom", "id", "id_dom", "local_id", "id_prm", "Код параметра", "Уникальный код дома", Points.Pref.ToString(), "параметры дома без дома");
                #endregion Выбираем параметры дома без дома

                #region 13. Выбираем параметры ЛС без квартиры

                CheckOneRelation(conn_db, finder, err, "file_paramsls", "ls_id, id", "file_kvar", "id", "ls_id", "id", "id_prm", "Код параметра", "Номер ЛС", Points.Pref.ToString(), "параметры ЛС без квартир");
                #endregion Выбираем параметры ЛС без квартиры

                #region 14. Выбираем параметры услуг без квартир

                CheckOneRelation(conn_db, finder, err, "file_servp", "ls_id, id", "file_kvar", "id", "ls_id", "id", "reval_month", "Дата перерасчета", "Номер ЛС", Points.Pref.ToString(), "параметры услуг без квартир");
                #endregion Выбираем параметры услуг без квартир

                #region 16. Выбираем недопоставки без квартир

                CheckOneRelation(conn_db, finder, err, "file_nedopost", "type_ned, id", "file_kvar", "id", "ls_id", "id", "type_ned", "Тип недопоставки", "Номер ЛС", Points.Pref.ToString(), "недопоставки без квартир");
                #endregion Выбираем недопоставки без квартир

                #region 17. Выбираем недопоставки без услуги

                CheckOneRelation(conn_db, finder, err, "file_nedopost", "type_ned, id", "file_services", "id_serv,id", "id_serv", "id_serv", "type_ned", "Тип недопоставки", "Код услуги", Points.Pref.ToString(), "недопоставки без услуг");
                #endregion Выбираем недопоставки без услуги

                #region 18. Выбираем недопоставки без типа недопоставки

                CheckOneRelation(conn_db, finder, err, "file_nedopost", "type_ned, id", "file_typenedopost", "type_ned, id", "type_ned", "type_ned", "dat_nedstart",
                                                        "Дата начала недопоставки", "Тип недопоставки", Points.Pref.ToString(), "недопоставки без типа недопоставки");
                #endregion Выбираем недопоставки без типа недопоставки

                #region 19. Выбираем временно убывших без квартир

                CheckOneRelation(conn_db, finder, err, "file_vrub", "ls_id, id", "file_kvar", "id", "ls_id", "id", "gil_id", "Уникальный код гражданина", "Номер ЛС", Points.Pref.ToString(), "временно убывшие без квартир");
                #endregion Выбираем временно убывших без квартир


                #region 21. Выбираем показания ОДПУ без ОДПУ

                CheckOneRelation(conn_db, finder, err, "file_odpu_p", "id_odpu, id", "file_odpu", "local_id, id", "id_odpu", "local_id", "dat_uchet", "Дата учета", "Код ОДПУ", Points.Pref.ToString(), "показания ОДПУ без ОДПУ");
                #endregion Выбираем показания ОДПУ без ОДПУ



                #region 23. Выбираем ОДПУ без кода услуги

                CheckOneRelation(conn_db, finder, err, "file_odpu", "local_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "num_cnt", "Заводской номер ПУ", "Код услуги", Points.Pref.ToString(), "ОДПУ без кода услуги");
                #endregion Выбираем общедомовые приборы учета без кода услуги

                #region 24. Выбираем услуги, не входящие в перечень выгруженных услуг

                CheckOneRelation(conn_db, finder, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "ls_id", "Номер лицевого счета в системе поставщика", "Код услуги", Points.Pref.ToString(), "услуги, не входящие в перечень выгруженных услуг, ");
                
                
                //CheckOneRelation(conn_db, finder, err, "file_services", "id_serv,  id", "file_serv", "ls_id, nzp_serv, nzp_measure, id", "id_serv", "nzp_serv",  "service", "Наименование услуги", "Код услуги", Points.Pref.ToString(), "услуги, не входящие в перечень выгруженных услуг, ");
                #endregion Выбираем услуги без поставщиков

                #region 23. Выбираем ИПУ без кода услуги

                //CheckOneRelation(conn_db, finder, err, "file_ipu", "local_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "num_cnt", "Заводской номер ПУ", "Код услуги", Points.Pref.ToString(), "ИПУ без кода услуги");
                CheckOneRelation(conn_db, finder, err, "file_ipu", "local_id, id", "file_services", "id_serv,  id", "kod_serv", "id_serv", "num_cnt", "Заводской номер ПУ", "Код услуги", Points.Pref.ToString(), "ИПУ без кода услуги");
                #endregion Выбираем ИПУ без кода услуги

                #region 24. Выбираем квартиры без кода типа по газоснабжению

                CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_gaz", "id_prm, id", "gas_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по газоснабжению");
                #endregion Выбираем квартиры без кода типа по газоснабжению

                #region 25. Выбираем квартиры без кода типа по водоснабжению

                CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_voda", "id_prm, id", "water_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по водоснабжению");
                #endregion Выбираем квартиры без кода типа по водоснабжению

                #region 26. Выбираем квартиры без кода типа по горячему водоснабжению

                CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_voda", "id_prm, id", "hotwater_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по горячему водоснабжению");
                #endregion Выбираем квартиры без кода типа по горячему водоснабжению

                #region 27. Выбираем квартиры без кода типа по канализации

                CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_voda", "id_prm, id", "canalization_type", "id_prm", "id", "Номер ЛС", "Код параметра", Points.Pref.ToString(), "квартиры без кода типа по канализации");
                #endregion Выбираем квартиры без кода типа по канализации

                #region 27. Выбираем квартиры без кода ЮЛ

                CheckOneRelation(conn_db, finder, err, "file_kvar", "id", "file_urlic", "supp_id, id", "id_urlic", "urlic_id", "id", "Номер ЛС", "Код ЮЛ", Points.Pref.ToString(), "квартиры без кода ЮЛ");
                #endregion Выбираем квартиры без кода ЮЛ

                #region 28. Выбираем ИПУ без единиц измерения

                CheckOneRelation(conn_db, finder, err, "file_ipu", "local_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "num_cnt", "Заводской номер ПУ", "Код единицы измерения", Points.Pref.ToString(), "ИПУ без единиц измерения");
                #endregion Выбираем ИПУ без единиц измерения

                /* #region 29. Выбираем оплаты без номера пачки

                CheckOneRelation(conn_db, finder, err, "file_oplats", "ls_id, id", "file_pack", "num_plat,id", "nzp_pack", "num_plat", "numplat", "Номер платежного документа", "Номер пачки", Points.Pref.ToString(), "оплаты без номера пачки");

                #endregion Выбираем оплаты без номера пачки */

                #region 29. Выбираем оплаты без номера пачки

                CheckOneRelation(conn_db, finder, err, "file_oplats", "ls_id, id", "file_pack", "num_plat,id", "nzp_pack", "id", "kod_oplat", "уникальный код оплаты", "Номер пачки", Points.Pref.ToString(), "оплаты без номера пачки");

                #endregion Выбираем оплаты без номера пачки
               

                #region 30. Выбираем услуги ЛС без ЛС

                CheckOneRelation(conn_db, finder, err, "file_servls", "ls_id, id", "file_kvar", "id", "ls_id", "id", "id_serv", "Код услуги", "Номер ЛС", Points.Pref.ToString(), "услуги ЛС без ЛС");
                #endregion Выбираем услуги ЛС без ЛС

                #region 32. Выбираем перерасчеты начислений по услугам без услуг

                CheckOneRelation(conn_db, finder, err, "file_servp", "ls_id, id", "file_services", "id_serv,  id", "nzp_serv", "id_serv", "reval_month", "Дата перерасчета", "Уникальный код услуги", Points.Pref.ToString(), "перерасчеты начислений по услугам без услуг");
                #endregion Выбираем перерасчеты начислений по услугам без услуг

                #region 33. Выбираем перерасчеты начислений по услугам без единиц измерения

                CheckOneRelation(conn_db, finder, err, "file_servp", "ls_id, id", "file_measures", "id_measure, id", "nzp_measure", "id_measure", "reval_month", "Дата перерасчета", "Уникальный код услуги", Points.Pref.ToString(), "перерасчеты начислений по услугам без единиц измерения");
                #endregion Выбираем перерасчеты начислений по услугам без единиц измерения

                //    if (finder.format_name == "'1.3.3'" || finder.format_name == "'1.3.4'" || finder.format_name == "'1.3.5'")
                if (!(finder.format_name == "'1.2.1'" || finder.format_name == "'1.2.2'" || finder.format_name == "'1.0'"
                    || finder.format_name == "'1.3.2'" || finder.format_name == "'1.3.1'"))
                {

                    //объект для передачи параметров  
                    RelationParams prms = new RelationParams(finder.nzp_file, err);


                    #region 34. Выбираем оказываемые услуги без договоров

                    prms.ChildTbl = "file_serv";
                    prms.ParentTbl = "file_dog";
                    prms.ChildFieldRelation = "dog_id";
                    prms.ParentFieldRelation = "dog_id";
                    prms.ChildFieldLog = "nzp_serv";
                    prms.ChildLogName = "Код услуги";
                    prms.ChildRelationName = "Номер договора";
                    prms.ErrMessage = "оказываемые услуги без договоров";
                    CheckOneRelation(prms);

                    // CheckOneRelation(conn_db, finder, err, "file_serv", "nzp_serv, dog_id", "file_dog", "id_agent, id_urlic_p, id_supp", "dog_id", "dog_id", "nzp_serv", "Код услуги", "Номер договора", Points.Pref.ToString(), "оказываемые услуги без договоров");

                    #endregion Выбираем оказываемые услуги без договоров 

                    #region 35. Выбираем реестр ЛС без информации о ЛС 

                    prms.ChildTbl = "file_reestr_ls";
                    prms.ParentTbl = "file_kvar";
                    prms.ChildFieldRelation = " ls_id_ns ";
                    prms.ParentFieldRelation = " id ";
                    prms.ChildFieldLog = "ls_pkod"; 
                    prms.ChildLogName = "Реестр ЛС";
                    prms.ChildRelationName = "Информация о ЛС";
                    prms.ErrMessage = "реестр ЛС без информации о ЛС";
                   
                    CheckOneRelation(prms);  // не проверять 

                    //CheckOneRelation(conn_db, finder, err, "file_reestr_ls", "ls_id_supp", "file_kvar", "id", "ls_id_supp", "id", "ls_id_supp", "Реестр ЛС", "Информация о ЛС", Points.Pref.ToString(), "реестр ЛС без информации о ЛС");

                    #endregion Выбираем реестр ЛС без информации о ЛС

                    #region 36. Выбираем номер ЛС без уникального кода жильца квартиросъемщика

                    prms.ChildTbl = "file_kvar";
                    prms.ParentTbl = "file_gilec";
                    prms.ChildFieldRelation = "id_gil";
                    prms.ParentFieldRelation = "nzp_gil";
                    prms.ChildFieldLog = "id";
                    prms.ChildLogName = "Номер ЛС";
                    prms.ChildRelationName = "Уникальный код жильца квартиросъемщика";
                    prms.ErrMessage = "номер ЛС без уникального кода жильца квартиросъемщика";
                    CheckOneRelation(prms);

                    // CheckOneRelation(conn_db, finder, err, "file_gilec", "id_gil, id", "file_kvar", "nzp_gil, num_ls", "nzp_gil", "id_gil", "nzp_gil", "Информация по проживающим", "Информация о ЛС", Points.Pref.ToString(), "информация по проживающим без информации о ЛС");

                    #endregion Выбираем номер ЛС без уникального кода жильца квартиросъемщика

                    #region 37. Выбираем информацию о ЛС без информации о ЮЛ

                    prms.ChildTbl = "file_kvar";
                    prms.ParentTbl = "file_urlic";
                    prms.ChildFieldRelation = "id_urlic_pass_dom";
                    prms.ParentFieldRelation = "urlic_id";
                    prms.ChildFieldLog = "id";
                    prms.ChildLogName = "Информация о ЛС";
                    prms.ChildRelationName = "Информация о ЮЛ";
                    prms.ErrMessage = "информация о ЛС без информации о ЮЛ";
                    CheckOneRelation(prms);

                    //CheckOneRelation(conn_db, finder, err, "file_kvar", "id, id_urlic_pass_dom", "file_urlic", "urlic_id", "id_urlic_pass_dom", "urlic_id", "id", "Информация о ЛС", "Информация о ЮЛ", Points.Pref.ToString(), "информациюя о ЛС без информации о ЮЛ");

                    #endregion Выбираем информацию о ЛС без информации о ЮЛ


                    #region 38. Выбираем код договора без кода агента получателя платежей-РЦ 

                    prms.ChildTbl = "file_dog";
                    prms.ParentTbl = "file_urlic";
                    prms.ChildFieldRelation = "id_agent";
                    prms.ParentFieldRelation = "urlic_id";
                    prms.ChildFieldLog = "dog_id";
                    prms.ChildLogName = "код договора";
                    prms.ChildRelationName = "код агента получателя платежей-РЦ";
                    prms.ErrMessage = "код договора без кода агента получателя платежей-РЦ";
                    CheckOneRelation(prms);

                    //CheckOneRelation(conn_db, finder, err, "file_dog", "dog_id, id_agent", "file_urlic", "urlic_id", "id_agent", "urlic_id", "id_agent", "код агента получателя платежей-РЦ", "код ЮЛ", Points.Pref.ToString(), "код агента получателя платежей-РЦ без кода ЮЛ");

                    #endregion Выбираем код договора без кода агента получателя платежей-РЦ

                    #region 39. Выбираем код договора без кода ЮЛ принципиала

                    prms.ChildTbl = "file_dog";
                    prms.ParentTbl = "file_urlic";
                    prms.ChildFieldRelation = "id_urlic_p";
                    prms.ParentFieldRelation = "urlic_id";
                    prms.ChildFieldLog = "dog_id";
                    prms.ChildLogName = "код договора";
                    prms.ChildRelationName = "код ЮЛ принципала";
                    prms.ErrMessage = "код договора без кода ЮЛ принципиала";
                    CheckOneRelation(prms);

                    //CheckOneRelation(conn_db, finder, err, "file_dog", "dog_id, id_agent, id_urlic_p", "file_urlic", "urlic_id", "id_urlic_p", "urlic_id", "id_urlic_p", 
                    // "код ЮЛ принципала", "код ЮЛ", Points.Pref.ToString(), "код ЮЛ принципала без кода ЮЛ");

                    #endregion Выбираем код договора без кода ЮЛ принципиала

                    #region 40. Выбираем код договора без кода поставщика, который оказывает ЖКУ

                    prms.ChildTbl = "file_dog";
                    prms.ParentTbl = "file_urlic";
                    prms.ChildFieldRelation = "id_supp";
                    prms.ParentFieldRelation = "urlic_id";
                    prms.ChildFieldLog = "dog_id";
                    prms.ChildLogName = "код договора";
                    prms.ChildRelationName = "код поставщика,который оказывает ЖКУ";
                    prms.ErrMessage = "код договора без кода поставщика, который оказывает ЖКУ";
                    CheckOneRelation(prms);

                    //CheckOneRelation(conn_db, finder, err, "file_dog", "dog_id, id_supp", "file_urlic", "urlic_id", "id_supp", "urlic_id", "id_supp", 
                    //    "код поставщика,который оказывает ЖКУ", "код ЮЛ", Points.Pref.ToString(), "код поставщика,который оказывает ЖКУ без кода ЮЛ");

                    #endregion Выбираем код поставщика, который оказывает ЖКУ без кода ЮЛ

                    #region 41. Выбираем код ЮЛ без уникального кода банка

                    prms.ChildTbl = "file_rs";
                    prms.ParentTbl = "file_urlic";
                    prms.ChildFieldRelation = "id_bank";
                    prms.ParentFieldRelation = "urlic_id";
                    prms.ChildFieldLog = "id_urlic";
                    prms.ChildLogName = "код ЮЛ";
                    prms.ChildRelationName = "Уникальный код банка";
                    prms.ErrMessage = "код ЮЛ без уникального кода банка";
                    CheckOneRelation(prms);

                    //CheckOneRelation(conn_db, finder, err, "file_rs", "id_bank", "file_urlic", "urlic_id", "id_bank", "urlic_id", "id_bank", 
                    //    "Расчетный счет", "Уникальный код банка", Points.Pref.ToString(), "расчетный счет без банка");

                    #endregion Выбираем расчетный счет без банка

                    #region 42. Выбираем уникальный код банка без кода ЮЛ

                    prms.ChildTbl = "file_rs";
                    prms.ParentTbl = "file_urlic";
                    prms.ChildFieldRelation = "id_urlic";
                    prms.ParentFieldRelation = "urlic_id";
                    prms.ChildFieldLog = "id_bank";
                    prms.ChildLogName = "Уникальный код банка";
                    prms.ChildRelationName = "код ЮЛ";
                    prms.ErrMessage = "уникальный код банка без кода ЮЛ";
                    CheckOneRelation(prms);

                    //CheckOneRelation(conn_db, finder, err, "file_rs", "id_urlic, rs", "file_urlic", "urlic_id", "id_urlic", "urlic_id", "id_urlic", 
                    //    "Расчетный счет", "ЮЛ", Points.Pref.ToString(), "расчетный счет без ЮЛ");

                    #endregion Выбираем уникальный код банка без кода ЮЛ

                    #region 43. Выбираем соглашения по перечислениям без информации о договорах на оказание ЖКУ

                    prms.ChildTbl = "file_agreement";
                    prms.ParentTbl = "file_dog";
                    prms.ChildFieldRelation = "id_dog";
                    prms.ParentFieldRelation = "dog_id";
                    prms.ChildFieldLog = "id_dog";
                    prms.ChildLogName = "Уникальный код договора";
                    prms.ChildRelationName = "код дома в системе отправителя";
                    prms.ErrMessage = "соглашения по перечислениям без информации о договорах на оказание ЖКУ";
                    CheckOneRelation(prms);

                    #endregion Выбираем соглашения по перечислениям без информации о договорах на оказание ЖКУ

                    #region 44. Выбираем уникальный код агента получателя комиссии без уникального кода ЮЛ

                    prms.ChildTbl = "file_agreement";
                    prms.ParentTbl = "file_urlic";
                    prms.ChildFieldRelation = "id_urlic_agent";
                    prms.ParentFieldRelation = "urlic_id";
                    prms.ChildFieldLog = "id_dog";
                    prms.ChildLogName = "уникальный код договора";
                    prms.ChildRelationName = "код агента получателя комиссии";
                    prms.ErrMessage = "уникальный код агента получателя комиссии без уникального кода ЮЛ";
                    CheckOneRelation(prms);

                    #endregion Выбираем уникальный код агента получателя комиссии без уникального кода ЮЛ

                    #region 45. Информация о перерасчетах начислений по услугам без информации о договорах на оказание ЖКУ

                    prms.ChildTbl = "file_servp";
                    prms.ParentTbl = "file_dog";
                    prms.ChildFieldRelation = "dog_id";
                    prms.ParentFieldRelation = "dog_id";
                    prms.ChildFieldLog = "nzp_serv";
                    prms.ChildLogName = "Информация о перерасчетах начислений по услугам";
                    prms.ChildRelationName = "Информация о договорах на оказание ЖКУ";
                    prms.ErrMessage = "Информация о перерасчетах начислений по услугам без информации о договорах на оказание ЖКУ";
                    CheckOneRelation(prms);

                    // CheckOneRelation(conn_db, finder, err, "file_gilec", "id_gil, id", "file_kvar", "nzp_gil, num_ls", "nzp_gil", "id_gil", "nzp_gil", "Информация по проживающим", "Информация о ЛС", Points.Pref.ToString(), "информация по проживающим без информации о ЛС");

                    #endregion Информация о перерасчетах начислений по услугам без информации о договорах на оказание ЖКУ

                    #region 46. Информация о договорах без информации о расчетном счете

                    prms.ChildTbl = "file_dog";
                    prms.ParentTbl = "file_rs";
                    prms.ChildFieldRelation = "rs";
                    prms.ParentFieldRelation = "rs";
                    prms.ChildFieldLog = "dog_id";
                    prms.ChildLogName = "Уникальный код договора на оказание ЖКУ";
                    prms.ChildRelationName = "Расчетный счет";
                    prms.ErrMessage = "Информация о договорах без информации о расчетном счете";
                    CheckOneRelation(prms);
                    
                    #endregion Информация о договорах без информации о расчетном счете

                    #region 31. Выбираем услуги ЛС без договора

                    CheckOneRelation(conn_db, finder, err, "file_servls", "ls_id, id", "file_dog", "dog_id", "supp_id", "dog_id", "id_serv", "Код услуги", "Код договора", Points.Pref.ToString(), "услуги ЛС без договора");
                    #endregion Выбираем услуги ЛС без договора

                    #region 47. Информация о распределение оплат без информации о перечне выгруженных услуг

                    prms.ChildTbl = "file_raspr";
                    prms.ParentTbl = "file_services";
                    prms.ChildFieldRelation = "id_serv";
                    prms.ParentFieldRelation = "id_serv";
                    prms.ChildFieldLog = "kod_oplat";
                    prms.ChildLogName = "Уникальный код оплаты";
                    prms.ChildRelationName = "код услуги";
                    prms.ErrMessage = "Информация о распределение оплат без информации о перечне выгруженных услуг";
                    CheckOneRelation(prms);

                    #endregion Информация о распределение оплат без информации о перечне выгруженных услуг

                    #region 48. Информация о распределение оплат без информации о договорах на оказание ЖКУ

                    prms.ChildTbl = "file_raspr";
                    prms.ParentTbl = "file_dog";
                    prms.ChildFieldRelation = "id_dog";
                    prms.ParentFieldRelation = "dog_id";
                    prms.ChildFieldLog = "kod_oplat";
                    prms.ChildLogName = "Уникальный код оплаты";
                    prms.ChildRelationName = "код договора";
                    prms.ErrMessage = "Информация о распределение оплат без информации о договорах на оказание ЖКУ";
                    CheckOneRelation(prms);

                    #endregion Информация о распределение оплат без информации о договорах на оказание ЖКУ  
                  
                   #region 49. Информация о распределение оплат без информации о перечне оплат проведенных по ЛС

                    prms.ChildTbl = "file_raspr";
                    prms.ParentTbl = "file_oplats";
                    prms.ChildFieldRelation = "kod_oplat";
                    prms.ParentFieldRelation = "kod_oplat";
                    prms.ChildFieldLog = "id_dog";
                    prms.ChildLogName = "код договора";
                    prms.ChildRelationName = "Уникальный код оплаты";
                    prms.ErrMessage = "Информация о распределение оплат без информации о перечни оплат проведенных по ЛС";
                    CheckOneRelation(prms);

                    #endregion Информация о распределение оплат без информации о перечне оплат проведенных по ЛС 

                    #region 50. Информация о перекидки без информации о ЛС

                    prms.ChildTbl = "file_perekidki";
                    prms.ParentTbl = "file_kvar";
                    prms.ChildFieldRelation = "id_ls";
                    prms.ParentFieldRelation = "id";
                    prms.ChildFieldLog = "dog_id";
                    prms.ChildLogName = "код договора";
                    prms.ChildRelationName = "номер ЛС в системе поставщика";
                    prms.ErrMessage = "Информация о перекидки без информации о ЛС";
                    CheckOneRelation(prms);

                    #endregion Информация о перекидки без информации о ЛС

                    #region 51. Информация о перекидки без информации о перечне выгруженных услуг

                    prms.ChildTbl = "file_perekidki";
                    prms.ParentTbl = "file_services";
                    prms.ChildFieldRelation = "id_serv";
                    prms.ParentFieldRelation = "id_serv";
                    prms.ChildFieldLog = "dog_id";
                    prms.ChildLogName = "код договора";
                    prms.ChildRelationName = "код услуги";
                    prms.ErrMessage = "Информация о перекидки без информации о перечне выгруженных услуг";
                    CheckOneRelation(prms);

                    #endregion Информация о перекидки без информации о перечне выгруженных услуг

                    #region 52. Информация о перекидки без информации о договорах на оказание ЖКУ

                    prms.ChildTbl = "file_perekidki";
                    prms.ParentTbl = "file_dog";
                    prms.ChildFieldRelation = "dog_id";
                    prms.ParentFieldRelation = "dog_id";
                    prms.ChildFieldLog = "id_ls";
                    prms.ChildLogName = "номер ЛС в системе поставщика";
                    prms.ChildRelationName = "код договора";
                    prms.ErrMessage = "Информация о перекидки без информации о договорах на оказание ЖКУ";
                    CheckOneRelation(prms);

                    #endregion Информация о перекидки без информации о договорах на оказание ЖКУ

                    #region 53. Информация о перечени оплат проведенных по ЛС без информации перечени принятых пачек платежей ЛС

                    prms.ChildTbl = "file_oplats";
                    prms.ParentTbl = "file_pack";
                    prms.ChildFieldRelation = "nzp_pack";
                    prms.ParentFieldRelation = "id";
                    prms.ChildFieldLog = "kod_oplat";
                    prms.ChildLogName = "уникальный код оплаты";
                    prms.ChildRelationName = "уникальный номер пачки оплат";
                    prms.ErrMessage = "Информация о перечне оплат проведенных по ЛС без информации перечени принятых пачек платежей ЛС";
                    CheckOneRelation(prms);

                    #endregion Информация о перечени оплат проведенных по ЛС без информации перечени принятых пачек платежей ЛС


                }
                else
                {
                    #region 31. Выбираем услуги ЛС без поставщика

                    CheckOneRelation(conn_db, finder, err, "file_servls", "ls_id, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "id_serv", "Код услуги", "Код поставщика", Points.Pref.ToString(), "услуги ЛС без поставщика");
                    #endregion Выбираем услуги ЛС без поставщика

                    #region 22. Выбираем дома без УК

                    CheckOneRelation(conn_db, finder, err, "file_dom", "id", "file_area", "id", "area_id", "id", "id", "Уникальный код дома", "Уникальный код УК", Points.Pref.ToString(), "дома без УК");
                    #endregion Выбираем дома без УК

                    #region 15. Выбираем параметры услуг без поставщиков 

                    CheckOneRelation(conn_db, finder, err, "file_servp", "ls_id, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "reval_month", "Дата перерасчета", "Уникальный код поставщика", Points.Pref.ToString(), "параметры услуг без поставщиков");
                    #endregion Выбираем параметры услуг без поставщиков

                    #region 20. Выбираем услуги без поставщиков

                    CheckOneRelation(conn_db, finder, err, "file_serv", "ls_id, nzp_serv, nzp_measure, id", "file_supp", "supp_id, id", "supp_id", "supp_id", "nzp_serv", "Код услуги", "Код поставщика", Points.Pref.ToString(), "услуги без поставщиков");
                    #endregion Выбираем услуги без поставщиков

                }

            }
            catch
            {
                err.Append("Ошибка при проверке несвязности таблиц " + Environment.NewLine);
            }
            return ret;
        }

        //Функция проверки связности конкретной пары таблиц
        ///<summary>
        /// создание индексов и проверка связности для двух таблиц
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="err"></param>
        /// <param name="childTbl">название дочерней таблицы</param>
        /// <param name="doch_field_for_index">поле, по которому будет создаваться индекс дочерней таблицы</param>
        /// <param name="rodit_tbl">название родительской таблицы</param>
        /// <param name="rodit_field_for_index">поле, по которому будет создаваться индекс родительской таблицы</param>
        /// <param name="doch_field_relation"> поле дочерней таблицы для связи с родительской</param>
        /// <param name="rodit_field_relation">поле родительской таблицы для связи с дочерней</param>
        /// <param name="doch_field_log"></param>
        /// <param name="field1_name"></param>
        /// <param name="feild2_name"></param>
        /// <param name="pref"></param>
        /// <param name="errMessage"></param>
        /// <returns></returns>
        private void CheckOneRelation(
            IDbConnection conn_db, FilesImported finder, StringBuilder err, string childTbl, string doch_field_for_index, 
            string rodit_tbl, string rodit_field_for_index, string doch_field_relation, string rodit_field_relation, string doch_field_log, 
            string field1_name, string feild2_name, string pref, string errMessage)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                string sql;
#if PG
                ret = ExecSQL(conn_db, " SET search_path TO '" + pref.Trim() + "_upload'", true);
#else
                ret = ExecSQL(conn_db, " DATABASE " + pref.Trim() + "_upload", true);
#endif

                ret = ExecSQL(conn_db, " Create index ix1_" + childTbl.Trim() + " on " + pref.Trim() + DBManager.sUploadAliasRest + "" + childTbl.Trim() + " (" + doch_field_for_index.Trim() + ")", false);
                if (ret.result)
                {
                    ExecSQL(conn_db, DBManager.sUpdStat + " " + pref.Trim() + DBManager.sUploadAliasRest + "" + childTbl.Trim(), false);
                }
                ret = ExecSQL(conn_db, " Create index ix1_" + rodit_tbl.Trim() + " on " + pref.Trim() + DBManager.sUploadAliasRest + "" + rodit_tbl.Trim() + " (" + rodit_field_for_index.Trim() + ")", false);
                if (ret.result)
                {
                    ExecSQL(conn_db, DBManager.sUpdStat + "  " + pref.Trim() + DBManager.sUploadAliasRest + "" + rodit_tbl.Trim(), false);
                }

                sql = 
                    "select distinct a." + doch_field_log.Trim() + " as field1, a." + doch_field_relation.Trim() + " as field2 " + 
                    " from " + pref.Trim() + DBManager.sUploadAliasRest + "" + childTbl.Trim() + " a " +
                    " where a.nzp_file =" + finder.nzp_file + " and a." + doch_field_relation.Trim() + " is not null and " +
                    " not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + DBManager.sUploadAliasRest + "" + rodit_tbl.Trim() + " b" +
#if PG
                    " where cast( b." + rodit_field_relation.Trim() + " as varchar(100))= cast (a." + doch_field_relation.Trim() + " as varchar(100)) " +
#else
 " where  b." + rodit_field_relation.Trim() + "= a." + doch_field_relation.Trim() + " " +
#endif
                    " and b.nzp_file =" + finder.nzp_file + ")";

                MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);

                var dt = ClassDBUtils.OpenSQL(sql, conn_db);


                if (dt.resultData.Rows.Count > 0)
                {
                    err.Append("Обнаружена несвязность данных. Имеются " + errMessage + " в количестве " + dt.resultData.Rows.Count + "." + Environment.NewLine);
                    err.Append(String.Format("{0,30}|{1,30}|{2}", field1_name, feild2_name, Environment.NewLine));

                    foreach (DataRow rr in dt.GetData().Rows)
                    {
                        string testMePls = String.Format("{0,30}|{1,30}|{2}", rr["field1"].ToString().Trim(), rr["field2"].ToString().Trim(), Environment.NewLine);
                        err.Append(testMePls);
                    }

                    //для лицевых счетов при несвязности меняем nzp_status
                    if (childTbl.Trim() == "file_kvar")
                    {
                        sql = "update " + pref.Trim() + DBManager.sUploadAliasRest + "" + childTbl.Trim() +
                              " set nzp_status = 2 " +
                              " where nzp_file =" + finder.nzp_file +
                              " and not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + DBManager.sUploadAliasRest + "" + rodit_tbl.Trim() + " b" +
                              " where b." + rodit_field_relation.Trim() + " = " + doch_field_relation.Trim() +
                              " and b.nzp_file =" + finder.nzp_file + ") and nzp_status is null";
                        ExecSQL(conn_db, sql, true);
                    }
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке несвязности таблиц в функции CheckRelation при проверке имеются ли " + errMessage + ex.Message + ex.TargetSite, MonitorLog.typelog.Error, true);
            }
            return;
        }

        private void CheckOneRelation(RelationParams prms)
        {
            try
            {
                string sql;
#if PG
                ExecSQL(" SET search_path TO '" + Points.Pref + "_upload'", true);
#else
                ExecSQL(" DATABASE " + Points.Pref.Trim() + "_upload", true);
#endif

                sql =
                    " SELECT distinct a." + prms.ChildFieldLog.Trim() + " as field1, " +
                    "                 a." + prms.ChildFieldRelation.Trim() + " as field2 " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "" + prms.ChildTbl.Trim() + " a " +
                    " WHERE a.nzp_file =" + prms.NzpFile +
                    "   AND a." + prms.ChildFieldRelation.Trim() + " is not null  " +
                    "   AND not exists " +
                    "            (  SELECT b." + prms.ParentFieldRelation.Trim() +
                    "               FROM " + Points.Pref + DBManager.sUploadAliasRest + "" + prms.ParentTbl.Trim() + " b" +
                    "               WHERE cast( b." + prms.ParentFieldRelation.Trim() + " as varchar(100)) = cast (a." + prms.ChildFieldRelation.Trim() + " as varchar(100)) " +
                    "               AND b.nzp_file =" + prms.NzpFile + ")";
                
                DataTable dt = OpenSQL(sql).resultData;

                if (dt.Rows.Count > 0)
                {
                    prms.Err.Append("Обнаружена несвязность данных. Имеются " + prms.ErrMessage + " в количестве " + dt.Rows.Count + "." + Environment.NewLine);
                    prms.Err.Append(String.Format("{0,40}|{1,40}|{2}", prms.ChildLogName, prms.ChildRelationName, Environment.NewLine));

                    foreach (DataRow rr in dt.Rows)
                    {
                        prms.Err.Append(String.Format("{0,40}|{1,40}|{2}", rr["field1"].ToString().Trim(), rr["field2"].ToString().Trim(), Environment.NewLine));
                    }

                    //для лицевых счетов при несвязности меняем nzp_status
                    if (prms.ChildTbl.Trim() == "file_kvar")
                    {
                        sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "" + prms.ChildTbl.Trim() +
                              " set nzp_status = 2 " +
                              " where nzp_file =" + prms.NzpFile +
                              " and not exists (select b." + prms.ParentFieldRelation.Trim() + " from " + Points.Pref + DBManager.sUploadAliasRest + "" + prms.ParentTbl.Trim() + " b" +
                              " where b." + prms.ParentFieldRelation.Trim() + " = " + prms.ChildFieldRelation.Trim() +
                              " and b.nzp_file =" + prms.NzpFile + ") and nzp_status is null";
                        ExecSQL(sql, true);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке несвязности таблиц в функции CheckRelation при проверке имеются ли " + prms.ErrMessage + ex.Message + ex.TargetSite, MonitorLog.typelog.Error, true);
            }
            return;
        }

        /// <summary>
        /// Функция проверки качества данных 6 секции
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public Returns Check6Section(IDbConnection conn_db, FilesImported finder, StringBuilder err)
        {
            Returns ret = Utils.InitReturns();
            string sql = "";
            string columnName = "supp_id";
            string messageColumnNAme = "Код поставщика";

            if (finder.format_name != "'1.3.1'" || finder.format_name != "'1.3.0'" || finder.format_name.Substring(1,4) != "'1.2")
            {
                columnName = "dog_id";
                messageColumnNAme = "Код договора";
            }


            try
            {
                sql = " SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                      " WHERE abs(sum_insaldo + sum_nach + sum_reval + sum_perekidka - sum_money - sum_outsaldo) > 0.01 " +
                      " AND  nzp_file = " + finder.nzp_file;
                var dt1 = ClassDBUtils.OpenSQL(sql, conn_db);

                if (dt1.resultData.Rows.Count > 0)
                {
                    err.Append("Результат проверки исходящего сальдо." + Environment.NewLine);

                    string str = String.Format(@"{0,20}|{1,20}|{2,20}|{3,20}|{4,20}|{5,20}|{6,20}|{7,20}|{8,20}|", "Лицевые счета", messageColumnNAme, "Код услуги", "Входящее сальдо",
                    "Сумма начисления", "Сумма перерасчета", "Сумма перекидки", "Сумма оплаты", "Исходящее сальдо");
                    str += Environment.NewLine;
                    err.Append(str);

                    foreach (DataRow rr in dt1.GetData().Rows)
                    {
                        str = String.Format(@"{0,20}|{1,20}|{2,20}|{3,20}|{4,20}|{5,20}|{6,20}|{7,20}|{8,20}|", rr["ls_id"].ToString().Trim(),
                            rr[columnName].ToString().Trim(), rr["nzp_serv"].ToString().Trim(), rr["sum_insaldo"].ToString().Trim(), rr["sum_nach"].ToString().Trim(),
                            rr["sum_reval"].ToString().Trim(), rr["sum_perekidka"].ToString().Trim(), rr["sum_money"].ToString().Trim(),
                            rr["sum_outsaldo"].ToString().Trim());
                        str += Environment.NewLine;
                        err.Append(str);
                    }
                }

                sql = "select ls_id, " + columnName + ", nzp_serv, eot, reg_tarif, fact_rashod, norm_rashod from " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                    " where nzp_file = " + finder.nzp_file +
                    " and (eot = 0 or reg_tarif = 0 or fact_rashod = 0 or fact_rashod = 0)";

                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                if (dt.resultData.Rows.Count > 0)
                {
                    err.Append("Результат проверки качества данных 6 секции." + Environment.NewLine);

                    string str = String.Format(@"{0,20}|{1,20}|{2,20}|{3,20}|{4,20}|{5,20}|{6,20}|", "Лицевые счета", messageColumnNAme, "Код услуги", "ЭО тариф",
                    "Рег. тариф", "Расход факт.", "Расход по норм.");
                    str += Environment.NewLine;
                    err.Append(str);

                    foreach (DataRow rr in dt.GetData().Rows)
                    {
                        str = String.Format(@"{0,20}|{1,20}|{2,20}|{3,20}|{4,20}|{5,20}|{6,20}|", rr["ls_id"].ToString().Trim(),
                            rr[columnName].ToString().Trim(), rr["nzp_serv"].ToString().Trim(), rr["eot"].ToString().Trim(), rr["reg_tarif"].ToString().Trim(),
                            rr["fact_rashod"].ToString().Trim(), rr["norm_rashod"].ToString().Trim());
                        str += Environment.NewLine;
                        err.Append(str);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке качества данных 6 секции в функции Check6Section " + ex.Message + ex.TargetSite, MonitorLog.typelog.Error, true);
            }
            return ret;
        }

        /// <summary>
        /// Функция проверки перехода через ноль в показаниях ИПУ
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public Returns CheckIpuP(IDbConnection conn_db, FilesImported finder, StringBuilder err)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                string sql = " select ls_id, local_id from " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu " +
                    " where local_id in " +
                             " (select  a.id_ipu from " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu_p a ," +
                             Points.Pref + DBManager.sUploadAliasRest + "file_ipu_p b," +
                              Points.Pref + DBManager.sUploadAliasRest + "files_imported i " +
                             " where a.id_ipu=b.id_ipu and a.dat_uchet > b.dat_uchet and a.val_cnt < b.val_cnt and" +
                             " i.nzp_file = b.nzp_file and trim(i.pref) = '" + finder.bank + "' and" +
                    " a.nzp_file =" + finder.nzp_file + " )" +
                    " and nzp_file  = " + finder.nzp_file;

                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                if (dt.resultData.Rows.Count > 0)
                {
                    err.Append("В показаниях ИПУ возможно имеется переход через 0. Код таких ИПУ" + Environment.NewLine);
                    string str = String.Format(@"{0,20}|{1,20}|", "Лицевые счета", "Код ИПУ");
                    str += Environment.NewLine;
                    err.Append(str);

                    foreach (DataRow rr in dt.GetData().Rows)
                    {
                        str = String.Format(@"{0,20}|{1,20}|", rr["ls_id"].ToString().Trim(),
                            rr["local_id"].ToString().Trim());
                        str += Environment.NewLine;
                        err.Append(str);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке перехода через ноль показаний ИПУ в CheckIpuP " + ex.Message + ex.TargetSite, MonitorLog.typelog.Error, true);
            }
            return ret;
        }

    }
}
