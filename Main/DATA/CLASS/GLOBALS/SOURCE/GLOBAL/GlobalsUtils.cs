using System;
using System.Text;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System.IO;

namespace STCLINE.KP50.Global
{

    //----------------------------------------------------------------------
    static public class Utils //утилиты
    //----------------------------------------------------------------------
    {
        static public bool ConvertStringToMoney(string s_sum, out decimal d_sum)
        {
            d_sum = 0;
            // non-breaking space, ничего лучше пока не придумала
            s_sum = s_sum.Replace("&#160;", "").Trim();

            // замена разделителя дробной части
            s_sum = s_sum.Replace(",", ".");

            int decSepPos = s_sum.LastIndexOf(".");

            if (decSepPos > -1)
            {
                // получаем дробную часть
                string curr_part = s_sum.Substring(decSepPos + 1);
                // получаем целую часть
                string int_part = s_sum.Substring(0, decSepPos);
                // убираем разделитель групп
                int_part = int_part.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "");
                // оставляем только цифры в дробной части
                curr_part = Regex.Replace(curr_part, @"[^\d]", "");
                // оставляем только цифры в целой части
                int_part = Regex.Replace(int_part, @"[^\d]", "");
                // собираем число
                s_sum = int_part + "." + curr_part;
            }
            else
            {
                // убираем разделитель групп
                s_sum = s_sum.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "");
                // оставляем только цифры
                s_sum = Regex.Replace(s_sum, @"[^\d]", "");
            }

            try
            {
                d_sum = s_sum.Trim() == "" ? 0 : Decimal.Parse(s_sum);
                return true;
            }
            catch
            {
                return false;
            }
        }

        //----------------------------------------------------------------------
        static public string RunFile(string rf)
        //----------------------------------------------------------------------
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = rf;
            proc.EnableRaisingEvents = true;

            //proc.Exited += new EventHandler(proc_Exited);

            try
            {
                proc.Start();
                proc.WaitForExit();

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        //----------------------------------------------------------------------
        static public Returns InitReturns() //инициализация переменной Returns
        //----------------------------------------------------------------------
        {
            Returns ret;
            ret.result = true;
            ret.text = "";
            ret.tag = 0;
            ret.sql_error = "";
            return ret;
        }

        //----------------------------------------------------------------------
        static public void UserLogin(string cons_User, out string Login, out string Password) //вытащить логины
        //----------------------------------------------------------------------
        {
            int l = cons_User.Length;
            int k = cons_User.LastIndexOf(";");

            string[] result = cons_User.Split(new string[] { ";" }, StringSplitOptions.None);

            try
            {
                Login = result[0].Trim();
                Password = result[1].Trim();
            }
            catch
            {
                Login = "";
                Password = "";
            }
        }
        //----------------------------------------------------------------------
        static public string IfmxDatabase(string st) //данные коннекта
        //----------------------------------------------------------------------
        {
            string srv = "";
            string bds = "";
            string usr = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("Database") & (!zap.StartsWith("Database L")))
                {
                    try
                    {
                        srv = result2[1];
                    }
                    catch
                    {
                    }
                }
                else
                    if (zap.StartsWith("Server"))
                    {
                        try
                        {
                            bds = result2[1];
                        }
                        catch
                        {
                        }
                    }
                    else
                        if (zap.StartsWith("UID"))
                        {
                            try
                            {
                                usr = result2[1];
                            }
                            catch
                            {
                            }
                        }
            }

            return bds.Trim() + "@" + srv.Trim() + "  (" + usr.Trim() + ")";
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbDatabase(string st) //данные коннекта FireBird
        //----------------------------------------------------------------------
        {
            string srv = "";
            string bds = "";
            string usr = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("data source"))
                {
                    try
                    {
                        srv = result2[1];
                    }
                    catch
                    {
                    }
                }
                else
                    if (zap.StartsWith("initial catalog"))
                    {
                        try
                        {
                            bds = result2[1];
                        }
                        catch
                        {
                        }
                    }
                    else
                        if (zap.StartsWith("user id"))
                        {
                            try
                            {
                                usr = result2[1];
                            }
                            catch
                            {
                            }
                        }
            }

            return bds.Trim() + ":" + srv.Trim() + "  (" + usr.Trim() + ")";
        }


        //----------------------------------------------------------------------
        static public string GetCorrectFIO(string st)
        //----------------------------------------------------------------------
        {
            if (st.Trim() == "") return st;

            //string[] masStr = st.Split(" ", StringSplitOptions.None);
            char[] delimiterChars = { ' ', ',', '.', ':' };
            string[] masStr = st.Split(delimiterChars);

            int i = 1;
            StringBuilder resStr = new StringBuilder();

            foreach (string st_ in masStr)
            {
                switch (i)
                {
                    case 1: resStr.Append(st_.Trim());
                        break;
                    case 2:
                        if (st_.Trim().Length > 1) resStr.Append(" " + st_.Trim().Substring(0, 1) + ".");
                        break;
                    case 3: if (st_.Trim().Length > 1) resStr.Append(" " + st_.Trim().Substring(0, 1) + ".");
                        break;
                    default:
                        break;
                }
                i++;
            }
            if (i != 4) return st.Trim();
            else return resStr.ToString();

        }

        //----------------------------------------------------------------------
        static public string IfmxGetPref(string kernel) //вытащить префикс
        //----------------------------------------------------------------------
        {
            if (kernel == null) return "";
            int k, l;
            k = kernel.LastIndexOf("_kernel");
            if ((k - 9) > 0) //вызов из ConnectionString
            {
                string s;
                k = kernel.LastIndexOf("_kernel");
                s = kernel.Substring(k - 9, 9);
                l = s.Length;
                k = s.LastIndexOf("=");
                return (s.Substring(k + 1, l - k - 1)).Trim();
            }
            else  //вызов из названии
            {
                return (kernel.Substring(0, k)).Trim();
            }
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbInitialCatalog(string st) //вытащить исходный каталог
        //----------------------------------------------------------------------
        {
            string dir = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("initial catalog"))
                {
                    try
                    {
                        dir = result2[1];
                        break;
                    }
                    catch
                    {
                        return "";
                    }
                }
            }

            if (dir == "") return "";

            string[] result3 = dir.Split(new string[] { "\\\\" }, StringSplitOptions.None);
            string ndir = "";
            int l = result3.Length;

            foreach (string zap in result3)
            {
                if (l != 1) ndir += zap + "\\\\";
                l -= 1;
            }

            return ndir;
        }
        //"data source=MAMBA;initial catalog=D:\\Komplat.Lite\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3051;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";
        //----------------------------------------------------------------------
        static public string FdbChangeDir(string st, string pref) //заменить путь к базе на pref
        //----------------------------------------------------------------------
        {
            string dir = "";

            string[] result = st.Split(new string[] { ";" }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                string[] result2 = zap.Split(new string[] { "=" }, StringSplitOptions.None);
                if (zap.StartsWith("initial catalog"))
                {
                    try
                    {
                        dir = result2[1];
                        break;
                    }
                    catch
                    {
                        return "";
                    }
                }
            }

            if (dir == "") return "";

            string s = st.Replace(dir, pref);
            return s;
        }
        //----------------------------------------------------------------------
        static public bool ValInString(string st_in, string st_val, string st_split)
        //----------------------------------------------------------------------
        {
            string[] result = st_in.Split(new string[] { st_split }, StringSplitOptions.None);
            foreach (string zap in result)
            {
                if (zap.Trim() == st_val.Trim())
                    return true;
            }

            return false;
        }
        //----------------------------------------------------------------------
        static public bool IsInRoleVal(List<_RolesVal> RolesVal, string val, int tip, int kod)
        //----------------------------------------------------------------------
        {
            bool b = true; //по-умолчанию разрешаем доступ

            if (RolesVal != null)
                if (RolesVal.Count > 0)
                {

                    foreach (_RolesVal role in RolesVal)
                    {
                        if (role.tip == tip & role.kod == kod)
                        {
                            b = false; //ограничения определены

                            if (ValInString(role.val, val, ",")) return true;
                        }
                    }
                }

            return b;
        }
        //----------------------------------------------------------------------
        public static string IfmxFormatDatetimeToHour(string datahour, out Returns ret)
        //----------------------------------------------------------------------
        {
            //привести "дд.мм.гггг чч:мм" к формату "гггг-мм-дд ч"
            ret = new Returns(false);
            string outs = "";

            if (String.IsNullOrEmpty(datahour))
            {
                return outs;
            }

            datahour = datahour.Trim();

            string[] mas1 = datahour.Split(new string[] { " " }, StringSplitOptions.None);

            string dt = "";
            string hm = "";
            try
            {
                dt = mas1[0].Trim();
                hm = mas1[1].Trim();

                if (String.IsNullOrEmpty(dt) || String.IsNullOrEmpty(hm))
                {
                    return outs;
                }

                string[] mas2 = dt.Split(new string[] { "." }, StringSplitOptions.None);
                string[] mas3 = hm.Split(new string[] { ":" }, StringSplitOptions.None);

                outs = mas2[2].Trim() + "-" + mas2[1].Trim() + "-" + mas2[0].Trim() + " " + mas3[0].Trim();
                ret.result = true;
            }
            catch
            {
                return outs;
            }

            return outs;
        }
        //----------------------------------------------------------------------
        public static bool is_UNDEF_(string s) //
        //----------------------------------------------------------------------
        {
            return s == Constants._UNDEF_;
        }
        //----------------------------------------------------------------------
        public static string blank_UNDEF_(string s)
        //----------------------------------------------------------------------
        {
            if (is_UNDEF_(s))
                return "";
            else
                return s;
        }

        //----------------------------------------------------------------------
        /// <summary> Подготовка строки для вставки в SQL-запрос (экранирование символов, добавление внешних кавычек)
        /// </summary>
        public static string EStrNull(string s)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, 255, "NULL");
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, byte l)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, l, "NULL");
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, string defaultValue)
        //----------------------------------------------------------------------
        {
            return EStrNull(s, 255, defaultValue);
        }
        //----------------------------------------------------------------------
        public static string EStrNull(string s, int l, string defaultValue)
        //----------------------------------------------------------------------
        {
            if (s == null) s = "";
            else s = s.Trim();
            if (s == "")
            {
                if (defaultValue.ToUpper() == "NULL")
                    return " " + defaultValue + " ";
                else s = defaultValue;
            }
            if (s.Length > l) s = s.Substring(0, l);
#if PG
            return "'" + s.Replace("'", "\"") + "'";
#else
            return "'" + s.Replace("'", "''") + "'";
#endif
        }

        //----------------------------------------------------------------------
        public static int EInt0(string s)
        //----------------------------------------------------------------------
        {
            try
            {
                int i;
                int.TryParse(s, out i);
                return i;
            }
            catch
            {
                return 0;
            }
        }
        //----------------------------------------------------------------------
        public static long ELong0(string s)
        //----------------------------------------------------------------------
        {
            try
            {
                long i;
                Int64.TryParse(s, out i);
                return i;
            }
            catch
            {
                return 0;
            }
        }
        //----------------------------------------------------------------------
        public static string ENull(string s)
        //----------------------------------------------------------------------
        {
            if (s == null)
                return "";
            else return s.Trim();
        }
        //----------------------------------------------------------------------
        public static string EFlo0(decimal f)
        //----------------------------------------------------------------------
        {
            return EFlo0(f.ToString(), "0.00");
        }
        //----------------------------------------------------------------------
        public static string EFlo0(string f)
        //----------------------------------------------------------------------
        {
            return EFlo0(f, "0.00");
        }
        //----------------------------------------------------------------------
        public static string EFlo0(string f, string _default)
        //----------------------------------------------------------------------
        {
            if (f.Trim() == "")
                return _default;
            else
            {
                NumberFormatInfo nfi = new CultureInfo("ru-RU", false).NumberFormat;
                nfi.NumberDecimalSeparator = ".";
                nfi.CurrencyDecimalSeparator = ".";
                double d = Double.Parse(f.Replace(",", ".").Replace(" ", ""), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, nfi);
                return d.ToString("G", nfi);
            }
        }
        //----------------------------------------------------------------------
        public static string FormatDate(string d)
        //----------------------------------------------------------------------
        {
            try
            {
                DateTime dt = DateTime.ParseExact(d, "dd.MM.yyyy", new CultureInfo("ru-RU"));
                return String.Format("{0:dd.MM.yyyy}", dt);
            }
            catch
            {
                return "";
            }
        }

        //----------------------------------------------------------------------
        public static string EDateNull(string d)
        //----------------------------------------------------------------------
        {
            if ((d == null) || (d.Trim() == ""))
            {
                return "null";

            }
            else
            {
                return "'" + d.Trim() + "'";
            }
        }

        //----------------------------------------------------------------------
        public static string FormatDateMDY(string d)
        //----------------------------------------------------------------------
        {
            try
            {
                DateTime dt = DateTime.ParseExact(d, "dd.MM.yyyy", new CultureInfo("ru-RU"));
                return "mdy(" + String.Format("{0:MM,dd,yyyy}", dt) + ")";
            }
            catch
            {
                return "";
            }
        }

        /// <summary> проверяет наличие параметра в строке параметров, а также определяет порядковый номер параметра
        /// </summary>
        public static bool GetParams(string prms, int p)
        {
            int num;
            return GetParams(prms, p.ToString(), out num);
        }
        public static bool GetParams(string prms, string p)
        {
            int num;
            return GetParams(prms, p, out num);
        }
        public static bool GetParams(string prms, int p, out int sequenceNumber)
        {
            return GetParams(prms, p.ToString(), out sequenceNumber);
        }
        public static bool GetParams(string prms, string p, out int sequenceNumber)
        {
            sequenceNumber = -1;
            if (prms == null) return false;

            //Regex reg = new Regex(@",\d+", RegexOptions.IgnoreCase);
            Regex reg = new Regex(@"[^,]+", RegexOptions.IgnoreCase);

            foreach (Match match in reg.Matches(prms))
            {
                sequenceNumber++;
                if (match.Value == p) return true;
            }
            return false;
        }

        /// <summary>Удаляет параметр из строки параметров
        /// </summary>
        /// <param name="prms">Строка с параметрами в формате ,p1,p2,p3,... </param>
        /// <param name="p">Удаляемый параметр</param>
        /// <returns>Измененная строка параметров</returns>
        public static string RemoveParam(string prms, string p)
        {
            if (prms == null) return prms;

            string[] arr = prms.Split(',');
            string res = "";

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != p) res += "," + arr[i];
            }
            return res;
        }

        public class CssClasses
        {
            private List<string> _classes;
            private int _count;

            public CssClasses()
            {
                _classes = new List<string>();
                _count = 0;
            }

            public CssClasses(string classes)
            {
                string[] cls = classes.Trim().Split(' ');
                _classes = cls.ToList<string>();
                _count = _classes.Count;
            }

            public CssClasses AddClass(string className)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (_classes[i] == className) return this;
                }
                _classes.Add(className);
                _count++;
                return this;
            }

            public CssClasses RemoveClass(string className)
            {
                if (className != "")
                {
                    while (_classes.IndexOf(className) >= 0) _classes.Remove(className);
                }
                _count = _classes.Count;
                return this;
            }

            public override string ToString()
            {
                string result = "";
                for (int i = 0; i < _count; i++)
                {
                    result += " " + _classes[i];
                }
                return result.Trim();
            }
        }

        //----------------------------------------------------------------------
        public static int PutIdMonth(int y, int m)
        //----------------------------------------------------------------------
        {
            int i = 0;
            int.TryParse(y.ToString() + m.ToString("00"), out i);
            return i;
        }
        //----------------------------------------------------------------------
        public static void GetIdMonth(int id, ref int y, ref int m)
        //----------------------------------------------------------------------
        {
            GetIdMonth(id.ToString(), ref y, ref m);
        }
        //----------------------------------------------------------------------
        public static void GetIdMonth(string id, ref int y, ref int m)
        //----------------------------------------------------------------------
        {
            try
            {
                y = Convert.ToInt32(id.Substring(0, 4));
                m = Convert.ToInt32(id.Substring(4, 2));
            }
            catch
            {
                y = 0;
                m = 0;
            }
        }
        //----------------------------------------------------------------------
        public static void GetIdMonth(string id, ref RecordMonth rm)
        //----------------------------------------------------------------------
        {
            try
            {
                rm.year_ = Convert.ToInt32(id.Substring(0, 4));
                rm.month_ = Convert.ToInt32(id.Substring(4, 2));
            }
            catch
            {
                rm.year_ = 0;
                rm.month_ = 0;
            }
        }
        //----------------------------------------------------------------------
        public static int GetInt(string s)
        //----------------------------------------------------------------------
        {
            if (s == "")
                return 0;
            else
            {
                int i;
                try
                {
                    i = Convert.ToInt32(s);
                    return i;
                }
                catch { }

                int l = s.Length;
                int k = 1;
                while (k < l)
                {
                    s = s.Substring(0, l - k);
                    try
                    {
                        i = Convert.ToInt32(s);
                        return i;
                    }
                    catch { }

                    k = k + 1;
                }
                return 0;
            }
        }
        //----------------------------------------------------------------------
        // Поиск и выдача целого значения из строкового списка (pFindString) значений разделенных 
        // символьной строкой (pDelimiter) по позиции в списке (pPositionInSpis)
        public static Returns FindKeyFromSpis(string pFindString, string pDelimiter, int pPositionInSpis, out long nzp)
        //----------------------------------------------------------------------
        {
            Returns oResult;

            oResult.result = false;
            oResult.text = string.Empty;
            oResult.tag = 0;
            oResult.sql_error = "";
            nzp = 0;

            if (pPositionInSpis >= 0)
            {
                MatchCollection myFind = Regex.Matches(pFindString, pDelimiter, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                int iposN = 0; int iposK = 0; int iCount = 0;
                foreach (Match nextFind in myFind)
                {
                    iposK = nextFind.Index;
                    if (iCount == pPositionInSpis)
                    {
                        oResult.result = true;
                        oResult.text = pFindString.Substring(iposN, iposK - iposN);
                        break;
                    }
                    iCount++; iposN = iposK + 1;
                }
                int.TryParse(oResult.text, out oResult.tag);
                Int64.TryParse(oResult.text, out nzp);
            }
            return oResult;
        }
        //----------------------------------------------------------------------
        // Поиск и выдача целого значения из строкового списка (pFindString) значений разделенных 
        // символьной строкой (pDelimiter) по позиции в списке (pPositionInSpis)
        public static Returns FindKeyFromSpis(string pFindString, string pDelimiter, int pPositionInSpis)
        //----------------------------------------------------------------------
        {
            long nzp;
            return FindKeyFromSpis(pFindString, pDelimiter, pPositionInSpis, out nzp);
        }
        //----------------------------------------------------------------------
        // Поиск и выдача целого значения из строкового списка (pFindString) значений разделенных 
        // символьной строкой (pDelimiter) по позиции в списке (pPositionInSpis)
        public static Returns FindPosFromSpis(string pFindString, string pDelimiter, long pValInSpis)
        //----------------------------------------------------------------------
        {
            Returns oResult;

            oResult.result = false;
            oResult.text = string.Empty;
            oResult.sql_error = "";
            oResult.tag = 0;

            MatchCollection myFind = Regex.Matches(pFindString, pDelimiter, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            int iposN = 0; int iposK = 0; int iCount = 0;
            foreach (Match nextFind in myFind)
            {
                iposK = nextFind.Index;
                if (pFindString.Substring(iposN, iposK - iposN) == pValInSpis.ToString())
                {
                    oResult.result = true;
                    oResult.tag = iCount;
                    break;
                }
                iCount++; iposN = iposK + 1;
            }
            return oResult;
        }

        //----------------------------------------------------------------------
        public static string GetSN(string sn)
        //----------------------------------------------------------------------
        {
            string[] result = sn.Split(new string[] { "-" }, StringSplitOptions.None);
            sn = "";
            for (int i = 0; i < result.Length; i = i + 1)
            {
                sn += result[i];
            }

            return (sn.Trim()).ToUpper();
        }
        //----------------------------------------------------------------------
        public static Int64 BarcodeCRC10(string barcode)
        //----------------------------------------------------------------------
        {
            char c = Convert.ToChar("0");
            barcode = barcode.PadLeft(9, c);

            int sum = 0;
            string s = "";
            for (int i = 1; i <= barcode.Length; i++)
            {
                s = barcode.Substring(i - 1, 1);
                if (i != 10)
                {
                    if (i % 2 == 0)
                        sum = sum + 3 * Convert.ToInt32(s);
                    else
                        sum = sum + Convert.ToInt32(s);
                }
            }
            s = barcode.Trim() + Convert.ToString((10 - sum % 10) % 10);

            return Convert.ToInt64(s);
        }


        //----------------------------------------------------------------------
        public static Int64 BarcodeCRC13(string barcode)
        //----------------------------------------------------------------------
        {
            char c = Convert.ToChar("0");
            barcode = barcode.PadLeft(12, c);

            int sum = 0;
            string s = "";
            for (int i = 0; i < barcode.Length; i++)
            {
                s = barcode.Substring(i, 1);
                if (i != 12)
                {
                    if (i % 2 == 0)
                        sum = sum + 3 * Convert.ToInt32(s);
                    else
                        sum = sum + Convert.ToInt32(s);
                }
            }
            s = barcode.Trim() + Convert.ToString((10 - sum % 10) % 10);

            return Convert.ToInt64(s);
        }

        //----------------------------------------------------------------------
        public static long EncodePKod(string kod_erc, int num_ls)
        //----------------------------------------------------------------------
        {
            string s = num_ls.ToString();
            char c = '0';
            s = s.PadLeft(9 - kod_erc.Length, c);

            return BarcodeCRC10(kod_erc + s);
        }
        //----------------------------------------------------------------------
        public static Returns DecodePKod(string pkod, out int kod_erc, out int num_ls)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            ret.result = false;
            kod_erc = 0;
            num_ls = 0;

            if (pkod.Length != 10)
            {
                ret.tag = Constants.svc_pk_Format;
                return ret;
            }

            long l;
            if (!Int64.TryParse(pkod, out l))
            {
                ret.tag = Constants.svc_pk_Format;
                return ret;
            }


            string s = pkod.Substring(0, 1);
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (s == "3") //Челны или НКамск
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            {
                if (!int.TryParse(pkod.Substring(0, 2), out kod_erc))
                {
                    ret.tag = Constants.svc_pk_Prefix;
                }
                else
                {
                    if (!int.TryParse(pkod.Substring(2, 7), out num_ls))
                    {
                        ret.tag = Constants.svc_pk_NumLs;
                    }
                }
            }
            else
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                if (s == "2") //Казань, РТ
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                {
                    if (!int.TryParse(pkod.Substring(0, 3), out kod_erc))
                    {
                        ret.tag = Constants.svc_pk_Prefix;
                    }
                    else
                    {
                        if (!int.TryParse(pkod.Substring(3, 6), out num_ls))
                        {
                            ret.tag = Constants.svc_pk_NumLs;
                        }
                    }
                }
                else
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    if (s == "5") //Лайтовцы
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    {
                        if (!int.TryParse(pkod.Substring(0, 4), out kod_erc))
                        {
                            ret.tag = Constants.svc_pk_Prefix;
                        }
                        else
                        {
                            if (!int.TryParse(pkod.Substring(4, 5), out num_ls))
                            {
                                ret.tag = Constants.svc_pk_NumLs;
                            }
                        }
                    }
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    else //не определен префикс
                        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        ret.tag = Constants.svc_pk_Prefix;


            if (ret.tag < 0)
            {
                return ret;
            }

            int k = 0;
            if (!int.TryParse(pkod.Substring(9, 1), out k))
            {
                ret.tag = Constants.svc_pk_Bit;
                return ret;
            }

            Int64 test = EncodePKod(kod_erc.ToString(), num_ls);

            if (test.ToString() != pkod)
            {
                ret.tag = Constants.svc_pk_Bit;
                return ret;
            }

            return Utils.InitReturns(); ;
        }
        //----------------------------------------------------------------------
        public static string GetKontrSamara(string als)
        //----------------------------------------------------------------------
        {

            int sum_mod = 0;
            int i;
            int j;
            int first_k;
            int second_k;
            string ss;

            for (i = 0; i < als.Length; i++)
            {
                ss = als.Substring(i, 1);
                j = Convert.ToInt32(ss);
                if (i % 2 == 0)
                {
                    switch (j)
                    {
                        case 0: sum_mod = sum_mod + 4; break;
                        case 1: sum_mod = sum_mod + 6; break;
                        case 2: sum_mod = sum_mod + 8; break;
                        case 3: sum_mod = sum_mod + 1; break;
                        case 4: sum_mod = sum_mod + 3; break;
                        case 5: sum_mod = sum_mod + 5; break;
                        case 6: sum_mod = sum_mod + 7; break;
                        case 7: sum_mod = sum_mod + 9; break;
                        case 8: sum_mod = sum_mod + 2; break;
                        case 9: sum_mod = sum_mod + 0; break;
                    }
                }
                else sum_mod = sum_mod + j;
            }
            first_k = (10 - sum_mod % 10) % 10;

            sum_mod = 0;

            for (i = 0; i < Math.Min(11, als.Length); i++)
            {
                j = Convert.ToInt16(als.Substring(i, 1));
                switch (i + 1)
                {
                    case 1: sum_mod = sum_mod + j * 6; break;
                    case 2: sum_mod = sum_mod + j * 5; break;
                    case 3: sum_mod = sum_mod + j * 4; break;
                    case 4: sum_mod = sum_mod + j * 3; break;
                    case 5: sum_mod = sum_mod + j * 2; break;
                    case 6: sum_mod = sum_mod + j * 1; break;
                    case 7: sum_mod = sum_mod + j * 1; break;
                    case 8: sum_mod = sum_mod + j * 2; break;
                    case 9: sum_mod = sum_mod + j * 3; break;
                    case 10: sum_mod = sum_mod + j * 4; break;
                    case 11: sum_mod = sum_mod + j * 5; break;
                }
            }

            second_k = (10 - (sum_mod % 10)) % 10;

            return first_k.ToString() + second_k.ToString();

        }


        public static string BarcodeCrcSamara(string acode)
        {
            var sum = 0;

            for (int i = 0; i < acode.Length; i++)
            {
                switch (i + 1)
                {
                    case 1:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 29;
                        break;
                    case 2:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 27;
                        break;
                    case 3:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 25;
                        break;
                    case 4:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 23;
                        break;
                    case 5:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 21;
                        break;
                    case 6:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 19;
                        break;
                    case 7:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 17;
                        break;
                    case 8:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 15;
                        break;
                    case 9:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 13;
                        break;
                    case 10:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 11;
                        break;
                    case 11:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 9;
                        break;
                    case 12:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 7;
                        break;
                    case 13:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 5;
                        break;
                    case 14:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 3;
                        break;
                    case 15:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 1;
                        break;
                    case 16:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 2;
                        break;
                    case 17:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 4;
                        break;
                    case 18:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 6;
                        break;
                    case 19:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 8;
                        break;
                    case 20:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 10;
                        break;
                    case 21:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 12;
                        break;
                    case 22:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 14;
                        break;
                    case 23:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 16;
                        break;
                    case 24:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 18;
                        break;
                    case 25:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 20;
                        break;
                    case 26:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 22;
                        break;
                    case 27:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 24;
                        break;
                    case 28:
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1)) * 26;
                        break;

                }

            }

            String s = (sum % 99).ToString("00");

            return s.Substring(0, 2);

        }

        //----------------------------------------------------------------------
        /// <summary> Установить региональные настройки
        /// </summary>
        public static void setCulture()
        //----------------------------------------------------------------------
        {
            CultureInfo culture = new CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            culture.DateTimeFormat.ShortTimePattern = "HH:mm:ss";
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        public static DataTable ConvertDBFtoDataTable(Stream fs, string codePage, out Returns ret, string filename)
        {
            bool isArchive = false;
            if (filename != null && filename.Contains('.'))
            {
                string extention = Path.GetExtension(filename.ToLower()); //filename.Split('.')[1].ToLower();
                switch (extention)
                {
                    case ".7z":
                    case ".7zip":
                    case ".zip":
                    case ".bz":
                    case ".gzip":
                    case ".tar":
                    case ".xz":
                    case ".rar":
                        isArchive = true;
                        break;
                }
            }
            if (!isArchive)
            {
                return ConvertDBFtoDataTable(fs, codePage, out ret);
            }
            Stream file;
            if (!Directory.Exists(Constants.Directories.ImportAbsoluteDir)) Directory.CreateDirectory(Constants.Directories.ImportAbsoluteDir);
            using (file = File.Create(filename = Path.Combine(Constants.Directories.ImportAbsoluteDir, filename)))
            {
                CopyStream(fs, file);
                file.Close();
            }
            string[] res;
            ret = new Returns(false);

            if (
                (res = Archive.GetInstance(filename).Decompress(filename, Constants.Directories.ImportAbsoluteDir, true))
                    .Any())
                ret = new Returns(true, res.First());

            using (file = File.Open(Path.Combine(Constants.Directories.ImportAbsoluteDir, ret.text), FileMode.Open))

                return ConvertDBFtoDataTable(file, codePage, out ret);
        }

        public static DataTable ConvertDBFtoDataTable(System.IO.Stream fs, out Returns ret)
        {
            return ConvertDBFtoDataTable(fs, "", out ret);
        }

        public static DataTable ConvertDBFtoDataTable(Stream fs, string codePage, out Returns ret)
        {
            return ConvertDBFtoDataTable(fs, codePage, false, out ret);
        }

        /// <summary>
        /// Конвертирование DBF в DataTable
        /// </summary>
        /// <param name="fs">Файловый Поток</param>
        /// <param name="codePage">Кодовая страница, если известна</param>
        /// <param name="suppessFormat">Подавлять формат 0х03</param>
        /// <param name="ret">Результат конвертации</param>
        /// <returns></returns>
        public static DataTable ConvertDBFtoDataTable(Stream fs, string codePage, bool suppessFormat, out Returns ret)
        {
            //Описание формата DBF файлов:
            //  1. http://www.hardline.ru/3/36/687/
            //  2. http://articles.org.ru/docum/dbfall.php
            //      FoxBASE+/dBASE III +, без memo - 0х03
            //      http://www.autopark.ru/ASBProgrammerGuide/DBFSTRUC.HTM#Table_2
            //  3. http://ru.wikipedia.org/wiki/DBF
            DataTable dt = new DataTable();
            ret = InitReturns();

            int tag = 0;
            try
            {
                // определение кодировки файла
                byte[] buffer = new byte[1];
                fs.Position = 0x00;
                fs.Read(buffer, 0, buffer.Length);
                if (buffer[0] != 0x03 && !suppessFormat)
                {
                    ret.result = false;
                    ret.text = "Данный формат DBF файла не поддерживается";
                    ret.tag = -1;
                    return null;
                }

                // определение кодировки файла (взято из http://ru.wikipedia.org/wiki/DBF)
                Encoding encoding;
                if (codePage == "866") encoding = Encoding.GetEncoding(866);
                else if (codePage == "1251") encoding = Encoding.GetEncoding(1251);
                else
                {
                    buffer = new byte[1];
                    fs.Position = 0x1D;
                    fs.Read(buffer, 0, buffer.Length);
                    if (buffer[0] != 0x65 &&    //Codepage_866_Russian_MSDOS
                        buffer[0] != 0x26 &&    //кодовая страница 866 DOS Russian
                        buffer[0] != 0xC9 &&    //Codepage_1251_Russian_Windows
                        buffer[0] != 0x57)    //кодовая страница 1251 Windows ANSI
                    {
                        ret = new Returns(false, "Кодовая страница не задана или не поддерживается", -1);
                        return null;
                    }
                    if (buffer[0] == 0x65 || buffer[0] == 0x26)
                        encoding = Encoding.GetEncoding(866);
                    else
                        encoding = Encoding.GetEncoding(1251);
                }

                buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
                fs.Position = 4;
                fs.Read(buffer, 0, buffer.Length);
                int RowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000);
                buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
                fs.Position = 8;
                fs.Read(buffer, 0, buffer.Length);
                int FieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
                string[] FieldName = new string[FieldCount]; // Массив названий полей
                string[] FieldType = new string[FieldCount]; // Массив типов полей
                byte[] FieldSize = new byte[FieldCount]; // Массив размеров полей
                byte[] FieldDigs = new byte[FieldCount]; // Массив размеров дробной части
                buffer = new byte[32 * FieldCount]; // Описание полей: 32 байтa * кол-во, начиная с 33-го
                fs.Position = 32;
                fs.Read(buffer, 0, buffer.Length);
                int FieldsLength = 0;
                DataColumn col;
                for (int i = 0; i < FieldCount; i++)
                {
                    // Заголовки
                    FieldName[i] = System.Text.Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new char[] { (char)0x00 });
                    FieldType[i] = "" + (char)buffer[i * 32 + 11];
                    FieldSize[i] = buffer[i * 32 + 16];
                    FieldDigs[i] = buffer[i * 32 + 17];
                    FieldsLength = FieldsLength + FieldSize[i];
                    // Создаю колонки
                    switch (FieldType[i])
                    {
                        case "L": dt.Columns.Add(FieldName[i], Type.GetType("System.Boolean")); break;
                        case "D": dt.Columns.Add(FieldName[i], Type.GetType("System.DateTime")); break;
                        case "N":
                            {
                                if (FieldDigs[i] == 0)
                                    dt.Columns.Add(FieldName[i], Type.GetType("System.Int32"));
                                else
                                {
                                    col = new DataColumn(FieldName[i], Type.GetType("System.Decimal"));
                                    col.ExtendedProperties.Add("precision", FieldSize[i]);
                                    col.ExtendedProperties.Add("scale", FieldDigs[i]);
                                    col.ExtendedProperties.Add("length", FieldSize[i] + FieldDigs[i]);
                                    dt.Columns.Add(col);
                                }
                                break;
                            }
                        case "F": dt.Columns.Add(FieldName[i], Type.GetType("System.Double")); break;
                        default:
                            col = new DataColumn(FieldName[i], Type.GetType("System.String"));
                            col.MaxLength = FieldSize[i];
                            dt.Columns.Add(col);
                            break;
                    }
                }
                fs.ReadByte(); // Пропускаю разделитель схемы и данных Должен быть равен 13
                System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("ru-RU", false).DateTimeFormat;
                System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("ru-RU", false).NumberFormat;
                nfi.NumberDecimalSeparator = ".";
                nfi.CurrencyDecimalSeparator = ".";


                buffer = new byte[FieldsLength];
                dt.BeginLoadData();
                //fs.ReadByte(); // Пропускаю стартовый байт элемента данных
                int delPriznak = 0;
                char[] numericValidChars = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };

                for (int j = 0; j < RowsCount; j++)
                {

                    delPriznak = fs.ReadByte(); // Пропускаю стартовый байт элемента данных

                    fs.Read(buffer, 0, buffer.Length);
                    System.Data.DataRow R = dt.NewRow();
                    int Index = 0;



                    for (int i = 0; i < FieldCount; i++)
                    {

                        string l = encoding.GetString(buffer, Index, FieldSize[i]).TrimEnd(new char[] { (char)0x00, (char)0x20 });
                        Index = Index + FieldSize[i];

                        if (l != "")
                            switch (FieldType[i])
                            {
                                case "L": R[i] = l == "T" ? true : false; break;
                                case "D":
                                    try
                                    {
                                        R[i] = DateTime.ParseExact(l, "yyyyMMdd", dfi);
                                    }
                                    catch
                                    {
                                        tag = -1;
                                        throw new Exception("Ожидалась дата в формате ГГГГММДД в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + l);
                                    }
                                    break;
                                case "N":
                                    {
                                        l = l.Trim().Replace(",", ".");
                                        string val = "";
                                        foreach (char c in l.ToCharArray()) if (numericValidChars.Contains(c)) val += c; else break;

                                        if (FieldDigs[i] == 0)
                                        {
                                            try
                                            {
                                                R[i] = int.Parse(val, nfi);
                                            }
                                            catch
                                            {
                                                tag = -1;
                                                throw new Exception("Ожидалось целое число в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + l);
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                R[i] = decimal.Parse(val, nfi);
                                            }
                                            catch
                                            {
                                                tag = -1;
                                                throw new Exception("Ожидалось вещественное число в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + l);
                                            }
                                        }
                                        break;
                                    }
                                case "F": R[i] = double.Parse(l.Trim(), nfi); break;
                                default: R[i] = l; break;
                            }
                        else
                            R[i] = DBNull.Value;
                    }
                    if (delPriznak == 32)
                        dt.Rows.Add(R);
                }
                dt.EndLoadData();
                fs.Close();
                return dt;
            }
            catch (Exception e)
            {
                ret.result = false;
                if (tag < 0) ret.text = e.Message;
                else ret.text = "Ошибка конвертации DBF файла";
                ret.tag = tag;
                MonitorLog.WriteLog("Ошибка конвертации DBF файла: " + e.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }

        /// <summary>
        /// Конвертирование DBF в DataTable
        /// </summary>
        /// <param name="fs">Файловый Поток</param>
        /// <param name="codePage">Кодовая страница, если известна</param>
        /// <param name="suppessFormat">Подавлять формат 0х03</param>
        /// <param name="ret">Результат конвертации</param>
        /// <returns></returns>
        public static DataTable ConvertDBFtoDataTableFox(Stream fs, string codePage, bool suppessFormat, out Returns ret)
        {
        //Описание формата DBF файлов:
        //  1. http://www.hardline.ru/3/36/687/
        //  2. http://articles.org.ru/docum/dbfall.php
        //      Visual FoxPro, без memo - 0х30
        //     http://www.autopark.ru/ASBProgrammerGuide/DBFSTRUC.HTM#Table_2
        //  3. http://ru.wikipedia.org/wiki/DBF
            DataTable dt = new DataTable();
            ret = InitReturns();

            int tag = 0;
            try
            {
                // определение кодировки файла
                byte[] buffer = new byte[1];
                fs.Position = 0x00;
                fs.Read(buffer, 0, buffer.Length);
                if (buffer[0] != 0x30 && !suppessFormat)
                {
                    ret.result = false;
                    ret.text = "Данный формат DBF файла не поддерживается";
                    ret.tag = -1;
                    return null;
                }

                // определение кодировки файла (взято из http://ru.wikipedia.org/wiki/DBF)
                Encoding encoding;
                if (codePage == "866") encoding = Encoding.GetEncoding(866);
                else if (codePage == "1251") encoding = Encoding.GetEncoding(1251);
                else
                {
                    buffer = new byte[1];
                    fs.Position = 0x1D;
                    fs.Read(buffer, 0, buffer.Length);
                    if (buffer[0] != 0x65 &&    //Codepage_866_Russian_MSDOS
                        buffer[0] != 0x26 &&    //кодовая страница 866 DOS Russian
                        buffer[0] != 0xC9 &&    //Codepage_1251_Russian_Windows
                        buffer[0] != 0x57)    //кодовая страница 1251 Windows ANSI
                    {
                        ret = new Returns(false, "Кодовая страница не задана или не поддерживается", -1);
                        return null;
                    }
                    if (buffer[0] == 0x65 || buffer[0] == 0x26)
                        encoding = Encoding.GetEncoding(866);
                    else
                        encoding = Encoding.GetEncoding(1251);
                }

                buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
                fs.Position = 4;
                fs.Read(buffer, 0, buffer.Length);
                int RowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000);
                buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
                fs.Position = 8;
                fs.Read(buffer, 0, buffer.Length);
                int FieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
                string[] FieldName = new string[FieldCount]; // Массив названий полей
                string[] FieldType = new string[FieldCount]; // Массив типов полей
                byte[] FieldSize = new byte[FieldCount]; // Массив размеров полей
                byte[] FieldDigs = new byte[FieldCount]; // Массив размеров дробной части
                buffer = new byte[32 * FieldCount]; // Описание полей: 32 байтa * кол-во, начиная с 33-го
                fs.Position = 32;
                fs.Read(buffer, 0, buffer.Length);
                int FieldsLength = 0;

                #region Корректировка количество полей из-за кривого файла обнинска
                for (int i = 0; i<buffer.Length; i++)
                {
                    if (buffer[i] == 13)
                    {
                        FieldCount = i/32;
                        break;
                        
                    }
                }
                #endregion

                DataColumn col;
                for (int i = 0; i < FieldCount; i++)
                {
                    // Заголовки
                    FieldName[i] = Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new char[] { (char)0x00 });
                    FieldType[i] = "" + (char)buffer[i * 32 + 11];
                    FieldSize[i] = buffer[i * 32 + 16];
                    FieldDigs[i] = buffer[i * 32 + 17];
                    FieldsLength = FieldsLength + FieldSize[i];
                    // Создаю колонки
                    switch (FieldType[i])
                    {
                        case "L": dt.Columns.Add(FieldName[i], Type.GetType("System.Boolean")); break;
                        case "D": dt.Columns.Add(FieldName[i], Type.GetType("System.DateTime")); break;
                        case "N":
                            {
                                if (FieldDigs[i] == 0)
                                    dt.Columns.Add(FieldName[i], Type.GetType("System.Int64"));
                                else
                                {
                                    col = new DataColumn(FieldName[i], Type.GetType("System.Decimal"));
                                    col.ExtendedProperties.Add("precision", FieldSize[i]);
                                    col.ExtendedProperties.Add("scale", FieldDigs[i]);
                                    col.ExtendedProperties.Add("length", FieldSize[i] + FieldDigs[i]);
                                    dt.Columns.Add(col);
                                }
                                break;
                            }
                        case "F": dt.Columns.Add(FieldName[i], Type.GetType("System.Double")); break;
                        default:
                            col = new DataColumn(FieldName[i], Type.GetType("System.String"));
                            col.MaxLength = FieldSize[i];
                            dt.Columns.Add(col);
                            break;
                    }
                }
                #region Пропускаем незначащие нулевые байты Обнинск

                while (fs.Position < fs.Length && fs.ReadByte() == 0)
                {
                    
                }
                fs.Position--;
                //fs.ReadByte(); // Пропускаю разделитель схемы и данных
                #endregion
                
                System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("ru-RU", false).DateTimeFormat;
                System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("ru-RU", false).NumberFormat;
                nfi.NumberDecimalSeparator = ".";
                nfi.CurrencyDecimalSeparator = ".";


                buffer = new byte[FieldsLength];
                dt.BeginLoadData();
                //fs.ReadByte(); // Пропускаю стартовый байт элемента данных
                int delPriznak = 0;
                char[] numericValidChars = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };

                for (int j = 0; j < RowsCount; j++)
                {

                    delPriznak = fs.ReadByte(); // Пропускаю стартовый байт элемента данных

                    fs.Read(buffer, 0, buffer.Length);
                    System.Data.DataRow R = dt.NewRow();
                    int Index = 0;



                    for (int i = 0; i < FieldCount; i++)
                    {

                        string l = encoding.GetString(buffer, Index, FieldSize[i]).TrimEnd(new char[] { (char)0x00, (char)0x20 });
                        Index = Index + FieldSize[i];

                        if (l != "")
                            switch (FieldType[i])
                            {
                                case "L": R[i] = l == "T" ? true : false; break;
                                case "D":
                                    try
                                    {
                                        R[i] = DateTime.ParseExact(l, "yyyyMMdd", dfi);
                                    }
                                    catch
                                    {
                                        tag = -1;
                                        throw new Exception("Ожидалась дата в формате ГГГГММДД в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + l);
                                    }
                                    break;
                                case "N":
                                    {
                                        l = l.Trim().Replace(",", ".");
                                        string val = "";
                                        foreach (char c in l.ToCharArray()) if (numericValidChars.Contains(c)) val += c; else break;

                                        if (FieldDigs[i] == 0)
                                        {
                                            try
                                            {
                                                R[i] = Int64.Parse(val, nfi);
                                            }
                                            catch(Exception ex)
                                            {
                                                tag = -1;
                                                throw new Exception("Ожидалось целое число в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + val);
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                R[i] = decimal.Parse(val, nfi);
                                            }
                                            catch
                                            {
                                                tag = -1;
                                                throw new Exception("Ожидалось вещественное число в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + val);
                                            }
                                        }
                                        break;
                                    }
                                case "F": R[i] = double.Parse(l.Trim(), nfi); break;
                                default: R[i] = l; break;
                            }
                        else
                            R[i] = DBNull.Value;
                    }
                    if (delPriznak == 32)
                        dt.Rows.Add(R);
                }
                dt.EndLoadData();
                fs.Close();
                return dt;
            }
            catch (Exception e)
            {
                ret.result = false;
                if (tag < 0) ret.text = e.Message;
                else ret.text = "Ошибка конвертации DBF файла";
                ret.tag = tag;
                MonitorLog.WriteLog("Ошибка конвертации DBF файла: " + e.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        static public string GetSupgCurDate(string DFormat, string TFormat)
        //----------------------------------------------------------------------
        {
            string rStr = "";
            if (Points.IsDemo)
            {
                if (DFormat == "D") rStr = System.Convert.ToDateTime(Points.DateOper.Date).ToString("dd.MM.yyyy");
                if (DFormat == "T") rStr = System.Convert.ToDateTime(Points.DateOper.Date).ToString("yyyy-MM-dd");
                if (DFormat == "F") rStr = System.Convert.ToDateTime(Points.DateOper.Date).ToString("yyyy_MM_dd");
            }
            else
            {
                if (DFormat == "D") rStr = System.Convert.ToDateTime(DateTime.Now.ToString()).ToString("dd.MM.yyyy");
                if (DFormat == "T") rStr = System.Convert.ToDateTime(DateTime.Now.ToString()).ToString("yyyy-MM-dd");
                if (DFormat == "F") rStr = System.Convert.ToDateTime(DateTime.Now.ToString()).ToString("yyyy_MM_dd");
            }

            if (TFormat != "")
            {
                if (DFormat != "F")
                {
                    if (TFormat == "H") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH");
                    if (TFormat == "m") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH:mm");
                    if (TFormat == "s") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH:mm:ss");
                }
                else
                {
                    if (TFormat == "H") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH");
                    if (TFormat == "m") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH_mm");
                    if (TFormat == "s") rStr = rStr + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString(" HH:mm:ss");
                }

            }

            return rStr;

        }

        /// <summary>
        /// получение названия месяца по целочисленному значению
        /// </summary>
        /// <param name="month">месяц</param>
        /// <returns></returns>
        static public string GetMonthName(int month)
        {
            switch (month)
            {
                case 1:
                    return "Январь";
                case 2:
                    return "Февраль";
                case 3:
                    return "Март";
                case 4:
                    return "Апрель";
                case 5:
                    return "Май";
                case 6:
                    return "Июнь";
                case 7:
                    return "Июль";
                case 8:
                    return "Август";
                case 9:
                    return "Сентябрь";
                case 10:
                    return "Октябрь";
                case 11:
                    return "Ноябрь";
                case 12:
                    return "Декабрь";
                default:
                    return "";
            }
        }

        /// <summary>
        /// процедура подсчета MD5 строки
        /// </summary>
        /// <param name="input">строка</param>
        /// <returns></returns>
        static public string CreateMD5StringHash(string input)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (var t in hashBytes)
            {
                sb.Append(t.ToString("X2"));
            }
            return sb.ToString();
        }

        public static bool PrmValToBool(this string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                int i = 0;
                int.TryParse(s, out i);
                return i == 1;
            }
            return false;
        }

        public class SplitedAddress
        {
            public string street;
            public string dom;
            public string kor;
            public string kvar;

            public SplitedAddress()
            {
                street = "";
                dom = "";
                kor = "";
                kvar = "";
            }
        }

        public static SplitedAddress SplitAddress(string address)
        {
            var adr = new SplitedAddress();
            if (address.IndexOf("УМЕР") == -1 && address.IndexOf(',') != -1)
            {
                try
                {
                    adr.street = address.Substring(0, address.IndexOf(','));
                    address = address.Substring(address.IndexOf(',') + 1).Trim();

                    if (address.IndexOf("Д.") != -1)
                    {
                        address = address.Substring(address.IndexOf("Д.") + 2).Trim();
                        adr.dom = address.Substring(0, address.IndexOf(' '));
                    }
                    else
                    {
                        int dom;
                        if (address.IndexOf(',') == -1)
                        {
                            if (Int32.TryParse(address.Trim(), out dom))
                            {
                                adr.dom = dom.ToString();
                            }
                            else
                            {
                                adr.dom = address.Substring(0, address.Length - 1);
                                adr.kor = address.Substring(address.Length - 1, 1);
                            }
                        }
                        else if (Int32.TryParse(address.Trim().Substring(0, address.IndexOf(',')), out dom))
                        {
                            adr.dom = dom.ToString();
                        }
                        else
                        {
                            adr.dom = address.Substring(0, address.IndexOf(',') - 1);
                            adr.kor = address.Substring(address.IndexOf(',') - 1, 1);
                        }
                    }

                    if (address.IndexOf("КОРПУС") != -1)
                    {
                        address = address.Substring(address.IndexOf("КОРПУС") + 6).Trim();
                        adr.kor = address.Substring(0, 1);
                    }
                    if (address.IndexOf(',') != -1)
                    {
                        address = address.Substring(address.IndexOf(',') + 1).Trim();
                        int kvar;
                        if (Int32.TryParse(address, out kvar))
                            adr.kvar = kvar.ToString();
                    }
                }
                catch (Exception)
                {
                    adr.street = "";
                    adr.dom = "";
                    adr.kor = "";
                    adr.kvar = "";

                }

            }

            return adr;
        } 

    }
}
