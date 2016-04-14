

using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
namespace Bars.KP50.DB.Faktura
{

    public class OldRTFaktura
    {
        /// <summary> вытащить Начисления Для счет фактуры
        /// </summary>
        public List<Charge> GetBillCharge(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = new Returns { result = true, text = String.Empty };
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            var spis = new List<Charge>();

            if (finder.YM.year_ <= 0) return null;
            if (finder.YM.month_ <= 0) return null;

            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            string tXxSpls;
            try
            {
                ret = DBManager.OpenDb(connWeb, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Формирование счетов. Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                tXxSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + 
                    "t" + finder.nzp_user + "_charge";
            }
            finally
            {
                connWeb.Close();
            }

            IDataReader reader2;



            // выбрать данные из КЭШ таблицы
            var s = new StringBuilder();
            s.Append(" Select service, a.nzp_serv, a.nzp_frm,  a.tarif, a.is_device, a.measure, a.ordering ");
            s.Append(", sum(a.reval) as reval ");
            s.Append(", sum(a.rsum_tarif) as rsum_tarif ");
            s.Append(", sum(a.sum_real) as sum_real ");
            s.Append(", sum(a.sum_tarif) as sum_tarif ");
            s.Append(", sum(a.sum_nedop) as sum_nedop ");
            s.Append(", sum(a.sum_lgota) as sum_lgota ");
            s.Append(", sum(a.sum_dlt_tarif_p) as sum_dlt_tarif_p ");
            s.Append(", sum(a.sum_tarif_sn_f) as sum_tarif_sn_f ");
            s.Append(", sum(a.real_charge) as real_charge ");
            s.Append(", sum(a.sum_money) as sum_money ");
            s.Append(", sum(a.sum_insaldo) as sum_insaldo ");
            s.Append(", sum(a.sum_outsaldo) as sum_outsaldo ");
            s.Append(", sum(a.c_calc) as c_calc ");
            s.Append(", sum(a.c_reval) as c_reval ");
            s.Append(", sum(a.sum_charge) as sum_charge ");
            s.Append(", sum(a.c_okaz) as c_okaz ");
            s.Append(" From " + tXxSpls + " a ");
            s.Append(" where nzp_kvar = " + finder.nzp_kvar);
            s.Append(" and dat_month = '01." + finder.month_ + "." + finder.year_ + "'");
            s.Append(" and nzp_serv>1 and dat_charge is null  ");
            s.Append(
                " and abs(rsum_tarif)+abs(reval)+abs(real_charge)+abs(sum_insaldo)+abs(sum_outsaldo)+abs(sum_money)>0.001  ");
            s.Append(" group by service, a.nzp_serv, a.nzp_frm, a.tarif, a.is_device, a.measure, a.ordering  ");
            s.Append(" order by ordering, service, a.nzp_serv, a.nzp_frm, a.tarif  ");



            if (!DBManager.ExecRead(connWeb, out reader2, s.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + s, MonitorLog.typelog.Error, 20, 201, true);
                connWeb.Close();
                return null;
            }
            try
            {
                int id = 0;
                while (reader2.Read())
                {
                    Charge zap = new Charge();

                    zap.id = ++id;
                    zap.dat_charge = "";

                    if (reader2["tarif"] != DBNull.Value) zap.tarif = Convert.ToDecimal(reader2["tarif"]);
                    if (reader2["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                    if (reader2["is_device"] != DBNull.Value) zap.is_device = Convert.ToInt32(reader2["is_device"]);
                    if (reader2["service"] != DBNull.Value) zap.service = Convert.ToString(reader2["service"]);
                    if (reader2["measure"] != DBNull.Value) zap.measure = Convert.ToString(reader2["measure"]);
                    if (reader2["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader2["nzp_frm"]);
                    if (reader2["sum_real"] != DBNull.Value) zap.sum_real = Convert.ToDecimal(reader2["sum_real"]);
                    if (reader2["reval"] != DBNull.Value) zap.reval = Convert.ToDecimal(reader2["reval"]);
                    if (reader2["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader2["sum_tarif"]);
                    if (reader2["rsum_tarif"] != DBNull.Value)
                        zap.rsum_tarif = Convert.ToDecimal(reader2["rsum_tarif"]);
                    if (reader2["sum_nedop"] != DBNull.Value) zap.sum_nedop = Convert.ToDecimal(reader2["sum_nedop"]);
                    if (reader2["sum_lgota"] != DBNull.Value) zap.sum_lgota = Convert.ToDecimal(reader2["sum_lgota"]);
                    if (reader2["sum_dlt_tarif_p"] != DBNull.Value)
                        zap.sum_dlt_tarif_p = Convert.ToDecimal(reader2["sum_dlt_tarif_p"]);
                    if (reader2["sum_tarif_sn_f"] != DBNull.Value)
                        zap.sum_tarif_sn_f = Convert.ToDecimal(reader2["sum_tarif_sn_f"]);
                    if (reader2["real_charge"] != DBNull.Value)
                        zap.real_charge = Convert.ToDecimal(reader2["real_charge"]);
                    if (reader2["sum_money"] != DBNull.Value) zap.sum_money = Convert.ToDecimal(reader2["sum_money"]);
                    if (reader2["sum_insaldo"] != DBNull.Value)
                        zap.sum_insaldo = Convert.ToDecimal(reader2["sum_insaldo"]);
                    if (reader2["sum_outsaldo"] != DBNull.Value)
                        zap.sum_outsaldo = Convert.ToDecimal(reader2["sum_outsaldo"]);
                    if (reader2["sum_charge"] != DBNull.Value)
                        zap.sum_charge = Convert.ToDecimal(reader2["sum_charge"]);
                    if (reader2["c_reval"] != DBNull.Value) zap.c_reval = Convert.ToDecimal(reader2["c_reval"]);
                    if (reader2["c_calc"] != DBNull.Value) zap.c_calc = Convert.ToDecimal(reader2["c_calc"]);
                    if (reader2["c_okaz"] != DBNull.Value) zap.c_okaz = Convert.ToDecimal(reader2["c_okaz"]);
                    //if (zap.tarif > 0) zap.c_calc = Decimal.Round(zap.sum_tarif / zap.tarif, 4);
                    spis.Add(zap);
                }

                reader2.Close();
                connWeb.Close();


                #region Объединение по service_uniona

                string connectionString = Points.GetConnByPref(finder.pref);
                IDbConnection connDB = DBManager.GetConnection(connectionString);
                ret = DBManager.OpenDb(connDB, true);
                if (!ret.result)
                {
                    connDB.Close();
                    return null;
                }


                s.Remove(0, s.Length);
                s.Append(" Select service, s.nzp_serv_uni, s.nzp_serv_base ");
                s.Append(" From " + finder.pref + DBManager.sKernelAliasRest + "service_union s," +
                    DBManager.sKernelAliasRest + "services a");
                s.Append(" where s.nzp_serv_base=a.nzp_serv");
                s.Append(" order by service, s.nzp_serv_base, s.nzp_serv_uni ");

                if (!DBManager.ExecRead(connDB, out reader2, s.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + s, MonitorLog.typelog.Error, 20, 201, true);
                    connDB.Close();
                    return spis;
                }

                int nzpServUni = 0;
                int nzpServBase = 0;
                string service = "";

                while (reader2.Read())
                {
                    if (reader2["nzp_serv_uni"] != DBNull.Value)
                        nzpServUni = Convert.ToInt32(reader2["nzp_serv_uni"]);
                    if (reader2["nzp_serv_base"] != DBNull.Value)
                        nzpServBase = Convert.ToInt32(reader2["nzp_serv_base"]);
                    if (reader2["service"] != DBNull.Value) service = Convert.ToString(reader2["service"]);

                    for (int i = 0; i <= spis.Count - 1; i++)
                    {
                        if (spis[i].nzp_serv == nzpServUni)
                        {
                            if (spis[i].nzp_serv == 14)
                            {
                                spis[i].c_calc = 0;
                            }
                            spis[i].nzp_serv = nzpServBase;

                            spis[i].service = service;
                        }
                    }
                }
                reader2.Close();
                connDB.Close();

                for (int i = 0; i <= spis.Count - 1; i++)
                {
                    Charge zap = spis[i];
                    for (int j = i + 1; j <= spis.Count - 1; j++)
                    {
                        if (zap.nzp_serv == spis[j].nzp_serv)
                        {
                            if (zap.nzp_serv != 25)
                                zap.tarif = zap.tarif + spis[j].tarif;
                            else
                                zap.tarif = Math.Max(zap.tarif, spis[j].tarif);

                            if (spis[j].rsum_tarif > 0)
                            {
                                zap.is_device = spis[j].is_device;
                                zap.nzp_frm = spis[j].nzp_frm;
                            }
                            zap.sum_real = zap.sum_real + spis[j].sum_real;
                            zap.reval = zap.reval + spis[j].reval;
                            zap.sum_tarif = zap.sum_tarif + spis[j].sum_tarif;
                            zap.rsum_tarif = zap.rsum_tarif + spis[j].rsum_tarif;
                            zap.sum_nedop = zap.sum_nedop + spis[j].sum_nedop;
                            zap.sum_lgota = zap.sum_lgota + spis[j].sum_lgota;
                            zap.sum_dlt_tarif_p = zap.sum_dlt_tarif_p + spis[j].sum_dlt_tarif_p;
                            zap.sum_tarif_sn_f = zap.sum_tarif_sn_f + spis[j].sum_tarif_sn_f;
                            zap.real_charge = zap.real_charge + spis[j].real_charge;
                            zap.sum_money = zap.sum_money + spis[j].sum_money;
                            zap.sum_insaldo = zap.sum_insaldo + spis[j].sum_insaldo;
                            zap.sum_outsaldo = zap.sum_outsaldo + spis[j].sum_outsaldo;
                            zap.sum_charge = zap.sum_charge + spis[j].sum_charge;
                            zap.c_calc = zap.c_calc + spis[j].c_calc;
                            spis[j].nzp_serv = -1;

                        }
                    }
                    spis[i] = zap;
                }

                int k = 0;
                while (k < spis.Count)
                {
                    if (spis[k].nzp_serv < 0)
                        spis.RemoveAt(k);
                    else
                        k++;
                }

                #endregion

                return spis;
            }
            catch (Exception ex)
            {
                
                reader2.Close();
                connWeb.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения начислений для счета " + err, MonitorLog.typelog.Error, 20, 201,
                    true);

                return null;
            }



        } //GetBill

        /// <summary> вытащить Начисления Для счет фактуры
        /// </summary>
        public List<Charge> GetNewBillCharge(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = new Returns { result = true, text = String.Empty };
            var spis = new List<Charge>();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }


            if (finder.YM.year_ <= 0) return spis;
            if (finder.YM.month_ <= 0) return spis;

            #region Подключение к БД и заполнение кэша или из основного источника

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection connDB = DBManager.GetConnection(connectionString);
            ret = DBManager.OpenDb(connDB, true);
            if (!ret.result)
            {
                connDB.Close();
                return null;
            }

            string curPref = finder.pref;
            string baseAlias = curPref + "_charge_" + (finder.YM.year_ - 2000).ToString("D2");
            string kernelAlias = curPref + "_kernel";
            string tableCharge = "charge2_" + finder.YM.month_.ToString("D2");

            

          

            // Проверка наличия базы данных
            // Проверка наличия таблицы
            if (!DBManager.TempTableInWebCashe(connDB, baseAlias + DBManager.tableDelimiter + tableCharge)) return null;



            #endregion





            MyDataReader reader2;
            var s = new StringBuilder();
            // выбрать данные из таблицы
            s.Remove(0, s.Length);
            s.Append(" Select service, a.nzp_serv, a.nzp_frm, a.tarif, a.is_device, sm.measure, s.ordering ");
            s.Append(", sum(a.reval) as reval ");
            s.Append(", sum(a.rsum_tarif) as rsum_tarif ");
            s.Append(", sum(a.sum_real) as sum_real ");
            s.Append(", sum(a.sum_tarif) as sum_tarif ");
            s.Append(", sum(a.sum_nedop) as sum_nedop ");
            s.Append(", sum(a.sum_lgota) as sum_lgota ");
            s.Append(", sum(a.sum_dlt_tarif_p) as sum_dlt_tarif_p ");
            //s.Append(", sum(a.sum_tarif_sn_f) as sum_tarif_sn_f ");
            s.Append(", sum(a.real_charge) as real_charge ");
            s.Append(", sum(a.sum_money) as sum_money ");
            s.Append(", sum(a.sum_insaldo) as sum_insaldo ");
            s.Append(", sum(a.sum_outsaldo) as sum_outsaldo ");
            s.Append(", sum(a.c_calc) as c_calc ");
            s.Append(", sum(a.c_reval) as c_reval ");
            s.Append(", sum(a.sum_charge) as sum_charge ");
            s.Append(", sum(a.c_okaz) as c_okaz ");
#if PG
            s.Append(" From " + baseAlias + "." + tableCharge + " a, " + kernelAlias + ".services s, " + kernelAlias + ".formuls f,");
            s.Append(kernelAlias + ".s_measure sm");
#else
            s.Append(" From " + baseAlias + ":" + tableCharge + " a, " + kernelAlias + ":services s, " + kernelAlias +
                     ":formuls f,");
            s.Append(kernelAlias + ":s_measure sm");
#endif
            s.Append(" where nzp_kvar = " + finder.nzp_kvar);
            s.Append(
                " and a.nzp_serv=s.nzp_serv and a.nzp_frm=f.nzp_frm and f.nzp_measure=sm.nzp_measure and a.nzp_serv>1 and dat_charge is null  ");
            s.Append(" group by service, a.nzp_serv, a.nzp_frm, a.tarif, a.is_device, sm.measure, s.ordering  ");
            s.Append(" order by ordering, service, a.nzp_serv, a.nzp_frm, a.tarif  ");



            if (!DBManager.ExecRead(connDB, out reader2, s.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + s, MonitorLog.typelog.Error, 20, 201, true);
                connDB.Close();
                return spis;
            }

            try
            {
                int id = 0;
                while (reader2.Read())
                {
                    var zap = new Charge { id = ++id, dat_charge = "" };

                    if (reader2["tarif"] != DBNull.Value) zap.tarif = Convert.ToDecimal(reader2["tarif"]);
                    if (reader2["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                    if (reader2["is_device"] != DBNull.Value) zap.is_device = Convert.ToInt32(reader2["is_device"]);
                    if (reader2["service"] != DBNull.Value) zap.service = Convert.ToString(reader2["service"]);
                    if (reader2["measure"] != DBNull.Value) zap.measure = Convert.ToString(reader2["measure"]);
                    if (reader2["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader2["nzp_frm"]);
                    if (reader2["sum_real"] != DBNull.Value) zap.sum_real = Convert.ToDecimal(reader2["sum_real"]);
                    if (reader2["reval"] != DBNull.Value) zap.reval = Convert.ToDecimal(reader2["reval"]);
                    if (reader2["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader2["sum_tarif"]);
                    if (reader2["rsum_tarif"] != DBNull.Value)
                        zap.rsum_tarif = Convert.ToDecimal(reader2["rsum_tarif"]);
                    if (reader2["sum_nedop"] != DBNull.Value) zap.sum_nedop = Convert.ToDecimal(reader2["sum_nedop"]);
                    if (reader2["sum_lgota"] != DBNull.Value) zap.sum_lgota = Convert.ToDecimal(reader2["sum_lgota"]);
                    if (reader2["sum_dlt_tarif_p"] != DBNull.Value)
                        zap.sum_dlt_tarif_p = Convert.ToDecimal(reader2["sum_dlt_tarif_p"]);
                    if (reader2["sum_dlt_tarif_p"] != DBNull.Value)
                        zap.sum_tarif_sn_f = Convert.ToDecimal(reader2["sum_dlt_tarif_p"]);
                    //if (reader2["sum_tarif_sn_f"] != DBNull.Value) zap.sum_tarif_sn_f = Convert.ToDecimal(reader2["sum_tarif_sn_f"]);
                    if (reader2["real_charge"] != DBNull.Value)
                        zap.real_charge = Convert.ToDecimal(reader2["real_charge"]);
                    if (reader2["sum_money"] != DBNull.Value) zap.sum_money = Convert.ToDecimal(reader2["sum_money"]);
                    if (reader2["sum_insaldo"] != DBNull.Value)
                        zap.sum_insaldo = Convert.ToDecimal(reader2["sum_insaldo"]);
                    if (reader2["sum_outsaldo"] != DBNull.Value)
                        zap.sum_outsaldo = Convert.ToDecimal(reader2["sum_outsaldo"]);
                    if (reader2["sum_charge"] != DBNull.Value)
                        zap.sum_charge = Convert.ToDecimal(reader2["sum_charge"]);
                    if (reader2["c_reval"] != DBNull.Value) zap.c_reval = Convert.ToDecimal(reader2["c_reval"]);
                    if (reader2["c_calc"] != DBNull.Value) zap.c_calc = Convert.ToDecimal(reader2["c_calc"]);
                    if (reader2["c_okaz"] != DBNull.Value) zap.c_okaz = Convert.ToDecimal(reader2["c_okaz"]);
                    spis.Add(zap);
                }

                reader2.Close();

                #region Объединение по service_uniona

                s.Remove(0, s.Length);
                s.Append(" Select service, s.nzp_serv_uni, s.nzp_serv_base ");
#if PG
                s.Append(" From " + finder.pref + "_kernel.service_union s," + finder.pref + "_kernel.services a");
#else
                s.Append(" From " + finder.pref + "_kernel:service_union s," + finder.pref + "_kernel:services a");
#endif
                s.Append(" where s.nzp_serv_base=a.nzp_serv");
                s.Append(" order by service, s.nzp_serv_base, s.nzp_serv_uni ");

                if (!DBManager.ExecRead(connDB, out reader2, s.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + s, MonitorLog.typelog.Error, 20, 201, true);
                    connDB.Close();
                    return spis;
                }

                int nzpServUni = 0;
                int nzpServBase = 0;
                string service = "";

                while (reader2.Read())
                {
                    if (reader2["nzp_serv_uni"] != DBNull.Value)
                        nzpServUni = Convert.ToInt32(reader2["nzp_serv_uni"]);
                    if (reader2["nzp_serv_base"] != DBNull.Value)
                        nzpServBase = Convert.ToInt32(reader2["nzp_serv_base"]);
                    if (reader2["service"] != DBNull.Value) service = Convert.ToString(reader2["service"]);

                    for (int i = 0; i <= spis.Count - 1; i++)
                    {
                        if (spis[i].nzp_serv == nzpServUni)
                        {
                            spis[i].nzp_serv = nzpServBase;
                            spis[i].service = service;
                        }
                    }
                }
                reader2.Close();
                connDB.Close();

                for (int i = 0; i <= spis.Count - 1; i++)
                {
                    Charge zap = spis[i];
                    for (int j = i + 1; j <= spis.Count - 1; j++)
                    {
                        if (zap.nzp_serv == spis[j].nzp_serv)
                        {
                            if (zap.nzp_serv != 25)
                                zap.tarif = zap.tarif + spis[j].tarif;
                            else
                                zap.tarif = Math.Max(zap.tarif, spis[j].tarif);

                            if (spis[j].rsum_tarif > 0)
                            {
                                zap.is_device = spis[j].is_device;
                                zap.nzp_frm = spis[j].nzp_frm;
                            }
                            zap.sum_real = zap.sum_real + spis[j].sum_real;
                            zap.reval = zap.reval + spis[j].reval;
                            zap.sum_tarif = zap.sum_tarif + spis[j].sum_tarif;
                            zap.rsum_tarif = zap.rsum_tarif + spis[j].rsum_tarif;
                            zap.sum_nedop = zap.sum_nedop + spis[j].sum_nedop;
                            zap.sum_lgota = zap.sum_lgota + spis[j].sum_lgota;
                            zap.sum_dlt_tarif_p = zap.sum_dlt_tarif_p + spis[j].sum_dlt_tarif_p;
                            zap.sum_tarif_sn_f = zap.sum_tarif_sn_f + spis[j].sum_tarif_sn_f;
                            zap.real_charge = zap.real_charge + spis[j].real_charge;
                            zap.sum_money = zap.sum_money + spis[j].sum_money;
                            zap.sum_insaldo = zap.sum_insaldo + spis[j].sum_insaldo;
                            zap.sum_outsaldo = zap.sum_outsaldo + spis[j].sum_outsaldo;
                            zap.sum_charge = zap.sum_charge + spis[j].sum_charge;
                            spis[j].nzp_serv = -1;

                        }
                    }
                    spis[i] = zap;
                }

                int k = 0;
                while (k < spis.Count)
                {
                    if (spis[k].nzp_serv < 0)
                        spis.RemoveAt(k);
                    else
                        k++;
                }

                #endregion

                connDB.Close();



                return spis;
            }
            catch (Exception ex)
            {
                
                reader2.Close();
                connDB.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения начислений для счета " + err, MonitorLog.typelog.Error, 20, 201,
                    true);

                return null;
            }



        } //GetNewBill
    }
}


