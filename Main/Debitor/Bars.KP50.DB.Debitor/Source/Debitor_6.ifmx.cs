using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    public partial class Debitor : DataBaseHead
    {
        public void GetLawsuitData(int nzp_lawsuit, ref lawsuit_Data Data, out Returns ret)
        {
            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            ret = Utils.InitReturns();
            try
            {
                if (nzp_lawsuit > 0)
                {
                    string strSqlQuery =
                        String.Format(
                            "SELECT * FROM {0}_debt.lawsuit WHERE {0}_debt.lawsuit.nzp_lawsuit = {1} LIMIT 1;",
                            Points.Pref, nzp_lawsuit);
                    conn.Open();
                    IDataReader reader;
                    ExecRead(conn, out reader, strSqlQuery, true);
                    while (reader.Read())
                    {
                        Data.nzp_lawsuit = nzp_lawsuit;
                        Data.number = CastValue<string>(reader["number"]).Trim();
                        Data.nzp_sector = CastValue<int>(reader["nzp_sector"]);
                        Data.lawsuit_price = CastValue<decimal>(reader["lawsuit_price"]);
                        Data.lawsuit_date = CastValue<DateTime>(reader["lawsuit_date"]);
                        Data.presenter = CastValue<string>(reader["presenter"]).Trim();
                        Data.nzp_lawsuit_status = CastValue<int>(reader["nzp_lawsuit_status"]);
                        Data.comment = CastValue<string>(reader["comment"]).Trim(); ;
                        Data.tax = CastValue<decimal>(reader["tax"]);
                        Data.lawsuit_price_peni = CastValue<decimal>(reader["lawsuit_price_peni"]);
                        Data.lawsuit_from = CastValue<DateTime>(reader["lawsuit_from"]);
                        Data.lawsuit_to = CastValue<DateTime>(reader["lawsuit_to"]);
                        Data.decide_number = CastValue<string>(reader["decide_number"]);
                        Data.dn_lawsuit_price = CastValue<decimal>(reader["dn_lawsuit_price"]);
                        Data.dn_tax = CastValue<decimal>(reader["dn_tax"]);
                        Data.dn_date = CastValue<DateTime>(reader["dn_date"]);
                        Data.dn_lawsuit_price_peni = CastValue<decimal>(reader["dn_lawsuit_price_peni"]);
                        Data.il_number = CastValue<string>(reader["il_number"]);
                        Data.il_where_directed = CastValue<string>(reader["il_where_directed"]);
                        Data.il_date = CastValue<DateTime>(reader["il_date"]);
                        Data.is_executed = CastValue<int>(reader["is_executed"]);
                    }
                }
                if (nzp_lawsuit == 0 && Data.nzp_deal > 0)
                {
                    string strSqlQuery = String.Format("SELECT MAX({0}_debt.lawsuit.number) AS new_lawsuit_num,MAX({0}_debt.lawsuit.decide_number) AS decide_number,MAX({0}_debt.lawsuit.il_number) AS il_number" +
                                                       " FROM {0}_debt.lawsuit WHERE {0}_debt.lawsuit.nzp_deal = {1};", Points.Pref, Data.nzp_deal);
                    if (conn.State != ConnectionState.Open) conn.Open();
                    IDataReader reader;
                    ret = ExecRead(conn, out reader, strSqlQuery, true);
                    int new_lawsuit_num = 0;
                    while (reader.Read())
                    {
                        if (reader["new_lawsuit_num"] != DBNull.Value)
                        {
                            if (reader["new_lawsuit_num"] is Int32)
                                new_lawsuit_num = Convert.ToInt32(reader["new_lawsuit_num"]);
                            else
                                new_lawsuit_num = 1;
                        }
                        Data.il_number = (CastValue<int>(reader["il_number"]) + 1).ToString();
                        Data.decide_number = (CastValue<int>(reader["decide_number"]) + 1).ToString();
                    }
                    Data.number = (++new_lawsuit_num).ToString(CultureInfo.InvariantCulture);
                }

                GetLawsuitStatuses(ref Data, out ret);
                if (!ret.result) return;

                GetLawsuitSectors(ref Data, out ret);
                if (!ret.result) return;

                //GetLawsuitFiles(nzp_lawsuit, ref Data, out ret);
                //if (!ret.result) return;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = String.Format("Ошибка в функции GetLawsuitData: \n{0}", ex.Message);
                MonitorLog.WriteLog(String.Format("Ошибка в функции GetLawsuitData: \n{0}", ex.Message), MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn.Close();
            }
        }

        private void GetLawsuitStatuses(ref lawsuit_Data Data, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                IDbConnection conn = GetConnection(Constants.cons_Kernel);
                if (conn.State != ConnectionState.Open) conn.Open();
                string strSqlQuery = String.Format("SELECT {0}_debt.s_lawsuit_statuses.nzp_lawsuit_status, {0}_debt.s_lawsuit_statuses.name_lawsuit_status FROM {0}_debt.s_lawsuit_statuses;", Points.Pref);
                IDataReader reader;
                ExecRead(conn, out reader, strSqlQuery, true);
                int nzp_lawsuit_status = 0;
                string name_lawsuit_status = "";
                while (reader.Read())
                {
                    if (reader["nzp_lawsuit_status"] != DBNull.Value) nzp_lawsuit_status = Convert.ToInt32(reader["nzp_lawsuit_status"]);
                    if (reader["name_lawsuit_status"] != DBNull.Value) name_lawsuit_status = reader["name_lawsuit_status"].ToString();
                    if (nzp_lawsuit_status != 0 && name_lawsuit_status != "")
                    {
                        Data.Statuses.Add(nzp_lawsuit_status, name_lawsuit_status);
                        nzp_lawsuit_status = 0;
                        name_lawsuit_status = "";
                    }
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, false);
            }
        }

        private void GetLawsuitSectors(ref lawsuit_Data Data, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                IDbConnection conn = GetConnection(Constants.cons_Kernel);
                if (conn.State != ConnectionState.Open) conn.Open();
                string strSqlQuery = String.Format("SELECT {0}_debt.s_jud_sector.nzp_sector, {0}_debt.s_jud_sector.name_sector FROM {0}_debt.s_jud_sector;", Points.Pref);
                IDataReader reader;
                ExecRead(conn, out reader, strSqlQuery, true);
                int nzp_sector = 0;
                string name_sector = "";
                while (reader.Read())
                {
                    if (reader["nzp_sector"] != DBNull.Value) nzp_sector = Convert.ToInt32(reader["nzp_sector"]);
                    if (reader["name_sector"] != DBNull.Value) name_sector = reader["name_sector"].ToString();
                    if (nzp_sector != 0 && name_sector != "")
                    {
                        Data.Sectors.Add(nzp_sector, name_sector);
                        nzp_sector = 0;
                        name_sector = "";
                    }
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, false);
            }
        }

        private void GetLawsuitFiles(int nzp_lawsuit, ref lawsuit_Data Data, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                IDbConnection conn = GetConnection(Constants.cons_Kernel);
                if (conn.State != ConnectionState.Open) conn.Open();
                string strSqlQuery = String.Format("SELECT {0}_debt.lawsuit_reports.file_name, {0}_debt.lawsuit_reports.file_path FROM {0}_debt.lawsuit_reports WHERE {0}_debt.lawsuit_reports.lawsuit_reports = {1};", Points.Pref, nzp_lawsuit);
                IDataReader reader;
                ExecRead(conn, out reader, strSqlQuery, true);
                string strName = "";
                string strPath = "";
                while (reader.Read())
                {
                    if (reader["file_name"] != DBNull.Value) strName = reader["file_name"].ToString();
                    if (reader["file_path"] != DBNull.Value) strPath = reader["file_path"].ToString();
                    if (strPath != "" && strName != "")
                    {
                        Data.Files.Add(strName, strPath);
                        strName = "";
                        strPath = "";
                    }
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, false);
            }
        }

        public void SetLawsuitData(lawsuit_Data Data, out Returns ret)
        {
            Utils.setCulture();
            ret = Utils.InitReturns();
            IDbConnection conn = GetConnection(Constants.cons_Kernel);

            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                string strSqlQuery = string.Format("select count(*) from {0}lawsuit " +
                      "where nzp_deal = {3} and ((lawsuit_from <= '{1}' and lawsuit_to >= '{1}') or " +
                      "(lawsuit_from <= '{2}' and lawsuit_to >= '{2}'))" +
                      " and nzp_lawsuit <> {4}", Points.Pref + "_debt.", Data.lawsuit_from, Data.lawsuit_to, Data.nzp_deal, Data.nzp_lawsuit);

                var period_count = DBManager.ExecScalar<int>(conn, strSqlQuery);
                
                if (period_count > 0)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = String.Format("За указанный период иск уже подан");
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Warn, true);
                    return;
                }
                if (Data.nzp_lawsuit == 0)
                {
                    IDataReader reader;
                    int nzp_lawsuit = 0;
                    strSqlQuery = String.Format("INSERT INTO {0}_debt.lawsuit (number, nzp_sector, lawsuit_price, lawsuit_date, presenter, nzp_lawsuit_status, comment, tax, nzp_deal,lawsuit_price_peni," +
                        "lawsuit_from,lawsuit_to,decide_number,dn_lawsuit_price,dn_tax,dn_date,dn_lawsuit_price_peni,il_number,il_where_directed,is_executed,il_date) " +
                        "VALUES ({1}, {2}, {3}, '{4}', '{5}', {6}, {7}, {8}, {9}, {10}, '{11}', '{12}', '{13}', {14}, {15}, '{16}', {17}, '{18}', '{19}', {20}, '{21}') RETURNING nzp_lawsuit;",
                        Points.Pref, Data.number, Data.nzp_sector, Data.lawsuit_price, Data.lawsuit_date.ToShortDateString(), Data.presenter,
                        Data.nzp_lawsuit_status, Utils.EStrNull(Data.comment), Data.tax, Data.nzp_deal, Data.lawsuit_price_peni, Data.lawsuit_from, Data.lawsuit_to,
                        Data.decide_number, Data.dn_lawsuit_price, Data.dn_tax, Data.dn_date, Data.dn_lawsuit_price_peni, Data.il_number, Data.il_where_directed, Data.is_executed, Data.il_date);
                    ret = ExecRead(conn, out reader, strSqlQuery, true);
                    if (ret.result)
                        while (reader.Read())
                            if (reader["nzp_lawsuit"] != DBNull.Value) nzp_lawsuit = Convert.ToInt32(reader["nzp_lawsuit"]);

                    //смена статуса у дела
                    this.MakeOperOnDeal(new deal_states_history() { nzp_deal = Data.nzp_deal, nzp_oper = EnumOpers.GiveLawSuit.GetHashCode() }, conn, null, out ret);
                    if (!ret.result) return;
                    ret.tag = nzp_lawsuit;
                }
                else
                {
                    strSqlQuery = String.Format("UPDATE {0}_debt.lawsuit SET number = {1}, nzp_sector = {2}," +
                        " lawsuit_price = {3}, lawsuit_date = '{4}', presenter = '{5}', nzp_lawsuit_status = {6}, comment = {7}, tax = {8}, " +
                        " lawsuit_price_peni = {10}, lawsuit_from = '{11}', lawsuit_to = '{12}', decide_number = '{13}', dn_lawsuit_price = {14}, dn_tax = {15}, " +
                        " dn_date = '{16}', dn_lawsuit_price_peni = {17}, il_number = '{18}', il_where_directed = '{19}', is_executed = {20}, il_date = '{21}'" +
                        "WHERE {0}_debt.lawsuit.nzp_lawsuit = {9};", Points.Pref, Data.number, Data.nzp_sector, Data.lawsuit_price, Data.lawsuit_date.ToShortDateString(),
                        Data.presenter, Data.nzp_lawsuit_status, Utils.EStrNull(Data.comment), Data.tax, Data.nzp_lawsuit, Data.lawsuit_price_peni, Data.lawsuit_from, Data.lawsuit_to,
                        Data.decide_number, Data.dn_lawsuit_price, Data.dn_tax, Data.dn_date, Data.dn_lawsuit_price_peni, Data.il_number, Data.il_where_directed, Data.is_executed, Data.il_date);
                        ret = ExecSQL(conn, strSqlQuery, true);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = String.Format("Ошибка выполнения операции в функции SetLawsuitData:\n{0}", ex.Message);
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn.Close();
            }
        }

        public void DeleteLawsuitData(int nzp_lawsuit, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            if (conn.State != ConnectionState.Open) conn.Open();
            string strSqlQuery = String.Format("DELETE FROM {0}_debt.lawsuit WHERE {0}_debt.lawsuit.nzp_lawsuit = {1};", Points.Pref, nzp_lawsuit);
            ret = ExecSQL(conn, strSqlQuery.ToString(), true);
            conn.Close();
        }

        public Returns GetDDLstData(out lawsuit_Files lstPreCourt, out lawsuit_Files lstCourt)
        {
            Returns ret = Utils.InitReturns();
            lstPreCourt = new lawsuit_Files();
            lstCourt = new lawsuit_Files();

            try
            {
                IDbConnection conn = GetConnection(Constants.cons_Kernel);
                IDataReader reader;
                if (conn.State != ConnectionState.Open) conn.Open();
                //string strSqlQuery = String.Format("SELECT nzp_oper, name, nzp_oper_group FROM {0}_debt.s_opers WHERE nzp_oper_group = 1 OR nzp_oper_group = 2;", Points.Pref);
                //
                string strSqlQuery = String.Format("SELECT so.nzp_oper, so.name, so.nzp_oper_group FROM {0}_debt.s_opers so left outer JOIN {0}_debt.opers_in_group oig on oig.nzp_oper = so.nzp_oper WHERE (so.nzp_oper_group = 1 OR so.nzp_oper_group = 2) and so.nzp_oper in (4,5,6);", Points.Pref);
                ret = ExecRead(conn, out reader, strSqlQuery, true);

                int nzp_oper = 0;
                string name = "";
                int nzp_oper_group = 0;

                while (reader.Read())
                {
                    if (reader["nzp_oper"] != DBNull.Value) nzp_oper = Convert.ToInt32(reader["nzp_oper"]);
                    if (reader["name"] != DBNull.Value) name = reader["name"].ToString();
                    if (reader["nzp_oper_group"] != DBNull.Value) nzp_oper_group = Convert.ToInt32(reader["nzp_oper_group"]);

                    if (nzp_oper != 0 && name != "" && nzp_oper_group != 0)
                    {
                        switch (nzp_oper_group)
                        {
                            case 1:
                                lstPreCourt.Add(name, nzp_oper.ToString());
                                break;
                            case 2:
                                lstCourt.Add(name, nzp_oper.ToString());
                                break;
                        }
                        nzp_oper = 0;
                        name = "";
                        nzp_oper_group = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDDLstData: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return ret;
        }

        public decimal GetLawsuitPrice(int nzp_deal, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            IDataReader reader;
            if (conn.State != ConnectionState.Open) conn.Open();
            string strSqlQuery = String.Format("SELECT debt_money FROM {0}_debt.deal WHERE nzp_deal = {1};", Points.Pref, nzp_deal);
            ret = ExecRead(conn, out reader, strSqlQuery, true);

            decimal debt_money = 0;
            while (reader.Read())
                if (reader["debt_money"] != DBNull.Value) debt_money = Decimal.Parse(reader["debt_money"].ToString());

            return debt_money;
        }
    }
}
