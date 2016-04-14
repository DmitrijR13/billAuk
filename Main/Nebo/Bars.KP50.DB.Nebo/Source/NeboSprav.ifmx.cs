using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.EPaspXsd;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;
using STCLINE.KP50.IFMX.Server.SOURCE.NEBO;

namespace STCLINE.KP50.DataBase
{

    public class DbNeboSprav : DbBase
    {
        public DbNeboSprav()
            : base()
        {

        }

        public IntfResultObjectType<List<NeboService>> SelectServiceList(
            IntfRequestType request, IDbConnection connectionID)
        {
            ResultPaging pagingInfo = new ResultPaging();
            int nzp_area = Convert.ToInt32(request.keyID);
            DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц            
            int pred_year = pred_month_calc.Year;
            string yy = (pred_year - 2000).ToString("00");

            string _sql =
                " SELECT UNIQUE s.nzp_serv, trim(s.service) service, ns.nzp_area " +
                " FROM " + Points.Pref + "_kernel:services s, " + Points.Pref + "_fin_" + yy + ": nebo_rsaldo ns " +
                " WHERE s.nzp_serv > 1 " +
                " AND s.nzp_serv = ns.nzp_serv "+                
                " AND ns.nzp_area = " + nzp_area ;
            {
                List<NeboService> list = ExecRead(connectionID, _sql, NeboSpravDataConverter.ToService);
                pagingInfo.totalRowsCount = list.Count;
                pagingInfo.totalPagesCount = 1;
                pagingInfo.rowsInCurPage = list.Count;
                return new IntfResultObjectType<List<NeboService>>(list) { paging = pagingInfo };
            }
        }

        public IntfResultObjectType<List<NeboDom>> SelectDomList(
            IntfRequestType request, IDbConnection connectionID)
        {
            string tableName = "t_dom_list";
            try
            {
                ResultPaging pagingInfo = new ResultPaging();
                Returns ret = Utils.InitReturns();
                int nzp_area = Convert.ToInt32(request.keyID);
                StringBuilder sql = new StringBuilder();
                DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц            
                int pred_year = pred_month_calc.Year;
                string yy = (pred_year - 2000).ToString("00");

                #region создание временной таблицы
                sql.Remove(0, sql.Length);
                sql.Append("CREATE TEMP TABLE " + tableName);
                sql.Append("(nzp      SERIAL NOT NULL,");
                sql.Append(" nzp_dom  INTEGER,");
                sql.Append(" nzp_area INTEGER,");
                sql.Append(" rajon    CHAR(30),");
                sql.Append(" town     CHAR(30),");
                sql.Append(" ulica    CHAR(40),");
                sql.Append(" ndom     NCHAR(10),");
                sql.Append(" nkor     NCHAR(3),");
                sql.Append(" page_number INTEGER");
                sql.Append(") WITH NO LOG;");
                ret = ExecSQL(connectionID, sql.ToString(), true);
                if (!ret.result)
                    new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };
                #endregion

                #region заполнение результирующей таблицы данными
                sql.Remove(0, sql.Length);
                sql.Append("INSERT INTO " + tableName + "(nzp_dom, nzp_area, rajon, town, ulica, ndom, nkor) ");
                sql.Append("SELECT d.nzp_dom, nd.nzp_area, ");
                sql.Append("TRIM(r.rajon) rajon, TRIM(t.town) town, TRIM(u.ulica) ulica, TRIM(d.ndom) ndom, TRIM(d.nkor) nkor ");
                sql.Append("FROM  " + Points.Pref + "_data: dom d, ");
                sql.Append(Points.Pref + "_data: s_rajon r, ");
                sql.Append(Points.Pref + "_data: s_town t, ");
                sql.Append(Points.Pref + "_data: s_ulica u, ");
                sql.Append(Points.Pref + "_fin_" + yy + ": nebo_dom nd ");
                sql.Append("WHERE d.nzp_raj = r.nzp_raj ");
                sql.Append("AND d.nzp_town = t.nzp_town ");
                sql.Append("AND d.nzp_ul =  u.nzp_ul ");
                sql.Append("AND d.nzp_dom = nd.nzp_dom ");
                sql.Append("AND nd.nzp_area = " + nzp_area);
                ret = ExecSQL(connectionID, sql.ToString(), true);
                if (!ret.result)
                    new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };
                #endregion

                #region постраничное разбиение
                DbNeboSaldo ns = new DbNeboSaldo();
                ret = ns.NumRows(connectionID, "nzp", 500, tableName, "");
                if (!ret.result)
                    new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };
                #endregion

                #region итоговая выборка
                sql.Remove(0, sql.Length);
                sql.Append("SELECT * FROM " + tableName + " WHERE page_number = " + request.paging.curPageNumber);
                List<NeboDom> list = ExecRead(connectionID, sql.ToString(), NeboSpravDataConverter.ToDom);

                ret = new DbNeboUtils().FillResultPaging(connectionID, tableName, "page_number", list.Count, out pagingInfo);
                if (!ret.result)
                    return new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };

                return new IntfResultObjectType<List<NeboDom>>(list) { paging = pagingInfo };
                #endregion
            }
            catch (Exception ex)
            {
                 return new IntfResultObjectType<List<NeboDom>>() { resultMessage = ex.Message, resultCode = -1 };
            }
            finally
            {
                ExecSQL(connectionID, "DROP TABLE " + tableName, false);
            }
        }


        public IntfResultObjectType<List<NeboSupplier>> SelectSupplierList(
            IntfRequestType request, IDbConnection connectionID)
        {
            ResultPaging pagingInfo = new ResultPaging();
            Returns ret = new Returns();
            int nzp_area = Convert.ToInt32(request.keyID);
            string t_nebo_supplier = "t_nebo_supplier" + DateTime.Now.Ticks.ToString();
            StringBuilder sql = new StringBuilder();
            try
            {
                ExecSQL(connectionID, "DROP TABLE " + t_nebo_supplier, false);
            }
            catch
            {
            }

            sql.Append("CREATE TEMP TABLE " + t_nebo_supplier);
            sql.Append("(name_supp CHAR(100), ");
            sql.Append(" nzp_supp INT, ");
            sql.Append(" inn_supp INT, ");                          //проверить тип-char или int?
            sql.Append(" kpp_supp INT, ");                          //проверить тип-char или int?
            sql.Append(" nzp_area INT ");
            sql.Append(" ) WITH NO LOG; ");

            ret = ExecSQL(connectionID, sql.ToString(), true);
            if (!ret.result)
                new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };

            DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц            
            int pred_year = pred_month_calc.Year;
            string yy = (pred_year - 2000).ToString("00");

            sql.Remove(0, sql.Length);
            sql.Append(" INSERT INTO " + t_nebo_supplier + " (name_supp, nzp_supp, nzp_area) ");
            sql.Append(" SELECT s.name_supp, s.nzp_supp, ns.nzp_area ");
            sql.Append(" FROM  " + Points.Pref + "_kernel: supplier s, ");
            sql.Append(Points.Pref + "_fin_" + yy + ": nebo_supp ns ");
            sql.Append(" WHERE s.nzp_supp = ns.nzp_supp ");
            sql.Append(" AND ns.nzp_area = " + nzp_area);
            ExecSQL(connectionID, sql.ToString(), true);

            List<_Point> prefixs = new List<_Point>();
            prefixs = Points.PointList;
            //косяк
            foreach (var points in prefixs)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE " + t_nebo_supplier);
                sql.Append(" SET inn_supp = (select max(val_prm) FROM " + Points.Pref + "_data:prm_7 p," + Points.Pref + "_fin_" + yy + ":nebo_supp ns ");
                sql.Append(" WHERE p.nzp_prm = 445 and p.nzp = " + t_nebo_supplier + ".nzp_supp ");
                sql.Append(" AND p.is_actual <> 100 and MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ") BETWEEN p.dat_s AND p.dat_po ");
                sql.Append(" AND ns.nzp_area = " + nzp_area + "), ");
                sql.Append(" kpp_supp = (SELECT MAX(val_prm) FROM " + Points.Pref + "_data:prm_7 p ," + Points.Pref + "_fin_" + yy + ":nebo_supp ns ");
                sql.Append(" WHERE p.nzp_prm = 870 and p.nzp = " + t_nebo_supplier + ".nzp_supp ");
                sql.Append(" AND p.is_actual <> 100 AND MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ") BETWEEN p.dat_s AND p.dat_po");
                sql.Append(" AND ns.nzp_area = " + nzp_area + "), ");
                sql.Append(" nzp_area = (" + nzp_area + ")");
                ExecSQL(connectionID, sql.ToString(), true);
            }

            sql.Remove(0, sql.Length);
            sql.Append("SELECT UNIQUE TRIM(name_supp) name_supp, nzp_supp, inn_supp , kpp_supp, nzp_area FROM " + t_nebo_supplier);
            {
                List<NeboSupplier> list = ExecRead(connectionID, sql.ToString(), NeboSpravDataConverter.ToSupplier);
                pagingInfo.totalRowsCount = list.Count;
                pagingInfo.totalPagesCount = 1;
                pagingInfo.rowsInCurPage = list.Count;
                return new IntfResultObjectType<List<NeboSupplier>>(list) { paging = pagingInfo };
            }
        }

        public IntfResultObjectType<List<NeboRenters>> GetRentersList(
            IntfRequestType request, IDbConnection connectionID)
        {
            ResultPaging pagingInfo = new ResultPaging();
            StringBuilder sql = new StringBuilder();
            Returns ret = new Returns();
            int nzp_area = Convert.ToInt32(request.keyID);
            string soul = DateTime.Now.Ticks.ToString();
            string t_renters = "t_nebo_renter_" + soul;
            ExecSQL(connectionID, "DROP TABLE " + t_renters, false);

            sql.Remove(0, sql.Length);
            sql.Append("CREATE TEMP TABLE " + t_renters);
            sql.Append("(nzp SERIAL NOT NULL,");
            sql.Append(" renter_id INTEGER,");
            sql.Append(" inn_kvar CHAR(20),");
            sql.Append(" kpp_kvar CHAR(20),");
            sql.Append(" account_number DECIMAL(10),");
            sql.Append(" nzp_dom INTEGER,");
            sql.Append(" nkvar NCHAR(10),");
            sql.Append(" nkvar_n NCHAR(3),");
            sql.Append(" description NCHAR(40),");
            sql.Append(" nzp_area INTEGER,");
            sql.Append(" page_number INTEGER");
            sql.Append(" ) WITH NO LOG; ");
            ret = ExecSQL(connectionID, sql.ToString(), true);
            if (!ret.result)
                new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };

            DateTime before = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1);

            #region цикл по локальным БД
            foreach (var _point in Points.PointList)
            {
                sql.Remove(0, sql.Length);
                sql.Append("INSERT INTO " + t_renters + " (");
                sql.Append("renter_id, inn_kvar, kpp_kvar, account_number, nzp_dom, nkvar, nkvar_n, nzp_area) ");
                sql.Append("SELECT 0,");
                sql.Append("(SELECT MAX(val_prm) FROM " + _point.pref + "_data: prm_1 WHERE is_actual <> 100 AND nzp = kv.nzp_kvar AND nzp_prm = 886  AND MDY(" + before.Month.ToString("00") + ",1," + before.Year + ") BETWEEN dat_s AND dat_po) AS inn, ");
                sql.Append("(SELECT MAX(val_prm) FROM " + _point.pref + "_data: prm_1 WHERE is_actual <> 100 AND nzp = kv.nzp_kvar AND nzp_prm = 1014  AND MDY(" + before.Month.ToString("00") + ",1," + before.Year + ") BETWEEN dat_s AND dat_po) AS kpp, ");
                sql.Append("kv.pkod, kv.nzp_dom, kv.nkvar, kv.nkvar_n, kv.nzp_area ");
                sql.Append("FROM " + _point.pref + "_data: kvar kv ");
                sql.Append("WHERE kv.typek <> 1 ");
                sql.Append("AND kv.nzp_area = " + nzp_area + ";");
                ret = ExecSQL(connectionID, sql.ToString(), true);
                if (!ret.result)
                    new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };
            }
            #endregion

            #region постраничное разбиение
            DbNeboSaldo ns = new DbNeboSaldo();
            ret = ns.NumRows(connectionID, "nzp", 500, t_renters, "");
            if (!ret.result)
                new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };
            #endregion

            #region итоговая выборка
            sql.Remove(0, sql.Length);
            sql.Append("SELECT ");
            sql.Append(" renter_id, inn_kvar, ");
            sql.Append(" kpp_kvar, account_number,");
            sql.Append(" nzp_dom, nkvar,");
            sql.Append(" nkvar_n, description, nzp_area ");
            sql.Append("FROM " + t_renters + " ");
            sql.Append("ORDER BY account_number;");
            List<NeboRenters> list = ExecRead(connectionID, sql.ToString(), NeboSpravDataConverter.ToRenters);

            ret = new DbNeboUtils().FillResultPaging(connectionID, t_renters, "page_number", list.Count, out pagingInfo);
            if (!ret.result)
                return new IntfResultObjectType<List<NeboRenters>>() { resultMessage = ret.text, resultCode = -1 };

            return new IntfResultObjectType<List<NeboRenters>>(list) { paging = pagingInfo };
            #endregion
        }

        public IntfResultObjectType<List<NeboArea>> SelectAreaList(
           IntfRequestType request, IDbConnection connectionID)
        {
            StringBuilder sql = new StringBuilder();
            int nzp_area = Convert.ToInt32(request.keyID);
            string t_nebo_area = "t_nebo_area" + DateTime.Now.Ticks.ToString();
            try
            {
                ExecSQL(connectionID, "DROP TABLE " + t_nebo_area, false);
            }
            catch
            {
            }

            sql.Append("CREATE TEMP TABLE " + t_nebo_area);
            sql.Append("(inn_area INT, ");
            sql.Append(" kpp_area INT, ");
            sql.Append(" area CHAR(150), ");
            sql.Append(" address_area CHAR(150), ");                        
            sql.Append(" nzp_area INT ");
            sql.Append(" ) WITH NO LOG; ");
            ExecSQL(connectionID, sql.ToString(), true);

            List<_Point> prefixs = new List<_Point>();
            prefixs = Points.PointList;

            foreach (var points in prefixs)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO " + t_nebo_area + " (area, nzp_area) ");
                sql.Append(" SELECT UNIQUE a.area, a.nzp_area  ");
                sql.Append(" FROM " + points.pref + "_data: s_area a, " + points.pref + "_data: prm_7 p, " + points.pref + "_kernel: prm_name n ");
                sql.Append(" WHERE p.nzp_prm = 2500 ");
                sql.Append(" AND p.val_prm = 1 ");
                sql.Append(" AND p.is_actual = 1 ");
                sql.Append(" AND p.nzp_prm = n.nzp_prm ");
                sql.Append(" AND a.nzp_area = " + nzp_area);
                ExecSQL(connectionID, sql.ToString(), true);
            }

#warning пустая выборка
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT inn_area, kpp_area, TRIM(area) area, TRIM(address_area) address_area, nzp_area  ");
            sql.Append(" FROM " + t_nebo_area);   

            {
                List<NeboArea> list = ExecRead(connectionID, sql.ToString(), NeboSpravDataConverter.ToArea);
                return new IntfResultObjectType<List<NeboArea>>(list);
            }
        }

    }   
}
