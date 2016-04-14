using System;
using System.Collections.Generic;
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
                    string strSqlQuery = String.Format("SELECT {0}_debt.lawsuit.number, {0}_debt.lawsuit.nzp_sector, {0}_debt.lawsuit.lawsuit_price, {0}_debt.lawsuit.lawsuit_date, {0}_debt.lawsuit.presenter, {0}_debt.lawsuit.nzp_lawsuit_status, {0}_debt.lawsuit.comment, {0}_debt.lawsuit.tax FROM {0}_debt.lawsuit WHERE {0}_debt.lawsuit.nzp_lawsuit = {1} LIMIT 1;", Points.Pref, nzp_lawsuit);
                    conn.Open();
                    IDataReader reader;
                    ExecRead(conn, out reader, strSqlQuery, true);

                    int number = 0;
                    int nzp_sector = 0;
                    decimal lawsuit_price = 0;
                    DateTime lawsuit_date = new DateTime();
                    string presenter = "";
                    int nzp_lawsuit_status = 0;
                    string comment = "";
                    decimal tax = 0;

                    while (reader.Read())
                    {
                        if (reader["number"] != DBNull.Value) number = Convert.ToInt32(reader["number"]);
                        if (reader["nzp_sector"] != DBNull.Value) nzp_sector = Convert.ToInt32(reader["nzp_sector"]);
                        if (reader["lawsuit_price"] != DBNull.Value) lawsuit_price = Convert.ToDecimal(reader["lawsuit_price"]);
                        if (reader["lawsuit_date"] != DBNull.Value) lawsuit_date = Convert.ToDateTime(reader["lawsuit_date"]);
                        if (reader["presenter"] != DBNull.Value) presenter = reader["presenter"].ToString();
                        if (reader["nzp_lawsuit_status"] != DBNull.Value) nzp_lawsuit_status = Convert.ToInt32(reader["nzp_lawsuit_status"]);
                        if (reader["comment"] != DBNull.Value) comment = reader["comment"].ToString();
                        if (reader["tax"] != DBNull.Value) tax = Convert.ToDecimal(reader["tax"]);
                    }

                    Data.nzp_lawsuit = nzp_lawsuit;
                    Data.number = number;
                    Data.nzp_sector = nzp_sector;
                    Data.lawsuit_price = lawsuit_price;
                    Data.lawsuit_date = lawsuit_date;
                    Data.presenter = presenter;
                    Data.nzp_lawsuit_status = nzp_lawsuit_status;
                    Data.comment = comment;
                    Data.tax = tax;
                }

                if (nzp_lawsuit == 0 && Data.nzp_deal > 0)
                {
                    string strSqlQuery = String.Format("SELECT MAX({0}_debt.lawsuit.number) AS new_lawsuit_num FROM {0}_debt.lawsuit WHERE {0}_debt.lawsuit.nzp_deal = {1};", Points.Pref, Data.nzp_deal);
                    conn.Open();
                    IDataReader reader;
                    ret = ExecRead(conn, out reader, strSqlQuery, true);
                    int new_lawsuit_num = 0;
                    while (reader.Read()) if (reader["new_lawsuit_num"] != DBNull.Value) new_lawsuit_num = Convert.ToInt32(reader["new_lawsuit_num"]);
                    Data.number = ++new_lawsuit_num;
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
                conn.Open();
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
                conn.Open();
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
                conn.Open();
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
                conn.Open();
                string strSqlQuery;
                if (Data.nzp_lawsuit == 0)
                {
                    IDataReader reader;
                    int nzp_lawsuit = 0;
                    strSqlQuery = String.Format("INSERT INTO {0}_debt.lawsuit (number, nzp_sector, lawsuit_price, lawsuit_date, presenter, nzp_lawsuit_status, comment, tax, nzp_deal) VALUES ({1}, {2}, {3}, '{4}', '{5}', {6}, {7}, {8}, {9}) RETURNING nzp_lawsuit;", Points.Pref, Data.number, Data.nzp_sector, Data.lawsuit_price, Data.lawsuit_date.ToShortDateString(), Data.presenter, Data.nzp_lawsuit_status, Utils.EStrNull(Data.comment), Data.tax, Data.nzp_deal);
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
                    strSqlQuery = String.Format("UPDATE {0}_debt.lawsuit SET number = {1}, nzp_sector = {2}, lawsuit_price = {3}, lawsuit_date = '{4}', presenter = '{5}', nzp_lawsuit_status = {6}, comment = {7}, tax = {8} WHERE {0}_debt.lawsuit.nzp_lawsuit = {9};", Points.Pref, Data.number, Data.nzp_sector, Data.lawsuit_price, Data.lawsuit_date.ToShortDateString(), Data.presenter, Data.nzp_lawsuit_status, Utils.EStrNull(Data.comment), Data.tax, Data.nzp_lawsuit);
                    ret = ExecSQL(conn, strSqlQuery, true);
                }
            }            
            catch(Exception ex)
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
            conn.Open();
            string strSqlQuery = String.Format("DELETE FROM {0}_debt.lawsuit WHERE {0}_debt.lawsuit.nzp_lawsuit = {1};", Points.Pref, nzp_lawsuit);
            ret = ExecSQL(conn, strSqlQuery.ToString(), true);
            conn.Close();
        }

        public Returns  GetDDLstData(out lawsuit_Files lstPreCourt, out lawsuit_Files lstCourt)
        {
            Returns ret = Utils.InitReturns();
            lstPreCourt = new lawsuit_Files();
            lstCourt = new lawsuit_Files();

            try
            {
                IDbConnection conn = GetConnection(Constants.cons_Kernel);
                IDataReader reader;
                conn.Open();
                //string strSqlQuery = String.Format("SELECT nzp_oper, name, nzp_oper_group FROM {0}_debt.s_opers WHERE nzp_oper_group = 1 OR nzp_oper_group = 2;", Points.Pref);
                string strSqlQuery = String.Format("SELECT so.nzp_oper, so.name, so.nzp_oper_group FROM {0}_debt.s_opers so INNER JOIN {0}_debt.opers_in_group oig on oig.nzp_oper = so.nzp_oper WHERE so.nzp_oper_group = 1 OR so.nzp_oper_group = 2;", Points.Pref);
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
            conn.Open();
            string strSqlQuery = String.Format("SELECT debt_money FROM {0}_debt.deal WHERE nzp_deal = {1};", Points.Pref, nzp_deal);
            ret = ExecRead(conn, out reader, strSqlQuery, true);

            decimal debt_money = 0;
            while (reader.Read())
                if (reader["debt_money"] != DBNull.Value) debt_money = Decimal.Parse(reader["debt_money"].ToString());

            return debt_money;
        }
    }
}
