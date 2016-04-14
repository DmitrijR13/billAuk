using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;
using System.Globalization;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbSzClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        /// <summary>
        /// процедура возвращает результат поиска
        /// </summary>
        /// <returns></returns>
        public List<SzFinder> GetFindSz(SzFinder finder, out Returns ret)
        {
            List<SzFinder> resList = new List<SzFinder>();
            string tXX_sz = "t" + finder.nzp_user + "_sz";//название таблицы
            string data_table = "sz_lsdata";
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;
            ret = Utils.InitReturns();
            string skip = "";

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            if (finder.skip > 0)
            {
                skip = " skip " + finder.skip;
            }

#if PG
            sql.Append("Select COUNT(*) From " + conn_web.Database + "@" + DBManager.getServer(conn_web) + "." + tXX_sz + ";");
#else
sql.Append("Select COUNT(*) From " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_sz + ";");
#endif
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки GetFindSz" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }
            if (reader != null)
            {
                while (reader.Read())
                {
                    if (reader.GetValue(0) != DBNull.Value) ret.tag = Convert.ToInt32(reader.GetString(0));//общее количество строк в таблице
                }
            }

            reader = null;


            if (ret.tag != 0)
            {
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" Select " + skip +
                                           " a.nzp_key," +
                                           " a.mo," +
                                           " a.rajon," +
                                           " a.name_uk," +
                                           " a.name_podr," +
                                           " \'ул.\'||trim(a.ulica)||" +
                                           " (case when a.ndom is null then \' \' else " +
                                           " \' д.\'||trim(a.ndom)|| " +
                                           " (case when a.nkor=\'-\' then \' \' else  " +
                                           " \' корп.\'||trim(a.nkor) end) end)|| " +
                                           " (case when a.nkvar is null then \' \' else " +
                                           " \' кв.\'||trim(a.nkvar)|| " +
                                           " (case when trim(a.nkvar_n)=\'-\' then \' \' else " +
                                           " \' комн.\'||trim(a.nkvar_n) end) " +
                                           " end ) adr," +
                                           " a.nzp_pretender," +
                                           " a.num_ls " +
                                           " From " + conn_web.Database + "." + data_table + " a," +
                                                      conn_web.Database + "." + tXX_sz + " b" +
                                           " Where a.nzp_key = b.nzp_key " +
                                           " ORDER BY a.mo, a.rajon, a.name_uk, a.name_podr, a.ulica, a.idom, a.ndom, a.ikvar, a.nkvar;");
#else
 sql.Append(" Select " + skip +
                            " a.nzp_key," +
                            " a.mo," + 
                            " a.rajon," +
                            " a.name_uk," +
                            " a.name_podr," +
                            " \'ул.\'||trim(a.ulica)||" +
                            " (case when a.ndom is null then \' \' else " +
                            " \' д.\'||trim(a.ndom)|| " +
                            " (case when a.nkor=\'-\' then \' \' else  " +
                            " \' корп.\'||trim(a.nkor) end) end)|| " +
                            " (case when a.nkvar is null then \' \' else " +
                            " \' кв.\'||trim(a.nkvar)|| " +
                            " (case when trim(a.nkvar_n)=\'-\' then \' \' else " +
                            " \' комн.\'||trim(a.nkvar_n) end) " +
                            " end ) adr," +
                            " a.nzp_pretender," +
                            " a.num_ls " +
                            " From " + conn_web.Database + ":" + data_table + " a," +
                                       conn_web.Database + ":" + tXX_sz + " b" +
                            " Where a.nzp_key = b.nzp_key " +
                            " ORDER BY a.mo, a.rajon, a.name_uk, a.name_podr, a.ulica, a.idom, a.ndom, a.ikvar, a.nkvar;");
#endif
                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки GetFindSz" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;
                        SzFinder item = new SzFinder();
                        if (reader["nzp_key"] != DBNull.Value) item.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                        if (reader["mo"] != DBNull.Value) item.mo = Convert.ToString(reader["mo"]).Trim();
                        if (reader["rajon"] != DBNull.Value) item.rajon = Convert.ToString(reader["rajon"]).Trim();
                        if (reader["name_uk"] != DBNull.Value) item.name_uk = Convert.ToString(reader["name_uk"]).Trim();
                        if (reader["adr"] != DBNull.Value) item.adr = Convert.ToString(reader["adr"]).Trim();
                        if (reader["nzp_pretender"] != DBNull.Value) item.pss = Convert.ToString(reader["nzp_pretender"]).Trim();
                        if (reader["num_ls"] != DBNull.Value) item.pss += " / " + Convert.ToInt32(reader["num_ls"]);
                        if (reader["num_ls"] != DBNull.Value) item.num_ls = Convert.ToInt32(reader["num_ls"]);
                        resList.Add(item);
                        if (finder.rows > 0 && i >= finder.rows) break;
                    }
                    reader.Close();
                }
            }
            conn_web.Close();
            return resList;
        }


        /// <summary>
        /// процедура возвращает данные карточки 
        /// </summary>
        /// <returns></returns>
        public SzKart GetKartSz(SzFinder finder, out Returns ret)
        {
            SzKart res = new SzKart();
            string data_table = "sz_lsdata";
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;
            ret = Utils.InitReturns();
            bool flag = false;

            decimal tarif7;
            decimal tarif9;

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" Select " +
                                    " mo," +
                                    " rajon," +
                                    " name_uk, " +
                                    " name_supp, " +
                                    " ulica, " +
                                    " ndom, " +
                                    " nkor, " +
                                    " nkvar, " +
                                    " nkvar_n, " +
                                    " nzp_pretender, " +
                                    " num_ls, " +
                                    " cnt_gil, " +
                                    " cnt_lg, " +
                                    " s_ot, " +
                                    " s_ot_sn " +
                                    " From " + conn_web.Database + "." + data_table +
                                    " Where nzp_key = " + finder.nzp_key + ";");
#else
sql.Append( " Select " + 
                        " mo," +
                        " rajon," +
                        " name_uk, " +
                        " name_supp, " +
                        " ulica, " +
                        " ndom, " +
                        " nkor, " +
                        " nkvar, " +
                        " nkvar_n, " +
                        " nzp_pretender, " +
                        " num_ls, " +
                        " cnt_gil, " +
                        " cnt_lg, " +
                        " s_ot, " +
                        " s_ot_sn " +
                        " From " + conn_web.Database + ":" + data_table +
                        " Where nzp_key = " + finder.nzp_key + ";");
#endif

            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки GetKartSz " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }
            if (reader != null)
            {
                while (reader.Read())
                {
                    if (reader["mo"] != DBNull.Value) res.mo = Convert.ToString(reader["mo"]).Trim();
                    if (reader["rajon"] != DBNull.Value) res.rajon = Convert.ToString(reader["rajon"]).Trim();
                    if (reader["name_uk"] != DBNull.Value) res.name_uk = Convert.ToString(reader["name_uk"]).Trim();
                    if (reader["name_supp"] != DBNull.Value) res.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["ulica"] != DBNull.Value) res.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["ndom"] != DBNull.Value) res.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) res.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["nkvar"] != DBNull.Value) res.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                    if (reader["nkvar_n"] != DBNull.Value) res.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                    if (reader["nzp_pretender"] != DBNull.Value) res.pss = Convert.ToString(reader["nzp_pretender"]).Trim();
                    if (reader["num_ls"] != DBNull.Value) res.num_ls = Convert.ToInt32(reader["num_ls"]);
                    if (reader["cnt_gil"] != DBNull.Value) res.cnt_gil = Convert.ToInt32(reader["cnt_gil"]);
                    if (reader["cnt_lg"] != DBNull.Value) res.cnt_lg = Convert.ToInt32(reader["cnt_lg"]);
                    if (reader["s_ot"] != DBNull.Value) res.s_ot = Convert.ToString(reader["s_ot"]).Trim();
                    if (reader["s_ot_sn"] != DBNull.Value) res.s_ot_sn = Convert.ToString(reader["s_ot_sn"]).Trim();
                }
            }



            sql.Remove(0, sql.Length);
            reader = null;
#if PG
            sql.Append(" Select " +
                                  " nzp_raj " +
                                  " From " + conn_web.Database + "." + data_table +
                                  " Where nzp_key = " + finder.nzp_key + ";");
#else
 sql.Append(" Select " +
                       " nzp_raj " +
                       " From " + conn_web.Database + ":" + data_table +
                       " Where nzp_key = " + finder.nzp_key + ";");
#endif
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки GetKartSz " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }
            if (reader != null)
            {
                while (reader.Read())
                {
                    if (reader["nzp_raj"] != DBNull.Value) res.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                }
            }

            if(res.nzp_raj == 1 ||
               res.nzp_raj == 2 ||
               res.nzp_raj == 3 ||
               res.nzp_raj == 4 ||
               res.nzp_raj == 5 ||
               res.nzp_raj == 6 ||
               res.nzp_raj == 7 ||
               res.nzp_raj == 27)
            {
                flag = true;
            }

            if(flag)
            {
                tarif7 = (decimal)(600.38/600.00);
                tarif9 = (decimal)(1271.00/600.00);
            }
            else
            {
                tarif7 = (decimal)(600.38 / 566.40);
                tarif9 = (decimal)(1271.00 / 566.40);
            }


            List<SzList> list = new List<SzList>();
            reader = null;
            sql.Remove(0, sql.Length);


#if PG
            sql.Append(" Select " +
                                   " tarif_ot, " +
                                   " tarif_gv, " +
                                   " ot_nach7, " +
                                   " gv_nach7, " +
                                   " ot_lg7, " +
                                   " gv_lg7, " +
                                   " ot_smo7, " +
                                   " gv_smo7, " +
                                   " ot_sum7, " +
                                   " gv_sum7, " +
                                   " kv_sum7, " +
                                   " ot_nach9, " +
                                   " gv_nach9, " +
                                   " ot_lg9, " +
                                   " gv_lg9, " +
                                   " ot_smo9, " +
                                   " gv_smo9, " +
                                   " ot_k9, " +
                                   " gv_k9, " +
                                   " ot_sum9, " +
                                   " gv_sum9, " +
                                   " kv_sum9, " +
                                   " perc9, " +
                                   " ot_sum9k_ot, " +
                                   " gv_sum9k_ot, " +
                                   " kv_sum9k_ot, " +
                                   " perc9k_ot, " +
                                   " ot_sum9k, " +
                                   " gv_sum9k, " +
                                   " kv_sum9k, " +
                                   " perc9k " +
                                   " From " + conn_web.Database + "." + data_table +
                                   " Where nzp_key = " + finder.nzp_key + ";");
#else
 sql.Append(" Select " +
                        " tarif_ot, " +
                        " tarif_gv, " +
                        " ot_nach7, " +
                        " gv_nach7, " +
                        " ot_lg7, " +
                        " gv_lg7, " +
                        " ot_smo7, " +
                        " gv_smo7, " +
                        " ot_sum7, " +
                        " gv_sum7, " +
                        " kv_sum7, " +
                        " ot_nach9, " +
                        " gv_nach9, " +
                        " ot_lg9, " +
                        " gv_lg9, " +
                        " ot_smo9, " +
                        " gv_smo9, " +
                        " ot_k9, " +
                        " gv_k9, " +
                        " ot_sum9, " +
                        " gv_sum9, " +
                        " kv_sum9, " +
                        " perc9, " +
                        " ot_sum9k_ot, " +
                        " gv_sum9k_ot, " +
                        " kv_sum9k_ot, " +
                        " perc9k_ot, " +
                        " ot_sum9k, " +
                        " gv_sum9k, " +
                        " kv_sum9k, " +
                        " perc9k " +
                        " From " + conn_web.Database + ":" + data_table +
                        " Where nzp_key = " + finder.nzp_key + ";");
#endif

            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки GetKartSz " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }
            if (reader != null)
            {
                while (reader.Read())
                {
                    SzList temp0 = new SzList();
                    if (reader["tarif_ot"] != DBNull.Value) temp0.otop = Convert.ToString(reader["tarif_ot"]).Trim();
                    if (reader["tarif_gv"] != DBNull.Value) temp0.gvs = Convert.ToString(reader["tarif_gv"]).Trim();
     
                    SzList temp1 = new SzList();
                    temp1.parametr = "прогнозируемый тариф июля";
                    decimal temp_otop7 = Convert.ToDecimal(temp0.otop) * tarif7;
                    decimal temp_gvs7 = Convert.ToDecimal(temp0.gvs) * tarif7;
                    temp1.otop = Convert.ToString(Math.Round(temp_otop7,2));
                    temp1.gvs = Convert.ToString(Math.Round(temp_gvs7,2));
                    list.Add(temp1);

                    SzList temp2 = new SzList();
                    temp2.parametr = "начислено за июль 2012";
                    if (reader["ot_nach7"] != DBNull.Value) temp2.otop = Convert.ToString(reader["ot_nach7"]).Trim();
                    if (reader["gv_nach7"] != DBNull.Value) temp2.gvs = Convert.ToString(reader["gv_nach7"]).Trim();
                    decimal t2 = Math.Round((Convert.ToDecimal(temp2.otop) + Convert.ToDecimal(temp2.gvs)),2);
                    temp2.summ = Convert.ToString(t2);
                    list.Add(temp2);

                    SzList temp3 = new SzList();
                    temp3.parametr = "льгота за июль 2012";
                    if (reader["ot_lg7"] != DBNull.Value) temp3.otop = Convert.ToString(reader["ot_lg7"]).Trim();
                    if (reader["gv_lg7"] != DBNull.Value) temp3.gvs = Convert.ToString(reader["gv_lg7"]).Trim();
                    decimal t3 = Math.Round((Convert.ToDecimal(temp3.otop) + Convert.ToDecimal(temp3.gvs)),2);
                    temp3.summ = Convert.ToString(t3);
                    list.Add(temp3);

                    SzList temp4 = new SzList();
                    temp4.parametr = "СМО за июль 2012";
                    if (reader["ot_smo7"] != DBNull.Value) temp4.otop = Convert.ToString(reader["ot_smo7"]).Trim();
                    if (reader["gv_smo7"] != DBNull.Value) temp4.gvs = Convert.ToString(reader["gv_smo7"]).Trim();
                    decimal t4 = Math.Round((Convert.ToDecimal(temp4.otop) + Convert.ToDecimal(temp4.gvs)), 2);
                    temp4.summ = Convert.ToString(t4);
                    list.Add(temp4);

                    SzList temp5 = new SzList();
                    temp5.parametr = "платеж за июль 2012";
                    if (reader["ot_sum7"] != DBNull.Value) temp5.otop = Convert.ToString(reader["ot_sum7"]).Trim();
                    if (reader["gv_sum7"] != DBNull.Value) temp5.gvs = Convert.ToString(reader["gv_sum7"]).Trim();
                    if (reader["kv_sum7"] != DBNull.Value) temp5.summ = Convert.ToString(reader["kv_sum7"]).Trim();
                    list.Add(temp5);

                    SzList temp6 = new SzList();
                    temp6.parametr = "прогнозируемый тариф сентября";
                    decimal temp_otop = Convert.ToDecimal(temp0.otop) * tarif9;
                    decimal temp_gvs = Convert.ToDecimal(temp0.gvs) * tarif9;
                    temp6.otop = Convert.ToString(Math.Round(temp_otop,2));
                    temp6.gvs = Convert.ToString(Math.Round(temp_gvs,2));
                    list.Add(temp6);

                    SzList temp7 = new SzList();
                    temp7.parametr = "начислено за сентябрь 2012";
                    if (reader["ot_nach9"] != DBNull.Value) temp7.otop = Convert.ToString(reader["ot_nach9"]).Trim();
                    if (reader["gv_nach9"] != DBNull.Value) temp7.gvs = Convert.ToString(reader["gv_nach9"]).Trim();
                    decimal t7 = Math.Round((Convert.ToDecimal(temp7.otop) + Convert.ToDecimal(temp7.gvs)), 2);
                    temp7.summ = Convert.ToString(t7);
                    list.Add(temp7);


                    SzList temp8 = new SzList();
                    temp8.parametr = "льгота за сентябрь 2012";
                    if (reader["ot_lg9"] != DBNull.Value) temp8.otop = Convert.ToString(reader["ot_lg9"]).Trim();
                    if (reader["gv_lg9"] != DBNull.Value) temp8.gvs = Convert.ToString(reader["gv_lg9"]).Trim();
                    decimal t8 = Math.Round((Convert.ToDecimal(temp8.otop) + Convert.ToDecimal(temp8.gvs)), 2);
                    temp8.summ = Convert.ToString(t8);
                    list.Add(temp8);

                    SzList temp9 = new SzList();
                    temp9.parametr = "СМО за сентябрь 2012";
                    if (reader["ot_smo9"] != DBNull.Value) temp9.otop = Convert.ToString(reader["ot_smo9"]).Trim();
                    if (reader["gv_smo9"] != DBNull.Value) temp9.gvs = Convert.ToString(reader["gv_smo9"]).Trim();
                    decimal t9 = Math.Round((Convert.ToDecimal(temp9.otop) + Convert.ToDecimal(temp9.gvs)), 2);
                    temp9.summ = Convert.ToString(t9);
                    list.Add(temp9);

                    SzList temp10 = new SzList();
                    temp10.parametr = "целевая компенсация за сентябрь 2012";
                    if (reader["ot_k9"] != DBNull.Value) temp10.otop = Convert.ToString(reader["ot_k9"]).Trim();
                    if (reader["gv_k9"] != DBNull.Value) temp10.gvs = Convert.ToString(reader["gv_k9"]).Trim();
                    decimal t10 = Math.Round((Convert.ToDecimal(temp10.otop) + Convert.ToDecimal(temp10.gvs)), 2);
                    temp10.summ = Convert.ToString(t10);
                    list.Add(temp10);

                    SzList temp11 = new SzList();
                    temp11.parametr = "платеж за сентябрь 2012 без учета ЦК";
                    if (reader["ot_sum9"] != DBNull.Value) temp11.otop = Convert.ToString(reader["ot_sum9"]).Trim();
                    if (reader["gv_sum9"] != DBNull.Value) temp11.gvs = Convert.ToString(reader["gv_sum9"]).Trim();
                    if (reader["kv_sum9"] != DBNull.Value) temp11.summ = Convert.ToString(reader["kv_sum9"]).Trim();
                    list.Add(temp11);


                    SzList temp12 = new SzList();
                    temp12.parametr = "процент повышения сентября к июлю без учета ЦК ";
                    decimal otop_12;
                    decimal gvs_12;
                    if (temp11.otop != "0.00")
                    {
                        otop_12 = Math.Round(Convert.ToDecimal(temp11.otop) / Convert.ToDecimal(temp5.otop) * 100, 2);
                        temp12.otop = Convert.ToString(otop_12 - 100);
                    }
                    else
                    {
                        temp12.otop = "0.00";
                    }
                    if (temp11.gvs != "0.00")
                    {
                        gvs_12 = Math.Round(Convert.ToDecimal(temp11.gvs) / Convert.ToDecimal(temp5.gvs) * 100, 2);
                        temp12.gvs = Convert.ToString(gvs_12 - 100);
                    }
                    else
                    {
                        temp12.gvs = "0.00";
                    }
                    if (reader["perc9"] != DBNull.Value) temp12.summ = Convert.ToString(reader["perc9"]).Trim();
                    list.Add(temp12);

                    SzList temp13 = new SzList();
                    temp13.parametr = "платеж за сентябрь 2012 с учетом ЦК";
                    if (reader["ot_sum9k_ot"] != DBNull.Value) temp13.otop = Convert.ToString(reader["ot_sum9k_ot"]).Trim();
                    if (reader["gv_sum9k_ot"] != DBNull.Value) temp13.gvs = Convert.ToString(reader["gv_sum9k_ot"]).Trim();
                    if (reader["kv_sum9k_ot"] != DBNull.Value) temp13.summ = Convert.ToString(reader["kv_sum9k_ot"]).Trim();
                    list.Add(temp13);
                }
                reader.Close();
            }
            res.list = list;
            conn_web.Close();
            return res;
        }

    }
}
