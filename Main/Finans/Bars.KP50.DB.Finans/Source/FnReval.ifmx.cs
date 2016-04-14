using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.DataBase
{
    public partial class DbFnReval : DataBaseHead
    {
        public ReturnsObjectType<DataTable> GetFnRevalList(FnRevalFinder finder, IDbConnection connectionID)
        {
            if (!(finder.nzp_user > 0)) return new ReturnsObjectType<DataTable>("Не задан пользователь");
            if (finder.dat_oper == DateTime.MinValue) return new ReturnsObjectType<DataTable>("Не задана дата операции");
            if (finder.dat_oper_po == DateTime.MinValue) finder.dat_oper_po = finder.dat_oper;

            DataTable table = null;
            string sqlText = "";

            // Ограничения пользователя
            string sqlRoleFilter = "";
            string strKeys = "";
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        strKeys = role.val.Trim(new char[] { ' ', ',' });
                        if (role.kod == Constants.role_sql_area)
                            sqlRoleFilter += " and r.nzp_area in (" + strKeys + ") and r2.nzp_area in (" + strKeys + ") ";
                        else if (role.kod == Constants.role_sql_serv)
                            sqlRoleFilter += " and r.nzp_serv in (" + strKeys + ") and r2.nzp_serv in (" + strKeys + ") ";
                        else if (role.kod == Constants.role_sql_payer)
                            sqlRoleFilter += " and r.nzp_payer in (" + strKeys + ") and r2.nzp_payer in (" + strKeys + ") ";
                    }
                }
            }

            // Создать временную таблицу
            sqlText = " drop table alx_tmp_fn_reval ";
            ClassDBUtils.ExecSQL(sqlText, connectionID, ClassDBUtils.ExecMode.Log);
#if PG
            sqlText =
                          " create temp table alx_tmp_fn_reval ( " +
                          "   nzp_reval INTEGER, nzp_area INTEGER, nzp_serv INTEGER, nzp_payer INTEGER, " +
                          "   area character(40), service character(100), payer character(200), " +
                          "   nzp_reval_2 INTEGER, nzp_area_2 INTEGER, nzp_serv_2 INTEGER, nzp_payer_2 INTEGER, " +
                          "   area_2 character(40), service_2 character(100), payer_2 character(200), " +
                          "   dat_oper DATE, sum_reval NUMERIC(14,2), comment character(60), nzp_user INTEGER, user_ character(100), dat_when DATE " +
                          " )  ";
#else
  sqlText =
                " create temp table alx_tmp_fn_reval ( " +
                "   nzp_reval INTEGER, nzp_area INTEGER, nzp_serv INTEGER, nzp_payer INTEGER, " +
                "   area VARCHAR(40), service VARCHAR(100), payer VARCHAR(200), " +
                "   nzp_reval_2 INTEGER, nzp_area_2 INTEGER, nzp_serv_2 INTEGER, nzp_payer_2 INTEGER, " +
                "   area_2 VARCHAR(40), service_2 VARCHAR(100), payer_2 VARCHAR(200), " +
                "   dat_oper DATE, sum_reval DECIMAL(14,2), comment VARCHAR(60), nzp_user INTEGER, user_ VARCHAR(100), dat_when DATE " +
                " ) with no log ";
#endif
            ClassDBUtils.ExecSQL(sqlText, connectionID);

            // Поиск по базам xxx_fin_YY (по годам из интервала дат dat_oper - dat_oper_po)
            string tableFnReval;
#if PG
            sqlText =
                      " select yearr from " + Points.Pref + "_kernel.s_baselist " +
                      " where idtype = 4 and yearr >= " + finder.dat_oper.ToString("yyyy") + " and yearr <= " + finder.dat_oper_po.ToString("yyyy") +
                      " order by yearr ";
#else
      sqlText =
                " select yearr from " + Points.Pref + "_kernel:s_baselist " +
                " where idtype = 4 and yearr >= " + finder.dat_oper.ToString("yyyy") + " and yearr <= " + finder.dat_oper_po.ToString("yyyy") +
                " order by yearr ";
#endif
            foreach (DataRow row in ClassDBUtils.OpenSQL(sqlText, connectionID).GetData().Rows)
            {
#if PG
                tableFnReval = Points.Pref + "_fin_" + row["yearr"].ToString().Substring(2, 2) + ".fn_reval";
                sqlText =
                    " INSERT INTO alx_tmp_fn_reval ( " +
                    "  nzp_reval, nzp_area, nzp_serv, nzp_payer, area, service, payer, " +
                    "  nzp_reval_2, nzp_area_2, nzp_serv_2, nzp_payer_2, area_2, service_2, payer_2, " +
                    "  dat_oper, sum_reval, comment, nzp_user, user_, dat_when ) " +
                    " SELECT r.nzp_reval, r.nzp_area, r.nzp_serv, r.nzp_payer, " +
                    "   trim(a.area) as area, trim(s.service) as service, trim(p.payer) as payer, " +
                    "   r2.nzp_reval_2, r2.nzp_area as nzp_area_2, r2.nzp_serv as nzp_serv_2, r2.nzp_payer as nzp_payer_2, " +
                    "   trim(a2.area) as area_2, trim(s2.service) as service_2, trim(p2.payer) as payer_2, " +
                    "   r.dat_oper, r.sum_reval, r.comment, r.nzp_user, '' as user_, r.dat_when " +
                    " FROM " + tableFnReval + " r " +
                    " left outer join " + Points.Pref + "_data.s_area a on r.nzp_area=a.nzp_area " +
                    " left outer join " + Points.Pref + "_kernel.services s on r.nzp_serv=s.nzp_serv " +
                    " left outer join " + Points.Pref + "_kernel.s_payer p on r.nzp_payer=p.nzp_payer, " +
                    tableFnReval + " r2 " +
                    " left outer join " + Points.Pref + "_data.s_area a2 on r2.nzp_area=a2.nzp_area " +
                    " left outer join " + Points.Pref + "_kernel.services s2 on r2.nzp_serv=s2.nzp_serv " +
                    " left outer join " + Points.Pref + "_kernel.s_payer p2 on r2.nzp_payer=p2.nzp_payer " +
                    " WHERE r.nzp_reval_2=r2.nzp_reval " +
                    "   and r.sum_reval > 0 " +
                    ((finder.nzp_reval > 0) ? " and r.nzp_reval = " + finder.nzp_reval : "") +
                    ((finder.dat_oper_po == finder.dat_oper)
                    ? " and r.dat_oper = public.mdy(" + finder.dat_oper.ToString("MM,dd,yyyy") + ") "
                    : " and r.dat_oper >= public.mdy(" + finder.dat_oper.ToString("MM,dd,yyyy") + ") " +
                    " and r.dat_oper <= public.mdy(" + finder.dat_oper_po.ToString("MM,dd,yyyy") + ") "
                    ) +
                    ((finder.nzp_area > 0) ? " and (r.nzp_area = " + finder.nzp_area + " or r2.nzp_area = " + finder.nzp_area + ") " : "") +
                    ((finder.nzp_serv > 0) ? " and (r.nzp_serv = " + finder.nzp_serv + " or r2.nzp_serv = " + finder.nzp_serv + ")" : "") +
                    ((finder.nzp_payer > 0) ? " and (r.nzp_payer = " + finder.nzp_payer + " or r2.nzp_payer = " + finder.nzp_payer + ")" : "") +
                    // ограничения пользователя
                    sqlRoleFilter;
#else
tableFnReval = Points.Pref + "_fin_" + row["yearr"].ToString().Substring(2, 2) + ":fn_reval";
                sqlText =
                    " INSERT INTO alx_tmp_fn_reval ( " +
                    "  nzp_reval, nzp_area, nzp_serv, nzp_payer, area, service, payer, " +
                    "  nzp_reval_2, nzp_area_2, nzp_serv_2, nzp_payer_2, area_2, service_2, payer_2, " +
                    "  dat_oper, sum_reval, comment, nzp_user, user_, dat_when ) " +
                    " SELECT r.nzp_reval, r.nzp_area, r.nzp_serv, r.nzp_payer, " +
                    "   trim(a.area) as area, trim(s.service) as service, trim(p.payer) as payer, " +
                    "   r2.nzp_reval_2, r2.nzp_area as nzp_area_2, r2.nzp_serv as nzp_serv_2, r2.nzp_payer as nzp_payer_2, " +
                    "   trim(a2.area) as area_2, trim(s2.service) as service_2, trim(p2.payer) as payer_2, " +
                    "   r.dat_oper, r.sum_reval, r.comment, r.nzp_user, '' as user_, r.dat_when " +
                    " FROM " + tableFnReval + " r, " + tableFnReval + " r2, " +
                    "   outer " + Points.Pref + "_data:s_area a, outer " + Points.Pref + "_kernel:services s, outer " + Points.Pref + "_kernel:s_payer p, " +
                    "   outer " + Points.Pref + "_data:s_area a2, outer " + Points.Pref + "_kernel:services s2, outer " + Points.Pref + "_kernel:s_payer p2 " +
                    " WHERE r.nzp_reval_2=r2.nzp_reval " +
                    "   and r.nzp_area=a.nzp_area and r.nzp_serv=s.nzp_serv and r.nzp_payer=p.nzp_payer " +
                    "   and r2.nzp_area=a2.nzp_area and r2.nzp_serv=s2.nzp_serv and r2.nzp_payer=p2.nzp_payer " +
                    "   and r.sum_reval > 0 " +
                    ((finder.nzp_reval > 0) ? " and r.nzp_reval = " + finder.nzp_reval : "") +
                    ((finder.dat_oper_po == finder.dat_oper)
                    ? " and r.dat_oper = mdy(" + finder.dat_oper.ToString("MM,dd,yyyy") + ") "
                    : " and r.dat_oper >= mdy(" + finder.dat_oper.ToString("MM,dd,yyyy") + ") and r.dat_oper <= mdy(" + finder.dat_oper_po.ToString("MM,dd,yyyy") + ") "
                    ) +
                    ((finder.nzp_area > 0) ? " and (r.nzp_area = " + finder.nzp_area + " or r2.nzp_area = " + finder.nzp_area + ") " : "") +
                    ((finder.nzp_serv > 0) ? " and (r.nzp_serv = " + finder.nzp_serv + " or r2.nzp_serv = " + finder.nzp_serv + ")" : "") +
                    ((finder.nzp_payer > 0) ? " and (r.nzp_payer = " + finder.nzp_payer + " or r2.nzp_payer = " + finder.nzp_payer + ")" : "") +
                    // ограничения пользователя
                    sqlRoleFilter;
#endif
                ClassDBUtils.ExecSQL(sqlText, connectionID);
            }

            // Создать индексы на временной таблице
            //!!! возможно добавить индексы
            sqlText = " create index ix1_alx_tmp_fn_reval on alx_tmp_fn_reval (dat_oper) ";
            ClassDBUtils.ExecSQL(sqlText, connectionID);
            sqlText = " create index ix2_alx_tmp_fn_reval on alx_tmp_fn_reval (nzp_reval) ";
            ClassDBUtils.ExecSQL(sqlText, connectionID);
#if PG
            sqlText = " analyze alx_tmp_fn_reval ";
#else
            sqlText = " update statistics for table alx_tmp_fn_reval ";
#endif
            ClassDBUtils.ExecSQL(sqlText, connectionID);

            // Получить кол-во записей
            sqlText = " select count(*) as cnt from alx_tmp_fn_reval ";
            table = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
            Int32 cntReval = Convert.ToInt32(table.Rows[0]["cnt"]);

            // Получить записи для выдачи
#if PG
            sqlText =
                " select  *  from alx_tmp_fn_reval " +
                //!!! поправить сортировку
                " order by dat_oper, area, payer, service, sum_reval desc " +
                ((finder.skip > 0) ? " offset " + finder.skip : "") +
                "  " + ((finder.rows > 0) ? " limit " + finder.rows : "");
#else
sqlText =
                " select " + ((finder.skip > 0) ? " skip " + finder.skip : "") +
                "        " + ((finder.rows > 0) ? " first " + finder.rows : "") + " * " +
                " from alx_tmp_fn_reval " +
                //!!! поправить сортировку
            " order by dat_oper, area, payer, service, sum_reval desc ";
#endif

            table = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();

            // Удалить временную таблицу
            sqlText = " drop table alx_tmp_fn_reval ";
            ClassDBUtils.ExecSQL(sqlText, connectionID);

            return new ReturnsObjectType<DataTable>(table) { tag = cntReval };
        }

        public ReturnsObjectType<DataTable> GetFnRevalListSupp(FnRevalFinder finder, IDbConnection connectionID)
        {
            if (!(finder.nzp_user > 0)) return new ReturnsObjectType<DataTable>("Не задан пользователь");
            if (finder.dat_oper == DateTime.MinValue) return new ReturnsObjectType<DataTable>("Не задана дата операции");
            if (finder.dat_oper_po == DateTime.MinValue) finder.dat_oper_po = finder.dat_oper;

            DataTable table = null;
            string sqlText = "";

            // Ограничения пользователя
            string sqlRoleFilter = "";
            string strKeys = "";
            if (finder.RolesVal != null)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        strKeys = role.val.Trim(new char[] { ' ', ',' });
                        /*if (role.kod == Constants.role_sql_area)
                            sqlRoleFilter += " and r.nzp_area in (" + strKeys + ") and r2.nzp_area in (" + strKeys + ") ";
                        else*/ if (role.kod == Constants.role_sql_serv)
                            sqlRoleFilter += " and r.nzp_serv in (" + strKeys + ") and r2.nzp_serv in (" + strKeys + ") ";
                        else if (role.kod == Constants.role_sql_payer)
                            sqlRoleFilter += " and r.nzp_payer in (" + strKeys + ") and r2.nzp_payer in (" + strKeys + ") ";
                    }
                }
            }

            // Создать временную таблицу
            sqlText = " drop table alx_tmp_fn_reval ";
            ClassDBUtils.ExecSQL(sqlText, connectionID, ClassDBUtils.ExecMode.Log);
#if PG
            sqlText =
                          " create temp table alx_tmp_fn_reval ( " +
                          "   nzp_reval INTEGER, nzp_supp INTEGER, nzp_serv INTEGER, nzp_payer INTEGER, " +
                          "   supp character(100), service character(100), payer character(200), " +
                          "   nzp_reval_2 INTEGER, nzp_supp_2 INTEGER, nzp_serv_2 INTEGER, nzp_payer_2 INTEGER, " +
                          "   supp_2 character(100), service_2 character(100), payer_2 character(200), " +
                          "   dat_oper DATE, sum_reval NUMERIC(14,2), comment character(60), nzp_user INTEGER, user_ character(100), dat_when DATE " +
                          " )  ";
#else
            sqlText =
                          " create temp table alx_tmp_fn_reval ( " +
                          "   nzp_reval INTEGER, nzp_supp INTEGER, nzp_serv INTEGER, nzp_payer INTEGER, " +
                          "   supp VARCHAR(100), service VARCHAR(100), payer VARCHAR(200), " +
                          "   nzp_reval_2 INTEGER, nzp_supp_2 INTEGER, nzp_serv_2 INTEGER, nzp_payer_2 INTEGER, " +
                          "   supp_2 VARCHAR(100), service_2 VARCHAR(100), payer_2 VARCHAR(200), " +
                          "   dat_oper DATE, sum_reval DECIMAL(14,2), comment VARCHAR(60), nzp_user INTEGER, user_ VARCHAR(100), dat_when DATE " +
                          " ) with no log ";
#endif
            ClassDBUtils.ExecSQL(sqlText, connectionID);

            // Поиск по базам xxx_fin_YY (по годам из интервала дат dat_oper - dat_oper_po)
            string tableFnReval;
#if PG
            sqlText =
                      " select yearr from " + Points.Pref + "_kernel.s_baselist " +
                      " where idtype = 4 and yearr >= " + finder.dat_oper.ToString("yyyy") + " and yearr <= " + finder.dat_oper_po.ToString("yyyy") +
                      " order by yearr ";
#else
            sqlText =
                      " select yearr from " + Points.Pref + "_kernel:s_baselist " +
                      " where idtype = 4 and yearr >= " + finder.dat_oper.ToString("yyyy") + " and yearr <= " + finder.dat_oper_po.ToString("yyyy") +
                      " order by yearr ";
#endif
            foreach (DataRow row in ClassDBUtils.OpenSQL(sqlText, connectionID).GetData().Rows)
            {
#if PG
                tableFnReval = Points.Pref + "_fin_" + row["yearr"].ToString().Substring(2, 2) + ".fn_reval";
                sqlText =
                    " INSERT INTO alx_tmp_fn_reval ( " +
                    "  nzp_reval, nzp_supp, nzp_serv, nzp_payer, supp, service, payer, " +
                    "  nzp_reval_2, nzp_supp_2, nzp_serv_2, nzp_payer_2, supp_2, service_2, payer_2, " +
                    "  dat_oper, sum_reval, comment, nzp_user, user_, dat_when ) " +
                    " SELECT r.nzp_reval, r.nzp_supp, r.nzp_serv, r.nzp_payer, " +
                    "   trim(sp.name_supp) as supp, trim(s.service) as service, trim(p.payer) as payer, " +
                    "   r2.nzp_reval_2, r2.nzp_supp as nzp_supp_2, r2.nzp_serv as nzp_serv_2, r2.nzp_payer as nzp_payer_2, " +
                    "   trim(sp2.name_supp) as supp_2, trim(s2.service) as service_2, trim(p2.payer) as payer_2, " +
                    "   r.dat_oper, r.sum_reval, r.comment, r.nzp_user, '' as user_, r.dat_when " +
                    " FROM " + tableFnReval + " r " +
                    " left outer join " + Points.Pref + "_kernel.supplier sp on r.nzp_supp=sp.nzp_supp " +
                    " left outer join " + Points.Pref + "_kernel.services s on r.nzp_serv=s.nzp_serv " +
                    " left outer join " + Points.Pref + "_kernel.s_payer p on r.nzp_payer=p.nzp_payer, " +
                    tableFnReval + " r2 " +
                    " left outer join " + Points.Pref + "_kernel.supplier sp2 on r2.nzp_supp=sp2.nzp_supp " +
                    " left outer join " + Points.Pref + "_kernel.services s2 on r2.nzp_serv=s2.nzp_serv " +
                    " left outer join " + Points.Pref + "_kernel.s_payer p2 on r2.nzp_payer=p2.nzp_payer " +
                    " WHERE r.nzp_reval_2=r2.nzp_reval " +
                    "   and r.sum_reval > 0 " +
                    ((finder.nzp_reval > 0) ? " and r.nzp_reval = " + finder.nzp_reval : "") +
                    ((finder.dat_oper_po == finder.dat_oper)
                    ? " and r.dat_oper = public.mdy(" + finder.dat_oper.ToString("MM,dd,yyyy") + ") "
                    : " and r.dat_oper >= public.mdy(" + finder.dat_oper.ToString("MM,dd,yyyy") + ") " +
                    " and r.dat_oper <= public.mdy(" + finder.dat_oper_po.ToString("MM,dd,yyyy") + ") "
                    ) +
                    ((finder.nzp_supp > 0) ? " and (r.nzp_supp = " + finder.nzp_supp + " or r2.nzp_supp = " + finder.nzp_supp + ") " : "") +
                    ((finder.nzp_serv > 0) ? " and (r.nzp_serv = " + finder.nzp_serv + " or r2.nzp_serv = " + finder.nzp_serv + ")" : "") +
                    ((finder.nzp_payer > 0) ? " and (r.nzp_payer = " + finder.nzp_payer + " or r2.nzp_payer = " + finder.nzp_payer + ")" : "") +
                    // ограничения пользователя
                    sqlRoleFilter;
#else
                tableFnReval = Points.Pref + "_fin_" + row["yearr"].ToString().Substring(2, 2) + ":fn_reval";
                sqlText =
                    " INSERT INTO alx_tmp_fn_reval ( " +
                    "  nzp_reval, nzp_supp, nzp_serv, nzp_payer, supp, service, payer, " +
                    "  nzp_reval_2, nzp_supp_2, nzp_serv_2, nzp_payer_2, supp_2, service_2, payer_2, " +
                    "  dat_oper, sum_reval, comment, nzp_user, user_, dat_when ) " +
                    " SELECT r.nzp_reval, r.nzp_supp, r.nzp_serv, r.nzp_payer, " +
                    "   trim(sp.name_supp) as supp, trim(s.service) as service, trim(p.payer) as payer, " +
                    "   r2.nzp_reval_2, r2.nzp_supp as nzp_supp_2, r2.nzp_serv as nzp_serv_2, r2.nzp_payer as nzp_payer_2, " +
                    "   trim(sp2.name_supp) as supp_2, trim(s2.service) as service_2, trim(p2.payer) as payer_2, " +
                    "   r.dat_oper, r.sum_reval, r.comment, r.nzp_user, '' as user_, r.dat_when " +
                    " FROM " + tableFnReval + " r, " + tableFnReval + " r2, " +
                    "   outer " + Points.Pref + "_kernel:supplier sp, outer " + Points.Pref + "_kernel:services s, outer " + Points.Pref + "_kernel:s_payer p, " +
                    "   outer " + Points.Pref + "_kernel:supplier sp2, outer " + Points.Pref + "_kernel:services s2, outer " + Points.Pref + "_kernel:s_payer p2 " +
                    " WHERE r.nzp_reval_2=r2.nzp_reval " +
                    "   and r.nzp_supp=sp.nzp_supp and r.nzp_serv=s.nzp_serv and r.nzp_payer=p.nzp_payer " +
                    "   and r2.nzp_supp=sp2.nzp_supp and r2.nzp_serv=s2.nzp_serv and r2.nzp_payer=p2.nzp_payer " +
                    "   and r.sum_reval > 0 " +
                    ((finder.nzp_reval > 0) ? " and r.nzp_reval = " + finder.nzp_reval : "") +
                    ((finder.dat_oper_po == finder.dat_oper)
                    ? " and r.dat_oper = mdy(" + finder.dat_oper.ToString("MM,dd,yyyy") + ") "
                    : " and r.dat_oper >= mdy(" + finder.dat_oper.ToString("MM,dd,yyyy") + ") and r.dat_oper <= mdy(" + finder.dat_oper_po.ToString("MM,dd,yyyy") + ") "
                    ) +
                    ((finder.nzp_supp > 0) ? " and (r.nzp_supp = " + finder.nzp_supp + " or r2.nzp_supp = " + finder.nzp_supp + ") " : "") +
                    ((finder.nzp_serv > 0) ? " and (r.nzp_serv = " + finder.nzp_serv + " or r2.nzp_serv = " + finder.nzp_serv + ")" : "") +
                    ((finder.nzp_payer > 0) ? " and (r.nzp_payer = " + finder.nzp_payer + " or r2.nzp_payer = " + finder.nzp_payer + ")" : "") +
                    // ограничения пользователя
                    sqlRoleFilter;
#endif
                ClassDBUtils.ExecSQL(sqlText, connectionID);
            }

            // Создать индексы на временной таблице
            //!!! возможно добавить индексы
            sqlText = " create index ix1_alx_tmp_fn_reval on alx_tmp_fn_reval (dat_oper) ";
            ClassDBUtils.ExecSQL(sqlText, connectionID);
            sqlText = " create index ix2_alx_tmp_fn_reval on alx_tmp_fn_reval (nzp_reval) ";
            ClassDBUtils.ExecSQL(sqlText, connectionID);
#if PG
            sqlText = " analyze alx_tmp_fn_reval ";
#else
            sqlText = " update statistics for table alx_tmp_fn_reval ";
#endif
            ClassDBUtils.ExecSQL(sqlText, connectionID);

            // Получить кол-во записей
            sqlText = " select count(*) as cnt from alx_tmp_fn_reval ";
            table = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
            Int32 cntReval = Convert.ToInt32(table.Rows[0]["cnt"]);

            // Получить записи для выдачи
#if PG
            sqlText =
                " select  *  from alx_tmp_fn_reval " +
                //!!! поправить сортировку
                " order by dat_oper, supp, payer, service, sum_reval desc " +
                ((finder.skip > 0) ? " offset " + finder.skip : "") +
                "  " + ((finder.rows > 0) ? " limit " + finder.rows : "");
#else
            sqlText =
                            " select " + ((finder.skip > 0) ? " skip " + finder.skip : "") +
                            "        " + ((finder.rows > 0) ? " first " + finder.rows : "") + " * " +
                            " from alx_tmp_fn_reval " +
                //!!! поправить сортировку
                        " order by dat_oper, supp, payer, service, sum_reval desc ";
#endif

            table = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();

            // Удалить временную таблицу
            sqlText = " drop table alx_tmp_fn_reval ";
            ClassDBUtils.ExecSQL(sqlText, connectionID);

            return new ReturnsObjectType<DataTable>(table) { tag = cntReval };
        }

        public ReturnsType SaveFnRevalDom(FnReval finder, IDbConnection connectionID, IDbTransaction transactionID,
            List<decimal> sum_list, List<int> nzp_dom_list, int mode, int nzp_reval, int nzp_payer, int nzp_serv)
        {
            string tableFnRevalDom = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ":fn_reval_dom";
            string sqlText = "";

            if (mode > 0)
            {
                #region обновить данные по домам
                //--------------------------------------------------------------------------------------------------------------------------
                for (int i = 0; i < sum_list.Count; i++)
                {
#if PG
                    sqlText = "update " + tableFnRevalDom + " set sum_reval = " + sum_list[i] + ", " +
                        " dat_when = current_date " +
                       " where nzp_dom = " + nzp_dom_list[i] + " and nzp_serv = " + nzp_serv +
                       " and nzp_area = " + finder.nzp_area + " and nzp_payer = " + nzp_payer +
                       " and nzp_reval = " + nzp_reval +
                       " and nzp_bank = -1";
#else
                    sqlText = "update " + tableFnRevalDom + " set sum_reval = " + sum_list[i] + ", " +
                        " dat_when = today " +
                        " where nzp_dom = " + nzp_dom_list[i] + " and nzp_serv = " + nzp_serv +
                        " and nzp_area = " + finder.nzp_area + " and nzp_payer = " + nzp_payer +
                        " and nzp_reval = " + nzp_reval + 
                        " and nzp_bank = -1";
#endif
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);
                }
                //--------------------------------------------------------------------------------------------------------------------------
                #endregion

                return new ReturnsType(true, "", 0);
            }
            else
            {
                #region вставить данные по домам
                //--------------------------------------------------------------------------------------------------------------------------
                for (int i = 0; i < sum_list.Count; i++)
                {
#if PG
                    sqlText = "insert into " + tableFnRevalDom +
                        " (nzp_reval, dat_oper, nzp_dom, nzp_payer, nzp_area, nzp_serv, sum_reval, dat_when, nzp_bank) " +
                        " values (" + nzp_reval + ", " + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ", " +
                        nzp_dom_list[i] + ", " + nzp_payer + ", " + finder.nzp_area + ", " + nzp_serv + "," +
                        Utils.EFlo0(sum_list[i]) + ", now(), -1)";
#else
                    sqlText = "insert into " + tableFnRevalDom + 
                        " (nzp_reval, dat_oper, nzp_dom, nzp_payer, nzp_area, nzp_serv, sum_reval, dat_when, nzp_bank) " +
                        " values (" + nzp_reval + ", " + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ", " +
                        nzp_dom_list[i] + ", " + nzp_payer + ", " + finder.nzp_area + ", " + nzp_serv + "," +
                        Utils.EFlo0(sum_list[i]) + ", current, -1)";
#endif
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);
                }
                //--------------------------------------------------------------------------------------------------------------------------
                #endregion

                //return new ReturnsType(true, "", 0);
            }
            return new ReturnsType(true, "", 0);

        }

        public ReturnsType SaveFnRevalDomSupp(IDbConnection connectionID, IDbTransaction transactionID,
            List<decimal> sum_list, List<int> nzp_dom_list, int nzp_reval, int nzp_payer, int nzp_supp, int nzp_serv)
        {
            string tableFnRevalDom = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + tableDelimiter + "fn_reval_dom";
            string sqlText = "delete from " + tableFnRevalDom +
                      " where " +
                      " nzp_reval = " + nzp_reval;
            ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);
           
            #region вставить данные по домам
            //--------------------------------------------------------------------------------------------------------------------------
            for (int i = 0; i < sum_list.Count; i++)
            {
                sqlText = "insert into " + tableFnRevalDom +
                    " (nzp_reval, dat_oper, nzp_dom, nzp_payer, nzp_supp, nzp_serv, sum_reval, dat_when, nzp_bank) " +
                    " values (" + nzp_reval + ", " + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ", " +
                    nzp_dom_list[i] + ", " + nzp_payer + ", " + nzp_supp + ", " + nzp_serv + "," +
                    Utils.EFlo0(sum_list[i]) + ", "+sCurDateTime+", -1)";

                ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);
            }
            //--------------------------------------------------------------------------------------------------------------------------
            #endregion

          
            return new ReturnsType(true, "", 0);

        }


        public ReturnsType SaveFnReval(FnReval finder, IDbConnection connectionID)
        {
            #region Проверка входных параметров
            if (!(finder.nzp_user > 0)) return new ReturnsType(false, "Не задан пользователь", -1);
            if (!(finder.nzp_area > 0)) return new ReturnsType(false, "Не задана Управляющая организация", -1);
            if (!(finder.nzp_payer > 0)) return new ReturnsType(false, "Не задан подрядчик (начисление)", -1);
            if (!(finder.nzp_serv > 0)) return new ReturnsType(false, "Не задана услуга (начисление)", -1);
            if (!(finder.nzp_payer_2 > 0)) return new ReturnsType(false, "Не задан подрядчик (снятие)", -1);
            if (!(finder.nzp_serv_2 > 0)) return new ReturnsType(false, "Не задана услуга (снятие)", -1);
            if (!(finder.sum_reval != 0)) return new ReturnsType(false, "Не задана сумма", -1);
            if ((finder.nzp_payer == finder.nzp_payer_2) && (finder.nzp_serv == finder.nzp_serv_2))
                return new ReturnsType(false, "Сочетание Подрядчик-Услуга начисления и снятия должны отличаться", -1);
            #endregion

#if PG
            string tableFnReval = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ".fn_reval";
            string tableFnRevalDom = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ".fn_reval_dom";
#else
            string tableFnReval = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ":fn_reval";
#endif
            string sqlText = "";
            DataTable dt = null;
            FnReval fnReval = null;

            // Проверить при изменении
            if (finder.nzp_reval > 0)
            {
#if PG
                sqlText =
                    " SELECT r.nzp_reval, r.nzp_area, r.nzp_serv, r.nzp_payer, " +
                    "   trim(a.area) as area, trim(s.service) as service, trim(p.payer) as payer, " +
                    "   r2.nzp_reval_2, r2.nzp_area as nzp_area_2, r2.nzp_serv as nzp_serv_2, r2.nzp_payer as nzp_payer_2, " +
                    "   trim(a2.area) as area_2, trim(s2.service) as service_2, trim(p2.payer) as payer_2, " +
                    "   r.dat_oper, r.sum_reval, r.comment, r.nzp_user, '' as user_, r.dat_when " +
                    " FROM " + tableFnReval + " r " +
                    " left outer join " + Points.Pref + "_data.s_area a on r.nzp_area=a.nzp_area " +
                    " left outer join " + Points.Pref + "_kernel.services s on r.nzp_serv=s.nzp_serv " +
                    " left outer join " + Points.Pref + "_kernel.s_payer p on r.nzp_payer=p.nzp_payer, " +
                    tableFnReval + " r2 " +
                    " left outer join " + Points.Pref + "_data.s_area a2 on r2.nzp_area=a2.nzp_area " +
                    " left outer join " + Points.Pref + "_kernel.services s2 on r2.nzp_serv=s2.nzp_serv " +
                    " left outer join " + Points.Pref + "_kernel.s_payer p2 on r2.nzp_payer=p2.nzp_payer " +
                    " WHERE r.nzp_reval_2=r2.nzp_reval " +
                    "   and r.sum_reval > 0 " +
                    "   and r.nzp_reval = " + finder.nzp_reval;
#else
                sqlText =
                    " SELECT r.nzp_reval, r.nzp_area, r.nzp_serv, r.nzp_payer, " +
                    "   trim(a.area) as area, trim(s.service) as service, trim(p.payer) as payer, " +
                    "   r2.nzp_reval_2, r2.nzp_area as nzp_area_2, r2.nzp_serv as nzp_serv_2, r2.nzp_payer as nzp_payer_2, " +
                    "   trim(a2.area) as area_2, trim(s2.service) as service_2, trim(p2.payer) as payer_2, " +
                    "   r.dat_oper, r.sum_reval, r.comment, r.nzp_user, '' as user_, r.dat_when " +
                    " FROM " + tableFnReval + " r, " + tableFnReval + " r2, " +
                    "   outer " + Points.Pref + "_data:s_area a, outer " + Points.Pref + "_kernel:services s, outer " + Points.Pref + "_kernel:s_payer p, " +
                    "   outer " + Points.Pref + "_data:s_area a2, outer " + Points.Pref + "_kernel:services s2, outer " + Points.Pref + "_kernel:s_payer p2 " +
                    " WHERE r.nzp_reval_2=r2.nzp_reval " +
                    "   and r.nzp_area=a.nzp_area and r.nzp_serv=s.nzp_serv and r.nzp_payer=p.nzp_payer " +
                    "   and r2.nzp_area=a2.nzp_area and r2.nzp_serv=s2.nzp_serv and r2.nzp_payer=p2.nzp_payer " +
                    "   and r.sum_reval > 0 " +
                    "   and r.nzp_reval = " + finder.nzp_reval;
#endif
                dt = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
                if (dt.Rows.Count == 0)
                    return new ReturnsType(false, "Не определена перекидка", -1);
                fnReval = DbFnReval.ToFnRevalValue(dt.Rows[0]);

                if (fnReval.dat_oper != Points.DateOper)
                    return new ReturnsType(false, "Можно редактировать только перекидки текущего операционного дня", -1);
                if ((finder.nzp_area != fnReval.nzp_area) || (finder.nzp_area != fnReval.nzp_area_2) ||
                    (finder.nzp_payer != fnReval.nzp_payer) || (finder.nzp_payer_2 != fnReval.nzp_payer_2) ||
                    (finder.nzp_serv != fnReval.nzp_serv) || (finder.nzp_serv_2 != fnReval.nzp_serv_2))
                    return new ReturnsType(false, "Можно изменить только сумму перекидки", -1);
            }
            // Проверить при вставке
            else
            {
#if PG
                sqlText =
                    " select count(*) as cnt " +
                    " from " + tableFnReval + " r, " + tableFnReval + " r2 " +
                    " where r.nzp_reval_2=r2.nzp_reval " +
                    " and r.dat_oper = public.mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + ") " +
                    " and r.nzp_area = " + finder.nzp_area +
                    " and r.nzp_payer = " + finder.nzp_payer +
                    " and r.nzp_serv = " + finder.nzp_serv +
                    " and r2.nzp_area = " + finder.nzp_area +
                    " and r2.nzp_payer = " + finder.nzp_payer_2 +
                    " and r2.nzp_serv = " + finder.nzp_serv_2;
#else
 sqlText =
                    " select count(*) as cnt " +
                    " from " + tableFnReval + " r, " + tableFnReval + " r2 " +
                    " where r.nzp_reval_2=r2.nzp_reval " +
                    " and r.dat_oper = mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + ") " +
                    " and r.nzp_area = " + finder.nzp_area +
                    " and r.nzp_payer = " + finder.nzp_payer +
                    " and r.nzp_serv = " + finder.nzp_serv +
                    " and r2.nzp_area = " + finder.nzp_area +
                    " and r2.nzp_payer = " + finder.nzp_payer_2 +
                    " and r2.nzp_serv = " + finder.nzp_serv_2;
#endif
                dt = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
                if (Convert.ToInt16(dt.Rows[0]["cnt"]) > 0) return new ReturnsType(false, "Такая перекидка уже существует, при необходимости измените в ней сумму", -1);
            }

            // Сохранить
            Int32 keyID = finder.nzp_reval;

            #region Определение локального пользователя
            Returns ret = Utils.InitReturns();

            Int32 nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            Int32 nzpUser = db.GetLocalUser(connectionID, finder, out ret);
            db.Close();
            if (!ret.result) return new ReturnsType(false, ret.text, 0);*/
            #endregion

            IDbTransaction transactionID = connectionID.BeginTransaction();
            try
            {
                List<decimal> sum_send_list = new List<decimal>();
                List<decimal> sum_list = new List<decimal>();

                #region получить суммы по домам для распределения по домам
                //-----------------------------------------------------------------------------------------------------------
                IDataReader reader = null;

                List<Int32> nzp_dom_list = new List<Int32>();

                Int32 nzp_dom = 0;
                decimal sum_send = 0;

                string distrib = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + (Points.DateOper.Month % 100).ToString("00");

#if PG
                var nvl = " coalesce ";
#else 
                var nvl = " nvl ";
#endif

                sqlText = "select nzp_dom, " +
#if PG
 "sum(coalesce(sum_out, 0)) as sum_ " +
#else
                        " sum(nvl(sum_in, 0) + nvl(sum_rasp, 0) - nvl(sum_ud, 0)) as sum_ " +
#endif
 " from " + distrib +
                " where nzp_area = " + finder.nzp_area +
                "   and nzp_payer = " + finder.nzp_payer +
                "   and nzp_serv = " + finder.nzp_serv +
                "   and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(Points.DateOper).ToShortDateString()) +
                "   and (" + nvl + "(sum_in, 0) + " + nvl + "(sum_rasp, 0) - " + nvl + "(sum_ud, 0)) > 0 " +
                " group by 1" +
                " order by 1";

                ret = ExecRead(connectionID, transactionID, out reader, sqlText, true);
                if (!ret.result)
                {
                    connectionID.Close();
                    reader.Close();
                    return new ReturnsType() { tag = -1 }; ;
                }

                while (reader.Read())
                {
                    nzp_dom = 0;
                    sum_send = 0;
                    if (reader["nzp_dom"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["sum_"] != DBNull.Value) sum_send = Convert.ToDecimal(reader["sum_"]);
                    sum_send_list.Add(sum_send);
                    nzp_dom_list.Add(nzp_dom);
                }
                //-----------------------------------------------------------------------------------------------------
                #endregion

                if (finder.nzp_reval > 0)
                {
                    #region Добавление в sys_events события 'Изменение перекидки'
                    try
                    {
                        //получение старых значений
                        var changed_fields = "";
                        var asd = "select * from " + tableFnReval + " where nzp_reval = " + finder.nzp_reval;
                        ret = ExecRead(connectionID, transactionID, out reader, "select * from " + tableFnReval + " where nzp_reval = " + finder.nzp_reval, true);
                        while (reader.Read())
                        {
                            if (reader["sum_reval"] != DBNull.Value && reader["sum_reval"].ToString().Trim() != Utils.EFlo0(finder.sum_reval).ToString())
                                changed_fields += "Сумма перекидки: c " + reader["sum_reval"].ToString().Trim() + " на " + Utils.EFlo0(finder.sum_reval) + ". ";
                            if (reader["comment"] != DBNull.Value && reader["comment"].ToString().Trim() != Utils.EStrNull(finder.comment))
                                changed_fields += "Комментарий: c " + reader["comment"].ToString().Trim() + " на " + Utils.EStrNull(finder.comment) + ". ";
                        }

                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = Points.Pref,
                            bank = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2),
                            nzp_user = finder.nzp_user,
                            nzp_dict = 6599,
                            nzp_obj = finder.nzp_reval,
                            note = changed_fields != "" ? "Были изменены следующие поля: " + changed_fields : "Перекидка была изменена"
                        }, transactionID, connectionID);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    }
                    #endregion

#if PG
                    sqlText =
                        " update " + tableFnReval + " set " +
                        "   sum_reval = " + Utils.EFlo0(finder.sum_reval) + ", " +
                        "   comment   = " + Utils.EStrNull(finder.comment) + ", " +
                        "   nzp_user  = " + nzpUser + ", " +
                        "   dat_when  =     current_date " +
#else
                    sqlText =
                        " update " + tableFnReval + " set " +
                        "   sum_reval = " + Utils.EFlo0(finder.sum_reval) + ", " +
                        "   comment   = " + Utils.EStrNull(finder.comment) + ", " +
                        "   nzp_user  = " + nzpUser + ", " +
                        "   dat_when  =     today " +
#endif
 " where nzp_reval = " + finder.nzp_reval;
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                    // распределить суммы и сохранить их
                    sum_list = MathUtility.DistributeSum(finder.sum_reval, sum_send_list);
                    SaveFnRevalDom(finder, connectionID, transactionID, sum_list, nzp_dom_list, 1, finder.nzp_reval, finder.nzp_payer, finder.nzp_serv);

                    sqlText = " select nzp_reval from " + tableFnReval + " where nzp_reval_2 = " + finder.nzp_reval;

                    dt = ClassDBUtils.OpenSQL(sqlText, connectionID, transactionID).GetData();
                    Int32 nzpReval = ((dt != null) && (dt.Rows.Count > 0)) ? Convert.ToInt32(dt.Rows[0]["nzp_reval"]) : 0;

#if PG
                    sqlText =
                        " update " + tableFnReval + " set " +
                        "   sum_reval = " + Utils.EFlo0(finder.sum_reval * -1) + ", " +
                        "   comment   = " + Utils.EStrNull(finder.comment) + ", " +
                        "   nzp_user  = " + nzpUser + ", " +
                        "   dat_when  =     current_date " +
                        " where nzp_reval = " + nzpReval;
#else
                    sqlText =
                        " update " + tableFnReval + " set " +
                        "   sum_reval = " + Utils.EFlo0(finder.sum_reval * -1) + ", " +
                        "   comment   = " + Utils.EStrNull(finder.comment) + ", " +
                        "   nzp_user  = " + nzpUser + ", " +
                        "   dat_when  =     today " +
                        " where nzp_reval = " + nzpReval;
#endif
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                    // распределить суммы и сохранить их
                    sum_list = MathUtility.DistributeSum(finder.sum_reval * -1, sum_send_list);
                    SaveFnRevalDom(finder, connectionID, transactionID, sum_list, nzp_dom_list, 1, nzpReval, finder.nzp_payer_2, finder.nzp_serv_2);
                }
                else
                {

#if PG
                    sqlText =
                        " insert into " + tableFnReval + " ( " +
                        "   dat_oper, " +
                        "   nzp_area, " +
                        "   nzp_payer, " +
                        "   nzp_serv, " +
                        "   sum_reval, " +
                        "   comment, " +
                        "   nzp_user, " +
                        "   dat_when ) " +
                        " values ( " +
                        "   public.mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + "), " +
                            finder.nzp_area + ", " +
                            finder.nzp_payer + ", " +
                            finder.nzp_serv + ", " +
                            Utils.EFlo0(finder.sum_reval) + ", " +
                            Utils.EStrNull(finder.comment) + ", " +
                            nzpUser + ", " +
                        "   current_date " +
                        " ) ";
#else
 sqlText =
                        " insert into " + tableFnReval + " ( " +
                        "   dat_oper, " +
                        "   nzp_area, " +
                        "   nzp_payer, " +
                        "   nzp_serv, " +
                        "   sum_reval, " +
                        "   comment, " +
                        "   nzp_user, " +
                        "   dat_when ) " +
                        " values ( " +
                        "   mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + "), " +
                            finder.nzp_area + ", " +
                            finder.nzp_payer + ", " +
                            finder.nzp_serv + ", " +
                            Utils.EFlo0(finder.sum_reval) + ", " +
                            Utils.EStrNull(finder.comment) + ", " +
                            nzpUser + ", " +
                        "   today " +
                        " ) ";
#endif
                    keyID = Convert.ToInt32(ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID, true, ClassDBUtils.ExecMode.Exception).GetID());

                    #region Добавление в sys_events события 'Добавление перекидки'
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        bank = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2),
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6597,
                        nzp_obj = keyID,
                        note = "Сумма перекидки: " + Utils.EFlo0(finder.sum_reval)
                    }, transactionID, connectionID);
                    #endregion

                    // распределить суммы и сохранить их
                    sum_list = MathUtility.DistributeSum(finder.sum_reval, sum_send_list);
                    SaveFnRevalDom(finder, connectionID, transactionID, sum_list, nzp_dom_list, 0, keyID, finder.nzp_payer, finder.nzp_serv);

#if PG
                    sqlText =
                        " insert into " + tableFnReval + " ( " +
                        "   nzp_reval_2, " +
                        "   dat_oper, " +
                        "   nzp_area, " +
                        "   nzp_payer, " +
                        "   nzp_serv, " +
                        "   sum_reval, " +
                        "   comment, " +
                        "   nzp_user, " +
                        "   dat_when ) " +
                        " values ( " +
                            keyID.ToString() + ", " +
                        "   public.mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + "), " +
                            finder.nzp_area + ", " +
                            finder.nzp_payer_2 + ", " +
                            finder.nzp_serv_2 + ", " +
                            Utils.EFlo0(finder.sum_reval * -1) + ", " +
                            Utils.EStrNull(finder.comment) + ", " +
                            nzpUser + ", " +
                        "   current_date " +
                        " ) ";
#else
sqlText =
                        " insert into " + tableFnReval + " ( " +
                        "   nzp_reval_2, " +
                        "   dat_oper, " +
                        "   nzp_area, " +
                        "   nzp_payer, " +
                        "   nzp_serv, " +
                        "   sum_reval, " +
                        "   comment, " +
                        "   nzp_user, " +
                        "   dat_when ) " +
                        " values ( " +
                            keyID.ToString() + ", " +
                        "   mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + "), " +
                            finder.nzp_area + ", " +
                            finder.nzp_payer_2 + ", " +
                            finder.nzp_serv_2 + ", " +
                            Utils.EFlo0(finder.sum_reval * -1)   + ", " +
                            Utils.EStrNull(finder.comment) + ", " +
                            nzpUser + ", " +
                        "   today " +
                        " ) ";
#endif
                    Int32 nzpReval2 = Convert.ToInt32(ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID, true, ClassDBUtils.ExecMode.Exception).GetID());

                    sqlText =
                        " update " + tableFnReval +
                        " set nzp_reval_2 = " + nzpReval2 +
                        " where nzp_reval = " + keyID;
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                    // распределить суммы и сохранить их
                    sum_list = MathUtility.DistributeSum(finder.sum_reval * -1, sum_send_list);
                    SaveFnRevalDom(finder, connectionID, transactionID, sum_list, nzp_dom_list, 0, nzpReval2, finder.nzp_payer_2, finder.nzp_serv_2);
                }

                #region Обновить суммы в fn_distrib_dom

                DbCalcPack db1 = new DbCalcPack();
                CalcTypes.ParamCalc _paramcalc = new CalcTypes.ParamCalc(0, 0, "", Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Year, Points.DateOper.Month, 0); ;
                CalcTypes.PackXX packXX = new CalcTypes.PackXX(_paramcalc, 0, false);
                packXX.paramcalc = new CalcTypes.ParamCalc(-1, -1, "", Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Year, Points.DateOper.Month);
                packXX.paramcalc.b_pack = false;


                Returns ret2 = new Returns();

                if (!db1.Update_reval(connectionID, transactionID, packXX, true, out ret2))
                {
                    db1.Close();
                    ReturnsType ret3 = new ReturnsType();
                    ret3.result = false;
                    ret3.tag = -1;
                    ret3.text = "Ошибка обновления информации по перекидкам";
                    return ret3;
                }



                db1.Close();
                #endregion

                transactionID.Commit();
            }
            catch (Exception ex)
            {
                transactionID.Rollback();
                return new ReturnsType(false, ex.Message, 0);
            }

            return new ReturnsType() { tag = keyID };
        }

        public ReturnsType DelFnReval(FnReval finder, IDbConnection connectionID)
        {
            if (!(finder.nzp_user > 0)) return new ReturnsType(false, "Не задан пользователь", -1);
            if (!(finder.nzp_reval > 0)) return new ReturnsType(false, "Не задан код записи", -1);

#if PG
            string tableFnReval = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ".fn_reval";
            string tableFnRevalDom = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ".fn_reval_dom";
#else
            string tableFnReval = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ":fn_reval";
            string tableFnRevalDom = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ":fn_reval_dom";
#endif

            string sqlText =
                " select dat_oper from " + tableFnReval +
                " where nzp_reval = " + finder.nzp_reval;
            DataTable dt = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
            if (dt.Rows.Count == 0)
                return new ReturnsType(false, "Не определена перекидка", -1);
            if (Convert.ToDateTime(dt.Rows[0]["dat_oper"]) != Points.DateOper)
                return new ReturnsType(false, "Можно удалять только перекидки текущего операционного дня", -1);

            IDbTransaction transactionID = connectionID.BeginTransaction();
            try
            {
                sqlText = " delete from " + tableFnRevalDom + " where nzp_reval in (select nzp_reval from  " + tableFnReval + " where nzp_reval =" + finder.nzp_reval + " or nzp_reval_2 = " + finder.nzp_reval + ")";
                ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                sqlText =
                    " delete from " + tableFnReval +
                    " where nzp_reval = " + finder.nzp_reval + " or nzp_reval_2 = " + finder.nzp_reval;
                ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                #region Добавление в sys_events события 'Удаление перекидки'
                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = Points.Pref,
                    bank = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2),
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6598,
                    nzp_obj = finder.nzp_reval,
                    note = "Перекидка была успешно удалена"
                }, transactionID, connectionID);
                #endregion

                #region Обновить суммы в fn_distrib_dom

                DbCalcPack db1 = new DbCalcPack();
                CalcTypes.ParamCalc _paramcalc = new CalcTypes.ParamCalc(0, 0, "", Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Year, Points.DateOper.Month, 0); ;
                CalcTypes.PackXX packXX = new CalcTypes.PackXX(_paramcalc, 0, false);
                packXX.paramcalc = new CalcTypes.ParamCalc(-1, -1, "", Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Year, Points.DateOper.Month);
                packXX.paramcalc.b_pack = false;


                Returns ret2 = new Returns();

                if (!db1.Update_reval(connectionID, transactionID, packXX, true, out ret2))
                {
                    db1.Close();
                    ReturnsType ret = new ReturnsType();
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Ошибка обновления информации по перекидкам";
                    return ret;
                }

                db1.Close();
                #endregion

                transactionID.Commit();
            }
            catch (Exception ex)
            {
                transactionID.Rollback();
                return new ReturnsType(false, ex.Message, 0);
            }

            return new ReturnsType();
        }

        public ReturnsType SaveFnRevalSupp(FnReval finder, IDbConnection connectionID)
        {
            #region Проверка входных параметров
            if (!(finder.nzp_user > 0)) return new ReturnsType(false, "Не задан пользователь", -1);
            if (!(finder.nzp_supp > 0)) return new ReturnsType(false, "Не задан договор (начисление)", -1);
            if (!(finder.nzp_supp_2 > 0)) return new ReturnsType(false, "Не задан договор (снятие)", -1);
            if (!(finder.nzp_payer > 0)) return new ReturnsType(false, "Не задан подрядчик (начисление)", -1);
            if (!(finder.nzp_serv > 0)) return new ReturnsType(false, "Не задана услуга (начисление)", -1);
            if (!(finder.nzp_payer_2 > 0)) return new ReturnsType(false, "Не задан подрядчик (снятие)", -1);
            if (!(finder.nzp_serv_2 > 0)) return new ReturnsType(false, "Не задана услуга (снятие)", -1);
            if (!(finder.sum_reval != 0)) return new ReturnsType(false, "Не задана сумма", -1);
            if ((finder.nzp_payer == finder.nzp_payer_2) && (finder.nzp_serv == finder.nzp_serv_2))
                return new ReturnsType(false, "Сочетание Подрядчик-Услуга начисления и снятия должны отличаться", -1);
            #endregion

#if PG
            string tableFnReval = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ".fn_reval";
            string tableFnRevalDom = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ".fn_reval_dom";
#else
            string tableFnReval = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ":fn_reval";
#endif
            string sqlText = "";
            DataTable dt = null;
            FnReval fnReval = null;

            // Проверить при изменении
            if (finder.nzp_reval > 0)
            {
#if PG
                sqlText =
                    " SELECT r.nzp_reval, r.nzp_supp, r.nzp_serv, r.nzp_payer, " +
                    "   trim(sp.name_supp) as supp, trim(s.service) as service, trim(p.payer) as payer, " +
                    "   r2.nzp_reval_2, r2.nzp_supp as nzp_supp_2, r2.nzp_serv as nzp_serv_2, r2.nzp_payer as nzp_payer_2, " +
                    "   trim(sp2.name_supp) as supp_2, trim(s2.service) as service_2, trim(p2.payer) as payer_2, " +
                    "   r.dat_oper, r.sum_reval, r.comment, r.nzp_user, '' as user_, r.dat_when " +
                    " FROM " + tableFnReval + " r " +
                    " left outer join " + Points.Pref + "_kernel.supplier sp on r.nzp_supp=sp.nzp_supp " +
                    " left outer join " + Points.Pref + "_kernel.services s on r.nzp_serv=s.nzp_serv " +
                    " left outer join " + Points.Pref + "_kernel.s_payer p on r.nzp_payer=p.nzp_payer, " +
                    tableFnReval + " r2 " +
                    " left outer join " + Points.Pref + "_kernel.supplier sp2 on r2.nzp_supp=sp2.nzp_supp " +
                    " left outer join " + Points.Pref + "_kernel.services s2 on r2.nzp_serv=s2.nzp_serv " +
                    " left outer join " + Points.Pref + "_kernel.s_payer p2 on r2.nzp_payer=p2.nzp_payer " +
                    " WHERE r.nzp_reval_2=r2.nzp_reval " +
                    "   and r.sum_reval > 0 " +
                    "   and r.nzp_reval = " + finder.nzp_reval;
#else
                sqlText =
                    " SELECT r.nzp_reval, r.nzp_supp, r.nzp_serv, r.nzp_payer, " +
                    "   trim(sp.name_supp) as supp, trim(s.service) as service, trim(p.payer) as payer, " +
                    "   r2.nzp_reval_2, r2.nzp_supp as nzp_supp_2, r2.nzp_serv as nzp_serv_2, r2.nzp_payer as nzp_payer_2, " +
                    "   trim(sp2.name_supp) as supp_2, trim(s2.service) as service_2, trim(p2.payer) as payer_2, " +
                    "   r.dat_oper, r.sum_reval, r.comment, r.nzp_user, '' as user_, r.dat_when " +
                    " FROM " + tableFnReval + " r, " + tableFnReval + " r2, " +
                    "   outer " + Points.Pref + "_kernel:supplier sp, outer " + Points.Pref + "_kernel:services s, outer " + Points.Pref + "_kernel:s_payer p, " +
                    "   outer " + Points.Pref + "_kernel:supplier sp2, outer " + Points.Pref + "_kernel:services s2, outer " + Points.Pref + "_kernel:s_payer p2 " +
                    " WHERE r.nzp_reval_2=r2.nzp_reval " +
                    "   and r.nzp_supp=sp.nzp_supp and r.nzp_serv=s.nzp_serv and r.nzp_payer=p.nzp_payer " +
                    "   and r2.nzp_supp=sp2.nzp_supp and r2.nzp_serv=s2.nzp_serv and r2.nzp_payer=p2.nzp_payer " +
                    "   and r.sum_reval > 0 " +
                    "   and r.nzp_reval = " + finder.nzp_reval;
#endif
                dt = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
                if (dt.Rows.Count == 0)
                    return new ReturnsType(false, "Не определена перекидка", -1);
                fnReval = DbFnReval.ToFnRevalValueSupp(dt.Rows[0]);

                if (fnReval.dat_oper != Points.DateOper)
                    return new ReturnsType(false, "Можно редактировать только перекидки текущего операционного дня", -1);
                if ((finder.nzp_supp != fnReval.nzp_supp) || (finder.nzp_supp_2 != fnReval.nzp_supp_2) ||
                    (finder.nzp_payer != fnReval.nzp_payer) || (finder.nzp_payer_2 != fnReval.nzp_payer_2) ||
                    (finder.nzp_serv != fnReval.nzp_serv) || (finder.nzp_serv_2 != fnReval.nzp_serv_2))
                    return new ReturnsType(false, "Можно изменить только сумму перекидки", -1);
            }
            // Проверить при вставке
            else
            {

                sqlText =
                        " select count(*) as cnt " +
                        " from " + tableFnReval + " r, " + tableFnReval + " r2 " +
                        " where r.nzp_reval_2=r2.nzp_reval " +
                        " and r.dat_oper = " + sDefaultSchema+"MDY(" + Points.DateOper.ToString("MM,dd,yyyy") + ") " +
                        " and r.nzp_supp = " + finder.nzp_supp +
                        " and r.nzp_payer = " + finder.nzp_payer +
                        " and r.nzp_serv = " + finder.nzp_serv +
                        " and r2.nzp_supp = " + finder.nzp_supp_2 +
                        " and r2.nzp_payer = " + finder.nzp_payer_2 +
                        " and r2.nzp_serv = " + finder.nzp_serv_2;

                dt = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
                if (Convert.ToInt16(dt.Rows[0]["cnt"]) > 0) return new ReturnsType(false, "Такая перекидка уже существует, при необходимости измените в ней сумму", -1);
            }

            // Сохранить
            Int32 keyID = finder.nzp_reval;

            #region Определение локального пользователя
            Returns ret = Utils.InitReturns();

            Int32 nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            Int32 nzpUser = db.GetLocalUser(connectionID, finder, out ret);
            db.Close();
            if (!ret.result) return new ReturnsType(false, ret.text, 0);*/
            #endregion

            IDbTransaction transactionID = connectionID.BeginTransaction();
            try
            {
                List<decimal> sum_send_list = new List<decimal>();
                List<decimal> sum_list = new List<decimal>();

                #region получить суммы по домам для распределения по домам
                //-----------------------------------------------------------------------------------------------------------
                IDataReader reader = null;

                List<Int32> nzp_dom_list = new List<Int32>();

                Int32 nzp_dom = 0;
                decimal sum_send = 0;

                string distrib = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + (Points.DateOper.Month % 100).ToString("00");

                sqlText = "select nzp_dom, " +
                " sum(" + sNvlWord + "(sum_in, 0) + " + sNvlWord + "(sum_rasp, 0) - " + sNvlWord + "(sum_ud, 0)) as sum_ " +
                " from " + distrib +
                " where nzp_supp = " + finder.nzp_supp +
                "   and nzp_payer = " + finder.nzp_payer +
                "   and nzp_serv = " + finder.nzp_serv +
                "   and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(Points.DateOper).ToShortDateString()) +
                "   and (" + sNvlWord + "(sum_in, 0) + " + sNvlWord + "(sum_rasp, 0) - " + sNvlWord + "(sum_ud, 0)) > 0 " +
                " group by 1" +
                " order by 1";

                ret = ExecRead(connectionID, transactionID, out reader, sqlText, true);
                if (!ret.result)
                {
                    connectionID.Close();
                    reader.Close();
                    return new ReturnsType() { tag = -1 }; ;
                }

                while (reader.Read())
                {
                    nzp_dom = 0;
                    sum_send = 0;
                    if (reader["nzp_dom"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["sum_"] != DBNull.Value) sum_send = Convert.ToDecimal(reader["sum_"]);
                    sum_send_list.Add(sum_send);
                    nzp_dom_list.Add(nzp_dom);
                }
                //-----------------------------------------------------------------------------------------------------
                #endregion

                if (finder.nzp_reval > 0)
                {
                    #region Добавление в sys_events события 'Изменение перекидки'
                    try
                    {
                        //получение старых значений
                        var changed_fields = "";
                        var asd = "select * from " + tableFnReval + " where nzp_reval = " + finder.nzp_reval;
                        ret = ExecRead(connectionID, transactionID, out reader, "select * from " + tableFnReval + " where nzp_reval = " + finder.nzp_reval, true);
                        while (reader.Read())
                        {
                            if (reader["sum_reval"] != DBNull.Value && reader["sum_reval"].ToString().Trim() != Utils.EFlo0(finder.sum_reval).ToString())
                                changed_fields += "Сумма перекидки: c " + reader["sum_reval"].ToString().Trim() + " на " + Utils.EFlo0(finder.sum_reval) + ". ";
                            if (reader["comment"] != DBNull.Value && reader["comment"].ToString().Trim() != Utils.EStrNull(finder.comment))
                                changed_fields += "Комментарий: c " + reader["comment"].ToString().Trim() + " на " + Utils.EStrNull(finder.comment) + ". ";
                        }

                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = Points.Pref,
                            bank = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2),
                            nzp_user = finder.nzp_user,
                            nzp_dict = 6599,
                            nzp_obj = finder.nzp_reval,
                            note = changed_fields != "" ? "Были изменены следующие поля: " + changed_fields : "Перекидка была изменена"
                        }, transactionID, connectionID);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    }
                    #endregion

                    sqlText =
                        " update " + tableFnReval + " set " +
                        "   sum_reval = " + Utils.EFlo0(finder.sum_reval) + ", " +
                        "   comment   = " + Utils.EStrNull(finder.comment) + ", " +
                        "   nzp_user  = " + nzpUser + ", " +
                        "   dat_when  =  " +sCurDate+
                        " where nzp_reval = " + finder.nzp_reval;
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                    // распределить суммы и сохранить их
                    sum_list = MathUtility.DistributeSum(finder.sum_reval, sum_send_list);
                    SaveFnRevalDomSupp(connectionID, transactionID, sum_list, nzp_dom_list, finder.nzp_reval, finder.nzp_payer, finder.nzp_supp, finder.nzp_serv);

                    sqlText = " select nzp_reval from " + tableFnReval + " where nzp_reval_2 = " + finder.nzp_reval;

                    dt = ClassDBUtils.OpenSQL(sqlText, connectionID, transactionID).GetData();
                    Int32 nzpReval = ((dt != null) && (dt.Rows.Count > 0)) ? Convert.ToInt32(dt.Rows[0]["nzp_reval"]) : 0;

                    sqlText =
                        " update " + tableFnReval + " set " +
                        "   sum_reval = " + Utils.EFlo0(finder.sum_reval * -1) + ", " +
                        "   comment   = " + Utils.EStrNull(finder.comment) + ", " +
                        "   nzp_user  = " + nzpUser + ", " +
                        "   dat_when  =  " +sCurDate+
                        " where nzp_reval = " + nzpReval;

                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                    // распределить суммы и сохранить их
                    sum_list = MathUtility.DistributeSum(finder.sum_reval * -1, sum_send_list);
                    SaveFnRevalDomSupp(connectionID, transactionID, sum_list, nzp_dom_list, nzpReval, finder.nzp_payer_2, finder.nzp_supp_2, finder.nzp_serv_2);
                }
                else
                {

                    sqlText =
                            " insert into " + tableFnReval + " ( " +
                            "   dat_oper, " +
                            "   nzp_supp, " +
                            "   nzp_payer, " +
                            "   nzp_serv, " +
                            "   sum_reval, " +
                            "   comment, " +
                            "   nzp_user, " +
                            "   dat_when ) " +
                            " values ( " +
                            "   "+sDefaultSchema+"MDY(" + Points.DateOper.ToString("MM,dd,yyyy") + "), " +
                                finder.nzp_supp + ", " +
                                finder.nzp_payer + ", " +
                                finder.nzp_serv + ", " +
                                Utils.EFlo0(finder.sum_reval) + ", " +
                                Utils.EStrNull(finder.comment) + ", " +
                                nzpUser + ", " +
                                sCurDate +
                            " ) ";

                    keyID = Convert.ToInt32(ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID, true, ClassDBUtils.ExecMode.Exception).GetID());

                    #region Добавление в sys_events события 'Добавление перекидки'
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        bank = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2),
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6597,
                        nzp_obj = keyID,
                        note = "Сумма перекидки: " + Utils.EFlo0(finder.sum_reval)
                    }, transactionID, connectionID);
                    #endregion

                    // распределить суммы и сохранить их
                    sum_list = MathUtility.DistributeSum(finder.sum_reval, sum_send_list);
                    SaveFnRevalDomSupp(connectionID, transactionID, sum_list, nzp_dom_list, keyID, finder.nzp_payer, finder.nzp_supp, finder.nzp_serv);

                    sqlText =
                            " insert into " + tableFnReval + " ( " +
                            "   nzp_reval_2, " +
                            "   dat_oper, " +
                            "   nzp_supp, " +
                            "   nzp_payer, " +
                            "   nzp_serv, " +
                            "   sum_reval, " +
                            "   comment, " +
                            "   nzp_user, " +
                            "   dat_when ) " +
                            " values ( " +
                                keyID.ToString() + ", " +
                            "   "+sDefaultSchema+"MDY(" + Points.DateOper.ToString("MM,dd,yyyy") + "), " +
                                finder.nzp_supp_2 + ", " +
                                finder.nzp_payer_2 + ", " +
                                finder.nzp_serv_2 + ", " +
                                Utils.EFlo0(finder.sum_reval * -1) + ", " +
                                Utils.EStrNull(finder.comment) + ", " +
                                nzpUser + ", " +
                                sCurDate +
                            " ) ";

                    Int32 nzpReval2 = Convert.ToInt32(ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID, true, ClassDBUtils.ExecMode.Exception).GetID());

                    sqlText =
                        " update " + tableFnReval +
                        " set nzp_reval_2 = " + nzpReval2 +
                        " where nzp_reval = " + keyID;
                    ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                    // распределить суммы и сохранить их
                    sum_list = MathUtility.DistributeSum(finder.sum_reval * -1, sum_send_list);
                    SaveFnRevalDomSupp(connectionID, transactionID, sum_list, nzp_dom_list, nzpReval2, finder.nzp_payer_2, finder.nzp_supp_2, finder.nzp_serv_2);
                }
                transactionID.Commit();
                #region Обновить суммы в fn_distrib_dom

                DbCalcPack db1 = new DbCalcPack();
                
                Returns ret2 = new Returns();

                ret2 = db1.UpdateRevalSupp(connectionID, Points.DateOper);
                if (!ret2.result)
                {
                    db1.Close();
                    ReturnsType ret3 = new ReturnsType();
                    ret3.result = false;
                    ret3.tag = -1;
                    ret3.text = "Ошибка обновления информации по перекидкам";
                    return ret3;
                }



                db1.Close();
                #endregion

               
            }
            catch (Exception ex)
            {
                transactionID.Rollback();
                return new ReturnsType(false, ex.Message, 0);
            }

            return new ReturnsType() { tag = keyID };
        }

        public ReturnsType DelFnRevalSupp(FnReval finder, IDbConnection connectionID)
        {
            if (!(finder.nzp_user > 0)) return new ReturnsType(false, "Не задан пользователь", -1);
            if (!(finder.nzp_reval > 0)) return new ReturnsType(false, "Не задан код записи", -1);

#if PG
            string tableFnReval = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ".fn_reval";
            string tableFnRevalDom = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ".fn_reval_dom";
#else
            string tableFnReval = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ":fn_reval";
            string tableFnRevalDom = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2) + ":fn_reval_dom";
#endif

            string sqlText =
                " select dat_oper from " + tableFnReval +
                " where nzp_reval = " + finder.nzp_reval;
            DataTable dt = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
            if (dt.Rows.Count == 0)
                return new ReturnsType(false, "Не определена перекидка", -1);
            if (Convert.ToDateTime(dt.Rows[0]["dat_oper"]) != Points.DateOper)
                return new ReturnsType(false, "Можно удалять только перекидки текущего операционного дня", -1);

            IDbTransaction transactionID = connectionID.BeginTransaction();
            try
            {
                sqlText = " delete from " + tableFnRevalDom + " where nzp_reval in (select nzp_reval from  " + tableFnReval + " where nzp_reval =" + finder.nzp_reval + " or nzp_reval_2 = " + finder.nzp_reval + ")";
                ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                sqlText =
                    " delete from " + tableFnReval +
                    " where nzp_reval = " + finder.nzp_reval + " or nzp_reval_2 = " + finder.nzp_reval;
                ClassDBUtils.ExecSQL(sqlText, connectionID, transactionID);

                #region Добавление в sys_events события 'Удаление перекидки'
                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = Points.Pref,
                    bank = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2),
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6598,
                    nzp_obj = finder.nzp_reval,
                    note = "Перекидка была успешно удалена"
                }, transactionID, connectionID);
                #endregion

                transactionID.Commit();

                #region Обновить суммы в fn_distrib_dom

                DbCalcPack db1 = new DbCalcPack();
                
                Returns ret2 = new Returns();

                ret2 = db1.UpdateRevalSupp(connectionID, Points.DateOper);
                if (!ret2.result)
                {
                    db1.Close();
                    ReturnsType ret = new ReturnsType();
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Ошибка обновления информации по перекидкам";
                    return ret;
                }

                db1.Close();
                #endregion

               // transactionID.Commit();
            }
            catch (Exception ex)
            {
                transactionID.Rollback();
                return new ReturnsType(false, ex.Message, 0);
            }

            return new ReturnsType();
        }
    } //end class

} //end namespace