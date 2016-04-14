using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    public partial class DbCharge : DbChargeClient
    {
        public enum enLevel

        {
            nzp_kvar,
            nzp_dom,
            nzp_geu,
            nzp_area,
            nzp_supp
        }

        bool CheckSaldoSupp(IDbConnection conn_web, Saldo finder, out Returns ret) 
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return false;
            }

            //необходимо проверить сальдо по поставщику, делаем так:
            //проверям, что все nzp_area посчитаны, где этот поставщик присутствует
            //если нет, то добавляем задание на выполнение
            //т.е. сначала считаем сальдо по всем nzp_area (фактичекси заполняем saldo_bydom & saldo_byarea)
            //и сальдо по nzp_supp получается автоматически после

            bool b_chk = true;
            string s_chk = "";

            int year = finder.YM.year_;
            int month = finder.YM.month_;

            string analiz1 = "anl" + year;
            //если analiz1 еще не был создан, то вызовем процедуру заполнения за текущий год
            if (!TableInWebCashe(conn_web, analiz1))
            {
                DbAnaliz db = new DbAnaliz();
                db.LoadAnaliz1(out ret, year, true);
                db.Close();
                if (!ret.result)
                {
                    //conn_web.Close();
                    return false;
                }
            }

            string saldo_byarea= "saldo_" + year;
            bool b_saldo_byarea = TableInWebCashe(conn_web, saldo_byarea);
            bool b_saldo_fon    = TableInWebCashe(conn_web, "saldo_fon");

            //получим список всех nzp_area
            IDataReader reader;
            ret = ExecRead(conn_web, out reader,
#if PG
                " Select distinct nzp_area From " + analiz1 + " Where nzp_supp = " + finder.nzp_supp, true);
#else
                " Select unique nzp_area From " + analiz1 + " Where nzp_supp = " + finder.nzp_supp, true);
#endif
            if (!ret.result)
            {
                //conn_web.Close();
                return false;
            }
            while (reader.Read())
            {
                //цикл по месяцам
                int mb = 1;
                int me = 12;
                int nzp_area = 0;

                if (reader["nzp_area"] != DBNull.Value)
                    nzp_area = (int)reader["nzp_area"];

                if (nzp_area == 0) continue;

                if (!b_saldo_byarea)
                {
                    //только создание таблицы saldo_byarea
                    ReCreateSaldo_byarea(conn_web, out ret, year, month, nzp_area, 0);
                    if (!ret.result)
                    {
                        reader.Close();
                        //conn_web.Close();
                        return false;
                    }
                    b_saldo_byarea = true;
                }

                if (!b_saldo_fon)
                {
                    //если есть задания подсчета сальдо УК со статусом выполняется, в очереди, ошибка
                    //то ret.resul = false
                    CheckSaldoFon(conn_web, out ret, nzp_area, year, month, 0, "");
                    if (!ret.result)
                    {
                        //conn_web.Close();
                        return false;
                    }
                    b_saldo_fon = true;
                }

                //фоновые процессы
                IDataReader reader2;
                ret = ExecRead(conn_web, out reader2,
                      " Select * From saldo_fon " +
                      " Where nzp_area = " + nzp_area +
                      "   and kod_info in (0,3,-1) ", true);
                if (!ret.result)
                {
                    reader.Close();
                    //conn_web.Close();
                    return false;
                }

                bool b_continue = false;
                List<RecordMonth> lrm = new List<RecordMonth>();

                while (reader2.Read())
                {
                    //есть задания на выполнение для этого nzp_area
                    b_chk = false;
                    
                    if (reader2["kod_info"] != DBNull.Value)
                    {
                        if ((int)reader2["kod_info"] == -1)
                        {
                            //сразу вылетаем из обработки
                            //conn_web.Close();
                            reader.Close();
                            reader2.Close();

                            ret.text = "Обнаружена ошибка подсчета данных, обратитесь к разработчикам ";
                            ret.result = false;
                            return false;
                        }
                        else
                            s_chk = "Задание было помещено в очередь ожидания на выполнение. Пожалуйста, проверьте данные позднее.";
                    }

                    int y1 = 0;
                    int m1 = 0;
                    if (reader2["year_"]  != DBNull.Value) y1 = (int)reader2["year_"];
                    if (reader2["month_"] != DBNull.Value) m1 = (int)reader2["month_"];

                    RecordMonth rm = new RecordMonth();
                    rm.year_  = y1;
                    rm.month_ = m1;
                    lrm.Add(rm);

                    if (y1 == year && m1 == 0)
                    {
                        ret.text = s_chk; //задание на подсчет всего года для данного nzp_area уже в очереди
                        b_continue = true;
                        break;
                    }
                }
                reader2.Close();


                if (b_continue) continue;

                //проверяем дальше уже по месяцам, если надо - кидаем в очередь на выполнение
                if (month > 0)
                {
                    mb = month;
                    me = mb;
                }
                else
                    if (year == Points.CalcMonth.year_)
                    {
                        me = Points.CalcMonth.month_;
                    }

                for (int m = mb; m <= me; m = m + 1)
                {
                    //проверим наличие фоновых задач по данному месяцу
                    foreach (RecordMonth rm in lrm)
                    {
                        if (rm.year_ == year && (rm.month_ == 0 || rm.month_ == m))
                        {
                            //задание уже в очереди
                            ret.text = s_chk; 
                            b_continue = true;
                            break;
                        }
                    }

                    if (b_continue) continue;

                    //наличие сумм в saldo_byarea
                    IDataReader reader3;
#if PG
                    ret = ExecRead(conn_web, out reader3,
                        " Select oid From " + saldo_byarea + " a " +
                        " Where nzp_area = " + nzp_area +
                          " and month_ = " + m + " limit 1", true);
#else
                    ret = ExecRead(conn_web, out reader3,
                        " Select first 1 rowid From " + saldo_byarea + " a " +
                        " Where nzp_area = " + nzp_area +
                          " and month_ = " + m, true);
#endif
                    if (!ret.result)
                    {
                        //conn_web.Close();
                        reader.Close();
                        return false;
                    }

                    if (reader3.Read())
                    {
                        //данные присутствуют, все ок
                    }
                    else
                    {
                        //ага, данных нет - кидаем в фон для выполнения
                        b_chk = false;
                        s_chk = "Задание помещено в очередь ожидания на выполнение. Пожалуйста, проверьте данные позднее.";

                        ret = ExecSQL(conn_web,
                            " Insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in) " +
#if PG
                            " Values (" + nzp_area + "," + year + "," + m + ",3,now())", true);
#else
                            " Values (" + nzp_area + "," + year + "," + m + ",3,current)", true);
#endif
                        if (!ret.result)
                        {
                            //conn_web.Close();
                            reader.Close();
                            reader3.Close();
                            return false;
                        }
                    }
                    reader3.Close();
                }
            }
            reader.Close();
            //conn_web.Close();

            ret.text = s_chk;
            return b_chk;
        }

        
        /// <summary>
        /// Получить начисления по ЛС, поставщику, территории (УК)
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <param name="itog">Строка итого</param>
        /// <returns></returns>
        public List<Saldo> GetSaldo(Saldo finder, out Returns ret, ref _RecordSaldo itog) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            ret = ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            if (finder.nzp_supp > 0)
            {
                //сальдо по поставщику: проверим данные по всем nzp_area
                bool b = CheckSaldoSupp(conn_web, finder, out ret);
                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
                if (!b) //должен быть запущен фоновой процесс подсчета
                {
                    ret.tag = Constants.workinfon;
                    ret.result = false;
                    conn_web.Close();
                    return null;
                }
            }

            //проверка наличия фоновых процессов
            if (finder.nzp_area > 0)
            {
                CheckSaldoFon(conn_web, out ret, finder.nzp_area, finder.YM.year_, finder.YM.month_, 0, "");
                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
            }

            List<Saldo> Spis = new List<Saldo>();

            Spis.Clear();

#if PG
            string webdb_path = pgDefaultDb + ".";
#else
            string webdb_path = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":";
#endif
            string userSaldo = "t" + Convert.ToString(finder.nzp_user) + "_saldo"; //сальдо для отображения (пользователю)
            //ExecSQL(conn_web, " Delete From " + userSaldo, false);

            //пересобрать агрегированные данные, для этого сначало все удалим
            //и это работает только для текущего месяца
            RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(finder.pref));
            if ( finder.find_from_the_start == 10 && 
                 finder.YM.year_  == r_m.year_ &&
                 finder.YM.month_ == r_m.month_
               )
            {
                if (finder.nzp_area > 0)
                {
                    ReCreateSaldo_bydom(conn_web, out ret, finder.YM.year_, finder.YM.month_, 0, finder.nzp_area, 0, "");
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return null;
                    }

                    ReCreateSaldo_byarea(conn_web, out ret, finder.YM.year_, finder.YM.month_, finder.nzp_area, 0);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return null;
                    }
                }
                if (finder.nzp_dom > 0)
                {
                    ReCreateSaldo_bydom(conn_web, out ret, finder.YM.year_, finder.YM.month_, finder.nzp_dom, 0, 0, "");
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return null;
                    }
                }

                finder.find_from_the_start = 1; //далее работаем по обычному сценарию выбора данных
            }

            if (!TempTableInWebCashe(conn_web, webdb_path + userSaldo)) 
            {
                //пересоздать userSaldo
                CreateUserSaldo(conn_web, userSaldo, out ret);
                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
            }

            bool groupby_month    = (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()));
            bool groupby_service  = (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString()));
            bool groupby_supplier = (Utils.GetParams(finder.groupby, Constants.act_groupby_supplier.ToString()));
            bool groupby_area     = (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString()));
            bool groupby_geu      = (Utils.GetParams(finder.groupby, Constants.act_groupby_geu.ToString()));
            bool groupby_dom      = (Utils.GetParams(finder.groupby, Constants.act_groupby_dom.ToString()));

            StringBuilder sel = new StringBuilder();
            sel.Append( "Select ");

            if (groupby_month)    sel.Append(" month_,year_, ");                 else sel.Append(" 0 as month_,   0 year_, ");
            if (groupby_service)  sel.Append(" a.nzp_serv,service,ordering, ");  else sel.Append(" 0 as nzp_serv, '' as service,0 as ordering, ");
            if (groupby_supplier) sel.Append(" a.nzp_supp,name_supp, ");         else sel.Append(" 0 as nzp_supp, '' as name_supp, ");
            if (groupby_area)     sel.Append(" a.nzp_area,area, ");              else sel.Append(" 0 as nzp_area, '' as area, ");
            if (groupby_geu)      sel.Append(" a.nzp_geu,geu, ");                else sel.Append(" 0 as nzp_geu,  '' as geu, ");

            if (groupby_dom) 
#if PG
                sel.Append(" a.nzp_dom, trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '||trim(coalesce(ndom,''))||'  корп. '|| trim(coalesce(nkor,'')) as adr, ");
#else
                sel.Append(" a.nzp_dom, trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,'')) as adr, ");
#endif
            else 
                sel.Append(" 0 as nzp_dom,  '' as adr, ");

            sel.Append(" sum(sum_real) as sum_real, sum(rsum_tarif) as rsum_tarif, sum(izm_saldo) as izm_saldo, sum(reval) as reval, sum(real_charge) as real_charge, ");
            sel.Append(" sum(sum_money) as sum_money, sum(money_to) as money_to, sum(money_from) as money_from, sum(money_del) as money_del, sum(sum_fin) as sum_fin, ");

            if (!groupby_month)
            {
                int mb = 1;
                int me = 12;
            
                //отобразим данные по текущий месяц
                if (finder.YM.year_ == r_m.year_)
                    me = r_m.month_;

                if (finder.YM.month_ > 0)
                {
                    mb = finder.YM.month_;
                    me = mb;
                }
                sel.Append(" sum(case when month_ = " + mb.ToString() + " then sum_insaldo  else 0 end) as sum_insaldo, ");
                sel.Append(" sum(case when month_ = " + me.ToString() + " then sum_charge   else 0 end) as sum_charge,  ");
                sel.Append(" sum(case when month_ = " + me.ToString() + " then sum_outsaldo else 0 end) as sum_outsaldo,");
                sel.Append(" sum(case when month_ = " + me.ToString() + " then sum_dolg     else 0 end) as sum_dolg ");
            }
            else
            {
                sel.Append(" sum(sum_insaldo)  as sum_insaldo, ");
                sel.Append(" sum(sum_charge)   as sum_charge,  ");
                sel.Append(" sum(sum_outsaldo) as sum_outsaldo,");
                sel.Append(" sum(sum_dolg)     as sum_dolg ");
            }


            string sw0 = "";
            string sw_UserSaldo = "";

            sw_UserSaldo = " and year_ = " + finder.YM.year_.ToString();
            if (finder.YM.month_ > 0)
                sw_UserSaldo += " and month_ = " + finder.YM.month_.ToString();

            sw_UserSaldo += " and a.nzp_serv > 1 ";

            //ограничение отображения
            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    string sq = "";

                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                sq = "nzp_serv in (";
                            else
                                if (role.kod == Constants.role_sql_supp)
                                    sq = "nzp_supp in (";
                        }
                        if (sq != "") sw_UserSaldo += " and a." + sq + role.val + ")";
                    }
                }
            }

            string orderby = " Order by year_,month_,ordering,name_supp"; //пока железно сортировка по услуге";
            /*
            switch (finder.sortby)
            {
                case Constants.sortby_serv: orderby += ",ordering"; break;
                case Constants.sortby_supp: orderby += ",name_supp"; break;
            }
            */

            enLevel level = 0;
            long nzp_key = 0;

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (finder.nzp_kvar > 0) //сальдо по лс
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            {
                level = 0;
                nzp_key = finder.nzp_kvar;

                sel.Append(" From " + webdb_path + userSaldo + " a ");

                if (groupby_service) 
                {
#if PG
                    sel.Append(" ," + Points.Pref + "_kernel.services s ");
#else
                    sel.Append(" ," + Points.Pref + "_kernel:services s ");
#endif
                    sw0 = sw0 + " and a.nzp_serv = s.nzp_serv ";
                }
                if (groupby_supplier) 
                {
#if PG
                    sel.Append("," + Points.Pref + "_kernel.supplier p ");
#else
                    sel.Append(" , outer " + Points.Pref + "_kernel:supplier p ");
#endif
                    sw0 = sw0 + " and a.nzp_supp = p.nzp_supp ";
                }

                sw_UserSaldo = " and a.nzp_kvar = " + finder.nzp_kvar.ToString() + sw_UserSaldo;

                sel.Append(" Where 1 = 1 " + sw0 + sw_UserSaldo);
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            else //сальдо по домам (по указанному году)
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            {
                //данные перегоним в userSaldo из saldo_bydom (для отображения!)
                sel.Append(" From " + webdb_path + userSaldo + " a ");

                if (finder.nzp_dom > 0)
                {
                    level = enLevel.nzp_dom;
                    nzp_key = finder.nzp_dom;
                    sw_UserSaldo = " and a.nzp_dom = " + finder.nzp_dom.ToString() + " and a.nzp_kvar = -100 " + sw_UserSaldo;
                }
                else
                    if (finder.nzp_geu > 0)
                    {
                        level = enLevel.nzp_geu;
                        nzp_key = finder.nzp_geu;
                        sw_UserSaldo = " and a.nzp_geu = " + finder.nzp_geu.ToString() + " and a.nzp_kvar = -101 " + sw_UserSaldo;
                    }
                    else
                        if (finder.nzp_area > 0)
                        {
                            level = enLevel.nzp_area;
                            nzp_key = finder.nzp_area;
                            sw_UserSaldo = " and a.nzp_area = " + finder.nzp_area.ToString() + " and a.nzp_kvar = -102 " + sw_UserSaldo;
                        }
                        else
                            if (finder.nzp_supp > 0)
                            {
                                level = enLevel.nzp_supp;
                                nzp_key = finder.nzp_supp;
                                sw_UserSaldo = " and a.nzp_supp = " + finder.nzp_supp.ToString() + " and a.nzp_kvar = -103 " + sw_UserSaldo;
                            }
                            else
                            {
                                ret.text = "Не указан адресат";
                                ret.result = false;
                                return null;
                            }

                if (groupby_service)
                {
#if PG
                    sel.Append(" ," + Points.Pref + "_kernel.services s ");
#else
                    sel.Append(" ," + Points.Pref + "_kernel:services s ");
#endif
                    sw0 = sw0 + " and a.nzp_serv = s.nzp_serv ";
                }
                if (groupby_supplier)
                {
#if PG
                    sel.Append("  ," + Points.Pref + "_kernel.supplier p ");
                    sw0 = sw0 + " and a.nzp_supp = p.nzp_supp ";
#else
                    sel.Append(" , outer " + Points.Pref + "_kernel:supplier p ");
                    sw0 = sw0 + " and a.nzp_supp = p.nzp_supp ";
#endif
                }
                if (groupby_area)
                {
#if PG
                    sel.Append("  ," + Points.Pref + "_data.s_area e ");
                    sw0 = sw0 + " and a.nzp_area = e.nzp_area ";
#else
                    sel.Append(" , outer " + Points.Pref + "_data:s_area e ");
                    sw0 = sw0 + " and a.nzp_area = e.nzp_area ";
#endif
                }
                if (groupby_geu)
                {
#if PG
                    sel.Append("  ," + Points.Pref + "_data.s_geu g ");
                    sw0 = sw0 + " and a.nzp_geu = g.nzp_geu ";
#else
                    sel.Append(" , outer " + Points.Pref + "_data:s_geu g ");
                    sw0 = sw0 + " and a.nzp_geu = g.nzp_geu ";
#endif
                }
                if (groupby_dom)
                {
#if PG
                    sel.Append(" , " + Points.Pref + "_data.dom d," + Points.Pref + "_data.s_ulica u left outer join " + Points.Pref + "_data.s_rajon r ");
                    sw0 = sw0 + " and a.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj ";
#else
                    sel.Append(" , " + Points.Pref + "_data:dom d," + Points.Pref + "_data:s_ulica u, outer " + Points.Pref + "_data:s_rajon r ");
                    sw0 = sw0 + " and a.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj ";
#endif
                }

                sel.Append(" Where 1 = 1 " + sw0 + sw_UserSaldo);
            }
            
            //if (finder.YM.month_ > 0)
            //{
            sel.Append(" and ( abs(sum_real)>0.001 or abs(rsum_tarif)>0.001 or abs(sum_charge)>0.001 or abs(reval)>0.001 or abs(real_charge)>0.001 ");
                sel.Append(" or abs(sum_money)>0.001 or abs(money_to)>0.001 or abs(money_from)>0.001 or abs(money_del)>0.001 ");
                sel.Append(" or abs(sum_insaldo)>0.001 or abs(izm_saldo)>0.001 or abs(sum_outsaldo)>0.001 ");
                sel.Append(" or abs(sum_dolg)>0.001 or abs(sum_fin)>0.001 ) ");
            //}
            
            sel.Append(" Group by 1,2,3,4,5,6,7,8,9,10,11 " + orderby);



            //открыть основную базу для выборки
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string cur_pref = "";
            //поверим наличие данных и при необходимости заполним
            foreach (_Point zap in Points.PointList)
            {
                if (finder.pref != "")
                {
                    if (zap.pref != finder.pref) continue;
                }

                cur_pref = zap.pref;

                int mb = 1; 
                int me = 12;

                if (finder.YM.month_ > 0)
                {
                    mb = finder.YM.month_;
                    me = mb;
                }
                else
                    if (finder.YM.year_ == r_m.year_)
                    {
                        me = r_m.month_;
                    }

                for (int m = mb; m <= me; m = m + 1)
                {
                    _RecordSaldo sld = new _RecordSaldo();

                    GetSaldoDb(conn_db, conn_web, out ret, userSaldo,
                               nzp_key, level, 
                               finder.YM.year_, m,
                               cur_pref, zap.nzp_wp,
                               ref sld, finder.find_from_the_start, finder.RolesVal);

                    if (!ret.result)
                    {
                        conn_web.Close();
                        conn_db.Close();

                        //постановка в очередь на выполнение
                        if (ret.tag == Constants.workinfon)
                        {
                            DbCharge db = new DbCharge();
                            db.InSaldoFon(finder);
                            db.Close();
                        }
                        return null;
                    }

                    if (m == mb)
                    {
                        itog = sld;
                    }
                    else
                    {
                        itog.sum_real    += sld.sum_real;
                        itog.real_charge += sld.real_charge;
                        itog.reval       += sld.reval;
                        itog.sum_money   += sld.sum_money;
                        itog.sum_fin     += sld.sum_fin;

                        if (sld.sum_charge > 0 || Math.Abs(sld.sum_outsaldo) > 0)
                        {
                            itog.sum_charge   = sld.sum_charge;
                            itog.sum_outsaldo = sld.sum_outsaldo;
                            itog.sum_dolg     = sld.sum_dolg;
                        }

                        if (m == me)
                        {
                            if (sld.sum_charge > 0 || Math.Abs(sld.sum_outsaldo) > 0)
                            {
                                itog.sum_charge   = sld.sum_charge;
                                itog.sum_outsaldo = sld.sum_outsaldo;
                                itog.sum_dolg     = sld.sum_dolg;
                            }
                        }
                    }
                }
            }

            

            //выбрать список
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sel.ToString(), true).result)
            {
                conn_db.Close();
                conn_web.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Saldo zap = new Saldo();

                    if (groupby_month)
                    {
                        if (reader["month_"] != DBNull.Value)
                            zap.YM.month_ = (int)reader["month_"];
                        if (reader["year_"] != DBNull.Value)
                            zap.YM.year_ = (int)reader["year_"];

                        zap.num = zap.YM.name; 
                    }
                    if (groupby_service)
                    {
                        if (reader["service"] != DBNull.Value)
                            zap.service = (string)reader["service"];
                        if (reader["nzp_serv"] != DBNull.Value)
                            zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    }
                    if (groupby_supplier)
                    {
                        if (reader["name_supp"] != DBNull.Value)
                            zap.supplier = (string)reader["name_supp"];
                        if (reader["nzp_supp"] != DBNull.Value)
                            zap.nzp_supp = Convert.ToInt64(reader["nzp_supp"]);
                    }
                    if (groupby_area)
                    {
                        if (reader["area"] != DBNull.Value)
                            zap.area = (string)reader["area"];
                        if (reader["nzp_area"] != DBNull.Value)
                            zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    }
                    if (groupby_geu)
                    {
                        if (reader["geu"] != DBNull.Value)
                            zap.geu = (string)reader["geu"];
                        if (reader["nzp_geu"] != DBNull.Value)
                            zap.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                    }
                    if (groupby_dom)
                    {
                        if (reader["adr"] != DBNull.Value)
                            zap.adr = (string)reader["adr"];
                    }
                    if (reader["ordering"] != DBNull.Value)
                        zap.ordering = (int)reader["ordering"];
                    if (reader["sum_real"] != DBNull.Value)
                        zap.sum_real = (decimal)reader["sum_real"];
                    if (reader["rsum_tarif"] != DBNull.Value)
                        zap.rsum_tarif = (decimal)reader["rsum_tarif"];
                    if (reader["sum_charge"] != DBNull.Value)
                        zap.sum_charge = (decimal)reader["sum_charge"];
                    if (reader["reval"] != DBNull.Value)
                        zap.reval = (decimal)reader["reval"];
                    if (reader["real_charge"] != DBNull.Value)
                        zap.real_charge = (decimal)reader["real_charge"];
                    if (reader["sum_money"] != DBNull.Value)
                        zap.sum_money = (decimal)reader["sum_money"];
                    if (reader["money_to"] != DBNull.Value)
                        zap.money_to = (decimal)reader["money_to"];
                    if (reader["money_from"] != DBNull.Value)
                        zap.money_from = (decimal)reader["money_from"];
                    if (reader["money_del"] != DBNull.Value)
                        zap.money_del = (decimal)reader["money_del"];
                    if (reader["sum_insaldo"] != DBNull.Value)
                        zap.sum_insaldo = (decimal)reader["sum_insaldo"];
                    if (reader["izm_saldo"] != DBNull.Value)
                        zap.izm_saldo = (decimal)reader["izm_saldo"];
                    if (reader["sum_outsaldo"] != DBNull.Value)
                        zap.sum_outsaldo = (decimal)reader["sum_outsaldo"];

                    if (reader["sum_fin"] != DBNull.Value)
                        zap.sum_fin = (decimal)reader["sum_fin"];
                    if (reader["sum_dolg"] != DBNull.Value)
                        zap.sum_dolg = (decimal)reader["sum_dolg"];

                    Spis.Add(zap);
                }

                ret.tag = 1;

                reader.Close();
                conn_db.Close();
                conn_web.Close();
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

                MonitorLog.WriteLog("Ошибка заполнения сальдо лс " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }//GetSaldo

        //----------------------------------------------------------------------
        void GetSaldoDb(IDbConnection conn_db,
                               IDbConnection conn_web,
                               out Returns ret,
                               string userSaldo,
                               long nzp_key, enLevel level,
                               int yy, int mm, string cur_pref, int nzp_wp,
                               ref _RecordSaldo zap, int find, List<_RolesVal> rolesval) 
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string whereString = "";
            string flds = "";
            switch (level)
            {
                case enLevel.nzp_kvar:
                    {
                        whereString = " and nzp_kvar = " + nzp_key;
                        flds = "nzp_kvar, num_ls,nzp_dom,";
                        break;
                    }
                case enLevel.nzp_dom:
                    {
                        whereString = " and nzp_dom = " + nzp_key + " and nzp_kvar = -100";
                        flds = "-100, -100,nzp_dom,";
                        break;
                    }
                case enLevel.nzp_geu:
                    {
                        whereString = " and nzp_geu = " + nzp_key + " and nzp_kvar = -101";
                        flds = "-101, -101,0,";
                        break;
                    }
                case enLevel.nzp_area:
                    {
                        whereString = " and nzp_area = " + nzp_key + " and nzp_kvar = -102"; 
                        flds = "-102, -102,0,";
                        break;
                    }
                case enLevel.nzp_supp:
                    {
                        whereString = " and nzp_supp = " + nzp_key + " and nzp_kvar = -103";
                        flds = "-103, -103,0,";
                        break;
                    }
            }

            whereString += " and year_ = " + yy + " and month_= " + mm;

            string role_supp = "";
            if (rolesval != null)
            {
                if (rolesval.Count > 0)
                {
                    string sq = "";

                    foreach (_RolesVal role in rolesval)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                sq = "nzp_serv in (";
                            else
                                if (role.kod == Constants.role_sql_supp)
                                    sq = "nzp_supp in (";
                        }
                        if (sq != "") role_supp += " and " + sq + role.val + ")";
                    }
                }
            }

            //проверим наличие данные в userSaldo
            if (find == 0)
            {
                IDataReader reader;
                if (!ExecRead(conn_web, out reader,
                    " Select * From " + sDefaultSchema + userSaldo + " a Where 1 = 1 " + whereString + role_supp, true).result)
                {
                    ret.result = false;
                    return;
                }

                if (!reader.Read()) find = 1; //если нет данных, то перевыбрать нафиг
                reader.Close();
            }


            //----------------------------------------------------
            //выбрать данные
            //----------------------------------------------------
            if (find > 0)
            {
#if PG
                string webdb_path = pgDefaultDb + ".";
#else
                string webdb_path = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":";
#endif
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                //вызов процедуры выборки данных из charge_xx
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                CalcSaldo(conn_db, conn_web, out ret, "ttt_sld", cur_pref, nzp_wp, nzp_key, level, yy, mm, find);
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                if (!ret.result)
                {
                    return;
                }

                ret = ExecSQL(conn_web, " Delete From " + userSaldo + " Where year_= " + yy + " and month_= " + mm, true);
                if (!ret.result)
                {
                    return;
                }
                
                //перегрузка в кеш-таблицу из временной таблицы ttt_sld
                StringBuilder sql = new StringBuilder();

                sql.Append(" Insert into " + webdb_path + userSaldo);
                sql.Append(" ( year_,month_,nzp_kvar, num_ls, nzp_dom,nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
                sql.Append("   sum_real,rsum_tarif,sum_charge,reval,real_charge,sum_money,money_to,money_from,money_del,sum_insaldo,izm_saldo,sum_outsaldo, sum_fin,sum_dolg ) ");

                sql.Append(" Select " + yy + "," + mm + "," + flds);

                sql.Append("  nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
                sql.Append("  sum(sum_real) as sum_real, sum(rsum_tarif) as rsum_tarif, sum(sum_charge) as sum_charge, sum(reval) as reval, sum(real_charge) as real_charge, ");
                sql.Append("  sum(sum_money) as sum_money, sum(money_to) as money_to, sum(money_from) as money_from, sum(money_del) as money_del, ");
                sql.Append("  sum(sum_insaldo) as sum_insaldo, sum(izm_saldo) as izm_saldo, sum(sum_outsaldo) as sum_outsaldo, ");
                sql.Append("  sum(sum_fin) as sum_fin, sum(sum_charge - sum_fin) as sum_dolg ");

                sql.Append(" From ttt_sld Where 1 = 1 " + role_supp + 
                           " Group by 1,2,3,4,5,6,7,8,9 ");

                ret = ExecSQL(conn_db, sql.ToString(), true, 3000);
                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Drop table ttt_sld ", false);
            }

            //----------------------------------------------------
            //расчет итоговых сумм по месяцу
            //----------------------------------------------------
            StringBuilder sql_itog = new StringBuilder();

            sql_itog.Append(" Select sum(sum_real) as sum_real, sum(rsum_tarif) as rsum_tarif, sum(sum_charge) as sum_charge, sum(reval) as reval, sum(real_charge) as real_charge, ");
            sql_itog.Append(      "  sum(sum_money) as sum_money, sum(money_to) as money_to, sum(money_from) as money_from, sum(money_del) as money_del, ");
            sql_itog.Append(      "  sum(sum_insaldo) as sum_insaldo, sum(izm_saldo) as izm_saldo, sum(sum_outsaldo) as sum_outsaldo, ");
            sql_itog.Append(      "  sum(sum_fin) as sum_fin, sum(sum_dolg) as sum_dolg ");
            sql_itog.Append(" From " + userSaldo + " a Where 1 = 1 " + whereString + role_supp);

            IDataReader reader_itog;
            if (!ExecRead(conn_web, out reader_itog, sql_itog.ToString(), true).result)
            {
                ret.result = false;
                return;
            }

            try
            {
                while (reader_itog.Read())
                {
                    if (reader_itog["sum_real"] != DBNull.Value)
                        zap.sum_real = (decimal)reader_itog["sum_real"];
                    if (reader_itog["sum_charge"] != DBNull.Value)
                        zap.sum_charge = (decimal)reader_itog["sum_charge"];
                    if (reader_itog["reval"] != DBNull.Value)
                        zap.reval = (decimal)reader_itog["reval"];
                    if (reader_itog["real_charge"] != DBNull.Value)
                        zap.real_charge = (decimal)reader_itog["real_charge"];
                    if (reader_itog["sum_money"] != DBNull.Value)
                        zap.sum_money = (decimal)reader_itog["sum_money"];
                    if (reader_itog["money_to"] != DBNull.Value)
                        zap.money_to = (decimal)reader_itog["money_to"];
                    if (reader_itog["money_from"] != DBNull.Value)
                        zap.money_from = (decimal)reader_itog["money_from"];
                    if (reader_itog["money_del"] != DBNull.Value)
                        zap.money_del = (decimal)reader_itog["money_del"];
                    if (reader_itog["sum_insaldo"] != DBNull.Value)
                        zap.sum_insaldo = (decimal)reader_itog["sum_insaldo"];
                    if (reader_itog["izm_saldo"] != DBNull.Value)
                        zap.izm_saldo = (decimal)reader_itog["izm_saldo"];
                    if (reader_itog["sum_outsaldo"] != DBNull.Value)
                        zap.sum_outsaldo = (decimal)reader_itog["sum_outsaldo"];

                    if (reader_itog["sum_fin"] != DBNull.Value)
                        zap.sum_fin = (decimal)reader_itog["sum_fin"];
                    if (reader_itog["sum_dolg"] != DBNull.Value)
                        zap.sum_dolg = (decimal)reader_itog["sum_dolg"];
                }
                reader_itog.Close();
            }
            catch (Exception ex)
            {
                reader_itog.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения итогового сальдо " + err, MonitorLog.typelog.Error, 20, 201, true);

            }
        }//CheckLoadSaldo


        //----------------------------------------------------------------------
        void CalcSaldo ( IDbConnection conn_db,  //статистика сальдо по всем домам (или по дому nzp_dom, или по лс nzp_kvar)
                                IDbConnection conn_web,
                                out Returns ret,
                                string ttt_sld,         //временная таблица, где заполняются суммы
                                string cur_pref, int nzp_wp,
                                long nzp_key, enLevel level,  //0-kvar,1-dom,2-geu,3-area,....
                                int yy, int mm, int find) 
        //----------------------------------------------------------------------
        {
            //find:
            //0 - выборка из кэша
            ret = Utils.InitReturns();
#if PG
            string webdb_path  = pgDefaultDb + ".";  //путь базе кеш
#else
            string webdb_path  = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":";  //путь базе кеш
#endif
            string saldo_bydom = "saldo_" + yy + "_" + mm.ToString("00");          //сальдо по домам
            string saldo_byarea= "saldo_" + yy ;                                   //сальдо по УК

            long nzp_kvar = 0;
            long nzp_dom  = 0;
            long nzp_geu  = 0;
            long nzp_area = 0;

            string fld = "";

            switch (level)
            {
                case enLevel.nzp_kvar:
                    {
                        //find = 1 - выборка из charge_xx
                        nzp_kvar = nzp_key; break;
                    }
                case enLevel.nzp_dom:
                    {
                        //find = 1 - выборка из saldo_bydom, если нет, то из charge_xx
                        nzp_dom = nzp_key; break;
                    }
                case enLevel.nzp_geu:
                    {
                        nzp_geu  = nzp_key;
                        fld = "nzp_geu";
                        break;
                    }
                case enLevel.nzp_area:
                    {
                        //find = 1 - выборка из saldo_byarea, если нет, то выборка из saldo_bydom, если нет, то find:=2
                        //find = 2 - перевыбрать saldo_bydom, saldo_byarea (долгий процесс)
                        nzp_area = nzp_key;
                        fld = "nzp_area";
                        break;
                    }
                case enLevel.nzp_supp:
                    {
                        nzp_area = nzp_key;
                        fld = "nzp_supp";
                        break;
                    }
            }

            StringBuilder sql = new StringBuilder();
            string select_area = "";
            string ttt_doms = "";

            //----------------------------------------------------
            //для убыстрения выборки сальдо
            //сначало проверим в saldo_bydom, если нет, то догрузим в saldo_xx
            //find=2 - полная перевыборка из charge_xx
            //----------------------------------------------------
            if (find > 0 & level > 0)
            {
                //------------------------------------------------
                //по дому
                //------------------------------------------------
                if (level == enLevel.nzp_dom) //dom из saldo_bydom
                {
                    IDataReader reader;
#if PG
                    bool b = ExecRead(conn_web, out reader,
                        " Select oid From " + saldo_bydom +
                        " Where nzp_dom = " + nzp_dom + " limit 1", true).result;
#else
                    bool b = ExecRead(conn_web, out reader, 
                        " Select first 1 rowid From " + saldo_bydom + 
                        " Where nzp_dom = " + nzp_dom, true).result;
#endif
                    if (!b || !reader.Read() )
                    {
                        //ошибка доступа или нет данных, надо перевыбрать в saldo_xx
                        ReCreateSaldo_bydom(conn_web, out ret, yy, mm, nzp_dom, 0, nzp_wp, ""); //поэтому прежде почистим saldo_dom на всякий случай (или пересоздадим)
                        if (!ret.result)
                        {
                            reader.Close();
                            return;
                        }
                    }
                    else
                    {
                        reader.Close();

                        ExecSQL(conn_db, " Drop table " + ttt_sld, false);

                        //загоним в ttt_sld данные из saldo_xx и вернемся
                        //вставка данных в saldo_xx по домам
                        if (sql.Length > 0) sql.Remove(0, sql.Length);

                        sql.Append(" Select nzp_dom,nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
                        sql.Append("  sum(sum_real) as sum_real, sum(rsum_tarif) as rsum_tarif, sum(sum_charge) as sum_charge, sum(reval) as reval, sum(real_charge) as real_charge, ");
                        sql.Append("  sum(sum_money) as sum_money, sum(money_to) as money_to, sum(money_from) as money_from, sum(money_del) as money_del, ");
                        sql.Append("  sum(sum_insaldo) as sum_insaldo, sum(izm_saldo) as izm_saldo, sum(sum_outsaldo) as sum_outsaldo, ");
                        sql.Append("  sum(sum_fin) as sum_fin, sum(sum_charge - sum_fin) as sum_dolg ");
                        sql.Append(" From " + webdb_path + saldo_bydom + 
                                   " Where nzp_dom = " + nzp_dom);
                        sql.Append(" Group by 1,2,3,4,5 ");
#if PG
#else
                        sql.Append(" Into temp " + ttt_sld + " With no log "); //временная таблица
#endif
#if PG
                        var sqlText = sql.ToString().AddIntoStatement(" Into temp " + ttt_sld);
#else
                        var sqlText = sql.ToString();
#endif

                        ret = ExecSQL(conn_db, sqlText, true);

                        return;
                    }
                }
                //------------------------------------------------
                //по УК или поставщику
                //------------------------------------------------
                else //area из saldo_byarea
                {
                    //прежде сохраним выборку по saldo_byarea на будущее
                    if (sql.Length > 0) sql.Remove(0, sql.Length);

                    sql.Append(" Select 0 as nzp_dom,nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
                    sql.Append("  sum(sum_real) as sum_real, sum(rsum_tarif) as rsum_tarif, sum(sum_charge) as sum_charge, sum(reval) as reval, sum(real_charge) as real_charge, ");
                    sql.Append("  sum(sum_money) as sum_money, sum(money_to) as money_to, sum(money_from) as money_from, sum(money_del) as money_del, ");
                    sql.Append("  sum(sum_insaldo) as sum_insaldo, sum(izm_saldo) as izm_saldo, sum(sum_outsaldo) as sum_outsaldo, ");
                    sql.Append("  sum(sum_fin) as sum_fin, sum(sum_charge - sum_fin) as sum_dolg ");
                    sql.Append(" From " + webdb_path + saldo_byarea +
                               " Where month_ = " + mm +
                               "   and " + fld + " = " + nzp_key +
                               "   and nzp_wp = " + nzp_wp);
                    sql.Append(" Group by 1,2,3,4,5 ");
#if PG
#else
                    sql.Append(" Into temp " + ttt_sld + " With no log "); //временная таблица
#endif
#if PG
                    var sqlText = sql.ToString().AddIntoStatement(" Into temp " + ttt_sld);
#else
                    var sqlText = sql.ToString();
#endif

                    select_area = sqlText;

                    bool noReadArea  = false;
                    bool noReadDom   = false;
                    bool noSaldoArea = false;
                    bool noSaldoDom  = false;

                    if (level == enLevel.nzp_supp)
                    {
                        //по поставщику считаем, что данные посчитанные уже лежат в saldo_byarea по всем nzp_area, где этот поставщик есть
                        //поэтому берем уже посчитанные данные по nzp_supp
                    }
                    else
                    {
                        //выборка для nzp_area

                        //проверим, есть ли уже посчитанные данные nzp_area в saldo_byarea
                        //для find=2 этот этап пропускаем, т.к. все равно перевыберем saldo_bydom
                        if (find == 1)
                        {
                            //наличие таблицы
                            noSaldoArea = !TableInWebCashe(conn_web, saldo_byarea);
                            noSaldoDom  = !TableInWebCashe(conn_web, saldo_bydom);

                            IDataReader reader;
                            if (!noSaldoArea)
                            {
#if PG
                                ret = ExecRead(conn_web, out reader,
                                    " Select oid From " + saldo_byarea +
                                    " Where month_ = " + mm +
                                    "   and " + fld + " = " + nzp_key +
                                    "   and nzp_wp =" + nzp_wp + " limit 1", true);
#else
                                ret = ExecRead(conn_web, out reader,
                                    " Select first 1 rowid From " + saldo_byarea +
                                    " Where month_ = " + mm +
                                    "   and " + fld + " = " + nzp_key +
                                    "   and nzp_wp =" + nzp_wp, true);
#endif
                                if (!ret.result)
                                {
                                    return;
                                }
                                noReadArea = !reader.Read(); //наличие строк
                                reader.Close();
                            }
                            if (!noSaldoDom)
                            {
                                //и данные в saldo_bydom
#if PG
                                ret = ExecRead(conn_web, out reader,
                                    " Select oid From " + saldo_bydom +
                                    " Where " + fld + " = " + nzp_key +
                                    "   and nzp_wp =" + nzp_wp + " limit 1", true);
#else
                                ret = ExecRead(conn_web, out reader,
                                    " Select first 1 rowid From " + saldo_bydom +
                                    " Where " + fld + " = " + nzp_key +
                                    "   and nzp_wp =" + nzp_wp, true);
#endif
                                if (!ret.result)
                                {
                                    return;
                                }
                                noReadDom = !reader.Read(); //наличие строк
                                reader.Close();
                            }
                            
                        }
                    }

                    //если фоновый процесс, или таблицы не созданы, либо по УК нет сальдо
                    if ( find == 2 || noSaldoArea || noSaldoDom || (nzp_area > 0 & noReadArea) ) //||   || noReadDom  || 
                    {
                        //создадим saldo_byarea, если нет
                        if (noSaldoArea) ReCreateSaldo_byarea(conn_web, out ret, yy, mm, nzp_area, nzp_wp);
                        if (!ret.result) return;

                        //создадим saldo_bydom, если нет
                        if (noSaldoDom) ReCreateSaldo_bydom(conn_web, out ret, yy, mm, 0, nzp_area, nzp_wp, "");
                        if (!ret.result) return;

                        if (find == 1)
                        {
                            if (noReadArea || noReadDom)
                            {
                                //расчитать в фоне
                                find = 20; //надо еще проверить, есть ли вообще дома в этом банке, только тогда можно запустить фоновый процесс
                            }
                        }
                    }
                    else
                    {
                        ExecSQL(conn_db, " Drop table " + ttt_sld, false);

                        //выборка по nzp_area или по nzp_supp
                        ret = ExecSQL(conn_db, select_area, true, 3000);

                        return;
                    }
                }
            }

            ExecSQL(conn_db, " Drop table " + ttt_sld, false);

            //посчитать данные
            //------------------------------------------------
            if (nzp_area > 0) //для nzp_area отдельная обработка - цикл по домам!
            //------------------------------------------------
            {
                //кол-во домов в этом банке
                ttt_doms = "ttt_doms_1";

                ExecSQL(conn_db, " Drop table " + ttt_doms, false);

#if PG
                ret = ExecSQL(conn_db,
                    " Select distinct nzp_dom Into temp " + ttt_doms + 
                     " From " + cur_pref + "_data.kvar " +
                    " Where nzp_area = " + nzp_area
                    , true);
#else
                ret = ExecSQL(conn_db,
                    " Select unique nzp_dom From " + cur_pref + "_data:kvar " +
                    " Where nzp_area = " + nzp_area +
                    " Into temp " + ttt_doms + " With no log "
                    , true);
#endif
                if (!ret.result)
                {
                    ret.result = false;
                    return;
                }
#if PG
                ret = ExecSQL(conn_db, " Create unique index ix1_" + ttt_doms + " on " + ttt_doms + "(nzp_dom)", true);
#else
                ret = ExecSQL(conn_db, " Create unique index ix1_" + ttt_doms + " on " + ttt_doms + "(nzp_dom)", true);
#endif
                if (!ret.result)
                {
                    ret.result = false;
                    return;
                }

                IDataReader reader;
                ret = ExecRead(conn_db, out reader, " Select * From " + ttt_doms, true);
                if (!ret.result)
                {
                    ret.result = false;
                    return;
                }

                int k = 0; 
                if (reader.Read()) k = 1;
                reader.Close();
                 

                /*
                IDataReader reader;
                if (!ExecRead(conn_db, out reader, 
                    " Select unique nzp_dom From " + cur_pref + "_data:kvar "+
                    " Where nzp_area = " + nzp_area + 
                    " Order by 1 ", true).result) 
                {
                    ret.result = false;
                    return;
                }
                List<int> doms = new List<int>();

                int k = 0; 
                if (find == 1)
                {
                    //при выборе из saldo_bydom, достаточно один дом (поскольку цикл по домам не выполняется)
                    if (reader.Read()) k = 1;
                }
                else
                {
                    while (reader.Read())
                    {
                        if (reader["nzp_dom"] != DBNull.Value)
                        {
                            k += 1;
                            doms.Add((int)reader["nzp_dom"]);
                        }
                    }
                }
                */

                if (k > 0) //если есть дома в этом банке, считаем
                {
                    if (find == 20)
                    {
                        //надо вернуться и выполнить подсчет в фоновом режиме (другой вызов сервиса с oneway=true)
                        ret.result = false;
                        ret.tag = Constants.workinfon;
                        ret.text = "Запущен фоновой процесс подсчета данных. Это займет некоторое время. Пожалуйста, проверьте данные позднее.";

                        return;
                    }

                    if (find == 2)
                    {
                        //прежде почистим saldo_dom по <nzp_area,nzp_wp> (или пересоздадим)
                        ReCreateSaldo_bydom(conn_web, out ret, yy, mm, 0, nzp_area, nzp_wp, ttt_doms);
                        if (!ret.result) return;

                        //проверим fn_supplire или to_supplier
                        string fn_supplier = "";
                        ret = ExecRead(
                                       conn_db,
                            out reader,
#if PG
                            string.Format(
                                          " Select * From information_schema.tables where table_name = '{0}' and table_schema = '{1}'",
                                "fn_supplier01",
                                cur_pref + "_charge_" + ((yy - 2000) % 100).ToString("00")),
                            false);
#else
                            " Select * From " + cur_pref + "_charge_" + (yy - 2000).ToString("00") + ":systables Where tabname = 'fn_supplier01' ", false);
#endif
                        if (!ret.result)
                        {
                            return;
                        }

                        if (reader.Read())
                        {
                            fn_supplier = "fn_supplier";
                        }
                        else
                        {
                            fn_supplier = "to_supplier";
                        }
                        reader.Close();

                        string tab = "ttts_1"; //для отладки
                        ExecSQL(conn_db, " Drop table " + tab, false);

                        //выборка из charge_xx 
                        GetChargeData(conn_db, out ret, tab, webdb_path, saldo_bydom, saldo_byarea,
                                       nzp_wp, cur_pref, 0, -12345678, nzp_area, yy, mm, ttt_doms, fn_supplier);

                        ExecSQL(conn_db, " Drop table " + ttt_doms, false);
                        ExecSQL(conn_db, " Drop table " + tab, false);
                        if (!ret.result)
                        {
                            return;
                        }

                    }

                    //поэтому прежде почистим saldo_byarea на всякий случай (или пересоздадим)
                    ReCreateSaldo_byarea(conn_web, out ret, yy, mm, nzp_area, nzp_wp); 

                    //вставка данных в saldo_y по УК из saldo_dom
                    if (sql.Length > 0) sql.Remove(0, sql.Length);
                    sql.Append(" Insert into " + webdb_path + saldo_byarea);
                    sql.Append(" ( month_,nzp_wp,nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
                    sql.Append("   sum_real,rsum_tarif,sum_charge,reval,real_charge,sum_money,money_to,money_from,money_del,sum_insaldo,izm_saldo,sum_outsaldo, sum_fin,sum_dolg ) ");

                    sql.Append(" Select " + mm + "," + nzp_wp);
                    sql.Append("  ,nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
                    sql.Append("  sum(sum_real) as sum_real, sum(rsum_tarif) as rsum_tarif, sum(sum_charge) as sum_charge, sum(reval) as reval, sum(real_charge) as real_charge, ");
                    sql.Append("  sum(sum_money) as sum_money, sum(money_to) as money_to, sum(money_from) as money_from, sum(money_del) as money_del, ");
                    sql.Append("  sum(sum_insaldo) as sum_insaldo, sum(izm_saldo) as izm_saldo, sum(sum_outsaldo) as sum_outsaldo, ");
                    sql.Append("  sum(sum_fin) as sum_fin, sum(sum_charge - sum_fin) as sum_dolg ");
                    sql.Append(" From " + webdb_path + saldo_bydom);
                    sql.Append(" Where nzp_area = " + nzp_area + " and nzp_wp =" + nzp_wp);
                    sql.Append(" Group by 1,2,3,4,5,6 ");

                    ret = ExecSQL(conn_db, sql.ToString(), true, 3000);
                    if (!ret.result)
                    {
                        return;
                    }

                    //необходимо, чтобы хоть какие-то данные по nzp_area присутствовали в saldo_byarea
                    //это нужно, чтобы знать, что расчет был выполнен
                    IDataReader reader3;
#if PG
                    ret = ExecRead(conn_web, out reader3,
                        " Select oid From " + saldo_byarea + " a " +
                        " Where nzp_area = " + nzp_area +
                          " and month_ = " + mm + " and nzp_wp = " + nzp_wp + " limit 1", true);
#else
                    ret = ExecRead(conn_web, out reader3,
                        " Select first 1 rowid From " + saldo_byarea + " a " +
                        " Where nzp_area = " + nzp_area +
                          " and month_ = " + mm + " and nzp_wp = " + nzp_wp, true);
#endif
                    if (!ret.result)
                    {
                        return;
                    }
                    if (!reader3.Read())
                    {
                        //занесем нули
                        if (sql.Length > 0) sql.Remove(0, sql.Length);
                        sql.Append(" Insert into " + saldo_byarea);
                        sql.Append(" ( month_,nzp_wp,nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
                        sql.Append("   sum_real,rsum_tarif,sum_charge,reval,real_charge,sum_money,money_to,money_from,money_del,sum_insaldo,izm_saldo,sum_outsaldo, sum_fin,sum_dolg ) ");
                        sql.Append(" Values ( " + mm + "," + nzp_wp + ",-188," + nzp_area + ",-188,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0 ) ");

                        ret = ExecSQL(conn_web, sql.ToString(), true, 3000);
                        if (!ret.result)
                        {
                            return;
                        }
                    }
                    reader3.Close();

                }

                //вот здесь нам пригодился сохраненный запрос по saldo_byarea!
                if (find != 2) ret = ExecSQL(conn_db, select_area, true, 3000);
            }
            //------------------------------------------------
            else
            //------------------------------------------------
            {
                if (level == enLevel.nzp_dom)
                {
                    //почистим saldo_bydom по <nzp_dom,nzp_wp> 
                    ReCreateSaldo_bydom(conn_web, out ret, yy, mm, nzp_dom, 0, nzp_wp, "");
                    if (!ret.result) return;
                }

                //проверим fn_supplire или to_supplier
                IDataReader reader;
                string fn_supplier = "";
#if PG
                ret = ExecRead(
                               conn_db,
                    out reader,
                    string.Format(
                                  " Select * From information_schema.tables where table_name = '{0}' and table_schema = '{1}'",
                        "fn_supplier01",
                        cur_pref + "_charge_" + (yy - 2000).ToString("00")),
                    false);
#else
                ret = ExecRead(conn_db, out reader,
                    " Select * From " + cur_pref + "_charge_" + (yy - 2000).ToString("00") + ":systables Where tabname = 'fn_supplier01' ", false);
#endif
                if (!ret.result)
                {
                    return;
                }

                if (reader.Read())
                {
                    fn_supplier = "fn_supplier";
                }
                else
                {
                    fn_supplier = "to_supplier";
                }

                //выборка по лс или по конкретному дому
                GetChargeData(conn_db, out ret, ttt_sld, webdb_path, saldo_bydom, saldo_byarea,
                               nzp_wp, cur_pref, nzp_kvar, nzp_dom, 0, yy, mm, "", fn_supplier);
                if (!ret.result)
                {
                    return;
                }
            }
        }//CalcSaldo
        //----------------------------------------------------------------------
        void GetChargeData( IDbConnection conn_db, out Returns ret, string ttt_sld, string webdb_path, 
                                   string saldo_bydom, string saldo_byarea,
                                   int nzp_wp, string cur_pref, long nzp_kvar, long nzp_dom, long nzp_area, 
                                   int yy, int mm, string ttt_doms, string fn_supplier) 
        //----------------------------------------------------------------------
        {
            //вот здесь фактически выбираются данные по начислениям и оплатам!
            RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(cur_pref));
            ret = Utils.InitReturns();
#if PG
            string cur_charge = cur_pref + "_charge_" + ((yy - 2000) % 100).ToString("00") + ".charge_" + mm.ToString("00");
#else
            string cur_charge = cur_pref + "_charge_" + (yy - 2000).ToString("00") + ":charge_" + mm.ToString("00");
#endif
            StringBuilder sql = new StringBuilder();

            sql.Append(" Select ");

            if (nzp_kvar > 0) sql.Append("  k.nzp_kvar,k.num_ls,");

            sql.Append("  nzp_dom,nzp_geu,nzp_area,nzp_serv,nzp_supp,");
            sql.Append("  sum(sum_real) as sum_real, sum(rsum_tarif) as rsum_tarif, sum(sum_charge) as sum_charge, sum(reval) as reval, sum(real_charge) as real_charge, ");
            sql.Append("  sum(sum_money) as sum_money, sum(money_to) as money_to, sum(money_from) as money_from, sum(money_del) as money_del, ");
            sql.Append("  sum(sum_insaldo) as sum_insaldo, sum(izm_saldo) as izm_saldo, sum(sum_outsaldo) as sum_outsaldo, ");
            sql.Append("  0 as sum_fin, 0 as sum_dolg ");
#if PG
            sql.Append(" From " + cur_pref + "_data.kvar k, " + cur_charge + " ch ");
#else
            sql.Append(" From " + cur_pref + "_data:kvar k, " + cur_charge + " ch ");
#endif
            sql.Append(" Where k.nzp_kvar = ch.nzp_kvar and ch.dat_charge is null and ch.nzp_serv > 1 ");

            if (ttt_doms.Trim() != "")
            {
                sql.Append(" and k.nzp_dom in ( Select nzp_dom From " + ttt_doms + " ) ");
            }

            if (nzp_dom  > 0) sql.Append(" and k.nzp_dom  = " + nzp_dom);
            if (nzp_kvar > 0) sql.Append(" and k.nzp_kvar = " + nzp_kvar);
            if (nzp_area > 0) sql.Append(" and k.nzp_area = " + nzp_area);

            if (nzp_kvar > 0)
                sql.Append(" Group by 1,2,3,4,5,6,7 ");
            else
                sql.Append(" Group by 1,2,3,4,5 ");

            //вдобавок вытащим распределенные оплаты ЕРЦ!



            bool b_opl = false;
            if (mm < 12)
            {
#if PG
                cur_charge = cur_pref + "_charge_" + ((yy - 2000) % 100).ToString("00") + "." + fn_supplier + (mm + 1).ToString("00");
#else
                cur_charge = cur_pref + "_charge_" + (yy - 2000).ToString("00") + ":" + fn_supplier + (mm + 1).ToString("00");
#endif
                b_opl = true;
            }
            else
            {
                if (yy < r_m.year_)
                {
                    //только в прошлые годы можно выбрать январские оплаты следующего года
#if PG
                    cur_charge = cur_pref + "_charge_" + ((yy - 2000 + 1) % 100).ToString("00") + "." + fn_supplier + "01";
#else
                    cur_charge = cur_pref + "_charge_" + (yy - 2000 + 1).ToString("00") + ":" + fn_supplier + "01";
#endif
                    b_opl = true;
                }
            }

            if (b_opl)
            {
                sql.Append(" Union Select ");

                if (nzp_kvar > 0) sql.Append("  k.nzp_kvar,k.num_ls,");

                sql.Append("  nzp_dom,nzp_geu,nzp_area,nzp_serv,nzp_supp,");
                sql.Append("  0 as sum_real, 0 as rsum_tarif, 0 as sum_charge, 0 as reval, 0 as real_charge, ");
                sql.Append("  0 as sum_money, 0 as money_to, 0 as money_from, 0 as money_del, ");
                sql.Append("  0 as sum_insaldo, 0 as izm_saldo, 0 as sum_outsaldo, ");
                sql.Append("  sum(sum_prih) as sum_fin, 0 as sum_dolg ");
#if PG
                sql.Append(" From " + cur_pref + "_data.kvar k, " + cur_charge + " ch ");
#else
                sql.Append(" From " + cur_pref + "_data:kvar k, " + cur_charge + " ch ");
#endif
                sql.Append(" Where k.num_ls = ch.num_ls and ch.nzp_serv > 1 ");

                if (nzp_dom > 0) sql.Append(" and k.nzp_dom  = " + nzp_dom);
                if (nzp_kvar > 0) sql.Append(" and k.nzp_kvar = " + nzp_kvar);
                if (nzp_area > 0) sql.Append(" and k.nzp_area = " + nzp_area);

                if (nzp_kvar > 0)
                    sql.Append(" Group by 1,2,3,4,5,6,7 ");
                else
                    sql.Append(" Group by 1,2,3,4,5 ");
            }

#if PG
            var sqlText = sql.ToString().AddIntoStatement(" Into temp " + ttt_sld);
#else
            sql.Append(" Into temp " + ttt_sld + " With no log "); //временная таблица
            var sqlText = sql.ToString();
#endif

            
            ret = ExecSQL(conn_db, sqlText, true, 3000);
            if (!ret.result)
            {
                return;
            }

            if (nzp_kvar > 0) return; //сразу выйдем, если надо было получить данные только по лс

            //------------------------------------------------
            //вставка данных в saldo_xx по домам
            //------------------------------------------------
            sql.Remove(0, sql.Length);
            sql.Append(" Insert into " + webdb_path + saldo_bydom);
            sql.Append(" ( nzp_wp,nzp_dom,nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
            sql.Append("   sum_real,sum_charge,reval,real_charge,sum_money,money_to,money_from,money_del,sum_insaldo,izm_saldo,sum_outsaldo, sum_fin,sum_dolg ) ");

            sql.Append(" Select " + nzp_wp);
            sql.Append("  ,nzp_dom,nzp_geu,nzp_area,nzp_serv,nzp_supp, ");
            sql.Append("  sum(sum_real) as sum_real, sum(sum_charge) as sum_charge, sum(reval) as reval, sum(real_charge) as real_charge, ");
            sql.Append("  sum(sum_money) as sum_money, sum(money_to) as money_to, sum(money_from) as money_from, sum(money_del) as money_del, ");
            sql.Append("  sum(sum_insaldo) as sum_insaldo, sum(izm_saldo) as izm_saldo, sum(sum_outsaldo) as sum_outsaldo, ");
            sql.Append("  sum(sum_fin) as sum_fin, sum(sum_charge - sum_fin) as sum_dolg ");
            sql.Append(" From " + ttt_sld + " Where 1 = 1 ");
            //sql.Append(" Group by 1,2,3,4,5,6 ");

            ExecByStep(conn_db, ttt_sld, "nzp_dom",
                    sql.ToString()
                    , 1000, "  Group by 1,2,3,4,5,6 ", out ret);
            //ret = ExecSQL(conn_db, sql.ToString(), true, 3000);
            if (!ret.result)
            {
                return;
            }
        }
        //----------------------------------------------------------------------
        void CreateUserSaldo(IDbConnection conn_web, string tab, out Returns ret) //
        //----------------------------------------------------------------------
        {
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            ExecSQL(conn_web, " Drop table " + tab, false);

            //создать таблицу webdata:txxSaldo
#if PG
            ret = ExecSQL(conn_web,
                      " Create table " + tab +
                      " ( nzp_kvar     integer, " +
                      "   num_ls       integer, " +
                      "   nzp_dom      integer, " +
                      "   nzp_geu      integer, " +
                      "   nzp_area     integer, " +
                      "   nzp_serv     integer, " +
                      "   nzp_supp     integer, " +
                      
                      "   year_        integer, " +
                      "   month_       integer, " +
                      "   sum_real     numeric(13,2) default 0, " +
                      "   rsum_tarif   numeric(13,2) default 0, " +
                      "   sum_charge   numeric(13,2) default 0, " +
                      "   reval        numeric(13,2) default 0, " +
                      "   real_charge  numeric(13,2) default 0, " +
                      "   sum_money    numeric(13,2) default 0, " +
                      "   money_to     numeric(13,2) default 0, " +
                      "   money_from   numeric(13,2) default 0, " +
                      "   money_del    numeric(13,2) default 0, " +
                      "   sum_insaldo  numeric(13,2) default 0, " +
                      "   izm_saldo    numeric(13,2) default 0, " +
                      "   sum_outsaldo numeric(13,2) default 0, " +
                      "   sum_fin      numeric(13,2) default 0, " +
                      "   sum_dolg     numeric(13,2) default 0  " +
                      " ) ", true);
#else
            ret = ExecSQL(conn_web,
                      " Create table " + tab +
                      " ( nzp_kvar     integer, " +
                      "   num_ls       integer, " +
                      "   nzp_dom      integer, " +
                      "   nzp_geu      integer, " +
                      "   nzp_area     integer, " +
                      "   nzp_serv     integer, " +
                      "   nzp_supp     integer, " +
                      
                      "   year_        integer, " +
                      "   month_       integer, " +
                      "   sum_real     decimal(13,2) default 0, " +
                      "   rsum_tarif   decimal(13,2) default 0, " +
                      "   sum_charge   decimal(13,2) default 0, " +
                      "   reval        decimal(13,2) default 0, " +
                      "   real_charge  decimal(13,2) default 0, " +
                      "   sum_money    decimal(13,2) default 0, " +
                      "   money_to     decimal(13,2) default 0, " +
                      "   money_from   decimal(13,2) default 0, " +
                      "   money_del    decimal(13,2) default 0, " +
                      "   sum_insaldo  decimal(13,2) default 0, " +
                      "   izm_saldo    decimal(13,2) default 0, " +
                      "   sum_outsaldo decimal(13,2) default 0, " +
                      "   sum_fin      decimal(13,2) default 0, " +
                      "   sum_dolg     decimal(13,2) default 0  " +
                      " ) ", true);
#endif
            if (ret.result)
            {
                string ix = "i" + tab + "_";

                ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tab + " (nzp_kvar,month_,year_) ", true);
                ExecSQL(conn_web, " Create index " + ix + "_2 on " + tab + " (nzp_dom, year_, month_ )", true); 
                ExecSQL(conn_web, " Create index " + ix + "_3 on " + tab + " (nzp_area,year_, month_) ", true); 
                ExecSQL(conn_web, " Create index " + ix + "_4 on " + tab + " (nzp_geu, year_, month_) ", true); 
                ExecSQL(conn_web, " Create index " + ix + "_5 on " + tab + " (year_, month_ ) ", true);

#if PG
                ExecSQL(conn_web, " analyze  " + tab, true);
#else
                ExecSQL(conn_web, " Update statistics for table  " + tab, true); 
                ExecSQL(conn_web, " Alter table " + tab + " lock mode (row)", true); 
#endif
            }
        }


        /// <summary>
        /// Создает таблицу saldo_YYYY_MM без заполнения
        /// </summary>
        void ReCreateSaldo_bydom(IDbConnection conn_web, out Returns ret, int yy, int mm, long nzp_dom, long nzp_area, int nzp_wp, string ttt_doms) //
        {
            ret = Utils.InitReturns();
            string saldo_bydom = "saldo_" + yy + "_" + mm.ToString("00");

            if (TableInWebCashe(conn_web, saldo_bydom))
            {
                string sql = "Delete From " + saldo_bydom + " Where 1 = 1 ";

                if (nzp_dom > 0) sql += " and nzp_dom  = " + nzp_dom;
                if (nzp_area > 0) sql += " and nzp_area = " + nzp_area;
                if (nzp_wp > 0) sql += " and nzp_wp   = " + nzp_wp;

                ret = ExecSQL(conn_web, sql, true, 3000);
            }
            else
            {
#if PG
                ret = ExecSQL(conn_web,
                          " Create table " + saldo_bydom +
                          " ( nzp_key      serial  not null, " +
                          "   nzp_dom      integer default 0 not null, " +
                          "   nzp_geu      integer default 0 not null, " +
                          "   nzp_area     integer default 0 not null, " +
                          "   nzp_wp       integer default 0 not null, " +
                          "   nzp_serv     integer default 0 not null, " +
                          "   nzp_supp     integer default 0 not null, " +
                          "   sum_real     numeric(13,2) default 0, " +
                          "   rsum_tarif   numeric(13,2) default 0, " +
                          "   sum_charge   numeric(13,2) default 0, " +
                          "   reval        numeric(13,2) default 0, " +
                          "   real_charge  numeric(13,2) default 0, " +
                          "   sum_money    numeric(13,2) default 0, " +
                          "   money_to     numeric(13,2) default 0, " +
                          "   money_from   numeric(13,2) default 0, " +
                          "   money_del    numeric(13,2) default 0, " +
                          "   sum_insaldo  numeric(13,2) default 0, " +
                          "   izm_saldo    numeric(13,2) default 0, " +
                          "   sum_outsaldo numeric(13,2) default 0, " +
                          "   sum_fin      numeric(13,2) default 0, " +
                          "   sum_dolg     numeric(13,2) default 0  " +
                          " ) ", true);
#else
                ret = ExecSQL(conn_web,
                          " Create table " + saldo_bydom +
                          " ( nzp_key      serial  not null, " +
                          "   nzp_dom      integer default 0 not null, " +
                          "   nzp_geu      integer default 0 not null, " +
                          "   nzp_area     integer default 0 not null, " +
                          "   nzp_wp       integer default 0 not null, " +
                          "   nzp_serv     integer default 0 not null, " +
                          "   nzp_supp     integer default 0 not null, " +
                          "   sum_real     decimal(13,2) default 0, " +
                          "   rsum_tarif   decimal(13,2) default 0, " +
                          "   sum_charge   decimal(13,2) default 0, " +
                          "   reval        decimal(13,2) default 0, " +
                          "   real_charge  decimal(13,2) default 0, " +
                          "   sum_money    decimal(13,2) default 0, " +
                          "   money_to     decimal(13,2) default 0, " +
                          "   money_from   decimal(13,2) default 0, " +
                          "   money_del    decimal(13,2) default 0, " +
                          "   sum_insaldo  decimal(13,2) default 0, " +
                          "   izm_saldo    decimal(13,2) default 0, " +
                          "   sum_outsaldo decimal(13,2) default 0, " +
                          "   sum_fin      decimal(13,2) default 0, " +
                          "   sum_dolg     decimal(13,2) default 0  " +
                          " ) ", true);
#endif
                if (ret.result)
                {
                    string ix = "i" + saldo_bydom;

                    ExecSQL(conn_web, " Create unique index " + ix + "_1 on " + saldo_bydom + " (nzp_key) ", true);
                    ExecSQL(conn_web, " Create unique index " + ix + "_2 on " + saldo_bydom + " (nzp_dom,nzp_area,nzp_geu,nzp_wp,nzp_supp,nzp_serv) ", true);
                    ExecSQL(conn_web, " Create index " + ix + "_3 on " + saldo_bydom + " (nzp_area) ", true);
                    ExecSQL(conn_web, " Create index " + ix + "_4 on " + saldo_bydom + " (nzp_geu) ", true);
                    ExecSQL(conn_web, " Create index " + ix + "_5 on " + saldo_bydom + " (nzp_supp) ", true);
                    ExecSQL(conn_web, " Create index " + ix + "_6 on " + saldo_bydom + " (nzp_serv) ", true);
                    ExecSQL(conn_web, " Create index " + ix + "_7 on " + saldo_bydom + " (nzp_wp) ", true);

#if PG
                    ExecSQL(conn_web, " analyze  " + saldo_bydom, true);
#else
                    ExecSQL(conn_web, " Update statistics for table  " + saldo_bydom, true);

                    ExecSQL(conn_web, " Alter table " + saldo_bydom + " lock mode (row)", true);
#endif
                }
            }
        }

        
        /// <summary>
        /// Создает таблицу saldo_YYYY без заполнения
        /// </summary>
        /// <param name="conn_web"></param>
        /// <param name="ret"></param>
        /// <param name="yy"></param>
        /// <param name="mm"></param>
        /// <param name="nzp_area"></param>
        /// <param name="nzp_wp"></param>
        void ReCreateSaldo_byarea(IDbConnection conn_web, out Returns ret, int yy, int mm, long nzp_area, int nzp_wp) //
        {
            ret = Utils.InitReturns();
            string saldo_byarea = "saldo_" + yy;

            if (TableInWebCashe(conn_web, saldo_byarea))
            {
                string sql = "Delete From " + saldo_byarea + " Where month_ = " + mm;

                if (nzp_area > 0) sql += " and nzp_area = " + nzp_area;
                if (nzp_wp > 0) sql += " and nzp_wp   = " + nzp_wp;

                ret = ExecSQL(conn_web, sql, false, 3000);
            }
            else
            {
#if PG
                ret = ExecSQL(conn_web,
                    " Create table " + saldo_byarea +
                    " ( nzp_key      serial  not null, " +
                    "   nzp_area     integer default 0 not null, " +
                    "   nzp_geu      integer default 0 not null, " +
                    "   nzp_wp       integer default 0 not null, " +
                    "   nzp_serv     integer default 0 not null, " +
                    "   nzp_supp     integer default 0 not null, " +
                    "   month_       integer default 0 not null, " +
                    "   sum_real     numeric(13,2) default 0, " +
                    "   rsum_tarif   numeric(13,2) default 0, " +
                    "   sum_charge   numeric(13,2) default 0, " +
                    "   reval        numeric(13,2) default 0, " +
                    "   real_charge  numeric(13,2) default 0, " +
                    "   sum_money    numeric(13,2) default 0, " +
                    "   money_to     numeric(13,2) default 0, " +
                    "   money_from   numeric(13,2) default 0, " +
                    "   money_del    numeric(13,2) default 0, " +
                    "   sum_insaldo  numeric(13,2) default 0, " +
                    "   izm_saldo    numeric(13,2) default 0, " +
                    "   sum_outsaldo numeric(13,2) default 0, " +
                    "   sum_fin      numeric(13,2) default 0, " +
                    "   sum_dolg     numeric(13,2) default 0  " +
                    " ) ", true);
#else
                ret = ExecSQL(conn_web,
                    " Create table " + saldo_byarea +
                    " ( nzp_key      serial  not null, " +
                    "   nzp_area     integer default 0 not null, " +
                    "   nzp_geu      integer default 0 not null, " +
                    "   nzp_wp       integer default 0 not null, " +
                    "   nzp_serv     integer default 0 not null, " +
                    "   nzp_supp     integer default 0 not null, " +

                    "   month_       integer default 0 not null, " +

                    "   sum_real     decimal(13,2) default 0, " +
                    "   rsum_tarif   decimal(13,2) default 0, " +
                    "   sum_charge   decimal(13,2) default 0, " +
                    "   reval        decimal(13,2) default 0, " +
                    "   real_charge  decimal(13,2) default 0, " +
                    "   sum_money    decimal(13,2) default 0, " +
                    "   money_to     decimal(13,2) default 0, " +
                    "   money_from   decimal(13,2) default 0, " +
                    "   money_del    decimal(13,2) default 0, " +
                    "   sum_insaldo  decimal(13,2) default 0, " +
                    "   izm_saldo    decimal(13,2) default 0, " +
                    "   sum_outsaldo decimal(13,2) default 0, " +
                    "   sum_fin      decimal(13,2) default 0, " +
                    "   sum_dolg     decimal(13,2) default 0  " +
                    " ) ", true);
#endif
                if (ret.result)
                {
                    string ix = "i" + saldo_byarea;

                    ExecSQL(conn_web, " Create unique index " + ix + "_1 on " + saldo_byarea + " (nzp_key) ", true);
                    ExecSQL(conn_web, " Create unique index " + ix + "_2 on " + saldo_byarea + " (month_,nzp_area,nzp_geu,nzp_wp,nzp_supp,nzp_serv) ", true);
                    ExecSQL(conn_web, " Create index " + ix + "_3 on " + saldo_byarea + " (nzp_area) ", true);
                    ExecSQL(conn_web, " Create index " + ix + "_4 on " + saldo_byarea + " (nzp_geu) ", true); 
                    ExecSQL(conn_web, " Create index " + ix + "_5 on " + saldo_byarea + " (nzp_supp) ", true); 
                    ExecSQL(conn_web, " Create index " + ix + "_6 on " + saldo_byarea + " (nzp_serv) ", true);
                    ExecSQL(conn_web, " Create index " + ix + "_7 on " + saldo_byarea + " (nzp_wp) ", true); 

#if PG
                    ExecSQL(conn_web, " analyze  " + saldo_byarea, true); 
#else
                    ExecSQL(conn_web, " Update statistics for table  " + saldo_byarea, true); 

                    ExecSQL(conn_web, " Alter table " + saldo_byarea + " lock mode (row)", true); 
#endif
                }
            }
        }

        /// <summary>
        /// Проверяет наличие задач по подсчету сальдо УК со статусом выполняется, в очереди, ошибка.
        /// Если таблица с заданиями не существует, то она создается.
        /// Если mode = 1, то добавляется задача подсчета сальдо по УК
        /// Если mode = 2 или -1, то существующая задача подсчета сальдо УК со статусом выполняется завершается или выставляется статус ошибка.
        /// </summary>
        /// <param name="conn_web"></param>
        /// <param name="ret"></param>
        /// <param name="nzp_area"></param>
        /// <param name="yy"></param>
        /// <param name="mm"></param>
        /// <param name="mode">если 4 - то выход</param>
        /// <param name="txt">примеяется, если mode = 2 или -1</param>
        void CheckSaldoFon(IDbConnection conn_web, out Returns ret, int nzp_area, int yy, int mm, int mode, string txt)
        {
            //mode: 0 - проверка, 1 - установка задании, 2 - снятие (завершение), 3 - очередь, 4 - безусловное выполнение, -1 - ошибка
            ret = Utils.InitReturns();
            if (mode == 4) return;

            if (mode == 2 || mode == -1) //завершение задания 
            {
                ret = ExecSQL(conn_web,
#if PG
                        " Update " + pgDefaultDb + ".saldo_fon Set dat_out = now(), txt = " + Utils.EStrNull(txt) + ", kod_info = " + mode +
#else
                        " Update saldo_fon Set dat_out = current, txt = " + Utils.EStrNull(txt) + ", kod_info = " + mode +
#endif
                        " Where nzp_area = " + nzp_area + " and year_ = " + yy + " and month_= " + mm + " and kod_info = 0", true);
                //conn_web.Close();
                return;
            }

            if (TableInWebCashe(conn_web, "saldo_fon"))
            {
                string sql = " Select kod_info as kod From saldo_fon " +
                             " Where nzp_area = " + nzp_area +
                             "   and year_    = " + yy +
                             "   and month_   in (0, " + mm + ") " +
                             "   and kod_info in (0,3,-1) ";

                IDataReader reader;
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result)
                {
                    //conn_web.Close();
                    return;
                }
                if (reader.Read())
                {
                    int kod = 0;
                    if (reader["kod"] != DBNull.Value) kod = (int)reader["kod"];

                    switch (kod)
                    {
                        case 0:
                            {
                                ret.text = "Выполняется фоновый процесс подсчета. Пожалуйста, проверьте данные позднее.";
                                ret.tag = Constants.workinfon;
                                break;
                            }
                        case 3:
                            {
                                ret.text = "Задание уже находится в очереди. Пожалуйста, проверьте данные позднее.";
                                ret.tag = Constants.workinfon;
                                break;
                            }
                        case -1:
                            {
                                ret.text = "Возникла ошибка выполнения фонового процесса подсчета. Обратитесь к разработчикам.";
                                ret.tag = Constants.workinfon;
                                break;
                            }
                    }
                    ret.result = false;
                    reader.Close();
                    //conn_web.Close();
                    return;
                }
            }
            else
            {
                ret = DbSaldoQueueClient.PrepareQueue(conn_web);
                if (!ret.result) return;
            }

            if (mode == 1) //установка задании
            {
                //проверить, выполняются ли другие задания в данный момент, есди да, то кинуть в очередь на выполнение
                //по-умолчанию, кинем в очередь
#if PG
                ret = ExecSQL(conn_web,
                    string.Format(" Insert into {0}.saldo_fon (nzp_area,year_,month_,kod_info,dat_in) " +
                    " Values (" + nzp_area + "," + yy + "," + mm + ",3,now())", pgDefaultDb), true);
#else
                ret = ExecSQL(conn_web,
                    " Insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in) " +
                    " Values (" + nzp_area + "," + yy + "," + mm + ",3,current)", true);
#endif
                if (!ret.result)
                {
                    //conn_web.Close();
                    return;
                }
                /*
                //и если никто не выполняется в данный момент, тогда переведем на немедленное выполнение
                IDataReader reader;
                ret = ExecRead(conn_web, out reader, " Select kod_info as kod From saldo_fon Where kod_info = 0 ", true);
                if (!ret.result)
                {
                    //conn_web.Close();
                    return;
                }
                if (reader.Read())
                {
                    //conn_web.Close();
                    ret.text = "Задание помещено в очередь ожидания на выполнение. Пожалуйста, проверьте данные позднее.";
                    ret.tag = Constants.workinfon;
                    ret.result = false;
                    return;
                }
                ret = ExecSQL(conn_web,
                        " Update saldo_fon Set kod_info = 0 "+
                        " Where nzp_area = " + nzp_area + " and year_ = " + yy + " and month_= " + mm + " and kod_info = 3", true);
                //conn_web.Close();
                return;
                */
            }
            //conn_web.Close();
        }
        //----------------------------------------------------------------------
        public void InSaldoFon(Saldo finder) //
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                return;
            }
            if (finder.nzp_area == 0) //вызов следует из сальдо поставщиков
            {
                return;
            }
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            //проверка выполнения и установка задания
            CheckSaldoFon(conn_web, out ret, finder.nzp_area, finder.YM.year_, finder.YM.month_, 1, "");
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            /*
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

            SaldoProc(conn_db, conn_web, out ret, finder.nzp_area, finder.YM.year_, finder.YM.month_, finder.pref);
            conn_db.Close();
            */

            conn_web.Close();
        }
        //----------------------------------------------------------------------
        public bool SaldoFonTasks(bool check) //
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return false;

            if (check)
            {
                //проверить наличие таблицы
                CheckSaldoFon(conn_web, out ret, 0, 0, 0, 0, "");
            }

            //конечно, надо проверять зависшие процессы и снимать их (кто выполняется больше 3 часов)
#if PG
            ret = ExecSQL(conn_web, " Update" + pgDefaultDb + ".saldo_fon Set kod_info = 3 Where kod_info = 0 and now() - INTERVAL '3 hours' > dat_in ", true);
#else
            ret = ExecSQL(conn_web, " Update saldo_fon Set kod_info = 3 Where kod_info = 0 and current - 3 units hour > dat_in ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return false;
            }

            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader, " Select * From " + pgDefaultDb + ".saldo_fon Where kod_info = 3 Order by nzp_area, year_, month_ ", true);
#else
            ret = ExecRead(conn_web, out reader, " Select * From saldo_fon Where kod_info = 3 Order by nzp_area, year_, month_ ", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return false;
            }
            bool b = reader.Read();
            reader.Close();
            conn_web.Close();
            return b;
        }
        
        public bool SaldoFon(out Returns ret) //
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return false;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return false;

            MyDataReader reader = null;
            string tab = sDefaultSchema + "saldo_fon";

            try
            {
                bool isAnyTaskBeingProcessed = DbSaldoQueueClient.IsAnyTaskBeingProcessed(conn_web, tab, out ret);

                if (!ret.result || isAnyTaskBeingProcessed)
                {
                    return false;
                }

//#if PG
//                DBManager.ExecSQL(conn_web, "set search_path to 'public'", false);
//#endif

                string sqlStr;

                sqlStr = " Select * From " + tab + " Where kod_info = 3 Order by dat_in";

                ret = ExecRead(conn_web, out reader, sqlStr, true);
                if (!ret.result)
                {
                    return false;
                }
                if (!reader.Read())
                {
                    //заданий нет, этот процесс славно поработал, выходим из цикла обработки
                    return false;
                }
                else
                {
                    //есть задание, выполняем!
                    int nzp_area = 0;
                    int yy = 0;
                    int mm = -1;
                    int nzp_key = 0;

                    if (reader["nzp_key"] != DBNull.Value) nzp_key = (int)reader["nzp_area"];
                    if (reader["nzp_area"] != DBNull.Value) nzp_area = (int)reader["nzp_area"];
                    if (reader["year_"] != DBNull.Value) yy = (int)reader["year_"];
                    if (reader["month_"] != DBNull.Value) mm = (int)reader["month_"];
                    
                    reader.Close();

                    if (nzp_area > 0 && yy > 0 && mm >= 0)
                    {
                        ret = ExecSQL(conn_web,
                            " Update " + tab + 
                            " Set kod_info = 0, dat_work =  " + sCurDateTime +
                            " Where nzp_key = " + nzp_key, true);
                        if (!ret.result) return false;

                        SaldoProc(conn_db, conn_web, out ret, nzp_area, yy, mm, "");
                        return ret.result;
                    }
                    else
                    {
                        ret = ExecSQL(conn_web,
                            " Update " + tab + 
                            " Set kod_info = " + (int)FonTask.Statuses.Failed + ", dat_work =  " + sCurDateTime + ", dat_out =  " + sCurDateTime +", txt = 'Неверные параметры задачи'"+
                            " Where nzp_key = " + nzp_key, true);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("DbCharge.SaldoFon\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally 
            {
                if (reader != null) reader.Close();
                conn_web.Close();
                conn_db.Close();
            }
            return false;
        }
        //----------------------------------------------------------------------
        void SaldoProc( IDbConnection conn_db,
                               IDbConnection conn_web,
                               out Returns ret,
                               int nzp_area, int yy, int mm, string cur_pref) //
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(cur_pref));
            foreach (_Point zap in Points.PointList)
            {                
                if (cur_pref != "")
                {
                    if (zap.pref != cur_pref) continue;
                }

                int mb = 1;
                int me = 12;

                if (mm > 0)
                {
                    mb = mm;
                    me = mb;
                }
                else
                    if (yy == r_m.year_)
                    {
                        me = r_m.month_;
                    }

                for (int m = mb; m <= me; m = m + 1)
                {
                    CalcSaldo( conn_db, conn_web, out ret, "ttt_sld",
                               zap.pref, zap.nzp_wp,
                               nzp_area, enLevel.nzp_area,
                               yy, m, 2);

                    if (!ret.result) break;
                }

                if (!ret.result) break;
            }

            int mode = 2;
            string txt = "";
            if (!ret.result)
            {
                mode = -1;
                txt = "Ошибка выполнения! Смотрите журнал ошибок."; //ret.text;
            }
            CheckSaldoFon(conn_web, out ret, nzp_area, yy, mm, mode, txt);
        }

        //----------------------------------------------------------------------
        public void LoadSaldo_Area(RecordMonth ym_s, RecordMonth ym_po, string pref, int nzp_wp, int nzp_area, out Returns ret) //
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

            for (int yy = ym_s.year_; yy <= ym_po.year_; yy++)
            {
                int mm_s = 1;
                int mm_po = 12;

                if (yy == ym_s.year_)
                    mm_s = ym_s.month_;
                else
                    if (yy == ym_po.year_)
                        mm_po = ym_po.month_;

                for (int mm = mm_s; mm <= mm_po; mm++)
                {
                    CalcSaldo(conn_db, conn_web, out ret, "ttt_sld", pref, nzp_wp, nzp_area, enLevel.nzp_area, yy, mm, 2);
                    ExecSQL(conn_db, " Drop table ttt_sld ", false);
                    if (!ret.result) break;
                }

                if (!ret.result) break;
            }

            conn_db.Close();
            conn_web.Close();

        }//LoadSaldo_dom
    }
}
