using System;
using System.Collections.Generic;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.DataBase
{
    public partial class DbParameters : DataBaseHead
    {
        public List<Prm> FindPrmTarif(Prm finder, out Returns ret)
        {
            List<Prm> list = null;
            ret = new Returns(true);
            
            using (ClassGetCalcPrm getPrm = new ClassGetCalcPrm())
            {
                if (Utils.GetParams(finder.prms, Constants.act_groupby_service.ToString()))
                {
                    list = getPrm.GetServiceCalcPrm(finder, out ret);
                }
                else
                {
                    list = getPrm.GetCalcPrm(finder, out ret);
                }
            }
           
            return list;
        }
    }
    
    /// <summary>
    /// Класс получения параметров расчета
    /// </summary>
    public class ClassGetCalcPrm: DataBaseHead
    {
        private IDbConnection conn_db = null;
        private IDataReader reader = null;
        private string temp_calc_prm = "";
        
        /// <summary>
        /// параметры расчета
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Prm> GetCalcPrm(Prm finder, out Returns ret)
        {
            ret = CheckFinder(finder);
            if (!ret.result)
            {
                return null;
            }

            List<Prm> list = null;
            
            try
            {
                string connectionString = Points.GetConnByPref(finder.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);
                list = GetCalcPrm(finder, out ret.tag);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при загрузке услуг параметров расчета";
                MonitorLog.WriteLog("Ошибка FindPrmTarif " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }

                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return list;
        }

        /// <summary>
        /// услуги параметров расчета
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Prm> GetServiceCalcPrm(Prm finder, out Returns ret)
        {
            ret = CheckFinder(finder);
            if (!ret.result)
            {
                return null;
            }

            List<Prm> list = null;

            try
            {
                string connectionString = Points.GetConnByPref(finder.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);
                list = GetCalcPrmServices(finder);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при загрузке услуг параметров расчета";
                MonitorLog.WriteLog("Ошибка FindPrmTarif " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }

                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return list;
        }

        /// <summary>
        /// Проверка входных параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns CheckFinder(Prm finder)
        {
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Не определен пользователь");
            }

            if (!Utils.GetParams(finder.prms, Constants.act_groupby_service.ToString()))
            {
                if (finder.pref == "")
                {
                    return new Returns(false, "Не определен префикс базы данных");
                }
                if (finder.year_ == 0 || finder.month_ == 0)
                {
                    return new Returns(false, "Не задан расчетный месяц");
                }
            }

            return new Returns(true);
        }

        /// <summary>
        /// Формировать условие 
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private string MakeWhere(Prm finder)
        {
            string where = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) where += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) where += " and t.nzp_prm in (" + role.val + ")";
                }

            if (finder.nzp_serv > 0) where += " and t.nzp_serv = " + finder.nzp_serv;
            if (finder.nzp_prm > 0) where += " and t.nzp_prm = " + finder.nzp_prm;

            return where;
        }

        /// <summary>
        /// Получить услуги параметров расчета
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private List<Prm> GetCalcPrmServices(Prm finder)
        {
            string sql = "Select t.nzp_serv, s.service " +
                    " From " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "prm_name p, " +
                    Points.Pref + "_kernel" + DBManager.tableDelimiter + "prm_tarifs t," +
                    Points.Pref + "_kernel" + DBManager.tableDelimiter + "services s " +
                    " Where t.nzp_prm = p.nzp_prm  and t.nzp_serv = s.nzp_serv " +
                    MakeWhere(finder) +
                    " group by 1,2 " +
#if PG
                    " order by s.service";
#else            
                    " Order by s.ordering";
#endif
            Returns ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) throw new Exception(ret.text);

            List<Prm> list = new List<Prm>();
            while (reader.Read())
            {
                Prm zap = new Prm();
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                list.Add(zap);
            }

            return list;
        }

        /// <summary>
        /// Получить параметры расчета со значениями
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="totalRecordCount"></param>
        /// <returns></returns>
        private List<Prm> GetCalcPrm(Prm finder, out int totalRecordCount)
        {
            Returns ret = new Returns(true);
            totalRecordCount = 0;
            temp_calc_prm = "temp_calc_prm_" + finder.nzp_user;

            string sql = "drop table " + temp_calc_prm;
            ExecSQL(conn_db, sql, false);

            sql = "create temp table " + temp_calc_prm + "(" +
                " nzp_prm  integer, " +
                " nzp_res  integer, " +
                " name_prm varchar(200), " +
                " measure  varchar(100), " +
                " type_prm varchar(20), " +
                " val_prm  varchar(255) "  + ")";
            ExecSQLWE(conn_db, sql, true);

            sql = "insert into " + temp_calc_prm + " (nzp_prm, nzp_res, name_prm, measure, type_prm) " +
                 " Select distinct p.nzp_prm, p.nzp_res, p.name_prm, m.measure, p.type_prm " +
                 " From " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "prm_name p " +
                 "   left outer join " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "prm_tarifs t on t.nzp_prm = p.nzp_prm " +
                 "   left outer join " + finder.pref + "_kernel" + DBManager.tableDelimiter + "formuls f    on t.nzp_frm = f.nzp_frm " +
                 "   left outer join " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_measure m  on f.nzp_measure = m.nzp_measure " +
                 " Where p.prm_num = 5 " + MakeWhere(finder);
            ExecSQLWE(conn_db, sql, true);

            // значения параметров
            sql = "Update " + temp_calc_prm + " t Set " +
                " val_prm = (Select max(p.val_prm) " +
                    " From " + finder.pref + "_data" + DBManager.tableDelimiter + "prm_5 p" +
                    " Where p.nzp_prm = t.nzp_prm "+
                        " and p.is_actual <> 100" +
                        " and p.dat_s  <= " + DBManager.MDY(finder.month_, 28, finder.year_) +
                        " and p.dat_po >= " + DBManager.MDY(finder.month_, 1, finder.year_) + ")";
            ExecSQLWE(conn_db, sql, true);
            
            if (!Utils.GetParams(finder.prms, Constants.act_showallprm))
            {
                sql = "delete from " + temp_calc_prm + " where " + DBManager.sNvlWord + "(val_prm, '') = '' ";
                ExecSQLWE(conn_db, sql, true);
            }

            // значения из справочника
            sql = "Update " + temp_calc_prm + " t Set " +
                " val_prm = (Select max(y.name_y) " +
                    " From " + finder.pref + "_kernel" + DBManager.tableDelimiter + "res_y y" +
                    " Where y.nzp_res = t.nzp_res and t.val_prm = y.nzp_y" + DBManager.sConvToChar + ") " +
                " where lower(trim(type_prm)) = 'sprav'";
            ExecSQLWE(conn_db, sql, true);

            // булевские значения
            sql = "Update " + temp_calc_prm + " t Set val_prm = (case when trim(val_prm) = '1' then 'Да' else 'Нет' end) " +
                  " where lower(trim(type_prm)) = 'bool' ";
            ExecSQLWE(conn_db, sql, true);

            //sql = "Update " + temp_calc_prm + " t Set val_prm = 'Нет' where lower(trim(type_prm)) = 'bool' and trim(val_prm) <> '1' ";
            //ExecSQLWE(conn_db, sql, true);
            
            sql = "select * from " + temp_calc_prm + " order by name_prm";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) throw new Exception(ret.text);

            List<Prm> list = new List<Prm>();

            totalRecordCount = 0;
            while (reader.Read())
            {
                totalRecordCount++;

                if (finder.rows > 0 && totalRecordCount <= finder.skip + finder.rows && totalRecordCount > finder.skip)
                {
                    Prm zap = new Prm();
                    if (reader["nzp_prm"] != DBNull.Value) zap.nzp_prm = Convert.ToInt32(reader["nzp_prm"]);
                    if (reader["name_prm"] != DBNull.Value) zap.name_prm = Convert.ToString(reader["name_prm"]).Trim();
                    if (reader["measure"] != DBNull.Value) zap.measure = Convert.ToString(reader["measure"]).Trim();
                    if (reader["val_prm"] != DBNull.Value) zap.val_prm = Convert.ToString(reader["val_prm"]).Trim();
                    zap.prm_num = 5;
                    zap.pref = finder.pref;
                    zap.num = totalRecordCount.ToString();
                    list.Add(zap);
                }
           }

           return list;
        }
    }
}
