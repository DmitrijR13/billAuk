using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using Bars.KP50.Faktura.Source.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System.IO;
using System.Data.Common;
using SevenZip;
using System.Globalization;
using System.Threading;
using System.Text.RegularExpressions;
using Bars.KP50.DB.Faktura;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;




namespace STCLINE.KP50.DataBase
{
    public partial class DbCharge : DbChargeClient
    {
        private string first_month, last_month;
        private Charge ishZap = null;
        public List<ServReval> listReval; //Список оснований перерасчета
        public BaseServ summaryServ; //Итого в счете, в т.ч. к оплате
        public List<BaseServ> listServ; //Список услуг счета
        public CUnionServ cUnionServ; //Правила объединения услуг
        public List<BaseServ> ListKommServ; //Коммунальные услуги

        /// <summary> Сформировать условие отбора данных
        /// </summary>
        //----------------------------------------------------------------------
        private void WhereStringForGet(ChargeFind finder, ref string whereString, out bool isOneMonth)
        //----------------------------------------------------------------------
        {
            StringBuilder swhere = new StringBuilder();

            if (finder.nzp_kvar > 0) swhere.Append(" and a.nzp_kvar = " + finder.nzp_kvar.ToString());

            // определить месяцы начислений
            first_month = MDY(1, 1, finder.YM.year_);
            last_month = MDY(12, 31, finder.YM.year_);
            if (finder.month_po > 0)
            {
                if (finder.YM.month_ > 0) first_month = MDY(finder.YM.month_, 1, finder.YM.year_);
                DateTime lastMonthDate = new DateTime(finder.YM.year_, finder.month_po, 1).AddMonths(1).AddDays(-1);
                last_month = MDY(lastMonthDate.Month, lastMonthDate.Day, lastMonthDate.Year);
            }
            else if (finder.YM.month_ > 0)
            {
                first_month = last_month = MDY(finder.YM.month_, 1, finder.YM.year_);
            }

            if (last_month != first_month)
            {
                swhere.Append(" and a.dat_month <= " + last_month);
                swhere.Append(" and a.dat_month >= " + first_month);
                isOneMonth = false;
            }
            else
            {
                swhere.Append(" and a.dat_month = " + first_month);
                isOneMonth = true;
            }

            // не загружать перерасчеты, если запрашиваются начисления за несколько месяцев и при этом нет группировки по месяцам
            // или стоит признак "не показывать перерасчеты"
            if ((first_month != last_month && !Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString())) ||
                (finder.is_show_reval != 1)
                )
                swhere.Append(" and a.dat_charge is null");

            whereString += swhere.ToString();
        }

        //----------------------------------------------------------------------
        private void AddZapToList(ref Charge zap, ref List<Charge> list, ref int position, ChargeFind finder)
        //----------------------------------------------------------------------
        {
            if (zap == null) return;
            if ((zap.dat_charge == "" && (zap.has_past_reval == 1 || zap.has_future_reval == 1)) ||
                (zap.tarif != 0 ||
                 zap.tarif_p != 0 ||
                 zap.reval != 0 ||
                 zap.rsum_tarif != 0 ||
                 zap.sum_real != 0 ||
                 zap.sum_tarif != 0 ||
                 zap.sum_tarif_p != 0 ||
                 zap.sum_nedop != 0 ||
                 zap.sum_nedop_p != 0 ||
                 zap.sum_lgota != 0 ||
                 zap.rashod != 0 ||
                 zap.norma_rashod != 0 ||
                 zap.sum_lgota_p != 0 ||
                 zap.sum_dlt_tarif != 0 ||
                 zap.sum_dlt_tarif_p != 0
                    ) ||
                (
                    (
                        zap.real_charge != 0 ||
                        zap.money_to != 0 ||
                        zap.money_from != 0 ||
                        zap.sum_insaldo != 0 ||
                        zap.sum_outsaldo != 0 ||
                        zap.sum_charge != 0
                        ) &&
                    Utils.GetParams(finder.groupby, Constants.act_show_saldo.ToString())
                    )
                )
            {
                if (zap.dat_charge == "")
                {
                    list.Insert(position, zap);
                    position = list.Count;
                }
                else
                {
                    if (Convert.ToDateTime(zap.dat_charge) > Convert.ToDateTime(zap.dat_month))
                    {
                        if (ishZap != null) ishZap.has_future_reval = 1;
                    }
                    if (Convert.ToDateTime(zap.dat_charge) < Convert.ToDateTime(zap.dat_month))
                    {
                        if (ishZap != null) ishZap.has_past_reval = 1;
                    }
                    list.Add(zap);
                }
            }

        }

        //----------------------------------------------------------------------
        public List<Charge> GetCharge(ChargeFind finder, out Returns ret) //вытащить Начисления из Кэш-БД
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            List<Charge> Spis = new List<Charge>();

            if (finder.YM.year_ <= 0) return null;

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXXCharge = sDefaultSchema + "t" + finder.nzp_user.ToString() + "_charge";

            IDataReader reader;

            //проверить наличие начислений по заданному году
            // если их нет, то подггрузить из основной БД
            StringBuilder s = new StringBuilder();

            //#if PG
            //            s.Append(" select * from information_schema.tables where table_schema = CURRENT_SCHEMA() and table_name = '" + tXXCharge + "'");
            //#else
            //            s.Append(" Select * From systables Where tabname = '" + tXXCharge + "'");
            //#endif
            //            if (!ExecRead(conn_web, out reader, s.ToString(), true).result)
            //            {
            //                conn_web.Close();
            //                return null;
            //            }
            //if (!reader.Read()) // таблица еще не создана
            if (!TempTableInWebCashe(conn_web, tXXCharge)) // таблица еще не создана
            {
                finder.find_from_the_start = 1;
                FindCharge(finder, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка заполнения списка начислений " +
                                        tXXCharge + " "
                                        + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_web.Close();
                    return null;
                }
            }
            else
            {
                s = new StringBuilder();
#if PG
                s.Append(" select * ");
#else
                s.Append(" select first 1 * ");
#endif
                s.Append(" from " + tXXCharge + " where dat_month between " + MDY(1, 1, finder.YM.year_) + " and " +
                         MDY(12, 31, finder.YM.year_));
                if (finder.nzp_kvar > 0) s.Append(" and nzp_kvar = " + finder.nzp_kvar.ToString());
#if PG
                s.Append(" limit 1");
#endif
                if (!ExecRead(conn_web, out reader, s.ToString(), true).result)
                {
                    reader.Close();
                    conn_web.Close();
                    return null;
                }
                if (!reader.Read())
                {
                    FindCharge(finder, out ret);
                    if (!ret.result)
                    {
                        reader.Close();
                        MonitorLog.WriteLog("Ошибка заполнения списка начислений " +
                                            tXXCharge + " "
                                            + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                        conn_web.Close();
                        return null;
                    }
                }
                reader.Close();
            }

            // выбрать данные из КЭШ таблицы
            s = new StringBuilder();
            s.Append(" Select ");

            string where = " Where 1=1 ";
            bool isOneMonth;
            WhereStringForGet(finder, ref where, out isOneMonth);

#if PG
            string template =
                " coalesce(a.dat_charge, public.mdy(1,1,3000)) as dat_charge, {dat_month}, {nzp_serv}, {nzp_supp}, {nzp_frm} ";
#else
            string template = " nvl(a.dat_charge, mdy(1,1,3000)) as dat_charge, {dat_month}, {nzp_serv}, {nzp_supp}, {nzp_frm} ";
#endif
            string groupby = " group by a.dat_charge";

            string orderby = "";
#if PG

#else
            orderby = "''";
#endif


            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()) || isOneMonth)
            {
                template = template.Replace("{dat_month}", "a.dat_month");
                groupby += ", a.dat_month";
                if (orderby != "") orderby += ", ";
                orderby += " a.dat_month";
            }
            else
                template = template.Replace("{dat_month}", "max((case when a.dat_charge is null then date(null) else " +
                                                           " case when a.dat_charge > a.dat_month then date(a.dat_charge - 1) else date(a.dat_charge + 1) end end)) as dat_month");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString()))
            {
                template = template.Replace("{nzp_serv}", "a.nzp_serv, a.service, a.ordering");
                groupby += ", a.nzp_serv, a.service, a.ordering";
                if (orderby != "") orderby += ", ";
                orderby += " a.ordering, a.service, a.nzp_serv";
            }
            else template = template.Replace("{nzp_serv}", "0 as nzp_serv, '' as service, 0 as ordering ");

            if ((Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()) || isOneMonth) &&
                (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString())))
            {
                template += ",a.norma, a.norma_rashod, a.priznak_rasch, a.rashod, a.rashod_odn ";
                groupby += ", a.norma, a.norma_rashod, a.priznak_rasch, a.rashod, a.rashod_odn ";
            }

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supplier.ToString()))
            {
                template = template.Replace("{nzp_supp}", "a.nzp_supp, a.name_supp");
                groupby += ", a.nzp_supp, a.name_supp";
                if (orderby != "") orderby += ", ";
                orderby += " a.name_supp, a.nzp_supp";
            }
            else template = template.Replace("{nzp_supp}", "0 as nzp_supp, '' as name_supp");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_formula.ToString()))
            {
                template = template.Replace("{nzp_frm}", "a.nzp_frm, a.name_frm, a.measure ");
                groupby += ", a.nzp_frm, a.name_frm, a.measure ";
                if (orderby != "") orderby += ", ";
                orderby += " a.name_frm, a.nzp_frm";
            }
            else template = template.Replace("{nzp_frm}", "0 as nzp_frm, '' as name_frm, '' as measure");

            if (orderby != "") orderby += ", ";
            orderby += " 1 desc";

            s.Append(template);
            s.Append(", max(isdel) as maxisdel ");
            s.Append(", min(isdel) as minisdel ");
            s.Append(", max(a.tarif) as tarif ");
            s.Append(", max(a.tarif_p) as tarif_p ");
            s.Append(", max(a.tarif_f) as tarif_f ");
            s.Append(", max(a.tarif_f_p) as tarif_f_p ");
            s.Append(", max(a.c_calc) as c_calc ");
            s.Append(", sum(a.reval) as reval ");
            s.Append(", sum(case when a.reval > 0 then a.reval else 0 end) as reval_pol ");
            s.Append(", sum(case when a.reval < 0 then a.reval else 0 end) as reval_otr ");
            s.Append(", sum(a.rsum_tarif) as rsum_tarif ");
            s.Append(", sum(a.sum_real) as sum_real ");
            s.Append(", sum(a.sum_tarif) as sum_tarif ");
            s.Append(", sum(a.sum_tarif_p) as sum_tarif_p ");
            s.Append(", sum(a.sum_tarif_f) as sum_tarif_f ");
            s.Append(", sum(a.sum_tarif_f_p) as sum_tarif_f_p ");
            s.Append(", sum(a.sum_subsidy) as sum_subsidy ");
            s.Append(", sum(a.sum_nedop) as sum_nedop ");
            s.Append(", sum(a.sum_nedop_p) as sum_nedop_p ");
            s.Append(", sum(a.sum_lgota) as sum_lgota ");
            s.Append(", sum(a.sum_lgota_p) as sum_lgota_p ");
            s.Append(", sum(a.sum_dlt_tarif) as sum_dlt_tarif ");
            s.Append(", sum(a.sum_dlt_tarif_p) as sum_dlt_tarif_p ");
            s.Append(", sum(case when a.real_charge > 0 then a.real_charge else 0 end) as real_charge_pol ");
            s.Append(", sum(case when a.real_charge < 0 then a.real_charge else 0 end) as real_charge_otr ");
            s.Append(", sum(a.real_charge) as real_charge");
            s.Append(", sum(a.sum_money) as sum_money ");
            s.Append(", sum(a.money_to) as money_to ");
            s.Append(", sum(a.money_from) as money_from ");
            s.Append(", sum(a.money_del) as money_del ");
            s.Append(", sum(a.sum_insaldo) as sum_insaldo ");
            s.Append(", sum(a.sum_outsaldo) as sum_outsaldo ");
            s.Append(", sum(a.sum_charge) as sum_charge ");
            s.Append(", sum(a.sum_subsidy_all) as sum_subsidy_all ");
            s.Append(", sum(case when a.dat_month = " + first_month + " then a.sum_insaldo else 0 end) as sum_insaldo_begin ");
            s.Append(", sum(case when a.dat_month = " + last_month + " then a.sum_outsaldo else 0 end) as sum_outsaldo_end ");
            s.Append(", sum(case when a.dat_month = " + last_month + " then a.sum_charge else 0 end) as sum_charge_end ");

            s.Append(" From " + tXXCharge + " a ");

            s.Append(where + groupby + " order by " + orderby);

            /*s.Append(" having max(a.tarif) <> 0 ");
            s.Append(" or max(a.tarif_p) <> 0 "); 
            s.Append(" or sum(a.reval) <> 0 ");
            s.Append(" or sum(a.rsum_tarif) <> 0 "); 
            s.Append(" or sum(a.sum_real) <> 0 ");
            s.Append(" or sum(a.sum_tarif) <> 0 "); 
            s.Append(" or sum(a.sum_tarif_p) <> 0 ");
            s.Append(" or sum(a.sum_nedop) <> 0 "); 
            s.Append(" or sum(a.sum_nedop_p) <> 0 ");
            s.Append(" or sum(a.sum_lgota) <> 0 "); 
            s.Append(" or sum(a.sum_lgota_p) <> 0 ");
            s.Append(" or sum(a.sum_dlt_tarif) <> 0 "); 
            s.Append(" or sum(a.sum_dlt_tarif_p) <> 0 ");
            s.Append(" or sum(a.real_charge) <> 0 "); 
            s.Append(" or sum(a.sum_money) <> 0 ");
            s.Append(" or sum(a.sum_insaldo) <> 0 "); 
            s.Append(" or sum(a.sum_outsaldo) <> 0 ");
            s.Append(" or sum(a.sum_charge) <> 0 ");
            s.Append(" or sum(case when a.dat_month = '" + first_month + "' then a.sum_insaldo else 0 end) <> 0 ");
            s.Append(" or sum(case when a.dat_month = '" + last_month + "' then a.sum_outsaldo else 0 end) <> 0 ");
            s.Append(" or sum(case when a.dat_month = '" + last_month + "' then a.sum_charge else 0 end) <> 0 ");*/

            if (!ExecRead(conn_web, out reader, s.ToString(), true).result)
            {
                conn_web.Close();
                MonitorLog.WriteLog("Ошибка заполнения списка начислений " +
                                    tXXCharge + " "
                                    + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            try
            {
                int id = 0; // уникальный код записи
                int parent_id = 0; // уникальный код родительской записи
                int position = 0; // позиция, в которую надо вставить исходную строку
                ishZap = null;
                while (reader.Read())
                {
                    Charge zap = new Charge();

                    zap.id = ++id;
                    zap.num = zap.id.ToString();

                    if (Convert.ToDateTime(reader["dat_charge"]) == DateTime.Parse("01.01.3000"))
                    {
                        //if (ishZap != null) Spis.Insert(ishZap.id - 1, ishZap);
                        AddZapToList(ref ishZap, ref Spis, ref position, finder);

                        zap.dat_charge = "";
                        zap.parent_id = 0;
                        parent_id = id;
                        // Запись с исходным расчетом идет первой, все связанные перерасчеты потом по убыванию даты перерасчета
                        // поэтому как только встречается запись с пустым dat_charge, используем id этой записи как parent_id последующих записей 
                        ishZap = zap;
                        // Запомним запись с исходным расчетом, чтобы затем задать ему признаки наличия прошлых и будущих перерасчетов
                        if (reader["dat_month"] != DBNull.Value)
                        {
                            zap.dat_month = Convert.ToString(reader["dat_month"]);
                            zap.YM.month_ = Convert.ToDateTime(reader["dat_month"]).Month;
                            zap.YM.year_ = Convert.ToDateTime(reader["dat_month"]).Year;
                        }
                    }
                    else
                    {
                        zap.dat_charge = Convert.ToString(reader["dat_charge"]);
                        zap.parent_id = parent_id;
                        zap.dat_month = Convert.ToString(reader["dat_month"]);
                        zap.YM.month_ = Convert.ToDateTime(reader["dat_charge"]).Month;
                        zap.YM.year_ = Convert.ToDateTime(reader["dat_charge"]).Year;
                    }

                    if ((first_month == last_month || Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString())) &&
                        Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString()))
                    {
                        if (reader["tarif"] != DBNull.Value) zap.tarif = Convert.ToDecimal(reader["tarif"]);
                        if (reader["tarif_p"] != DBNull.Value) zap.tarif_p = Convert.ToDecimal(reader["tarif_p"]);
                        if (reader["tarif_f"] != DBNull.Value) zap.tarif_f = Convert.ToDecimal(reader["tarif_f"]);
                        if (reader["tarif_f_p"] != DBNull.Value) zap.tarif_f_p = Convert.ToDecimal(reader["tarif_f_p"]);
                    }

                    //if (reader["num_ls"] != DBNull.Value) zap.num_ls = Convert.ToString((int)reader["num_ls"]);
                    //if (reader["nzp_kvar"] != DBNull.Value) zap.nzp_kvar = (int)reader["nzp_kvar"];
                    //if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = (int)reader["nzp_dom"];
                    if ((Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()) || isOneMonth) &&
                        (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString())))
                    {
                        if (reader["norma"] != DBNull.Value) zap.norma = Convert.ToDecimal(reader["norma"]);
                        if (reader["norma_rashod"] != DBNull.Value)
                            zap.norma_rashod = Convert.ToDecimal(reader["norma_rashod"]);
                        if (reader["priznak_rasch"] != DBNull.Value)
                            zap.priznak_rasch = Convert.ToInt32(reader["priznak_rasch"]);
                        if (reader["rashod"] != DBNull.Value) zap.rashod = Convert.ToDecimal(reader["rashod"]);
                        if (reader["rashod_odn"] != DBNull.Value)
                            zap.rashod_odn = Convert.ToDecimal(reader["rashod_odn"]);
                    }
                    int minisdel = 0, maxisdel = 0;
                    if (reader["minisdel"] != DBNull.Value) minisdel = Convert.ToInt32(reader["minisdel"]);
                    if (reader["maxisdel"] != DBNull.Value) maxisdel = Convert.ToInt32(reader["maxisdel"]);
                    if (minisdel == maxisdel && maxisdel == 1) zap.isdel = 1;
                    else zap.isdel = 0;
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]);
                    if (reader["ordering"] != DBNull.Value) zap.ordering = Convert.ToInt32(reader["ordering"]);
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.supplier = Convert.ToString(reader["name_supp"]);
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                    if (reader["name_frm"] != DBNull.Value) zap.name_frm = Convert.ToString(reader["name_frm"]);
                    if (reader["measure"] != DBNull.Value) zap.measure = Convert.ToString(reader["measure"]);
                    if (reader["sum_real"] != DBNull.Value) zap.sum_real = Convert.ToDecimal(reader["sum_real"]);
                    if (reader["reval"] != DBNull.Value) zap.reval = Convert.ToDecimal(reader["reval"]);
                    if (reader["reval_pol"] != DBNull.Value) zap.reval_pol = Convert.ToDecimal(reader["reval_pol"]);
                    if (reader["reval_otr"] != DBNull.Value) zap.reval_otr = Convert.ToDecimal(reader["reval_otr"]);
                    //if (reader["tarif"] != DBNull.Value)            zap.tarif = Convert.ToDecimal(reader["tarif"]);
                    //if (reader["tarif_p"] != DBNull.Value)          zap.tarif_p = Convert.ToDecimal(reader["tarif_p"]);
                    if (reader["rsum_tarif"] != DBNull.Value) zap.rsum_tarif = Convert.ToDecimal(reader["rsum_tarif"]);
                    if (reader["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader["sum_tarif"]);
                    if (reader["sum_tarif_p"] != DBNull.Value)
                        zap.sum_tarif_p = Convert.ToDecimal(reader["sum_tarif_p"]);
                    if (reader["sum_tarif_f"] != DBNull.Value)
                        zap.sum_tarif_f = Convert.ToDecimal(reader["sum_tarif_f"]);
                    if (reader["sum_tarif_f_p"] != DBNull.Value)
                        zap.sum_tarif_f_p = Convert.ToDecimal(reader["sum_tarif_f_p"]);
                    if (reader["sum_nedop"] != DBNull.Value) zap.sum_nedop = Convert.ToDecimal(reader["sum_nedop"]);
                    if (reader["sum_nedop_p"] != DBNull.Value) zap.sum_nedop_p = Convert.ToDecimal(reader["sum_nedop_p"]);
                    zap.rsum_tarif_p = zap.sum_tarif_p + zap.sum_nedop_p;
                    if (reader["sum_lgota"] != DBNull.Value) zap.sum_lgota = Convert.ToDecimal(reader["sum_lgota"]);
                    if (reader["sum_lgota_p"] != DBNull.Value) zap.sum_lgota_p = Convert.ToDecimal(reader["sum_lgota_p"]);
                    if (reader["sum_dlt_tarif"] != DBNull.Value) zap.sum_dlt_tarif = Convert.ToDecimal(reader["sum_dlt_tarif"]);
                    if (reader["sum_dlt_tarif_p"] != DBNull.Value) zap.sum_dlt_tarif_p = Convert.ToDecimal(reader["sum_dlt_tarif_p"]);

                    if (reader["real_charge"] != DBNull.Value) zap.real_charge = Convert.ToDecimal(reader["real_charge"]);
                    if (reader["real_charge_pol"] != DBNull.Value) zap.real_charge_pol = Convert.ToDecimal(reader["real_charge_pol"]);
                    if (reader["real_charge_otr"] != DBNull.Value) zap.real_charge_otr = Convert.ToDecimal(reader["real_charge_otr"]);
                    if (reader["money_to"] != DBNull.Value) zap.money_to = Convert.ToDecimal(reader["money_to"]);
                    if (reader["money_from"] != DBNull.Value) zap.money_from = Convert.ToDecimal(reader["money_from"]);
                    if (reader["money_del"] != DBNull.Value) zap.money_del = Convert.ToDecimal(reader["money_del"]);
                    if (reader["sum_money"] != DBNull.Value) zap.sum_money = Convert.ToDecimal(reader["sum_money"]);
                    if (reader["sum_subsidy_all"] != DBNull.Value) zap.sum_subsidy_all = Convert.ToDecimal(reader["sum_subsidy_all"]);

                    if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()) || first_month == last_month)
                    {
                        if (reader["sum_insaldo"] != DBNull.Value) zap.sum_insaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                        if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);
                        if (reader["sum_charge"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                    }
                    else
                    {
                        if (reader["sum_insaldo_begin"] != DBNull.Value) zap.sum_insaldo = Convert.ToDecimal(reader["sum_insaldo_begin"]);
                        if (reader["sum_outsaldo_end"] != DBNull.Value) zap.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo_end"]);
                        if (reader["sum_charge_end"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_charge_end"]);
                    }

                    if (zap.tarif > 0)
                    {
                        if (reader["c_calc"] != DBNull.Value) zap.c_calc = Convert.ToDecimal(reader["c_calc"]);
                        //zap.c_calc = Decimal.Round(zap.sum_tarif / zap.tarif, 4);
                        zap.c_calc_full = Decimal.Round(zap.rsum_tarif / zap.tarif, 4);
                    }
                    if (zap.tarif_p > 0)
                    {
                        zap.c_calc_p = Decimal.Round(zap.sum_tarif_p / zap.tarif_p, 4);
                        zap.c_calc_full_p = Decimal.Round(zap.rsum_tarif_p / zap.tarif_p, 4);
                    }

                    if (zap.c_calc != 0) zap.c_calc_full = zap.c_calc;

                    //if (zap.dat_charge != "") Spis.Add(zap);
                    if (zap.dat_charge != "") AddZapToList(ref zap, ref Spis, ref position, finder);
                }
                //if (ishZap != null) Spis.Insert(ishZap.id - 1, ishZap);
                AddZapToList(ref ishZap, ref Spis, ref position, finder);

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

                MonitorLog.WriteLog("Ошибка заполнения списка начислений " + err, MonitorLog.typelog.Error, 20, 201,true);

                return null;
            }

        } //GetPu

        public List<Charge> GetBillCharge(ChargeFind finder, out Returns ret)
        {
            var dbFaktura = new OldRTFaktura();
            var result = dbFaktura.GetBillCharge(finder, out ret);
            return result;

        }

        public List<Charge> GetNewBillCharge(ChargeFind finder, out Returns ret)
        {
            var dbFaktura = new OldRTFaktura();
            var result = dbFaktura.GetNewBillCharge(finder, out ret);
            return result;

        }


        //----------------------------------------------------------------------
        private string Datas(string dat_nam, string dat_s, string dat_po)
        //----------------------------------------------------------------------
        {
            StringBuilder swhere = new StringBuilder();

            if (dat_po != "")
            {
                swhere.Append(" and " + dat_nam + " <= " + dat_po);
                if (dat_s != "") swhere.Append(" and " + dat_nam + " >= " + dat_s);
            }
            else if (dat_s != "") swhere.Append(" and " + dat_nam + " = " + dat_s);

            return swhere.ToString();
        }

        /// <summary> Сформировать условие отбора данных
        /// </summary>
        private void WhereStringForFindCommon(ChargeFind finder, string alias, ref string whereString)
        //----------------------------------------------------------------------
        {
            StringBuilder swhere = new StringBuilder();

            if (finder.nzp_kvar > 0)
            {
                swhere.Append(" and " + alias + ".nzp_kvar = " + finder.nzp_kvar.ToString());
            }

            //if (finder.get_koss)
            //    swhere.Append(" and "+alias+".nzp_serv in (25,210,11,242) ");
            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                swhere.Append(" and " + alias + ".nzp_serv in (" + role.val + ") ");
                            if (role.kod == Constants.role_sql_supp)
                                swhere.Append(" and " + alias + ".nzp_supp in (" + role.val + ") ");
                        }
                    }
                }
            }
            if (finder.is_device != "")
                swhere.Append(" and " + alias + ".is_device = " + finder.is_device);

            whereString += swhere.ToString();
        }

        /// <summary> Сформировать условие отбора данных
        /// </summary>
        private void WhereStringForFindServices(ChargeFind finder, string alias, ref string whereString)
        //----------------------------------------------------------------------
        {
            if (finder.nzp_serv <= 0) return;

            StringBuilder swhere = new StringBuilder();

            finder.sum_real = Utils.EFlo0(finder.sum_real.Trim(), "");
            finder.sum_real_po = Utils.EFlo0(finder.sum_real_po.Trim(), "");
            if ((finder.sum_real != "") && (finder.sum_real_po == "" || finder.sum_real == finder.sum_real_po))
                swhere.Append(" and " + alias + ".sum_real = " + finder.sum_real);
            else
            {
                if (finder.sum_real != "") swhere.Append(" and " + alias + ".sum_real >= " + finder.sum_real);
                if (finder.sum_real_po != "") swhere.Append(" and " + alias + ".sum_real <= " + finder.sum_real_po);
            }

            finder.sum_charge = Utils.EFlo0(finder.sum_charge.Trim(), "");
            finder.sum_charge_po = Utils.EFlo0(finder.sum_charge_po.Trim(), "");
            if ((finder.sum_charge != "") && (finder.sum_charge_po == "" || finder.sum_charge == finder.sum_charge_po))
                swhere.Append(" and " + alias + ".sum_charge = " + finder.sum_charge);
            else
            {
                if (finder.sum_charge != "") swhere.Append(" and " + alias + ".sum_charge >= " + finder.sum_charge);
                if (finder.sum_charge_po != "")
                    swhere.Append(" and " + alias + ".sum_charge <= " + finder.sum_charge_po);
            }

            finder.reval = Utils.EFlo0(finder.reval.Trim(), "");
            finder.reval_po = Utils.EFlo0(finder.reval_po.Trim(), "");
            if ((finder.reval != "") && (finder.reval_po == "" || finder.reval == finder.reval_po))
                swhere.Append(" and " + alias + ".reval = " + finder.reval);
            else
            {
                if (finder.reval != "") swhere.Append(" and " + alias + ".reval >= " + finder.reval);
                if (finder.reval_po != "") swhere.Append(" and " + alias + ".reval <= " + finder.reval_po);
            }

            finder.real_charge = Utils.EFlo0(finder.real_charge.Trim(), "");
            finder.real_charge_po = Utils.EFlo0(finder.real_charge_po.Trim(), "");
            if ((finder.real_charge != "") &&
                (finder.real_charge_po == "" || finder.real_charge == finder.real_charge_po))
                swhere.Append(" and " + alias + ".real_charge = " + finder.real_charge);
            else
            {
                if (finder.real_charge != "") swhere.Append(" and " + alias + ".real_charge >= " + finder.real_charge);
                if (finder.real_charge_po != "")
                    swhere.Append(" and " + alias + ".real_charge <= " + finder.real_charge_po);
            }

            finder.sum_money = Utils.EFlo0(finder.sum_money.Trim(), "");
            finder.sum_money_po = Utils.EFlo0(finder.sum_money_po.Trim(), "");
            if ((finder.sum_money != "") && (finder.sum_money_po == "" || finder.sum_money == finder.sum_money_po))
                swhere.Append(" and " + alias + ".sum_money = " + finder.sum_money);
            else
            {
                if (finder.sum_money != "") swhere.Append(" and " + alias + ".sum_money >= " + finder.sum_money);
                if (finder.sum_money_po != "") swhere.Append(" and " + alias + ".sum_money <= " + finder.sum_money_po);
            }

            finder.sum_insaldo = Utils.EFlo0(finder.sum_insaldo.Trim(), "");
            finder.sum_insaldo_po = Utils.EFlo0(finder.sum_insaldo_po.Trim(), "");
            if ((finder.sum_insaldo != "") &&
                (finder.sum_insaldo_po == "" || finder.sum_insaldo == finder.sum_insaldo_po))
                swhere.Append(" and " + alias + ".sum_insaldo = " + finder.sum_insaldo);
            else
            {
                if (finder.sum_insaldo != "") swhere.Append(" and " + alias + ".sum_insaldo >= " + finder.sum_insaldo);
                if (finder.sum_insaldo_po != "")
                    swhere.Append(" and " + alias + ".sum_insaldo <= " + finder.sum_insaldo_po);
            }

            finder.sum_outsaldo = Utils.EFlo0(finder.sum_outsaldo.Trim(), "");
            finder.sum_outsaldo_po = Utils.EFlo0(finder.sum_outsaldo_po.Trim(), "");
            if ((finder.sum_outsaldo != "") &&
                (finder.sum_outsaldo_po == "" || finder.sum_outsaldo == finder.sum_outsaldo_po))
                swhere.Append(" and " + alias + ".sum_outsaldo = " + finder.sum_outsaldo);
            else
            {
                if (finder.sum_outsaldo != "")
                    swhere.Append(" and " + alias + ".sum_outsaldo >= " + finder.sum_outsaldo);
                if (finder.sum_outsaldo_po != "")
                    swhere.Append(" and " + alias + ".sum_outsaldo <= " + finder.sum_outsaldo_po);
            }

            finder.tarif = Utils.EFlo0(finder.tarif.Trim(), "");
            finder.tarif_po = Utils.EFlo0(finder.tarif_po.Trim(), "");
            if ((finder.tarif != "") && (finder.tarif_po == "" || finder.tarif == finder.tarif_po))
                swhere.Append(" and " + alias + ".tarif = " + finder.tarif);
            else
            {
                if (finder.tarif != "") swhere.Append(" and " + alias + ".tarif >= " + finder.tarif);
                if (finder.tarif_po != "") swhere.Append(" and " + alias + ".tarif <= " + finder.tarif_po);
            }

            finder.rsum_tarif = Utils.EFlo0(finder.rsum_tarif.Trim(), "");
            finder.rsum_tarif_po = Utils.EFlo0(finder.rsum_tarif_po.Trim(), "");
            if ((finder.rsum_tarif != "") && (finder.rsum_tarif_po == "" || finder.rsum_tarif == finder.rsum_tarif_po))
                swhere.Append(" and " + alias + ".rsum_tarif = " + finder.rsum_tarif);
            else
            {
                if (finder.rsum_tarif != "") swhere.Append(" and " + alias + ".rsum_tarif >= " + finder.rsum_tarif);
                if (finder.rsum_tarif_po != "")
                    swhere.Append(" and " + alias + ".rsum_tarif <= " + finder.rsum_tarif_po);
            }

            finder.sum_tarif = Utils.EFlo0(finder.sum_tarif.Trim(), "");
            finder.sum_tarif_po = Utils.EFlo0(finder.sum_tarif_po.Trim(), "");
            if ((finder.sum_tarif != "") && (finder.sum_tarif_po == "" || finder.sum_tarif == finder.sum_tarif_po))
                swhere.Append(" and " + alias + ".sum_tarif = " + finder.sum_tarif);
            else
            {
                if (finder.sum_tarif != "") swhere.Append(" and " + alias + ".sum_tarif >= " + finder.sum_tarif);
                if (finder.sum_tarif_po != "") swhere.Append(" and " + alias + ".sum_tarif <= " + finder.sum_tarif_po);
            }

            finder.sum_dlt_tarif = Utils.EFlo0(finder.sum_dlt_tarif.Trim(), "");
            finder.sum_dlt_tarif_po = Utils.EFlo0(finder.sum_dlt_tarif_po.Trim(), "");
            if ((finder.sum_dlt_tarif != "") &&
                (finder.sum_dlt_tarif_po == "" || finder.sum_dlt_tarif == finder.sum_dlt_tarif_po))
                swhere.Append(" and " + alias + ".sum_dlt_tarif = " + finder.sum_dlt_tarif);
            else
            {
                if (finder.sum_dlt_tarif != "")
                    swhere.Append(" and " + alias + ".sum_dlt_tarif >= " + finder.sum_dlt_tarif);
                if (finder.sum_dlt_tarif_po != "")
                    swhere.Append(" and " + alias + ".sum_dlt_tarif <= " + finder.sum_dlt_tarif_po);
            }

            finder.sum_lgota = Utils.EFlo0(finder.sum_lgota.Trim(), "");
            finder.sum_lgota_po = Utils.EFlo0(finder.sum_lgota_po.Trim(), "");
            if ((finder.sum_lgota != "") && (finder.sum_lgota_po == "" || finder.sum_lgota == finder.sum_lgota_po))
                swhere.Append(" and " + alias + ".sum_lgota = " + finder.sum_lgota);
            else
            {
                if (finder.sum_lgota != "") swhere.Append(" and " + alias + ".sum_lgota >= " + finder.sum_lgota);
                if (finder.sum_lgota_po != "") swhere.Append(" and " + alias + ".sum_lgota <= " + finder.sum_lgota_po);
            }

            finder.sum_nedop = Utils.EFlo0(finder.sum_nedop.Trim(), "");
            finder.sum_nedop_po = Utils.EFlo0(finder.sum_nedop_po.Trim(), "");
            if ((finder.sum_nedop != "") && (finder.sum_nedop_po == "" || finder.sum_nedop == finder.sum_nedop_po))
                swhere.Append(" and " + alias + ".sum_nedop = " + finder.sum_nedop);
            else
            {
                if (finder.sum_nedop != "") swhere.Append(" and " + alias + ".sum_nedop >= " + finder.sum_nedop);
                if (finder.sum_nedop_po != "") swhere.Append(" and " + alias + ".sum_nedop <= " + finder.sum_nedop_po);
            }

            finder.c_calc = Utils.EFlo0(finder.c_calc.Trim(), "");
            finder.c_calc_po = Utils.EFlo0(finder.c_calc_po.Trim(), "");
            if ((finder.c_calc != "") && (finder.c_calc_po == "" || finder.c_calc == finder.c_calc_po))
                swhere.Append(" and " + alias + ".c_calc = " + finder.c_calc);
            else
            {
                if (finder.c_calc != "") swhere.Append(" and " + alias + ".c_calc >= " + finder.c_calc);
                if (finder.c_calc_po != "") swhere.Append(" and " + alias + ".c_calc <= " + finder.c_calc_po);
            }

            if (swhere.Length > 0)
            {
                swhere.Insert(0, " (" + alias + ".nzp_serv = " + finder.nzp_serv.ToString());
                swhere.Append(")");
                if (whereString == "") whereString += swhere.ToString();
                else whereString += " or " + swhere.ToString();
            }
        }

        /// <summary> Выполняет поиск начислений в основной БД, создает врем. табл. txxCharge и вызывает метод GetCharge
        /// </summary>
        public void FindCharge(ChargeFind finder, out Returns ret)
        {
            #region Предварительные действия

            ret = Utils.InitReturns();

            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                ret.result = false;
                ret.text = "Данные о начислениях не доступны, т.к. установлен режим работы с центральным банком данных";
                ret.tag = -1;
                return;
            }

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }

            if (finder.YM.year_ <= 0)
            {
                ret.text = "Год не задан";
                return;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                return;
            }
            string tXXCharge = "t" + Convert.ToString(finder.nzp_user) + "_charge";
#if PG
            string tXXCharge_full = "public." + tXXCharge;
#else
            string tXXCharge_full = DBManager.GetFullBaseName(conn_web)+ tableDelimiter +tXXCharge;
#endif

            #endregion


            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            #region создать таблицу webdata:tXX_charge если нужно

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            if (finder.find_from_the_start == 1) ExecSQL(conn_web, " Drop table " + tXXCharge, false);

            bool isTableCreated = false;

            StringBuilder sql = new StringBuilder();
            if (!TempTableInWebCashe(conn_web, tXXCharge))
            {

#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

                ret = DBManager.DbCreateTable(DBManager.ConnectToDb.Web, DBManager.CreateTableArgs.CreateIfNotExists,
                    DBManager.GetFullBaseName(conn_web), tXXCharge,
                    "dat_month DATE", 
                    "pref CHAR(20)", 
                    "nzp_charge SERIAL NOT NULL", 
                    "nzp_kvar INTEGER",
                    "num_ls INTEGER", 
                    "nzp_serv INTEGER",
                    "service CHAR(100)", 
                    "ordering INTEGER", 
                    "nzp_supp INTEGER", 
                    "name_supp CHAR(100)",
                    "nzp_frm INTEGER", 
                    "name_frm CHAR(60)",
                    "nzp_measure INTEGER", 
                    "measure CHAR(20)", 
                    "dat_charge DATE",
                    "tarif "                + sDecimalType + "(14,3)",
                    "tarif_p "              + sDecimalType + "(14,3)", 
                    "rsum_tarif "           + sDecimalType + "(14,2)",
                    "rsum_lgota "           + sDecimalType + "(14,2)", 
                    "sum_tarif "            + sDecimalType + "(14,2)", 
                    "sum_tarif_p "          + sDecimalType + "(14,2)",
                    "sum_dlt_tarif "        + sDecimalType + "(14,2)",
                    "sum_dlt_tarif_p "      + sDecimalType + "(14,2)", 
                    "sum_tarif_sn_f "       + sDecimalType + "(14,2)", 
                    "sum_lgota "            + sDecimalType + "(14,2)",
                    "sum_dlt_lgota "        + sDecimalType + "(14,2)",
                    "sum_dlt_lgota_p "      + sDecimalType + "(14,2)", 
                    "sum_lgota_p "          + sDecimalType + "(14,2)", 
                    "sum_nedop "            + sDecimalType + "(14,2)",
                    "sum_nedop_p "          + sDecimalType + "(14,2)",
                    "sum_real "             + sDecimalType + "(14,2)",
                    "sum_charge "           + sDecimalType + "(14,2)", 
                    "reval "                + sDecimalType + "(14,2)",
                    "real_pere "            + sDecimalType + "(14,2)", 
                    "sum_pere "             + sDecimalType + "(14,2)",
                    "real_charge "          + sDecimalType + "(14,2)", 
                    "sum_money "            + sDecimalType + "(14,2)", 
                    "money_to "             + sDecimalType + "(14,2)",
                    "money_from "           + sDecimalType + "(14,2)", 
                    "money_del "            + sDecimalType + "(14,2)",
                    "sum_fakt "             + sDecimalType + "(14,2)", 
                    "fakt_to "              + sDecimalType + "(14,2)", 
                    "fakt_from "            + sDecimalType + "(14,2)",
                    "fakt_del "             + sDecimalType + "(14,2)", 
                    "sum_insaldo "          + sDecimalType + "(14,2)",
                    "izm_saldo "            + sDecimalType + "(14,2)", 
                    "sum_outsaldo "         + sDecimalType + "(14,2)", 
                    "sum_subsidy "          + sDecimalType + "(14,2)",
                    "sum_subsidy_p "        + sDecimalType + "(14,2)",
                    "sum_subsidy_reval "    + sDecimalType + "(14,2)", 
                    "sum_subsidy_all "      + sDecimalType + "(14,2)", 
                    "tarif_f "              + sDecimalType + "(14,3)",
                    "tarif_f_p "            + sDecimalType + "(14,3)",
                    "sum_tarif_f "          + sDecimalType + "(14,2)", 
                    "sum_tarif_f_p "        + sDecimalType + "(14,2)", 
                    "isblocked INTEGER",
                    "is_device INTEGER default 0", 
                    "c_calc "               + sDecimalType + "(14,2)",
                    "c_sn "                 + sDecimalType + "(14,2) default 0", 
                    "c_okaz "               + sDecimalType + "(14,2)", 
                    "c_nedop "              + sDecimalType + "(14,2)", 
                    "isdel INTEGER",
                    "c_reval "              + sDecimalType + "(14,2)",
                    "norma "                + sDecimalType + "(14,7)", 
                    "norma_rashod "         + sDecimalType + "(14,7)", 
                    "rashod "               + sDecimalType + "(14,7)",
                    "rashod_odn "           + sDecimalType + "(14,7)", 
                    "priznak_rasch INTEGER");
                if (!(isTableCreated = ret.result))
                {
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            //заполнить webdata:tXXCharge
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) { conn_web.Close(); return; }

            #region Собираем начисления по месяцам


            #region подготовка выборки
            string whereString = ""; //чтобы ничего не выбиралось
            string dat_month;
            string dat_charge, dat_charge_past, charge_table;
            string cur_pref = finder.pref;

            WhereStringForFindCommon(finder, "a", ref whereString);
            WhereStringForFindServices(finder, "a", ref whereString);

            sql = new StringBuilder();

            string Insert = " Insert into " + tXXCharge_full +
                            " ( dat_month, pref, nzp_charge, nzp_kvar, num_ls, nzp_serv, service, ordering, nzp_supp, name_supp, nzp_frm, name_frm, nzp_measure, measure, dat_charge, tarif, tarif_p, " +
                            " rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p,sum_tarif_sn_f, sum_tarif_p, sum_lgota, sum_dlt_lgota, " +
                            " sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                            " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, " +
                            " izm_saldo, sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, " +
                            " c_sn, c_okaz, c_nedop, isdel, c_reval, tarif_f, tarif_f_p, sum_tarif_f, sum_tarif_f_p " +
                            ") ";
            string fields = "";
            int s = 0;

            IDataReader reader = null, reader2 = null;
            string charge, charge_full;
            string counters, counters_full;
#if PG
            string sServerName = "";
#else
            string sServerName = "@" + DBManager.getServer(conn_db);
#endif

            string services = Points.Pref + "_kernel" + sServerName + tableDelimiter + "services s";
            string supplier = Points.Pref + "_kernel" + sServerName + tableDelimiter + "supplier supp";
            string formuls  = Points.Pref + "_kernel" + sServerName + tableDelimiter + "formuls f";

            if (!TempTableInWebCashe(conn_db, null,
#if PG
                Regex.Match(formuls, "[\\w\\d\\.]+").Value
#else
                formuls
#endif
                ))
            {
                ret.result = false;
                ret.text = "Данные о начислениях временно не доступны.";
                ret.tag = -1;

                conn_web.Close();
                conn_db.Close();
                return;
            }
            #endregion

            // определить месяцы начислений
            int m1 = 1;
            int m2 = 12;
            /*if (finder.month_po > 0)
            {
                m2 = finder.month_po;
                if (finder.month_ > 0) m1 = finder.month_;
            }
            else if (finder.month_ > 0) m1 = m2 = finder.month_;*/

            for (var i = m1; i <= m2; i++)
            {
                #region выборка для текущего месяца

                charge        = cur_pref + "_charge_" + (finder.YM.year_ % 100).ToString("00")                  + tableDelimiter + "charge_" + i.ToString("00");
                charge_full   = cur_pref + "_charge_" + (finder.YM.year_ % 100).ToString("00") + sServerName    + tableDelimiter + "charge_" + i.ToString("00");

                if (!TempTableInWebCashe(conn_db, null, charge)) continue;
                dat_month = MDY(i, 1, finder.YM.year_);

#if PG
                string sExistsCol =
                    " select count(*) from information_schema.columns" +
                    " where table_name = '" + charge +
                    "' and table_schema = CURRENT_SCHEMA() and column_name = 'sum_tarif_sn_f'";
#else
                string sExistsCol =
                    " select count(*) from systables t, syscolumns c"+
                    " where lower(tabname) = '" + charge + "' " +
                    " and c.tabid = t.tabid and c.colname='sum_tarif_sn_f'";
#endif
                IDbCommand cmd = DBManager.newDbCommand(sExistsCol, conn_db);
                try
                {
                    s = Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (Exception ex)
                {
                    conn_web.Close();
                    conn_db.Close();

                    ret.result = false;
                    ret.text = "Ошибка в запросе"; //ex.Message;

                    string err;
                    if (Constants.Viewerror)
                        err = " \n " + ex.Message;
                    else
                        err = "";

                    MonitorLog.WriteLog("Ошибка в запросе " + err, MonitorLog.typelog.Error, 20, 201, true);
                    return;
                }

                if (s > 0)
                {
                    fields = " a.sum_tarif_sn_f, ";
                }
                else fields = " a.sum_dlt_tarif_p as sum_tarif_sn_f, ";

                // загрузка исходных начислений и будущих перерасчетов
                sql = new StringBuilder();
                sql.Append(Insert);
                sql.Append(" Select " + dat_month + ", '" + cur_pref +
                           "', a.nzp_charge, a.nzp_kvar, a.num_ls, a.nzp_serv, s.service, s.ordering, a.nzp_supp, supp.name_supp, a.nzp_frm, f.name_frm, f.nzp_measure, " +
                           " (case when a.nzp_frm = 0 then s.ed_izmer  else (select measure from " + cur_pref + "_kernel" + tableDelimiter + "s_measure " +
                           " where nzp_measure = f.nzp_measure) end) as measure, a.dat_charge, a.tarif, a.tarif_p, " +
                           " a.rsum_tarif, a.rsum_lgota, a.sum_tarif, a.sum_dlt_tarif, a.sum_dlt_tarif_p, " + fields +
                           " a.sum_tarif_p, a.sum_lgota, a.sum_dlt_lgota, " +
                           " a.sum_dlt_lgota_p, a.sum_lgota_p, a.sum_nedop, a.sum_nedop_p, a.sum_real, a.sum_charge, a.reval, a.real_pere, a.sum_pere, " +
                           " a.real_charge, a.sum_money, a.money_to, a.money_from, a.money_del, a.sum_fakt, a.fakt_to, a.fakt_from, a.fakt_del, a.sum_insaldo, " +
                           " a.izm_saldo, a.sum_outsaldo, a.sum_subsidy, a.sum_subsidy_p, a.sum_subsidy_reval, a.sum_subsidy_all, a.isblocked, a.is_device, a.c_calc, " +
                           " a.c_sn, a.c_okaz, a.c_nedop, a.isdel, a.c_reval, a.tarif_f, a.tarif_f_p, a.sum_tarif_f, a.sum_tarif_f_p " +
                           " From " + charge_full + " a");
#if PG
                sql.Append(" left outer join " + services + " ON a.nzp_serv = s.nzp_serv " +
                           " left outer join " + supplier + " ON a.nzp_supp = supp.nzp_supp " +
                           " left outer join " + formuls  + " ON a.nzp_frm  = f.nzp_frm ");
                sql.Append(" Where 1 = 1");
#else
                sql.Append(", outer " + services +
                           ", outer " + supplier +
                           ", outer " + formuls);
                sql.Append(" Where a.nzp_serv = s.nzp_serv and a.nzp_supp = supp.nzp_supp and a.nzp_frm = f.nzp_frm");
#endif
                // загрузка исходных начислений и будущих перерасчетов
                sql.Append(" and a.nzp_serv > 1 ");//"and a.dat_charge is null ");
                sql.Append(whereString);

                //записать текст sql в лог-журнал
                //int key = LogSQL(conn_web, finder.nzp_user, sql.ToString());
                //MonitorLog.WriteLog(sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) { conn_db.Close(); conn_web.Close(); return; }

                //загрузка нормы                   
                string calc_gku_full = cur_pref + "_charge_" + (finder.YM.year_ % 100).ToString("00") + sServerName + tableDelimiter + "calc_gku_" + i.ToString("00");

                sql = new StringBuilder();
                sql.Append(" select rash_norm_one,rashod_norm,nzp_serv,nzp_kvar,nzp_supp from " + calc_gku_full +
                           " where nzp_kvar in (Select nzp_kvar from " + tXXCharge_full + ") ");
                if (ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                {
                    try
                    {
                        while (reader2.Read())
                        {
                            decimal norma = 0, rashod_norm = 0;
                            int nzp_kvar = 0, nzp_serv = 0, nzp_supp = 0;
                            if (reader2["rash_norm_one"] != DBNull.Value) norma = Convert.ToDecimal(reader2["rash_norm_one"]);
                            if (reader2["rashod_norm"] != DBNull.Value) rashod_norm = Convert.ToDecimal(reader2["rashod_norm"]);
                            if (reader2["nzp_kvar"] != DBNull.Value) nzp_kvar = Convert.ToInt32(reader2["nzp_kvar"]);
                            if (reader2["nzp_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                            if (reader2["nzp_supp"] != DBNull.Value) nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);

                            sql = new StringBuilder();
                            sql.Append(" update " + tXXCharge_full +
                                       " set norma_rashod = " + rashod_norm + ", norma = " + norma +
                                       " where nzp_serv = " + nzp_serv + " and nzp_supp=" + nzp_supp + " and nzp_kvar = " + nzp_kvar +
                                         " and dat_charge is null and dat_month = " + dat_month);
                            ret = ExecSQL(conn_db, sql.ToString(), true);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (reader2 != null) reader2.Close();
                        conn_web.Close();
                        conn_db.Close();
                        ret = new Returns(false, "Ошибка в FindCharge:" + ex.Message);
                        MonitorLog.WriteLog(
                            "Ошибка в FindCharge:" + (Constants.Viewerror ? "\n " + ex.Message : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }
                    reader2.Close();
                }
                
                #endregion
                
                #region выборка по перерасчетам

                string sChargeDB = cur_pref + "_charge_" + finder.YM.year_.ToString("0000").Substring(2, 2) + tableDelimiter;
                // загрузка прошлых перерасчетов                    
                sql = new StringBuilder();
                sql.Append(" Select distinct month_, year_" +
                           " From " + sChargeDB + "lnk_charge_" + i.ToString("00") + " a " +
                           " Where a.nzp_kvar in ( Select nzp_kvar from " + tXXCharge_full + " b Where b.dat_month = " + dat_month + ")" +
                             " and ( (a.year_ > " + Points.BeginWork.year_ + ") or " +
                                   " (a.year_ = " + Points.BeginWork.year_ + " and a.month_ >= " + Points.BeginWork.month_ + ")" +
                                 " ) ");
                if (ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    try
                    {
                        while (reader.Read())
                        {
                            // таблица с начислениями за месяц, в котором был перерасчет, вызванный из месяца dat_month
                            charge_table = cur_pref + "_charge_" + String.Format("{0:0000}", reader["year_"]).Substring(2, 2) + tableDelimiter + 
                                "charge_" + String.Format("{0:00}", reader["month_"]);
                            // dat_charge в этой таблице
                            dat_charge = MDY(i, 28, finder.YM.year_);
                            // "28." + String.Format("{0:00}.{1:0000}", i, finder.YM.year_);
                            // dat_charge, который будет в КЭШ таблице, он < dat_month и указывает на прошлые перерасчеты, вызванные в месяце dat_month
                            dat_charge_past = MDY(Convert.ToInt32(reader["month_"]), 28, Convert.ToInt32(reader["year_"]));
                            //"28." + String.Format("{0:00}.{1:0000}", reader["month_"], reader["year_"]);

                            sql = new StringBuilder();
                            sql.Append(Insert);
                            sql.Append(" Select " + dat_month + ", '" + cur_pref + "', a.nzp_charge, a.nzp_kvar, a.num_ls," +
                                       " a.nzp_serv, s.service, s.ordering, a.nzp_supp, supp.name_supp, a.nzp_frm, f.name_frm, f.nzp_measure, " +
                                       " (case when a.nzp_frm = 0" +
                                             " then s.ed_izmer " +
                                             " else (select measure from " + cur_pref + "_kernel" + tableDelimiter + "s_measure" +
                                                    " where nzp_measure = f.nzp_measure)" +
                                       "  end) as measure " + ", " +
                                       dat_charge_past + ", a.tarif, a.tarif_p, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, " +
                                       fields + " sum_tarif_p, sum_lgota, sum_dlt_lgota, " +
                                       " sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                                       " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, " +
                                       " izm_saldo, sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, a.is_device," +
                                       " c_calc, c_sn, c_okaz, c_nedop, isdel, c_reval,  a.tarif_f, a.tarif_f_p, a.sum_tarif_f, a.sum_tarif_f_p " +
                                       " From " + charge_table + " a");
#if PG
                            sql.Append(" left outer join " + services + " ON a.nzp_serv = s.nzp_serv " +
                                       " left outer join " + supplier + " ON a.nzp_supp = supp.nzp_supp " +
                                       " left outer join " + formuls  + " ON a.nzp_frm = f.nzp_frm " +
                                       " Where 1 = 1");
#else
                            sql.Append(", outer " + services +
                                       ", outer " + supplier +
                                       ", outer " + formuls +
                                       " Where a.nzp_serv = s.nzp_serv and a.nzp_supp = supp.nzp_supp and a.nzp_frm = f.nzp_frm");
#endif

                            sql.Append(" and a.nzp_serv > 1 and a.dat_charge = " + dat_charge + " ");
                            sql.Append(whereString);
                            ret = ExecSQL(conn_db, sql.ToString(), true);
                            if (!ret.result) { conn_db.Close(); conn_web.Close(); return; }

                            //загрузка нормы                   
                            string calc_gku_db = cur_pref + "_charge_" + String.Format("{0:0000}", reader["year_"]).Substring(2, 2) + tableDelimiter +
                                "calc_gku" + String.Format("{0:0000}", finder.YM.year_).Substring(2, 2) + String.Format("{0:00}", i) + "_" + String.Format("{0:00}", reader["month_"]);

                            sql = new StringBuilder();
                            sql.Append(" select rash_norm_one,rashod_norm,nzp_serv,nzp_kvar,nzp_supp from " + calc_gku_db +
                                       " where nzp_kvar in (Select nzp_kvar from " + tXXCharge_full + ") ");
                            try
                            {
                                ret = ExecRead(conn_db, out reader2, sql.ToString(), false);
                                if (ret.result)
                                {
                                    while (reader2.Read())
                                    {
                                        decimal norma = 0, rashod_norm = 0;
                                        int nzp_kvar = 0, nzp_serv = 0, nzp_supp = 0;
                                        if (reader2["rash_norm_one"] != DBNull.Value) norma = Convert.ToDecimal(reader2["rash_norm_one"]);
                                        if (reader2["rashod_norm"] != DBNull.Value) rashod_norm = Convert.ToDecimal(reader2["rashod_norm"]);
                                        if (reader2["nzp_kvar"] != DBNull.Value) nzp_kvar = Convert.ToInt32(reader2["nzp_kvar"]);
                                        if (reader2["nzp_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                                        if (reader2["nzp_supp"] != DBNull.Value) nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);

                                        sql = new StringBuilder();
                                        sql.Append(" update " + tXXCharge_full +
                                                   " set norma_rashod = " + rashod_norm + ", norma = " + norma +
                                                   " where nzp_serv = " + nzp_serv + " and nzp_supp=" + nzp_supp +
                                                   " and nzp_kvar = " + nzp_kvar +
                                                   " and dat_charge=" + dat_charge_past + " and dat_month = " + dat_month);
                                        ret = ExecSQL(conn_db, sql.ToString(), true);
                                    }
                                }
                            }
                            finally
                            {
                                if (reader2 != null) reader2.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (reader != null) reader.Close();
                        conn_web.Close();
                        conn_db.Close();
                        ret = new Returns(false, "Ошибка в FindCharge:" + ex.Message);
                        MonitorLog.WriteLog(
                            "Ошибка в FindCharge:" + (Constants.Viewerror ? "\n " + ex.Message : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }
                    reader.Close();
                }
                #endregion
                
            }

            #endregion

            #region Установка признака расчета и начислено по нормативу

            sql = new StringBuilder();
            sql.Append(" update " + tXXCharge_full + 
                       " set  priznak_rasch = 1" +
                       " where is_device = 0 ");
            ret = ExecSQL(conn_db, sql.ToString(), true);

            sql = new StringBuilder();
            sql.Append("select distinct dat_month from " + tXXCharge_full);
            if (ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                while (reader.Read())
                {
                    DateTime datmonth = DateTime.MinValue;
                    if (reader["dat_month"] != DBNull.Value) datmonth = Convert.ToDateTime(reader["dat_month"]);
                    counters_full = cur_pref + "_charge_" + (datmonth.Year % 100).ToString("00") + sServerName + tableDelimiter + "counters_" + datmonth.Month.ToString("00");

                    sql = new StringBuilder();
                    sql.Append(" update " + tXXCharge_full + 
                               " set priznak_rasch = 4" +
                               " where exists ( Select 1" +
                                              " from " + counters_full + " c," + cur_pref + "_kernel" + sServerName + tableDelimiter + "serv_odn s," + 
                                                         cur_pref + "_data" + sServerName + tableDelimiter + "kvar k" +
                                              " where c.nzp_dom=k.nzp_dom and k.nzp_kvar=" + tXXCharge_full + ".nzp_kvar" +
                                                " and c.nzp_serv=s.nzp_serv_link and s.nzp_serv=" + tXXCharge_full + ".nzp_serv" +
                                                " and c.nzp_type=1 and c.stek in (1,2) )" +
                                 " and " + tXXCharge_full + ".dat_month = " + Utils.EStrNull(datmonth.ToShortDateString()));
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    sql = new StringBuilder();
                    sql.Append(" update " + tXXCharge_full + 
                               " set  priznak_rasch = 3" +
                               " where is_device in (1,9)" +
                                 " and exists ( Select 1 from " + counters_full + " c" +
                                              " where c.nzp_kvar=" + tXXCharge_full + ".nzp_kvar and c.nzp_serv=" + tXXCharge_full +".nzp_serv" +
                                                " and nzp_type=3 and stek in (2) ) "+
                                 " and " + tXXCharge_full + ".dat_month = " + Utils.EStrNull(datmonth.ToShortDateString()));
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    sql = new StringBuilder();
                    sql.Append(" update " + tXXCharge_full + 
                               " set  priznak_rasch = 2" +
                               " where is_device in (1,9)" +
                                 " and exists ( Select 1 from " + counters_full + " c" +
                                              " where c.nzp_kvar=" + tXXCharge_full + ".nzp_kvar and c.nzp_serv=" + tXXCharge_full + ".nzp_serv" +
                                                " and nzp_type=3 and stek in (1) ) "+
                                 " and " + tXXCharge_full + ".dat_month = " + Utils.EStrNull(datmonth.ToShortDateString()));
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    //выставить признак расчета 4 -	исходя из показаний ОДПУ, в случае если расход ОДПУ=0
                    sql = new StringBuilder();
                    sql.Append(" update " + tXXCharge_full +
                               " set  priznak_rasch = 4" +
                               " where is_device = 0 " +
                                 " and exists ( Select 1 from " + counters_full + " c " +
                                                 " where c.nzp_kvar=" + tXXCharge_full + ".nzp_kvar" +
                                                 " and c.nzp_serv=" + tXXCharge_full + ".nzp_serv" +
                                                 " and c.nzp_type=3 and c.stek=3 and c.kod_info > 100 ) " +
                                 " and " + tXXCharge_full + ".dat_month = " + Utils.EStrNull(datmonth.ToShortDateString()));
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                }
                reader.Close();
            }

            sql = new StringBuilder();
            sql.Append(" update " + tXXCharge_full + 
                       " set  priznak_rasch = 5" +
                       " where is_device = 0" +
                         " and exists (Select 1 from " + cur_pref + "_data" + tableDelimiter + "kvar k" +
                                     " where k.nzp_kvar=" + tXXCharge_full + ".nzp_kvar and typek in (2,3) ) ");
            ret = ExecSQL(conn_db, sql.ToString(), true);

            // установить по норме для ОДН если нет ОДПУ
            sql = new StringBuilder();
            sql.Append(" update " + tXXCharge_full + 
                       " set priznak_rasch = 1" +
                       " where " + sNvlWord + "(priznak_rasch,0) <> 4" +
                         " and exists (Select 1 from " + Points.Pref + "_kernel" + tableDelimiter + "serv_odn k" +
                                     " where k.nzp_serv = " + tXXCharge_full + ".nzp_serv) ");
            ret = ExecSQL(conn_db, sql.ToString(), true);

            // установить по норме всем кому не определилось
            sql = new StringBuilder();
            sql.Append(" update " + tXXCharge_full +
                       " set priznak_rasch = 1" +
                       " where " + sNvlWord + "(priznak_rasch,0) < 1 or " + sNvlWord + "(priznak_rasch,0) > 5 ");
            ret = ExecSQL(conn_db, sql.ToString(), true);

            #endregion

            #region Определение признака расхода для услуги Канализация

            object count = ExecScalar(conn_web, 
                " select count(*) from " + tXXCharge_full + 
                " where nzp_serv = " + (int)ServiceIds.Kanalizacia + " and priznak_rasch = 1"
                , out ret, true);
            int recordsTotalCount = 0;
            try
            {
                recordsTotalCount = Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Ошибка FindCharge " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                ret =  new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                return ;
            }

            if (recordsTotalCount > 0)
            {

                ExecSQL(conn_db, "drop table tcharge7", false);
                ret = ExecSQL(conn_db,
                    "create temp table tcharge7" +
                    "(nzp_kvar integer," +
                    " nzp_serv integer," +
                    " dat_charge date," +
                    " dat_month date," +
                    " priznak integer default 0," +
                    " priznak_hvs integer default 0," +
                    " priznak_gvs integer default 0) " +
                    sUnlogTempTable);
                if (!ret.result) return;

                try
                {
                    ret = ExecSQL(conn_db,
                        " insert into tcharge7 (nzp_kvar, nzp_serv, dat_charge, dat_month)" +
                        " select distinct nzp_kvar, nzp_serv, " + sNvlWord + "(dat_charge," + MDY(1, 1, 3000) + "), dat_month" +
                        " from " + tXXCharge_full + 
                        " where nzp_serv = " + (int) ServiceIds.Kanalizacia + " and priznak_rasch = 1 ");
                    if (!ret.result) return;

                    ret = ExecSQL(conn_db,
                        " update tcharge7" +
                        " set" +
                          " priznak_hvs = (select " + sNvlWord + "(max(priznak_rasch),0)" +
                                         " from " + tXXCharge_full + 
                                         " where nzp_kvar = tcharge7.nzp_kvar and nzp_serv = " + (int) ServiceIds.HVS +
                                           " and " + sNvlWord + "(dat_charge," + MDY(1, 1, 3000) + ") = tcharge7.dat_charge" +
                                           " and dat_month = tcharge7.dat_month) " +
                          ",priznak_gvs = (select " + sNvlWord + "(max(priznak_rasch),0)" +
                                         " from " + tXXCharge_full +
                                         " where nzp_kvar = tcharge7.nzp_kvar and nzp_serv = " + (int) ServiceIds.GVS + 
                                           " and " + sNvlWord + "(dat_charge," + MDY(1, 1, 3000) + ") = tcharge7.dat_charge" +
                                           " and dat_month = tcharge7.dat_month) ");
                    if (!ret.result) return;

                    ret = ExecSQL(conn_db,
                        "update tcharge7" +
                        " set priznak =" +
                        " case " +
                        " when priznak_hvs = priznak_gvs then priznak_hvs " +
                        " when priznak_gvs = 0 then priznak_hvs" +
                        " when priznak_hvs = 0 then priznak_gvs" +
                        " when priznak_hvs = 2 or priznak_gvs = 2 then 2" +
                        " when priznak_hvs = 3 or priznak_gvs = 3 then 3" +
                        " when priznak_hvs = 4 or priznak_gvs = 4 then 4" +
                        " when priznak_hvs = 5 or priznak_gvs = 5 then 5" +
                        " else priznak" +
                        " end"
                        );
                    if (!ret.result) return;

                    ret = ExecSQL(conn_db, 
                        "update " + tXXCharge_full +
                        " set priznak_rasch =" +
                        " (select priznak from tcharge7" +
                         " where nzp_kvar = " + tXXCharge_full + ".nzp_kvar" +
                          " and dat_charge = " + sNvlWord + "(" + tXXCharge_full + ".dat_charge," + MDY(1, 1, 3000) + ")" +
                          " and dat_month = " + tXXCharge_full + ".dat_month) " +
                        " where nzp_serv = " + (int) ServiceIds.Kanalizacia);
                    if (!ret.result) return;
                }
                finally
                {
                    ExecSQL(conn_db, "drop table tcharge7", true);
                }
            }

            #endregion

            #region добавить фиктивные записи для случаев, когда в каком-то месяце есть перерасчеты но нет начислений

            sql = new StringBuilder();
            sql.Append(Insert);
            sql.Append(
                " Select" +
                "  a.dat_month, a.pref, 0, a.nzp_kvar, a.num_ls, a.nzp_serv, a.service, a.ordering, 0, '', 0, '', 0, ''," +
                "  date(null), 0, 0,  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, " +
                "  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 "+
                " From " + tXXCharge_full + " a "+
                " Where dat_charge is not null" +
                  " and not exists (select 1 from " + tXXCharge_full + " b" +
                                  " where b.dat_month = a.dat_month and b.pref = a.pref " +
                                    " and b.nzp_kvar = a.nzp_kvar and b.nzp_serv = a.nzp_serv and b.dat_charge is null) "+
                " Group by dat_month, pref, nzp_kvar, num_ls, nzp_serv, service, ordering ");
            ret = ExecSQL(conn_web, sql.ToString(), true);

            //норма для перерасчетов
            sql = new StringBuilder();
            sql.Append(" select distinct dat_charge, dat_month" +
                       " from " + tXXCharge_full + 
                       " where dat_charge is not null ");
            if (ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                DateTime datmonth;
                DateTime datcharge;

                while (reader.Read())
                {
                    datmonth = DateTime.MinValue;
                    datcharge = DateTime.MinValue;

                    if (reader["dat_month"]  != DBNull.Value) datmonth  = Convert.ToDateTime(reader["dat_month"]);
                    if (reader["dat_charge"] != DBNull.Value) datcharge = Convert.ToDateTime(reader["dat_charge"]);

                    string calc_gku_YYMM_full;

                    if (datmonth > datcharge)
                    {
                        calc_gku_YYMM_full =
                            cur_pref + "_charge_" + (datcharge.Year % 100).ToString("00") + tableDelimiter + 
                            "calc_gku" + (datmonth.Year%100).ToString("00") + datmonth.Month.ToString("00") + "_" + datcharge.Month.ToString("00");
                    }
                    else
                    {
                        calc_gku_YYMM_full = 
                            cur_pref + "_charge_" + (datmonth.Year%100).ToString("00") + tableDelimiter + 
                            "calc_gku" + (datcharge.Year%100).ToString("00") + datcharge.Month.ToString("00") + "_" + datmonth.Month.ToString("00");
                    }
               

                    if (!TempTableInWebCashe(conn_db, calc_gku_YYMM_full)) continue;

                    sql = new StringBuilder();
                    sql.Append(" select rash_norm_one,rashod_norm, nzp_serv,nzp_kvar,nzp_supp" +
                               " from " + calc_gku_YYMM_full +
                               " where nzp_kvar in (Select nzp_kvar from " + tXXCharge_full + ") and stek=3 ");
                    if (ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                    {
                        try
                        {
                            while (reader2.Read())
                            {
                                decimal norma = 0, norma_rashod = 0;
                                int nzp_kvar = 0, nzp_serv = 0, nzp_supp = 0;
                                if (reader2["rash_norm_one"] != DBNull.Value) norma = Convert.ToDecimal(reader2["rash_norm_one"]);
                                if (reader2["rashod_norm"]   != DBNull.Value) norma_rashod = Convert.ToDecimal(reader2["rashod_norm"]);
                                if (reader2["nzp_kvar"]      != DBNull.Value) nzp_kvar = Convert.ToInt32(reader2["nzp_kvar"]);
                                if (reader2["nzp_serv"]      != DBNull.Value) nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                                if (reader2["nzp_supp"]      != DBNull.Value) nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);

                                sql = new StringBuilder();
                                sql.Append(" update " + tXXCharge_full + 
                                           " set norma = " + norma + ", norma_rashod=" + norma_rashod +
                                           " where nzp_serv = " + nzp_serv + " and nzp_supp=" + nzp_supp +
                                             " and nzp_kvar = " + nzp_kvar +
                                             " and dat_month = " + MDY(datmonth.Month, datmonth.Day, datmonth.Year) +
                                             " and dat_charge = " + MDY(datcharge.Month, datcharge.Day, datcharge.Year));
                                ret = ExecSQL(conn_db, sql.ToString(), true);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (reader != null) reader.Close();
                            if (reader2 != null) reader2.Close();
                            conn_web.Close();
                            conn_db.Close();
                            ret = new Returns(false, "Ошибка в FindCharge:" + ex.Message);
                            MonitorLog.WriteLog(
                                "Ошибка в FindCharge:" + (Constants.Viewerror ? "\n " + ex.Message : ""),
                                MonitorLog.typelog.Error, 20, 201, true);
                            return;
                        }
                        reader2.Close();
                    }
                }
                reader.Close();
            }
            #endregion

            #region Для Самары - расход и расход с учетом ОДН

            if (Points.IsSmr)
            {
                //расход и расход с учетом ОДН
                sql = new StringBuilder();
                sql.Append("select * from " + tXXCharge_full + " where dat_charge is null");
                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                int nzp_kvar = 0, nzp_serv = 0, nzp_charge = 0;
                string datmonth = "";
                DateTime dt = DateTime.MinValue;
                decimal rashod = 0, vl210 = 0;
                while (reader.Read())
                {
                    if (reader["nzp_charge"] != DBNull.Value) nzp_charge = Convert.ToInt32(reader["nzp_charge"]);
                    if (reader["nzp_kvar"]   != DBNull.Value) nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                    if (reader["nzp_serv"]   != DBNull.Value) nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["dat_month"]  != DBNull.Value) datmonth = Convert.ToString(reader["dat_month"]);
                    dt = DateTime.MinValue;
                    DateTime.TryParse(datmonth, out dt);
                    if (dt == DateTime.MinValue) continue;
                    counters_full = 
                        cur_pref + "_charge_" + (dt.Year - 2000).ToString("00") + sServerName + tableDelimiter + "counters_" + dt.Month.ToString("00");

                    sql = new StringBuilder();
                    sql.Append(" select rashod, (val1+val2+dlt_reval+dlt_real_charge) as val" +
                               " from " + counters_full +
                               " where nzp_kvar = " + nzp_kvar + " and nzp_serv = " + nzp_serv + " and nzp_type = 3 and stek = 3 ");
                    ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
                    if (!ret.result)
                    {
                        reader.Close();
                        conn_db.Close();
                        return;
                    }
                    rashod = vl210 = 0;
                    if (reader2.Read())
                    {
                        if (reader2["rashod"] != DBNull.Value) rashod = Convert.ToDecimal(reader2["rashod"]);
                        if (reader2["val"]    != DBNull.Value) vl210  = Convert.ToDecimal(reader2["val"]);
                    }
                    reader2.Close();

                    sql = new StringBuilder();
                    sql.Append(" update " + tXXCharge_full + 
                               " set rashod  = " + vl210 + ", rashod_odn = " + rashod +
                               " where nzp_charge = " + nzp_charge+" and isdel<>1");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        CloseReader(ref reader);
                        conn_db.Close();
                        return;
                    }
                }
                reader.Close();
            }
            #endregion

            conn_db.Close(); //закрыть соединение с основной базой
            //далее работаем с кешем
            
            #region создаем индексы на tXXCharge
            if (isTableCreated)
            {
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
                string ix = "ix" + finder.nzp_user + "_charge";
                ret = ExecSQL(conn_web,
                    " Create index " + ix + "_1 on " + sDefaultSchema + tXXCharge + " (dat_month,nzp_kvar,nzp_serv, nzp_supp, nzp_frm,pref) "
                    , false);
            }
            if (ret.result) ret = ExecSQL(conn_web, DBManager.sUpdStat + " " + tXXCharge, true);
            #endregion

            conn_web.Close();
            return;
        } //FindCharge

        public Returns PrepareReport5_20(ChargeFind finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Не указан пользователь", -1);

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
#if PG
            string tXX_spls_full = "public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            string tXX_rep5_20 = "t" + Convert.ToString(finder.nzp_user) + "_rep5_20";
#if PG
            string tXX_rep5_20_full = "public." + tXX_rep5_20;
#else
            string tXX_rep5_20_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_rep5_20;
#endif

            if (!TableInWebCashe(conn_web, tXX_spls))
            {
                conn_web.Close();
                return new Returns(false, "Лицевые счета не выбраны", -1);
            }

            ExecSQL(conn_web, "drop table " + tXX_rep5_20, false);

            string sql;

            bool isTableCreated = false;
            if (!TableInWebCashe(conn_web, tXX_rep5_20))
            {
                #region создать таблицу webdata:tXX_rep5_20

#if PG
                sql = "CREATE TABLE " + tXX_rep5_20 +
                      " ( nzp_rep serial not null, " +
                      "   adr char(250), " +
                      "   sum_insaldo NUMERIC(14,2), " +
                      "   sum_real NUMERIC(14,2), " +
                      "   sum_izm NUMERIC(14,2), " +
                      "   sum_money NUMERIC(14,2), " +
                      "   sum_outsaldo NUMERIC(14,2))";
#else
                sql = "CREATE TABLE " + tXX_rep5_20 +
                    " ( nzp_rep serial not null, " +
                    "   adr char(250), " +
                    "   sum_insaldo DECIMAL(14,2), " +
                    "   sum_real DECIMAL(14,2), " +
                    "   sum_izm DECIMAL(14,2), " +
                    "   sum_money DECIMAL(14,2), " +
                    "   sum_outsaldo DECIMAL(14,2))";
#endif
                ret = ExecSQL(conn_web, sql.ToString(), false);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
                else isTableCreated = true;

                #endregion
            }
            else
            {
                ret = ExecSQL(conn_web, "delete from " + tXX_rep5_20, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }

            string database = "";
            string tablename = "";
            string pref = "";
            string where_str = "";

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            IDataReader reader = null;
#if PG
            sql = "select distinct pref From " + tXX_spls;
#else
            sql = "select unique pref From " + tXX_spls;
#endif
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            try
            {
                while (reader.Read()) // цикл по префиксам
                {
                    if (reader["pref"] == DBNull.Value) continue;

                    pref = (string)reader["pref"].ToString().Trim();

                    database = pref + "_charge_" + finder.year_.ToString().Substring(2, 2);
                    tablename = "charge_" +
                                ((finder.month_.ToString().Trim().Length == 1)
                                    ? "0" + finder.month_.ToString().Trim()
                                    : finder.month_.ToString().Trim());
#if PG
                    ExecSQL(conn_db, "set search_path to '" + database + "'", true);
#else
                    ExecSQL(conn_db, "database " + database, true);
#endif
                    if (!TableInWebCashe(conn_db, tablename)) continue;

                    sql = "Insert into " + tXX_rep5_20_full +
                          " (nzp_rep, adr, sum_insaldo, sum_real, sum_money, sum_izm, sum_outsaldo)";
                    sql +=
                        " Select 0, trim(a.adr)||' '||trim(a.fio), sum(sum_insaldo), sum(real_charge + reval), sum(sum_real), sum(sum_money), sum(sum_outsaldo)" +
                        " From " + tablename + " ch, " + tXX_spls_full + " a" +
                        " Where ch.nzp_kvar = a.nzp_kvar and ch.dat_charge is null and ch.nzp_serv > 1";
                    WhereStringForFindCommon(finder, "ch", ref where_str);
                    sql += where_str + " Group by a.nzp_kvar, a.adr, a.fio";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return ret;
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_db.Close();
                conn_web.Close();

                MonitorLog.WriteLog("Ошибка выполнения отчета 5.20 " + (Constants.Viewerror ? "\n" + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);

                return new Returns(false, ex.Message);
                ;
            }

            conn_db.Close();

            if (isTableCreated)
            {
                string ix = "ix" + finder.nzp_user.ToString() + "_rep5_20";
                ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_rep5_20 + " (adr) ", false);
            }

            sql = "select count(*) from " + tXX_rep5_20;
            object count = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotalCount;
            try
            {
                recordsTotalCount = Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Ошибка PrepareReport5_20 " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                return new Returns(false,
                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                ;
            }
            conn_web.Close();
            ret.tag = recordsTotalCount;
            return ret;

        }


        /// <summary> Выполняет поиск начислений в основной БД, создает врем. табл. txxCharge и вызывает метод GetCharge
        /// </summary>
        public void FindCalcSz(ChargeFind finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            return;
        } //FindCalcSz



        /// <summary> Получить статистику по начислениям
        /// </summary>
        public void FindChargeStatistics(ChargeFind finder, out Returns ret)
        {
            #region Проверка входных параметров

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }
            if (finder.year_ < 1 || finder.month_ < 1)
            {
                ret = new Returns(false, "Не определен расчетный месяц");
                return;
            }
            if (finder.nzp_dom > 0 && finder.nzp_wp < 1)
            {
                ret = new Returns(false,
                    "Неверные входные параметры префикс:" + finder.pref + ", nzp_wp:" + finder.nzp_wp);
                MonitorLog.WriteLog(
                    "Ошибка FindChargeStatistics : " + "Неверные входные параметры префикс:" + finder.pref + ", nzp_wp:" +
                    finder.nzp_wp, MonitorLog.typelog.Error, 20, 201, true);

                return;
            }

            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            #region Создание таблицы tXXUkRguCharge в кэше

            string tXXUkRguCharge = "t" + Convert.ToString(finder.nzp_user) + "_ukrgucharge";
#if PG
            //conn_web.Database поменять на public
            //string tXXUkRguCharge_full = conn_web.Database + "." + tXXUkRguCharge;
            string tXXUkRguCharge_full = "public." + tXXUkRguCharge;
#else
            string tXXUkRguCharge_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXXUkRguCharge;
#endif

            ExecSQL(conn_web, " Drop table " + tXXUkRguCharge, false);

            bool isTableCreated = false;

            IDataReader reader;
            StringBuilder sql = new StringBuilder();
#if PG
            sql.Append("select * from information_schema.tables where table_name = '" + tXXUkRguCharge +
                       "' and table_schema = CURRENT_SCHEMA()");
#else
            sql.Append("select * from systables where tabname = '" + tXXUkRguCharge + "'");
#endif
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);

            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            if (!reader.Read())
            {
                sql = new StringBuilder();

                sql.Append("CREATE TABLE " + tXXUkRguCharge + "(" +
                           " pref CHAR(20), " +
                           " year_ INTEGER, " +
                           " month_ INTEGER, " +
                           " nzp_area INTEGER NOT NULL, " +
                           " area CHAR(40), " +
                           " nzp_geu INTEGER NOT NULL, " +
                           " geu CHAR(60), " +
                           " nzp_serv INTEGER NOT NULL, " +
                           " ordering INTEGER, " +
                           " service CHAR(100), " +
                           " nzp_supp INTEGER NOT NULL, " +
                           " name_supp CHAR(100), " +
                           " nzp_dom INTEGER NOT NULL, " +
                           " sum_insaldo " + sDecimalType + "(14,2) default 0, " +
                           " sum_insaldo_k " + sDecimalType + "(14,2) default 0, " +
                           " sum_insaldo_d " + sDecimalType + "(14,2) default 0, " +
                           " rsum_tarif " + sDecimalType + "(14,2) default 0, " +
                           " sum_nedop " + sDecimalType + "(14,2) default 0, " +
                           " sum_tarif " + sDecimalType + "(14,2) default 0, " +
                           " real_charge " + sDecimalType + "(14,2) default 0, " +
                           " real_charge_k " + sDecimalType + "(14,2) default 0, " +
                           " real_charge_d " + sDecimalType + "(14,2) default 0, " +
                           " reval " + sDecimalType + "(14,2) default 0, " +
                           " reval_k " + sDecimalType + "(14,2) default 0, " +
                           " reval_d " + sDecimalType + "(14,2) default 0, " +
                           " sum_money " + sDecimalType + "(14,2) default 0, " +
                           " sum_nach " + sDecimalType + "(14,2) default 0, " +
                           " sum_outsaldo " + sDecimalType + "(14,2) default 0, " +
                           " sum_outsaldo_k " + sDecimalType + "(14,2) default 0, " +
                           " sum_outsaldo_d " + sDecimalType + "(14,2) default 0) ");

                ret = ExecSQL(conn_web, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
                else isTableCreated = true;
            }
            else
            {
                sql = new StringBuilder("delete from " + tXXUkRguCharge);
                ret = ExecSQL(conn_web, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }
            reader.Close();

            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

            string supplier = finder.pref + "_kernel" + tableDelimiter + "supplier p";
            string area = finder.pref + "_data" + tableDelimiter + "s_area a";
            string service = finder.pref + "_kernel" + tableDelimiter + "services s";
            string geu = finder.pref + "_data" + tableDelimiter + "s_geu g";
            string chargeTableName = (finder.nzp_dom > 0 ? "fn_ukrgudom" : "fn_ukrgucharge");

#if PG
            string insert = "Insert into public." + tXXUkRguCharge +
#else
            string insert = "Insert into " + tXXUkRguCharge_full +
#endif


 " ( pref, year_, month_, nzp_area, area, nzp_geu, geu, nzp_serv, service,ordering, nzp_supp, " +
                            " name_supp, nzp_dom, sum_insaldo, sum_insaldo_k, sum_insaldo_d, " +
                            " rsum_tarif, sum_nedop, sum_tarif, real_charge, reval, real_charge_k, real_charge_d, reval_k, reval_d, sum_money, " +
                            " sum_nach, sum_outsaldo, sum_outsaldo_k, sum_outsaldo_d)";
            string select = " Select " + Utils.EStrNull(finder.pref) +
                            ", year_, month_{area}{geu}{service}{supplier}{nzp_dom}";
            select += ", sum(sum_insaldo)";
            select += ", sum(sum_insaldo_k)";
            select += ", sum(sum_insaldo_d)";
            select += ", sum(rsum_tarif)";
            select += ", sum(sum_nedop)";
            select += ", sum(sum_tarif)";
            select += ", sum(real_charge)";
            select += ", sum(reval)";
            select += ", sum(real_charge_k)";
            select += ", sum(real_charge_d)";
            select += ", sum(reval_k)";
            select += ", sum(reval_d)";
            select += ", sum(sum_money)";
            select += ", sum(sum_nach)";
            select += ", sum(sum_outsaldo)";
            select += ", sum(sum_outsaldo_k)";
            select += ", sum(sum_outsaldo_d)";
            string from = " From {charge}";
            string where = " Where 1=1";
            string groupBy = " Group by year_, month_";
            string having = " Having sum(sum_insaldo) <> 0 or sum(sum_insaldo_k) <> 0 or sum(sum_insaldo_d) <> 0" +
                            " or sum(rsum_tarif) <> 0 or sum(sum_nedop) <> 0 or sum(sum_tarif) <> 0 or sum(real_charge) <> 0" +
                            " or sum(reval) <> 0 or sum(reval_k) <> 0 or  sum(reval_d) <> 0 or sum(sum_money) <> 0 or sum(sum_nach) <> 0" +
                            " or sum(sum_outsaldo) <> 0 or sum(sum_outsaldo_k) <> 0 or sum(sum_outsaldo_d) <> 0";

            if (finder.RolesVal != null)
                foreach (_RolesVal rv in finder.RolesVal)
                {
                    if (rv.tip == Constants.role_sql && rv.val.Trim() != "")
                    {
                        switch (rv.kod)
                        {
                            case Constants.role_sql_area:
                                where += " and c.nzp_area in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_serv:
                                where += " and c.nzp_serv in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_supp:
                                where += " and c.nzp_supp in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_geu:
                                where += " and c.nzp_geu in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                        }
                    }
                }

            int sequenceNumber;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber))
            {
                select = select.Replace("{area}", ", c.nzp_area, a.area");
#if PG
                from += " left outer join " + area + " ON c.nzp_area = a.nzp_area ";
#else
                from += ", outer " + area;
                where += " and c.nzp_area = a.nzp_area";
#endif
                groupBy += ", c.nzp_area, a.area";
            }
            else select = select.Replace("{area}", ", 0, ''");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber))
            {
                select = select.Replace("{service}", ", c.nzp_serv, s.service,s.ordering");
#if PG
                from += " left outer join " + service + " ON c.nzp_serv = s.nzp_serv ";
#else
                from += ", outer " + service;
                where += " and c.nzp_serv = s.nzp_serv";
#endif
                groupBy += ", c.nzp_serv, s.service, s.ordering";
            }
            else select = select.Replace("{service}", ", 0, '',0");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supplier.ToString(), out sequenceNumber))
            {
                select = select.Replace("{supplier}", ", c.nzp_supp, p.name_supp");
#if PG
                from += " left outer join " + supplier + " ON c.nzp_supp = p.nzp_supp ";
#else
                from += ", outer " + supplier;
                where += " and c.nzp_supp = p.nzp_supp";
#endif
                groupBy += ", c.nzp_supp, p.name_supp";
            }
            else select = select.Replace("{supplier}", ", 0, ''");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_geu.ToString(), out sequenceNumber))
            {
                select = select.Replace("{geu}", ", c.nzp_geu, g.geu");
#if PG
                from += " left outer join " + geu + " ON c.nzp_geu = g.nzp_geu ";
#else
                from += ", outer " + geu;
                where += " and c.nzp_geu = g.nzp_geu";
#endif
                groupBy += ", c.nzp_geu, g.geu";
            }
            else select = select.Replace("{geu}", ", 0, ''");

            if (finder.nzp_dom > 0)
            {
                select = select.Replace("{nzp_dom}", ", " + finder.nzp_dom);
                where += " and c.nzp_dom = " + finder.nzp_dom;
                where += " and c.nzp_wp = " + finder.nzp_wp;
            }
            else
                select = select.Replace("{nzp_dom}", ", 0");

            int m1, m2, y1, y2;
            if (finder.year_po < 1 || finder.month_po < 1)
            {
                m1 = m2 = finder.month_;
                y1 = y2 = finder.year_;
            }
            else
            {
                m1 = finder.month_;
                y1 = finder.year_;
                m2 = finder.month_po;
                y2 = finder.year_po;
            }

            string charge = "";

            try
            {
                for (int y = y1; y <= y2; y++)
                {
                    // проверить наличие таблицы
#if PG
                    ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_fin_" + (y % 100).ToString("00") + "'",
                        true);
#else
                    ret = ExecSQL(conn_db, "database " + finder.pref + "_fin_" + (y % 100).ToString("00"), true);
#endif
                    if (!ret.result) continue;
                    if (!TableInWebCashe(conn_db, chargeTableName)) continue;

                    string filter = " and c.year_ = " + y;
                    if (y == y1 && y1 == y2 && m1 == m2) filter += " and c.month_ = " + m1;
                    else if (y == y1 && y1 == y2 && m1 < m2) filter += " and c.month_ between " + m1 + " and " + m2;
                    else if (y == y1 && y1 < y2) filter += " and c.month_ >= " + m1;
                    else if (y == y2) filter += " and c.month_ <= " + m2;

#if PG
                    charge = finder.pref + "_fin_" + (y % 100).ToString("00") + "." + chargeTableName + " c";
#else
                    charge = finder.pref + "_fin_" + (y % 100).ToString("00") + ":" + chargeTableName + " c";
#endif

                    // сформировать запрос
                    sql =
                        new StringBuilder(insert + select + from.Replace("{charge}", charge) + where + filter + groupBy +
                                          having);

                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) throw new Exception(ret.text);
                }

                if (isTableCreated)
                {
                    string ix = "ix" + finder.nzp_user.ToString() + "_ukrgucharge";

                    string tableName = (tableDelimiter == "." ? tXXUkRguCharge_full : tXXUkRguCharge);

                    ret = ExecSQL(conn_web,
                        " Create index " + ix + "_1 on " + tableName +
                        " (year_, month_, nzp_area, nzp_geu, nzp_serv, nzp_supp) ", false);
                    if (!ret.result) throw new Exception(ret.text);

                    ret = ExecSQL(conn_web, " Create index " + ix + "_2 on " + tableName + " (nzp_dom) ", false);
                    if (!ret.result) throw new Exception(ret.text);
                }

#if PG
                ret = ExecSQL(conn_web, " analyze  " + tXXUkRguCharge_full, true);
#else
                ret = ExecSQL(conn_web, " Update statistics for table  " + tXXUkRguCharge, true);
#endif
                if (!ret.result) throw new Exception(ret.text);

                conn_web.Close();
                conn_db.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка FindChargeStatistics : " + ex.Message, MonitorLog.typelog.Error, 20, 201,
                    true);

                ret.text = ex.Message;
                ret.result = false;
                conn_web.Close();
                conn_db.Close();
            }
        }

        /// <summary> Получить статистику по начислениям
        /// </summary>
        public void FindChargeStatisticsSupp(ChargeFind finder, out Returns ret)
        {
            #region Проверка входных параметров

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }
            if (finder.year_ < 1 || finder.month_ < 1)
            {
                ret = new Returns(false, "Не определен расчетный месяц");
                return;
            }
            if (finder.nzp_dom > 0 && finder.nzp_wp < 1)
            {
                ret = new Returns(false,
                    "Неверные входные параметры префикс:" + finder.pref + ", nzp_wp:" + finder.nzp_wp);
                MonitorLog.WriteLog(
                    "Ошибка FindChargeStatisticsSupp : " + "Неверные входные параметры префикс:" + finder.pref +
                    ", nzp_wp:" + finder.nzp_wp, MonitorLog.typelog.Error, 20, 201, true);

                return;
            }

            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

#if PG
            ret = ExecSQL(conn_web, "set search_path to 'public'", true);
#endif

            #region Создание таблицы tXXUkRguCharge в кэше

            string tXXUkRguCharge = "t" + Convert.ToString(finder.nzp_user) + "_ukrgucharge";
            string tXXUkRguCharge_full = "";
#if PG
            //conn_web.Database поменять на public
            //string tXXUkRguCharge_full = conn_web.Database + "." + tXXUkRguCharge;
            tXXUkRguCharge_full = "public." + tXXUkRguCharge;
#else
            tXXUkRguCharge_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXXUkRguCharge;
#endif

            ret = ExecSQL(conn_web, " Drop table " + tXXUkRguCharge, false);

            bool isTableCreated = false;

            IDataReader reader;
            StringBuilder sql = new StringBuilder();
#if PG
            sql.Append("select * from information_schema.tables where table_name = '" + tXXUkRguCharge +
                       "' and table_schema = CURRENT_SCHEMA()");
#else
            sql.Append("select * from systables where tabname = '" + tXXUkRguCharge + "'");
#endif
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);

            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            if (!reader.Read())
            {
                sql = new StringBuilder();

                sql.Append("CREATE TABLE " + tXXUkRguCharge + "(" +
                           " pref CHAR(20), " +
                           " year_ INTEGER, " +
                           " month_ INTEGER, " +
                           " nzp_area INTEGER NOT NULL, " +
                           " area CHAR(40), " +
                           " nzp_geu INTEGER NOT NULL, " +
                           " geu CHAR(60), " +
                           " nzp_serv INTEGER NOT NULL, " +
                           " ordering INTEGER, " +
                           " service CHAR(100), " +
                           " nzp_supp INTEGER NOT NULL, " +
                           " name_supp CHAR(100), " +

                           " nzp_payer_agent INTEGER , " +
                           " agent CHAR(200), " +
                           " nzp_payer_princip INTEGER, " +
                           " princip CHAR(200), " +
                           " nzp_payer_supp INTEGER , " +
                           " supp CHAR(200), " +

                           " nzp_dom INTEGER NOT NULL, " +
                           " sum_insaldo " + sDecimalType + "(14,2) default 0, " +
                           " sum_insaldo_k " + sDecimalType + "(14,2) default 0, " +
                           " sum_insaldo_d " + sDecimalType + "(14,2) default 0, " +
                           " rsum_tarif " + sDecimalType + "(14,2) default 0, " +
                           " sum_nedop " + sDecimalType + "(14,2) default 0, " +
                           " sum_tarif " + sDecimalType + "(14,2) default 0, " +
                           " real_charge " + sDecimalType + "(14,2) default 0, " +
                           " real_charge_k " + sDecimalType + "(14,2) default 0, " +
                           " real_charge_d " + sDecimalType + "(14,2) default 0, " +
                           " reval " + sDecimalType + "(14,2) default 0, " +
                           " reval_k " + sDecimalType + "(14,2) default 0, " +
                           " reval_d " + sDecimalType + "(14,2) default 0, " +
                           " sum_money " + sDecimalType + "(14,2) default 0, " +
                           " sum_nach " + sDecimalType + "(14,2) default 0, " +
                           " sum_outsaldo " + sDecimalType + "(14,2) default 0, " +
                           " sum_outsaldo_k " + sDecimalType + "(14,2) default 0, " +
                           " sum_outsaldo_d " + sDecimalType + "(14,2) default 0) ");

                ret = ExecSQL(conn_web, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
                else isTableCreated = true;
            }
            else
            {
                sql = new StringBuilder("delete from " + tXXUkRguCharge);
                ret = ExecSQL(conn_web, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }
            reader.Close();

            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);

            ret = OpenDb(conn_db, true);
            DbTables tables = new DbTables(conn_db);
            if (!ret.result) return;
            string s_payer = finder.pref + "_kernel" + tableDelimiter + "s_payer";
            string supplier = finder.pref + "_kernel" + tableDelimiter + "supplier p";
            string area = finder.pref + "_data" + tableDelimiter + "s_area a";
            string service = finder.pref + "_kernel" + tableDelimiter + "services s";
            string geu = finder.pref + "_data" + tableDelimiter + "s_geu g";
            string chargeTableName = (finder.nzp_dom > 0 ? "fn_ukrgudom" : "fn_ukrgucharge");

#if PG
            string insert = "Insert into public." + tXXUkRguCharge +
#else
            string insert = "Insert into " + tXXUkRguCharge_full +
#endif


 " ( pref, year_, month_, nzp_payer_agent, agent, nzp_payer_princip, princip, nzp_payer_supp, supp, nzp_area, area, nzp_geu, geu, nzp_serv, service,ordering, nzp_supp, " +
                            " name_supp, nzp_dom, sum_insaldo, sum_insaldo_k, sum_insaldo_d, " +
                            " rsum_tarif, sum_nedop, sum_tarif, real_charge, reval, real_charge_k, real_charge_d, reval_k, reval_d, sum_money, " +
                            " sum_nach, sum_outsaldo, sum_outsaldo_k, sum_outsaldo_d)";
            string select = " Select " + Utils.EStrNull(finder.pref) +
                            ", year_, month_{agent}{princip}{supp}{area}{geu}{service}{supplier}{nzp_dom}";
            select += ", sum(sum_insaldo)";
            select += ", sum(sum_insaldo_k)";
            select += ", sum(sum_insaldo_d)";
            select += ", sum(rsum_tarif)";
            select += ", sum(sum_nedop)";
            select += ", sum(sum_tarif)";
            select += ", sum(real_charge)";
            select += ", sum(reval)";
            select += ", sum(real_charge_k)";
            select += ", sum(real_charge_d)";
            select += ", sum(reval_k)";
            select += ", sum(reval_d)";
            select += ", sum(sum_money)";
            select += ", sum(sum_nach)";
            select += ", sum(sum_outsaldo)";
            select += ", sum(sum_outsaldo_k)";
            select += ", sum(sum_outsaldo_d)";
            string from = " From " + tables.supplier + " supp, {charge}";
            string where = " Where c.nzp_supp = supp.nzp_supp";
            string groupBy = " Group by year_, month_";
            string having = " Having sum(sum_insaldo) <> 0 or sum(sum_insaldo_k) <> 0 or sum(sum_insaldo_d) <> 0" +
                            " or sum(rsum_tarif) <> 0 or sum(sum_nedop) <> 0 or sum(sum_tarif) <> 0 or sum(real_charge) <> 0" +
                            " or sum(reval) <> 0 or sum(reval_k) <> 0 or  sum(reval_d) <> 0 or sum(sum_money) <> 0 or sum(sum_nach) <> 0" +
                            " or sum(sum_outsaldo) <> 0 or sum(sum_outsaldo_k) <> 0 or sum(sum_outsaldo_d) <> 0";

            if (finder.RolesVal != null)
                foreach (_RolesVal rv in finder.RolesVal)
                {
                    if (rv.tip == Constants.role_sql && rv.val.Trim() != "")
                    {
                        switch (rv.kod)
                        {
                            case Constants.role_sql_area:
                                where += " and c.nzp_area in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_serv:
                                where += " and c.nzp_serv in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_supp:
                                where += " and c.nzp_supp in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_geu:
                                where += " and c.nzp_geu in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                        }
                    }
                }

            if (finder.agent != null && finder.agent != "")
                where += " and supp.nzp_payer_agent in (" + finder.agent + ")";
            if (finder.princip != null && finder.princip != "")
                where += " and supp.nzp_payer_princip in (" + finder.princip + ")";
            if (finder.supp != null && finder.supp != "") where += " and supp.nzp_payer_supp in (" + finder.supp + ")";


            int sequenceNumber;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber))
            {
                select = select.Replace("{area}", ", c.nzp_area, a.area");
#if PG
                from += " left outer join " + area + " ON c.nzp_area = a.nzp_area ";
#else
                from += ", outer " + area;
                where += " and c.nzp_area = a.nzp_area";
#endif
                groupBy += ", c.nzp_area, a.area";
            }
            else select = select.Replace("{area}", ", 0, ''");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_agent.ToString(), out sequenceNumber))
            {
                select = select.Replace("{agent}", ", supp.nzp_payer_agent, ''");
                groupBy += ", supp.nzp_payer_agent";
            }
            else select = select.Replace("{agent}", ", 0 as nzp_payer_agent, '' as agent");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_princip.ToString(), out sequenceNumber))
            {
                select = select.Replace("{princip}", ", supp.nzp_payer_princip, ''");
                groupBy += ", supp.nzp_payer_princip";
            }
            else select = select.Replace("{princip}", ", 0 as nzp_payer_princip, '' as princip");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supp.ToString(), out sequenceNumber))
            {
                select = select.Replace("{supp}", ", supp.nzp_payer_supp, ''");
                groupBy += ", supp.nzp_payer_supp";
            }
            else select = select.Replace("{supp}", ", 0 as nzp_payer_supp, '' as supp");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber))
            {
                select = select.Replace("{service}", ", c.nzp_serv, s.service,s.ordering");
#if PG
                from += " left outer join " + service + " ON c.nzp_serv = s.nzp_serv ";
#else
                from += ", outer " + service;
                where += " and c.nzp_serv = s.nzp_serv";
#endif
                groupBy += ", c.nzp_serv, s.service, s.ordering";
            }
            else select = select.Replace("{service}", ", 0, '',0");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supplier.ToString(), out sequenceNumber))
            {
                select = select.Replace("{supplier}", ", c.nzp_supp, p.name_supp");
#if PG
                from += " left outer join " + supplier + " ON c.nzp_supp = p.nzp_supp ";
#else
                from += ", outer " + supplier;
                where += " and c.nzp_supp = p.nzp_supp";
#endif
                groupBy += ", c.nzp_supp, p.name_supp";
            }
            else select = select.Replace("{supplier}", ", 0, ''");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_geu.ToString(), out sequenceNumber))
            {
                select = select.Replace("{geu}", ", c.nzp_geu, g.geu");
#if PG
                from += " left outer join " + geu + " ON c.nzp_geu = g.nzp_geu ";
#else
                from += ", outer " + geu;
                where += " and c.nzp_geu = g.nzp_geu";
#endif
                groupBy += ", c.nzp_geu, g.geu";
            }
            else select = select.Replace("{geu}", ", 0, ''");

            if (finder.nzp_dom > 0)
            {
                select = select.Replace("{nzp_dom}", ", " + finder.nzp_dom);
                where += " and c.nzp_dom = " + finder.nzp_dom;
                where += " and c.nzp_wp = " + finder.nzp_wp;
            }
            else
                select = select.Replace("{nzp_dom}", ", 0");

            int m1, m2, y1, y2;
            if (finder.year_po < 1 || finder.month_po < 1)
            {
                m1 = m2 = finder.month_;
                y1 = y2 = finder.year_;
            }
            else
            {
                m1 = finder.month_;
                y1 = finder.year_;
                m2 = finder.month_po;
                y2 = finder.year_po;
            }

            string charge = "";

            try
            {
                for (int y = y1; y <= y2; y++)
                {
                    // проверить наличие таблицы
#if PG
                    ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_fin_" + (y % 100).ToString("00") + "'",
                        true);
#else
                    ret = ExecSQL(conn_db, "database " + finder.pref + "_fin_" + (y % 100).ToString("00"), true);
#endif
                    if (!ret.result) continue;
                    if (!TableInWebCashe(conn_db, chargeTableName)) continue;

                    string filter = " and c.year_ = " + y;
                    if (y == y1 && y1 == y2 && m1 == m2) filter += " and c.month_ = " + m1;
                    else if (y == y1 && y1 == y2 && m1 < m2) filter += " and c.month_ between " + m1 + " and " + m2;
                    else if (y == y1 && y1 < y2) filter += " and c.month_ >= " + m1;
                    else if (y == y2) filter += " and c.month_ <= " + m2;

#if PG
                    charge = finder.pref + "_fin_" + (y % 100).ToString("00") + "." + chargeTableName + " c";
#else
                    charge = finder.pref + "_fin_" + (y % 100).ToString("00") + ":" + chargeTableName + " c";
#endif

                    // сформировать запрос
                    sql =
                        new StringBuilder(insert + select + from.Replace("{charge}", charge) + where + filter + groupBy +
                                          having);

                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) throw new Exception(ret.text);
                }

                if (Utils.GetParams(finder.groupby, Constants.act_groupby_agent.ToString(), out sequenceNumber))
                {
                    ret = ExecSQL(conn_db,
                        "update " + tXXUkRguCharge_full + " set agent = (select payer from " + tables.payer +
                        " where nzp_payer = " + tXXUkRguCharge_full + ".nzp_payer_agent)", true);
                    if (!ret.result) return;
                }

                if (Utils.GetParams(finder.groupby, Constants.act_groupby_princip.ToString(), out sequenceNumber))
                {
                    ret = ExecSQL(conn_db,
                        "update " + tXXUkRguCharge_full + " set princip = (select payer from " + tables.payer +
                        " where nzp_payer = " + tXXUkRguCharge_full + ".nzp_payer_princip)", true);
                    if (!ret.result) return;
                }

                if (Utils.GetParams(finder.groupby, Constants.act_groupby_supp.ToString(), out sequenceNumber))
                {
                    ret = ExecSQL(conn_db,
                        "update " + tXXUkRguCharge_full + " set supp = (select payer from " + tables.payer +
                        " where nzp_payer = " + tXXUkRguCharge_full + ".nzp_payer_supp)", true);
                    if (!ret.result) return;
                }

                if (isTableCreated)
                {
                    string ix = "ix" + finder.nzp_user.ToString() + "_ukrgucharge_temp";

                    string tableName = (tableDelimiter == "." ? tXXUkRguCharge_full : tXXUkRguCharge);

                    ret = ExecSQL(conn_web,
                        " Create index " + ix + "_1 on " + tableName +
                        " (year_, month_, nzp_area, nzp_geu, nzp_serv, nzp_supp) ", false);
                    if (!ret.result) throw new Exception(ret.text);

                    ret = ExecSQL(conn_web, " Create index " + ix + "_2 on " + tableName + " (nzp_dom) ", false);
                    if (!ret.result) throw new Exception(ret.text);
                }

#if PG
                ret = ExecSQL(conn_web, " analyze  " + tXXUkRguCharge_full, true);
#else
                ret = ExecSQL(conn_web, " Update statistics for table  " + tXXUkRguCharge, true);
#endif
                if (!ret.result) throw new Exception(ret.text);

                conn_web.Close();
                conn_db.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка FindChargeStatistics : " + ex.Message, MonitorLog.typelog.Error, 20, 201,
                    true);

                ret.text = ex.Message;
                ret.result = false;
                conn_web.Close();
                conn_db.Close();
            }
        }

        public void FindMoneyDistrib(MoneyDistrib finder, out Returns ret)
        {
            #region Проверка входных параметров

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }
            if (finder.dat_oper == "")
            {
                ret = new Returns(false, "Не определен период платежей");
                return;
            }

            if (Utils.GetParams(finder.prms, Constants.page_payer_transfer))
            {
                FindPayerTransfer(finder, out ret);
                return;
            }

            DateTime datOper = DateTime.MinValue;
            DateTime datOperPo = DateTime.MinValue;

            if (!DateTime.TryParse(finder.dat_oper, out datOper))
            {
                ret = new Returns(false, "Неверный формат даты начала платежей");
                return;
            }

            if (finder.dat_oper_po != "")
            {
                if (!DateTime.TryParse(finder.dat_oper_po, out datOperPo))
                {
                    ret = new Returns(false, "Неверный формат даты окончания платежей");
                    return;
                }
            }
            else datOperPo = datOper;

            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            #region Создание таблицы tXXDistrib в кэше

            string tXXDistrib = "t" + Convert.ToString(finder.nzp_user) + "_distrib";

            bool isTableCreated = CreateTXXDistrib(conn_web, tXXDistrib, out ret);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            #endregion

#if PG
            string tXXDistrib_full = /*conn_web.Database + "." +*/ "public." + tXXDistrib;
#else
            string tXXDistrib_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXXDistrib;
#endif

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

#if PG
            string payer = finder.pref + "_kernel.s_payer p";
            string area = finder.pref + "_data.s_area a";
            string service = finder.pref + "_kernel.services s";
            //string bank = finder.pref + "_kernel.s_bank b";
            string bank = finder.pref + "_kernel.s_payer b";
#else
            string payer = finder.pref + "_kernel:s_payer p";
            string area = finder.pref + "_data:s_area a";
            string service = finder.pref + "_kernel:services s";
            //string bank = finder.pref + "_kernel:s_bank b";
            string bank = finder.pref + "_kernel:s_payer b";
#endif

            // составные части запроса
            string insert = "Insert into " + tXXDistrib_full +
                            " (nzp_dis, pref, year_, month_, nzp_payer, payer, nzp_area, area, nzp_serv, service, nzp_bank, bank, dat_oper, dat_oper_po, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out)";
            string select = " Select 0, " + Utils.EStrNull(finder.pref) +
                            "{year}{month}{payer}{area}{service}{bank}{dat_oper}";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString()))
                select += ", sum(sum_in) as sum_in";
            else
                select += ", sum(case when dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()) +
                          " then sum_in else 0 end) as sum_in";

            select += ", sum(sum_rasp) as sum_rasp";
            select += ", sum(sum_ud) as sum_ud";
            select += ", sum(sum_naud) as sum_naud";
            select += ", sum(sum_reval) as sum_reval";
            select += ", sum(sum_charge) as sum_charge";
            select += ", sum(sum_send) as sum_send";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString()))
                select += ", sum(sum_out) as sum_out";
            else
                select += ", sum(case when dat_oper = " + Utils.EStrNull(datOperPo.ToShortDateString()) +
                          " then sum_out else 0 end) as sum_out";

            string from = " From {distrib}";
            //string where = " Where d.nzp_serv not in (2,21,22)"; // эти три услуги (2, 21, 22) заменяет услуга 98
            string where = " Where 1=1";
            string groupBy = " Group by 1";
            string having =
                " Having sum(sum_in) <> 0 or sum(sum_rasp) <> 0 or sum(sum_ud) <> 0 or sum(sum_naud) <> 0 or sum(sum_reval) <> 0 or sum(sum_charge) <> 0 or sum(sum_send) <> 0 or sum(sum_out) <> 0";

            if (finder.RolesVal != null)
                foreach (_RolesVal rv in finder.RolesVal)
                {
                    if (rv.tip == Constants.role_sql && rv.val.Trim() != "")
                    {
                        switch (rv.kod)
                        {
                            case Constants.role_sql_area:
                                where += " and d.nzp_area in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_serv:
                                where += " and d.nzp_serv in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_payer:
                                where += " and d.nzp_payer in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_bank:
                                where += " and d.nzp_bank in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_supp:
                                where += " and exists (select * from " + payer +
                                         "1 where p1.nzp_payer = d.nzp_payer and p1.nzp_supp in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + "))";
                                break;
                        }
                    }
                }

            int sequenceNumber;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString(), out sequenceNumber))
            {
                select = select.Replace("{dat_oper}", ", dat_oper, dat_oper");
                groupBy += ", dat_oper";
            }
            else
                select = select.Replace("{dat_oper}",
                    ", " + Utils.EStrNull(datOper.ToShortDateString()) + ", " +
                    Utils.EStrNull(datOperPo.ToShortDateString()));

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber))
            {
                select = select.Replace("{area}", ", d.nzp_area, a.area");
#if PG
                from += " left outer join " + area + " ON d.nzp_area = a.nzp_area ";
#else
                from += ", outer " + area;
                where += " and d.nzp_area = a.nzp_area";
#endif
                groupBy += ", d.nzp_area, a.area";
            }
            else select = select.Replace("{area}", ", 0 as nzp_area, '' as area");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber))
            {
                select = select.Replace("{service}", ", d.nzp_serv, s.service");
#if PG
                from += " left outer join " + service + " ON d.nzp_serv = s.nzp_serv ";
#else
                from += ", outer " + service;
                where += " and d.nzp_serv = s.nzp_serv";
#endif
                groupBy += ", d.nzp_serv, s.service";
            }
            else select = select.Replace("{service}", ", 0 as nzp_serv, '' as service");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_payer.ToString(), out sequenceNumber))
            {
                select = select.Replace("{payer}", ", d.nzp_payer, p.payer");
#if PG
                from += " left outer join " + payer + " ON d.nzp_payer = p.nzp_payer ";
#else
                from += ", outer " + payer;
                where += " and d.nzp_payer = p.nzp_payer";
#endif
                groupBy += ", d.nzp_payer, p.payer";
            }
            else select = select.Replace("{payer}", ", 0 as nzp_payer, '' as payer");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_bank.ToString(), out sequenceNumber))
            {
                select = select.Replace("{bank}", ", d.nzp_bank, b.payer as bank");
#if PG
                from += " left outer join " + bank + " ON d.nzp_bank = b.nzp_payer ";
#else
                from += ", outer " + bank;
                where += " and d.nzp_bank = b.nzp_payer";
#endif
                groupBy += ", d.nzp_bank, b.payer";
            }
            else select = select.Replace("{bank}", ", 0 as nzp_bank, '' as bank");

            /*groupBy = groupBy.Replace("{group_by} ,", "Group by ")
                .Replace("{group_by}", "");*/

            int m1, m2, y1, y2;
            if (datOperPo == DateTime.MinValue)
            {
                m1 = m2 = datOper.Month;
                y1 = y2 = datOper.Year;
                where += " and d.dat_oper = " + Utils.EStrNull(datOper.ToShortDateString());
                datOperPo = datOper;
            }
            else
            {
                m1 = datOper.Month;
                y1 = datOper.Year;
                m2 = datOperPo.Month;
                y2 = datOperPo.Year;
                where += " and d.dat_oper >= " + Utils.EStrNull(datOper.ToShortDateString()) +
                         " and d.dat_oper <= " + Utils.EStrNull(datOperPo.ToShortDateString());
            }

            string distrib = "";
            StringBuilder sql;

            for (int y = y1; y <= y2; y++)
                for (int m = 1; m <= 12; m++)
                {
                    if (y == y1 && m < m1) continue;
                    if (y == y2 && m > m2) continue;

                    // проверить наличие таблицы
#if PG
                    ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_fin_" + (y % 100).ToString("00") + "'",
                        false);
#else
                    ret = ExecSQL(conn_db, "database " + finder.pref + "_fin_" + (y % 100).ToString("00"), false);
#endif
                    if (!ret.result) continue;
                    if (!TableInWebCashe(conn_db, "fn_distrib_" + m.ToString("00"))) continue;

#if PG
                    distrib = finder.pref + "_fin_" + (y % 100).ToString("00") + ".fn_distrib_" + m.ToString("00") + " d";
#else
                    distrib = finder.pref + "_fin_" + (y % 100).ToString("00") + ":fn_distrib_" + m.ToString("00") + " d";
#endif
                    select = select.Replace("{year}", ", " + y.ToString()).Replace("{month}", ", " + m.ToString());

                    // сформировать запрос
                    sql =
                        new StringBuilder(insert + select + from.Replace("{distrib}", distrib) + where + groupBy +
                                          having);

                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return;
                    }
                }

            if (isTableCreated)
            {
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
                string ix = "ix" + finder.nzp_user.ToString() + "_distrib";
                ret = ExecSQL(conn_web, " Create unique index " + ix + "_1 on " + tXXDistrib + " (nzp_dis) ", false);
                ret = ExecSQL(conn_web,
                    " Create index " + ix + "_2 on " + tXXDistrib + " (nzp_payer, nzp_serv, nzp_area, nzp_bank) ", false);
            }

#if PG
            if (ret.result) ret = ExecSQL(conn_web, " analyze  " + tXXDistrib, true);
#else
            if (ret.result) ret = ExecSQL(conn_web, " Update statistics for table  " + tXXDistrib, true);
#endif

            conn_web.Close();
            conn_db.Close();
            return;
        }

        public void FindMoneyDistribDom(MoneyDistrib finder, out Returns ret)
        {
            #region Проверка входных параметров

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }
            if (finder.dat_oper == "")
            {
                ret = new Returns(false, "Не определен период платежей");
                return;
            }

            if (Utils.GetParams(finder.prms, Constants.page_payer_transfer))
            {
                FindPayerTransfer(finder, out ret);
                return;
            }

            DateTime datOper = DateTime.MinValue;
            DateTime datOperPo = DateTime.MinValue;

            if (!DateTime.TryParse(finder.dat_oper, out datOper))
            {
                ret = new Returns(false, "Неверный формат даты начала платежей");
                return;
            }

            if (finder.dat_oper_po != "")
            {
                if (!DateTime.TryParse(finder.dat_oper_po, out datOperPo))
                {
                    ret = new Returns(false, "Неверный формат даты окончания платежей");
                    return;
                }
            }
            else datOperPo = datOper;

            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            #region Создание таблицы tXXDistrib в кэше

            string tXXDistribDom = "t" + Convert.ToString(finder.nzp_user) + "_distrib_dom";

            bool isTableCreated = CreateTXXDistribDom(conn_web, tXXDistribDom, out ret);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            #endregion

#if PG
            string tXXDistribDom_full = /*conn_web.Database + "." +*/ "public." + tXXDistribDom;
#else
            string tXXDistribDom_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXXDistribDom;
#endif

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

#if PG
            string payer = finder.pref + "_kernel.s_payer p";
            string area = finder.pref + "_data.s_area a";
            string service = finder.pref + "_kernel.services s";
            //string bank = finder.pref + "_kernel.s_bank b";
            string bank = finder.pref + "_kernel.s_payer b";
            string dom = finder.pref + "_data.dom dm";
#else
            string payer = finder.pref + "_kernel:s_payer p";
            string area = finder.pref + "_data:s_area a";
            string service = finder.pref + "_kernel:services s";
            //string bank = finder.pref + "_kernel:s_bank b";
            string bank = finder.pref + "_kernel:s_payer b";
            string dom = finder.pref + "_data:dom dm";
#endif
            DbTables tables = new DbTables(conn_db);

            // составные части запроса
#if PG
            string insert = "Insert into " + tXXDistribDom_full +
                            " (pref, year_, month_, nzp_payer, payer, nzp_area, area, nzp_serv, service, nzp_dom, adr, nzp_bank, bank, dat_oper, dat_oper_po, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out)";
            string select = " Select "
#else
            string insert = "Insert into " + tXXDistribDom_full +
                " (nzp_dis, pref, year_, month_, nzp_payer, payer, nzp_area, area, nzp_serv, service, nzp_dom, adr, nzp_bank, bank, dat_oper, dat_oper_po, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out)";
            string select = " Select 0, "
#endif

 + Utils.EStrNull(finder.pref) + "{year}{month}{payer}{area}{service}{dom}{bank}{dat_oper}";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString()))
                select += ", sum(sum_in) as sum_in";
            else
                select += ", sum(case when dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()) +
                          " then sum_in else 0 end) as sum_in";

            select += ", sum(sum_rasp) as sum_rasp";
            select += ", sum(sum_ud) as sum_ud";
            select += ", sum(sum_naud) as sum_naud";
            select += ", sum(sum_reval) as sum_reval";
            select += ", sum(sum_charge) as sum_charge";
            select += ", sum(sum_send) as sum_send";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString()))
                select += ", sum(sum_out) as sum_out";
            else
                select += ", sum(case when dat_oper = " + Utils.EStrNull(datOperPo.ToShortDateString()) +
                          " then sum_out else 0 end) as sum_out";

            string from = " From {distrib}";
            //string where = " Where d.nzp_serv not in (2,21,22)"; // эти три услуги (2, 21, 22) заменяет услуга 98
            string where = " Where 1=1";
            string groupBy = " Group by 1";
            string having =
                " Having sum(sum_in) <> 0 or sum(sum_rasp) <> 0 or sum(sum_ud) <> 0 or sum(sum_naud) <> 0 or sum(sum_reval) <> 0 or sum(sum_charge) <> 0 or sum(sum_send) <> 0 or sum(sum_out) <> 0";

            if (finder.RolesVal != null)
                foreach (_RolesVal rv in finder.RolesVal)
                {
                    if (rv.tip == Constants.role_sql && rv.val.Trim() != "")
                    {
                        switch (rv.kod)
                        {
                            case Constants.role_sql_area:
                                where += " and d.nzp_area in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_serv:
                                where += " and d.nzp_serv in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_payer:
                                where += " and d.nzp_payer in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_bank:
                                if (finder.bank_not_choosen == 0)
                                    where += " and d.nzp_bank in (" +
                                             (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_supp:
                                where += " and exists (select * from " + payer +
                                         "1 where p1.nzp_payer = d.nzp_payer and p1.nzp_supp in (" +
                                         (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + "))";
                                break;
                        }
                    }
                }

            if (finder.bank_not_choosen == 1) where += " and (d.nzp_bank is null or d.nzp_bank = -1) ";

            int sequenceNumber;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString(), out sequenceNumber))
            {
                select = select.Replace("{dat_oper}", ", dat_oper, dat_oper");
                groupBy += ", dat_oper";
            }
            else
                select = select.Replace("{dat_oper}",
                    ", " + Utils.EStrNull(datOper.ToShortDateString()) + ", " +
                    Utils.EStrNull(datOperPo.ToShortDateString()));

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber))
            {
                select = select.Replace("{area}", ", d.nzp_area, a.area");
#if PG
                from += " left outer join " + area + " ON d.nzp_area = a.nzp_area ";
#else
                from += ", outer " + area;
                where += " and d.nzp_area = a.nzp_area";
#endif
                groupBy += ", d.nzp_area, a.area";
            }
            else select = select.Replace("{area}", ", 0 as nzp_area, '' as area");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber))
            {
                select = select.Replace("{service}", ", d.nzp_serv, s.service");
#if PG
                from += " left outer join " + service + " ON d.nzp_serv = s.nzp_serv ";
#else
                from += ", outer " + service;
                where += " and d.nzp_serv = s.nzp_serv";
#endif
                groupBy += ", d.nzp_serv, s.service";
            }
            else select = select.Replace("{service}", ", 0 as nzp_serv, '' as service");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_dom.ToString(), out sequenceNumber))
            {
                select = select.Replace("{dom}", ", d.nzp_dom, 'ул.'|| u.ulica || ', д.' || dm.ndom");
#if PG
                from += " left outer join " + dom +
                        " left outer join " + tables.ulica + " u  on u.nzp_ul = dm.nzp_ul " +
                        " ON d.nzp_dom = dm.nzp_dom ";
#else
                from += ", outer (" + dom + ", " + tables.ulica + " u) ";
                where += " and d.nzp_dom = dm.nzp_dom and dm.nzp_ul=u.nzp_ul ";
#endif
                if (finder.ndom != "") where += " and d.nzp_dom in (" + finder.ndom + ") ";
                groupBy += ", d.nzp_dom, 12, u.ulica, dm.ndom";
            }
            else select = select.Replace("{dom}", ", 0 as nzp_dom, '' as adr");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_payer.ToString(), out sequenceNumber))
            {
                select = select.Replace("{payer}", ", d.nzp_payer, p.payer");
#if PG
                from += " left outer join " + payer + " ON d.nzp_payer = p.nzp_payer ";
#else
                from += ", outer " + payer;
                where += " and d.nzp_payer = p.nzp_payer";
#endif
                groupBy += ", d.nzp_payer, p.payer";
            }
            else select = select.Replace("{payer}", ", 0 as nzp_payer, '' as payer");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_bank.ToString(), out sequenceNumber))
            {
                select = select.Replace("{bank}", ", d.nzp_bank, b.payer as bank");
#if PG
                from += " left outer join " + bank + " ON d.nzp_bank = b.nzp_payer ";
#else
                from += ", outer " + bank;
                where += " and d.nzp_bank = b.nzp_payer";
#endif
                groupBy += ", d.nzp_bank, b.payer";
            }
            else select = select.Replace("{bank}", ", 0 as nzp_bank, '' as bank");

            /*groupBy = groupBy.Replace("{group_by} ,", "Group by ")
                .Replace("{group_by}", "");*/

            int m1, m2, y1, y2;
            if (datOperPo == DateTime.MinValue)
            {
                m1 = m2 = datOper.Month;
                y1 = y2 = datOper.Year;
                where += " and d.dat_oper = " + Utils.EStrNull(datOper.ToShortDateString());
                datOperPo = datOper;
            }
            else
            {
                m1 = datOper.Month;
                y1 = datOper.Year;
                m2 = datOperPo.Month;
                y2 = datOperPo.Year;
                where += " and d.dat_oper >= " + Utils.EStrNull(datOper.ToShortDateString()) +
                         " and d.dat_oper <= " + Utils.EStrNull(datOperPo.ToShortDateString());
            }

            string distrib = "";
            StringBuilder sql;

            for (int y = y1; y <= y2; y++)
                for (int m = 1; m <= 12; m++)
                {
                    if (y == y1 && m < m1) continue;
                    if (y == y2 && m > m2) continue;

                    // проверить наличие таблицы
#if PG
                    ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_fin_" + (y % 100).ToString("00") + "'",
                        false);
#else
                    ret = ExecSQL(conn_db, "database " + finder.pref + "_fin_" + (y % 100).ToString("00"), false);
#endif
                    if (!ret.result) continue;
                    if (!TableInWebCashe(conn_db, "fn_distrib_dom_" + m.ToString("00"))) continue;

#if PG
                    distrib = finder.pref + "_fin_" + (y % 100).ToString("00") + ".fn_distrib_dom_" + m.ToString("00") +
                              " d";
#else
                    distrib = finder.pref + "_fin_" + (y % 100).ToString("00") + ":fn_distrib_dom_" + m.ToString("00") + " d";
#endif
                    select = select.Replace("{year}", ", " + y.ToString()).Replace("{month}", ", " + m.ToString());

                    // сформировать запрос
                    sql =
                        new StringBuilder(insert + select + from.Replace("{distrib}", distrib) + where + groupBy +
                                          having);

                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return;
                    }
                }

            if (isTableCreated)
            {
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
                string ix = "ix" + finder.nzp_user.ToString() + "_distrib_dom";
                ret = ExecSQL(conn_web, " Create unique index " + ix + "_1 on " + tXXDistribDom_full + " (nzp_dis) ",
                    false);
                ret = ExecSQL(conn_web,
                    " Create index " + ix + "_2 on " + tXXDistribDom_full +
                    " (nzp_payer, nzp_serv, nzp_area, nzp_bank,nzp_dom) ", false);
            }

#if PG
            if (ret.result) ret = ExecSQL(conn_web, " analyze  " + tXXDistribDom_full, true);
#else
            if (ret.result) ret = ExecSQL(conn_web, " Update statistics for table  " + tXXDistribDom, true);
#endif

            conn_web.Close();
            conn_db.Close();
            return;
        }

        private bool CreateTXXDistrib(IDbConnection conn_web, string tXXDistrib, out Returns ret)
        {
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
            ExecSQL(conn_web, " Drop table " + tXXDistrib, false);

            IDataReader reader;
#if PG
            string sql = "select * from information_schema.tables where table_name = '" + tXXDistrib +
                         "' and table_schema = CURRENT_SCHEMA() ";
#else
            string sql = "select * from systables where tabname = '" + tXXDistrib + "'";
#endif
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                return false;
            }
            bool result;
            if (!reader.Read())
            {
#if PG
                sql = "CREATE TABLE " + tXXDistrib + "(" +
                      " nzp_dis SERIAL NOT NULL, " +
                      " pref CHAR(20), " +
                      " year_ INTEGER, " +
                      " month_ INTEGER, " +
                      " nzp_payer INTEGER NOT NULL, " +
                      " payer CHAR(200), " +
                      " nzp_area INTEGER NOT NULL, " +
                      " area CHAR(40), " +
                      " nzp_serv INTEGER NOT NULL, " +
                      " service CHAR(100), " +
                      " dat_oper DATE, " +
                      " dat_oper_po DATE, " +
                      " sum_in NUMERIC(14,2) default 0, " +
                      " sum_rasp NUMERIC(14,2) default 0, " +
                      " sum_ud NUMERIC(14,2) default 0, " +
                      " sum_naud NUMERIC(14,2) default 0, " +
                      " sum_reval NUMERIC(14,2) default 0, " +
                      " sum_charge NUMERIC(14,2) default 0, " +
                      " sum_send NUMERIC(14,2) default 0, " +
                      " sum_out NUMERIC(14,2) default 0, " +
                      " nzp_bank INTEGER default -1, " +
                      " bank CHAR(200) ) ";
#else
                sql = "CREATE TABLE " + tXXDistrib + "(" +
                    " nzp_dis SERIAL NOT NULL, " +
                    " pref CHAR(20), " +
                    " year_ INTEGER, " +
                    " month_ INTEGER, " +
                    " nzp_payer INTEGER NOT NULL, " +
                    " payer CHAR(200), " +
                    " nzp_area INTEGER NOT NULL, " +
                    " area CHAR(40), " +
                    " nzp_serv INTEGER NOT NULL, " +
                    " service CHAR(100), " +
                    " dat_oper DATE, " +
                    " dat_oper_po DATE, " +
                    " sum_in DECIMAL(14,2) default 0, " +
                    " sum_rasp DECIMAL(14,2) default 0, " +
                    " sum_ud DECIMAL(14,2) default 0, " +
                    " sum_naud DECIMAL(14,2) default 0, " +
                    " sum_reval DECIMAL(14,2) default 0, " +
                    " sum_charge DECIMAL(14,2) default 0, " +
                    " sum_send DECIMAL(14,2) default 0, " +
                    " sum_out DECIMAL(14,2) default 0, " +
                    " nzp_bank INTEGER default -1, " +
                    " bank CHAR(200) ) ";
#endif

                ret = ExecSQL(conn_web, sql, false);
                result = ret.result;
            }
            else
            {
                result = false;
                sql = "delete from " + tXXDistrib;
                ret = ExecSQL(conn_web, sql, true);
            }
            reader.Close();
            return result;
        }

        private bool CreateTXXDistribDom(IDbConnection conn_web, string tXXDistribDom, out Returns ret)
        {
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
            ExecSQL(conn_web, " Drop table " + tXXDistribDom, false);

            IDataReader reader;
#if PG
            string sql = "select * from information_schema.tables where table_name = '" + tXXDistribDom +
                         "' and table_schema = CURRENT_SCHEMA() ";
#else
            string sql = "select * from systables where tabname = '" + tXXDistribDom + "'";
#endif
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                return false;
            }
            bool result;
            if (!reader.Read())
            {
#if PG
                sql = "CREATE TABLE " + tXXDistribDom + "(" +
                      " nzp_dis SERIAL NOT NULL, " +
                      " pref CHAR(20), " +
                      " year_ INTEGER, " +
                      " month_ INTEGER, " +
                      " nzp_payer INTEGER NOT NULL, " +
                      " payer CHAR(200), " +
                      " nzp_area INTEGER NOT NULL, " +
                      " area CHAR(40), " +
                      " nzp_serv INTEGER NOT NULL, " +
                      " nzp_dom INTEGER NOT NULL, " +
                      " adr CHAR(250), " +
                      " service CHAR(100), " +
                      " dat_oper DATE, " +
                      " dat_oper_po DATE, " +
                      " sum_in NUMERIC(14,2) default 0, " +
                      " sum_rasp NUMERIC(14,2) default 0, " +
                      " sum_ud NUMERIC(14,2) default 0, " +
                      " sum_naud NUMERIC(14,2) default 0, " +
                      " sum_reval NUMERIC(14,2) default 0, " +
                      " sum_charge NUMERIC(14,2) default 0, " +
                      " sum_send NUMERIC(14,2) default 0, " +
                      " sum_out NUMERIC(14,2) default 0, " +
                      " nzp_bank INTEGER default -1, " +
                      " bank CHAR(200) ) ";
#else
                sql = "CREATE TABLE " + tXXDistribDom + "(" +
                    " nzp_dis SERIAL NOT NULL, " +
                    " pref CHAR(20), " +
                    " year_ INTEGER, " +
                    " month_ INTEGER, " +
                    " nzp_payer INTEGER NOT NULL, " +
                    " payer CHAR(200), " +
                    " nzp_area INTEGER NOT NULL, " +
                    " area CHAR(40), " +
                    " nzp_serv INTEGER NOT NULL, " +
                    " nzp_dom INTEGER NOT NULL, " +
                    " adr CHAR(250), " +
                    " service CHAR(100), " +
                    " dat_oper DATE, " +
                    " dat_oper_po DATE, " +
                    " sum_in DECIMAL(14,2) default 0, " +
                    " sum_rasp DECIMAL(14,2) default 0, " +
                    " sum_ud DECIMAL(14,2) default 0, " +
                    " sum_naud DECIMAL(14,2) default 0, " +
                    " sum_reval DECIMAL(14,2) default 0, " +
                    " sum_charge DECIMAL(14,2) default 0, " +
                    " sum_send DECIMAL(14,2) default 0, " +
                    " sum_out DECIMAL(14,2) default 0, " +
                    " nzp_bank INTEGER default -1, " +
                    " bank CHAR(200) ) ";
#endif

                ret = ExecSQL(conn_web, sql, false);
                result = ret.result;
            }
            else
            {
                result = false;
                sql = "delete from " + tXXDistribDom;
                ret = ExecSQL(conn_web, sql, true);
            }
            reader.Close();
            return result;
        }

        private void FindPayerTransfer(MoneyDistrib finder, out Returns ret)
        {
            #region Проверка входных параметров

            DateTime datOper = DateTime.MinValue;

            if (!DateTime.TryParse(finder.dat_oper, out datOper))
            {
                ret = new Returns(false, "Неверный формат даты начала платежей");
                return;
            }

            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            string tXXDistrib = "t" + Convert.ToString(finder.nzp_user) + "_distrib";

            bool isTableCreated = CreateTXXDistrib(conn_web, tXXDistrib, out ret);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

#if PG
            string tXXDistrib_full = /*conn_web.Database +*/ "public." + tXXDistrib;
#else
            string tXXDistrib_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXXDistrib;
#endif

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

#if PG
            string distrib = finder.pref + "_fin_" + (datOper.Year % 100).ToString("00") + ".fn_distrib_" +
                             datOper.Month.ToString("00");
#else
            string distrib = finder.pref + "_fin_" + (datOper.Year % 100).ToString("00") + ":fn_distrib_" + datOper.Month.ToString("00");
#endif

            if (!TempTableInWebCashe(conn_db, distrib))
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

#if PG
            string payer = finder.pref + "_kernel.s_payer p";
            string area = finder.pref + "_data.s_area a";
            string service = finder.pref + "_kernel.services s";
            string bank = finder.pref + "_kernel.s_payer b";
#else
            string payer = finder.pref + "_kernel:s_payer p";
            string area = finder.pref + "_data:s_area a";
            string service = finder.pref + "_kernel:services s";
            string bank = finder.pref + "_kernel:s_payer b";
#endif

            // составные части запроса
#if PG
            string sql = "Insert into " + tXXDistrib_full +
                         " (nzp_dis, pref, year_, month_, nzp_area, area, nzp_payer, payer, nzp_serv, service, nzp_bank, bank, dat_oper, dat_oper_po, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out)" +
                         " Select nzp_dis, " + Utils.EStrNull(finder.pref) + "," + datOper.Year + "," + datOper.Month +
                         ", d.nzp_area, a.area, d.nzp_payer, p.payer, d.nzp_serv, s.service, d.nzp_bank, b.payer as bank, d.dat_oper, d.dat_oper, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out" +
                         " From " + distrib + " d " +
                         " left outer join " + area + " on d.nzp_area = a.nzp_area " +
                         " left outer join " + payer + " on d.nzp_payer = p.nzp_payer " +
                         " left outer join " + service + " on d.nzp_serv = s.nzp_serv " +
                         " left outer join " + bank + " on d.nzp_bank = b.nzp_payer " +
                         " Where d.nzp_serv not in (2,21,22)" + // эти три услуги (2, 21, 22) заменяет услуга 98
                         " and d.dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()) +
                         (finder.nzp_area > 0 ? " and d.nzp_area = " + finder.nzp_area : "") +
                         (finder.nzp_payer > 0 ? " and d.nzp_payer = " + finder.nzp_payer : "");
#else
            string sql = "Insert into " + tXXDistrib_full +
                " (nzp_dis, pref, year_, month_, nzp_area, area, nzp_payer, payer, nzp_serv, service, nzp_bank, bank, dat_oper, dat_oper_po, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out)" +
                " Select nzp_dis, " + Utils.EStrNull(finder.pref) + "," + datOper.Year + "," + datOper.Month +
                ", d.nzp_area, a.area, d.nzp_payer, p.payer, d.nzp_serv, s.service, d.nzp_bank, b.payer as bank, d.dat_oper, d.dat_oper, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out" +
            " From " + distrib + " d, outer " + area + ", outer " + payer + ", outer " + service + ", outer " + bank +
            " Where d.nzp_serv not in (2,21,22)" + // эти три услуги (2, 21, 22) заменяет услуга 98
                " and d.dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()) +
                " and d.nzp_area = a.nzp_area" +
                " and d.nzp_serv = s.nzp_serv" +
                " and d.nzp_payer = p.nzp_payer" +
                " and d.nzp_bank = b.nzp_payer" +
                (finder.nzp_area > 0 ? " and d.nzp_area = " + finder.nzp_area : "") +
                (finder.nzp_payer > 0 ? " and d.nzp_payer = " + finder.nzp_payer : "");
#endif

            if (finder.RolesVal != null)
                foreach (_RolesVal rv in finder.RolesVal)
                {
                    if (rv.tip == Constants.role_sql && rv.val.Trim() != "")
                    {
                        switch (rv.kod)
                        {
                            case Constants.role_sql_area:
                                sql += " and d.nzp_area in (" +
                                       (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_serv:
                                sql += " and d.nzp_serv in (" +
                                       (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_payer:
                                sql += " and d.nzp_payer in (" +
                                       (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_bank:
                                sql += " and d.nzp_bank in (" +
                                       (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_supp:
                                sql += " and exists (select * from " + payer +
                                       "1 where p1.nzp_payer = d.nzp_payer and p1.nzp_supp in (" +
                                       (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + "))";
                                break;
                        }
                    }
                }

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            if (isTableCreated)
            {
#if PG
                ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
                string ix = "ix" + finder.nzp_user.ToString() + "_distrib";
                ret = ExecSQL(conn_web, " Create unique index " + ix + "_1 on " + tXXDistrib + " (nzp_dis) ", false);
                ret = ExecSQL(conn_web,
                    " Create index " + ix + "_2 on " + tXXDistrib + " (nzp_payer, nzp_serv, nzp_area, nzp_bank) ", false);
            }

#if PG
            if (ret.result) ret = ExecSQL(conn_web, " analyze  " + tXXDistrib, true);
#else
            if (ret.result) ret = ExecSQL(conn_web, " Update statistics for table  " + tXXDistrib, true);
#endif

            conn_web.Close();
            conn_db.Close();
            return;
        }

        private List<MoneyDistrib> GetPayerTransfer(MoneyDistrib finder, out Returns ret)
        {
            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXXDistrib = "t" + Convert.ToString(finder.nzp_user) + "_distrib";

            // составные части запроса
            string sql =
                "Select nzp_area, area, nzp_payer, payer, nzp_serv, service, dat_oper, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                " From " + tXXDistrib +
                " Group by nzp_area, area, nzp_payer, payer, nzp_serv, service, dat_oper" +
                " Order by area, payer, service, dat_oper";

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<MoneyDistrib> list = new List<MoneyDistrib>();

            decimal sum_charge = 0, sum_send = 0;
            MoneyDistrib zap;
            try
            {
                int i = finder.skip;
                while (reader.Read())
                {
                    i++;
                    zap = new MoneyDistrib();

                    zap.num = i.ToString();

                    zap.nzp_user = finder.nzp_user;

                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.area = ((string)reader["area"]).Trim();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = (int)reader["nzp_payer"];
                    if (reader["payer"] != DBNull.Value) zap.payer = ((string)reader["payer"]).Trim();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();

                    if (reader["dat_oper"] != DBNull.Value)
                        zap.dat_oper = Convert.ToDateTime(reader["dat_oper"]).ToShortDateString();

                    if (reader["sum_charge"] != DBNull.Value)
                    {
                        zap.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                        sum_charge += zap.sum_charge;
                    }
                    if (reader["sum_send"] != DBNull.Value)
                    {
                        zap.sum_send = Convert.ToDecimal(reader["sum_send"]);
                        sum_send += zap.sum_send;
                    }

                    list.Add(zap);

                    if (finder.rows > 0 && list.Count >= finder.rows) break;
                }
            }
            catch (Exception e)
            {
                ret = new Returns(false,
                    "Ошибка загрузки перечислений подрядчикам:" + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetPayerTransfer " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_web.Close();
                return null;
            }

            reader.Close();
            conn_web.Close();

            if (finder.nzp_dis < 1)
                list.Add(new MoneyDistrib()
                {
                    num = "Итого",
                    nzp_user = finder.nzp_user,
                    sum_charge = sum_charge,
                    sum_send = sum_send
                });

            return list;
        }

        public List<MoneyNaud> FindMoneyNaud(MoneyNaud finder, out Returns ret)
        {
            if (finder.distrib == 1)
            {
                return FindMoneyNaudWithoutDom(finder, out ret);
            }
            else
            {
                return FindMoneyNaudWithDom(finder, out ret);
            }
        }


        public List<MoneyNaud> FindMoneyNaudWithoutDom(MoneyNaud finder, out Returns ret)
        {
            #region Проверка входных параметров

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.nzp_dis < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }

            #endregion

            ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            string tXXDistrib = "";
            string fields = "";
            string dom = "";
            if (finder.distrib == 1)
            {
                //
            }
            else
            {
                fields = ", nzp_dom";
                dom = "_dom";
            }
#if PG
            tXXDistrib = "public.t" + Convert.ToString(finder.nzp_user) + "_distrib" + dom;
#else
            tXXDistrib = "t" + Convert.ToString(finder.nzp_user) + "_distrib" + dom;
#endif
            IDataReader reader;

            ret = ExecRead(conn_web, out reader,
                "select nzp_payer, nzp_area, nzp_serv " + fields + " , nzp_bank, dat_oper, dat_oper_po from " +
                tXXDistrib + " Where nzp_dis = " + finder.nzp_dis, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            int nzp_area = 0, nzp_serv = 0, nzp_payer = 0, nzp_bank = 0, nzp_dom = 0;
            DateTime dat_oper = DateTime.MinValue, dat_oper_po = DateTime.MinValue;
            try
            {
                if (reader.Read())
                {
                    if (reader["nzp_area"] != DBNull.Value) nzp_area = (int)reader["nzp_area"];
                    if (reader["nzp_serv"] != DBNull.Value) nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_payer"] != DBNull.Value) nzp_payer = (int)reader["nzp_payer"];
                    if (reader["nzp_bank"] != DBNull.Value) nzp_bank = (int)reader["nzp_bank"];
                    if (finder.distrib == 2) if (reader["nzp_dom"] != DBNull.Value) nzp_dom = (int)reader["nzp_dom"];
                    if (reader["dat_oper"] != DBNull.Value) dat_oper = Convert.ToDateTime(reader["dat_oper"]);
                    if (reader["dat_oper_po"] != DBNull.Value) dat_oper_po = Convert.ToDateTime(reader["dat_oper_po"]);

                    if (dat_oper == DateTime.MinValue || dat_oper_po == DateTime.MinValue)
                    {
                        ret = new ReturnsType("Не задан операционный день").GetReturns();
                        return null;
                    }
                }
                else
                {
                    ret = new ReturnsType("Данных не найдено").GetReturns();
                    return null;
                }
            }
            catch
            {
                reader.Close();
                conn_web.Close();
                return null;
            }
            finally
            {
                reader.Close();
                conn_web.Close();
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            ExecSQL(conn_db, "drop table tmp_naud" + dom, false);

            int m1, y1, m2, y2;
            m1 = dat_oper.Month;
            y1 = dat_oper.Year;
            m2 = dat_oper_po.Month;
            y2 = dat_oper_po.Year;

            string sql = "";

            for (int y = y1; y <= y2; y++)
            {
#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_fin_" + (y % 100).ToString("00") + "'",
                    false);
#else
                ret = ExecSQL(conn_db, "database " + finder.pref + "_fin_" + (y % 100).ToString("00"), false);
#endif
                if (!ret.result) continue;
                if (!TableInWebCashe(conn_db, "fn_naud" + dom)) continue;

                if (sql != "") sql += " union all ";


#if PG
                if (sql != "")
                    sql += "select distinct nzp_payer_2, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud from " +
                           finder.pref + "_fin_" + (y % 100).ToString("00") + ".fn_naud" + dom;
                else
                    sql += "select distinct nzp_payer_2, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud " +
                           " into temp tmp_naud" + dom +
                           " from " + finder.pref + "_fin_" + (y % 100).ToString("00") + ".fn_naud" + dom;
#else
                sql += "select unique nzp_payer_2, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud from " + finder.pref + "_fin_" + (y % 100).ToString("00") + ":fn_naud" + dom;
#endif
                sql += " where dat_oper between " + Utils.EStrNull(dat_oper.ToShortDateString()) + " and " +
                       Utils.EStrNull(dat_oper_po.ToShortDateString());

                if (nzp_area > 0) sql += " and nzp_area = " + nzp_area;
                if (nzp_payer > 0) sql += " and a.nzp_payer = " + nzp_payer;
                if (nzp_serv > 0) sql += " and nzp_serv = " + nzp_serv;
                if (nzp_bank > 0) sql += " and nzp_bank = " + nzp_bank;
                if (nzp_dom > 0) sql += " and nzp_dom = " + nzp_dom;

                sql += " group by nzp_payer_2";
            }

            if (sql != "")
            {
#if PG
                //
#else
                sql += " into temp tmp_naud" + dom;
#endif
            }
            else
            {
                conn_db.Close();
                return new List<MoneyNaud>();
            }

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            sql = "Select a.nzp_payer_2, p.payer, sum(a.sum_ud) as sum_ud, sum(a.sum_naud) as sum_naud" +
#if PG
 " From tmp_naud" + dom + " a, " + finder.pref + "_kernel.s_payer p" +
#else
 " From tmp_naud" + dom + " a, " + finder.pref + "_kernel:s_payer p" +
#endif
 " Where a.nzp_payer_2 = p.nzp_payer" +
                  " Group by a.nzp_payer_2, p.payer" +
                  " Order by p.payer";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<MoneyNaud> list = new List<MoneyNaud>();
            while (reader.Read())
            {
                MoneyNaud zap = new MoneyNaud();
                if (reader["nzp_payer_2"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer_2"]);
                if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                if (reader["sum_ud"] != DBNull.Value) zap.sum_ud = Convert.ToDecimal(reader["sum_ud"]);
                if (reader["sum_naud"] != DBNull.Value) zap.sum_naud = Convert.ToDecimal(reader["sum_naud"]);

                list.Add(zap);
            }

            reader.Close();

            ExecSQL(conn_db, "drop table tmp_naud" + dom, false);

            conn_db.Close();

            return list;
        }

        public List<MoneyNaud> FindMoneyNaudWithDom(MoneyNaud finder, out Returns ret)
        {
            #region Проверка входных параметров

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.nzp_dis < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.mode == 0)
            {
                ret = new Returns(false, "Не определена колонка");
                return null;
            }

            #endregion

            ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
            string tXXDistrib = "";

#if PG
            tXXDistrib = "public.t" + Convert.ToString(finder.nzp_user) + "_distrib_dom";
#else
            tXXDistrib = "t" + Convert.ToString(finder.nzp_user) + "_distrib_dom";
#endif
            IDataReader reader;

            ret = ExecRead(conn_web, out reader,
                "select nzp_payer, nzp_area, nzp_serv, nzp_dom , nzp_bank, dat_oper, dat_oper_po from " +
                tXXDistrib + " Where nzp_dis = " + finder.nzp_dis, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            int nzp_area = 0, nzp_serv = 0, nzp_payer = 0, nzp_bank = 0, nzp_dom = 0;
            DateTime dat_oper = DateTime.MinValue, dat_oper_po = DateTime.MinValue;
            try
            {
                if (reader.Read())
                {
                    if (reader["nzp_area"] != DBNull.Value) nzp_area = (int)reader["nzp_area"];
                    if (reader["nzp_serv"] != DBNull.Value) nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_payer"] != DBNull.Value) nzp_payer = (int)reader["nzp_payer"];
                    if (reader["nzp_bank"] != DBNull.Value) nzp_bank = (int)reader["nzp_bank"];
                    if (finder.distrib == 2) if (reader["nzp_dom"] != DBNull.Value) nzp_dom = (int)reader["nzp_dom"];
                    if (reader["dat_oper"] != DBNull.Value) dat_oper = Convert.ToDateTime(reader["dat_oper"]);
                    if (reader["dat_oper_po"] != DBNull.Value) dat_oper_po = Convert.ToDateTime(reader["dat_oper_po"]);

                    if (dat_oper == DateTime.MinValue || dat_oper_po == DateTime.MinValue)
                    {
                        ret = new ReturnsType("Не задан операционный день").GetReturns();
                        return null;
                    }
                }
                else
                {
                    ret = new ReturnsType("Данных не найдено").GetReturns();
                    return null;
                }
            }
            catch
            {
                reader.Close();
                conn_web.Close();
                return null;
            }
            finally
            {
                reader.Close();
                conn_web.Close();
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            ExecSQL(conn_db, "drop table tmp_naud_dom", false);

            int m1, y1, m2, y2;
            m1 = dat_oper.Month;
            y1 = dat_oper.Year;
            m2 = dat_oper_po.Month;
            y2 = dat_oper_po.Year;

            string sql = "";
            for (int y = y1; y <= y2; y++)
            {
                string database = finder.pref + "_fin_" + (y % 100).ToString("00");
#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + database + "'", false);
#else
                ret = ExecSQL(conn_db, "database " + database, false);
#endif
                if (!ret.result) continue;

                if (finder.mode == 1) //- Следует удержать за обслуживание
                {
                    if (!TableInWebCashe(conn_db, "fn_naud_dom")) continue;
                    if (sql != "") sql += " union all ";
#if PG
                    if (sql != "")
                        sql += " select s.payer, srv.service, SUM(sum_prih) as sum_prih, " +
                               " perc_ud,SUM(sum_naud) as sum_naud from " +
                               database + tableDelimiter + "fn_naud_dom a, " +
                               Points.Pref + "_kernel" + tableDelimiter + "s_payer  s," +
                               Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
                    else
                        sql += " select s.payer, srv.service, SUM(sum_prih) as sum_prih, " +
                               " perc_ud,SUM(sum_naud) as sum_naud " +
                               " into temp tmp_naud_dom from " +
                               database + tableDelimiter + "fn_naud_dom a, " +
                               Points.Pref + "_kernel" + tableDelimiter + "s_payer  s," +
                               Points.Pref + "_kernel" + tableDelimiter + "services  srv ";

#else
                    sql += " select s.payer, srv.service, SUM(sum_prih) as sum_prih, " +
                            " perc_ud,SUM(sum_naud) as sum_naud from " +
                            database + tableDelimiter + "fn_naud_dom a, " +
                            Points.Pref + "_kernel" + tableDelimiter + "s_payer  s," +
                            Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
#endif
                    sql += " where " +
                           "  a.nzp_payer = s.nzp_payer and srv.nzp_serv = a.nzp_serv and " +
                           " dat_oper between '" + dat_oper.ToShortDateString() + "' and '" +
                           dat_oper_po.ToShortDateString() + "'";

                    if (nzp_area > 0) sql += " and a.nzp_area = " + nzp_area;
                    if (nzp_payer > 0) sql += " and a.nzp_payer_2 = " + nzp_payer;
                    if (nzp_serv > 0) sql += " and a.nzp_serv = " + nzp_serv;
                    if (nzp_bank > 0) sql += " and a.nzp_bank = " + nzp_bank;
                    if (nzp_dom > 0) sql += " and a.nzp_dom = " + nzp_dom;

                    sql += " group by 1,2,4";
                }
                else
                {

                    if (!TableInWebCashe(conn_db, "fn_perc_dom")) continue;
                    if (sql != "") sql += " union all ";

#if PG
                    if (sql != "")
                        sql +=
                            " select s.name_supp as payer, srv.service, SUM(sum_prih) as sum_prih,perc_ud,SUM(sum_perc) as sum_naud from " +
                            database + tableDelimiter + "fn_perc_dom a, " +
                            Points.Pref + "_kernel" + tableDelimiter + "supplier  s," +
                            Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
                    else
                        sql +=
                            " select s.name_supp as payer, srv.service, SUM(sum_prih) as sum_prih,perc_ud,SUM(sum_perc) as sum_naud " +
                            " into temp tmp_naud_dom from " +
                            database + tableDelimiter + "fn_perc_dom a, " +
                            Points.Pref + "_kernel" + tableDelimiter + "supplier  s," +
                            Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
#else
                    sql += " select s.name_supp as payer, srv.service, SUM(sum_prih) as sum_prih,perc_ud,SUM(sum_perc) as sum_naud from " +
                            database + tableDelimiter + "fn_perc_dom a, " +
                            Points.Pref + "_kernel" + tableDelimiter + "supplier  s," +
                            Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
#endif

                    sql += " where " +
                           "   a.nzp_supp = s.nzp_supp and srv.nzp_serv = a.nzp_serv and " +
                           " dat_oper between '" + dat_oper.ToShortDateString() + "' and '" +
                           dat_oper_po.ToShortDateString() + "'";

                    if (nzp_area > 0) sql += " and a.nzp_area = " + nzp_area;
                    if (nzp_payer > 0) sql += " and a.nzp_payer = " + nzp_payer;
                    if (nzp_serv > 0) sql += " and a.nzp_serv = " + nzp_serv;
                    if (nzp_bank > 0) sql += " and a.nzp_bank = " + nzp_bank;
                    if (nzp_dom > 0) sql += " and a.nzp_dom = " + nzp_dom;

                    sql += " group by 1,2,4";

                }
            }

            if (sql != "")
            {
#if PG
                //
#else
                sql += " into temp tmp_naud_dom";
#endif
            }
            else
            {
                conn_db.Close();
                return new List<MoneyNaud>();
            }

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            sql = "Select payer, service, sum_prih, perc_ud, sum_naud " +
                  " From tmp_naud_dom Order by payer";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<MoneyNaud> list = new List<MoneyNaud>();
            while (reader.Read())
            {
                MoneyNaud zap = new MoneyNaud();
                if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                if (reader["sum_prih"] != DBNull.Value) zap.sum_prih = Convert.ToDecimal(reader["sum_prih"]);
                if (reader["perc_ud"] != DBNull.Value) zap.perc_ud = Convert.ToDecimal(reader["perc_ud"]);
                if (reader["sum_naud"] != DBNull.Value) zap.sum_naud = Convert.ToDecimal(reader["sum_naud"]);

                list.Add(zap);
            }

            reader.Close();

            ExecSQL(conn_db, "drop table tmp_naud_dom", false);

            conn_db.Close();

            return list;
        }

        public List<MoneySended> FindMoneySended(MoneySended finder, out Returns ret)
        {
            #region Проверка входных параметров

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.nzp_area < 1 || finder.nzp_payer < 1 || finder.nzp_serv < 1 || finder.dat_oper == "")
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }
            DateTime dat_oper = DateTime.MinValue;

            if (!DateTime.TryParse(finder.dat_oper, out dat_oper))
            {
                ret = new Returns(false, "Неверный формат даты операционного дня");
                return null;
            }

            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "";
#if PG
            string sended = finder.pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + ".fn_sended";
            string distrib = finder.pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + ".fn_distrib_" +
                             dat_oper.Month.ToString("00");
            string dogovor = finder.pref + "_data.fn_dogovor";
            string osnov = finder.pref + "_data.fn_osnov";
            string bank = finder.pref + "_data.fn_bank";
#else
            string sended = finder.pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + ":fn_sended";
            string distrib = finder.pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + ":fn_distrib_" + dat_oper.Month.ToString("00");
            string dogovor = finder.pref + "_data:fn_dogovor";
            string osnov = finder.pref + "_data:fn_osnov";
            string bank = finder.pref + "_data:fn_bank";
#endif

            List<MoneySended> list = new List<MoneySended>();

            if (!TempTableInWebCashe(conn_db, sended))
            {
                conn_db.Close();
                return list;
            }

            IDataReader reader;

            sql =
                "Select a.nzp_area, a.nzp_payer, a.nzp_serv, a.dat_oper, sum(case when a.nzp_bank = -1 then s.sum_send else 0 end) as sum_send, d.nzp_fd, o.osnov, d.num_dog, d.dat_dog, b.rcount, b.bank_name, b.npunkt " +
#if PG
 " From " + dogovor + " d " +
                " left outer join " + osnov + " o ON o.nzp_osnov = d.nzp_osnov " +
                " left outer join " + bank + " b ON d.nzp_fb = b.nzp_fb, " +
                distrib + " a, " +
                " left outer join " + sended +
                " s ON a.nzp_payer = s.nzp_payer and a.nzp_area = s.nzp_area and a.nzp_serv = s.nzp_serv and a.dat_oper = s.dat_oper" +
                " Where a.nzp_area = " + finder.nzp_area +
                " and a.nzp_payer = " + finder.nzp_payer +
                " and a.nzp_serv = " + finder.nzp_serv +
                " and a.dat_oper = " + Utils.EStrNull(dat_oper.ToShortDateString()) +
                " and a.nzp_payer = d.nzp_payer and a.nzp_area = d.nzp_area " +
                " and s.nzp_fd = d.nzp_fd" +
#else
 " From " + distrib + " a, " + dogovor + " d, outer " + osnov + " o, outer " + bank + " b, outer " + sended + " s" +
                " Where a.nzp_area = " + finder.nzp_area +
                    " and a.nzp_payer = " + finder.nzp_payer +
                    " and a.nzp_serv = " + finder.nzp_serv +
                    " and a.dat_oper = " + Utils.EStrNull(dat_oper.ToShortDateString()) +
                    " and a.nzp_payer = d.nzp_payer and a.nzp_area = d.nzp_area and o.nzp_osnov = d.nzp_osnov and d.nzp_fb = b.nzp_fb" +
                    " and a.nzp_payer = s.nzp_payer and a.nzp_area = s.nzp_area and a.nzp_serv = s.nzp_serv and a.dat_oper = s.dat_oper and s.nzp_fd = d.nzp_fd" +
#endif
                //" and a.nzp_bank = -1 "+
                " Group by a.nzp_area, a.nzp_payer, a.nzp_serv, a.dat_oper, d.nzp_fd, o.osnov, d.num_dog, d.dat_dog, b.rcount, b.bank_name, b.npunkt";

            /*
            sql = "Select a.nzp_area, a.nzp_payer, a.nzp_serv, a.dat_oper, d.nzp_fd, o.osnov, d.num_dog, "+
                        " d.dat_dog, b.rcount, b.bank_name, b.npunkt , sum(a.sum_send) as sum_send " +
                " From " + sended + " a, outer (" + dogovor + " d, outer " + osnov + " o, outer " + bank + " b) "+
                " Where a.nzp_payer = d.nzp_payer and a.nzp_area = d.nzp_area "+
                    " and o.nzp_osnov = d.nzp_osnov and d.nzp_fb = b.nzp_fb " +
                    " and a.dat_oper = " + Utils.EStrNull(dat_oper.ToShortDateString()) +
                    " and a.nzp_area = " + finder.nzp_area +
                    " and a.nzp_payer = " + finder.nzp_payer +
                    " and a.nzp_serv = " + finder.nzp_serv +
                    //" and a.nzp_payer = s.nzp_payer and a.nzp_area = s.nzp_area and a.nzp_serv = s.nzp_serv and a.dat_oper = s.dat_oper and s.nzp_fd = d.nzp_fd" +
                    //" and a.nzp_bank = -1 " +
                " Group by a.nzp_area, a.nzp_payer, a.nzp_serv, a.dat_oper, d.nzp_fd, o.osnov, d.num_dog, d.dat_dog, b.rcount, b.bank_name, b.npunkt";
            */

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            while (reader.Read())
            {
                MoneySended zap = new MoneySended();
                //if (reader["nzp_snd"] != DBNull.Value) zap.nzp_snd = Convert.ToInt32(reader["nzp_snd"]);
                if (reader["sum_send"] != DBNull.Value) zap.sum_send = Convert.ToDecimal(reader["sum_send"]);

                if (reader["nzp_fd"] != DBNull.Value) zap.nzp_fd = Convert.ToInt32(reader["nzp_fd"]);
                if (reader["osnov"] != DBNull.Value) zap.dogovor = Convert.ToString(reader["osnov"]).Trim();
                if (reader["num_dog"] != DBNull.Value) zap.dogovor += " №" + Convert.ToString(reader["num_dog"]).Trim();
                if (reader["dat_dog"] != DBNull.Value)
                    zap.dogovor += " от " + Convert.ToDateTime(reader["dat_dog"]).ToShortDateString();

                zap.dogovor_bank = "Р/сч ";
                if (reader["rcount"] != DBNull.Value) zap.dogovor_bank += Convert.ToString(reader["rcount"]).Trim();
                if (reader["bank_name"] != DBNull.Value)
                    zap.dogovor_bank += " в " + Convert.ToString(reader["bank_name"]).Trim();
                if (reader["npunkt"] != DBNull.Value)
                    zap.dogovor_bank += ", " + Convert.ToString(reader["npunkt"]).Trim();

                list.Add(zap);
            }

            reader.Close();
            conn_db.Close();
            return list;
        }



        /// <summary> Данные для квитанции Лицевой счет
        /// </summary>
        public List<Charge> GetLicChetData(ref Kart finder, out Returns ret, int y, int m)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                return null;
            }

            //заполнить webdata:tXX_cnt
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDataReader reader;
            //IDataReader reader2;
            List<Charge> Spis = new List<Charge>();



            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string cur_pref = finder.pref;
            StringBuilder sql1 = new StringBuilder();

            string platFio = "";

            sql1.Remove(0, sql1.Length);
            sql1.Append(" select val_prm ");
            sql1.Append(" from " + cur_pref + "_data"
#if PG
 + "." +
#else
 + ":" +
#endif
 "prm_3 where nzp_prm=46 and is_actual=1 ");
#if PG
            sql1.Append(" and dat_s<=public.MDY(" + m + ", 01, " + y + ") ");
            sql1.Append(" and dat_po>=public.MDY(" + m + ", 01, " + y + ") and nzp=" + finder.nzp_kvar);
#else
            sql1.Append(" and dat_s<=MDY(" + m + ", 01, " + y + ") ");
            sql1.Append(" and dat_po>=MDY(" + m + ", 01, " + y + ") and nzp=" + finder.nzp_kvar);
#endif
            if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }
            else
            {
                if (reader.Read())
                {
                    if (reader["val_prm"] != DBNull.Value)
                        if (reader["val_prm"].ToString().Trim() != "")
                            platFio = reader["val_prm"].ToString().Trim();
                }
            }
            reader.Close();

            sql1.Remove(0, sql1.Length);
            sql1.Append(" Select s.ordering, kv.num_ls, kv.fio, ");
            sql1.Append(" s.service_small as service, ");
            sql1.Append(" c.num_ls, s.nzp_serv,  ");
#if PG
            sql1.Append(" coalesce(m.measure, m1.measure) as measure, coalesce(f.name_frm,'-') as name_frm, ");
#else
            sql1.Append(" nvl(m.measure, m1.measure) as measure, nvl(f.name_frm,'-') as name_frm, ");
#endif
            sql1.Append(" max(c.tarif) as tarif, ");
            sql1.Append(" sum(c.sum_charge) as sum_charge, ");
            sql1.Append(" sum(c.c_sn) as c_sn, ");
            sql1.Append(" sum(c_calc) as c_calc, ");
            sql1.Append(" sum(c.sum_tarif) as sum_tarif, ");
            sql1.Append(" sum(c.sum_insaldo) as Dolg");
            sql1.Append(" From ");
#if PG
            sql1.Append("  " + cur_pref + "_data. kvar kv, ");
            sql1.Append("  " + cur_pref + "_charge_" + y.ToString() + " . charge_" + m.ToString("00") + " c ");
            sql1.Append(" left outer join " + cur_pref + "_kernel.formuls f ");
            sql1.Append(" left outer join " + cur_pref + "_kernel.s_measure m on f.nzp_measure = m.nzp_measure ");
            sql1.Append(" on c.nzp_frm = f.nzp_frm, ");
            sql1.Append("  " + cur_pref + "_kernel . services s, ");
            sql1.Append("  " + cur_pref + "_kernel . s_measure m1 ");
            sql1.Append(" Where  ");
            sql1.Append("  kv.nzp_kvar = c.nzp_kvar ");
            sql1.Append(" and c.nzp_serv = s.nzp_serv ");
            sql1.Append(" and m1.nzp_measure = s.nzp_measure ");
#else
            sql1.Append("  " + cur_pref + "_data: kvar kv, ");
            sql1.Append("  " + cur_pref + "_charge_" + y.ToString() + " : charge_" + m.ToString("00") + " c, ");
            sql1.Append("  " + cur_pref + "_kernel : services s, outer (");
            sql1.Append("  " + cur_pref + "_kernel : formuls f, ");
            sql1.Append("  " + cur_pref + "_kernel : s_measure m), ");
            sql1.Append("  " + cur_pref + "_kernel : s_measure m1 ");
            sql1.Append(" Where  ");
            sql1.Append("  kv.nzp_kvar = c.nzp_kvar ");
            sql1.Append(" and c.nzp_serv = s.nzp_serv ");
            sql1.Append(" and m1.nzp_measure = s.nzp_measure ");
            sql1.Append(" and c.nzp_frm = f.nzp_frm ");
            sql1.Append(" and f.nzp_measure = m.nzp_measure ");
#endif

            sql1.Append(" and kv.nzp_kvar = " + finder.nzp_kvar.ToString());
            sql1.Append(" and c.dat_charge is null ");
            sql1.Append(" and c.nzp_serv>1 ");
            sql1.Append(" group by 1,2,3,4,5,6,7,8");
            sql1.Append(" order by 1, 6 ");

            if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            try
            {

                while (reader.Read())
                {
                    Charge zap = new Charge();

                    if (platFio == "")
                        if (reader["fio"] != DBNull.Value) platFio = reader["fio"].ToString().Trim();
                    zap.fio = platFio;
                    if (reader["num_ls"] != DBNull.Value) zap.num_ls = Convert.ToInt32(reader["num_ls"]);
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]);
                    if (reader["measure"] != DBNull.Value) zap.measure = Convert.ToString(reader["measure"]);
                    if (reader["tarif"] != DBNull.Value) zap.tarif = Convert.ToDecimal(reader["tarif"]);
                    if (reader["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader["sum_tarif"]);
                    if (reader["c_sn"] != DBNull.Value) zap.c_sn = Convert.ToDecimal(reader["c_sn"]);
                    if (reader["c_calc"] != DBNull.Value) zap.c_calc = Convert.ToDecimal(reader["c_calc"]);
                    if (reader["sum_charge"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                    if (reader["dolg"] != DBNull.Value) zap.sum_pere = Convert.ToDecimal(reader["dolg"]);
                    if (reader["name_frm"] != DBNull.Value) zap.name_frm = reader["name_frm"].ToString().Trim();

                    Spis.Add(zap);


                }
                reader.Close();

                #region Засчитываем услугу ОДН, как услугу ОДН под услугой содержание жилья

                Charge zapOdn = new Charge();
                zapOdn.service = "ОДН";
                zapOdn.measure = "м2";
                zapOdn.nzp_serv = -515;
                for (int i = 0; i <= Spis.Count - 1; i++)
                {
                    if (Spis[i].nzp_serv == 515)
                    {
                        zapOdn.sum_tarif = -Spis[i].sum_tarif;
                        zapOdn.sum_charge = -Spis[i].sum_charge;
                    }
                }

                int indexZap = -1;
                for (int i = 0; i <= Spis.Count - 1; i++)
                {
                    if (Spis[i].nzp_serv == 17)
                    {
                        indexZap = i;
                        zapOdn.name_frm = Spis[i].name_frm;
                    }
                }

                if (indexZap > -1)
                {
                    Spis.Insert(indexZap + 1, zapOdn);
                    Spis[indexZap].sum_charge -= zapOdn.sum_charge;
                }

                #endregion


                #region Норматив на ОДН по электроэнергии

                decimal kfodnEl = 0;
                sql1.Remove(0, sql1.Length);
                //sql1.Append(" select value from " + cur_pref + "_data:prm_2 a, ");
                //sql1.Append(cur_pref + "_kernel:res_values ");
                //sql1.Append(" where nzp_prm=2050 and nzp=" + finder.nzp_dom.ToString());
                //sql1.Append(" and nzp_res=3010 and nzp_y=a.val_prm ");

                sql1.Append(" select kf307 from ");
#if PG
                sql1.Append("  " + cur_pref + "_charge_" + y.ToString() + " . counters_" + m.ToString("00") + " c ");
#else
                sql1.Append("  " + cur_pref + "_charge_" + y.ToString() + " : counters_" + m.ToString("00") + " c ");
#endif
                sql1.Append(" where nzp_kvar=" + finder.nzp_kvar);
                sql1.Append(" and kod_info>0 and nzp_serv=25 and stek=3 and nzp_type=3 ");
                if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return null;
                }
                while (reader.Read())
                {

                    kfodnEl = reader["kf307"] != DBNull.Value ? System.Convert.ToDecimal(reader["kf307"]) : 0;
                }
                reader.Close();

                for (int i = 0; i < Spis.Count; i++)
                {
                    if ((Spis[i].nzp_serv == 25) & (Spis[i].tarif < 0.001m))
                    {
                        Spis[i].c_sn = kfodnEl > 0.001m ? kfodnEl : 0;
                    }
                }

                #endregion

                //#region Неучтенные оплаты
                //sql1.Remove(0, sql1.Length);
                //sql1.Append(" select sum(g_sum_ls) as g_sum_ls from " + Points.Pref);
                //sql1.Append("_fin_" + y.ToString("00") + ":pack_ls a, "+cur_pref + "_data:kvar k ");
                //sql1.Append(" where a.num_ls=k.num_ls and nzp_kvar=" + finder.nzp_kvar.ToString());
                //sql1.Append(" and dat_uchet>=MDY("+m+",01,"+y+")+1 units month ");
                //if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    conn_db.Close();
                //    return null;
                //}
                //while (reader.Read())
                //{
                //    if (Spis.Count > 0)
                //    {
                //        Spis[0].sum_pere -= reader["g_sum_ls"] != DBNull.Value ? System.Convert.ToDecimal(reader["g_sum_ls"]) : 0;
                //    }
                //}
                //reader.Close();

                //#endregion
                reader.Dispose();

                #region Объединение по service_uniona


                //sql1.Remove(0, sql1.Length);
                //sql1.Append(" Select service, s.nzp_serv_uni, s.nzp_serv_base ");
                //sql1.Append(" From " + cur_pref + "_kernel:service_union s," + cur_pref + "_kernel:services a");
                //sql1.Append(" where s.nzp_serv_base=a.nzp_serv");
                //sql1.Append(" order by service, s.nzp_serv_base, s.nzp_serv_uni ");

                //if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    conn_db.Close();
                //    return Spis;
                //}

                //int nzp_serv_uni = 0;
                //int nzp_serv_base = 0;
                //string service = "";

                //while (reader.Read())
                //{
                //    if (reader["nzp_serv_uni"] != DBNull.Value) nzp_serv_uni = Convert.ToInt32(reader["nzp_serv_uni"]);
                //    if (reader["nzp_serv_base"] != DBNull.Value) nzp_serv_base = Convert.ToInt32(reader["nzp_serv_base"]);
                //    if (reader["service"] != DBNull.Value) service = Convert.ToString(reader["service"]);

                //    for (int i = 0; i <= Spis.Count - 1; i++)
                //    {
                //        if (Spis[i].nzp_serv == nzp_serv_uni)
                //        {
                //            Spis[i].nzp_serv = nzp_serv_base;
                //            Spis[i].service = service;
                //        }
                //    }
                //}
                //reader.Close();
                //conn_db.Close();

                //for (int i = 0; i <= Spis.Count - 1; i++)
                //{
                //    Charge zap = Spis[i];
                //    for (int j = i + 1; j <= Spis.Count - 1; j++)
                //    {
                //        if (zap.nzp_serv == Spis[j].nzp_serv)
                //        {
                //            if (zap.nzp_serv != 25)
                //                zap.tarif = zap.tarif + Spis[j].tarif;
                //            else
                //                zap.tarif = Math.Max(zap.tarif, Spis[j].tarif);

                //            zap.sum_tarif = zap.sum_tarif + Spis[j].sum_tarif;
                //            zap.sum_charge = zap.sum_charge + Spis[j].sum_charge;
                //            zap.sum_pere = zap.sum_pere + Spis[j].sum_pere;
                //            Spis[j].nzp_serv = -1;
                //        }
                //    }
                //    Spis[i] = zap;
                //}

                //int k = 0;
                //while (k < Spis.Count)
                //{
                //    if (Spis[k].nzp_serv < 0)
                //        Spis.RemoveAt(k);
                //    else
                //        k++;
                //}

                #endregion



                return Spis;
            }
            finally
            {
                conn_db.Close(); //закрыть соединение с основной базой
            }
        }



        public List<_RecordDomODN> FillRep_Protokol_odn(ChargeFind finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                return null;
            }

            //заполнить webdata:tXX_cnt
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDataReader reader;
            //IDataReader reader2;
            List<_RecordDomODN> Spis = new List<_RecordDomODN>();
            _RecordDomODN zap;


            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string cur_pref = finder.pref;
            StringBuilder sql1 = new StringBuilder();

            #region Данные по квартирам

            if (Points.Region == Regions.Region.Samarskaya_obl || Points.IsSmr)
                sql1.Append(" Select kv.pkod10 as num_ls, kv.fio, c.cnt1 as count_gil,");
            else
                sql1.Append(" Select kv.num_ls as num_ls, kv.fio, c.cnt1 as count_gil,");
            sql1.Append(" trim(kv.nkvar) || (CASE WHEN kv.nkvar_n <> '-' THEN ' ' || kv.nkvar_n ELSE '' END) as nkvar, ");
            sql1.Append(" (case when nzp_serv = 25 then cnt2 else 0 end) as count_room, ");
            sql1.Append(" (case when cnt_stage = 1 then val2 else 0 end) as ipu, ");
            sql1.Append(" (case when cnt_stage = 0 then val1 else 0 end) as wipu, ");
            sql1.Append(" rashod, squ1 ");
#if PG
            sql1.Append(" From " + cur_pref + "_data. kvar kv, ");
#else
            sql1.Append(" From " + cur_pref + "_data: kvar kv, ");
#endif
            sql1.Append("  " + cur_pref + "_charge_" + (finder.year_ - 2000).ToString("00") +
#if PG
 " . counters_" + finder.month_.ToString("00") + " c ");
#else
 " : counters_" + finder.month_.ToString("00") + " c ");
#endif
            sql1.Append(" Where  ");
            sql1.Append(" kv.nzp_kvar=c.nzp_kvar and stek = 3 and nzp_type = 3 ");
            sql1.Append(" and c.nzp_serv =  " + finder.nzp_serv.ToString());
            sql1.Append(" and kv.nzp_dom = " + finder.nzp_dom.ToString());
            sql1.Append(" and 0< (select max(1) from " + cur_pref + "_data" +
#if PG
 "." +
#else
 ":" +
#endif
 " prm_3 a where nzp_prm=51");
            sql1.Append(" and val_prm='1' and is_actual=1 and kv.nzp_kvar=a.nzp ");
            sql1.Append(" and dat_s<=date('01." + finder.month_.ToString() + "." + finder.year_.ToString() + "')");
            sql1.Append(" and dat_po>=date('01." + finder.month_.ToString() + "." + finder.year_.ToString() + "'))");
            sql1.Append(" order by 4,2 ");

            if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            int i = 0;
            try
            {
                while (reader.Read())
                {
                    zap.count_gil = 0;
                    zap.count_room = 0;
                    zap.fio = "";
                    zap.kpu_rashod = 0;
                    zap.norma = 0;
                    zap.norma_rashod = 0;
                    zap.num_ls = 0;
                    zap.pl_kvar = 0;
                    zap.rashod = 0;
                    zap.alg = "";
                    zap.nkvar = "";


                    if (reader["fio"] != DBNull.Value) zap.fio = Convert.ToString(reader["fio"]);
                    if (reader["num_ls"] != DBNull.Value) zap.num_ls = Convert.ToInt32(reader["num_ls"]);
                    if (reader["ipu"] != DBNull.Value) zap.kpu_rashod = Convert.ToDecimal(reader["ipu"]);
                    if (reader["wipu"] != DBNull.Value) zap.norma_rashod = Convert.ToDecimal(reader["wipu"]);
                    if (reader["rashod"] != DBNull.Value) zap.rashod = Convert.ToDecimal(reader["rashod"]);
                    if (reader["squ1"] != DBNull.Value) zap.pl_kvar = Convert.ToDecimal(reader["squ1"]);
                    if (reader["nkvar"] != DBNull.Value) zap.nkvar = reader["nkvar"].ToString().Trim();

                    if (reader["count_gil"] != DBNull.Value)
                    {
                        zap.count_gil = Convert.ToInt32(reader["count_gil"]);

                        if (zap.count_gil > 0)
                            zap.norma = (zap.norma_rashod / zap.count_gil);
                    }
                    else zap.norma = 0;
                    if (reader["count_room"] != DBNull.Value) zap.count_room = Convert.ToInt32(reader["count_room"]);
                    else
                        zap.count_room = 0;

                    //if (i % 4 == 0)
                    Spis.Add(zap);
                    i++;
                }
                reader.Close();

            #endregion;

                #region коэффициент коррекции

                sql1.Remove(0, sql1.Length);
                sql1.Append(" Select kf307, name_type ");
                sql1.Append(" From ");
                sql1.Append("  " + cur_pref + "_charge_" + (finder.year_ - 2000).ToString("00") +
#if PG
 " . counters_" + finder.month_.ToString("00") + " c,  ");
#else
 " : counters_" + finder.month_.ToString("00") + " c,  ");
#endif

#if PG
                sql1.Append("  " + cur_pref + "_kernel.s_type_alg s ");
#else
                sql1.Append("  " + cur_pref + "_kernel:s_type_alg s ");
#endif
                sql1.Append(" Where  c.kod_info=s.nzp_type_alg ");
                sql1.Append(" and stek = 3 and nzp_type = 1 ");
                sql1.Append(" and c.nzp_serv =  " + finder.nzp_serv.ToString());
                sql1.Append(" and nzp_dom = " + finder.nzp_dom.ToString());

                if (!ExecRead(conn_db, out reader, sql1.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql1.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }

                while (reader.Read())
                {
                    zap.count_gil = 0;
                    zap.count_room = 0;
                    zap.fio = "Коэффициент коррекции";
                    zap.kpu_rashod = 0;
                    zap.norma = 0;
                    zap.norma_rashod = 0;
                    zap.num_ls = 0;
                    zap.pl_kvar = 0;
                    zap.rashod = 0;
                    zap.alg = "";
                    zap.nkvar = "";

                    if (reader["kf307"] != DBNull.Value)
                    {
                        zap.rashod = Convert.ToDecimal(reader["kf307"]);
                        zap.alg = Convert.ToString(reader["name_type"]);

                    }

                    Spis.Add(zap);

                }
                reader.Close();

                #endregion


                return Spis;
            }
            finally
            {
                conn_db.Close(); //закрыть соединение с основной базой
            }
        }

        public bool IsTableFilledIn(ChargeFind finder, TableName table, out Returns ret)
        {
            string tableName = "", databaseName = "";

            switch (table)
            {
                case TableName.UkRguCharge:
                    tableName = (finder.nzp_dom > 0 ? "fn_ukrgudom" : "fn_ukrgucharge");
                    databaseName = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00");
                    break;
                case TableName.Distrib:
                    tableName = "fn_distrib_" + finder.month_.ToString("00");
                    databaseName = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00");
                    break;
                default:
                    ret = new Returns(false, "Неверный код таблицы");
                    return false;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return false;

#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + databaseName, false);
#else
            ret = ExecSQL(conn_db, "database " + databaseName, false);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return false;
            }

            if (!TableInWebCashe(conn_db, tableName))
            {
                conn_db.Close();
                return false;
            }

            //опеределить количество записей
            string sql = "select count(*) from " + tableName;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try
            {
                recordsTotalCount = Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                ret = new Returns(false,
                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog(
                    "Ошибка IsTableFilledIn(" + table.ToString() + ") " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                recordsTotalCount = 0;
            }
            conn_db.Close();
            return (recordsTotalCount > 0);
        }

        public List<SaldoRep> FillRepServ(ChargeFind finder, out Returns ret, int num_rep)
        {
            ret = new Returns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не указан пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
#if PG
            string tXX_spls_full = "public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            string sql = "select distinct pref from " + tXX_spls;
            string pref = "";
            IDataReader reader, reader2;
            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                ret.result = false;
                conn_web.Close();
                return null;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            List<SaldoRep> list = new List<SaldoRep>();
            SaldoRep saldoRep;
            string database = "";
            string tablename = "";
            try
            {
                while (reader.Read())
                {
                    pref = (string)reader["pref"].ToString().Trim();
                    database = pref + "_charge_" + finder.year_.ToString().Substring(2, 2);
                    tablename = "charge_" +
                                ((finder.month_.ToString().Trim().Length == 1)
                                    ? "0" + finder.month_.ToString().Trim()
                                    : finder.month_.ToString().Trim());
                    ExecSQL(conn_db, "database " + database, true);
                    if (TableInWebCashe(conn_db, tablename))
                    {
                        sql = "select " +
                              "s.service, s.nzp_serv, " +
                              "sum(case when sum_insaldo < 0 then sum_insaldo else 0 end) as sum_insaldo_k, " +
                              "sum(case when sum_insaldo > 0 then sum_insaldo else 0 end) as sum_insaldo_d, " +
                              "sum(sum_insaldo) as sum_insaldo, " +
                              "sum(real_charge + reval) as sum_izm, " +
                              "sum(sum_real) as sum_real, " +
                              "sum(sum_money) as sum_money, " +
                              "sum(case when sum_outsaldo < 0 then sum_outsaldo else 0 end) as sum_outsaldo_k, " +
                              "sum(case when sum_outsaldo > 0 then sum_outsaldo else 0 end) as sum_outsaldo_d, " +
                              "sum(sum_outsaldo) as sum_outsaldo " +
#if PG
 "from " + database + "." + tablename + " ch, " + pref + "_kernel.services s " +
#else
 "from " + database + "@" + DBManager.getServer(conn_db) + ":" + tablename + " ch, " + pref + "_kernel@" + DBManager.getServer(conn_db) + ":services s " +
#endif
 "where s.nzp_serv = ch.nzp_serv and ch.nzp_kvar in (select nzp_kvar from " + tXX_spls_full +
                              ") and ch.dat_charge is null and ch.nzp_serv > 1 " +
                              "group by s.service, s.nzp_serv order by s.service";
                        if (!ExecRead(conn_db, out reader2, sql, true).result)
                        {
                            reader.Close();
                            ret.result = false;
                            conn_db.Close();
                            conn_web.Close();
                            return null;
                        }
                        try
                        {
                            while (reader2.Read())
                            {
                                saldoRep = new SaldoRep();
                                if (reader2["service"] != DBNull.Value)
                                    saldoRep.service = Convert.ToString(reader2["service"]);
                                if (reader2["nzp_serv"] != DBNull.Value)
                                    saldoRep.nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                                if (reader2["sum_insaldo_k"] != DBNull.Value)
                                    saldoRep.sum_insaldo_k = Convert.ToDecimal(reader2["sum_insaldo_k"]);
                                if (reader2["sum_insaldo_d"] != DBNull.Value)
                                    saldoRep.sum_insaldo_d = Convert.ToDecimal(reader2["sum_insaldo_d"]);
                                if (reader2["sum_insaldo"] != DBNull.Value)
                                    saldoRep.sum_insaldo = Convert.ToDecimal(reader2["sum_insaldo"]);
                                if (reader2["sum_izm"] != DBNull.Value)
                                    saldoRep.reval = Convert.ToDecimal(reader2["sum_izm"]);
                                if (reader2["sum_real"] != DBNull.Value)
                                    saldoRep.sum_real = Convert.ToDecimal(reader2["sum_real"]);
                                if (reader2["sum_money"] != DBNull.Value)
                                    saldoRep.sum_money = Convert.ToDecimal(reader2["sum_money"]);
                                if (reader2["sum_outsaldo_k"] != DBNull.Value)
                                    saldoRep.sum_outsaldo_k = Convert.ToDecimal(reader2["sum_outsaldo_k"]);
                                if (reader2["sum_outsaldo_d"] != DBNull.Value)
                                    saldoRep.sum_outsaldo_d = Convert.ToDecimal(reader2["sum_outsaldo_d"]);
                                if (reader2["sum_outsaldo"] != DBNull.Value)
                                    saldoRep.sum_outsaldo = Convert.ToDecimal(reader2["sum_outsaldo"]);
                                list.Add(saldoRep);
                            }
                            reader2.Close();
                        }
                        catch (Exception ex)
                        {
                            reader.Close();
                            reader2.Close();
                            conn_db.Close();
                            conn_web.Close();

                            ret.result = false;
                            ret.text = ex.Message;

                            string err = "";
                            if (Constants.Viewerror) err = " \n " + ex.Message;

                            MonitorLog.WriteLog("Ошибка выполнения отчета 5.10 " + err, MonitorLog.typelog.Error, 20,
                                201, true);

                            return null;
                        }
                    }
                }
                reader.Close();
                conn_db.Close();
                conn_web.Close();
                return list;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка выполнения отчета 5.10 " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }


        public List<Perekidka> LoadPerekidki(Perekidka finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_kvar == 0)
            {
                ret.result = false;
                ret.text = "Не выбран лицевой счет";
                ret.tag = -1;
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Не задан префикс";
                ret.tag = -1;
                return null;
            }

            string sql = "";
            string usl = "";
            if (finder.year_ > 0) usl += " and yearr = " + finder.year_.ToString();

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string where = "";
            if (finder.month_ > 0) where += " and p.month_ = " + finder.month_ + " ";

            RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(finder.pref));
            DateTime calcmonth = new DateTime(r_m.year_, r_m.month_, 1);


            List<Perekidka> list = new List<Perekidka>();
            Perekidka perekidka;

            List<Perekidka> lf = new List<Perekidka>();
            IDataReader reader2 = null;
            decimal itog_sum = 0;

#if PG
            sql = "select dbname, yearr from " + finder.pref + "_kernel.s_baselist where idtype=1 " + usl;
#else
            sql = "select dbname, yearr from " + finder.pref + "_kernel:s_baselist where idtype=1 " + usl;
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                string from_where =
#if PG
 "from " +
                    finder.pref + "_kernel.services s, " +
                    finder.pref + "_kernel.supplier sp, " +
                    Points.Pref + "_kernel.s_typercl tr, " +
                    "{BASE}.perekidka p " +
                    " left outer join " + Points.Pref + "_data.users u on u.nzp_user = p.nzp_user " +
                    " left outer join " + Points.Pref +
                    "_fin_{YEAR}.reestr_perekidok rp on rp.nzp_reestr = p.nzp_reestr" +
                    " left outer join " + Points.Pref + "_data.document_base db " +
                    "  left outer join " + Points.Pref + "_kernel.s_type_doc td on  td.nzp_type_doc = db.nzp_type_doc " +
                    " on db.nzp_doc_base = p.nzp_doc_base " +
                    " where p.nzp_kvar = " + finder.nzp_kvar + "  and p.nzp_serv = s.nzp_serv " +
                    " and p.nzp_supp = sp.nzp_supp and tr.type_rcl = p.type_rcl " + where;
#else
                          "from {BASE}:perekidka p, " +
                          finder.pref + "_kernel:services s, " +
                          finder.pref + "_kernel:supplier sp, " +
                          Points.Pref + "_kernel: s_typercl tr, " +
                         "outer "+ Points.Pref + "_data:document_base db, "+
                          " outer " + Points.Pref + "_kernel:s_type_doc td, " +
                          " outer " + Points.Pref + "_data:users u " +
                          ", outer " + Points.Pref + "_fin_{YEAR}:reestr_perekidok rp " +
                          "where p.nzp_kvar = " + finder.nzp_kvar + "  and p.nzp_serv = s.nzp_serv " +
                          " and p.nzp_doc_base = db.nzp_doc_base and td.nzp_type_doc = db.nzp_type_doc and p.nzp_supp = sp.nzp_supp and u.nzp_user = p.nzp_user  and tr.type_rcl = p.type_rcl " + where +
                          " and rp.nzp_reestr = p.nzp_reestr ";
#endif

                while (reader.Read())
                {
                    Perekidka f = new Perekidka();
                    if (reader["yearr"] != DBNull.Value) f.year_ = Convert.ToInt32(reader["yearr"]);
                    if (reader["dbname"] != DBNull.Value) f.database = Convert.ToString(reader["dbname"]).Trim();
                    if (finder.year_ != 0 && finder.year_ != f.year_) continue;
                    if (f.year_ > calcmonth.Year) continue;
                    lf.Add(f);

#if PG
                    if (!TempTableInWebCashe(conn_db, f.database + ".perekidka")) continue;
#else
                    if (!TempTableInWebCashe(conn_db, f.database + ":perekidka")) continue;
#endif


                    sql = "select sum(p.sum_rcl) as sum_rcl " +
                          from_where.Replace("{BASE}", f.database).Replace("{YEAR}", (f.year_ % 100).ToString("00"));

                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result) continue;
                    if (reader2.Read())
                    {
                        if (reader2["sum_rcl"] != DBNull.Value) itog_sum += Convert.ToDecimal(reader2["sum_rcl"]);
                    }
                    reader2.Close();
                }

                int totalrecords = 0;


                foreach (Perekidka f in lf)
                {
#if PG
                    if (!TempTableInWebCashe(conn_db, f.database + ".perekidka")) continue;
#else
                    if (!TempTableInWebCashe(conn_db, f.database + ":perekidka")) continue;
#endif

                    sql = "select count(*) as cnt " +
                          from_where.Replace("{BASE}", f.database).Replace("{YEAR}", (f.year_ % 100).ToString("00"));

                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result) continue;

                    while (reader2.Read())
                    {
                        if (reader2["cnt"] != DBNull.Value) totalrecords += Convert.ToInt32(reader2["cnt"]);
                    }
                    reader2.Close();
                }

                int i = 0;
                foreach (Perekidka f in lf)
                {
#if PG
                    if (!TempTableInWebCashe(conn_db, f.database + ".perekidka")) continue;
#else
                    if (!TempTableInWebCashe(conn_db, f.database + ":perekidka")) continue;
#endif

                    sql =
                        "select p.nzp_rcl, p.nzp_kvar, p.num_ls, p.nzp_serv, p.nzp_supp, p.date_rcl, p.nzp_doc_base, " +
                        "p.sum_rcl, p.tarif, p.volum, p.month_, db.comment, p.nzp_user, s.service, sp.name_supp, " +
                        "trim(u.name) as login, trim(u.comment) as user_name, tr.type_rcl, tr.is_volum, tr.typename," +
                        " rp.nzp_reestr, rp.nzp_oper, td.doc_name as type_doc, db.nzp_type_doc, db.num_doc, db.dat_doc " +
                        from_where.Replace("{BASE}", f.database).Replace("{YEAR}", (f.year_ % 100).ToString("00"));

                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result) continue;

                    while (reader2.Read())
                    {
                        i++;
                        if (i <= finder.skip) continue;
                        perekidka = new Perekidka();
                        if (reader2["nzp_rcl"] != DBNull.Value) perekidka.nzp_rcl = Convert.ToInt32(reader2["nzp_rcl"]);
                        if (reader2["nzp_kvar"] != DBNull.Value)
                            perekidka.nzp_kvar = Convert.ToInt32(reader2["nzp_kvar"]);
                        if (reader2["nzp_serv"] != DBNull.Value)
                            perekidka.nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                        if (reader2["nzp_supp"] != DBNull.Value)
                            perekidka.nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);
                        if (reader2["date_rcl"] != DBNull.Value)
                            perekidka.date_rcl = Convert.ToDateTime(reader2["date_rcl"]);
                        if (reader2["sum_rcl"] != DBNull.Value)
                            perekidka.sum_rcl = Convert.ToDecimal(reader2["sum_rcl"]);
                        if (reader2["tarif"] != DBNull.Value) perekidka.tarif = Convert.ToDecimal(reader2["tarif"]);
                        if (reader2["volum"] != DBNull.Value) perekidka.volum = Convert.ToDecimal(reader2["volum"]);
                        if (reader2["month_"] != DBNull.Value) perekidka.month_ = Convert.ToInt32(reader2["month_"]);
                        if (reader2["comment"] != DBNull.Value)
                            perekidka.doc_base.comment = Convert.ToString(reader2["comment"]);
                        if (reader2["nzp_user"] != DBNull.Value)
                            perekidka.nzp_user = Convert.ToInt32(reader2["nzp_user"]);
                        if (reader2["service"] != DBNull.Value)
                            perekidka.service = Convert.ToString(reader2["service"]);
                        if (reader2["name_supp"] != DBNull.Value)
                            perekidka.name_supp = Convert.ToString(reader2["name_supp"]);
                        if (reader2["login"] != DBNull.Value) perekidka.webLogin = Convert.ToString(reader2["login"]);
                        if (reader2["user_name"] != DBNull.Value)
                            perekidka.webUname = Convert.ToString(reader2["user_name"]);
                        if (reader2["type_rcl"] != DBNull.Value)
                            perekidka.typercl.type_rcl = Convert.ToInt32(reader2["type_rcl"]);
                        if (reader2["is_volum"] != DBNull.Value)
                            perekidka.typercl.is_volum = Convert.ToInt32(reader2["is_volum"]);
                        if (reader2["typename"] != DBNull.Value)
                            perekidka.typercl.typename = Convert.ToString(reader2["typename"]);

                        if (reader2["nzp_doc_base"] != DBNull.Value)
                            perekidka.doc_base.nzp_doc_base = Convert.ToInt32(reader2["nzp_doc_base"]);
                        if (reader2["nzp_reestr"] != DBNull.Value)
                            perekidka.nzp_reestr = Convert.ToInt32(reader2["nzp_reestr"]);
                        if (reader2["nzp_oper"] != DBNull.Value)
                        {
                            int oper = Convert.ToInt32(reader2["nzp_oper"]);
                            perekidka.nzp_oper = oper;
                            perekidka.reestr = "№" + perekidka.nzp_reestr + ". " +
                                               ParamsForGroupPerekidki.GetOperationNameById(oper);
                        }

                        if (reader2["nzp_type_doc"] != DBNull.Value)
                            perekidka.doc_base.nzp_type_doc = Convert.ToInt32(reader2["nzp_type_doc"]);
                        if (reader2["type_doc"] != DBNull.Value)
                            perekidka.doc_base.type_doc = Convert.ToString(reader2["type_doc"]);

                        if (reader2["num_doc"] != DBNull.Value)
                            perekidka.doc_base.num_doc = Convert.ToString(reader2["num_doc"]);
                        if (reader2["dat_doc"] != DBNull.Value)
                            perekidka.doc_base.dat_doc = Convert.ToDateTime(reader2["dat_doc"]).ToShortDateString();


                        perekidka.year_ = f.year_;
                        list.Add(perekidka);
                        if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                    }
                    reader2.Close();
                }

                reader.Close();
                Perekidka p = new Perekidka();
                p.sum_rcl = itog_sum;
                list.Insert(0, p);
                ret.tag = totalrecords;
                return list;
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                CloseReader(ref reader2);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения перекидок " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public Returns SavePerekidka(Perekidka finder)
        {
            Returns ret = Utils.InitReturns();
            if (!(finder.nzp_user > 0))
            {
                ret.text = "Пользователь не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            if (!(finder.year_ > 0))
            {
                ret.text = "Год не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            if (!(finder.nzp_kvar > 0))
            {
                ret.text = "Лицевой счет не выбран";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region определение локального пользователя
            int nzpUser = finder.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/

            #endregion

            string sql = "";
            sql = "select num_ls from " + finder.pref + "_data" + tableDelimiter + "kvar where nzp_kvar = " +
                  finder.nzp_kvar;

            IDataReader reader2;
            if (!ExecRead(conn_db, out reader2, sql, true).result)
            {
                ret.result = false;
                conn_db.Close();
                return ret;
            }
            if (reader2.Read())
                if (reader2["num_ls"] != DBNull.Value) finder.num_ls = Convert.ToInt32(reader2["num_ls"]);


            string usl = "";
            usl += " and yearr = " + finder.year_.ToString();
#if PG
            sql = "select dbname, yearr from " + finder.pref + "_kernel.s_baselist where idtype=1 " + usl;
#else
            sql = "select dbname, yearr from " + finder.pref + "_kernel:s_baselist where idtype=1 " + usl;
#endif

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.result = false;
                conn_db.Close();
                return ret;
            }

            if (finder.typercl.is_volum == 1)
            {
                finder.sum_rcl = finder.tarif * finder.volum;
            }

            string dbname = "";
            try
            {
                if (reader.Read())
                {
                    if (reader["dbname"] != DBNull.Value) dbname = Convert.ToString(reader["dbname"]).Trim();
                    ret = ExecSQL(conn_db,
#if PG
                        "set search_path to '" + dbname + "'"
#else
 "database " + dbname
#endif
                        , false);
                    if (!ret.result) return ret;
                    IDbTransaction transaction;
                    transaction = conn_db.BeginTransaction();
                    ret = SaveDocumentBase(conn_db, transaction, finder.doc_base);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                    finder.doc_base.nzp_doc_base = ret.tag;

                    int nzp_dict = 0;
                    if (finder.nzp_rcl > 0)
                    {
                        nzp_dict = 6599; //изменение перекидки
                        //string s = "";

                        ret = UpdateSumReestrPerekidok(conn_db, transaction, finder, dbname);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            conn_db.Close();
                            return ret;
                        }

#if PG
                        sql = "update " + dbname + ".perekidka set nzp_serv = " + finder.nzp_serv + ", nzp_supp = " +
                              finder.nzp_supp + ", sum_rcl =" + finder.sum_rcl +
#else
                        sql = "update " + dbname + ":perekidka set nzp_serv = " + finder.nzp_serv + ", nzp_supp = " + finder.nzp_supp + ", sum_rcl =" + finder.sum_rcl +
#endif
                            ", tarif = " + finder.tarif + ", volum = " + finder.volum + ", type_rcl = " +
                              finder.typercl.type_rcl + ", " + " nzp_user = " + nzpUser +
                              ", date_rcl = '" + DateTime.Now.ToShortDateString() + "', nzp_doc_base = " +
                              finder.doc_base.nzp_doc_base +
                              " where nzp_rcl = " + finder.nzp_rcl;
                    }
                    else
                    {
                        nzp_dict = 6597; //добавление перекидки

                        /*  string s = "", f = "";
                          if (finder.doc_base.nzp_type_doc > 0)
                          {
                              f += ", nzp_type_doc";
                              s += "," + finder.doc_base.nzp_type_doc;
                          }
                          if (finder.doc_base.num_doc != "")
                          {
                              f += ", num_doc ";
                              s += ", " + Utils.EStrNull(finder.doc_base.num_doc);
                          }
                          if (finder.doc_base.dat_doc != "")
                          {
                              f += ", dat_doc";
                              s += ", '" + finder.doc_base.dat_doc + "'";
                          }*/

#if PG
                        sql = "insert into " + dbname +
                              ".perekidka (nzp_kvar, num_ls, nzp_serv, nzp_supp, date_rcl, type_rcl, tarif, volum, " +
#else
                        sql = "insert into " + dbname + ":perekidka (nzp_kvar, num_ls, nzp_serv, nzp_supp, date_rcl, type_rcl, tarif, volum, " +
#endif
                            "sum_rcl, month_, nzp_user,nzp_doc_base" + ") values (" + finder.nzp_kvar + "," +
                              finder.num_ls + "," + finder.nzp_serv + "," + finder.nzp_supp + "," +
                              "'" + DateTime.Now.ToShortDateString() + "'" + "," + finder.typercl.type_rcl + "," +
                              finder.tarif + "," + finder.volum +
                              "," + finder.sum_rcl + "," + finder.month_ + "," + +nzpUser + "," +
                              finder.doc_base.nzp_doc_base + ")";
                    }


                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }

                    #region Добавление в sys_events события

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = finder.pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = nzp_dict,
                        nzp_obj = finder.nzp_kvar,
                        note =
                            "Перекидка на сумму " + finder.sum_rcl + " была " +
                            (nzp_dict == 6597 ? "добавлена" : "изменена.")
                    }, transaction, conn_db);

                    #endregion

                    transaction.Commit();
                }
                else
                {
                    ret = new Returns(false, "Не удалось сохранить. Не выполнен переход на " + finder.year_+ " год (" +Points.GetPoint(finder.pref).point+"). Обратитесь к администратору.", -1);
                }
                reader.Close();

                if (ret.result)
                {
                    var dbCalc = new DbCalcCharge();
                    dbCalc.CalcChargeXXUchetOplatForLs(conn_db, null,
                        new Charge()
                        {
                            num_ls = finder.num_ls,
                            pref = finder.pref,
                            year_ = finder.year_,
                            month_ = finder.month_
                        }, CalcTypes.FunctionType.Perekidki);
                    dbCalc.Close();
                }
                return ret;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения перекидок " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

        }

        private Returns UpdateSumReestrPerekidok(IDbConnection connDB, IDbTransaction transaction, Perekidka finder,
            string charge_dbname)
        {
            Returns ret;
            string reestr_perekidok = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter +
                                      "reestr_perekidok";
            string sql = "update " + reestr_perekidok + " set sum_oper = (" +
                         reestr_perekidok + ".sum_oper - (select sum_rcl from " + charge_dbname + tableDelimiter +
                         "perekidka p where nzp_rcl = " + finder.nzp_rcl + ") + " + finder.sum_rcl +
                         " ) where nzp_oper = " + (int)ParamsForGroupPerekidki.Operations.PerekidkaLs +
                         " and nzp_reestr = (select nzp_reestr from " + charge_dbname + tableDelimiter +
                         "perekidka  where nzp_rcl =   " + finder.nzp_rcl + ")";
            ret = ExecSQL(connDB, transaction, sql, true);
            return ret;
        }

        public Returns SaveDocumentBase(IDbConnection connDB, IDbTransaction transaction, TDocumentBase docbase)
        {
            Returns ret;
            var tables = new DbTables(connDB);
            string sql;
            if (docbase.nzp_doc_base > 0)
            {
                sql = " update " + tables.document_base + " set num_doc = '" + docbase.num_doc + "'," +
                      " dat_doc ='" + docbase.dat_doc + "', nzp_type_doc = " + docbase.nzp_type_doc +
                      ", comment='" + docbase.comment + "' where nzp_doc_base = " + docbase.nzp_doc_base;
                ret = ExecSQL(connDB, transaction, sql, true);
                ret.tag = docbase.nzp_doc_base;
            }
            else
            {
                sql = " insert into " + tables.document_base + "(num_doc,dat_doc,nzp_type_doc,comment) " +
                      " values ('" + docbase.num_doc + "','" + docbase.dat_doc + "', " + docbase.nzp_type_doc +
                      ", '" + docbase.comment + "')";
                ret = ExecSQL(connDB, transaction, sql, true);
                if (!ret.result) return ret;
                MyDataReader reader;
#if PG
                sql = "SELECT lastval() as co";
#else
                sql="SELECT first 1 dbinfo('sqlca.sqlerrd1') as co from systables";
#endif
                ret = ExecRead(connDB, transaction, out reader, sql.ToString(), true);


                if (reader.Read())
                    if (reader["co"] != DBNull.Value)
                        ret.tag = System.Convert.ToInt32(reader["co"]);
                reader.Close();
            }

            return ret;
        }

        public Returns DeleteDocumentBase(IDbConnection connDB, IDbTransaction transaction, TDocumentBase docbase)
        {
            Returns ret = Utils.InitReturns();
            var tables = new DbTables(connDB);
            string sql;
            if (docbase.nzp_doc_base > 0)
            {
                sql = " delete from " + tables.document_base + " where nzp_doc_base = " + docbase.nzp_doc_base;
                ret = ExecSQL(connDB, transaction, sql, true);
            }

            return ret;
        }

        public Returns DeletePerekidka(Perekidka finder)
        {
            Returns ret = Utils.InitReturns();
            if (!(finder.nzp_user > 0))
            {
                ret.text = "Пользователь не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            if (!(finder.year_ > 0))
            {
                ret.text = "Год не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string sql = "";
            string usl = "";
            usl += " and yearr = " + finder.year_.ToString();
#if PG
            sql = "select dbname, yearr from " + finder.pref + "_kernel.s_baselist where idtype=1 " + usl;
#else
            sql = "select dbname, yearr from " + finder.pref + "_kernel:s_baselist where idtype=1 " + usl;
#endif

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.result = false;
                conn_db.Close();
                return ret;
            }

            string dbname = "";
            int month = 0, num_ls = 0;
            try
            {
                if (reader.Read())
                {
                    if (reader["dbname"] != DBNull.Value) dbname = Convert.ToString(reader["dbname"]).Trim();
#if PG
                    ret = ExecSQL(conn_db, "set search_path to '" + dbname + "'", false);
#else
                    ret = ExecSQL(conn_db, "database " + dbname, false);
#endif
                    if (!ret.result) return ret;

                    if (finder.nzp_rcl > 0)
                    {
                        IDbTransaction transaction;
                        transaction = conn_db.BeginTransaction();

                        sql = "select nzp_doc_base, month_, num_ls from " + dbname + tableDelimiter +
                              "perekidka where nzp_rcl = " + finder.nzp_rcl;
                        MyDataReader myreader;
                        ret = ExecRead(conn_db, transaction, out myreader, sql, true);
                        if (!ret.result)
                        {
                            transaction.Rollback();
                            conn_db.Close();
                            return ret;
                        }
                        int nzp_doc_base = 0;
                        if (myreader.Read())
                        {
                            if (myreader["nzp_doc_base"] != DBNull.Value)
                                nzp_doc_base = Convert.ToInt32(myreader["nzp_doc_base"]);
                            if (myreader["month_"] != DBNull.Value) month = Convert.ToInt32(myreader["month_"]);
                            if (myreader["num_ls"] != DBNull.Value) num_ls = Convert.ToInt32(myreader["num_ls"]);
                        }

                        ret = UpdateSumReestrPerekidok(conn_db, transaction, finder, dbname);
                        if (!ret.result)
                        {
                            transaction.Rollback();
                            conn_db.Close();
                            return ret;
                        }

                        sql = "delete from " + dbname + tableDelimiter + "perekidka where nzp_rcl = " + finder.nzp_rcl;

                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            transaction.Rollback();
                            conn_db.Close();
                            return ret;
                        }

                        TDocumentBase db = new TDocumentBase();
                        db.nzp_doc_base = nzp_doc_base;

                        sql = "select count(*) from " + Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") +
                              tableDelimiter + "reestr_perekidok where nzp_doc_base = " + db.nzp_doc_base;
                        Object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                        int count = 0;
                        try
                        {
                            count = Convert.ToInt32(obj);
                        }
                        catch
                        {
                        }
                        if (count == 0)
                        {
                            ret = DeleteDocumentBase(conn_db, transaction, db);
                            if (!ret.result)
                            {
                                transaction.Rollback();
                                conn_db.Close();
                                return ret;
                            }
                        }

                        #region Добавление в sys_events события

                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = finder.pref,
                            nzp_user = finder.nzp_user,
                            nzp_dict = 6598,
                            nzp_obj = finder.nzp_kvar,
                            note = "Перекидка была удалена."
                        }, transaction, conn_db);

                        #endregion

                        transaction.Commit();
                    }
                }
                reader.Close();

                if (ret.result)
                {
                    var dbCalc = new DbCalcCharge();
                    Returns ret2 = dbCalc.CalcChargeXXUchetOplatForLs(conn_db, null,
                        new Charge() { num_ls = num_ls, pref = finder.pref, year_ = finder.year_, month_ = month }, CalcTypes.FunctionType.Perekidki);
                    dbCalc.Close();
                }
                return ret;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка удаления перекидок " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
        }

        public List<Perekidka> LoadSumsPerekidkaLs(Perekidka finder, out Returns ret)
        {
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс БД");
                return null;
            }

            if (finder.month_ <= 0 && finder.year_ <= 0)
            {
                ret = new Returns(false, "Не задан расчетный месяц");
                return null;
            }

            if (finder.nzp_kvar <= 0)
            {
                ret = new Returns(false, "Не задан лицевой счет");
                return null;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            IDataReader reader;

            string sql = "Select ch.nzp_serv, s.service, sp.name_supp, sp.nzp_supp " +
                         ", sum(sum_charge) as sum_charge " +
                         ", sum(sum_tarif) as sum_tarif " +
                         ", sum(sum_insaldo) as sum_insaldo " +
                         ", sum(sum_outsaldo) as sum_outsaldo " +
#if PG
 " From " + finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + ".charge_" +
                         finder.month_.ToString("00") + " ch," +
                         finder.pref + "_kernel.services s" + ", " + finder.pref + "_kernel.supplier sp" +
#else
 " From " + finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + ":charge_" + finder.month_.ToString("00") + " ch," +
                        finder.pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":services s" + ", " + finder.pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":supplier sp" +
#endif
 " Where ch.nzp_kvar = " + finder.nzp_kvar +
                         " and ch.dat_charge is null" +
                         " and ch.nzp_serv > 1" +
                         " and ch.nzp_serv = s.nzp_serv" +
                         " and ch.nzp_supp = sp.nzp_supp" +
                         " group by 1, 2, 3, 4";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Perekidka> list = new List<Perekidka>();
            try
            {
                while (reader.Read())
                {
                    Perekidka zap = new Perekidka();
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["name_supp"] != DBNull.Value)
                        zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    switch (finder.etalon)
                    {
                        case 1:
                            if (reader["sum_charge"] != DBNull.Value)
                                zap.sum_rcl = Convert.ToDecimal(reader["sum_charge"]);
                            break;
                        case 2:
                            if (reader["sum_tarif"] != DBNull.Value)
                                zap.sum_rcl = Convert.ToDecimal(reader["sum_tarif"]);
                            break;
                        case 3:
                            if (reader["sum_insaldo"] != DBNull.Value)
                                zap.sum_rcl = Convert.ToDecimal(reader["sum_insaldo"]);
                            break;
                        case 4:
                            if (reader["sum_outsaldo"] != DBNull.Value)
                                zap.sum_rcl = Convert.ToDecimal(reader["sum_outsaldo"]);
                            break;
                    }
                    list.Add(zap);
                }
            }
            catch (Exception ex)
            {
                conn_db.Close();
                reader.Close();
                ret = new Returns(false, ex.Message);
                list = null;
                MonitorLog.WriteLog("Ошибка LoadSumsPerekidkaLs " + (Constants.Viewerror ? "\n " + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
            }

            reader.Close();
            conn_db.Close();
            return list;
        }

        public Returns SaveSumsPerekidkaLs(List<Perekidka> listfinder)
        {
            Returns ret;

            #region проверки

            if (listfinder.Count == 0) return new Returns(false, "Нет данных для сохранения", -1);
            if (listfinder[0].nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (listfinder[0].year_ < 1) return new Returns(false, "Не задан год");
            if (listfinder[0].month_ < 1) return new Returns(false, "Не задан месяц");
            if (listfinder[0].pref == "") return new Returns(false, "Не задан префикс");
            if (listfinder[0].nzp_kvar < 1) return new Returns(false, "Не задан лицевой счет");

            #endregion

            string pref_ = "";
            int nzp_kvar = 0, month_ = 0, year_ = 0, num_ls = 0;
            TDocumentBase docbase;

            pref_ = listfinder[0].pref;
            nzp_kvar = listfinder[0].nzp_kvar;
            month_ = listfinder[0].month_;
            year_ = listfinder[0].year_;
            docbase = listfinder[0].doc_base;
            decimal sum = 0;
            foreach (Perekidka p in listfinder) sum += p.distr_sum;

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            IDataReader reader;
#if PG
            string sql = "select num_ls from " + pref_ + "_data.kvar where nzp_kvar = " + nzp_kvar;
#else
            string sql = "select num_ls from " + pref_ + "_data" + "@" + DBManager.getServer(conn_db) + ":kvar where nzp_kvar = " + nzp_kvar;
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (reader.Read()) if (reader["num_ls"] != DBNull.Value) num_ls = Convert.ToInt32(reader["num_ls"]);
            reader.Close();

            #region определение локального пользователя
            int nzpUser = listfinder[0].nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, listfinder[0], out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/

            #endregion

            IDbTransaction transaction = conn_db.BeginTransaction();

            ret = SaveDocumentBase(conn_db, transaction, docbase);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            docbase.nzp_doc_base = ret.tag;

#if PG
            sql = "insert into " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") +
                  ".reestr_perekidok " +
#else
            sql = "insert into " + Points.Pref +  "_fin_"+(Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + ":reestr_perekidok " +
#endif
 "(dat_uchet, nzp_doc_base, comment, nzp_oper, sum_oper, type_rcl, is_actual, created_by, changed_by, created_on, changed_on) " +
                  " values ('" + Points.DateOper.ToShortDateString() + "'," + docbase.nzp_doc_base +
                  ",'Изменение сальдо по лицевому счету'" + "," +
                  ParamsForGroupPerekidki.Operations.PerekidkaLs.GetHashCode() + "," + sum + "," +
                  listfinder[0].typercl.type_rcl + ",1," + nzpUser + "," + nzpUser +
#if PG
 ", now(), now())";
#else
 ", current, current)";
#endif
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            int nzp = GetSerialValue(conn_db, transaction);

            foreach (Perekidka finder in listfinder)
            {
#if PG
                sql = "insert into " + pref_ + "_charge_" + (finder.year_ % 100).ToString("00") + ".perekidka " +
#else
                sql = "insert into " + pref_ + "_charge_" + (finder.year_ % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + ":perekidka " +
#endif
 " (nzp_kvar, num_ls,nzp_serv,nzp_supp,type_rcl,date_rcl,sum_rcl,month_,nzp_user, nzp_reestr," +
                      " nzp_doc_base) " +
                      " values (" + nzp_kvar + "," + num_ls + "," + finder.nzp_serv + "," + finder.nzp_supp + "," +
                      finder.typercl.type_rcl + "," + Utils.EStrNull(finder.date_rcl.ToShortDateString()) + "," +
                      finder.distr_sum + "," + month_ + "," + nzpUser + "," + nzp + "," +
                      docbase.nzp_doc_base +
                      ")";

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }

                #region Добавление в sys_events события

                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = pref_,
                    nzp_user = listfinder[0].nzp_user,
                    nzp_dict = 6597,
                    nzp_obj = nzp_kvar,
                    note = "Перекидка на сумму " + finder.distr_sum + " была добавлена"
                }, transaction, conn_db);

                #endregion
            }

            if (ret.result) transaction.Commit();
            else transaction.Rollback();

            if (ret.result)
            {
                var dbCalc = new DbCalcCharge();
                dbCalc.CalcChargeXXUchetOplatForLs(conn_db, null,
                    new Charge() { num_ls = num_ls, pref = pref_, year_ = year_, month_ = month_ }, CalcTypes.FunctionType.Perekidki);
                dbCalc.Close();
            }

            conn_db.Close();
            return ret;
        }

        public List<TypeRcl> LoadTypeRcl(TypeRcl finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<TypeRcl> list = new List<TypeRcl>();

            string where = "";
            if (finder.type_rcl > 0) where = " and type_rcl = " + finder.type_rcl;
            if (finder.is_auto != "") where += " and is_auto = " + finder.is_auto;
            if (finder.is_volum != -1) where += " and is_volum = " + finder.is_volum;
            if (finder.nzp_type_uchet_exclude != "")
                where += " and nzp_type_uchet not in (" + finder.nzp_type_uchet_exclude + ")";
            if (finder.nzp_type_uchet > 0) where += " and nzp_type_uchet = " + finder.nzp_type_uchet;

            string sql = " SELECT type_rcl,is_volum,typename" +
                         " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_typercl" +
                         " WHERE 1=1 and is_actual<>100 " + where;

            IDataReader reader = null;

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    TypeRcl typercl = new TypeRcl();
                    if (reader["type_rcl"] != DBNull.Value) typercl.type_rcl = Convert.ToInt32(reader["type_rcl"]);
                    if (reader["is_volum"] != DBNull.Value) typercl.is_volum = Convert.ToInt32(reader["is_volum"]);
                    if (reader["typename"] != DBNull.Value) typercl.typename = Convert.ToString(reader["typename"]);
                    list.Add(typercl);
                }
            }
            catch (Exception ex)
            {
                conn_db.Close();
                reader.Close();
                ret = new Returns(false, ex.Message);
                list = null;
                MonitorLog.WriteLog("Ошибка LoadTypeRcl " + (Constants.Viewerror ? "\n " + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
            }

            conn_db.Close();
            reader.Close();
            return list;
        }

        public List<TypeDoc> LoadTypeDoc(TypeDoc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<TypeDoc> list = new List<TypeDoc>();

            string where = "";
            if (finder.nzp_type_doc > 0) where = " and nzp_type_doc = " + finder.nzp_type_doc;
            if (finder.nzp_doc_group > 0) where += " and nzp_doc_group = " + finder.nzp_doc_group;

            string sql = "select nzp_type_doc, doc_name from " + Points.Pref + "_kernel" + tableDelimiter +
                         "s_type_doc where 1=1 " + where;

            IDataReader reader = null;

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    TypeDoc type_doc = new TypeDoc();
                    if (reader["nzp_type_doc"] != DBNull.Value)
                        type_doc.nzp_type_doc = Convert.ToInt32(reader["nzp_type_doc"]);
                    if (reader["doc_name"] != DBNull.Value) type_doc.doc_name = Convert.ToString(reader["doc_name"]);
                    list.Add(type_doc);
                }
            }
            catch (Exception ex)
            {
                conn_db.Close();
                reader.Close();
                ret = new Returns(false, ex.Message);
                list = null;
                MonitorLog.WriteLog("Ошибка LoadTypeDoc " + (Constants.Viewerror ? "\n " + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
            }

            conn_db.Close();
            reader.Close();
            return list;
        }


        //заглушка для Informix
        public List<SaldoRep> FillRep_5_10(ChargeFind finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не указан пользователь";
                return null;
            }


            List<SaldoRep> list = new List<SaldoRep>();
            SaldoRep saldoRep;


            #region Подключение к БД

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata); //new IDbConnection(Constants.cons_Webdata);
            IDataReader reader;
            IDataReader reader2;

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            IDbConnection conn_db = GetConnection(Constants.cons_Kernel); //new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ",
                    MonitorLog.typelog.Error, true);
                conn_web.Close();
                ret.result = false;
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            StringBuilder sql2 = new StringBuilder();
#if PG
            string tXX_spls = "public." + "t" + finder.nzp_user.ToString() + "_spls";
#else
            string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + finder.nzp_user.ToString() + "_spls";
#endif
            string pref = "";

            #region Выборка по локальным банкам

            DataTable LocalTable = new DataTable();
            string whereStr = "";

            WhereStringForFindCommon(finder, "a", ref whereStr);


            #region Ограничения

            string where_wp = "";
            string where_supp = "";
            string where_serv = "";
            string where_area = "";
            string where_geu = "";
            string where_dom = "";
            bool has_spls = false; //по списку ЛС или по все ЛС(определяет параметр Constants.act_report_spis_gil_mod) 

            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                where_serv += " and nzp_serv in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_supp)
                                where_supp += " and nzp_supp in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_wp)
                                where_wp += " and nzp_wp in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_area)
                                where_area += " and k.nzp_area in (" + role.val + ") ";
                            if (role.kod == Constants.role_sql_geu)
                                where_geu += " and k.nzp_geu in (" + role.val + ") ";
                            if (role.kod == Constants.act_report_spis_gil_mod)
                                has_spls = (role.val != "" ? true : false); //если по списку ЛС - true


                        }
                    }
                }
            }
            if (finder.nzp_geu > 0) where_geu += " and k.nzp_geu=" + finder.nzp_geu.ToString();
            if (finder.nzp_area > 0) where_area += " and k.nzp_area=" + finder.nzp_area.ToString();
            if (finder.nzp_dom > 0) where_dom += " and k.nzp_dom=" + finder.nzp_dom.ToString();
            if (finder.nzp_ul > 0)
                where_dom += " and k.nzp_dom in (select nzp_dom from " +

#if PG
 conn_db.Database.Replace("_kernel", "data") + ".dom where nzp_ul=" + finder.nzp_ul.ToString() + ")";
#else
 conn_db.Database.Replace("_kernel", "data") + "@" +
            DBManager.getServer(conn_db) + ":dom where nzp_ul=" + finder.nzp_ul.ToString() + ")";
#endif

            #endregion

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged table t_svod( ");
            sql2.Append(" nzp_serv integer,");
            sql2.Append(" sum_insaldo Numeric(14,2),");
            sql2.Append(" sum_insaldo_k Numeric(14,2),");
            sql2.Append(" sum_insaldo_d Numeric(14,2),");
            sql2.Append(" sum_real Numeric(14,2),");
            sql2.Append(" reval Numeric(14,2),");
            sql2.Append(" real_charge Numeric(14,2),");
            sql2.Append(" sum_outsaldo Numeric(14,2),");
            sql2.Append(" sum_outsaldo_k Numeric(14,2),");
            sql2.Append(" sum_outsaldo_d Numeric(14,2),");
            sql2.Append(" sum_money Numeric(14,2))");
#else
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" nzp_serv integer,");
            sql2.Append(" sum_insaldo Decimal(14,2),");
            sql2.Append(" sum_insaldo_k Decimal(14,2),");
            sql2.Append(" sum_insaldo_d Decimal(14,2),");
            sql2.Append(" sum_real Decimal(14,2),");
            sql2.Append(" reval Decimal(14,2),");
            sql2.Append(" real_charge Decimal(14,2),");
            sql2.Append(" sum_outsaldo Decimal(14,2),");
            sql2.Append(" sum_outsaldo_k Decimal(14,2),");
            sql2.Append(" sum_outsaldo_d Decimal(14,2),");
            sql2.Append(" sum_money Decimal(14,2)) with no log");
#endif

            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            /*
             sql.Remove(0, sql.Length);
             sql.Append(" select count(*) as co ");
             sql.Append(" from  t" + finder.nzp_user.ToString() + "_spls ");

             if (!ExecRead(conn_web, out reader, sql.ToString(), false).result)
             {
                 has_spls = false;
             }
             else
                 if (reader.Read())
                 {
                     if (reader["co"] != DBNull.Value)
                         if (System.Convert.ToInt32(reader["co"]) > 0)
                             has_spls = true;

                 }
             */

            if (has_spls)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" select pref ");
                sql.Append(" from  t" + finder.nzp_user.ToString() + "_spls group by 1");
                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_web.Close();
                    conn_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

            }
            else
            {
                sql.Remove(0, sql.Length);
                sql.Append(" select bd_kernel as pref ");
#if PG
                sql.Append(" from public.s_point ");
#else
                sql.Append(" from  " + conn_db.Database + "@" + DBManager.getServer(conn_db) + ":s_point ");
#endif
                sql.Append(" where nzp_wp>1 " + where_wp);

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_web.Close();
                    conn_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
            }
            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    pref = Convert.ToString(reader["pref"]).Trim();
                    ExecRead(conn_db, out reader2, "drop table t1", false);
                    sql2.Remove(0, sql2.Length);



                    sql2.Append(" insert into t_svod(nzp_serv, sum_insaldo_k, sum_insaldo_d, ");
                    sql2.Append(" sum_insaldo, sum_real, reval, real_charge, sum_money, ");
                    sql2.Append(" sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)");
                    sql2.Append(
                        " select nzp_serv , sum(case when sum_insaldo<0 then sum_insaldo else 0 end) as sum_insaldo_k,");
                    sql2.Append(" sum(case when sum_insaldo<0 then 0 else sum_insaldo end) as sum_insaldo_d,");
                    sql2.Append(" sum(sum_insaldo) as sum_insaldo,");
                    sql2.Append(" sum(sum_real) as sum_real,");
                    sql2.Append(" sum(reval) as reval,");
                    sql2.Append(" sum(real_charge) as real_charge,");
                    sql2.Append(" sum(sum_money) as sum_money,");
                    sql2.Append(" sum(case when sum_outsaldo<0 then sum_outsaldo else 0 end) as sum_outsaldo_k,");
                    sql2.Append(" sum(case when sum_outsaldo<0 then 0 else sum_outsaldo end) as sum_outsaldo_d,");
                    sql2.Append(" sum(sum_outsaldo) as sum_outsaldo");
                    if (has_spls)
                    {
#if PG
                        sql2.Append(" from " + pref + "_charge_" + (finder.year_ - 2000).ToString("00") + ".charge_");
#else
                        sql2.Append(" from " + pref + "_charge_" + (finder.year_ - 2000).ToString("00") + ":charge_");
#endif
                        sql2.Append(finder.month_.ToString("00") + " a, " + tXX_spls + " k ");
                        sql2.Append(" where a.nzp_kvar=k.nzp_kvar and  nzp_serv >1 and dat_charge is null " + whereStr);
                        sql2.Append(" group by 1");

                    }
                    else
                    {
#if PG
                        sql2.Append(" from " + pref + "_charge_" + (finder.year_ - 2000).ToString("00") + ".charge_");
                        sql2.Append(finder.month_.ToString("00") + " a, " + pref + "_data.kvar k ");
#else
                        sql2.Append(" from " + pref + "_charge_" + (finder.year_ - 2000).ToString("00") + ":charge_");
                        sql2.Append(finder.month_.ToString("00") + " a, " + pref + "_data:kvar k ");
#endif
                        sql2.Append(" where a.nzp_kvar=k.nzp_kvar and  nzp_serv >1 and dat_charge is null " + whereStr);
                        sql2.Append(where_area + where_geu + where_dom);
                        sql2.Append(" group by 1");

                    }

                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        conn_web.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }
                }
            }

            #endregion

            reader.Close();

            sql2.Remove(0, sql2.Length);
            sql2.Append(" select service , trim(sum(sum_insaldo_k)::char(50)) as sum_insaldo_k,");
            sql2.Append(" trim(sum(sum_insaldo_d)::character(50)) as sum_insaldo_d,");
            sql2.Append(" trim(sum(sum_insaldo)::character(50)) as sum_insaldo,");
            sql2.Append(" trim(sum(sum_real)::character(50)) as sum_real,");
            sql2.Append(" trim(sum(reval)::character(50)) as reval,");
            sql2.Append(" trim(sum(real_charge)::character(50)) as real_charge,");
            sql2.Append(" trim(sum(sum_money)::character(50)) as sum_money,");
            sql2.Append(" trim(sum(sum_outsaldo_k)::character(50)) as sum_outsaldo_k,");
            sql2.Append(" trim(sum(sum_outsaldo_d)::character(50)) as sum_outsaldo_d,");
            sql2.Append(" trim(sum(sum_outsaldo)::character(50)) as sum_outsaldo");
            sql2.Append(" from t_svod a, ");
#if PG
            sql2.Append("public.services s ");
#else
            sql2.Append(conn_db.Database + "@" + DBManager.getServer(conn_db) + ":services s ");
#endif
            sql2.Append(" where a.nzp_serv=s.nzp_serv");
            sql2.Append(" group by service ");
            sql2.Append(" order by service ");
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                return null;
            }
            while (reader2.Read())
            {
                saldoRep = new SaldoRep();
                if (reader2["service"] != DBNull.Value) saldoRep.service = Convert.ToString(reader2["service"]).Trim();
                if (reader2["sum_insaldo_k"] != DBNull.Value)
                    saldoRep.sum_insaldo_k = Decimal.Parse(reader2["sum_insaldo_k"].ToString().Replace(".", ","));
                // Convert.ToDecimal(reader2["sum_insaldo_k"]);
                if (reader2["sum_insaldo_d"] != DBNull.Value)
                    saldoRep.sum_insaldo_d = Decimal.Parse(reader2["sum_insaldo_d"].ToString().Replace(".", ","));
                if (reader2["sum_insaldo"] != DBNull.Value)
                    saldoRep.sum_insaldo = Decimal.Parse(reader2["sum_insaldo"].ToString().Replace(".", ","));
                if (reader2["sum_real"] != DBNull.Value)
                    saldoRep.sum_real = Decimal.Parse(reader2["sum_real"].ToString().Replace(".", ","));
                if (reader2["reval"] != DBNull.Value)
                    saldoRep.reval = Decimal.Parse(reader2["reval"].ToString().Replace(".", ","));
                if (reader2["real_charge"] != DBNull.Value)
                    saldoRep.real_charge = Decimal.Parse(reader2["real_charge"].ToString().Replace(".", ","));
                if (reader2["sum_money"] != DBNull.Value)
                    saldoRep.sum_money = Decimal.Parse(reader2["sum_money"].ToString().Replace(".", ","));
                if (reader2["sum_outsaldo_k"] != DBNull.Value)
                    saldoRep.sum_outsaldo_k = Decimal.Parse(reader2["sum_outsaldo_k"].ToString().Replace(".", ","));
                if (reader2["sum_outsaldo_d"] != DBNull.Value)
                    saldoRep.sum_outsaldo_d = Decimal.Parse(reader2["sum_outsaldo_d"].ToString().Replace(".", ","));
                if (reader2["sum_outsaldo"] != DBNull.Value)
                    saldoRep.sum_outsaldo = Decimal.Parse(reader2["sum_outsaldo"].ToString().Replace(".", ","));


                list.Add(saldoRep);
            }
            reader2.Close();
            ExecRead(conn_db, out reader2, "drop table t_svod", true);
            conn_db.Close();
            conn_web.Close();

            return list;
        }

        public decimal GetSumKOplate(Saldo finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return 0;

#if PG
            string table = finder.pref + "_charge_" + finder.year_.ToString("0000").Substring(2, 2) + ".charge_" +
                           finder.month_.ToString("00");
#else
            string table = finder.pref + "_charge_" + finder.year_.ToString("0000").Substring(2, 2) + ":charge_" + finder.month_.ToString("00");
#endif
            if (!TempTableInWebCashe(conn_db, table))
            {
                ret = new Returns(false, "Начислений не найдено", -1);
                conn_db.Close();
                return 0;
            }

            string sql = "select  sum(sum_charge) as sum_charge from " + table +
                         " where dat_charge is null and nzp_serv > 1 and nzp_kvar = " + finder.nzp_kvar;
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return 0;
            }
            decimal sumKOplate = 0;
            if (reader.Read())
                if (reader["sum_charge"] != DBNull.Value) sumKOplate = Convert.ToDecimal(reader["sum_charge"]);

            reader.Close();
            conn_db.Close();
            return sumKOplate;
        }

        private IntfResultType DistrSumLoadSended(IDbConnection connectionID, string tshutable, decimal sum_distr,
            string where, int fdNum, string sum_field)
        {
            IntfResultType intfRes = new IntfResultType();

            try
            {
                string sql = "drop table tmp_sum_send";
                ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);

                sql = "create temp table tmp_sum_send (" +
                      " id           integer, " +
                      " sum_distr    " + sDecimalType + "(14,2), " +
                      " sum_must     " + sDecimalType + "(14,2), " +
                      " sum_must_tot " + sDecimalType + "(14,2), " +
                      " coeff float, " +
                      " sum_send " + sDecimalType + "(14,2), " +
                      " sum_send_tot " + sDecimalType + "(14,2) " +
                      ")" + (DBManager.tableDelimiter == ":" ? " with no log" : "");
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.0." + intfRes.resultMessage);

                sql = " insert into tmp_sum_send (id, sum_must, sum_distr) " +
                      " select ordering, " + sum_field + ", " + sum_distr + " from " + tshutable + " where cnt_fd > 0 " +
                      (where != "" ? " and " + where : "");
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.1." + intfRes.resultMessage);

                sql = "drop table tmp_sum_must_tot";
                ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);

                sql = "create temp table tmp_sum_must_tot (" +
                      " sum_must_tot " + sDecimalType + "(14,2) " +
                      ")" + (DBManager.tableDelimiter == ":" ? " with no log" : "");
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.1." + intfRes.resultMessage);

                sql = " insert into tmp_sum_must_tot (sum_must_tot) " +
                      " select sum(sum_must) from tmp_sum_send ";
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.2." + intfRes.resultMessage);

                sql = " update tmp_sum_send set sum_must_tot =  (select sum_must_tot from tmp_sum_must_tot)";
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.3." + intfRes.resultMessage);

                sql = " update tmp_sum_send set coeff = sum_must / sum_must_tot";
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.4." + intfRes.resultMessage);

                sql = " update tmp_sum_send set sum_send = coeff * sum_distr";
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.5." + intfRes.resultMessage);

                //
                sql = "drop table tmp_sum_send_tot";
                ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);

                sql = "create temp table tmp_sum_send_tot (sum_send_tot " + sDecimalType + "(14,2))" +
                      (DBManager.tableDelimiter == ":" ? " with no log" : "");
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.6." + intfRes.resultMessage);

                sql = " insert into tmp_sum_send_tot (sum_send_tot) select sum(sum_send) from tmp_sum_send ";
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.7." + intfRes.resultMessage);

                sql = " update tmp_sum_send set sum_send_tot =  (select sum_send_tot from tmp_sum_send_tot)";
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.8." + intfRes.resultMessage);

                //
                sql = " select " + (DBManager.tableDelimiter == ":" ? " first 1 " : "") + " id from tmp_sum_send " +
                      " order by sum_send desc " + (DBManager.tableDelimiter == "." ? " limit 1 " : "");
                IntfResultTableType tab = ClassDBUtils.OpenSQL(sql, connectionID);
                if (tab.resultCode < 0) throw new Exception("2.10." + intfRes.resultMessage);

                sql = " update tmp_sum_send set sum_send =  sum_send + sum_distr - sum_send_tot " +
                      " where id = " + tab.resultData.Rows[0]["id"].ToString();
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.12." + intfRes.resultMessage);

                sql = " update " + tshutable + " set sum_send_" + fdNum +
                      " = (select t.sum_send from tmp_sum_send t where t.id = ordering) " +
                      " where ordering in (select t.id from tmp_sum_send t) ";
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0) throw new Exception("2.13." + intfRes.resultMessage);

                return intfRes;
            }
            catch (Exception ex)
            {
                return new IntfResultType(-1, ex.Message);
            }
        }


        public ReturnsObjectType<DataTable> LoadSended(MoneySended finder, IDbConnection connectionID)
        {
            if (!(finder.nzp_user > 0))
                return new ReturnsObjectType<DataTable>(null, false, "Не задан пользователь", -1);
            if (finder.nzp_supp < 1 && finder.nzp_payer < 1)
                return new ReturnsObjectType<DataTable>(null, false, "Не определен договор или поставщик", -1);
            DateTime dat_oper = DateTime.MinValue;
            if (!DateTime.TryParse(finder.dat_oper, out dat_oper))
            {
                return new ReturnsObjectType<DataTable>(null, false, "Не задана дата операции", -1);
            }
            DataTable table = null;
            DataTable table2 = null;
            string sql = "";

            // Ограничения пользователя
            string sqlRoleFilter = "";
            string strKeys = "";
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        strKeys = role.val.Trim(new char[] { ' ', ',' });
                        //if (role.kod == Constants.role_sql_area)
                        //    sqlRoleFilter += " and d.nzp_area in (" + strKeys + ") ";
                        if (role.kod == Constants.role_sql_payer)
                            sqlRoleFilter += " and d.nzp_payer in (" + strKeys + ") ";
                    }
                }
            }

            if (finder.pref.Trim() == "") finder.pref = Points.Pref;
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return new ReturnsObjectType<DataTable>(null, false, ret.text, ret.tag);

            string sended = finder.pref + "_fin_" + (Convert.ToDateTime(finder.dat_oper).Year % 100).ToString("00") +
                            tableDelimiter + "fn_sended";
            string distrib = Points.Pref + "_fin_" + (Convert.ToDateTime(finder.dat_oper).Year % 100).ToString("00") +
                             tableDelimiter + "fn_distrib_dom_" +
                             (Convert.ToDateTime(finder.dat_oper).Month % 100).ToString("00");
            string dogovor = finder.pref + "_data" + tableDelimiter + "fn_dogovor";
            string osnov = finder.pref + "_data" + tableDelimiter + "fn_osnov";
            string bank = finder.pref + "_data" + tableDelimiter + "fn_bank";
            string supp = Points.Pref + "_kernel" + tableDelimiter + "supplier";
            string services = finder.pref + "_kernel" + tableDelimiter + "services";
            string payer = finder.pref + "_kernel" + tableDelimiter + "s_payer";
            string dogovor_bank = Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor_bank";

            string s_bank = Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_bank";

            string tshutable = "tshu_" + finder.nzp_user.ToString().Trim() + "_fn_sended";
            // Создать временную таблицу
            sql = " drop table " + tshutable;
            ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);
            sql = " drop table " + tshutable.Trim() + "_p";
            ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);

            sql = " select d.nzp_supp, d.nzp_payer, count(*) cnt, " +
                  sNvlWord + "((select " + sNvlWord + "(v.priznak_perechisl, 1) from " + dogovor +
                  " v where v.nzp_fd = (select min(a.nzp_fd) from " + dogovor +
                  " a where a.nzp_payer = d.nzp_payer and a.nzp_supp = d.nzp_supp)), 0) as osn_priznak " +
                  (tableDelimiter == "." ? " into temp " + tshutable.Trim() + "_p " : "") +
                  " from " + dogovor + " d where 1=1 " + sqlRoleFilter;
            if (finder.nzp_supp > 0) sql += " and d.nzp_supp = " + finder.nzp_supp.ToString();
            if (finder.nzp_payer > 0) sql += " and d.nzp_payer = " + finder.nzp_payer.ToString();
            if (finder.nzp_fd > 0) sql += " and d.nzp_fd = " + finder.nzp_fd.ToString();
            sql += " group by 1,2 order by 3 desc " +
                   (tableDelimiter == ":" ? "into temp " + tshutable.Trim() + "_p with no log" : "");

            IntfResultType intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
            if (intfRes.resultCode < 0)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false,
                    "Ошибка создания временой таблицы" + "\n" + intfRes.resultMessage, -1);
            }

            sql = " select MAX(cnt) mcnt from " + tshutable.Trim() + "_p";
            IntfResultTableType intfResTable = ClassDBUtils.OpenSQL(sql, connectionID);
            if (intfResTable.resultCode < 0)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false,
                    "Ошибка получения количества договоров" + "\n" + intfResTable.resultMessage, -1);
            }
            Int32 cntFd = 0;

            if ((intfResTable.resultData != null) && (intfResTable.resultData.Rows.Count > 0))
                if (intfResTable.resultData.Rows[0]["mcnt"].ToString().Trim() != "")
                {
                    cntFd = Convert.ToInt32(intfResTable.resultData.Rows[0]["mcnt"]);
                }

            sql = " create temp table " + tshutable + "( " +
                  "   ordering SERIAL,           " +
                  "   dat_oper DATE,             " +
                  "   nzp_supp INTEGER,          " +
                  "   name_supp VARCHAR(100),    " +
                  "   nzp_serv INTEGER,          " +
                  "   service VARCHAR(100),      " +
                  "   nzp_payer INTEGER,         " +
                  "   payer VARCHAR(200),        " +
                  "   sum_charge " + sDecimalType + "(14,2),    " +
                  "   sum_must " + sDecimalType + "(14,2),    " +
                  "   sum_send " + sDecimalType + "(14,2),    " +
                  "   cnt_fd INTEGER DEFAULT 0,  " +
                  "   sum_send_fd " + sDecimalType + "(14,2), " +
                  "   osn_priznak INTEGER, " +
                  "   sum_bank " + sDecimalType + "(14,2) default 0.00, " +
                  "   sum_send_p " + sDecimalType + "(14,2) default 0.00, " +
                  "   priznak INTEGER default 0";
            for (int iCount = 1; iCount <= cntFd; iCount++)
            {
                sql +=
                    " ,nzp_fd_" + iCount.ToString() + " INTEGER, sum_send_" + iCount.ToString() + " " + sDecimalType +
                    "(13,2) DEFAULT 0.00, " +
                    " s_osnov_" + iCount.ToString() + " VARCHAR(200), osnov_" + iCount.ToString() +
                    " VARCHAR(60), num_dog_" +
                    iCount.ToString() + " VARCHAR(20), dat_dog_" + iCount.ToString() + " DATE, rs_bank_" +
                    iCount.ToString() +
                    " VARCHAR(200), rcount_" + iCount.ToString() + " VARCHAR(30), bank_name_" + iCount.ToString() +
                    " VARCHAR(100), " +
                    " npunkt_" + iCount.ToString() + " VARCHAR(60), target_" + iCount.ToString() + " VARCHAR(200), pp_" +
                    iCount.ToString() + " VARCHAR(40), num_pp_" + iCount.ToString() + " INTEGER, dat_pp_" +
                    iCount.ToString() + " DATE, " +
                    " max_sum_" + iCount.ToString() + " " + sDecimalType + "(14,2), " +
                    " min_sum_" + iCount.ToString() + " " + sDecimalType + "(14,2), " +
                    " sum_serv_" + iCount + " " + sDecimalType + "(14,2) default 0.00, " +
                    " bank_cnt_" + iCount.ToString() + " integer ";
            }
            sql += ")" + (tableDelimiter == ":" ? " WITH NO LOG " : "");

            intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
            if (intfRes.resultCode < 0)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false,
                    "Ошибка создания временой таблицы" + "\n" + intfRes.resultMessage, -1);
            }

            sql =
                " insert into " + tshutable +
                "(ordering, dat_oper, nzp_supp, name_supp, nzp_serv, service, nzp_payer, payer, sum_charge, sum_must, sum_send, cnt_fd, sum_send_fd, osn_priznak)  " +
                " select 0, d.dat_oper, d.nzp_supp, a.name_supp," +
                // услуга
                " case when " + sNvlWord + "(pp.osn_priznak, 0) > 1 then d.nzp_serv else 0 end, " +
                " case when " + sNvlWord + "(pp.osn_priznak, 0) > 1 then s.service else '' end, " +
                // поставщик
                " d.nzp_payer, p.payer, " +
                " sum(" + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord + "(d.sum_reval, 0)), " +
                " sum(" + sNvlWord + "(d.sum_in, 0) + " + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord +
                "(d.sum_reval, 0)), " +
                " sum(d.sum_send), " + // исправил Андрей К. 21.04.2013
                " pp.cnt, 0, " +
                sNvlWord + "(pp.osn_priznak, 0) " +
                " from " + supp + " a, " + services + " s, " + payer + " p," + distrib + " d " +
                " left outer join " + tshutable.Trim() +
                "_p pp on pp.nzp_supp = d.nzp_supp and pp.nzp_payer = d.nzp_payer " +
                " where 1=1 and d.nzp_supp = a.nzp_supp and d.nzp_serv = s.nzp_serv and d.nzp_payer = p.nzp_payer " +
                " " + sqlRoleFilter + " and d.dat_oper = " +
                Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString());
            if (finder.nzp_supp > 0) sql += " and d.nzp_supp = " + finder.nzp_supp.ToString();
            if (finder.nzp_payer > 0) sql += " and d.nzp_payer = " + finder.nzp_payer.ToString();
            if (finder.nzp_serv > 0) sql += " and d.nzp_serv = " + finder.nzp_serv.ToString();
            if (finder.nzp_fd > 0) sql += " and d.nzp_fd = " + finder.nzp_fd.ToString();
            sql += " group by 1,2,3,4,5,6,7,8,12,13,14 ";

            intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
            if (intfRes.resultCode < 0)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false,
                    "Ошибка выбора данных" + "\n" + intfRes.resultMessage, -1);
            }
            sql = " create index tix_" + tshutable + "_01 on " + tshutable + " (dat_oper) ";
            ClassDBUtils.ExecSQL(sql, connectionID);
            sql = " create index tix_" + tshutable + "_02 on " + tshutable + " (nzp_supp) ";
            ClassDBUtils.ExecSQL(sql, connectionID);
            sql = " create index tix_" + tshutable + "_03 on " + tshutable + " (nzp_serv) ";
            ClassDBUtils.ExecSQL(sql, connectionID);
            sql = " create index tix_" + tshutable + "_04 on " + tshutable + " (nzp_payer) ";
            ClassDBUtils.ExecSQL(sql, connectionID);
            sql = " create index tix_" + tshutable + "_05 on " + tshutable + " (ordering) ";
            ClassDBUtils.ExecSQL(sql, connectionID);

            sql = DBManager.sUpdStat + " " + tshutable;
            ClassDBUtils.ExecSQL(sql, connectionID);

            sql = " select * from " + tshutable;
            intfResTable = ClassDBUtils.OpenSQL(sql, connectionID);
            if (intfResTable.resultCode < 0)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false,
                    "Ошибка получения данных" + "\n" + intfResTable.resultMessage, -1);
            }
            table = intfResTable.GetData();

            Int32 osn_priznak = 0;

            foreach (DataRow dr in table.Rows)
            {
                osn_priznak = Convert.ToInt32(dr["osn_priznak"]);

                sql = " select distinct v.nzp_fd, sum(" + DBManager.sNvlWord + "(d.sum_send,0)) sum_send, " +
                      "   trim(" + DBManager.sNvlWord + "(c.osnov,' '))||' № '||trim(" + DBManager.sNvlWord +
                      "(v.num_dog,' '))||trim(case when v.dat_dog is null then '' else ' от '|| v.dat_dog end) s_osnov, " +
                      "   c.osnov, v.num_dog, v.dat_dog, trim(" + DBManager.sNvlWord +
                      "(b.bank_name,' '))||' р/с '||trim(" + DBManager.sNvlWord +
                      "(b.rcount,' ')) rs_bank, b.rcount, b.bank_name, b.npunkt, " +
                      "   v.target, d.num_pp, d.dat_pp, v.max_sum, v.min_sum, " +
                      "   (select count(*) from " + dogovor_bank + " fdb where v.nzp_fd = fdb.nzp_fd) as bank_cnt " +
#if PG
 " from " + bank + " b, " + dogovor + " v " +
                      " left outer join " + sended + " d on d.nzp_fd = v.nzp_fd and d.dat_oper = " +
                      Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()) + " and d.nzp_serv = " +
                      Convert.ToInt32(dr["nzp_serv"]).ToString() +
                      " left outer join " + osnov + " c on v.nzp_osnov = c.nzp_osnov " +
                      " where 1=1 and v.nzp_fb = b.nzp_fb " +
#else
                    " from " + dogovor + " v, " + bank + " b, outer " + sended + " d, outer " + osnov + " c " +
                    " where 1=1 and d.nzp_fd = v.nzp_fd and v.nzp_fb = b.nzp_fb and v.nzp_osnov = c.nzp_osnov " +
                    " and d.dat_oper = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()) +
#endif
 " " + sqlRoleFilter +
                      " and v.nzp_payer = " + Convert.ToInt32(dr["nzp_payer"]).ToString() +
                      " and v.nzp_supp = " + Convert.ToInt32(dr["nzp_supp"]).ToString() +
                      (osn_priznak > 1 ? " and d.nzp_serv = " + Convert.ToInt32(dr["nzp_serv"]) : "") +
                      " group by 1,3,4,5,6,7,8,9,10,11,12,13,14,15 order by 1 ";

                intfResTable = ClassDBUtils.OpenSQL(sql, connectionID);
                if (intfResTable.resultCode < 0)
                {
                    conn_db.Close();
                    return new ReturnsObjectType<DataTable>(null, false,
                        "Ошибка получения данных" + "\n" + intfResTable.resultMessage, -1);
                }
                DataTable tableSend = intfResTable.GetData();
                if (tableSend == null)
                {
                    conn_db.Close();
                    return new ReturnsObjectType<DataTable>(null, false, "Ошибка получения данных", -1);
                }

                Int32 ordering = Convert.ToInt32(dr["ordering"]);
                int iNumRow = 0;
                foreach (DataRow drSend in tableSend.Rows)
                {
                    iNumRow++;
                    if (iNumRow > cntFd) break;

                    string sqlSend = "";

                    sqlSend = " update  " + tshutable + " set (nzp_fd_" + iNumRow.ToString() + ", sum_send_" +
                              iNumRow.ToString() + ", s_osnov_" + iNumRow.ToString() + ", " +
                              " osnov_" + iNumRow.ToString() + ", num_dog_" + iNumRow.ToString() + ", dat_dog_" +
                              iNumRow.ToString() + ", rs_bank_" + iNumRow.ToString() + ", " +
                              " rcount_" + iNumRow.ToString() + ", bank_name_" + iNumRow.ToString() + ", npunkt_" +
                              iNumRow.ToString() + ", target_" + iNumRow.ToString() + ", " +
                              " num_pp_" + iNumRow.ToString() + ", dat_pp_" + iNumRow.ToString() + ", " +
                              " max_sum_" + iNumRow.ToString() + ", min_sum_" + iNumRow.ToString() + ", " +
                              " bank_cnt_" + iNumRow.ToString() + ") =  " +
                              " (" + Utils.EStrNull(Convert.ToString(drSend["nzp_fd"])) + ", " +
                              Utils.EStrNull(Convert.ToString(drSend["sum_send"])) + ", " +
                              Utils.EStrNull(Convert.ToString(drSend["s_osnov"])) + ", " +
                              Utils.EStrNull(Convert.ToString(drSend["osnov"])) + ", " +
                              Utils.EStrNull(Convert.ToString(drSend["num_dog"])) + ", ";
                    if (drSend["dat_dog"] == DBNull.Value) sqlSend += " NULL, ";
                    else sqlSend += Utils.EStrNull(Convert.ToDateTime(drSend["dat_dog"]).ToShortDateString()) + ", ";
                    sqlSend += Utils.EStrNull(Convert.ToString(drSend["rs_bank"])) + ", " +
                               Utils.EStrNull(Convert.ToString(drSend["rcount"])) + ", " +
                               Utils.EStrNull(Convert.ToString(drSend["bank_name"])) + ", " +
                               Utils.EStrNull(Convert.ToString(drSend["npunkt"])) + ", " +
                               Utils.EStrNull(Convert.ToString(drSend["target"])) + ", " +
                               Utils.EStrNull(Convert.ToString(drSend["num_pp"])) + ", ";
                    // дата платежного поручения: если не заполнена, то выставлять текущий операционный день
                    if (drSend["dat_pp"] == DBNull.Value)
                        sqlSend += Utils.EStrNull(Points.DateOper.ToShortDateString()) + ", ";
                    else sqlSend += Utils.EStrNull(Convert.ToDateTime(drSend["dat_pp"]).ToShortDateString()) + ", ";
                    // лимит
                    if (drSend["max_sum"] == DBNull.Value) sqlSend += " NULL, ";
                    else sqlSend += Convert.ToDecimal(drSend["max_sum"]) + ", ";
                    // минимальная сумма к перечислению
                    if (drSend["min_sum"] == DBNull.Value) sqlSend += " NULL, ";
                    else sqlSend += Convert.ToDecimal(drSend["min_sum"]) + ", ";
                    // количество банков, через которые должны перечисляться деньги по договору
                    if (drSend["bank_cnt"] == DBNull.Value) sqlSend += " NULL) ";
                    else sqlSend += Convert.ToInt32(drSend["bank_cnt"]) + ") ";

                    sqlSend += " where ordering = " + Convert.ToInt32(dr["ordering"]).ToString();
                    intfRes = ClassDBUtils.ExecSQL(sqlSend, connectionID);
                    if (intfRes.resultCode < 0)
                    {
                        conn_db.Close();
                        return new ReturnsObjectType<DataTable>(null, false,
                            "Ошибка обновления данных" + "\n" + intfRes.resultMessage, -1);
                    }

                }
            }

            if (cntFd > 0)
            {
                sql = " update " + tshutable + " set sum_send_fd = ";
                for (int iCntSum = 1; iCntSum <= cntFd; iCntSum++)
                {
                    sql += " sum_send_" + iCntSum.ToString();
                    if (iCntSum < cntFd) sql += " + ";
                }
                intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                if (intfRes.resultCode < 0)
                {
                    conn_db.Close();
                    return new ReturnsObjectType<DataTable>(null, false,
                        "Ошибка получения суммы по договорам" + "\n" + intfRes.resultMessage, -1);
                }
            }

            sql = " select count(*) as cnt from " + tshutable;
            intfResTable = ClassDBUtils.OpenSQL(sql, connectionID);
            if (intfResTable.resultCode < 0)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false,
                    "Ошибка получения количества записей" + "\n" + intfResTable.resultMessage, -1);
            }
            table = intfResTable.GetData();
            Int32 cntRecord = Convert.ToInt32(table.Rows[0]["cnt"]);

            #region 1. Объединить суммы по услугам для договоров, у которых не установлен признак "Перечислять в разрезе услуг"

            //--------------------------------------------------------------------------------------------------------------------------------------------------------------
            if (cntFd > 0)
            {
                try
                {
                    sql = "drop table tmp_target";
                    ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);

                    sql = "create temp table tmp_target (ordering integer, target VARCHAR(200))" +
                          (DBManager.tableDelimiter == ":" ? " with no log" : "");
                    intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                    if (intfRes.resultCode < 0) throw new Exception("1.1." + intfRes.resultMessage);

                    sql = "insert into tmp_target (ordering, target) " +
                          " select ordering, target_1 from " + tshutable +
                          " where osn_priznak = 1 and cnt_fd > 0 ";
                    intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                    if (intfRes.resultCode < 0) throw new Exception("1.2." + intfRes.resultMessage);

                    sql = "update " + tshutable +
                          " set service = (select a.target from tmp_target a where a.ordering = ordering), nzp_serv = 0 " +
                          " where ordering in (select a.ordering from tmp_target a)";
                    intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                    if (intfRes.resultCode < 0) throw new Exception("1.3." + intfRes.resultMessage);
                }
                catch (Exception ex)
                {
                    conn_db.Close();
                    return new ReturnsObjectType<DataTable>(null, false, ex.Message, -1);
                }
            }
            //--------------------------------------------------------------------------------------------------------------------------------------------------------------

            #endregion

            #region 2. Распределить суммы (кнопка "Распределить суммы")

            //--------------------------------------------------------------------------------------------------------------------------------------------------------------
            if (finder.sum_send > 0 && cntFd > 0)
            {
                try
                {
                    intfRes = DistrSumLoadSended(connectionID, tshutable, finder.sum_send, "", 1, "sum_must");
                    if (intfRes.resultCode < 0) throw new Exception(intfRes.resultMessage);
                }
                catch (Exception ex)
                {
                    conn_db.Close();
                    return new ReturnsObjectType<DataTable>(null, false, ex.Message, -1);
                }
            }
            //--------------------------------------------------------------------------------------------------------------------------------------------------------------

            #endregion

            #region 3. Зачислить суммы

            //--------------------------------------------------------------------------------------------------------------------------------------------------------------
            try
            {
                #region Зачислить суммы за опер. день или зачислить все

                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (cntFd > 0 && finder.copy_id > 0)
                {
                    string sql_sum = "";

                    // зачесть за опер. день
                    if (finder.copy_id == 1)
                        sql_sum = "sum(" + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord + "(d.sum_reval, 0))";
                    // зачесть все
                    else
                        sql_sum = "sum(" + sNvlWord + "(d.sum_in, 0) + " + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord +
                                  "(d.sum_reval, 0))";

                    for (int i = 1; i <= cntFd; i++)
                    {
                        // определить суммы по договорам и банкам, которые указаны в договорах
                        sql = "drop table tmp_fd_sum_send";
                        ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);

                        sql = " create temp table tmp_fd_sum_send (ordering integer, fd_sum_send " + sDecimalType +
                              " (14,2) ) " +
                              (DBManager.tableDelimiter == ":" ? " with no log" : "");

                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.1." + intfRes.resultMessage);

                        sql = " insert into tmp_fd_sum_send (ordering, fd_sum_send) " +
                              " select t.ordering, " + sql_sum +
                              " from " + distrib + " d, " + tshutable + " t, " + dogovor_bank + " fdb, " + s_bank +
                              " b " +
                              " where t.nzp_fd_" + i + " = fdb.nzp_fd " +
                              "   and fdb.nzp_bank = b.nzp_bank " +
                              "   and b.nzp_payer  = d.nzp_bank " +
                              "   and d.dat_oper = " +
                              Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()) +
                              "   and d.nzp_supp = t.nzp_supp " +
                              "   and d.nzp_payer = t.nzp_payer " +
                              "   and d.nzp_serv = (case when t.nzp_serv > 0 then t.nzp_serv else d.nzp_serv end) " +
                              "   and t.bank_cnt_" + i + " > 0" +
                              " group by 1";

                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.1." + intfRes.resultMessage);

                        sql = " update " + tshutable + " set sum_send_" + i +
                              " = (select a.fd_sum_send from tmp_fd_sum_send a where ordering = a.ordering) " +
                              " where ordering in (select a.ordering from tmp_fd_sum_send a) ";
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.1." + intfRes.resultMessage);
                    }

                    for (int i = 1; i <= cntFd; i++)
                    {
                        // cобрать все суммы по банкам
                        sql = "update " + tshutable + " set " +
                              " sum_bank = sum_bank + sum_send_" + i;
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.1." + intfRes.resultMessage);
                    }

                    // определить сумму, которая не распределяется по банкам
                    sql = "update " + tshutable + " set sum_send_p = " +
                          (finder.copy_id == 1 ? "sum_charge" : "sum_must") + " - sum_bank";
                    intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                    if (intfRes.resultCode < 0) throw new Exception("3.2." + intfRes.resultMessage);

                    // положить эту нераспределенную сумму в первый договор, где не указаны банки 
                    // и в который еще не сохранили нераспределенную сумму (priznak = 0)
                    for (int i = 1; i <= cntFd; i++)
                    {
                        sql = "drop table tmp_ordering_sum_send";
                        ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);

                        sql = " create temp table tmp_ordering_sum_send (ordering integer) " +
                              (DBManager.tableDelimiter == ":" ? " with no log" : "");

                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.3." + intfRes.resultMessage);

                        // определить нужные договоры 
                        sql = "insert into tmp_ordering_sum_send (ordering) " +
                              "select ordering from " + tshutable + " where bank_cnt_" + i + " = 0 and priznak = 0";
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.4." + intfRes.resultMessage);

                        // сохранить сумму
                        sql = "update " + tshutable + " set sum_send_" + i + " = sum_send_p, priznak = 1 " +
                              " where ordering in (select ordering from tmp_ordering_sum_send) ";
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.5." + intfRes.resultMessage);
                    }
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------

                #endregion

                #region ограничить суммы верхним и нижним потолком

                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (cntFd > 0 && (finder.sum_send > 0 || finder.copy_id > 0))
                {
                    for (int i = 1; i <= cntFd; i++)
                    {
                        // ограничить суммы по договорам, у которых нет признака "Перечислять в разрезе услуг"
                        sql = "update " + tshutable + " set sum_send_" + i + " = (case when sum_send_" + i +
                              " < min_sum_" + i + " and min_sum_" + i + " > 0 then 0.00 " +
                              " else case when sum_send_" + i + " > max_sum_" + i + " and max_sum_" + i +
                              " > 0 then max_sum_" + i + " " +
                              " else sum_send_" + i + " end end) " +
                              "where cnt_fd > 0 and osn_priznak = 1";
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.5." + intfRes.resultMessage);

                        // по договорам, у которых перечисление в разрезе услуг
                        sql = "drop table tmp_sum_serv";
                        ClassDBUtils.ExecSQL(sql, connectionID, ClassDBUtils.ExecMode.Log);

                        // получить коды УК и поставщиков и итоговые суммы по услугам
                        sql = "create temp table tmp_sum_serv (" +
                              " supp_id integer, payer_id integer, serv_sum " + sDecimalType + "(14,2))" +
                              (DBManager.tableDelimiter == ":" ? " with no log" : "");
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.2.1." + intfRes.resultMessage);

                        sql = " insert into tmp_sum_serv (supp_id, payer_id, serv_sum) " +
                              " select nzp_supp, nzp_payer, sum(" + DBManager.sNvlWord + "(sum_send_" + i +
                              ", 0.00)) from " + tshutable +
                              " where cnt_fd > 0 and osn_priznak = 2 " +
                              " group by 1,2";
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.2.2." + intfRes.resultMessage);

                        // cохранить итоговые суммы по услугам
                        sql = " update " + tshutable + " set " +
                              " sum_serv_" + i + "  = (select t.serv_sum from tmp_sum_serv t where t.payer_id = " +
                              tshutable + ".nzp_payer and t.supp_id = " + tshutable + ".nzp_supp) " +
                              " where nzp_supp in (select supp_id from tmp_sum_serv) " +
                              "   and nzp_payer in (select payer_id from tmp_sum_serv) ";
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.2.3." + intfRes.resultMessage);

                        // ограничить суммы нижним потолком
                        sql = " update " + tshutable + " set sum_send_" + i + " = 0 " +
                              " where sum_serv_" + i + "  < min_sum_" + i + " and osn_priznak = 2";
                        intfRes = ClassDBUtils.ExecSQL(sql, connectionID);
                        if (intfRes.resultCode < 0) throw new Exception("3.2.4." + intfRes.resultMessage);

                        // получить коды поставщиков и УК и верхние потолки
                        sql = "select distinct nzp_payer, nzp_sup, max_sum_" + i + " from " + tshutable +
                              " where sum_serv_" + i + " > max_sum_" + i + " and osn_priznak = 2";
                        intfResTable = ClassDBUtils.OpenSQL(sql, connectionID);
                        if (intfResTable.resultCode < 0) throw new Exception("3.2.5." + intfRes.resultMessage);
                        table2 = intfResTable.GetData();

                        for (int j = 0; j < table2.Rows.Count; j++)
                        {
                            int nzp_payer = Convert.ToInt32(table2.Rows[j]["nzp_payer"]);
                            int nzp_supp = Convert.ToInt32(table2.Rows[j]["nzp_supp"]);
                            decimal max_sum = Convert.ToDecimal(table2.Rows[j]["max_sum_" + i]);

                            // выполнить перераспределение по услугам, ограничившись верхним потолком
                            intfRes = DistrSumLoadSended(connectionID, tshutable, max_sum,
                                "nzp_payer = " + nzp_payer + " and nzp_supp = " + nzp_supp, i, "sum_send_" + i);
                            if (intfRes.resultCode < 0) throw new Exception("3.2.6." + intfRes.resultMessage);
                        }
                    }
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------

                #endregion
            }
            catch (Exception ex)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false, ex.Message, -1);
            }

            //--------------------------------------------------------------------------------------------------------------------------------------------------------------

            #endregion

            #region 4. Определить максимальный номер платежного поручения за день

            //-----------------------------------------------------------
            sql = "select max(" + sNvlWord + "(d.num_pp, 0)) as num_pp from " + sended + " d  where d.dat_oper = " +
                  Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString());
            intfResTable = ClassDBUtils.OpenSQL(sql, connectionID);
            if (intfResTable.resultCode < 0)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false,
                    "Ошибка определения номера платежного поручения" + "\n" + intfResTable.resultMessage, -1);
            }
            table = intfResTable.GetData();

            Int32 num_pp = 0;

            if ((table != null) && (table.Rows.Count > 0))
                if (table.Rows[0]["num_pp"].ToString().Trim() != "")
                {
                    num_pp = Convert.ToInt32(table.Rows[0]["num_pp"]);
                }
            //-----------------------------------------------------------

            #endregion

            sql = " select * from " + tshutable + " order by 2,4,8,6,ordering";
            intfResTable = ClassDBUtils.OpenSQL(sql, connectionID);
            if (intfResTable.resultCode < 0)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false,
                    "Ошибка получения данных" + "\n" + intfResTable.resultMessage, -1);
            }
            table = intfResTable.GetData();
            if (table == null)
            {
                conn_db.Close();
                return new ReturnsObjectType<DataTable>(null, false, "Ошибка получения данных", -1);
            }

            sql = " drop table " + tshutable;
            ClassDBUtils.ExecSQL(sql, connectionID);

            #region 5. Добавить в основание основного договора максимальную и минимальную ежедневную сумму к перечислению + проставить номера платежных поручений

            //--------------------------------------------------------------------------------------------------------------------------------------------------------------
            string s_sums = "";
            int cnt = 0;

            foreach (DataRow dr in table.Rows)
            {
                if (Convert.ToString(dr["cnt_fd"]).Trim() != "")
                {
                    cnt = Convert.ToInt32(dr["cnt_fd"]);
                    if (cnt > 0)
                    {
                        for (int j = 1; j <= cnt; j++)
                        {
                            if (Convert.ToString(dr["num_pp_" + j]).Trim() == "")
                            {
                                num_pp++;
                                dr["num_pp_" + j] = num_pp;
                            }

                            s_sums = "";
                            if (Convert.ToString(dr["max_sum_" + j]).Trim() != "")
                                s_sums = ",<br>макс. сумма: " + Convert.ToDecimal(dr["max_sum_" + j]).ToString("N2");
                            if (Convert.ToString(dr["min_sum_" + j]).Trim() != "")
                                s_sums += ",<br>мин. сумма: " + Convert.ToDecimal(dr["min_sum_" + j]).ToString("N2");
                            dr["s_osnov_" + j] = (string)dr["s_osnov_" + j] + s_sums;
                        }
                    }
                }
            }

            table.AcceptChanges();
            //--------------------------------------------------------------------------------------------------------------------------------------------------------------

            #endregion

            return new ReturnsObjectType<DataTable>(table) { tag = cntRecord };
        }

        [Obsolete("Устарело. Перечисление денег в разрезе УК")]
        public Returns SaveMoneySended(List<MoneySended> list)
        {
            if ((list == null) || (list != null && list.Count == 0))
                return new Returns(false, "Не заданы суммы перечислений", -1);

            if (list[0].nzp_area < 1) return new Returns(false, "Не определена Управляющая организация", -1);
            if (list[0].nzp_payer < 1) return new Returns(false, "Не задан подрядчик", -1);
            if (!(list[0].nzp_user > 0)) return new Returns(false, "Не задан пользователь", -1);

            DateTime dat_oper = DateTime.MinValue;
            DateTime min_dat_oper = Points.DateOper;
            for (int iCount = 0; iCount < list.Count; iCount++)
            {
                if (list[iCount].nzp_area < 1 || list[iCount].nzp_payer < 1 /*|| list[iCount].nzp_serv < 1*/||
                    list[iCount].dat_oper == "")
                {
                    return new Returns(false, "Неверные входные параметры");
                }

                if (!DateTime.TryParse(list[iCount].dat_oper, out dat_oper))
                {
                    return new Returns(false, "Неверный формат даты операционного дня");
                }

                // разрешить перечислять деньги задним числом
                //if (dat_oper != Points.DateOper)
                //    return new Returns(false, "Можно редактировать только перечисления текущего операционного дня");

                if (list[iCount].pref.Trim() == "") list[iCount].pref = Points.Pref;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region определение локального пользователя
            int nzpUser = list[0].nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, list[0], out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/

            #endregion

            IDbTransaction transaction;
            try
            {
                transaction = conn_db.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }
            string sql = "";
            IDataReader reader = null;

            try
            {
                #region очистка таблиц sended и sended_dom

                //--------------------------------------------------------------------------------------------------------------------
                foreach (MoneySended item in list)
                {
                    // получить операционый день
                    dat_oper = DateTime.Parse(item.dat_oper);
                    // названия таблиц
                    string sended = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter +
                                    "fn_sended";
                    string sended_dom = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter +
                                        "fn_sended_dom";

                    // очистка таблиц
                    sql = "delete from " + sended + " where nzp_area = " + item.nzp_area + " and nzp_payer = " +
                          item.nzp_payer + " and dat_oper = " +
                          Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = "delete from " + sended_dom + " where nzp_area = " + item.nzp_area + " and nzp_payer = " +
                          item.nzp_payer + " and dat_oper = " +
                          Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }
                //--------------------------------------------------------------------------------------------------------------------

                #endregion

                Int32 nzp_dom = 0;

                foreach (MoneySended item in list)
                {
                    // получить операционый день
                    dat_oper = DateTime.Parse(item.dat_oper);
                    // названия таблиц
                    string sended = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter +
                                    "fn_sended";
                    string sended_dom = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter +
                                        "fn_sended_dom";
                    string distrib = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter +
                                     "fn_distrib_dom_" + (dat_oper.Month % 100).ToString("00");

                    if (item.nzp_fd < 1) throw new Exception("Неверные входные параметры");

                    Int32 nzp_snd = 0;
                    if (item.sum_send > 0)
                    {
                        sql = "insert into " + sended + " (" +
                            // nzp_snd
                              (DBManager.tableDelimiter == ":" ? "nzp_snd, " : "") +
                            // остальные поля
                              " dat_oper, nzp_area, nzp_serv, nzp_payer, nzp_fd, sum_send, nzp_user, dat_when, dat_pp, num_pp)" +
                              " values (" +
                            // nzp_snd
                              (DBManager.tableDelimiter == ":" ? "0, " : "") +
                            // остальные поля
                              Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) + ", " +
                              item.nzp_area + ", " + item.nzp_serv + ", " + item.nzp_payer +
                              ", " + item.nzp_fd + ", " + item.sum_send + ", " + nzpUser + ", " + sCurDateTime + ", " +
                            /*Points.DateOper.ToShortDateString()*/
                              Utils.EStrNull(item.dat_pp) +
                              "," + item.num_pp + ")";

                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result) throw new Exception(ret.text);

                        // получить ключ
                        sql = "select nzp_snd from " + sended +
                              " where dat_oper = " +
                              Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                              "   and nzp_area = " + item.nzp_area +
                              (item.nzp_serv > 0 ? " and nzp_serv = " + item.nzp_serv : "") +
                              "   and nzp_payer = " + item.nzp_payer +
                              "   and nzp_fd = " + item.nzp_fd;

                        ret = ExecRead(conn_db, transaction, out reader, sql, true);
                        if (!ret.result) throw new Exception("Не удалось получить ключ");

                        if (reader.Read())
                        {
                            if (reader["nzp_snd"] != DBNull.Value) nzp_snd = Convert.ToInt32(reader["nzp_snd"]);
                        }
                    }

                    if (nzp_snd <= 0) continue;

                    #region распределить суммы по домам

                    //-----------------------------------------------------------------------------------------------------------
                    string distrib_dom = "tmp_distrib_dom_" + nzpUser + "_" + nzp_snd;
                    sql = "drop table " + distrib_dom;
                    ExecSQL(conn_db, transaction, sql, false);

                    // создать временную таблицу
                    sql = " create temp table " + distrib_dom + " (" +
                          " nzp_dom     integer, " +
                          " nzp_serv    integer, " +
                          " sum_out     " + DBManager.sDecimalType + " (14,2), " +
                          " sum_out_tot " + DBManager.sDecimalType + " (14,2), " +
                          " coeff       float, " +
                          " sum_send    " + DBManager.sDecimalType + " (14,2), " +
                          " sum_distr   " + DBManager.sDecimalType + " (14,2)  " + ")" +
                          (DBManager.tableDelimiter == ":" ? " With no log " : "");

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    // сохранить в временную таблицу коды домов, услуг, суммы, по которым будет выполняться распределение
                    sql = " insert into " + distrib_dom + " (nzp_dom, nzp_serv, sum_out) " +
                          " select nzp_dom, nzp_serv, sum(" + sNvlWord + "(sum_out, 0)) " +
                          " from " + distrib +
                          " where nzp_area = " + item.nzp_area +
                          "   and nzp_payer = " + item.nzp_payer +
                          (item.nzp_serv > 0 ? " and nzp_serv = " + item.nzp_serv : "") +
                          "   and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                          " group by 1,2";

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    // получить сумму sum_out
                    sql = "select sum(sum_out) from " + distrib_dom;
                    Object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                    decimal sum_out_tot = 0;
                    try
                    {
                        sum_out_tot = Convert.ToDecimal(obj);
                    }
                    catch
                    {
                        throw new Exception("Не удалось определить сумму по домам");
                    }

                    // проставить суммы по умолчанию: сумму sum_out и sum_send
                    sql = "update " + distrib_dom + " set sum_out_tot = " + sum_out_tot + ", sum_send = " +
                          item.sum_send;
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    // вычислить коэффициенты распределения
                    sql = " update " + distrib_dom + " set coeff = sum_out / sum_out_tot";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    // вычислить суммы для распрелеения = coeff * sum_send
                    sql = " update " + distrib_dom + " set sum_distr = coeff * sum_send";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    // вычислить сумму sum_distrib
                    sql = "select sum(sum_distr) from " + distrib_dom;
                    obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                    decimal sum_distrib_tot = 0;
                    try
                    {
                        sum_distrib_tot = Convert.ToDecimal(obj);
                    }
                    catch
                    {
                        throw new Exception("Не удалось определить сумму распределений");
                    }

                    // если распределяемая сумма и сумма распределенных сумм не совпадают,
                    // то определить код дома с наибольшей суммой и добавить разницу между item.sum_send и sum_distrib_tot 
                    if (sum_distrib_tot != item.sum_send)
                    {
                        sql = " select " + (DBManager.tableDelimiter == ":" ? " first 1 " : "") +
                              " nzp_dom, nzp_serv from " + distrib_dom +
                              " order by sum_distr desc " + (DBManager.tableDelimiter == "." ? " limit 1 " : "");

                        ret = ExecRead(conn_db, transaction, out reader, sql, true);
                        if (!ret.result) throw new Exception(ret.text);

                        nzp_dom = 0;
                        int nzp_serv = 0;
                        if (reader.Read())
                        {
                            if (reader["nzp_dom"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                            if (reader["nzp_serv"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader["nzp_serv"]);
                        }

                        sql = " update " + distrib_dom + " set sum_distr = sum_distr + " + item.sum_send + " - " +
                              sum_distrib_tot +
                              " where nzp_dom = " + nzp_dom + " and nzp_serv = " + nzp_serv;
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                    //-----------------------------------------------------------------------------------------------------------

                    #endregion

                    // sended_dom
                    sql = "insert into " + sended_dom + " (" +
                        // nzp_snd
                          (DBManager.tableDelimiter == ":" ? "nzp_snd, " : "") +
                        // остальные поля
                          " nzp_send, dat_oper, nzp_area, nzp_serv, nzp_payer, nzp_fd, sum_send, nzp_user, dat_when, nzp_dom)" +
                          " Select " +
                        // nzp_snd
                          (DBManager.tableDelimiter == ":" ? "0, " : "") +
                        // остальные поля        
                          nzp_snd + ", " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                          ", " + item.nzp_area + ", " + item.nzp_serv + ", " + item.nzp_payer +
                          ", " + item.nzp_fd + ", " +
                          (item.nzp_serv > 0 ? "sum_distr" : "sum(sum_distr)") +
                          ", " + nzpUser + ", " + sCurDateTime + ", nzp_dom " +
                          " from " + distrib_dom +
                          (item.nzp_serv > 0 ? "" : " group by nzp_dom");

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    // distrib
                    ret = ExecSQL(conn_db, transaction,
                        " delete from  " + distrib + " where  sum_send > 0" +
                        " and nzp_area = " + item.nzp_area +
                        " and nzp_payer = " + item.nzp_payer +
                        (item.nzp_serv > 0 ? " and nzp_serv = " + item.nzp_serv : "") +
                        " and nzp_bank = -1 " +
                        " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()), true);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = "insert into " + distrib +
                          " (sum_send, nzp_area, nzp_serv, nzp_payer, nzp_bank, dat_oper, nzp_dom) " +
                          " Select sum_distr, " +
                          item.nzp_area + ", nzp_serv, " + item.nzp_payer + ", -1 " + ", " +
                          Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                          ", nzp_dom " +
                          " from " + distrib_dom;
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = "drop table " + distrib_dom;
                    ret = ExecSQL(conn_db, transaction, sql, false);
                }

                // delete
                foreach (MoneySended item in list)
                {
                    // получить операционый день
                    dat_oper = DateTime.Parse(item.dat_oper);
                    if (min_dat_oper >= dat_oper)
                    {
                        min_dat_oper = dat_oper;
                    }
                    // названия таблиц
                    string sended = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter +
                                    "fn_sended";
                    string distrib = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter +
                                     "fn_distrib_dom_" + (dat_oper.Month % 100).ToString("00");

                    sql = " update " + distrib + " set sum_send = 0 " +
                          " where (select count(*) from " + sended + " where nzp_area = " + distrib + ".nzp_area " +
                          " and nzp_payer = " + distrib + ".nzp_payer " +
                          " and nzp_serv = (case when nzp_serv <> 0 then " + distrib + ".nzp_serv else nzp_serv end) " +
                          " and nzp_bank = -1 " +
                          " and dat_oper = " + distrib + ".dat_oper) = 0 " +
                          " and nzp_area = " + item.nzp_area +
                          " and nzp_payer = " + item.nzp_payer +
                          " and nzp_bank = -1 " +
                          " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                    /*

                    ret = ExecSQL(conn_db,
                        " Update " + distrib +
                        " Set sum_send = ( " +
                                    " Select sum(sum_send) From ttt_paxx a " +
                                    " Where a.nzp_payer= " + distrib + ".nzp_payer " +
                                    "   and a.nzp_area = " + distrib + ".nzp_area " +
                                    "   and a.nzp_dom = " + distrib + ".nzp_dom " +
                                    "   and a.nzp_serv = " + distrib + ".nzp_serv " +
                                    "   and a.nzp_bank = " + distrib + ".nzp_bank " +
                                    " ) " +
                        " Where dat_oper = " + dat_oper + sConvToDate + " " +
                        "   and 0 < ( Select count(*) From ttt_paxx a " +
                                    " Where a.nzp_payer= " + distrib + ".nzp_payer " +
                                    "   and a.nzp_area = " + distrib + ".nzp_area " +
                                    "   and a.nzp_dom = " + distrib + ".nzp_dom " +
                                    "   and a.nzp_serv = " + distrib + ".nzp_serv " +
                                    "   and a.nzp_bank = " + distrib + ".nzp_bank " +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        return;
                    }
                    */
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //расчет итогового сальдо
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    string strNVL;
#if PG
                    strNVL = "coalesce(";
#else
                strNVL = "nvl(";
#endif

                    ret = ExecSQL(conn_db, transaction,
                        " Update " + distrib +
                        " Set sum_out = " + strNVL + "sum_in,0) + " + strNVL + "sum_rasp,0) - " + strNVL +
                        "sum_ud,0) + " + strNVL + "sum_naud,0) + " + strNVL + "sum_reval,0) - " + strNVL +
                        "sum_send,0) " +
                        "   ,sum_charge =" + strNVL + "sum_rasp,0) - " + strNVL + "sum_ud,0) + " + strNVL +
                        "sum_naud,0) + " + strNVL + "sum_reval,0) " +
                        " Where dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                        "  " +
                        " and nzp_payer = " + item.nzp_payer +
                        " and nzp_area = " + item.nzp_area +
                        //" and nzp_serv = " + item.nzp_serv +
                        " and nzp_bank = -1 ", true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                    
                }

                if (transaction != null)
                {
                    transaction.Commit();
                }


                foreach (MoneySended item in list)
                {

                    //int yy =  Convert.ToDateTime(item.dat_oper).Year;
                    //int mm =  Convert.ToDateTime(item.dat_oper).Month;
                    //int yy = min_dat_oper.Year;
                    //int mm = min_dat_oper.Month;
                    //Convert.CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(-1, -1, "", yy, mm, yy, mm);
                    DbCalcPack db2 = new DbCalcPack();
                    //paramcalc.dat_oper = Convert.ToDateTime(min_dat_oper.ToShortDateString());
                    //paramcalc.dat_oper = Convert.ToDateTime(min_dat_oper.ToShortDateString());
                    db2.UpdateSaldoFndistrib(min_dat_oper, Convert.ToInt32(item.nzp_payer), Convert.ToInt32(item.nzp_area), out ret);
                    db2.Close();
                }


                //DbCalc dbc = new DbCalc();
                //dbc.DistribPaXX_1(dat_oper, dat_oper, out ret);
                //dbc.Close();

                conn_db.Close();
                return ret;
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return new Returns(false, ex.Message);
            }
        }

        public List<FnPercent> FindFnPercent(FnPercent finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }
            /*if (finder.nzp_payer < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }*/

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string where = "";
            if (finder.nzp_payer > 0) where = " and p.nzp_payer = " + finder.nzp_payer;

            else if (finder.nzp_payers != null && finder.nzp_payers.Count > 0)
            {
                string str = "";
                foreach (int it in finder.nzp_payers)
                {
                    if (str == "") str += it;
                    else str += "," + it;
                }
                if (str != "") where = " and p.nzp_payer in (" + str + ")";
            }

            string sql =
                "Select p.nzp_fp, p.nzp_payer, pp.payer, p.nzp_supp, sp.name_supp, p.nzp_serv, s.service, p.nzp_area, a.area, p.nzp_bank, b.bank, p.perc_ud, p.dat_s, p.dat_po " +
#if PG
 " From " + Points.Pref + "_data.fn_percent p " +
                " left outer join " + Points.Pref + "_kernel.s_payer pp on p.nzp_payer = pp.nzp_payer " +
                " left outer join " + Points.Pref + "_kernel.supplier sp on p.nzp_supp = sp.nzp_supp " +
                " left outer join " + Points.Pref + "_kernel.services s on p.nzp_serv = s.nzp_serv " +
                " left outer join " + Points.Pref + "_data.s_area a on p.nzp_area = a.nzp_area " +
                " left outer join " + Points.Pref + "_kernel.s_bank b on p.nzp_bank = b.nzp_bank " +
                " Where 1 = 1 " + where;
#else
 " From " + Points.Pref + "_data:fn_percent p, outer " + Points.Pref + "_kernel:s_payer pp " + ", outer " + Points.Pref + "_kernel:supplier sp, outer " + Points.Pref + "_kernel:services s, outer " + Points.Pref + "_data:s_area a, outer " + Points.Pref + "_kernel:s_bank b " +
                " Where 1 = 1" + where + " and p.nzp_payer = pp.nzp_payer and p.nzp_supp = sp.nzp_supp and p.nzp_serv = s.nzp_serv and p.nzp_area = a.nzp_area and p.nzp_bank = b.nzp_bank ";
#endif

            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_serv)
                            sql += " and (p.nzp_serv < 0 or p.nzp_serv in (" + role.val + ")) ";
                        else if (role.kod == Constants.role_sql_supp)
                            sql += " and (p.nzp_supp < 0 or p.nzp_supp in (" + role.val + ")) ";
                        else if (role.kod == Constants.role_sql_area)
                            sql += " and (p.nzp_area < 0 or p.nzp_area in (" + role.val + ")) ";
                        else if (role.kod == Constants.role_sql_geu)
                            sql += " and (p.nzp_geu < 0 or p.nzp_geu in (" + role.val + ")) ";
                    }
                }
            }

            sql += " Order by pp.payer, sp.name_supp, s.service, a.area, p.dat_s, p.dat_po";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<FnPercent> list = new List<FnPercent>();

            int i = 0;
            while (reader.Read())
            {
                i++;
                if (finder.skip > 0 && i <= finder.skip) continue;
                if (finder.rows > 0 && i > finder.rows + finder.skip) continue;

                FnPercent zap = new FnPercent();
                zap.num = i.ToString();

                if (reader["nzp_fp"] != DBNull.Value) zap.nzp_fp = Convert.ToInt32(reader["nzp_fp"]);
                if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();

                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                if (zap.nzp_supp == -1) zap.name_supp = "Все";
                else
                {
                    if (reader["name_supp"] != DBNull.Value)
                        zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                }

                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (zap.nzp_serv == -1) zap.service = "Все";
                else
                {
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                }

                if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                if (zap.nzp_area == -1) zap.area = "Все";
                else
                {
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                }

                if (reader["nzp_bank"] != DBNull.Value) zap.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                if (zap.nzp_bank == -1) zap.bank = "Все";
                else
                {
                    if (reader["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader["bank"]).Trim();
                }
                if (reader["perc_ud"] != DBNull.Value) zap.perc_ud = Convert.ToDecimal(reader["perc_ud"]);
                if (reader["dat_s"] != DBNull.Value)
                    zap.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                if (reader["dat_po"] != DBNull.Value)
                    if (Convert.ToDateTime(reader["dat_po"]) == new DateTime(3000, 1, 1)) zap.dat_po = "";
                    else zap.dat_po = Convert.ToDateTime(reader["dat_po"]).ToShortDateString();

                list.Add(zap);
            }

            reader.Close();
            conn_db.Close();

            ret.tag = i;
            return list;
        }

        public List<FnPercent> FindFnPercentDom(FnPercent finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }
            /*if (finder.nzp_payer < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }*/

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string where = "";

            if (finder.perc_ud > 0) where += " and p.perc_ud = " + finder.perc_ud;
            if (finder.dat_s != "" && finder.dat_po != "")
                where += " and '" + finder.dat_s + "' <= p.dat_po " +
                         " and '" + finder.dat_po + "' >= p.dat_s";
            else if (finder.dat_s != "") where += " and '" + finder.dat_s + "' between p.dat_s and p.dat_po ";
            else if (finder.dat_po != "") where += " and '" + finder.dat_po + "' between p.dat_s and p.dat_po ";

            // поставщик
            if (finder.nzp_supp > 0) where += " and p.nzp_supp = " + finder.nzp_supp;
            if (finder.nzp_supp_snyat > 0) where += " and p.nzp_supp_snyat = " + finder.nzp_supp_snyat;
            // услуга
            if (finder.nzp_serv > 0) where += " and p.nzp_serv = " + finder.nzp_serv;
            // услуга упр орг
            if (finder.nzp_serv_from > 0) where += " and p.nzp_serv_from = " + finder.nzp_serv_from;
            // Управляющая организация
            if (finder.nzp_area > 0) where += " and p.nzp_area = " + finder.nzp_area;
            // ППП
            if (finder.nzp_bank > 0) where += " and p.nzp_bank = " + finder.nzp_bank;
            //дом
            StringBuilder from_dom = new StringBuilder();
            ExecSQL(conn_db, "drop table temp_dom", false);
            ret = ExecSQL(conn_db, "create temp table temp_dom (nzp_dom integer) " + sUnlogTempTable, true);
            if (!ret.result) return null;

            string where_dom = "";
            if (finder.nzp_town > 0 || finder.nzp_raj > 0 || finder.nzp_ul > 0 || finder.nzp_dom > 0)
            {
                from_dom.Append(" insert into temp_dom (nzp_dom) ");
                from_dom.Append(" select nzp_dom from " + Points.Pref + "_data" + tableDelimiter + "dom d, " +
                                Points.Pref + "_data" + tableDelimiter + "s_ulica u,  " +
                                Points.Pref + "_data" + tableDelimiter + "s_rajon r ");
                where_dom += " d.nzp_ul = u.nzp_ul and r.nzp_raj=u.nzp_raj ";
                if (finder.nzp_town > 0) where_dom += " and r.nzp_town = " + finder.nzp_town;
                if (finder.nzp_raj > 0) where_dom += " and r.nzp_raj = " + finder.nzp_raj;
                if (finder.nzp_ul > 0) where_dom += " and u.nzp_ul = " + finder.nzp_ul;
                if (finder.nzp_dom > 0) where_dom += " and d.nzp_dom = " + finder.nzp_dom;
                from_dom.Append(" where " + where_dom);
                ret = ExecSQL(conn_db, from_dom.ToString(), true);
                if (!ret.result) return null;  where += " and (p.nzp_dom in (select nzp_dom from temp_dom) ) ";
            }

          

            if (finder.nzp_payer > 0) where += " and p.nzp_payer = " + finder.nzp_payer;
            else if (finder.nzp_payers != null && finder.nzp_payers.Count > 0)
            {
                string str = "";
                foreach (int it in finder.nzp_payers)
                {
                    if (str == "") str += it;
                    else str += "," + it;
                }
                if (str != "") where += " and p.nzp_payer in (" + str + ")";
            }

            ExecSQL(conn_db, "drop table temp_fn_percent_dom", false);
            ret = ExecSQL(conn_db, "create temp table temp_fn_percent_dom (nzp_fp integer, nzp_dom integer," +
                                   "nzp_payer integer, payer character(40), nzp_supp integer, name_supp character(100), nzp_supp_snyat integer, name_supp_snyat character(100), " +
                                   "nzp_serv integer, service character(100), nzp_serv_from integer,  service_from character(100), " +
                                   " nzp_area integer, area character(40), nzp_bank integer, bank character(40), perc_ud  numeric(10,2) DEFAULT 0, " +
                                   " dat_s date, dat_po date " +
                                   ") " + sUnlogTempTable, true);
            if (!ret.result) return null;

            string sql =
                "insert into temp_fn_percent_dom (nzp_fp, nzp_dom, nzp_payer, payer, nzp_supp, name_supp, nzp_supp_snyat, name_supp_snyat, " +
                "nzp_serv, service, nzp_serv_from, service_from, nzp_area, area, nzp_bank, bank, perc_ud, dat_s, dat_po) " +
                "Select p.nzp_fp, p.nzp_dom, p.nzp_payer, pp.payer, p.nzp_supp, sp.name_supp, p.nzp_supp_snyat, sp_sn.name_supp, " +
                "p.nzp_serv, s.service, p.nzp_serv_from, s2.service as service_from, p.nzp_area, a.area, " +
                "p.nzp_bank, b.bank, p.perc_ud, p.dat_s, p.dat_po " +
#if PG
 " From " + Points.Pref + "_data.fn_percent_dom p " +
                " left outer join " + Points.Pref + "_kernel.s_payer pp on p.nzp_payer = pp.nzp_payer " +
                " left outer join " + Points.Pref + "_kernel.supplier sp on p.nzp_supp = sp.nzp_supp " +
                " left outer join " + Points.Pref + "_kernel.supplier sp_sn on p.nzp_supp_snyat = sp_sn.nzp_supp " +
                " left outer join " + Points.Pref + "_kernel.services s on p.nzp_serv = s.nzp_serv " +
                " left outer join " + Points.Pref + "_kernel.services s2 on p.nzp_serv_from = s2.nzp_serv " +
                " left outer join " + Points.Pref + "_data.s_area a on p.nzp_area = a.nzp_area " +
                " left outer join " + Points.Pref + "_kernel.s_bank b on p.nzp_bank = b.nzp_bank " +

                " Where 1 = 1 " + where;
#else
 " From " + Points.Pref + "_data:fn_percent_dom p, " + 
            " outer " + Points.Pref + "_kernel:s_payer pp," + 
            " outer " + Points.Pref + "_kernel:supplier sp, " + 
            " outer " + Points.Pref + "_kernel:supplier sp_sn, " + 
            " outer " + Points.Pref + "_kernel:services s, " + 
            " outer " + Points.Pref + "_kernel:services s2, " + 
            " outer " + Points.Pref + "_data:s_area a, " + 
            " outer " + Points.Pref + "_kernel:s_bank b " + from_dom.ToString()+
                " Where 1 = 1" + where + " and p.nzp_payer = pp.nzp_payer and p.nzp_supp = sp.nzp_supp and p.nzp_serv = s.nzp_serv"+
                "  and p.nzp_serv_from = s2.nzp_serv and p.nzp_area = a.nzp_area and p.nzp_bank = b.nzp_bank and p.nzp_supp_snyat = sp_sn.nzp_supp ";
#endif

            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_serv)
                            sql += " and (p.nzp_serv < 0 or p.nzp_serv in (" + role.val + ")) ";
                        else if (role.kod == Constants.role_sql_supp)
                            sql += " and (p.nzp_supp < 0 or p.nzp_supp in (" + role.val + ")) ";
                        else if (role.kod == Constants.role_sql_area)
                            sql += " and (p.nzp_area < 0 or p.nzp_area in (" + role.val + ")) ";
                        else if (role.kod == Constants.role_sql_geu)
                            sql += " and (p.nzp_geu < 0 or p.nzp_geu in (" + role.val + ")) ";
                    }
                }
            }

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return null;

            from_dom.Remove(0, from_dom.Length);
            where_dom = "";
#if PG
            from_dom.Append(" left outer join " + Points.Pref + "_data.dom d " +
                            " left outer join " + Points.Pref + "_data.s_ulica u " +
                            " left outer join " + Points.Pref + "_data.s_rajon r on r.nzp_raj = u.nzp_raj" +
                            " on d.nzp_ul = u.nzp_ul on p.nzp_dom = d.nzp_dom ");
#else                
            from_dom.Append(" , outer (" + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_ulica u,  " +
                Points.Pref + "_data:s_rajon r) ");
            where_dom += " where  d.nzp_dom = p.nzp_dom and  d.nzp_ul = u.nzp_ul and r.nzp_raj=u.nzp_raj ";
#endif

            sql = "select nzp_fp, p.nzp_dom, nzp_payer, payer, nzp_supp, name_supp, nzp_supp_snyat, name_supp_snyat, " +
                  "nzp_serv, service, nzp_serv_from, service_from, p.nzp_area, area, nzp_bank, bank, perc_ud, dat_s, dat_po, " +
                  " u.nzp_ul, r.nzp_raj, r.nzp_town, " +
                  " trim(" + sNvlWord + "(ulicareg,'улица'))||' '||trim(" + sNvlWord + "(u.ulica,''))||' / '||trim(" +
                  sNvlWord + "(r.rajon,''))||'   дом '||" +
                  " trim(" + sNvlWord + "(ndom,''))||'  корп. '|| trim(" + sNvlWord + "(nkor,'')) as adr " +
                  " from temp_fn_percent_dom p " + from_dom.ToString() + where_dom;
            sql += " Order by payer, name_supp, service, area, dat_s, dat_po";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            ExecSQL(conn_db, "drop table temp_dom", false);
            List<FnPercent> list = new List<FnPercent>();

            int i = 0;
            while (reader.Read())
            {
                i++;
                if (finder.skip > 0 && i <= finder.skip) continue;
                if (finder.rows > 0 && i > finder.rows + finder.skip) continue;

                FnPercent zap = new FnPercent();
                zap.num = i.ToString();

                if (reader["nzp_fp"] != DBNull.Value) zap.nzp_fp = Convert.ToInt32(reader["nzp_fp"]);
                if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                if (reader["nzp_ul"] != DBNull.Value) zap.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                if (reader["nzp_raj"] != DBNull.Value) zap.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                if (reader["nzp_town"] != DBNull.Value) zap.nzp_town = Convert.ToInt32(reader["nzp_town"]);
                if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                if (zap.nzp_dom > 0)
                {
                    if (reader["adr"] != DBNull.Value) zap.adr = Convert.ToString(reader["adr"]).Trim();
                }
                else zap.adr = "";
                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                if (zap.nzp_supp == -1) zap.name_supp = "Все";
                else
                {
                    if (reader["name_supp"] != DBNull.Value)
                        zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                }
                if (reader["nzp_supp_snyat"] != DBNull.Value)
                    zap.nzp_supp_snyat = Convert.ToInt32(reader["nzp_supp_snyat"]);
                if (zap.nzp_supp_snyat == -1) zap.name_supp_snyat = "Все";
                else
                {
                    if (reader["name_supp_snyat"] != DBNull.Value)
                        zap.name_supp_snyat = Convert.ToString(reader["name_supp_snyat"]).Trim();
                }
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (zap.nzp_serv == -1) zap.service = "Все";
                else
                {
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                }

                if (reader["nzp_serv_from"] != DBNull.Value)
                    zap.nzp_serv_from = Convert.ToInt32(reader["nzp_serv_from"]);
                if (zap.nzp_serv_from == -1) zap.service_from = "Все";
                else
                {
                    if (reader["service_from"] != DBNull.Value)
                        zap.service_from = Convert.ToString(reader["service_from"]).Trim();
                }

                if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                if (zap.nzp_area == -1) zap.area = "Все";
                else
                {
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                }

                if (reader["nzp_bank"] != DBNull.Value) zap.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                if (zap.nzp_bank == -1) zap.bank = "Все";
                else
                {
                    if (reader["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader["bank"]).Trim();
                }
                if (reader["perc_ud"] != DBNull.Value) zap.perc_ud = Convert.ToDecimal(reader["perc_ud"]);
                if (reader["dat_s"] != DBNull.Value)
                    zap.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                if (reader["dat_po"] != DBNull.Value)
                    if (Convert.ToDateTime(reader["dat_po"]) == new DateTime(3000, 1, 1)) zap.dat_po = "";
                    else zap.dat_po = Convert.ToDateTime(reader["dat_po"]).ToShortDateString();

                list.Add(zap);
            }

            reader.Close();
            conn_db.Close();

            ret.tag = i;
            return list;
        }

        public List<FnPercent> GetFnPercentDomLog(FnPercent finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            var from = new StringBuilder();
            from.Append(" From " + Points.Pref + "_data" + tableDelimiter + "fn_percent_dom_log p ");
            from.Append(" left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer pp on p.nzp_payer = pp.nzp_payer ");
            from.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "users ur on ur.nzp_user = p.changed_by ");
            from.Append(" left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier sp on p.nzp_supp = sp.nzp_supp ");
            from.Append(" left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier sp_sn on p.nzp_supp_snyat = sp_sn.nzp_supp ");
            from.Append(" left outer join " + Points.Pref + "_kernel" + tableDelimiter + "services s on p.nzp_serv = s.nzp_serv ");
            from.Append(" left outer join " + Points.Pref + "_kernel" + tableDelimiter + "services s2 on p.nzp_serv_from = s2.nzp_serv ");
            from.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_area a on p.nzp_area = a.nzp_area ");
            from.Append(" left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_bank b on p.nzp_bank = b.nzp_bank ");
            from.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "dom d ");
            from.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_ulica u ");
            from.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_rajon r on r.nzp_raj = u.nzp_raj");
            from.Append( " on d.nzp_ul = u.nzp_ul on p.nzp_dom = d.nzp_dom ");
            from.Append(" , " + Points.Pref + "_data" + tableDelimiter + "s_data_operation op ");

            var where = new StringBuilder(" Where p.nzp_data_operation = op.nzp_data_operation ");
            
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_serv)
                            where.Append(" and (p.nzp_serv < 0 or p.nzp_serv in (" + role.val + ")) ");
                        else if (role.kod == Constants.role_sql_supp)
                            where.Append(" and (p.nzp_supp < 0 or p.nzp_supp in (" + role.val + ")) ");
                        else if (role.kod == Constants.role_sql_area)
                            where.Append(" and (p.nzp_area < 0 or p.nzp_area in (" + role.val + ")) ");
                        else if (role.kod == Constants.role_sql_geu)
                            where.Append(" and (p.nzp_geu < 0 or p.nzp_geu in (" + role.val + ")) ");
                    }
                }
            }

            if (finder.nzp_fp > 0) where.Append(" and nzp_fp = " + finder.nzp_fp);
            DateTime d1 = DateTime.MinValue, d2 = DateTime.MinValue;
            if (finder.changed_on != "")
            {
                if (!DateTime.TryParse(finder.changed_on, out d1))
                {
                    ret = new Returns(false, "Неверный формат даты изменений", -1);
                    conn_db.Close();
                    return null;
                }
            }
            if (finder.changed_on_po != "")
            {
                if (!DateTime.TryParse(finder.changed_on_po, out d2))
                {
                    ret = new Returns(false, "Неверный формат даты изменений", -1);
                    conn_db.Close();
                    return null;
                }
            }
            if (finder.changed_on != "" && finder.changed_on_po != "")
            {
                where.Append(" and p.changed_on" + sConvToDate + " >= '" + d1.ToShortDateString() + "'" +
                             " and p.changed_on" + sConvToDate + " <= '" + d2.ToShortDateString() + "'");
            }
            else if (finder.changed_on != "" && finder.changed_on_po == "")
            {
                where.Append(" and p.changed_on" + sConvToDate + " = '" + d1.ToShortDateString() + "'");
            }
            else if (finder.changed_on == "" && finder.changed_on_po != "")
            {
                where.Append(" and p.changed_on" + sConvToDate + " = '" + d2.ToShortDateString() + "'");
            }

            if (finder.nzp_data_operation > 0) where.Append(" and p.nzp_data_operation = " + finder.nzp_data_operation);

            if (finder.perc_ud > 0) where.Append(" and p.perc_ud = " + finder.perc_ud);

            if (finder.changed_by > 0) where.Append(" and p.changed_by = " + finder.changed_by);

            var fields = " nzp_fp_log, nzp_fp, p.nzp_dom, p.nzp_payer, payer, p.nzp_supp, sp.name_supp, nzp_supp_snyat, sp_sn.name_supp name_supp_snyat, p.nzp_serv, s.service, nzp_serv_from, s2.service service_from, p.nzp_area, " +
                         " area, p.nzp_bank, bank, perc_ud, dat_s, dat_po,  u.nzp_ul, r.nzp_raj, r.nzp_town, ur.name ||'('||ur.comment||')' as user,  " +
                  " trim(" + sNvlWord + "(ulicareg,'улица'))||' '||trim(" + sNvlWord + "(u.ulica,''))||' / '||trim(" +
                  sNvlWord + "(r.rajon,''))||'   дом '||" +
                  " trim(" + sNvlWord + "(ndom,''))||'  корп. '|| trim(" + sNvlWord + "(nkor,'')) as adr, p.changed_on, data_operation, p.changed_by ";

            var sql = new StringBuilder("select " + fields + " ");
            sql.Append(from  + " " + where);
            sql.Append(" Order by changed_on desc");

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToStr(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            var list = new List<FnPercent>();

            int i = 0;
            while (reader.Read())
            {
                i++;
                if (finder.skip > 0 && i <= finder.skip) continue;
                if (finder.rows > 0 && i > finder.rows + finder.skip) continue;

                FnPercent zap = new FnPercent();
                zap.num = i.ToString();

                if (reader["nzp_fp"] != DBNull.Value) zap.nzp_fp = Convert.ToInt32(reader["nzp_fp"]);
                if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                if (reader["nzp_ul"] != DBNull.Value) zap.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                if (reader["nzp_raj"] != DBNull.Value) zap.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                if (reader["nzp_town"] != DBNull.Value) zap.nzp_town = Convert.ToInt32(reader["nzp_town"]);
                if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                if (reader["user"] != DBNull.Value) zap.changed_by_name = Convert.ToString(reader["user"]).Trim();
                if (reader["data_operation"] != DBNull.Value)
                    zap.data_operation = Convert.ToString(reader["data_operation"]);
                if (reader["changed_on"] != DBNull.Value)
                    zap.changed_on = Convert.ToDateTime(reader["changed_on"]).ToLongDateString();
                if (zap.nzp_dom > 0)
                {
                    if (reader["adr"] != DBNull.Value) zap.adr = Convert.ToString(reader["adr"]).Trim();
                }
                else zap.adr = "";
                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                if (zap.nzp_supp == -1) zap.name_supp = "Все";
                else
                {
                    if (reader["name_supp"] != DBNull.Value)
                        zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                }
                if (reader["nzp_supp_snyat"] != DBNull.Value)
                    zap.nzp_supp_snyat = Convert.ToInt32(reader["nzp_supp_snyat"]);
                if (zap.nzp_supp_snyat == -1) zap.name_supp_snyat = "Все";
                else
                {
                    if (reader["name_supp_snyat"] != DBNull.Value)
                        zap.name_supp_snyat = Convert.ToString(reader["name_supp_snyat"]).Trim();
                }
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (zap.nzp_serv == -1) zap.service = "Все";
                else
                {
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                }

                if (reader["nzp_serv_from"] != DBNull.Value)
                    zap.nzp_serv_from = Convert.ToInt32(reader["nzp_serv_from"]);
                if (zap.nzp_serv_from == -1) zap.service_from = "Все";
                else
                {
                    if (reader["service_from"] != DBNull.Value)
                        zap.service_from = Convert.ToString(reader["service_from"]).Trim();
                }

                if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                if (zap.nzp_area == -1) zap.area = "Все";
                else
                {
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                }

                if (reader["nzp_bank"] != DBNull.Value) zap.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                if (zap.nzp_bank == -1) zap.bank = "Все";
                else
                {
                    if (reader["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader["bank"]).Trim();
                }
                if (reader["perc_ud"] != DBNull.Value) zap.perc_ud = Convert.ToDecimal(reader["perc_ud"]);
                if (reader["dat_s"] != DBNull.Value)
                    zap.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                if (reader["dat_po"] != DBNull.Value)
                    if (Convert.ToDateTime(reader["dat_po"]) == new DateTime(3000, 1, 1)) zap.dat_po = "";
                    else zap.dat_po = Convert.ToDateTime(reader["dat_po"]).ToShortDateString();

                list.Add(zap);
            }

            reader.Close();


            conn_db.Close();

            ret.tag = i;
            return list;
        }

        public ReturnsType SaveFnPercentDom(FnPercent finder, IDbConnection connectionID)
        {
            #region Проверка входных параметров

            if (!(finder.nzp_user > 0)) throw new Utility.UserException("Не задан пользователь");
            if (!(finder.nzp_payer > 0)) throw new Utility.UserException("Не задан подрядчик");

            try
            {
                DateTime.Parse(finder.dat_s);
            }
            catch
            {
                throw new Utility.UserException("Неверно задана дата начала периода действия");
            }

            try
            {
                DateTime.Parse(finder.dat_po);
            }
            catch
            {
                finder.dat_po = "01.01.3000";
                //    throw new Utility.UserException("Неверно задана дата окончания периода действия"); 
            }

            if (DateTime.Parse(finder.dat_s) == DateTime.MinValue)
                throw new Utility.UserException("Не задана дата начала периода действия");
            if (DateTime.Parse(finder.dat_po) == DateTime.MinValue)
                throw new Utility.UserException("Не задана дата окончания периода действия");
            if (DateTime.Parse(finder.dat_s) > DateTime.Parse(finder.dat_po))
                throw new Utility.UserException("Неверно задан период действия");
            if ((finder.perc_ud < Convert.ToDecimal(0.0001)) || (finder.perc_ud > 100))
                throw new Utility.UserException("Неверно задан процент удержания");

            #endregion

            #region
            Returns ret = new Returns(true);
            finder.pref = Points.Pref;
            finder.nzp_user_main = finder.nzp_user;

            /*using (DbWorkUser db = new DbWorkUser())
            {
                finder.nzp_user_main = db.GetLocalUser(connectionID, finder, out ret);
            }
            if (!ret.result) throw new Utility.UserException("Не удалось определить пользователя");*/
            #endregion

            // Проверить пересечение периодов
            string sqlText = " select count(*) as cnt " +
                " from " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_percent_dom " +
                " where not ( " +
                "     date(" + Utils.EStrNull(finder.dat_s.Trim()) + ") < " + DBManager.sNvlWord + "(dat_s," + DBManager.MDY(1, 1, 1900) + ") " +
                "     and date(" + Utils.EStrNull(finder.dat_po.Trim()) + ") < " + DBManager.sNvlWord + "(dat_s," + DBManager.MDY(1, 1, 1900) + ") " +
                "  or date(" + Utils.EStrNull(finder.dat_s.Trim()) + ") > " + DBManager.sNvlWord + "(dat_po," + DBManager.MDY(1, 1, 4000) + ") " +
                "     and date(" + Utils.EStrNull(finder.dat_po.Trim()) + ") > " + DBManager.sNvlWord + "(dat_po," + DBManager.MDY(1, 1, 4000) + ") " +
                " ) " +
                " and nzp_payer = " + finder.nzp_payer.ToString() +
                " and nzp_supp = " + ((finder.nzp_supp > 0) ? finder.nzp_supp.ToString() : "-1") +
                " and nzp_supp_snyat = " + ((finder.nzp_supp_snyat > 0) ? finder.nzp_supp_snyat.ToString() : "-1") +
                " and nzp_serv = " + ((finder.nzp_serv > 0) ? finder.nzp_serv.ToString() : "-1") +
                " and nzp_serv_from = " + ((finder.nzp_serv_from > 0) ? finder.nzp_serv_from.ToString() : "-1") +
                " and nzp_area = " + ((finder.nzp_area > 0) ? finder.nzp_area.ToString() : "-1") +
                " and nzp_bank = " + ((finder.nzp_bank > 0) ? finder.nzp_bank.ToString() : "-1") +
                " and nzp_fp <> " + finder.nzp_fp.ToString();

            DataTable dt = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();

            if (Convert.ToInt16(dt.Rows[0]["cnt"]) > 0)
                throw new Utility.UserException("Пересечение периодов действия");

            // Сохранить
            if (finder.nzp_fp > 0)
            {
                sqlText = " update " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_percent_dom set " +
                    "   nzp_payer = " + finder.nzp_payer + ", " +
                    "   nzp_supp  = " + (finder.nzp_supp > 0 ? finder.nzp_supp : -1) + ", " +
                    "   nzp_supp_snyat  = " + (finder.nzp_supp_snyat > 0 ? finder.nzp_supp_snyat : -1) + ", " +
                    "   nzp_serv  = " + (finder.nzp_serv > 0 ? finder.nzp_serv : -1) + ", " +
                    "   nzp_serv_from  = " + (finder.nzp_serv_from > 0 ? finder.nzp_serv_from : -1) + ", " +
                    "   nzp_area  = " + (finder.nzp_area > 0 ? finder.nzp_area : -1) + ", " +
                    "   nzp_bank  = " + (finder.nzp_bank > 0 ? finder.nzp_bank : -1) + ", " +
                    "   perc_ud   = " + finder.perc_ud + ", " +
                    "   dat_s     = " + Utils.EStrNull(finder.dat_s) + ", " +
                    "   dat_po    = " + Utils.EStrNull(finder.dat_po) + "," +
                    "   nzp_dom   = " + (finder.nzp_dom > 0 ? finder.nzp_dom : -1) + ", " +
                    "   changed_by = " + finder.nzp_user_main + ", " +
                    "   changed_on = " + DBManager.sCurDateTime + 
                    " where nzp_fp = " + finder.nzp_fp + " ";
            }
            else
            {
                sqlText =
                    " insert into " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_percent_dom ( " +
                    "   nzp_payer, " +
                    "   nzp_supp, " +
                    "   nzp_supp_snyat, " +
                    "   nzp_serv, " +
                    "   nzp_serv_from, " +
                    "   nzp_area, " +
                    "   nzp_bank, " +
                    "   perc_ud, " +
                    "   dat_s, " +
                    "   dat_po, " +
                    "   nzp_dom, " +
                    "   changed_by, " +
                    "   changed_on " +
                    ") " +
                    " values (" + finder.nzp_payer + "," +
                    (finder.nzp_supp > 0 ? finder.nzp_supp : -1) + "," +
                    (finder.nzp_supp_snyat > 0 ? finder.nzp_supp_snyat : -1) + "," +
                    (finder.nzp_serv > 0 ? finder.nzp_serv : -1) + "," +
                    (finder.nzp_serv_from > 0 ? finder.nzp_serv_from : -1) + "," +
                    (finder.nzp_area > 0 ? finder.nzp_area : -1) + "," +
                    (finder.nzp_bank > 0 ? finder.nzp_bank : -1) + "," +
                    finder.perc_ud + "," + 
                    Utils.EStrNull(finder.dat_s) + "," + 
                    Utils.EStrNull(finder.dat_po) + "," +
                    (finder.nzp_dom > 0 ? finder.nzp_dom : -1) + "," +
                    finder.nzp_user_main + "," +
                    DBManager.sCurDateTime +
                    ") ";
            }


            //-----------------------------------------------------------
            // Начать транзакцию
            //-----------------------------------------------------------
            int keyID = 0;

            using (IDbTransaction transactionID = connectionID.BeginTransaction())
            {
                keyID = finder.nzp_fp;

                try
                {

                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);
                    if (finder.nzp_fp == 0) keyID = Convert.ToInt32(ClassDBUtils.GetSerialKey(connectionID, transactionID));

                    //-----------------------------------------------------------
                    // Завершить транзакцию
                    //-----------------------------------------------------------
                    transactionID.Commit();
                }
                catch (DbException e)
                {
                    //-----------------------------------------------------------
                    // Откатить транзакцию
                    //-----------------------------------------------------------
                    transactionID.Rollback();

                    //-----------------------------------------------------------
                    // обработка ошибки
                    //-----------------------------------------------------------
                    throw new Exception(e.Message);
                }
            }
            
            return new ReturnsType() { tag = keyID };

        }

        public ReturnsType SaveFnPercent(FnPercent finder, IDbConnection connectionID)
        {
            #region Проверка входных параметров

            if (!(finder.nzp_user > 0)) throw new Utility.UserException("Не задан пользователь");
            if (!(finder.nzp_payer > 0)) throw new Utility.UserException("Не задан подрядчик");

            try
            {
                DateTime.Parse(finder.dat_s);
            }
            catch
            {
                throw new Utility.UserException("Неверно задана дата начала периода действия");
            }

            try
            {
                DateTime.Parse(finder.dat_po);
            }
            catch
            {
                finder.dat_po = "01.01.3000";
                //    throw new Utility.UserException("Неверно задана дата окончания периода действия"); 
            }

            if (DateTime.Parse(finder.dat_s) == DateTime.MinValue)
                throw new Utility.UserException("Не задана дата начала периода действия");
            if (DateTime.Parse(finder.dat_po) == DateTime.MinValue)
                throw new Utility.UserException("Не задана дата окончания периода действия");
            if (DateTime.Parse(finder.dat_s) > DateTime.Parse(finder.dat_po))
                throw new Utility.UserException("Неверно задан период действия");
            if ((finder.perc_ud < Convert.ToDecimal(0.0001)) || (finder.perc_ud > 100))
                throw new Utility.UserException("Неверно задан процент удержания");

            #endregion


            // Проверить пересечение периодов
            string sqlText =
#if PG
 " select count(*) as cnt " +
                " from " + Points.Pref + "_data.fn_percent " +
                " where not ( " +
                "     date('" + finder.dat_s.Trim() + "') < coalesce(dat_s,public.mdy(1,1,1900)) " +
                "     and date('" + finder.dat_po.Trim() + "') < coalesce(dat_s,public.mdy(1,1,1900)) " +
                "  or date('" + finder.dat_s.Trim() + "') > coalesce(dat_po,public.mdy(1,1,4000)) " +
                "     and date('" + finder.dat_po.Trim() + "') > coalesce(dat_po,public.mdy(1,1,4000)) " +
                " ) " +
#else
 " select count(*) as cnt " +
                " from " + Points.Pref + "_data:fn_percent " +
                " where not ( " +
                "     date('" + finder.dat_s.Trim() + "') < nvl(dat_s,mdy(1,1,1900)) " +
                "     and date('" + finder.dat_po.Trim() + "') < nvl(dat_s,mdy(1,1,1900)) " +
                "  or date('" + finder.dat_s.Trim() + "') > nvl(dat_po,mdy(1,1,4000)) " +
                "     and date('" + finder.dat_po.Trim() + "') > nvl(dat_po,mdy(1,1,4000)) " +
                " ) " +
#endif
 " and nzp_payer = " + finder.nzp_payer.ToString() +
                " and nzp_supp = " + ((finder.nzp_supp > 0) ? finder.nzp_supp.ToString() : "-1") +
                " and nzp_serv = " + ((finder.nzp_serv > 0) ? finder.nzp_serv.ToString() : "-1") +
                " and nzp_area = " + ((finder.nzp_area > 0) ? finder.nzp_area.ToString() : "-1") +
                " and nzp_bank = " + ((finder.nzp_bank > 0) ? finder.nzp_bank.ToString() : "-1") +
                " and nzp_fp <> " + finder.nzp_fp.ToString();

            DataTable dt = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();

            if (Convert.ToInt16(dt.Rows[0]["cnt"]) > 0)
                throw new Utility.UserException("Пересечение периодов действия");

            // Сохранить
            if (finder.nzp_fp > 0)
            {
                sqlText =
#if PG
 " update " + Points.Pref + "_data.fn_percent set " +
#else
 " update " + Points.Pref + "_data:fn_percent set " +
#endif
#if PG
 "   nzp_payer = " + finder.nzp_payer + ", " +
                    "   nzp_supp  = " + (finder.nzp_supp > 0 ? finder.nzp_supp : -1) + ", " +
                    "   nzp_serv  = " + (finder.nzp_serv > 0 ? finder.nzp_serv : -1) + ", " +
                    "   nzp_area  = " + (finder.nzp_area > 0 ? finder.nzp_area : -1) + ", " +
                    "   nzp_bank  = " + (finder.nzp_bank > 0 ? finder.nzp_bank : -1) + ", " +
                    "   perc_ud   = " + finder.perc_ud + ", " +
                    "   dat_s     = '" + finder.dat_s + "', " +
                    "   dat_po    = '" + finder.dat_po + "'" +
                    " where nzp_fp = " + finder.nzp_fp + " ";
#else
 "   nzp_payer = ?, " +
                    "   nzp_supp  = ?, " +
                    "   nzp_serv  = ?, " +
                    "   nzp_area  = ?, " +
                    "   nzp_bank  = ?, " +
                    "   perc_ud   =  ?, " +
                    "   dat_s     =  ?, " +
                    "   dat_po    =  ? " +
                    " where nzp_fp =  ? ";
#endif
            }
            else
            {
#if PG
                sqlText =
                    " insert into " + Points.Pref + "_data.fn_percent ( " +
#else
                sqlText =
                    " insert into " + Points.Pref + "_data:fn_percent ( " +
#endif
 "   nzp_payer, " +
                    "   nzp_supp, " +
                    "   nzp_serv, " +
                    "   nzp_area, " +
                    "   nzp_bank, " +
                    "   perc_ud, " +
                    "   dat_s, " +
                    "   dat_po ) " +

#if PG
 " values (" + finder.nzp_payer + "," +
                    (finder.nzp_supp > 0 ? finder.nzp_supp : -1) + "," +
                    (finder.nzp_serv > 0 ? finder.nzp_serv : -1) + "," +
                    (finder.nzp_area > 0 ? finder.nzp_area : -1) + "," +
                    (finder.nzp_bank > 0 ? finder.nzp_bank : -1) + "," +
                    finder.perc_ud + "," + "'" + finder.dat_s + "'" + "," + "'" + finder.dat_po + "'" + ") ";
#else
 " values (?,?,?,?,?,?,?,?) ";
#endif

            }

            IDbCommand IDbCommand = DBManager.newDbCommand(sqlText, connectionID);
#if PG
#else
            DBManager.addDbCommandParameter(IDbCommand, "nzp_payer", finder.nzp_payer);
            DBManager.addDbCommandParameter(IDbCommand, "nzp_supp", (finder.nzp_supp > 0 ? finder.nzp_supp : -1));
            DBManager.addDbCommandParameter(IDbCommand, "nzp_serv", (finder.nzp_serv > 0 ? finder.nzp_serv : -1));
            DBManager.addDbCommandParameter(IDbCommand, "nzp_area", (finder.nzp_area > 0 ? finder.nzp_area : -1));
            DBManager.addDbCommandParameter(IDbCommand, "nzp_bank", (finder.nzp_bank > 0 ? finder.nzp_bank : -1));
            DBManager.addDbCommandParameter(IDbCommand, "perc_ud", finder.perc_ud);
            DBManager.addDbCommandParameter(IDbCommand, "dat_s", Convert.ToDateTime(finder.dat_s));
            DBManager.addDbCommandParameter(IDbCommand, "dat_po", Convert.ToDateTime(finder.dat_po));

            if (finder.nzp_fp > 0)
                DBManager.addDbCommandParameter(IDbCommand, "nzp_fp", finder.nzp_fp);
#endif
            //-----------------------------------------------------------
            // Начать транзакцию
            //-----------------------------------------------------------
            IDbTransaction transactionID = connectionID.BeginTransaction();
            IDbCommand.Transaction = transactionID;
            int keyID = finder.nzp_fp;

            try
            {

                IDbCommand.ExecuteNonQuery();
                if (finder.nzp_fp == 0) keyID = Convert.ToInt32(ClassDBUtils.GetSerialKey(connectionID, transactionID));

                //-----------------------------------------------------------
                // Завершить транзакцию
                //-----------------------------------------------------------
                transactionID.Commit();
            }
            catch (DbException e)
            {
                //-----------------------------------------------------------
                // Откатить транзакцию
                //-----------------------------------------------------------
                transactionID.Rollback();

                //-----------------------------------------------------------
                // обработка ошибки
                //-----------------------------------------------------------
                throw new Exception(e.Message);

            }

            return new ReturnsType() { tag = keyID };

        }

        public ReturnsType DelFnPercent(FnPercent finder, IDbConnection connectionID)
        {
            if (!(finder.nzp_user > 0)) throw new Utility.UserException("Не задан пользователь");
            if (!(finder.nzp_fp > 0)) throw new Utility.UserException("Не задан код записи");

            string sqlText =
#if PG
 " delete from " + Points.Pref + "_data.fn_percent " +
#else
 " delete from " + Points.Pref + "_data:fn_percent " +
#endif
 " where nzp_fp = " + finder.nzp_fp.ToString();
            ClassDBUtils.ExecSQL(sqlText, connectionID);

            return new ReturnsType();
        }

        public ReturnsType DelFnPercentDom(FnPercent finder, IDbConnection connectionID)
        {
            if (!(finder.nzp_user > 0)) throw new Utility.UserException("Не задан пользователь");
            if (!(finder.nzp_fp > 0)) throw new Utility.UserException("Не задан код записи");

            #region
            Returns ret = new Returns(true);
            finder.pref = Points.Pref;
            finder.nzp_user_main = finder.nzp_user;

            /*using (DbWorkUser db = new DbWorkUser())
            {
                finder.nzp_user_main = db.GetLocalUser(connectionID, finder, out ret);
            }
            if (!ret.result) throw new Utility.UserException("Не удалось определить пользователя");*/
            #endregion

            int nzp_data_operation_del = 3;
            using (IDbTransaction transactionID = connectionID.BeginTransaction())
            {
                try
                {

                    string sqlText = " insert into " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_percent_dom_log " +
                        " (nzp_data_operation, nzp_fp, nzp_payer, nzp_supp, nzp_serv, nzp_area, nzp_geu, perc_ud, " +
                        "   dat_s, dat_po, nzp_rs, nzp_bank, nzp_dom, minpl, nzp_serv_from, nzp_supp_snyat, changed_by, changed_on) " +
                        " Select " + nzp_data_operation_del + ", nzp_fp, nzp_payer, nzp_supp, nzp_serv, nzp_area, nzp_geu, perc_ud, " +
                        "   dat_s, dat_po, nzp_rs, nzp_bank, nzp_dom, minpl, nzp_serv_from, nzp_supp_snyat, " + finder.nzp_user_main + "," + DBManager.sCurDateTime + 
                        " from " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_percent_dom " + 
                        " where nzp_fp = " + finder.nzp_fp;
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                    sqlText = " delete from " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_percent_dom where nzp_fp = " + finder.nzp_fp;
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                    //-----------------------------------------------------------
                    // Завершить транзакцию
                    //-----------------------------------------------------------
                    transactionID.Commit();
                }
                catch (DbException e)
                {
                    //-----------------------------------------------------------
                    // Откатить транзакцию
                    //-----------------------------------------------------------
                    transactionID.Rollback();

                    //-----------------------------------------------------------
                    // обработка ошибки
                    //-----------------------------------------------------------
                    throw new Exception(e.Message);
                }
            }

            return new ReturnsType();
        }

        

        public ReturnsType IsAllowCorrectSaldo(FinderObjectType<Ls> finder, IDbConnection connectionID)
        {
            if (finder.entity.nzp_user < 1) return new ReturnsType("Не задан пользователь");
            if (finder.entity.pref == "") return new ReturnsType("Не задан префикс БД");
            if (finder.entity.nzp_kvar < 1) return new ReturnsType("Не задан ЛС");
            if (finder.entity.dat_calc == "") return new ReturnsType("Не задан текущий расчетный месяц");

            DateTime dt;
            DateTime.TryParse(finder.entity.dat_calc, out dt);

            DateTime dat;
            string d;
#if PG
            string sql = "select MIN(dat_s) as dt from " + finder.entity.pref +
                         "_data.prm_3 where nzp_prm = 51 and val_prm = '1' and is_actual =1 and nzp = " +
                         finder.entity.nzp_kvar;
#else
            string sql = "select MIN(dat_s) as dt from " + finder.entity.pref + "_data:prm_3 where nzp_prm = 51 and val_prm = '1' and is_actual =1 and nzp = " + finder.entity.nzp_kvar;
#endif
            DataTable dtbl = ClassDBUtils.OpenSQL(sql, connectionID).GetData();
            DataRow r = dtbl.Rows[0];
            d = Convert.ToString(r["dt"]);
            DateTime.TryParse(d, out dat);

            if (!(dat.Month == dt.Month && dat.Year == dt.Year))
            {
                return new ReturnsType(true,
                    "Корректировка входящего сальдо разрешена, если лицевой счет открыт в текущем расчетном месяце. " +
                    "Лицевой счет открыт " + dat.ToString("MM.yyyy"), -100);
            }

            dtbl.Clear();

            int tot = 0;
#if PG
            sql = "Select count(*) as cnt from " + finder.entity.pref + "_data.tarif where nzp_kvar=" +
                  finder.entity.nzp_kvar + " and dat_po >= public.mdy(1,1,3000)";
#else
            sql = "Select count(*) as cnt from " + finder.entity.pref + "_data:tarif where nzp_kvar=" + finder.entity.nzp_kvar + " and dat_po >= mdy(1,1,3000)";
#endif
            dtbl = ClassDBUtils.OpenSQL(sql, connectionID).GetData();
            r = dtbl.Rows[0];
            tot = Convert.ToInt32(r["cnt"]);
            if (tot == 0)
            {
                return new ReturnsType(true,
                    "Для корректировки входящего сальдо необходимо предварительно открыть услуги", -200);
            }

            return new ReturnsType(true);
        }

        #region Запись корректировки входящего сальдо

        //-----------------------------------------------------------------------------
        public bool InsOutSaldoInPrevMonth(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int nzp_serv,
            int nzp_supp, decimal rsum, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //входящие параметры:
            //1. параметры расчета - ParamCalc paramcalc = new ParamCalc(nzp_kvar, 0, pref, cur_yy, cur_mm, cur_yy, cur_mm);
            //   paramcalc.num_ls = <num_ls>
            //2. nzp_serv - код услуги
            //3. nzp_supp - код поставщика
            //4. rsum - сумма входящего сальдо 
            //  
            //выходящие параметры:
            //ret.tag = 0 & ret.result = true - нормальное завершение
            //ret.tag = 1 - нет prev_charge_xx
            //ret.tag = 2 - неверные параметры для выполнения 

            if ((paramcalc.nzp_kvar > 0) && (nzp_serv > 0) && (nzp_supp > 0))
            {

                //CalcTypes.ChargeXX chargeXX = new CalcTypes.ChargeXX(paramcalc);

                // prev_charge_xx
                string prev_charge_tab =
                    "charge_" + paramcalc.prev_calc_mm.ToString("00");
                string prev_charge_xx =
                    paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") +
                    DBManager.tableDelimiter + prev_charge_tab;

                ret = ExecSQL(conn_db,
                    " Delete From " + prev_charge_xx +
                    " Where nzp_kvar = " + paramcalc.nzp_kvar + " and nzp_serv= " + nzp_serv + " and nzp_supp=" +
                    nzp_supp
                    , true);
                if (!ret.result)
                {
                    ret.tag = 1;
                    return false;
                }

                if (Math.Abs(rsum) > Convert.ToDecimal(0.0000001))
                {

                    string sql =
                        " Insert into " + prev_charge_xx +
                        " ( nzp_charge, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, tarif_p, gsum_tarif, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, " +
                        " sum_tarif_p,sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                        " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                        " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                        " c_nedop, isdel, c_reval, order_print) " +
                        " values( " + DBManager.sSerialDefault + ", " +
                        paramcalc.nzp_kvar + "," + paramcalc.num_ls + ", " + nzp_serv + ", " + nzp_supp + ", " +
                        "  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, " +
                        rsum + ", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ) ";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }

                }
            }
            else
            {
                ret.result = false;
                ret.tag = 2;
                ret.text = "Неудачная запись корректировки входящего сальдо! (" + paramcalc.nzp_kvar + "/" + nzp_serv +
                           "/" + nzp_supp + ")";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, 1, 2, true);
            }
            return true;
        }

        #endregion Запись корректировки входящего сальдо

        public ReturnsType SaveCorrectSaldo(FinderObjectType<List<Saldo>> finder, IDbConnection connectionID)
        {
            if (finder.entity.Count > 0)
            {
                if (finder.entity[0].nzp_user < 1) return new ReturnsType("Не задан пользователь");
                if (finder.entity[0].pref == "") return new ReturnsType("Не задан префикс БД");
                if (finder.entity[0].nzp_kvar < 1) return new ReturnsType("Не задан ЛС");
                if (finder.entity[0].year_ < 1) return new ReturnsType("Не задан текущий расчетный месяц");
                if (finder.entity[0].month_ < 1) return new ReturnsType("Не задан текущий расчетный месяц");

                string pref = finder.entity[0].pref;
                int nzp_kvar = finder.entity[0].nzp_kvar;
                int year_ = finder.entity[0].year_;
                int month_ = finder.entity[0].month_;

                string sql;
                DataTable dtbl;
                DataRow r;
                int num_ls;
                foreach (Saldo saldo in finder.entity)
                {
#if PG
                    sql = "select num_ls from " + pref + "_data.kvar where nzp_kvar = " + nzp_kvar;
#else
                    sql = "select num_ls from " + pref + "_data:kvar where nzp_kvar = " + nzp_kvar;
#endif
                    dtbl = ClassDBUtils.OpenSQL(sql, connectionID).GetData();
                    r = dtbl.Rows[0];
                    num_ls = Convert.ToInt32(r["num_ls"]);

                    CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, 0, pref, year_, month_, year_,
                        month_);
                    paramcalc.num_ls = num_ls;

                    Returns ret;

                    int nzp_supp = 0;
                    Int32.TryParse(saldo.nzp_supp.ToString(), out nzp_supp);

                    InsOutSaldoInPrevMonth(connectionID, paramcalc, saldo.nzp_serv, nzp_supp, saldo.sum_insaldo, out ret);
                    if (!ret.result)
                    {
                        return new ReturnsType(false);
                    }
                }
            }

            return new ReturnsType(true);
        }

        //для протоколарасчета
        public ReturnsType MakeProtCalc(FinderObjectType<Calcs> finder, IDbConnection conn_db)
        {
            var paramCalc = new CalcTypes.ParamCalc(finder.entity.nzp_kvar, 0, finder.entity.pref,
              finder.entity.year, finder.entity.month, finder.entity.year_, finder.entity.month_);

            Returns ret;
            using (var db = new DbCalcCharge())
            {
                db.MakeProtCalcForMonth(conn_db, paramCalc, finder.entity.nzp_serv, finder.entity.nzp_supp, out ret);
            }

            return new ReturnsType() { result = ret.result, sql_error = ret.sql_error, tag = ret.tag, text = ret.text };
        }

        public Returns SaveGroupPerekidki(ParamsForGroupPerekidki finder)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDbConnection conn_db = GetConnection(Points.GetConnByPref(finder.pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string tXX_spls_perekidki = "t" + Convert.ToString(finder.nzp_user) + "_spls_perekidki";
#if PG
            string tXX_spls_perekidki_full = "public." + tXX_spls_perekidki;
#else
            string tXX_spls_perekidki_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls_perekidki;
#endif
            if (!TempTableInWebCashe(conn_web, tXX_spls_perekidki_full))
            {
                ret = new Returns(false, "Нет данных для сохранения", -1);
                conn_db.Close();
                conn_web.Close();
                return ret;
            }
            string sql = "Select count(*) From " + tXX_spls_perekidki_full;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int cnt;
            try
            {
                cnt = Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                ret = new Returns(false,
                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка SaveGroupPerekidki " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                conn_web.Close();
                return ret;
            }
            if (cnt <= 0)
            {
                ret = new Returns(false, "Нет данных для сохранения", -1);
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            IDataReader reader;

            IDbTransaction transaction;
            transaction = conn_db.BeginTransaction();

            #region определение локального пользователя
            int nzpUser = finder.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            decimal sum = 0;
            if (finder.sum_izm != 0) sum = finder.sum_izm;
            else if (finder.sum_raspr != 0) sum = finder.sum_raspr;

            string nzp_serv_on = "null", nzp_supp_on = "null";
            if (finder.on_nzp_serv > 0) nzp_serv_on = finder.on_nzp_serv.ToString();
            if (finder.on_nzp_supp > 0) nzp_supp_on = finder.on_nzp_supp.ToString();

            ret = SaveDocumentBase(conn_db, transaction, finder.doc_base);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                conn_db.Close();
                return ret;
            }
            finder.doc_base.nzp_doc_base = ret.tag;


#if PG
            sql = "insert into " + Points.Pref + "_fin_" +
                  (Points.DateOper.Year % 100).ToString("00") + ".reestr_perekidok " +
#else
            sql = "insert into " + Points.Pref + "_fin_"+(Points.DateOper.Year % 100).ToString("00")+ "@" + DBManager.getServer(conn_db) + ":reestr_perekidok " +
#endif
 " (nzp_doc_base, dat_uchet, created_by, changed_by, created_on, changed_on,nzp_oper,nzp_serv,nzp_supp,sum_oper,sposob_raspr,comment,nzp_serv_on, nzp_supp_on,saldo_part,type_rcl) " +
                  " values (" + finder.doc_base.nzp_doc_base + ",'" + Points.DateOper.ToShortDateString() + "'," + nzpUser + "," + nzpUser +
#if PG
 ",now(),now(),"
#else
 ",current,current,"
#endif
 + finder.oper_perekidri + "," + finder.nzp_serv + "," + finder.nzp_supp + "," + sum + "," +
                  finder.sposob_raspr + "," + Utils.EStrNull(finder.comment) + "," + nzp_serv_on + "," + nzp_supp_on +
                  "," + finder.saldo_part + "," + finder.type_rcl + ")";
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                conn_db.Close();
                return ret;
            }
            int nzp = GetSerialValue(conn_db, transaction);

#if PG
            sql = "select distinct pref from " + tXX_spls_perekidki_full;
#else
            sql = "select unique pref from " + tXX_spls_perekidki_full;
#endif
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                conn_db.Close();
                return ret;
            }
            string pref_ = "";
            DateTime dt1 = Convert.ToDateTime(finder.dat_uchet);

            while (reader.Read())
            {
                if (reader["pref"] != DBNull.Value) pref_ = Convert.ToString(reader["pref"]).Trim();
                finder.pref = pref_;
                RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(finder.pref));
                DateTime dt2 = new DateTime(r_m.year_, r_m.month_, 1);

                #region определение локального пользователя

                int nzpUserloc = finder.nzp_user;

                /*db = new DbWorkUser();
                int nzpUserloc = db.GetLocalUser(conn_db, transaction, finder, out ret);
                db.Close();
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }*/

                #endregion

#if PG
                sql = "insert into " + pref_ + "_charge_" + (r_m.year_ % 100).ToString("00") + ".perekidka " +
#else
                sql = "insert into " + pref_ + "_charge_" + (r_m.year_ % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + ":perekidka " +
#endif
 "(nzp_kvar, num_ls,nzp_serv,nzp_supp,type_rcl,date_rcl,sum_rcl,month_,comment,nzp_user, nzp_reestr, nzp_doc_base) " +
                      " select nzp_kvar, num_ls,nzp_serv, nzp_supp," + finder.type_rcl + "," +
                      Utils.EStrNull(DateTime.Now.ToShortDateString()) +
                      ",sum_izm," + r_m.month_ + ",'" + finder.comment + "'," + nzpUserloc + "," + nzp + "," +
                      finder.doc_base.nzp_doc_base +
                      " from " + tXX_spls_perekidki_full + " where pref = " + Utils.EStrNull(pref_) + " and sum_izm<>0";
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    conn_db.Close();
                    return ret;
                }
                if (dt1 < dt2) dt1 = dt2;
            }

            transaction.Commit();

            sql = "select distinct nzp_kvar, pref from " + tXX_spls_perekidki_full;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                conn_db.Close();
                return ret;
            }

            while (reader.Read())
            {
                string loc_pref = "";
                int nzpkvar = 0;
                if (reader["pref"] != DBNull.Value) loc_pref = Convert.ToString(reader["pref"]).Trim();
                if (reader["nzp_kvar"] != DBNull.Value) nzpkvar = Convert.ToInt32(reader["nzp_kvar"]);

                #region Добавление в sys_events события

                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = loc_pref,
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6597,
                    nzp_obj = nzpkvar,
                    note = "Была добавлена групповая перекидка. Номер реестра перекидок " + nzp.ToString()
                }, null, conn_db);

                #endregion
            }
            
            // подсчет сальдо
            CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(Points.Pref));
            calcfon.TaskType = CalcFonTask.Types.taskCalcChargeForReestr; //расчет сальдо
            calcfon.Status = FonTask.Statuses.New;
            calcfon.nzp = nzp;
            calcfon.month_ = Points.DateOper.Month;
            calcfon.year_ = Points.DateOper.Year;
            DbCalcQueueClient dbCalc = new DbCalcQueueClient();
            ret = dbCalc.AddTask(conn_db, null, calcfon);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            return ret;
        }

        public List<ParamsForGroupPerekidki> LoadReestrPerekidok(ParamsForGroupPerekidki finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Points.GetConnByPref(finder.pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<ParamsForGroupPerekidki> list = new List<ParamsForGroupPerekidki>();
            IDataReader reader, reader2;
            DbTables tables = new DbTables(conn_db);
#if PG
            string table_reestr_perekidok = Points.Pref + "_fin_" +
                                            (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString("00") +
                                            ".reestr_perekidok";
#else
            string table_reestr_perekidok = Points.Pref + "_fin_" +(Convert.ToDateTime(finder.dat_uchet).Year% 100).ToString("00") +"@" + DBManager.getServer(conn_db) + ":reestr_perekidok";
#endif

            string where = "1=1";
            if (finder.nzp_reestr > 0) where += " and nzp_reestr = " + finder.nzp_reestr;
            if (finder.nzp_kvar > 0)
            {
#if PG
                string perekidka = finder.pref + "_charge_" +
                                   (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString("00") + ".perekidka";
#else
                string perekidka = finder.pref + "_charge_" + (Convert.ToDateTime(finder.dat_uchet).Year% 100).ToString("00") + ":perekidka";
#endif
                if (!TempTableInWebCashe(conn_db, perekidka))
                {
                    conn_db.Close();
                    return list;
                }
                where += " and rp.nzp_reestr in (select nzp_reestr from " + perekidka + " where nzp_kvar = " +
                         finder.nzp_kvar + ")";
            }
            //Определить общее количество записей
#if PG
            string sql = "Select count(*) From " + table_reestr_perekidok + " rp " +
                         " left outer join " + tables.services + " s on rp.nzp_serv = s.nzp_serv " +
                         " left outer join " + tables.supplier + " sp on rp.nzp_supp = sp.nzp_supp ";
            if (!string.IsNullOrEmpty(where)) sql += " where " + where;
#else
            string sql = "Select count(*) From " + table_reestr_perekidok + " rp, outer " +
                         tables.services + " s, outer " + tables.supplier + " sp " +
                         " where rp.nzp_serv = s.nzp_serv and rp.nzp_supp = sp.nzp_supp " + where;
#endif
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try
            {
                recordsTotalCount = Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                ret = new Returns(false,
                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка LoadReestrPerekidok " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }

#if PG
            sql =
                "select rp.nzp_reestr,rp.dat_uchet, rp.created_by, rp.created_on,rp.changed_by,rp.changed_on,rp.nzp_oper,s.service,sp.name_supp,rp.sum_oper,rp.sposob_raspr,rp.comment," +
                "rp.nzp_serv_on, rp.nzp_supp_on,rp.saldo_part from " + table_reestr_perekidok + " rp " +
                " left outer join " + tables.services + " s on rp.nzp_serv = s.nzp_serv " +
                " left outer join " + tables.supplier + " sp on rp.nzp_supp = sp.nzp_supp ";
            if (!string.IsNullOrEmpty(where)) sql += " where " + where;
            sql += " order by dat_uchet desc";
#else
            sql = "select rp.nzp_reestr,rp.dat_uchet, rp.created_by, rp.created_on,rp.changed_by,rp.changed_on,rp.nzp_oper,s.service,sp.name_supp,rp.sum_oper,rp.sposob_raspr,rp.comment," +
                  "rp.nzp_serv_on, rp.nzp_supp_on,rp.saldo_part from " + table_reestr_perekidok + " rp, outer " +
                   tables.services + " s, outer " + tables.supplier + " sp " +
                  " where rp.nzp_serv = s.nzp_serv and rp.nzp_supp = sp.nzp_supp " + where +
                  " order by dat_uchet desc";
#endif

#if PG
            sql =
                "select nzp_reestr,dat_uchet, created_by, created_on,rp.changed_by,rp.changed_on,nzp_oper,service,name_supp,sum_oper,sposob_raspr,rp.comment," +
                "nzp_serv_on, nzp_supp_on,saldo_part, db.nzp_doc_base, db.comment as comment_doc, db.num_doc, db.dat_doc, db.nzp_type_doc, td.doc_name from " +
                table_reestr_perekidok + " rp " +
                " left outer join " + tables.services + " s on rp.nzp_serv = s.nzp_serv " +
                " left outer join " + tables.document_base + " db " +
                " left outer join " + tables.s_type_doc + " td on db.nzp_type_doc = td.nzp_type_doc " +
                " on db.nzp_doc_base = rp.nzp_doc_base " +
                " left outer join " + tables.supplier + " sp on rp.nzp_supp = sp.nzp_supp ";
            if (!string.IsNullOrEmpty(where)) sql += " where " + where;
            sql += " order by nzp_reestr desc";
#else
            sql = "select nzp_reestr, dat_uchet, created_by, created_on,rp.changed_by,rp.changed_on,nzp_oper,service,name_supp,sum,sposob_raspr,rp.comment," +
                  "nzp_serv_on, nzp_supp_on,saldo_part , db.nzp_doc_base, db.comment as comment_doc, db.num_doc, db.dat_doc, db.nzp_type_doc, td.doc_name from " + table_reestr_perekidok + " rp, outer " +
                   tables.services + " s, outer " + tables.supplier + " sp, outer " + tables.document_base + " db, outer " +tables.s_type_doc + " td "+
                  " where "+ where+" and rp.nzp_serv = s.nzp_serv and rp.nzp_supp = sp.nzp_supp and db.nzp_type_doc = td.nzp_type_doc and db.nzp_doc_base = rp.nzp_doc_base "  +
                  " order by nzp_reestr desc";
#endif

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            int i = 0;
            while (reader.Read())
            {
                i++;
                if (i <= finder.skip) continue;
                ParamsForGroupPerekidki pgp = new ParamsForGroupPerekidki();
                pgp.num = i.ToString();
                if (reader["nzp_reestr"] != DBNull.Value) pgp.nzp_reestr = Convert.ToInt32(reader["nzp_reestr"]);
                if (reader["dat_uchet"] != DBNull.Value)
                    pgp.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                if (reader["created_by"] != DBNull.Value)
                    pgp.created_by = String.Format("{0:dd.MM.yyyy}", reader["created_on"]) +
                                     " (" + Convert.ToString(reader["created_by"]).Trim() + ")";
                if (reader["changed_by"] != DBNull.Value)
                    pgp.changed_by = String.Format("{0:dd.MM.yyyy}", reader["changed_on"]) +
                                     " (" + Convert.ToString(reader["changed_by"]).Trim() + ")";
                if (reader["service"] != DBNull.Value) pgp.service = Convert.ToString(reader["service"]).Trim();
                if (reader["name_supp"] != DBNull.Value) pgp.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                if (reader["nzp_oper"] != DBNull.Value) pgp.oper_perekidri = Convert.ToInt32(reader["nzp_oper"]);
                if (reader["sum_oper"] != DBNull.Value) pgp.sum_izm = Convert.ToDecimal(reader["sum_oper"]);
                if (reader["sposob_raspr"] != DBNull.Value) pgp.sposob_raspr = Convert.ToInt32(reader["sposob_raspr"]);
                if (reader["comment"] != DBNull.Value) pgp.comment = Convert.ToString(reader["comment"]).Trim();
                if (reader["nzp_serv_on"] != DBNull.Value) pgp.on_nzp_serv = Convert.ToInt32(reader["nzp_serv_on"]);
                if (reader["nzp_supp_on"] != DBNull.Value) pgp.on_nzp_supp = Convert.ToInt32(reader["nzp_supp_on"]);
                if (reader["saldo_part"] != DBNull.Value) pgp.saldo_part = Convert.ToInt32(reader["saldo_part"]);

                if (reader["nzp_doc_base"] != DBNull.Value)
                    pgp.doc_base.nzp_doc_base = Convert.ToInt32(reader["nzp_doc_base"]);
                if (reader["nzp_type_doc"] != DBNull.Value)
                    pgp.doc_base.nzp_type_doc = Convert.ToInt32(reader["nzp_type_doc"]);
                if (reader["comment_doc"] != DBNull.Value)
                    pgp.doc_base.comment = Convert.ToString(reader["comment_doc"]).Trim();
                if (reader["num_doc"] != DBNull.Value)
                    pgp.doc_base.num_doc = Convert.ToString(reader["num_doc"]).Trim();
                if (reader["doc_name"] != DBNull.Value)
                    pgp.doc_base.doc_name = Convert.ToString(reader["doc_name"]).Trim();
                if (reader["dat_doc"] != DBNull.Value)
                    pgp.doc_base.dat_doc = Convert.ToDateTime(reader["dat_doc"]).ToShortDateString();


                pgp.oper_perekidri_text = ParamsForGroupPerekidki.GetOperationNameById(pgp.oper_perekidri);

                if (pgp.saldo_part == SaldoPart.Negative.GetHashCode())
                    pgp.saldo_part_text = "Только отрицательную часть";
                else if (pgp.saldo_part == SaldoPart.Positive.GetHashCode())
                    pgp.saldo_part_text = "Только положительную часть";
                else if (pgp.saldo_part == SaldoPart.PositiveNegative.GetHashCode())
                    pgp.saldo_part_text = "Положительную и отрицательную части";

                if (pgp.sposob_raspr == SposobRaspr.TotSquare.GetHashCode())
                    pgp.oper_perekidri_text = "Пропорционально общей площади";
                if (pgp.sposob_raspr == SposobRaspr.OtoplSquare.GetHashCode())
                    pgp.oper_perekidri_text = "Пропорционально отапливаемой площади";
                else if (pgp.sposob_raspr == SposobRaspr.CountGil.GetHashCode())
                    pgp.oper_perekidri_text = "Пропорционально количеству жильцов";
                else if (pgp.sposob_raspr == SposobRaspr.Ravnomerno.GetHashCode())
                    pgp.oper_perekidri_text = "Равномерно";

                if (pgp.on_nzp_serv > 0)
                {
                    sql = "select service from " + tables.services + " where nzp_serv = " + pgp.on_nzp_serv;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        CloseReader(ref reader);
                        conn_db.Close();
                        return null;
                    }
                    if (reader2.Read())
                    {
                        if (reader2["service"] != DBNull.Value)
                            pgp.on_service = Convert.ToString(reader2["service"]).Trim();
                    }
                    reader2.Close();
                }

                if (pgp.on_nzp_supp > 0)
                {
                    sql = "select name_supp from " + tables.services + " where nzp_supp = " + pgp.on_nzp_supp;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        CloseReader(ref reader);
                        conn_db.Close();
                        return null;
                    }
                    if (reader2.Read())
                    {
                        if (reader2["name_supp"] != DBNull.Value)
                            pgp.on_name_supp = Convert.ToString(reader2["name_supp"]).Trim();
                    }
                    reader2.Close();
                }

                list.Add(pgp);
                if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
            }

            ret.tag = recordsTotalCount;
            reader.Close();
            return list;
        }

        public Returns DeleteFromReestrPerekidok(ParamsForGroupPerekidki finder)
        {
            IDbConnection conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ret = DeleteFromReestrPerekidok(finder, conn_db);

            conn_db.Close();

            return ret;
        }

        public Returns DeleteFromReestrPerekidok(ParamsForGroupPerekidki finder, IDbConnection conn_db)
        {
            if (finder.nzp_user <= 0)
            {
                return new Returns(false, "Не задан пользователь");
            }

            if (finder.nzp_reestr <= 0)
            {
                return new Returns(false, "Не задан код реестра");
            }

            Returns ret;
            int nzpUser;
            nzpUser = finder.nzp_user;

            /*using (DbWorkUser db = new DbWorkUser())
            {
                finder.pref = Points.Pref;
                nzpUser = db.GetLocalUser(conn_db, finder, out ret);
                if (!ret.result) return ret;
            }*/

            string sql = "";
            string table_reestr_perekidok = Points.Pref + "_fin_" +
                                            (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString("00") +
                                            tableDelimiter + "reestr_perekidok";

            DbTables tbls = new DbTables(conn_db);

            #region Определяем расчетные годы, затрагиваемые реестром

            MyDataReader reader;
            sql = "select rp.dat_uchet, tr.nzp_type_uchet from " + table_reestr_perekidok + " rp, " + tbls.s_typercl +
                  " tr where rp.type_rcl = tr.type_rcl and  rp.nzp_reestr = " + finder.nzp_reestr;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                return ret;
            }
            int nzp_type_uchet = 0;
            DateTime datuchet = DateTime.MinValue;
            try
            {
                if (reader.Read())
                {
                    if (reader["dat_uchet"] != DBNull.Value) datuchet = Convert.ToDateTime(reader["dat_uchet"]);
                    if (reader["nzp_type_uchet"] != DBNull.Value)
                        nzp_type_uchet = Convert.ToInt32(reader["nzp_type_uchet"]);
                }
            }
            finally
            {
                reader.Close();
            }

            if (datuchet == DateTime.MinValue)
            {
                ret = new Returns(false, "Не задан операционный день");
                return ret;
            }

            #endregion

            List<string> tables = new List<string>();
            List<string> prefs = new List<string>();

            string perekidka_table;

            //Признак, что нужно создавать отменяющий реестр
            bool createNewReestr = false;

            //Определяем необходимость создания отменяющего реестра и проверяем наличие таблиц
            //Если изменения сальдо хотя бы в одном банке выполнены в прошлых расчетных месяцах, то необходимо создать отменяющий реестр
            //Если все изменения выполнены в текущем расчетном месяце, то можно удалить реестр
            foreach (_Point point in Points.PointList)
            {
                //проверяем наличие таблиц
                //perekidka_table = point.pref + "_charge_" + (j % 100).ToString("00") + tableDelimiter + "perekidka";
                perekidka_table = point.pref + "_charge_" +
                                  (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString("00") + tableDelimiter +
                                  "perekidka";
                if (!TempTableInWebCashe(conn_db, perekidka_table)) continue;

                tables.Add(perekidka_table);
                prefs.Add(point.pref);
            }

            if (nzp_type_uchet == TypeUchetPerekidka.Oplats.GetHashCode()) //если перекидки оплатами, то проверки
            {
                //в пределах операционного дня
                if (Points.DateOper == datuchet) createNewReestr = false;
                else createNewReestr = true;
            }
            else
            {
                //в пределах операционного месяца
                if (Points.DateOper.Month == datuchet.Month && Points.DateOper.Year == datuchet.Year)
                    createNewReestr = false;
                else createNewReestr = true;
            }

            ret = ExecSQL(conn_db,
                "create temp table t_reestrperekidok_ls (nzp_kvar integer, pref char(20)) " + sUnlogTempTable, true);
            if (!ret.result) return ret;

            if (createNewReestr)
            {
                ////Создаем отменяющий реестр
                List<ParamsForGroupPerekidki> reestr =
                    LoadReestrPerekidok(new ParamsForGroupPerekidki() { nzp_reestr = finder.nzp_reestr }, out ret);
                if (!ret.result) return ret;

                if (reestr == null || reestr.Count == 0)
                {
                    ret.result = false;
                    ret.text = "Не найден реестр изменений сальдо с кодом " + finder.nzp_reestr;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Warn, true);
                    return ret;
                }

                using (IDbTransaction transaction = conn_db.BeginTransaction())
                {
                    //Создание отменяющего реестра
                    sql = "insert into " + table_reestr_perekidok +
                          " (dat_uchet, comment, sposob_raspr, nzp_oper, nzp_serv, nzp_supp, nzp_serv_on, nzp_supp_on, saldo_part, sum_oper, is_actual, changed_by, changed_on, created_by, created_on, nzp_doc_base, type_rcl)" +
                          " select '" + Points.DateOper.ToShortDateString() + "', 'Отмена реестра " +
                          reestr[0].nzp_reestr +
                          "', sposob_raspr, nzp_oper, nzp_serv, nzp_supp, nzp_serv_on, nzp_supp_on, saldo_part, -sum_oper, is_actual, " +
                          nzpUser + ", " + sCurDateTime + ", " + nzpUser + ", " + sCurDateTime +
                          ", nzp_doc_base, type_rcl " +
                          " from " + table_reestr_perekidok + " where nzp_reestr = " + finder.nzp_reestr;

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) return ret;

                    int newReestrId = GetSerialValue(conn_db, transaction);

                    ret = ExecSQL(conn_db, transaction, "create temp table t_perekidka (dt integer) " + sUnlogTempTable,
                        true);
                    if (!ret.result) return ret;

                    try
                    {
                        foreach (_Point point in Points.PointList)
                        {
                            nzpUser = finder.nzp_user;
                            
                            /*using (DbWorkUser db = new DbWorkUser())
                            {
                                finder.pref = point.pref;
                                nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret);
                                if (!ret.result) return ret;
                            }*/

                            perekidka_table = point.pref + "_charge_" + (datuchet.Year % 100).ToString("00") +
                                              tableDelimiter + "perekidka";
                            if (!tables.Contains(perekidka_table)) continue;

                            sql = "insert into " + point.pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") +
                                  tableDelimiter + "perekidka " +
                                  " (nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, date_rcl, tarif, volum, sum_rcl, month_, nzp_user, nzp_reestr,  nzp_doc_base) " +
                                  " select nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, " + sCurDateTime +
                                  ", case when tarif is null then null else -tarif end" +
                                  ", case when volum is null then null else -volum end, -sum_rcl, " +
                                  Points.DateOper.Month + ", " + nzpUser + ", " + newReestrId +
                                  ", nzp_doc_base " +
                                  " from " + perekidka_table + " where nzp_reestr = " + finder.nzp_reestr;
                            ret = ExecSQL(conn_db, transaction, sql, true);
                            if (!ret.result) return ret;

                            ret = ExecSQL(conn_db, transaction,
                                "insert into t_perekidka (dt) select distinct " + Points.DateOper.Year.ToString("0000") +
                                Points.DateOper.Month.ToString("00") +
                                " from " + point.pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") +
                                tableDelimiter + "perekidka " +
                                " where nzp_reestr = " + newReestrId, true);
                            if (!ret.result) return ret;

                            sql = "select nzp_kvar from " + perekidka_table + " where nzp_reestr = " + finder.nzp_reestr;
                            ret = ExecRead(conn_db, out reader, sql, true);
                            if (!ret.result) return ret;
                            List<int> nzp_kvars = new List<int>();
                            try
                            {
                                while (reader.Read())
                                    if (reader["nzp_kvar"] != DBNull.Value)
                                        nzp_kvars.Add(Convert.ToInt32(reader["nzp_kvar"]));
                            }
                            finally
                            {
                                reader.Close();
                            }

                            #region Добавление в sys_events события

                            foreach (int nzp_kvar in nzp_kvars)
                            {
                                DbAdmin.InsertSysEvent(new SysEvents()
                                {
                                    pref = point.pref,
                                    nzp_user = finder.nzp_user,
                                    nzp_dict = 6597, //добавление перекидки
                                    nzp_obj = nzp_kvar,
                                    note =
                                        "Создание отменяющего реестра. Перекидка на отрицательную сумму была добавлена"
                                }, transaction, conn_db);
                            }

                            #endregion

                            ret = ExecSQL(conn_db, transaction, "insert into t_reestrperekidok_ls (nzp_kvar, pref) " +
                                                                "select nzp_kvar,'" + point.pref + "' from " +
                                                                perekidka_table + " where nzp_reestr = " +
                                                                finder.nzp_reestr, true);
                            if (!ret.result) return ret;
                        }
                    }
                    finally
                    {
                        ExecSQL(conn_db, transaction, "drop table t_perekidka", true);
                    }

                    transaction.Commit();

                    ret.tag = -1;
                    ret.text =
                        "Реестр изменений сальдо не удален, т.к. изменения сальдо были выполнены в закрытом расчетном месяце. Вместо удаления создан отменяющий реестр № " +
                        newReestrId;
                }
            }
            else
            {
                //Удаляем реестр
                for (int i = 0; i < tables.Count; i++)
                {
                    //список nzp_kvar из таблицы perekidka для реестра finder.nzp_reestr
                    sql = "select nzp_kvar from " + tables[i] + " where nzp_reestr = " + finder.nzp_reestr;
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result) return ret;
                    List<int> nzp_kvars = new List<int>();
                    try
                    {
                        while (reader.Read())
                            if (reader["nzp_kvar"] != DBNull.Value)
                                nzp_kvars.Add(Convert.ToInt32(reader["nzp_kvar"]));
                    }
                    finally
                    {
                        reader.Close();
                    }

                    ret = ExecSQL(conn_db, "insert into t_reestrperekidok_ls (nzp_kvar, pref) " +
                                           "select nzp_kvar,'" + prefs[i] + "' from " + tables[i] +
                                           " where nzp_reestr = " + finder.nzp_reestr, true);
                    if (!ret.result) return ret;

                    IDbTransaction tr = conn_db.BeginTransaction();
                    sql = "delete from " + tables[i] + " where nzp_reestr = " + finder.nzp_reestr;
                    ret = ExecSQL(conn_db, tr, sql, true);
                    if (!ret.result)
                    {
                        tr.Rollback();
                        return ret;
                    }

                    #region Добавление в sys_events события

                    foreach (int nzp_kvar in nzp_kvars)
                    {
                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = prefs[i],
                            nzp_user = finder.nzp_user,
                            nzp_dict = 6598, //удаление перекидки
                            nzp_obj = nzp_kvar,
                            note = "Удаление реестра. Перекидка была удалена"
                        }, tr, conn_db);
                    }

                    #endregion

                    if (ret.result) tr.Commit();
                    else tr.Rollback();
                }

                sql = "select nzp_doc_base from " + table_reestr_perekidok + " where nzp_reestr = " + finder.nzp_reestr;
                Object obj = ExecScalar(conn_db, sql, out ret, true);
                int nzp_doc_base = 0;
                try
                {
                    nzp_doc_base = Convert.ToInt32(obj);
                }
                catch
                {
                }
                TDocumentBase db = new TDocumentBase();
                db.nzp_doc_base = nzp_doc_base;

                sql = "delete from " + table_reestr_perekidok + " where nzp_reestr = " + finder.nzp_reestr;
                ret = ExecSQL(conn_db, sql, true);

                sql = "select count(*) from " + table_reestr_perekidok + " where nzp_doc_base = " + db.nzp_doc_base;
                obj = ExecScalar(conn_db, sql, out ret, true);
                int count = 0;
                try
                {
                    count = Convert.ToInt32(obj);
                }
                catch
                {
                }
                if (count == 0)
                {
                    ret = DeleteDocumentBase(conn_db, null, db);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
            }


            using (DbCalcCharge db = new DbCalcCharge())
            {
                Returns ret2 = db.CalcChargeXXForDelReestrPerekidok(conn_db, null, finder, "t_reestrperekidok_ls");
            }


            return ret;
        }

        public Returns SavePerekidkiLsToLs(List<PerekidkaLsToLs> listfinder, ParamsForGroupPerekidki reestr)
        {
            Returns ret = Utils.InitReturns();

            if (listfinder == null || listfinder.Count == 0) return new Returns(false, "Нет данных для сохранения", -1);

            IDbConnection conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            if (reestr.nzp_reestr > 0) DeleteFromReestrPerekidok(reestr, conn_db);
#if PG
            string table_reestr_perekidok = Points.Pref + "_fin_" +
                                            (Convert.ToDateTime(reestr.dat_uchet).Year % 100).ToString("00") +
                                            tableDelimiter + "reestr_perekidok";
#else
            string table_reestr_perekidok = Points.Pref +  "_fin_" + (Convert.ToDateTime(reestr.dat_uchet).Year % 100).ToString("00") +"@" + DBManager.getServer(conn_db) + ":reestr_perekidok";
#endif
            IDbTransaction transaction;
            transaction = conn_db.BeginTransaction();

            #region определение локального пользователя

            int nzpUser = listfinder[0].nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, transaction, listfinder[0], out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/

            #endregion

            ret = SaveDocumentBase(conn_db, transaction, listfinder[0].doc_base);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            listfinder[0].doc_base.nzp_doc_base = ret.tag;

            //сохранение в reestr_perekidok
            //string sql = " insert into " + table_reestr_perekidok + "(nzp_oper,month_,year_, nzp_doc_base,month_2,year_2,sum,is_actual,created_by,created_on) " +
            //             " values (" + (int)ParamsForGroupPerekidki.Operations.PerekidkaLsToLs + "," + listfinder[0].month_ + "," + listfinder[0].year_ +
            //             "," + listfinder[0].doc_base.nzp_doc_base + 
            //             "," + listfinder[0].month_2 + "," + listfinder[0].year_2 + "," +

            string sql = " insert into " + table_reestr_perekidok +
                         "(nzp_oper,dat_uchet,type_rcl,nzp_doc_base,sum_oper,is_actual,created_by,changed_by,created_on,changed_on) " +
                         " values (" + (int)ParamsForGroupPerekidki.Operations.PerekidkaLsToLs + ",'" +
                         listfinder[0].dat_uchet + "'," +
                         listfinder[0].type_rcl + "," + listfinder[0].doc_base.nzp_doc_base + "," +
#if PG
 reestr.sum_raspr + ",1," + nzpUser + "," + nzpUser + ",now(),now())";
#else
 reestr.sum_raspr + ",1," + nzpUser + "," + nzpUser + ",current,current)";
#endif
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            int nzp_reestr = GetSerialValue(conn_db, transaction);

            foreach (PerekidkaLsToLs plsls in listfinder)
            {
                if (plsls.distr_sum != "")
                {
#if PG
                    sql = " insert into " + plsls.pref + "_charge_" +
                          (Convert.ToDateTime(plsls.dat_uchet).Year % 100).ToString("00") + ".perekidka " +
#else
                    sql = " insert into " + plsls.pref + "_charge_" + (Convert.ToDateTime(plsls.dat_uchet).Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + ":perekidka " +
#endif
 " (nzp_kvar, num_ls,nzp_serv,nzp_supp,type_rcl,date_rcl,sum_rcl,month_,nzp_user, nzp_reestr, " +
                          "nzp_doc_base) " +
                          " values " +
                          "(" + plsls.nzp_kvar + ", " +
                          GetNumLsFromNzpKvar(plsls.nzp_kvar, plsls.pref, conn_db, transaction) + "," +
                          plsls.nzp_serv + "," + plsls.nzp_supp + "," + plsls.type_rcl + "," +
                          Utils.EStrNull(DateTime.Now.ToShortDateString()) + "," +
                          plsls.distr_sum + "," + Convert.ToDateTime(plsls.dat_uchet).Month + "," + nzpUser + "," +
                          nzp_reestr + "," +
                          listfinder[0].doc_base.nzp_doc_base +
                          ")";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }

                    #region Добавление в sys_events события

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = plsls.pref,
                        nzp_user = listfinder[0].nzp_user,
                        nzp_dict = 6597,
                        nzp_obj = plsls.nzp_kvar,
                        note = "Перекидка на сумму " + plsls.distr_sum + " была добавлена"
                    }, transaction, conn_db);

                    #endregion
                }

                if (plsls.distr_sum2 != "")
                {
#if PG
                    sql = " insert into " + plsls.pref2 + "_charge_" +
                          (Convert.ToDateTime(plsls.dat_uchet).Year % 100).ToString("00") + ".perekidka " +
#else
                    sql = " insert into " + plsls.pref2 + "_charge_" + (Convert.ToDateTime(plsls.dat_uchet).Year % 100).ToString("00") + "@" + DBManager.getServer(conn_db) + ":perekidka " +
#endif
 " (nzp_kvar, num_ls,nzp_serv,nzp_supp,type_rcl,date_rcl,sum_rcl,month_,nzp_user, nzp_reestr," +
                          "nzp_doc_base) " +
                          " values " +
                          "(" + plsls.nzp_kvar2 + ", " +
                          GetNumLsFromNzpKvar(plsls.nzp_kvar2, plsls.pref2, conn_db, transaction) + "," +
                          plsls.nzp_serv + "," + plsls.nzp_supp + "," + plsls.type_rcl + "," +
                          Utils.EStrNull(DateTime.Now.ToShortDateString()) + "," +
                          plsls.distr_sum2 + "," + Convert.ToDateTime(plsls.dat_uchet).Month + "," + nzpUser + "," +
                          nzp_reestr + "," +
                          listfinder[0].doc_base.nzp_doc_base +
                          ")";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }

                    #region Добавление в sys_events события

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = plsls.pref2,
                        nzp_user = listfinder[0].nzp_user,
                        nzp_dict = 6597,
                        nzp_obj = plsls.nzp_kvar2,
                        note = "Перекидка на сумму " + plsls.distr_sum2 + " была добавлена"
                    }, transaction, conn_db);

                    #endregion
                }
            }
            transaction.Commit();

            // подсчет сальдо
            CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(Points.Pref));
            calcfon.TaskType = CalcFonTask.Types.taskCalcChargeForReestr; //расчет сальдо
            calcfon.Status = FonTask.Statuses.New;
            calcfon.nzp = nzp_reestr;
            calcfon.month_ = Convert.ToDateTime(listfinder[0].dat_uchet).Month;
            calcfon.year_ = Convert.ToDateTime(listfinder[0].dat_uchet).Year;
            DbCalcQueueClient dbCalc = new DbCalcQueueClient();
            ret = dbCalc.AddTask(conn_db, null, calcfon);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            ret.tag = nzp_reestr;
            return ret;
        }


        private bool GetNachForMustCalc(IDbConnection conn_db, ref IDbCommand cmd,
            ref StringBuilder sql, string tableCharge, int nzp_kvar)
        {
            IDataReader reader2 = null;

            sql.Remove(0, sql.Length);
            sql.Append(" select ordering, service_name as service, su.payer as name_supp,");
            sql.Append(" a.tarif,a.nzp_serv,  a.nzp_frm,a.nzp_supp, max(is_device) as is_device, ");
            sql.Append(" sum(gsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop,");
            sql.Append(" sum(rsum_tarif-gsum_tarif+reval-sum_nedop) as reval, sum(0) as sum_sn, ");
            sql.Append(" sum(real_charge) as real_charge, sum(sum_outsaldo) as sum_outsaldo, ");
            sql.Append(
                " sum(sum_charge) as sum_charge, sum(sum_money) as sum_money, sum(rsum_tarif-gsum_tarif) as reval_gil, ");
            sql.Append(" sum(sum_insaldo) as sum_insaldo, max(c_calc) as c_calc, max(c_reval) as c_reval ");
#if PG
            sql.Append(" from  " + tableCharge + " a, " + Points.Pref + "_kernel.services s,  ");
            sql.Append(Points.Pref + "_kernel.s_payer su ");
#else
            sql.Append(" from  " + tableCharge + " a, " + Points.Pref + "_kernel:services s,  ");
            sql.Append(Points.Pref + "_kernel:s_payer su ");
#endif
            sql.Append(" where nzp_kvar=" + nzp_kvar.ToString() + " and a.nzp_serv=s.nzp_serv ");
            sql.Append(" and dat_charge is null   and a.nzp_serv>1 and a.nzp_supp = su.nzp_supp ");
            sql.Append(" group by 1,2,3,4,5,6,7");
            sql.Append(" order by ordering,nzp_serv ");
            cmd = DBManager.newDbCommand(sql.ToString(), conn_db);

            try
            {
                reader2 = cmd.ExecuteReader();
                while (reader2.Read())
                {
                    BaseServ serv;
                    if ((Int32.Parse(reader2["nzp_serv"].ToString()) > 509) &
                        (Int32.Parse(reader2["nzp_serv"].ToString()) < 518))
                    {
                        serv = new BaseServ(true);
                    }
                    else
                    {
                        serv = new BaseServ(false);
                    }
                    serv.Serv.NzpServ = Int32.Parse(reader2["nzp_serv"].ToString());

                    serv.Serv.NameServ = reader2["service"].ToString();
                    if (((serv.Serv.NzpServ > 5) & (serv.Serv.NzpServ < 11)) || (serv.Serv.NzpServ == 25) ||
                        ((serv.Serv.NzpServ > 509) & (serv.Serv.NzpServ < 518)))
                    {
                        serv.Serv.NameSupp = "-" + reader2["name_supp"].ToString();
                    }

                    if ((serv.Serv.NzpServ == 14) & (Int32.Parse(reader2["nzp_supp"].ToString()) == 612))
                    {
                        serv.Serv.NameServ = "Хол.вода на ГВС";
                    }

                    if ((serv.Serv.NzpServ == 14))
                    {
                        serv.Serv.NameSupp = "-" + reader2["name_supp"].ToString();
                    }

                    serv.Serv.Ordering = Int32.Parse(reader2["ordering"].ToString());


                    serv.Serv.OldMeasure = serv.Serv.NzpMeasure;
                    serv.Serv.Tarif = reader2["tarif"] != DBNull.Value ? Convert.ToDecimal(reader2["tarif"]) : 0;
                    // Добавил Андрей Кайнов 19.12.2012

                    serv.Serv.RsumTarif = Decimal.Parse(reader2["rsum_tarif"].ToString());
                    serv.Serv.SumNedop = Decimal.Parse(reader2["sum_nedop"].ToString());
                    serv.Serv.Reval = Decimal.Parse(reader2["reval"].ToString());
                    serv.Serv.RevalGil = Decimal.Parse(reader2["reval_gil"].ToString());
                    serv.Serv.RealCharge = Decimal.Parse(reader2["real_charge"].ToString());
                    serv.Serv.SumCharge = Decimal.Parse(reader2["sum_charge"].ToString());
                    serv.Serv.SumMoney = Decimal.Parse(reader2["sum_money"].ToString());
                    serv.Serv.SumInsaldo = Decimal.Parse(reader2["sum_insaldo"].ToString());
                    serv.Serv.SumSn = Decimal.Parse(reader2["sum_sn"].ToString());
                    serv.Serv.SumOutsaldo = Decimal.Parse(reader2["sum_outsaldo"].ToString());
                    serv.Serv.CCalc = Decimal.Parse(reader2["c_calc"].ToString());
                    serv.Serv.CReval = Decimal.Parse(reader2["c_reval"].ToString());
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001))
                        serv.Serv.CCalc = 0; // Добавил Андрей Кайнов 19.12.2012



                    serv.CopyToOdn();
                    AddServ(serv);
                }
                cmd.Dispose();
                reader2.Close();
                reader2.Dispose();
                return true;
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Ошибка при выборке начислений " + e.Message, MonitorLog.typelog.Error, true);
                if (cmd != null)
                {
                    cmd.Dispose();
                }
                if (reader2 != null)
                {
                    reader2.Close();
                    reader2.Dispose();
                }
                return false;
            }

        }

        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected virtual bool FillRevalReason(DataRow dr)
        {
            if (dr == null) return false;

            int kanNumber = -1;
            string hvsReason = "";
            string gvsReason = "";
            for (int i = 0; i < 6; i++)
            {
                if (listReval.Count > i)
                {
                    dr["serv_pere" + (i + 1).ToString()] = listReval[i].ServiceName;
                    if (listReval[i].NzpServ == 7) kanNumber = i + 1;
                    if (listReval[i].Reason == null)
                    {
                        if (System.Math.Abs(listReval[i].CReval) < 0.001m)
                        {
                            if (System.Math.Abs(listReval[i].SumGilReval) < 0.001m)
                            {
                                dr["osn_pere" + (i + 1).ToString()] = "Изменение тарифа/недопоставка ";
                            }
                            else
                                dr["osn_pere" + (i + 1).ToString()] = "Временное выбытие жильца";

                        }
                        else
                        {
                            dr["osn_pere" + (i + 1).ToString()] = "Изменение расхода по услуге";
                        }
                    }
                    else
                        dr["osn_pere" + (i + 1).ToString()] = listReval[i].Reason;
                    dr["sum_pere" + (i + 1).ToString()] = listReval[i].SumReval;

                    if (listReval[i].NzpServ == 6) hvsReason = dr["osn_pere" + (i + 1).ToString()].ToString();
                    if (listReval[i].NzpServ == 9) gvsReason = dr["osn_pere" + (i + 1).ToString()].ToString();
                }
                else
                {
                    dr["serv_pere" + (i + 1).ToString()] = "";
                    dr["osn_pere" + (i + 1).ToString()] = "";
                    dr["sum_pere" + (i + 1).ToString()] = "";

                }
            }
            if (kanNumber > -1)
            {
                if (hvsReason != "")
                    dr["osn_pere" + kanNumber] = hvsReason;
                else
                    dr["osn_pere" + kanNumber] = gvsReason;
            }

            return true;
        }

        /// <summary>
        /// Заполение 1 строки резульирующей таблицы данными ЛС
        /// </summary>
        /// <param name="dt">результирующая таблица</param>
        /// <returns></returns>
        public virtual bool FillRow(DataTable dt)
        {
            DataRow dr = dt.Rows.Add();
            FillRevalReason(dr);
            return false;
        }

        public List<ServReval> GetListReason(StreamWriter writer, out Returns ret, SupgFinder finder,
            IDbConnection conn_db, ref IDbCommand cmd, string year, string month)
        {
            ret = Utils.InitReturns();
            IDataReader reader3 = null;

            StringBuilder sql = new StringBuilder();
            List<_Point> prefixs = new List<_Point>();
            _Point point = new _Point();
            if (year == null || month == null)
            {
                ret.result = false;
                ret.text = "Не определен расчетный срок";
                ret.result = false;

                return null;
            }
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.result = false;

                return null;
            }

            if (finder.pref != "")
            {
                point.pref = finder.pref;
                prefixs.Add(point);
            }
            else
            {
                prefixs = Points.PointList;
            }

            string conn_kernel = Points.GetConnByPref(Points.Pref);




            string s_year = year.Substring(2);


            string baseData = finder.pref + "_data";
            string tablePerekidka = finder.pref + "_charge_" + (int.Parse(year) - 2000).ToString("00") +
#if PG
 ".perekidka ";
#else
 ":perekidka ";
#endif

            #region Загружаем причины перерасчета

            //Недопоставки
            sql.Remove(0, sql.Length);
            sql.Append(" select a.nzp_serv  ");
#if PG
            sql.Append(" from  " + baseData + ".nedop_kvar a  ");
#else
            sql.Append(" from  " + baseData + ":nedop_kvar a  ");
#endif
            sql.Append(" where a.nzp_kvar=" + finder.nzp_kvar + " and month_calc = " +
#if PG
 "public.MDY(");
#else
 "MDY(");
#endif
            sql.Append(month + ",01," + year + ") ");
            sql.Append(" and is_actual<>100 ");
            sql.Append(" group by 1");

            cmd = DBManager.newDbCommand(sql.ToString(), conn_db);
            try
            {
                reader3 = cmd.ExecuteReader();
                while (reader3.Read())
                {
                    if (reader3["nzp_serv"] != DBNull.Value)
                    {
                        AddReasonReval(Int32.Parse(reader3["nzp_serv"].ToString()), "Недопоставка");
                    }
                }
                reader3.Close();
                reader3.Dispose();
                cmd.Dispose();
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                cmd.Dispose();
                conn_db.Close();
                return null;
            }



            //Счетчики
            sql.Remove(0, sql.Length);
            sql.Append(" select a.nzp_serv ");
#if PG
            sql.Append(" from  " + baseData + ".counters a ");
#else
            sql.Append(" from  " + baseData + ":counters a ");
#endif
            sql.Append(" where a.nzp_kvar=" + finder.nzp_kvar + " and month_calc =" +
#if PG
 " public.MDY(");
#else
 " MDY(");
#endif
            sql.Append(month + ",01," + year + ") ");
            sql.Append(" and is_actual<>100 and month(dat_uchet)<" +
#if PG
 "public.MDY(");
#else
 "MDY(");
#endif
            sql.Append(month + ",01," + year + ")");
            sql.Append(" group by 1");
            cmd = DBManager.newDbCommand(sql.ToString(), conn_db);
            try
            {
                reader3 = cmd.ExecuteReader();
                while (reader3.Read())
                {
                    if (reader3["nzp_serv"] != DBNull.Value)
                    {
                        AddReasonReval(Int32.Parse(reader3["nzp_serv"].ToString()), " Показания счетчиков");
                    }
                }
                reader3.Close();
                reader3.Dispose();
                cmd.Dispose();
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                cmd.Dispose();
                conn_db.Close();
                return null;
            }



            //Счетчики
            sql.Remove(0, sql.Length);

#if PG
            sql.Append(" select a.nzp_serv, val_prm, coalesce(num_cnt,'') as num_cnt ");
            sql.Append(" from  " + baseData + ".counters_spis a, " + baseData + ".prm_17 p");
            sql.Append(" where nzp_type=3 and a.nzp=" + finder.nzp_kvar + " and p.month_calc = public.MDY(");
#else
            sql.Append(" select a.nzp_serv, val_prm, nvl(num_cnt,'') as num_cnt ");
            sql.Append(" from  " + baseData + ":counters_spis a, " + baseData + ":prm_17 p");
            sql.Append(" where nzp_type=3 and a.nzp=" + finder.nzp_kvar + " and p.month_calc = MDY(");
#endif

            sql.Append(month + ",01," + year + ") ");
            sql.Append(" and p.is_actual<>100 and a.is_actual<>100 and a.nzp_counter=p.nzp and p.nzp_prm=2027 ");

            cmd = DBManager.newDbCommand(sql.ToString(), conn_db);
            try
            {
                reader3 = cmd.ExecuteReader();
                while (reader3.Read())
                {
                    if (reader3["val_prm"] != DBNull.Value)
                    {
                        AddReasonReval(Int32.Parse(reader3["nzp_serv"].ToString()),
                            " счетчик №" + reader3["num_cnt"].ToString()
                            + " опломбирован " + DateTime.Parse(reader3["val_prm"].ToString()).ToShortDateString());
                    }
                }
                reader3.Close();
                reader3.Dispose();
                cmd.Dispose();
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                cmd.Dispose();
                conn_db.Close();
                return null;
            }

            //Перекидки
            sql.Remove(0, sql.Length);
            sql.Append(" select nzp_serv, comment ");
            sql.Append(" from  " + tablePerekidka);
            sql.Append(" where nzp_kvar =" + finder.nzp_kvar.ToString() + " and month_ = ");
            sql.Append(month + "  group by 1,2 ");
            if (!ExecRead(conn_db, out reader3, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }
            while (reader3.Read())
            {
                if ((reader3["nzp_serv"] != DBNull.Value) & (reader3["comment"] != DBNull.Value))
                {
                    if (reader3["comment"].ToString().Trim() != "")
                        AddReasonReval(Int32.Parse(reader3["nzp_serv"].ToString()),
                            reader3["comment"].ToString().Trim());
                }
            }
            reader3.Close();

            #endregion

            return listReval;
        }

        public virtual void AddReasonReval(int nzpServ, string reason)
        {
            ServReval aReval;
            for (int i = 0; i < listReval.Count; i++)
            {
                if (listReval[i].NzpServ == nzpServ)
                {
                    aReval = listReval[i];
                    if (aReval.Reason != null)
                    {
                        aReval.Reason += "," + reason;
                    }
                    else
                    {
                        aReval.Reason = reason;
                    }
                    listReval[i] = aReval;
                }
            }


        }

        /// <summary>
        /// Добавление услуги в счета
        /// </summary>
        /// <param name="cServ">Правила объединения услуг</param>
        /// <param name="aServ">Услуга</param>
        public virtual void AddServ(BaseServ aServ)
        {

            //Проверка на пустоту добавляемой услуги
            if (aServ.Empty()) return;

            for (int i = 0; i < ListKommServ.Count; i++)
            {
                if (ListKommServ[i].Serv.NzpServ == aServ.Serv.NzpServ)
                {
                    aServ.KommServ = true;
                }
            }

            summaryServ.AddSum(aServ.Serv); //Подсчитываем Итого
            if (System.Math.Abs(aServ.Serv.Reval) > 0.01m || (System.Math.Abs(aServ.Serv.RealCharge) > 0.001m))
            {
                AddReval(aServ.Serv);
            }
            BaseServ mainServ = cUnionServ.GetMainServBySlave(aServ.Serv.NzpServ);
            //Определяем к какой услуге добавить
            if (mainServ == null) //услуга не объединяемая
            {
                bool findServ = false;
                for (int i = 0; i < listServ.Count; i++)
                {
                    if (listServ[i].Serv.NzpServ == aServ.Serv.NzpServ) //если такая услуга уже есть то добавляем к ней
                    {
                        listServ[i].AddSum(aServ.Serv);
                        if (listServ[i].Serv.NameSupp == "")
                        {
                            listServ[i].Serv.NameSupp = aServ.Serv.NameSupp;
                        }
                        else
                        {
                            if (aServ.Serv.Tarif > 0)
                            {
                                listServ[i].Serv.NameSupp = aServ.Serv.NameSupp;
                            }
                        }
                        findServ = true;
                    }
                }
                if (!findServ) listServ.Add(aServ); //иначе добавляем новую
            }
            else //если услуга объединяемая
            {
                bool findServ = false;
                for (int i = 0; i < listServ.Count; i++)
                {
                    if (listServ[i].Serv.NzpServ == mainServ.Serv.NzpServ)
                    //если мастер услуга уже присутствует, то добавляем к ней
                    {
                        if (aServ.Serv.IsOdn) aServ.Serv.CanAddTarif = false;


                        if (aServ.Serv.Tarif > 0)
                        {
                            listServ[i].Serv.NameSupp = aServ.Serv.NameSupp;
                        }
                        listServ[i].AddSum(aServ.Serv);
                        findServ = true;
                    }
                }
                if (!findServ) //Не найдена объединенная услуга, добавляем её
                {
                    BaseServ newMainServ = (BaseServ)mainServ.Clone();
                    newMainServ.KommServ = aServ.KommServ;

                    if (aServ.Serv.Tarif > 0)
                    {
                        newMainServ.Serv.NameSupp = aServ.Serv.NameSupp;

                    }
                    newMainServ.Serv.Measure = aServ.Serv.Measure;
                    newMainServ.AddSum(aServ.Serv);
                    listServ.Add(newMainServ);
                }
            }

        }

        public virtual void AddReval(SumServ aServ)
        {
            ServReval aReval;
            bool findServ = false;


            for (int i = 0; i < listReval.Count; i++)
            {
                if (listReval[i].NzpServ == aServ.NzpServ)
                {
                    aReval = listReval[i];
                    aReval.SumReval = aServ.Reval + aServ.RealCharge;
                    aReval.CReval = aServ.CReval;
                    aReval.SumGilReval = aServ.RevalGil;
                    listReval[i] = aReval;
                    findServ = true;
                }
            }

            if (findServ == false)
            {
                aReval = new ServReval();
                aReval.NzpServ = aServ.NzpServ;
                aReval.ServiceName = aServ.NameServ;
                aReval.SumReval = aServ.Reval + aServ.RealCharge;
                aReval.CReval = aServ.CReval;
                aReval.SumGilReval = aServ.RevalGil;
                listReval.Add(aReval);
            }



        }

        public StreamWriter InfoAboutServices(List<string> pref, string Pref, StreamWriter writer, IDbConnection conn)
        {
            string sqlString = "";

            if (!ExecSQL(conn,
                " drop table tmp_inf_serv;", false).result)
            {
            }

#if PG
            sqlString = "create unlogged table tmp_inf_serv( num_ls char(20), " +
                        " nzp_supp integer, " +
                        " nzp_serv integer, " +
                        " nzp_frm integer,  " +
                        " sum_insaldo     numeric(14,2), " +
                        " tarif           numeric(14,3), " +
                        " proc_reg_tarif  numeric(14,3), " +
                        " tarif_f         numeric(14,3), " +
                        " nzp_measure     integer,  " +
                        " rashod_fact     numeric(14,2), " +
                        "  c_sn            numeric(14,2), " +
                        "  is_device       integer," +
                        "  sum_tarif        numeric(14,2)," +
                        " sum_pere_izm_saldo    numeric(14,2)," +
                        " sum_dotac        numeric(14,2)," +
                        " sum_pere_dot        numeric(14,2)," +
                        " sum_lgot          numeric(14,2)," +
                        " sum_pere_lgot        numeric(14,2)," +
                        " sum_smo               numeric(14,2)," +
                        " sum_smo_pred          numeric(14,2)," +
                        " sum_money             numeric(14,2)," +
                        "  isdel                 integer," +
                        "  sum_outsaldo         numeric(14,2)," +
                        " kol_ls  integer " +
                        "); ";
#else
            sqlString = "create temp table tmp_inf_serv( num_ls char(20), " +
             " nzp_supp integer, " +
             " nzp_serv integer, " +
             " nzp_frm integer,  " +
             " sum_insaldo     decimal(14,2), " +
             " tarif           decimal(14,3), " +
             " proc_reg_tarif  decimal(14,3), " +
             " tarif_f         decimal(14,3), " +
             " nzp_measure     integer,  " +
             " rashod_fact     decimal(14,2), " +
             "  c_sn            decimal(14,2), " +
             "  is_device       integer," +
             "  sum_tarif        decimal(14,2)," +
             " sum_pere_izm_saldo    decimal(14,2)," +
             " sum_dotac        decimal(14,2)," +
             " sum_pere_dot        decimal(14,2)," +
             " sum_lgot          decimal(14,2)," +
             " sum_pere_lgot        decimal(14,2)," +
             " sum_smo               decimal(14,2)," +
             " sum_smo_pred          decimal(14,2)," +
             " sum_money             decimal(14,2)," +
             "  isdel                 integer," +
             "  sum_outsaldo         decimal(14,2)," +
             " kol_ls  integer " +
            ")with no log; ";
#endif

            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
                sqlString =
                    "   insert into tmp_inf_serv(num_ls ,  nzp_supp,  nzp_serv,nzp_frm, sum_insaldo, tarif, proc_reg_tarif, tarif_f , " +
                    " nzp_measure , rashod_fact , c_sn ,is_device,  sum_tarif, sum_pere_izm_saldo  , sum_dotac , sum_pere_dot , " +
                    " sum_lgot  , sum_pere_lgot , sum_smo   ,sum_smo_pred , sum_money   , isdel   ,  sum_outsaldo) " +
                    " select  c.num_ls, c.nzp_supp, c.nzp_serv,c.nzp_frm, c.sum_insaldo, c.tarif, 0, c.tarif, 0, case when c.tarif=0 then 0 else c.sum_tarif/c.tarif end,  " +
                    " c.c_sn, c.is_device, c.sum_tarif, c.reval+real_charge, 0, 0,0,0,0,0, c.sum_money, c.isdel, c.sum_outsaldo " +
#if PG
 " from " + p + "_charge_13.charge_04 c " +
#else
 " from " + p + "_charge_13:charge_04 c " +
#endif
 " where c.dat_charge is null and c.nzp_serv>1 ; ";

                ClassDBUtils.OpenSQL(sqlString, conn);
            }


            sqlString = " create index ix_0 on tmp_inf_serv(num_ls); ";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = " create index ix_1 on tmp_inf_serv(nzp_serv); ";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "  create index ix_2 on tmp_inf_serv(nzp_frm);  ";
            ClassDBUtils.OpenSQL(sqlString, conn);
#if PG
            sqlString = "  analyze tmp_inf_serv; ";
#else
            sqlString = "  update statistics for table tmp_inf_serv; ";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);


            sqlString = " UPDATE tmp_inf_serv " +
                        " SET nzp_measure = (SELECT s.nzp_measure FROM   " + Pref + "_kernel" +
#if PG
 "."
#else
 ":"
#endif
 + "services s WHERE s.nzp_serv = tmp_inf_serv.nzp_serv) " +
                        " WHERE nzp_frm<=0; ";
            ClassDBUtils.OpenSQL(sqlString, conn);

            sqlString = " UPDATE tmp_inf_serv " +
                        " SET nzp_measure = (SELECT f.nzp_measure FROM   " + Pref + "_kernel" +
#if PG
 "."
#else
 ":"
#endif
 + "formuls f  WHERE tmp_inf_serv.nzp_frm = f.nzp_frm  ) " +
                        " WHERE nzp_frm>0 ; ";
            ClassDBUtils.OpenSQL(sqlString, conn);


            sqlString = " UPDATE tmp_inf_serv " +
                        " SET is_device  = (CASE WHEN (is_device =1 or is_device =3 or is_device =5 or is_device =7 ) THEN  1 ELSE   0 " +
                        " END ) ; ";
            ClassDBUtils.OpenSQL(sqlString, conn);

            sqlString = " UPDATE tmp_inf_serv " +
                        " SET kol_ls  = ( select count(*) from tmp_pere_supp s where s.num_ls=tmp_inf_serv.num_ls)";
            ClassDBUtils.OpenSQL(sqlString, conn);

            return writer;
        }

        public int WriteCharge(StreamWriter writer, IDbConnection conn)
        {
            IDataReader reader = null;
            string sqlString = "";
            sqlString = "select num_ls ,  nzp_supp,  nzp_serv, sum_insaldo, tarif, proc_reg_tarif, tarif_f , " +
                        "  nzp_measure , rashod_fact , c_sn ,is_device,  sum_tarif, sum_pere_izm_saldo  , sum_dotac , sum_pere_dot , " +
                        " sum_lgot  , sum_pere_lgot , sum_smo   ,sum_smo_pred , sum_money   , isdel   ,  sum_outsaldo, kol_ls " +
                        "  from tmp_inf_serv order by  num_ls, nzp_supp, nzp_serv ";
            int i = 0;
            if (!ExecRead(conn, out reader, sqlString, true).result)
            {
                conn.Close();
                return 0;
            }
            try
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {

                        string str = "6|" +
                                     (reader["num_ls"] != DBNull.Value
                                         ? ((string)reader["num_ls"]).ToString().Trim() + "|"
                                         : "|") +
                                     (reader["nzp_supp"] != DBNull.Value ? ((int)reader["nzp_supp"]) + "|" : "|") +
                                     (reader["nzp_serv"] != DBNull.Value ? ((int)reader["nzp_serv"]) + "|" : "|") +
                                     (reader["sum_insaldo"] != DBNull.Value
                                         ? ((Decimal)reader["sum_insaldo"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["tarif"] != DBNull.Value
                                         ? ((Decimal)reader["tarif"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["proc_reg_tarif"] != DBNull.Value
                                         ? ((Decimal)reader["proc_reg_tarif"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["tarif_f"] != DBNull.Value
                                         ? ((Decimal)reader["tarif_f"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["nzp_measure"] != DBNull.Value ? ((int)reader["nzp_measure"]) + "|" : "|") +
                                     (reader["rashod_fact"] != DBNull.Value
                                         ? ((Decimal)reader["rashod_fact"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["c_sn"] != DBNull.Value
                                         ? ((Decimal)reader["c_sn"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["is_device"] != DBNull.Value ? ((int)reader["is_device"]) + "|" : "|") +
                                     (reader["sum_tarif"] != DBNull.Value
                                         ? ((Decimal)reader["sum_tarif"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_pere_izm_saldo"] != DBNull.Value
                                         ? ((Decimal)reader["sum_pere_izm_saldo"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_dotac"] != DBNull.Value
                                         ? ((Decimal)reader["sum_dotac"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_pere_dot"] != DBNull.Value
                                         ? ((Decimal)reader["sum_pere_dot"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_lgot"] != DBNull.Value
                                         ? ((Decimal)reader["sum_lgot"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_pere_lgot"] != DBNull.Value
                                         ? ((Decimal)reader["sum_pere_lgot"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_smo"] != DBNull.Value
                                         ? ((Decimal)reader["sum_smo"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_smo_pred"] != DBNull.Value
                                         ? ((Decimal)reader["sum_smo_pred"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_money"] != DBNull.Value
                                         ? ((Decimal)reader["sum_money"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["isdel"] != DBNull.Value ? ((int)reader["isdel"]) + "|" : "|") +
                                     (reader["sum_outsaldo"] != DBNull.Value
                                         ? ((Decimal)reader["sum_outsaldo"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["kol_ls"] != DBNull.Value ? ((int)reader["kol_ls"]) + "|" : "|");

                        writer.WriteLine(str);
                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных в DataTable в GetUploadLS " + ex.Message,
                    MonitorLog.typelog.Error, true);
                reader.Close();
                return 0;
            }

            writer.Flush();

            return i;
        }



        public StreamWriter LsUploadPererash(List<string> pref, StreamWriter writer, IDbConnection conn, string year,
            string month)
        {
            IDataReader reader = null;
            string sqlString = "";
            if (!ExecSQL(conn,
                " drop table temp_ls_perer;", false).result)
            {
            }

#if PG
            sqlString =
                "  create unlogged table temp_ls_perer(month_ char(10), year_ char(10), nzp_dom integer, num_ls integer, typek integer, " +
                " fio char(100), nkvar char(10), nkvar_n char(3),date_ls_open date,date_ls_close date, nzp_kvar integer, kol_prib integer, " +
                " kol_vr_prib integer, kol_vr_ubiv integer,kol_kom integer,obch_ploch numeric, gil_ploch numeric,otapl_ploch numeric, " +
                " kom_kvar integer,  nal_el_plit integer, nal_gaz_plit integer,  nal_gaz_kol integer,nal_ognev_plit integer,kod_tip_gil integer, " +
                " kod_tip_gil_otopl integer,kod_tip_gil_kan integer, nal_zab integer,kol_uslyga integer,kol_perer integer, kol_ind_prib_ucheta integer " +
                " ) ";
#else
            sqlString = "  create temp table temp_ls_perer(month_ char(10), year_ char(10), nzp_dom integer, num_ls integer, typek integer, " +
              " fio char(100), nkvar char(10), nkvar_n char(3),date_ls_open date,date_ls_close date, nzp_kvar integer, kol_prib integer, " +
              " kol_vr_prib integer, kol_vr_ubiv integer,kol_kom integer,obch_ploch decimal, gil_ploch decimal,otapl_ploch decimal, " +
              " kom_kvar integer,  nal_el_plit integer, nal_gaz_plit integer,  nal_gaz_kol integer,nal_ognev_plit integer,kod_tip_gil integer, " +
              " kod_tip_gil_otopl integer,kod_tip_gil_kan integer, nal_zab integer,kol_uslyga integer,kol_perer integer, kol_ind_prib_ucheta integer " +
              " )with no log ";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "select  month_, year_ from " + p + "_charge_" + year + ".lnk_charge_" + month +
                            "  group by 1,2 order by 1,2";
#else
                sqlString = "select  month_, year_ from " + p + "_charge_" + year + ":lnk_charge_" + month + "  group by 1,2 order by 1,2";
#endif

                if (!ExecRead(conn, out reader, sqlString, true).result)
                {
                    conn.Close();
                    return null;
                }
                try
                {
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            string month_ = ((Int16)reader["month_"]) < 10
                                ? "0" + ((Int16)reader["month_"]).ToString()
                                : ((Int16)reader["month_"]).ToString() + "";
                            string year_ = ((Int16)reader["year_"]).ToString().Substring(2, 2);
#if PG
                            sqlString =
                                "insert into temp_ls_perer(nzp_kvar, year_ , month_) select distinct ch.nzp_kvar , " +
                                year_ + " ," + month_ + " from " +
                                p + "_charge_" + year_ + ".charge_" + month_ +
                                " ch where ch.nzp_serv>1 and ch.dat_charge ='28." + month + "." + year + "'";
#else
                            sqlString = "insert into temp_ls_perer(nzp_kvar, year_ , month_) select unique ch.nzp_kvar , " + year_ + " ," + month_ + " from " +
                                p + "_charge_" + year_ + ":charge_" + month_ + " ch where ch.nzp_serv>1 and ch.dat_charge ='28." + month + "." + year + "'";
#endif
                            ClassDBUtils.OpenSQL(sqlString, conn);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в GetUploadLS " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    reader.Close();
                    return null;
                }

            }
            reader = null;


            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls_perer set " +
                            "nzp_dom=(select k.nzp_dom from " + p +
                            "_data.kvar k where temp_ls_perer.nzp_kvar=k.nzp_kvar), " +
                            "num_ls=(select k.num_ls from " + p +
                            "_data.kvar k where temp_ls_perer.nzp_kvar=k.nzp_kvar), " +
                            "typek=(select (case when k.typek=3 then 2 when k.typek<>3 then 1 end) from " + p +
                            "_data.kvar k where temp_ls_perer.nzp_kvar=k.nzp_kvar), " +
                            "fio=(select k.fio from " + p + "_data.kvar k where temp_ls_perer.nzp_kvar=k.nzp_kvar), " +
                            "nkvar=(select k.nkvar from " + p +
                            "_data.kvar k where temp_ls_perer.nzp_kvar=k.nzp_kvar), " +
                            "nkvar_n=(select k.nkvar_n from " + p +
                            "_data.kvar k where temp_ls_perer.nzp_kvar=k.nzp_kvar), " +
                            "kol_prib=(select " + p +
                            "_data.get_kol_gil(public.mdy(5,1,2013),public.mdy(6,1,2013),15,k.nzp_kvar,0) from " + p +
                            "_data.kvar k where temp_ls_perer.nzp_kvar=k.nzp_kvar) ";
#else
                sqlString = "  update temp_ls_perer set (nzp_dom,num_ls,typek, fio, nkvar, nkvar_n, kol_prib )= " +
                 " (( select k.nzp_dom,k.num_ls,( case when k.typek=3 then 2 when k.typek<>3 then 1 end ) " +
                " ,k.fio,k.nkvar,k.nkvar_n, " + p + "_data:get_kol_gil(mdy(5,1,2013),mdy(6,1,2013),15,k.nzp_kvar,0) from " + p +
                "_data:kvar k where temp_ls_perer.nzp_kvar=k.nzp_kvar   ))";
#endif

                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set date_ls_open=(select min(dat_s) from " + p +
                            "_data.prm_3 p where nzp_prm=51 and temp_ls_perer.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013)and val_prm='1')";
#else
                sqlString = "update temp_ls_perer set date_ls_open=(select min(dat_s) from " + p + "_data:prm_3 p where nzp_prm=51 and temp_ls_perer.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013)and val_prm='1')";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set date_ls_close=(select max(dat_s) from " + p +
                            "_data.prm_3 p where nzp_prm=51 and temp_ls_perer.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013)and val_prm='2')";
#else
                sqlString = "update temp_ls_perer set date_ls_close=(select max(dat_s) from " + p + "_data:prm_3 p where nzp_prm=51 and temp_ls_perer.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013)and val_prm='2')";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set kol_vr_prib=(select max(replace(val_prm, ',', '.')) from " + p +
                            "_data.prm_1 p where nzp_prm=131 and temp_ls_perer.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "update temp_ls_perer set kol_vr_prib=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=131 and temp_ls_perer.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls_perer set kol_vr_ubiv= (select count( distinct nzp_gilec) from " + p +
                            "_data.gil_periods p where temp_ls_perer.nzp_kvar=p.nzp_kvar  and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "  update temp_ls_perer set kol_vr_ubiv= (select count( unique nzp_gilec) from " + p + "_data:gil_periods p where temp_ls_perer.nzp_kvar=p.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set kol_kom=(select max(replace(val_prm, ',', '.')) from " + p +
                            "_data.prm_1 p where nzp_prm=107 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "update temp_ls_perer set kol_kom=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=107 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p +
                            "_data.prm_1 p where nzp_prm=4 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "update temp_ls_perer set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=4 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set gil_ploch=(select max(replace(val_prm, ',', '.')) from " + p +
                            "_data.prm_1 p where nzp_prm=6 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "update temp_ls_perer set gil_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=6 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set otapl_ploch=(select max(replace(val_prm, ',', '.')) from " + p +
                            "_data.prm_1 p where nzp_prm=133 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "update temp_ls_perer set otapl_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=133 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls_perer set kom_kvar = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls_perer set kom_kvar= (select 1 from " + p +
                            "_data.prm_1 p where p.val_prm='2' and p.nzp_prm=3 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "  update temp_ls_perer set kom_kvar= (select 1 from " + p + "_data:prm_1 p where p.val_prm='2' and p.nzp_prm=3 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls_perer set nal_el_plit = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls_perer set nal_el_plit= (select 1 from " + p +
                            "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=19 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "  update temp_ls_perer set nal_el_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=19 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls_perer set nal_gaz_plit = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls_perer set nal_gaz_plit= (select 1 from " + p +
                            "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=551 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "  update temp_ls_perer set nal_gaz_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=551 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls_perer set nal_gaz_kol = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls_perer set nal_gaz_kol= (select 1 from  " + p +
                            "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=1 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "  update temp_ls_perer set nal_gaz_kol= (select 1 from  " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=1 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls_perer set nal_ognev_plit = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls_perer set nal_ognev_plit= (select 1 from " + p +
                            "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=1172 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "  update temp_ls_perer set nal_ognev_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=1172 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set kod_tip_gil=(select max(replace(val_prm, ',', '.')) from " + p +
                            "_data.prm_1 p where nzp_prm=7 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "update temp_ls_perer set kod_tip_gil=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=7 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }


            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " +
                            p +
                            "_data.prm_1 p where nzp_prm=894 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "update temp_ls_perer set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=894 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            foreach (var p in pref)
            {
#if PG
                sqlString = "update temp_ls_perer set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " +
                            p +
                            "_data.prm_2 p where nzp_prm=38 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "update temp_ls_perer set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=38 and temp_ls_perer.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_ls_perer set  nal_zab = 0";
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "  update temp_ls_perer set nal_zab= (select 1 from " + p +
                            "_data.prm_2 p where p.val_prm='1' and p.nzp_prm=35 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<public.mdy(6,1,2013) and dat_po>=public.mdy(5,1,2013))";
#else
                sqlString = "  update temp_ls_perer set nal_zab= (select 1 from " + p + "_data:prm_2 p where p.val_prm='1' and p.nzp_prm=35 and p.nzp=temp_ls_perer.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(6,1,2013) and dat_po>=mdy(5,1,2013))";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            return writer;
        }

        public int WriteLsUploadPererash(StreamWriter writer, IDbConnection conn, bool is_il)
        {

            IDataReader reader = null;
            string sqlString = "";
            sqlString = " select * from  temp_ls_perer order by nzp_dom,fio, nkvar, nkvar_n, nzp_kvar, num_ls";
            int i = 0;
            if (!ExecRead(conn, out reader, sqlString, true).result)
            {
                conn.Close();
                return 0;
            }
            try
            {
                if (reader != null)
                {

                    while (reader.Read())
                    {
                        string[] names =
                            (reader["fio"] != DBNull.Value ? ((string)reader["fio"]).ToString().Trim() : "").Split(' ');

                        string first_name = (names.Length == 3 ? names[0] : "");
                        string name = (names.Length == 3 ? names[1] : "");
                        string second_name = (names.Length == 3 ? names[2] : "");

                        string str = "7|" +
                                     "01." +
                                     (reader["month_"] != DBNull.Value
                                         ? ((string)reader["month_"]).ToString().Trim() + ".20"
                                         : ".20") +
                                     (reader["year_"] != DBNull.Value
                                         ? ((string)reader["year_"]).ToString().Trim()
                                         : "") +
                                     (reader["nzp_dom"] != DBNull.Value ? ((int)reader["nzp_dom"]) + "|" : "|") +
                                     (reader["num_ls"] != DBNull.Value ? ((int)reader["num_ls"]) + "|" : "|") +
                                     (reader["typek"] != DBNull.Value ? ((int)reader["typek"]) + "|" : "|") +
                                     (is_il != true
                                         ? (first_name + "|" + name + "|" + second_name + "||")
                                         : "ФИО " +
                                           (reader["num_ls"] != DBNull.Value
                                               ? ((string)reader["num_ls"]).ToString().Trim() + "|"
                                               : "|")) +
                                     (reader["nkvar"] != DBNull.Value
                                         ? ((string)reader["nkvar"]).ToString().Trim() + "|"
                                         : "|") +
                                     (reader["nkvar_n"] != DBNull.Value
                                         ? ((string)reader["nkvar_n"]).ToString().Trim() + "|"
                                         : "|") +
                                     (reader["date_ls_open"] != DBNull.Value
                                         ? ((DateTime)reader["date_ls_open"]).ToString("dd.MM.yyyy") + "||"
                                         : "||") +
                                     (reader["date_ls_close"] != DBNull.Value
                                         ? ((DateTime)reader["date_ls_close"]).ToString("dd.MM.yyyy") + "||"
                                         : "||") +
                                     (reader["nzp_kvar"] != DBNull.Value ? ((int)reader["nzp_kvar"]) + "|" : "|") +
                                     (reader["kol_prib"] != DBNull.Value ? ((int)reader["kol_prib"]) + "|" : "|") +
                                     (reader["kol_vr_prib"] != DBNull.Value ? ((int)reader["kol_vr_prib"]) + "|" : "|") +
                                     (reader["kol_vr_ubiv"] != DBNull.Value ? ((int)reader["kol_vr_ubiv"]) + "|" : "|") +
                                     (reader["kol_kom"] != DBNull.Value ? ((int)reader["kol_kom"]) + "|" : "|") +
                                     (reader["obch_ploch"] != DBNull.Value
                                         ? ((Decimal)reader["obch_ploch"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["gil_ploch"] != DBNull.Value
                                         ? ((Decimal)reader["gil_ploch"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["otapl_ploch"] != DBNull.Value
                                         ? ((Decimal)reader["otapl_ploch"]).ToString("0.00").Trim() + "||"
                                         : "||") +
                                     (reader["kom_kvar"] != DBNull.Value ? ((int)reader["kom_kvar"]) + "|" : "|") +
                                     (reader["nal_el_plit"] != DBNull.Value ? ((int)reader["nal_el_plit"]) + "|" : "|") +
                                     (reader["nal_gaz_plit"] != DBNull.Value
                                         ? ((int)reader["nal_gaz_plit"]) + "|"
                                         : "|") +
                                     (reader["nal_gaz_kol"] != DBNull.Value ? ((int)reader["nal_gaz_kol"]) + "|" : "|") +
                                     (reader["nal_ognev_plit"] != DBNull.Value
                                         ? ((int)reader["nal_ognev_plit"]) + "||"
                                         : "||") +
                                     (reader["kod_tip_gil"] != DBNull.Value ? ((int)reader["kod_tip_gil"]) + "|" : "|") +
                                     (reader["kod_tip_gil_otopl"] != DBNull.Value
                                         ? ((int)reader["kod_tip_gil_otopl"]) + "|"
                                         : "|") +
                                     (reader["kod_tip_gil_kan"] != DBNull.Value
                                         ? ((int)reader["kod_tip_gil_kan"]) + "|"
                                         : "|") +
                                     (reader["nal_zab"] != DBNull.Value ? ((int)reader["nal_zab"]) + "||" : "||");

                        writer.WriteLine(str);
                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных  " + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                return 0;
            }

            writer.Flush();

            return i;
        }

        public StreamWriter InfAboutChargeServ(List<string> pref, string Pref, StreamWriter writer, IDbConnection conn,
            int year, int month)
        {
            IDataReader reader = null;
            string sqlString = "";
            if (!ExecSQL(conn, " drop table tmp_pere_supp;", false).result)
            {
            }

#if PG
            sqlString =
                "  create unlogged table tmp_pere_supp(time_pere    char(20), num_ls      char(20), nzp_supp    integer, " +
                " nzp_serv    integer,  nzp_frm     integer, tarif           numeric(14,3), proc_reg_tarif  numeric(14,3), " +
                "   tarif_f         numeric(14,3),  nzp_measure integer, rashod_fact     numeric(14,2), c_sn       numeric(14,2), " +
                "  is_device       integer, reval numeric(14,2),sum_pere_dot   numeric(14,2), sum_pere_lgot  numeric(14,2), " +
                " sum_pere_smo   numeric(14,2)    ) ";
#else
            sqlString = "  create temp table tmp_pere_supp(time_pere    char(20), num_ls      char(20), nzp_supp    integer, " +
              " nzp_serv    integer,  nzp_frm     integer, tarif           decimal(14,3), proc_reg_tarif  decimal(14,3), " +
                "   tarif_f         decimal(14,3),  nzp_measure integer, rashod_fact     decimal(14,2), c_sn       decimal(14,2), " +
                 "  is_device       integer, reval decimal(14,2),sum_pere_dot   decimal(14,2), sum_pere_lgot  decimal(14,2), " +
                 " sum_pere_smo   decimal(14,2)    ) with no log; ";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = "select  month_, year_ from " + p + "_charge_" + year.ToString().Substring(2) +
                            ".lnk_charge_05  group by 1,2 order by 1,2";
#else
                sqlString = "select  month_, year_ from " + p + "_charge_" + year.ToString().Substring(2) + ":lnk_charge_05  group by 1,2 order by 1,2";
#endif

                if (!ExecRead(conn, out reader, sqlString, true).result)
                {
                    conn.Close();
                    return null;
                }
                try
                {
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            string month_ = ((Int16)reader["month_"]) < 10
                                ? "0" + ((Int16)reader["month_"]).ToString()
                                : ((Int16)reader["month_"]).ToString() + "";
                            string year_ = ((Int16)reader["year_"]).ToString().Substring(2, 2);
                            sqlString =
                                "     insert into tmp_pere_supp(time_pere, num_ls ,  nzp_supp,  nzp_serv,nzp_frm, tarif, proc_reg_tarif, tarif_f , nzp_measure , rashod_fact , c_sn ,is_device, " +
                                " reval, sum_pere_dot , sum_pere_lgot ,sum_pere_smo)   " +
                                " select  '" + "01." + month_ + ".20" + year_ +
                                "',c.num_ls, c.nzp_supp, c.nzp_serv, c.nzp_frm, c.tarif, 0, c.tarif, 0, case when c.tarif=0 then 0 else c.sum_tarif/c.tarif end, " +
#if PG
 " c.c_sn, c.is_device,  c.reval, 0, 0,0    from " + p + "_charge_" + year_ +
                                ".charge_" + month_ + " c " +
#else
 " c.c_sn, c.is_device,  c.reval, 0, 0,0    from " + p + "_charge_" + year_ + ":charge_" + month_ + " c " +
#endif
 "  where c.dat_charge='28." + "05." + year + "' and c.nzp_serv>1 ;";
                            ClassDBUtils.OpenSQL(sqlString, conn);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка получения перерасчетных месяцев  " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    reader.Close();
                    return null;
                }

            }


            sqlString = " create index ixs_0 on tmp_pere_supp(num_ls);  ";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = " create index ixs_1 on tmp_pere_supp(nzp_serv); ";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = " create index ixs_2 on tmp_pere_supp(nzp_frm); ";
            ClassDBUtils.OpenSQL(sqlString, conn);
#if PG
            sqlString = " analyze tmp_pere_supp; ";
#else
            sqlString = " update statistics for table tmp_pere_supp; ";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);


#if PG
            sqlString = " UPDATE tmp_pere_supp SET nzp_measure = (SELECT s.nzp_measure FROM   " + Pref +
                        "_kernel.services s WHERE s.nzp_serv = tmp_pere_supp.nzp_serv) WHERE nzp_frm<=0; ";
#else
            sqlString = " UPDATE tmp_pere_supp SET nzp_measure = (SELECT s.nzp_measure FROM   " + Pref + "_kernel:services s WHERE s.nzp_serv = tmp_pere_supp.nzp_serv) WHERE nzp_frm<=0; ";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);
#if PG
            sqlString = "UPDATE tmp_pere_supp SET nzp_measure = (SELECT f.nzp_measure FROM   " + Pref +
                        "_kernel.formuls f  WHERE tmp_pere_supp.nzp_frm = f.nzp_frm  ) WHERE nzp_frm>0 ; ";
#else
            sqlString = "UPDATE tmp_pere_supp SET nzp_measure = (SELECT f.nzp_measure FROM   " + Pref + "_kernel:formuls f  WHERE tmp_pere_supp.nzp_frm = f.nzp_frm  ) WHERE nzp_frm>0 ; ";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString =
                " UPDATE tmp_pere_supp SET is_device  = (CASE WHEN (is_device =1 or is_device =3 or is_device =5 or is_device =7 ) THEN  1 ELSE   0 END ) ; ";
            ClassDBUtils.OpenSQL(sqlString, conn);

            return writer;
        }


        public int WriteInfAboutChargeServ(StreamWriter writer, IDbConnection conn)
        {
            IDataReader reader = null;
            string sqlString = "";
            sqlString =
                "select time_pere, num_ls ,  nzp_supp,  nzp_serv, tarif, proc_reg_tarif, tarif_f , nzp_measure , rashod_fact , c_sn ,is_device, " +
                " reval, sum_pere_dot , sum_pere_lgot ,sum_pere_smo    from tmp_pere_supp " +
                "   order by time_pere, num_ls, nzp_supp,  nzp_serv ";
            int i = 0;
            if (!ExecRead(conn, out reader, sqlString, true).result)
            {
                conn.Close();
                return 0;
            }
            try
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {

                        string str = "8|" +
                                     (reader["time_pere"] != DBNull.Value
                                         ? ((string)reader["time_pere"]).ToString().Trim() + "|"
                                         : "|") +
                                     (reader["num_ls"] != DBNull.Value
                                         ? ((string)reader["num_ls"]).ToString().Trim() + "|"
                                         : "|") +
                                     (reader["nzp_supp"] != DBNull.Value ? ((int)reader["nzp_supp"]) + "|" : "|") +
                                     (reader["nzp_serv"] != DBNull.Value ? ((int)reader["nzp_serv"]) + "|" : "|") +
                                     (reader["tarif"] != DBNull.Value
                                         ? ((Decimal)reader["tarif"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["proc_reg_tarif"] != DBNull.Value
                                         ? ((Decimal)reader["proc_reg_tarif"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["tarif_f"] != DBNull.Value
                                         ? ((Decimal)reader["tarif_f"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["nzp_measure"] != DBNull.Value
                                         ? ((int)reader["nzp_measure"]) + "||"
                                         : "||") +
                                     (reader["rashod_fact"] != DBNull.Value
                                         ? ((Decimal)reader["rashod_fact"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["c_sn"] != DBNull.Value
                                         ? ((Decimal)reader["c_sn"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["is_device"] != DBNull.Value ? ((int)reader["is_device"]) + "|" : "|") +
                                     (reader["reval"] != DBNull.Value
                                         ? ((Decimal)reader["reval"]).ToString("0.00").Trim() + "|||"
                                         : "|||") +
                                     (reader["sum_pere_dot"] != DBNull.Value
                                         ? ((Decimal)reader["sum_pere_dot"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_pere_lgot"] != DBNull.Value
                                         ? ((Decimal)reader["sum_pere_lgot"]).ToString("0.00").Trim() + "|"
                                         : "|") +
                                     (reader["sum_pere_smo"] != DBNull.Value
                                         ? ((Decimal)reader["sum_pere_smo"]).ToString("0.00").Trim() + "|||"
                                         : "|||");


                        writer.WriteLine(str);
                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка записи данных в reader  " + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                return 0;
            }

            writer.Flush();

            return i;

        }

        /// <summary>
        /// Для заданного реестра перекидок формирует выбранный список лицевых счетов
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns FindLsForReestrPerekidok(ParamsForGroupPerekidki finder)
        {
            #region проверка входных параметров

            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.nzp_reestr < 1)
            {
                return new Returns(false, "Не определен реестр изменений сальдо");
            }

            if (finder.dat_uchet == "")
            {
                return new Returns(false, "Операционный день не определен");
            }

            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";

            //создать кэш-таблицу
            using (var db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, true);
            }
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            StringBuilder sql = new StringBuilder();

            //заполнить webdata:tXX_spls
#if PG
            string tXX_spls_full = "public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif

            try
            {
                if (
                    !DBManager.DbCreateTable(conn_db, DBManager.CreateTableArgs.DropIfExists, true,
                        DBManager.GetFullBaseName(conn_web),
                        "temp_tXX_spls", "nzp_kvar INTEGER", " pref CHAR(20)").result)
                {
                    return ret;
                }
                foreach (_Point point in Points.PointList)
                {
                    if (
                        !TempTableInWebCashe(conn_db,
                            point.pref + "_charge_" + (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString("00") +
                            ".perekidka")) continue;
                    //todo: не хватает фильтрации доступных банков данных                
                    sql.Remove(0, sql.Length);

                    sql.Append(" Insert into temp_tXX_spls (nzp_kvar,pref) ");
                    sql.Append(" Select distinct nzp_kvar,'" + point.pref + "' ");
                    sql.Append(" From " + point.pref + "_charge_" +
                               (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString("00") + tableDelimiter +
                               "perekidka pk");
                    sql.Append(" Where pk.nzp_reestr = " + finder.nzp_reestr);

                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                DbAdres dba = new DbAdres();
                Ls finderls = new Ls();
                finderls.nzp_user = finder.nzp_user;
                ret = dba.Fill_tXX_spls(conn_db, conn_web, null, finderls, "temp_tXX_spls", 0);
                if (!ret.result)
                {
                    return ret;
                }
                dba.Close();

                ExecSQL(conn_db, "drop table temp_tXX_spls", false);

                //создаем индексы на tXX_spls
                using (var db = new DbAdresClient())
                {
                    ret = db.CreateTableWebLs(conn_web, tXX_spls, false);
                }
            }
            finally
            {
                conn_web.Close();
                conn_db.Close();
            }

            return ret;
        }

        public List<PeniNoCalc> GetPeniNoCalcList(PeniNoCalc finder, out Returns ret)
        {
            List<PeniNoCalc> lpnc = new List<PeniNoCalc>();
            ret = Utils.InitReturns();

            #region проверка входных параметров

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return lpnc;
            }

            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return lpnc;
            string sql;
            string warnMsg =
                "Данные не добавлены, т.к. временной диапазон строк с указанными порядковыми номерами перекрывает заданный диапазон: ";
            IDataReader reader = null;
            string where = "";
            List<int> idNotInDateRange = new List<int>();
            switch (finder.CurAction)
            {
                case PeniNoCalc.Actions.Add:

                    DateTime parsedDatePo;
                    DateTime parsedDateS;
                    string dateS = "'" + finder.date_from + "'";
                    string datePo = "'" + finder.date_to + "'";

                    if (!DateTime.TryParse(finder.date_from, out parsedDateS))
                    {
                        ret.result = false;
                        ret.text = "Ошибка преобразования даты в методе GetPeniNoCalcList(...)";
                        return lpnc;
                    }
                    if (!DateTime.TryParse(finder.date_to, out parsedDatePo))
                    {
                        ret.result = false;
                        ret.text = "Ошибка преобразования даты в методе GetPeniNoCalcList(...)";
                        return lpnc;
                    }
                    TimeSpan ts = new TimeSpan(1, 0, 0, 0);
                    string changedOnDate;
                    foreach (int nzpServ in finder.nzp_serv_list)
                    {
                        bool servExists; // = CheckExistsServ(conn_web, "", finder.nzp_supp, nzpServ.Key, out ret);
                        // первый период
                        // заданный пользователем       ----------
                        // сущ                       ----------------
                        // Для первого диапазона извлекаем id, чтобы потом указать на те строки в которых есть такой диапазон
                        // Если такой диапазон встретился, то услуга с заданным диапазоном не вставляется, т.к. этот диавпазон перекрывает заданный
                        sql = " and ((date_from<=" + dateS + " and date_to>" + datePo + ") OR " +
                                             "(date_from<" + dateS + " and date_to>=" + datePo + " ))";
                        servExists = сheckExistsServ(conn_web, sql, finder.nzp_supp, nzpServ, out ret);
                        if (!ret.result)
                        {
                            return lpnc;
                        }
                        if (servExists)
                        {
                            string mainSql = "Select id FROM " + Points.Pref + sKernelAliasRest + tableDelimiter + "peni_no_calc where" +
                                             " nzp_serv=" + nzpServ + " and nzp_supp=" + finder.nzp_supp +
                                             " and is_actual=1" + sql;

                            ret = ExecRead(conn_web, out reader, mainSql, true);
                            if (!ret.result)
                            {
                                return lpnc;
                            }
                            try
                            {
                                if (reader != null)
                                {
                                    while (reader.Read())
                                    {
                                        if (reader["id"] != DBNull.Value)
                                            idNotInDateRange.Add((int)reader["id"]);
                                    }
                                }
                                continue;
                            }
                            catch (Exception ex)
                            {
                                MonitorLog.WriteLog("Ошибка при считвании данных  " + ex.Message,
                                    MonitorLog.typelog.Error,
                                    true);
                                reader.Close();
                                ret.result = false;
                                return lpnc;
                            }
                        }
                        // второй период
                        // заданный пользователем       ----------
                        // сущ                      --------
                        sql = " and (date_from<" + dateS + " and date_to>" + dateS + " and date_to<" + datePo + ")";
                        servExists = сheckExistsServ(conn_web, sql, finder.nzp_supp, nzpServ, out ret);
                        if (!ret.result)
                        {
                            return lpnc;
                        }
                        if (servExists)
                        {
                            string newDateTo = "'" + parsedDateS.Subtract(ts).ToShortDateString() + "'";
                            changedOnDate = "'" + DateTime.Now + "'";
                            sql = "Update " + Points.Pref + sKernelAliasRest + "peni_no_calc SET date_to=" + newDateTo +
                                  ", changed_by=" + finder.nzp_user + ", " + "changed_on=" + changedOnDate + " where " +
                                  " nzp_serv=" + nzpServ + " and nzp_supp=" + finder.nzp_supp + " and is_actual=1" +
                                  sql;
                            ret = ExecSQL(conn_web, sql, true);
                            if (!ret.result)
                            {
                                return lpnc;
                            }
                        }
                        // третий период
                        // заданный пользователем       ----------
                        // сущ                                 --------
                        sql = " and (date_from>" + dateS + " and date_from<" + datePo + " and date_to>" + datePo + ")";
                        servExists = сheckExistsServ(conn_web, sql, finder.nzp_supp, nzpServ, out ret);
                        if (!ret.result)
                        {
                            return lpnc;
                        }
                        if (servExists)
                        {
                            string newDateFrom = "'" + parsedDatePo.Add(ts).ToShortDateString() + "'";
                            changedOnDate = "'" + DateTime.Now + "'";
                            sql = "Update " + Points.Pref + sKernelAliasRest + "peni_no_calc SET date_from=" + newDateFrom +
                                  ", changed_by=" + finder.nzp_user + ", " + "changed_on=" + changedOnDate + " where " +
                                  " nzp_serv=" + nzpServ + " and nzp_supp=" + finder.nzp_supp + sql;
                            ret = ExecSQL(conn_web, sql, true);
                            if (!ret.result)
                            {
                                return lpnc;
                            }
                        }

                        // четвертый период
                        // заданный пользователем       ----------
                        // сущ                            ------
                        sql = " and (date_from>=" + dateS + " and date_from<" + datePo + " and date_to>" + dateS +
                              " and date_to<=" + datePo + ")";
                        servExists = сheckExistsServ(conn_web, sql, finder.nzp_supp, nzpServ, out ret);
                        if (!ret.result)
                        {
                            return lpnc;
                        }
                        if (servExists)
                        {
                            changedOnDate = "'" + DateTime.Now + "'";
                            sql = "Update " + Points.Pref + sKernelAliasRest + "peni_no_calc SET is_actual=" + 100 +
                                  ", changed_by=" + finder.nzp_user + ", " + "changed_on=" + changedOnDate + " where " +
                                  " nzp_serv=" + nzpServ + " and nzp_supp=" + finder.nzp_supp + " and is_actual=1" +
                                  sql;
                            ret = ExecSQL(conn_web, sql, true);
                            if (!ret.result)
                            {
                                return lpnc;
                            }
                        }
                        // вставка новой записи производится в случаях:
                        // 1.если заданной услуги с заданным договором не существует 
                        // 2.если услуга с договором существует, но их временной диапазон не пересекается с заданным диапазоном 
                        string insert = insertSrvToNopeniCalcTable(finder, nzpServ);
                        ret = ExecSQL(conn_web, insert, true);
                        if (!ret.result)
                        {
                            return lpnc;
                        }
                    }
                    break;
                case PeniNoCalc.Actions.Delete:
                    sql = "update " + Points.Pref + sKernelAliasRest +
                          "peni_no_calc set is_actual=100 where id=" + finder.id;
                    ExecScalar(conn_web, sql, out ret, true);
                    if (!ret.result)
                    {
                        return lpnc;
                    }
                    break;
                // Только в случае обновления учитываем дату при выборе записей для gridview
                case PeniNoCalc.Actions.Update:
                    if (finder.date_from != "") where += " and ucr.date_from>=" + "'" + finder.date_from + "'";
                    if (finder.date_to != "") where += " and ucr.date_to<=" + "'" + finder.date_to + "'";
                    break;
            }
            if (finder.nzp_serv_list.Count != 0)
            {
                Int32 i = 0;
                string servList = "";
                foreach (int serv in finder.nzp_serv_list)
                {
                    i++;
                    servList += (i == 1 ? "(" : "") + serv + (i == finder.nzp_serv_list.Count ? ")" : ", ");
                }
                if (servList != "") where += " and ucr.nzp_serv in " + servList;
            }
            if (finder.nzp_supp != 0) where += " and ucr.nzp_supp=" + finder.nzp_supp;
            if (finder.is_actual == 1)
            {
                where += " and ucr.is_actual=1";
            }
            sql = "with srv as (select s.service_name, clc.* from " + Points.Pref + sKernelAliasRest +
                  "peni_no_calc clc left outer join " + Points.Pref + sKernelAliasRest +
                  "services s on (clc.nzp_serv=s.nzp_serv)), " +
                  "suppname as ( select supp.name_supp, srv.* from srv left outer join " + Points.Pref + sKernelAliasRest +
                  "supplier supp on (srv.nzp_supp=supp.nzp_supp)), " +
                  "ucr as (select suppname.*, " + sNvlWord + "(u.comment,'')||''||" + sNvlWord + "('('||u.name||')','') as usercreate from suppname left outer join " + Points.Pref + sDataAliasRest +
                  "users u on (suppname.created_by=u.nzp_user)) " +
                  "select ucr.*, COALESCE(u.comment,'')||''||COALESCE('('||u.name||')','') as userchang from ucr left outer join " + Points.Pref + sDataAliasRest +
                  "users u on (ucr.changed_by=u.nzp_user) where 1=1 " + where + " Order By id";
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                return lpnc;
            }
            try
            {
                if (reader != null)
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;
                        if (finder.skip > 0 && i <= finder.skip) continue;
                        if (i > finder.skip + finder.rows) continue;
                        PeniNoCalc pnc = new PeniNoCalc();
                        pnc.order_num = i;
                        pnc.id = reader["id"] != DBNull.Value ? (int)reader["id"] : 0;
                        pnc.service_name = reader["service_name"] != DBNull.Value
                            ? reader["service_name"].ToString()
                            : "";
                        pnc.nzp_supp = reader["nzp_serv"] != DBNull.Value ? (int)reader["nzp_serv"] : 0;
                        pnc.supp_name = reader["name_supp"] != DBNull.Value ? reader["name_supp"].ToString() : "";
                        pnc.nzp_supp = reader["nzp_supp"] != DBNull.Value ? (int)reader["nzp_supp"] : 0;
                        pnc.date_from = reader["date_from"] != DBNull.Value
                            ? ((DateTime)reader["date_from"]).ToString("dd.MM.yyyy")
                            : "";
                        pnc.date_to = reader["date_to"] != DBNull.Value
                            ? ((DateTime)reader["date_to"]).ToString("dd.MM.yyyy")
                            : "";
                        pnc.created_on = reader["created_on"] != DBNull.Value
                            ? ((DateTime)reader["created_on"]).ToString()
                            : "";
                        pnc.changed_on = reader["changed_on"] != DBNull.Value
                            ? ((DateTime)reader["changed_on"]).ToString()
                            : "";
                        pnc.is_actual = reader["is_actual"] != DBNull.Value ? (int)reader["is_actual"] : 1;
                        pnc.is_actual_str = pnc.is_actual != 1 ? "Нет" : "Да";
                        pnc.user_created = reader["usercreate"] != DBNull.Value ? reader["usercreate"].ToString() : "";
                        pnc.user_changed = reader["userchang"] != DBNull.Value ? reader["userchang"].ToString() : "";
                        lpnc.Add(pnc);
                    }
                    ret.tag = i;
                }
                // Если нашлись строки по первому диапазону при добавлении новых записей 
                if (finder.CurAction == PeniNoCalc.Actions.Add)
                {
                    if (lpnc.Count != 0 && idNotInDateRange.Count != 0)
                    {
                        for (int j = 0; j < idNotInDateRange.Count; j++)
                        {
                            int j1 = j;
                            // выставить соответствия порядкового номера и id этих записей
                            // Порядковый номер - это номер строки в gridview
                            IEnumerable<int> order_nums = from pnc in lpnc
                                                          where pnc.id == idNotInDateRange[j1]
                                                          select pnc.order_num;
                            var nums = order_nums as int[] ?? order_nums.ToArray();
                            if (!nums.Any()) continue;
                            // Формируем строку из этих порядковых номеров, чтобы отобразить в сообщении
                            foreach (int num in nums)
                            {
                                if (ret.text == "")
                                {
                                    ret.text += warnMsg + num;
                                }
                                else
                                {
                                    ret.text += ", " + num;
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при считвании данных  " + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                ret.result = false;
                return lpnc;
            }
            finally
            {
                if (reader != null && !reader.IsClosed) reader.Close();
                conn_web.Close();
            }
            return lpnc;
        }

        private string insertSrvToNopeniCalcTable(PeniNoCalc finder, int nzp_serv)
        {

            string dateS = "'" + finder.date_from + "'";
            string datePo = "'" + finder.date_to + "'";
            return "Insert into " + Points.Pref + sKernelAliasRest +
      "peni_no_calc (nzp_serv, nzp_supp, date_from, date_to, is_actual, created_on, created_by) " +
      "values (" + nzp_serv + ", " + finder.nzp_supp + ", " + dateS + ", " + datePo +
      ", " + 1 + ", " + "'" + DateTime.Now + "'" + ", " +
      finder.nzp_user +
      ")";
        }

        private bool сheckExistsServ(IDbConnection conn, string sql, int nzp_supp, int nzp_serv, out Returns ret)
        {
            ret = Utils.InitReturns();
            string mainSql = "Select count(*) FROM " + Points.Pref + sKernelAliasRest + "peni_no_calc where" +
                             " nzp_serv=" + nzp_serv + " and nzp_supp=" + nzp_supp + " and is_actual=" + 1 + sql;
            int parsedCount = -1;
            object count = ExecScalar(conn, mainSql, out ret, true);
            if (!ret.result)
            {
                return false;
            }
            if (!int.TryParse(count.ToString(), out parsedCount))
            {
                ret.result = false;
            }
            if (parsedCount != 0)
            {
                return true;
            }
            return false;
        }

        
        
    }
}
