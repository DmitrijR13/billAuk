using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
     [Migration(2015100306401, Migrator.Framework.DataBase.Web)]
    public class Migration_2015100306401:Migration
    {
        public override void Apply()
        {
            droptables(" like('%_spls')");
            droptables(" like('%_splsdom')");
        }

        private void droptables(string spls)
        {
            string sql = "select table_name " +
                        "from information_schema.tables  " +
                        "where table_schema='public'  and table_name like('t%')  and table_name " + spls;
            IDataReader reader = Database.ExecuteReader(sql);
            try
            {
                while (reader.Read())
                {
                    try
                    {
                        if (reader["table_name"] == DBNull.Value) continue;
                        string dropsql = "drop table public" + Database.TableDelimiter + reader["table_name"].ToString().Trim();
                        Database.ExecuteNonQuery(dropsql);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            finally
            {
                if (reader!=null) reader.Close();
            }
        }
    }
}
