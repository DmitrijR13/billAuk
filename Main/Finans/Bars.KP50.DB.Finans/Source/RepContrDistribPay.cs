using System;
using System.Collections.Generic;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Linq;
using System.IO;
using FastReport;

namespace STCLINE.KP50.DataBase
{
    public partial class DbPack : DbPackClient
    {
        /// <summary>
        /// Отчет FastReport - "Контроль распределения оплат"
        /// </summary>
        /// <param name="pay">Объект с таблицами оплат</param>
        /// <returns>Имя файла отчета</returns>
        public Returns GenConDistrPayments(Payments pay)
        {
            return GenConDistrPayments(pay, false, null);
        }

        /// <summary>
        /// Отчет FastReport - "Контроль распределения оплат"
        /// </summary>
        /// <param name="pay">Объект с таблицами оплат</param>
        /// <returns>Имя файла отчета</returns>
        public Returns GenConDistrPaymentsPDF(Payments pay, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            return GenConDistrPayments(pay, true, setTaskProgress);
        }

        /// <summary>
        /// Отчет FastReport - "Контроль распределения оплат"
        /// </summary>
        /// <param name="pay">Объект с таблицами оплат</param>
        /// <returns>Имя файла отчета</returns>
        public Returns GenConDistrPayments(Payments pay, bool inPdf, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            using (var report = new PaymentDistributionControl())
            {
                return report.GetReport(pay, inPdf, setTaskProgress);
            }
        }
    }
    
    /// <summary>
    /// Отчет "Контроль распределения оплат"
    /// </summary>
    public class PaymentDistributionControl : DataBaseHead
    {
        // соединение с базой
        private IDbConnection _connDb;
        
        // период
        private DateTime _datS;
        private DateTime _datPo;
        private string _whereDat;
        private int _nzpWp;
        private List<string> _prefList;
        private bool _showSumMoney;
        private bool _checkCanChangeOperDay;
        private bool _hideEqualSum;

        private DataTable _dtCenterPointPackLs = null;
        private DataTable _dtCenterPayerPackLs = null;
        private DataTable _dtPayerPackLs = null;
        private DataTable _dtSuppPackLs = null;
        private DataTable _dt321 = null;
        private DataTable _dt322 = null;
        private DataTable _dtPointPack = null;
        private DataTable _dtSuppPack = null;
        private DataTable _dtUnlinkSupp = null;
        
        /// <summary>
        /// Получить отчет
        /// </summary>
        /// <param name="pay"></param>
        /// <param name="inPdf"></param>
        /// <returns></returns>
        public Returns GetReport(Payments pay, bool inPdf, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            Returns ret = new Returns(true);
          
            ret = CheckIncomingPrms(pay);
            if (!ret.result) return ret;

            _connDb = GetConnection(Points.GetConnByPref(Points.Pref));
            ret = OpenDb(_connDb, true);
            if (!ret.result) throw new Exception(ret.text);

            //записываем в "Мои файлы"
            int id = 0;
            ExcelRepClient dbRep = new ExcelRepClient();
            try
            {
                ret = dbRep.AddMyFile(new ExcelUtility()
                {
                    nzp_user = pay.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Контроль распределения оплат "
                });
                if (!ret.result)
                {
                    _connDb.Close();
                    return ret;
                }
                id = ret.tag;


                _datS = Convert.ToDateTime(pay.dat_s);
                _datPo = Convert.ToDateTime(pay.dat_po);
                _nzpWp = pay.nzp_wp;
                _checkCanChangeOperDay = pay.checkCanChangeOperDay;
                _hideEqualSum = pay.hide_equal;

                // сформировать условие на дату учета
                if (_datS == _datPo)
                {
                    _whereDat = " = " + Utils.EStrNull(_datS.ToShortDateString());
                }
                else
                {
                    _whereDat = " between " + Utils.EStrNull(_datS.ToShortDateString()) + " and " +
                                Utils.EStrNull(_datPo.ToShortDateString());
                }

                _prefList = new List<string>();
                ret = GetPrefList(pay.nzp_wp, out _prefList);
                if (!ret.result) throw new Exception(ret.text);

                // показывать столбец "Учтено в расчетах"
                _showSumMoney = true;
                DateTime dd1 = new DateTime(_datS.Year, _datS.Month, 1);
                DateTime dd2 = new DateTime(_datPo.Year, _datPo.Month, 1);
                dd2 = dd2.AddMonths(1).AddDays(-1);
                _showSumMoney = (_datPo - _datS).TotalDays == (dd2 - dd1).TotalDays;

                // Контроль распределения по квитанциям
                // ... таблица 1. Средства РЦ
                ret = GetTable1PaymentCenterReceipt(out _dtCenterPointPackLs, "center_point_pack_ls");
                if (!ret.result) throw new Exception(ret.text);

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) 0.1
                });
                if (setTaskProgress != null) setTaskProgress((decimal) 0.1);

                // ... таблица 2. Средства РЦ по принципалам
                ret = GetTable2PrincipalReceipt(out _dtCenterPayerPackLs, "center_payer_pack_ls");
                if (!ret.result) throw new Exception(ret.text);

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) 0.2
                });
                if (setTaskProgress != null) setTaskProgress((decimal) 0.2);

                // ... таблица 3. Средства сторонних организаций по банкам
                ret = GetTable3PaymentUKiPU(out _dt321, "Q321");
                if (!ret.result) throw new Exception(ret.text);

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) 0.3
                });
                if (setTaskProgress != null) setTaskProgress((decimal) 0.3);

                //ret = GetTable31UkiPUSupp(out _dt322, "Q322");
                //if (!ret.result) throw new Exception(ret.text);

                //dbRep.SetMyFileProgress(new ExcelUtility()
                //{
                //    nzp_exc = id,
                //    progress = (decimal)0.4
                //});


                // ... таблица 3.1. Платежи принципалов
                ret = GetTable31Princip(out _dtPayerPackLs, "payer_pack_ls");
                if (!ret.result) throw new Exception(ret.text);

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) .4
                });

                if (setTaskProgress != null) setTaskProgress((decimal) 0.4);
                // ... таблица 3.2. Платежи по договорам
                ret = GetTable32Supplier(out _dtSuppPackLs, "supp_pack_ls");
                if (!ret.result) throw new Exception(ret.text);

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) .5
                });
                if (setTaskProgress != null) setTaskProgress((decimal) 0.5);

                //// ... таблица 3.2.1 Средства принципалов и поставщиков по банкам данных.
                //// ... таблица 3.2.2  Платежи по договорам
                //ret = GetTable3_2(out _dt321, out _dt322);
                //if (!ret.result) throw new Exception(ret.text);

                // Контроль распределения по пачкам
                // ... таблица 4. Средства РЦ
                ret = GetTable4PaymentCenterPack(out _dtPointPack);
                if (!ret.result) throw new Exception(ret.text);

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) .6
                });
                if (setTaskProgress != null) setTaskProgress((decimal) 0.6);

                // ... таблица 5. Средства сторонних организаций
                ret = GetTable5PrincipalPack(out _dtSuppPack);
                if (!ret.result) throw new Exception(ret.text);

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) 0.7
                });
                if (setTaskProgress != null) setTaskProgress((decimal) 0.7);

                // таблица 6. Непривязанные к контрагентам поставщики
                ret = GetTable6UnlinkSupp(out _dtUnlinkSupp, "unlink_supp");
                if (!ret.result) throw new Exception(ret.text);

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) .8
                });
                if (setTaskProgress != null) setTaskProgress((decimal) 0.8);

                #region проверка возможности смены операционного дня

                //-------------------------------------------------------------------
                bool canChangeOperDay = true;

                if (pay.checkCanChangeOperDay)
                {
                    if (_dtCenterPointPackLs.Rows.Count > 0 || _dtCenterPayerPackLs.Rows.Count > 0 ||
                        _dtPayerPackLs.Rows.Count > 0
                        || _dt321.Rows.Count > 0 //|| _dt322.Rows.Count > 0 
                        //|| _dtSuppPackLs.Rows.Count > 0
                        )
                    {
                        canChangeOperDay = false;
                    }
                }
                //-------------------------------------------------------------------                

                #endregion

                dbRep.SetMyFileProgress(new ExcelUtility()
                {
                    nzp_exc = id,
                    progress = (decimal) .9
                });
                if (setTaskProgress != null) setTaskProgress((decimal) 0.9);

                ret = PrepareReport(pay, inPdf);
                //    if (!ret.result) throw new Exception(ret.text);

                if (ret.result)
                    dbRep.SetMyFileState(
                        (new ExcelUtility() {nzp_exc = id, status = ExcelUtility.Statuses.Success, exc_path = ret.text}));
                else dbRep.SetMyFileState((new ExcelUtility() {nzp_exc = id, status = ExcelUtility.Statuses.Failed}));

                // была выполнена проверка возможности закрытия опер. дня, результат проверки - опер. день можно закрыть
                // возращаем true
                if (pay.checkCanChangeOperDay && canChangeOperDay) return new Returns(true);

                // если не надо формировать отчет и нельзя изменить опер. день, то вернуть результат
                if (!pay.prepareContrDistribReport && !canChangeOperDay) return new Returns(true, "", -7);

                return ret;
            }
            catch (Exception e)
            {
                dbRep.SetMyFileState((new ExcelUtility() {nzp_exc = id, status = ExcelUtility.Statuses.Failed}));
                MonitorLog.WriteLog("Ошибка формирования отчета \"Контроль распределения оплат\" " + e.Message,
                    MonitorLog.typelog.Error, 20, 201, true);
                return new Returns(false, e.Message);
            }
            finally
            {
                _connDb.Close();
                dbRep.Close();
            }
        }
        
        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        /// <param name="pay"></param>
        /// <returns></returns>
        private Returns CheckIncomingPrms(Payments pay)
        {
            Returns ret = new Returns(true);
            
            // проверка месяца
            if (pay.dat_s == "")
            {
                return new Returns(false, "Не определена начальная дата учета");
            }

            if (pay.dat_po == "")
            {
                return new Returns(false, "Не определена конечная дата учета");
            }

            DateTime tmpDate;

            if (!DateTime.TryParse(pay.dat_s, out tmpDate))
            {
                return new Returns(false, "Неверный формат начальной даты учета: " + pay.dat_s);
            }
            
            if (!DateTime.TryParse(pay.dat_po, out tmpDate))
            {
                return new Returns(false, "Неверный формат конечной даты учета: " + pay.dat_po);
            }

            return ret;
        }

        /// <summary>
        /// Получить список префиксов
        /// </summary>
        /// <returns></returns>
        private Returns GetPrefList(int nzpWp, out List<string> prefList)
        {
            IDataReader reader = null;
            prefList = new List<string>();
            Returns ret = new Returns(true);

            try
            {
                if (nzpWp > 0)
                {
                    string sql = "Select distinct bd_kernel From " + Points.Pref + "_kernel" + tableDelimiter + "s_point  " +
                        " where nzp_wp = " +_nzpWp +
                        " order by 1";

                    ret = ExecRead(_connDb, out reader, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    while (reader.Read())
                    {
                        if (reader["bd_kernel"] != DBNull.Value) prefList.Add(Convert.ToString(reader["bd_kernel"]).Trim());
                    }
                    reader.Close();
                }
                else
                {
                    for (int i = 0; i < Points.PointList.Count; i++) prefList.Add(Points.PointList[i].pref);
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);    
            }

            return ret;
        }

        /// <summary>
        /// Подготовить отчет
        /// </summary>
        /// <param name="pay"></param>
        /// <param name="inPdf"></param>
        /// <returns></returns>
        private Returns PrepareReport(Payments pay, bool inPdf)
        {
            Returns ret = new Returns(true);

            try
            {
                DataSet ds_rep = new DataSet();

                ds_rep.Tables.Add(_dtCenterPointPackLs);
                ds_rep.Tables.Add(_dtCenterPayerPackLs);
                ds_rep.Tables.Add(_dtPayerPackLs);
                ds_rep.Tables.Add(_dtSuppPackLs);
                ds_rep.Tables.Add(_dt321);
                //ds_rep.Tables.Add(_dt322);
                ds_rep.Tables.Add(_dtPointPack);
                ds_rep.Tables.Add(_dtSuppPack);
                ds_rep.Tables.Add(_dtUnlinkSupp);

                FastReport.Report rep = new FastReport.Report();
                rep.Load(System.IO.Directory.GetCurrentDirectory() + @"\Template\condistrpayments.frx");

                rep.RegisterData(ds_rep);

                //параметры
                rep.SetParameterValue("dat_s", pay.dat_s);
                rep.SetParameterValue("dat_po", pay.dat_po);

                rep.SetParameterValue("time_now", DateTime.Now.ToString());
                rep.SetParameterValue("uname", pay.uname);

                rep.SetParameterValue("showSumMoney", (_showSumMoney ? "1" : "0"));

                if (pay.hide_equal) rep.SetParameterValue("reportView", "Только несовпадающие суммы по квитанциям");
                else rep.SetParameterValue("reportView", "Все суммы");

                string fileName = "";
                string filePath = "";
                FastReport.EnvironmentSettings env = new FastReport.EnvironmentSettings();
                env.ReportSettings.ShowProgress = false;
                rep.Prepare();
                
                var dir = "";
                if (InputOutput.useFtp) dir = InputOutput.GetOutputDir();
                else dir = STCLINE.KP50.Global.Constants.ExcelDir;

                fileName = (pay.nzp_user * DateTime.Now.Second) + "_" + DateTime.Now.Ticks + "_conDistribPay.fpx";
                filePath = dir + fileName;
                if (inPdf)
                {
                    fileName = (pay.nzp_user * DateTime.Now.Second) + "_" + DateTime.Now.Ticks + "_conDistribPay.pdf";
                filePath = dir + fileName;
                    var exportPdf = new FastReport.Export.Pdf.PDFExport { ShowProgress = false };
                   exportPdf.Compressed = false;
                    exportPdf.Export(rep, filePath);

                    ret.text = fileName;//Convert.ToBase64String(ms.ToArray());
                    //if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(Path.Combine(dir, filePath));
                    //// была выполнена проверка возможности закрытия опер. дня, результат проверки - опер. день нельзя закрыть, возвращаем спец. код -7
                    //if (pay.checkCanChangeOperDay) return new Returns(true, fileName, -7);
                    //else return new Returns(true, fileName);
                }
                else
                {
                    rep.SavePrepared(filePath);

                   
                } if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(Path.Combine(dir, filePath));
                    // была выполнена проверка возможности закрытия опер. дня, результат проверки - опер. день нельзя закрыть, возвращаем спец. код -7
                    if (pay.checkCanChangeOperDay) return new Returns(true, fileName, -7);
                    else return new Returns(true, fileName);
            }
            catch (Exception ex)
            {
                return new Returns(false, "Ошибка формирования отчета \"Контроль распределения оплат\" " + ex.Message);
            }
        }
        
        /// <summary>
        /// Получить количество месяцев между датами _datS и _datPo
        /// </summary>
        /// <returns></returns>
        private int GetMonthCount()
        {
            return (_datPo.Year - _datS.Year) * 12 - _datS.Month + _datPo.Month + 1;    
        }

        /// <summary>
        /// Получить количество годов между датами _datS и _datPo
        /// </summary>
        /// <returns></returns>
        private int GetYearCount()
        {
            return _datPo.Year - _datS.Year + 1;
        }

        /// <summary>
        /// Вставить во временную таблицу tempTable суммы из поля moneyField с группировкой по полю groupField
        /// </summary>
        /// <returns></returns>
        private Returns InsertIntoTempTableMoneyFieldFromCharge(string tempTable, string groupField, 
            string groupFieldType, string moneyField, bool isCalcmoneyField)
        {
            Returns ret = new Returns(true);

            groupField = groupField.Trim().ToLower();

            try
            {
                ExecSQL(_connDb, " Drop table " + tempTable, false);

                string sql = "Create temp table " + tempTable +
                    "(" + groupField + " " + groupFieldType + ", " +
                    moneyField + " " + DBManager.sDecimalType + "(14,2) default 0 " +
                    ") ";

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                if (!isCalcmoneyField) return ret;

                if (_showSumMoney)
                {
                    int monthCount = GetMonthCount();
                    DateTime curMonth = new DateTime(_datS.Year, _datS.Month, 1);

                    for (int j = 0; j < monthCount; j++)
                    {
                        string curr_year = (curMonth.Year % 100).ToString("00");
                        string curr_month = curMonth.Month.ToString("00");

                        // выполнить цикл по префиксам
                        foreach (string cur_pref in _prefList)
                        {
                            sql = " insert into " + tempTable + " (" + groupField + ", " + moneyField + ") " +
                                " select " + (groupField == "pref" ? Utils.EStrNull(cur_pref) : DBManager.sNvlWord + "(" + groupField + ")") + "," +
                                "   sum(" + sNvlWord + "(ch." + moneyField + ", 0)) as " + moneyField +
                                " from " + cur_pref + "_charge_" + curr_year + tableDelimiter + "charge_" + curr_month + " ch " +
                                " where ch.dat_charge is null " +
                                "   and ch.nzp_serv > 1 " +
                                " group by 1";

                            ret = ExecSQL(_connDb, sql, true);
                            if (!ret.result) throw new Exception(ret.text);
                        }

                        curMonth = curMonth.AddMonths(1);
                    }//for
                }//if 
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Вставить во временную таблицу сумму sum_prih из {local_pref}_charge_{YY}.fn_supplier{MM}
        /// </summary>
        /// <param name="isOplRC">оплаты РЦ</param>
        /// <returns></returns>
        private Returns InsertIntoTempTalbeSumPrih(string tempTable, string groupField, bool isOplRC)
        {
            Returns ret = new Returns(true);

            try
            {
                string sql = "";
                groupField = groupField.Trim().ToLower();

                int monthCount = GetMonthCount();
                DateTime curMonth = new DateTime(_datS.Year, _datS.Month, 1);

                string where;
                string tableRaspr = "";
                if (isOplRC)
                {
                    where = "  and p.pack_type not in (20,30)"; // ... оплаты на расчётных счетах РЦ
                    tableRaspr = "fn_supplier" + curMonth.Month.ToString("00");
                }
                else
                {
                    where = "  and p.pack_type in (20, 30)";
                    tableRaspr = "from_supplier";
                }

                for (int j = 0; j < monthCount; j++)
                {
                    foreach (string cur_pref in _prefList)
                    {
                        sql = " insert into " + tempTable + "(" + groupField + ", sum_prih) " +
                            " select " + (groupField == "pref" ? Utils.EStrNull(cur_pref) : "fn." + groupField) + ", " +
                            "   sum(" + sNvlWord + "(fn.sum_prih, 0)) " +
                            " from " + cur_pref + "_charge_" + (curMonth.Year % 100).ToString("00") + tableDelimiter + tableRaspr + " fn, " +
                                Points.Pref + "_fin_" + (curMonth.Year % 100).ToString("00") + tableDelimiter + "pack p, " +
                                Points.Pref + "_fin_" + (curMonth.Year % 100).ToString("00") + tableDelimiter + "pack_ls pls " +
                            " where pls.nzp_pack_ls = fn.nzp_pack_ls " +
                            "   and p.nzp_pack = pls.nzp_pack " +
                            // ... условие по дате учета
                            "   and pls.dat_uchet " + _whereDat +
                            where +
                            " group by 1";
                        
                        ret = ExecSQL(_connDb, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }

                    curMonth = curMonth.AddMonths(1);
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Cохранить во временную таблицу уникальные значения поля из разных таблиц
        /// </summary>
        /// <param name="field"></param>
        /// <param name="tables"></param>
        /// <param name="resultTable"></param>
        /// <returns></returns>
        private Returns GetDistinctValues(string field, string[] tables, string resultTable)
        {
            Returns ret = new Returns();

            try
            {
                ExecSQL(_connDb, " Drop table tmp_list_union ", false);

                string sql = " Create temp table tmp_list_union (" +
                     field + " integer)";

                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                for (int i = 0; i < tables.Length; i++)
                {
                    sql = " insert into tmp_list_union (" + field + ")" +
                        " select distinct " + field + " from " + tables[i];

                    ret = ExecSQL(_connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }

                ExecSQL(_connDb, " Drop table " + resultTable, false);
                sql = " Create temp table " + resultTable + " (" + field + " integer)";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into " + resultTable + " (" + field + ") " +
                    " select distinct " + field + " from tmp_list_union ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                return ret;
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Таблица 1. Данные по средствам РЦ по квитанциям
        /// </summary>
        /// <returns></returns>
        private Returns GetTable1PaymentCenterReceipt(out DataTable dataTable, string tableName)
        {
            Returns ret = new Returns(true);
            dataTable = null;

            try
            {
                ret = GetTable1GSumLs1(true);
                if (!ret.result) throw new Exception(ret.text);

                ret = GetTable1SumPrih2(true);
                if (!ret.result) throw new Exception(ret.text);

                // Получить сумму "Учтено в расчетах" из {local_pref}_charge_{YY}.charge_{MM}.money_to
                ret = InsertIntoTempTableMoneyFieldFromCharge("tmp_wp_money", "pref", "char(10)", "money_to", true);
                if (!ret.result) throw new Exception(ret.text);

                ret = GetTable1SumRasp4();
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(_connDb, "drop table tmp_point_pack_ls", false);

                string sql = "Create temp table tmp_point_pack_ls (" +
                    " point    char(100), " +
                    " sum_prih " + DBManager.sDecimalType + "(14,2)  default 0 ," +
                    " sum_money " + DBManager.sDecimalType + "(14,2)  default 0 ," +
                    " sum_rasp " + DBManager.sDecimalType + "(14,2)  default 0 ," +
                    " pack_count integer  default 0 , " +
                    " kvit_count integer  default 0 , " +
                    " g_sum_ls " + DBManager.sDecimalType + "(14,2)  default 0 " + ") ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_point_pack_ls (point, sum_prih, sum_money, sum_rasp, pack_count, kvit_count, g_sum_ls) " +
                    " select " + sNvlWord + "(a.point, '!НЕ УКАЗАН'), " +
                    "   (select sum(a.sum_prih)  from tmp_wp_prih     a where trim(a.pref) = trim(t.pref)), " +
                    "   (select sum(b.money_to)  from tmp_wp_money    b where trim(b.pref) = trim(t.pref)), " +
                    "   (select sum(r.sum_rasp)  from tmp_wp_sum_rasp r where trim(r.pref) = trim(t.pref)), " +
                    "   sum(t.pack_count), " +
                    "   sum(t.kvit_count), " +
                    "   sum(t.g_sum_ls) " +
                    " from tmp_wp_pack_ls t " +
                    "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_point a on a.bd_kernel = trim(t.pref) " +
                    " group by 1,2,3,4 ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                // оставить только те строки, в которых не совпадают суммы
                if (_hideEqualSum || _checkCanChangeOperDay)
                {
                    sql = " delete from tmp_point_pack_ls where " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_prih, 0) ";
                    if (_showSumMoney) sql += " and " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_money, 0) ";
                    sql += " and " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_rasp, 0) ";

                    ret = ExecSQL(_connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }

                dataTable = ClassDBUtils.OpenSQL("select * from tmp_point_pack_ls order by point", tableName, _connDb).GetData();
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Таблица 1. Получить сумму "Распределено" из {Points.Pref}_fin_{YY}.pack_ls.g_sum_ls
        /// </summary>
        /// <param name="isOPlRC">Признак для проверки типа пачек</param>
        /// true - оплаты РЦ (pack_type <> 20)
        /// false - чужие оплаты (pack_type = 20)
        /// <returns></returns>
        private Returns GetTable1GSumLs1(bool isOPlRC)
        {
            Returns ret = new Returns(true);
            
            try
            {
                ExecSQL(_connDb, " Drop table tmp_wp_pack_ls", false);

                string sql = "Create temp table tmp_wp_pack_ls" +
                      "( pref        char(20), " +
                      "  pack_count  int  default 0 , " +
                      "  kvit_count  int  default 0 , " +
                      "  g_sum_ls    " + DBManager.sDecimalType + "(14,2)  default 0 " +
                      ") ";

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                string where = "";
                if (isOPlRC) where += "   and p.pack_type not in (20, 30)"; // ... оплаты на расчётных счетах РЦ 
                else where += "  and p.pack_type in (20, 30) ";// ... чужие оплаты 

                int yearCnt = GetYearCount();

                for (int i = 0; i < yearCnt; i++)
                {
                    string curr_year = ((_datS.Year + i) % 100).ToString("00");

                    sql = " insert into tmp_wp_pack_ls (pref, pack_count, kvit_count, g_sum_ls) " +
                        " select " + sNvlWord + "(k.pref, ''), " +
                        "    count(distinct p.nzp_pack), " +
                        "    count(pls.nzp_pack_ls), " +
                        "    sum(" + sNvlWord + "(pls.g_sum_ls, 0)) " +
                        " from " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack p, " +
                                   Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls pls " +
                        "   left outer join " + Points.Pref + "_data" + tableDelimiter + "kvar k on pls.num_ls = k.num_ls " +
                        " where p.nzp_pack = pls.nzp_pack " +
                        "    and " + sNvlWord + "(cast(pls.alg as int),0) <> 0 " +
                        "    and pls.inbasket = 0 " +
                        where +
                        (_nzpWp > 0 ? " and k.nzp_wp = " + _nzpWp : "") +
                        // ... условие по дате учета
                        "   and pls.dat_uchet " + _whereDat +
                        " group by 1 ";

                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Таблица 1. Получить сумму "Учтено в лицевых счетах" из {local_pref}_charge_{YY}.fn_supplier{MM}.sum_prih
        /// </summary>
        /// <returns></returns>
        private Returns GetTable1SumPrih2(bool isOplRC)
        {
            Returns ret = new Returns(true);
            
            try
            {
                ExecSQL(_connDb, " Drop table tmp_wp_prih ", false);

                string sql = "Create temp table tmp_wp_prih " +
                    "( pref        char(10), " +
                    "  sum_prih    " + DBManager.sDecimalType + "(14,2)  default 0 " + ") ";

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                ret = InsertIntoTempTalbeSumPrih("tmp_wp_prih", "pref", isOplRC);
                if (!ret.result) throw new Exception(ret.text);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Таблица 1. Получить сумму "Учтено для перечисления" из {Points.Pref}_fin_{YY}.fn_distrib_dom{MM}.sum_rasp
        /// </summary>
        /// <returns></returns>
        private Returns GetTable1SumRasp4()
        {
            Returns ret = new Returns(true);

            try
            {
                ExecSQL(_connDb, " Drop table tmp_wp_sum_rasp ", false);

                string sql = "Create temp table tmp_wp_sum_rasp " +
                    "( pref        char(10), " +
                    "  sum_rasp    " + DBManager.sDecimalType + "(14,2)  default 0 " +
                    ") ";

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                int monthCount = GetMonthCount();
                DateTime curMonth = new DateTime(_datS.Year, _datS.Month, 1);

                for (int j = 0; j < monthCount; j++)
                {
                    string curr_year = (curMonth.Year % 100).ToString("00");
                    string curr_month = curMonth.Month.ToString("00");

                    sql = " insert into tmp_wp_sum_rasp (pref, sum_rasp) " +
                        " select d.pref, sum(" + sNvlWord + "(fd.sum_rasp, 0)) " +
                        " from " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "fn_distrib_dom_" + curr_month + " fd, " +
                                   Points.Pref + "_data" + tableDelimiter + "dom d " +
                        " where fd.nzp_dom = d.nzp_dom " +
                        // ... условие по дате
                        "    and fd.dat_oper " + _whereDat +
                        (_nzpWp > 0 ? " and d.nzp_wp = " + _nzpWp : "") +
                        " group by 1 ";

                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    curMonth = curMonth.AddMonths(1);
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Таблица 2. Средства РЦ по принципалам 
        /// Распределение денежных средств квитанций по принципалам
        /// </summary>
        /// <returns></returns>
        private Returns GetTable2PrincipalReceipt(out DataTable dataTable, string tableName)
        {
            Returns ret = new Returns(true);
            dataTable = null;

            try
            {
                ret = GetTable2PrincipalReceiptSumPrih1(true);
                if (!ret.result) throw new Exception(ret.text);

                // Получить сумму "Учтено в расчетах" из {local_pref}_charge_{YY}.charge_{MM}.money_to
                ret = InsertIntoTempTableMoneyFieldFromCharge("tmp_supplier_money", "nzp_supp", "int", "money_to", true);
                if (!ret.result) throw new Exception(ret.text);

                ret = GetTable2PrincipalReceiptSumRasp3();
                if (!ret.result) throw new Exception(ret.text);

                // получить список всех договоров
                ret = GetDistinctValues("nzp_supp", new string[] { "tmp_supplier_prih", "tmp_supplier_money", "tmp_supplier_sum_rasp" }, "tmp_supplier_list");
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(_connDb, "drop table tmp_supplier_pack_ls", false);

                string sql = " Create temp table tmp_supplier_pack_ls (" +
                    " nzp_payer int,  " +
                    " payer char(100), " +
                    " sum_prih " + DBManager.sDecimalType + "(14,2) default 0 " + "," +
                    " sum_money " + DBManager.sDecimalType + "(14,2) default 0 " + "," +
                    " sum_rasp " + DBManager.sDecimalType + "(14,2) default 0 " + ") ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_supplier_pack_ls (nzp_payer, payer, sum_prih, sum_money, sum_rasp) " +
                    " select " + sNvlWord + "(p.nzp_payer, 0), " + sNvlWord + "(p.payer, '!НЕ УКАЗАН'), " +
                    "   sum((select sum(a.sum_prih)  from tmp_supplier_prih     a where a.nzp_supp = t.nzp_supp)), " +
                    "   sum((select sum(b.money_to)  from tmp_supplier_money    b where b.nzp_supp = t.nzp_supp)), " +
                    "   sum((select sum(r.sum_rasp)  from tmp_supplier_sum_rasp r where r.nzp_supp = t.nzp_supp)) " +
                    " from tmp_supplier_list t " +
                    "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier a on a.nzp_supp =  t.nzp_supp " +
                    "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer  p on p.nzp_payer = a.nzp_payer_princip " +
                    " group by 1, 2";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                // удалить строки с одинаковыми суммами
                if (_hideEqualSum || _checkCanChangeOperDay)
                {
                    sql = " delete from tmp_supplier_pack_ls where 1=1 ";
                    if (_showSumMoney) sql += " and " + DBManager.sNvlWord + "(sum_prih, 0) = " + DBManager.sNvlWord + "(sum_money, 0) ";
                    sql += " and " + DBManager.sNvlWord + "(sum_prih, 0) = " + DBManager.sNvlWord + "(sum_rasp, 0) ";

                    ret = ExecSQL(_connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }

                dataTable = ClassDBUtils.OpenSQL("select * from tmp_supplier_pack_ls order by payer", tableName, _connDb).GetData();
            }
            catch (Exception ex)
            { 
                return new Returns(false, ex.Message);
            }
            
            return ret;
        }

        /// <summary>
        /// Таблица 2. Суммы "Учтено в лицевых счетах" {local_pref}_charge_{YY}.fn_supplier{MM}.sum_prih
        /// </summary>
        /// <returns></returns>
        private Returns GetTable2PrincipalReceiptSumPrih1(bool isOplRC)
        {
            Returns ret = new Returns();

            try
            {
                ExecSQL(_connDb, " Drop table tmp_supplier_prih ", false);

                string sql = "Create temp table tmp_supplier_prih " +
                    "( nzp_supp    int, " +
                    "  sum_prih    " + DBManager.sDecimalType + "(14,2) default 0 " +
                    ") " + DBManager.sUnlogTempTable;

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                ret = InsertIntoTempTalbeSumPrih("tmp_supplier_prih", "nzp_supp", isOplRC);
                if (!ret.result) throw new Exception(ret.text);

            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Таблица 2. Суммы "Учтено для перечисления" {Points.Pref}_fin_{YY}fn_distrib_dom_{MM}.sum_rasp
        /// </summary>
        /// <returns></returns>
        private Returns GetTable2PrincipalReceiptSumRasp3()
        {
            Returns ret = new Returns();
            
            try
            {
                ExecSQL(_connDb, " Drop table tmp_supplier_sum_rasp ", false);

                string sql = "Create temp table tmp_supplier_sum_rasp " +
                    "( nzp_supp    int, " +
                    "  sum_rasp    " + DBManager.sDecimalType + "(14,2) default 0 " + 
                    ") " + DBManager.sUnlogTempTable;

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                int monthCount = GetMonthCount();
                DateTime curMonth = new DateTime(_datS.Year, _datS.Month, 1);

                for (int j = 0; j < monthCount; j++)
                {
                    string curr_year = (curMonth.Year % 100).ToString("00");
                    string curr_month = curMonth.Month.ToString("00");

                    sql = " insert into tmp_supplier_sum_rasp (nzp_supp, sum_rasp) " +
                        " select fd.nzp_supp, sum(" + sNvlWord + "(fd.sum_rasp, 0)) as sum_rasp " +
                        " from " + Points.Pref + "_fin_" + curr_year + tableDelimiter + " fn_distrib_dom_" + curr_month + " fd, " +
                        Points.Pref + "_data" + tableDelimiter + "dom d " +
                        " where fd.nzp_dom = d.nzp_dom " +
                        // ... условие на дату
                        "   and fd.dat_oper " + _whereDat +
                        // ... условие на банк данных
                        (_nzpWp > 0 ? " and d.nzp_wp = " + _nzpWp : "") +
                        " group by 1 ";

                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                    
                    curMonth = curMonth.AddMonths(1);
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Таблица 3.# Получить сумму "Распределено"
        /// </summary>
        /// <param name="tempTable">имя временной таблицы</param>
        /// <param name="groupField">поле, по которому выполняется группировка</param>
        /// <returns></returns>
        private Returns GetTable3GSumLs1(string tempTable, string groupField, string kod_sum)
        {
            Returns ret = new Returns(true);

            try
            {
                ExecSQL(_connDb, " Drop table " + tempTable, false);

                string sql = "Create temp table " + tempTable +
                  "( " + groupField + " int, " +
                  "  pack_count  int default 0 , " +
                  "  kvit_count  int default 0 , " +
                  "  g_sum_ls    " + DBManager.sDecimalType + "(14,2) default 0 " +
                  ") ";

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                string where = "";
                if (kod_sum != "") where = "   and pls.kod_sum in (" + kod_sum+")"; 

                int yearCnt = GetYearCount();

                for (int i = 0; i < yearCnt; i++)
                {
                    string curr_year = ((_datS.Year + i) % 100).ToString("00");

                    sql = " insert into " + tempTable + " (" + groupField + ", pack_count, kvit_count, g_sum_ls)" +
                        "select " + sNvlWord + "(pls." + groupField + ", 0), " + 
                        "   count(distinct p.nzp_pack), " +
                        "   count(pls.nzp_pack_ls), " +
                        "   sum (" + sNvlWord + "(pls.g_sum_ls, 0)) " +
                        " from " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack p, " +
                            Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls pls ";

                    // если указан банк данных, соединиться с таблицей kvar
                    if (_nzpWp > 0) sql += " left outer join " + Points.Pref + "_data" + tableDelimiter + "kvar k on pls.num_ls = k.num_ls ";

                    sql += " where p.nzp_pack = pls.nzp_pack " +
                           // ... условие на дату
                           "   and pls.dat_uchet " + _whereDat +
                           "   and " + sNvlWord + "(cast(pls.alg as int),0) <> 0 " +
                           "   and pls.inbasket = 0 " +
                           // ... оплаты от сторонних организаций
                           "   and p.pack_type in (20, 30) " +
                           where;

                    // условие по банку данных
                    if (_nzpWp > 0) sql += " and k.nzp_wp = " + _nzpWp;

                    sql += " group by 1 ";

                    ret = ExecSQL(_connDb, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Таблица 3. Данные по средствам УК и ПУ
        /// </summary>
        /// <returns></returns>
        private Returns GetTable3PaymentUKiPU(out DataTable dataTable, string tableName)
        {
            Returns ret = new Returns(true);
            dataTable = null;

            try
            {
                ret = GetTable1GSumLs1(false);
                if (!ret.result) throw new Exception(ret.text);

                ret = GetTable1SumPrih2(false);
                if (!ret.result) throw new Exception(ret.text);

                // Получить сумму "Учтено в расчетах" из {local_pref}_charge_{YY}.charge_{MM}.money_to
                ret = InsertIntoTempTableMoneyFieldFromCharge("tmp_wp_money", "pref", "char(10)", "money_from", true);
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(_connDb, "drop table tmp_point_pack_ls_20", false);

                string sql = "Create temp table tmp_point_pack_ls_20 (" +
                    " point    char(100), " +
                    " sum_prih " + DBManager.sDecimalType + "(14,2) default 0," +
                    " money_from " + DBManager.sDecimalType + "(14,2)  default 0," +
                    " pack_count integer  default 0, " +
                    " kvit_count integer   default 0, " +
                    " g_sum_ls " + DBManager.sDecimalType + "(14,2)  default 0" + ") ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_point_pack_ls_20 (point, sum_prih, money_from, pack_count, kvit_count, g_sum_ls) " +
                    " select " + sNvlWord + "(a.point, '!НЕ УКАЗАН'), " +
                    "   (select sum(a.sum_prih)  from tmp_wp_prih     a where trim(a.pref) = trim(t.pref)), " +
                    "   (select sum(b.money_from)  from tmp_wp_money    b where trim(b.pref) = trim(t.pref)), " +
                    "   sum(t.pack_count), " +
                    "   sum(t.kvit_count), " +
                    "   sum(t.g_sum_ls) " +
                    " from tmp_wp_pack_ls t " +
                    "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_point a on a.bd_kernel = trim(t.pref) " +
                    " group by 1,2,3 ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                // оставить только те строки, в которых не совпадают суммы
                if (_hideEqualSum || _checkCanChangeOperDay)
                {
                    sql = " delete from tmp_point_pack_ls_20 where " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_prih, 0) ";
                    if (_showSumMoney) sql += " and " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(money_from, 0) ";
                    ret = ExecSQL(_connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }

                dataTable = ClassDBUtils.OpenSQL("select * from tmp_point_pack_ls_20 order by point", tableName, _connDb).GetData();
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Таблица 3.1. Средства УК и ПУ по договорам
        /// </summary>
        /// <returns></returns>
        private Returns GetTable31UkiPUSupp(out DataTable dataTable, string tableName)
        {
            Returns ret = new Returns(true);
            dataTable = null;

            try
            {
                ret = GetTable3SumPrih2("tmp_supp_sum_prih", "nzp_supp", "");
                if (!ret.result) throw new Exception(ret.text);

                // Получить сумму "Учтено в расчетах" из {local_pref}_charge_{YY}.charge_{MM}.money_to
                ret = InsertIntoTempTableMoneyFieldFromCharge("tmp_supp_money", "nzp_supp", "int", "money_from", true);
                if (!ret.result) throw new Exception(ret.text);

                //ret = GetTable3GSumLs1("tmp_supp_pack_ls", "nzp_supp", "");
                //if (!ret.result) throw new Exception(ret.text);

                // получить все договора
                ret = GetDistinctValues("nzp_supp", new string[] { "tmp_supp_sum_prih", "tmp_supp_money" }, "tmp_supp_list");
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(_connDb, "drop table tmp_supp_pack_ls_show", false);
                string sql = "Create temp table tmp_supp_pack_ls_show (" +
                    " nzp_supp  integer, " +
                    " name_supp char(100), " +
                    " sum_prih  " + DBManager.sDecimalType + "(14,2) default 0 ," +
                    " money_from " + DBManager.sDecimalType + "(14,2) default 0 ," +
                    " pack_count integer default 0 ," +
                    " kvit_count integer default 0 , " +
                    " g_sum_ls " + DBManager.sDecimalType + "(14,2) default 0 ) ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_supp_pack_ls_show (nzp_supp, name_supp, sum_prih, money_from, pack_count, kvit_count, g_sum_ls) " +
                        " select distinct t.nzp_supp, " + sNvlWord + "(a.name_supp, '!НЕ УКАЗАН') as name_supp, " +
                        " (select sum(a.sum_prih)    from tmp_supp_sum_prih a where a.nzp_supp = t.nzp_supp) as sum_prih, " +
                        " (select sum(b.money_from)  from tmp_supp_money    b where b.nzp_supp = t.nzp_supp) as money_from,0,0,0 " +
                        //" (select sum(c.pack_count)  from tmp_supp_pack_ls  c where c.nzp_supp = t.nzp_supp) as pack_count, " +
                        //" (select sum(c.kvit_count)  from tmp_supp_pack_ls  c where c.nzp_supp = t.nzp_supp) as kvit_count, " +
                        //" (select sum(c.g_sum_ls) from tmp_supp_pack_ls c where c.nzp_supp = t.nzp_supp) as g_sum_ls " +
                    " from tmp_supp_list t " +
                    "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier a on a.nzp_supp = t.nzp_supp ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                if (_hideEqualSum || _checkCanChangeOperDay)
                {
                    sql = " delete from tmp_supp_pack_ls_show where " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_prih, 0) ";
                    if (_showSumMoney) sql += " and " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(money_from, 0) ";
                    ret = ExecSQL(_connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }

                dataTable = ClassDBUtils.OpenSQL("select * from tmp_supp_pack_ls_show order by name_supp", tableName, _connDb).GetData();
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Новая таблица Таблица 3.2.1 Чужие средства по банкам данных
        /// Новая таблица Таблица 3.2.2 Чужие средства по договорам
        /// </summary>
        /// <param name="tempTable">имя временной таблицы</param>
        private Returns GetTable3_2(out DataTable dt1, out DataTable dt2)
        {
            Returns ret = new Returns(true);
            string tempTable = "tmp_opl_pack20";
            string tempTable2 = "tmp_check_supp";
            dt2 = null;
            dt1 = null;
            try
            {
                ExecSQL(_connDb, " Drop table " + tempTable, false);
 
                var sql =new StringBuilder("Create temp table " + tempTable +
                  "( " +
                  "  pref character(100), " +
                  "  point character(100), " +
                  "  pack_count  int default 0 , " +
                  "  kvit_count  int default 0 , " +
                  "  sum_g_sum_ls    " + DBManager.sDecimalType + "(14,2) default 0, " +
                  "  sum_sum_prih    " + DBManager.sDecimalType + "(14,2) default 0" +
                  ") ");

                ret = ExecSQL(_connDb, sql.ToString(), true);
                if (!ret.result) throw new Exception(ret.text);

                sql.Remove(0, sql.Length);
                sql.AppendFormat("insert into {0} (pref, point)", tempTable);
                sql.AppendFormat("select bd_kernel, point from {0}_kernel{1}s_point where nzp_wp > 1", 
                    Points.Pref, tableDelimiter);
                if (_nzpWp > 0) sql.Append(" and nzp_wp = " + _nzpWp);
                ret = ExecSQL(_connDb, sql.ToString(), true);
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(_connDb, " Drop table pls" + tempTable, false);
                sql.Remove(0, sql.Length);
                sql.AppendFormat("Create temp table pls" + tempTable +
                  "( " +
                  "  year_  int, " +
                  "  nzp_pack_ls  int, " +
                  "  num_ls  int, " +
                  "  g_sum_ls    " + DBManager.sDecimalType + "(14,2) default 0  " +
                  ") ");
                ret = ExecSQL(_connDb, sql.ToString(), true);
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(_connDb, " Drop table numls" + tempTable, false);
                sql.Remove(0, sql.Length);
                sql.AppendFormat("Create temp table numls" + tempTable +
                  "( " +
                  "  num_ls  int, " +
                  "  pref  character(100) " +
                  ") ");
                ret = ExecSQL(_connDb, sql.ToString(), true);
                if (!ret.result) throw new Exception(ret.text);
               
                int yearCnt = GetYearCount();

                for (int i = 0; i < yearCnt; i++)
                {
                    string curr_year = ((_datS.Year + i) % 100).ToString("00");
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat(" insert into pls{0} (year_, nzp_pack_ls, num_ls, g_sum_ls)", tempTable);
                    sql.AppendFormat(" select "+curr_year+", nzp_pack_ls, num_ls, g_sum_ls from {0}_fin_{1}{2}pack p,", Points.Pref, curr_year, tableDelimiter);
                    sql.AppendFormat(" {0}_fin_{1}{2}pack_ls pls ", Points.Pref, curr_year, tableDelimiter);
                    sql.Append(" where p.nzp_pack = pls.nzp_pack and p.pack_typein (20, 30) ");
                    sql.AppendFormat(" and p.dat_uchet {0} and {1}(alg {2},0)<>0 " +
                                     " and inbasket = 0 and p.par_pack <> p.nzp_pack ", _whereDat, sNvlWord, sConvToInt);
                    ret = ExecSQL(_connDb, sql.ToString(), true);
                    if (!ret.result) throw new Exception(ret.text);
                }       
                
                foreach (var cur_pref in _prefList)
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("insert into numls{0} (pref, num_ls)", tempTable);
                    sql.AppendFormat("select '{0}', num_ls from {1}_data{2}kvar k ", 
                        cur_pref, Points.Pref, tableDelimiter);
                    sql.AppendFormat("where pref='{0}' and num_ls in (select num_ls from pls{1});", cur_pref, tempTable);
                    ret = ExecSQL(_connDb, sql.ToString(), true);
                    if (!ret.result) throw new Exception(ret.text);

                    sql.Remove(0, sql.Length);
                    for (int i = 0; i < yearCnt; i++)
                    {
                        sql.Remove(0, sql.Length);
                        string curr_year = ((_datS.Year + i) % 100).ToString("00");
                        sql.AppendFormat(" update {0} set sum_sum_prih = {1}(sum_sum_prih + ", tempTable, sNvlWord);
                        sql.AppendFormat(" (select coalesce(sum(sum_prih),0) from {0}_charge_{1}{2}from_supplier frs, ", cur_pref,
                            curr_year, tableDelimiter);
                        sql.AppendFormat(" pls{0} pls where ", tempTable);
                        sql.AppendFormat(" frs.nzp_pack_ls = pls.nzp_pack_ls and frs.num_ls = pls.num_ls and frs.dat_uchet {0} and pls.year_ = {1}),0)",
                            _whereDat, curr_year);
                        sql.AppendFormat(" where pref='{0}'", cur_pref); 
                        ret = ExecSQL(_connDb, sql.ToString(), true);
                        if (!ret.result) throw new Exception(ret.text);

                        sql.Remove(0, sql.Length);
                        sql.AppendFormat(" update {0} set sum_g_sum_ls = {1}(sum_g_sum_ls + ", tempTable, sNvlWord);
                        sql.AppendFormat(" (select sum(g_sum_ls) from  pls{0} pls, numls{0} ls ", tempTable);
                        sql.AppendFormat(" where pls.num_ls = ls.num_ls and pls.year_ = {0} and ls.pref = '{1}'),0)",
                            curr_year, cur_pref);
                        sql.AppendFormat(" where pref='{0}'", cur_pref);
                        ret = ExecSQL(_connDb, sql.ToString(), true);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                }
                ExecSQL(_connDb, " Drop table " + tempTable2, false);
                    sql.Remove(0, sql.Length);
                    sql.Append("Create temp table " + tempTable2 +
                      "( " +
                      "  nzp_supp  int, " +
                      "  supplier  character(2000), " +
                      "  pack_count  int, " +
                      "  kvit_count  int, " +
                      "  sum_g_sum_ls    " + DBManager.sDecimalType + "(14,2) default 0, " +
                      "  sum_money_from    " + DBManager.sDecimalType + "(14,2) default 0 , " +
                      "  sum_sum_prih    " + DBManager.sDecimalType + "(14,2) default 0  " +
                      ") ");
                    ret = ExecSQL(_connDb, sql.ToString(), true);
                    if (!ret.result) throw new Exception(ret.text);
          //      if (_showSumMoney)
                {
                   

                    ExecSQL(_connDb, " Drop table tmp_nzpsupp" + tempTable2, false);
                    sql.Remove(0, sql.Length); 
                    sql.AppendFormat("create temp table tmp_nzpsupp{0}", tempTable2);
                    sql.Append("(pref character(100), nzp_supp integer);");
                    ret = ExecSQL(_connDb, sql.ToString(), true);
                    if (!ret.result) throw new Exception(ret.text);
               
                    int monthCount = GetMonthCount();
                    DateTime curMonth = new DateTime(_datS.Year, _datS.Month, 1);

                    for (int j = 0; j < monthCount; j++)
                    {
                        string curr_year = (curMonth.Year % 100).ToString("00");
                        string curr_month = curMonth.Month.ToString("00");

                        // выполнить цикл по префиксам
                        foreach (string cur_pref in _prefList)
                        {
                            sql.Remove(0, sql.Length);
                            sql.AppendFormat(" insert into tmp_nzpsupp{0}(pref, nzp_supp) ", tempTable2);
                            sql.AppendFormat(" select distinct '{0}', nzp_supp from {0}_charge_{1}{2}charge_{3} chg,",
                                cur_pref, curr_year, tableDelimiter, curr_month);
                            sql.AppendFormat(" numls{0} ls where ", tempTable);
                            sql.Append(" chg.num_ls = ls.num_ls and chg.nzp_serv > 1 and ");
                            sql.Append(" chg.dat_charge is null and nzp_supp not in ");
                            sql.AppendFormat(" (select nzp_supp from tmp_nzpsupp{0} where pref = '{1}')", tempTable2, cur_pref);
                            sql.AppendFormat(" and ls.pref = '{0}'", cur_pref);
                            ret = ExecSQL(_connDb, sql.ToString(), true);
                            if (!ret.result) throw new Exception(ret.text);

                            sql.Remove(0, sql.Length);
                            sql.AppendFormat(" insert into tmp_nzpsupp{0}(pref, nzp_supp) ", tempTable2);
                            sql.AppendFormat(" select distinct '{0}', nzp_supp from {0}_charge_{1}{2}from_supplier frs,", cur_pref,
                                curr_year, tableDelimiter);
                            sql.AppendFormat(" pls{0} pls, ", tempTable);
                            sql.AppendFormat(" numls{0} ls ", tempTable);
                            sql.Append(" where frs.nzp_pack_ls = pls.nzp_pack_ls and frs.num_ls= pls.num_ls and ");
                            sql.AppendFormat(" ls.pref = '{0}' and pls.num_ls = ls.num_ls ", cur_pref);
                            sql.AppendFormat(" and frs.dat_uchet {0}", _whereDat);
                            sql.AppendFormat(" and nzp_supp not in (select nzp_supp from tmp_nzpsupp{0} where pref = '{1}') ", tempTable2,cur_pref);
                            ret = ExecSQL(_connDb, sql.ToString(), true);
                            if (!ret.result) throw new Exception(ret.text);

                            sql.Remove(0, sql.Length);
                            sql.AppendFormat("insert into {0}(nzp_supp) ", tempTable2);
                            sql.AppendFormat("select nzp_supp from tmp_nzpsupp{0} where not exists (select 1 from {0})",tempTable2);
                            ret = ExecSQL(_connDb, sql.ToString(), true);
                            if (!ret.result) throw new Exception(ret.text);

                            
                            sql.Remove(0, sql.Length);
                            sql.AppendFormat(" update {0} t set sum_money_from = coalesce((sum_money_from +", tempTable2);
                            sql.AppendFormat(" (select sum(money_from) from {0}_charge_{1}{2}charge_{3} chg, pls{4} pls where ",
                                cur_pref, curr_year, tableDelimiter, curr_month, tempTable);
                            sql.Append(" chg.num_ls = pls.num_ls and chg.nzp_serv > 1 and chg.dat_charge is null and nzp_supp = t.nzp_supp)),0)");
                            ret = ExecSQL(_connDb, sql.ToString(), true);
                            if (!ret.result) throw new Exception(ret.text);
                            
                            sql.Remove(0, sql.Length);
                            sql.AppendFormat(" update {0} t set sum_sum_prih = coalesce(sum_sum_prih +", tempTable2);
                            sql.AppendFormat(" (select sum(sum_prih) from {0}_charge_{1}{2}from_supplier frs, pls{3} pls where ",
                                cur_pref, curr_year, tableDelimiter,tempTable);
                            sql.Append(" frs.num_ls = pls.num_ls and frs.num_ls= pls.num_ls and");
                            sql.AppendFormat(" frs.dat_uchet {0} and nzp_supp = t.nzp_supp),0)", _whereDat);
                             ret = ExecSQL(_connDb, sql.ToString(), true);
                            if (!ret.result) throw new Exception(ret.text);
                        }

                        curMonth = curMonth.AddMonths(1);
                    }//for
                }

                if (_hideEqualSum || _checkCanChangeOperDay)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" delete from " + tempTable + " where " + DBManager.sNvlWord + "(sum_g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_sum_prih, 0) ");
                    ret = ExecSQL(_connDb, sql.ToString());
                    if (!ret.result) throw new Exception(ret.text);

                    sql.Remove(0, sql.Length);
                    sql.Append(" delete from " + tempTable2 + " where " + DBManager.sNvlWord + "(sum_g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_sum_prih, 0) ");
                    ret = ExecSQL(_connDb, sql.ToString());
                    if (!ret.result) throw new Exception(ret.text);
                }

                dt1 = ClassDBUtils.OpenSQL("select * from " + tempTable, "Q321", _connDb).GetData();
               
                dt2 = ClassDBUtils.OpenSQL("select * from " + tempTable2, "Q322", _connDb).GetData();
               
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

     

        /// <summary>
        /// Таблица 3.# Получить сумму "Учтено в лиц. счетах"
        /// </summary>
        /// <param name="tempTable">имя временной таблицы</param>
        /// <param name="groupField">поле, по которому выполняется группировка</param>
        /// <returns></returns>
        private Returns GetTable3SumPrih2(string tempTable, string groupField, string kod_sum)
        {
            Returns ret = new Returns(true);

            try
            {
                ExecSQL(_connDb, " Drop table " + tempTable, false);

                string sql = "Create temp table " + tempTable +
                    "( " + groupField + " int, " +
                    "  sum_prih    " + DBManager.sDecimalType + "(14,2) default 0 " + ") ";
                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                string where = "";
                if (kod_sum != "") where += "   and pls.kod_sum in (" + kod_sum + ")";

                int yearCnt = GetYearCount();

                for (int i = 0; i < yearCnt; i++)
                {
                    string curr_year = ((i + _datS.Year) % 100).ToString("00");

                    // выполнить цикл по префиксам
                    foreach (string cur_pref in _prefList)
                    {
                        sql = " insert into " + tempTable + " (" + groupField + ", sum_prih) " +
                            " select " + DBManager.sNvlWord + "(pls." + groupField + ", 0), sum(" + sNvlWord + "(fn.sum_prih, 0)) as sum_prih " +
                            " from " + Points.Pref + "_fin_" + curr_year + tableDelimiter + " pack p, " +
                                    Points.Pref + "_fin_" + curr_year + tableDelimiter + " pack_ls pls, " +
                                    cur_pref + "_charge_" + curr_year + tableDelimiter + " from_supplier fn " +
                            " where p.nzp_pack = pls.nzp_pack " +
                            "   and pls.nzp_pack_ls = fn.nzp_pack_ls " +
                            // ... условие на дату
                            "   and pls.dat_uchet " + _whereDat +
                            "   and p.pack_type in (20, 30) " +
                           where+
                            " group by 1 ";

                        ret = ExecSQL(_connDb, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Контроль распределения по квитанциям. Таблица 3.1. Платежи принципалов
        /// </summary>
        /// <returns></returns>
        private Returns GetTable31Princip(out DataTable dataTable, string tableName)
        {
            Returns ret = new Returns(true);
            dataTable = null;

            try
            {
                ret = GetTable3GSumLs1("tmp_payer_pack_ls", "nzp_payer", "49");
                if (!ret.result) throw new Exception(ret.text);

                ret = GetTable3SumPrih2("tmp_payer_sum_prih", "nzp_payer", "49");
                if (!ret.result) throw new Exception(ret.text);

                // получить всех принципалов
                ret = GetDistinctValues("nzp_payer", new string[] { "tmp_payer_pack_ls", "tmp_payer_sum_prih" }, "tmp_payer_list");
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(_connDb, "drop table tmp_payer_pack_ls_show", false);
                string sql = "Create temp table tmp_payer_pack_ls_show (" +
                    " nzp_payer  integer, " +
                    " payer      char(100), " +
                    " sum_prih  " + DBManager.sDecimalType + "(14,2) default 0 ," +
                    " pack_count integer default 0 ," +
                    " kvit_count integer default 0 , " +
                    " g_sum_ls " + DBManager.sDecimalType + "(14,2) default 0 ) ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_payer_pack_ls_show (nzp_payer, payer, sum_prih, pack_count, kvit_count, g_sum_ls) " +
                        " select distinct t.nzp_payer, " + sNvlWord + "(a.payer, '!НЕ УКАЗАН'), " +
                        " (select sum(a.sum_prih)    from tmp_payer_sum_prih a where a.nzp_payer = t.nzp_payer) as sum_prih, " +
                        " (select sum(c.pack_count)  from tmp_payer_pack_ls  c where c.nzp_payer = t.nzp_payer) as pack_count, " +
                        " (select sum(c.kvit_count)  from tmp_payer_pack_ls  c where c.nzp_payer = t.nzp_payer) as kvit_count, " +
                        " (select sum(c.g_sum_ls) from tmp_payer_pack_ls c where c.nzp_payer = t.nzp_payer) as g_sum_ls " +
                    " from tmp_payer_list t " +
                    "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer a on a.nzp_payer = t.nzp_payer ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                if (_hideEqualSum || _checkCanChangeOperDay)
                {
                    sql = " delete from tmp_payer_pack_ls_show where " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_prih, 0) ";
                    ret = ExecSQL(_connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }

                dataTable = ClassDBUtils.OpenSQL("select * from tmp_payer_pack_ls_show order by payer", tableName, _connDb).GetData();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Контроль распределения по квитанциям. Таблица 3.2. Платежи по договорам
        /// </summary>
        /// <returns></returns>
        private Returns GetTable32Supplier(out DataTable dataTable, string tableName)
        {
            Returns ret = new Returns(true);
            dataTable = null;

            try
            {
                ret = GetTable3GSumLs1("tmp_supp_pack_ls", "nzp_supp", "50");
                if (!ret.result) throw new Exception(ret.text);

                ret = GetTable3SumPrih2("tmp_supp_sum_prih", "nzp_supp", "50");
                if (!ret.result) throw new Exception(ret.text);

                // Получить сумму "Учтено в расчетах" из {local_pref}_charge_{YY}.charge_{MM}.money_from
                //ret = InsertIntoTempTableMoneyFieldFromCharge("tmp_supp_money", "nzp_supp", "integer", "money_from", false);
                //if (!ret.result) throw new Exception(ret.text);
                
                // получить все договора
                ret = GetDistinctValues("nzp_supp", new string[] { "tmp_supp_pack_ls", "tmp_supp_sum_prih" }, "tmp_supp_list");
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(_connDb, "drop table tmp_supp_pack_ls_show", false);
                string sql = "Create temp table tmp_supp_pack_ls_show (" +
                    " nzp_supp  integer, " +
                    " name_supp char(100), " +
                    " sum_prih  " + DBManager.sDecimalType + "(14,2) default 0 ," +
                    " money_from " + DBManager.sDecimalType + "(14,2) default 0 ," +
                    " pack_count integer default 0 ," +
                    " kvit_count integer default 0 , " +
                    " g_sum_ls " + DBManager.sDecimalType + "(14,2) default 0 ) ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_supp_pack_ls_show (nzp_supp, name_supp, sum_prih, money_from, pack_count, kvit_count, g_sum_ls) " +
                        " select distinct t.nzp_supp, " + sNvlWord + "(a.name_supp, '!НЕ УКАЗАН') as name_supp, " +
                        " (select sum(a.sum_prih)    from tmp_supp_sum_prih a where a.nzp_supp = t.nzp_supp) as sum_prih, 0," +
                       
                        " (select sum(c.pack_count)  from tmp_supp_pack_ls  c where c.nzp_supp = t.nzp_supp) as pack_count, " +
                        " (select sum(c.kvit_count)  from tmp_supp_pack_ls  c where c.nzp_supp = t.nzp_supp) as kvit_count, " +
                        " (select sum(c.g_sum_ls) from tmp_supp_pack_ls c where c.nzp_supp = t.nzp_supp) as g_sum_ls " +
                    " from tmp_supp_list t " +
                    "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier a on a.nzp_supp = t.nzp_supp ";
                ret = ExecSQL(_connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                if (_hideEqualSum || _checkCanChangeOperDay)
                {
                    sql = " delete from tmp_supp_pack_ls_show where " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(sum_prih, 0) ";
                    if (_showSumMoney) sql += " and " + DBManager.sNvlWord + "(g_sum_ls, 0) = " + DBManager.sNvlWord + "(money_from, 0) ";
                    ret = ExecSQL(_connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }

                dataTable = ClassDBUtils.OpenSQL("select * from tmp_supp_pack_ls_show order by name_supp", tableName, _connDb).GetData();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Контроль распределения по пачкам. Таблица 4. Средства РЦ
        /// </summary>
        /// <returns></returns>
        private Returns GetTable4PaymentCenterPack(out DataTable dataTable)
        {
            Returns ret = new Returns(true);
            dataTable = null;
            
            try
            {
                ExecSQL(_connDb, " Drop table tmp_wp_pack ", false);

                string sql = "Create temp table tmp_wp_pack " +
                    "( nzp_wp      int, " +
                    "  pack_count  int default 0 , " +
                    "  kvit_count  int default 0 , " +
                    "  total_sum     " + DBManager.sDecimalType + "(14,2) default 0 , " +
                    "  undistrib_sum " + DBManager.sDecimalType + "(14,2) default 0 , " +
                    "  basket_sum    " + DBManager.sDecimalType + "(14,2) default 0 , " +
                    "  distrib_sum   " + DBManager.sDecimalType + "(14,2) default 0  " + ") ";

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (!_checkCanChangeOperDay)
                {
                    int yearCnt = GetYearCount();
                    
                    for (int i = 0; i < yearCnt; i++)
                    {
                        string curr_year = ((_datS.Year + i) % 100).ToString("00");
                        
                        sql = " insert into tmp_wp_pack (nzp_wp, pack_count, kvit_count, total_sum, undistrib_sum, basket_sum, distrib_sum) " +
                            " select " + sNvlWord + "(k.nzp_wp, 0), " +
                            "   count(distinct p.nzp_pack) as pack_count, " +
                            "   count(pls.nzp_pack_ls) as kvit_count, " +
                            "   sum(pls.g_sum_ls) as total_sum, " +
                            "   sum(" + sNvlWord + "(c.g_sum_ls, 0)) as undistrib_sum, " +
                            "   sum(" + sNvlWord + "(d.g_sum_ls, 0)) as basket_sum, " +
                            "   sum(" + sNvlWord + "(e.g_sum_ls, 0)) as distrib_sum " +
                            " from " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack p, " +
                                Points.Pref + "_data" + tableDelimiter + "kvar k, " +
                                Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls pls " +
                            // не распределенно
                            "   left outer join " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls c on c.nzp_pack_ls = pls.nzp_pack_ls  " +
                            "       and (c.dat_uchet is null or " + sNvlWord + "(cast(c.alg as integer),0) = 0) " +
                            // в корзине 
                            " left outer join " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls d on d.nzp_pack_ls = pls.nzp_pack_ls " +
                            "       and (d.dat_uchet is null or " + sNvlWord + "(cast(d.alg as integer),0) = 0) and d.inbasket = 1 " +
                            // распределено 
                            " left outer join " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls e on e.nzp_pack_ls = pls.nzp_pack_ls " +
                            "       and " + sNvlWord + "(cast(e.alg as int),0) <> 0 and e.inbasket = 0 " +
                            " where p.nzp_pack = pls.nzp_pack " +
                            "   and k.num_ls = pls.num_ls " +
                            "   and p.dat_uchet " + _whereDat +
                            // оплаты на расчётных счетах РЦ
                            "   and p.pack_type not in (20, 30) " +
                            (_nzpWp > 0 ? " and k.nzp_wp = " + _nzpWp : "") +
                            " group by 1 ";

                        ret = ExecSQL(_connDb, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }// for
                }//if

                sql = "select " + sNvlWord + "(a.point, '!НЕ УКАЗАН') as point, " +
                    " sum(t.pack_count) as pack_count, " +
                    " sum(t.kvit_count) as kvit_count, " +
                    " sum(t.total_sum) as total_sum, " +
                    " sum(t.undistrib_sum) as undistrib_sum, " +
                    " sum(t.basket_sum) as basket_sum, " +
                    " sum(t.distrib_sum) as distrib_sum " +
                " from tmp_wp_pack t " +
                "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_point a on a.nzp_wp = t.nzp_wp " +
                " group by 1 " +
                " order by 1";

                dataTable = ClassDBUtils.OpenSQL(sql, "area_pack", _connDb).GetData();

                return ret;
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Контроль распределения по пачкам. Таблица 5. Данные по средствам поставщиков по пачкам
        /// </summary>
        /// <returns></returns>
        private Returns GetTable5PrincipalPack(out DataTable dataTable)
        {
            Returns ret = new Returns();
            dataTable = null;

            try
            {
                ExecSQL(_connDb, " Drop table tmp_supp_pack ", false);

                string sql = "Create temp table tmp_supp_pack" +
                    "( nzp_supp      int, " +
                    "  pack_count    int default 0 , " +
                    "  kvit_count    int default 0 , " +
                    "  total_sum     " + DBManager.sDecimalType + "(14,2) default 0 , " +
                    "  undistrib_sum " + DBManager.sDecimalType + "(14,2) default 0 , " +
                    "  basket_sum    " + DBManager.sDecimalType + "(14,2) default 0 , " +
                    "  distrib_sum   " + DBManager.sDecimalType + "(14,2) default 0 ) ";

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);
            
                if (!_checkCanChangeOperDay)
                {
                    int yearCnt = GetYearCount();
                    
                    for (int i = 0; i < yearCnt; i++)
                    {
                        string curr_year = ((_datS.Year + i) % 100).ToString("00");

                        sql = " insert into tmp_supp_pack (nzp_supp, pack_count, kvit_count, total_sum, undistrib_sum, basket_sum, distrib_sum) " +
                            " select " + sNvlWord + "(p.nzp_supp, 0) as nzp_supp, " +
                            "   count(distinct p.nzp_pack) as pack_count, " +
                            "   count(pls.nzp_pack_ls) as kvit_count, " +
                            "   sum(pls.g_sum_ls) as total_sum, " +
                            "   sum(" + sNvlWord + "(c.g_sum_ls, 0)) as undistrib_sum, " +
                            "   sum(" + sNvlWord + "(d.g_sum_ls, 0)) as basket_sum, " +
                            "   sum(" + sNvlWord + "(e.g_sum_ls, 0)) as distrib_sum " +
                            " from " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack p, " +
                            Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls pls " +
                            // не распределенно
                            "   left outer join " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls c on c.nzp_pack_ls = pls.nzp_pack_ls  " +
                            "       and (c.dat_uchet is null or " + sNvlWord + "(cast(c.alg as integer),0) = 0) " +
                            // в корзине 
                            " left outer join " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls d on d.nzp_pack_ls = pls.nzp_pack_ls " +
                            "       and (d.dat_uchet is null or " + sNvlWord + "(cast(d.alg as integer),0) = 0) and d.inbasket = 1 " +
                            // распределено 
                            " left outer join " + Points.Pref + "_fin_" + curr_year + tableDelimiter + "pack_ls e on e.nzp_pack_ls = pls.nzp_pack_ls " +
                            "       and " + sNvlWord + "(cast(e.alg as int),0) <> 0 and e.inbasket = 0 " +
                            " where p.nzp_pack = pls.nzp_pack " +
                            "   and p.dat_uchet " + _whereDat +
                            // оплаты от сторонних организаций
                                " and p.pack_type in (20, 30) " +
                            " group by 1 ";

                        ret = ExecSQL(_connDb, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                }

                sql = "select " + sNvlWord + "(a.name_supp, '!НЕ УКАЗАН') as name_supp, t.nzp_supp, " +
                    " sum(t.pack_count) as pack_count, " +
                    " sum(t.kvit_count) as kvit_count, " +
                    " sum(t.total_sum) as total_sum, " +
                    " sum(t.undistrib_sum) as undistrib_sum, " +
                    " sum(t.basket_sum) as basket_sum, " +
                    " sum(t.distrib_sum) as distrib_sum " +
                " from tmp_supp_pack t " +
                "   left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier a on a.nzp_supp = t.nzp_supp " +
                " group by 1,2 " +
                " order by 1";
                
                dataTable = ClassDBUtils.OpenSQL(sql, "supp_pack", _connDb).GetData();

                return ret;
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
        }

        /// <summary>
        /// Таблица 6. Непривязанные к контрагентам поставщики
        /// </summary>
        /// <returns></returns>
        private Returns GetTable6UnlinkSupp(out DataTable dataTable, string tableName)
        {
            Returns ret = new Returns(true);
            dataTable = null;
            
            try
            {
                ExecSQL(_connDb, " Drop table tmp_unlink_supp ", false);

                string sql = "Create temp table tmp_unlink_supp" +
                                "( point      char(100), " +
                                "  name_supp  char(100) ) ";

                ret = ExecSQL(_connDb, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                //if (_checkCanChangeOperDay)
                //{
                //    foreach (string cur_pref in _prefList)
                //    {
                //       sql = "Insert into tmp_unlink_supp (point, name_supp) " +
                //            " select (select point from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_point where bd_kernel=" + Utils.EStrNull(cur_pref) + "), " +
                //            "   s.name_supp from " + cur_pref + "_kernel" + DBManager.tableDelimiter + "supplier s " +
                //            " where (select count(*) from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer p where p.nzp_supp = s.nzp_supp) = 0 ";
                //        ret = ExecSQL(conn_db, sql, true);
                //        if (!ret.result) throw new Exception(ret.text);
                //    }
                //}

                dataTable = ClassDBUtils.OpenSQL("select point, name_supp from tmp_unlink_supp order by 1", tableName, _connDb).GetData();
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }

            return ret;
        }

    }
}
