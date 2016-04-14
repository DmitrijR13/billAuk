using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Data;
using System.Windows.Forms.VisualStyles;
using Bars.KP50.Utils;
using FastReport.Data;
using Globals.SOURCE.Config;
using Globals.SOURCE.Container;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public class DbGilec : DbGilecClient
    //----------------------------------------------------------------------
    {

        /// <summary>
        /// Формирование признаков перерасчета на основании изменения сведений о жильцах
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        protected Returns SaveMustCalc(IDbConnection connection, IDbTransaction transaction, EditInterDataMustCalc finder)
        {
            finder.nzp_wp = Points.GetPoint(finder.pref).nzp_wp;
            finder.mcalcType = enMustCalcType.mcalc_Gil;
            finder.dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'";
            finder.dat_po = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1).AddDays(-1).ToShortDateString() + "'";
            finder.intvType = enIntvType.intv_Day;
            finder.table = "pere_gilec";
            finder.primary = "nzp_pere_gilec";
            finder.kod2 = 0;

            finder.keys = new Dictionary<string, string>();
            finder.vals = new Dictionary<string, string>();

            finder.dopFind = new List<string>();
            finder.dopFind.Add(" and p.nzp_kvar = " + finder.nzp_kvar);
            Returns ret;
            //db.MustCalc(connection, transaction, finder, out ret); устаревшая функция
            var dbMustCalcNew = new DbMustCalcNew(connection);
            dbMustCalcNew.MustCalc(finder, out ret);
            return ret;
        }

        public Returns RecalcGillXX(Kart finder)
        {
            return new Returns(true);
            StringBuilder sql = new StringBuilder();
            Returns ret;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            if (CastValue<int>(finder.nzp_kart) <= 0)
            {
                ret.result = false;
                ret.text = "Не определена карточка жильца";
                return ret;
            }

            sql.Remove(0, sql.Length);
            sql.AppendFormat(" Select val_prm " +
                            " From {0} p , {1} k " +
                            " Where p.nzp_prm =  {2} " +
                            "   and p.is_actual = 1 " +
                            "   and p.dat_s  <= " + DBManager.sCurDate +
                            "   and p.dat_po >= " + DBManager.sCurDate +
                            "   and k.nzp_kart = {3} and k.nzp_kvar = p.nzp ", finder.pref + "_data" + tableDelimiter + "prm_1", finder.pref + "_data" + tableDelimiter + "kart", 130, finder.nzp_kart);
            object count5 = ExecScalar(conn_db, sql.ToString(), out ret, true);
            string val = "";
            if (ret.result)
            {
                try
                {
                    val = Convert.ToString(count5).Trim();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return ret;
                }
            }
            if (val != "1") return new Returns(true); //если не считать по паспортистке

            CalcMonthParams cmParams = new CalcMonthParams(finder.pref);
            RecordMonth rm = Points.GetCalcMonth(cmParams);

            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(finder.nzp_kvar, 0,
                finder.pref, rm.year_, rm.month_, rm.year_, rm.month_);

            using (var db = new DbCalcCharge())
            {
                db.ChoiseTempKvar(conn_db, ref paramcalc, true, out  ret);
                db.LoadTempTableOther(conn_db,ref paramcalc, out ret);
                db.LoadTempTablesForMonth(conn_db,ref paramcalc, out ret);

                bool b = db.CalcGilXX(conn_db, paramcalc, out ret);
                if (!b) return new Returns(false);
                if (!ret.result) return new Returns(false);
            }

            ret = ReSaveGilPrm(conn_db, finder);
            conn_db.Close();
            return ret;
        }

        public Returns ReSaveGilPrm(IDbConnection conn_db, Kart finder)
        {
            CalcMonthParams cmParams = new CalcMonthParams(finder.pref);
            RecordMonth rm = Points.GetCalcMonth(cmParams);
            Returns ret;
            string gil_xx = finder.pref + "_charge_" + (rm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "gil_" + rm.month_.ToString("00");
            StringBuilder sql = new StringBuilder();
            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" select count(distinct nzp_gil) from {0} where nzp_kvar ={1} and stek = 1 and '{2}' between dat_s and dat_po",
            //      gil_xx, finder.nzp_kvar, (new DateTime(rm.year_, rm.month_, DateTime.DaysInMonth(rm.year_, rm.month_)).ToShortDateString()));

            //object count3 = ExecScalar(conn_db, sql.ToString(), out ret, true);//2005 параметр

            sql.Remove(0, sql.Length);
            sql.AppendFormat("select cnt2 from {0} where stek = 3 and nzp_kvar = {1}", gil_xx, finder.nzp_kvar);

            object count4 = ExecScalar(conn_db, sql.ToString(), out ret, true); //5 параметр
            object count3 = count4;//2005 параметр
            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" Select val_prm " +
            //                " From {0} p " +
            //                " Where p.nzp_prm =  {1}" +
            //                "   and p.is_actual = 1 " +
            //                "   and p.dat_s  <= " + DBManager.sCurDate +
            //                "   and p.dat_po >= " + DBManager.sCurDate, finder.pref + "_data" + tableDelimiter + "prm_1", 130);
            //object count5 = ExecScalar(conn_db, sql.ToString(), out ret, true);
            //string val = "";
            //if (ret.result)
            //{
            //    try
            //    {
            //        val = Convert.ToString(count5).Trim();
            //    }
            //    catch (Exception ex)
            //    {
            //        ret.result = false;
            //        ret.text = ex.Message;
            //        return ret;
            //    }
            //}

            Param prm = new Param();

            if (!Points.IsSmr)
            {
                prm.dat_s = new DateTime(rm.year_, rm.month_, 1).ToShortDateString();
                prm.nzp_user = finder.nzp_user;
                prm.webLogin = finder.webLogin;
                prm.webUname = finder.webUname;
                prm.pref = finder.pref;
                prm.nzp = finder.nzp_kvar;
                prm.nzp_prm = 2005;
                prm.val_prm = Convert.ToInt32(count3).ToString();
                prm.prm_num = 1;
                using (var db2 = new DbSavePrm())
                {
                    ret = db2.Save(prm);
                }
            }
            //if (val.Trim() == "1")
            //{
            prm = new Param();

            prm.dat_s = new DateTime(rm.year_, rm.month_, 1).ToShortDateString();
            prm.nzp_user = finder.nzp_user;
            prm.webLogin = finder.webLogin;
            prm.webUname = finder.webUname;
            prm.pref = finder.pref;
            prm.nzp = finder.nzp_kvar;
            prm.nzp_prm = 5;
            prm.val_prm = Convert.ToInt32(count4).ToString();
            prm.prm_num = 1;
            using (var db2 = new DbSavePrm())
            {
                ret = db2.Save(prm);
            }

            //}
            return ret;
        }

        //public Returns ReSaveGilPrm(Kart finder)
        //{
        //    IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
        //    Returns ret = OpenDb(conn_db, true);
        //    if (!ret.result) return ret;
        //    StringBuilder sql = new StringBuilder();
        //    sql.Remove(0, sql.Length);
        //    sql.AppendFormat(" Select val_prm " +
        //                    " From {0} p " +
        //                    " Where p.nzp_prm =  {1}" +
        //                    "   and p.is_actual = 1 " +
        //                    "   and p.dat_s  <= " + DBManager.sCurDate +
        //                    "   and p.dat_po >= " + DBManager.sCurDate, finder.pref + "_data" + tableDelimiter + "prm_1", 130);
        //    object count5 = ExecScalar(conn_db, sql.ToString(), out ret, true);
        //    string val = "";
        //    if (ret.result)
        //    {
        //        try
        //        {
        //            val = Convert.ToString(count5).Trim();
        //        }
        //        catch (Exception ex)
        //        {
        //            ret.result = false;
        //            ret.text = ex.Message;
        //            return ret;
        //        }
        //    }
        //    if (val != "1") return new Returns(true); //если не считать по паспортистке
        //    return ReSaveGilPrm(conn_db, finder);
        //}

        //----------------------------------------------------------------------
        public List<String> ProverkaKart(Kart new_kart, out Returns ret) //
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------
            ret = Utils.InitReturns();

            if (new_kart.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string a_data = new_kart.pref + "_data.";
#else
            string a_data = new_kart.pref + "_data:";
#endif
            string sql = "";
            List<String> returner = new List<String>();
            String sss = "";

            string my_kart = "kart";
            if (new_kart.is_arx)
            {
                my_kart = "kart_arx";
            }

            string my_num_ls = "num_ls";
            if (Points.IsSmr)
            {
                my_num_ls = "pkod10";
            }



            //----------------------------------------------------------------------
            if (new_kart.dat_ofor != "" && Convert.ToDateTime(new_kart.dat_ofor).Date > System.DateTime.Today)
            {
                returner.Add("Дата оформления больше текущей даты.");
            }

            //----------------------------------------------------------------------
            //----------------------------------------------------------------------
            if ((new_kart.fam.Trim() != "") && (new_kart.ima.Trim() != "") && (new_kart.dat_rog.Trim() != "") && (new_kart.nzp_tkrt.Trim() == "1"))
            {
                sql = "select distinct kv." + my_num_ls + " from " + a_data + my_kart + " k, " + a_data + "kvar kv " +
                    " where k.nzp_kvar=kv.nzp_kvar" +
                    " and upper(k.fam) = '" + new_kart.fam.Trim().ToUpper() + "'" +
                    " and upper(k.ima) = '" + new_kart.ima.Trim().ToUpper() + "'" +
                    " and upper(k.otch) = '" + new_kart.otch.Trim().ToUpper() + "'" +
                    " and k.dat_rog = '" + new_kart.dat_rog.Trim() + "'" +
                    " and kv.nzp_kvar <> " + new_kart.nzp_kvar.ToString() +
                    " and k.nzp_tkrt=1" +
                    " and k.isactual='1'";

                if (new_kart.nzp_gil.Trim() != "")
                {
                    sql += " and k.nzp_gil <> " + new_kart.nzp_gil.Trim();
                }

                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.text = "Ошибка открытия поиска уникальности жильца по ФИОДР";
                    ret.result = false;
                    ret.tag = -1;
                    return null;
                }
                while (reader.Read())
                {
                    sss += " " + Convert.ToString(reader[my_num_ls]).Trim() + ";";
                }
                reader.Close();

                if (sss != "")
                {
                    returner.Add("В БД обнаружены другие жильцы с такими же ФИОДР.Список ЛС:" + sss);
                }
                //----------------------------------------------------------------------

                sql = "select distinct kv." + my_num_ls + ", k.fam, k.ima, k.otch, k.dat_rog from " + a_data + my_kart + " k, " + a_data + "kvar kv " +
                    " where k.nzp_kvar=kv.nzp_kvar" +
                    " and upper(k.fam_c) = '" + new_kart.fam.Trim().ToUpper() + "'" +
                    " and upper(k.ima_c) = '" + new_kart.ima.Trim().ToUpper() + "'" +
                    " and upper(k.otch_c) = '" + new_kart.otch.Trim().ToUpper() + "'" +
                    " and k.dat_rog_c = '" + new_kart.dat_rog.Trim() + "'" +
                    " and kv.nzp_kvar <> " + new_kart.nzp_kvar.ToString() +
                    " and k.nzp_tkrt=1" +
                    " and k.isactual='1'";

                if (new_kart.nzp_gil.Trim() != "")
                {
                    sql += " and k.nzp_gil <> " + new_kart.nzp_gil.Trim();
                }
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.text = "Ошибка открытия поиска уникальности жильца по изменными ФИОДР";
                    ret.result = false;
                    ret.tag = -1;
                    return null;
                }

                sss = "";

                while (reader.Read())
                {
                    sss += " " + Convert.ToString(reader[my_num_ls]).Trim() + " ";
                    sss += " " + Convert.ToString(reader["fam"]).Trim() + " ";
                    sss += " " + Convert.ToString(reader["ima"]).Trim() + " ";
                    sss += " " + Convert.ToString(reader["otch"]).Trim() + " ";
                    sss += " " + Convert.ToString(reader["dat_rog"]).Trim() + ";";
                }

                reader.Close();

                if (sss != "")
                {
                    returner.Add("В БД обнаружены другие жильцы с такими же изменными ФИОДР.\r\nСписок ЛС:" + sss);
                }

            }

            //----------------------------------------------------------------------
            if ((new_kart.fam_c.Trim() != "") && (new_kart.ima_c.Trim() != "") && (new_kart.dat_rog_c.Trim() != "") && (new_kart.nzp_tkrt.Trim() == "1"))
            {
                sql = "select distinct kv." + my_num_ls + " from " + a_data + my_kart + " k, " + a_data + "kvar kv " +
                   " where k.nzp_kvar=kv.nzp_kvar" +
                   " and upper(k.fam) = '" + new_kart.fam_c.Trim().ToUpper() + "'" +
                   " and upper(k.ima) = '" + new_kart.ima_c.Trim().ToUpper() + "'" +
                   " and upper(k.otch) = '" + new_kart.otch_c.Trim().ToUpper() + "'" +
                   " and k.dat_rog = '" + new_kart.dat_rog_c.Trim() + "'" +
                   " and kv.nzp_kvar <> " + new_kart.nzp_kvar.ToString() +
                   " and k.nzp_tkrt=1" +
                   " and k.isactual='1'";

                if (new_kart.nzp_gil.Trim() != "")
                {
                    sql += " and k.nzp_gil <> " + new_kart.nzp_gil.Trim();
                }

                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.text = "Ошибка открытия поиска уникальности жильца по ФИОДР";
                    ret.result = false;
                    ret.tag = -1;
                    return null;
                }

                sss = "";
                while (reader.Read())
                {
                    sss += " " + Convert.ToString(reader[my_num_ls]).Trim() + ";";
                }
                reader.Close();

                if (sss != "")
                {
                    returner.Add("В БД обнаружены другие жильцы с такими же ФИОДР как и изменные ФИОДР у данного жильца. Список ЛС:" + sss);
                }

            }
            //----------------------------------------------------------------------
            if ((new_kart.nzp_dok.Trim() == "") || (new_kart.nzp_dok.Trim() == "-1") || (new_kart.nomer.Trim() == "") ||
                (new_kart.vid_mes.Trim() == "") || (new_kart.vid_dat.Trim() == ""))
            {
                returner.Add("Удостоверение личности не введено.");
            }

            //----------------------------------------------------------------------

            if ((new_kart.nzp_dok.Trim() != "") && (new_kart.nzp_dok.Trim() != "-1") && (new_kart.serij.Trim().ToUpper() != "") && (new_kart.nomer.Trim().ToUpper() != "") && (new_kart.nzp_tkrt.Trim() == "1"))
            {
                sql = "select distinct kv." + my_num_ls + ", k.fam, k.ima, k.otch, k.dat_rog  from " + a_data + my_kart + " k, " + a_data + "kvar kv" +
                    " where k.nzp_kvar=kv.nzp_kvar" +
                    " and k.nzp_dok = " + new_kart.nzp_dok.Trim() +
                    " and k.serij = '" + new_kart.serij.Trim().ToUpper() + "'" +
                    " and k.nomer = '" + new_kart.nomer.Trim().ToUpper() + "'" +
                    " and kv.nzp_kvar <> " + new_kart.nzp_kvar.ToString() +
                    " and( upper(k.fam) <> '" + new_kart.fam.Trim().ToUpper() + "'" +
                    " or  upper(k.ima) <> '" + new_kart.ima.Trim().ToUpper() + "'" +
                    " or  upper(k.otch) <> '" + new_kart.otch.Trim().ToUpper() + "'" +
                                  " or  k.dat_rog <> '" + new_kart.dat_rog.Trim() + "')" +
                                  " and k.nzp_tkrt=1" +
                    " and k.isactual='1'";

                if (new_kart.nzp_gil.Trim() != "")
                {
                    sql += " and k.nzp_gil <> " + new_kart.nzp_gil.Trim();
                }

                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.text = "Ошибка открытия поиска уникальности удостоверения личности";
                    ret.tag = -1;
                    return null;
                }

                sss = "";
                int kol_gil = 0;
                while (reader.Read())
                {
                    sss += " " + Convert.ToString(reader[my_num_ls]).Trim() + " ";
                    sss += " " + Convert.ToString(reader["fam"]).Trim() + " ";
                    sss += " " + Convert.ToString(reader["ima"]).Trim() + " ";
                    sss += " " + Convert.ToString(reader["otch"]).Trim() + " ";
                    sss += " " + Convert.ToString(reader["dat_rog"]).Trim() + ";";
                    kol_gil++;
                }

                reader.Close();
                if (sss != "")
                {
                    returner.Add("В БД обнаружены другие жильцы с такими же удостоверениями личности" +
                        (kol_gil > 10 ? " в количестве " + kol_gil.ToString() : ": " + sss));
                }
            }
            //----------------------------------------------------------------------
            if ((new_kart.dat_sost.Trim() != "") && (new_kart.nzp_gil.Trim() != ""))
            {
#if PG
                sql = "select k.dat_sost||'' as dat_sost  from " + a_data + my_kart + " k" +
                                   " where k.nzp_kvar = " + new_kart.nzp_kvar.ToString() +
                                   " and k.dat_sost>date('" + new_kart.dat_sost.Trim() + "')" +
                                   " and k.nzp_gil=" + new_kart.nzp_gil.Trim();
#else
                sql = "select k.dat_sost||'' as dat_sost  from " + a_data + my_kart + " k" +
                                   " where k.nzp_kvar = " + new_kart.nzp_kvar.ToString() +
                                   " and k.dat_sost>date('" + new_kart.dat_sost.Trim() + "')" +
                                   " and k.nzp_gil=" + new_kart.nzp_gil.Trim();
#endif
                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.text = "Ошибка открытия поиска более поздних карточек";
                    ret.result = false;
                    ret.tag = -1;
                    return null;
                }
                sss = "";
                while (reader.Read())
                {
                    sss += " " + Convert.ToString(reader["dat_sost"]).Trim() + ";";
                }
                reader.Close();

                if (sss != "")
                {
                    returner.Add("В ЛС обнаружены другие карточки жильца с более поздней датой составления.Список дат составления:" + sss);
                }
            }
            //----------------------------------------------------------------------
            conn_db.Close();
            return returner;
        }

        private Returns CheckBeforeSaveKart(Kart card)
        {
            Returns ret = Utils.InitReturns();

            if (card.fam.Length > 40) return new Returns(false, "Анкета. Фамилия превышает 40 символов", -1);
            if (card.ima.Length > 40) return new Returns(false, "Анкета. Имя превышает 40 символов", -1);
            if (card.otch.Length > 40) return new Returns(false, "Анкета. Отчество превышает 40 символов", -1);
            if (card.fam_c.Length > 40) return new Returns(false, "Дополнительная информация. Фамилия превышает 40 символов", -1);
            if (card.ima_c.Length > 40) return new Returns(false, "Дополнительная информация. Имя превышает 40 символов", -1);
            if (card.otch_c.Length > 40) return new Returns(false, "Дополнительная информация. Отчество превышает 40 символов", -1);
            if (card.who_pvu.Length > 40) return new Returns(false, "Дополнительная информация. Отношение к военной службе. Кем принят на учет превышает 40 символов", -1);
            if (card.jobpost.Length > 40) return new Returns(false, "Дополнительная информация. Сведения о работе. Должность превышает 40 символов", -1);
            if (card.jobname.Length > 40) return new Returns(false, "Дополнительная информация. Сведения о работе. Место работы превышает 40 символов", -1);
            if (card.rem_op.Length > 80) return new Returns(false, "Прибытие/убытие. Прибытие. Улица, дом, кв. превышает 80 символов", -1);
            if (card.rem_ku.Length > 80) return new Returns(false, "Прибытие/убытие. Убытие. Улица, дом, кв. превышает 80 символов", -1);
            if (card.namereg.Length > 80) return new Returns(false, "Анкетные данные. Наименование органа регистрации превышает 80 символов", -1);
            if (card.kod_namereg_prn.Length > 7) return new Returns(false, "Анкетные данные. Код органа регистрации превышает 7 символов", -1);
            if (card.serij.Length > 10) return new Returns(false, "Анкетные данные. Удостоверение личности. Серия документа превышает 10 символов", -1);
            if (card.nomer.Length > 7) return new Returns(false, "Анкетные данные. Удостоверение личности. Номер документа превышает 7 символов", -1);
            if (card.vid_mes.Length > 70) return new Returns(false, "Анкетные данные. Удостоверение личности. Место выдачи документа превышает 70 символов", -1);
            if (card.kod_podrazd.Length > 20) return new Returns(false, "Анкетные данные. Удостоверение личности. Код подразделения превышает 20 символов", -1);
            if (card.rod.Length > 30) return new Returns(false, "Анкетные данные. Родственные отношения превышает 30 символов", -1);

            if (card.rem_mr.Length > 80) return new Returns(false, "Анкетные данные. Место рождения. Комментарий превышает 80 символов", -1);
            if (card.strana_mr.Length > 30) return new Returns(false, "Анкетные данные. Место рождения. Страна превышает 30 символов", -1);
            if (card.region_mr.Length > 30) return new Returns(false, "Анкетные данные. Место рождения. Регион превышает 30 символов", -1);
            if (card.okrug_mr.Length > 30) return new Returns(false, "Анкетные данные. Место рождения. Район превышает 30 символов", -1);
            if (card.gorod_mr.Length > 30) return new Returns(false, "Анкетные данные. Место рождения. Город превышает 30 символов", -1);
            if (card.npunkt_mr.Length > 30) return new Returns(false, "Анкетные данные. Место рождения. Населенный пункт превышает 30 символов", -1);

            if (card.strana_op.Length > 30) return new Returns(false, "Прибытие/убытие. Прибытие. Страна превышает 30 символов", -1);
            if (card.region_op.Length > 30) return new Returns(false, "Прибытие/убытие. Прибытие. Регион превышает 30 символов", -1);
            if (card.okrug_op.Length > 30) return new Returns(false, "Прибытие/убытие. Прибытие. Район превышает 30 символов", -1);
            if (card.npunkt_op.Length > 30) return new Returns(false, "Прибытие/убытие. Прибытие. Населенный пункт превышает 30 символов", -1);
            if (card.gorod_op.Length > 30) return new Returns(false, "Прибытие/убытие. Прибытие. Город превышает 30 символов", -1);

            if (card.strana_ku.Length > 30) return new Returns(false, "Прибытие/убытие. Убытие. Страна превышает 30 символов", -1);
            if (card.region_ku.Length > 30) return new Returns(false, "Прибытие/убытие. Убытие. Регион превышает 30 символов", -1);
            if (card.okrug_ku.Length > 30) return new Returns(false, "Прибытие/убытие. Убытие. Район превышает 30 символов", -1);
            if (card.npunkt_ku.Length > 30) return new Returns(false, "Прибытие/убытие. Убытие. Населенный пункт превышает 30 символов", -1);

            return ret;
        }

        //----------------------------------------------------------------------
        public Kart SaveKart(Kart old_kart, Kart new_kart, out Returns ret) //
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();

            if (new_kart.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }
            ret = CheckBeforeSaveKart(new_kart);
            if (!ret.result) return null;

            Kart returner = new Kart();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            #region Определить пользователя
            int nzpUser = new_kart.nzp_user;

            /*Finder finder = new Finder();
            finder.pref = new_kart.pref;
            finder.nzp_user = new_kart.nzp_user;
            
            DbWorkUser db = new DbWorkUser();
            int nzpUser =  db.GetLocalUser(conn_db, finder, out ret); //локальный пользователь 
            db.Close();
            if (!ret.result) return null;*/
            #endregion
#if PG
            string a_data = new_kart.pref + "_data.";
#else
            string a_data = new_kart.pref + "_data:";
#endif
            string my_kart = "kart";
            string my_gilec = "gilec";
            string my_arx = "0";

            if (new_kart.is_arx)
            {
                my_kart = "kart_arx";
                my_gilec = "gilec_arx";
                my_arx = "1";
            }


            //-проверки-------------------------------------------------------------
            if ((new_kart.nzp_gil == "")
                && (new_kart.nzp_kart != ""))
            {
                ret.text = "Код карточки есть, кода жильца нет.";
                ret.result = false;
                ret.tag = -1;
                conn_db.Close();
                return null;
            }
            string sql = "";

            #region Серия номер
            if ((new_kart.nzp_dok != "") && (new_kart.nzp_dok != "-1"))
            {
                sql = "select serij_mask from " + a_data + "s_dok where nzp_dok=" + new_kart.nzp_dok;
                string serij_mask = Convert.ToString(ExecScalarThr(conn_db, sql, out ret, true)).Trim();
                string new_serij;
                if (CheckMask(serij_mask, new_kart.serij, out new_serij) == 0)
                {
                    new_kart.serij = new_serij;
                }
                else
                {
                    ret.text = "Не принят ошибочный ввод серии удостоверения (маска " + serij_mask + ")";
                    ret.result = false;
                    ret.tag = -1;
                    conn_db.Close();
                    return null;
                }
                sql = "select nomer_mask from " + a_data + "s_dok where nzp_dok=" + new_kart.nzp_dok;
                string nomer_mask = Convert.ToString(ExecScalarThr(conn_db, sql, out ret, true)).Trim();
                string new_nomer;
                if (CheckMask(nomer_mask, new_kart.nomer, out new_nomer) == 0)
                {
                    new_kart.nomer = new_nomer;
                }
                else
                {
                    ret.text = "Не принят ошибочный ввод номера удостоверения (маска " + nomer_mask + ")";
                    ret.result = false;
                    ret.tag = -1;
                    conn_db.Close();
                    return null;
                }
            }
            #endregion

            IDataReader reader;
            if (new_kart.neuch != "1")
            {
                #region проверка № 1 жилец с таким ФИОДР уже есть в лицевом счете - добавление нового жильца

                //----------------------------------------------------------------------
                if (new_kart.nzp_gil == "" && new_kart.nzp_kart == "")
                {
                    sql = "select * from " + a_data + my_kart +
                          " where upper(fam) = '" + new_kart.fam.Trim().ToUpper() + "'" +
                          " and upper(ima) = '" + new_kart.ima.Trim().ToUpper() + "'" +
                          " and upper(otch) = '" + new_kart.otch.Trim().ToUpper() + "'" +
                          " and dat_rog = '" + new_kart.dat_rog.Trim() + "'" +
                          " and " + sNvlWord + "(neuch" + sConvToInt + ",0)=0" +
                          " and nzp_kvar = " + new_kart.nzp_kvar.ToString();
                }
                else
                {
                    sql = "select * from " + a_data + my_kart +
                          " where upper(fam) = '" + new_kart.fam.Trim().ToUpper() + "'" +
                          " and upper(ima) = '" + new_kart.ima.Trim().ToUpper() + "'" +
                          " and upper(otch) = '" + new_kart.otch.Trim().ToUpper() + "'" +
                          " and dat_rog = '" + new_kart.dat_rog.Trim() + "'" +
                          " and " + sNvlWord + "(neuch" + sConvToInt + ",0)=0" +
                          " and nzp_kvar = " + new_kart.nzp_kvar.ToString() +
                          " and nzp_gil <> " + new_kart.nzp_gil.Trim();
                }

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.text = "Ошибка проверки уникальности жильца. Проверка № 1";
                    return null;
                }



                if (reader.Read())
                {
                    reader.Close();
                    conn_db.Close();
                    ret.text = "В данном лицевом счете уже есть другой жилец с такими же Ф.И.О.Др.!";
                    ret.result = false;
                    ret.tag = -1;
                    return null;
                }
                //----------------------------------------------------------------------

                #endregion

                #region проверка № 2 существует жилец с измененными анкетными данными

                //----------------------------------------------------------------------
                if (new_kart.nzp_gil == "" && new_kart.nzp_kart == "")
                {
                    sql = " select * from " + a_data + my_kart +
                          " where upper(fam_c) = '" + new_kart.fam.Trim().ToUpper() + "'" +
                          " and upper(ima_c) = '" + new_kart.ima.Trim().ToUpper() + "'" +
                          " and upper(otch_c) = '" + new_kart.otch.Trim().ToUpper() + "'" +
                          " and dat_rog_c = '" + new_kart.dat_rog.Trim() + "'" +
                          " and " + sNvlWord + "(neuch" + sConvToInt + ",0)=0" +
                          " and nzp_kvar = " + new_kart.nzp_kvar.ToString();
                }
                else
                {
                    sql = " select * from " + a_data + my_kart +
                          " where upper(fam_c) = '" + new_kart.fam.Trim().ToUpper() + "'" +
                          " and upper(ima_c) = '" + new_kart.ima.Trim().ToUpper() + "'" +
                          " and upper(otch_c) = '" + new_kart.otch.Trim().ToUpper() + "'" +
                          " and dat_rog_c = '" + new_kart.dat_rog.Trim() + "'" +
                          " and nzp_kvar = " + new_kart.nzp_kvar.ToString() +
                          " and " + sNvlWord + "(neuch" + sConvToInt + ",0)=0" +
                          " and nzp_gil <> " + new_kart.nzp_gil.Trim();
                }

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.text = "Ошибка проверки изменных ФИОДР. Проверка № 2";
                    return null;
                }

                if (reader.Read())
                {
                    reader.Close();
                    conn_db.Close();
                    ret.text = "В данном лицевом счете другой жилец " + new_kart.fam + " " + new_kart.ima + " " +
                               new_kart.otch + " " + new_kart.dat_rog + " г.р.\n" +
                               " имеет измененные анетные данные как у этого жильца Ф.И.О.Др.";
                    ret.result = false;
                    ret.tag = -1;
                    return null;
                }
                //----------------------------------------------------------------------

                #endregion

                #region проверка № 3 -  Уже есть карточки на другого жильца такими же Ф.И.О.Др. как в графе "Смена анкетных данных!"

                //----------------------------------------------------------------------
                if (new_kart.fam_c.Trim() != "")
                {
                    if (new_kart.nzp_gil == "" && new_kart.nzp_kart == "")
                    {
                        sql = "select * from " + a_data + my_kart +
                              " where upper(fam) = '" + new_kart.fam_c.Trim().ToUpper() + "'" +
                              " and upper(ima) = '" + new_kart.ima_c.Trim().ToUpper() + "'" +
                              " and upper(otch) = '" + new_kart.otch_c.Trim().ToUpper() + "'" +
                              " and dat_rog = '" + new_kart.dat_rog_c.Trim() + "'" +
                              " and " + sNvlWord + "(neuch" + sConvToInt + ",0)=0" +
                              " and nzp_kvar = " + new_kart.nzp_kvar.ToString();
                    }
                    else
                    {
                        sql = "select * from " + a_data + my_kart +
                              " where upper(fam) = '" + new_kart.fam_c.Trim().ToUpper() + "'" +
                              " and upper(ima) = '" + new_kart.ima_c.Trim().ToUpper() + "'" +
                              " and upper(otch) = '" + new_kart.otch_c.Trim().ToUpper() + "'" +
                              " and dat_rog = '" + new_kart.dat_rog_c.Trim() + "'" +
                              " and nzp_kvar = " + new_kart.nzp_kvar.ToString() +
                              " and " + sNvlWord + "(neuch" + sConvToInt + ",0)=0" +
                              " and nzp_gil <> " + new_kart.nzp_gil.Trim();
                    }

                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        ret.text = "Ошибка проверки смены анкетных данных. Проверка № 3";
                        return null;
                    }

                    if (reader.Read())
                    {
                        reader.Close();
                        conn_db.Close();
                        ret.text =
                            "В данном лицевом уже есть карточки на другого жильца такими же Ф.И.О.Др. как в графе 'Смена анкетных данных!'";
                        ret.result = false;
                        ret.tag = -1;
                        return null;
                    }
                }
                //----------------------------------------------------------------------

                #endregion

                #region проверка № 4 -  удостоверение личности

                //----------------------------------------------------------------------
                if (
                    (new_kart.nzp_dok.Trim() != "") && (new_kart.nzp_dok.Trim() != "-1") &&
                    (new_kart.serij.Trim() != "") && (new_kart.nomer.Trim() != "")
                    )
                {
                    if (new_kart.nzp_gil == "" && new_kart.nzp_kart == "")
                    {
                        sql = "select * from " + a_data + my_kart +
                              " where nzp_dok = " + new_kart.nzp_dok.Trim() +
#if PG
 " and replace(serij,' ','') = replace('" + new_kart.serij.Trim().ToUpper() + "',' ','')" +
#else
                        " and replace(serij,' ') = replace('" + new_kart.serij.Trim().ToUpper() + "',' ')" +
#endif
 " and nomer = '" + new_kart.nomer.Trim().ToUpper() + "'" +
                              " and " + sNvlWord + "(neuch" + sConvToInt + ",0)=0" +
                              " and nzp_kvar = " + new_kart.nzp_kvar.ToString();
                    }
                    else
                    {
                        sql = "select * from " + a_data + my_kart +
                              " where nzp_dok = " + new_kart.nzp_dok.Trim() +
#if PG
 " and replace(serij,' ','') = replace('" + new_kart.serij.Trim().ToUpper() + "',' ','')" +
#else
                        " and replace(serij,' ') = replace('" + new_kart.serij.Trim().ToUpper() + "',' ')" +
#endif
 " and nomer = '" + new_kart.nomer.Trim().ToUpper() + "'" +
                              " and nzp_gil <> " + new_kart.nzp_gil.Trim() +
                              " and " + sNvlWord + "(neuch" + sConvToInt + ",0)=0" +
                              " and nzp_kvar = " + new_kart.nzp_kvar.ToString();
                    }

                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        ret.text = "Ошибка проверки удостоверения личности. Проверка № 4";
                        return null;
                    }

                    if (reader.Read())
                    {
                        string fiodr = "";
                        fiodr = Convert.ToString(reader["fam"]).Trim() + " ";
                        fiodr += Convert.ToString(reader["ima"]).Trim() + " ";
                        fiodr += Convert.ToString(reader["otch"]).Trim() + " ";
                        fiodr += Convert.ToString(reader["dat_rog"]).Substring(0, 10) + " г.р.";

                        reader.Close();
                        conn_db.Close();
                        ret.text = "Другой жилец " + fiodr + " уже имеет такое же удостоверение личности!";
                        ret.result = false;
                        ret.tag = -1;
                        return null;
                    }
                }
                //----------------------------------------------------------------------

                #endregion
            }

            #region проверка № 5 - периоды убытия
            //----------------------------------------------------------------------
            if (!new_kart.is_arx)
            {
                if (new_kart.nzp_gil != "") //существующий  жилец
                {


                    sql = "Select count(*) as cnt from  " + a_data + "gil_periods" +
                          " where nzp_kvar=" + new_kart.nzp_kvar.ToString() +
                          " and nzp_gilec=" + new_kart.nzp_gil +
                          " and is_actual<>100 ";

                    if (Convert.ToInt32(ExecScalarThr(conn_db, sql, out ret, true)) > 0) //есть вр. убытие
                    {
                        ExecSQL(conn_db, "drop table temp_kart", false);
                        ExecSQLThr(conn_db,
#if PG
 "create unlogged table temp_kart (nzp_kart integer, nzp_tkrt integer,dat_oprp date, dat_ofor date)",
#else
 "create temp table temp_kart (nzp_kart integer, nzp_tkrt integer,dat_oprp date, dat_ofor date) with no log",
#endif
 true);

                        sql = "insert into temp_kart" +
                         " select  nzp_kart, nzp_tkrt, dat_oprp, dat_ofor from " + a_data + "kart  " +
                         " Where nzp_gil=" + new_kart.nzp_gil +
                         " and   nzp_kvar=" + new_kart.nzp_kvar.ToString() +
#if PG
 " and   coalesce(neuch::int,0)=0 ";
#else
 " and   nvl(neuch,0)=0 ";
#endif

                        if (new_kart.nzp_kart != "")
                            sql += " and nzp_kart<>" + new_kart.nzp_kart;


                        ExecSQLThr(conn_db, sql, true);
                        if ((new_kart.neuch == "")
                        || (new_kart.neuch == "-1")
                        || (new_kart.neuch == "0"))
                        {
                            /*
                            sql = "insert into temp_kart(nzp_kart, nzp_tkrt, dat_oprp , dat_ofor) values(";
                            if (new_kart.nzp_kart != "") sql += new_kart.nzp_kart + ",";
                            else sql += new_kart.nzp_kart + "1000000000,";

                            sql += new_kart.nzp_tkrt + "," +
                                                Utils.EDateNull(new_kart.dat_oprp) + "," +
                                                Utils.EDateNull(new_kart.dat_ofor) + ")";
                            */

                            string[] args = new string[] { 
                                (new_kart.nzp_kart != "") ? new_kart.nzp_kart:new_kart.nzp_kart + "1000000000",
                                new_kart.nzp_tkrt,
                                Utils.EDateNull(new_kart.dat_oprp),
                                Utils.EDateNull(new_kart.dat_ofor)
                            };
                            sql = string.Format("insert into temp_kart(nzp_kart, nzp_tkrt, dat_oprp , dat_ofor) values({0})", string.Join(", ", args));
                            ExecSQLThr(conn_db, sql, true);
                        }
                        // периоды прибытия
                        ExecSQL(conn_db, "drop table temp_s_po", false);
                        ExecSQLThr(conn_db,
#if PG
 "create unlogged table temp_s_po (dat_s date, dat_po date )",
#else
 "create temp table temp_s_po (dat_s date, dat_po date ) with no log",
#endif
 true);

                        string my_date = "01.01.1900";
                        string dat_s;
                        string dat_po;

                        // вытащить первую  карточку жильца в квартире
                        sql = "select * from temp_kart a " +
                                 " Order by dat_ofor,nzp_tkrt";

                        ExecReadThr(conn_db, out reader, sql, true);
                        if (reader.Read())
                        {
                            if ((Convert.ToString(reader["nzp_tkrt"]).Trim() == "2")   //если первая карточка убытие
                            && (Convert.ToString(reader["dat_ofor"]).Trim() != ""))   //   с датой
                            {
                                // Сохранить период от бесконечности до первого убытия   !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                my_date = Convert.ToString(reader["dat_ofor"]).Trim();
                                ExecSQLThr(conn_db, " Insert into temp_s_po   Values('01.01.1901','" + my_date + "')", true);
                            }

                            bool fl_end = false;
                            // а теперь цикл по всем листкам пр/уб для данного ЛС данного жильца
                            while (!fl_end)
                            {
                                // вытащить первую прибытия
#if PG
                                sql =
                                    "select *, coalesce(dat_ofor,date('01.01.1901')) as my_date from temp_kart a " +
                                    " where nzp_tkrt=1" +
                                    " and coalesce(dat_ofor, date('01.01.1901'))>date('" + my_date + "')" +
                                    " Order by my_date";
#else
                                sql =
                                    "select *, nvl(dat_ofor,date('01.01.1901')) as my_date from temp_kart a " +
                                     " where nzp_tkrt=1" +
                                     " and nvl(dat_ofor, date('01.01.1901'))>date('" + my_date + "')" +
                                     " Order by my_date";
#endif
                                ExecReadThr(conn_db, out reader, sql, true);

                                if (!reader.Read()) //DM_kart1.RxQ_temp.isempty
                                    fl_end = true;
                                else
                                {
                                    // вытащить первуе убытие
                                    my_date = Convert.ToString(reader["my_date"]).Trim();
                                    dat_s = my_date;
#if PG
                                    sql =
                                                                            "select dat_ofor as my_date, nzp_tkrt from temp_kart a " +
                                                                             " where nzp_tkrt=2" +
                                                                             " and dat_ofor>'" + my_date + "'" +
                                                                             " Union " +
                                                                             " select dat_oprp as my_date, nzp_tkrt from temp_kart a " +
                                                                             " where nzp_tkrt=1" +
                                                                             " and dat_oprp>'" + my_date + "'" +
                                                                             " Order by 1, 2 desc";
#else
                                    sql =
                                                                            "select dat_ofor as my_date, nzp_tkrt from temp_kart a " +
                                                                             " where nzp_tkrt=2" +
                                                                             " and dat_ofor>'" + my_date + "'" +
                                                                             " Union " +
                                                                             " select dat_oprp as my_date, nzp_tkrt from temp_kart a " +
                                                                             " where nzp_tkrt=1" +
                                                                             " and dat_oprp>'" + my_date + "'" +
                                                                             " Order by 1, 2 desc";
#endif
                                    ExecReadThr(conn_db, out reader, sql, true);
                                    if (!reader.Read())
                                    {
                                        fl_end = true;
                                        dat_po = "01.01.3000";
                                    }
                                    else
                                    {
                                        my_date = Convert.ToString(reader["my_date"]).Trim();

                                        if (my_date != "") dat_po = my_date;
                                        else dat_po = "01.01.3000";
                                        reader.Close();

                                    }
                                    // Сохранить период    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
#if PG
                                    sql = " Insert into temp_s_po (   DAT_S,DAT_PO  ) " +
                                                                           " Values('" + dat_s + "','" + dat_po + "')";
#else
                                    sql = " Insert into temp_s_po (   DAT_S,DAT_PO  ) " +
                                                                           " Values('" + dat_s + "','" + dat_po + "')";
#endif
                                    ExecSQLThr(conn_db, sql, true);
                                }
                            }

                        }
                        sql = "Select count(*) as cnt from  " + a_data + "gil_periods a" +
                        " where a.nzp_kvar=" + new_kart.nzp_kvar.ToString() +
                        " and a.nzp_gilec=" + new_kart.nzp_gil +
                        " and a.is_actual<>100" +
                        " and 0=" +
                        " (select count(*) from temp_s_po b" +
                        " where a.dat_s between b.dat_s and b.dat_po" +
                        " and   a.dat_po  between b.dat_s and b.dat_po)";

                        if (Convert.ToInt32(ExecScalarThr(conn_db, sql, out ret, true)) > 0)
                        {
                            if (reader != null) reader.Close();
                            conn_db.Close();
                            ret.text = "По данным ПО \"КАРТОТЕКА\" Oбнаружено пересечение периодов временного и постоянного убытия.";
                            ret.result = false;
                            ret.tag = -1;
                            return null;
                        };
                    }
                }
            }
            //----------------------------------------------------------------------
            #endregion


            if (new_kart.nzp_gil != "")
            {
                returner.nzp_gil = new_kart.nzp_gil;
            }
            else
            //--новый жилец------------------------------------------------------------
            {
#if PG
                sql = "insert into " + a_data + my_gilec + "(nzp_gil) values(default)";
#else
                sql = "insert into " + a_data + my_gilec + "(nzp_gil) values(0)";
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи карточки жильца";
                    conn_db.Close();
                    return null;
                }
                returner.nzp_gil = Convert.ToString(GetSerialValue(conn_db));
            }

            string mropku_fields = "";
            string mropku_data = "";

            if (Points.Region != Regions.Region.Tatarstan)
            {
                mropku_fields = " strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr," +
                          " strana_op, region_op, okrug_op, gorod_op, npunkt_op," +
                          " strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku," +
                          " dat_smert, dat_fio_c, rodstvo,";

                mropku_data =
                            "'" + new_kart.strana_mr + "', " +
                            "'" + new_kart.region_mr + "', " +
                            "'" + new_kart.okrug_mr + "', " +
                            "'" + new_kart.gorod_mr + "', " +
                            "'" + new_kart.npunkt_mr + "', " +

                            "'" + new_kart.strana_op + "', " +
                            "'" + new_kart.region_op + "', " +
                            "'" + new_kart.okrug_op + "', " +
                            "'" + new_kart.gorod_op + "', " +
                            "'" + new_kart.npunkt_op + "', " +

                            "'" + new_kart.strana_ku + "', " +
                            "'" + new_kart.region_ku + "', " +
                            "'" + new_kart.okrug_ku + "', " +
                            "'" + new_kart.gorod_ku + "', " +
                            "'" + new_kart.npunkt_ku + "', " +

                            Utils.EStrNull(new_kart.dat_smert) + ", " +
                            Utils.EStrNull(new_kart.dat_fio_c) + ", " +

                            "'" + new_kart.rod + "', ";

            }

            string str_fields = "(nzp_gil, fam, ima, otch, dat_rog, nzp_tkrt, dat_sost, gender, nzp_rod, nzp_sud, neuch, ndees, " +
                                 "nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd, " +
                                 "nzp_kvar, tprp, namereg, kod_namereg_prn, dat_ofor, dat_oprp, dat_prop, " +
                                 "nzp_celp, nzp_lnop, nzp_stop, nzp_tnop, nzp_rnop, rem_op, " +
                                 "nzp_celu, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, " +
                                 "fam_c, ima_c, otch_c, dat_rog_c, " +
                                 "nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, " +
                                 "jobname, jobpost," +
                                 "who_pvu, dat_pvu, dat_svu," +

                                 mropku_fields +

                                 "dat_izm, nzp_user)";
            string str_data = "(" +
                            "'" + returner.nzp_gil + "'," +
                            "'" + new_kart.fam + "'," +
                            "'" + new_kart.ima + "'," +
                            "'" + new_kart.otch + "'," +
                            "'" + new_kart.dat_rog + "', " +
                                  new_kart.nzp_tkrt + ", " +
                            "'" + new_kart.dat_sost + "', " +
                            "'" + new_kart.gender + "', " +
#if PG
 "" + (new_kart.nzp_rod == "" ? "null" : new_kart.nzp_rod) + ", " +
                            "" + (new_kart.nzp_sud == "" ? "null" : new_kart.nzp_sud) + ", " +
#else
 "'" + new_kart.nzp_rod + "', " +
                            "'" + new_kart.nzp_sud + "', " +
#endif
 "'" + new_kart.neuch + "', " +
                            "'" + new_kart.ndees + "', " +
                // документ    
                                  new_kart.nzp_dok + ", " +
                            "'" + new_kart.serij + "', " +
                            "'" + new_kart.nomer + "', " +
#if PG
 "" + Utils.EStrNull(new_kart.vid_dat) + ", " +
#else
 "'" + new_kart.vid_dat + "', " +
#endif
 "'" + new_kart.vid_mes + "', " +
                            "'" + new_kart.kod_podrazd + "', " +
                // регистрация
                                  new_kart.nzp_kvar.ToString() + ", " +
                            "'" + new_kart.tprp + "', " +
                            "'" + new_kart.namereg + "', " +
                            "'" + new_kart.kod_namereg_prn + "', " +
#if PG
 "" + Utils.EStrNull(new_kart.dat_ofor) + ", " +
                            "" + Utils.EStrNull(new_kart.dat_oprp) + ", " +
                            "" + Utils.EStrNull(new_kart.dat_prop) + ", " +
                // откуда прибыл
                                  new_kart.nzp_celp + ", " +
                            "" + (new_kart.nzp_lnop == "" ? "null" : new_kart.nzp_lnop) + ", " +
                            "" + (new_kart.nzp_stop == "" ? "null" : new_kart.nzp_stop) + ", " +
                            "" + (new_kart.nzp_tnop == "" ? "null" : new_kart.nzp_tnop) + ", " +
                            "" + (new_kart.nzp_rnop == "" ? "null" : new_kart.nzp_rnop) + ", " +
                            "'" + new_kart.rem_op + "', " +
                // куда выбыл
                                  new_kart.nzp_celu + ", " +
                            "" + (new_kart.nzp_lnku == "" ? "null" : new_kart.nzp_lnku) + ", " +
                            "" + (new_kart.nzp_stku == "" ? "null" : new_kart.nzp_stku) + ", " +
                            "" + (new_kart.nzp_tnku == "" ? "null" : new_kart.nzp_tnku) + ", " +
                            "" + (new_kart.nzp_rnku == "" ? "null" : new_kart.nzp_rnku) + ", " +
                            "'" + new_kart.rem_ku + "', " +
#else
 "'" + new_kart.dat_ofor + "', " +
                            "'" + new_kart.dat_oprp + "', " +
                            "'" + new_kart.dat_prop + "', " +
                // откуда прибыл
                                  new_kart.nzp_celp + ", " +
                            "'" + new_kart.nzp_lnop + "', " +
                            "'" + new_kart.nzp_stop + "', " +
                            "'" + new_kart.nzp_tnop + "', " +
                            "'" + new_kart.nzp_rnop + "', " +
                            "'" + new_kart.rem_op + "', " +
                // куда выбыл
                                  new_kart.nzp_celu + ", " +
                            "'" + new_kart.nzp_lnku + "', " +
                            "'" + new_kart.nzp_stku + "', " +
                            "'" + new_kart.nzp_tnku + "', " +
                            "'" + new_kart.nzp_rnku + "', " +
                            "'" + new_kart.rem_ku + "', " +
#endif

                // смена анкентных данных
                            "'" + new_kart.fam_c + "', " +
                            "'" + new_kart.ima_c + "', " +
                            "'" + new_kart.otch_c + "', " +
#if PG
 "" + Utils.EStrNull(new_kart.dat_rog_c) + ", " +
                // место рождения
                           "" + (new_kart.nzp_lnmr == "" ? "null" : new_kart.nzp_lnmr) + ", " +
                           "" + (new_kart.nzp_stmr == "" ? "null" : new_kart.nzp_stmr) + ", " +
                           "" + (new_kart.nzp_tnmr == "" ? "null" : new_kart.nzp_tnmr) + ", " +
                           "" + (new_kart.nzp_rnmr == "" ? "null" : new_kart.nzp_rnmr) + ", " +
#else
 "'" + new_kart.dat_rog_c + "', " +
                // место рождения
                           "'" + new_kart.nzp_lnmr + "', " +
                           "'" + new_kart.nzp_stmr + "', " +
                           "'" + new_kart.nzp_tnmr + "', " +
                           "'" + new_kart.nzp_rnmr + "', " +
#endif
 "'" + new_kart.rem_mr + "', " +
                // работа
                            "'" + new_kart.jobname + "', " +
                            "'" + new_kart.jobpost + "', " +
                // военная служба
                            "'" + new_kart.who_pvu + "', " +
#if PG
 "" + Utils.EStrNull(new_kart.dat_pvu) + ", " +
                            "" + Utils.EStrNull(new_kart.dat_svu) + ", " +
#else
 "'" + new_kart.dat_pvu + "', " +
                            "'" + new_kart.dat_svu + "', " +
#endif

 mropku_data +

#if PG
 " now()," + nzpUser + ")";
#else
 " current," + nzpUser + ")";
#endif

            if (new_kart.nzp_kart == "")
            //--вставка карточки------------------------------------------------------------
            {
                sql = "insert into " + a_data + my_kart + str_fields + " values " + str_data;
            }
            else
            //--изменение  карточки------------------------------------------------------------
            {
                returner.nzp_kart = new_kart.nzp_kart;
                sql = "update " + a_data + my_kart + " set " + str_fields + " = " + str_data + " where nzp_kart=" + new_kart.nzp_kart;
            }

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи Карточки жильца";
                conn_db.Close();
                return null;
            }

            if (new_kart.nzp_kart == "")     //--вставка
            {
                returner.nzp_kart = Convert.ToString(GetSerialValue(conn_db));
            }

            // Актуальность            
            ExecSQLThr(conn_db, "UPDATE " + a_data + my_kart + " SET isactual='' WHERE nzp_gil=" + returner.nzp_gil, true);
            sql =
                "SELECT K.dat_sost, K.nzp_kart FROM " + a_data + my_kart + " K " +
                 " WHERE K.nzp_gil=" + returner.nzp_gil +
#if PG
 " and coalesce(K.neuch::int,0)=0" +
#else
 " and nvl(K.neuch,0)=0" +
#endif
 " order by 1 desc, 2 desc";

            ExecReadThr(conn_db, out reader, sql, true);
            if (reader.Read())
            {
                sql = "UPDATE " + a_data + my_kart + " SET isactual='1' WHERE nzp_kart=" + Convert.ToString(reader["nzp_kart"]).Trim();
                ExecSQLThr(conn_db, sql, true);
            }
            reader.Close();

            if (!new_kart.is_arx)
            {
                #region Гражданства
                ExecSQLThr(conn_db, "delete from " + a_data + "grgd  WHERE nzp_kart=" + returner.nzp_kart, true);

                foreach (KartGrgd gr in new_kart.listKartGrgd)
                {
                    ExecSQLThr(conn_db, "insert into  " + a_data + "grgd (nzp_kart,nzp_grgd) values(" + returner.nzp_kart + "," + gr.nzp_grgd + ")", true);
                }
                #endregion

                #region Перерасчеты

                if (!Points.IsSmr)   // Для Закрыл по просьбе Анэса
                {
                    if (new_kart.nzp_tkrt == "2") new_kart.dat_oprp = "";
                    if (old_kart.nzp_tkrt == "2") old_kart.dat_oprp = "";

                    if (new_kart.nzp_kart == "")         // вставка
                    {
                        string dat_ofor = (new_kart.dat_ofor == "") ? "null" : "'" + new_kart.dat_ofor + "'";
                        string dat_oprp = (new_kart.dat_oprp == "") ? "null" : "'" + new_kart.dat_oprp + "'";
                        ExecSQLThr(conn_db, "insert into  " + a_data + "pere_gilec(nzp_kvar, dat_s, dat_po,is_actual) values(" +
                                           new_kart.nzp_kvar + "," + dat_ofor + "," + dat_oprp + ",1)", true);
                    }
                    else // изменение
                    {
                        var dat_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_ + 1, 1).AddDays(-1);
                        UpdateOldRecords(conn_db, a_data + "pere_gilec", old_kart.nzp_kvar);
                        if ((old_kart.nzp_tkrt != new_kart.nzp_tkrt)
                        || (old_kart.neuch != new_kart.neuch))
                        {
                            DateTime? dat_ofor = null, dat_oprp = null, dat_min = null;
                            string dat_min_str, dat_calc_str;
                            DateTime dat_of_new, dat_op_new, dat_of_old, dat_op_old;
                            DateTime.TryParse(new_kart.dat_ofor, out dat_of_new);
                            DateTime.TryParse(new_kart.dat_oprp, out dat_op_new);
                            DateTime.TryParse(old_kart.dat_ofor, out dat_of_old);
                            DateTime.TryParse(old_kart.dat_oprp, out dat_op_old);
                            if (dat_of_new != dat_of_old)
                            {
                                dat_ofor = dat_of_old > dat_of_new ? dat_of_new : dat_of_old;
                            }
                            if (dat_op_new != dat_op_old)
                            {
                                dat_oprp = dat_op_old > dat_op_new ? dat_op_new : dat_op_old;
                            }
                            if (dat_ofor != null && dat_oprp != null)
                            {
                                dat_min = dat_oprp > dat_ofor ? dat_ofor : dat_oprp;
                            }
                            else if (dat_ofor != null)
                            {
                                dat_min = dat_ofor;
                            }
                            else if (dat_oprp != null)
                            {
                                dat_min = dat_oprp;
                            }
                            dat_min_str = (dat_min == null) ? "null" : "'" + dat_min + "'";
                            dat_calc_str = (dat_calc == null) ? "null" : "'" + dat_calc + "'";
                            if (dat_min < dat_calc)
                                ExecSQLThr(conn_db, "insert into  " + a_data + "pere_gilec(nzp_kvar, dat_s, dat_po,is_actual) values(" +
                                                   old_kart.nzp_kvar + "," + dat_min_str + "," + dat_calc_str + ",1)", true);
                        }
                        else
                        {
                            if ((old_kart.dat_ofor != new_kart.dat_ofor)
                              || (old_kart.dat_oprp != new_kart.dat_oprp))
                            {

                                string d_max;
                                string d_min;
                                DateTime dat_s1, dat_s2, dat_po1, dat_po2;

                                if (old_kart.dat_ofor == "")
                                    dat_s1 = Convert.ToDateTime("31.12.1899");
                                else
                                    dat_s1 = Convert.ToDateTime(old_kart.dat_ofor);

                                if (new_kart.dat_ofor == "")
                                    dat_s2 = Convert.ToDateTime("31.12.1899");
                                else
                                    dat_s2 = Convert.ToDateTime(new_kart.dat_ofor);

                                if (old_kart.dat_oprp == "")
                                    dat_po1 = Convert.ToDateTime("31.12.9999");
                                else
                                    dat_po1 = Convert.ToDateTime(old_kart.dat_oprp);

                                if (new_kart.dat_oprp == "")
                                    dat_po2 = Convert.ToDateTime("31.12.9999");
                                else
                                    dat_po2 = Convert.ToDateTime(new_kart.dat_oprp);


                                //------------------------------------------------------------------------------
                                if (dat_s1 != dat_s2)
                                {
                                    if (dat_s2 < dat_s1)
                                    {
                                        if (dat_po2 < dat_s1)
                                        {
                                            d_min = new_kart.dat_ofor;
                                            d_max = new_kart.dat_oprp;
                                        }
                                        else
                                        {
                                            d_min = new_kart.dat_ofor;
                                            d_max = old_kart.dat_ofor;
                                        }
                                    }
                                    else
                                    {
                                        if (dat_po1 < dat_s2)
                                        {
                                            d_min = old_kart.dat_ofor;
                                            d_max = old_kart.dat_oprp;
                                        }
                                        else
                                        {
                                            d_min = old_kart.dat_ofor;
                                            d_max = new_kart.dat_ofor;
                                        }
                                    }

                                    d_min = (d_min.Trim().Length == 0 ? DateNullString : "'" + d_min + "'");
                                    d_max = (d_max.Trim().Length == 0 ? DateNullString : "'" + d_max + "'");

                                    ExecSQLThr(conn_db, "insert into  " + a_data + "pere_gilec(nzp_kvar, dat_s, dat_po, is_actual) values(" +
                                              new_kart.nzp_kvar + ", " + d_min + ", " + d_max + ",1)", true);
                                }
                                //------------------------------------------------------------------------------

                                if (dat_po1 != dat_po2)
                                {
                                    if (dat_po1 < dat_po2)
                                    {
                                        if (dat_po1 < dat_s2)
                                        {
                                            d_min = new_kart.dat_ofor;
                                            d_max = new_kart.dat_oprp;
                                        }
                                        else
                                        {
                                            d_min = old_kart.dat_oprp;
                                            d_max = new_kart.dat_oprp;
                                        }
                                    }
                                    else
                                    {
                                        if (dat_po2 < dat_s1)
                                        {
                                            d_min = old_kart.dat_ofor;
                                            d_max = old_kart.dat_oprp;
                                        }
                                        else
                                        {
                                            d_min = new_kart.dat_oprp;
                                            d_max = old_kart.dat_oprp;
                                        }
                                    }

                                    d_min = (d_min.Trim().Length == 0 ? DateNullString : "'" + d_min + "'");
                                    d_max = (d_max.Trim().Length == 0 ? DateNullString : "'" + d_max + "'");

                                    ExecSQLThr(conn_db, "insert into  " + a_data + "pere_gilec(nzp_kvar, dat_s, dat_po,is_actual) values(" +
                                              new_kart.nzp_kvar + ", " + d_min + ", " + d_max + ",1)", true);
                                }
                            }
                        }
                    }

                    if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                    {
                        ret = SaveMustCalc(conn_db, null, new EditInterDataMustCalc()
                        {
                            pref = new_kart.pref,
                            nzp_kvar = new_kart.nzp_kvar,
                            nzp_user = new_kart.nzp_user,
                            webLogin = new_kart.webLogin,
                            webUname = new_kart.webUname,
                            comment_action = new_kart.comment_action.Length == 0 ? "Изменение карточки жильца" : new_kart.comment_action
                        });
                    }
                }
                #endregion
            }

            conn_db.Close();
            //return returner;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return returner;

            if (new_kart.neuch != "1")
                new_kart.neuch = "";

            ExecSQL(conn_web,
                "update t" + Convert.ToString(new_kart.nzp_user).Trim() + "_gil set " +
               " neuch='" + new_kart.neuch + "'," +
               " fam='" + new_kart.fam + "'," +
               " ima='" + new_kart.ima + "'," +
               " otch='" + new_kart.otch + "'," +
               " dat_rog='" + new_kart.dat_rog + "'" +
                " where nzp_kart='" + returner.nzp_kart + "'" +
                " and pref='" + new_kart.pref + "'" +
                " and is_arx=" + my_arx
                , false);

            //ExecSQL(conn_web, 
            //    "update t" + Convert.ToString(new_kart.nzp_user).Trim() + "_gilkvar set neuch='"+new_kart.neuch+"'"+
            //    " where nzp_kart='"+returner.nzp_kart+"'"+
            //    " and pref='"+new_kart.pref+"'" , false);

            conn_web.Close();

            return returner;
        }

        private void UpdateOldRecords(IDbConnection conn_db, string table, int nzp_kvar)
        {
            var sql = string.Format("update {0} set is_actual = 100 where nzp_kvar = {1}", table, nzp_kvar);
            ExecSQL(conn_db, sql, true);
        }

        //----------------------------------------------------------------------
        public List<Sprav> FindSprav(Sprav finder, out Returns ret) // достать справочник
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (finder.pref == "")
            {
                //из центрального банка
                finder.pref = Points.Pref;
            }

            List<Sprav> Spis = new List<Sprav>();
            Spis.Clear();

            IDbConnection conn_web = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            string a_data = finder.pref + "_data.";
#else
            string a_data = finder.pref + "_data:";
#endif
            //выбрать справочник
            string sqlStr = "";

            switch (finder.name_sprav)
            {
                case "s_dok":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_dok c " +
                            " order by dok";
                        break;
                    }

                case "s_dok_sv":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_dok_sv c " + (finder.nzp_sprav != "" ? " Where nzp_dok_sv = " + finder.nzp_sprav : "") +
                            " order by dok_sv";
                        break;
                    }

                case "s_rod":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_rod c " + (finder.nzp_sprav != "" ? " Where nzp_rod = " + finder.nzp_sprav : "") +
                            " order by rod";
                        break;
                    }
                case "s_cel":
                    {
                        if (finder.dop_kod == "1" || finder.dop_kod == "2")
                        {
                            sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                                " From " + a_data + "s_cel c " +
                                " where c.nzp_tkrt = " + "'" + finder.dop_kod + "'" + " order by cel";
                        }
                        else
                        {
#if PG
                            sqlStr = " Select  distinct(cel) as cel, '" + finder.pref + "' as pref, '0' as nzp_cel " +
                                                                 " From " + a_data + "s_cel order by cel";
#else
                            sqlStr = " Select  unique(cel) as cel, '" + finder.pref + "' as pref, '0' as nzp_cel " +
                                     " From " + a_data + "s_cel order by cel";
#endif
                        }
                        break;
                    }
                case "s_land":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_land c " + (finder.nzp_sprav != "" ? " Where nzp_land = " + finder.nzp_sprav : "") +
                            " order by land";
                        break;
                    }
                case "s_stat":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_stat c " + (finder.nzp_sprav != "" ? " Where nzp_stat = " + finder.nzp_sprav : " where c.nzp_land = " + finder.dop_kod) +
                            " order by stat";
                        break;
                    }
                case "s_town":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_town c " + (finder.nzp_sprav != "" ? " Where nzp_town = " + finder.nzp_sprav : " where c.nzp_stat = " + finder.dop_kod) +
                            " order by town";
                        break;
                    }
                case "s_rajon":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_rajon c " + (finder.nzp_sprav != "" ? " Where nzp_raj = " + finder.nzp_sprav : " where c.nzp_town = " + finder.dop_kod) +
                            " order by rajon";
                        break;
                    }
                case "s_typkrt":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_typkrt c " +
                            " order by nzp_tkrt";
                        break;
                    }
                case "s_grgd":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_grgd c " + (finder.nzp_sprav != "" ? " Where nzp_grgd = " + finder.nzp_sprav : "") +
                            " order by nzp_grgd";
                        break;
                    }
                case "s_rajon_dom":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_rajon_dom c " + (finder.nzp_sprav != "" ? " Where nzp_raj_dom = " + finder.nzp_sprav : "") +
                            " order by rajon_dom";
                        break;
                    }
                //Для Самары=======
                case "s_strana":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_strana c " +
                            " order by strana";
                        break;
                    }
                case "s_region":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_region c " +
                            " order by region";
                        break;
                    }
                case "s_okrug":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_okrug c " +
                            " order by okrug";
                        break;
                    }
                case "s_gorod":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_gorod c " +
                            " order by gorod";
                        break;
                    }
                case "s_npunkt":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_npunkt c " +
                            " order by npunkt";
                        break;
                    }
                case "s_vid_mes":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_vid_mes c " + (finder.nzp_sprav != "" ? " Where kod_vid_mes = " + finder.nzp_sprav : "") +
                            " order by vid_mes";
                        break;
                    }
                case "s_departure_types":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_departure_types c " + (finder.nzp_sprav != "" ? " Where id = " + finder.nzp_sprav : "") +
                            " order by type_name";
                        break;
                    }
                case "s_place_requirement":
                    {
                        sqlStr = " Select  '" + finder.pref + "' as pref , *" +
                            " From " + a_data + "s_place_requirement c " + (finder.nzp_sprav != "" ? " Where id = " + finder.nzp_sprav : "") +
                            " order by place";
                        break;
                    }
                case "s_remark_kart":
                    {
                        sqlStr = " Select  '" + Points.Pref + "' as pref , *" +
                            " From " + Points.Pref + DBManager.sDataAliasRest + "s_remark_kart c " + (finder.nzp_sprav != "" ? " Where id_rem = " + finder.nzp_sprav : "") +
                            " order by remark desc";
                        break;

                    }

                //Конец Для Самары=======
                default:
                    ret = new Returns(false, "Неверное наименование справочника");
                    conn_web.Close();
                    return null;
            }

            IDataReader reader;

            if (!ExecRead(conn_web, out reader, sqlStr, true).result)
            {
                conn_web.Close();
                ret.result = false;
                return null;
            }
            try
            {
                //добавляем пустую запись
                while (reader.Read())
                {
                    Sprav zap = new Sprav();
                    zap.pref = Convert.ToString(reader["pref"]).Trim();

                    switch (finder.name_sprav)
                    {
                        case "s_dok":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_dok"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["dok"]).Trim();
                                break;
                            }
                        case "s_dok_sv":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_dok_sv"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["dok_sv"]).Trim();
                                break;
                            }
                        case "s_rod":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_rod"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["rod"]).Trim();
                                break;
                            }
                        case "s_cel":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_cel"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["cel"]).Trim();
                                break;
                            }
                        case "s_land":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_land"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["land"]).Trim();
                                zap.dop_kod = Convert.ToString(reader["soato"]).Trim();
                                break;
                            }
                        case "s_stat":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_stat"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["stat"]).Trim();
                                zap.dop_kod = Convert.ToString(reader["soato"]).Trim();
                                break;
                            }
                        case "s_town":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_town"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["town"]).Trim();
                                zap.dop_kod = Convert.ToString(reader["soato"]).Trim();
                                break;
                            }
                        case "s_rajon":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_raj"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["rajon"]).Trim();
                                zap.dop_kod = Convert.ToString(reader["soato"]).Trim();
                                break;
                            }
                        case "s_typkrt":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_tkrt"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["typkrt"]).Trim();
                                break;
                            }
                        case "s_grgd":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_grgd"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["grgd"]).Trim();
                                break;
                            }
                        case "s_rajon_dom":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_raj_dom"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["rajon_dom"]).Trim();
                                zap.dop_kod = Convert.ToString(reader["alt_rajon_dom"]).Trim();
                                break;
                            }
                        case "s_vid_mes":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["kod_vid_mes"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["vid_mes"]).Trim();
                                break;
                            }
                        case "s_departure_types":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["id"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["type_name"]).Trim();
                                break;
                            }
                        case "s_place_requirement":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["id"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["place"]).Trim();
                                break;
                            }
                        //Для Самары=======
                        case "s_strana":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_strana"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["strana"]).Trim();
                                break;
                            }
                        case "s_region":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_region"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["region"]).Trim();
                                break;
                            }
                        case "s_okrug":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_okrug"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["okrug"]).Trim();
                                break;
                            }
                        case "s_gorod":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_gorod"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["gorod"]).Trim();
                                break;
                            }
                        case "s_npunkt":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["nzp_npunkt"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["npunkt"]).Trim();
                                break;
                            }
                        case "s_remark_kart":
                            {
                                zap.nzp_sprav = Convert.ToString(reader["id_rem"]).Trim();
                                zap.val_sprav = Convert.ToString(reader["remark"]).Trim();
                                break;
                            }

                        //Конец Для Самары=======
                    }
                    Spis.Add(zap);
                }
                reader.Close();
                conn_web.Close();
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки справочника " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<Kart> LoadPaspInfo(Kart finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var kartList = new List<Kart>();
            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            MyDataReader reader;
            var sql =
                string.Format("select distinct(k1.serij,k1.nomer),k1.serij,k1.nomer,k1.vid_mes,k1.vid_dat,d.dok,k1.isactual " +
                              " from {0}kart k1 left outer join {0}s_dok d on k1.nzp_dok = d.nzp_dok,{0}.kart k where" +
                              " trim(k1.serij) <> ''" +
                              " and trim(k1.nomer) <> '' and k1.fam = k.fam and k.dat_rog = k1.dat_rog and k1.ima = k.ima " +
                              " and k1.otch = k.otch and k.nzp_kart = {1} order by vid_dat desc",
                              finder.pref + DBManager.sDataAliasRest, finder.nzp_kart);
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке карточки жильца";
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    kartList.Add(new Kart
                    {
                        vid_mes = CastValue<string>(reader["vid_mes"]).Trim(),
                        serij = CastValue<string>(reader["serij"]).Trim(),
                        nomer = CastValue<string>(reader["nomer"]).Trim(),
                        isactual = CastValue<string>(reader["isactual"]).Trim() == "1" ? "Действующий" : "",
                        vid_dat = CastValue<DateTime>(reader["vid_dat"]).ToShortDateString().Trim(),
                        dok = CastValue<string>(reader["dok"]).Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";
                MonitorLog.WriteLog("Ошибка выборки " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
                conn_db.Close();
            }
            return new List<Kart>(kartList);
        }

        //----------------------------------------------------------------------
        public Kart LoadKart(Kart finder, out Returns ret) //вытащить Карточку жильцов
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();

            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string a_data = finder.pref + "_data.";
#else
            string a_data = finder.pref + "_data:";
#endif
            DbTables tables = new DbTables(conn_db);

            string my_kart = "kart";
            if (finder.is_arx)
            {
                my_kart = "kart_arx";
            }

            string mropku = "";
            if (Points.Region != Regions.Region.Tatarstan)
            {
                mropku = " c.strana_mr, c.region_mr, c.okrug_mr, c.gorod_mr, c.npunkt_mr," +
                        " c.strana_op, c.region_op, c.okrug_op, c.gorod_op, c.npunkt_op," +
                        " c.strana_ku, c.region_ku, c.okrug_ku, c.gorod_ku, c.npunkt_ku," +
                        " c.dat_smert, c.dat_fio_c, c.rodstvo, ";
            }

            string tbls = "";

            //выбрать карточку
#if PG
            if (Points.Region == Regions.Region.Tatarstan)
            {
                tbls = a_data + "s_ulica u," + a_data + "s_rajon r," + a_data + "s_land ln, " + a_data + "s_stat st," + a_data + "s_town tn,"; ;
            }
            else tbls = tables.ulica + " u, " + tables.rajon + " r, " + tables.land + " ln, " + tables.stat + " st, " + tables.town + " tn,";

            string sqlStr =
                            " Select k.num_ls ,k.nzp_kvar,k.nzp_dom , '" + finder.pref + "' as pref , " +
                            "  trim(coalesce(u.ulicareg,''))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '||" +
                            "  trim(coalesce(d.ndom,''))||'  корп. '|| trim(coalesce(d.nkor,''))||'  кв. '||trim(coalesce(k.nkvar,''))||'  ком. '||trim(coalesce(k.nkvar_n,'')) as adr," +
                            "   c.nzp_tkrt," +
                            "   case when c.isactual='1' then '1' else ' ' end as isactual," +
                            " cp.cel as celp," +
                            " cu.cel as celu," +
                            "   case when c.tprp='В' then 'ПРЕБЫВАНИЯ' else 'ЖИТЕЛЬСТВА' end as tprp," +
                            " ln_mr.land as lnmr, st_mr.stat as stmr, tn_mr.town as tnmr, rn_mr.rajon as rnmr," +
                            " ln_op.land as lnop, st_op.stat as stop, tn_op.town as tnop, rn_op.rajon as rnop," +
                            " ln_ku.land as lnku, st_ku.stat as stku, tn_ku.town as tnku, rn_ku.rajon as rnku," +
                           " (trim(us.name) || ' (' || trim(us.comment)|| ')')  as comment," +
                            "   case when c.nzp_sud=3 then 'И' else ' ' end as sud, *, " +
                //",dok, serij,rod, land, stat,tow, rajon, ulica, " +
                //для Самары
                            mropku +
                            " 1=1" +
                            " From " + tables.kvar + " k, " + tables.dom + " d, " +
                //" left outer join " + a_data + "s_rajon_dom rd on d.nzp_raj=rd.nzp_raj_dom, " +
                            tbls +
                            " " + a_data + my_kart + " c " +
                            " left outer join " + a_data + "s_dok sd on c.nzp_dok=sd.nzp_dok " +
                            " left outer join " + a_data + "s_cel cp on c.nzp_celp=cp.nzp_cel " +
                            " left outer join " + a_data + "s_cel cu on c.nzp_celu=cu.nzp_cel " +
                            " left outer join " + a_data + "s_rod sr on c.nzp_rod=sr.nzp_rod " +
                            " left outer join " + Points.Pref + DBManager.sDataAliasRest + "users us on c.nzp_user= us.nzp_user " +
                            " left outer join " + a_data + "s_land ln_mr on c.nzp_lnmr=ln_mr.nzp_land " +
                            " left outer join " + a_data + "s_stat st_mr on c.nzp_stmr=st_mr.nzp_stat " +
                            " left outer join " + a_data + "s_town tn_mr on c.nzp_tnmr=tn_mr.nzp_town " +
                            " left outer join " + a_data + "s_rajon rn_mr on c.nzp_rnmr=rn_mr.nzp_raj " +
                            " left outer join " + a_data + "s_land ln_op on c.nzp_lnop=ln_op.nzp_land " +
                            " left outer join " + a_data + "s_stat st_op on c.nzp_stop=st_op.nzp_stat " +
                            " left outer join " + a_data + "s_town tn_op on c.nzp_tnop=tn_op.nzp_town " +
                            " left outer join " + a_data + "s_rajon rn_op on c.nzp_rnop=rn_op.nzp_raj " +
                            " left outer join " + a_data + "s_land ln_ku on c.nzp_lnku=ln_ku.nzp_land " +
                            " left outer join " + a_data + "s_stat st_ku on c.nzp_stku=st_ku.nzp_stat " +
                            " left outer join " + a_data + "s_town tn_ku on c.nzp_tnku=tn_ku.nzp_town " +
                            " left outer join " + a_data + "s_rajon rn_ku on c.nzp_rnku=rn_ku.nzp_raj " +
                            " WHERE c.nzp_kart = " + finder.nzp_kart +
                            " and   c.nzp_kvar=k.nzp_kvar " +
                            " and   k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj " +
                            " and st.nzp_land=ln.nzp_land " +
                            " and tn.nzp_stat=st.nzp_stat " +
                            " and r.nzp_town=tn.nzp_town ";
#else
            if (Points.Region == Regions.Region.Tatarstan)
            {
                tbls = a_data + "s_ulica u, outer(" + a_data + "s_rajon r," + a_data + "s_town tn," + a_data + "s_stat st," + a_data + "s_land ln),";
            }
            else tbls = tables.ulica + " u,  outer(" + tables.rajon + " r, " + a_data + "s_town tn," + a_data + "s_stat st," + a_data + "s_land ln),";
            string sqlStr =
                 " Select '" + finder.pref + "' as pref , " +
                 "  trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" +
                 "  trim(nvl(d.ndom,''))||'  корп. '|| trim(nvl(d.nkor,''))||'  кв. '||trim(nvl(k.nkvar,''))||'  ком. '||trim(nvl(k.nkvar_n,'')) as adr," +
                 "   c.nzp_tkrt," +
                 "   case when c.isactual='1' then '1' else ' ' end as isactual," +
                 " cp.cel as celp," +
                 " cu.cel as celu," +
                 "   case when c.tprp='В' then 'ПРЕБЫВАНИЯ' else 'ЖИТЕЛЬСТВА' end as tprp," +
                 " ln_mr.land as lnmr, st_mr.stat as stmr, tn_mr.town as tnmr, rn_mr.rajon as rnmr," +
                 " ln_op.land as lnop, st_op.stat as stop, tn_op.town as tnop, rn_op.rajon as rnop," +
                 " ln_ku.land as lnku, st_ku.stat as stku, tn_ku.town as tnku, rn_ku.rajon as rnku," +
                " trim(us.name) || ' (' || trim(us.comment)|| ')' comment," +
                 "   case when c.nzp_sud=3 then 'И' else ' ' end as sud," +
                //для Самары
                 mropku +
                 " *" +
                 " From " + tables.kvar + " k, " + tables.dom + " d, " + tbls +
                 " OUTER " + a_data + "s_dok sd," +
                 " OUTER " + a_data + "s_cel cp," +
                 " OUTER " + a_data + "s_cel cu," +
                 " OUTER " + a_data + "s_rod sr," +

                 " OUTER " + a_data + "s_rajon_dom rd," +
                 " OUTER " + a_data + "s_land ln_mr," +
                 " OUTER " + a_data + "s_stat st_mr," +
                 " OUTER " + a_data + "s_town tn_mr," +
                 " OUTER " + a_data + "s_rajon rn_mr," +
                 " OUTER " + a_data + "s_land ln_op," +
                 " OUTER " + a_data + "s_stat st_op," +
                 " OUTER " + a_data + "s_town tn_op," +
                 " OUTER " + a_data + "s_rajon rn_op," +
                 " OUTER " + a_data + "s_land ln_ku," +
                 " OUTER " + a_data + "s_stat st_ku," +
                 " OUTER " + a_data + "s_town tn_ku," +
                 " OUTER " + a_data + "s_rajon rn_ku," +
                 " OUTER " + Points.Pref + DBManager.sDataAliasRest + "users us," +
                 " " + a_data + my_kart + " c " +
                 " WHERE c.nzp_kart = " + finder.nzp_kart +
                 " and   c.nzp_kvar=k.nzp_kvar" +
                 " and   k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj" +
                 " and c.nzp_dok=sd.nzp_dok" +
                 " and c.nzp_celp=cp.nzp_cel" +
                 " and c.nzp_celu=cu.nzp_cel" +
                 " and c.nzp_rod=sr.nzp_rod" +
                 " and st.nzp_land=ln.nzp_land" +
                 " and tn.nzp_stat=st.nzp_stat" +
                 " and r.nzp_town=tn.nzp_town" +
                 " and d.nzp_raj=rd.nzp_raj_dom" +
                 " and c.nzp_lnmr=ln_mr.nzp_land" +
                 " and c.nzp_stmr=st_mr.nzp_stat" +
                 " and c.nzp_tnmr=tn_mr.nzp_town" +
                 " and c.nzp_rnmr=rn_mr.nzp_raj" +
                 " and c.nzp_lnop=ln_op.nzp_land" +
                 " and c.nzp_stop=st_op.nzp_stat" +
                 " and c.nzp_tnop=tn_op.nzp_town" +
                 " and c.nzp_rnop=rn_op.nzp_raj" +
                 " and c.nzp_lnku=ln_ku.nzp_land" +
                 " and c.nzp_stku=st_ku.nzp_stat" +
                 " and c.nzp_tnku=tn_ku.nzp_town" +
                 " and c.nzp_rnku=rn_ku.nzp_raj" +
                 " and c.nzp_user=(-1)* us.nzp_user" +
                 "";
#endif

            //            sqlStr = "select fam,ima from   " + a_data + "kart c ";         //where c.nzp_kart = " + finder.nzp_kart; 
            IDataReader reader;

            /*            
                            IDbCommand cmd;
                            cmd = DBManager.newDbCommand(sqlStr, conn_web);
                           IDataReader reader = cmd.ExecuteReader();
                           reader.Read();
                           string fam = Convert.ToString(reader["fam"]);
            */
            if (!ExecRead(conn_db, out reader, sqlStr, true).result)
            {
                conn_db.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке карточки жильца";
                return null;
            }
            try
            {
                if (!reader.Read())
                {
                    ret.result = false;
                    ret.text = "Карточка не найдена";
                    return null;
                }
                Kart zap = new Kart();

                if (reader["num_ls"] == DBNull.Value)
                    zap.num_ls = 0;
                else
                    zap.num_ls = (int)reader["num_ls"];

                if (reader["nzp_kvar"] == DBNull.Value)
                    zap.nzp_kvar = 0;
                else
                    zap.nzp_kvar = (int)reader["nzp_kvar"];

                if (reader["nzp_dom"] == DBNull.Value)
                    zap.nzp_dom = 0;
                else
                    zap.nzp_dom = (int)reader["nzp_dom"];

                if (reader["adr"] == DBNull.Value)
                    zap.adr = "";
                else
                    zap.adr = ((string)reader["adr"]).Trim();


                zap.nzp_gil = Convert.ToString(reader["nzp_gil"]).Trim();
                zap.nzp_kart = Convert.ToString(reader["nzp_kart"]).Trim();
                zap.fam = Convert.ToString(reader["fam"]).Trim();
                zap.ima = Convert.ToString(reader["ima"]).Trim();
                zap.otch = Convert.ToString(reader["otch"]).Trim();
                //                    zap.dat_rog = ((DateTime)reader["dat_rog"]).ToString("dd.MM.yyyy");
                zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();

                zap.fam_c = Convert.ToString(reader["fam_c"]).Trim();
                zap.ima_c = Convert.ToString(reader["ima_c"]).Trim();
                zap.otch_c = Convert.ToString(reader["otch_c"]).Trim();
                zap.dat_rog_c = Convert.ToString(reader["dat_rog_c"]).Trim();

                zap.gender = Convert.ToString(reader["gender"]).Trim();


                zap.nzp_dok = Convert.ToString(reader["nzp_dok"]).Trim();
                zap.dok = Convert.ToString(reader["dok"]).Trim();
                zap.serij = Convert.ToString(reader["serij"]).Trim();
                zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();
                zap.kod_podrazd = Convert.ToString(reader["kod_podrazd"]).Trim();

                zap.tprp = Convert.ToString(reader["tprp"]).Trim();

                zap.nzp_tkrt = Convert.ToString(reader["nzp_tkrt"]).Trim();

                zap.isactual = Convert.ToString(reader["isactual"]).Trim();

                zap.jobpost = Convert.ToString(reader["jobpost"]).Trim();
                zap.jobname = Convert.ToString(reader["jobname"]).Trim();
                zap.who_pvu = Convert.ToString(reader["who_pvu"]).Trim();
                zap.dat_pvu = Convert.ToString(reader["dat_pvu"]).Trim();
                zap.dat_svu = Convert.ToString(reader["dat_svu"]).Trim();
                zap.namereg = Convert.ToString(reader["namereg"]).Trim();
                zap.kod_namereg_prn = Convert.ToString(reader["kod_namereg_prn"]).Trim();

                zap.nzp_rod = Convert.ToString(reader["nzp_rod"]).Trim();
                zap.rod = Convert.ToString(reader["rod"]).Trim();

                zap.nzp_celp = Convert.ToString(reader["nzp_celp"]).Trim();
                zap.celp = Convert.ToString(reader["celp"]).Trim();
                zap.nzp_celu = Convert.ToString(reader["nzp_celu"]).Trim();
                zap.celu = Convert.ToString(reader["celu"]).Trim();
                zap.ndees = Convert.ToString(reader["ndees"]).Trim();
                zap.neuch = Convert.ToString(reader["neuch"]).Trim();
                zap.nzp_sud = Convert.ToString(reader["nzp_sud"]).Trim();
                zap.sud = Convert.ToString(reader["sud"]).Trim();

                zap.nzp_land = Convert.ToString(reader["nzp_land"]).Trim();
                zap.nzp_stat = Convert.ToString(reader["nzp_stat"]).Trim();
                zap.nzp_town = Convert.ToString(reader["nzp_town"]).Trim();
                //zap.nzp_raj = Convert.ToString(reader["nzp_raj"]);

                //zap.nzp_ul = Convert.ToString(reader["nzp_ul"]);
                //zap.nzp_dom = Convert.ToString(reader["nzp_dom"]);
                //zap.nzp_kvar = Convert.ToString(reader["nzp_kvar"]);
#warning Реализовать заполнение поля "rajon_dom"
                //zap.rajon_dom = Convert.ToString(reader["rajon_dom"]).Trim();

                zap.land = Convert.ToString(reader["land"]).Trim();
                zap.stat = Convert.ToString(reader["stat"]).Trim();
                zap.town = Convert.ToString(reader["town"]).Trim();
                zap.rajon = Convert.ToString(reader["rajon"]).Trim();
                zap.ulica = Convert.ToString(reader["ulica"]).Trim();
                zap.ulicareg = Convert.ToString(reader["ulicareg"]).Trim();
                zap.ndom = Convert.ToString(reader["ndom"]).Trim();
                zap.nkor = Convert.ToString(reader["nkor"]).Trim();
                zap.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                zap.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();

                zap.nzp_lnmr = Convert.ToString(reader["nzp_lnmr"]).Trim();
                zap.nzp_stmr = Convert.ToString(reader["nzp_stmr"]).Trim();
                zap.nzp_tnmr = Convert.ToString(reader["nzp_tnmr"]).Trim();
                zap.nzp_rnmr = Convert.ToString(reader["nzp_rnmr"]).Trim();
                zap.lnmr = Convert.ToString(reader["lnmr"]).Trim();
                zap.stmr = Convert.ToString(reader["stmr"]).Trim();
                zap.tnmr = Convert.ToString(reader["tnmr"]).Trim();
                zap.rnmr = Convert.ToString(reader["rnmr"]).Trim();
                zap.rem_mr = Convert.ToString(reader["rem_mr"]).Trim();

                zap.nzp_lnop = Convert.ToString(reader["nzp_lnop"]).Trim();
                zap.nzp_stop = Convert.ToString(reader["nzp_stop"]).Trim();
                zap.nzp_tnop = Convert.ToString(reader["nzp_tnop"]).Trim();
                zap.nzp_rnop = Convert.ToString(reader["nzp_rnop"]).Trim();
                zap.lnop = Convert.ToString(reader["lnop"]).Trim();
                zap.stop = Convert.ToString(reader["stop"]).Trim();
                zap.tnop = Convert.ToString(reader["tnop"]).Trim();
                zap.rnop = Convert.ToString(reader["rnop"]).Trim();
                zap.rem_op = Convert.ToString(reader["rem_op"]).Trim();

                zap.nzp_lnku = Convert.ToString(reader["nzp_lnku"]).Trim();
                zap.nzp_stku = Convert.ToString(reader["nzp_stku"]).Trim();
                zap.nzp_tnku = Convert.ToString(reader["nzp_tnku"]).Trim();
                zap.nzp_rnku = Convert.ToString(reader["nzp_rnku"]).Trim();
                zap.lnku = Convert.ToString(reader["lnku"]).Trim();
                zap.stku = Convert.ToString(reader["stku"]).Trim();
                zap.tnku = Convert.ToString(reader["tnku"]).Trim();
                zap.rnku = Convert.ToString(reader["rnku"]).Trim();
                zap.rem_ku = Convert.ToString(reader["rem_ku"]).Trim();

                zap.dat_prib = Convert.ToString(reader["dat_prib"]).Trim();
                zap.dat_prop = Convert.ToString(reader["dat_prop"]).Trim();
                zap.dat_oprp = Convert.ToString(reader["dat_oprp"]).Trim();

                zap.dat_ofor = Convert.ToString(reader["dat_ofor"]).Trim();
                zap.dat_sost = Convert.ToString(reader["dat_sost"]).Trim();

                zap.dat_izm = Convert.ToString(reader["dat_izm"]).Trim();
                zap.webUname = Convert.ToString(reader["comment"]).Trim();

                zap.pref = Convert.ToString(reader["pref"]).Trim();

                if (Points.Region != Regions.Region.Tatarstan)
                {
                    zap.strana_mr = Convert.ToString(reader["strana_mr"]).Trim();
                    zap.region_mr = Convert.ToString(reader["region_mr"]).Trim();
                    zap.okrug_mr = Convert.ToString(reader["okrug_mr"]).Trim();
                    zap.gorod_mr = Convert.ToString(reader["gorod_mr"]).Trim();
                    zap.npunkt_mr = Convert.ToString(reader["npunkt_mr"]).Trim();

                    zap.strana_op = Convert.ToString(reader["strana_op"]).Trim();
                    zap.region_op = Convert.ToString(reader["region_op"]).Trim();
                    zap.okrug_op = Convert.ToString(reader["okrug_op"]).Trim();
                    zap.gorod_op = Convert.ToString(reader["gorod_op"]).Trim();
                    zap.npunkt_op = Convert.ToString(reader["npunkt_op"]).Trim();

                    zap.strana_ku = Convert.ToString(reader["strana_ku"]).Trim();
                    zap.region_ku = Convert.ToString(reader["region_ku"]).Trim();
                    zap.okrug_ku = Convert.ToString(reader["okrug_ku"]).Trim();
                    zap.gorod_ku = Convert.ToString(reader["gorod_ku"]).Trim();
                    zap.npunkt_ku = Convert.ToString(reader["npunkt_ku"]).Trim();

                    zap.dat_smert = Convert.ToString(reader["dat_smert"]).Trim();
                    zap.dat_fio_c = Convert.ToString(reader["dat_fio_c"]).Trim();
                    zap.rod = Convert.ToString(reader["rodstvo"]).Trim();

                }
                else
                {
                    zap.gorod_mr = Convert.ToString(reader["rem_mr"]).Trim();
                }

                // гражданства
                if (!finder.is_arx)
                {
                    IDataReader reader_g;
                    if (!ExecRead(conn_db, out reader_g,
                         " Select  '" + finder.pref + "' as pref , g.nzp_kart, g.nzp_grgd, s.grgd" +
                         " From " + a_data + "grgd g, " + a_data + "s_grgd s where g.nzp_grgd=s.nzp_grgd   and g.nzp_kart =" + zap.nzp_kart, true).result)
                    {
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                    while (reader_g.Read())
                    {
                        KartGrgd grgd = new KartGrgd();
                        grgd.nzp_kart = Convert.ToString(reader_g["nzp_kart"]).Trim();
                        grgd.nzp_grgd = Convert.ToString(reader_g["nzp_grgd"]).Trim();
                        grgd.grgd = Convert.ToString(reader_g["grgd"]).Trim();
                        grgd.pref = Convert.ToString(reader_g["pref"]).Trim();
                        zap.listKartGrgd.Add(grgd);
                    }
                    reader_g.Close();
                }
                // гражданства end
                reader.Close();
                conn_db.Close();
                return zap;
            }
            catch (Exception ex)
            {

                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки карточки жильца " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }
        //----------------------------------------------------------------------
        string Datas(string dat_nam, string dat_s, string dat_po)
        //----------------------------------------------------------------------
        {
            StringBuilder swhere = new StringBuilder();

            if (dat_po != "")
            {
                swhere.Append(" and c." + dat_nam + " <= " + dat_po);

                if (dat_s != "")
                {
                    swhere.Append(" and c." + dat_nam + " >= " + dat_s);
                }
            }
            else
            {
                if (dat_s != "")
                {
                    swhere.Append(" and c." + dat_nam + " = " + dat_s);
                }
            }

            return swhere.ToString();
        }
        //----------------------------------------------------------------------
        public void FindKart(Kart finder, out Returns ret) //найти и заполнить список карточек жильцов
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return;
            }

            finder.mark = 0;  // отмечаем вызов: первый раз
            FindKartOnce(finder, out ret);

            bool fl_vse = false;
            if (finder.dopFind != null)
                if (finder.dopFind.Count > 0) //учесть дополнительные шаблоны
                {
                    foreach (string s in finder.dopFind)
                    {
                        if (s.IndexOf(" AND 500=500") >= 0) // пометка архи и не архив
                        {
                            fl_vse = true;
                            break;
                        }
                    }
                }

            if ((finder.nzp_kvar != Constants._ZERO_) && fl_vse)
            {
                finder.mark = 1;   // отмечаем вызов: второй раз
                finder.is_arx = !finder.is_arx;
                FindKartOnce(finder, out ret);
            }


            if ((finder.nzp_kart == "") || (finder.nzp_gil == "") || (!Points.IsSmr))
                return;
            else
            {
                finder.nzp_kart = "";     // отмечаем вызов: nzp_gil ,без nzp_kart
                finder.pref = "";
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return;

                string tab_dosie = "t" + Convert.ToString(finder.nzp_user).Trim() + "_gilhist";
                int cnt_save = -1;
                int cnt = Convert.ToInt32(ExecScalarThr(conn_web, "select count(*) as cnt from " + tab_dosie, out ret, true));
                while (cnt_save != cnt)
                {
                    cnt_save = cnt;
                    FindKartOnce(finder, out ret);
                    cnt = Convert.ToInt32(ExecScalarThr(conn_web, "select count(*) as cnt from " + tab_dosie, out ret, true));
                }
                conn_web.Close();
                return;
            }
        }

        //----------------------------------------------------------------------
        public void FindKartOnce(Kart finder, out Returns ret) //найти и заполнить список карточек жильцов
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return;
            }

            if (finder.callFromFindLs)
            {
                //надо прежде заполнить список адресов
                DbAdres db = new DbAdres();
                db.FindLs(finder, out ret);
                db.Close();
                if (!ret.result) return;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            string tab_gil;
            if (finder.nzp_gil.Trim() != "") tab_gil = "_gilhist"; //история жильца
            else if (finder.nzp_kvar == Constants._ZERO_) tab_gil = "_gil"; //список жильцов            
            else tab_gil = "_gilkvar";//поквартирная карточка

            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;

            string my_kart = "kart";
            string my_arx = "0";
            if (finder.is_arx)
            {
                my_kart = "kart_arx";
                my_arx = "1";
            }

            string tXX_grgd = "t" + Convert.ToString(finder.nzp_user).Trim() + "grgd";


            if (((finder.nzp_gil.Trim() == "") || (finder.nzp_kart.Trim() != ""))
                && (finder.mark == 0))
            {

                ExecSQL(conn_web, " Drop table " + tXX_cnt, false);

                //создать таблицу webdata:tXX_cnt
                ret = ExecSQL(conn_web,
                          " Create table " + tXX_cnt +
                          " ( " +
                          "   nzp_serial    serial, " +
                          "   is_checked    INTEGER default 0 not null check (is_checked between 0 AND 1), " +
                          "   nzp_kvar      INTEGER, " +
                          "   num_ls        INTEGER, " +
                          "   pkod10        INTEGER, " +
                          "   nzp_dom       INTEGER, " +
                          "   nzp_gil       INTEGER, " +
                          "   nzp_kart      INTEGER, " +
                          "   rod           CHAR(30), " +
                          "   fam           CHAR(80)," +
                          "   ima           CHAR(80)," +
                          "   otch          CHAR(80)," +
                          "   dat_rog       date," +
                          "   gender        CHAR(1)," +
                          "   rem_mr        CHAR(80)," +
                          "   grgd          CHAR(60)," +
                          "   fam_c         CHAR(40)," +
                          "   ima_c         CHAR(40)," +
                          "   otch_c        CHAR(40)," +
                          "   dat_rog_c     date," +
                          "   nzp_tkrt      CHAR(1)," +
                          "   isactual      CHAR(1)," +
                          "   neuch         CHAR(1)," +
                          "   sud           CHAR(2)," +
                          "   tprp          CHAR(10)," +
                          "   dat_ofor      date," +
                          "   dat_oprp      date," +
                          "   dat_sost      date," +
                          "   nzp_geu       INTEGER,  " +
                          "   geu           CHAR(60), " +
                          "   adr           CHAR(160), " +
                          " nzp_dok       INTEGER," +
                          " dok           CHAR(30), " +
                          " serij         NCHAR(10)," +
                          " nomer         NCHAR(7)," +
                          " vid_mes       CHAR(70)," +
                          " vid_dat       date," +
                          "   nzp_raj       INTEGER,  " +
                          "   rajon         CHAR(160), " +
                          "   nzp_ul        INTEGER,  " +
                          "   ulica         CHAR(160), " +
                          "   ndom          CHAR(15),  " +
                          "   nkor          CHAR(15), " +
                          "   nkvar         CHAR(15),  " +
                          "   nkvar_n       CHAR(15), " +
                          "   ikvar         INTEGER,  " +
                          "   idom          INTEGER,  " +
                          "   pref          CHAR(20),  " +
                          "   is_arx        INTEGER default 0  NOT NULL, " +
                          " fio_kvs     CHAR(70), " +
                          " obsh_plosh  CHAR(20), " +
                          " gil_plosh   CHAR(20), " +
                          " projiv      INTEGER, " +
                          " propis      INTEGER, " +
                          " tip_sobstv  CHAR(255), " +
                          " komfortnost CHAR(255) " +
                          " ) ", true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
#if PG
            string tXX_cnt_full = "public." + tXX_cnt;
#else
            string tXX_cnt_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cnt;
#endif
            string cur_pref;
            string whereString1 = "";

            WhereString(finder, tXX_cnt_full, ref whereString1);
            List<_Point> points = new List<_Point>();
            if (finder.callFromFindLs) //вызов из поиска адресов
            {
               
#if PG
                string tXX_spls = "public.t" + Convert.ToString(finder.nzp_user).Trim() + "_spls";
#else
                string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + Convert.ToString(finder.nzp_user).Trim() + "_spls";
#endif
                // извлечь префиксы из tXX_spls
                string sql = "select distinct pref from " + tXX_spls;
                IDataReader reader = null;
                try
                {
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }
                    while (reader.Read())
                    {
                        if (reader["pref"] == DBNull.Value) continue;
                        points.Add(Points.GetPoint(reader["pref"].ToString().Trim()));
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка поиска по списку карточек жильцов " + ex, MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    conn_db.Close();
                    return;
                }
                finally
                {
                    if (reader != null) reader.Close();
                }
                whereString1 = whereString1 + " AND exists ( SELECT 1 FROM " + tXX_spls + " t where k.nzp_kvar=t.nzp_kvar and k.pref='PREFX')  "; //выборка из кеша
                finder.pref = "";
            }
            else
            {
                points = Points.PointList;
            }
            foreach (_Point zap in points)
            {
                if (finder.nzp_wp > 0)
                {
                    if (zap.nzp_wp != finder.nzp_wp) continue;
                }
                if (finder.pref != "")
                {
                    if (finder.pref != zap.pref) continue;
                }

                if (!Utils.IsInRoleVal(finder.RolesVal, zap.nzp_wp.ToString(), Constants.role_sql, Constants.role_sql_wp)) continue;

                cur_pref = zap.pref;

                StringBuilder sql = new StringBuilder();
                string my_rod = "(SELECT rd.rod FROM " + cur_pref + "_data" + tableDelimiter + "s_rod rd WHERE rd.nzp_rod = c.nzp_rod) as rod, ";
                if (Points.Region != Regions.Region.Tatarstan) my_rod = "c.rodstvo as rod,";

                sql.Append(" Insert into " + tXX_cnt_full +
                            " ( pref,nzp_kvar,num_ls,pkod10,nzp_dom,ikvar,idom, nzp_geu, geu, adr, nzp_raj, rajon, nzp_ul, ulica," +
                            "   nzp_gil, nzp_kart,rod,fam,ima,otch,dat_rog,gender,rem_mr,fam_c,ima_c,otch_c,dat_rog_c,nzp_tkrt,isactual,neuch, " +
                            " sud,tprp, dat_ofor,dat_oprp,dat_sost, nzp_dok, dok, serij, nomer, vid_mes, vid_dat," +
                            "is_arx, fio_kvs ) ");
                sql.Append(" SELECT '" + cur_pref + "', k.nzp_kvar, k.num_ls, k.pkod10, k.nzp_dom, ikvar, idom");
                sql.Append(", g.nzp_geu, g.geu");
                sql.Append(",  trim(" + DBManager.sNvlWord + "(u.ulica,''))||' / '||trim(" + DBManager.sNvlWord + "(r.rajon,''))||'   дом '||" +
                            "   trim(" + DBManager.sNvlWord + "(ndom,''))||'  корп. '|| trim(" + DBManager.sNvlWord + "(nkor,''))||'  кв. '||trim(" + DBManager.sNvlWord + "(nkvar,''))||'  ком. '||trim(" + DBManager.sNvlWord + "(nkvar_n,'')) as adr, ");
                sql.Append("   r.nzp_raj,r.rajon,u.nzp_ul,u.ulica,");
                sql.Append("   c.nzp_gil,c.nzp_kart, " + my_rod + " c.fam,c.ima,c.otch, c.dat_rog, c.gender, c.rem_mr, c.fam_c,c.ima_c,c.otch_c, c.dat_rog_c," +
                            "   case when c.nzp_tkrt=1 then 'П' else 'У' end as nzp_tkrt," +
                            "   case when c.isactual='1' then '1' else ' ' end as isactual," +
                            "   case when c.neuch='1' then '1' else ' ' end as neuch," +
                            "   case when c.nzp_sud=3 then 'И' else ' ' end as sud," +
                            "   case when c.tprp='В' then 'ПРЕБЫВАНИЯ' else 'ЖИТЕЛЬСТВА' end as tprp," +
                            " c.dat_ofor, c.dat_oprp, c.dat_sost, c.nzp_dok, dk.dok, c.serij, c.nomer, c.vid_mes, c.vid_dat, " + my_arx + ", k.fio ");

                sql.Append(" FROM " + cur_pref + DBManager.sDataAliasRest + "" + my_kart + " c ");
                sql.Append(" left JOIN  " + cur_pref + DBManager.sDataAliasRest + "s_dok dk on  C .nzp_dok = dk.nzp_dok, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "kvar k, " + Points.Pref + DBManager.sDataAliasRest + "dom d, ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "s_ulica u,   " + Points.Pref + DBManager.sDataAliasRest + "s_rajon r");
                sql.Append(",  " + Points.Pref + DBManager.sDataAliasRest + "s_geu g ");

                sql.Append(" WHERE k.nzp_dom=d.nzp_dom AND d.nzp_ul=u.nzp_ul AND u.nzp_raj=r.nzp_raj  ");
                sql.Append("   AND c.nzp_kvar = k.nzp_kvar");
                sql.Append(" AND k.nzp_geu = g.nzp_geu");

                if (finder.RolesVal != null)
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
                            sql.Append(" AND k.nzp_area IN (" + role.val + ")");
                        if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_geu)
                            sql.Append(" AND k.nzp_geu IN (" + role.val + ")");
                    }


                sql.Append(whereString1.Replace("PREFX", cur_pref));

                if (finder.dopFind2 != null)
                    if (finder.dopFind2.Count > 0) //учесть дополнительные шаблоны
                    {
                        foreach (string s in finder.dopFind2)
                        {
                            if (s.Trim() != "") sql.Append(" AND 0 < (" + s.Replace("PREFX", cur_pref) + ")");
                        }
                    }

                if (finder.dopUsl != null)
                {
                    foreach (var s in finder.dopUsl)
                    {
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        sql.Append(" and " + s.Replace("PREFX", cur_pref).Replace("{ALIAS}", "k").Replace("CNTRPRFX", Points.Pref) + "");
                    }
                }

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
                string temp_table_prm = "temp_table_prm";
                string temp_table_grgd = "temp_table_grgd";
                var sql_s = " CREATE TEMP TABLE " + temp_table_prm + " AS " +
                            " SELECT t.nzp_kvar ," +
                            " MAX(CASE WHEN b.nzp_prm = 4 THEN val_prm END) obsh_plosh," +
                            " MAX(CASE WHEN b.nzp_prm = 6 THEN val_prm END) gil_plosh," +
                            " MAX(CASE WHEN b.nzp_prm = 5 THEN val_prm END)::int projiv," +
                            " MAX(CASE WHEN b.nzp_prm = 2005 THEN val_prm END)::int propis," +
                            " ''::varchar tip_sobstv," +
                            " ''::varchar komfortnost" +
                            " FROM " + DBManager.GetFullBaseName(conn_web) + tableDelimiter + tXX_cnt + " t," +
                            cur_pref + DBManager.sDataAliasRest + "prm_1 b " +                                               
                            " WHERE  t.nzp_kvar=b.nzp" +                          
                            " AND b.nzp_prm IN (4, 5, 6, 2005)"+
                            " AND b.is_actual = 1 AND b.dat_po>" + DBManager.sCurDate +
                            " AND " + DBManager.sCurDate + ">b.dat_s " +
                            " GROUP BY t.nzp_kvar";
                ret = ExecSQL(conn_db, sql_s, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                ret = ExecSQL(conn_db, "CREATE INDEX ix1_table_test on " + temp_table_prm + "(nzp_kvar)");
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                sql_s = " UPDATE " + temp_table_prm + " t SET " +
                        " tip_sobstv =(CASE WHEN b.nzp_prm = 2009 and nzp_res=3017 THEN c.name_y END), " +
                        " komfortnost =(CASE WHEN b.nzp_prm = 3 and nzp_res=1 THEN c.name_y END)" +
                        " FROM " + cur_pref + DBManager.sKernelAliasRest + "res_y c," +
                        cur_pref + DBManager.sDataAliasRest + "prm_1 b " +
                        " WHERE nzp_prm in (2009,3) AND nzp_res in (1,3017) " +
                        " AND c.nzp_y=b.val_prm" + DBManager.sConvToInt +
                        " AND t.nzp_kvar=b.nzp "+
                        " AND b.is_actual = 1 AND b.dat_po>" + DBManager.sCurDate +
                        " AND " + DBManager.sCurDate + ">b.dat_s ";
                ret = ExecSQL(conn_db, sql_s, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                sql_s =
                    " UPDATE " + DBManager.GetFullBaseName(conn_web) + tableDelimiter + tXX_cnt + " t SET " +
                    " obsh_plosh= tt.obsh_plosh," +
                    " gil_plosh = tt.gil_plosh," +
                    " projiv = tt.projiv," +
                    " propis = tt.propis," +
                    " tip_sobstv= tt.tip_sobstv," +
                    " komfortnost= tt.komfortnost" +
                    " FROM " + temp_table_prm + " tt " +
                    " WHERE  t.nzp_kvar=tt.nzp_kvar";
                ret = ExecSQL(conn_db, sql_s, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                //string sql_s =
                //    " UPDATE " + DBManager.GetFullBaseName(conn_web) + tableDelimiter + tXX_cnt + " SET " +
                //    " obsh_plosh = (SELECT max(val_prm) FROM " + cur_pref + DBManager.sDataAliasRest + "prm_1 b " +
                //    "                WHERE b.nzp_prm = 4 AND b.nzp = nzp_kvar AND b.is_actual = 1 AND b.dat_po>" + DBManager.sCurDate + " AND " + DBManager.sCurDate + ">b.dat_s), " +
                //    " gil_plosh = (SELECT max(val_prm) FROM " + cur_pref + DBManager.sDataAliasRest + "prm_1 b " +
                //    "                WHERE b.nzp_prm = 6 AND b.nzp = nzp_kvar AND b.is_actual = 1 AND b.dat_po>" + DBManager.sCurDate + " AND " + DBManager.sCurDate + ">b.dat_s), " +
                //    " projiv = (SELECT max(val_prm) FROM " + cur_pref + DBManager.sDataAliasRest + "prm_1 b " +
                //    "                WHERE b.nzp_prm = 5 AND b.nzp = nzp_kvar AND b.is_actual = 1 AND b.dat_po>" + DBManager.sCurDate + " AND " + DBManager.sCurDate + ">b.dat_s)" + DBManager.sConvToInt + ", " +
                //    " propis = (SELECT max(val_prm) FROM " + cur_pref + DBManager.sDataAliasRest + "prm_1 b " +
                //    "                WHERE b.nzp_prm = 2005 AND b.nzp = nzp_kvar AND b.is_actual = 1 AND b.dat_po>" + DBManager.sCurDate + " AND " + DBManager.sCurDate + ">b.dat_s)" + DBManager.sConvToInt + ", " +
                //    " tip_sobstv = (SELECT max(name_y) FROM " + cur_pref + DBManager.sDataAliasRest + "prm_1 b, " + cur_pref + DBManager.sKernelAliasRest + "res_y c " +
                //    "                WHERE b.nzp_prm = 2009 AND b.nzp = nzp_kvar AND b.is_actual = 1 AND b.dat_po>" + DBManager.sCurDate + " AND " + DBManager.sCurDate + ">b.dat_s " +
                //    "                      AND c.nzp_y = b.val_prm" + DBManager.sConvToInt + " AND nzp_res = 3017), " +
                //    " komfortnost = (SELECT max(name_y) FROM " + cur_pref + DBManager.sDataAliasRest + "prm_1 b, " + cur_pref + DBManager.sKernelAliasRest + "res_y c " +
                //    "                WHERE b.nzp_prm = 3 AND b.nzp = nzp_kvar  AND b.is_actual = 1 AND b.dat_po>" + DBManager.sCurDate + " AND " + DBManager.sCurDate + ">b.dat_s " +
                //    "                       AND c.nzp_y = b.val_prm" + DBManager.sConvToInt + " AND nzp_res = 1) " +
                //    " WHERE nzp_kvar IN " +
                //    " (SELECT nzp FROM " + cur_pref + DBManager.sDataAliasRest + "prm_1 a WHERE a.nzp_prm IN (3, 4, 5, 6, 2005, 2009)) ";
                             

                #region заполнить гражданства
                if (!finder.is_arx)
                {
#region 
                    //IDataReader reader = null;
                    //IDataReader reader1 = null;
                    //ret = ExecRead(conn_db, out reader, "SELECT nzp_kvar, nzp_kart FROM " + tXX_cnt_full + " WHERE pref = " + Utils.EStrNull(cur_pref), true);
                    //if (!ret.result)
                    //{
                    //    conn_db.Close();
                    //    return;
                    //}

                    sql_s = "CREATE TEMP TABLE "+temp_table_grgd+" AS " +
                            " SELECT t.nzp_kvar, t.nzp_kart, string_agg(b.grgd, ', ') grgd " +
                            " FROM " + tXX_cnt_full + " t, " + cur_pref + DBManager.sDataAliasRest + "s_grgd b, " +
                            cur_pref + DBManager.sDataAliasRest + "grgd a " +
                            " WHERE a.nzp_grgd = b.nzp_grgd AND a.nzp_kart=t.nzp_kart" +
                            " AND t.pref = " + Utils.EStrNull(cur_pref) +
                            " GROUP BY 1,2";
                    ret = ExecSQL(conn_db, sql_s, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }

                    ret = ExecSQL(conn_db, "CREATE INDEX ix1_table_test2 on " + temp_table_grgd + "(nzp_kart)");
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }

                    sql_s = " UPDATE " + tXX_cnt_full + " t SET " +
                            " grgd = t2.grgd" +
                            " FROM " + temp_table_grgd + " t2 " +
                            " WHERE t.nzp_kart=t2.nzp_kart";
                    ret = ExecSQL(conn_db, sql_s, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }
                    //    while (reader.Read())
                    //    {
                    //        sql_str =
                    //            " SELECT DISTINCT grgd " +
                    //            " FROM " + cur_pref + DBManager.sDataAliasRest + "grgd a, " + cur_pref + DBManager.sDataAliasRest + "s_grgd b " +
                    //            " WHERE a.nzp_kart = " + Convert.ToString(reader["nzp_kart"]).Trim() + " AND a.nzp_grgd = b.nzp_grgd";
                    //        ret = ExecRead(conn_db, out reader1, sql_str, true);
                    //        if (!ret.result)
                    //        {
                    //            return;
                    //        }
                    //        str_grgd = "";
                    //        while (reader1.Read())
                    //        {
                    //            if (str_grgd != "") str_grgd += ", " + Convert.ToString(reader1["grgd"]).Trim();
                    //            else str_grgd = Convert.ToString(reader1["grgd"]).Trim();
                    //        }
                    //        reader1.Close();
                    //        if (str_grgd != "")
                    //        {
                    //            sql_str =
                    //                " UPDATE " + tXX_cnt_full + " SET grgd = '" + str_grgd + "' " +
                    //                "WHERE nzp_kart = " + Convert.ToString(reader["nzp_kart"]).Trim() + " AND pref = '" + cur_pref + "'";
                    //            if (!ExecSQL(conn_db, sql_str, true).result)
                    //            {
                    //                ret.result = false;
                    //                return;
                    //            }
                    //        }
                    //    }

                    #endregion

                }
                #endregion

                ExecSQL(conn_db, "drop table " + temp_table_prm, false);
                ExecSQL(conn_db, "drop table " + temp_table_grgd, false);
            }
            
            conn_db.Close(); //закрыть соединение с основной базой

            if (((finder.nzp_gil.Trim() == "") || (finder.nzp_kart.Trim() != ""))
                && (finder.mark == 0))
            {

                //далее работаем с кешем
                //создаем индексы на tXX_cnt
                string ix = "ix" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;

                ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_cnt + " (nzp_kvar,pref) ", true);
                if (ret.result) { ret = ExecSQL(conn_web, " Create index " + ix + "_2 on " + tXX_cnt + " (nzp_gil,pref) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, " Create index " + ix + "_3 on " + tXX_cnt + " (nzp_kart,pref) ", true); }
                if (ret.result) { ret = ExecSQL(conn_web, DBManager.sUpdStat + " " + tXX_cnt, true); }
            }

            conn_web.Close();

            return;
        }//FindKart

        //----------------------------------------------------------------------
        /// <summary>
        /// найти и заполнить список жильцов по лицевому счету, с полной информацией по человеку
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public List<GilecFullInf> GetFullInfGilList(Gilec finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                return null;
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader;
            List<GilecFullInf> Spis = new List<GilecFullInf>();



            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string cur_pref = finder.pref;
            var sql = new StringBuilder();

            string my_kart = finder.is_arx ? "KART_ARX" : "KART";

            #region Создание временной таблицы t_gil_list_info

            ExecSQL(conn_db, " DROP TABLE t_gil_list_info ", false);

            string mropku = "", mropkuHeader = "";
            if (Points.Region != Regions.Region.Tatarstan)
            {
                mropkuHeader = " ,strana_mr CHARACTER(30), region_mr CHARACTER(30), okrug_mr CHARACTER(30), gorod_mr CHARACTER(30), npunkt_mr CHARACTER(30)," +
                        " strana_op CHARACTER(30), region_op CHARACTER(30), okrug_op CHARACTER(30), gorod_op CHARACTER(30), npunkt_op CHARACTER(30), " +
                        " strana_ku CHARACTER(30), region_ku CHARACTER(30), okrug_ku CHARACTER(30), gorod_ku CHARACTER(30), npunkt_ku CHARACTER(30)," +
                        " dat_smert DATE, dat_fio_c DATE, rodstvo CHARACTER(30) ";

                mropku = " k.strana_mr, k.region_mr, k.okrug_mr, k.gorod_mr, k.npunkt_mr," +
                        " k.strana_op, k.region_op, k.okrug_op, k.gorod_op, k.npunkt_op," +
                        " k.strana_ku, k.region_ku, k.okrug_ku, k.gorod_ku, k.npunkt_ku," +
                        " k.dat_smert, k.dat_fio_c, k.rodstvo,";
            }

            sql.Append(" CREATE TEMP TABLE t_gil_list_info" +
                       "(nzp_area INTEGER, " +
                       " nzp_dok INTEGER, " +
                       " is_arx INTEGER, " +
                       " fam CHARACTER(40), " +
                       " ima CHARACTER(40), " +
                       " otch CHARACTER(40), " +
                       " dat_rog DATE, " +
                       " serij CHARACTER(10), " +
                       " nomer CHARACTER(7), " +
                       " vid_dat DATE, " +
                       " vid_mes CHARACTER(70), " +
                       " nzp_tkrt INTEGER, " +
                       " tprp CHARACTER(1), " +
                       " cel CHARACTER(40), " +
                       " nzp_gil INTEGER, " +
                       " nzp_kvar INTEGER, " +
                       " nzp_kart INTEGER, " +
                       " statop CHARACTER(30), " +
                       " twnop CHARACTER(30), " +
                       " landop CHARACTER(30), " +
                       " rajonop CHARACTER(30), " +
                       " statku CHARACTER(30), " +
                       " twnku CHARACTER(30), " +
                       " landku CHARACTER(30), " +
                       " rajonku CHARACTER(30), " +
                       " rod CHARACTER(30), " +
                       " jobpost CHARACTER(40), " +
                       " jobname CHARACTER(40), " +
                       " dat_oprp DATE, " +
                       " dat_ofor DATE, " +
                       " dat_prop DATE, " +
                       " rem_op CHARACTER(40), " +
                       " rem_ku CHARACTER(40), " +
                       " dat_ubit CHARACTER(40), " +
                       " town_ CHARACTER(30), " +
                       " rajon_ CHARACTER(30), " +
                       " rajon_dom CHARACTER(30), " +
                       " gender CHARACTER(1), " +
                       " rem_mr CHARACTER(30), " +
                       " address CHARACTER(200), " +
                       " fio CHARACTER(50), " +
                       " property CHARACTER(20), " +
                       " s_ob CHARACTER(20), " +
                       " s_gil CHARACTER(20), " +
                       " sobstw CHARACTER(100), " +
                       " dok CHARACTER(30), " +
                       " area CHARACTER(40), " +
                       " dok_sv CHARACTER(1000) " + mropkuHeader + ") " + DBManager.sUnlogTempTable);
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion
            sql.Remove(0, sql.Length);

            sql.Append(" INSERT INTO t_gil_list_info(" +
                       " is_arx, nzp_area, nzp_dok, fam, ima, otch, dat_rog, serij, nomer, vid_dat, vid_mes, nzp_tkrt, " +
                       " tprp, cel, nzp_gil, nzp_kvar, nzp_kart, " +
                       " statop, twnop, landop, rajonop, statku, twnku, landku, rajonku, " +
                         mropku.Replace("k.", "") +
                       " rod, jobpost, jobname, dat_oprp, dat_ofor, dat_prop, rem_op, rem_ku, " + "dat_ubit, " +
                       " town_, rajon_, rajon_dom, gender, rem_mr, address, fio) ");
            sql.Append(" SELECT " + (finder.is_arx ? "1 as is_arx, " : "0 as is_arx, "));
            sql.Append(" kv.nzp_area, k.nzp_dok, fam, ima, otch, dat_rog, serij, nomer, vid_dat, vid_mes, k.nzp_tkrt, ");
            sql.Append(" tprp, cel, nzp_gil, k.nzp_kvar, k.nzp_kart, ");
            sql.Append(" ss1.stat as statop, st1.town as twnop, sl1.land as landop, sr1.rajon as rajonop,");
            sql.Append(" ss2.stat as statku, st2.town as twnku, sl2.land as landku, sr1.rajon as rajonku,");

            sql.Append(mropku);

            sql.Append(" rod, jobpost, jobname, dat_oprp, dat_ofor, dat_prop, rem_op, rem_ku ,dat_ubit ");
            //добавочные//
            sql.Append(" ,st3.town as town_, sr3.rajon as rajon_ ");
            sql.Append(" ,rd.rajon_dom");
            sql.Append(" ,k.gender ,k.rem_mr");
            sql.Append(" ," + DBManager.sNvlWord + "(CASE WHEN sr3.rajon <> '-' THEN sr3.rajon ELSE st3.town END,'') || " +
                       " ' ул.' || " + DBManager.sNvlWord + "(ul.ulica,'') || " +
                       " ' д.' || " + DBManager.sNvlWord + "(dd.ndom,'') || " +
                         DBManager.sNvlWord + "(CASE WHEN dd.nkor <> '-' THEN dd.nkor END,'') || " +
                       " (CASE WHEN kv.nkvar <> '0' AND kv.nkvar <> '-' AND kv.nkvar IS NOT NULL THEN " +
                       "' кв.' || kv.nkvar ELSE '' END) AS address ");
            sql.Append(" ,kv.fio ");
            // 

            sql.Append(" FROM " + cur_pref + DBManager.sDataAliasRest + my_kart + " k ");
            //добавочные//  
            sql.Append(" left outer join " + cur_pref + DBManager.sDataAliasRest + "s_cel sc on k.nzp_celp=sc.nzp_cel ");
            sql.Append(" left outer join " + cur_pref + DBManager.sDataAliasRest + "s_rod sr on k.nzp_rod=sr.nzp_rod ");
            sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_land sl1 on sl1.nzp_land=nzp_lnop ");
            sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_town st1 on st1.nzp_town=nzp_tnop ");
            sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_stat ss1 on ss1.nzp_stat=nzp_stop ");
            sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_rajon sr1 on sr1.nzp_raj=nzp_rnop ");
            sql.Append(" left outer join " + cur_pref + DBManager.sDataAliasRest + "s_land sl2 on sl2.nzp_land=nzp_lnku ");
            sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_town st2 on st2.nzp_town=nzp_tnku ");
            sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_stat ss2 on ss2.nzp_stat=nzp_stku ");
            sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_rajon sr2 on sr2.nzp_raj=nzp_rnku ");
            sql.Append(" ," + Points.Pref + DBManager.sDataAliasRest + "kvar kv");
            sql.Append(" ," + Points.Pref + DBManager.sDataAliasRest + "dom dd");
            sql.Append(" left outer join " + cur_pref + DBManager.sDataAliasRest + "s_rajon_dom rd on dd.nzp_raj = rd.nzp_raj_dom");
            sql.Append(" ," + Points.Pref + DBManager.sDataAliasRest + "s_ulica ul ");
            sql.Append(" left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_rajon sr3 " +
                            " left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_town st3 on sr3.nzp_town = st3.nzp_town");
            sql.Append(" on ul.nzp_raj = sr3.nzp_raj ");
            sql.Append(" where k.nzp_kvar = " + finder.nzp_kvar);
            //добавочные//
            sql.Append(" and k.nzp_kvar = kv.nzp_kvar");
            sql.Append(" and kv.nzp_dom = dd.nzp_dom");
            sql.Append(" and dd.nzp_ul = ul.nzp_ul");

            sql.Append(" and " + DBManager.sNvlWord + "(k.neuch" + DBManager.sConvToInt + ", '0') <> '1' ");


            if (finder.header != 1)
                sql.Append(" and k.tprp = 'П' ");
            if (finder.header != 2)
                sql.Append(" and k.nzp_tkrt =1 " +
                           " and " + DBManager.sNvlWord +
                           "(k.dat_oprp, date('01.01.3000'))>=" + DBManager.sCurDateTime);
            if (finder.isactual != "100")
                sql.Append(" and k.isactual='1'");
            if (finder.isactual == "100")
                sql.Append(" and (isactual='1' or (isactual<>'1' and k.nzp_tkrt =2))");

            string sql_str = sql.ToString();

            if (!ExecSQL(conn_db, sql_str, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            try
            {
                ExecSQL(conn_db, " create index idx_gil_list_info_kvar on t_gil_list_info(nzp_kvar) ");
                ExecSQL(conn_db, " create index idx_gil_list_info_area on t_gil_list_info(nzp_area) ");
                ExecSQL(conn_db, " create index idx_gil_list_info_dok on t_gil_list_info(nzp_dok) ");
                ExecSQL(conn_db, DBManager.sUpdStat + " t_gil_list_info ");
                RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(cur_pref));
                ExecSQL(conn_db, " UPDATE t_gil_list_info SET property = " +
                                 " (select max(val_prm) from " + cur_pref + DBManager.sDataAliasRest + "prm_1 p where nzp_prm = 2009 and is_actual <> 100 " +
                                 " and t_gil_list_info.nzp_kvar = p.nzp and '" + (new DateTime(rm.year_, rm.month_, 1)).ToShortDateString() + "' between dat_s and dat_po)");
                ExecSQL(conn_db, " UPDATE t_gil_list_info SET s_ob = " +
                                 " (select  max(val_prm) from " + cur_pref + DBManager.sDataAliasRest + "prm_1 p where nzp_prm = 4 and is_actual = 1 " +
                                 " and t_gil_list_info.nzp_kvar = p.nzp and '" + (new DateTime(rm.year_, rm.month_, 1)).ToShortDateString() + "' between dat_s and dat_po) ");
                ExecSQL(conn_db, " UPDATE t_gil_list_info SET s_gil = " +
                                 " (select  max(val_prm) from " + cur_pref + DBManager.sDataAliasRest + "prm_1 p where nzp_prm = 6 and is_actual = 1 " +
                                 " and t_gil_list_info.nzp_kvar = p.nzp and '" + (new DateTime(rm.year_, rm.month_, 1)).ToShortDateString() + "' between dat_s and dat_po) ");
                ExecSQL(conn_db, " UPDATE t_gil_list_info SET dok = " +
                                 " (select dok from " + cur_pref + DBManager.sDataAliasRest + "s_dok d where d.nzp_dok = t_gil_list_info.nzp_dok) ");
                ExecSQL(conn_db, " UPDATE t_gil_list_info SET area = " +
                                 " (select area from " + cur_pref + DBManager.sDataAliasRest + "s_area a where t_gil_list_info.nzp_area = a.nzp_area) ");

                ExecRead(conn_db, out reader, " select trim(fam)||' '||trim(ima)||' '||trim(otch) as fio from " + cur_pref + DBManager.sDataAliasRest + "sobstw " +
                                              " where is_actual = 1 AND nzp_kvar = " + finder.nzp_kvar, true);
                string sobstw = string.Empty;
                while (reader.Read())
                {
                    sobstw += reader["fio"] + ", ";
                }
                sobstw = sobstw.TrimEnd(',', ' ');
                //длина не более 100 символов
                sobstw = sobstw.Length > 100 ? sobstw.Substring(0, 100) : sobstw;
                if (sobstw.Length != 0)                          //Utils.EStrNull уже добавляет кавычки
                    ExecSQL(conn_db, " UPDATE t_gil_list_info SET sobstw = " + Utils.EStrNull(sobstw) + " ", true);

                ExecRead(conn_db, out reader, " select dok_sv from " + cur_pref + DBManager.sDataAliasRest +
                                              "s_dok_sv where nzp_dok_sv in (select distinct nzp_dok_sv from " +
                                                cur_pref + DBManager.sDataAliasRest + "sobstw " +
                                              " where nzp_kvar = " + finder.nzp_kvar + ") ", true);
                string dok_sv = string.Empty;
                while (reader.Read())
                {
                    dok_sv += reader["dok_sv"] + ", ";
                }
                dok_sv = dok_sv.TrimEnd(',', ' ');
                dok_sv = dok_sv.Length > 1000 ? dok_sv.Substring(0, 100) : dok_sv;
                if (dok_sv.Length != 0)
                    ExecSQL(conn_db, " UPDATE t_gil_list_info SET dok_sv = '" + dok_sv + "' ", true);
            }
            catch
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }



            sql_str = " select * from t_gil_list_info order by dat_rog, fam, ima ";

            if (!ExecRead(conn_db, out reader, sql_str, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            if (!ExecSQL(conn_db, " drop table t_gil_list_info ", true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    GilecFullInf zap = new GilecFullInf();
                    zap.num = i.ToString();
                    if (reader["fam"] != DBNull.Value) zap.fam = Convert.ToString(reader["fam"]).Trim();
                    if (reader["ima"] != DBNull.Value) zap.ima = Convert.ToString(reader["ima"]).Trim();
                    if (reader["otch"] != DBNull.Value) zap.otch = Convert.ToString(reader["otch"]).Trim();
                    if (reader["dat_rog"] != DBNull.Value) zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();
                    if (reader["serij"] != DBNull.Value) zap.serij = Convert.ToString(reader["serij"]).Trim();
                    if (reader["nomer"] != DBNull.Value) zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                    if (reader["vid_mes"] != DBNull.Value) zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                    if (reader["vid_dat"] != DBNull.Value) zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();
                    if (reader["cel"] != DBNull.Value) zap.cel = Convert.ToString(reader["cel"]).Trim();
                    if (reader["rod"] != DBNull.Value) zap.rod = Convert.ToString(reader["rod"]).Trim(); // для Самары дальше меняется
                    if (reader["jobname"] != DBNull.Value) zap.job_name = Convert.ToString(reader["jobname"]).Trim();
                    if (reader["jobpost"] != DBNull.Value) zap.job_post = Convert.ToString(reader["jobpost"]).Trim();
                    if (reader["landop"] != DBNull.Value) zap.landop = Convert.ToString(reader["landop"]).Trim();
                    if (reader["statop"] != DBNull.Value) zap.statop = Convert.ToString(reader["statop"]).Trim();
                    if (reader["twnop"] != DBNull.Value) zap.twnop = Convert.ToString(reader["twnop"]).Trim();
                    if (reader["rajonop"] != DBNull.Value) zap.rajonop = Convert.ToString(reader["rajonop"]).Trim();
                    if (reader["rem_op"] != DBNull.Value) zap.rem_op = Convert.ToString(reader["rem_op"]).Trim();
                    if (reader["landku"] != DBNull.Value) zap.landku = Convert.ToString(reader["landku"]).Trim();
                    if (reader["statku"] != DBNull.Value) zap.statku = Convert.ToString(reader["statku"]).Trim();
                    if (reader["twnku"] != DBNull.Value) zap.twnku = Convert.ToString(reader["twnku"]).Trim();
                    if (reader["rajonku"] != DBNull.Value) zap.rajonku = Convert.ToString(reader["rajonku"]).Trim();
                    if (reader["rem_ku"] != DBNull.Value) zap.rem_ku = Convert.ToString(reader["rem_ku"]).Trim();
                    if (reader["nzp_tkrt"] != DBNull.Value) zap.nzp_tkrt = Convert.ToString(reader["nzp_tkrt"]).Trim();
                    if (reader["dat_ofor"] != DBNull.Value) zap.dat_ofor = Convert.ToString(reader["dat_ofor"]).Trim();
                    if (reader["dat_oprp"] != DBNull.Value) zap.dat_oprp = Convert.ToString(reader["dat_oprp"]).Trim();
                    if (reader["dat_prop"] != DBNull.Value) zap.dat_prop = Convert.ToString(reader["dat_prop"]).Trim();
                    if (reader["tprp"] != DBNull.Value) zap.tprp = Convert.ToString(reader["tprp"]).Trim();
                    if (reader["nzp_gil"] != DBNull.Value) zap.nzp_gil = Convert.ToInt64(reader["nzp_gil"]);
                    if (reader["nzp_kart"] != DBNull.Value) zap.nzp_kart = Convert.ToInt32(reader["nzp_kart"]);
                    if (reader["is_arx"] != DBNull.Value) zap.is_arx = Convert.ToBoolean(reader["is_arx"]);



                    //добавочные//
                    if (reader["town_"] != DBNull.Value) zap.town_ = Convert.ToString(reader["town_"]);
                    if (reader["rajon_"] != DBNull.Value) zap.rajon_ = Convert.ToString(reader["rajon_"]).Trim();
                    if (zap.rajon_ == "" || zap.rajon_ == "-")
                        if (reader["rajon_dom"] != DBNull.Value) zap.rajon_ = Convert.ToString(reader["rajon_dom"]).Trim();

                    if (reader["fio"] != DBNull.Value) zap.fio = Convert.ToString(reader["fio"]).Trim();
                    if (reader["address"] != DBNull.Value) zap.address = Convert.ToString(reader["address"]).Trim();
                    if (reader["property"] != DBNull.Value) zap.property = Convert.ToString(reader["property"]).Trim();
                    if (reader["s_ob"] != DBNull.Value) zap.s_ob = Convert.ToString(reader["s_ob"]).Trim();
                    if (reader["s_gil"] != DBNull.Value) zap.s_gil = Convert.ToString(reader["s_gil"]).Trim();
                    if (reader["sobstw"] != DBNull.Value) zap.sobstw = Convert.ToString(reader["sobstw"]).Trim();
                    if (reader["dok"] != DBNull.Value) zap.dok = Convert.ToString(reader["dok"]).Trim();
                    if (reader["dok_sv"] != DBNull.Value) zap.dok_sv = Convert.ToString(reader["dok_sv"]).Trim();
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();





                    if (Points.Region != Regions.Region.Tatarstan)
                    {
                        zap.rem_mr = Convert.ToString(reader["strana_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["region_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["okrug_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["gorod_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["npunkt_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["rem_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim();

                        if (zap.rem_mr != "")
                        {
                            if (Convert.ToString(reader["gender"]).Trim() == "Ж")
                            {
                                zap.rem_mr = "уроженка " + zap.rem_mr;
                            }
                            else
                            {
                                zap.rem_mr = "уроженец " + zap.rem_mr;
                            }
                        }


                        zap.rem_op = Convert.ToString(reader["strana_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["region_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["okrug_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["gorod_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["npunkt_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["rem_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim();

                        zap.rem_ku = Convert.ToString(reader["strana_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["region_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["okrug_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["gorod_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["npunkt_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["rem_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim();

                        zap.dat_smert = Convert.ToString(reader["dat_smert"]).Trim();
                        zap.rod = Convert.ToString(reader["rodstvo"]).Trim();

                    }

                    if (zap.is_arx)
                        my_kart = "kart_arx";
                    else
                        my_kart = "kart";


                    if (zap.nzp_tkrt == "1")
                    {

                        zap.type_prop = "постоянная";

                        if ((zap.tprp == "В") ||
                            ((zap.tprp == "") && (zap.dat_oprp != ""))
                          )
                        {
                            zap.type_prop = "временная";
                        }

                        if (zap.dat_prop != "")
                            zap.dat_prib = zap.dat_prop;
                        else
                            zap.dat_prib = zap.dat_ofor;

                        if (zap.dat_oprp != "")
                            zap.dat_vip = zap.dat_oprp;
                        else
                        {
                            // Если дата оформления не пустая
                            if (!String.IsNullOrWhiteSpace(zap.dat_ofor))
                            {
                                // проверяем корректность даты
                                string formatedDateOfor = Utils.FormatDate(zap.dat_ofor);
                                // Если дата корректна
                                if (!String.IsNullOrWhiteSpace(formatedDateOfor))
                                {
                                    IDataReader reader2;
                                    string whereDatOfor = " and k.dat_ofor >date('" + formatedDateOfor + "')";
                                    ret = ExecRead(conn_db, out reader2, DBManager.SetLimitOffset(" Select k.dat_ofor " +
                                                                                                  " From " + cur_pref + "_data." + my_kart + " k " +
                                                                                                  " where k.nzp_gil =" + zap.nzp_gil +
                                                                                                  " and k.nzp_kvar=" + finder.nzp_kvar +
                                                                                                  " and k.nzp_tkrt=2" +
                                                                                                  " and " + DBManager.sNvlWord + "(neuch" + DBManager.sConvToInt + ",'0' )<>'1'" +
                                                                                                  whereDatOfor + " ORDER BY 1 ", 1, 0), true);
                                    if (!ret.result)
                                    {
                                        conn_db.Close();
                                        return null;
                                    }
                                    if (reader2.Read())
                                    {
                                        zap.dat_vip = Convert.ToString(reader2["dat_ofor"]).Trim();
                                    }
                                    reader2.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (zap.tprp == "В")
                        {
                            zap.type_prop = "временная";
                        }
                        else
                        {
                            zap.type_prop = "постоянная";
                        }


                        if (zap.dat_prop != "")
                            zap.dat_prib = zap.dat_prop;
                        else
                        {
                            IDataReader reader2;

                            if (!ExecRead(conn_db, out reader2, DBManager.SetLimitOffset(
                                                            " Select case when k.dat_prop is not null then k.dat_prop else k.dat_ofor end as my_date" +
                                                            " From " + cur_pref + "_data." + my_kart + " k " +
                                                            " where k.nzp_gil =" + zap.nzp_gil +
                                                            " and k.nzp_kvar=" + finder.nzp_kvar +
                                                            " and k.nzp_tkrt=1" +
                                                            " and " + DBManager.sNvlWord + "(neuch" + DBManager.sConvToInt + ",'0' ) <> '1'" +
                                                            " and case when k.dat_prop is not null then k.dat_prop else k.dat_ofor end < date('" + zap.dat_ofor + "')" +
                                                            " ORDER BY 1 DESC ", 1, 0), true).result)
                            {
                                conn_db.Close();
                                ret.result = false;
                                return null;
                            }

                            if (reader2.Read())
                            {
                                zap.dat_prib = Convert.ToString(reader2["my_date"]).Trim();
                            }
                            reader2.Close();

                        }
                        zap.dat_ubit = Convert.ToString(reader["dat_ubit"]).Trim();
                        zap.dat_vip = zap.dat_ofor;
                    }

                    Spis.Add(zap);
                }

                reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой
                return Spis;
            }
            finally
            {
                conn_db.Close(); //закрыть соединение с основной базой
            }

        }//GetFullInfGilList

        //----------------------------------------------------------------------
        public List<GilPer> GetGilPer(GilPer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }



            List<GilPer> Spis = new List<GilPer>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tab_gil = "_gilper";
            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;


#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
            if (!CasheExists(tXX_cnt))
            {
                ret.result = true;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                conn_web.Close();
                return null;
            }


            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_cnt, conn_web);
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                ret.tag = Convert.ToInt32(s);
            }
            catch (Exception ex)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки периодов убытия " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif
            string orderby = " order by is_actual desc, dat_s, dat_po, nzp_glp";


            //выбрать список
            IDataReader reader;
#if PG
            if (!ExecRead(conn_web, out reader,
                 " Select  *" +
                 " From " + tXX_cnt + orderby + skip, true).result)
#else
            if (!ExecRead(conn_web, out reader,
                 " Select " + skip + " *" +
                 " From " + tXX_cnt + orderby, true).result)
#endif
            {
                conn_web.Close();
                ret.result = false;
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    var zap = new GilPer
                    {
                        num = (i + finder.skip).ToString(),
                        nzp_kvar = CastValue<Int32>(reader["nzp_kvar"]),
                        nzp_gilec = CastValue<string>(reader["nzp_gilec"]).Trim(),
                        dat_s = CastValue<string>(reader["dat_s"]).Trim(),
                        dat_po = CastValue<string>(reader["dat_po"]).Trim(),
                        is_actual = CastValue<string>(reader["is_actual"]).Trim(),
                        sost = CastValue<string>(reader["sost"]).Trim(),
                        osnov = CastValue<string>(reader["osnov"]).Trim(),
                        nzp_glp = CastValue<string>(reader["nzp_glp"]).Trim(),
                        dat_when = CastValue<string>(reader["dat_when"]).Trim(),
                        dat_del = CastValue<string>(reader["dat_del"]).Trim(),
                        nzp_kart = CastValue<string>(reader["nzp_kart"]).Trim(),
                        fam = CastValue<string>(reader["fam"]).Trim(),
                        ima = CastValue<string>(reader["ima"]).Trim(),
                        otch = CastValue<string>(reader["otch"]).Trim(),
                        dat_rog = CastValue<string>(reader["dat_rog"]).Trim(),
                        no_podtv_docs = CastValue<Int32>(reader["no_podtv_docs"]),
                        id_departure_types = CastValue<Int32>(reader["id_departure_types"]),
                        departure_types = CastValue<string>(reader["departure_types"]).Trim(),
                        webUname = CastValue<string>(reader["uname"]).Trim(),
                        pref = CastValue<string>(reader["pref"]).Trim()
                    };
                    zap.podtv_doc_exist = zap.no_podtv_docs == 1 ? "Нет" : "Да";
                    Spis.Add(zap);
                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения периодов убытия " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public void FindGilPer(GilPer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return;
            }
            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                return;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            string tab_gil = "_gilper";


            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;
#if PG
            string tXX_cnt_full = "public." + tXX_cnt;
#else
            string tXX_cnt_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cnt;
#endif


            ExecSQL(conn_web, " Drop table " + tXX_cnt_full, false);

            //создать таблицу webdata:tXX_cnt
#if PG
            ret = ExecSQL(conn_web,
                            " Create table " + tXX_cnt_full +
                                    "(" +
                                    "   nzp_glp integer, " +
                                    "   nzp_kvar INTEGER , " +
                                    "   no_podtv_docs INTEGER , " +
                                    "   id_departure_types INTEGER , " +
                                    "   departure_types char(50) , " +
                                    "   nzp_gilec INTEGER , " +
                                    "   osnov CHAR(100), " +
                                    "   dat_s DATE , " +
                                    "   dat_po DATE , " +
                                    "   is_actual INTEGER , " +
                                    "    sost    char(20)," +
                                    "   dat_when DATE, " +
                                    "   dat_del DATE, " +
                                    "   nzp_user INTEGER, " +
                                    "   uname char(100), " +
                                  "   nzp_kart     integer," +
                                  "   fam     char(40)," +
                                  "   ima     char(40)," +
                                  "   otch     char(40)," +
                                  "   dat_rog     date," +
                                    "   pref     char(20)  " +
                                  " ) ", true);
#else
            ret = ExecSQL(conn_web,
                            " Create table " + tXX_cnt +
                                                "(" +
                                                "   nzp_glp integer, " +
                                                "   nzp_kvar INTEGER , " +
                                                "   nzp_gilec INTEGER , " +
                                                "   id_departure_types INTEGER , " +
                                                "   departure_types char(50) , " +
                                                "   osnov CHAR(100), " +
                                                "   dat_s DATE , " +
                                                "   dat_po DATE , " +
                                                "   is_actual INTEGER , " +
                                                "    sost    char(20)," +
                                                "   dat_when DATE, " +
                                                "   dat_del DATE, " +
                                                "   nzp_user INTEGER, " +
                                                "   uname char(100), " +
                                  "   nzp_kart     integer," +
                                  "   fam     char(40)," +
                                  "   ima     char(40)," +
                                  "   otch     char(40)," +
                                  "   dat_rog     date," +
                                    "   pref     char(20)  " +
                                  " ) ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            StringBuilder sql = new StringBuilder();

#if PG
            sql.Append(" Insert into " + tXX_cnt_full +
                       " ( pref, nzp_glp,  nzp_kvar, nzp_gilec, id_departure_types, departure_types, osnov,  no_podtv_docs, dat_s, dat_po, is_actual,  nzp_user, dat_when, dat_del,sost, nzp_kart, fam,ima,otch,dat_rog, uname)");
            sql.Append(" Select distinct '" + finder.pref + "' as pref, c.nzp_glp,  c.nzp_kvar, c.nzp_gilec, c.id_departure_types, td.type_name departure_types, c.osnov, c.no_podtv_docs, c.dat_s, c.dat_po, c.is_actual,  c.nzp_user, c.dat_when, c.dat_del,");
            sql.Append(" case when c.is_actual<>100 then 'действует' else 'не действует' end,");
            sql.Append(" k.nzp_kart, k.fam, k.ima, k.otch, k.dat_rog,");
            sql.Append(" u.comment");

            sql.Append(" From " + finder.pref + "_data.gil_periods c " +
                " left outer join " + Points.Pref + "_data.s_departure_types td on c.id_departure_types=td.id " +
                " left outer join " + Points.Pref + "_data.users u on c.nzp_user=u.nzp_user, " +
                 finder.pref + "_data.kart k ");
            sql.Append(" where c.nzp_kvar=" + finder.nzp_kvar);
            sql.Append(" and c.nzp_gilec=" + finder.nzp_gilec);
            sql.Append(" and k.nzp_kart=" + finder.nzp_kart);
            sql.Append(" and c.nzp_gilec=k.nzp_gil");

#else
            sql.Append(" Insert into " + tXX_cnt_full +
                       " ( pref, nzp_glp,  nzp_kvar, nzp_gilec, osnov, dat_s, dat_po, is_actual,  nzp_user, dat_when, dat_del,sost, nzp_kart, fam,ima,otch,dat_rog, uname)");
            sql.Append(" Select unique '" + finder.pref + "' as pref, c.nzp_glp,  c.nzp_kvar, c.nzp_gilec, c.osnov, c.dat_s, c.dat_po, c.is_actual,  c.nzp_user, c.dat_when, c.dat_del,");
            sql.Append(" case when c.is_actual<>100 then 'действует' else 'не действует' end,");
            sql.Append(" k.nzp_kart, k.fam, k.ima, k.otch, k.dat_rog,");
            sql.Append(" u.comment");
            sql.Append(" From " + finder.pref + "_data:gil_periods c," + finder.pref + "_data:kart k, outer " + Points.Pref + "_data:users u, outer " + Points.Pref + "_data:s_departure_types td ");
            sql.Append(" where c.nzp_kvar=" + finder.nzp_kvar);
            sql.Append(" and c.nzp_gilec=" + finder.nzp_gilec);
            sql.Append(" and k.nzp_kart=" + finder.nzp_kart);
            sql.Append(" and c.nzp_gilec=k.nzp_gil");
            sql.Append(" and c.nzp_user=u.nzp_user and c.id_departure_types=td.id ");
#endif

            if (finder.dopFind != null)
                if (finder.dopFind.Count > 0) //учесть дополнительные параметры
                {
                    foreach (string s in finder.dopFind)
                    {
                        sql.Append(s);
                    }
                }


            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            conn_db.Close(); //закрыть соединение с основной базой

            //далее работаем с кешем
            //создаем индексы на tXX_cnt


#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
            string ix = "ix" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;
#if PG
            ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_cnt + " (nzp_gilec, nzp_kvar, pref) ", true);
#else
            ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_cnt + " (nzp_gilec, nzp_kvar, pref) ", true);
#endif
            conn_web.Close();

            return;
        }

        //----------------------------------------------------------------------
        public GilPer LoadGilPer(GilPer finder, out Returns ret)
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();

            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }


            IDbConnection conn_web = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;


            //выбрать карточку
#if PG
            string a_data = finder.pref + "_data.";
            string sqlStr =
                           " Select '" + finder.pref + "' as pref , " +
                           " case when is_actual<>100 then 'действует' else 'не действует' end as sost," +
                           " c.*,u.comment, td.type_name as departure_types " +
                           " From " + a_data + "gil_periods c " +
                           " left outer join " + Points.Pref + DBManager.sDataAliasRest + "users u on c.nzp_user=u.nzp_user" +
                           " left outer join " + Points.Pref + "_data" + tableDelimiter + "s_departure_types td on c.id_departure_types=td.id" +
                           " WHERE c.nzp_glp = " + finder.nzp_glp;
#else
            string a_data = finder.pref + "_data:";
            string sqlStr =
                 " Select '" + finder.pref + "' as pref , " +
                 " case when is_actual<>100 then 'действует' else 'не действует' end as sost," +
                 " c.*,u.comment, td.type_name as departure_types " +
                 " From " + a_data + "gil_periods c, outer " + Points.Pref + DBManager.sDataAliasRest + "users u, outer " +Points.Pref + "_data" + tableDelimiter +"s_departure_types " +
                 " WHERE c.nzp_glp = " + finder.nzp_glp +
                 " and c.nzp_user=u.nzp_user and c.id_departure_types=td.id ";
#endif

            IDataReader reader;

            if (!ExecRead(conn_web, out reader, sqlStr, true).result)
            {
                conn_web.Close();
                ret.result = false;
                return null;
            }
            try
            {
                if (!reader.Read())
                {
                    ret.result = false;
                    ret.text = "Период убытия не найден";
                    return null;
                }
                GilPer zap = new GilPer();

                if (reader["nzp_kvar"] == DBNull.Value)
                    zap.nzp_kvar = 0;
                else
                    zap.nzp_kvar = (int)reader["nzp_kvar"];
                if (reader["nzp_gilec"] != DBNull.Value) zap.nzp_gilec = Convert.ToString(reader["nzp_gilec"]).Trim();
                if (reader["dat_s"] != DBNull.Value) zap.dat_s = Convert.ToString(reader["dat_s"]).Trim();
                if (reader["dat_po"] != DBNull.Value) zap.dat_po = Convert.ToString(reader["dat_po"]).Trim();
                if (reader["is_actual"] != DBNull.Value) zap.is_actual = Convert.ToString(reader["is_actual"]).Trim();
                if (reader["sost"] != DBNull.Value) zap.sost = Convert.ToString(reader["sost"]).Trim();
                if (reader["osnov"] != DBNull.Value) zap.osnov = Convert.ToString(reader["osnov"]).Trim();
                if (reader["nzp_glp"] != DBNull.Value) zap.nzp_glp = Convert.ToString(reader["nzp_glp"]).Trim();
                if (reader["departure_types"] != DBNull.Value) zap.departure_types = Convert.ToString(reader["departure_types"]).Trim();
                if (reader["id_departure_types"] != DBNull.Value) zap.id_departure_types = Convert.ToInt32(reader["id_departure_types"]);
                if (reader["no_podtv_docs"] != DBNull.Value) zap.no_podtv_docs = Convert.ToInt32(reader["no_podtv_docs"]);
                if (reader["dat_when"] != DBNull.Value) zap.dat_when = Convert.ToString(reader["dat_when"]).Trim();
                if (reader["dat_del"] != DBNull.Value) zap.dat_del = Convert.ToString(reader["dat_del"]).Trim();
                if (reader["pref"] != DBNull.Value) zap.pref = Convert.ToString(reader["pref"]).Trim();

                if (reader["comment"] != DBNull.Value) zap.webUname = Convert.ToString(reader["comment"]).Trim();

                reader.Close();
                conn_web.Close();
                return zap;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки периода убытия " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        private Returns CheckGilPeriod(IDbConnection conn_db, IDbTransaction transaction, GilPer finder, bool isDeleting)
        {
#if PG
            string a_data = finder.pref + "_data.";
#else
            string a_data = finder.pref + "_data:";
#endif
            string sql;
            Returns ret = Utils.InitReturns();

            if (!isDeleting)
            {
                sql = "select count(*) from " + a_data + "gil_periods a " +
                    " where a.is_actual<>100" +
                    " and nzp_gilec=" + finder.nzp_gilec +
                    " and nzp_kvar=" + finder.nzp_kvar.ToString() +
                    " and a.dat_po>='" + finder.dat_s + "'" +
                    " and a.dat_s<='" + finder.dat_po + "'";

                if (finder.nzp_glp != "")                       // Изменения
                {
                    sql += " and a.nzp_glp<>" + finder.nzp_glp;
                }

                object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                if (!ret.result) return ret;

                if (Convert.ToInt32(obj) > 0)
                {
                    return new Returns(false, " Oбнаружено пересечение периодов временного убытия", -1);
                };
            }

            #region проверка - периоды прибытия
            if (!isDeleting) // не удаление
            {

                ExecSQL(conn_db, transaction, "drop table temp_kart", false);
                ret = ExecSQL(conn_db, transaction,
#if PG
 "create unlogged table temp_kart (nzp_kart integer, nzp_tkrt integer,dat_oprp date, dat_ofor date)",
#else
 "create temp table temp_kart (nzp_kart integer, nzp_tkrt integer,dat_oprp date, dat_ofor date) with no log",
#endif
 true);
                if (!ret.result) return ret;

                sql = "insert into temp_kart" +
                 " select  nzp_kart, nzp_tkrt, dat_oprp, dat_ofor from " + a_data + "kart  " +
                 " Where nzp_gil=" + finder.nzp_gilec +
                 " and   nzp_kvar=" + finder.nzp_kvar +
#if PG
 " and   coalesce(neuch::int,0)=0 ";
#else
 " and   nvl(neuch,0)=0 ";
#endif

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;

                // периоды прибытия
                ExecSQL(conn_db, transaction, "drop table temp_s_po", false);
                ret = ExecSQL(conn_db, transaction,
#if PG
 "create unlogged table temp_s_po (dat_s date, dat_po date )",
#else
 "create temp table temp_s_po (dat_s date, dat_po date ) with no log",
#endif
 true);
                if (!ret.result) return ret;

                string my_date = "01.01.1900";
                string dat_s;
                string dat_po;

                // вытащить первую  карточку жильца в квартире
                sql = "select * from temp_kart a " +
                         " Order by dat_ofor,nzp_tkrt";

                IDataReader reader;

                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;

                if (reader.Read())
                {
                    if ((Convert.ToString(reader["nzp_tkrt"]).Trim() == "2") &&   //если первая карточка убытие
                        (Convert.ToString(reader["dat_ofor"]).Trim() != ""))   //   с датой
                    {
                        // Сохранить период от бесконечности до первого убытия   !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        my_date = Convert.ToDateTime(reader["dat_ofor"]).ToShortDateString();
                        ret = ExecSQL(conn_db, transaction, " Insert into temp_s_po   Values('01.01.1901','" + my_date + "')", true);
                        if (!ret.result)
                        {
                            reader.Close();
                            return ret;
                        }
                    }
                    reader.Close();

                    bool fl_end = false;
                    // а теперь цикл по всем листкам пр/уб для данного ЛС данного жильца
                    while (!fl_end)
                    {
                        // вытащить первую прибытия
#if PG
                        sql =
                            "select *, coalesce(dat_ofor,date('01.01.1901')) as my_date from temp_kart a " +
                                " where nzp_tkrt=1" +
                                " and coalesce(dat_ofor, date('01.01.1901'))>date('" + my_date + "')" +
                                " Order by my_date";
#else
                        sql =
                            "select *, nvl(dat_ofor,date('01.01.1901')) as my_date from temp_kart a " +
                             " where nzp_tkrt=1" +
                             " and nvl(dat_ofor, date('01.01.1901'))>date('" + my_date + "')" +
                             " Order by my_date";
#endif
                        ret = ExecRead(conn_db, transaction, out reader, sql, true);
                        if (!ret.result) return ret;

                        if (!reader.Read()) //DM_kart1.RxQ_temp.isempty
                        {
                            fl_end = true;
                        }
                        else
                        {
                            // вытащить первуе убытие
                            my_date = Convert.ToDateTime(reader["my_date"]).ToShortDateString();
                            reader.Close();
                            dat_s = my_date;
                            sql =
                                "select dat_ofor as my_date, nzp_tkrt from temp_kart a " +
                                 " where nzp_tkrt=2" +
                                 " and dat_ofor>'" + my_date + "'" +
                                 " Union " +
                                 " select dat_oprp as my_date, nzp_tkrt from temp_kart a " +
                                 " where nzp_tkrt=1" +
                                 " and dat_oprp>'" + my_date + "'" +
                                 " Order by 1, 2 desc";
                            ret = ExecRead(conn_db, transaction, out reader, sql, true);
                            if (!ret.result) return ret;
                            if (!reader.Read())
                            {
                                fl_end = true;
                                dat_po = "01.01.3000";
                            }
                            else
                            {
                                my_date = Convert.ToDateTime(reader["my_date"]).ToShortDateString();

                                if (my_date != "") dat_po = my_date;
                                else dat_po = "01.01.3000";
                            }
                            reader.Close();
                            // Сохранить период    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            sql = " Insert into temp_s_po (   DAT_S,DAT_PO  ) " +
                                " Values('" + dat_s + "','" + dat_po + "')";
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            if (!ret.result) return ret;
                        }
                    }

                }
                reader.Close();
                ExecSQL(conn_db, transaction, "drop table temp_kart", false);

                sql = "select count(*) from temp_s_po b" +
                    " where '" + finder.dat_s + "' between b.dat_s and b.dat_po" +
                    " and   '" + finder.dat_po + "'  between b.dat_s and b.dat_po";

                object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                if (!ret.result) return ret;
                ExecSQL(conn_db, transaction, "drop table temp_s_po", false);

                if (Convert.ToInt32(obj) == 0)
                {
                    return new Returns(false, "Oбнаружено пересечение периодов временного и постоянного убытия", -1);
                };
            }
            #endregion

            return ret;
        }

        private GilPer SaveGilPerForSelectedCards(GilPer gilPer, bool isDeleting, out Returns ret)
        {
            #region Проверка входных параметров
            if (gilPer.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не задан");
                return null;
            }
            if (gilPer.pref == "")
            {
                ret = new Returns(false, "Префикс базы данных не задан");
                return null;
            }
            if (gilPer.nzp_kvar < 1)
            {
                ret = new Returns(false, "Не задан лицевой счет");
                return null;
            }
            if (gilPer.is_actual == "100")
            {
                ret = new Returns(false, "Архивные данные нельзя редактировать", -1);
                return null;
            }
            DateTime datS = DateTime.MinValue;
            if (!DateTime.TryParse(gilPer.dat_s, out datS))
            {
                ret = new Returns(false, "Неверная дата начала периода");
                return null;
            }
            DateTime datPo = DateTime.MinValue;
            if (!DateTime.TryParse(gilPer.dat_po, out datPo))
            {
                ret = new Returns(false, "Неверная дата окончания периода");
                return null;
            }
            DateTime begin_date_is_lgt = new DateTime(2005, 1, 1);

            if (!isDeleting && datS < begin_date_is_lgt)
            {
                ret = new Returns(false, "Дата начала периода должна быть не ранее " + begin_date_is_lgt.ToShortDateString(), -1);
                return null;
            }
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }



            //определение локального пользователя
#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + gilPer.pref + "_data'", true);
#else
            ret = ExecSQL(conn_db, "database " + gilPer.pref + "_data", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return null;
            }

            #region определение локального пользователя
            int nzpUser = gilPer.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, gilPer, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_web.Close();
                conn_db.Close();
                return null;
            }*/
            #endregion

#if PG
            ExecSQLThr(conn_db, "set search_path to '" + gilPer.pref + "_kernel'", true);
#else
            ExecSQLThr(conn_db, "database " + gilPer.pref + "_kernel", true);
#endif


#if PG
            string a_data = gilPer.pref + "_data.";
#else
            string a_data = gilPer.pref + "_data:";
#endif
            string tXX_gilkvar = "t" + gilPer.nzp_user + "_gilkvar";
            string tXX_gilkvar_full = "";
#if PG
            tXX_gilkvar_full = "public." + tXX_gilkvar;
#else
            tXX_gilkvar_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_gilkvar;
#endif

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            string sql = "select nzp_gil from " + tXX_gilkvar_full + " where nzp_kvar = " + gilPer.nzp_kvar + " and pref = " + Utils.EStrNull(gilPer.pref) + " and is_checked = 1";
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                conn_db.Close();
                return null;
            }

            try
            {
                bool key = false;
                while (reader.Read())
                {
                    if (reader["nzp_gil"] != DBNull.Value) gilPer.nzp_gilec = reader["nzp_gil"].ToString();
                    else gilPer.nzp_gilec = "";

                    if (gilPer.nzp_gilec == "") throw new Exception("Поле nzp_gil в таблице " + tXX_gilkvar + " не задано");

                    ret = CheckGilPeriod(conn_db, transaction, gilPer, isDeleting);
                    if (!ret.result) throw new Exception("");

#if PG
                    sql = "insert into  " + a_data + "gil_periods (nzp_glp, nzp_tkrt, nzp_kvar, nzp_gilec, is_sn, is_lgt, osnov, dat_s, dat_po, is_actual,  dat_when, nzp_user)" +
                            " values(default, 2, " + gilPer.nzp_kvar + "," + gilPer.nzp_gilec + ", 0, 1," +
                            Utils.EStrNull(gilPer.osnov, "") + ", '" + datS.ToShortDateString() + "', '" + datPo.ToShortDateString() +
                        "', 1, current_date," +
#else
                    sql = "insert into  " + a_data + "gil_periods (nzp_glp, nzp_tkrt, nzp_kvar, nzp_gilec, is_sn, is_lgt, osnov, dat_s, dat_po, is_actual,  dat_when, nzp_user)" +
                       " values(0, 2, " + gilPer.nzp_kvar + "," + gilPer.nzp_gilec + ", 0, 1," +
                       Utils.EStrNull(gilPer.osnov, "") + ", '" + datS.ToShortDateString() + "', '" + datPo.ToShortDateString() +
                   "', 1, today," +
#endif
 nzpUser + ")";

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception();

                    sql = "insert into  " + a_data + "pere_gilec(nzp_kvar, dat_s, dat_po) values(" + gilPer.nzp_kvar + ",'" + datS.ToShortDateString() + "','" + datPo.ToShortDateString() + "')";

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception();

                    key = true;
                }

                if (key && Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                {
                    ret = SaveMustCalc(conn_db, transaction, new EditInterDataMustCalc()
                    {
                        pref = gilPer.pref,
                        nzp_kvar = gilPer.nzp_kvar,
                        nzp_user = gilPer.nzp_user,
                        webLogin = gilPer.webLogin,
                        webUname = gilPer.webUname,
                        comment_action = gilPer.comment_action
                    });
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                conn_db.Close();
                if (ex.Message != "") ret = new Returns(false, ex.Message);
                return null;
            }

            reader.Close();
            if (transaction != null) transaction.Commit();
            conn_web.Close();
            conn_db.Close();
            return null;
        }

        //----------------------------------------------------------------------
        public GilPer SaveGilPer(GilPer old_gilper, GilPer new_gilper, bool delete_flag, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (Utils.GetParams(new_gilper.prms, Constants.page_add_period_ub_to_selected))
                return SaveGilPerForSelectedCards(new_gilper, delete_flag, out ret);

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------            
            ret = Utils.InitReturns();

            if (new_gilper.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            string begin_date_is_lgt = "01.01.2005"; // старт без is_sn is_lgt

            string sql = "";

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);

            if (!ret.result) return null;
#if PG
            string a_data = new_gilper.pref + "_data.";
#else
            string a_data = new_gilper.pref + "_data:";
#endif

            #region проверки
            //-проверки-------------------------------------------------------------

            if (new_gilper.is_actual == "100")
            {
                ret.text = "Архивные данные нельзя редактировать.";
                ret.tag = -1;
                ret.result = false;
                conn_db.Close();
                return new_gilper;
            }

            if ((!delete_flag)
            && (Convert.ToDateTime(new_gilper.dat_s) < Convert.ToDateTime(begin_date_is_lgt)))
            {

                ret.text = "Данные слишком старые.";
                ret.tag = -1;
                ret.result = false;
                conn_db.Close();

                return new_gilper;
            }

            if ((!delete_flag)
            && (new_gilper.nzp_glp != "")                       // Изменения
            && (old_gilper.osnov.Trim() == new_gilper.osnov.Trim())           // нет изменений
            && (old_gilper.dat_s.Trim() == new_gilper.dat_s.Trim())
             && (old_gilper.no_podtv_docs == new_gilper.no_podtv_docs)
            && (old_gilper.dat_po.Trim() == new_gilper.dat_po.Trim())
                && (old_gilper.id_departure_types == new_gilper.id_departure_types)
                )
            {
                ret.text = "Нет изменений";
                ret.tag = -1;
                ret.result = false;
                conn_db.Close();

                return new_gilper;
            }
            if (
                ((new_gilper.nzp_gilec == "") || (new_gilper.nzp_kvar.ToString() == ""))
                && (new_gilper.nzp_glp == "")
               )
            {
                ret.text = "Код периода задан(добавление?), а код жильца или квартиры не задан.";
                ret.result = false;
                ret.tag = -1;
                conn_db.Close();
                return null;
            }


            if (!delete_flag)
            {
                sql = "select count(*) from " + a_data + "gil_periods a " +
                    " where a.is_actual<>100" +
                    " and nzp_gilec=" + new_gilper.nzp_gilec +
                    " and nzp_kvar=" + new_gilper.nzp_kvar.ToString() +
                    " and a.dat_po>='" + new_gilper.dat_s + "'" +
                    " and a.dat_s<='" + new_gilper.dat_po + "'" +
                    " and " + sNvlWord + "(a.no_podtv_docs,0)=" + new_gilper.no_podtv_docs;

                if (new_gilper.nzp_glp != "")                       // Изменения
                {
                    sql += " and a.nzp_glp<>" + new_gilper.nzp_glp;
                }

                if (Convert.ToInt32(ExecScalarThr(conn_db, sql, out ret, true)) > 0)
                {
                    conn_db.Close();
                    ret.text = " Oбнаружено пересечение периодов временного убытия.";
                    ret.result = false;
                    ret.tag = -1;
                    return null;
                };
            }


            #endregion
            #region проверка  - периоды прибытия
            //----------------------------------------------------------------------
            if (!delete_flag) // не удаление
            {

                ExecSQL(conn_db, "drop table temp_kart", false);
#if PG
                sql = "create unlogged table temp_kart (nzp_kart integer, nzp_tkrt integer,dat_oprp date, dat_ofor date)";
#else
                sql = "create temp table temp_kart (nzp_kart integer, nzp_tkrt integer,dat_oprp date, dat_ofor date) with no log";
#endif
                ExecSQLThr(conn_db, sql, true);

                sql = "insert into temp_kart" +
                 " select  nzp_kart, nzp_tkrt, dat_oprp, dat_ofor from " + a_data + "kart  " +
                 " Where nzp_gil=" + new_gilper.nzp_gilec +
                 " and   nzp_kvar=" + new_gilper.nzp_kvar.ToString() +
#if PG
 " and   coalesce(neuch::int,0)=0 ";
#else
 " and   nvl(neuch,0)=0 ";
#endif

                ExecSQLThr(conn_db, sql, true);

                // периоды прибытия
                ExecSQL(conn_db, "drop table temp_s_po", false);
#if PG
                sql = "create unlogged table temp_s_po (dat_s date, dat_po date )";
#else
                sql = "create temp table temp_s_po (dat_s date, dat_po date ) with no log";
#endif
                ExecSQLThr(conn_db, sql, true);

                string my_date = "01.01.1900";
                string dat_s;
                string dat_po;

                // вытащить первую  карточку жильца в квартире
                sql = "select * from temp_kart a " +
                         " Order by dat_ofor,nzp_tkrt";

                IDataReader reader;

                ExecReadThr(conn_db, out reader, sql, true);
                if (reader.Read())
                {
                    if ((Convert.ToString(reader["nzp_tkrt"]).Trim() == "2")   //если первая карточка убытие
                    && (Convert.ToString(reader["dat_ofor"]).Trim() != ""))   //   с датой
                    {
                        // Сохранить период от бесконечности до первого убытия   !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        my_date = Convert.ToString(reader["dat_ofor"]).Trim();
                        ExecSQLThr(conn_db, " Insert into temp_s_po   Values('01.01.1901','" + my_date + "')", true);
                    }

                    bool fl_end = false;
                    // а теперь цикл по всем листкам пр/уб для данного ЛС данного жильца
                    while (!fl_end)
                    {
                        // вытащить первую прибытия
#if PG
                        sql = "select *, coalesce(dat_ofor,date('01.01.1901')) as my_date from temp_kart a " +
                            " where nzp_tkrt=1" +
                            " and coalesce(dat_ofor, date('01.01.1901'))>date('" + my_date + "')" +
                            " Order by my_date";
#else
                        sql =
                           "select *, nvl(dat_ofor,date('01.01.1901')) as my_date from temp_kart a " +
                            " where nzp_tkrt=1" +
                            " and nvl(dat_ofor, date('01.01.1901'))>date('" + my_date + "')" +
                            " Order by my_date";
#endif
                        ExecReadThr(conn_db, out reader, sql, true);

                        if (!reader.Read()) //DM_kart1.RxQ_temp.isempty
                            fl_end = true;
                        else
                        {
                            // вытащить первуе убытие
                            my_date = Convert.ToString(reader["my_date"]).Trim();
                            dat_s = my_date;
                            sql =
                                "select dat_ofor as my_date, nzp_tkrt from temp_kart a " +
                                 " where nzp_tkrt=2" +
                                 " and dat_ofor>'" + my_date + "'" +
                                 " Union " +
                                 " select dat_oprp as my_date, nzp_tkrt from temp_kart a " +
                                 " where nzp_tkrt=1" +
                                 " and dat_oprp>'" + my_date + "'" +
                                 " Order by 1, 2 desc";
                            ExecReadThr(conn_db, out reader, sql, true);
                            if (!reader.Read())
                            {
                                fl_end = true;
                                dat_po = "01.01.3000";
                            }
                            else
                            {
                                my_date = Convert.ToString(reader["my_date"]).Trim();

                                if (my_date != "") dat_po = my_date;
                                else dat_po = "01.01.3000";


                            }
                            // Сохранить период    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            sql = " Insert into temp_s_po (   DAT_S,DAT_PO  ) " +
                                " Values('" + dat_s + "','" + dat_po + "')";
                            ExecSQLThr(conn_db, sql, true);
                        }
                    }

                }
                sql = "select count(*) from temp_s_po b" +
                    " where '" + new_gilper.dat_s + "' between b.dat_s and b.dat_po" +
                    " and   '" + new_gilper.dat_po + "'  between b.dat_s and b.dat_po";

                if (Convert.ToInt32(ExecScalarThr(conn_db, sql, out ret, true)) == 0)
                {
                    reader.Close();
                    conn_db.Close();
                    ret.text = "Oбнаружено пересечение периодов временного и постоянного убытия.";
                    ret.result = false;
                    ret.tag = -1;
                    return null;
                };
                reader.Close();
            }
            //----------------------------------------------------------------------
            #endregion


            // Запись в БД==================================================================================================            
            if (delete_flag)    //Удаление
            {
#if PG
                sql = "update " + a_data + "gil_periods set is_actual = 100,dat_del=current_date " +
                                      " where nzp_glp=" + new_gilper.nzp_glp;
#else
                sql = "update " + a_data + "gil_periods set is_actual = 100,dat_del=today " +
                      " where nzp_glp=" + new_gilper.nzp_glp;
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка удаления периода временного убытия";
                    ret.tag = -1;
                    conn_db.Close();
                    return null;
                }

                ExecSQLThr(conn_db, "insert into  " + a_data + "pere_gilec(nzp_kvar, dat_s, dat_po) values(" +
                                   new_gilper.nzp_kvar + ",'" + new_gilper.dat_s + "','" + new_gilper.dat_po + "')", true);

                if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                {
                    ret = SaveMustCalc(conn_db, null, new EditInterDataMustCalc()
                    {
                        pref = new_gilper.pref,
                        nzp_kvar = new_gilper.nzp_kvar,
                        nzp_user = new_gilper.nzp_user,
                        webLogin = new_gilper.webLogin,
                        webUname = new_gilper.webUname,
                        comment_action = new_gilper.comment_action
                    });
                }

                conn_db.Close();
                return new_gilper;          // ВЫХОД
            }

            if (new_gilper.nzp_glp != "") // Изменение
            {
                if ((old_gilper.osnov.Trim() != new_gilper.osnov.Trim() || // изменилось только основание
                    old_gilper.no_podtv_docs != -1 ||
                    old_gilper.id_departure_types != new_gilper.id_departure_types
                )
                && (old_gilper.dat_s.Trim() == new_gilper.dat_s.Trim())
                && (old_gilper.dat_po.Trim() == new_gilper.dat_po.Trim()))
                {
                    sql = "update  " + a_data + "gil_periods set " +
                        " osnov='" + new_gilper.osnov + "'," +
                        " no_podtv_docs=" + new_gilper.no_podtv_docs + ", " +
                        " id_departure_types = " + new_gilper.id_departure_types +
                        " where nzp_glp=" + new_gilper.nzp_glp;

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения основания временного убытия";
                        ret.tag = -1;
                        conn_db.Close();
                        return null;
                    }
                    conn_db.Close();
                    return new_gilper;          // ВЫХОД
                }
#if PG
                sql = "update  " + a_data + "gil_periods set (" +
                                   "  is_actual, dat_del" +
                                   ") = (" +
                                   " 100,  current_date" +
                                        ")" +
                                   " where nzp_glp=" + new_gilper.nzp_glp;
#else
                sql = "update  " + a_data + "gil_periods set (" +
                    "  is_actual, dat_del" +
                    ") = (" +
                    " 100,  today" +
                         ")" +
                    " where nzp_glp=" + new_gilper.nzp_glp;
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения периода временного убытия";
                    ret.tag = -1;
                    conn_db.Close();
                    return null;
                }

                ExecSQLThr(conn_db, "insert into  " + a_data + "pere_gilec(nzp_kvar, dat_s, dat_po) values(" +
                                   old_gilper.nzp_kvar + ",'" + old_gilper.dat_s + "','" + old_gilper.dat_po + "')", true);

                if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                {
                    ret = SaveMustCalc(conn_db, null, new EditInterDataMustCalc()
                    {
                        pref = new_gilper.pref,
                        nzp_kvar = old_gilper.nzp_kvar,
                        nzp_user = old_gilper.nzp_user,
                        webLogin = old_gilper.webLogin,
                        webUname = old_gilper.webUname,
                        comment_action = new_gilper.comment_action
                    });
                }
            }

            //определение локального пользователя
#if PG
            ExecSQLThr(conn_db, "set search_path to '" + new_gilper.pref + "_data'", true);
#else
            ExecSQLThr(conn_db, "database " + new_gilper.pref + "_data", true);
#endif

            #region определение локального пользователя
            /*DbWorkUser db = new DbWorkUser();
            db.GetLocalUser(conn_db, new_gilper, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }*/
            #endregion

#if PG
            ExecSQLThr(conn_db, "set search_path to '" + new_gilper.pref + "_kernel'", true);
#else
            ExecSQLThr(conn_db, "database " + new_gilper.pref + "_kernel", true);
#endif

            sql = "insert into  " + a_data + "gil_periods (" +
                "  nzp_glp, nzp_tkrt, nzp_kvar, nzp_gilec, is_sn, is_lgt," +
                " osnov, dat_s, dat_po, is_actual,  dat_when, nzp_user, no_podtv_docs, id_departure_types" +
                ") values(" +
#if PG
 " default, 2, " +
#else
 " 0, 2, " +
#endif
 new_gilper.nzp_kvar.ToString() + "," + new_gilper.nzp_gilec + ", 0, 1," +
                "'" + new_gilper.osnov + "', '" + new_gilper.dat_s + "', '" + new_gilper.dat_po +
#if PG
 "', 1, current_date," +
#else
 "', 1, today," +
#endif
 new_gilper.nzp_user + //!!!!!!
                "," + new_gilper.no_podtv_docs + ", " + new_gilper.id_departure_types + ")";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка вставки периода временного убытия";
                ret.tag = -1;
                conn_db.Close();
                return null;
            }
            new_gilper.nzp_glp = Convert.ToString(GetSerialValue(conn_db));

            UpdateOldRecords(conn_db, a_data + "pere_gilec", new_gilper.nzp_kvar);
            ExecSQLThr(conn_db, "insert into  " + a_data + "pere_gilec(nzp_kvar, dat_s, dat_po) values(" +
                                   new_gilper.nzp_kvar + ",'" + new_gilper.dat_s + "','" + new_gilper.dat_po + "')", true);

            if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
            {
                ret = SaveMustCalc(conn_db, null, new EditInterDataMustCalc()
                {
                    pref = new_gilper.pref,
                    nzp_kvar = new_gilper.nzp_kvar,
                    nzp_user = new_gilper.nzp_user,
                    webLogin = new_gilper.webLogin,
                    webUname = new_gilper.webUname,
                    comment_action = new_gilper.comment_action
                });
            }

            conn_db.Close();
            return new_gilper;
        }

        public Returns SaveDepartureType(Sprav finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);
            StringBuilder sql = new StringBuilder();
            Returns ret;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            MyDataReader reader;

            if (finder.nzp_sprav != "")
            {
                if (finder.dop_kod == "delete")
                {
                    //проверить ссылки
                    foreach (_Point p in Points.PointList)
                    {
                        if (p.pref == Points.Pref) continue;
                        sql.Remove(0, sql.Length);
                        sql.AppendFormat("select 1 from {0}_data{1}gil_periods where id_departure_types = {2}", p.pref, tableDelimiter, finder.nzp_sprav);
                        ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            return ret;
                        }
                        if (reader.Read())
                        {
                            conn_db.Close();
                            return new Returns(false, "Запись удалить нельзя, т.к. на нее есть ссылки из других таблиц", -1);
                        }
                    }

                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("delete from {0}_data{1}s_departure_types where id = {2}", Points.Pref, tableDelimiter, finder.nzp_sprav);
                }
                else //update
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("update {0}_data{1}s_departure_types set type_name = '{2}' where id  = {3}", Points.Pref, tableDelimiter, finder.val_sprav, finder.nzp_sprav);
                }
            }
            else // insert
            {
                sql.Remove(0, sql.Length);
                sql.AppendFormat("insert into {0}_data{1}s_departure_types (type_name) values ('{2}')", Points.Pref, tableDelimiter, finder.val_sprav);
            }
            ret = ExecSQL(conn_db, sql.ToString(), true);
            conn_db.Close();
            return ret;
        }

        public Returns SavePlaceRequirement(Sprav finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);
            StringBuilder sql = new StringBuilder();
            Returns ret;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            //MyDataReader reader;

            if (finder.nzp_sprav != "")
            {
                if (finder.dop_kod == "delete")
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("delete from {0}_data{1}s_place_requirement where id = {2}", Points.Pref, tableDelimiter, finder.nzp_sprav);
                }
                else //update
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("update {0}_data{1}s_place_requirement set place = '{2}' where id  = {3}", Points.Pref, tableDelimiter, finder.val_sprav, finder.nzp_sprav);
                }
            }
            else // insert
            {
                sql.Remove(0, sql.Length);
                sql.AppendFormat("insert into {0}_data{1}s_place_requirement (place) values ('{2}')", Points.Pref, tableDelimiter, finder.val_sprav);
            }
            ret = ExecSQL(conn_db, sql.ToString(), true);
            conn_db.Close();
            return ret;
        }

        //----------------------------------------------------------------------
        public List<Sobstw> GetSobstw(Sobstw finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }

            List<Sobstw> Spis = new List<Sobstw>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            string tab_gil = "_sobstw";
            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;


            if (!CasheExists(tXX_cnt))
            {
                ret.result = true;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                conn_web.Close();
                return null;
            }


            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_cnt, conn_web);
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                ret.tag = Convert.ToInt32(s);
            }
            catch (Exception ex)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки собственников " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif
            string orderby = " order by is_actual desc, fam, ima, otch";


            //выбрать список
            MyDataReader reader;//, reader2;
#if PG
            if (!ExecRead(conn_web, out reader,
                            " Select  *" +
                            " From " + tXX_cnt + orderby + skip, true).result)
#else
            if (!ExecRead(conn_web, out reader,
                                       " Select " + skip + " *" +
                                       " From " + tXX_cnt + orderby, true).result)
#endif
            {
                conn_web.Close();
                ret.result = false;
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Sobstw zap = new Sobstw();

                    zap.num = (i + finder.skip).ToString();

                    if (reader["nzp_kvar"] == DBNull.Value)
                        zap.nzp_kvar = 0;
                    else
                        zap.nzp_kvar = (int)reader["nzp_kvar"];

                    if (reader["num_doc"] == DBNull.Value)
                        zap.num_doc = 0;
                    else
                        zap.num_doc = (int)reader["num_doc"];

                    zap.nzp_sobstw = Convert.ToString(reader["nzp_sobstw"]).Trim();

                    zap.nzp_gil = Convert.ToString(reader["nzp_gil"]).Trim();
                    zap.nzp_rod = Convert.ToString(reader["nzp_rod"]).Trim();
                    zap.rod = Convert.ToString(reader["rod"]).Trim();

                    zap.fam = Convert.ToString(reader["fam"]).Trim();
                    zap.ima = Convert.ToString(reader["ima"]).Trim();
                    zap.otch = Convert.ToString(reader["otch"]).Trim();
                    zap.fio = zap.fam + " " + zap.ima + " " + zap.otch;
                    zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();

                    zap.adress = Convert.ToString(reader["adress"]).Trim();
                    zap.dop_info = Convert.ToString(reader["dop_info"]).Trim();

                    zap.nzp_dok = Convert.ToString(reader["nzp_dok"]).Trim();
                    zap.dok = Convert.ToString(reader["dok"]).Trim();
                    zap.serij = Convert.ToString(reader["serij"]).Trim();
                    zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                    zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                    zap.dolya = Convert.ToString(reader["dolya"]).Trim();
                    zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();

                    zap.is_actual = Convert.ToString(reader["is_actual"]).Trim();
                    zap.sost = Convert.ToString(reader["sost"]).Trim();

                    zap.webUname = Convert.ToString(reader["uname"]).Trim();

                    zap.pref = Convert.ToString(reader["pref"]).Trim();



                    Spis.Add(zap);

                    //if (i >= finder.rows) break;

                }

                reader.Close();
                conn_web.Close();
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения собственников " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public void FindSobstw(Sobstw finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return;
            }
            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            string tab_gil = "_sobstw";

            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;

            ExecSQL(conn_web, " Drop table " + tXX_cnt, false);

            //создать таблицу webdata:tXX_sobstw
#if PG
            ret = ExecSQL(conn_web,
                            " Create table " + tXX_cnt +
                                    "(" +
                                    "   nzp_sobstw SERIAL NOT NULL, " +
                                    "   nzp_kvar INTEGER NOT NULL, " +
                                    "   nzp_gil INTEGER , " +
                                    "   nzp_rod INTEGER, " +
                                    "   rod      char(30), " +
                                    "   fam CHAR(40), " +
                                    "   ima CHAR(40), " +
                                    "   otch CHAR(40), " +
                                    "   dat_rog DATE, " +
                                    "   adress CHAR(60), " +
                                    "   dop_info CHAR(60), " +
                                    "   nzp_dok INTEGER, " +
                                    "   dok char(30), " +
                                    "   serij CHAR(10), " +
                                    "   nomer CHAR(7), " +
                                    "   vid_mes CHAR(70), " +
                                    "   vid_dat DATE, " +
                                    "   dolya CHAR(100), " +
                                    "   is_actual INTEGER default 1 NOT NULL, " +
                                    "   sost char(20), " +
                                    "   num_doc INTEGER, " +
                                    "   nzp_user INTEGER, " +
                                    "   uname char(100), " +
                                    "   pref     char(20)  " +
                                  " ) ", true);
#else
            ret = ExecSQL(conn_web,
                            " Create table " + tXX_cnt +
                                                "(" +
                                                "   nzp_sobstw SERIAL NOT NULL, " +
                                                "   nzp_kvar INTEGER NOT NULL, " +
                                                "   nzp_gil INTEGER , " +
                                                "   nzp_rod INTEGER, " +
                                                "   rod      char(30), " +
                                                "   fam CHAR(40), " +
                                                "   ima CHAR(40), " +
                                                "   otch CHAR(40), " +
                                                "   dat_rog DATE, " +
                                                "   adress CHAR(60), " +
                                                "   dop_info CHAR(60), " +
                                                "   nzp_dok INTEGER, " +
                                                "   dok char(30), " +
                                                "   serij CHAR(10), " +
                                                "   nomer CHAR(7), " +
                                                "   vid_mes CHAR(70), " +
                                                "   vid_dat DATE, " +
                                                "   dolya CHAR(100), " +
                                                "   num_doc INTEGER, " +
                                                "   is_actual INTEGER default 1 NOT NULL, " +
                                                "   sost char(20), " +
                                                "   nzp_user INTEGER, " +
                                                "   uname char(100), " +
                                    "   pref     char(20)  " +
                                  " ) ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
#if PG
            string tXX_cnt_full = "public." + tXX_cnt;
#else
            string tXX_cnt_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cnt;
#endif
            StringBuilder sql = new StringBuilder();
            StringBuilder where = new StringBuilder(" 1=1 ");
            where.AppendFormat(" and c.nzp_kvar={0}", finder.nzp_kvar);
            if (finder.dopFind != null)
                if (finder.dopFind.Count > 0) //учесть дополнительные параметры
                {
                    foreach (string s in finder.dopFind)
                    {
                        where.Append(s);
                    }
                }

#if PG
            sql.Append(" Insert into " + tXX_cnt_full + "(" +
                                           " pref,nzp_sobstw,nzp_kvar ,nzp_gil ,nzp_rod ,rod,fam,ima ,otch ,dat_rog ," +
                                           " adress ,dop_info,nzp_dok ,dok ,serij ,nomer ,vid_mes,vid_dat," +
                                           " is_actual, sost" +
                                           ")" +
                                           " SELECT '" + finder.pref + "' as pref," +
                                           " c.nzp_sobstw," +
                                           " c.nzp_kvar ," +
                                           " c.nzp_gil ," +
                                           " r.nzp_rod ,r.rod," +
                                           " c.fam, c.ima , c.otch ,c.dat_rog ," +
                                           " c.adress ,c.dop_info," +
                                           " d.nzp_dok ,d.dok ,c.serij ,c.nomer ,c.vid_mes,c.vid_dat," +
                                           " c.is_actual," +
                                           " case when c.is_actual=1 then 'действует' else 'не действует' end " +
                                          " from " + finder.pref + "_data.sobstw c " +
                                          " left outer join " + finder.pref + "_data.s_rod r on c.nzp_rod=r.nzp_rod " +
                                          " left outer join " + finder.pref + "_data.s_dok d on c.nzp_dok=d.nzp_dok" +
                                          " where " + where.ToString());
#else
            sql.Append(" Insert into " + tXX_cnt_full + "(" +
                                           " pref,nzp_sobstw,nzp_kvar ,nzp_gil ,nzp_rod ,rod,fam,ima ,otch ,dat_rog ," +
                                           " adress ,dop_info,nzp_dok ,dok ,serij ,nomer ,vid_mes,vid_dat," +                                        
                                           " is_actual, sost" +
                                           ")" +
                                           " SELECT '" + finder.pref + "' as pref," +
                                           " c.nzp_sobstw," +
                                           " c.nzp_kvar ," +
                                           " c.nzp_gil ," +
                                           " r.nzp_rod ,r.rod," +
                                           " c.fam, c.ima , c.otch ,c.dat_rog ," +
                                           " c.adress ,c.dop_info," +
                                           " d.nzp_dok ,d.dok ,c.serij ,c.nomer ,c.vid_mes,c.vid_dat," +                                       
                                           " c.is_actual," +
                                                      " case when c.is_actual=1 then 'действует' else 'не действует' end " +
                                          " from " + finder.pref + "_data:sobstw c, OUTER " + finder.pref + "_data:s_rod r, " +
                                          " OUTER " + finder.pref + "_data:s_dok d " +
                                          " where " + where.ToString() +
                                          " and c.nzp_dok=d.nzp_dok" +
                                          " and c.nzp_rod=r.nzp_rod");
#endif





            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            MyDataReader reader, reader2;
            sql.Remove(0, sql.Length);
            sql.AppendFormat("select nzp_sobstw from " + finder.pref + "_data" + tableDelimiter + "sobstw c where " + where.ToString());
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            while (reader.Read())
            {
                int nzp_sobstw = 0;
                if (reader["nzp_sobstw"] != DBNull.Value) nzp_sobstw = Convert.ToInt32(reader["nzp_sobstw"]);

                sql.Remove(0, sql.Length);
                sql.AppendFormat("select dolya_up, dolya_down from " + finder.pref + "_data" + tableDelimiter + "doc_sobstw where nzp_sobstw = " + nzp_sobstw);
                ret = ExecRead(conn_web, out reader2, sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
                List<DocSobstw> list = new List<DocSobstw>();
                while (reader2.Read())
                {
                    DocSobstw ds = new DocSobstw();
                    if (reader2["dolya_up"] != DBNull.Value) ds.dolya_up = Convert.ToInt32(reader2["dolya_up"]);
                    if (reader2["dolya_down"] != DBNull.Value) ds.dolya_down = Convert.ToInt32(reader2["dolya_down"]);
                    list.Add(ds);
                }
                reader2.Close();
                string dolya = "0";
                if (list.Count > 0)
                {
                    DocSobstw doc = GetSumDolya(list);

                    if (doc != null)
                    {
                        if (doc.dolya_up > doc.dolya_down)
                        {
                            int o1 = doc.dolya_up / doc.dolya_down;
                            int o2 = doc.dolya_down * o1;
                            int o3 = doc.dolya_up - o2;
                            dolya = o1.ToString() + " " + o3.ToString() + "/" + doc.dolya_down;
                        }
                        else
                        {
                            dolya = doc.dolya_up.ToString() + "/" + doc.dolya_down.ToString();
                            if (doc.dolya_down > 0)
                                if (doc.dolya_up % doc.dolya_down == 0) dolya = doc.dolya_up.ToString();
                        }
                    }
                }
                //dolya = dolya;

                sql.Remove(0, sql.Length);
                sql.AppendFormat("update {0} set dolya ='{1}', num_doc= {2} where nzp_sobstw = {3} and pref='{4}'", tXX_cnt_full, dolya, list.Count, nzp_sobstw, finder.pref);
                ret = ExecSQL(conn_web, sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }

            conn_db.Close(); //закрыть соединение с основной базой

            //далее работаем с кешем
            //создаем индексы на tXX_cnt
            string ix = "ix" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;

            ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_cnt + " (nzp_sobstw, pref) ", true);

            conn_web.Close();

            return;
        }

        private DocSobstw GetSumDolya(List<DocSobstw> list)
        {
            if (list == null) return null;
            Int32 ob_znam = 1;
            Int32 sum_chis = 0;

            foreach (DocSobstw ds in list)
                if (ds.dolya_down > 0) ob_znam = ob_znam * ds.dolya_down;

            foreach (DocSobstw ds in list)
            {
                if (ds.dolya_up > 0 && ds.dolya_down > 0) sum_chis = sum_chis + ds.dolya_up * ob_znam / ds.dolya_down;
            }

            DocSobstw dcs = new DocSobstw();
            dcs.dolya_down = ob_znam;
            dcs.dolya_up = sum_chis;
            //GKHKPFIVE-9982 Не сокращать доли в домовой книге (в паспортистке)
            if (list.Count > 1) dcs = ToReduce(dcs);
            return dcs;
        }

        private DocSobstw ToReduce(DocSobstw ds)
        {
            int nod = STCLINE.KP50.Utility.MathUtility.Nod(ds.dolya_up, ds.dolya_down);
            if (nod != 0)
            {
                ds.dolya_up /= nod;
                ds.dolya_down /= nod;
            }
            return ds;
        }

        //----------------------------------------------------------------------
        public Sobstw LoadSobstw(Sobstw finder, out Returns ret)
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();
            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;


            //выбрать карточку
#if PG
            string a_data = finder.pref + "_data.";
            string sqlStr =
                                " SELECT '" + finder.pref + "' as pref," +
                                " case when is_actual=1 then 'действует' else 'не действует' end as sost," +
                                "*" +
                                " from " + a_data + "sobstw c " +
                                " left outer join " + a_data + "s_rod r on c.nzp_rod=r.nzp_rod" +
                                " left outer join " + a_data + "s_dok d on c.nzp_dok=d.nzp_dok" +
                                " where c.nzp_sobstw=" + finder.nzp_sobstw;
#else
            string a_data = finder.pref + "_data:";
            string sqlStr =
                                " SELECT '" + finder.pref + "' as pref," +
                               " case when is_actual=1 then 'действует' else 'не действует' end as sost," +
                               "*" +
                               " from  OUTER " + a_data + "s_rod r, " +
                               " OUTER " + a_data + "s_dok d,  " + 
                               " " + a_data + "sobstw c " +
                               " where c.nzp_sobstw=" + finder.nzp_sobstw +
                               " and c.nzp_dok=d.nzp_dok" +                           
                               " and c.nzp_rod=r.nzp_rod";
#endif


            MyDataReader reader, reader2;
            StringBuilder sql = new StringBuilder();
            if (!ExecRead(conn_web, out reader, sqlStr, true).result)
            {
                conn_web.Close();
                ret.result = false;
                return null;
            }
            try
            {
                if (!reader.Read())
                {
                    ret.result = false;
                    ret.text = "Собственник не найден";
                    return null;
                }
                Sobstw zap = new Sobstw();

                if (reader["nzp_kvar"] == DBNull.Value)
                    zap.nzp_kvar = 0;
                else
                    zap.nzp_kvar = (int)reader["nzp_kvar"];

                zap.nzp_sobstw = Convert.ToString(reader["nzp_sobstw"]).Trim();
                zap.nzp_gil = Convert.ToString(reader["nzp_gil"]).Trim();
                zap.nzp_rod = Convert.ToString(reader["nzp_rod"]).Trim();
                zap.rod = Convert.ToString(reader["rod"]).Trim();
                zap.fam = Convert.ToString(reader["fam"]).Trim();
                zap.ima = Convert.ToString(reader["ima"]).Trim();
                zap.otch = Convert.ToString(reader["otch"]).Trim();
                zap.fio = zap.fam + " " + zap.ima + " " + zap.otch;
                zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();
                zap.adress = Convert.ToString(reader["adress"]).Trim();
                zap.dop_info = Convert.ToString(reader["dop_info"]).Trim();
                zap.nzp_dok = Convert.ToString(reader["nzp_dok"]).Trim();
                zap.dok = Convert.ToString(reader["dok"]).Trim();
                zap.serij = Convert.ToString(reader["serij"]).Trim();
                zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();
                zap.tel = Convert.ToString(reader["tel"]).Trim();

                sql.Remove(0, sql.Length);
                sql.AppendFormat("select dolya_up, dolya_down from " + finder.pref + "_data" + tableDelimiter + "doc_sobstw where nzp_sobstw = " + zap.nzp_sobstw);
                ret = ExecRead(conn_web, out reader2, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
                List<DocSobstw> list = new List<DocSobstw>();
                while (reader2.Read())
                {
                    DocSobstw ds = new DocSobstw();
                    if (reader2["dolya_up"] != DBNull.Value) ds.dolya_up = Convert.ToInt32(reader2["dolya_up"]);
                    if (reader2["dolya_down"] != DBNull.Value) ds.dolya_down = Convert.ToInt32(reader2["dolya_down"]);
                    list.Add(ds);
                }
                reader2.Close();
                string dolya = "0";
                if (list.Count > 0)
                {
                    DocSobstw doc = GetSumDolya(list);

                    if (doc != null)
                    {
                        zap.dolya_up = doc.dolya_up.ToString();
                        zap.dolya_down = doc.dolya_down.ToString();

                        if (doc.dolya_up > doc.dolya_down)
                        {
                            int o1 = doc.dolya_up / doc.dolya_down;
                            int o2 = doc.dolya_down * o1;
                            int o3 = doc.dolya_up - o2;
                            dolya = o1.ToString() + " " + o3.ToString() + "/" + doc.dolya_down;
                        }
                        else
                        {
                            dolya = doc.dolya_up.ToString() + "/" + doc.dolya_down.ToString();
                            if (doc.dolya_down > 0)
                                if (doc.dolya_up % doc.dolya_down == 0) dolya = doc.dolya_up.ToString();
                        }
                    }
                }
                zap.dolya = dolya;

                //zap.nzp_dok_sv = Convert.ToString(reader["nzp_dok_sv"]).Trim();
                //zap.dok_sv = Convert.ToString(reader["dok_sv"]).Trim();
                //zap.serij_sv = Convert.ToString(reader["serij_sv"]).Trim();
                //zap.nomer_sv = Convert.ToString(reader["nomer_sv"]).Trim();
                //zap.vid_mes_sv = Convert.ToString(reader["vid_mes_sv"]).Trim();
                //zap.vid_dat_sv = Convert.ToString(reader["vid_dat_sv"]).Trim();

                zap.is_actual = Convert.ToString(reader["is_actual"]).Trim();
                zap.sost = Convert.ToString(reader["sost"]).Trim();

                reader.Close();
                conn_web.Close();
                return zap;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки собственника " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }


        //----------------------------------------------------------------------
        public Sobstw SaveSobstw(Sobstw old_Sobstw, Sobstw new_Sobstw, bool delete_flag, out Returns ret)
        //----------------------------------------------------------------------
        {
            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------            
            ret = Utils.InitReturns();

            if (new_Sobstw.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            string sql = "";

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);

            if (!ret.result) return null;
            string a_data = new_Sobstw.pref + "_data" + tableDelimiter;

            #region проверки
            //-проверки-------------------------------------------------------------

            if ((new_Sobstw.nzp_kvar.ToString() == "") && (new_Sobstw.nzp_sobstw == ""))
            {
                ret.text = "Код собственника не задан(добавление?), и код квартиры не задан.";
                ret.result = false;
                ret.tag = -1;
                conn_db.Close();
                return null;
            }
            #endregion


            // Запись в БД==================================================================================================            
            if (delete_flag)    //Удаление
            {
                sql = "update " + a_data + "sobstw set is_actual = 0 " +
                      " where nzp_sobstw=" + new_Sobstw.nzp_sobstw;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка удаления Собственника";
                    ret.tag = -1;
                    conn_db.Close();
                    return null;
                }

                conn_db.Close();
                return new_Sobstw;          // ВЫХОД
            }

            string str_fields =
                  " nzp_kvar, nzp_rod, fam, ima, otch, dat_rog, adress, tel, dop_info, nzp_dok, serij, nomer, vid_mes, vid_dat ";

            string str_data =
                           new_Sobstw.nzp_kvar + "," +
                     "'" + new_Sobstw.nzp_rod + "'," +
                     " initcap('" + new_Sobstw.fam.Trim() + "')," +
                     " initcap('" + new_Sobstw.ima.Trim() + "')," +
                     " initcap('" + new_Sobstw.otch.Trim() + "')," +
                     "'" + new_Sobstw.dat_rog.Trim() + "'," +
                     " upper('" + new_Sobstw.adress.Trim() + "')," +
                     " upper('" + new_Sobstw.tel.Trim() + "')," +
                     " upper('" + new_Sobstw.dop_info.Trim() + "')," +
                     "'" + new_Sobstw.nzp_dok + "'," +
                     " upper('" + new_Sobstw.serij + "')," +
                     " upper('" + new_Sobstw.nomer + "')," +
                     " upper('" + new_Sobstw.vid_mes + "')," +
                      Utils.EStrNull(new_Sobstw.vid_dat);


            if (new_Sobstw.nzp_sobstw != "") // Изменение
            {
                sql = "update  " + a_data + "sobstw set (" + str_fields + ")=(" + str_data + " )" +
                    " where nzp_sobstw=" + new_Sobstw.nzp_sobstw;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения собственника";
                    ret.tag = -1;
                    conn_db.Close();
                    return null;
                }
                conn_db.Close();
                return new_Sobstw;          // ВЫХОД

            }

            sql = "insert into  " + a_data + "sobstw (" + str_fields + ", is_actual) values(" + str_data + ", 1)";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка вставки собственника";
                ret.tag = -1;
                conn_db.Close();
                return null;
            }
            new_Sobstw.nzp_sobstw = Convert.ToString(GetSerialValue(conn_db));

            conn_db.Close();
            return new_Sobstw;
        }

        public Returns SaveDocSobstw(List<DocSobstw> finder)
        {
            #region Новая культура для дат
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            #endregion
            Returns ret = Utils.InitReturns();

            StringBuilder sql = new StringBuilder();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            foreach (DocSobstw doc in finder)
            {
                if (doc.pref.Trim() == "") return new Returns(false, "Банк данных не выбран", -1);
                if (doc.nzp_sobstw <= 0) return new Returns(false, "Собственник не выбран", -1);

                sql.Remove(0, sql.Length);

                string table = doc.pref + "_data" + tableDelimiter + "doc_sobstw";
                if (doc.nzp_doc > 0)
                {
                    sql.AppendFormat(" update {0} set ", table);
                    sql.AppendFormat(" nzp_sobstw = {0}, dolya_up = {1}, dolya_down = {2}, ", doc.nzp_sobstw, doc.dolya_up, doc.dolya_down);
                    sql.AppendFormat(" nzp_dok_sv = {0}, serij_sv = upper('{1}'), nomer_sv = upper('{2}'), ", doc.nzp_dok_sv, doc.serij_sv, doc.nomer_sv);
                    sql.AppendFormat(" vid_mes_sv = upper('{0}'), vid_dat_sv = {1} ", doc.vid_mes_sv, Utils.EStrNull(doc.vid_dat_sv));
                    sql.AppendFormat(" where nzp_doc = {0}", doc.nzp_doc);
                }
                else
                {
                    sql.AppendFormat(" insert into {0}", table);
                    sql.Append(" (nzp_sobstw, dolya_up, dolya_down, nzp_dok_sv, serij_sv, nomer_sv, vid_mes_sv, vid_dat_sv) ");
                    sql.AppendFormat(" values ({0}, {1}, {2}, {3}, upper('{4}'), upper('{5}'), upper('{6}'), {7})", doc.nzp_sobstw, doc.dolya_up, doc.dolya_down,
                        doc.nzp_dok_sv, doc.serij_sv, doc.nomer_sv, doc.vid_mes_sv, Utils.EStrNull(doc.vid_dat_sv));
                }
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения документа о собственности";
                    ret.tag = -1;
                    conn_db.Close();
                    return ret;
                }
            }

            conn_db.Close();
            return ret;
        }

        public List<DocSobstw> GetListDocSobstv(DocSobstw finder, out Returns ret)
        {
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не указан префикс БД", -1);
                return null;
            }
            if (finder.nzp_sobstw <= 0)
            {
                ret = new Returns(false, "Не указан собственник", -1);
                return null;
            }
            List<DocSobstw> docSobstv = new List<DocSobstw>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            MyDataReader reader;
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("select ds.nzp_doc, ds.dolya_up, ds.dolya_down, ds.nzp_dok_sv, s.dok_sv, " +
                             " ds.serij_sv, ds.nomer_sv, ds.vid_mes_sv, ds.vid_dat_sv " +
                             " from {0}_data{1}doc_sobstw ds " +
                             " left outer join {0}_data{1}s_dok_sv s on ds.nzp_dok_sv=s.nzp_dok_sv" +
                             " where ds.nzp_sobstw = {2}", finder.pref, tableDelimiter, finder.nzp_sobstw);
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    DocSobstw doc = new DocSobstw();
                    doc.nzp_sobstw = finder.nzp_sobstw;
                    if (reader["nzp_doc"] != DBNull.Value) doc.nzp_doc = Convert.ToInt32(reader["nzp_doc"]);
                    if (reader["dolya_up"] != DBNull.Value) doc.dolya_up = Convert.ToInt32(reader["dolya_up"]);
                    if (reader["dolya_down"] != DBNull.Value) doc.dolya_down = Convert.ToInt32(reader["dolya_down"]);
                    if (reader["nzp_dok_sv"] != DBNull.Value) doc.nzp_dok_sv = Convert.ToInt32(reader["nzp_dok_sv"]);
                    if (reader["nzp_dok_sv"] != DBNull.Value) doc.dok_sv = Convert.ToString(reader["dok_sv"]).Trim();
                    if (reader["serij_sv"] != DBNull.Value) doc.serij_sv = Convert.ToString(reader["serij_sv"]);
                    if (reader["nomer_sv"] != DBNull.Value) doc.nomer_sv = Convert.ToString(reader["nomer_sv"]);
                    if (reader["vid_mes_sv"] != DBNull.Value) doc.vid_mes_sv = Convert.ToString(reader["vid_mes_sv"]);
                    if (reader["vid_dat_sv"] != DBNull.Value) doc.vid_dat_sv = Convert.ToString(reader["vid_dat_sv"]);
                    if (doc.vid_dat_sv.Trim() != "")
                    {
                        DateTime dt = DateTime.MinValue;
                        DateTime.TryParse(doc.vid_dat_sv, out dt);
                        if (dt != DateTime.MinValue) doc.vid_dat_sv = dt.ToShortDateString();
                    }

                    docSobstv.Add(doc);
                }

                reader.Close();
                conn_db.Close();
                return docSobstv;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения собственников " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public Returns DeleteDocSobstv(DocSobstw finder)
        {
            if (finder.nzp_doc <= 0) return new Returns(false, "Не выбран документ для удаления", -1);
            if (finder.pref.Trim() == "") return new Returns(false, "Не указан банк данных", -1);
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("delete from {0} where nzp_doc = {1}", finder.pref + "_data" + tableDelimiter + "doc_sobstw", finder.nzp_doc);

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_db, sql.ToString(), true);
            conn_db.Close();
            return ret;
        }


        //----------------------------------------------------------------------
        public List<String> ProverkaSobstw(Sobstw new_Sobstw, bool delete_flag, out Returns ret) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (new_Sobstw.pref == "")
            {
                ret = new Returns(false, "Префикс базы данных не задан", -1);
                return null;
            }

            if (new_Sobstw.nzp_kvar <= 0)
            {
                ret = new Returns(false, "Лицевой счет не выбран", -1);
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string a_data = new_Sobstw.pref + "_data" + tableDelimiter;

            List<String> returner = new List<String>();

            Int64 ob_znam = 1;
            Int64 sum_chis = 0;
            string sql;

            MyDataReader reader;
            sql = "select nzp_sobstw from " + a_data + "sobstw s where s.nzp_kvar = " + new_Sobstw.nzp_kvar.ToString() + " and s.is_actual=1";
            if (new_Sobstw.nzp_sobstw.Trim() != "")
            {
                sql += " and s.nzp_sobstw <> " + new_Sobstw.nzp_sobstw.Trim();
            }
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result) return null;

            sql = "";
            while (reader.Read())
            {
                int nzp_sobstw = 0;
                if (reader["nzp_sobstw"] != DBNull.Value) nzp_sobstw = Convert.ToInt32(reader["nzp_sobstw"]);

                if (sql != "") sql += " UNION ";

                sql += "select " + sNvlWord + "(s.dolya_up,0) as dolya_up, " + sNvlWord + "(s.dolya_down,0) as dolya_down from " + a_data + "doc_sobstw s " + " where s.nzp_sobstw = " + nzp_sobstw;
            }

            if ((!delete_flag) && (new_Sobstw.is_actual != "0"))
            {
                if ((new_Sobstw.dolya_up.Trim() != "") && (new_Sobstw.dolya_down.Trim() != ""))
                    sql += " UNION ALL " +
                           " select " + new_Sobstw.dolya_up + "," + new_Sobstw.dolya_down +
                           " from " + Points.Pref + sKernelAliasRest + "services where nzp_serv=1";
            }

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                ret.text = "Ошибка при нахожденнии общ. знаменателя для долей собственности";
                ret.result = false;
                ret.tag = -1;
                return null;
            }
            while (reader.Read())
            {
                if (Convert.ToInt64(reader["dolya_down"]) > 0)
                    ob_znam = ob_znam * Convert.ToInt64(reader["dolya_down"]);
            }
            reader.Close();

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                ret.text = "Ошибка при нахожденнии числителя для долей собственности";
                ret.result = false;
                ret.tag = -1;
                return null;
            }
            while (reader.Read())
            {
                if (Convert.ToInt64(reader["dolya_up"]) > 0)
                    sum_chis = sum_chis +
                (Convert.ToInt64(reader["dolya_up"]) * ob_znam / Convert.ToInt64(reader["dolya_down"]));
            }
            reader.Close();

            if ((sum_chis % ob_znam != 0) ||
                ((sum_chis / ob_znam != 0) && (sum_chis / ob_znam != 1)))
            {
                returner.Add("Сумма долей не равна 1.");
            }

            conn_db.Close();
            return returner;
        }


        //----------------------------------------------------------------------
        public int CheckMask(string mask, string s_in, out string s)
        //----------------------------------------------------------------------
        {
            s = s_in.Trim().ToUpper();

            // если маска отсутствует , то без проверки
            if (mask == "") return 0;

            // если присутствует серия, а поле не заполнено, то ошибка
            if ((mask.Length > 0) && (s == ""))
            {
                return -1;
            }
            //  попытка преобразовать введенную строку к стандартной форме шаблона
            //  1)разобраться с римскими цифрами или
            // (это будет работать только с масками, где разделитель с римскими "-")
            if (mask.IndexOf("R") >= 0)
            {
                int i = s.IndexOf("-");
                if (i == -1) i = s.Length;

                string s1;
                if (TranslateToRoman(s.Substring(0, i), out s1) == 0)
                {
                    s = s1 + s.Substring(i, s.Length - i);
                }
                else
                {
                    return -1;
                }

            }
            // проверка на соответствие длин приведенной входной строки и маски
            if (s.Length > mask.Length)
            {
                return -1;
            }
            // теперь проверяем на посимвольное соответствие маске
            return CheckForMask(ref s, mask);
        }

        //----------------------------------------------------------------------
        public int TranslateToRoman(string instr, out string outstr)
        //----------------------------------------------------------------------
        {
            int result = 0;
            // пробуем перевести арабское число в римское
            try
            {
                int n = Convert.ToInt32(instr);
                outstr = IntToRoman(n);
                result = 0;
            }
            catch
            {
                result = -1;
                outstr = instr;
            }
            if (result == 0) return 0;

            //пробуем переводить с русских знаков на английские
            outstr = outstr.Replace("1", "I");
            outstr = outstr.Replace("У", "V");
            outstr = outstr.Replace("Х", "X");
            outstr = outstr.Replace("Л", "L");
            outstr = outstr.Replace("С", "C");
            outstr = outstr.Replace("М", "M");
            outstr = outstr.Replace("Д", "D");

            // проверяем является ли строка римским числом
            string expr = @"^(?i)M{0,3}(D?C{0,3}|C[DM])(L?X{0,3}|X[LC])(V?I{0,3}|I[VX])$";
            if (Regex.IsMatch(outstr, expr))
                return 0;
            else
                return -2;
        }

        //----------------------------------------------------------------------
        string IntToRoman(int n)
        //----------------------------------------------------------------------
        {
            int[] mas1 = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] mas2 = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            int i;
            i = 0;
            string s = "";
            while (n > 0)              //крутим цикл пока n>0
            {
                if (mas1[i] <= n)      // i - элемент массива арабских цифр меньше либо равен числу n то
                {
                    n = n - mas1[i];   // от числа вычитаем его эквивалент в массиве арабском 
                    s = s + mas2[i];   // в строку записываем его римское значение
                }
                else i++;
            }
            return s;
        }

        //----------------------------------------------------------------------
        int CheckForMask(ref string s, string mask)
        //----------------------------------------------------------------------
        {
            int Result = 0;
            s = s.PadLeft(mask.Length);

            for (int i = 0; i < mask.Length; i++)
            {

                switch (mask[i])
                {
                    case 'R':
                        if (!Regex.IsMatch(s[i].ToString(), @"[C|M|D|X|L|I|V| ']")) Result = -1;
                        break;
                    case 'Б':
                        if (!Regex.IsMatch(s[i].ToString(), @"[А-Я]")) Result = -1;
                        break;

                    case 'S':
                        if (!Regex.IsMatch(s[i].ToString(), @"[A-Z|А-Я|0-9]")) Result = -1;
                        break;

                    case 'X':
                        if (!Regex.IsMatch(s[i].ToString(), @"[A-Z|А-Я|0-9]")) Result = -1;
                        break;

                    case '9':
                        //                        if( s[i]==' ')  s[i]='0';
                        if (!Regex.IsMatch(s[i].ToString(), @"[0-9]")) Result = -1;
                        break;
                    case '0':
                        if (!Regex.IsMatch(s[i].ToString(), @"[0-9| ]")) Result = -1;
                        break;

                    default:
                        if (s[i] != mask[i]) Result = -1;
                        break;
                }

            }
            s.Trim();
            return Result;
        }

        //------------------------------------------------------------------------
        public string[] GetPasportistInformation(Gilec US, out Returns ret)
        //------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (US.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }
            if (US.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            IDbConnection conn_db2 = GetConnection(Constants.cons_Kernel);
            IDataReader reader;
            IDataReader reader2;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            //string cur_pref = US.pref;
            StringBuilder sql = new StringBuilder();
            StringBuilder sql1 = new StringBuilder();

#if PG
            sql.Append("set search_path to 'public'");
#else
            sql.Append(" set encryption password " + "'" + BasePwd + "';");
#endif
            ExecSQL(conn_db, sql.ToString(), true);
            sql.Remove(0, sql.Length);
            sql.Append(" select decrypt_char(uname) as uname from  users  ");
            sql.Append(" where nzp_user=" + US.nzp_user);

            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            //Выборка: Должности паспортиста, должности начальника, ФИО начальника
            string cur_pref = US.pref;
#if PG
            sql1.Append("Select (select max(val_prm) from " + cur_pref + "_data.prm_10  ");
            sql1.Append("   where nzp_prm =578 and is_actual<>100 and current_date between dat_s and dat_po ");
            sql1.Append(" ) as dolg_oper , ");
            sql1.Append(" (select max(val_prm) from " + cur_pref + "_data.prm_10  ");
            sql1.Append(" where nzp_prm =1048 and is_actual<>100 and current_date between dat_s and dat_po ");
            sql1.Append(" ) as dolg_nach , ");
            sql1.Append(" (select max(val_prm) from " + cur_pref + "_data.prm_10  ");
            sql1.Append("  where nzp_prm =1047 and is_actual<>100 and current_date between dat_s and dat_po ");
            sql1.Append(" ) as fio_nach  ");
#else
            sql1.Append("Select (select max(val_prm) from " + cur_pref + "_data:prm_10  ");
            sql1.Append("   where nzp_prm =578 and is_actual<>100 and today between dat_s and dat_po ");
            sql1.Append(" ) as dolg_oper , ");
            sql1.Append(" (select max(val_prm) from " + cur_pref + "_data:prm_10  ");
            sql1.Append(" where nzp_prm =1048 and is_actual<>100 and today between dat_s and dat_po ");
            sql1.Append(" ) as dolg_nach , ");
            sql1.Append(" (select max(val_prm) from " + cur_pref + "_data:prm_10  ");
            sql1.Append("  where nzp_prm =1047 and is_actual<>100 and today between dat_s and dat_po ");
            sql1.Append(" ) as fio_nach  ");
            sql1.Append(" from systables where tabid=1 ");
#endif
            string[] retStr = new string[] { "-", "-", "-", "-" };
            try
            {
                while (reader.Read())
                {
                    if (reader["uname"] != DBNull.Value) retStr[0] = Convert.ToString(reader["uname"]).Trim();
                }
                reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой  

                ret = OpenDb(conn_db2, true);
                if (!ret.result)
                {
                    return null;
                }
                if (!ExecRead(conn_db2, out reader2, sql1.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                while (reader2.Read())
                {
                    if (reader2["dolg_oper"] != DBNull.Value) retStr[1] = Convert.ToString(reader2["dolg_oper"]).Trim();
                    if (reader2["dolg_nach"] != DBNull.Value) retStr[2] = Convert.ToString(reader2["dolg_nach"]).Trim();
                    if (reader2["fio_nach"] != DBNull.Value) retStr[3] = Convert.ToString(reader2["fio_nach"]).Trim();
                }


                reader2.Close();
                sql.Remove(0, sql.Length);
                sql1.Remove(0, sql1.Length);
                conn_db2.Close(); //закрыть соединение с основной базой                                
                return retStr;
            }
            finally
            {
                conn_db.Close(); //закрыть соединение с основной базой
            }

        }

        /// <summary>
        /// найти и заполнить список жильцов по лицевому счету, с полной информацией по человеку
        /// возвращает также убывших жильцов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        //-------------------------------------------------------------------------
        public List<GilecFullInf> GetFullInfGilList_AllKards(Gilec finder, out Returns ret)
        //-------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            string my_kart = "kart";
            if (finder.is_arx)
            {
                my_kart = "kart_arx";
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader;
            IDataReader reader2;
            //IDataReader reader3;
            List<GilecFullInf> Spis = new List<GilecFullInf>();



            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string cur_pref = finder.pref;
            StringBuilder sql = new StringBuilder();
            StringBuilder sql2 = new StringBuilder();

            string mropku = "";
            if (Points.Region != Regions.Region.Tatarstan)
            {
                mropku = " k.strana_mr, k.region_mr, k.okrug_mr, k.gorod_mr, k.npunkt_mr," +
                        " k.strana_op, k.region_op, k.okrug_op, k.gorod_op, k.npunkt_op," +
                        " k.strana_ku, k.region_ku, k.okrug_ku, k.gorod_ku, k.npunkt_ku," +
                        " k.dat_smert, k.dat_fio_c, k.rodstvo,";
            }


            sql.Append(" SELECT 0 as is_arx, k.nzp_kart, fam, ima, otch, dat_rog, serij, nomer, vid_dat, vid_mes, k.nzp_tkrt, ");
            sql.Append(" tprp, cel, nzp_gil, k.nzp_kvar, k.nzp_kart, ");
            sql.Append(" ss1.stat as statop, st1.town as twnop, sl1.land as landop, sr1.rajon as rajonop,");
            sql.Append(" ss2.stat as statku, st2.town as twnku, sl2.land as landku, sr1.rajon as rajonku,");
            sql.Append(" rod, jobpost, jobname, dat_oprp, dat_ofor, rem_mr, rem_op, rem_ku,                 ");

            sql.Append(mropku);

            //добавочные//
            sql.Append(" st3.town as town_, sr3. rajon as rajon_ ,k.dat_ofor,   ");
            sql.Append(" k.isactual ");
            //
#if PG
            sql.Append(" FROM " + cur_pref + "_data." + my_kart + " k ");
            //добавочные//
            sql.Append(" left outer join " + cur_pref + "_data.s_cel sc on k.nzp_celp=sc.nzp_cel ");
            sql.Append(" left outer join " + cur_pref + "_data.s_rod sr on k.nzp_rod=sr.nzp_rod ");
            sql.Append(" left outer join " + cur_pref + "_data.s_land sl1 on sl1.nzp_land=nzp_lnop ");
            sql.Append(" left outer join " + cur_pref + "_data.s_town st1 on st1.nzp_town=nzp_tnop ");
            sql.Append(" left outer join " + cur_pref + "_data.s_stat ss1 on ss1.nzp_stat=nzp_stop ");
            sql.Append(" left outer join " + cur_pref + "_data.s_rajon sr1 on sr1.nzp_raj=nzp_rnop ");
            sql.Append(" left outer join " + cur_pref + "_data.s_land sl2 on sl2.nzp_land=nzp_lnku ");
            sql.Append(" left outer join " + cur_pref + "_data.s_town st2 on st2.nzp_town=nzp_tnku ");
            sql.Append(" left outer join " + cur_pref + "_data.s_stat ss2 on ss2.nzp_stat=nzp_stku ");
            sql.Append(" left outer join " + cur_pref + "_data.s_rajon sr2 on sr2.nzp_raj=nzp_rnku, ");
            sql.Append(" " + cur_pref + "_data.kvar kv, ");
            sql.Append(" " + cur_pref + "_data.dom dd ");
            sql.Append(" left outer join " + cur_pref + "_data.s_town st3 on dd.nzp_town  = st3.nzp_town ");
            sql.Append(" left outer join " + cur_pref + "_data.s_rajon sr3 on dd.nzp_raj  =  sr3.nzp_raj ");
            sql.Append(" where k.nzp_kvar =" + finder.nzp_kvar.ToString());
            //добавочные//
            sql.Append(" and  k.nzp_kvar = kv.nzp_kvar ");
            sql.Append(" and kv.nzp_dom = dd.nzp_dom  ");

            sql.Append(" and coalesce(k.neuch::int,0)<>'1'");
            //sql.Append(" and isactual=1 ");
#else
            sql.Append(" FROM " + cur_pref + "_data:" + my_kart + " k,  ");
            //добавочные//
            sql.Append("         " + cur_pref + "_data:kvar kv,                        ");
            sql.Append("         " + cur_pref + "_data:dom dd,                         ");
            sql.Append("        outer " + cur_pref + "_data:s_town st3,                        ");
            sql.Append("        outer " + cur_pref + "_data:s_rajon sr3,                        ");
            //
            sql.Append("        outer " + cur_pref + "_data:s_cel sc,                        ");
            sql.Append("        outer " + cur_pref + "_data:s_rod sr,                        ");
            sql.Append("        outer " + cur_pref + "_data:s_land sl1,                      ");
            sql.Append("        outer " + cur_pref + "_data:s_town st1,                      ");
            sql.Append("        outer " + cur_pref + "_data:s_stat ss1,                      ");
            sql.Append("        outer " + cur_pref + "_data:s_rajon sr1,                     ");
            sql.Append("        outer " + cur_pref + "_data:s_land sl2,                      ");
            sql.Append("        outer " + cur_pref + "_data:s_town st2,                      ");
            sql.Append("        outer " + cur_pref + "_data:s_stat ss2,                      ");
            sql.Append("        outer " + cur_pref + "_data:s_rajon sr2                      ");
            sql.Append(" where k.nzp_kvar =" + finder.nzp_kvar.ToString());
            //добавочные//
            sql.Append(" and  k.nzp_kvar = kv.nzp_kvar         ");
            sql.Append(" and kv.nzp_dom = dd.nzp_dom         ");
            sql.Append(" and dd.nzp_town  = st3.nzp_town         ");
            sql.Append(" and dd.nzp_raj  =  sr3.nzp_raj         ");
            //
            sql.Append(" and k.nzp_celp=sc.nzp_cel and k.nzp_rod=sr.nzp_rod         ");
            sql.Append(" and sl1.nzp_land=nzp_lnop                                 ");

            sql.Append(" and st1.nzp_town=nzp_tnop                                 ");

            sql.Append(" and ss1.nzp_stat=nzp_stop                                 ");

            sql.Append(" and sr1.nzp_raj=nzp_rnop                                  ");

            sql.Append(" and sl2.nzp_land=nzp_lnku                                 ");
            sql.Append(" and st2.nzp_town=nzp_tnku                                 ");
            sql.Append(" and ss2.nzp_stat=nzp_stku                                 ");
            sql.Append(" and sr2.nzp_raj=nzp_rnku                                  ");

            sql.Append(" and nvl(k.neuch,0)<>'1'");
            //sql.Append(" and isactual=1 ");
#endif

            string my_sql = sql.ToString();
            if (finder.is_arx)
            {
                // смотрим не только архив
                my_sql = my_sql.Replace("SELECT 0 as is_arx", "SELECT 1 as is_arx") + " UNION " + my_sql.Replace(my_kart, "kart");
            }

            my_sql += " order by dat_rog, fam, ima ";
            if (!ExecRead(conn_db, out reader, my_sql, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + my_sql, MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            try
            {
                while (reader.Read())
                {

                    GilecFullInf zap = new GilecFullInf();

                    if (reader["fam"] != DBNull.Value) zap.fam = Convert.ToString(reader["fam"]).Trim();
                    if (reader["ima"] != DBNull.Value) zap.ima = Convert.ToString(reader["ima"]).Trim();
                    if (reader["otch"] != DBNull.Value) zap.otch = Convert.ToString(reader["otch"]).Trim();
                    if (reader["dat_rog"] != DBNull.Value) zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();
                    if (reader["serij"] != DBNull.Value) zap.serij = Convert.ToString(reader["serij"]).Trim();
                    if (reader["nomer"] != DBNull.Value) zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                    if (reader["vid_mes"] != DBNull.Value) zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                    if (reader["vid_dat"] != DBNull.Value) zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();
                    if (reader["cel"] != DBNull.Value) zap.cel = Convert.ToString(reader["cel"]).Trim();
                    if (reader["rod"] != DBNull.Value) zap.rod = Convert.ToString(reader["rod"]).Trim(); // для Самары ниже меняется
                    if (reader["jobname"] != DBNull.Value) zap.job_name = Convert.ToString(reader["jobname"]).Trim();
                    if (reader["jobpost"] != DBNull.Value) zap.job_post = Convert.ToString(reader["jobpost"]).Trim();
                    if (reader["landop"] != DBNull.Value) zap.landop = Convert.ToString(reader["landop"]).Trim();
                    if (reader["statop"] != DBNull.Value) zap.statop = Convert.ToString(reader["statop"]).Trim();
                    if (reader["twnop"] != DBNull.Value) zap.twnop = Convert.ToString(reader["twnop"]).Trim();
                    if (reader["rajonop"] != DBNull.Value) zap.rajonop = Convert.ToString(reader["rajonop"]).Trim();
                    if (reader["rem_op"] != DBNull.Value) zap.rem_op = Convert.ToString(reader["rem_op"]).Trim();
                    if (reader["landku"] != DBNull.Value) zap.landku = Convert.ToString(reader["landku"]).Trim();
                    if (reader["statku"] != DBNull.Value) zap.statku = Convert.ToString(reader["statku"]).Trim();
                    if (reader["twnku"] != DBNull.Value) zap.twnku = Convert.ToString(reader["twnku"]).Trim();
                    if (reader["rajonku"] != DBNull.Value) zap.rajonku = Convert.ToString(reader["rajonku"]).Trim();
                    if (reader["rem_ku"] != DBNull.Value) zap.rem_ku = Convert.ToString(reader["rem_ku"]).Trim();
                    if (reader["nzp_tkrt"] != DBNull.Value) zap.nzp_tkrt = Convert.ToString(reader["nzp_tkrt"]).Trim();
                    if (reader["dat_ofor"] != DBNull.Value) zap.dat_ofor = Convert.ToString(reader["dat_ofor"]).Trim();
                    if (reader["dat_oprp"] != DBNull.Value) zap.dat_oprp = Convert.ToString(reader["dat_oprp"]).Trim();
                    if (reader["tprp"] != DBNull.Value) zap.tprp = Convert.ToString(reader["tprp"]).Trim();
                    if (reader["nzp_gil"] != DBNull.Value) zap.nzp_gil = Convert.ToInt64(reader["nzp_gil"]);
                    if (reader["nzp_kart"] != DBNull.Value) zap.nzp_kart = Convert.ToInt32(reader["nzp_kart"]);
                    if (reader["is_arx"] != DBNull.Value) zap.is_arx = Convert.ToBoolean(reader["is_arx"]);


                    if (Points.Region != Regions.Region.Tatarstan)
                    {
                        zap.rem_mr = Convert.ToString(reader["strana_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["region_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["okrug_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["gorod_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["npunkt_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim() + " " + Convert.ToString(reader["rem_mr"]).Trim();
                        zap.rem_mr = zap.rem_mr.Trim();

                        zap.rem_op = Convert.ToString(reader["strana_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["region_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["okrug_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["gorod_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["npunkt_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim() + " " + Convert.ToString(reader["rem_op"]).Trim();
                        zap.rem_op = zap.rem_op.Trim();

                        zap.rem_ku = Convert.ToString(reader["strana_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["region_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["okrug_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["gorod_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["npunkt_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim() + " " + Convert.ToString(reader["rem_ku"]).Trim();
                        zap.rem_ku = zap.rem_ku.Trim();

                        zap.rod = Convert.ToString(reader["rodstvo"]).Trim();
                        if (Convert.ToString(reader["dat_smert"]).Trim() != "")
                            zap.dat_smert = Convert.ToString(reader["dat_smert"]).Trim();
                        else
                            zap.dat_smert = zap.dat_ofor;

                    }

                    //добавочные//
                    if (reader["town_"] != DBNull.Value) zap.town_ = Convert.ToString(reader["town_"]);
                    if (reader["rajon_"] != DBNull.Value) zap.rajon_ = Convert.ToString(reader["rajon_"]);
                    //
                    if (reader["isactual"] != DBNull.Value) zap.isactual = Convert.ToString(reader["isactual"]);
                    //
                    if (zap.is_arx)
                        my_kart = "kart_arx";
                    else
                        my_kart = "kart";

                    if (zap.nzp_tkrt == "1")
                    {
                        zap.dat_prib = zap.dat_ofor;
                        if (zap.tprp == "П") zap.type_prop = "постоянная";
                        else
                            if (zap.tprp == "В") zap.type_prop = "временная";
#if PG
                        sql2.Append(" select  dat_ofor as max_date ");
                        sql2.Append("        from " + cur_pref + "_data." + my_kart);
#else
                        sql2.Append(" select first 1 dat_ofor as max_date ");
                        sql2.Append("        from " + cur_pref + "_data:" + my_kart);
#endif
                        sql2.Append("        where nzp_gil=" + zap.nzp_gil.ToString());
                        sql2.Append("        and nzp_kvar=" + finder.nzp_kvar.ToString());
                        sql2.Append("        and nzp_tkrt=2 ");
                        sql2.Append("        and dat_ofor>= ");
                        sql2.Append("        (  ");

#if PG
                        sql2.Append("        select date(coalesce(dat_ofor,'01.01.0001')) ");
                        sql2.Append("        from " + cur_pref + "_data." + my_kart + " where nzp_kart=" + zap.nzp_kart);
#else
                        sql2.Append("        select date(nvl(dat_ofor,'01.01.0001')) ");
                        sql2.Append("        from " + cur_pref + "_data:" + my_kart + " where nzp_kart=" + zap.nzp_kart);
#endif
                        sql2.Append("        ) ");
                        sql2.Append("        order by 1 ");
#if PG
                        sql2.Append(" limit 1 ");
#else
#endif


                        if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                        {

                            MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            return null;
                        }

                        if (reader2.Read()) zap.dat_vip = Convert.ToString(reader2["max_date"]).Trim();
                        reader2.Close();
                    }
                    else
                    {
                        if (zap.tprp == "В") zap.type_prop = "временная";
                        else zap.type_prop = zap.type_prop;
#if PG
                        sql2.Append(" select  dat_ofor as min_date ");
                        sql2.Append(" from " + cur_pref + "_data." + my_kart);
#else
                        sql2.Append(" select first 1 dat_ofor as min_date ");
                        sql2.Append(" from " + cur_pref + "_data:" + my_kart);
#endif
                        sql2.Append(" where nzp_gil= " + zap.nzp_gil.ToString());
                        sql2.Append(" and nzp_kvar= " + finder.nzp_kvar.ToString());
                        sql2.Append(" and nzp_tkrt=1 ");
                        sql2.Append(" and dat_ofor<=  ");
                        sql2.Append(" ( ");
#if PG
                        sql2.Append(" select date(coalesce(dat_ofor,'01.01.9999' )) ");
                        sql2.Append(" from " + cur_pref + "_data." + my_kart + " where nzp_kart=" + zap.nzp_kart);
#else
                        sql2.Append(" select date(nvl(dat_ofor,'01.01.9999' )) ");
                        sql2.Append(" from " + cur_pref + "_data:" + my_kart + " where nzp_kart=" + zap.nzp_kart);
#endif

                        sql2.Append(" ) ");
                        sql2.Append(" order by 1 desc");
#if PG
                        sql2.Append(" limit 1 ");
#else
#endif

                        if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            ret.result = false;
                            return null;
                        }

                        if (reader2.Read()) zap.dat_prib = Convert.ToString(reader2["min_date"]).Trim();
                        reader2.Close();
                        zap.dat_vip = zap.dat_ofor;
                    }

                    sql2.Remove(0, sql2.Length);
                    Spis.Add(zap);
                }
                //----------------------------------------------------------------------
                //даты приезда - уезда умершего
                DateTime dat_pribS = new DateTime();
                DateTime dat_ubitS = new DateTime();

                List<GilecFullInf> Spis3 = new List<GilecFullInf>();
                long nzp_gilS = -1;

                foreach (GilecFullInf gil in Spis)
                {
                    //                    if (gil.isactual != "")
                    //                    {
                    //нашли умершего
                    //                        if (gil.isactual == "1" && gil.nzp_kart == finder.nzp_kart)
                    if (gil.is_arx == finder.is_arx && gil.nzp_kart == finder.nzp_kart)
                    {
                        string[] mas_dat = gil.dat_ofor.Split(new char[] { '.', ':', ' ' });
                        dat_ubitS = new DateTime(
                           Convert.ToInt32(mas_dat[2]),
                           Convert.ToInt32(mas_dat[1]),
                           Convert.ToInt32(mas_dat[0]),
                           Convert.ToInt32(mas_dat[3]),
                           Convert.ToInt32(mas_dat[4]),
                        Convert.ToInt32(mas_dat[5]));

                        if (gil.dat_prib != "")
                        {
                            mas_dat = gil.dat_prib.Split(new char[] { '.', ':', ' ' });
                            dat_pribS = new DateTime(
                              Convert.ToInt32(mas_dat[2]),
                              Convert.ToInt32(mas_dat[1]),
                              Convert.ToInt32(mas_dat[0]),
                              Convert.ToInt32(mas_dat[3]),
                              Convert.ToInt32(mas_dat[4]),
                           Convert.ToInt32(mas_dat[5]));
                        }
                        else
                        {
                            dat_pribS = new DateTime(1, 01, 01, 0, 0, 0);
                        }

                        nzp_gilS = gil.nzp_gil;

                        Spis3.Add(gil);
                        break;
                    }
                    //                    }
                }
                //

                foreach (GilecFullInf gil in Spis)
                {
                    DateTime dat_prib = new DateTime();
                    if (gil.dat_prib != "")
                    {
                        string[] mas_dat = gil.dat_prib.Split(new char[] { '.', ':', ' ' });
                        dat_prib = new DateTime(
                          Convert.ToInt32(mas_dat[2]),
                          Convert.ToInt32(mas_dat[1]),
                          Convert.ToInt32(mas_dat[0]),
                          Convert.ToInt32(mas_dat[3]),
                          Convert.ToInt32(mas_dat[4]),
                       Convert.ToInt32(mas_dat[5]));
                    }
                    else
                    {
                        //dat_pribS = new DateTime(1, 01, 01, 0, 0, 0);
                        dat_prib = new DateTime(1, 01, 01, 0, 0, 0);
                    }

                    DateTime dat_vip = new DateTime();// = null;
                    if (gil.dat_vip != "")
                    {
                        string[] mas_dat = gil.dat_vip.Split(new char[] { '.', ':', ' ' });
                        dat_vip = new DateTime(
                          Convert.ToInt32(mas_dat[2]),
                          Convert.ToInt32(mas_dat[1]),
                          Convert.ToInt32(mas_dat[0]),
                          Convert.ToInt32(mas_dat[3]),
                          Convert.ToInt32(mas_dat[4]),
                       Convert.ToInt32(mas_dat[5]));
                    }
                    // может if (dat_prib <= dat_ubitS)
                    //if ((dat_prib <= dat_ubitS && dat_prib >= dat_pribS) || (dat_prib <= dat_ubitS && gil.dat_vip == ""))
                    if (dat_prib <= dat_ubitS)
                    {
                        if (dat_vip.ToShortDateString() != "01.01.0001" && dat_vip != null) // приписал && dat_vip != ""
                        {
                            //Карточка прибытия есть всегда (gil.nzp_tkrt == "1")                            
                            if (dat_vip >= dat_ubitS && gil.nzp_tkrt == "1" && gil.nzp_gil != nzp_gilS)
                            {
                                Spis3.Add(gil);
                            }
                        }
                        else
                        {
                            //Дата выписки отсутствует  и тип карточки прибытие
                            if (gil.nzp_tkrt == "1")
                            {
                                Spis3.Add(gil);
                            }
                        }
                    }

                }

                reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой
                //return Spis2;
                return Spis3;

            }
            finally
            {
                conn_db.Close(); //закрыть соединение с основной базой
            }

        }//GetFullInfGilList_AllKards


        //------------------------------------------------------------------------------------
        public List<Kart> Reg_Po_MestuGilec(Gilec finder, out Returns ret)
        //-------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return null;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            //заполнить webdata:tXX_cnt
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader;

            string my_kart = "kart";
            if (finder.is_arx)
            {
                my_kart = "kart_arx";
            }



            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string cur_pref = finder.pref;
            StringBuilder sql = new StringBuilder();
            List<Kart> Spis = new List<Kart>();

            string mropku = "";
            if (Points.Region != Regions.Region.Tatarstan)
            {
                mropku = " k.strana_mr, k.region_mr, k.okrug_mr, k.gorod_mr, k.npunkt_mr," +
                        " k.strana_op, k.region_op, k.okrug_op, k.gorod_op, k.npunkt_op," +
                        " k.strana_ku, k.region_ku, k.okrug_ku, k.gorod_ku, k.npunkt_ku," +
                        " k.dat_smert, k.dat_fio_c, k.rodstvo, ";
            }

            string pref = cur_pref;

#if PG
            sql.Append(
                             " SELECT  k.nzp_kart ,k.fam, k.ima, k.otch, k.dat_rog, k.gender, k.dat_ofor, k.serij, k.nomer, k.vid_dat, k.vid_mes, k.kod_podrazd," +
                             " k.namereg as namereg, k.kod_namereg_prn," +
                //"  -------------адрес откуда прибыл -------" +
                             " sl. land as land_op, " +
                             " st. stat as stat_op," +
                             " stn. town as town_op," +
                             " sr. rajon as rajon_op," +
                             " k.rem_op," +
                //-----------------воинский учет----------------
                             "k.who_pvu, k.dat_pvu, k.dat_svu,"+
                //--------------работа---------------------------
                             "k.jobpost, k.jobname,"+
                //" ----------------------------------------" +
                //"-----------Адрес куда прибыл(текущий для карточки)-------------------------------------------------------------------------------------------------------"+
                             " sl2.land as land, " +
                             " st2.stat as stat, " +
                             " stn2.town as town," +
                             " sr2.rajon as rajon," +
                             " su.ulica as ulica," +
                             " su.ulicareg as ulicareg," +
                             " d.ndom as ndom," +
                             " d.nkor as nkor," +
                             " kv.nkvar as nkvar," +
                             " rd.rajon_dom," +
                // "----------------------------------------------------------------------------------------------------------------------------------------------------------" +
                             " k. dat_oprp, k.nzp_tkrt, " +
                             " sd. dok, " +
                             mropku +
                             " ln_mr.land as lnmr, st_mr.stat as stmr, tn_mr.town as tnmr, rn_mr.rajon as rnmr, " +
                             "  k.rem_ku, kuln.land as land_ku,  kust.stat as stat_ku, kutn.town as town_ku, kurn.rajon as rajon_ku, k.rem_mr " +
                             " From " + cur_pref + "_data . " + my_kart + " k" +
                             " left outer join " + pref + "_data.s_land kuln on k.nzp_lnku = kuln.nzp_land  " +
                             " left outer join " + pref + "_data.  s_stat kust on k.nzp_stku = kust.nzp_stat " +
                             " left outer join " + pref + "_data.  s_town kutn on k.nzp_tnku = kutn.nzp_town " +
                             " left outer join " + pref + "_data.  s_rajon kurn on k.nzp_rnku = kurn.nzp_raj " +
                             " left outer join " + pref + "_data.s_land sl on k. nzp_lnop =  sl.nzp_land " +
                             " left outer join " + pref + "_data.s_stat st on k.nzp_stop =  st.nzp_stat " +
                             " left outer join " + pref + "_data.s_town stn on k. nzp_tnop =  stn. nzp_town " +
                             " left outer join " + pref + "_data.s_rajon sr on k. nzp_rnop =  sr. nzp_raj " +
                             " left outer join " + cur_pref + "_data.s_dok sd on sd. nzp_dok = k.nzp_dok " +
                             " left outer join " + pref + "_data.s_land ln_mr on k.nzp_lnmr=ln_mr.nzp_land " +
                             " left outer join " + pref + "_data.s_stat st_mr on k.nzp_stmr=st_mr.nzp_stat " +
                             " left outer join " + pref + "_data.s_town tn_mr on k.nzp_tnmr=tn_mr.nzp_town " +
                             " left outer join " + pref + "_data.s_rajon rn_mr on k.nzp_rnmr=rn_mr.nzp_raj, " +
                             pref + "_data. dom d " +
                             " left outer join " + pref + "_data. s_ulica su " +
                                " left outer join " + pref + "_data.  s_rajon sr2 " +
                                    " left outer join " + pref + "_data.  s_town stn2 " +
                                        " left outer join " + pref + "_data.  s_stat st2 " +
                                            " left outer join " + pref + "_data. s_land sl2 on st2.nzp_land = sl2.nzp_land " +
                                        " on  stn2.nzp_stat = st2.nzp_stat " +
                                    " on  sr2.nzp_town = stn2.nzp_town " +
                                " on  su.nzp_raj = sr2.nzp_raj " +
                            " on  d.nzp_ul = su.nzp_ul " +
                            " left outer join " + cur_pref + "_data.s_rajon_dom rd on  d.nzp_raj = rd.nzp_raj_dom, " +
                             pref + "_data. kvar kv" +
                             " Where " +
                //"  coalesce(k.neuch::int,0)<>'1'" + (#142899 Карточка регистрации)
                             " k.nzp_kart = " + finder.nzp_kart +
                             " and k.nzp_kvar = kv.nzp_kvar" +
                             " and kv.nzp_dom = d.nzp_dom  order by k.fam");
#else
            sql.Append(
                 " SELECT  k.nzp_kart ,k.fam, k.ima, k.otch, k.dat_rog, k.dat_ofor, k.serij, k.nomer, k.vid_dat, k.vid_mes, k.kod_podrazd," +
                 " k.namereg as namereg, k.kod_namereg_prn," +
                //"  -------------адрес откуда прибыл -------" +
                 " sl. land as land_op, " +
                 " st. stat as stat_op," +
                 " stn. town as town_op," +
                 " sr. rajon as rajon_op," +
                 " k.rem_op," +
                //" ----------------------------------------" +
                //"-----------Адрес куда прибыл(текущий для карточки)-------------------------------------------------------------------------------------------------------"+
                 " sl2.land as land, " +
                 " st2.stat as stat, " +
                 " stn2.town as town," +
                 " sr2.rajon as rajon," +
                 " su.ulica as ulica," +
                 " su.ulicareg as ulicareg," +
                 " d.ndom as ndom," +
                 " d.nkor as nkor," +
                 " kv.nkvar as nkvar," +
                 " rd.rajon_dom," +
                // "----------------------------------------------------------------------------------------------------------------------------------------------------------" +
                 " k. dat_oprp, k.nzp_tkrt, " +
                 " sd. dok, " +
                 mropku +
                 " ln_mr.land as lnmr, st_mr.stat as stmr, tn_mr.town as tnmr, rn_mr.rajon as rnmr, " +
                 "  k.rem_ku, kuln.land as land_ku,  kust.stat as stat_ku, kutn.town as town_ku, kurn.rajon as rajon_ku, k.rem_mr " +
                 " From " + cur_pref + "_data : " + my_kart + " k" +
                 ", " + pref + "_data: kvar kv" +
                 ", " + pref + "_data: dom d" +
                 ", outer " + pref + "_data: s_land sl" +
                 ", outer " + pref + "_data:  s_stat st" +
                 ", outer " + pref + "_data:  s_town stn" +
                 ", outer " + pref + "_data:  s_rajon sr" +
                 ", outer " + cur_pref + "_data:  s_dok sd" +
                 ", outer (" + pref + "_data: s_ulica su" +
                    ", outer (" + pref + "_data:  s_rajon sr2" +
                        ", outer (" + pref + "_data:  s_town stn2" +
                            ", outer (" + pref + "_data:  s_stat st2" +
                                ", outer " + pref + "_data: s_land sl2))))" +
                 ", outer " + cur_pref + "_data:s_rajon_dom rd, " +
                 " OUTER " + pref + "_data:s_land ln_mr, OUTER " + pref + "_data:s_stat st_mr, " +
                 " OUTER " + pref + "_data:s_town tn_mr, OUTER " + pref + "_data:s_rajon rn_mr, " +
                 " outer " + pref + "_data:s_land kuln, outer " + pref + "_data:  s_stat kust, " +
                 " outer " + pref + "_data:  s_town kutn, outer " + pref + "_data:  s_rajon kurn " +
                 " Where " +
                 "  nvl(k.neuch,0)<>'1'" +
                 " and k.nzp_kart = " + finder.nzp_kart +
                 " and k.nzp_kvar = kv.nzp_kvar" +
                 " and kv.nzp_dom = d.nzp_dom " +
                //" -------------адрес откуда прибыл -------"+
                 " and k. nzp_lnop =  sl.nzp_land" +
                 " and k.nzp_stop =  st.nzp_stat" +
                 " and k. nzp_tnop =  stn. nzp_town" +
                 " and k. nzp_rnop =  sr. nzp_raj" +
                // "----------------------------------------- "+
                //----------Адрес куда прибыл-----------------
                 " and  d.nzp_ul = su.nzp_ul" +
                 " and  su.nzp_raj = sr2.nzp_raj" +
                 " and  sr2.nzp_town = stn2.nzp_town" +
                 " and  stn2.nzp_stat = st2.nzp_stat" +
                 " and  st2.nzp_land = sl2.nzp_land" +
                 " and  d.nzp_raj = rd.nzp_raj_dom" +
                //--------------------------------------------
                 " and k.nzp_lnmr=ln_mr.nzp_land and k.nzp_stmr=st_mr.nzp_stat and k.nzp_tnmr=tn_mr.nzp_town " +
                 " and k.nzp_rnmr=rn_mr.nzp_raj " +
                 " and sd. nzp_dok = k.nzp_dok" +
                 "  and k.nzp_lnku = kuln.nzp_land and k.nzp_stku = kust.nzp_stat and k.nzp_tnku = kutn.nzp_town " +
                 " and k.nzp_rnku = kurn.nzp_raj " +
                 " order by k.fam"
                     );
#endif
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            try
            {
                while (reader.Read())
                {

                    Kart zap = new Kart();
                    if (reader["nzp_kart"] != DBNull.Value) zap.nzp_kart = Convert.ToString(reader["nzp_kart"]).Trim();
                    if (reader["namereg"] != DBNull.Value) zap.namereg = Convert.ToString(reader["namereg"]).Trim();
                    if (reader["kod_namereg_prn"] != DBNull.Value) zap.kod_namereg_prn = Convert.ToString(reader["kod_namereg_prn"]).Trim();
                    if (reader["fam"] != DBNull.Value) zap.fam = Convert.ToString(reader["fam"]).Trim();
                    if (reader["ima"] != DBNull.Value) zap.ima = Convert.ToString(reader["ima"]).Trim();
                    if (reader["otch"] != DBNull.Value) zap.otch = Convert.ToString(reader["otch"]).Trim();
                    if (reader["gender"] != DBNull.Value) zap.gender = Convert.ToString(reader["gender"]).Trim();
                    if (reader["dat_rog"] != DBNull.Value) zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();
                    if (reader["serij"] != DBNull.Value) zap.serij = Convert.ToString(reader["serij"]).Trim();
                    if (reader["nomer"] != DBNull.Value) zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                    if (reader["vid_dat"] != DBNull.Value) zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();
                    if (reader["vid_mes"] != DBNull.Value) zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                    if (reader["kod_podrazd"] != DBNull.Value) zap.kod_podrazd = Convert.ToString(reader["kod_podrazd"]).Trim();

                    if (reader["who_pvu"] != DBNull.Value) zap.who_pvu = Convert.ToString(reader["who_pvu"]).Trim();
                    if (reader["dat_pvu"] != DBNull.Value) zap.dat_pvu = Convert.ToString(reader["dat_pvu"]).Trim();
                    if (reader["dat_svu"] != DBNull.Value) zap.dat_svu = Convert.ToString(reader["dat_svu"]).Trim();
                    if (reader["jobpost"] != DBNull.Value) zap.jobpost = Convert.ToString(reader["jobpost"]).Trim();
                    if (reader["jobname"] != DBNull.Value) zap.jobname = Convert.ToString(reader["jobname"]).Trim();

                    if (reader["land_op"] != DBNull.Value) zap.land_op = Convert.ToString(reader["land_op"]).Trim();
                    if (reader["stat_op"] != DBNull.Value) zap.stat_op = Convert.ToString(reader["stat_op"]).Trim();
                    if (reader["town_op"] != DBNull.Value) zap.town_op = Convert.ToString(reader["town_op"]).Trim();
                    if (reader["rajon_op"] != DBNull.Value) zap.rajon_op = Convert.ToString(reader["rajon_op"]).Trim();
                    if (reader["rem_op"] != DBNull.Value) zap.rem_op = Convert.ToString(reader["rem_op"]).Trim();

                    if (reader["land_ku"] != DBNull.Value) zap.lnku = Convert.ToString(reader["land_ku"]).Trim();
                    if (reader["stat_ku"] != DBNull.Value) zap.stku = Convert.ToString(reader["stat_ku"]).Trim();
                    if (reader["town_ku"] != DBNull.Value) zap.tnku = Convert.ToString(reader["town_ku"]).Trim();
                    if (reader["rajon_ku"] != DBNull.Value) zap.rnku = Convert.ToString(reader["rajon_ku"]).Trim();
                    if (reader["rem_ku"] != DBNull.Value) zap.rem_ku = Convert.ToString(reader["rem_ku"]).Trim();

                    if (reader["lnmr"] != DBNull.Value) zap.lnmr = Convert.ToString(reader["lnmr"]).Trim();
                    if (reader["stmr"] != DBNull.Value) zap.stmr = Convert.ToString(reader["stmr"]).Trim();
                    if (reader["tnmr"] != DBNull.Value) zap.tnmr = Convert.ToString(reader["tnmr"]).Trim();
                    if (reader["rnmr"] != DBNull.Value) zap.rnmr = Convert.ToString(reader["rnmr"]).Trim();

                    if (reader["land"] != DBNull.Value) zap.land = Convert.ToString(reader["land"]).Trim();
                    if (reader["stat"] != DBNull.Value) zap.stat = Convert.ToString(reader["stat"]).Trim();
                    if (reader["town"] != DBNull.Value) zap.town = Convert.ToString(reader["town"]).Trim();
                    if (reader["rajon"] != DBNull.Value) zap.rajon = Convert.ToString(reader["rajon"]).Trim();
                    if (zap.rajon == "" || zap.rajon == "-")
                    {
                        if (reader["rajon_dom"] != DBNull.Value)
                            zap.rajon = Convert.ToString(reader["rajon_dom"]).Trim();
                    }
                    if (reader["ulica"] != DBNull.Value) zap.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["ulicareg"] != DBNull.Value) zap.ulicareg = Convert.ToString(reader["ulicareg"]).Trim();
                    if (reader["ndom"] != DBNull.Value) zap.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) zap.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["nkvar"] != DBNull.Value) zap.nkvar = Convert.ToString(reader["nkvar"]).Trim();


                    if (reader["dat_oprp"] != DBNull.Value) zap.dat_oprp = Convert.ToString(reader["dat_oprp"]).Trim();
                    if (reader["dat_ofor"] != DBNull.Value) zap.dat_ofor = Convert.ToString(reader["dat_ofor"]).Trim();
                    if (reader["dok"] != DBNull.Value) zap.dok = Convert.ToString(reader["dok"]).Trim();

                    if (reader["nzp_tkrt"] != DBNull.Value) zap.nzp_tkrt = Convert.ToString(reader["nzp_tkrt"]).Trim();

                    if (Points.Region != Regions.Region.Tatarstan)
                    {
                        zap.strana_mr = Convert.ToString(reader["strana_mr"]).Trim();
                        zap.region_mr = Convert.ToString(reader["region_mr"]).Trim();
                        zap.okrug_mr = Convert.ToString(reader["okrug_mr"]).Trim();
                        zap.gorod_mr = Convert.ToString(reader["gorod_mr"]).Trim();
                        zap.npunkt_mr = Convert.ToString(reader["npunkt_mr"]).Trim();

                        zap.strana_op = Convert.ToString(reader["strana_op"]).Trim();
                        zap.region_op = Convert.ToString(reader["region_op"]).Trim();
                        zap.okrug_op = Convert.ToString(reader["okrug_op"]).Trim();
                        zap.gorod_op = Convert.ToString(reader["gorod_op"]).Trim();
                        zap.npunkt_op = Convert.ToString(reader["npunkt_op"]).Trim();

                        zap.strana_ku = Convert.ToString(reader["strana_ku"]).Trim();
                        zap.region_ku = Convert.ToString(reader["region_ku"]).Trim();
                        zap.okrug_ku = Convert.ToString(reader["okrug_ku"]).Trim();
                        zap.gorod_ku = Convert.ToString(reader["gorod_ku"]).Trim();
                        zap.npunkt_ku = Convert.ToString(reader["npunkt_ku"]).Trim();

                        zap.dat_smert = Convert.ToString(reader["dat_smert"]).Trim();
                        zap.dat_fio_c = Convert.ToString(reader["dat_fio_c"]).Trim();
                        zap.rod = Convert.ToString(reader["rodstvo"]).Trim();

                    }
                    else
                    {
                        if (reader["rem_mr"] != DBNull.Value) zap.rem_mr = Convert.ToString(reader["rem_mr"]).Trim();
                    }
                    Spis.Add(zap);
                }


                reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой              
                return Spis;
            }
            finally
            {
                conn_db.Close(); //закрыть соединение с основной базой
            }



        }

        public void CopyToOwner(Gilec finder, List<string> checkedList, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.tag = -1;
                return;
            }
            string owner_where = String.Join(", ", checkedList.ToArray());
            string tXX_cnt = "";
            if (finder.prevPage == Constants.page_kvargil) tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + "_gilkvar";
            if (tXX_cnt == "")
            {
                ret.result = false;
                ret.text = "Не определена страница";
                return;
            }
            // проверка подключения
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            if (!CasheExists(tXX_cnt))
            {
                ret.result = true;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                conn_web.Close();
                return;
            }
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

#if PG
            string sql = " Select " +
                             " is_checked,fam,ima,otch,dat_rog,nzp_kvar,nzp_dok,serij,nomer,vid_mes,vid_dat,adr,nzp_gil,isactual " +
                             " From " + tXX_cnt + " where nzp_serial in (" + owner_where + ")";
#else
            string sql = " Select " +
                   " is_checked,fam,ima,otch,dat_rog,nzp_kvar,nzp_dok,serij,nomer,vid_mes,vid_dat,adr,nzp_gil,isactual " +
                   " From " + tXX_cnt + " where nzp_serial in (" + owner_where + ")";
#endif
            List<Kart> kartList = new List<Kart>();
            IDataReader reader;
            if (!ExecRead(conn_web, out reader,
                sql, true).result)
            {
                conn_web.Close();
                ret.result = false;
                return;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Kart zap = new Kart();

                    zap.num = (i + finder.skip).ToString();

                    if ((reader["is_checked"] == DBNull.Value)
                        || ((int)reader["is_checked"] == 0))
                        zap.is_checked = false;
                    else
                        zap.is_checked = true;

                    if (reader["nzp_kvar"] == DBNull.Value)
                        zap.nzp_kvar = 0;
                    else
                        zap.nzp_kvar = (int)reader["nzp_kvar"];

                    if (reader["adr"] == DBNull.Value)
                        zap.adr = "";
                    else
                        zap.adr = (string)reader["adr"];

                    zap.nzp_gil = Convert.ToString(reader["nzp_gil"]).Trim();
                    zap.fam = Convert.ToString(reader["fam"]).Trim();
                    zap.ima = Convert.ToString(reader["ima"]).Trim();
                    zap.otch = Convert.ToString(reader["otch"]).Trim();
                    zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();
                    zap.isactual = Convert.ToString(reader["isactual"]).Trim();
                    zap.nzp_dok = Convert.ToString(reader["nzp_dok"]);

                    // документ, удостоверяющий личность
                    if (reader["serij"] != DBNull.Value) zap.serij = Convert.ToString(reader["serij"]).Trim();
                    if (reader["nomer"] != DBNull.Value) zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                    if (reader["vid_mes"] != DBNull.Value) zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                    if (reader["vid_dat"] != DBNull.Value) zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();

                    kartList.Add(zap);
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения списка карточек жильцов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return;
            }
            int kol_add = 0;
            int kol_not_add = 0;
            StringBuilder insert_str = new StringBuilder();
            foreach (var kart in kartList)
            {
                DataTable dt = ClassDBUtils.OpenSQL("select count(*) from " + finder.pref + "_data" + DBManager.tableDelimiter + "sobstw where nzp_kvar =" + kart.nzp_kvar +
                    " and fam='" + kart.fam + "' and  ima= '" + kart.ima + "' and otch='" + kart.otch + "' and is_actual = 1", conn_db).GetData();
                if (Convert.ToInt32(dt.Rows[0][0]) == 0)
                {
                    insert_str.Append("insert into " + finder.pref + "_data" + DBManager.tableDelimiter +
                                      "sobstw(nzp_kvar,fam,ima,otch,dat_rog,adress,nzp_dok,serij,nomer,vid_mes,vid_dat,nzp_gil,is_actual) ");
                    insert_str.Append("values (" + kart.nzp_kvar + ",'" + kart.fam + "','" + kart.ima + "','" +
                                      kart.otch + "','" + Convert.ToDateTime(kart.dat_rog).ToString("dd.MM.yyyy") + "',");
                    insert_str.Append(" '" + kart.adr + "'," + kart.nzp_dok + ",'" + kart.serij + "','" + kart.nomer +
                                      "','" + kart.vid_mes + "'," +
                                      (kart.vid_dat.Length == 0
                                          ? "null"
                                          : "'" + Convert.ToDateTime(kart.vid_dat).ToString("dd.MM.yyyy") + "'") + "," +
                                      kart.nzp_gil + "," + kart.isactual + ");");

                    kol_add++;
                }
                else
                {
                    kol_not_add++;
                }
            }
            if (insert_str.Length > 0)
            {
                ret = ExecSQL(conn_db, insert_str.ToString(), true);
                if (!ret.result)
                {
                    conn_web.Close();
                    conn_db.Close();
                    return;
                }
            }
            ret.result = true;
            ret.text = "В собственники скопировано " + kol_add + (kol_add > 1 && kol_add < 5 ? " карточки" : (kol_add == 1 ? " карточка" : " карточек."));
            if (kol_not_add > 0)
                ret.text += " В собственники не были скопированы карточки в количестве " + kol_not_add + ", так как собственники с таким именем уже добавлены";

        }

        public void MakeResponsible(Otvetstv finder, out Returns ret)
        {
            ret = new Returns();
            IDbConnection conn_db = null;
            IDbConnection conn_web = null;

            try
            {
                if (finder.nzp_user < 1) throw new Exception("Не определен пользователь");

                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);

                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) throw new Exception(ret.text);


                Param prm = new Param();
                prm.nzp_prm = 46;
                prm.prm_num = 3;
                prm.is_day_uchet = 1;
                prm.pref = finder.pref;
                prm.type_prm = "char";
                prm.norm_type_id = 0;
                prm.dat_s = finder.dat_s == "" ? Points.DateOper.ToShortDateString() : finder.dat_s;
                prm.dat_po = finder.dat_po == "" ? "01.01.3000" : finder.dat_po;
                prm.nzp = finder.nzp_kvar;
                prm.val_prm = finder.fam;
                prm.nzp_user = finder.nzp_user;
                using (DbParameters dbparam = new DbParameters())
                {
                    ret = dbparam.SavePrm(prm);
                }
                if (!ret.result) throw new Exception(ret.text);

                string sql =
                    " UPDATE " + Points.Pref + DBManager.sDataAliasRest + "kvar" +
                    " SET (fio)=((" +
                    " SELECT trim(p.val_prm) FROM " + finder.pref + DBManager.sDataAliasRest + " prm_3 p" +
                    " WHERE p.is_actual <> 100 AND nzp_prm = 46 AND p.nzp = " + finder.nzp_kvar + " AND" +
                    " p.dat_s <= '" + Points.DateOper.ToShortDateString() + "' AND " +
                    " p.dat_po >= '" + Points.DateOper.ToShortDateString() + "'))" +
                    " WHERE nzp_kvar=" + finder.nzp_kvar;
                ret = ExecSQL(conn_db, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql =
                    " UPDATE " + finder.pref + DBManager.sDataAliasRest + "kvar" +
                    " SET (fio)=((" +
                    " SELECT trim(p.val_prm) FROM " + finder.pref + DBManager.sDataAliasRest + " prm_3 p " +
                    " WHERE p.is_actual <> 100 AND nzp_prm = 46 AND p.nzp = " + finder.nzp_kvar + " AND" +
                    " p.dat_s <= '" + Points.DateOper.ToShortDateString() + "' AND " +
                    " p.dat_po >= '" + Points.DateOper.ToShortDateString() + "'))" +
                    " WHERE nzp_kvar=" + finder.nzp_kvar;
                ret = ExecSQL(conn_db, sql);
                if (!ret.result) throw new Exception(ret.text);

                string sch = "";
#if PG
                sch = "public.";
#endif
                sql =
                    " UPDATE " + sch + "t" + finder.nzp_user + "_spls " +
                    " SET (fio)=((" +
                    " SELECT trim(p.val_prm) FROM " + finder.pref + DBManager.sDataAliasRest + " prm_3 p" +
                    " WHERE p.is_actual <> 100 AND nzp_prm = 46 AND p.nzp = " + finder.nzp_kvar + " AND" +
                    " p.dat_s <= '" + Points.DateOper.ToShortDateString() + "' AND " +
                    " p.dat_po >= '" + Points.DateOper.ToShortDateString() + "'))" +
                    " WHERE nzp_kvar=" + finder.nzp_kvar + " and pref=" + Utils.EStrNull(finder.pref);
                ret = ExecSQL(conn_web, sql);
                if (!ret.result) throw new Exception(ret.text);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
                if (conn_web != null) conn_web.Close();
            }

            return;
        }

        //----------------------------------------------------------------------
        public Kart KartForSprav(Kart finder, out Returns ret) //вытащить Карточку жильца для справок
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();
            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            string a_data = finder.pref + "_data.";
#else
            string a_data = finder.pref + "_data:";
#endif
            DbTables tables = new DbTables(conn_web);

            string my_kart = "kart";
            if (finder.is_arx)
            {
                my_kart = "kart_arx";
            }

            string mropku = "";
            if (Points.Region != Regions.Region.Tatarstan)
            {
                mropku = " c.strana_mr, c.region_mr, c.okrug_mr, c.gorod_mr, c.npunkt_mr," +
                        " c.strana_op, c.region_op, c.okrug_op, c.gorod_op, c.npunkt_op," +
                        " c.strana_ku, c.region_ku, c.okrug_ku, c.gorod_ku, c.npunkt_ku," +
                        " c.dat_smert, c.dat_fio_c, c.rodstvo,";
            }

            //выбрать карточку
            string sqlStr =
                 " Select '" + finder.pref + "' as pref , " +
#if PG
 "  trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '||" +
                 "  trim(coalesce(d.ndom,''))||'  корп. '|| trim(coalesce(d.nkor,''))||'  кв. '||trim(coalesce(k.nkvar,''))||'  ком. '||trim(coalesce(k.nkvar_n,'')) as adr," +
#else
 "  trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" +
                 "  trim(nvl(d.ndom,''))||'  корп. '|| trim(nvl(d.nkor,''))||'  кв. '||trim(nvl(k.nkvar,''))||'  ком. '||trim(nvl(k.nkvar_n,'')) as adr," +
#endif
 "   c.nzp_tkrt," +
                 "   case when c.isactual='1' then '1' else ' ' end as isactual," +
                 " cp.cel as celp," +
                 " cu.cel as celu," +
                 "   case when c.tprp='В' then 'ПРЕБЫВАНИЯ' else 'ЖИТЕЛЬСТВА' end as tprp," +
                 " ln_mr.land as lnmr, st_mr.stat as stmr, tn_mr.town as tnmr, rn_mr.rajon as rnmr," +
                 " ln_op.land as lnop, st_op.stat as stop, tn_op.town as tnop, rn_op.rajon as rnop," +
                 " ln_ku.land as lnku, st_ku.stat as stku, tn_ku.town as tnku, rn_ku.rajon as rnku," +
                 " us.comment," +
                //для Самары
                 mropku +

                 " *" +
#if PG
 " From " + tables.kvar + " k, " +
                 tables.dom + " d " +
                 " left outer join " + a_data + "s_rajon_dom rd on d.nzp_raj=rd.nzp_raj_dom, " +
                 tables.ulica + " u, " + tables.rajon + " r, " +
                 " " + a_data + my_kart + " c " +
                 " left outer join " + a_data + "s_dok sd on c.nzp_dok=sd.nzp_dok " +
                 " left outer join " + a_data + "s_cel cp on c.nzp_celp=cp.nzp_cel " +
                 " left outer join " + a_data + "s_cel cu on c.nzp_celu=cu.nzp_cel " +
                 " left outer join " + a_data + "s_rod sr on c.nzp_rod=sr.nzp_rod " +
                 " left outer join " + tables.land + " ln_mr on c.nzp_lnmr=ln_mr.nzp_land " +
                 " left outer join " + tables.stat + " st_mr on c.nzp_stmr=st_mr.nzp_stat " +
                 " left outer join " + tables.town + " tn_mr on c.nzp_tnmr=tn_mr.nzp_town " +
                 " left outer join " + tables.rajon + " rn_mr on c.nzp_rnmr=rn_mr.nzp_raj " +
                 " left outer join " + tables.land + " ln_op on c.nzp_lnop=ln_op.nzp_land " +
                 " left outer join " + tables.stat + " st_op on c.nzp_stop=st_op.nzp_stat " +
                 " left outer join " + tables.town + " tn_op on c.nzp_tnop=tn_op.nzp_town " +
                 " left outer join " + tables.rajon + " rn_op on c.nzp_rnop=rn_op.nzp_raj " +
                 " left outer join " + tables.land + " ln_ku on c.nzp_lnku=ln_ku.nzp_land " +
                 " left outer join " + tables.stat + " st_ku on c.nzp_stku=st_ku.nzp_stat " +
                 " left outer join " + tables.town + " tn_ku on c.nzp_tnku=tn_ku.nzp_town " +
                 " left outer join " + tables.rajon + " rn_ku on c.nzp_rnku=rn_ku.nzp_raj " +
                 " left outer join " + Points.Pref + DBManager.sDataAliasRest + "users us on c.nzp_user=(-1)* us.nzp_user, " +
                  tables.land + " ln," +
                  tables.stat + " st," +
                  tables.town + " tn " +
                 " WHERE c.nzp_kart = " + finder.nzp_kart +
                 " and c.nzp_kvar = k.nzp_kvar" +
                 " and k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj" +
                 " and st.nzp_land=ln.nzp_land" +
                 " and tn.nzp_stat=st.nzp_stat" +
                 " and r.nzp_town=tn.nzp_town ";
#else
 " From " + tables.kvar + " k, " + tables.dom + " d, " + tables.ulica + " u, " + tables.rajon + " r, " +
                 " OUTER " + a_data + "s_dok sd," +
                 " OUTER " + a_data + "s_cel cp," +
                 " OUTER " + a_data + "s_cel cu," +
                 " OUTER " + a_data + "s_rod sr," +
                  tables.land + " ln," +
                  tables.stat + " st," +
                  tables.town + " tn," +
                 " OUTER " + a_data + "s_rajon_dom rd," +
                 " OUTER " + tables.land + " ln_mr," +
                 " OUTER " + tables.stat + " st_mr," +
                 " OUTER " + tables.town + " tn_mr," +
                 " OUTER " + tables.rajon + " rn_mr," +
                 " OUTER " + tables.land + " ln_op," +
                 " OUTER " + tables.stat + " st_op," +
                 " OUTER " + tables.town + " tn_op," +
                 " OUTER " + tables.rajon + " rn_op," +
                 " OUTER " + tables.land + " ln_ku," +
                 " OUTER " + tables.stat + " st_ku," +
                 " OUTER " + tables.town + " tn_ku," +
                 " OUTER " + tables.rajon + " rn_ku," +
                 " OUTER " + Points.Pref + DBManager.sDataAliasRest + "users us," +
                 " " + a_data + my_kart + " c " +
                 " WHERE c.nzp_kart = " + finder.nzp_kart +
                 " and   c.nzp_kvar = k.nzp_kvar" +
                 " and   k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj" +
                 " and c.nzp_dok=sd.nzp_dok" +
                 " and c.nzp_celp=cp.nzp_cel" +
                 " and c.nzp_celu=cu.nzp_cel" +
                 " and c.nzp_rod=sr.nzp_rod" +
                 " and st.nzp_land=ln.nzp_land" +
                 " and tn.nzp_stat=st.nzp_stat" +
                 " and r.nzp_town=tn.nzp_town" +
                 " and d.nzp_raj=rd.nzp_raj_dom" +
                 " and c.nzp_lnmr=ln_mr.nzp_land" +
                 " and c.nzp_stmr=st_mr.nzp_stat" +
                 " and c.nzp_tnmr=tn_mr.nzp_town" +
                 " and c.nzp_rnmr=rn_mr.nzp_raj" +
                 " and c.nzp_lnop=ln_op.nzp_land" +
                 " and c.nzp_stop=st_op.nzp_stat" +
                 " and c.nzp_tnop=tn_op.nzp_town" +
                 " and c.nzp_rnop=rn_op.nzp_raj" +
                 " and c.nzp_lnku=ln_ku.nzp_land" +
                 " and c.nzp_stku=st_ku.nzp_stat" +
                 " and c.nzp_tnku=tn_ku.nzp_town" +
                 " and c.nzp_rnku=rn_ku.nzp_raj" +
                 " and c.nzp_user=(-1)* us.nzp_user" +
                 "";
#endif

            IDataReader reader;

            if (!ExecRead(conn_web, out reader, sqlStr, true).result)
            {
                conn_web.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при загрузке карточки жильца для справки";
                return null;
            }
            try
            {
                if (!reader.Read())
                {
                    ret.result = false;
                    ret.text = "Карточка не найдена";
                    return null;
                }
                Kart zap = new Kart();

                if (reader["num_ls"] == DBNull.Value)
                    zap.num_ls = 0;
                else
                    zap.num_ls = (int)reader["num_ls"];

                if (reader["nzp_kvar"] == DBNull.Value)
                    zap.nzp_kvar = 0;
                else
                    zap.nzp_kvar = (int)reader["nzp_kvar"];

                if (reader["nzp_dom"] == DBNull.Value)
                    zap.nzp_dom = 0;
                else
                    zap.nzp_dom = (int)reader["nzp_dom"];

                if (reader["adr"] == DBNull.Value)
                    zap.adr = "";
                else
                    zap.adr = ((string)reader["adr"]).Trim();


                zap.nzp_gil = Convert.ToString(reader["nzp_gil"]).Trim();
                zap.nzp_kart = Convert.ToString(reader["nzp_kart"]).Trim();
                zap.fam = Convert.ToString(reader["fam"]).Trim();
                zap.ima = Convert.ToString(reader["ima"]).Trim();
                zap.otch = Convert.ToString(reader["otch"]).Trim();
                //                    zap.dat_rog = ((DateTime)reader["dat_rog"]).ToString("dd.MM.yyyy");
                zap.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();

                zap.fam_c = Convert.ToString(reader["fam_c"]).Trim();
                zap.ima_c = Convert.ToString(reader["ima_c"]).Trim();
                zap.otch_c = Convert.ToString(reader["otch_c"]).Trim();
                zap.dat_rog_c = Convert.ToString(reader["dat_rog_c"]).Trim();

                zap.gender = Convert.ToString(reader["gender"]).Trim();


                zap.nzp_dok = Convert.ToString(reader["nzp_dok"]).Trim();
                zap.dok = Convert.ToString(reader["dok"]).Trim();
                zap.serij = Convert.ToString(reader["serij"]).Trim();
                zap.nomer = Convert.ToString(reader["nomer"]).Trim();
                zap.vid_mes = Convert.ToString(reader["vid_mes"]).Trim();
                zap.vid_dat = Convert.ToString(reader["vid_dat"]).Trim();
                zap.kod_podrazd = Convert.ToString(reader["kod_podrazd"]).Trim();

                zap.tprp = Convert.ToString(reader["tprp"]).Trim();

                zap.nzp_tkrt = Convert.ToString(reader["nzp_tkrt"]).Trim();

                zap.isactual = Convert.ToString(reader["isactual"]).Trim();

                zap.jobpost = Convert.ToString(reader["jobpost"]).Trim();
                zap.jobname = Convert.ToString(reader["jobname"]).Trim();
                zap.who_pvu = Convert.ToString(reader["who_pvu"]).Trim();
                zap.dat_pvu = Convert.ToString(reader["dat_pvu"]).Trim();
                zap.dat_svu = Convert.ToString(reader["dat_svu"]).Trim();
                zap.namereg = Convert.ToString(reader["namereg"]).Trim();
                zap.kod_namereg_prn = Convert.ToString(reader["kod_namereg_prn"]).Trim();

                zap.nzp_rod = Convert.ToString(reader["nzp_rod"]).Trim();
                zap.rod = Convert.ToString(reader["rod"]).Trim();     //для Самары дальше меняется

                zap.nzp_celp = Convert.ToString(reader["nzp_celp"]).Trim();
                zap.celp = Convert.ToString(reader["celp"]).Trim();
                zap.nzp_celu = Convert.ToString(reader["nzp_celu"]).Trim();
                zap.celu = Convert.ToString(reader["celu"]).Trim();
                zap.ndees = Convert.ToString(reader["ndees"]).Trim();
                zap.neuch = Convert.ToString(reader["neuch"]).Trim();

                zap.nzp_land = Convert.ToString(reader["nzp_land"]).Trim();
                zap.nzp_stat = Convert.ToString(reader["nzp_stat"]).Trim();
                zap.nzp_town = Convert.ToString(reader["nzp_town"]).Trim();
                //zap.nzp_raj = Convert.ToString(reader["nzp_raj"]);

                //zap.nzp_ul = Convert.ToString(reader["nzp_ul"]);
                //zap.nzp_dom = Convert.ToString(reader["nzp_dom"]);
                //zap.nzp_kvar = Convert.ToString(reader["nzp_kvar"]);

                zap.rajon_dom = Convert.ToString(reader["rajon_dom"]).Trim();

                zap.land = Convert.ToString(reader["land"]).Trim();
                zap.stat = Convert.ToString(reader["stat"]).Trim();
                zap.town = Convert.ToString(reader["town"]).Trim();
                zap.rajon = Convert.ToString(reader["rajon"]).Trim();
                zap.ulica = Convert.ToString(reader["ulica"]).Trim();
                zap.ulicareg = Convert.ToString(reader["ulicareg"]).Trim();
                zap.ndom = Convert.ToString(reader["ndom"]).Trim();
                zap.nkor = Convert.ToString(reader["nkor"]).Trim();
                zap.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                zap.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();

                zap.nzp_lnmr = Convert.ToString(reader["nzp_lnmr"]).Trim();
                zap.nzp_stmr = Convert.ToString(reader["nzp_stmr"]).Trim();
                zap.nzp_tnmr = Convert.ToString(reader["nzp_tnmr"]).Trim();
                zap.nzp_rnmr = Convert.ToString(reader["nzp_rnmr"]).Trim();
                zap.lnmr = Convert.ToString(reader["lnmr"]).Trim();
                zap.stmr = Convert.ToString(reader["stmr"]).Trim();
                zap.tnmr = Convert.ToString(reader["tnmr"]).Trim();
                zap.rnmr = Convert.ToString(reader["rnmr"]).Trim();
                zap.rem_mr = Convert.ToString(reader["rem_mr"]).Trim();

                zap.nzp_lnop = Convert.ToString(reader["nzp_lnop"]).Trim();
                zap.nzp_stop = Convert.ToString(reader["nzp_stop"]).Trim();
                zap.nzp_tnop = Convert.ToString(reader["nzp_tnop"]).Trim();
                zap.nzp_rnop = Convert.ToString(reader["nzp_rnop"]).Trim();
                zap.lnop = Convert.ToString(reader["lnop"]).Trim();
                zap.stop = Convert.ToString(reader["stop"]).Trim();
                zap.tnop = Convert.ToString(reader["tnop"]).Trim();
                zap.rnop = Convert.ToString(reader["rnop"]).Trim();
                zap.rem_op = Convert.ToString(reader["rem_op"]).Trim();

                zap.nzp_lnku = Convert.ToString(reader["nzp_lnku"]).Trim();
                zap.nzp_stku = Convert.ToString(reader["nzp_stku"]).Trim();
                zap.nzp_tnku = Convert.ToString(reader["nzp_tnku"]).Trim();
                zap.nzp_rnku = Convert.ToString(reader["nzp_rnku"]).Trim();
                zap.lnku = Convert.ToString(reader["lnku"]).Trim();
                zap.stku = Convert.ToString(reader["stku"]).Trim();
                zap.tnku = Convert.ToString(reader["tnku"]).Trim();
                zap.rnku = Convert.ToString(reader["rnku"]).Trim();
                zap.rem_ku = Convert.ToString(reader["rem_ku"]).Trim();


                zap.dat_prop = Convert.ToString(reader["dat_prop"]).Trim();
                zap.dat_oprp = Convert.ToString(reader["dat_oprp"]).Trim();

                zap.dat_ofor = Convert.ToString(reader["dat_ofor"]).Trim();
                zap.dat_sost = Convert.ToString(reader["dat_sost"]).Trim();

                zap.dat_izm = Convert.ToString(reader["dat_izm"]).Trim();
                zap.webUname = Convert.ToString(reader["comment"]).Trim();

                zap.pref = Convert.ToString(reader["pref"]).Trim();

                if (Points.Region != Regions.Region.Tatarstan)
                {
                    zap.strana_mr = Convert.ToString(reader["strana_mr"]).Trim();
                    zap.region_mr = Convert.ToString(reader["region_mr"]).Trim();
                    zap.okrug_mr = Convert.ToString(reader["okrug_mr"]).Trim();
                    zap.gorod_mr = Convert.ToString(reader["gorod_mr"]).Trim();
                    zap.npunkt_mr = Convert.ToString(reader["npunkt_mr"]).Trim();

                    zap.strana_op = Convert.ToString(reader["strana_op"]).Trim();
                    zap.region_op = Convert.ToString(reader["region_op"]).Trim();
                    zap.okrug_op = Convert.ToString(reader["okrug_op"]).Trim();
                    zap.gorod_op = Convert.ToString(reader["gorod_op"]).Trim();
                    zap.npunkt_op = Convert.ToString(reader["npunkt_op"]).Trim();

                    zap.strana_ku = Convert.ToString(reader["strana_ku"]).Trim();
                    zap.region_ku = Convert.ToString(reader["region_ku"]).Trim();
                    zap.okrug_ku = Convert.ToString(reader["okrug_ku"]).Trim();
                    zap.gorod_ku = Convert.ToString(reader["gorod_ku"]).Trim();
                    zap.npunkt_ku = Convert.ToString(reader["npunkt_ku"]).Trim();

                    zap.dat_smert = Convert.ToString(reader["dat_smert"]).Trim();
                    zap.dat_fio_c = Convert.ToString(reader["dat_fio_c"]).Trim();
                    zap.rod = Convert.ToString(reader["rodstvo"]).Trim();

                }

                #region Поля для справок
                if (zap.nzp_tkrt == "1")        // прибытие
                {
                    if (zap.dat_prop == "")
                        zap.dat_prop = zap.dat_ofor;
                    if (zap.dat_oprp != "")
                        zap.dat_vip = zap.dat_oprp;
                    else
                    {
                        // Если дата оформления не пустая
                        if (!String.IsNullOrWhiteSpace(zap.dat_ofor))
                        {
                           // проверяем на корректность
                            string formatedDateOfor = Utils.FormatDate(zap.dat_ofor);
                             // Если оказалась корректной
                            if (!String.IsNullOrWhiteSpace(formatedDateOfor))
                            {
                                IDataReader reader_m;
                                string whereDatOfor = " and k.dat_ofor >date('" + formatedDateOfor + "')";

#if PG

                                ret = ExecRead(conn_web, out reader_m,
                                    " Select k.dat_ofor " +
                                    " From " + a_data + my_kart + " k " +
                                    " where k.nzp_gil =" + zap.nzp_gil +
                                    " and k.nzp_kvar=" + zap.nzp_kvar +
                                    " and k.nzp_tkrt=2" +
                                    " and coalesce(neuch::int,'0' )<>'1'" +
                                    whereDatOfor +
                                    " ORDER BY 1  limit 1", true);
                                if (!ret.result)
                                {
                                    conn_web.Close();
                                    return null;
                                }
#else
                        if (!ExecRead(conn_web, out reader_m,
                                                     " Select first 1 k.dat_ofor " +
                                                     " From " + a_data + my_kart + " k " +
                                                     " where k.nzp_gil =" + zap.nzp_gil +
                                                     " and k.nzp_kvar=" + zap.nzp_kvar +
                                                     " and k.nzp_tkrt=2" +
                                                     " and nvl(neuch,'0' )<>'1'" +
                                                       whereDatOfor +
                                                    " ORDER BY 1", true).result)
                        {
                            conn_web.Close();
                            ret.result = false;
                            return null;
                        }
#endif

                                if (reader_m.Read())
                                {
                                    zap.dat_vip = Convert.ToString(reader_m["dat_ofor"]).Trim();
                                }
                                reader_m.Close();
                            }
                        }
                    }
                }
                else                          // убытие
                {
                    if (zap.dat_prop == "")
                    {
                        IDataReader reader_m;
#if PG
                        if (!ExecRead(conn_web, out reader_m,
                                                     " Select  case when k.dat_prop is not null then k.dat_prop else k.dat_ofor end as my_date" +
                                                     " From " + a_data + my_kart + " k " +
                                                     " where k.nzp_gil =" + zap.nzp_gil +
                                                     " and k.nzp_kvar=" + zap.nzp_kvar +
                                                     " and k.nzp_tkrt=1" +
                                                     " and coalesce(neuch::int,'0' )<>'1'" +
                                                     " and case when k.dat_prop is not null then k.dat_prop else k.dat_ofor end  < date('" + zap.dat_ofor + "')" +
                                                    " ORDER BY 1 DESC limit 1", true).result)
                        {
                            conn_web.Close();
                            ret.result = false;
                            return null;
                        }
#else
                        if (!ExecRead(conn_web, out reader_m,
                                                     " Select first 1 case when k.dat_prop is not null then k.dat_prop else k.dat_ofor end as my_date" +
                                                     " From " + a_data + my_kart + " k " +
                                                     " where k.nzp_gil =" + zap.nzp_gil +
                                                     " and k.nzp_kvar=" + zap.nzp_kvar +
                                                     " and k.nzp_tkrt=1" +
                                                     " and nvl(neuch,'0' )<>'1'" +
                                                     " and case when k.dat_prop is not null then k.dat_prop else k.dat_ofor end  < date('" + zap.dat_ofor + "')" +
                                                    " ORDER BY 1 DESC", true).result)
                        {
                            conn_web.Close();
                            ret.result = false;
                            return null;
                        }
#endif
                        if (reader_m.Read())
                        {
                            zap.dat_prop = Convert.ToString(reader_m["my_date"]).Trim();
                        }
                        reader_m.Close();

                    }
                    zap.dat_vip = zap.dat_ofor;
                }

                #endregion ;



                // гражданства
                if (!finder.is_arx)
                {
                    IDataReader reader_g;
                    if (!ExecRead(conn_web, out reader_g,
                         " Select  '" + finder.pref + "' as pref , g.nzp_kart, g.nzp_grgd, s.grgd" +
                         " From " + a_data + "grgd g, " + a_data + "s_grgd s where g.nzp_grgd=s.nzp_grgd   and g.nzp_kart =" + zap.nzp_kart, true).result)
                    {
                        conn_web.Close();
                        ret.result = false;
                        return null;
                    }

                    while (reader_g.Read())
                    {
                        KartGrgd grgd = new KartGrgd();
                        grgd.nzp_kart = Convert.ToString(reader_g["nzp_kart"]).Trim();
                        grgd.nzp_grgd = Convert.ToString(reader_g["nzp_grgd"]).Trim();
                        grgd.grgd = Convert.ToString(reader_g["grgd"]).Trim();
                        grgd.pref = Convert.ToString(reader_g["pref"]).Trim();
                        zap.listKartGrgd.Add(grgd);
                    }
                    reader_g.Close();
                }
                // гражданства end
                reader.Close();
                conn_web.Close();
                return zap;
            }
            catch (Exception ex)
            {

                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки карточки жильца  для справки " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public FullAddress LoadFullAddress(Ls finder, out Returns ret) //вытащить полный адрес
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string a_data = finder.pref + "_data.";
#else
            string a_data = finder.pref + "_data:";
#endif

            //выбрать карточку
#if PG
            string sqlStr =
                                " SELECT l.nzp_land, l.land, s.nzp_stat, s.stat ,t.nzp_town, t.town, r.nzp_raj, r.rajon," +
                                " u.nzp_ul, u.ulica, u.ulicareg,d.nzp_dom,d.ndom,d.nkor, k.nzp_kvar,k.nkvar,k.nkvar_n, " +
                                " rd.nzp_raj_dom, rd.rajon_dom" +
                                " from  " + Points.Pref + "_data.s_land l, " + Points.Pref + "_data.s_stat s, " +
                                Points.Pref + "_data.s_town t, " + Points.Pref + "_data.s_rajon r," + Points.Pref + "_data.s_ulica u, " +
                                Points.Pref + "_data.dom d " +
                                " left outer join " + a_data + "s_rajon_dom rd on d.nzp_raj=rd.nzp_raj_dom, " +
                                Points.Pref + "_data.kvar k" +
                                " where k.nzp_kvar=" + finder.nzp_kvar +
                                " and k.nzp_dom=d.nzp_dom" +
                                " and d.nzp_ul=u.nzp_ul" +
                                " and u.nzp_raj=r.nzp_raj" +
                                " and r.nzp_town=t.nzp_town" +
                                " and t.nzp_stat=s.nzp_stat" +
                                " and s.nzp_land=l.nzp_land";
#else
            string sqlStr =
                                           " SELECT l.nzp_land, l.land, s.nzp_stat, s.stat ,t.nzp_town, t.town, r.nzp_raj, r.rajon," +
                                                       " u.nzp_ul, u.ulica, u.ulicareg,d.nzp_dom,d.ndom,d.nkor, k.nzp_kvar,k.nkvar,k.nkvar_n, " +
                                                       " rd.nzp_raj_dom, rd.rajon_dom" +
                                           " from  OUTER " + a_data + "s_rajon_dom rd, " + Points.Pref + "_data:s_land l, " + Points.Pref + "_data:s_stat s, " + Points.Pref + "_data:s_town t, " + Points.Pref + "_data:s_rajon r," +
                                           " " + Points.Pref + "_data:s_ulica u, " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:kvar k" +
                                           " where k.nzp_kvar=" + finder.nzp_kvar +
                                           " and k.nzp_dom=d.nzp_dom" +
                                           " and d.nzp_ul=u.nzp_ul" +
                                           " and u.nzp_raj=r.nzp_raj" +
                                           " and r.nzp_town=t.nzp_town" +
                                           " and t.nzp_stat=s.nzp_stat" +
                                           " and s.nzp_land=l.nzp_land" +
                                           " and d.nzp_raj=rd.nzp_raj_dom";
#endif

            IDataReader reader;

            if (!ExecRead(conn_db, out reader, sqlStr, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }
            try
            {
                if (!reader.Read())
                {
                    ret.result = false;
                    ret.text = "Полный адрес не определен";
                    return null;
                }
                FullAddress zap = new FullAddress();

                zap.nzp_kvar = finder.nzp_kvar.ToString();

                zap.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                zap.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                zap.nzp_dom = Convert.ToString(reader["nzp_dom"]).Trim();
                zap.ndom = Convert.ToString(reader["ndom"]).Trim();
                zap.nkor = Convert.ToString(reader["nkor"]).Trim();
                zap.nzp_ul = Convert.ToString(reader["nzp_ul"]).Trim();
                zap.ulica = Convert.ToString(reader["ulica"]).Trim();
                zap.ulicareg = Convert.ToString(reader["ulicareg"]).Trim();
                zap.nzp_raj = Convert.ToString(reader["nzp_raj"]).Trim();
                zap.rajon = Convert.ToString(reader["rajon"]).Trim();
                zap.nzp_town = Convert.ToString(reader["nzp_town"]).Trim();
                zap.town = Convert.ToString(reader["town"]).Trim();
                zap.nzp_stat = Convert.ToString(reader["nzp_stat"]).Trim();
                zap.stat = Convert.ToString(reader["stat"]).Trim();
                zap.nzp_land = Convert.ToString(reader["nzp_land"]).Trim();
                zap.land = Convert.ToString(reader["land"]).Trim();
                zap.nzp_raj_dom = Convert.ToString(reader["nzp_raj_dom"]).Trim();
                zap.rajon_dom = Convert.ToString(reader["rajon_dom"]).Trim();

                reader.Close();
                conn_db.Close();
                return zap;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки полного Адреса " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<Kart> NeighborKart(Kart finder, out Returns ret) // Все соседи по коммунальной квартире для кправки на приватизацию
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();
            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return null;
            }

            if ((finder.nzp_kvar == Constants._ZERO_)
            || (finder.nzp_kvar.ToString() == ""))
            {
                ret.result = false;
                ret.text = "ID квартиры не задан";
                ret.tag = -1;
                return null;
            }


            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader;
            List<Kart> Spis = new List<Kart>();

            string my_kart = "kart";
            if (finder.is_arx)
            {
                my_kart = "kart_arx";
            }
            string my_rod = "";
            if (Points.Region != Regions.Region.Tatarstan) my_rod = " case when k.nzp_kart is null then 'собств.' else  k.rodstvo end as rodstvo, ";

#if PG
            string sql =
                                   "  select   " +
                                   "  k1.nzp_kvar, " +
                                   "  k1.pkod, " +
                                   "  k.nzp_kart, " +
                                   "  k.nzp_tkrt, " +
                                   "  case when k1.nzp_kvar=" + finder.nzp_kvar.ToString() + " then  1 else 100 end as sem, " + // флаг нужного ЛС
                                   "  case when  k.nzp_kart is null then   " +        // нет прописанных
                                   "  coalesce( (select max(p.val_prm) from " + finder.pref + "_data.prm_19 p  " +
                                   "  where  current_date between p.dat_s and p.dat_po  " +
                                   "  and p.is_actual<>100  " +
                                   "  and p.nzp_prm=2022 " +
                                   "  and p.nzp=k1.nzp_kvar " +
                                   "  ), k1.fio)   " +  //  квартиросъемщик ПУС или РЦ
                                   "  else upper(k.fam) end as fam , " + // Жилец
                                   "  upper(k.ima) as ima, upper(k.otch) as otch,  " +
                                   "  k.dat_rog, " +
                                   "  case when  k.nzp_kart  is null then 'собств.' else  r.rod end as rod, " +
                                   my_rod +
                                   "  case when  k.nzp_kart is null  then   'не зарегистр.' " +
                                   "  else  (case when k.tprp ='В' then 'по пребыванию' else 'постоянно' end ) end  as tprp, " +
                                   "  coalesce(k.dat_prop,k.dat_ofor) as dat_prop, k.dat_oprp,   " +
                                   " (select max(p.val_prm) from " + finder.pref + "_data.prm_1 p  " +
                                   " where  current_date between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm=107 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " ) as komn, " +// количество комнат
                                   " coalesce( (select max(p.val_prm) from " + finder.pref + "_data.prm_19 p  " +
                                   " where  current_date between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm=2016 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " ) ,  " +
                                   " (select max(p.val_prm) from " + finder.pref + "_data.prm_1 p  " +
                                   " where  current_date between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm=6 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " )  )  as gs, " +// жилая площадь ПУС или РЦ
                                   " coalesce( (select max(p.val_prm) from " + finder.pref + "_data.prm_19 p  " +
                                   " where current_date between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm = 2015 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " ) ,  " +
                                   " (select max(p.val_prm) from " + finder.pref + "_data.prm_1 p  " +
                                   " where current_date between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm = 4 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " )  )  as totsq, " + // общая площадь ПУС или РЦ
                                   " ( " +
                                   " select  max(y.name_y) from " + finder.pref + "_data.prm_1 p , " + finder.pref + "_kernel.res_y y " +
                                   " where  current_date between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm=2009 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " and y.nzp_res=3001 " +
                                   " and p.val_prm::int =y.nzp_y " +
                                   " ) as privat " +// статус приватизированно или нет
                                   " from " + finder.pref + "_data.kvar k1 " +
                                   " left outer join " + finder.pref + "_data." + my_kart + " k " +
                                        " left outer join " + finder.pref + "_data.s_rod r on k.nzp_rod=r.nzp_rod " +
                                   "  on k.nzp_kvar=k1.nzp_kvar " +
                                   " WHERE coalesce(k.neuch::int,'0') <>'1' " +
                                   " and coalesce(dat_oprp,date('01.01.3000')) > current_date " +
                                   " and k.isactual='1' ";
            string sosedi =
                        " and " +
                        " ( exists (select 1 from " + finder.pref + "_data.prm_3 p1 " +
                        " where p1.nzp_prm=51" +
                        " and k1.nzp_kvar=p1.nzp" +
                        " and current_date between p1.dat_s and p1.dat_po" +
                        " and p1.is_actual<>100" +
                        " and p1.val_prm='1')" +
                        "  OR k1.nzp_kvar=" + finder.nzp_kvar.ToString() + ")" + // открытые ЛС или нужный
                        " and exists (" +
                        " select 1 from " + finder.pref + "_data.kvar k2  " +
                        " where k2.nzp_dom=k1.nzp_dom " + // в том же доме
                        " and k2.nkvar=k1.nkvar " +       // тот же номер квартиры
                        " and k2.nzp_kvar=" + finder.nzp_kvar.ToString() +
                                   " ) ";
#else
            string sql =
                                   "  select   " +
                                   "  k1.nzp_kvar, " +
                                   "  k1.pkod, " +
                                   "  case when k1.nzp_kvar=" + finder.nzp_kvar.ToString() + " then  1 else 100 end as sem, " + // флаг нужного ЛС
                                   "  case when  k.nzp_kart is null then   " +        // нет прописанных
                                   "  nvl( (select max(p.val_prm) from " + finder.pref + "_data:prm_19 p  " +
                                   "  where  today between p.dat_s and p.dat_po  " +
                                   "  and p.is_actual<>100  " +
                                   "  and p.nzp_prm=2022 " +
                                   "  and p.nzp=k1.nzp_kvar " +
                                   "  ), k1.fio)   " +  //  квартиросъемщик ПУС или РЦ
                                   "  else upper(k.fam) end as fam , " + // Жилец
                                   "  upper(k.ima) as ima, upper(k.otch) as otch,  " +
                                   "  k.dat_rog, " +
                                   "  case when  k.nzp_kart  is null then 'собств.' else  r.rod end as rod, " +
                                   my_rod +
                                   "  case when  k.nzp_kart is null  then   'не зарегистр.' " +
                                   "  else  (case when k.tprp ='В' then 'по пребыванию' else 'постоянно' end ) end  as tprp, " +
                                   "  nvl(k.dat_prop,k.dat_ofor) as dat_prop, k.dat_oprp,   " +
                                   " (select max(p.val_prm) from " + finder.pref + "_data:prm_1 p  " +
                                   " where  today between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm=107 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " ) as komn, " +// количество комнат
                                   " nvl( (select max(p.val_prm) from " + finder.pref + "_data:prm_19 p  " +
                                   " where  today between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm=2016 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " ) ,  " +
                                   " (select max(p.val_prm) from " + finder.pref + "_data:prm_1 p  " +
                                   " where  today between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm=6 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " )  )  as gs, " +// жилая площадь ПУС или РЦ
                                   " nvl( (select max(p.val_prm) from " + finder.pref + "_data:prm_19 p  " +
                                   " where today between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm = 2015 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " ) ,  " +
                                   " (select max(p.val_prm) from " + finder.pref + "_data:prm_1 p  " +
                                   " where today between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm = 4 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " )  )  as totsq, " + // общая площадь ПУС или РЦ
                                   " ( " +
                                   " select  max(y.name_y) from " + finder.pref + "_data:prm_1 p , " + finder.pref + "_kernel:res_y y " +
                                   " where  today between p.dat_s and p.dat_po  " +
                                   " and p.is_actual<>100  " +
                                   " and p.nzp_prm=2009 " +
                                   " and p.nzp=k1.nzp_kvar " +
                                   " and y.nzp_res=3001 " +
                                   " and p.val_prm =y.nzp_y " +
                                   " ) as privat " +// статус приватизированно или нет
                                   " from " + finder.pref + "_data:kvar k1,  OUTER (" + finder.pref + "_data:" + my_kart + " k, outer " + finder.pref + "_data:s_rod r)" +
                                   " " +
                                   " WHERE k.nzp_kvar=k1.nzp_kvar " +
                                   " and k.nzp_tkrt=1 " +
                                   " and nvl(k.neuch,'0') <>'1' " +
                                   " and nvl(dat_oprp,date('01.01.3000'))>today " +
                                   " and k.isactual='1' " +
                                   " and k.nzp_rod=r.nzp_rod ";
            string sosedi =
                        " and " +
                        " ( exists (select 1 from " + finder.pref + "_data:prm_3 p1 " +
                        " where p1.nzp_prm=51" +
                        " and k1.nzp_kvar=p1.nzp" +
                        " and today between p1.dat_s and p1.dat_po" +
                        " and p1.is_actual<>100" +
                        " and p1.val_prm='1')" +
                        "  OR k1.nzp_kvar=" + finder.nzp_kvar.ToString() + ")" + // открытые ЛС или нужный
                        " and exists (" +
                        " select 1 from " + finder.pref + "_data:kvar k2  " +
                        " where k2.nzp_dom=k1.nzp_dom " + // в том же доме
                        " and k2.nkvar=k1.nkvar " +       // тот же номер квартиры
                        " and k2.nzp_kvar=" + finder.nzp_kvar.ToString() +
                                   " ) ";
#endif
            string tkrt = string.Empty;
            if (finder.dopFind != null)
                if (finder.dopFind.Count > 0) //учесть дополнительные шаблоны
                {
                    foreach (string s in finder.dopFind)
                    {
                        if (s == "НЕ УЧИТЫВАТЬ СОСЕДЕЙ")
                        {
                            sosedi = " and k1.nzp_kvar=" + finder.nzp_kvar.ToString();
                        }

                        if (s == "ВСЕ ТИПЫ")
                            tkrt = " and k.nzp_tkrt in (1,2) ";
                        else
                            tkrt = " and k.nzp_tkrt = 1 ";
                    }
                }

            sql = sql + tkrt + sosedi + " order by sem, k1.nzp_kvar , k.dat_rog ";
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    Kart zap = new Kart();
                    zap.num = i.ToString();
                    if (reader["nzp_kvar"] == DBNull.Value) zap.nzp_kvar = 0; else zap.nzp_kvar = (int)reader["nzp_kvar"];

                    zap.pkod = (reader["pkod"] != DBNull.Value) ? Convert.ToDecimal(reader["pkod"]).ToString("00").Trim() : "";
                    zap.nzp_kart = (reader["nzp_kart"] != DBNull.Value) ? Convert.ToString(reader["nzp_kart"]).Trim() : "";
                    zap.nzp_tkrt = (reader["nzp_tkrt"] != DBNull.Value) ? Convert.ToString(reader["nzp_tkrt"]).Trim() : "";

                    zap.fam = (reader["fam"] != DBNull.Value) ? Convert.ToString(reader["fam"]).Trim() : "";
                    zap.ima = (reader["ima"] != DBNull.Value) ? Convert.ToString(reader["ima"]).Trim() : "";
                    zap.otch = (reader["otch"] != DBNull.Value) ? Convert.ToString(reader["otch"]).Trim() : "";
                    zap.dat_rog = (reader["dat_rog"] != DBNull.Value) ? Convert.ToString(reader["dat_rog"]).Trim() : "";
                    zap.rod = (reader["rod"] != DBNull.Value) ? Convert.ToString(reader["rod"]).Trim() : "";

                    if (Points.Region != Regions.Region.Tatarstan)
                        zap.rod = (reader["rodstvo"] != DBNull.Value) ? Convert.ToString(reader["rodstvo"]).Trim() : "";

                    zap.tprp = (reader["tprp"] != DBNull.Value) ? Convert.ToString(reader["tprp"]).Trim() : "";
                    zap.dat_prop = (reader["dat_prop"] != DBNull.Value) ? Convert.ToString(reader["dat_prop"]).Trim() : "";
                    zap.dat_oprp = (reader["dat_oprp"] != DBNull.Value) ? Convert.ToString(reader["dat_oprp"]).Trim() : "";

                    zap.dopParams = new List<Prm>();

                    zap.dopParams.Add(new Prm() { nzp_prm = 1, name_prm = "флаг ЛС, на который справка", val_prm = (reader["sem"] != DBNull.Value) ? Convert.ToString(reader["sem"]).Trim() : "" });
                    zap.dopParams.Add(new Prm() { nzp_prm = 2, name_prm = "количество комнат", val_prm = (reader["komn"] != DBNull.Value) ? Convert.ToString(reader["komn"]).Trim() : "" });
                    zap.dopParams.Add(new Prm() { nzp_prm = 3, name_prm = "метраж жилой площади", val_prm = (reader["gs"] != DBNull.Value) ? Convert.ToString(reader["gs"]).Trim() : "" });
                    zap.dopParams.Add(new Prm() { nzp_prm = 4, name_prm = "статус", val_prm = (reader["privat"] != DBNull.Value) ? Convert.ToString(reader["privat"]).Trim() : "" });
                    zap.dopParams.Add(new Prm() { nzp_prm = 5, name_prm = "метраж общей площади", val_prm = (reader["totsq"] != DBNull.Value) ? Convert.ToString(reader["totsq"]).Trim() : "" });

                    Spis.Add(zap);
                }

                reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой
                return Spis;
            }
            catch (Exception ex)
            {

                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка выборки карточек соседей для справки на приватизацию " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }


        //----------------------------------------------------------------------
        public string TotalRooms(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {

            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------

            ret = Utils.InitReturns();
            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Префикс базы данных не задан";
                ret.tag = -1;
                return "";
            }

            if ((finder.nzp_kvar == Constants._ZERO_)
            || (finder.nzp_kvar.ToString() == ""))
            {
                ret.result = false;
                ret.text = "ID квартиры не задан";
                ret.tag = -1;
                return "";
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return "";

            string res = "";

#if PG
            string sql = "select max(val_prm)" +
                                           " from " + finder.pref + "_data.prm_1" +
                                           "  where nzp_prm=3" +
                                           "  and is_actual<>100" +
                                           "  and  current_date between dat_s and dat_po" +
                                           "  and nzp=" + finder.nzp_kvar;
#else
            string sql = "select max(val_prm)" +
                                           " from " + finder.pref + "_data:prm_1" +
                                           "  where nzp_prm=3" +
                                           "  and is_actual<>100" +
                                           "  and  today between dat_s and dat_po" +
                                           "  and nzp=" + finder.nzp_kvar;
#endif

            try
            {
#if PG
                if (Convert.ToString(ExecScalarThr(conn_db, sql, out ret, true)).Trim() == "2")// коммунальное
                {
                    sql =
                        " select sum(coalesce(p.val_prm,0)+0) " +
                        " from " + finder.pref + "_data.prm_1 p  " +
                        " where  current_date between p.dat_s and p.dat_po  " +
                        " and p.is_actual<>100  " +
                        " and p.nzp_prm=107 " +
                        " and p.nzp in (" +
                        " select nzp_kvar " +
                        " from " + finder.pref + "_data.kvar k" +
                        " where " +
                        " ( exists (" +
                        "    select 1 from " + finder.pref + "_data.prm_3 p1 " +
                        "    where p1.nzp_prm=51" +
                        "    and k.nzp_kvar=p1.nzp" +
                        "    and current_date between p1.dat_s and p1.dat_po" +
                        "    and p1.is_actual<>100" +
                        "    and p1.val_prm='1')" +
                        //                        "     OR k.nzp_kvar=" + finder.nzp_kvar + 
                        " )" +
                        " and exists (" +
                        "     select 1 from " + finder.pref + "_data.kvar k2 " +
                        "     where k2.nzp_dom=k.nzp_dom " +
                        "     and k2.nkvar=k.nkvar" +
                        "     and k2.nzp_kvar=" + finder.nzp_kvar + " )   " +
                        " )";
                    res = Convert.ToInt32(ExecScalarThr(conn_db, sql, out ret, true)).ToString("#");
                }
                else
                {
                    sql = "select max(val_prm)" +
                               " from " + finder.pref + "_data.prm_1" +
                               "  where nzp_prm=107" +
                               "  and is_actual<>100" +
                               "  and  current_date between dat_s and dat_po" +
                               "  and nzp=" + finder.nzp_kvar;
                    res = Convert.ToString(ExecScalarThr(conn_db, sql, out ret, true)).Trim();
                }
#else
                if (Convert.ToString(ExecScalarThr(conn_db, sql, out ret, true)).Trim() == "2")// коммунальное
                {
                    sql =
                        " select sum(nvl(p.val_prm,0)+0) " +
                        " from " + finder.pref + "_data:prm_1 p  " +
                        " where  today between p.dat_s and p.dat_po  " +
                        " and p.is_actual<>100  " +
                        " and p.nzp_prm=107 " +
                        " and p.nzp in (" +
                        " select nzp_kvar " +
                        " from " + finder.pref + "_data:kvar k" +
                        " where " +
                        " ( exists (" +
                        "    select 1 from " + finder.pref + "_data:prm_3 p1 " +
                        "    where p1.nzp_prm=51" +
                        "    and k.nzp_kvar=p1.nzp" +
                        "    and today between p1.dat_s and p1.dat_po" +
                        "    and p1.is_actual<>100" +
                        "    and p1.val_prm='1')" +
                        //                        "     OR k.nzp_kvar=" + finder.nzp_kvar + 
                        " )" +
                        " and exists (" +
                        "     select 1 from " + finder.pref + "_data:kvar k2 " +
                        "     where k2.nzp_dom=k.nzp_dom " +
                        "     and k2.nkvar=k.nkvar" +
                        "     and k2.nzp_kvar=" + finder.nzp_kvar + " )   " +
                        " )";
                    res = Convert.ToInt32(ExecScalarThr(conn_db, sql, out ret, true)).ToString("#");
                }
                else
                {
                    sql = "select max(val_prm)" +
                               " from " + finder.pref + "_data:prm_1" +
                               "  where nzp_prm=107" +
                               "  and is_actual<>100" +
                               "  and  today between dat_s and dat_po" +
                               "  and nzp=" + finder.nzp_kvar;
                    res = Convert.ToString(ExecScalarThr(conn_db, sql, out ret, true)).Trim();
                }
#endif

                conn_db.Close(); //закрыть соединение с основной базой
                return res;
            }
            catch (Exception ex)
            {

                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка получения количества комнат " + err, MonitorLog.typelog.Error, 20, 201, true);

                return "";
            }

        }
    }
}

namespace STCLINE.KP50.DataBase.Server
{
    public class DbServerGilec : DataBaseHead
    {
        public List<PaspCelPrib> FindCelPrib(PaspCelPrib finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Префикс базы данных не задан");
                return null;
            }
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string sql = "select nzp_cel, cel, nzp_tkrt from " + finder.pref + "_data.s_cel Where 1=1";
#else
            string sql = "select nzp_cel, cel, nzp_tkrt from " + finder.pref + "_data:s_cel Where 1=1";
#endif
            if (finder.nzp_tkrt > 0) sql += " and nzp_tkrt = " + finder.nzp_tkrt;
            if (finder.nzp_cel > 0) sql += " and nzp_cel = " + finder.nzp_cel;
            sql += " Order by cel";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<PaspCelPrib> list = new List<PaspCelPrib>();

            try
            {
                while (reader.Read())
                {
                    PaspCelPrib zap = new PaspCelPrib();
                    if (reader["nzp_cel"] != DBNull.Value) zap.nzp_cel = Convert.ToInt32(reader["nzp_cel"]);
                    if (reader["cel"] != DBNull.Value) zap.cel = Convert.ToString(reader["cel"]).Trim();
                    if (reader["nzp_tkrt"] != DBNull.Value) zap.nzp_tkrt = Convert.ToInt32(reader["nzp_tkrt"]);
                    list.Add(zap);
                }
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой
            }
            return list;
        }

        public Returns SaveCelPrib(PaspCelPrib finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.pref == "") return new Returns(false, "Не задан префикс базы данных");
            if (finder.cel.Trim() == "") return new Returns(false, "Не задано наименование цели");
            if (finder.nzp_cel < 1 && finder.nzp_tkrt < 1) return new Returns(false, "Не задан тип карточки");
            finder.cel = finder.cel.Trim().ToLower();
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
#if PG
            string sql = "select nzp_cel from " + finder.pref + "_data.s_cel" +
                " where nzp_tkrt = " + finder.nzp_tkrt + " and cel = " + Utils.EStrNull(finder.cel, "") + " and nzp_cel <> " + finder.nzp_cel;
#else
            string sql = "select nzp_cel from " + finder.pref + "_data:s_cel" +
                " where nzp_tkrt = " + finder.nzp_tkrt + " and cel = " + Utils.EStrNull(finder.cel, "") + " and nzp_cel <> " + finder.nzp_cel;
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            if (reader.Read())
            {
                reader.Close();
                conn_db.Close();
                return new Returns(false, "Цель прибытия/убытия с таким наименованием уже существует", -1);
            }
            reader.Close();
#if PG
            if (finder.nzp_cel > 0) sql = "update " + finder.pref + "_data.s_cel set cel = " + Utils.EStrNull(finder.cel, "") + " where nzp_cel = " + finder.nzp_cel;
            else sql = "insert into " + finder.pref + "_data.s_cel (nzp_cel, cel, nzp_tkrt) values (default, " + Utils.EStrNull(finder.cel, "") + ", " + finder.nzp_tkrt + ")";
#else
            if (finder.nzp_cel > 0) sql = "update " + finder.pref + "_data:s_cel set cel = " + Utils.EStrNull(finder.cel, "") + " where nzp_cel = " + finder.nzp_cel;
            else sql = "insert into " + finder.pref + "_data:s_cel (nzp_cel, cel, nzp_tkrt) values (0, " + Utils.EStrNull(finder.cel, "") + ", " + finder.nzp_tkrt + ")";
#endif
            ret = ExecSQL(conn_db, sql, true);
            if (ret.result && finder.nzp_cel < 1)
            {
                ret.tag = GetSerialValue(conn_db);
            }
            conn_db.Close();
            return ret;
        }

        public List<PaspDoc> FindDocs(PaspDoc finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Префикс базы данных не задан");
                return null;
            }
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string sql = "select nzp_dok, dok, serij_mask, nomer_mask, nzp_oso from " + finder.pref + "_data.s_dok Where 1=1";
#else
            string sql = "select nzp_dok, dok, serij_mask, nomer_mask, nzp_oso from " + finder.pref + "_data:s_dok Where 1=1";
#endif
            if (finder.nzp_dok > 0) sql += " and nzp_dok = " + finder.nzp_dok;
            sql += " Order by dok";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<PaspDoc> list = new List<PaspDoc>();

            try
            {
                while (reader.Read())
                {
                    PaspDoc zap = new PaspDoc();
                    if (reader["nzp_dok"] != DBNull.Value) zap.nzp_dok = Convert.ToInt32(reader["nzp_dok"]);
                    if (reader["dok"] != DBNull.Value) zap.dok = Convert.ToString(reader["dok"]).Trim();
                    if (reader["serij_mask"] != DBNull.Value) zap.serij_mask = Convert.ToString(reader["serij_mask"]).Trim();
                    if (reader["nomer_mask"] != DBNull.Value) zap.nomer_mask = Convert.ToString(reader["nomer_mask"]).Trim();
                    if (reader["nzp_oso"] != DBNull.Value) zap.nzp_oso = Convert.ToString(reader["nzp_oso"]).Trim();
                    list.Add(zap);
                }
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой
            }
            return list;
        }

        public Returns SaveDoc(PaspDoc finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.pref == "") return new Returns(false, "Не задан префикс базы данных");
            if (finder.dok.Trim() == "") return new Returns(false, "Не задан тип документа", -1);
            //if (finder.serij_mask.Trim() == "") return new Returns(false, "Не задана маска серии", -1);
            //if (finder.nomer_mask.Trim() == "") return new Returns(false, "Не задана маска номера", -1);
            //if (finder.nzp_oso.Trim() == "") return new Returns(false, "Не задан код для выгрузки", -1);
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
#if PG
            string sql = "select nzp_dok from " + finder.pref + "_data.s_dok" +
                " where upper(dok) = " + Utils.EStrNull(finder.dok.ToUpper(), "") + " and nzp_dok <> " + finder.nzp_dok;
#else
            string sql = "select nzp_dok from " + finder.pref + "_data:s_dok" +
                " where upper(dok) = " + Utils.EStrNull(finder.dok.ToUpper(), "") + " and nzp_dok <> " + finder.nzp_dok;
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            if (reader.Read())
            {
                reader.Close();
                conn_db.Close();
                return new Returns(false, "Документ с таким наименованием уже существует", -1);
            }
            reader.Close();

#if PG
            if (finder.nzp_dok > 0)
            {
                sql = "update " + finder.pref + "_data.s_dok set dok = " + Utils.EStrNull(finder.dok.Trim(), "") +
                    ", serij_mask = " + Utils.EStrNull(finder.serij_mask.Trim(), "") +
                    ", nomer_mask = " + Utils.EStrNull(finder.nomer_mask.Trim(), "") +
                    ", nzp_oso = " + Utils.EStrNull(finder.nzp_oso.Trim(), "") +
                    " where nzp_dok = " + finder.nzp_dok;
            }
            else
            {
                sql = "insert into " + finder.pref + "_data.s_dok (nzp_dok, dok, serij_mask, nomer_mask, nzp_oso)" +
                    " values (default, " + Utils.EStrNull(finder.dok.Trim(), "") +
                    ", " + Utils.EStrNull(finder.serij_mask.Trim(), "") +
                    ", " + Utils.EStrNull(finder.nomer_mask.Trim(), "") +
                    ", " + Utils.EStrNull(finder.nzp_oso.Trim(), "") + ")";
            }
#else
            if (finder.nzp_dok > 0)
            {
                sql = "update " + finder.pref + "_data:s_dok set dok = " + Utils.EStrNull(finder.dok.Trim(), "") +
                    ", serij_mask = " + Utils.EStrNull(finder.serij_mask.Trim(), "") +
                    ", nomer_mask = " + Utils.EStrNull(finder.nomer_mask.Trim(), "") +
                    ", nzp_oso = " + Utils.EStrNull(finder.nzp_oso.Trim(), "") +
                    " where nzp_dok = " + finder.nzp_dok;
            }
            else
            {
                sql = "insert into " + finder.pref + "_data:s_dok (nzp_dok, dok, serij_mask, nomer_mask, nzp_oso)" +
                    " values (0, " + Utils.EStrNull(finder.dok.Trim(), "") +
                    ", " + Utils.EStrNull(finder.serij_mask.Trim(), "") +
                    ", " + Utils.EStrNull(finder.nomer_mask.Trim(), "") +
                    ", " + Utils.EStrNull(finder.nzp_oso.Trim(), "") + ")";
            }
#endif

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result && finder.nzp_dok < 1)
            {
                ret.tag = GetSerialValue(conn_db);
            }
            conn_db.Close();
            return ret;
        }

        private struct Dependency
        {
            public string tabNameFrom;
            public string fieldNameFrom;
            public string tabNameTo;
            public string fieldNameTo;

            public Dependency(string _tabNameFrom, string _fieldNameFrom, string _tabNameTo, string _fieldNameTo)
            {
                tabNameFrom = _tabNameFrom;
                fieldNameFrom = _fieldNameFrom;
                tabNameTo = _tabNameTo;
                fieldNameTo = _fieldNameTo;
            }
        }

        private List<Dependency> GetDependencies(string tabNameTo)
        {
            List<Dependency> list = new List<Dependency>();

            tabNameTo = tabNameTo.Trim().ToLower();

            switch (tabNameTo)
            {
                case "s_cel":
                    list.Add(new Dependency("kart", "nzp_celp", "s_cel", "nzp_cel"));
                    list.Add(new Dependency("kart", "nzp_celu", "s_cel", "nzp_cel"));
                    list.Add(new Dependency("kart_arx", "nzp_celp", "s_cel", "nzp_cel"));
                    list.Add(new Dependency("kart_arx", "nzp_celu", "s_cel", "nzp_cel"));
                    break;
                case "s_dok":
                    list.Add(new Dependency("kart", "nzp_dok", "s_dok", "nzp_dok"));
                    list.Add(new Dependency("kart_arx", "nzp_dok", "s_dok", "nzp_dok"));
                    list.Add(new Dependency("sobstw", "nzp_dok", "s_dok", "nzp_dok"));
                    break;
                case "s_rod":
                    list.Add(new Dependency("kart", "nzp_rod", "s_rod", "nzp_rod"));
                    list.Add(new Dependency("kart_arx", "nzp_rod", "s_rod", "nzp_rod"));
                    list.Add(new Dependency("sobstw", "nzp_rod", "s_rod", "nzp_rod"));
                    break;
                case "s_grgd":
                    list.Add(new Dependency("grgd", "nzp_grgd", "s_grgd", "nzp_grgd"));
                    break;
                case "s_rajon_dom":
                    list.Add(new Dependency("dom", "nzp_raj", "s_rajon_dom", "nzp_raj_dom"));
                    break;
                case "s_dok_sv":
                    list.Add(new Dependency("sobstw", "nzp_dok_sv", "s_dok_sv", "nzp_dok_sv"));
                    break;
                case "s_land":
                    list.Add(new Dependency("kart", "nzp_lnku", "s_land", "nzp_land"));
                    list.Add(new Dependency("kart", "nzp_lnmr", "s_land", "nzp_land"));
                    list.Add(new Dependency("kart", "nzp_lnop", "s_land", "nzp_land"));
                    list.Add(new Dependency("kart_arx", "nzp_lnku", "s_land", "nzp_land"));
                    list.Add(new Dependency("kart_arx", "nzp_lnmr", "s_land", "nzp_land"));
                    list.Add(new Dependency("kart_arx", "nzp_lnop", "s_land", "nzp_land"));
                    break;
                case "s_stat":
                    list.Add(new Dependency("kart", "nzp_stku", "s_stat", "nzp_stat"));
                    list.Add(new Dependency("kart", "nzp_stmr", "s_stat", "nzp_stat"));
                    list.Add(new Dependency("kart", "nzp_stop", "s_stat", "nzp_stat"));
                    list.Add(new Dependency("kart_arx", "nzp_stku", "s_stat", "nzp_stat"));
                    list.Add(new Dependency("kart_arx", "nzp_stmr", "s_stat", "nzp_stat"));
                    list.Add(new Dependency("kart_arx", "nzp_stop", "s_stat", "nzp_stat"));
                    break;
                case "s_town":
                    list.Add(new Dependency("kart", "nzp_tnku", "s_town", "nzp_town"));
                    list.Add(new Dependency("kart", "nzp_tnmr", "s_town", "nzp_town"));
                    list.Add(new Dependency("kart", "nzp_tnop", "s_town", "nzp_town"));
                    list.Add(new Dependency("kart_arx", "nzp_tnku", "s_town", "nzp_town"));
                    list.Add(new Dependency("kart_arx", "nzp_tnmr", "s_town", "nzp_town"));
                    list.Add(new Dependency("kart_arx", "nzp_tnop", "s_town", "nzp_town"));
                    break;
                case "s_rajon":
                    list.Add(new Dependency("kart", "nzp_rnku", "s_rajon", "nzp_raj"));
                    list.Add(new Dependency("kart", "nzp_rnmr", "s_rajon", "nzp_raj"));
                    list.Add(new Dependency("kart", "nzp_rnop", "s_rajon", "nzp_raj"));
                    list.Add(new Dependency("kart_arx", "nzp_rnku", "s_rajon", "nzp_raj"));
                    list.Add(new Dependency("kart_arx", "nzp_rnmr", "s_rajon", "nzp_raj"));
                    list.Add(new Dependency("kart_arx", "nzp_rnop", "s_rajon", "nzp_raj"));
                    break;
            }

            return list;
        }

        private bool isHasReferences(IDbConnection connection, string pref, string table, string key_value, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Dependency> list = GetDependencies(table);

            IDataReader reader;
            string tabname_from, sql;
            foreach (Dependency dependency in list)
            {
#if PG
                tabname_from = pref + "_data." + dependency.tabNameFrom;
#else
                tabname_from = pref + "_data:" + dependency.tabNameFrom;
#endif
                if (!TempTableInWebCashe(connection, tabname_from)) continue;
                sql = "select " + dependency.fieldNameFrom + " from " + tabname_from + " where " + dependency.fieldNameFrom + " = " + key_value;
                ret = ExecRead(connection, out reader, sql, true);
                if (!ret.result) return true; // считаем, что ссылки есть
                if (reader.Read())
                {
                    reader.Close();
                    return true; // есть ссылки
                }
                reader.Close();
            }
            return false; // нет ссылок
        }

        public Returns SaveSprav(Sprav finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.pref == "") return new Returns(false, "Не задан префикс базы данных");
            if (finder.val_sprav.Trim() == "") return new Returns(false, "Не задано значение справочника", -1);

            string valfield = "", keyfield = "";
            switch (finder.name_sprav)
            {
                case "s_remark_kart":
                    finder.val_sprav = finder.val_sprav.Trim().ToLower();
                    keyfield = "id_rem";
                    valfield = "remark";
                    break;
                case "s_rod":
                    finder.val_sprav = finder.val_sprav.Trim().ToLower();
                    keyfield = "nzp_rod";
                    valfield = "rod";
                    break;
                case "s_grgd":
                    finder.val_sprav = finder.val_sprav.Trim().ToUpper();
                    keyfield = "nzp_grgd";
                    valfield = "grgd";
                    break;
                case "s_vid_mes":
                    finder.val_sprav = finder.val_sprav.Trim().ToUpper();
                    keyfield = "kod_vid_mes";
                    valfield = "vid_mes";
                    break;
                case "s_dok_sv":
                    keyfield = "nzp_dok_sv";
                    valfield = "dok_sv";
                    break;
                case "s_rajon_dom":
                    finder.val_sprav = finder.val_sprav.Trim().ToUpper();
                    finder.dop_kod = finder.dop_kod.Trim().ToUpper();
                    keyfield = "nzp_raj_dom";
                    valfield = "rajon_dom";
                    break;
                case "s_land":
                case "s_stat":
                case "s_town":
                case "s_rajon":
                    finder.val_sprav = finder.val_sprav.Trim().ToUpper();
                    finder.dop_kod = finder.dop_kod.Trim().ToUpper();
                    break;
                default:
                    return new Returns(false, "Неверно задано наименование справочника");
            }
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string sql;
#if PG
            if (finder.name_sprav == "s_land")
            {
                sql = "select nzp_land from " + finder.pref + "_data.s_land where land = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and nzp_land <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0");
            }
            else if (finder.name_sprav == "s_stat")
            {
                sql = "select nzp_stat from " + finder.pref + "_data.s_stat" +
                    " where stat = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and nzp_stat <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0") + " and nzp_land = " + finder.parent_nzp_sprav;
            }
            else if (finder.name_sprav == "s_town")
            {
                sql = "select nzp_town from " + finder.pref + "_data.s_town" +
                    " where town = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and nzp_town <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0") + " and nzp_stat = " + finder.parent_nzp_sprav;
            }
            else if (finder.name_sprav == "s_rajon")
            {
                sql = "select nzp_raj from " + finder.pref + "_data.s_rajon" +
                    " where rajon = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and nzp_raj <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0") + " and nzp_town = " + finder.parent_nzp_sprav;
            }
            else if (finder.name_sprav == "s_remark_kart")
            {
                sql = "select " + keyfield + " from " + Points.Pref + "_data." + finder.name_sprav +
                    " where upper(" + valfield + ") = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and " + keyfield + " <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0");
            }
            else
            {
                sql = "select " + keyfield + " from " + finder.pref + "_data." + finder.name_sprav +
                    " where upper(" + valfield + ") = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and " + keyfield + " <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0");
            }
#else
            if (finder.name_sprav == "s_land")
            {
                sql = "select nzp_land from " + finder.pref + "_data:s_land where land = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and nzp_land <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0");
            }
            else if (finder.name_sprav == "s_stat")
            {
                sql = "select nzp_stat from " + finder.pref + "_data:s_stat" +
                    " where stat = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and nzp_stat <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0") + " and nzp_land = " + finder.parent_nzp_sprav;
            }
            else if (finder.name_sprav == "s_town")
            {
                sql = "select nzp_town from " + finder.pref + "_data:s_town" +
                    " where town = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and nzp_town <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0") + " and nzp_stat = " + finder.parent_nzp_sprav;
            }
            else if (finder.name_sprav == "s_rajon")
            {
                sql = "select nzp_raj from " + finder.pref + "_data:s_rajon" +
                    " where rajon = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and nzp_raj <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0") + " and nzp_town = " + finder.parent_nzp_sprav;
            }
            else
            {
                sql = "select " + keyfield + " from " + finder.pref + "_data:" + finder.name_sprav +
                    " where upper(" + valfield + ") = " + Utils.EStrNull(finder.val_sprav.ToUpper(), "") + " and " + keyfield + " <> " + Utils.EStrNull(finder.nzp_sprav.ToUpper(), "0");
            }
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            if (reader.Read())
            {
                reader.Close();
                conn_db.Close();
                return new Returns(false, "Значение справочника с таким наименованием уже существует", -1);
            }
            reader.Close();

#if PG
            if (finder.nzp_sprav != "")
            {
                if (finder.name_sprav == "s_remark_kart")
                {
                    sql = "update " + Points.Pref + DBManager.sDataAliasRest + finder.name_sprav + " set " + valfield + " = " + Utils.EStrNull(finder.val_sprav, "");
                    sql += " where " + keyfield + " = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else if (finder.name_sprav == "s_land")
                {
                    sql = "update " + finder.pref + "_data.s_land set land = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", land_t = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", soato = " + Utils.EStrNull(finder.dop_kod, "") +
                        " where nzp_land = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else if (finder.name_sprav == "s_stat")
                {
                    sql = "update " + finder.pref + "_data.s_stat set stat = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", stat_t = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", soato = " + Utils.EStrNull(finder.dop_kod, "") +
                        " where nzp_stat = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else if (finder.name_sprav == "s_town")
                {
                    sql = "update " + finder.pref + "_data.s_town set town = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", town_t = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", soato = " + Utils.EStrNull(finder.dop_kod, "") +
                        " where nzp_town = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else if (finder.name_sprav == "s_rajon")
                {
                    sql = "update " + finder.pref + "_data.s_rajon set rajon = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", rajon_t = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", soato = " + Utils.EStrNull(finder.dop_kod, "") +
                        " where nzp_raj = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else
                {
                    sql = "update " + finder.pref + "_data." + finder.name_sprav + " set " + valfield + " = " + Utils.EStrNull(finder.val_sprav, "");
                    if (finder.name_sprav == "s_rajon_dom") sql += ", alt_rajon_dom = " + Utils.EStrNull(finder.dop_kod, "");
                    sql += " where " + keyfield + " = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
            }
            else
            {
                if (finder.name_sprav == "s_remark_kart")
                {
                    sql = "insert into " + Points.Pref + DBManager.sDataAliasRest + finder.name_sprav + " (" + keyfield + ", " + valfield + ")" +
                          " values (default, " + Utils.EStrNull(finder.val_sprav, "") + ")";
                }
                else
                    if (finder.name_sprav == "s_rajon_dom")
                        sql = "insert into " + finder.pref + "_data.s_rajon_dom (nzp_raj_dom, rajon_dom, alt_rajon_dom)" +
                            " values (default, " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                    else if (finder.name_sprav == "s_land")
                        sql = "insert into " + finder.pref + "_data.s_land (nzp_land, land, land_t, soato)" +
                            " values (default, " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                    else if (finder.name_sprav == "s_stat")
                        sql = "insert into " + finder.pref + "_data.s_stat (nzp_stat, nzp_land, stat, stat_t, soato)" +
                            " values (default, " + finder.parent_nzp_sprav + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                    else if (finder.name_sprav == "s_town")
                        sql = "insert into " + finder.pref + "_data.s_town (nzp_town, nzp_stat, town, town_t, soato)" +
                            " values (default, " + finder.parent_nzp_sprav + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                    else if (finder.name_sprav == "s_rajon")
                        sql = "insert into " + finder.pref + "_data.s_rajon (nzp_raj, nzp_town, rajon, rajon_t, soato)" +
                            " values (default, " + finder.parent_nzp_sprav + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                    else
                        sql = "insert into " + finder.pref + "_data." + finder.name_sprav + " (" + keyfield + ", " + valfield + ") values (default, " + Utils.EStrNull(finder.val_sprav, "") + ")";
            }
#else
            if (finder.nzp_sprav != "")
            {
                if (finder.name_sprav == "s_land")
                {
                    sql = "update " + finder.pref + "_data:s_land set land = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", land_t = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", soato = " + Utils.EStrNull(finder.dop_kod, "") +
                        " where nzp_land = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else if (finder.name_sprav == "s_stat")
                {
                    sql = "update " + finder.pref + "_data:s_stat set stat = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", stat_t = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", soato = " + Utils.EStrNull(finder.dop_kod, "") +
                        " where nzp_stat = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else if (finder.name_sprav == "s_town")
                {
                    sql = "update " + finder.pref + "_data:s_town set town = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", town_t = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", soato = " + Utils.EStrNull(finder.dop_kod, "") +
                        " where nzp_town = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else if (finder.name_sprav == "s_rajon")
                {
                    sql = "update " + finder.pref + "_data:s_rajon set rajon = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", rajon_t = " + Utils.EStrNull(finder.val_sprav, "") +
                        ", soato = " + Utils.EStrNull(finder.dop_kod, "") +
                        " where nzp_raj = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
                else
                {
                    sql = "update " + finder.pref + "_data:" + finder.name_sprav + " set " + valfield + " = " + Utils.EStrNull(finder.val_sprav, "");
                    if (finder.name_sprav == "s_rajon_dom") sql += ", alt_rajon_dom = " + Utils.EStrNull(finder.dop_kod, "");
                    sql += " where " + keyfield + " = " + Utils.EStrNull(finder.nzp_sprav, "0");
                }
            }
            else
            {
                if (finder.name_sprav == "s_rajon_dom")
                    sql = "insert into " + finder.pref + "_data:s_rajon_dom (nzp_raj_dom, rajon_dom, alt_rajon_dom)" +
                        " values (0, " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                else if (finder.name_sprav == "s_land")
                    sql = "insert into " + finder.pref + "_data:s_land (nzp_land, land, land_t, soato)" +
                        " values (0, " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                else if (finder.name_sprav == "s_stat")
                    sql = "insert into " + finder.pref + "_data:s_stat (nzp_stat, nzp_land, stat, stat_t, soato)" +
                        " values (0, " + finder.parent_nzp_sprav + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                else if (finder.name_sprav == "s_town")
                    sql = "insert into " + finder.pref + "_data:s_town (nzp_town, nzp_stat, town, town_t, soato)" +
                        " values (0, " + finder.parent_nzp_sprav + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                else if (finder.name_sprav == "s_rajon")
                    sql = "insert into " + finder.pref + "_data:s_rajon (nzp_raj, nzp_town, rajon, rajon_t, soato)" +
                        " values (0, " + finder.parent_nzp_sprav + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.val_sprav, "") + ", " + Utils.EStrNull(finder.dop_kod, "") + ")";
                else
                    sql = "insert into " + finder.pref + "_data:" + finder.name_sprav + " (" + keyfield + ", " + valfield + ") values (0, " + Utils.EStrNull(finder.val_sprav, "") + ")";
            }
#endif

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result && finder.nzp_sprav == "")
            {
                ret.tag = GetSerialValue(conn_db);
            }
            conn_db.Close();
            return ret;
        }

        public Returns DeleteSprav(Sprav finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.pref == "") return new Returns(false, "Не задан префикс базы данных");
            if (finder.nzp_sprav == "") return new Returns(false, "Не задано значение справочника");

            string keyfield = "";
            string pref = finder.pref;
            switch (finder.name_sprav)
            {
                case "s_cel": keyfield = "nzp_cel"; break;
                case "s_dok": keyfield = "nzp_dok"; break;
                case "s_namereg": keyfield = "kod_namereg"; break;
                case "s_rajon_dom": keyfield = "nzp_raj_dom"; break;
                case "s_rod": keyfield = "nzp_rod"; break;
                case "s_grgd": keyfield = "nzp_grgd"; break;
                case "s_vid_mes": keyfield = "kod_vid_mes"; break;
                case "s_dok_sv": keyfield = "nzp_dok_sv"; break;
                case "s_land": keyfield = "nzp_land"; break;
                case "s_stat": keyfield = "nzp_stat"; break;
                case "s_town": keyfield = "nzp_town"; break;
                case "s_rajon": keyfield = "nzp_raj"; break;
                case "s_remark_kart":
                    {
                        keyfield = "id_rem";
                        pref = Points.Pref; break;
                    }
                default: return new Returns(false, "Неверно задано наименование справочника");
            }
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;



            if (isHasReferences(conn_db, finder.pref, finder.name_sprav, finder.nzp_sprav, out ret))
            {
                if (ret.result)
                    ret = new Returns(false, "Существуют ссылки на значение справочника. Значение справочника не удалено!", -1);
            }
            else
            {
#if PG
                string sql = "delete from " + pref + "_data." + finder.name_sprav + " where " + keyfield + " = " + Utils.EStrNull(finder.nzp_sprav, "0");
#else
                string sql = "delete from " + pref + "_data:" + finder.name_sprav + " where " + keyfield + " = " + Utils.EStrNull(finder.nzp_sprav, "0");
#endif
                ret = ExecSQL(conn_db, sql, true);
            }
            conn_db.Close();
            return ret;
        }

        public List<PaspOrganRegUcheta> FindOrganRegUcheta(PaspOrganRegUcheta finder, out bool hasRequisites, out Returns ret)
        {
            hasRequisites = false;
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Префикс базы данных не задан");
                return null;
            }
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_data'", true);
#else
            ret = ExecSQL(conn_db, "database " + finder.pref + "_data", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            hasRequisites = isTableHasColumn(conn_db, "s_namereg", "ogrn");
            string sql = "select kod_namereg, namereg" + (hasRequisites ? ", ogrn, inn, kpp, adr_namereg, tel_namereg, dolgnost, fio_namereg" : "");

            if (isTableHasColumn(conn_db, "s_namereg", "kod_namereg_prn"))
                sql += ", kod_namereg_prn";
            else
                sql += ",'' as kod_namereg_prn";

#if PG
            sql += " From " + finder.pref + "_data.s_namereg" +
                (finder.kod_namereg != 0 ? " Where kod_namereg = " + finder.kod_namereg : "") +
                " Order by namereg";
#else
            sql += " From " + finder.pref + "_data:s_namereg" +
                (finder.kod_namereg != 0 ? " Where kod_namereg = " + finder.kod_namereg : "") +
                " Order by namereg";
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<PaspOrganRegUcheta> list = new List<PaspOrganRegUcheta>();

            try
            {
                while (reader.Read())
                {
                    PaspOrganRegUcheta zap = new PaspOrganRegUcheta();
                    if (reader["kod_namereg"] != DBNull.Value) zap.kod_namereg = Convert.ToInt32(reader["kod_namereg"]);
                    if (reader["namereg"] != DBNull.Value) zap.namereg = Convert.ToString(reader["namereg"]).Trim();
                    if (reader["kod_namereg_prn"] != DBNull.Value) zap.kod_namereg_prn = Convert.ToString(reader["kod_namereg_prn"]).Trim();

                    if (hasRequisites)
                    {
                        if (reader["ogrn"] != DBNull.Value) zap.ogrn = Convert.ToString(reader["ogrn"]).Trim();
                        if (reader["inn"] != DBNull.Value) zap.inn = Convert.ToString(reader["inn"]).Trim();
                        if (reader["kpp"] != DBNull.Value) zap.kpp = Convert.ToString(reader["kpp"]).Trim();
                        if (reader["adr_namereg"] != DBNull.Value) zap.adr_namereg = Convert.ToString(reader["adr_namereg"]).Trim();
                        if (reader["tel_namereg"] != DBNull.Value) zap.tel_namereg = Convert.ToString(reader["tel_namereg"]).Trim();
                        if (reader["dolgnost"] != DBNull.Value) zap.dolgnost = Convert.ToString(reader["dolgnost"]).Trim();
                        if (reader["fio_namereg"] != DBNull.Value) zap.fio_namereg = Convert.ToString(reader["fio_namereg"]).Trim();
                    }
                    list.Add(zap);
                }
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой
            }
            return list;
        }

        public Returns SaveOrganRegUcheta(PaspOrganRegUcheta finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.pref == "") return new Returns(false, "Не задан префикс базы данных");
            if (finder.namereg.Trim() == "") return new Returns(false, "Не задано наименование органа регистрационного учета", -1);
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

#if PG
            string sql = "select kod_namereg from " + finder.pref + "_data.s_namereg" +
                " where upper(namereg) = " + Utils.EStrNull(finder.namereg.ToUpper(), "") + " and kod_namereg <> " + finder.kod_namereg;
#else
            string sql = "select kod_namereg from " + finder.pref + "_data:s_namereg" +
                " where upper(namereg) = " + Utils.EStrNull(finder.namereg.ToUpper(), "") + " and kod_namereg <> " + finder.kod_namereg;
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            if (reader.Read())
            {
                reader.Close();
                conn_db.Close();
                return new Returns(false, "Орган регистрационного учета с таким наименованием уже существует", -1);
            }
            reader.Close();

#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_data'", true);
#else
            ret = ExecSQL(conn_db, "database " + finder.pref + "_data", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            string fields = "";
            if (isTableHasColumn(conn_db, "s_namereg", "ogrn")) fields = ", ogrn, inn, kpp, adr_namereg, tel_namereg, dolgnost, fio_namereg";
            if (finder.kod_namereg > 0)
            {
#if PG
                sql = "update " + finder.pref + "_data.s_namereg set namereg = " + Utils.EStrNull(finder.namereg.Trim(), "");
#else
                sql = "update " + finder.pref + "_data:s_namereg set namereg = " + Utils.EStrNull(finder.namereg.Trim(), "");
#endif
                if (fields != "")
                {
                    sql += ", ogrn = " + Utils.EStrNull(finder.ogrn.Trim(), "") +
                        ", inn = " + Utils.EStrNull(finder.inn.Trim(), "") +
                        ", kpp = " + Utils.EStrNull(finder.kpp.Trim(), "") +
                        ", adr_namereg = " + Utils.EStrNull(finder.adr_namereg.Trim(), "") +
                        ", tel_namereg = " + Utils.EStrNull(finder.tel_namereg.Trim(), "") +
                        ", dolgnost = " + Utils.EStrNull(finder.dolgnost.Trim(), "") +
                        ", fio_namereg = " + Utils.EStrNull(finder.fio_namereg.Trim(), "");
                }
                sql += " where kod_namereg = " + finder.kod_namereg;
            }
            else
            {
#if PG
                sql = "insert into " + finder.pref + "_data.s_namereg (kod_namereg, namereg" + fields + ")" +
                                   " values (default, " + Utils.EStrNull(finder.namereg.Trim(), "");
#else
                sql = "insert into " + finder.pref + "_data:s_namereg (kod_namereg, namereg" + fields + ")" +
                    " values (0, " + Utils.EStrNull(finder.namereg.Trim(), "");
#endif

                if (fields != "")
                {
                    sql += ", " + Utils.EStrNull(finder.ogrn.Trim(), "") +
                        ", " + Utils.EStrNull(finder.inn.Trim(), "") +
                        ", " + Utils.EStrNull(finder.kpp.Trim(), "") +
                        ", " + Utils.EStrNull(finder.adr_namereg.Trim(), "") +
                        ", " + Utils.EStrNull(finder.tel_namereg.Trim(), "") +
                        ", " + Utils.EStrNull(finder.dolgnost.Trim(), "") +
                        ", " + Utils.EStrNull(finder.fio_namereg.Trim(), "");
                }
                sql += ")";
            }

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result && finder.kod_namereg < 1)
            {
                ret.tag = GetSerialValue(conn_db);
            }
            conn_db.Close();
            return ret;
        }

        public List<Sobstw> GetSobstvForOtchet(Sobstw finder, out Returns ret)
        {
            //todo: PG
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user).Trim() + "_spls";
            if (!TableInWebCashe(conn_web, tXX_spls))
            {
                ret.result = true;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                conn_web.Close();
                return null;
            }
            string spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            string sqlText = " drop table t_sobstv ";
            ClassDBUtils.ExecSQL(sqlText, conn_db, ClassDBUtils.ExecMode.Log);

            sqlText = " CREATE TEMP TABLE t_sobstv (" +
                " ulica CHAR(40)," +
                " idom INTEGER," +
                " ndom CHAR(15)," +
                " nkor CHAR(15)," +
                " nzp_dom INTEGER," +
                " ikvar INTEGER," +
                " nkvar CHAR(10)," +
                " nkvar_n CHAR(3), " +
                " nzp_kvar INTEGER," +
                " fio CHAR(250), " +
                " priv CHAR(15)," +
                " s_ob CHAR(20)," +
                " s_dol CHAR(20), " +
                " dolya CHAR(20)) ";
            ClassDBUtils.ExecSQL(sqlText, conn_db);

            string sql = "select unique pref from " + tXX_spls;
            IDataReader reader, reader2;
            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }
            List<Sobstw> list = new List<Sobstw>();


            try
            {
                while (reader.Read())
                {
                    string pref = "";
                    if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();
                    if (pref == "")
                    {
                        reader.Close();
                        conn_web.Close();
                        conn_db.Close();
                        ret.result = false;
                        ret.text = "Нет префикса в списке лс";
                        return null;
                    }

                    sql = " insert into t_sobstv (ulica,idom,ndom,nkor,nzp_dom,ikvar,nkvar,nkvar_n,nzp_kvar,fio,priv,s_ob,s_dol,dolya) " +
                         " select u.ulica, d.idom ,d.ndom,d.nkor,d.nzp_dom, kv.ikvar,replace(kv.nkvar,',','.') as nkvar," +
                         " replace( kv.nkvar_n,',','.') as nkvar_n, kv.nzp_kvar , trim(s.fam)||' '||trim(s.ima)||' '||trim(s.otch) as fio," +
                         " case when (select max(p.val_prm) from " + pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1 p where p.is_actual<>100 and  today between p.dat_s and p.dat_po " +
                         " and p.nzp_prm=8 and p.nzp=kv.nzp_kvar)='1' then 'собств.' else 'найм' end as priv," +
                         " (select max(p.val_prm) from " + pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1 p where  p.is_actual<>100 and  today between p.dat_s and p.dat_po " +
                         " and p.nzp_prm=4 and p.nzp=kv.nzp_kvar) as s_ob," +
                         " (select  max(p.val_prm)*1 from " + pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1 p where p.nzp=kv.nzp_kvar and p.nzp_prm= 4 " +
                         " and  today between p.dat_s and p.dat_po and p.is_actual<>100) " + // площадь квартиры
                         " *      " + // умножим на долю
                         " CASE   WHEN  s.dolya_up  >0  and s.dolya_down>0 " + // --голосовал собственник с долями
                         " THEN  s.dolya_up/s.dolya_down  " +
                         " ELSE  1 /(select count(*) from " + pref + "_data@" + DBManager.getServer(conn_db) + ":sobstw s1 where s1.nzp_kvar=kv.nzp_kvar and s1.is_actual=1 ) " +// --голосовал собственник без долей
                         " END as s_dol, " +

                         " CASE   " +
                         " WHEN  s.dolya_up  >0  and s.dolya_down>0 " + // --голосовал собственник с долями
                         " THEN  s.dolya_up||'/'||s.dolya_down  " +
                         " ELSE  1||'/'||(select count(*) from " + pref + "_data@" + DBManager.getServer(conn_db) + ":sobstw s1 where s1.nzp_kvar=kv.nzp_kvar and s1.is_actual=1 ) " +//      --голосовал собственник без долей
                         " END as dolya " +

                         " from  " + spls + " spls, " + pref + "_data@" + DBManager.getServer(conn_db) + ":kvar kv, " +
                                   pref + "_data@" + DBManager.getServer(conn_db) + ":dom d, " +
                                   pref + "_data@" + DBManager.getServer(conn_db) + ":s_ulica u, " +
                                   pref + "_data@" + DBManager.getServer(conn_db) + ":sobstw s " +
                         " where kv.nzp_dom= d.nzp_dom" +
                         " and  d.nzp_ul=u.nzp_ul" +

                         " and kv.nzp_kvar=s.nzp_kvar" +
                         " and s.is_actual=1" +

                         " and exists (select 1" +
                            " from " + pref + "_data@" + DBManager.getServer(conn_db) + ":prm_3 p" +
                            " where  p.is_actual<>100 " +
                            " and  today between p.dat_s and p.dat_po " +
                            " and p.nzp_prm=51 " +
                            " and p.val_prm='1'" +
                            " and p.nzp=kv.nzp_kvar)" +
                            " and u.nzp_ul=spls.nzp_ul" +
                            " and kv.nzp_kvar= spls.nzp_kvar and spls.pref='" + pref + "'";
                    ClassDBUtils.ExecSQL(sql, conn_db);
                    sql = " insert into t_sobstv (ulica,idom,ndom,nkor,nzp_dom,ikvar,nkvar,nkvar_n,nzp_kvar,fio,priv,s_ob,s_dol,dolya) " +
                            " select  u.ulica, d.idom ,d.ndom,d.nkor, d.nzp_dom, kv.ikvar,replace( kv.nkvar,',','.'),replace(kv.nkvar_n,',','.')  , kv.nzp_kvar  ," +
                            " trim(initcap(kv.fio))," +
                            " case when (select max(p.val_prm) from " + pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1 p " +
                            " where  p.is_actual<>100 and  today between p.dat_s and p.dat_po  and p.nzp_prm=8 " +
                            " and p.nzp=kv.nzp_kvar)='1' then 'собств.' else 'найм' end as priv," +

                            " (select max(p.val_prm) from " + pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1 p " +
                            " where  p.is_actual<>100 and  today between p.dat_s and p.dat_po  and p.nzp_prm=4 " +
                            " and p.nzp=kv.nzp_kvar) as s_ob," +

                            " (select max(p.val_prm)*1  from " + pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1 p " +
                            " where  p.is_actual<>100 and  today between p.dat_s and p.dat_po and p.nzp_prm=4 " +
                            " and p.nzp=kv.nzp_kvar) as s_dol," +
                            " '' as dolya" +

                            " from  " + spls + " spls, " + pref + "_data@" + DBManager.getServer(conn_db) + ":kvar kv, " +
                                      pref + "_data@" + DBManager.getServer(conn_db) + ":dom d, " +
                                      pref + "_data@" + DBManager.getServer(conn_db) + ":s_ulica u " +
                            " where kv.nzp_dom= d.nzp_dom" +
                            " and  d.nzp_ul=u.nzp_ul" +

                            " and not exists (select 1 from " + pref + "_data@" + DBManager.getServer(conn_db) + ":sobstw s where kv.nzp_kvar=s.nzp_kvar  and s.is_actual=1)" +

                            " and exists (select 1" +
                                " from " + pref + "_data@" + DBManager.getServer(conn_db) + ":prm_3 p" +
                                " where  p.is_actual<>100 " +
                                " and  today between p.dat_s and p.dat_po " +
                                " and p.nzp_prm=51 " +
                                " and p.val_prm='1'" +
                                " and p.nzp=kv.nzp_kvar)" +
                                " and u.nzp_ul=spls.nzp_ul" +
                                " and kv.nzp_kvar= spls.nzp_kvar and spls.pref='" + pref + "'";

                    ClassDBUtils.ExecSQL(sql, conn_db);
                }
                reader.Close();

                IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From t_sobstv", conn_db);

                string st = Convert.ToString(cmd.ExecuteScalar());
                ret.tag = Convert.ToInt32(st);

                //string skip = ""; skip = " skip " + finder.skip.ToString();
                sql = "select ulica,idom,ndom,nkor,nzp_dom,ikvar,nkvar,nkvar_n,nzp_kvar,fio,priv,s_ob,s_dol,dolya from t_sobstv  order by 1,2,3,4,5,6,7,8,9,10";
                if (!ExecRead(conn_db, out reader2, sql, true).result)
                {
                    reader.Close();
                    conn_web.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }

                int i = 0;
                while (reader2.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    Sobstw s = new Sobstw();
                    if (reader2["ulica"] != DBNull.Value) s.ulica = Convert.ToString(reader2["ulica"]).Trim();
                    if (reader2["ndom"] != DBNull.Value) s.ndom = Convert.ToString(reader2["ndom"]).Trim();
                    if (reader2["nzp_dom"] != DBNull.Value) s.nzp_dom = Convert.ToInt32(reader2["nzp_dom"]);
                    if (reader2["nkor"] != DBNull.Value) s.nkor = Convert.ToString(reader2["nkor"]).Trim();
                    if (reader2["nkvar"] != DBNull.Value) s.nkvar = Convert.ToString(reader2["nkvar"]).Trim();
                    if (reader2["nkvar_n"] != DBNull.Value) s.nkvar_n = Convert.ToString(reader2["nkvar_n"]).Trim();
                    if (reader2["nzp_kvar"] != DBNull.Value) s.nzp_kvar = Convert.ToInt32(reader2["nzp_kvar"]);
                    if (reader2["fio"] != DBNull.Value) s.fio = Convert.ToString(reader2["fio"]).Trim();
                    s.dopParams = new List<Prm>();
                    Prm prm = new Prm();
                    prm.nzp_prm = 8;
                    if (reader2["priv"] != DBNull.Value) prm.val_prm = Convert.ToString(reader2["priv"]).Trim();
                    s.dopParams.Add(prm);
                    prm = new Prm();
                    prm.nzp_prm = 4;
                    if (reader2["s_ob"] != DBNull.Value) prm.val_prm = Convert.ToString(reader2["s_ob"]).Trim();
                    s.dopParams.Add(prm);
                    if (reader2["s_dol"] != DBNull.Value) s.dop_info = Convert.ToString(reader2["s_dol"]).Trim();
                    if (reader2["dolya"] != DBNull.Value) s.dolya = Convert.ToString(reader2["dolya"]).Trim();
                    list.Add(s);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                reader2.Close();
                reader.Close();
                conn_web.Close();
                conn_db.Close();
                return list;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка заполнения получения собственников для отчета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public Returns GetFioVlad(Ls finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ret = GetFioVlad(finder, conn_db);
            conn_db.Close();
            return ret;
        }

        public Returns GetFioVlad(Ls finder, IDbConnection conn_db)
        {
            //todo: PG
            Returns ret = Utils.InitReturns();

            IDataReader reader;

            string sql = "select fam,ima,otch, dat_rog  from " + finder.pref + "_data@" + DBManager.getServer(conn_db) + ":kart k, " + finder.pref + "_data@" + DBManager.getServer(conn_db) + ":s_rod r" +
                 " where k.nzp_rod=r.nzp_rod " +
                 " and (upper(rod) matches 'НАНИМ*'  OR upper(rod) matches 'СОБСТ*' OR upper(rod) matches 'ВЛАД*' OR upper(rod) matches 'КВАРТИРО*' )" +
                 " and nzp_kvar=" + finder.nzp_kvar +
                 " and isactual='1' and nzp_tkrt=1" +
                 " order by dat_rog";
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.result = false;
                return ret;
            }
            string fio = "";
            if (reader.Read())
            {
                if (reader["fam"] != DBNull.Value) fio += Convert.ToString(reader["fam"]).Trim();
                if (reader["ima"] != DBNull.Value) fio += " " + Convert.ToString(reader["ima"]).Trim();
                if (reader["otch"] != DBNull.Value) fio += " " + Convert.ToString(reader["otch"]).Trim();
            }
            if (fio.Trim() != "") return new Returns(true, fio);

            sql = "select fam,ima,otch, dat_rog  from " + finder.pref + "_data@" + DBManager.getServer(conn_db) + ":sobstw k" +
                  " where nzp_kvar=" + finder.nzp_kvar +
                  " order by dat_rog";
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.result = false;
                return ret;
            }

            fio = "";
            if (reader.Read())
            {
                if (reader["fam"] != DBNull.Value) fio += Convert.ToString(reader["fam"]).Trim();
                if (reader["ima"] != DBNull.Value) fio += " " + Convert.ToString(reader["ima"]).Trim();
                if (reader["otch"] != DBNull.Value) fio += " " + Convert.ToString(reader["otch"]).Trim();
            }
            if (fio.Trim() != "") return new Returns(true, fio);

            sql = "select initcap(fio) as fio from " + finder.pref + "_data@" + DBManager.getServer(conn_db) + ":kvar where nzp_kvar=" + finder.nzp_kvar;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.result = false;
                return ret;
            }

            fio = "";
            if (reader.Read())
            {
                if (reader["fio"] != DBNull.Value) fio = Convert.ToString(reader["fio"]).Trim();
            }
            if (fio.Trim() != "") return new Returns(true, fio);

            return ret;
        }

        public List<Kart> GetDataFromTXXTable(Kart finder, out Returns ret)
        {
            //todo: PG
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tab_gil;
            if (finder.nzp_gil.Trim() != "") tab_gil = "_gilhist";
            else if (finder.nzp_kvar == Constants._ZERO_) tab_gil = "_gil";
            else tab_gil = "_gilkvar";

            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;
            if (!TableInWebCashe(conn_web, tXX_cnt))
            {
                ret.result = true;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                conn_web.Close();
                return null;
            }
            string table = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cnt;
            conn_web.Close();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = " drop table t_gil ";
            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Log);

            sql = " select fam, ima, otch, dat_rog, dok, serij, nomer, grgd, adr, pref, nzp_gil, nzp_kart, nzp_kvar from " + table +
                  " Into temp t_gil With no log ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            sql = "select unique pref from t_gil";
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            sql = " drop table t_spisdata ";
            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Log);
            sql = " CREATE TEMP TABLE t_spisdata (" +
            "fam CHAR(40), ima CHAR(40), otch CHAR(40), dat_rog DATE, dok CHAR(30), serij NCHAR(10), nomer NCHAR(7), " +
            "adr CHAR(80), grgd CHAR(60), jobname NCHAR(40), jobpost NCHAR(40),  dat_ofor DATE, dat_oprp DATE, nzp_kvar INTEGER, pref CHAR(20)) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Kart> list = new List<Kart>();

            try
            {
                while (reader.Read())
                {
                    string pref = "";
                    if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();
                    if (pref == "")
                    {
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        ret.text = "Нет префикса в списке лс";
                        return null;
                    }

                    sql = " insert into t_spisdata (fam, ima, otch, dat_rog, dok, serij, nomer, adr, grgd, jobname, jobpost, dat_ofor, dat_oprp, nzp_kvar, pref)" +
                          " select k.fam, k.ima, k.otch, k.dat_rog, sd.dok, k.serij, k.nomer, t.adr, t.grgd, k.jobname, k.jobpost, k.dat_ofor, k.dat_oprp, t.nzp_kvar, t.pref " +
                          " from t_gil t, " + pref + "_data@" + DBManager.getServer(conn_db) + ":kart k, " +
                          " OUTER " + pref + "_data@" + DBManager.getServer(conn_db) + ":s_dok sd" +
                          " where t.nzp_kart = k.nzp_kart and k.nzp_dok = sd.nzp_dok and t.pref='" + pref + "'";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return null;
                    }

                }

                reader.Close();

                IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From t_spisdata", conn_db);
                string st = Convert.ToString(cmd.ExecuteScalar());
                Int32 tot = Convert.ToInt32(st);

                //  string skip = ""; skip = " skip " + finder.skip.ToString();
                sql = "select  fam, ima, otch, dat_rog, dok, serij, nomer, adr, grgd, jobname, jobpost, nzp_kvar, pref, dat_ofor, dat_oprp from t_spisdata  order by fam, ima, otch";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                Returns ret2;
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    Kart k = new Kart();
                    if (reader["nzp_kvar"] != DBNull.Value) k.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                    if (reader["pref"] != DBNull.Value) k.pref = Convert.ToString(reader["pref"]).Trim();
                    if (finder.mode == 1)
                    {
                        Ls ls = new Ls();
                        ls.nzp_user = finder.nzp_user;
                        ls.nzp_kvar = k.nzp_kvar;
                        ls.pref = k.pref;
                        ret2 = GetFioVlad(ls, conn_db);
                        if (ret2.result) k.fio = ret2.text;
                    }

                    if (reader["fam"] != DBNull.Value) k.fam = Convert.ToString(reader["fam"]).Trim();
                    if (reader["ima"] != DBNull.Value) k.ima = Convert.ToString(reader["ima"]).Trim();
                    if (reader["otch"] != DBNull.Value) k.otch = Convert.ToString(reader["otch"]);
                    if (reader["dat_rog"] != DBNull.Value) k.dat_rog = Convert.ToString(reader["dat_rog"]).Trim();
                    if (reader["dok"] != DBNull.Value) k.dok = Convert.ToString(reader["dok"]).Trim();
                    if (reader["serij"] != DBNull.Value) k.serij = Convert.ToString(reader["serij"]).Trim();
                    if (reader["nomer"] != DBNull.Value) k.nomer = Convert.ToString(reader["nomer"]);
                    if (reader["adr"] != DBNull.Value) k.adr = Convert.ToString(reader["adr"]).Trim();
                    if (reader["jobname"] != DBNull.Value) k.jobname = Convert.ToString(reader["jobname"]);
                    if (reader["grgd"] != DBNull.Value) k.grgd = Convert.ToString(reader["grgd"]);
                    if (reader["jobpost"] != DBNull.Value) k.jobpost = Convert.ToString(reader["jobpost"]).Trim();
                    if (reader["dat_ofor"] != DBNull.Value) k.dat_ofor = Convert.ToString(reader["dat_ofor"]).Trim();
                    if (reader["dat_oprp"] != DBNull.Value) k.dat_oprp = Convert.ToString(reader["dat_oprp"]).Trim();
                    list.Add(k);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();
                conn_web.Close();
                conn_db.Close();
                ret.tag = tot;
                return list;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка заполнения получения собственников для отчета " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }

        public List<Kart> GetDataFromKart(Kart finder, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = new Returns();
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;
                string pref = Points.Pref;
                var sql = " SELECT pref " +
                          " FROM  " + Points.Pref + DBManager.sDataAliasRest + "kvar " +
                          " WHERE num_ls = " + finder.nzp_kvar +
                          " AND nzp_wp > 1 ";
                IDataReader reader;
                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    ret.result = false;
                    return null;
                }
                reader.Read();
                pref = reader["pref"].ToString().ToLower().Trim();

                string table = pref + DBManager.sDataAliasRest + "kart ";
                if (!TempTableInWebCashe(conn_db, table))
                {
                    ret.result = false;
                    ret.text = "Данные не были выбраны! Выполните поиск.";
                    ret.tag = -22;
                    return null;
                }

                sql = " select nzp_kart, fam, ima, otch, dat_rog from " + table +
                             " where nzp_kvar = " + finder.nzp_kvar;
                var dt = DBManager.ExecSQLToTable(conn_db, sql);
                var dv = new DataView(dt);

                var list = (from DataRowView row in dv
                            select new Kart
                            {
                                nzp_kart = row["nzp_kart"].ToString(),
                                fam = row["fam"].ToString(),
                                ima = row["ima"].ToString(),
                                otch = row["otch"].ToString(),
                                dat_rog = row["dat_rog"].ToString()
                            }).ToList();
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";

                MonitorLog.WriteLog("Ошибка получения жителей из карты для лицевого счета " + err,
                    MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
            finally
            {
                conn_db.Close();
            }
        }


        public List<Otvetstv> GetOtvetstv(Ls finder, out Returns ret)
        {
            if (finder.nzp_kvar == 0)
            {
                ret = new Returns(false, "Не задан лицевой счет", -1);
                return null;
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = new Returns();
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка подключения к базе данных", -1);
                    return null;
                }

                var sql = " SELECT o.nzp_otv, o.fam, o.ima, o.otch, o.dat_rog, o.vipis_dat, r.rod  " +
                          " FROM  " + finder.pref + DBManager.sDataAliasRest + "otvetstv o " +
                          " LEFT OUTER JOIN " + finder.pref + DBManager.sDataAliasRest + "s_rod r on r.nzp_rod = o.nzp_rod" +
                          " WHERE o.nzp_kvar = " + finder.nzp_kvar + " AND o.is_actual <> 100 " +
                          " ORDER BY o.fam";
                var dt = DBManager.ExecSQLToTable(conn_db, sql);
                var dv = new DataView(dt);
                int i = 1;

                var list = (from DataRowView row in dv
                            select new Otvetstv
                            {
                                num = i++,
                                nzp_otv = row["nzp_otv"].ToInt(),
                                fam = row["fam"].ToString(),
                                ima = row["ima"].ToString(),
                                otch = row["otch"].ToString(),
                                dat_rog = row["dat_rog"].ToString().Substring(0, 10),
                                vipis_dat = row["vipis_dat"].ToString().Substring(0, 10),
                                rod = row["rod"].ToString(),
                                pref = finder.pref
                            }).ToList();
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка ответственных физических лиц ";
                MonitorLog.WriteLog("Ошибка получения списка ответственных физических лиц " + ex.Message + " " + ex.StackTrace,
                    MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                conn_db.Close();
            }
        }

        public Returns SaveOtvetstv(Otvetstv finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = Utils.InitReturns();
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка подключения к базе данных", -1);
                    return ret;
                }

                string sql;
#if PG
                sql = "insert into " + finder.pref + DBManager.sDataAliasRest + "gilec (nzp_gil) values(default)";
#else
                sql = "insert into " + finder.pref + DBManager.sDataAliasRest + "gilec (nzp_gil) values(0)";
#endif

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка записи карточки жильца";
                    return new Returns(false, "Ошибка записи карточки жильца", -1); ;
                }
                string nzp_gil = Convert.ToString(GetSerialValue(conn_db));


                sql =
                    " INSERT INTO " + finder.pref + DBManager.sDataAliasRest + "otvetstv" +
                    " (nzp_kvar, " +
                    (finder.nzp_rod > 0 ? "nzp_rod, " : "") +
                    "fam, ima, otch, dat_rog, " +
                    (finder.adress == "" ? "" : "adress, ") +
                    (finder.dop_info == "" ? "" : "dop_info,") +
                    (finder.nzp_dok > 0 ? "nzp_dok, " : "") +
                    (finder.serij == "" ? "" : "serij, ") +
                    (finder.nomer == "" ? "" : "nomer, ") +
                    (finder.vid_mes == "" ? "" : "vid_mes, ") +
                    (finder.vid_dat == "" ? "" : "vid_dat, ") +
                    "vipis_dat, nzp_gil, dat_s, dat_po, is_actual, nzp_user, dat_when) " +
                    " VALUES (" + finder.nzp_kvar + ", " +
                    (finder.nzp_rod > 0 ? finder.nzp_rod + ", " : "") +
                    "'" + finder.fam + "', '" + finder.ima + "', '" + finder.otch + "', '" + finder.dat_rog + "', " +
                    (finder.adress == "" ? "" : "'" + finder.adress + "',") +
                    (finder.dop_info == "" ? "" : " '" + finder.dop_info + "',") +
                    (finder.nzp_dok > 0 ? " '" + finder.nzp_dok + "', " : "") +
                    (finder.serij == "" ? "" : "'" + finder.serij + "', ") +
                    (finder.nomer == "" ? "" : "'" + finder.nomer + "', ") +
                    (finder.vid_mes == "" ? "" : "'" + finder.vid_mes + "',") +
                    (finder.vid_dat == "" ? "" : "'" + finder.vid_dat + "',") +
                    " '" + finder.vipis_dat + "', " + nzp_gil + ", '" +
                    (finder.dat_s == "" ? Points.DateOper.ToShortDateString() : finder.dat_s) + "', '" +
                    (finder.dat_po == "" ? "01.01.3000" : finder.dat_po) + "', 1, " + finder.nzp_user + ", " + DBManager.sCurDate + ")";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка сохранения нового физического лица";
                    return new Returns(false, "Ошибка сохранения нового физического лица", -1);
                }

                ret.tag = GetSerialValue(conn_db);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка сохранения нового физического лица ";
                MonitorLog.WriteLog("Ошибка сохранения нового физического лица " + ex.Message + " " + ex.StackTrace,
                    MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            return ret;
        }

        public List<RelationsFinder> LoadRelations(RelationsFinder finder, out Returns ret)
        {
            var sql = new StringBuilder();
            sql.AppendFormat("select * from {0}_data{1}s_rod where 1=1", finder.pref, tableDelimiter);
            if (finder.nzp_rod > 0) sql.AppendFormat(" and nzp_rod = {0}", finder.nzp_rod);
            if (finder.rodstvo.Trim() != "") sql.AppendFormat(" and lower(rod) like lower('%{0}%')", finder.rodstvo);

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = Utils.InitReturns();
            MyDataReader reader = null;
            var list = new List<RelationsFinder>();
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка подключения к базе данных", -1);
                    return null;
                }


                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка получения данных", -1);
                    return null;
                }

                while (reader.Read())
                {
                    var rf = new RelationsFinder();
                    if (reader["nzp_rod"] != DBNull.Value) rf.nzp_rod = Convert.ToInt32(reader["nzp_rod"]);
                    if (reader["rod"] != DBNull.Value) rf.rodstvo = Convert.ToString(reader["rod"]);
                    list.Add(rf);
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения данных ";
                MonitorLog.WriteLog("Ошибка  получения данных " + ex.Message + " " + ex.StackTrace,
                    MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close();
            }
            return list;
        }

        public List<Kart> GetKvarKart(Kart finder, out Returns ret)
        {
            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = Utils.InitReturns();
            MyDataReader reader = null;
            var list = new List<Kart>();

            var sql = new StringBuilder();
            sql.AppendFormat(" SELECT c.nzp_gil, c.nzp_kart, c.fam, c.ima, c.otch, c.dat_rog FROM {0}_data{1}kart c ", finder.pref, tableDelimiter);
            sql.AppendFormat(
                " WHERE c.nzp_kvar = {0} and c.nzp_tkrt=1 and coalesce(c.dat_oprp, date('01.01.3000')) >= current_date and c.isactual='1' and coalesce(c.neuch,'0')<>'1'",
                finder.nzp_kvar);

            try
            {
                ret = OpenDb(connDb, true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка подключения к базе данных", -1);
                    return null;
                }


                ret = ExecRead(connDb, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    ret = new Returns(false, "Ошибка получения данных", -1);
                    return null;
                }

                while (reader.Read())
                {
                    var rf = new Kart();
                    if (reader["nzp_gil"] != DBNull.Value) rf.nzp_gil = Convert.ToInt32(reader["nzp_gil"]).ToString();
                    if (reader["nzp_kart"] != DBNull.Value) rf.nzp_kart = Convert.ToInt32(reader["nzp_kart"]).ToString();
                    if (reader["fam"] != DBNull.Value) rf.fam = Convert.ToString(reader["fam"]);
                    if (reader["ima"] != DBNull.Value) rf.ima = Convert.ToString(reader["ima"]);
                    if (reader["otch"] != DBNull.Value) rf.otch = Convert.ToString(reader["otch"]);
                    if (reader["dat_rog"] != DBNull.Value) rf.dat_rog = Convert.ToDateTime(reader["dat_rog"]).ToShortDateString();
                    rf.nzp_kvar = finder.nzp_kvar;
                    list.Add(rf);
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения данных ";
                MonitorLog.WriteLog("Ошибка  получения данных " + ex.Message + " " + ex.StackTrace,
                    MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                connDb.Close();
            }
            return list;
        }
    }
}