using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.IO;


namespace STCLINE.KP50.DataBase
{
    public partial class DbPack 
    {
        public DataTable LoadUniversalFormat(string body, string filename)
        {
            byte[] binaryData = Convert.FromBase64String(body);
            var memstr = new MemoryStream();
            memstr.SetLength(binaryData.Length);
            memstr.Write(binaryData, 0, binaryData.Length);
            memstr.Position = 0;
           // int countInsRows=0;
            AddedPacksInfo insertedPackInfo= new AddedPacksInfo();
            DataTable result = null;
            try
            {
                result = LoadUniversalFormat(memstr, filename, insertedPackInfo);
            }
            finally
            {
                memstr.Close();
                memstr.Flush();
            }
            return result;

        }

        public DataTable LoadUniversalFormat(System.IO.Stream fs, string filename,  AddedPacksInfo packsInfo)
        {
            Returns ret;
            string fst = ChangeFileCodePage(fs, out ret);
            if (!ret.result)
            {
                return null;
            }
            string[] listSt = fst.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            Pack pack = null;
            var dtError = new DataTable();
            dtError.Columns.Add("number_string", typeof(int));
            dtError.Columns.Add("mes", typeof(string));
            dtError.Columns.Add("bank", typeof(string));
            //Разбор пачек
            if (listSt.Length > 0)
            {
                if (listSt[0].Substring(0, 3) == "***")
                {
                    pack = GetSuperPack(listSt, ref dtError, out ret);
                }
                else
                {
                    int i = 0;

                    pack = GetPack(listSt, ref i, ref dtError, out ret);
                }
            }
            if (dtError.Rows.Count > 0)
            {
                return dtError;
            }

            if (pack == null)
            {
                return null;
            }

            #region Сохранение в базе
            pack.file_name = filename;
            DbPackClient dbpClient = new DbPackClient();
            // сохранение в public.source_pack, public.source_pack_ls, public.source_pu_vals
            dbpClient.TypePayCode = typePayCode;// передать тип платежного кода
            ret = dbpClient.SaveUniversalFormatToWeb(ref pack, ref packsInfo);
            if (!ret.result)
            {
                dtError.Rows.Add((int)DownloadMessageTypes.Error, ret.text);
                return dtError;
            }
            // сохранение в f_fin_xx
            ret = UploadPackFromWeb(pack.nzp_pack, 0, ref packsInfo);
            if (!ret.result)
            {
                dtError.Rows.Add((int)DownloadMessageTypes.Error, ret.text);
                return dtError;
            }
            packsInfo.InsertErrMsg(dtError);
            packsInfo.InsertWarnMsg(dtError);
            dtError.DefaultView.Sort = "mes asc";
            dtError = dtError.DefaultView.ToTable();
            return dtError;  
            #endregion
        }

        public string ChangeFileCodePage(System.IO.Stream fs, out Returns ret)
        {
            ret = Utils.InitReturns();
            ret.result = false;
            var buffer = new byte[fs.Length];
            fs.Position = 0;
            fs.Read(buffer, 0, buffer.Length);

            //win кодировка
            string st1251 = System.Text.Encoding.GetEncoding(1251).GetString(buffer);

            if (System.Text.RegularExpressions.Regex.Replace(st1251, "[0-9А-яЁё|.#_№@*,a-zA-Z;\n\r-!:-]", "").Trim().Length == 0)
            {
                //Кодировка 1251
                ret.result = true;
                return st1251;
            }
            string st866 = System.Text.Encoding.GetEncoding(866).GetString(buffer);
            if (System.Text.RegularExpressions.Regex.Replace(st866,
                "[0-9А-яЁё|.#_№@*,a-zA-Z;\n\r-!:-]", "").Trim().Length == 0)
            {
                //Кодировка 866
                ret.result = true;
                return st866;
            }
            string st65001 = System.Text.Encoding.GetEncoding(65001).GetString(buffer);
            if (System.Text.RegularExpressions.Regex.Replace(st65001,
                "[0-9А-яЁё|.#_№@*,a-zA-Z;\n\r-!:-]", "").Trim().Length == 0)
            {
                //Кодировка 866
                ret.result = true;
                return st65001;
            }

            else

            {
              //  Master.ShowMessage(MessageTemplate.MsgType.Information,
              //"<b>Файл " + FileUploadPack.FileName +
              //" не является пачкой оплат в установленном формате версии </b> ");
                ret.result = true;
                return st866;
            }


        }

        /// <summary>
        /// Разбор строк в структуру суперпачки
        /// </summary>
        /// <param name="stmas">Массив строк файла оплат</param>
        /// <param name="dtError"></param>
        /// <param name="ret">Результат операции успешно - true </param>
        /// <returns>Сформированная суперпачка с подпачками</returns>
        public Pack GetSuperPack(string[] stmas, ref DataTable dtError, out Returns ret)
        {
            ret = Utils.InitReturns();
            ret.result = true;
            if (stmas.Length < 2) return null;


            //Определяем место формирования
            var superPack = new Pack();
            char[] delimetr = { '|' };
            var headSuper = stmas[0].Split(delimetr, StringSplitOptions.None);
            if (String.IsNullOrWhiteSpace(headSuper[1]))
            {
                dtError.Rows.Add(0, "Не задано наименования места формирования объединенной пачки ");
                ret.result = false;
            }
            else
            {
                superPack.bank = headSuper[1];
            }
            superPack.erc_code = headSuper[2];

            #region Номер пачки
            int numPack;
            if ((headSuper.Length >= 4) & (Int32.TryParse(headSuper[3], out numPack))) superPack.num_pack = numPack;
            else
            {
                superPack.num_pack = 0;
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректный номер суперпачки: " + headSuper[3] + ", диапазон значений от 0 до 2147483647");

                ret.result = false;
            }
            #endregion

            #region Дата пачки
            if ((headSuper.Length >= 5))
            {
                DateTime dt;
                if (DateTime.TryParse(headSuper[4], out dt))
                {
                    superPack.dat_pack = headSuper[4];
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректная дата суперпачки: " + headSuper[4] + ", формат даты ДД.ММ.ГГГГ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка суперпачки: " + stmas[0] + ", не хватает даты пачки");
                ret.result = false;
            }
            #endregion

            #region Время пачки
            if ((headSuper.Length >= 6))
            {

                try
                {
                    if (headSuper[5].Length == 8)
                    {
                        superPack.time_pack = DateTime.ParseExact(headSuper[5], "HH:mm:ss",
                            new CultureInfo("ru-RU")).ToShortTimeString();
                    }
                    else
                    {
                        superPack.time_pack = DateTime.ParseExact(headSuper[5], "H:mm:ss",
                            new CultureInfo("ru-RU")).ToShortTimeString();
                    }
                }
                catch (Exception ex)
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректное время создания пачки: " + ex.Message +
                        headSuper[5] + ", формат времени ЧЧ:ММ:СС");

                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка супер пачки: " + stmas[0] + ", не хватает времени созадания пачки");
                ret.result = false;
            }
            #endregion

            #region Дата операционного дня пачки пачки
            if ((headSuper.Length >= 7))
            {
                DateTime dt;
                if (DateTime.TryParse(headSuper[6], out dt))
                {
                    superPack.dat_calc = headSuper[6];
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректная дата операционного дня: " + headSuper[6] + ", формат даты ДД.ММ.ГГГГ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка суперпачки: " + stmas[0] + ", не хватает даты операционного дня");
                ret.result = false;
            }
            #endregion

            #region Количество квитанций
            if ((headSuper.Length >= 8))
            {
                int countPack;
                if (Int32.TryParse(headSuper[7], out countPack))
                {
                    superPack.count_kv = countPack;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректное количество подпачек: " + headSuper[7] + ", диапазон значений от 0 до 2147483647");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка суперпачки: " + stmas[0] + ", не хватает количества подпачек в пачке");
                ret.result = false;
            }
            #endregion

            #region Начисленная сумма
            if ((headSuper.Length >= 9))
            {
                decimal sumNach;
                if (Decimal.TryParse(headSuper[8], out sumNach))
                {
                    superPack.sum_nach = sumNach;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректная сумма начислений по суперпачке: " +
                        headSuper[8] + ", формат числа, разделитель разрядов точка ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка супер пачки: " + stmas[0] + ", не хватает начисленной суммы");
                ret.result = false;
            }
            #endregion

            #region Оплаченная сумма
            if ((headSuper.Length >= 10))
            {
                decimal sumOpl;
                if (Decimal.TryParse(headSuper[9], out sumOpl))
                {
                    superPack.sum_pack = sumOpl;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректная сумма оплат по суперпачке: " +
                        headSuper[9] + ", формат числа, разделитель разрядов точка ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка супер пачки: " +
                    stmas[0] + ", не хватает суммы оплат");
                ret.result = false;
            }
            #endregion

            #region Резерв
            if ((headSuper.Length >= 11))
            {

                long nzpSupp;
                if (Int64.TryParse(headSuper[10], out nzpSupp))
                {
                    superPack.nzp_supp = nzpSupp;
                    if (superPack.nzp_supp > 0) superPack.pack_type = 20;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректный формат заголовка суперпачки: " +
                    headSuper[10] + ", поле резерв");
                    ret.result = false;
                }

            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка суперпачки: " +
                    stmas[0] + ", не хватает поля резерв");
                ret.result = false;
            }
            #endregion

            #region Сумма изменений
            if ((headSuper.Length >= 12))
            {
                decimal sumIzm;
                if (Decimal.TryParse(headSuper[11], out sumIzm))
                {
                    superPack.sum_izm = sumIzm;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректная сумма оплат по суперпачке: " + headSuper[11] +
                        ", формат числа, разделитель разрядов точка ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка суперпачки: " +
                    stmas[0] + ", не хватает суммы оплат");
                ret.result = false;
            }
            #endregion

            #region Версия формата
            if ((headSuper.Length >= 13))
            {
                List<string> formats = new List<string>() { "!1.00", "!1.02" };
                superPack.version_pack = headSuper[12];
                if (!formats.Contains(superPack.version_pack.Trim()))
                {
                    dtError.Rows.Add(1, "Некорректный формат заголовка суперпачки: " +
                    stmas[0] + ", ожидается версия !1.00");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(1, "Некорректная формат заголовка суперпачки: " + stmas[0] + ", нет версии формата ");
                ret.result = false;
            }
            #endregion


            if (!ret.result) return null;
            // Разбор пачек
            superPack.listPack = new List<Pack>();
            int i = 1;
            decimal sumPack = 0;
            decimal sumPackNach = 0;
            decimal sumPackIzm = 0;


            while (i < stmas.Length)
            {
                if (stmas[i].Trim() != "")
                {
                    Pack pack = GetPack(stmas, ref i, ref dtError, out ret);
                    if (pack != null)
                    {
                        superPack.listPack.Add(pack);
                        sumPack += pack.sum_pack;
                        sumPackNach += pack.sum_nach;
                        sumPackIzm += pack.sum_izm;

                    }
                    else
                    {
                        ret.result = false;
                        return null;
                    }
                }
                i++;
            }
            #region Проверки
            if (superPack.sum_nach != sumPackNach)
            {
                dtError.Rows.Add(1, "Сумма начислений суперпачки: " + superPack.sum_nach +
                    " не совпадает с суммой начислений по пачкам " + sumPackNach);
            }
            if (superPack.sum_pack != sumPack)
            {
                dtError.Rows.Add(1, "Сумма оплат суперпачки: " + superPack.sum_pack +
                  " не совпадает с суммой оплат по пачкам " + sumPack);
            }
            if (superPack.sum_izm != sumPackIzm)
            {
                dtError.Rows.Add(1, "Сумма изменений суперпачки: " + superPack.sum_izm +
                  " не совпадает с суммой изменений по пачкам " + sumPackIzm);
            }
            if (superPack.count_kv != superPack.listPack.Count)
            {
                dtError.Rows.Add(1, "Количество пачек в суперпачке: " + superPack.count_kv +
                  " не совпадает с количеством пачек в файле " + superPack.listPack.Count);
            }



            #endregion

            return superPack;
        }

        /// <summary>
        /// Разбор строк в структуру пачки
        /// </summary>
        /// <param name="stmas">Массив строк файла оплат</param>
        /// <param name="index">Номер строки, с которой начинается разбор</param>
        /// <param name="dtError"></param>
        /// <param name="ret">Результат операции успешно - true</param>
        /// <returns>Сформированная пачка оплат</returns>
        public Pack GetPack(string[] stmas, ref int index, ref DataTable dtError, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (stmas[index].Length < 3)
            {
                dtError.Rows.Add(index, "Некоректный заголовок пачки" + stmas[index]);
                return null;
            }

            var pack = new Pack();
            //Разбор заголовка пачки
            char[] delimetr = { '|' };
            string[] headSuper = stmas[index].Split(delimetr, StringSplitOptions.None);
            int countLsWithCountersinPack = 0;
            int startPackString = index;
            if (String.IsNullOrWhiteSpace(headSuper[1]))
            {
                dtError.Rows.Add(index + 1, "Не задан пункта приема платежа, строка в файле " +
                     index + ". Пункт приема платежа не должен быть пустым");
                ret.result = false;
            }
            else
            {
                pack.bank = headSuper[1];
            }
            pack.erc_code = headSuper[2];


            #region Номер пачки
            int numPack;
            if ((headSuper.Length >= 4) & (Int32.TryParse(headSuper[3], out numPack))) pack.num_pack = numPack;
            else
            {
                pack.num_pack = 0;
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректный номер пачки: " +
                    headSuper[3] + ", диапазон значений от 0 до 2147483647");
                ret.result = false;
            }
            #endregion

            #region Дата пачки
            if ((headSuper.Length >= 5))
            {
                DateTime dt;
                if (DateTime.TryParse(headSuper[4], out dt))
                {
                    pack.dat_pack = headSuper[4];
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(index + 1, "Некорректная дата пачки: " +
                        headSuper[4] + ", формат даты ДД.ММ.ГГГГ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректная формат заголовка пачки: " +
                    stmas[index] + ", не хватает даты пачки");
                ret.result = false;
            }
            #endregion

            #region Дата операционного дня пачки пачки
            if ((headSuper.Length >= 6))
            {
                DateTime dt;
                if (DateTime.TryParse(headSuper[5], out dt))
                {
                    pack.dat_calc = headSuper[5];
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(index + 1, "Некорректная дата операционного дня: " +
                        headSuper[5] + ", формат даты ДД.ММ.ГГГГ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректная формат заголовка пачки: " +
                    stmas[index] + ", не хватает даты операционного дня");
                ret.result = false;
            }
            #endregion

            #region Количество квитанций
            if ((headSuper.Length >= 7))
            {
                int countPack;
                if (Int32.TryParse(headSuper[6], out countPack))
                {
                    pack.count_kv = countPack;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(index + 1, "Некорректное количество подпачек: " +
                        headSuper[6] + ", диапазон значений от 0 до 2147483647");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректная формат заголовка пачки: " +
                    stmas[index] + ", не хватает количества подпачек в пачке");
                ret.result = false;
            }
            #endregion

            #region Начисленная сумма
            if ((headSuper.Length >= 8))
            {
                decimal sumNach;
                if (Decimal.TryParse(headSuper[7], out sumNach))
                {
                    pack.sum_nach = sumNach;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(index + 1, "Некорректная сумма начислений по пачке: " +
                        headSuper[7] + ", формат числа, разделитель разрядов точка ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректная формат заголовка пачки: " +
                    stmas[index] + ", не хватает начисленной суммы");
                ret.result = false;
            }
            #endregion

            #region Оплаченная сумма
            if ((headSuper.Length >= 9))
            {
                decimal sumOpl;
                if (Decimal.TryParse(headSuper[8], out sumOpl))
                {
                    pack.sum_pack = sumOpl;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(index + 1, "Некорректная сумма оплат по пачке: " +
                        headSuper[8] + ", формат числа, разделитель разрядов точка ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректная формат заголовка пачки: " +
                    stmas[index] + ", не хватает суммы оплат");
                ret.result = false;
            }
            #endregion

            #region Резерв
            if ((headSuper.Length >= 10))
            {

                long nzpSupp;
                if (Int64.TryParse(headSuper[9], out nzpSupp))
                {
                    pack.nzp_supp = nzpSupp;
                    if (pack.nzp_supp > 0) pack.pack_type = 20;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(1, "Некорректный формат заголовка пачки: " +
                    headSuper[9] + ", поле резерв");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректная формат заголовка пачки: " +
                    stmas[index] + ", не хватает поля резерв");
                ret.result = false;
            }
            #endregion

            #region Сумма изменений
            if ((headSuper.Length >= 11))
            {
                decimal sumIzm;
                if (Decimal.TryParse(headSuper[10], out sumIzm))
                {
                    pack.sum_izm = sumIzm;
                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(index + 1, "Некорректная сумма изменений оплат по ЛС: " +
                        headSuper[9] +
                        ", формат числа, разделитель разрядов точка ");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректная формат заголовка пачки: " +
                    stmas[index] + ", не хватает суммы изменений оплат ");
                ret.result = false;
            }
            #endregion

            #region Резерв Количество ЛС со счетчиками
            if ((headSuper.Length >= 12))
            {
                if (Int32.TryParse(headSuper[11], out countLsWithCountersinPack))
                {

                }
                else
                {
                    //Ошибка в номере пачки
                    dtError.Rows.Add(index + 1, "Некорректное количество ЛС со счетчиками: " +
                        headSuper[6] + ", диапазон значений от 0 до 2147483647");
                    ret.result = false;
                }

            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректная формат заголовка пачки: " +
                    stmas[index] + ", не хватает поля резерв");
                ret.result = false;
            }
            #endregion

            #region Версия формата
            if ((headSuper.Length >= 13))
            {
                List<string> formats = new List<string>() { "!1.00", "!1.02" };
                pack.version_pack = headSuper[12];
                if (!formats.Contains(pack.version_pack.Trim()))
                {
                    dtError.Rows.Add(index + 1, "Некорректный формат заголовка пачки: " +
                    stmas[0] + ", ожидается версия !1.00");
                    ret.result = false;
                }
            }
            else
            {
                //Ошибка в номере пачки
                dtError.Rows.Add(index + 1, "Некорректный формат заголовка супер пачки: " +
                    stmas[0] + ", нет версии формата ");
                ret.result = false;
            }
            #endregion

            decimal sumPack = 0;
            decimal sumPackNach = 0;
            decimal sumPackIzm = 0;

            pack.listPackLs = new List<Pack_ls>();

            index++;

            while (index < stmas.Length)
            {
                if (stmas[index].IndexOf("#", StringComparison.Ordinal) > -1)
                {
                    index--;
                    break;
                }

                if (stmas[index].Trim() != "")
                {
                    string errText;
                    var packLS = GetPackLs(stmas[index], out errText, pack.version_pack.Trim());
                    if (packLS != null)
                    {
                        pack.listPackLs.Add(packLS);
                        sumPack += packLS.g_sum_ls;
                        sumPackNach += packLS.sum_ls;
                        sumPackIzm += packLS.sum_izm;
                    }
                    else
                    {
                        ret.result = false;

                        dtError.Rows.Add(index + 1, errText + " (номер строки в файле " + (index + 1) + ")");
                        return null;
                    }
                }
                index++;

            }


            #region Проверки
            if (pack.sum_nach != sumPackNach)
            {
                dtError.Rows.Add(startPackString + 1, "Сумма начислений пачки: " + pack.sum_nach +
                    " не совпадает с суммой начислений по ЛС " + sumPackNach);
                ret.result = false;
            }
            if (pack.sum_pack != sumPack)
            {
                dtError.Rows.Add(startPackString + 1, "Сумма оплат пачки: " + pack.sum_pack +
                  " не совпадает с суммой оплат по ЛС " + sumPack);
                ret.result = false;
            }
            if (pack.sum_izm != sumPackIzm)
            {
                dtError.Rows.Add(startPackString + 1, "Сумма изменений пачки: " + pack.sum_izm +
                  " не совпадает с суммой изменений по ЛС " + sumPackIzm);
                ret.result = false;
            }
            if (pack.count_kv != pack.listPackLs.Count)
            {
                dtError.Rows.Add(startPackString + 1, "Количество ЛС в пачке: " + pack.count_kv +
                  " не совпадает с количеством ЛС в файле " + pack.listPackLs.Count);
                ret.result = false;
            }

            foreach (Pack_ls t in pack.listPackLs)
            {
                if (t.puVals.Count > 0) countLsWithCountersinPack--;
            }
            if (countLsWithCountersinPack != 0)
            {
                dtError.Rows.Add(startPackString + 1, "Не совпадает количество ЛС в пачке со счетчиками с заявленным, разница : " +
                   countLsWithCountersinPack);
                ret.result = false;
            }
            #endregion

            if (ret.result == false) return null;

            return pack;
        }

        /// <summary>
        /// Разбор строки записи по платженому коду
        /// </summary>
        /// <param name="st">Строка из файла оплат</param>
        /// <param name="errText">Ошибки при разборе</param>
        /// <returns>Результат операции успешно - true</returns>
        public Pack_ls GetPackLs(string st, out string errText, string format)
        {
            var packLs = new Pack_ls();
            char[] delimetr = { '|' };
            string[] headLs = st.Split(delimetr, StringSplitOptions.None);
            var stError = new System.Text.StringBuilder();

            try
            {
                #region Номер справки
                int infoNum;
                if ((headLs.Length >= 1) & (Int32.TryParse(headLs[1], out infoNum))) packLs.info_num = infoNum;
                else
                {
                    packLs.num_pack = 0;
                    //Ошибка в номере пачки
                    stError.Append("Некорректный номер справки об оплате: " +
                        headLs[1] + ", диапазон значений от 0 до 2147483647");
                }
                #endregion

                #region Код управояющей компании
                if (headLs.Length >= 3)
                {
                    packLs.erc_code = headLs[2];
                }
                else
                {
                    stError.Append("Некорректный состав записи о ЛС: " + st + ", нет необходимых полей");
                }
                #endregion

                #region код квитанции
                int kodSum;
                if ((headLs.Length >= 4) & (Int32.TryParse(headLs[3], out kodSum))) packLs.kod_sum = kodSum;
                else
                {
                    packLs.kod_sum = 33;
                    //Ошибка в номере пачки
                    stError.Append("Некорректный код квитанции: " +
                        headLs[3] + ", диапазон значений от 01 до 99 ");
                }
                #endregion

                #region Источник платежа
                int paySource;
                if ((headLs.Length >= 5) & (Int32.TryParse(headLs[4], out paySource))) packLs.paysource = paySource;
                else
                {
                    packLs.paysource = 0;
                    //Ошибка в номере пачки
                    stError.Append("Некорректный источник платежа в квитанции: " +
                        headLs[4] + ", диапазон значений от 01 до 99 ");
                }
                #endregion

                #region Платежный код
                Int64 pkod;

                if (headLs.Length > 6)
                {
                    if (headLs[5].Length > 0)
                    {
                        packLs.pkod = headLs[5];
                        // Если платежный код состоит из 10 или 13 цифр
                        if (headLs[5].Length == 10 || headLs[5].Length == 13)
                        {
                            // и если он не парсится
                            if (!Int64.TryParse(headLs[5], out pkod))
                            {
                                // не стандартный
                               typePayCode= TypePayCode.Specific;
                            }
                        }
                        else
                        {
                            typePayCode = TypePayCode.Specific;
                        }
                    }
                    else
                    {
                        packLs.pkod = "0";
                        //Ошибка в номере пачки
                        stError.Append("Некорректный платежный код в квитанции: " +
                            headLs[5] + ", платежный код не должен быть пустым  "); 
                    }
                }
                else
                {
                        packLs.pkod = "0";
                        //Ошибка в номере пачки
                        stError.Append("Некорректный платежный код в квитанции: " +
                            headLs[5] + ", платежный код не должен быть пустым  "); 
                }
                //if ((headLs.Length >= 6) & (Int64.TryParse(headLs[5], out pkod))) packLs.pkod = headLs[5];
                //else
                //{
                //    packLs.pkod = "0";
                //    //Ошибка в номере пачки
                //    stError.Append("Некорректный платежный код в квитанции: " +
                //        headLs[5] + ", ожидается целое число 10 или 13 символов  ");
                //}

                //if ((packLs.pkod.Length != 13) & (packLs.pkod.Length != 10))
                //{
                //    packLs.pkod = "0";
                //    //Ошибка в номере пачки
                //    stError.Append("Некорректный платежный код в квитанции: " +
                //        headLs[5] + ", ожидается целое число 10 или 13 символов  ");
                //}


                #endregion

                #region Дата платежа
                if (headLs.Length >= 7)
                {
                    DateTime dt;
                    if (DateTime.TryParse(headLs[6], out dt))
                    {
                        packLs.dat_vvod = headLs[6];
                    }
                    else
                    {
                        //Ошибка в номере пачки
                        stError.Append("Некорректная дата платежа: " +
                            headLs[6] + ", формат даты ДД.ММ.ГГГГ");
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат записи по платежному коду: " +
                        st + ", не хватает даты оплаты");
                }
                #endregion

                #region Дата, за которую платит человек
                if ((headLs.Length >= 8))
                {
                    DateTime dt;
                    if (DateTime.TryParse(headLs[7], out dt))
                    {
                        packLs.dat_month = headLs[7];
                    }
                    else
                    {
                        //Ошибка в номере пачки
                        stError.Append("Некорректный месяц за который платит человек: " +
                            headLs[7] + ", формат даты ДД.ММ.ГГГГ");
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат записи по платежному коду: " +
                        st + ", не хватает даты месяца за который платит человек");
                }
                #endregion

                #region Номер счета в месяце
                if ((headLs.Length >= 9))
                {
                    int idBill;
                    if (Int32.TryParse(headLs[8], out idBill))
                    {
                        packLs.id_bill = idBill;
                    }
                    else
                    {
                        //Ошибка в номере пачки
                        stError.Append("Некорректный номер счета в месяце: " +
                            headLs[8] + ", диапазон значений от 0000 до 9999");
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат записи по платежному коду: " +
                        st + ", не хватает номера счета в месяце");
                }
                #endregion

                #region Начисленная сумма
                if ((headLs.Length >= 10))
                {
                    decimal sumNach;
                    if (Decimal.TryParse(headLs[9], out sumNach))
                    {
                        packLs.sum_ls = sumNach;
                    }
                    else
                    {
                        //Ошибка в номере пачки
                        stError.Append("Некорректная сумма начислений по платежному коду: " +
                            headLs[9] + ", формат числа, разделитель разрядов точка ");
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат записи по платежному коду: " +
                        st + ", не хватает начисленной суммы");
                }
                #endregion

                #region Оплаченная сумма
                if ((headLs.Length >= 11))
                {
                    decimal sumOpl;
                    if (Decimal.TryParse(headLs[10], out sumOpl))
                    {
                        packLs.g_sum_ls = sumOpl;
                    }
                    else
                    {
                        //Ошибка в номере пачки
                        stError.Append("Некорректная сумма оплат по платежному коду: " +
                            headLs[10] + ", формат числа, разделитель разрядов точка ");
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат записи по платежному коду: " +
                        st + ", не хватает суммы оплат");
                }
                #endregion

                #region Резерв
                if ((headLs.Length >= 12))
                {
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат записи по платежному коду: " +
                        st + ", не хватает поля резерв со значением 0 ");

                }
                #endregion

                #region Количество недопоставок(1.00)/Количество уточненных оплат (1.02)
                if ((headLs.Length >= 13))
                {
                    int field;
                    if (Int32.TryParse(headLs[12], out field))
                    {
                        packLs.count_nedop = field;
                    }
                    else
                    {
                        //Ошибка в номере пачки
                        stError.Append("Некорректный количество " + (format == "!1.02" ? "уточненных оплат: " : "недопоставок:" ) +
                            headLs[12] + ", диапазон значений от 0 до 99");
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат записи по платежному коду: " +
                        st + ", не хватает номера счета в месяце");
                }
                #endregion

                #region Количество изменений
                if ((headLs.Length >= 14))
                {
                    int countIzm;
                    if (Int32.TryParse(headLs[13], out countIzm))
                    {
                        packLs.count_izm = countIzm;
                    }
                    else
                    {
                        //Ошибка в номере пачки
                        stError.Append("Некорректное количество изменений: " +
                            headLs[13] + ", диапазон значений от 0 до 99");
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат записи по платежному коду: " +
                        st + ", не хватает номера счета в месяце");
                }
                #endregion

                #region Сумма изменений
                if ((headLs.Length >= 15))
                {
                    decimal sumIzm;
                    if (Decimal.TryParse(headLs[14], out sumIzm))
                    {
                        packLs.sum_izm = sumIzm;
                    }
                    else
                    {
                        //Ошибка в номере пачки
                        stError.Append("Некорректная сумма изменений по ЛС: " + headLs[14] +
                            ", формат числа, разделитель разрядов точка ");
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат заголовка супер пачки: " +
                        st + ", не хватает суммы оплат");

                }
                #endregion

                #region Изменения
                packLs.gilSums = new List<GilSum>();
                if ((headLs.Length >= 16))
                {
                    if (headLs[15].Trim() != "")
                    {
                        delimetr[0] = ';';
                        string[] izmLs = headLs[15].Split(delimetr, StringSplitOptions.None);
                        foreach (string t in izmLs)
                            if (t.Trim() != "")
                            {
                                GilSum gilSum = GetGilSum(t, format);
                                if (gilSum != null)
                                {
                                    packLs.gilSums.Add(gilSum);
                                }
                                else
                                {
                                    errText = "Некорректная запись об изменениях в ЛС: " + t;
                                    return null;

                                }
                            }
                    }
                }
                else
                {
                    //Ошибка в номере пачки
                    stError.Append("Некорректная формат заголовка супер пачки: " +
                        st + ", не хватает суммы оплат");

                }
                #endregion

                #region Счетчики
                packLs.puVals = new List<PuVals>();
                if ((headLs.Length >= 17))
                {
                    if (headLs[16].Trim() != "")
                    {
                        delimetr[0] = ';';
                        string[] puVals = headLs[16].Split(delimetr, StringSplitOptions.None);
                        foreach (string t in puVals)
                        {
                            PuVals puVal = GetPuVal(t);
                            if (puVal != null)
                            {
                                packLs.puVals.Add(puVal);
                            }
                            else
                            {
                                errText = "Некорректная запись о счетчиках в ЛС " + t;
                                return null;

                            }
                        }
                    }
                }

                #endregion

                #region Проверки
                decimal sumgilOpl = packLs.sum_izm;
                decimal countgilOpl = packLs.count_izm;
                decimal countgilNedop = packLs.count_nedop;
                foreach (GilSum t in packLs.gilSums)
                {
                    if ((format == "!1.02" ? t.nzp_supp.ToString() : t.day_nedo) != "") countgilNedop--;
                    if (t.sum_oplat != "")
                    {
                        countgilOpl--;
                        sumgilOpl -= decimal.Parse(t.sum_oplat);
                    }
                }
                if (countgilOpl != 0)
                {
                    stError.AppendLine("Заявленное количество строк графы «указано жильцом по услугам в счете» по ЛС: " + packLs.count_izm
                        + " не совпадает с фактическим: " + (packLs.count_izm - countgilOpl) + " ");
                }
                if (countgilNedop != 0)
                {
                    stError.AppendLine("Заявленное количество" +
                        (format == "!1.02" ? " строк «информация, указанная плательщиком по договорам в разрезе услуг»" : " недопоставок") +
                        " по ЛС: " + packLs.count_nedop
                        + " не совпадает с фактическим: " + (packLs.count_nedop - countgilNedop) + " ");
                }
                if (sumgilOpl != 0)
                {
                    stError.AppendLine("Заявленная сумма строк графы «указано жильцом по услугам в счете» по ЛС: " + packLs.sum_izm
                        + " не совпадает с фактической: " + (packLs.sum_izm - sumgilOpl) + " ");
                }

                #endregion
            }
            catch (Exception)
            {
                stError.Append("Ошибка формата файла");
            }


            errText = stError.ToString();
            if (errText != "") return null;
            return packLs;
        }

        /// <summary>
        /// Разбор записи оплат жильца
        /// </summary>
        /// <param name="st">Строка вида "номер услуги в счете,дней недопоставки,сумма оплаты"</param>
        /// <returns>Обект GilSum</returns>
        public GilSum GetGilSum(string st, string format)
        {
            var gs = new GilSum();
            char[] delimetr = { ',' };
            string[] izmLs = st.Split(delimetr, StringSplitOptions.None);
            if (izmLs.Length != 3)
            {
                return null;
            }

            for (int i = 0; i < izmLs.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        {
                            int firstField;
                            if (Int32.TryParse(izmLs[0], out firstField))
                            {
                                if (format == "!1.02")
                            {
                                    if (!String.IsNullOrEmpty(gs.nzp_serv.ToString()))
                                    gs.nzp_serv = firstField;
                                    else return null;
                                }
                                else gs.ordering = firstField;
                            }
                            else
                            {
                                return null;
                            }
                        } break;
                    case 1:
                        {
                            if (izmLs[1] != "")
                            {
                                int secondField;
                                if (Int32.TryParse(izmLs[1], out secondField))
                                {
                                    if (format == "!1.02")
                                {
                                        if (!String.IsNullOrEmpty(gs.nzp_supp.ToString()))
                                            gs.nzp_supp = secondField;
                                        else return null;
                                    }
                                    else gs.day_nedo = secondField.ToString(CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        } break;
                    case 2:
                        {
                            if (izmLs[2] != "")
                            {
                                decimal sumIzm;
                                if (Decimal.TryParse(izmLs[2], out sumIzm))
                                {
                                    gs.sum_oplat = izmLs[2];
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        } break;

                }
            }

            return gs;
        }

        /// <summary>
        /// Разбор записи показаний счетчиков
        /// </summary>
        /// <param name="st">Строка вида "номер счетчика в счете,показание" либо "код услуги из справочника,заводской
        /// номер прибора учета,показание"</param>
        /// <returns>Обект PuVals</returns>
        public PuVals GetPuVal(string st)
        {
            var puVals = new PuVals();
            char[] delimetr = { ',' };
            string[] puLs = st.Split(delimetr, StringSplitOptions.None);

            if (puLs.Length == 2)
            {
                int ordering;
                if (Int32.TryParse(puLs[0], out ordering))
                {
                    puVals.ordering = ordering;
                }
                else
                {
                    return null;
                }

                decimal valCnt;
                if (Decimal.TryParse(puLs[1], out valCnt))
                {
                    puVals.val_cnt = valCnt;
                }
                else
                {
                    return null;
                }
            }
            else
                if (puLs.Length == 3)
                {
                    int nzpServ;
                    if (Int32.TryParse(puLs[0], out nzpServ))
                    {
                        puVals.nzp_serv = nzpServ;
                    }
                    else
                    {
                        return null;
                    }


                    puVals.num_cnt = puLs[1];

                    decimal valCnt;
                    if (Decimal.TryParse(puLs[2], out valCnt))
                    {
                        puVals.val_cnt = valCnt;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }




            return puVals;
        }


      

    } //end class

} //end namespace