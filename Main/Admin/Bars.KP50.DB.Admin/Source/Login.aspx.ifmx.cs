using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbUser : DbUserClient
    {
        public ReturnsObjectType<BaseUser> GetUserOrganization(BaseUser finder)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return new ReturnsObjectType<BaseUser>(null, ret.result, ret.text, ret.tag);
            }

            ReturnsObjectType<BaseUser> result = GetUserOrganization(finder, conn_db, null);
            conn_db.Close();

            return result;
        }

        public ReturnsType FillUserOrganization(List<User> finder)
        {
            if (finder == null) return new ReturnsType(false, "Неверные входные параметры");

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return new ReturnsType(ret.result, ret.text, ret.tag);
            }

            ReturnsObjectType<BaseUser> result;
            foreach(BaseUser item in finder)
            {
                result = GetUserOrganization(item, conn_db, null);

                if (!result.result)
                {
                    conn_db.Close();
                    return result;
                }
                else if (result.returnsData != null)
                {
                    item.payer = result.returnsData.payer;
                    item.nzp_supp = result.returnsData.nzp_supp;
                    item.nzp_area = result.returnsData.nzp_area;
                }
            }

            conn_db.Close();

            return new ReturnsType(true);
        }
        

        /// <summary>
        /// Получить краткую информацию об организации пользователя
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public ReturnsObjectType<BaseUser> GetUserOrganization(BaseUser finder, IDbConnection connection, IDbTransaction transaction)
        {
#if PG
            string sql = "select p.npayer, p.nzp_supp, a.nzp_area" +
                " from " + Points.Pref + "_kernel.s_payer p" +
                    " left outer join " + Points.Pref + "_data.s_area a on p.nzp_supp = a.nzp_supp " +
                " where p.nzp_payer = " + finder.nzp_payer;
#else
            string sql = "select p.npayer, p.nzp_supp, a.nzp_area" +
                " from " + Points.Pref + "_kernel@" + DBManager.getServer(connection) + ":s_payer p" +
                    " , outer " + Points.Pref + "_data@" + DBManager.getServer(connection) + ":s_area a" +
                " where p.nzp_payer = " + finder.nzp_payer +
                    " and p.nzp_supp = a.nzp_supp";
#endif
            IDataReader reader;
            Returns ret = ExecRead(connection, transaction, out reader, sql, true);

            ReturnsObjectType<BaseUser> result = new ReturnsObjectType<BaseUser>(null, ret.result, ret.text, ret.tag);
            
            if (result.result)
            {
                if (reader.Read())
                {
                    BaseUser user = new BaseUser();
                    if (reader["npayer"] != DBNull.Value) user.payer = Convert.ToString(reader["npayer"]).Trim();
                    if (reader["nzp_supp"] != DBNull.Value) user.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["nzp_area"] != DBNull.Value) user.nzp_area = Convert.ToInt32(reader["nzp_area"]);

                    result.returnsData = user;
                }
                reader.Close();
            }
            return result;
        }
    }
}
