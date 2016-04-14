using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbAdresKernel : DataBaseHeadServer
    {
        /// <summary>
        /// Генерация платежного кода
        /// </summary>
        /// <param name="conn_db">текущее подключение</param>
        /// <param name="transaction">текущая транзакция</param>
        /// <param name="finder">параметры: либо (nzp_kvar, pref) либо (pref, nzp_area, nzp_geu)</param>
        /// <param name="ret">результат</param>
        /// <returns>платежный код</returns>
        public string GeneratePkod(IDbConnection conn_db, IDbTransaction transaction, Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            
            //Добавление ЛС
            if (finder.nzp_kvar < 1)
            {
                var series = Points.isUseSeries ? new Series(new int[] {1, 2, 10}) : new Series(new int[] {1, 2});

                var db = new DbSpravKernel();
                db.GetSeries(conn_db, transaction, finder.pref, series, out ret);
                db.Close();
                if (!ret.result)
                {
                    ret.text = "Ошибка получения ключей: " + ret.text;
                    return "";
                }

                _Series val = series.GetSeries(1);
                finder.nzp_kvar = (val.cur_val != Constants._ZERO_) ? val.cur_val : 0;

                val = series.GetSeries(2);
                finder.num_ls = (val.cur_val != Constants._ZERO_) ? val.cur_val : 0;

                if (Points.isUseSeries)
                {
                    val = series.GetSeries(10);
                    finder.pkod10 = (val.cur_val == Constants._ZERO_) ? finder.num_ls : val.cur_val;
                }
                else
                {
                    int pkod10 = 0, area_code = 0;
                    ret = GetPkod10(conn_db, transaction, finder, out pkod10, out area_code);
                    
                    if (!ret.result)
                    {
                        if (ret.tag != -1) ret.text = "Ошибка получения pkod10. " + ret.text;
                        return "";
                    }
                    finder.pkod10 = (pkod10 == Constants._ZERO_) ? finder.num_ls : pkod10;
                }
            }
            else
            {
                MyDataReader reader = null;

                var sql = " SELECT num_ls, pkod10, nzp_area, nzp_geu, area_code" +
                          " FROM " + Points.Pref + "_data" + tableDelimiter + "kvar WHERE nzp_kvar = " + finder.nzp_kvar;

                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    ret.text = "Ошибка получения данных по ключу квартиры";
                    ret.result = false;
                    return "";
                }

                if (reader.Read())
                {
                    finder.num_ls = (reader["num_ls"] != DBNull.Value) ? Convert.ToInt32(reader["num_ls"]) : 0;
                    finder.pkod10 = (reader["pkod10"] != DBNull.Value) ? Convert.ToInt32(reader["pkod10"]) : 0;
                    finder.nzp_area = (reader["nzp_area"] != DBNull.Value) ? Convert.ToInt32(reader["nzp_area"]) : 0;
                    finder.nzp_geu = (reader["nzp_geu"] != DBNull.Value) ? Convert.ToInt32(reader["nzp_geu"]) : 0;
                }
                reader.Close();
                reader = null;
            }

            if (finder.nzp_kvar < 1 || finder.num_ls == 0 || finder.pkod10 == 0)
            {
                ret.text = "Внутренняя ошибка получения ключей. " + ret.text;
                ret.result = false;
                return "";
            }

            //вытащить коды ЕРЦ
            _KodERC erc = GetKodErc(conn_db, transaction, finder.pref, finder.nzp_area, finder.nzp_geu, out ret);
            if (!ret.result)
            {
                ret.text = "Ошибка определения кода РЦ: " + ret.text;
                return "";
            }

            if (erc.kod_erc > 1000)
            {
                finder.pkod = erc.kod_erc.ToString();
                finder.pkod += finder.pkod10.ToString("00000");
                finder.pkod = Utils.BarcodeCRC13(finder.pkod).ToString();

                if (finder.pkod.Length != 13)
                {
                    ret.text = "Ошибка определения платежного кода: " + finder.pkod;
                    ret.result = false;
                    return "";
                }
            }
            else if (Points.IsSmr) //признак Самары
            {
                //пока сделаем в лоб определение платежного кода
                //todo уточнить про -600
                

                finder.pkod = erc.kod_erc + (finder.nzp_geu < 100 ? finder.nzp_geu: finder.nzp_geu % 100).ToString("00");//"00";
                finder.pkod += (finder.pkod10.ToString().Length == 6) ? finder.pkod10.ToString("000000") : finder.pkod10.ToString("00000") + "0";
                finder.pkod += Utils.GetKontrSamara(finder.pkod);

                if (finder.pkod.Length != 13)
                {
                    ret.text = "Ошибка определения платежного кода: " + finder.pkod;
                    ret.result = false;
                    return "";
                }
            }
            else
            {
                long lpkod = Utils.EncodePKod(erc.kod_erc.ToString(), finder.num_ls);

                try
                {
                    //пока в лоб определение казанских УК
                    if (Points.Pref.Substring(0, 2) == "uk")
                   {
                        lpkod = Utils.EncodePKod(erc.kod_erc.ToString(), finder.pkod10);
                    }
                }
                catch { }


                finder.pkod = lpkod.ToString();
                if (lpkod < 1 || finder.pkod.Length != 10)
                {
                    ret.text = "Ошибка определения платежного кода: " + finder.pkod;
                    ret.result = false;
                    return "";
                }
            }

            return finder.pkod;
        }

        #region Генерация платежного кода               
        /// <summary>
        /// Генерация платежного кода
        /// </summary>
        /// <returns>результат</returns>
        public Returns GeneratePkodOnLsList(Ls finder)
        {
            IDbConnection conn_db = null;
            IDbTransaction transaction = null;
            Returns ret = Utils.InitReturns();
            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения с БД.",MonitorLog.typelog.Error,true);
                    return ret;
                }

                //таблица содержащая результат поиска ЛС
                string tXXSplsTable = String.Empty;
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
               
                    ret = OpenDb(conn_web, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения с БД.",MonitorLog.typelog.Error,true);
                        return ret;
                    }
                    //таблица выбранных ЛС
                    tXXSplsTable = DBManager.GetFullBaseName(conn_web, conn_web.Database,
                        "t" + finder.nzp_user + "_spls");                    
               conn_web.Close();

                #region Генерация
                //Таблица ЛС центральной БД
                string kvarTable = DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", "kvar");

                //список ЛС с необходимымми данными
                StringBuilder sql = new StringBuilder();
                sql.Append(" select nzp_kvar, nzp_area, nzp_geu, pref, pkod, pkod10 ");
                sql.AppendFormat(" from {0}  ", kvarTable);                
                sql.AppendFormat(" where nzp_kvar in (select nzp_kvar from {0}) ", tXXSplsTable);
                List<Ls> kvarToUpdate = DBManager.ExecSQLToTable(conn_db, sql.ToString())
                    .AsEnumerable()
                    .Select(
                        x =>
                            new Ls()
                            {
                                nzp_kvar = x["nzp_kvar"] != DBNull.Value ? Convert.ToInt32(x["nzp_kvar"]) : 0,
                                nzp_geu = x["nzp_geu"] != DBNull.Value ? Convert.ToInt32(x["nzp_geu"]) : 0,
                                nzp_area = x["nzp_area"] != DBNull.Value ? Convert.ToInt32(x["nzp_area"]) : 0,
                                pkod = x["pkod"] != DBNull.Value ? Convert.ToString(x["pkod"]).Trim() : "",
                                pref = x["pref"] != DBNull.Value ? x["pref"].ToString().Trim() : "",
                                pkod10 = x["pkod10"] != DBNull.Value ? Convert.ToInt32(x["pkod10"]) : 0
                            }).ToList();

                //Добавляем код ЖЭУ (n из s_points) - используется только в Самаре
                kvarToUpdate.ForEach(k=>k.geuCode = Points.PointList.First(p=>p.pref == k.pref).n);

               
                //Обновление платежного кода
                foreach (Ls ls in kvarToUpdate)
                {
                    //получение платежного кода, area_code, pkod10
                    int areaCode, pkod10 = 0;
                    ls.nzp_user = finder.nzp_user;
                    transaction = conn_db.BeginTransaction();
                    string pkod = this.GeneratePkodOneLS(ls, transaction, out areaCode, ref pkod10, out ret);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    if (pkod != "")
                    {
                        //сохранение в центральную базу
                        sql = new StringBuilder();
                        sql.AppendFormat(" UPDATE {0} set area_code = {1}, pkod10 = {2} ", kvarTable, areaCode, pkod10);
                        sql.AppendFormat("  ,pkod = {0} where nzp_kvar = {1} ", pkod, ls.nzp_kvar);
                        ret = DBManager.ExecSQL(conn_db, transaction, sql.ToString(), true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка сохранения платежного кода в центральный банк: " + ret.text,
                                MonitorLog.typelog.Error, true);
                            return ret;
                        }

                        //обновление в локальном банке
                        sql = new StringBuilder();
                        sql.AppendFormat(" UPDATE {0} set pkod= {1}, pkod10 = {2} where nzp_kvar = {3} ",
                            DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "kvar"),
                            pkod, //1
                            pkod10, //2
                            ls.nzp_kvar); //3
                        ret = DBManager.ExecSQL(conn_db, transaction, sql.ToString(), true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка сохранения платежного кода в локальный банк: " + ret.text,
                                MonitorLog.typelog.Error, true);
                            return ret;
                        }
                    }

                    transaction.Commit();

                    #region Определение кодов поставщиков для Самары. Закомментировано, отдельный режим
                    // Определение кодов поставщиков для Самары
                    //int areaCodeSupp, pkod10Supp = pkod10;
                    //string pkodSupp = this.GeneratePkodSuppOneLS(ls, transaction, out areaCodeSupp, ref pkod10Supp,
                    //    out ret);
                    //if (!ret.result)
                    //{
                    //    return ret;
                    //}
                    //if (pkodSupp != "")
                    //{                                                
                    //    //todo возможно переделать - Сделать проверку на существование
                    //    //обновление в локальном банке supplier_codes
                    //    ret = DBManager.ExecSQL(conn_db,transaction,
                    //        String.Format("delete from {0} where nzp_kvar={1}",
                    //            DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "supplier_codes"), ls.nzp_kvar),
                    //        true);
                    //    if (!ret.result)
                    //    {
                    //        MonitorLog.WriteLog("Ошибка сохранения удаления данных из pkodSupp в локальный банк: " + ret.text,
                    //            MonitorLog.typelog.Error, true);
                    //        return ret;
                    //    }


                    //    sql = new StringBuilder();
                    //    sql.AppendFormat(" insert into {0} (nzp_kvar,nzp_supp,kod_geu,pkod10,pkod_supp) values({1}, {2}, {3}, {4}, {5}) ",
                    //        DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "supplier_codes"),      //0
                    //        ls.nzp_kvar,                                                            //1
                    //        "null",                                                                 //2
                    //        ls.geuCode,                                                             //3
                    //        pkod10Supp,                                                             //4
                    //        pkodSupp);                                                              //5
                    //    ret = DBManager.ExecSQL(conn_db, transaction, sql.ToString(), true);
                    //    if (!ret.result)
                    //    {
                    //        MonitorLog.WriteLog("Ошибка сохранения pkodSupp в локальный банк: " + ret.text,
                    //            MonitorLog.typelog.Error, true);
                    //        return ret;
                    //    }

                    //    transaction.Commit();
                    //}
                    //else
                    //{
                    //    MonitorLog.WriteLog("pkodSupp =пусто!" + ls.pkod_supp,
                    //           MonitorLog.typelog.Warn, true);
                    //    transaction.Commit();                        
                    //}
                    #endregion       
                }                
                #endregion

                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException(" Ошибка генерации платежных кодов. ", ex);
                ret.result = false;
                return ret;
            }
            finally
            {
                if (transaction != null)
                {
                    if (!ret.result) transaction.Rollback();
                    transaction.Dispose();
                }                

                if (conn_db != null)
                {
                    conn_db.Close();
                }
            }
        }

        /// <summary>
        /// Генерация платежных кодов принципалов и агентов для списка ЛС
        /// </summary>
        /// <param name="finder">используется dopFind[0], в котором передается код таблицы с выбранным ЛС</param>
        public Returns NewGeneratePkod(Finder finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения с БД.",MonitorLog.typelog.Error,true);
                return ret;
            }
#if PG
            ExecSQL(conn_db, "set search_path to 'public'", false);
#endif

            DBMyFiles dbRep2 = new DBMyFiles();
            //добавим информацию о протоколе генерации в мои файлы            
            ret = dbRep2.AddFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Протокол генерации платежных кодов",
                is_shared = 1
            });
            int nzpExc = ret.tag; 

            dbRep2.SetFileProgress(nzpExc, (decimal)0.1);

            //подготовить таблицу на основе которой будут сгенерированы платежные коды
            string preptable = "";
            ret = NewGeneratePrepareLs(conn_db, finder, out preptable);
            if (!ret.result || preptable == "")
            {
                conn_db.Close();
                if (preptable == "") MonitorLog.WriteLog("Нет данных для генерации платежных кодов.", MonitorLog.typelog.Error, true);
                return ret;
            }

            dbRep2.SetFileProgress(nzpExc, (decimal)0.3);

            MyDataReader reader;
            StringBuilder sql = new StringBuilder();

            //для каждой записи preptable генерируется платежный код
            sql.Remove(0, sql.Length);
            sql.AppendFormat("select t.nzp_kvar,t.id, t.pref, t.nzp_payer, t.is_princip, p.payer from {0} t, {1}_kernel{2}s_payer p where p.nzp_payer = t.nzp_payer", 
                             preptable, Points.Pref, tableDelimiter);
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            int cnt = 0;
            //список. Несовпадение количества символов в составных частях. 
            //Ошибка получения кодa area_code
            List<string> listErrMsg = new List<string>();

            //список. Ошибка получения идентификатора абонента в платежном коде
            List<string> listErrPkod = new List<string>();
            while(reader.Read())
            {
                cnt++;
                dbRep2.SetFileProgress(nzpExc, (decimal)0.3 + cnt / 100);
                string errMsg = "";
                string errMsgLog = "";
                int id = 0;
                AreaCodes ac = new AreaCodes();
                if (reader["nzp_payer"] != DBNull.Value) ac.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                if (reader["id"] != DBNull.Value) id = Convert.ToInt32(reader["id"]);
                if (reader["payer"] != DBNull.Value) ac.payer = Convert.ToString(reader["payer"]);
                
                //получить код контрагента
                ac.code = NewGetAreaCodes(conn_db, null, ac, out ret);
                if (!ret.result)
                {
                    errMsg = ret.tag < 0 ? "Невозможно получить платежный код контрагента: " + ret.text : "Ошибка получения платежного кодa контрагента: " + ret.text;
                    listErrMsg.Add(errMsg);
                    errMsgLog = "Ошибка получения кодa area_code: " + ret.text;
                    MonitorLog.WriteLog("Ошибка при генерации платежных кодов. " + errMsgLog, MonitorLog.typelog.Error, true);
                    continue;
                }
                               
                //получить идентификатор абонента в платежном коде
                int pkod10 = 0;
                ret = NewGetPkod10(conn_db, null, ac, out pkod10);
                if (pkod10 == 0 || !ret.result || Points.Region.GetHashCode() <= 0 || ac.code <= 0) listErrPkod.Add(String.Format("Ошибка получения идентификатора абонента в платежном коде: regionCode={0}, areaCode={1}, pkod10={2} ",
                                        Points.Region.GetHashCode(), ac.code, pkod10));
                
                if (Points.Region.GetHashCode() > 99 || ac.code > 99999 || pkod10 > 99999)
                {
                    ret.result = false;
                    ret.text = String.Format("Ошибка генерации платежного кода. Несовпадение количества символов в составных частях. Код региона: {0}, Код контрагента: {1}, Пкод10: {2}",
                               Points.Region.GetHashCode(), ac.code, pkod10);
                    MonitorLog.WriteLog(String.Format("Ошибка генерации платежного кода: regionCode={0}, areaCode={1}, pkod10={2} ",
                                        Points.Region.GetHashCode(), ac.code, pkod10), MonitorLog.typelog.Error, true);
                    listErrMsg.Add(ret.text);
                    continue;
                }

                //генерация платежного кода
                string pkod = Points.Region.GetHashCode().ToString("00") + ac.code.ToString("00000") + pkod10.ToString("00000");                
                //считаем цифры в нечетных позициях и умножаем их на 3 после чего складываем с четными позициями 
                int sum = 0;
                for(int i = 0; i< pkod.Length; i++)            
                {                
                    sum += (i + 1) % 2 == 0 ? Convert.ToInt32(pkod[i].ToString()) : Convert.ToInt32(pkod[i].ToString()) * 3;
                }
                //определяем дополнение до 10
                int dop10 = 10 - sum%10;
                string respkod = pkod + (dop10 == 10 ? 0 : dop10);

                //обновление временной таблицы preptable
                //код контрагента, идентификатор абонента в платежном коде, сгенерированный платежный код
                sql.Remove(0, sql.Length);
                sql.AppendFormat("update {0} set area_code = {1}, pkod = {2}, pkod10 = {3} where id = {4}", preptable, ac.code, respkod, pkod10, id);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    conn_db.Close();
                    return ret;
                }
            }
            reader.Close();

            dbRep2.SetFileProgress(nzpExc, (decimal)0.7);

            //в таблицу tlogs записать платежные коды, которые уже есть в kvarpkodes
            sql.Remove(0, sql.Length);
            sql.Append("drop table tlogs");
            ExecSQL(conn_db, sql.ToString(), false);
            sql.Remove(0, sql.Length);
            sql.AppendFormat("select pt.num_ls, pt.is_princip, pt.payer, pt.area_code, pt.pkod, pt.nzp_payer into temp tlogs from {0} pt, {1}_data{2}kvar_pkodes kpk where kpk.pkod = pt.pkod", preptable, Points.Pref, tableDelimiter);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            
            #region Определить пользователя
            int nzpUser = finder.nzp_user; 
            
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret); //локальный пользователь      
            db.Close();
            if (!ret.result) 
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            //список удачных платежных кодов
            List<string> listSucces = new List<string>();
            //список ошибок
            List<string> listError = new List<string>();
            StringBuilder sb = new StringBuilder();

            if (!(listErrPkod.Count == cnt || listErrMsg.Count == cnt))
            {
                dbRep2.SetFileProgress(nzpExc, (decimal)0.9);

                //Записать в kvar_pkodes сгенерированные платежные коды из preptable, исключая те, которые попали в таблицу tlogs
                sql.Remove(0, sql.Length);
                sql.AppendFormat(" insert into {0}_data{1}kvar_pkodes (nzp_kvar, nzp_payer, is_princip, area_code, pkod10, pkod, is_default, changed_on, changed_by) ", Points.Pref, tableDelimiter);
                sql.AppendFormat(" select nzp_kvar, nzp_payer, is_princip, area_code, pkod10, pkod, 1, {0}, {1} from {2} where coalesce(pkod,0) <> 0 and pkod not in (select pkod from tlogs)", sCurDateTime, nzpUser, preptable);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                //подготовить список listSucces, который запишется в протокол
                //список содержит информацию о сгенерированных и сохраненных в 
                //kvar_pkodes платежных кодах
                sql.Remove(0, sql.Length);
                sql.AppendFormat("select num_ls,is_princip , p.payer, area_code , pkod from {0} a, {1}_kernel{2}s_payer p where a.pkod not in (select pkod from tlogs) and a.nzp_payer=p.nzp_payer", preptable, Points.Pref, tableDelimiter);
                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }                
                while (reader.Read())
                {
                    sb.Remove(0, sb.Length);
                    sb.AppendFormat("ЛС: {0}, {1}: {2}, Платежный код контрагента: {3}, Сгенерированный платежный код: {4}",
                        (reader["num_ls"] == DBNull.Value ? "" : Convert.ToString(reader["num_ls"]).Trim()),
                        ((((reader["is_princip"] == DBNull.Value ? 0 : Convert.ToInt32(reader["is_princip"]))) == 1) ? "Принципал" : "Агент"),
                        (reader["payer"] == DBNull.Value ? "" : Convert.ToString(reader["payer"]).Trim()),
                        (reader["area_code"] == DBNull.Value ? "" : Convert.ToString(reader["area_code"]).Trim()),
                        (reader["pkod"] == DBNull.Value ? "" : Convert.ToString(reader["pkod"]).Trim()));
                    listSucces.Add(sb.ToString());
                }
            }

            //подготовить список listError
            //содержит информацию из tlogs
            //дублирование платежных кодов
            sql.Remove(0, sql.Length);
            sql.AppendFormat("select a.num_ls, a.is_princip, p.payer, a.area_code, a.pkod from tlogs a, {0}_kernel{1}s_payer p where p.nzp_payer=a.nzp_payer", Points.Pref, tableDelimiter);
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            while (reader.Read())
            {
                sb.Remove(0, sb.Length);
                sb.AppendFormat("ЛС: {0}, {1}: {2}, Платежный код контрагента: {3}, Дублирующийся платежный код: {4}",
                    (reader["num_ls"] == DBNull.Value ? "" : Convert.ToString(reader["num_ls"]).Trim()),
                    ((((reader["is_princip"] == DBNull.Value ? 0 : Convert.ToInt32(reader["is_princip"]))) == 1) ? "Принципал" : "Агент"),
                    (reader["payer"] == DBNull.Value ? "" : Convert.ToString(reader["payer"]).Trim()),
                    (reader["area_code"] == DBNull.Value ? "" : Convert.ToString(reader["area_code"]).Trim()),
                    (reader["pkod"] == DBNull.Value ? "" : Convert.ToString(reader["pkod"]).Trim()));
                listError.Add(sb.ToString());
            }

            //получить имя пользователя, запустившего генерацию плат кодов, чтобы отразить его в протоколе
            sql.Remove(0, sql.Length);
            sql.AppendFormat("select uname from {0}{1}users where nzp_user={2}", DBManager.sDefaultSchema, tableDelimiter, finder.nzp_user);
            var obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (!ret.result) 
            {
                conn_db.Close();
                return ret;
            }
            finder.webLogin = (string)obj;

            //сохранение результатов в файл
            //наименование файла
            int k = 0;
            string filename = "prot_genpkod_" + finder.nzp_user + "_";
            while (System.IO.File.Exists(Constants.ExcelDir + filename + k + ".txt")) k++;
            filename = filename + k + ".txt";
            string fullfilename = Constants.ExcelDir + filename;
            
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(Constants.ExcelDir + filename, true, Encoding.GetEncoding(1251)))
            {
                //запись в файл
                file.WriteLine("Протокол результатов генерации платежных кодов от " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                file.WriteLine("Пользователь: " + finder.webLogin + " (код " + finder.nzp_user + ")");
                file.WriteLine("Всего обработано ЛС: " + cnt.ToString() + ".");
                file.WriteLine("Всего сгенерированных платежных кодов: " + listSucces.Count + ".");
                file.WriteLine("Всего ошибок: " + listErrMsg.Count + ".");
                file.WriteLine("Всего дублирований платежных кодов: " + listError.Count + ".");

                //запись ошибок
                if (listErrMsg.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список ошибок");
                    file.WriteLine();
                    foreach (string val in listErrMsg)
                    {
                        file.WriteLine(val);
                    }
                }
                if (listError.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список дублирований платежных кодов");
                    file.WriteLine();
                    int c = 0;
                    foreach (string val in listError)
                    {
                        c++;
                        file.WriteLine(c + ". "+val);
                    }
                }

                //запись результатов
                if (listSucces.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список сгенерированных платежных кодов");
                    file.WriteLine();
                    int c1 = 0;
                    foreach (string val in listSucces)
                    {
                        c1++;
                        file.WriteLine(c1 + ". " + val);
                    }
                }
            }

            if (InputOutput.useFtp) filename = InputOutput.SaveOutputFile(fullfilename);
                             
            dbRep2.SetFileStatus(nzpExc, ExcelUtility.Statuses.Success);
            dbRep2.SetFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = filename });
            dbRep2.Close();
            return ret;
        }

        /// <summary>
        /// Подготовка таблицы с заполными полямисписком num_ls, nzp_kvar, pref, payer, nzp_payer, is_princip
        /// </summary>
        /// <param name="conn_db">текущее соединение</param>
        /// <param name="finder">используется dopFind[0], в котором передается код таблицы с выбранным ЛС</param>
        /// <param name="preptable">наименование сформированной таблицы,
        /// в которую записываются ЛС и контрагенты, для которых нужно сгенерировать плтежный код
        /// </param>
        /// <returns></returns>
        public Returns NewGeneratePrepareLs(IDbConnection conn_db, Finder finder, out string preptable)
        {
            Returns ret = Utils.InitReturns();

            //таблица на основе которой будут сгенерированы платежные коды
            preptable = "";

            //список выбранных лицевых счетов, после шаблона поиска, сохраненных в таблицу tXX,
            //где XX - номер очереди & уникальный код задачи в этой очереди
            //ЛС отфильтрованы mark=1
            string tXX = "t";
            if (finder.dopFind != null && finder.dopFind.Count > 0 && finder.dopFind[0] != "")
            {
                tXX += finder.dopFind[0];
            }
            if (!TempTableInWebCashe(tXX))
            {
                ret = new Returns(false, "Не выбранных ЛС");
                MonitorLog.WriteLog("Ошибка генерации платежных кодов: " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            StringBuilder sql = new StringBuilder();
            MyDataReader reader;

            //временная таблица temptablels включающая 
            //num_ls - нужен для комментария
            //nzp_kvar, pref - для идентификации ЛС
            //код агента и принципала, dpd - признак генерировать платежный код принципала
            ExecSQL(conn_db, "drop table temptablels", false);
            ret = ExecSQL(conn_db, "create temp table temptablels (num_ls integer, nzp_kvar integer, pref varchar(20), nzp_payer_agent integer, nzp_payer_princip integer, dpd smallint)", true);
            if(!ret.result) return ret;

            //заполнение temptablels. Таблица включает все ЛС из tXX
            ret = ExecSQL(conn_db, "insert into temptablels (nzp_kvar, num_ls, pref) select nzp_kvar, num_ls, pref from " + tXX, true);
            if (!ret.result) return ret;

            //создание индекса на поле nzp_kvar для ускорения работы запросов
            ret = ExecSQL(conn_db, "create index ix_temptablels_1 on temptablels (nzp_kvar)", true);
            if (!ret.result) return ret;

            //цикл по pref для обращения к таблице loc_data.tarif
            sql.Remove(0, sql.Length);
            sql.Append("select distinct pref from temptablels");
            ExecRead(conn_db, out reader, sql.ToString(), true);
            while(reader.Read())
            {
                string pref = "";
                if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]);

                //создается временная таблица включающая все договора ЛС
                sql.Remove(0, sql.Length);
                sql.Append("drop table tsupp");
                ret = ExecSQL(conn_db, sql.ToString(), false);
                if (!ret.result) return ret;
                sql.Remove(0, sql.Length);
                sql.Append("select distinct supp.nzp_supp, supp.nzp_payer_agent, supp.nzp_payer_princip, supp.dpd, t.nzp_kvar, s.pref into temp tsupp ");
                sql.AppendFormat("from {0}_data.tarif t, temptablels s, {1}_kernel.supplier supp  ", pref, Points.Pref);
                sql.AppendFormat("where s.pref = '{0}'  and is_actual<>100 ", pref);
                sql.Append("and t.nzp_kvar = s.nzp_kvar and supp.nzp_supp = t.nzp_supp ");
                ret = ExecSQL(conn_db, sql.ToString(), false);
                if (!ret.result) return ret;

                //обновление temptablels
                //заполнение кодов агентов и принципалов для каждого ЛС, у которого есть договор
                sql.Remove(0, sql.Length);
                sql.Append(" update temptablels set ");
                sql.Append(" nzp_payer_agent = tsupp.nzp_payer_agent, nzp_payer_princip = tsupp.nzp_payer_princip, dpd = tsupp.dpd ");
                sql.AppendFormat(" from tsupp where tsupp.pref = '{0}'  and temptablels.nzp_kvar = tsupp.nzp_kvar ", pref);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;
            }

            //создание таблицы temptablels2, на основе которой будут сгенерированы плат коды
            preptable = "temptablels2";
            ExecSQL(conn_db, "drop table " + preptable,false);
            ret = ExecSQL(conn_db, " create temp table "+preptable+
                                   " (id serial not null, num_ls integer, nzp_kvar integer, pref varchar(20), payer varchar(40), nzp_payer integer, "+
                                   " is_princip integer, area_code integer, pkod10 integer, pkod decimal(13,0))", true);
            if (!ret.result) return ret;

            //в таблицу temptablels2 добавляются все агенты
            sql.Remove(0, sql.Length);
            sql.Append(" insert into "+preptable+" (num_ls, nzp_kvar, pref, nzp_payer, is_princip)");
            sql.Append(" select distinct num_ls, nzp_kvar, pref, nzp_payer_agent, 0 from temptablels");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            //в таблицу temptablels2 добавляются только те принципалы, у которых в договоре установлен признак генерировать плат код
            sql.Remove(0, sql.Length);
            sql.Append(" insert into " + preptable + " (num_ls, nzp_kvar, pref, nzp_payer, is_princip) ");
            sql.Append(" select distinct num_ls, nzp_kvar, pref, nzp_payer_princip, 1 from temptablels ");
            ret = ExecSQL(conn_db, sql.ToString());
            if (!ret.result) return ret;

            //из таблицы temptablels2 удаляются те записи, которые соответствуют имеющимся
            //платежным кодам в таблице f_data.kvar_pkodes
            sql.Remove(0, sql.Length);
            sql.Append(" delete from " + preptable + " where  exists (");
            sql.AppendFormat(" select 1 from {0}_data{1}kvar_pkodes kp where ", Points.Pref, tableDelimiter);
            sql.AppendFormat(" {0}.nzp_kvar = kp.nzp_kvar and kp.nzp_payer = {0}.nzp_payer and kp.is_default = 1 and kp.is_princip = {0}.is_princip)", preptable);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            return ret;
        }

        /// <summary>
        /// Процедура получения платежного кода
        /// </summary>
        /// <param name="ls">nzp_area, nzp_geu,pref,nzp_kvar</param>
        /// <param name="transaction">текущая транзакция</param>
        /// <param name="areaCode">код УК</param>
        /// <param name="pkod10">пкод10</param>
        /// <param name="ret">результат</param>
        /// <returns>платежный код</returns>
        public string GeneratePkodOneLS(Ls ls, IDbTransaction transaction, out int areaCode, ref int pkod10, out Returns ret)
        {
            ret = Utils.InitReturns();           
          
            IDbConnection conn_db = transaction.Connection;

            areaCode = 0;
            decimal intPkod;
            Decimal.TryParse(ls.pkod, out intPkod);
            //проверка на существующий платежный код
            if (intPkod > 0)
            {
                pkod10 = ls.pkod10;
                return "";
            }
    
            ////проверка кодов
            //if (areaCode == 0 || pkod10 == 0)
            //{
            //    ret.result = false;
            //    ret.text = String.Format("area_code = {0}, pkod10 = {1}", areaCode, pkod10);
            //    MonitorLog.WriteLog("Ошибка получения кодов pkod10,area_code: " + ret.text,
            //        MonitorLog.typelog.Error, true);
            //    return "";
            //}


            //получение платежного кода                    
            string pkod = String.Empty;
            switch (Points.functionTypeGeneratePkod)
            {
                case FunctionsTypesGeneratePkod.standart:
                {
                    pkod = this.GeneratePkod_GetPkod13(Points.Region.GetHashCode(), ls, transaction, ref pkod10,
                        out areaCode, out ret);
                    break;
                }
                case FunctionsTypesGeneratePkod.samara:
                {
                    pkod = this.GeneratePkod_GetSamaraPkod13(ls,
                        transaction, ref pkod10,
                        out areaCode,
                        out ret);
                    break;
                }
            }

            if (!ret.result)
            {
                //проверка платежного кода
                if (String.IsNullOrEmpty(pkod) || pkod == "0")
                {
                    ret.result = false;
                    if (ret.tag >= 0)
                    {
                        ret.text = String.Format("Ошибка получения платежного кода: pkod={0}", pkod);
                        MonitorLog.WriteLog("Ошибка получения платежного кода: " + String.Format("pkod={2}, nzp_kvar={0},area_code={1}", ls.nzp_kvar, areaCode, pkod), MonitorLog.typelog.Error,
                            true);
                    }
                    return "";
                }
            }

            //проверка на уникальность
            var countPkodExist = ExecScalar(conn_db, transaction,
                String.Format("select count(*) from {0} where pkod = {1}",
                    DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", "kvar"), pkod), out ret, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка проверки платежного кода: " + ret.text,
                MonitorLog.typelog.Error, true);
                return "";
            }
            if (Convert.ToInt64(countPkodExist) > 0)
            {
                ret.result = false;
                ret.text = String.Format("Произошло дублирование платежного кода: {0} количество: {1}, ЛС = {2}",
                    pkod, countPkodExist, ls.num_ls);
                MonitorLog.WriteLog(
                    String.Format(
                        "Произошло дублирование платежного кода: {0} количество: {1}, nzp_kvar = {2}, nzp_geu={3},pkod10={4},area_code={5}",
                        pkod,
                        countPkodExist, ls.nzp_kvar, ls.nzp_geu, pkod10, areaCode),
                    MonitorLog.typelog.Error, true);

                return "";
            }
            
            return pkod;
        }

        /// <summary>
        /// Процедура генерации pkod_supp для одного ЛС
        /// </summary>
        /// <param name="ls">ЛС</param>
        /// <param name="transaction">транзакция</param>
        /// <param name="areaCode">код УК</param>
        /// <param name="pkod10">пкод10</param>
        /// <param name="ret">результат</param>
        /// <returns>pkod_supp</returns>
        public string GeneratePkodSuppOneLS(Ls ls, IDbTransaction transaction, out int areaCode, ref int pkod10,
            out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_db = transaction.Connection;

            areaCode = 0;

            //todo Сделать заполнение pkod_supp!!!
            decimal intPkodSupp;
            Decimal.TryParse(ls.pkod_supp, out intPkodSupp);
            if (intPkodSupp > 0)
            {
                return "";
            }          

            //получение платежного кода                    
            string pkodSupp = String.Empty;
            switch (Points.functionTypeGeneratePkod)
            {
                case FunctionsTypesGeneratePkod.samara:
                {
                    pkodSupp = this.GeneratePkod_GetSamaraSuppPkod(ls.nzp_area, ls.geuCode, ls.pref, ls.nzp_kvar,
                        transaction, ref pkod10,
                        out ret);
                        break;
                    }
                default:
                {
                    return "";
                }
            }

            //проверка платежного кода
            if (String.IsNullOrEmpty(pkodSupp) || pkodSupp == "0")
            {
                ret.result = false;
                ret.text = String.Format("pkodSupp={2}, nzp_kvar={0},area_code={1}", ls.nzp_kvar,
                    areaCode, pkodSupp);
                MonitorLog.WriteLog("Ошибка получения pkodSupp: " + ret.text,
                  MonitorLog.typelog.Error, true);
                return "";
            }

            ////проверка на уникальность
            //var countPkodExist = ExecScalar(conn_db, transaction,
            //    String.Format("select count(*) from {0} where pkod_supp = {1}",
            //        DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "supplier_codes"), pkodSupp), out ret, true);
            //if (!ret.result)
            //{
            //    MonitorLog.WriteLog("Ошибка проверки pkod_supp: " + ret.text,
            //    MonitorLog.typelog.Error, true);
            //    return "";
            //}
            //if (Convert.ToInt64(countPkodExist) > 0)
            //{
            //    MonitorLog.WriteLog("Произошло дублирование pkod_supp: " + pkodSupp + " количество:" + countPkodExist,
            //        MonitorLog.typelog.Error, true);
            //    return "";
            //}

            return pkodSupp;
        }
        
        /// <summary>
        /// Сгенерировать платежный код
        /// </summary>
        /// <param name="regionCode">код региона</param>
        /// <param name="areaCode">код организации</param>
        /// <param name="pkod10">код10</param>
        /// <returns>платежный код</returns>
        private string GeneratePkod_GetPkod13(int regionCode, Ls ls, IDbTransaction transaction, ref int pkod10, out int areaCode, out Returns ret)
        {
            ret = Utils.InitReturns();
            string errMsg = "";
            string errMsgLog = "";

            //pkod10 приходит готовый
            if (pkod10 > 0)
            {
                areaCode = GetAreaCodes(transaction.Connection, transaction,ls, out ret);
                if (!ret.result)
                {
                    errMsg = ret.tag < 0 ? "Невозможно получить код управляющей компании: " + ret.text : "Ошибка получения кодa управляющей компании: " + ret.text;
                    errMsgLog = "Ошибка получения кодa area_code: " + ret.text;
                }
            }
            else
            {
                ret = this.GetPkod10(transaction.Connection, transaction, ls, out pkod10, out areaCode);
                if (!ret.result)
                {
                    errMsg = ret.tag < 0 ? "Невозможно получить часть платежного кода : " + ret.text : "Ошибка получения части платежного кода : " + ret.text;
                    errMsgLog = "Ошибка получения кодов pkod10,area_code: " + ret.text;
                }                                                                            
            }           
            if (!ret.result)
            {
                ret.text = errMsg;
                MonitorLog.WriteLog(errMsgLog, MonitorLog.typelog.Error, true);
                return "";
            }

            if (regionCode > 99 || areaCode > 99999 || pkod10 > 99999 || regionCode <= 0 || areaCode <= 0 || pkod10 <= 0)
            {
                ret.result = false;
                ret.text =
                    String.Format(
                        "Ошибка генерации платежного кода. Несовпадение количества символов в составных частях. Код региона: {0}, Код УК: {1}, Пкод10: {2}",
                        regionCode, areaCode, pkod10);
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации платежного кода: regionCode={0}, areaCode={1}, pkod10={2} ",
                        regionCode, areaCode, pkod10), MonitorLog.typelog.Error, true);
                return "";
            }

            string pkod = regionCode.ToString("00") + areaCode.ToString("00000") + pkod10.ToString("00000");
            //считаем цифры в нечетных позициях и умножаем их на 3 после чего складываем с четными позициями 
            int sum = 0;
            for(int i = 0; i< pkod.Length; i++)            
            {                
                sum += (i + 1) % 2 == 0 ? Convert.ToInt32(pkod[i].ToString()) : Convert.ToInt32(pkod[i].ToString()) * 3;
            }
            //определяем дополнение до 10
            int dop10 = 10 - sum%10;

            return pkod + (dop10 == 10 ? 0 : dop10);
        }

        /// <summary>
        /// Сгенерировать платежный код по новому алгоритму
        /// </summary>
        /// <param name="regionCode">код региона</param>
        /// <param name="areaCode">код организации</param>
        /// <param name="pkod10">код10</param>
        /// <returns>платежный код</returns>
        private string NewGeneratePkod_GetPkod13(int regionCode, Ls ls, IDbTransaction transaction, ref int pkod10, out int areaCode, out Returns ret)
        {
            ret = Utils.InitReturns();
            string errMsg = "";
            string errMsgLog = "";

            //pkod10 приходит готовый
            if (pkod10 > 0)
            {
                areaCode = GetAreaCodes(transaction.Connection, transaction, ls, out ret);
                if (!ret.result)
                {
                    errMsg = ret.tag < 0 ? "Невозможно получить код управляющей компании: " + ret.text : "Ошибка получения кодa управляющей компании: " + ret.text;
                    errMsgLog = "Ошибка получения кодa area_code: " + ret.text;
                }
            }
            else
            {
                ret = this.GetPkod10(transaction.Connection, transaction, ls, out pkod10, out areaCode);
                if (!ret.result)
                {
                    errMsg = ret.tag < 0 ? "Невозможно получить часть платежного кода : " + ret.text : "Ошибка получения части платежного кода : " + ret.text;
                    errMsgLog = "Ошибка получения кодов pkod10,area_code: " + ret.text;
                }
            }
            if (!ret.result)
            {
                ret.text = errMsg;
                MonitorLog.WriteLog(errMsgLog, MonitorLog.typelog.Error, true);
                return "";
            }

            if (regionCode > 99 || areaCode > 99999 || pkod10 > 99999)
            {
                ret.result = false;
                ret.text =
                    String.Format(
                        "Ошибка генерации платежного кода. Несовпадение количества символов в составных частях. Код региона: {0}, Код УК: {1}, Пкод10: {2}",
                        regionCode, areaCode, pkod10);
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации платежного кода: regionCode={0}, areaCode={1}, pkod10={2} ",
                        regionCode, areaCode, pkod10), MonitorLog.typelog.Error, true);
                return "";
            }

            string pkod = regionCode.ToString("00") + areaCode.ToString("00000") + pkod10.ToString("00000");
            //считаем цифры в нечетных позициях и умножаем их на 3 после чего складываем с четными позициями 
            int sum = 0;
            for (int i = 0; i < pkod.Length; i++)
            {
                sum += (i + 1) % 2 == 0 ? Convert.ToInt32(pkod[i].ToString()) : Convert.ToInt32(pkod[i].ToString()) * 3;
            }
            //определяем дополнение до 10
            int dop10 = 10 - sum % 10;

            return pkod + (dop10 == 10 ? 0 : dop10);
        }

        /// <summary>
        /// Сгенерировать платежный код (Самара)
        /// </summary>
        /// <param name="nzp_area">УК</param>
        /// <param name="nzp_geu">ЖЭУ</param>
        /// <param name="pkod10">пкод10</param>
        /// <param name="pref">префикс</param>
        /// <param name="litera">литера</param>
        /// <param name="transaction?">текущая транзакция</param>
        /// <param name="ret">результат</param>
        /// <returns>платежный код для Самары</returns>
        private string GeneratePkod_GetSamaraPkod13(Ls ls,
            IDbTransaction transaction, ref int pkod10 , out int areaCode ,out Returns ret)
        {

            ret = Utils.InitReturns();
            areaCode = 0;

            //Проверка на отделение ЛС
            if (ls.nzp_geu < 1)
            {
                ret.text = "Невозможно получить новый платежный код, т.к. не определено отделение лицевого счета";
                ret.result = false;
                ret.tag = -1;                
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return "";
            }
           
            //pkod10 либо создать, либо приходит готовый
            if (pkod10 <= 0)
            {
                //Получение ключей(pkod10)
                if (Points.isUseSeries) // работа с таблицей series
                {
                    var series = new Series(new int[] { 10 });//Points.isUseSeries ? new Series(new int[] { 1, 2, 10 }) : new Series(new int[] { 1, 2 });
                    var db = new DbSpravKernel();
                    db.GetSeries(transaction.Connection, transaction, ls.pref, series, out ret);
                    db.Close();
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(
                            "Ошибка получения ключей: " + ret.text, MonitorLog.typelog.Error,
                            true);
                        return "";
                    }
                    _Series val = series.GetSeries(10);
                    pkod10 = val.cur_val;
                }
                else
                {
                    int acode = 0;
                    ret = GetPkod10(transaction.Connection, transaction, ls, out pkod10, out acode);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(
                            "Ошибка получения pkod10: " + ret.text, MonitorLog.typelog.Error,
                            true);
                        return "";
                    }
                }
            }
            
            if (pkod10 > 99999 || pkod10 < 1)
            {
                ret.result = false;
                ret.text = String.Format("Ошибка генерации платежного кода: Пкод10 = {0}", pkod10);
                MonitorLog.WriteLog(String.Format("Ошибка генерации платежного кода:  pkod10={0} ", pkod10), MonitorLog.typelog.Error, true);
                return "";
            }

            //код счета
            _KodERC ercKode = GetKodErc(transaction.Connection, transaction, ls.pref, ls.nzp_area, ls.nzp_geu, out ret);
            if (!ret.result)
            {
                ret.text = String.Format("Ошибка генерации платежного кода:  Код ЕРЦ={0} ", ercKode.kod_erc);
                MonitorLog.WriteLog(String.Format("Ошибка генерации платежного кода:  kod_erc={0} ", ercKode.kod_erc), MonitorLog.typelog.Error, true);
                return "";
            }
            if (ercKode.kod_erc > 999)
            {
                ret.result = false;
                ret.text = String.Format("Ошибка генерации платежного кода. КодЕРЦ превышает 3 знака :{0} ", ercKode.kod_erc);
                MonitorLog.WriteLog(String.Format("Ошибка генерации платежного кода:  kod_erc={0} ", ercKode.kod_erc), MonitorLog.typelog.Error, true);
                return "";
            }

            //литера       
            int litera = this.GetLitera(new Ls() { pref = ls.pref, nzp_kvar = ls.nzp_kvar }, out ret, transaction.Connection,
                transaction);
            if (!ret.result)
            {
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации платежного кода:  litera={0} ", litera), MonitorLog.typelog.Error,
                    true);
                return "";
            }

            //area_code для Самары составляемый
            string strAreaCode = ercKode.kod_erc.ToString("000") + (ls.nzp_geu%100).ToString("00");
            Int32.TryParse(strAreaCode, out areaCode);            

            string pkod = ercKode.kod_erc.ToString("000") + (ls.nzp_geu % 100).ToString("00") +
                pkod10.ToString("00000") + (litera > 9 ? 0 : litera);
            if (pkod.Length != 11)
            {
                ret.result = false;
                ret.text =
                    String.Format(
                        "Ошибка генерации платежного кода. Необходимая часть кода не соответсвует 11 знакам. Код={0} ", pkod);
                ret.tag = -1;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return "";
            }
            return pkod + Utils.GetKontrSamara(pkod);
        }

        /// <summary>
        /// Генерация pkod_supp для Самары
        /// </summary>
        /// <param name="nzp_area">УК</param>
        /// <param name="geuCode">Код ЖЭУ</param>
        /// <param name="pref">Префикс</param>
        /// <param name="nzp_kvar">Ключ квартиры</param>
        /// <param name="transaction">транзакция</param>
        /// <param name="pkod10">пкод10</param>
        /// <param name="ret">результат</param>
        /// <returns>pkod_supp</returns>
        private string GeneratePkod_GetSamaraSuppPkod(int nzp_area, int geuCode, string pref, int nzp_kvar,
            IDbTransaction transaction, ref int pkod10, out Returns ret)
        {
            ret = Utils.InitReturns();

            //pkod10 либо создать, либо приходит готовый
            if (pkod10 <= 0)
            {
                //Получение ключей(pkod10)
                var series = new Series(new int[] { 10 });//Points.isUseSeries ? new Series(new int[] { 1, 2, 10 }) : new Series(new int[] { 1, 2 });
                var db = new DbSpravKernel();
                db.GetSeries(transaction.Connection, transaction, pref, series, out ret);
                db.Close();
                if (!ret.result)
                {
                    MonitorLog.WriteLog(
                        "Ошибка получения ключей: " + ret.text, MonitorLog.typelog.Error,
                        true);
                    return "";
                }
                _Series val = series.GetSeries(10);
                pkod10 = val.cur_val;
            }

            if (pkod10 > 99999 || pkod10 < 1)
            {
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации кода pkod_supp:  pkod10={0} ", pkod10), MonitorLog.typelog.Error,
                    true);
                return "";
            }
    
            //код ФКР
            int fkrCode = 500;
            if (fkrCode > 999)
            {
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации pkod_supp:  fkrCode={0} ", fkrCode), MonitorLog.typelog.Error,
                    true);
                return "";
            }

            //литера       
            int litera = this.GetLitera(new Ls() { pref = pref, nzp_kvar = nzp_kvar }, out ret, transaction.Connection,
                transaction);
            if (!ret.result)
            {
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации pkod_supp:  litera={0} ", litera), MonitorLog.typelog.Error,
                    true);
                return "";
            }

            string pkod = fkrCode.ToString("000") + geuCode.ToString("00") +
                          pkod10.ToString("00000") + (litera > 9 ? 0 : litera);
            if (pkod.Length != 11)
            {
                MonitorLog.WriteLog(
                   String.Format("Ошибка генерации pkod_supp(!=11):  pkod={0} ", pkod), MonitorLog.typelog.Error,
                   true);
            }
            return pkod.Length == 11 ? pkod + Utils.GetKontrSamara(pkod) : "";
        }

        #endregion

    /*    #region Генерация платежного кода по новому алгоритму
        /// <summary>
        /// Генерация платежного кода
        /// </summary>
        /// <returns>результат</returns>
        public Returns GeneratePkodOnLsList(Ls finder)
        {
            IDbConnection conn_db = null;
            IDbTransaction transaction = null;
            Returns ret = Utils.InitReturns();
            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения с БД.", MonitorLog.typelog.Error, true);
                    return ret;
                }

                #region Генерация
                //Таблица ЛС центральной БД
                string kvarTable = DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", "kvar");

                //список ЛС с необходимымми данными
                StringBuilder sql = new StringBuilder();
                sql.Append(" select nzp_kvar, nzp_area, nzp_geu, pref, pkod, pkod10 ");
                sql.AppendFormat(" from {0}  ", kvarTable);
                sql.AppendFormat(" where nzp_kvar in (select nzp_kvar from {0}) ", tXXSplsTable);
                List<Ls> kvarToUpdate = DBManager.ExecSQLToTable(conn_db, sql.ToString())
                    .AsEnumerable()
                    .Select(
                        x =>
                            new Ls()
                            {
                                nzp_kvar = x["nzp_kvar"] != DBNull.Value ? Convert.ToInt32(x["nzp_kvar"]) : 0,
                                nzp_geu = x["nzp_geu"] != DBNull.Value ? Convert.ToInt32(x["nzp_geu"]) : 0,
                                nzp_area = x["nzp_area"] != DBNull.Value ? Convert.ToInt32(x["nzp_area"]) : 0,
                                pkod = x["pkod"] != DBNull.Value ? Convert.ToString(x["pkod"]).Trim() : "",
                                pref = x["pref"] != DBNull.Value ? x["pref"].ToString().Trim() : "",
                                pkod10 = x["pkod10"] != DBNull.Value ? Convert.ToInt32(x["pkod10"]) : 0
                            }).ToList();

                //Добавляем код ЖЭУ (n из s_points) - используется только в Самаре
                kvarToUpdate.ForEach(k => k.geuCode = Points.PointList.First(p => p.pref == k.pref).n);


                //Обновление платежного кода
                foreach (Ls ls in kvarToUpdate)
                {
                    //получение платежного кода, area_code, pkod10
                    int areaCode, pkod10 = 0;
                    ls.nzp_user = finder.nzp_user;
                    transaction = conn_db.BeginTransaction();
                    string pkod = this.GeneratePkodOneLS(ls, transaction, out areaCode, ref pkod10, out ret);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    if (pkod != "")
                    {
                        //сохранение в центральную базу
                        sql = new StringBuilder();
                        sql.AppendFormat(" UPDATE {0} set area_code = {1}, pkod10 = {2} ", kvarTable, areaCode, pkod10);
                        sql.AppendFormat("  ,pkod = {0} where nzp_kvar = {1} ", pkod, ls.nzp_kvar);
                        ret = DBManager.ExecSQL(conn_db, transaction, sql.ToString(), true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка сохранения платежного кода в центральный банк: " + ret.text,
                                MonitorLog.typelog.Error, true);
                            return ret;
                        }

                        //обновление в локальном банке
                        sql = new StringBuilder();
                        sql.AppendFormat(" UPDATE {0} set pkod= {1}, pkod10 = {2} where nzp_kvar = {3} ",
                            DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "kvar"),
                            pkod, //1
                            pkod10, //2
                            ls.nzp_kvar); //3
                        ret = DBManager.ExecSQL(conn_db, transaction, sql.ToString(), true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка сохранения платежного кода в локальный банк: " + ret.text,
                                MonitorLog.typelog.Error, true);
                            return ret;
                        }
                    }

                    transaction.Commit();

                    #region Определение кодов поставщиков для Самары. Закомментировано, отдельный режим
                    // Определение кодов поставщиков для Самары
                    //int areaCodeSupp, pkod10Supp = pkod10;
                    //string pkodSupp = this.GeneratePkodSuppOneLS(ls, transaction, out areaCodeSupp, ref pkod10Supp,
                    //    out ret);
                    //if (!ret.result)
                    //{
                    //    return ret;
                    //}
                    //if (pkodSupp != "")
                    //{                                                
                    //    //todo возможно переделать - Сделать проверку на существование
                    //    //обновление в локальном банке supplier_codes
                    //    ret = DBManager.ExecSQL(conn_db,transaction,
                    //        String.Format("delete from {0} where nzp_kvar={1}",
                    //            DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "supplier_codes"), ls.nzp_kvar),
                    //        true);
                    //    if (!ret.result)
                    //    {
                    //        MonitorLog.WriteLog("Ошибка сохранения удаления данных из pkodSupp в локальный банк: " + ret.text,
                    //            MonitorLog.typelog.Error, true);
                    //        return ret;
                    //    }


                    //    sql = new StringBuilder();
                    //    sql.AppendFormat(" insert into {0} (nzp_kvar,nzp_supp,kod_geu,pkod10,pkod_supp) values({1}, {2}, {3}, {4}, {5}) ",
                    //        DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "supplier_codes"),      //0
                    //        ls.nzp_kvar,                                                            //1
                    //        "null",                                                                 //2
                    //        ls.geuCode,                                                             //3
                    //        pkod10Supp,                                                             //4
                    //        pkodSupp);                                                              //5
                    //    ret = DBManager.ExecSQL(conn_db, transaction, sql.ToString(), true);
                    //    if (!ret.result)
                    //    {
                    //        MonitorLog.WriteLog("Ошибка сохранения pkodSupp в локальный банк: " + ret.text,
                    //            MonitorLog.typelog.Error, true);
                    //        return ret;
                    //    }

                    //    transaction.Commit();
                    //}
                    //else
                    //{
                    //    MonitorLog.WriteLog("pkodSupp =пусто!" + ls.pkod_supp,
                    //           MonitorLog.typelog.Warn, true);
                    //    transaction.Commit();                        
                    //}
                    #endregion
                }
                #endregion

                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException(" Ошибка генерации платежных кодов. ", ex);
                ret.result = false;
                return ret;
            }
            finally
            {
                if (transaction != null)
                {
                    if (!ret.result) transaction.Rollback();
                    transaction.Dispose();
                }

                if (conn_db != null)
                {
                    conn_db.Close();
                }
            }
        }
        */
        /*
        public Returns NewGeneratePkod(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            string tselkvar = "t";
            if (finder.dopFind != null && finder.dopFind.Count > 0 && finder.dopFind[0] != "")
            {
                tselkvar += finder.dopFind[0];
            }
            if (!TempTableInWebCashe(tselkvar))
            {
                ret = new Returns(false, "Не выбранных ЛС");
                MonitorLog.WriteLog("Ошибка генерации платежных кодов: " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }



            return ret;
        }*/

        /// <summary>
        /// Процедура получения платежного кода
        /// </summary>
        /// <param name="ls">nzp_area, nzp_geu,pref,nzp_kvar</param>
        /// <param name="transaction">текущая транзакция</param>
        /// <param name="areaCode">код УК</param>
        /// <param name="pkod10">пкод10</param>
        /// <param name="ret">результат</param>
        /// <returns>платежный код</returns>
    /*    public string GeneratePkodOneLS(Ls ls, IDbTransaction transaction, out int areaCode, ref int pkod10, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_db = transaction.Connection;

            areaCode = 0;
            decimal intPkod;
            Decimal.TryParse(ls.pkod, out intPkod);
            //проверка на существующий платежный код
            if (intPkod > 0)
            {
                pkod10 = ls.pkod10;
                return "";
            }

            ////проверка кодов
            //if (areaCode == 0 || pkod10 == 0)
            //{
            //    ret.result = false;
            //    ret.text = String.Format("area_code = {0}, pkod10 = {1}", areaCode, pkod10);
            //    MonitorLog.WriteLog("Ошибка получения кодов pkod10,area_code: " + ret.text,
            //        MonitorLog.typelog.Error, true);
            //    return "";
            //}


            //получение платежного кода                    
            string pkod = String.Empty;
            switch (Points.functionTypeGeneratePkod)
            {
                case FunctionsTypesGeneratePkod.standart:
                    {
                        pkod = this.GeneratePkod_GetPkod13(Points.Region.GetHashCode(), ls, transaction, ref pkod10,
                            out areaCode, out ret);
                        break;
                    }
                case FunctionsTypesGeneratePkod.samara:
                    {
                        pkod = this.GeneratePkod_GetSamaraPkod13(ls,
                            transaction, ref pkod10,
                            out areaCode,
                            out ret);
                        break;
                    }
            }

            if (!ret.result)
            {
                //проверка платежного кода
                if (String.IsNullOrEmpty(pkod) || pkod == "0")
                {
                    ret.result = false;
                    if (ret.tag >= 0)
                    {
                        ret.text = String.Format("Ошибка получения платежного кода: pkod={0}", pkod);
                        MonitorLog.WriteLog("Ошибка получения платежного кода: " + String.Format("pkod={2}, nzp_kvar={0},area_code={1}", ls.nzp_kvar, areaCode, pkod), MonitorLog.typelog.Error,
                            true);
                    }
                    return "";
                }
            }

            //проверка на уникальность
            var countPkodExist = ExecScalar(conn_db, transaction,
                String.Format("select count(*) from {0} where pkod = {1}",
                    DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", "kvar"), pkod), out ret, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка проверки платежного кода: " + ret.text,
                MonitorLog.typelog.Error, true);
                return "";
            }
            if (Convert.ToInt64(countPkodExist) > 0)
            {
                ret.result = false;
                ret.text = String.Format("Произошло дублирование платежного кода: {0} количество: {1}, ЛС = {2}",
                    pkod, countPkodExist, ls.num_ls);
                MonitorLog.WriteLog(
                    String.Format(
                        "Произошло дублирование платежного кода: {0} количество: {1}, nzp_kvar = {2}, nzp_geu={3},pkod10={4},area_code={5}",
                        pkod,
                        countPkodExist, ls.nzp_kvar, ls.nzp_geu, pkod10, areaCode),
                    MonitorLog.typelog.Error, true);

                return "";
            }

            return pkod;
        }
*/
        /// <summary>
        /// Процедура генерации pkod_supp для одного ЛС
        /// </summary>
        /// <param name="ls">ЛС</param>
        /// <param name="transaction">транзакция</param>
        /// <param name="areaCode">код УК</param>
        /// <param name="pkod10">пкод10</param>
        /// <param name="ret">результат</param>
        /// <returns>pkod_supp</returns>
     /*   public string GeneratePkodSuppOneLS(Ls ls, IDbTransaction transaction, out int areaCode, ref int pkod10,
            out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_db = transaction.Connection;

            areaCode = 0;

            //todo Сделать заполнение pkod_supp!!!
            decimal intPkodSupp;
            Decimal.TryParse(ls.pkod_supp, out intPkodSupp);
            if (intPkodSupp > 0)
            {
                return "";
            }

            //получение платежного кода                    
            string pkodSupp = String.Empty;
            switch (Points.functionTypeGeneratePkod)
            {
                case FunctionsTypesGeneratePkod.samara:
                    {
                        pkodSupp = this.GeneratePkod_GetSamaraSuppPkod(ls.nzp_area, ls.geuCode, ls.pref, ls.nzp_kvar,
                            transaction, ref pkod10,
                            out ret);
                        break;
                    }
                default:
                    {
                        return "";
                    }
            }

            //проверка платежного кода
            if (String.IsNullOrEmpty(pkodSupp) || pkodSupp == "0")
            {
                ret.result = false;
                ret.text = String.Format("pkodSupp={2}, nzp_kvar={0},area_code={1}", ls.nzp_kvar,
                    areaCode, pkodSupp);
                MonitorLog.WriteLog("Ошибка получения pkodSupp: " + ret.text,
                  MonitorLog.typelog.Error, true);
                return "";
            }

            ////проверка на уникальность
            //var countPkodExist = ExecScalar(conn_db, transaction,
            //    String.Format("select count(*) from {0} where pkod_supp = {1}",
            //        DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "supplier_codes"), pkodSupp), out ret, true);
            //if (!ret.result)
            //{
            //    MonitorLog.WriteLog("Ошибка проверки pkod_supp: " + ret.text,
            //    MonitorLog.typelog.Error, true);
            //    return "";
            //}
            //if (Convert.ToInt64(countPkodExist) > 0)
            //{
            //    MonitorLog.WriteLog("Произошло дублирование pkod_supp: " + pkodSupp + " количество:" + countPkodExist,
            //        MonitorLog.typelog.Error, true);
            //    return "";
            //}

            return pkodSupp;
        }
        */
        /// <summary>
        /// Сгенерировать платежный код
        /// </summary>
        /// <param name="regionCode">код региона</param>
        /// <param name="areaCode">код организации</param>
        /// <param name="pkod10">код10</param>
        /// <returns>платежный код</returns>
      /*  private string NewGeneratePkod_GetPkod13(int regionCode, Ls ls, IDbTransaction transaction, ref int pkod10, out int areaCode, out Returns ret)
        {
            ret = Utils.InitReturns();
            string errMsg = "";
            string errMsgLog = "";

            //pkod10 приходит готовый
            if (pkod10 > 0)
            {
                areaCode = GetAreaCodes(transaction.Connection, transaction, ls, out ret);
                if (!ret.result)
                {
                    errMsg = ret.tag < 0 ? "Невозможно получить код управляющей компании: " + ret.text : "Ошибка получения кодa управляющей компании: " + ret.text;
                    errMsgLog = "Ошибка получения кодa area_code: " + ret.text;
                }
            }
            else
            {
                ret = this.GetPkod10(transaction.Connection, transaction, ls, out pkod10, out areaCode);
                if (!ret.result)
                {
                    errMsg = ret.tag < 0 ? "Невозможно получить часть платежного кода : " + ret.text : "Ошибка получения части платежного кода : " + ret.text;
                    errMsgLog = "Ошибка получения кодов pkod10,area_code: " + ret.text;
                }
            }
            if (!ret.result)
            {
                ret.text = errMsg;
                MonitorLog.WriteLog(errMsgLog, MonitorLog.typelog.Error, true);
                return "";
            }

            if (regionCode > 99 || areaCode > 99999 || pkod10 > 99999)
            {
                ret.result = false;
                ret.text =
                    String.Format(
                        "Ошибка генерации платежного кода. Несовпадение количества символов в составных частях. Код региона: {0}, Код УК: {1}, Пкод10: {2}",
                        regionCode, areaCode, pkod10);
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации платежного кода: regionCode={0}, areaCode={1}, pkod10={2} ",
                        regionCode, areaCode, pkod10), MonitorLog.typelog.Error, true);
                return "";
            }

            string pkod = regionCode.ToString("00") + areaCode.ToString("00000") + pkod10.ToString("00000");
            //считаем цифры в нечетных позициях и умножаем их на 3 после чего складываем с четными позициями 
            int sum = 0;
            for (int i = 0; i < pkod.Length; i++)
            {
                sum += (i + 1) % 2 == 0 ? Convert.ToInt32(pkod[i].ToString()) : Convert.ToInt32(pkod[i].ToString()) * 3;
            }
            //определяем дополнение до 10
            int dop10 = 10 - sum % 10;

            return pkod + (dop10 == 10 ? 0 : dop10);
        }*/

     

     //   #endregion

        public int GetAreaCodes(IDbConnection conn_db, IDbTransaction transaction, Ls finder, out Returns ret)
        {
            int code = Constants._ZERO_;
            ret = Utils.InitReturns();

            if (Points.IsSmr)
            {
                string scode = "";
                ret = GetAreaCodeForSmr(conn_db, transaction, finder, out scode);
                if (ret.result && scode != "") code = Convert.ToInt32(scode);
            }
            else
            {
                string sql = "select code from " + Points.Pref +
#if PG
                        "_data.area_codes" +
#else
 "_data:area_codes" +
#endif
 " where is_active = 1 and nzp_area = " + finder.nzp_area;
                IDataReader reader = null;
                if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
                {
                    ret = new Returns(false, "Ошибка получения данных из area_codes");
                    return code;
                }

                if (reader.Read())
                {
                    if (reader["code"] != DBNull.Value) code = Convert.ToInt32(reader["code"]);
                }
                reader.Close();

                if (code == Constants._ZERO_)
                {
                    ret = new Returns(false, "Не установлен код управляющей организации " + finder.area, -1);
                }
            }
            return code;
        }

        public int NewGetAreaCodes(IDbConnection conn_db, IDbTransaction transaction, AreaCodes finder, out Returns ret)
        {
            int code = Constants._ZERO_;
            ret = Utils.InitReturns();
           
            string sql = " select code from " + Points.Pref + "_data" + tableDelimiter + "area_codes" +
                         " where is_active = 1 and nzp_payer = " + finder.nzp_payer;
            IDataReader reader = null;
            if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
            {
                ret = new Returns(false, "Ошибка получения данных из area_codes");
                return code;
            }

            if (reader.Read())
            {
                if (reader["code"] != DBNull.Value) code = Convert.ToInt32(reader["code"]);
            }
            reader.Close();

            if (code == Constants._ZERO_)
            {
                ret = new Returns(false, "Не установлен платежный код контрагента " + finder.payer, -1);
            }
            
            return code;
        }
        
        public Returns GetPkod10(IDbConnection conn_db, IDbTransaction transaction, Ls finder, out int pkod10, out int area_code)
        {            
            pkod10 = Constants._ZERO_;
            Returns ret = Utils.InitReturns();
            area_code = GetAreaCodes(conn_db, transaction, finder, out ret);
            if (!ret.result) return ret;

            ret = GetNextPkod10(conn_db, transaction, area_code, out pkod10);
            if (!ret.result) return ret;
            if (Points.IsSmr)
            {
                if (pkod10 > 99999)
                {
                    ret = new Returns(false, "Значение лицевого счета превысило 99999", -1);                  
                }
            }
            else
            {

                IDataReader reader = null;
                string table_area = Points.Pref + "_data" + tableDelimiter + "s_area";
                string sql = "select area from " + table_area +
                              " where nzp_area = " + finder.nzp_area;
                if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
                {
                    ret = new Returns(false, "Ошибка получения данных из area_codes");
                    return ret;
                }
                if (reader.Read()) if (reader["area"] != DBNull.Value) finder.area = Convert.ToString(reader["area"]);
                reader.Close();

                if (pkod10 > 99999)
                {
                    string table_area_code = Points.Pref + "_data" + tableDelimiter + "area_codes";

                    sql = "select code, is_active from " + table_area_code +
                                 " where nzp_area = " + finder.nzp_area + " and code > " + area_code + " and is_active = 0 order by code";

                    if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
                    {
                        ret = new Returns(false, "Ошибка получения данных из area_codes");
                        return ret;
                    }

                    int code = 0;
                    pkod10 = area_code = Constants._ZERO_;
                    while (reader.Read())
                    {
                        if (reader["code"] != DBNull.Value) code = Convert.ToInt32(reader["code"]);

                        sql = "update " + table_area_code + " set is_active=0 where nzp_area = " + finder.nzp_area;
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result) return ret;

                        sql = "update " + table_area_code + " set is_active=1 where nzp_area = " + finder.nzp_area + " and code=" + code;
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result) return ret;

                        area_code = GetAreaCodes(conn_db, transaction, finder, out ret);
                        if (!ret.result) return ret;

                        ret = GetNextPkod10(conn_db, transaction, area_code, out pkod10);
                        if (!ret.result) return ret;

                        if (pkod10 <= 99999) break;

                        pkod10 = area_code = Constants._ZERO_;
                    }
                    reader.Close();
                    if (pkod10 == Constants._ZERO_)
                    {
                        ret = new Returns(false, "Дополнительных кодов для управляющей организации '" + finder.area + "' нет", -1);
                        return ret;
                    }
                    return ret;
                }
                else
                {
                    if (pkod10 <= 0) ret = new Returns(false, "Дополнительных кодов для управляющей организации '" + finder.area + "' нет", -1);
                    return ret;
                }
            }
            return ret;
        }

        public Returns NewGetPkod10(IDbConnection conn_db, IDbTransaction transaction, AreaCodes finder, out int pkod10)
        {
            pkod10 = Constants._ZERO_;
            Returns ret = Utils.InitReturns();
            
            ret = NewGetNextPkod10(conn_db, transaction, finder.code, out pkod10);
            if (!ret.result) return ret;
          
            IDataReader reader = null;
            string sql;
           
            if (pkod10 > 99999)
            {
                string table_area_code = Points.Pref + "_data" + tableDelimiter + "area_codes";

                sql = "select code, is_active from " + table_area_code + " where nzp_payer = " + finder.nzp_payer + " and code > " + finder.code + " and is_active = 0 order by code";
                if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
                {
                    ret = new Returns(false, "Ошибка получения данных из area_codes");
                    return ret;
                }

                int code = 0;
                pkod10 = Constants._ZERO_;
                int area_code = 0;
                while (reader.Read())
                {
                    if (reader["code"] != DBNull.Value) code = Convert.ToInt32(reader["code"]);

                    sql = "update " + table_area_code + " set is_active=0 where nzp_payer = " + finder.nzp_payer;
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) return ret;

                    sql = "update " + table_area_code + " set is_active=1 where nzp_payer = " + finder.nzp_payer + " and code=" + code;
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) return ret;

                    area_code = NewGetAreaCodes(conn_db, transaction, finder, out ret);
                    if (!ret.result) return ret;

                    ret = NewGetNextPkod10(conn_db, transaction, area_code, out pkod10);
                    if (!ret.result) return ret;

                    if (pkod10 <= 99999) break;

                    pkod10 = area_code = Constants._ZERO_;
                }
                reader.Close();
                if (pkod10 == Constants._ZERO_)
                {
                    ret = new Returns(false, "Дополнительных платежных кодов для контрагента '" + finder.payer + "' нет", -1);
                    return ret;
                }              
            }
            else if (pkod10 <= 0) ret = new Returns(false, "Дополнительных платежных кодов для контрагента '" + finder.payer + "' нет", -1);
                
            return ret;
        }

        private Returns GetNextPkod10(IDbConnection conn_db, IDbTransaction transaction, int area_code, out int pkod10)
        {
            Returns ret = Utils.InitReturns();
            pkod10 = Constants._ZERO_;

            string seqName = "kvar_pkod10_" + area_code + "_seq";
            ////проверка на существование sequence
            //if (!DBManager.TableInBase(conn_db, transaction, Points.Pref + "_data", seqName))
            //{
            //    if (!ExecSQL(conn_db, transaction, "create sequence " + DBManager.GetFullBaseName(conn_db, Points.Pref + "_data", seqName) + ";", true).result)
            //    {
            //        ret = new Returns(false, "Ошибка создания последовательности: " + seqName);
            //        return ret;
            //    }
            //}
            
           
            string sql =
#if PG
                " SELECT nextval('" + Points.Pref +  "_data.kvar_pkod10_" + area_code+"_seq') as pkod10";
#else
                " SELECT " + Points.Pref + "_data:" + seqName + ".nextval as pkod10 from " +
                        Points.Pref + "_data:dual";
#endif
            IDataReader reader = null;
            ret = ExecRead(conn_db, transaction, out reader, sql, false);
           
            if (!ret.result)
            {
                if (Points.IsSmr)
                {
                    ret = GenSeqOnePkod10(area_code);
                    if (!ret.result) return ret;
                    pkod10 = ret.tag;
                    return new Returns(true);
                }

                MonitorLog.WriteLog(" Ошибка получения pkod10 "+ret.text, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка получения pkod10");
                return ret;
            }

            if (reader.Read())
            {
                if (reader["pkod10"] != DBNull.Value) pkod10 = Convert.ToInt32(reader["pkod10"]);
            }
            reader.Close();
           
            return ret;
        }

        private Returns NewGetNextPkod10(IDbConnection conn_db, IDbTransaction transaction, int area_code, out int pkod10)
        {
            Returns ret = Utils.InitReturns();
            pkod10 = Constants._ZERO_;

            string seqName = "kvar_pkod10_" + area_code + "_seq";
            
            string sql =
#if PG
                        " SELECT nextval('" + Points.Pref + "_data.kvar_pkod10_" + area_code + "_seq') as pkod10";
#else
                        " SELECT " + Points.Pref + "_data:" + seqName + ".nextval as pkod10 from " + Points.Pref + "_data:dual";
#endif
            IDataReader reader = null;
            ret = ExecRead(conn_db, transaction, out reader, sql, false);

            if (!ret.result)
            {                
                ret = new Returns(false, "Ошибка получения pkod10");
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return ret;                
            }

            if (reader.Read())
            {
                if (reader["pkod10"] != DBNull.Value) pkod10 = Convert.ToInt32(reader["pkod10"]);
            }
            reader.Close();

            return ret;
        }

        //----------------------------------------------------------------------
        public _KodERC GetKodErc(IDbConnection conn_db, IDbTransaction transaction, string pref, int nzp_area, long nzp_geu, out Returns ret) //вытащить коды ЕРЦ
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            _KodERC kod = new _KodERC();
            kod.bdf = "";
            kod.erc = "";
            kod.kod_erc = Constants._ZERO_;

            MyDataReader reader;

            //префикс УК по-умолчанию
#if PG
            string sql = " Select kod as kod_erc, erc From " + Points.Pref + "_kernel.s_erck ";
#else
            string sql = " Select kod as kod_erc, erc From " + Points.Pref + "_kernel:s_erck ";
#endif
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result)
            {
                if (reader != null) reader.Close();
                //reader.Dispose();
                reader = null;
                return kod;
            }
            if (reader.Read())
            {
                if (reader["kod_erc"] != DBNull.Value)
                    kod.kod_erc = (int)reader["kod_erc"];
                if (reader["erc"] != DBNull.Value)
                    kod.erc = (string)reader["erc"];
            }
            if (reader != null) reader.Close();
            //reader.Dispose();
            reader = null;

            try
            {
#if PG
                sql = " Select bdf From " + pref + "_kernel.uk_setup ";
#else
                sql = " Select bdf From " + pref + "_kernel:uk_setup ";
#endif
                if (ExecRead(conn_db, transaction, out reader, sql, false).result)
                {
                    if (reader.Read())
                    {
                        if (reader["bdf"] != DBNull.Value)
                            kod.bdf = (string) reader["bdf"];
                    }
                }
            }
            catch
            {
                //нет uk_setup
            }
            finally
            {
                if(reader != null) reader.Close();
                //reader.Dispose();
                reader = null;
            }

            if (nzp_area > 0)
            {
#if PG
                sql = " Select val_prm From " + Points.Pref + "_data.prm_7 " +
                      " Where nzp_prm = 995 " +
                      "   and dat_s  <= MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ")" +
                      "   and dat_po >= MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ")" +
                      "   and is_actual <> 100 " +
                      "   and nzp = " + nzp_area;
#else
                sql = " Select val_prm From " + pref + "_data:prm_7 " +
                      " Where nzp_prm = 995 " +
                      "   and dat_s  <= MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ")" +
                      "   and dat_po >= MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ")" +
                      "   and is_actual <> 100 " +
                      "   and nzp = " + nzp_area;
#endif
                if (ExecRead(conn_db, transaction, out reader, sql, true).result)
                {
                    if (reader.Read())
                    {
                        if (reader["val_prm"] != DBNull.Value)
                        {
                            string s = (string)reader["val_prm"];
                            int i;
                            if (Int32.TryParse(s, out i))
                            {
                                kod.kod_erc = i;
                            }
                        }
                    }
                }
                if (reader != null) reader.Close();                
                //reader.Dispose();
                reader = null;
            }
            if (nzp_geu > 0)
            {
#if PG
                sql = " Select val_prm From " + pref + "_data.prm_8 " +
                      " Where nzp_prm = 708 " +
                      "   and dat_s  <= MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ")" +
                      "   and dat_po >= MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ")" +
                      "   and is_actual <> 100 " +
                      "   and nzp = " + nzp_geu;
#else
                sql = " Select val_prm From " + pref + "_data:prm_8 " +
                      " Where nzp_prm = 708 " +
                      "   and dat_s  <= MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ")" +
                      "   and dat_po >= MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ")" +
                      "   and is_actual <> 100 " +
                      "   and nzp = " + nzp_geu;
#endif
                if (ExecRead(conn_db, transaction, out reader, sql, true).result)
                {
                    if (reader.Read())
                    {
                        if (reader["val_prm"] != DBNull.Value)
                        {
                            string s = (string)reader["val_prm"];
                            int i;
                            if (Int32.TryParse(s, out i))
                            {
                                kod.kod_erc = i;
                            }
                        }
                    }
                }
                if (reader != null) reader.Close();
                //reader.Dispose();
                reader = null;
            }

            //reader.Close();

            

            return kod;
        }
        /// <summary>
        /// Получить литеру ЛС
        /// </summary>
        /// <param name="finder">префикс,nzp_kvar</param>
        /// <param name="ret">результат</param>
        /// <param name="conn_db">подключение</param>
        /// <param name="transaction">транзакция</param>
        /// <returns>литера</returns>
        public int GetLitera(Ls finder, out Returns ret, IDbConnection conn_db, IDbTransaction transaction)
        {
#if PG
            string tprm1 = finder.pref + "_data.prm_1";
            string sql = "select extract(year from dat_s::date)-2000 as litera from " + tprm1 + " where nzp_prm=2004 and nzp = " + finder.nzp_kvar;
#else
            string tprm1 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1";
            string sql = "select year(dat_s)-2000 as litera from " + tprm1 + " where nzp_prm=2004 and nzp = " + finder.nzp_kvar;
#endif
            int litera = 0;
            MyDataReader reader;
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result) return litera;
            if (reader.Read()) if (reader["litera"] != DBNull.Value) litera = Convert.ToInt32(reader["litera"]);
            reader.Close();
            return litera;
        }

      
        private Returns GetAreaCodeForSmr(IDbConnection conn_db, IDbTransaction transaction, Ls finder,
                  out string area_code)
        {
            area_code = "";
            Returns ret;
            _KodERC kod_erc = GetKodErc(conn_db, transaction, finder.pref, finder.nzp_area, finder.nzp_geu, out ret);
            if (!ret.result) return ret;

            area_code = kod_erc.kod_erc.ToString("000") + (finder.nzp_geu % 100).ToString("00");
            DbTables tables = new DbTables(conn_db);
            StringBuilder sql =
                new StringBuilder("select count(*) from " + tables.area_codes + " where code = " + area_code);
            var areaCodeExist = ExecScalar(conn_db, transaction, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка проверки уникальности записи для кода управляющей организации: " + ret.text,
                    MonitorLog.typelog.Error, true);
                return ret;
            }
            if (Convert.ToInt64(areaCodeExist) == 0)
            {
                int nzpUser = finder.nzp_user;
                
                /*#region Определить пользователя

                DbWorkUser db = new DbWorkUser();
                int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret); //локальный пользователь      
                db.Close();
                if (!ret.result) return ret;

                #endregion*/

                sql.Remove(0, sql.Length);
                sql.AppendFormat(" insert into {0} (code,nzp_area,changed_by,changed_on,is_active) " +
                                 " values ({1},{2},{3},{4},1)", tables.area_codes, area_code, finder.nzp_area,
                    nzpUser,sCurDateTime);
                ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка сохранения кода управляющей организации: " + ret.text,
                        MonitorLog.typelog.Error, true);
                    return ret;
                }

                //ДУБЛЬ!
                //sql.Remove(0, sql.Length);
                //sql.AppendFormat(" insert into {0} (code,nzp_area,changed_by,changed_on,is_active) " +
                //                 " values ({1},{2},{3},current,1)", tables.area_codes, area_code, finder.nzp_area, nzpUser);
                //ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
                //if (!ret.result)
                //{
                //    MonitorLog.WriteLog("Ошибка сохранения кода управляющей организации: " + ret.text,
                //        MonitorLog.typelog.Error, true);
                //    return ret;
                //}

            }
            sql.Remove(0, sql.Length);
            sql.AppendFormat("update {0} set area_code = {1} where nzp_kvar = {2} and pref = '{3}'",
                tables.kvar, area_code, finder.nzp_kvar, finder.pref);
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка сохранения кода управляющей организации: " + ret.text,
                    MonitorLog.typelog.Error, true);
                return ret;
            }

            return ret;
        }

        private Returns GenSeqOnePkod10(int area_code)
        {
            Returns ret;

            string seq = Points.Pref + "_data" + tableDelimiter + "kvar_pkod10_" + area_code + "_seq";
            DbAdmin db = new DbAdmin();
            ret = db.CreateNewSeq(Points.Pref + "_data", Series.Types.PKod10.GetHashCode(), seq, "pkod10", Points.Pref + "_data" + tableDelimiter + "kvar" + " where area_code = " + area_code);
            if (!ret.result)
            {
                return ret;
            }
            return ret;
        }
    }
}
