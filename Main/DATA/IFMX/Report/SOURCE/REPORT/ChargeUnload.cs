using System;
using System.Collections.Generic;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Linq;
using System.IO;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep
    {
        /// <summary>
        /// Выгрузка по принятым для перечисления денежным средствам 
        /// </summary>
        /// <returns>Результат</returns>
        public Returns GetChargeUnload(ChargeUnloadPrm finder)
        {
            ClassChargeUnload classChargeUnload = new ClassChargeUnload();
            return classChargeUnload.GetChargeUnload(finder);
        }
    }
    
    /// <summary>
    /// Класс выгрузки по принятым для перечисления денежным средствам 
    /// </summary>
    class ClassChargeUnload : ExcelRepClient
    {
        #region свойства
        //---------------------------------------------------------
        // соединение с базой
        protected IDbConnection _connDb;

        private ExcelRep _excelRepDb = null;
        protected IDataReader _reader;

        // период выгрузки
        // дата начала
        protected string _inDatOperS;

        // дата окончания
        protected string _inDatOperPo;

        // период выгрузки
        // дата начала
        protected DateTime _datOperS;
 
        // дата окончания
        protected DateTime _datOperPo;

        // код агента
        protected int _nzpPayerAgent;

        // код принципала
        protected int _nzpPayerPrincip;

        // код пользователя
        protected int _nzpUser;
        // имя пользователя
        protected string _userName;

        protected ChargeUnloadPrm _finder = null;

        // код файла 
        protected int _nzpExc;

        // код строки заголовка файла
        private string _fileHeadStringCode = "1";
        // код строки тела файла
        private string _fileBodyStringCode = "2";
        // дата файла
        private string _fileDate = "";

        // номер версии
        private string _versionNumber = "1.0";

        // наименование агената
        private string _agentName = "";
        //---------------------------------------------------------
        #endregion

        /// <summary>
        /// Получить выгрузку по перечислениям 
        /// </summary>
        /// <returns></returns>
        public Returns GetChargeUnload(ChargeUnloadPrm finder)
        {
            _finder = finder;

            _nzpPayerAgent = finder.nzp_payer_agent;
            _nzpPayerPrincip = finder.nzp_payer_princip;
            _inDatOperS = finder.dat_s;
            _inDatOperPo = finder.dat_po;
            _nzpUser = finder.nzp_user;
            _userName = finder.webUname;

            Returns ret = new Returns(true);
            // проверить параметры
            ret = CheckInputPrm();
            if (!ret.result) return ret;

            _datOperS = Convert.ToDateTime(_inDatOperS);
            _datOperPo = Convert.ToDateTime(_inDatOperPo);

            _connDb = null;

            try
            {
                // получить соединение
                string conn_kernel = Points.GetConnByPref(Points.Pref);
                _connDb = DBManager.newDbConnection(conn_kernel);
                ret = OpenDb(_connDb, true);
                if (!ret.result) throw new Exception(ret.text);

                // получить имя агенат
                _agentName = GetAgentName(out ret);
                if (!ret.result) throw new Exception(ret.text);

                //сформировать имя файла отчета
                var fileNameKassa = GetFileNameChargeUnload();

                string fileNameOut = fileNameKassa + ".7z";
                string fileNameIn = fileNameKassa + ".txt";

                // сохранить информацию о файле
                _excelRepDb = new ExcelRep();

                string comment = "Выгрузка перечислений в  систему за период: " + _datOperS.ToShortDateString() + "-" + _datOperPo.ToShortDateString() + " от " + _agentName;
                ret = _excelRepDb.AddMyFile(new ExcelUtility
                {
                    nzp_user = _nzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = comment.Length > 100 ? comment.Substring(0, 100) : comment,
                    file_name = Path.GetFileName(fileNameIn),
                    exc_path = Path.GetFileName(fileNameIn)
                });

                if (!ret.result) return ret;
                _nzpExc = ret.tag;

                _fileDate = DateTime.Today.ToShortDateString();

                // cоздать временную таблицу для промежуточных данных
                ret = CreateTableIntermediateChargeUnload();
                if (!ret.result) throw new Exception(ret.text);

                // подготовить промежуточные данные
                ret = PreapareDataIntermediateChargeUnload();
                if (!ret.result) throw new Exception(ret.text);

                // создать временную таблицу для сводных данных
                ret = CreateTableChargeUnload();
                if (!ret.result) throw new Exception(ret.text);

                // подготовить сводные данные
                ret = PreapareDataChargeUnload();
                if (!ret.result) throw new Exception(ret.text);

                // запись в файл
                ret = WriteToFile(fileNameIn);
                if (!ret.result) throw new Exception(ret.text);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
                if (_excelRepDb != null) _excelRepDb.Close();
                if (_connDb != null) _connDb.Close();
            }

            return ret;
        }

        /// <summary>
        /// Создать временную таблицу для промежуточных данных с распределение сумм по домам 
        /// </summary>
        /// <returns></returns>
        private Returns CreateTableIntermediateChargeUnload()
        {
            ExecSQL(_connDb, "drop table tmp_intermediate_unload_transfer", false);

            string sql = String.Concat("create temp table tmp_intermediate_unload_transfer (",
                " nzp_dom           integer,",  // код дома
                " nzp_wp            integer, ", // код банка данных
                " nzp_serv          integer,",  // код услуги 
                " nzp_payer_princip integer,",  // код принципала
                " sum_rasp   ", DBManager.sDecimalType, "(14,2), ", //Рапределено
                " sum_ud     ", DBManager.sDecimalType, "(14,2), ", //Удержано
                " sum_charge ", DBManager.sDecimalType, "(14,2)",   //К перечислению
                " ) " + DBManager.sUnlogTempTable);
            return ExecSQL(_connDb, sql);
        }

        /// <summary>
        /// Создать временную таблицу для итоговой информации 
        /// </summary>
        /// <returns></returns>
        private Returns CreateTableChargeUnload()
        {
            ExecSQL(_connDb, "drop table tmp_unload_transfer", false);

            string sql = String.Concat("create temp table tmp_unload_transfer (",
                " point             varchar(100), ", // название банка данных
                " nzp_payer_princip integer, ",      // код принципала
                " npayer            varchar(100),",  // наименование принципала
                " inn               varchar(12), ",  // ИНН принципала
                " kpp               varchar(9),",    // КПП принципала
                " nzp_serv          integer,",       // код услуги
                " sum_rasp   ", DBManager.sDecimalType, "(14,2), ", //Рапределено
                " sum_ud     ", DBManager.sDecimalType, "(14,2), ", //Удержано
                " sum_charge ", DBManager.sDecimalType, "(14,2)",   //К перечислению
                " ) " + DBManager.sUnlogTempTable);
            return ExecSQL(_connDb, sql);
        }

        /// <summary>
        /// Подготовка промежуточных данных с распределеним сумм по домам
        /// </summary>
        /// <returns></returns>
        private Returns PreapareDataIntermediateChargeUnload()
        {
            Returns ret = new Returns(true);
            
            try
            {
                // условие на dat_oper
                string whereDatOper = "";
                if (_datOperS == _datOperPo)
                {
                    whereDatOper = " and a.dat_oper = " + Utils.EStrNull(_datOperS.ToShortDateString());
                }
                else
                {
                    whereDatOper = " and a.dat_oper >= " + Utils.EStrNull(_datOperS.ToShortDateString()) + " and a.dat_oper <= " + Utils.EStrNull(_datOperPo.ToShortDateString());
                }
                
                // вычислить количество месяцев
                int month_count = (_datOperPo.Year - _datOperS.Year) * 12 - _datOperS.Month + _datOperPo.Month + 1;
                string supplier = Points.Pref + DBManager.sKernelAliasRest + "supplier";
                string sql = "";

                string whereNzpServ = this.GetRolesCondition(_finder, Constants.role_sql_serv);
                string whereNzpSupp = this.GetRolesCondition(_finder, Constants.role_sql_supp);
                
                // вставка промежуточных данных по месяцам
                DateTime cur_date = _datOperS; 
                for (var i = 0; i < month_count; i++)
                {
                    string distribXX = Points.Pref + "_fin_" + (cur_date.Year % 100).ToString("00") + DBManager.tableDelimiter + "fn_distrib_dom_" + cur_date.Month.ToString("00");
                    sql = " insert into tmp_intermediate_unload_transfer " +
                        " (nzp_dom, nzp_serv, nzp_payer_princip, sum_rasp, sum_ud, sum_charge) " +
                        " SELECT a.nzp_dom, a.nzp_serv, a.nzp_payer, sum(a.sum_rasp), sum(a.sum_ud), sum(a.sum_charge) " +
                        " FROM " + distribXX + " a, " + supplier + " s " +
                        " WHERE a.nzp_supp = s.nzp_supp " +
                        "   and s.nzp_payer_agent = " + _nzpPayerAgent +
                        // условие на услуги
                        (whereNzpServ != "" ? " and a.nzp_serv in (" + whereNzpServ + ") " : "") +
                        // условие на договоры
                        (whereNzpSupp != "" ? " and a.nzp_supp in (" + whereNzpSupp + ") " : "") +
                        // условие на дату операцию
                        whereDatOper +
                        // условие на принципала
                        (_nzpPayerPrincip > 0 ? " and a.nzp_payer = " + _nzpPayerPrincip : "") + 
                        " group by 1,2,3" +
                        " having sum(sum_rasp + sum_ud + sum_charge) > 0 ";

                    ret = ExecSQL(_connDb, sql);
                    if (!ret.result) throw new Exception(ret.text); 
                }

                // создать индексы
                sql = "create index ix_tmp_iut_01 on tmp_intermediate_unload_transfer (nzp_dom)";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text); 
                
                // обновить статистику
                sql = DBManager.sUpdStat + " tmp_intermediate_unload_transfer";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text); 

                // проставить коды банков
                sql = "update tmp_intermediate_unload_transfer a set nzp_wp = (select b.nzp_wp from " + Points.Pref + "_data" + DBManager.tableDelimiter + "dom b where a.nzp_dom = b.nzp_dom)";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text); 
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
            }

            return ret;
        }

        /// <summary>
        /// Подготовка промежуточных данных с распределеним сумм по домам
        /// </summary>
        /// <returns></returns>
        private Returns PreapareDataChargeUnload()
        {
            Returns ret = new Returns(true);

            try
            {
                string whereNzpWp = this.GetRolesCondition(_finder, Constants.role_sql_wp);
                
                // cгруппировать промежуточные данные по банкам, принципалам, услугам
                string sql = "insert into tmp_unload_transfer(point, nzp_payer_princip, nzp_serv, sum_rasp, sum_ud, sum_charge) " +
                    " select point.point, t.nzp_payer_princip, t.nzp_serv, sum(t.sum_rasp) sum_rasp, sum(t.sum_ud) sum_ud, sum(t.sum_charge) sum_charge " +
                    " from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_point point, " +
                        " tmp_intermediate_unload_transfer t " +
                    " where point.nzp_wp = t.nzp_wp " +
                    // условие на банки данных
                    (whereNzpWp != "" ? " and t.nzp_wp in (" + whereNzpWp + ") " : "") +
                    " group by 1,2,3 ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                // добавить информацию по принципалам
                sql = " update tmp_unload_transfer a set npayer = (select b.npayer from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer b where b.nzp_payer = a.nzp_payer_princip), " + 
                    " inn = (select b.inn from nftul_kernel.s_payer b where b.nzp_payer = a.nzp_payer_princip), " +
                    " kpp = (select b.kpp from nftul_kernel.s_payer b where b.nzp_payer = a.nzp_payer_princip); ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
            }

            return ret;
        }

        /// <summary>
        /// Получить залоговок выгрузки
        /// </summary>
        /// <param name="ret">Результат</param>
        /// <returns>Строка заголовка</returns>
        private string GetFileHead(out Returns ret)
        {
            // заголовок файла
            // 1. Тип строки
            // 2. Версия формата
            // 3. Наименование организации-отправителя 
            // 4. ИНН
            // 5. КПП
            // 6. № файла
            // 7. Дата файла
            // 8. Телефон отправителя
            // 9. ФИО отправителя
            // 10. Период выгрузки: дата начала
            // 11. Период выгрузки: дата окончания
            // 12. Количество записей в файле (включая заголовок)
            // 13. Общая сумма принятых платежей
            // 14. Общая сумма комиссионного сбора
            // 15.Общая сумма подлежит к перечислению

            // пример: 1|1.0|ООО ОЕИРЦ|710000121212|7101001|1|10.10.2014|71-71-71|Иванов И И|01.10.2014|10.10.2014|4|2000|15|1985

            ret = new Returns(true);
            
            try
            {
                string sql = "select npayer, inn, kpp from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer where nzp_payer = " + _nzpPayerAgent;

                ret = ExecRead(_connDb, out _reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (!_reader.Read())
                {
                    throw new Exception("Выгрузка перечислений в бух. систему. Не удалось определить данные агента");
                }

                string fileHead = _fileHeadStringCode + "|" +
                    _versionNumber + "|" +
                    _agentName + "|" +
                    _reader["inn"].ToString() + "|" +
                    _reader["kpp"].ToString() + "|" +
                    _nzpExc + "|" +
                    _fileDate + "|";

                // телефон отправителя = Телефон ИРЦ
                sql = "select max(val_prm) as val_prm from " + Points.Pref + "_data" + DBManager.tableDelimiter + "prm_10 " +
                    " where nzp_prm = 96 " + 
                    "   and is_actual = 1 " +
                    "   and " + DBManager.sCurDate + " between dat_s and dat_po";

                ret = ExecRead(_connDb, out _reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (_reader.Read())
                {
                    fileHead += _reader["val_prm"].ToString().Trim();    
                }

                fileHead  += "|" +
                    _userName + "|" +
                    _datOperS.ToShortDateString() + "|" +
                    _datOperPo.ToShortDateString();

                // количество записей, общие суммы
                sql = "select count(*) + 1 as  cnt, " +
                    DBManager.sNvlWord + "(sum(sum_rasp), 0.00) as sum_rasp, " +
                    DBManager.sNvlWord + "(sum(sum_ud), 0.00) as sum_ud, " +
                    DBManager.sNvlWord + "(sum(sum_charge), 0.00) as sum_charge " +
                    " from tmp_unload_transfer ";
                ret = ExecRead(_connDb, out _reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (_reader.Read())
                {
                    fileHead += "|" + _reader["cnt"].ToString() +
                    "|" + ((Decimal)_reader["sum_rasp"]).ToString("0.00") +
                    "|" + ((Decimal)_reader["sum_ud"]).ToString("0.00") +
                    "|" + ((Decimal)_reader["sum_charge"]).ToString("0.00");
                }
                else
                {
                    fileHead += "|1|0.00|0.00|0.00"; 
                }
                
                return fileHead;
            }
            catch (Exception ex)
            {
                ret.text = ex.Message;
                ret.result = false;
                return "";
            }
        }

        /// <summary>
        /// Получить имя агента
        /// </summary>
        /// <param name="ret">Результат выполнения</param>
        /// <returns></returns>
        private string GetAgentName(out Returns ret)
        { 
            ret = new Returns(true);
            
            try
            {
                string sql = "select npayer from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer where nzp_payer = " + _nzpPayerAgent;

                ret = ExecRead(_connDb, out _reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (_reader.Read())
                {
                    return _reader["npayer"].ToString();
                }
                else
                {
                    throw new Exception("Выгрузка перечислений в бух. систему. Не удалось определить наименование агента");
                }
            }
            catch (Exception ex)
            {
                ret.text = ex.Message;
                ret.result = false;
                return "";
            }
        }

        /// <summary>
        /// Сформировать имя файла
        /// </summary>
        /// <returns>string имя файла</returns>
        private string GetFileNameChargeUnload()
        {
            string fileNameKassa = "BuhSysChargeUnload_" + RandomText.Generate() + "_" +
                _nzpPayerAgent + "_" +
                DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            
            fileNameKassa = Path.Combine(Constants.Directories.ReportDir, fileNameKassa);
            return fileNameKassa;
        }

        /// <summary>
        /// Запись данных в файл
        /// </summary>
        /// <param name="fileNameIn">Имя файла</param>
        /// <returns>Результат</returns>
        private Returns WriteToFile(string fileNameIn)
        {
            Returns ret = new Returns(true);
            
            string dir = Path.Combine(Constants.ExcelDir, fileNameIn);
            FileStream memstr = new FileStream(dir, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            StreamWriter writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

            try
            {
                // получить заголовок файла
                string fileHead = GetFileHead(out ret);
                if (!ret.result) throw new Exception(ret.text);

                // записать в файл залоговок
                writer.WriteLine(fileHead);

                // получить данные
                var dt = ClassDBUtils.OpenSQL("select * from tmp_unload_transfer order by point, npayer, nzp_serv", _connDb);
                if (dt.resultCode < 0) return new Returns(false, dt.resultMessage);

                int num = dt.resultData.Rows.Count;

                for (int j = 0; j < dt.resultData.Rows.Count; j++)
                {
                    /// Cформировать строчку для записи в файл
                    writer.WriteLine(AssemblyOneString(dt.resultData.Rows[j]));
                    if (j % 100 == 0) _excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = _nzpExc, progress = ((decimal)j) / num });
                }

                writer.Flush();
                writer.Close();
                memstr.Close();

                if (InputOutput.useFtp)
                {
                    fileNameIn = InputOutput.SaveOutputFile(dir);

                }
            }
            catch (Exception ex)
            {
                writer.Flush();
                writer.Close();
                memstr.Close();

                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
                if (ret.result)
                {
                    _excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = _nzpExc, progress = 1 });
                    _excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = _nzpExc, status = ExcelUtility.Statuses.Success, exc_path = Path.GetFileName(fileNameIn) });
                }
            }

            return ret;
        }

        /// <summary>
        /// Cформировать строчку для записи в файл
        /// </summary>
        /// <param name="row">DataRow</param>
        /// <returns>Строка тела выгрузки</returns>
        protected string AssemblyOneString(DataRow row)
        {
            //1. Тип строки
            //2. Район
            //3. ИНН
            //4. КПП
            //5. Наименование организации – принципала
            //6. Код услуги 
            //7. Приняты платежи от населения
            //8. Комиссия (с НДС)
            //9. Подлежит к перечислению

            //2|XXXX-кий район|7102000099|7101001|МУП Водоканал|6|1000|10|990
            //2|XXXX-кий район|7102000099|7101001|МУП Водоканал|7|500|5|495
            //2|УУУУ-кий район|7102000011|7101001|ООО ЦвеТОЧКИ|26|500|0|500

            string s = _fileBodyStringCode + "|" + 
                (row["point"] != DBNull.Value ? ((string)row["point"]).Trim().Replace("|", "") : "") + "|" +
                (row["inn"] != DBNull.Value ? ((string)row["inn"]).Trim().Replace("|", "") : "") + "|" +
                (row["kpp"] != DBNull.Value ? ((string)row["kpp"]).Trim().Replace("|", "") : "") + "|" +
                (row["npayer"] != DBNull.Value ? ((string)row["npayer"]).Trim().Replace("|", "") : "") + "|" +
                (row["npayer"] != DBNull.Value ? ((int)row["nzp_serv"]).ToString() : "") + "|" +
                (row["sum_rasp"] != DBNull.Value ? ((Decimal)row["sum_charge"]).ToString("0.00") : "") + "|" +
                (row["sum_ud"] != DBNull.Value ? ((Decimal)row["sum_charge"]).ToString("0.00") : "")  + "|" +
                (row["sum_charge"] != DBNull.Value ? ((Decimal)row["sum_charge"]).ToString("0.00") : "");
            
            return s;
        }

        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        /// <returns>Результат</returns>
        private Returns CheckInputPrm()
        {
            Returns ret = new Returns(false);

            if (_nzpUser < 1)
            {
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (_nzpPayerAgent < 1)
            {
                ret.text = "Не определен платежный агент";
                return ret;
            }

            DateTime tmp_date = new DateTime();

            if (!DateTime.TryParse(_inDatOperS, out tmp_date))
            {
                ret.text = "Неверная дата начала периода";
                return ret;
            }

            if (!DateTime.TryParse(_inDatOperPo, out tmp_date))
            {
                ret.text = "Неверная дата конца периода";
                return ret;
            }

            ret.result = true;
            return ret;
        }
    }
}
