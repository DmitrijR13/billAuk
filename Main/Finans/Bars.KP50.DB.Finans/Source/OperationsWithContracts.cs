using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.DB.Sprav.Source;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Finans.Source
{
    class OperationsWithContracts : DbPackClient
    {
        // Извлекает информацию по договору ЖКУ
        public SupplierInfo GetSupplierInfo(SupplierInfo finder, out Returns ret)
        {
            SupplierInfo si = new SupplierInfo();
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return si;
            }
            #region соединение
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return si; ;
            # endregion

            string where = ";";
            if (finder.nzp_supp > 0)
            {
                where = " and s.nzp_supp=" + finder.nzp_supp + ";";
            }
            string sql = "Select distinct s.nzp_supp, s.name_supp, s.nzp_payer_agent, s.fn_dogovor_bank_lnk_id, a.payer as agent," +
                           "s.nzp_payer_agent, a.payer as agent, s.nzp_payer_princip, pr.payer as principal, " +
                           "s.nzp_payer_supp, su.payer as supplier, s.nzp_payer_podr, p.payer as podr,s.nzp_scope " +
                           "From " + Points.Pref + sKernelAliasRest + "supplier s " +
                           "left outer join " + Points.Pref + sKernelAliasRest + "s_payer a on s.nzp_payer_agent = a.nzp_payer " +
                           "left outer join  " + Points.Pref + sKernelAliasRest + "s_payer  pr on s.nzp_payer_princip=pr.nzp_payer " +
                           "left outer join  " + Points.Pref + sKernelAliasRest + "s_payer  su on s.nzp_payer_supp=su.nzp_payer " +
                           "left outer join  " + Points.Pref + sKernelAliasRest + "s_payer  p on s.nzp_payer_podr=p.nzp_payer " +
                           " where s.nzp_supp > 0 " + where;

            sql += "select d.nzp_fd, d.num_dog, d.dat_dog, ba.rcount, bd.payer as payer, pbn.payer as payer_bank, bd.kpp, bd.bik, bd.ks, a.payer as agent, pr.payer as princip, d.nzp_scope " +
                   "from " + Points.Pref + sKernelAliasRest + "supplier s " +
                   "left outer join " + Points.Pref + sDataAliasRest + "fn_dogovor_bank_lnk b on s.fn_dogovor_bank_lnk_id=b.id " +
                   "left outer join " + Points.Pref + sDataAliasRest + "fn_dogovor d on b.nzp_fd=d.nzp_fd " +
                   "left outer join " + Points.Pref + sKernelAliasRest + "s_payer a on d.nzp_payer_agent = a.nzp_payer " +
                   "left outer join " + Points.Pref + sKernelAliasRest + "s_payer  pr on d.nzp_payer_princip=pr.nzp_payer " +
                   "left outer join  " + Points.Pref + sDataAliasRest + "fn_bank  ba on ba.nzp_fb=b.nzp_fb " +
                   "left outer join  " + Points.Pref + sKernelAliasRest + "s_payer  bd on bd.nzp_payer=ba.nzp_payer " +
                   "left outer join  " + Points.Pref + sKernelAliasRest + "s_payer  pbn on pbn.nzp_payer=ba.nzp_payer_bank " +
                   "where s.nzp_supp > 0 " + where;
            IDataReader readerMain;
            ret = ExecRead(conn_db, out readerMain, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return si;
            }
            try
            {
                while (readerMain.Read())
                {
                    if (readerMain["nzp_supp"] != DBNull.Value) si.nzp_supp = Convert.ToInt32(readerMain["nzp_supp"]);
                    if (readerMain["name_supp"] != DBNull.Value) si.name_supp = (readerMain["name_supp"]).ToString();
                    if (readerMain["nzp_payer_agent"] != DBNull.Value) si.nzp_payer_agent = Convert.ToInt32(readerMain["nzp_payer_agent"]);
                    if (readerMain["agent"] != DBNull.Value) si.agent = (readerMain["agent"]).ToString();
                    if (readerMain["nzp_payer_princip"] != DBNull.Value) si.nzp_payer_princip = Convert.ToInt32(readerMain["nzp_payer_princip"]);
                    if (readerMain["principal"] != DBNull.Value) si.principal = (readerMain["principal"]).ToString();
                    if (readerMain["nzp_payer_supp"] != DBNull.Value) si.nzp_payer_supp = Convert.ToInt32(readerMain["nzp_payer_supp"]);
                    if (readerMain["supplier"] != DBNull.Value) si.supplier = (readerMain["supplier"]).ToString();
                    if (readerMain["nzp_payer_podr"] != DBNull.Value) si.nzp_payer_podr = Convert.ToInt32(readerMain["nzp_payer_podr"]);
                    if (readerMain["podr"] != DBNull.Value) si.podr = (readerMain["podr"]).ToString();
                    if (readerMain["nzp_scope"] != DBNull.Value) si.nzp_scope = Convert.ToInt32(readerMain["nzp_scope"]);
                    if (readerMain["fn_dogovor_bank_lnk_id"] != DBNull.Value) si.fn_dogovor_bank_lnk_id = Convert.ToInt32(readerMain["fn_dogovor_bank_lnk_id"]);
                    break;
                }
                readerMain.NextResult();
                while (readerMain.Read())
                {
                    if (readerMain["nzp_fd"] != DBNull.Value) si.nzp_fd = Convert.ToInt32(readerMain["nzp_fd"]);
                    if (readerMain["num_dog"] != DBNull.Value) si.num_dog = (readerMain["num_dog"]).ToString();
                    if (readerMain["agent"] != DBNull.Value) si.agent_fd = Convert.ToString(readerMain["agent"]);
                    if (readerMain["princip"] != DBNull.Value) si.principal_fd = Convert.ToString(readerMain["princip"]);
                    if (readerMain["dat_dog"] != DBNull.Value) si.dat_dog = Convert.ToString(readerMain["dat_dog"]);
                    if (readerMain["payer"] != DBNull.Value) si.payer = (readerMain["payer"]).ToString();
                    if (readerMain["kpp"] != DBNull.Value) si.kpp = readerMain["kpp"].ToString();
                    if (readerMain["bik"] != DBNull.Value) si.bik = readerMain["bik"].ToString();
                    if (readerMain["ks"] != DBNull.Value) si.ks = readerMain["ks"].ToString();
                    if (readerMain["payer_bank"] != DBNull.Value) si.payer_bank = (readerMain["payer_bank"]).ToString();
                    if (readerMain["rcount"] != DBNull.Value) si.rcount = (readerMain["rcount"]).ToString();
                    if (readerMain["nzp_scope"] != DBNull.Value) si.parent_nzp_scope = Convert.ToInt32(readerMain["nzp_scope"]);
                    if (si.parent_nzp_scope <= 0) break;
                    sql = "select distinct nzp_wp from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope=" + si.parent_nzp_scope;
                    IDataReader readerNzpWp;
                    ret = ExecRead(conn_db, out readerNzpWp, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return si;
                    }
                    while (readerNzpWp.Read())
                    {
                        if (readerNzpWp["nzp_wp"] != DBNull.Value) si.list_nzp_wp.Add(Convert.ToInt32(readerNzpWp["nzp_wp"]));
                    }
                    readerNzpWp.Close();
                    break;
                }
                readerMain.Close();
                conn_db.Close();
                return si;
            }
            catch (Exception ex)
            {
                readerMain.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return si;
            }


        }

        public Returns UpdateSupplierScope(SupplierInfo finder)
        {
            Returns ret;
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Не задан пользователь", -1);
            }
            #region соединение
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret; ;
            # endregion
            int new_nzp_scope = finder.parent_nzp_scope;
            if (finder.nzp_scope == finder.parent_nzp_scope)
            {
                DbScopeAddress db = new DbScopeAddress();
                ScopeAdress sa = new ScopeAdress();
                sa.nzp_user = finder.nzp_user;
                sa.parent_nzp_scope = finder.parent_nzp_scope;
                ret = db.CopyParentScopes(sa);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                new_nzp_scope = ret.tag;
            }
            string sql = "update " + Points.Pref + sKernelAliasRest + "supplier set nzp_scope=" + new_nzp_scope + " where nzp_supp=" + finder.nzp_supp;
            ret = ExecSQL(conn_db, sql, true);
            conn_db.Close();
            ret.tag = new_nzp_scope;
            return ret;
        }

        public List<int> GetDogovorERCChildsScope(SupplierInfo finder, out Returns ret)
        {
            List<int> list= new List<int>();
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return list;
            }
            #region соединение
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list; 
            # endregion

            string sql =
                "select distinct s.nzp_scope from " + Points.Pref + sKernelAliasRest + "supplier s, " +
                Points.Pref + sDataAliasRest + "fn_dogovor_bank_lnk l, " +
                Points.Pref + sDataAliasRest + "fn_dogovor d " +
                "where s.fn_dogovor_bank_lnk_id=l.id and l.nzp_fd="+finder.nzp_fd;

            IDataReader readerMain;
            ret = ExecRead(conn_db, out readerMain, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return list;
            }
            try
            {
                while (readerMain.Read())
                {
                    if (readerMain["nzp_scope"] != DBNull.Value) list.Add(Convert.ToInt32(readerMain["nzp_scope"]));
                }
                readerMain.Close();
                conn_db.Close();
                return list;
            }
            catch (Exception ex)
            {
                readerMain.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return list;
            }
        }
    }
}
