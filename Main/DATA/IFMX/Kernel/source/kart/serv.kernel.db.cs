using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections.Generic;

namespace STCLINE.KP50.DataBase
{
    public class DbServKernel : DbLsServicesClient
    {
        public List<int> GetDependenciesServicesList(IDbConnection connection, IDbTransaction transaction, int NzpMasterService, int NzpArea, out Returns ret)
        {
            List<int> lstServices = new List<int>();
            ret = new Returns();

            IDataReader reader = null;

            try
            {
                bool boolByNzpArea = true;

                for (int i = 0; i < 2; i++)
                {
                    DateTime dtCalcMonth = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);

                    SqlBuilder sql = new SqlBuilder();
                    SqlBuilder strSqlQuery = new SqlBuilder();
                    strSqlQuery.Append(" SELECT dep.nzp_serv_slave ");
                    strSqlQuery.Append(" FROM " + Points.Pref + "_data" + tableDelimiter + "dep_servs dep ");
                    strSqlQuery.Append(" WHERE dep.is_actual = 1 ");
                    strSqlQuery.Append(" AND dep.nzp_serv = " + NzpMasterService);
                    strSqlQuery.Append(" AND " + sNvlWord + "(dep.dat_s,  " + MDY(1, 1, 1900) + ") <= " + Utils.EStrNull(dtCalcMonth.ToShortDateString()));
                    strSqlQuery.Append(" AND " + sNvlWord + "(dep.dat_po, " + MDY(1, 1, 3000) + " ) >= " + Utils.EStrNull(dtCalcMonth.ToShortDateString()));

                    if (boolByNzpArea) strSqlQuery.Append(" AND dep.nzp_area = " + NzpArea);

                    ret = ExecRead(connection, transaction, out reader, strSqlQuery.ToString(), true);
                    if (!ret.result) throw new Exception(ret.text);

                    while (reader.Read())
                    {
                        if (Convert.ToString(reader["nzp_serv_slave"]).Trim() != "") lstServices.Add(Convert.ToInt32(reader["nzp_serv_slave"]));
                    }

                    if (lstServices.Count > 0) break;
                    else boolByNzpArea = false;
                }
            }
            catch (Exception ex) { throw new Exception(ex.Message, ex); }

            return lstServices;
        }
    }
}
