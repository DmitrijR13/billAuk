using System.Globalization;
using Bars.KP50.Faktura.Source.Base;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;
    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;

    public class TagilFaktura : TulaFaktura
    {
        protected DataTable _table;
        private const int FAKTURA_CODE = 125;
        private const int MAX_SUPPLIER_COUNT = 15;

        public TagilFaktura() : base()
        {
            FakturaBlocks.HasCountersBlock = false;
            FakturaBlocks.HasCountersDoubleBlock = true;
        }
        
        public override string GetBarCode()
        {
            string vars = "EZPB" + Pkod;
            vars = vars + (Math.Max(0, SumTicket) * 100).ToString("00");
            return vars;
        }

        protected void FillAdditionalParams(DataRow dr)
        {            
            string strSymb = "'";
            IDbConnection conDb = DBManager.GetConnection(Constants.cons_Kernel);
            DBManager.OpenDb(conDb, false);
            string sql_add_params = "select * from faktura.faktura_add_params ("
                                   + FAKTURA_CODE.ToString() + ", "
                                   + strSymb + Year.ToString() + '-' + Month.ToString() + "-01', "
                                   + LicSchet.ToString() + ", "
                                   + strSymb + Pref + strSymb + ", "
                                   + strSymb + Points.Pref + strSymb + ", "
                                   + NzpArea.ToString()
                                   + ")";

            IDbCommand cmd_add_params = DBManager.newDbCommand(sql_add_params, conDb);
            IDataReader reader = null;
            try
            {
                reader = cmd_add_params.ExecuteReader();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (!_table.Columns.Contains(reader.GetName(i)))
                        {
                            _table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                        }
                        dr[reader.GetName(i)] = reader.GetValue(i);
                    }
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog(
                    "Счет-квитанция :  Ошибка запроса " + sql_add_params + " для лс  " + LicSchet + "\r\n" + e.Message,
                    MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd_add_params != null)
                {
                    cmd_add_params.Dispose();
                }
                conDb.Close();
            }

            if (DomSquare > 0 && DomSquare - MopSquare > 0)
            {
                dr["pl_dom_gil"] = DomSquare - MopSquare;
            }
            else
            {
                dr["pl_dom_gil"] = 0;
            }
        }

        protected override bool FillSummuryBill(DataRow dr)
        {            
            dr["charge_summary"] = (SummaryServ.Serv.RsumTarif + SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString();
            dr["sum_outsaldo"] = SummaryServ.Serv.SumOutsaldo.ToString("0.00");
            var peni = ListServ.Find(s => s.Serv.NzpServ == 500);
            if (peni != null)
            {
                dr["peni_payment"] = peni.Serv.SumMoney.ToString("0.00");
            }
            
            return base.FillSummuryBill(dr);
        }

         private void FillSupplierGrid(DataRow dr)
        {
            FillAdditionalParams(dr);
             
             int index = 1;
            string SuppList = "";

            foreach (var service in ListSupp)
            {
                if (!IsShowServInGrid(service)) continue;
                SuppList += service.Serv.NzpSupp + ",";
            }

            string strSymb = "'";
            string sql = "";
            sql = "select * from faktura.fn_Supplier_requisits ("
                                   + FAKTURA_CODE.ToString() + ", "
                                   + "'01." + Month.ToString("00") + "." + Year + "', "
                                   + LicSchet.ToString() + ", "
                                   + strSymb + Pref + strSymb + ", "
                                   + strSymb + Points.Pref + strSymb + ", "
                                   + strSymb + SuppList.TrimEnd(',') + strSymb
                                   + ")";

            IDbConnection conDb = DBManager.GetConnection(Constants.cons_Kernel);
            DBManager.OpenDb(conDb, false);
            IDataReader reader = null;
            try
            {
                DBManager.ExecRead(conDb, out reader, sql, true);
                while (reader.Read())
                {
                    dr["supp_name" + index] = reader["name"] != DBNull.Value ? reader["name"].ToString().Trim() : "";
                    dr["supp_address" + index] = reader["address"] != DBNull.Value ? reader["address"].ToString().Trim() : "";
                    dr["supp_inn" + index] = reader["inn"] != DBNull.Value ? reader["inn"].ToString().Trim() : "";
                    dr["supp_kpp" + index] = reader["kpp"] != DBNull.Value ? reader["kpp"].ToString().Trim() : "";
                    dr["supp_rs" + index] = reader["rs"] != DBNull.Value ? reader["rs"].ToString().Trim() : "";
                    dr["supp_bank_name" + index] = reader["bank_name"] != DBNull.Value ? reader["bank_name"].ToString().Trim() : "";
                    dr["supp_bank_ks" + index] = reader["bank_ks"] != DBNull.Value ? reader["bank_ks"].ToString().Trim() : "";
                    dr["supp_bank_bik" + index] = reader["bank_bik"] != DBNull.Value ? reader["bank_bik"].ToString().Trim() : "";
                    index++;
                    if (index > MAX_SUPPLIER_COUNT)
                        break;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки реквизитов поставщиков /n " + sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conDb.Close();
            }
        }

        protected override bool FillMainChargeGrid(DataRow dr)
         {
             if (dr == null) return false;
             FillSupplierGrid(dr);
             if (GvsNormGkal == 0) GvsNormGkal = 0.0611m;
             SetServRashod();
             ListServ.Sort();

             decimal sumTarifAll = 0;
             decimal reval = 0;
             decimal sumDolg = 0;
             decimal sumCharge = 0;
             decimal tarif = 0;
             string measure = "";

             int numberString = 1;

             decimal CCalcODN = 0;
            // decimal CCalc = 0;
             foreach (BaseServ t in ListServ)
             {                
                 if (IsShowServInGrid(t))
                 {
                     dr["name_serv" + numberString] = t.Serv.NameServ.Trim();
                     dr["measure" + numberString] = t.Serv.Measure.Trim();

                     if (Math.Abs(t.ServOdn.CCalc) > 0.001m)
                     {
                         if (t.Serv.NzpServ == 7)
                             dr["c_calc_odn" + numberString] = "x";
                         else if (t.Serv.NzpServ == 9)
                         {
                             if (t.ServOdn.RsumTarif != 0)
                             {
                                 CCalcODN = Math.Round((t.ServOdn.Tarif != 0 ? (t.ServOdn.RsumTarif / t.ServOdn.Tarif) : t.ServOdn.CCalc), 4);
                                 dr["c_calc_odn" + numberString] = CCalcODN.ToString("0.0000");
                             }
                         }
                         else    
                             dr["c_calc_odn" + numberString] = t.ServOdn.CCalc.ToString("0.0000");
                     }

                     if ((Math.Abs(t.Serv.CCalc) > 0.001m) &
                         (t.Serv.IsOdn == false))
                     {
                         if ((t.Serv.RsumTarif ==
                              t.ServOdn.RsumTarif) & (t.Serv.RsumTarif > 0.001m))
                         {
                         }
                         else
                         {
                             if (Math.Abs(t.Serv.RsumTarif) > 0.001m)
                             {
                                 if (t.Serv.NzpServ == 8)
                                     dr["c_calc" + numberString] =
                                         Math.Round((t.Serv.Tarif != 0 ? (t.Serv.RsumTarif / t.Serv.Tarif) : t.Serv.CCalc), 4)
                                             .ToString("0.0000");
                                 else if (t.Serv.NzpServ == 9)
                                     dr["c_calc" + numberString] =
                                         Math.Round((t.Serv.Tarif != 0 ? (t.Serv.RsumTarif / t.Serv.Tarif) - CCalcODN : t.Serv.CCalc), 4)
                                             .ToString("0.0000");
                                 else    
                                     dr["c_calc" + numberString] = t.Serv.CCalc.ToString("0.0000");
                             }
                         }
                     }


                     if (Math.Abs(t.Serv.Tarif) > 0.001m)
                     {
                         dr["tarif" + numberString] = t.Serv.Tarif.ToString("0.00");
                     }

                     if (((t.Serv.NzpServ == 6) ||
                          (t.Serv.NzpServ == 7)) & (t.Serv.NzpMeasure != 3))
                     {
                         if (t.Serv.Norma > 0.001m)
                         {
                             dr["tarif" + numberString] = (t.Serv.Tarif /
                                                                      t.Serv.Norma).ToString("0.00");

                         }
                         dr["measure" + numberString] = "Куб.м.";
                     }

                     if (Math.Abs(t.Serv.RsumTarif - t.ServOdn.RsumTarif) > 0.001m)
                     {
                         dr["rsum_tarif" + numberString] = (t.Serv.RsumTarif -
                                                                       t.ServOdn.RsumTarif).ToString("0.00");
                     }

                     if (Math.Abs(t.ServOdn.RsumTarif) > 0.001m)
                     {
                         dr["rsum_tarif_odn" + numberString] = t.ServOdn.RsumTarif.ToString("0.00");
                     }
                     if (Math.Abs(t.Serv.RsumTarif) > 0.001m)
                     {
                         dr["rsum_tarif_all" + numberString] = t.Serv.RsumTarif.ToString("0.00");
                     }

                     if (Math.Abs(t.Serv.Reval +
                                         t.Serv.RealCharge) > 0.001m)
                     {
                         dr["reval" + numberString] = (t.Serv.Reval +
                                                                  t.Serv.RealCharge).ToString("0.00");
                     }

                     //dr["reval13"] = 56;

                     dr["sum_lgota" + numberString] = "";

                     if (Math.Abs(t.Serv.SumCharge) > 0.001m)
                     {
                         dr["sum_charge_all" + numberString] = t.Serv.SumCharge.ToString("0.00");
                     }


                     if (Math.Abs(t.Serv.SumInsaldo -
                                         t.Serv.SumMoney) > 0.001m)
                     {
                         dr["sum_dolg" + numberString] =
                             (t.Serv.SumInsaldo -
                              t.Serv.SumMoney).ToString("0.00");
                     }

                     if (Math.Abs(t.Serv.SumCharge -
                                         t.ServOdn.SumCharge) > 0.001m)
                     {
                         dr["sum_charge" + numberString] = (t.Serv.SumCharge -
                                                                       t.ServOdn.SumCharge).ToString("0.00");
                     }
                     if (Math.Abs(t.ServOdn.SumCharge) > 0.001m)
                     {
                         dr["sum_charge_odn" + numberString] = t.ServOdn.SumCharge.ToString("0.00");
                     }

                     dr["calc_and_reval" + numberString] = t.Serv.RsumTarif + t.Serv.Reval + t.Serv.RealCharge;

                     base.FillServiceVolume(dr, numberString, t.Serv.NzpServ);

                     numberString++;
                 }
             }
             for (int i = numberString; i < 19; i++)
             {
                 dr["name_serv" + i] = "";
                 dr["measure" + i] = "";
                 dr["c_calc" + i] = "";
                 dr["c_calc_odn" + i] = "";
                 dr["tarif" + i] = "";
                 dr["rsum_tarif" + i] = "";
                 dr["rsum_tarif_odn" + i] = "";
                 dr["rsum_tarif_all" + i] = "";
                 dr["reval" + i] = "";
                 dr["sum_lgota" + i] = "";
                 dr["sum_charge_all" + i] = "";
                 dr["sum_charge" + i] = "";
                 dr["sum_charge_odn" + i] = "";
                 dr["sum_nedop" + i] = "";
                 dr["sum_sn" + i] = "";
                 dr["sum_outsaldo" + i] = "";
                 dr["real_charge" + i] = "";
             }
             return true;
         }
        
        private void AddTableColumn(string aColName) 
        {
            if (_table.Columns.IndexOf(aColName) == -1)
                _table.Columns.Add(aColName, typeof(string));
        }

        public override DataTable MakeTable() 
        {
            int MaxRevalNumber = 10;
            int MaxLsCountersCount = 9;
            int MaxHouseCountersCount = 9;
            int MaxSupplierCount = 15;
            int MaxServCount = 40;
            
            _table = base.MakeTable();

            for (int i = 1; i < MaxServCount; i++)
            {
                AddTableColumn("name_serv" + i);
                AddTableColumn("measure" + i);
                AddTableColumn("c_calc" + i);
                AddTableColumn("c_calc_odn" + i);
                AddTableColumn("tarif" + i);
                AddTableColumn("rsum_tarif" + i);
                AddTableColumn("rsum_tarif_odn" + i);
                AddTableColumn("rsum_tarif_all" + i);
                AddTableColumn("reval" + i);
                AddTableColumn("revalnull" + i);
                AddTableColumn("sum_lgota" + i);
                AddTableColumn("sum_dolg" + i);
                AddTableColumn("sum_charge_all" + i);
                AddTableColumn("sum_charge" + i);
                AddTableColumn("sum_charge_odn" + i);
                AddTableColumn("sum_nedop" + i);
                AddTableColumn("sum_sn" + i);
                AddTableColumn("sum_outsaldo" + i);
                AddTableColumn("real_charge" + i);
                AddTableColumn("rash_name" + i);
                AddTableColumn("rash_norm" + i);
                AddTableColumn("rash_norm_odn" + i);
                AddTableColumn("rash_pu" + i);
                AddTableColumn("rash_pu_odn" + i);
                AddTableColumn("rash_dpu" + i);
                AddTableColumn("rash_dpu_pu" + i);
                AddTableColumn("rash_dpu_odn" + i);
                AddTableColumn("calc_and_reval" + i); //начисления плюс перерасчеты
            }

            for (int i = 1; i < MaxSupplierCount; i++)
            {
                AddTableColumn("supp_name" + i);
                AddTableColumn("supp_address" + i);
                AddTableColumn("supp_inn" + i);
                AddTableColumn("supp_kpp" + i);
                AddTableColumn("supp_rs" + i);
                AddTableColumn("supp_bank_name" + i);
                AddTableColumn("supp_bank_ks" + i);
                AddTableColumn("supp_bank_bik" + i);
            }

            for (int i = 1; i < MaxRevalNumber; i++)
            {
                AddTableColumn("serv_pere" + i);
                AddTableColumn("period_pere" + i);
                AddTableColumn("osn_pere" + i);
                AddTableColumn("sum_pere" + i);
            }

            for (int i = 1; i <= MaxHouseCountersCount; i++)
            {
                AddTableColumn("hcounter_service_name" + i);
                AddTableColumn("hcounter_device_num" + i);
                AddTableColumn("hcounter_date_start" + i);
                AddTableColumn("hcounter_date_end" + i);
                AddTableColumn("hcounter_reading_start" + i);
                AddTableColumn("hcounter_reading_end" + i);
                AddTableColumn("hcounter_consumpt_all" + i);
                AddTableColumn("hcounter_consumpt_charge" + i);
            }

            for (int i = 1; i <= MaxLsCountersCount; i++)
            {
                AddTableColumn("lscounter_service_name" + i);
                AddTableColumn("lscounter_device_num" + i);
                AddTableColumn("lscounter_date_start" + i);
                AddTableColumn("lscounter_date_end" + i);
                AddTableColumn("lscounter_reading_start" + i);
                AddTableColumn("lscounter_reading_end" + i);
                AddTableColumn("lscounter_consumpt_all" + i);
            }

            AddTableColumn("charge_summary");
            AddTableColumn("pl_dom_gil");
            AddTableColumn("sum_outsaldo");
            AddTableColumn("peni_payment");
            //S общ.имущ. [Q_master.pl_mop] м2; S. жил. [Q_master.pl_dom_gil] м2
            //[Q_master.sum_outsaldo]
            //[Q_master.peni_payment]

            return _table;
        }

        public override bool FillDomCounters(DataRow dr)
        {
            int countersIndex = 1;

            string strSymb = "'";
            string sql = "";
            sql = "select * from faktura.fn_House_counter_readings ("
                                   + LicSchet.ToString() + ", "
                                   + "'01." + Month.ToString("00") + "." + Year + "', "
                                   + LicSchet.ToString() + ", "
                                   + strSymb + Pref + strSymb + ", "
                                   + strSymb + Points.Pref + strSymb + ", "
                                   + BaseDom.ToString()
                                   + ")";

            IDataReader reader = null;
            IDbConnection conDb = DBManager.GetConnection(Constants.cons_Kernel);
            IDbCommand cmd = DBManager.newDbCommand(sql, conDb);
            try
            {
                int MaxHouseCountersCount = 8;
                DBManager.OpenDb(conDb, false);
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dr["hcounter_service_name" + countersIndex] = reader["service_name"] != DBNull.Value ? reader["service_name"].ToString().Trim() : "";
                    dr["hcounter_device_num" + countersIndex] = reader["counter_num"] != DBNull.Value ? reader["counter_num"].ToString().Trim() : "";
                    dr["hcounter_date_start" + countersIndex] = reader["date_start"] != DBNull.Value ? Convert.ToDateTime(reader["date_start"]).ToShortDateString() : "";
                    dr["hcounter_date_end" + countersIndex] = reader["date_end"] != DBNull.Value ? Convert.ToDateTime(reader["date_end"]).ToShortDateString() : "";
                    dr["hcounter_reading_start" + countersIndex] = reader["reading_start"] != DBNull.Value ? reader["reading_start"].ToString().Trim() : "";
                    dr["hcounter_reading_end" + countersIndex] = reader["reading_end"] != DBNull.Value ? reader["reading_end"].ToString().Trim() : "";
                    dr["hcounter_consumpt_all" + countersIndex] = reader["consumpt_all"] != DBNull.Value ? reader["consumpt_all"].ToString().Trim() : "";
                    dr["hcounter_consumpt_charge" + countersIndex] = reader["consumpt_charge"] != DBNull.Value ? reader["consumpt_charge"].ToString().Trim() : "";
                    countersIndex++;
                    if (countersIndex > MaxHouseCountersCount)
                        break;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки счетчиков /n " + sql +
                    " /n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                conDb.Close();
            }
            return true;
        }

        public override bool FillCounters(DataRow dr)
        {
            int countersIndex = 1;
            int MaxLsCountersCount = 10;
            for (int i = 0; i < Math.Min(8, ListCounters.Count); i++)
            {
                dr["lscounter_service_name" + countersIndex] = countersIndex + ". " + ListCounters[i].ServiceName;
                dr["lscounter_date_start" + countersIndex] = ListCounters[i].DatUchetPred.ToShortDateString();
                dr["lscounter_reading_start" + countersIndex] = ListCounters[i].ValuePred.ToString("0.00");
                dr["lscounter_date_end" + countersIndex] = ListCounters[i].DatUchet.ToShortDateString();
                dr["lscounter_reading_end" + countersIndex] = ListCounters[i].Value.ToString("0.00");
                dr["lscounter_consumpt_all" + countersIndex] = (ListCounters[i].Value - ListCounters[i].ValuePred).ToString("0.00");
                countersIndex++;
                if (countersIndex > MaxLsCountersCount)
                    break;
            }
            return true;
        }
    }
}
