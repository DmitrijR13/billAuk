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
    public partial class DbAdresKernel : DbAdresClient
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
                                
                using (IDbConnection conn_web = GetConnection(Constants.cons_Webdata))
                {
                    ret = OpenDb(conn_web, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения с БД.",MonitorLog.typelog.Error,true);
                        return ret;
                    }
                    //таблица выбранных ЛС
                    tXXSplsTable = DBManager.GetFullBaseName(conn_web, conn_web.Database,
                        "t" + finder.nzp_user + "_spls");                    
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

                //Добавляем код ЖЭУ (n из s_points)
                kvarToUpdate.ForEach(k=> k.geuCode = Points.PointList.First(p=>p.pref == k.pref).n);                
               
                //Обновление платежного кода
                foreach (Ls ls in kvarToUpdate)
                {
                    //получение платежного кода, area_code, pkod10
                    int areaCode, pkod10 = 0;
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
                            MonitorLog.WriteLog("Ошибка сохранения платежного кода: " + ret.text,
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

                    // для Самары
                    int areaCodeSupp, pkod10Supp = pkod10;
                    string pkodSupp = this.GeneratePkodSuppOneLS(ls, transaction, out areaCodeSupp, ref pkod10Supp,
                        out ret);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    if (pkodSupp != "")
                    {                        
                        
                        //todo переделать!
#warning Сделать проверку на существование 
                        //обновление в локальном банке supplier_codes
                        ret = DBManager.ExecSQL(conn_db,transaction,
                            String.Format("delete from {0} where nzp_kvar={1}",
                                DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "supplier_codes"), ls.nzp_kvar),
                            true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка сохранения удаления данных из pkodSupp в локальный банк: " + ret.text,
                                MonitorLog.typelog.Error, true);
                            return ret;
                        }


                        sql = new StringBuilder();
                        sql.AppendFormat(" insert into {0} (nzp_kvar,nzp_supp,kod_geu,pkod10,pkod_supp) values({1}, {2}, {3}, {4}, {5}) ",
                            DBManager.GetFullBaseName(conn_db, ls.pref + "_data", "supplier_codes"),      //0
                            ls.nzp_kvar,                                                            //1
                            "null",                                                                 //2
                            ls.geuCode,                                                             //3
                            pkod10Supp,                                                             //4
                            pkodSupp);                                                              //5
                        ret = DBManager.ExecSQL(conn_db, transaction, sql.ToString(), true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка сохранения pkodSupp в локальный банк: " + ret.text,
                                MonitorLog.typelog.Error, true);
                            return ret;
                        }                        
                    }                    

                    transaction.Commit();     
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
                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (transaction != null)
                {
                    if(!ret.result) transaction.Rollback();
                    transaction.Dispose();
                }                
            }
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
                    pkod = this.GeneratePkod_GetPkod13(Points.Region.GetHashCode(), ls.nzp_area, transaction, ref pkod10,
                        out areaCode);
                    break;
                }
                case FunctionsTypesGeneratePkod.samara:
                {
                    pkod = this.GeneratePkod_GetSamaraPkod13(ls.nzp_area, ls.nzp_geu, ls.pref,
                        ls.nzp_kvar,
                        transaction, ref pkod10,
                        out ret);
                    break;
                }
            }

            //проверка платежного кода
            if (String.IsNullOrEmpty(pkod) || pkod == "0")
            {
                ret.result = false;
                ret.text = String.Format("pkod={2}, nzp_kvar={0},area_code={1}", ls.nzp_kvar,
                    areaCode, pkod);
                MonitorLog.WriteLog("Ошибка получения платежного кода: " + ret.text,
                  MonitorLog.typelog.Error, true);
                return "";
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
                MonitorLog.WriteLog(
                    String.Format(
                        "Произошло дублирование платежного кода: {0} количество: {1}, nzp_kvar = {2}, area_code={3}, pkod10={4}",
                        pkod, countPkodExist, ls.nzp_kvar, areaCode, pkod10),
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
        private string GeneratePkod_GetPkod13(int regionCode, int nzp_area, IDbTransaction transaction, ref int pkod10, out int areaCode)
        {
            Returns ret = Utils.InitReturns();
            string errMsg = "";
            
            //pkod10 приходит готовый
            if (pkod10 > 0)
            {
                areaCode = GetAreaCodes(transaction.Connection, transaction, new Ls() { nzp_area = nzp_area }, out ret);
                if (!ret.result) errMsg = "Ошибка получения кодa area_code: " + ret.text;
            }
            else
            {
                ret = this.GetPkod10(transaction.Connection, transaction, new Ls() { nzp_area = nzp_area }, out pkod10, out areaCode);
                if (!ret.result) errMsg = "Ошибка получения кодов pkod10,area_code: " + ret.text;                                                                       
            }           
            if (!ret.result)
            {
                MonitorLog.WriteLog(errMsg, MonitorLog.typelog.Error, true);
                return "";
            }

            if (regionCode > 99 || areaCode > 99999 || pkod10 > 99999)
            {
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
        private string GeneratePkod_GetSamaraPkod13(int nzp_area, int nzp_geu, string pref, int nzp_kvar,
            IDbTransaction transaction, ref int pkod10 ,out Returns ret)
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
                    String.Format("Ошибка генерации платежного кода:  pkod10={0} ", pkod10), MonitorLog.typelog.Error,
                    true);
                return "";
            }

            //код счета
            _KodERC ercKode = GetKodErc(transaction.Connection, transaction, pref, nzp_area, nzp_geu, out ret);
            if (!ret.result)
            {
                MonitorLog.WriteLog(
                  String.Format("Ошибка генерации платежного кода:  kod_erc={0} ", ercKode.kod_erc), MonitorLog.typelog.Error,
                  true);
                return "";
            }
            if (ercKode.kod_erc > 999)
            {
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации платежного кода:  kod_erc={0} ", ercKode.kod_erc), MonitorLog.typelog.Error,
                    true);
                return "";
            }

            //литера       
            int litera = this.GetLitera(new Ls() {pref = pref, nzp_kvar = nzp_kvar}, out ret, transaction.Connection,
                transaction);
            if (!ret.result)
            {
                MonitorLog.WriteLog(
                    String.Format("Ошибка генерации платежного кода:  litera={0} ", litera), MonitorLog.typelog.Error,
                    true);
                return "";
            }

            string pkod = ercKode.kod_erc.ToString("000") + (nzp_geu%100).ToString("00") +
                pkod10.ToString("00000") + (litera > 9 ? 0 : litera);
            if (pkod.Length != 11)
            {
                MonitorLog.WriteLog(
                   String.Format("Ошибка генерации платежного кода(!=11):  pkod={0} ", pkod), MonitorLog.typelog.Error,
                   true);
            }
            return pkod.Length == 11 ? pkod + Utils.GetKontrSamara(pkod) : "";
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

        protected int GetAreaCodes(IDbConnection conn_db, IDbTransaction transaction, Ls finder, out Returns ret)
        {
            int code = Constants._ZERO_;
            ret = Utils.InitReturns();

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
                ret = new Returns(false, "Не установлен код управляющей организации "+finder.area,-1);
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
            if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
            {
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
                sql = " Select val_prm From " + pref + "_data.prm_7 " +
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
            string sql = "select year(dat_s)-2000 as litera from " + tprm1 + " where nzp_prm=2004 and nzp = " + finder.nzp_kvar;
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
    }
}
