using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using System.IO;
using SevenZip;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep
    {
        /// <summary>
        /// Данные для кассы 3.0 
        /// </summary>
        /// <returns></returns>
        public Returns GetUploadKassa(out Returns ret, SupgFinder finder, string year, string month)
        {
            var unloadKassa = new UnloadForKassa30();

            return unloadKassa.GetUploadKassa(out ret, finder.nzp_user, finder.nzp_supp, finder.prefList, year, month);

        }

    }


    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(decimal progress)
        {
            Progress = progress;
        }

        public decimal Progress { get; set; }
    }


    /// <summary>
    /// Выгрузка в Кассу 3.0
    /// </summary>
    public class UnloadForKassa30 : DataBaseHead
    {
        private IDbConnection _conDb;
        private List<_Point> _listPrefix;
        private ExcelRep _excelRepDb;
        private int _nzpExc;

        void HandleProgressEvent(object sender, ProgressEventArgs e)
        {
            _excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = _nzpExc, progress = e.Progress });
        }

        /// <summary>
        /// Данные для кассы 3.0
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="nzpUser">Код пользователя инициализировавшего процесс</param>
        /// <param name="nzpSupp">Код поставщика, если не выбран, то 0</param>
        /// <param name="apref">Если выбран бан, то префикс схемы иначе пусто</param>
        /// <param name="year">Год выгрузки</param>
        /// <param name="month">Месяц выгрузки</param>
        /// <returns></returns>
        public Returns GetUploadKassa(out Returns ret, int nzpUser, int nzpSupp, List<string> apref, string year, string month)
        {
            _listPrefix = new List<_Point>();

            if (CheckInputPrm(out ret, nzpUser, apref, year, month)) return ret;


            string connKernel = Points.GetConnByPref(Points.Pref);
            _conDb = GetConnection(connKernel);
            ret = OpenDb(_conDb, true);
            if (!ret.result)
            {
                return ret;
            }

            try
            {

                //время записи в БД
                //Имя файла отчета
                _excelRepDb = new ExcelRep();
                //запись в БД о постановки в поток(статус 0)
                var fileNameKassa = GetFileNameKassa(year, month);

                string fileNameOut = fileNameKassa + ".7z";
                string fileNameIn = fileNameKassa + ".txt";

                ret = _excelRepDb.AddMyFile(new ExcelUtility
                {
                    nzp_user = nzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = " Выгрузка в кассу 3.0 за " + month.Trim() + "." + year.Trim(),
                    file_name = "Kassa_3_0_" + year + "_" + month + ".7z",
                    exc_path = fileNameOut
                });

                if (!ret.result) return ret;
                _nzpExc = ret.tag;



                var memstr = new FileStream(fileNameIn, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                string version = "*version*|1.0.2|" + DateTime.Now.ToUniversalTime().ToShortDateString() + "|";
                writer.WriteLine(version);

                #region Выгрузка по ЛС

                var unloadForKassa30Ls = new UnloadForKassa30Ls(_conDb);
                unloadForKassa30Ls.RaiseProgressEvent += HandleProgressEvent;
                ret = unloadForKassa30Ls.GetUploadLS(writer, nzpSupp, _listPrefix);

                if (!ret.result) throw new Exception("Ошибка выгрузки в кассу по ЛС");

                _excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = _nzpExc, progress = 0.1m });

                #endregion

                #region Выгрузка счетчиков

                GetUploadCounters(writer, year, month);
                _excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = _nzpExc, progress = 0.2m });

                #endregion

                writer.Flush();
                writer.Close();
                memstr.Close();

                GetUploadCharge(fileNameIn, out ret, nzpSupp, year, month, _excelRepDb, _nzpExc);

                ret = MakeArchiv(fileNameIn, ref fileNameOut);

                if (ret.result)
                {
                    _excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = _nzpExc, progress = 1 });
                    _excelRepDb.SetMyFileState(new ExcelUtility
                    {
                        nzp_exc = _nzpExc,
                        status = ExcelUtility.Statuses.Success,
                        exc_path = Path.GetFileName(fileNameOut)
                    });
                }
                else
                {
                    _excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = _nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                _excelRepDb.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Выгрузка в кассу 3.0 не выполнена " + ex, MonitorLog.typelog.Error, true);

            }
            finally
            {
                _conDb.Close();
            }
            return ret;
        }

        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="nzpUser">код пользователя</param>
        /// <param name="apref">Выбранный банк</param>
        /// <param name="year">Год выгрузки</param>
        /// <param name="month">Месяц выгрузки</param>
        /// <returns></returns>
        private bool CheckInputPrm(out Returns ret, int nzpUser, List<string> apref, string year, string month)
        {
            ret = Utils.InitReturns();


            if (nzpUser < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return true;
            }


            if (apref.Any())
            {
                _listPrefix.AddRange(apref.Select(x => new _Point { pref = x }));
            }
            else
            {
                _listPrefix = Points.PointList;
            }



            if (year == null || month == null)
            {
                ret.result = false;
                ret.text = "Не определен расчетный срок";
                return true;
            }
            return false;
        }


        /// <summary>
        /// Архивация выгрузки в кассу 3.0 
        /// </summary>
        /// <param name="fileNameIn">Файл выгрузки</param>
        /// <param name="fileNameOut">Архив</param>
        private Returns MakeArchiv(string fileNameIn, ref string fileNameOut)
        {
            var ret = Utils.InitReturns();
            try
            {
                SevenZipCompressor.SetLibraryPath(AppDomain.CurrentDomain.BaseDirectory + (IntPtr.Size * 8 == 32 ? "7z" : "7z64") +
                                                  ".dll");
                var file = new SevenZipCompressor();
                file.EncryptHeaders = true;
                file.CompressionMethod = CompressionMethod.BZip2;
                file.DefaultItemName = Path.GetFileName(fileNameIn);
                file.CompressionLevel = CompressionLevel.Normal;
                file.DirectoryStructure = false;

                file.CompressFilesEncrypted(fileNameOut, Constants.Kassa_3_0, fileNameIn);
                if (InputOutput.useFtp)
                    fileNameOut = InputOutput.SaveOutputFile(fileNameOut);
                File.Delete(fileNameIn);

                //file.CompressFilesEncrypted(Path.Combine(Constants.Directories.ReportDir, fileNameOut),
                //    Constants.Kassa_3_0, Path.Combine(Constants.Directories.ReportDir, fileNameIn));
                //if (InputOutput.useFtp)
                //    InputOutput.SaveOutputFile(Path.Combine(Constants.Directories.ReportDir,
                //        fileNameOut));
                //File.Delete(Path.Combine(Constants.Directories.ReportDir, fileNameIn));

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка архивации. Возможно неообходимо заменить файл 7z.dll в UPDATER_EXE на соответствующий битности системы. " +
                    ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
            }
            return ret;
        }




        /// <summary>
        /// Получение имени файла выгрузки
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        private string GetFileNameKassa(string year, string month)
        {
            string fileNameKassa = "Kassa_3_0_" + year + "_" + month + "_" + RandomText.Generate() + "_" +
                                   DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            //fileNameKassa = FileUtility.GetFileName(Constants.Directories.ReportDir, fileNameKassa);
            fileNameKassa = Path.Combine(Constants.Directories.ReportDir, fileNameKassa);
            return fileNameKassa;
        }



        /// <summary>
        /// Данные для кассы 3.0 по счетчикам
        /// </summary>
        /// <returns></returns>
        public Returns GetUploadCounters(StreamWriter writer, string year, string month)
        {
            Returns ret = Utils.InitReturns();
            var dataSet = new DataSet("Счетчики");

            string sql = " Select nzp_serv,service , ordering " +
                         " From " + Points.Pref + sKernelAliasRest + "services s ";
            DataTable resTable = DBManager.ExecSQLToTable(_conDb, sql);
            resTable.TableName = "Выгрузка справочника услуг";
            resTable.Namespace = "*services*|";
            dataSet.Tables.Add(resTable);


            ExecSQL(_conDb, " drop table t_bad", false);
            sql = "Create temp table t_bad (" +
                  " nzp_kvar integer," +
                  " nzp_counter integer)" + DBManager.sUnlogTempTable;
            ExecSQL(_conDb, sql, true);


            foreach (var points in _listPrefix)
            {

                sql = " Insert into t_bad ( nzp_kvar, nzp_counter) " +
                      " Select a.nzp_kvar, nzp_counter " +
                      " From " + points.pref + sDataAliasRest + "counters a, " +
                      "      " + points.pref + sDataAliasRest + "kvar b" +
                      " Where a.nzp_kvar=b.nzp_kvar " +
                      " and a.dat_close is not null" +
                      " and a.dat_close <Date('01." + month + "." + year + "')" +
                      " and is_actual=1 ";
                if (!ExecSQL(_conDb, sql, true).result) return ret;

            }


            if (!ExecSQL(_conDb,
                "create index ix_tmp1_02 on t_bad (nzp_kvar, nzp_counter) ", true).result) return ret;

            ExecSQL(_conDb, DBManager.sUpdStat + " t_bad", true);

            ExecSQL(_conDb, " drop table t_unload", false);



            sql = " Create temp table t_unload(nzp Serial, " +
                  " pkod " + sDecimalType + ",  " +
                  " fio char(50),  " +
                  " num_ls integer, " +
                  " nzp_counter integer, " +
                  " nzp_serv integer,  " +
                  " cnt_stage " + sDecimalType + ",  " +
                  " name_type char(40), " +
                  " formula char(100),  " +
                  " num_cnt char(40),  " +
                  " dat_uchet date,  " +
                  " val_cnt " + sDecimalType + "(18,6) , " +
                  " base char(40), " +
                  " nzp_cnttype integer ) ";
            if (!ExecSQL(_conDb, sql, true).result) return ret;



            resTable = new DataTable("Выборка счетчиков");
            resTable.Namespace = "*counter*|";
            foreach (var points in _listPrefix)
            {
                ExecSQL(_conDb, " drop table t1", false);

                sql = " Select nzp_counter, max(a.dat_uchet) as dat_uchet " +
                    " into temp t1 " +
                      "   From " + points.pref + sDataAliasRest + "counters a  " +
                      " Where a. dat_uchet>=Date('01.01." + (DateTime.Now.Year - 1) + "')" +
                      "         and a.is_actual=1 " +
                      "         and 0=( Select count(*) From t_bad d " +
                      "                 Where a.nzp_kvar=d.nzp_kvar  " +
                      "                 and a.nzp_counter=d.nzp_counter) group by 1";
                if (!ExecSQL(_conDb, sql, true).result) return ret;


                if (!ExecSQL(_conDb,
                    "create index ix_tmp1_04 on t1 (nzp_counter, dat_uchet) ", true).result) return ret;

                ExecSQL(_conDb, DBManager.sUpdStat + " t1", true);

                sql = " insert into t_unload(pkod, fio, num_ls, nzp_counter, nzp_serv, cnt_stage, " +
                      "   name_type, formula, num_cnt, dat_uchet, val_cnt ,base ,nzp_cnttype ) " +
                      " Select k.pkod,k.fio,k.num_ls,a.nzp_counter, a.nzp_serv,sc.cnt_stage, " +
                      "   sc.name_type,sc.formula,a.num_cnt,a.dat_uchet, a.val_cnt, " +
                      " '" + points.pref + "' , a.nzp_cnttype " +
                      "   From " + points.pref + sDataAliasRest + "counters a, " +
                      "        " + points.pref + sKernelAliasRest + "s_counttypes sc,  " +
                      "        " + points.pref + sDataAliasRest + "kvar k, t1 " +
                      " Where a.nzp_kvar=k.nzp_kvar " +
                      "         and k.typek=1 " +
                      "         and a.nzp_cnttype=sc.nzp_cnttype " +
                      "         and a.dat_uchet>=Date('01.01." + (DateTime.Now.Year - 1) + "')" +
                      "         and a.is_actual=1 " +
                      "         and a.nzp_counter=t1.nzp_counter " +
                      "         and a.dat_uchet=t1.dat_uchet ";
                if (!ExecSQL(_conDb, sql, true).result) return ret;

            }
            ExecSQL(_conDb, " drop table t_bad", false);

            if (!ExecSQL(_conDb, " create index ix_tmp1_03 on t_unload (nzp)  ", true).result) return ret;
            ExecSQL(_conDb, DBManager.sUpdStat + " t_unload  ", true);


            object obj = ExecScalar(_conDb, " select count(*) as co from t_unload ", out ret, true);
            int count = (obj != null && obj != DBNull.Value) ? Convert.ToInt32(obj.ToString().Trim()) : 0;



            resTable = new DataTable("Выгрузка счетчиков:");
            DataColumn col = new DataColumn("Выгрузка счетчиков");
            resTable.Namespace = "*counter*|";
            resTable.Columns.Add(col);
            int countMax = (count / 10000) + 1;
            for (int i = 0; i < countMax; i++)
            {
                sql = " select num_ls, pkod, nzp_serv, num_cnt,nzp_counter, max(dat_uchet) as dat_uchet," +
                      " max(fio) as fio," +
                      " max(cnt_stage) as cnt_stage," +
                      " max(name_type) as name_type , " +
                      " max(formula) as formula , " +
                      " max(val_cnt) as val_cnt , max(base) as base , " +
                      " max (nzp_cnttype) as nzp_cnttype " +
                      " from t_unload  where pkod>0" +
                      " group by 1,2,3,4,5 " +
                      " order by pkod, nzp_serv, num_cnt, nzp_counter, dat_uchet" +
                      " limit 10000 offset 10000*" + i;

                MyDataReader reader;
                if (!ExecRead(_conDb, out reader, sql, true).result) return ret;

                Decimal oldPkod = 0;
                int icount = 0;
                string s = "";
                if (reader != null)
                {

                    while (reader.Read())
                    {
                        if (oldPkod != (Decimal)reader["pkod"])
                        {
                            icount = 1;
                            oldPkod = (Decimal)reader["pkod"];
                        }
                        else
                            icount++;

                        if (reader["num_ls"] != DBNull.Value) s += (int)reader["num_ls"] + "|";
                        else s += "|";
                        if (reader["pkod"] != DBNull.Value) s += ((Decimal)reader["pkod"]).ToString("0").Trim() + "|";
                        else s += "|";
                        if (reader["fio"] != DBNull.Value) s += reader["fio"].ToString().Trim() + "|";
                        else s += "|";
                        s += "01." + month + "." + year + "|";
                        if (reader["num_cnt"] != DBNull.Value) s += reader["num_cnt"].ToString().Trim() + "|";
                        else s += "|";
                        if (reader["nzp_serv"] != DBNull.Value) s += (int)reader["nzp_serv"] + "|";
                        else s += "|";
                        if (reader["name_type"] != DBNull.Value) s += reader["name_type"].ToString().Trim() + "|";
                        else s += "|";
                        if (reader["formula"] != DBNull.Value) s += reader["formula"].ToString().Trim() + "|";
                        else s += "|";
                        if (reader["dat_uchet"] != DBNull.Value)
                            s += ((DateTime)reader["dat_uchet"]).ToString("dd.MM.yyyy").Trim() + "|";
                        else s += "|";
                        if (reader["val_cnt"] != DBNull.Value)
                            s += ((Decimal)reader["val_cnt"]).ToString("0.00").Trim() + "|";
                        else s += "|";
                        s += icount + "|";
                        if (reader["base"] != DBNull.Value) s += reader["base"].ToString().Trim() + "|0|";
                        else s += "|0|";
                        if (reader["nzp_cnttype"] != DBNull.Value) s += (int)reader["nzp_cnttype"];
                        else s += "|";
                        resTable.Rows.Add(s);
                        s = "";
                    }
                    reader.Close();
                }

            }
            dataSet.Tables.Add(resTable);

            ret.text = "Выполнено";
            ret.result = true;

            foreach (DataTable thisTable in dataSet.Tables)
            {
                foreach (DataRow row in thisTable.Rows)
                {
                    writer.Write(thisTable.Namespace);
                    foreach (DataColumn column in thisTable.Columns)
                    {
                        writer.Write(row[column].ToString().Trim() + "|");
                    }
                    writer.WriteLine();
                }
            }
            return ret;
        }


        /// <summary>
        /// Выгрузка начислений
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="ret"></param>
        /// <param name="nzpSupp"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="excelRepDb"></param>
        /// <param name="nzpExc"></param>
        /// <returns></returns>
        public void GetUploadCharge(string filename, out Returns ret, int nzpSupp,
            string year, string month, ExcelRepClient excelRepDb, int nzpExc)
        {
            ret = Utils.InitReturns();


            try
            {
                string sYear = year.Substring(2);

                #region получение начислений



                ExecSQL(_conDb, " drop table tmp_charge2;", false);


                string sql = " CREATE temp TABLE tmp_charge2 ( " +
                             " YEAR_           integer, " +
                             " MONTH_          integer, " +
                             " PKOD           " + sDecimalType + "(13), " +
                             " nzp_kvar           integer, " +
                             " nzp_frm           integer, " +
                             " ORDER_PRINT     integer, " +
                             " NZP_SERV        INTEGER, " +
                             " nzp_supp           integer, " +
                             " NZP_SERV_VAR        INTEGER, " +
                             " ED_IZMER        char(20), " +
                             " TARIF    " + sDecimalType + "(14,2), " +
                             " SUM_TARIF " + sDecimalType + "(14,2),  " +
                             " RSUM_TARIF " + sDecimalType + "(14,2), " +
                             " SUM_CHARGE " + sDecimalType + "(14,2), " +
                             " SUM_PERE " + sDecimalType + "(14,2), " +
                             " SUM_REAL " + sDecimalType + "(14,2), " +
                             " SUM_LGOTA  " + sDecimalType + "(14,2),  " +
                             " SUM_MONEY  " + sDecimalType + "(14,2), " +
                             " MONEY_FROM  " + sDecimalType + "(14,2), " +
                             " SUM_NEDOP  " + sDecimalType + "(14,2), " +
                             " SUM_INSALDO  " + sDecimalType + "(14,2), " +
                             " SUM_OUTSALDO " + sDecimalType + "(14,2), " +
                             " SUM_TARIF_SN_F  " + sDecimalType + "(14,2)," +
                             " REVAL   " + sDecimalType + "(14,2)," +
                             " REAL_CHARGE  " + sDecimalType + "(14,2),  " +
                             " C_OKAZ  " + sDecimalType + "(14,2), " +
                             " C_NEDOP  " + sDecimalType + "(14,2), " +
                             " C_CALC  " + sDecimalType + "(14,2), " +
                             " C_REVAL  " + sDecimalType + "(14,2), " +
                             " NORMA " + sDecimalType + "(14,2), " +
                             " RASHOD " + sDecimalType + "(14,7)) " + sUnlogTempTable;

                if (!ExecSQL(_conDb, sql, true).result) return;


                int j = 0;
                foreach (var points in _listPrefix)
                {
                    j++;
                    if (!TempTableInWebCashe(_conDb, points.pref + "_charge_" + sYear +
                                                      tableDelimiter + "charge_" + month))
                    {
                        excelRepDb.SetMyFileProgress(new ExcelUtility
                        {
                            nzp_exc = nzpExc,
                            progress = (0.2m + 0.5m / _listPrefix.Count * j)
                        });
                        continue;
                    }

                    ExecSQL(_conDb, " drop table tmp_charge;", false);


                    sql = " CREATE temp TABLE tmp_charge ( " +
                          " YEAR_           integer, " +
                          " MONTH_          integer, " +
                          " PKOD           " + sDecimalType + "(13), " +
                          " nzp_kvar           integer, " +
                          " nzp_frm           integer, " +
                          " ORDER_PRINT     integer, " +
                          " nzp_supp           integer, " +
                          " NZP_SERV        INTEGER, " +
                          " NZP_SERV_VAR        INTEGER, " +
                          " ED_IZMER        char(20), " +
                          " TARIF    " + sDecimalType + "(14,2), " +
                          " SUM_TARIF " + sDecimalType + "(14,2),  " +
                          " RSUM_TARIF " + sDecimalType + "(14,2), " +
                          " SUM_CHARGE " + sDecimalType + "(14,2), " +
                          " SUM_PERE " + sDecimalType + "(14,2), " +
                          " SUM_REAL " + sDecimalType + "(14,2), " +
                          " SUM_LGOTA  " + sDecimalType + "(14,2),  " +
                          " SUM_MONEY  " + sDecimalType + "(14,2), " +
                          " MONEY_FROM  " + sDecimalType + "(14,2), " +
                          " SUM_NEDOP  " + sDecimalType + "(14,2)," +
                          " SUM_INSALDO  " + sDecimalType + "(14,2), " +
                          " SUM_OUTSALDO " + sDecimalType + "(14,2), " +
                          " SUM_TARIF_SN_F  " + sDecimalType + "(14,2)," +
                          " REVAL   " + sDecimalType + "(14,2), " +
                          " REAL_CHARGE  " + sDecimalType + "(14,2),  " +
                          " C_OKAZ  " + sDecimalType + "(14,2)," +
                          " C_NEDOP  " + sDecimalType + "(14,2)," +
                          " C_CALC  " + sDecimalType + "(14,2), " +
                          " C_REVAL  " + sDecimalType + "(14,2), " +
                          " NORMA " + sDecimalType + "(14,2)," +
                          " RASHOD " + sDecimalType + "(14,7)) " + DBManager.sUnlogTempTable;

                    if (!(ret = ExecSQL(_conDb, sql, true)).result) return;

                    if (points.pref == "tula" && month == "12" && year == "2014")
                    {

                        sql = "  insert into  tmp_charge(YEAR_,MONTH_,PKOD,nzp_kvar, nzp_frm, " +
                             "  ORDER_PRINT,NZP_SERV,NZP_SERV_VAR,ED_IZMER, tarif,SUM_TARIF, " +
                             "  RSUM_TARIF,SUM_CHARGE,SUM_PERE,SUM_REAL,sum_lgota,SUM_MONEY, " +
                             "  money_from,SUM_NEDOP,SUM_INSALDO, sum_outsaldo, sum_tarif_sn_f, reval, " +
                             "  REAL_CHARGE,  c_okaz, c_nedop, c_calc, c_reval,nzp_supp ) " +

                                " select 2014 as year, 12 as month, 0 as PKOD, m.NZP_KVAR, 0 as nzp_frm, " +
                               " 0 as order_print, m.NZP_SERV, m.NZP_SERV as nzp_serv_var, '' as ED_IZMER , " +
                              "  m.\"Тариф\" as tarif, " +
                               " m.\"Начисление\" SUM_TARIF, " +
                               " m.\"Начисление\" RSUM_TARIF,  " +
                               " case when m.\"Исх.сальдо\">0 then m.\"Исх.сальдо\" else 0 end as SUM_CHARGE, " +
                               " 0  as SUM_PERE, " +
                               " m.\"Начисление\" as SUM_REAL,  " +
                               " 0 as sum_lgota, " +
                               " m.\"Оплата\" as SUM_MONEY, " +
                               " m.\"Оплата\" as money_from, " +
                               " 0 as SUM_NEDOP, " +
                               " m.\"Вх.сальдо\" as SUM_INSALDO, " +
                               " m.\"Исх.сальдо\" as sum_outsaldo, " +
                               " 0 as sum_tarif_sn_f, 0 as reval, " +
                               " m.\"Списание\"  as REAL_CHARGE,  0 as c_okaz, 0 as c_nedop, 0 as c_calc, " +
                               " m.\"Корректировка\" as c_reval, 0 as nzp_supp " +
                               " from   public.tula_charge_november_total m,  " +
                               "       tula_data.kvar k " +
                               " where m.nzp_kvar=k.nzp_kvar and k.typek=1 ";
                        if (!ExecSQL(_conDb, sql, true).result) return;
                        // Проставление формул и единиц измерения


                        ExecSQL(_conDb, " create index ix1_ch on tmp_charge(nzp_kvar) ", true);
                        ExecSQL(_conDb, " create index ix2_ch on tmp_charge(nzp_frm) ", true);
                        ExecSQL(_conDb, " create index ix3_ch on tmp_charge(nzp_serv) ", true);
                        ExecSQL(_conDb, " create index ix6_ch on tmp_charge(nzp_kvar, nzp_frm, nzp_serv , tarif) ", true);
                        ExecSQL(_conDb, " create index ix6_ch1 on tmp_charge(nzp_kvar, nzp_frm, nzp_serv , norma ); ",
                            true);

                        ExecSQL(_conDb, sUpdStat + " tmp_charge; ", true);

                        // Проставляем формулу
                        sql = " UPDATE tmp_charge SET nzp_frm = coalesce((select max(a.nzp_frm) " +
                              " from " + points.pref + "_charge_" + sYear + tableDelimiter + "calc_gku_" + month + " a " +
                              " where a.nzp_kvar=tmp_charge.nzp_kvar and a.nzp_serv=tmp_charge.nzp_serv),0)";
                        if (!ExecSQL(_conDb, sql, true).result) return;


                        //Проставляем порядковый номер услуги
                        sql = " UPDATE tmp_charge SET order_print = coalesce((select max(a.ordering) " +
                              " from " + points.pref + "_kernel.services a " +
                              " where a.nzp_serv=tmp_charge.nzp_serv),0)";
                        if (!ExecSQL(_conDb, sql, true).result) return;


                    }
                    else
                    {


                        sql = " insert into  tmp_charge(YEAR_,MONTH_,PKOD,nzp_kvar, nzp_frm," +
                              "     ORDER_PRINT,NZP_SERV,NZP_SERV_VAR,ED_IZMER, tarif,SUM_TARIF, " +
                              "     RSUM_TARIF,SUM_CHARGE,SUM_PERE,SUM_REAL,sum_lgota,SUM_MONEY, " +
                              "     money_from,SUM_NEDOP,SUM_INSALDO, sum_outsaldo, sum_tarif_sn_f, reval, " +
                              "     REAL_CHARGE,  c_okaz, c_nedop, c_calc, c_reval,nzp_supp )  " +
                              " select " + year + "," + month + ", 0, m.nzp_kvar, m.nzp_frm, m.order_print, " +
                              "     m.NZP_SERV, m.NZP_SERV, '' , " +
                              "     m.tarif, m.SUM_TARIF, m.RSUM_TARIF, m.SUM_CHARGE," +
                              "     m.SUM_PERE, m.SUM_REAL, m.sum_lgota, m.SUM_MONEY," +
                              "     m.money_from, m.SUM_NEDOP, m.SUM_INSALDO, " +
                              "     m.sum_outsaldo, m.sum_tarif_sn_f, m.reval, " +
                              "     m.REAL_CHARGE,  m.c_okaz, m.c_nedop, m.c_calc, " +
                              "     m.c_reval, m.nzp_supp " +
                              " from   " + points.pref + "_charge_" + sYear +
                              tableDelimiter + "charge_" + month + " m,   " +
                              points.pref + "_data.kvar k " +
                              " where m.nzp_kvar=k.nzp_kvar " +
                              " and k.typek=1 " +
                              " and nzp_serv>1 " + (nzpSupp == 0 ? "" : " and nzp_supp=" + nzpSupp) +
                              " and  m.dat_charge is null ";


                        if (!ExecSQL(_conDb, sql, true).result) return;



                        ExecSQL(_conDb, " create index ix1_ch on tmp_charge(nzp_kvar) ", true);
                        ExecSQL(_conDb, " create index ix2_ch on tmp_charge(nzp_frm) ", true);
                        ExecSQL(_conDb, " create index ix3_ch on tmp_charge(nzp_serv) ", true);
                        ExecSQL(_conDb, " create index ix6_ch on tmp_charge(nzp_kvar, nzp_frm, nzp_serv , tarif) ", true);
                        ExecSQL(_conDb, " create index ix6_ch1 on tmp_charge(nzp_kvar, nzp_frm, nzp_serv , norma ); ",
                            true);



                        ExecSQL(_conDb, sUpdStat + " tmp_charge; ", true);


                        sql = " UPDATE tmp_charge SET NORMA = (select max(a.rashod) " +
                              " from " + points.pref + "_charge_" + sYear + tableDelimiter + "calc_gku_" + month + " a " +
                              " where a.nzp_kvar=tmp_charge.nzp_kvar and a.nzp_serv=tmp_charge.nzp_serv " +
                              " and a.nzp_frm=tmp_charge.nzp_frm and a.nzp_supp=tmp_charge.nzp_supp),  " +
                              " rashod = (select max(a.rashod) " +
                              " from " + points.pref + "_charge_" + sYear + tableDelimiter + "calc_gku_" + month + " a " +
                              " where a.nzp_kvar=tmp_charge.nzp_kvar and a.nzp_serv=tmp_charge.nzp_serv " +
                              " and a.nzp_frm=tmp_charge.nzp_frm and a.nzp_supp=tmp_charge.nzp_supp)  ";
                        if (!ExecSQL(_conDb, sql, true).result) return;



                        //обновление nzp_serv на nzp_serv_base из service_union
                        sql = " UPDATE tmp_charge SET nzp_serv = (SELECT max(s.nzp_serv_base)  " +
                              " FROM  " + Points.Pref + sKernelAliasRest + "service_union s " +
                              " WHERE s.nzp_serv_uni = tmp_charge.nzp_serv) " +
                              " WHERE EXISTS (SELECT max(s.nzp_serv_base)  " +
                              " FROM " + Points.Pref + sKernelAliasRest + "service_union s " +
                              " WHERE s.nzp_serv_uni = tmp_charge.nzp_serv); ";



                        if (!ExecSQL(_conDb, sql, true).result) return;

                        ExecSQL(_conDb,
                            " create index ix6_ch2 on tmp_charge(nzp_kvar,nzp_frm, nzp_serv ,NZP_SERV_VAR, tarif ); ",
                            true);
                    }
                    //группировка по nzp_serv 
                    sql =
                        " insert into tmp_charge2( YEAR_,MONTH_,PKOD,nzp_kvar,nzp_frm, order_print," +
                        " NZP_SERV,NZP_SERV_VAR,ED_IZMER, tarif,SUM_TARIF,RSUM_TARIF,SUM_CHARGE,SUM_PERE," +
                        " SUM_REAL,sum_lgota,SUM_MONEY, money_from,SUM_NEDOP,SUM_INSALDO, sum_outsaldo, " +
                        " sum_tarif_sn_f, reval, REAL_CHARGE,  c_okaz, c_nedop, c_calc, c_reval,rashod," +
                        " NORMA,nzp_supp) " +

                        " select    max(YEAR_),max(MONTH_),max(pkod),nzp_kvar, nzp_frm, " +
                        " max(order_print), NZP_SERV, NZP_SERV_VAR, max(ED_IZMER), tarif," +
                        " sum(SUM_TARIF),sum(RSUM_TARIF),sum(SUM_CHARGE), " +
                        " sum(SUM_PERE),sum(SUM_REAL),sum(sum_lgota),sum(SUM_MONEY), " +
                        " sum(money_from),sum(SUM_NEDOP),sum(SUM_INSALDO), sum(sum_outsaldo), " +
                        " sum(sum_tarif_sn_f), sum(reval), sum(REAL_CHARGE),  sum(c_okaz), sum(c_nedop), " +
                        " sum(c_calc), sum(c_reval), " +
                        " max(rashod),max(NORMA),nzp_supp" +
                        " from  tmp_charge  group by nzp_kvar,nzp_frm, nzp_serv ,NZP_SERV_VAR, tarif,nzp_supp; ";
                    if (!ExecSQL(_conDb, sql, true).result) return;

                    excelRepDb.SetMyFileProgress(new ExcelUtility
                    {
                        nzp_exc = nzpExc,
                        progress = (0.2m + 0.5m / _listPrefix.Count * j)
                    });
                }




                ExecSQL(_conDb, "  create index ix1_ch2 on tmp_charge2(nzp_kvar); ", true);
                ExecSQL(_conDb, "   create index ix2_ch2 on tmp_charge2(nzp_frm); ", true);
                ExecSQL(_conDb, "   create index ix3_ch2 on tmp_charge2(nzp_serv); ", true);
                ExecSQL(_conDb, "   create index ix3_ch22 on tmp_charge2(nzp_kvar, pkod); ", true);
                ExecSQL(_conDb, "   create index ix3_ch23 on tmp_charge2(nzp_frm, nzp_serv, ed_izmer); ", true);
                ExecSQL(_conDb, "   create index ix3_ch24 on tmp_charge2(nzp_frm,  ed_izmer); ", true);
                ExecSQL(_conDb, sUpdStat + "  tmp_charge2; ", true);


                //обновление pkod 

                sql = " UPDATE tmp_charge2 SET pkod = (SELECT k.pkod " +
                      " FROM   " + Points.Pref + sDataAliasRest + "kvar k  " +
                      " WHERE k.nzp_kvar = tmp_charge2.nzp_kvar)";
                if (!ExecSQL(_conDb, sql, true).result) return;

                //обновление ed_izmer для nzp_frm<=0 

                sql = " UPDATE tmp_charge2 SET ED_IZMER = (SELECT s.ed_izmer " +
                      " FROM   " + Points.Pref + sKernelAliasRest + "services s " +
                      " WHERE s.nzp_serv = tmp_charge2.nzp_serv) WHERE nzp_frm<=0; ";
                if (!ExecSQL(_conDb, sql, true).result) return;


                //обновление ed_izmer для nzp_frm>0 
                sql = " UPDATE tmp_charge2 SET ED_IZMER = (SELECT m. measure " +
                      " FROM   " + Points.Pref + sKernelAliasRest + "s_measure m, " +
                      "        " + Points.Pref + sKernelAliasRest + "formuls f " +
                      " WHERE tmp_charge2.nzp_frm = f.nzp_frm " +
                      " and m.nzp_measure=f.nzp_measure ) " +
                      " WHERE nzp_frm>0 ;  ";
                if (!ExecSQL(_conDb, sql, true).result) return;

                #endregion



                Utils.setCulture();

                const string sqlc = "Select count(*) from tmp_charge2 where pkod is not null ";
                object obj = ExecScalar(_conDb, sqlc, out ret, true);
                int count = obj != null ? Convert.ToInt32(obj.ToString().Trim()) : 0;

                MonitorLog.WriteLog("В темповой таблице: " + count + " записей", MonitorLog.typelog.Info, true);
                int countRez = count / 10000 + 1;
                if (count == 0) return;

                ExecSQL(_conDb, " drop table tmp_charge_distinct;", false);
                sql = " Select distinct " +
                      " YEAR_,MONTH_,PKOD,ORDER_PRINT, NZP_SERV,NZP_SERV_VAR," +
                      " ED_IZMER, tarif,SUM_TARIF,RSUM_TARIF,SUM_CHARGE,SUM_PERE," +
                      " SUM_REAL,sum_lgota,SUM_MONEY, " +
                      " money_from,SUM_NEDOP,SUM_INSALDO, sum_outsaldo, sum_tarif_sn_f," +
                      " reval, REAL_CHARGE,  c_okaz, c_nedop, c_calc, c_reval, " +
                      " norma, rashod,nzp_supp into temp tmp_charge_distinct" +
                      " From tmp_charge2  ";
                ExecSQL(_conDb, sql, true);

                for (int i = 1; i <= countRez; i++)
                {
                    MonitorLog.WriteLog("Выгрузка в кассу: " + i + "  шаг " + countRez + " всего ",
                        MonitorLog.typelog.Info, true);
                    int count_c = 10000; // (int)count / count_rez;


                    string sqls = " Select * From tmp_charge_distinct  " +
                                  " offset " + count_c * (i - 1) + " limit " + count_c + "  ";

                    DataTable dt = DBManager.ExecSQLToTable(_conDb, sqls);

                    try
                    {
                        //while (reader.Read())//
                        using (StreamWriter sw = new StreamWriter(filename, true, Encoding.GetEncoding(1251)))
                        {
                            foreach (DataRow reader2 in dt.Rows)
                            {
                                string edIzm = reader2["ED_IZMER"] != DBNull.Value
                                    ? (reader2["ED_IZMER"]).ToString().Trim() : "Неопр.";

                                string s = "*charge*|" +
                                           year + "|" + month + "|" +
                                           (reader2["PKOD"] != DBNull.Value
                                               ? ((Decimal)reader2["PKOD"]).ToString("0").Trim() + "|"
                                               : "0|") +
                                           (reader2["ORDER_PRINT"] != DBNull.Value
                                               ? ((int)reader2["ORDER_PRINT"]) + "|"
                                               : "0|") +
                                           (reader2["NZP_SERV_VAR"] != DBNull.Value
                                               ? ((int)reader2["NZP_SERV_VAR"]) + "|"
                                               : "0|") +
                                               (String.IsNullOrEmpty(edIzm) ? "Неопр.|" : edIzm + "|") +
                                    //            (reader["nzp_supp"] != DBNull.Value ? ((int)reader["nzp_supp"]) + "|" : "|") +
                                           (reader2["tarif"] != DBNull.Value
                                               ? ((Decimal)reader2["tarif"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["SUM_TARIF"] != DBNull.Value
                                               ? ((Decimal)reader2["SUM_TARIF"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["RSUM_TARIF"] != DBNull.Value
                                               ? ((Decimal)reader2["RSUM_TARIF"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["SUM_CHARGE"] != DBNull.Value
                                               ? ((Decimal)reader2["SUM_CHARGE"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["SUM_PERE"] != DBNull.Value
                                               ? ((Decimal)reader2["SUM_PERE"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["SUM_REAL"] != DBNull.Value
                                               ? ((Decimal)reader2["SUM_REAL"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["sum_lgota"] != DBNull.Value
                                               ? ((Decimal)reader2["sum_lgota"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["SUM_MONEY"] != DBNull.Value
                                               ? ((Decimal)reader2["SUM_MONEY"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["money_from"] != DBNull.Value
                                               ? ((Decimal)reader2["money_from"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +

                                           (reader2["SUM_NEDOP"] != DBNull.Value
                                               ? ((Decimal)reader2["SUM_NEDOP"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["SUM_INSALDO"] != DBNull.Value
                                               ? ((Decimal)reader2["SUM_INSALDO"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["sum_outsaldo"] != DBNull.Value
                                               ? ((Decimal)reader2["sum_outsaldo"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["sum_tarif_sn_f"] != DBNull.Value
                                               ? ((Decimal)reader2["sum_tarif_sn_f"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +

                                           (reader2["reval"] != DBNull.Value
                                               ? ((Decimal)reader2["reval"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["REAL_CHARGE"] != DBNull.Value
                                               ? ((Decimal)reader2["REAL_CHARGE"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["c_okaz"] != DBNull.Value
                                               ? ((Decimal)reader2["c_okaz"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["c_nedop"] != DBNull.Value
                                               ? ((Decimal)reader2["c_nedop"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["c_calc"] != DBNull.Value
                                               ? ((Decimal)reader2["c_calc"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["c_reval"] != DBNull.Value
                                               ? ((Decimal)reader2["c_reval"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["norma"] != DBNull.Value
                                               ? ((Decimal)reader2["norma"]).ToString("0.00").Trim() + "|"
                                               : "0.00|") +
                                           (reader2["rashod"] != DBNull.Value
                                               ? ((Decimal)reader2["rashod"]).ToString("0.0000000").Trim() + "|"
                                               : "0.0000000|");

                                sw.WriteLine(s);
                            }
                            sw.Flush();
                            sw.Close();
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при записи данных в writer в GetUploadCharge " + ex.Message,
                            MonitorLog.typelog.Error, true);

                        ret.result = false;
                        return;
                    }
                    dt.Clear();
                    dt.Dispose();
                    excelRepDb.SetMyFileProgress(new ExcelUtility
                    {
                        nzp_exc = nzpExc,
                        progress = (0.7m + 0.3m * i / countRez)
                    });
                }

            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Ошибка функции GetUploadCharge " + e.Message, MonitorLog.typelog.Error, true);
            }
            ret.text = "Выполнено";
            ret.result = true;


        }
    }


    /// <summary>
    /// Выгрузка адресного пространства и состояния по ЛС для кассы 3.0
    /// </summary>
    public class UnloadForKassa30Ls : DataBaseHead
    {
        private readonly IDbConnection _conDb;
        private DataSet _dataSet;
        private decimal _increment;

        public event EventHandler<ProgressEventArgs> RaiseProgressEvent;


        public UnloadForKassa30Ls(IDbConnection conDb)
        {
            _conDb = conDb;
        }


        protected virtual void OnRaiseProgressEvent(ProgressEventArgs e)
        {
            EventHandler<ProgressEventArgs> handler = RaiseProgressEvent;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Данные для кассы 3.0 по лс
        /// </summary>
        /// <returns></returns>
        public Returns GetUploadLS(StreamWriter writer, int nzpSupp, List<_Point> pointlist)
        {
            Returns ret = Utils.InitReturns();
            _dataSet = new DataSet("ЛС");
            if (pointlist.Count > 0) _increment = 10m / pointlist.Count;
            try
            {


                #region Получение ЛС


                PrepareUnloadTable(nzpSupp, pointlist);
                OnRaiseProgressEvent(new ProgressEventArgs(0.02m));

                //Наименование ЕРЦ
                UnloadErcName(nzpSupp);

                //страны
                UnloadCountries();

                //регионы
                UnloadRegions();

                //Города
                UnloadTown();

                //Районы
                UnloadRajons();

                //Улицы
                UnloadStreets();
                OnRaiseProgressEvent(new ProgressEventArgs(0.03m));

                //Дома
                UnloadHouses();
                OnRaiseProgressEvent(new ProgressEventArgs(0.04m));

                //Управляющая организация
                UnloadUk();
                OnRaiseProgressEvent(new ProgressEventArgs(0.05m));
                //Лицевые счета
                UnloadLS();
                OnRaiseProgressEvent(new ProgressEventArgs(0.06m));

                //Характеристики лицевого счета
                UnloadLsDetails();
                OnRaiseProgressEvent(new ProgressEventArgs(0.07m));

                UnloadSosLs();
                OnRaiseProgressEvent(new ProgressEventArgs(0.08m));

                //ПСС
                UnloadPss();
                OnRaiseProgressEvent(new ProgressEventArgs(0.09m));
                _conDb.Close();


                #endregion

                ret.text = "Выполнено";
                ret.result = true;

            }
            catch (Exception)
            {
                ret.text = "Ошибка выполнения выгрузки по адресам";
                ret.result = false;
            }
            finally
            {
                _conDb.Close();

            }

            if (ret.result) WriteInWriter(writer);

            return ret;
        }


        /// <summary>
        /// Непосредственная запись в файл
        /// </summary>
        /// <param name="writer">Ссылка на писатель</param>
        private void WriteInWriter(StreamWriter writer)
        {
            const decimal value = 1;
            foreach (DataTable thisTable in _dataSet.Tables)
            {
                foreach (DataRow row in thisTable.Rows)
                {
                    writer.Write(thisTable.Namespace);
                    foreach (DataColumn column in thisTable.Columns)
                    {
                        if (ReferenceEquals(row[column].GetType(), value.GetType()))
                        {
                            writer.Write(((Decimal)(row[column])).ToString("0") + "|");
                        }
                        else
                            writer.Write(row[column].ToString().Trim() + "|");
                        writer.Flush();
                    }
                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// Выгрузка ПСС
        /// </summary>
        private void UnloadPss()
        {
            string sql1;
            DataTable resTable = new DataTable("ПСС");
            foreach (var item in Points.PointList)
            {
                resTable.Namespace = "*prm_15*|";
                sql1 = " Select distinct num_ls, a.nzp_prm,a.dat_s,a.dat_po,a.val_prm,pkod " +
                       " From " + item.pref + "_data.prm_15 a, tmp_loadls b " +
                       " Where a.nzp = b.nzp_kvar  " +
                       "   and is_actual= 1   " +
                       "   and nzp_prm  = 162 " +
                       "   and dat_po>=" + sCurDate + " " +
                       " Order by pkod ";

                MyDataReader reader;
                if (!ExecRead(_conDb, out reader, sql1, true).result)
                    throw new Exception("Ошибка выборки данных");

                if (reader != null)
                {
                    DataColumn col = new DataColumn();
                    resTable.Columns.Add(col);
                    try
                    {

                        while (reader.Read())
                        {
                            string str = "";
                            if (reader["pkod"] != DBNull.Value)
                                str += Convert.ToString(Convert.ToDecimal(reader["pkod"]).ToString("0")) + "|";
                            else str += "|";
                            if (reader["nzp_prm"] != DBNull.Value)
                                str += ((int)reader["nzp_prm"]).ToString(CultureInfo.InvariantCulture).Trim() + "|";
                            else str += "|";
                            if (reader["dat_s"] != DBNull.Value)
                                str += ((DateTime)reader["dat_s"]).ToString("dd.MM.yyyy").Trim() + "|";
                            else str += "|";
                            if (reader["dat_po"] != DBNull.Value)
                                str += ((DateTime)reader["dat_po"]).ToString("dd.MM.yyyy").Trim() + "|";
                            else str += "|";
                            if (reader["val_prm"] != DBNull.Value) str += reader["val_prm"].ToString().Trim();
                            else str += "|";
                            resTable.Rows.Add(str);
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }
            _dataSet.Tables.Add(resTable);

        }

        /// <summary>
        /// Выгрузка состояния ЛС
        /// </summary>
        private void UnloadSosLs()
        {
            MyDataReader reader;
            ExecSQL(_conDb, "drop table t_prm_3; ", false);
            string sql1 = "create temp table t_prm_3 (" +
                          " pkod char(100)," +
                          " nzp_prm integer," +
                          " dat_s date," +
                          " dat_po date," +
                          " val_prm char(100)" +
                          "); ";
            if (!ExecSQL(_conDb, sql1, true).result)
                throw new Exception("Ошибка выборки данных");


            //Состояние ЛС
            DataTable resTable = new DataTable("Состояние ЛС");
            foreach (var item in Points.PointList)
            {
                resTable.Namespace = "*prm_3*|";

                sql1 = " Insert into t_prm_3(pkod,nzp_prm,dat_s,dat_po,val_prm) " +
                       " Select distinct pkod, a.nzp_prm,a.dat_s,a.dat_po,a.val_prm " +
                       " From " + item.pref + sDataAliasRest + "prm_3 a, tmp_loadls b " +
                       " Where a.nzp = b.nzp_kvar  " +
                       "   and is_actual= 1 and pkod is not null " +
                       "   and nzp_prm  = 51 " +
                       "   and dat_po>=" + sCurDate + " ";
                if (!ExecSQL(_conDb, sql1, true).result)
                {
                    throw new Exception("Ошибка выборки данных");
                }
            }


            sql1 = " select * from t_prm_3 Order by pkod ";

            if (!ExecRead(_conDb, out reader, sql1, true).result)
                throw new Exception("Ошибка выборки данных");




            if (reader != null)
            {
                DataColumn col = new DataColumn();
                resTable.Columns.Add(col);
                try
                {
                    while (reader.Read())
                    {
                        string str = "";
                        if (reader["pkod"] != DBNull.Value)
                            str += Convert.ToString(Convert.ToDecimal(reader["pkod"]).ToString("0")) + "|";
                        else str += "|";
                        if (reader["nzp_prm"] != DBNull.Value)
                            str += ((int)reader["nzp_prm"]).ToString(CultureInfo.InvariantCulture).Trim() + "|";
                        else str += "|";
                        if (reader["dat_s"] != DBNull.Value)
                            str += ((DateTime)reader["dat_s"]).ToString("dd.MM.yyyy").Trim() + "|";
                        else str += "|";
                        if (reader["dat_po"] != DBNull.Value)
                            str += ((DateTime)reader["dat_po"]).ToString("dd.MM.yyyy").Trim() + "|";
                        else str += "|";
                        if (reader["val_prm"] != DBNull.Value) str += reader["val_prm"].ToString().Trim();
                        else str += "|";
                        resTable.Rows.Add(str);
                    }

                }
                finally
                {
                    reader.Close();

                }
            }
            _dataSet.Tables.Add(resTable);
        }


        /// <summary>
        /// Выгрузка характеристик ЛС
        /// </summary>
        private void UnloadLsDetails()
        {
            MyDataReader reader;
            DataTable resTable = new DataTable("Состояние ЛС1");
            ExecSQL(_conDb, "drop table t_prm_1; ", false);
            var sqlStr = "create temp table t_prm_1 (" +
                         " pkod char(100)," +
                         " nzp_prm integer," +
                         " dat_s date," +
                         " dat_po date," +
                         " val_prm char(100)); ";
            ExecSQL(_conDb, sqlStr, true);
            foreach (var item in Points.PointList)
            {
                resTable.Namespace = "*prm_1*|";

                sqlStr = " Insert into t_prm_1(pkod, nzp_prm, dat_s, dat_po, val_prm) " +
                         " Select distinct pkod, a.nzp_prm,a.dat_s,a.dat_po,a.val_prm " +
                         " From " + item.pref + sDataAliasRest + "prm_1 a, tmp_loadls b " +
                         " Where a.nzp = b.nzp_kvar  " +
                         "   and is_actual= 1 and pkod is not null  " +
                         "   and nzp_prm  in (4,5,10,133) " +
                         "   and dat_po>=" + sCurDate + " ";
                Returns ret = ExecSQL(_conDb, sqlStr, true);
                if (!ret.result)
                    throw new Exception("Ошибка выборки данных");

            }

            string sql1 = " select * from t_prm_1 Order by pkod ";
            if (!ExecRead(_conDb, out reader, sql1, true).result)
            {
                throw new Exception("Ошибка выборки данных");
            }
            if (reader != null)
            {
                try
                {


                    DataColumn col = new DataColumn();
                    resTable.Columns.Add(col);

                    while (reader.Read())
                    {
                        string str = "";
                        if (reader["pkod"] != DBNull.Value)
                            str += Convert.ToString(Convert.ToDecimal(reader["pkod"]).ToString("0")) + "|";
                        else str += "|";
                        if (reader["nzp_prm"] != DBNull.Value)
                            str += ((int)reader["nzp_prm"]).ToString(CultureInfo.InvariantCulture).Trim() + "|";
                        else str += "|";
                        if (reader["dat_s"] != DBNull.Value)
                            str += ((DateTime)reader["dat_s"]).ToString("dd.MM.yyyy").Trim() + "|";
                        else str += "|";
                        if (reader["dat_po"] != DBNull.Value)
                            str += ((DateTime)reader["dat_po"]).ToString("dd.MM.yyyy").Trim() + "|";
                        else str += "|";
                        str += reader["val_prm"].ToString().Trim() + "|1";
                        resTable.Rows.Add(str);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }



            _dataSet.Tables.Add(resTable);

        }

        /// <summary>
        /// Выгрузка лицевых счетов
        /// </summary>
        private void UnloadLS()
        {
            string sql1 = " Select num_ls,nzp_dom,nkvar,nkvar_n,porch,phone,fio,nzp_area,ikvar, pkod, ecode, ls_code " +
                          " From tmp_loadls " +
                          " where pkod is not null  ";
            var resTable = DBManager.ExecSQLToTable(_conDb, sql1);
            resTable.Namespace = "*kvar*|";
            resTable.TableName = "Лицевые счета";
            _dataSet.Tables.Add(resTable);
        }


        /// <summary>
        /// Выгрузка управляющих компаний
        /// </summary>
        private void UnloadUk()
        {
            string sql1 = " Select distinct nzp_area,area " +
                          " From " + Points.Pref + sDataAliasRest + "s_area " +
                          " Where nzp_area in ( Select nzp_area From tmp_loadls ) " +
                          " Order by 1 ;";
            DataTable resTable = DBManager.ExecSQLToTable(_conDb, sql1);
            resTable.Namespace = "*area*|";
            resTable.TableName = "Управляющие организации";
            _dataSet.Tables.Add(resTable);
        }


        /// <summary>
        /// Выгрузка домов
        /// </summary>
        private void UnloadHouses()
        {
            string sql1 = " Select distinct nzp_dom, nzp_ul, idom, ndom, nkor, indecs " +
                          " From tmp_loadls Order by 1 ";
            DataTable resTable = DBManager.ExecSQLToTable(_conDb, sql1);
            resTable.Namespace = "*dom*|";
            resTable.TableName = "Дома";
            _dataSet.Tables.Add(resTable);

        }

        /// <summary>
        /// Выгрузка улиц
        /// </summary>
        private void UnloadStreets()
        {
            MyDataReader reader;
            string sql1 = " Select distinct nzp_ul, ulica, ulicareg, nzp_raj " +
                          " From tmp_loadls Order by 1 ";
            if (!ExecRead(_conDb, out reader, sql1, true).result)
                throw new Exception("Ошибка выборки данных при выгрузке улиц в кассу");

            if (reader != null)
            {
                DataTable resTable = new DataTable("Улицы");
                resTable.Namespace = "*ulica*|";
                resTable.Columns.Add("nzp_ul");
                resTable.Columns.Add("ulica");
                resTable.Columns.Add("ulicareg");
                resTable.Columns.Add("nzp_raj");
                while (reader.Read())
                {
                    resTable.Rows.Add(
                        reader["nzp_ul"] != DBNull.Value ? Convert.ToInt32(reader["nzp_ul"]) : 0,
                        reader["ulica"] != DBNull.Value ? Convert.ToString(reader["ulica"]) : "",
                        reader["ulicareg"] != DBNull.Value ? Convert.ToString(reader["ulicareg"]) : "",
                        reader["nzp_raj"] != DBNull.Value ? Convert.ToInt32(reader["nzp_raj"]) : 0
                        );
                }
                //res_table.Load(reader, LoadOption.PreserveChanges);
                _dataSet.Tables.Add(resTable);
            }

        }

        /// <summary>
        /// Выгрузка населенных пунктов
        /// </summary>
        private void UnloadRajons()
        {
            string sql1 = " Select nzp_raj,nzp_town,rajon,rajon_t,soato " +
                          " From " + Points.Pref + sDataAliasRest + "s_rajon " +
                          " Where nzp_raj in ( Select nzp_raj From tmp_loadls ) ";
            var resTable = DBManager.ExecSQLToTable(_conDb, sql1);
            resTable.Namespace = "*rajon*|";
            resTable.TableName = "Районы";
            _dataSet.Tables.Add(resTable);
        }


        /// <summary>
        /// Выгрузка городов
        /// </summary>
        private void UnloadTown()
        {
            string sql1 = "  Select t.nzp_town,t.nzp_stat,t.town,t.town_t,t.soato " +
                          " From  " + Points.Pref + sDataAliasRest + "s_town t, " +
                          "       " + Points.Pref + sDataAliasRest + "s_stat s " +
                          " where s.nzp_stat=t.nzp_stat  ";
            var resTable = DBManager.ExecSQLToTable(_conDb, sql1);
            resTable.Namespace = "*town*|";
            resTable.TableName = "Города";
            _dataSet.Tables.Add(resTable);
        }

        /// <summary>
        /// Выгрузка регионов
        /// </summary>
        private void UnloadRegions()
        {
            string sql1 = " Select s.nzp_stat,s.nzp_land,s.stat,s.stat_t,s.soato " +
                          " From " + Points.Pref + sDataAliasRest + "s_stat s, " +
                          "      " + Points.Pref + sDataAliasRest + "s_land l " +
                          " where  s.nzp_land=l.nzp_land  ";
            var resTable = DBManager.ExecSQLToTable(_conDb, sql1);
            resTable.TableName = "Регионы";
            resTable.Namespace = "*stat*|";
            _dataSet.Tables.Add(resTable);
        }

        /// <summary>
        /// Выгрузка стран
        /// </summary>
        private void UnloadCountries()
        {
            string sql1 = " Select nzp_land,land,land_t,soato From " + Points.Pref + sDataAliasRest + "s_land ";
            DataTable resTable = DBManager.ExecSQLToTable(_conDb, sql1);
            resTable.Namespace = "*land*|";
            resTable.TableName = "Страны";
            _dataSet.Tables.Add(resTable);
        }


        /// <summary>
        /// Выгрузка наименование ЕРЦ
        /// </summary>
        private void UnloadErcName(int nzpSupp)
        {
            string suppName = String.Empty;

            if (nzpSupp > 0)
            {
                Returns ret;
                object obj = ExecScalar(_conDb, "select name_supp from " + Points.Pref +
                                               sKernelAliasRest + "supplier where nzp_supp=" + nzpSupp, out ret, true);

                if (ret.result && obj != null && obj != DBNull.Value)
                    suppName = obj.ToString().Trim();
            }

            string sql1 = " select s.name_raj, p.val_prm as name_erc, s.erc_code,'' as ls_code ,'" +
                          (nzpSupp != 0 ? nzpSupp.ToString(CultureInfo.InvariantCulture) : "") + "' as nzp_supp,'" +
                          (nzpSupp != 0 ? suppName : "") + "' as supp_name " +
                          " from " + Points.Pref + sDataAliasRest + "prm_10 p,  " +
                          Points.Pref + sKernelAliasRest + "s_erc_code s " +
                          " where p.nzp_prm=80 " +
                          " and s.is_current=1 " +
                          " and p.is_actual<>100";
            var resTable = DBManager.ExecSQLToTable(_conDb, sql1);
            resTable.TableName = "Наименование ЕРЦ";
            resTable.Namespace = "*s_erc_code*|";
            _dataSet.Tables.Add(resTable);
        }


        /// <summary>
        /// Подготовка таблицы для выгрузки
        /// </summary>
        /// <param name="nzpSupp">Код поставщика, если нет то 0</param>
        /// <param name="pointlist">Список префиксов схем БД</param>
        private void PrepareUnloadTable(int nzpSupp, IEnumerable<_Point> pointlist)
        {
            ExecSQL(_conDb, " drop table tmp_loadls;", false);

            string sql1 = "Create temp table tmp_loadls(" +
                          " num_ls integer, " +
                          " nzp_kvar integer, " +
                          " nzp_dom integer, " +
                          " nzp_ul integer, " +
                          " nzp_area integer, " +
                          " nzp_geu integer, " +
                          " nzp_raj integer, " +
                          " porch smallint, " +
                          " phone char(200), " +
                          " fio char(200), " +
                          " area char(200), " +
                          " geu char(200), " +
                          " nzp_supp char(100), " +
                          " ulica char(200), " +
                          " ulicareg char(200), " +
                          " ndom char(200), " +
                          " nkor char(200), " +
                          " nkvar char(200), " +
                          " nkvar_n char(200), " +
                          " idom integer, " +
                          " ikvar integer, " +
                          " indecs char(20), " +
                          " pkod " + sDecimalType + "(13,0), " +
                          " ecode char(20), " +
                          " ls_code char(20) )" + sUnlogTempTable;
            ExecSQL(_conDb, sql1, false);

            Returns ret;
            sql1 = " select max(erc_code) from " + Points.Pref + sKernelAliasRest + "s_erc_code " +
                   " where is_current=1";
            object obj = ExecScalar(_conDb, sql1, out ret, true);
            string ercCode = "''";
            if (ret.result && obj != null && obj != DBNull.Value)
            {
                ercCode = obj.ToString();
            }

            int step = 0;
            foreach (var pref in pointlist)
            {

                sql1 = "Create temp table sel_kvar(nzp_kvar integer, nzp_supp integer)" + sUnlogTempTable;
                ExecSQL(_conDb, sql1, true);

                sql1 = " insert into sel_kvar (nzp_kvar, nzp_supp)" +
                       " select k.nzp_kvar, " + (nzpSupp != 0 ? " t.nzp_supp " : " null ") +
                       " from " + pref.pref + sDataAliasRest + "kvar k, " + pref.pref + sDataAliasRest + "prm_3 b" +
                        (nzpSupp != 0 ? "," + pref.pref + sDataAliasRest + "tarif t " : "") +
                       " where k.typek=1 and k.nzp_kvar=b.nzp " +
                       " and b.nzp_prm=51 and b.val_prm in ('1','2') and b.is_actual<>100 " +
                       " and b.dat_s<=" + sCurDate + " and b.dat_po>=" + sCurDate;
                if (nzpSupp != 0)
                    sql1 += " and k.nzp_kvar=t.nzp_kvar " +
                            " and t.is_actual<>100 " +
                            " and t.nzp_supp = " + nzpSupp +
                            " and t.dat_s<=" + sCurDate +
                            " and t.dat_po>= " + sCurDate;
                ExecSQL(_conDb, sql1, true);

                ExecSQL(_conDb, "create index ix_skv_022 on sel_kvar(nzp_kvar)", true);
                ExecSQL(_conDb, sUpdStat + " sel_kvar ", true);

                OnRaiseProgressEvent(new ProgressEventArgs(0.00m + _increment * (step - 0.5m) * 0.001m));

                sql1 = " Insert into tmp_loadls (num_ls, nzp_kvar, nzp_dom, nzp_ul, nzp_area, nzp_geu," +
                       " nzp_raj, porch, phone, fio, area, geu, nzp_supp, ulica, ulicareg, ndom, nkor, nkvar," +
                       " nkvar_n, idom, ikvar, indecs, pkod, ecode, ls_code) " +
                       " Select k.num_ls, k.nzp_kvar, k.nzp_dom, d.nzp_ul, k.nzp_area, " +
                       " k.nzp_geu as nzp_geu,u.nzp_raj, porch,phone,fio, area,geu, " +
                       " sk.nzp_supp, ulica, ulicareg, ndom, nkor, nkvar, nkvar_n, idom, " +
                       " ikvar, indecs, pkod, " + ercCode + " as ecode,'' as ls_code " +
                       " From sel_kvar sk, " + pref.pref + sDataAliasRest + "kvar k left outer join " +
                       Points.Pref + sDataAliasRest + "s_geu g on k.nzp_geu  = g.nzp_geu, " +
                       Points.Pref + sDataAliasRest + "dom d, " +
                       Points.Pref + sDataAliasRest + "s_ulica u, " +
                       Points.Pref + sDataAliasRest + "s_area a " +
                       " Where sk.nzp_kvar=k.nzp_kvar " +
                       " and k.nzp_dom = d.nzp_dom " +
                       " and d.nzp_ul  = u.nzp_ul " +
                       " and k.nzp_area = a.nzp_area " +
                       " and k.num_ls > 0 " +
                       " and k.pkod is not null  ";
                if (!ExecSQL(_conDb, sql1, true).result)
                    throw new Exception("таблица tmp_loadls не создана");

                ExecSQL(_conDb, "drop table sel_kvar", true);
                OnRaiseProgressEvent(new ProgressEventArgs(0.00m + _increment * step * 0.001m));
                step++;
            }


            ExecSQL(_conDb, " Create index ix_tls_1 on tmp_loadls (nzp_kvar); ", true);
            ExecSQL(_conDb, " Create index ix_tls_2 on tmp_loadls (num_ls); ", true);
            ExecSQL(_conDb, " Create index ix_tls_3 on tmp_loadls (nzp_dom); ", true);
            ExecSQL(_conDb, " Create index ix_tls_4 on tmp_loadls (nzp_ul); ", true);
            ExecSQL(_conDb, " Create index ix_tls_5 on tmp_loadls (nzp_raj); ", true);
        }
    }
}

