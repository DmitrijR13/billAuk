using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.EPaspXsd;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;
using Globals.SOURCE.INTF._EPasp.classes;

namespace STCLINE.KP50.DataBase
{
    
    public class EPaspReqPrm
    {
        public int year { get; set; }
        public int month { get; set; }
        public int nzp_dom { get; set; }
        public int nzp_kvar { get; set; }
        public string pref { get; set; }
    }


 
    public class DbEPasp : DbBase
    {
        public DbEPasp()
            : base()
        {

        }

        //МКД
        public IntfResultObjectType<custom_multiApartmentBuilding> SelectMultiApartmentBuilding(
            IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {

            DataTable MultiApartmentBuilding = new DataTable();

            custom_multiApartmentBuilding t = new custom_multiApartmentBuilding();
            StringBuilder sql = new StringBuilder();

            List<_Point> prefixs = new List<_Point>();
            prefixs = Points.PointList;

            ExecSQL(connectionID, "DROP TABLE mkd", false);

#if PG
            sql.Remove(0, sql.Length);
            sql.Append("CREATE TEMP TABLE mkd ");
            sql.Append("(distinctNumber INTEGER, ");
            sql.Append("addressFiasGuid CHAR(200), ");
            sql.Append("address CHAR(200), ");
            sql.Append("buildingTotalArea DECIMAL(14,2), ");
            sql.Append("commissioningYear INTEGER, ");
            sql.Append("residentCount INTEGER, ");
            sql.Append("personalAccountCount INTEGER, ");
            sql.Append("pref CHAR(10)) WITH NO LOG;");
#else
            sql.Remove(0, sql.Length);
            sql.Append("CREATE TEMP TABLE mkd ");
            sql.Append("(uniqueNumber INTEGER, ");
            sql.Append("addressFiasGuid CHAR(200), ");
            sql.Append("address CHAR(200), ");
            sql.Append("buildingTotalArea DECIMAL(14,2), ");
            sql.Append("commissioningYear INTEGER, ");
            sql.Append("residentCount INTEGER, ");
            sql.Append("personalAccountCount INTEGER, ");
            sql.Append("pref CHAR(10)) WITH NO LOG;");
#endif

            if (!ExecSQL(connectionID, sql.ToString(), true).result)
            {
                connectionID.Close();
                MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                return null;
            }
#if PG
            foreach (var points in prefixs)
            {
                sql.Remove(0, sql.Length);
                sql.Append("INSERT INTO mkd ");
                sql.Append("SELECT DISTINCT d.nzp_dom, ");
                sql.Append("max(trim(t.town)||'/'||trim(sr.rajon)||'/'||trim(ul.ulica)||'/'||trim(d.ndom)||'/'||trim(d.nkor)) AS address, ");
                sql.Append("max(trim(t.town)||'/'||trim(sr.rajon)||'/'||trim(ul.ulica)||'/'||trim(d.ndom)||'/'||trim(d.nkor)), ");
                sql.Append("max(CASE WHEN p2.nzp_prm=40 THEN p2.val_prm ELSE NULL END) AS buildingTotalArea, ");
                sql.Append("max(year(CASE WHEN p2.nzp_prm=150 THEN p2.val_prm ELSE NULL END)) AS commissioningYear, ");
                sql.Append("0, ");
                sql.Append("0, ");
                sql.Append("'" + points.pref + "' ");
                sql.Append("FROM " + points.pref + "_data.s_ulica ul, ");
                sql.Append(points.pref + "_data.s_rajon sr, ");
                sql.Append(points.pref + "_data.s_town t, ");
                sql.Append(points.pref + "_data.dom d ");
                sql.Append("LEFT OUTER JOIN " + points.pref + "_data.prm_2 p2 ON d.nzp_dom=p2.nzp ");
                sql.Append("WHERE d.nzp_town=t.nzp_town ");
                sql.Append("AND d.nzp_raj=sr.nzp_raj ");
                sql.Append("AND d.nzp_ul=ul.nzp_ul ");
                sql.Append("AND d.nzp_dom=" + request.entity.nzp_dom + " ");
                sql.Append("GROUP BY d.nzp_dom; ");
                if (!ExecSQL(connectionID, sql.ToString(), true).result)
                {
                    connectionID.Close();
                    MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                    return null;
                }

                sql.Remove(0, sql.Length);
                sql.Append("UPDATE mkd ");
                sql.Append("SET (residentCount) =( ");
                sql.Append("(SELECT sum(cast(coalesce(p1.val_prm,0) AS integer)) ");
                sql.Append("FROM " + points.pref + "_data.prm_1 p1, ");
                sql.Append(points.pref + "_data.kvar k ");
                sql.Append("WHERE k.nzp_kvar=p1.nzp ");
                sql.Append("AND mkd.uniqueNumber=k.nzp_dom ");
                sql.Append("AND p1.nzp_prm=5)) ");
                sql.Append("WHERE mkd.pref='" + points.pref + "' ");
                sql.Append("AND mkd.uniqueNumber=" + request.entity.nzp_dom + "; ");



                if (!ExecSQL(connectionID, sql.ToString(), true).result)
                {
                    connectionID.Close();
                    MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                    return null;
                }

                sql.Remove(0, sql.Length);
                sql.Append("UPDATE mkd ");
                sql.Append("SET personalAccountCount = ");
                sql.Append("(SELECT count(k.nzp_kvar) ");
                sql.Append("FROM " + points.pref + "_data.kvar k, ");
                sql.Append(points.pref + "_data.prm_3 p3 ");
                sql.Append("WHERE p3.nzp=k.nzp_kvar ");
                sql.Append("AND (nzp_prm=51 ");
                sql.Append("AND val_prm<>100) ");
                sql.Append("AND mkd.uniqueNumber = k.nzp_dom ");
                sql.Append("AND mkd.pref='" + points.pref + "' ");
                sql.Append("AND mkd.uniqueNumber=" + request.entity.nzp_dom + "); ");
                if (!ExecSQL(connectionID, sql.ToString(), true).result)
                {
                    connectionID.Close();
                    MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                    return null;
                }

                sql.Remove(0, sql.Length);
                sql.Append("SELECT * FROM mkd");
                DataTable dt = OpenSql(connectionID, sql.ToString());
                if (dt.Rows.Count != 0)
                {
                    MultiApartmentBuilding = MultiApartmentBuilding.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
                }
            }
#else
            foreach (var points in prefixs)
            {
                sql.Remove(0, sql.Length);
                sql.Append("INSERT INTO mkd ");
                sql.Append("SELECT UNIQUE d.nzp_dom, ");
                sql.Append("max(trim(t.town)||'/'||trim(sr.rajon)||'/'||trim(ul.ulica)||'/'||trim(d.ndom)||'/'||trim(d.nkor)) AS address, ");
                sql.Append("max(trim(t.town)||'/'||trim(sr.rajon)||'/'||trim(ul.ulica)||'/'||trim(d.ndom)||'/'||trim(d.nkor)), ");
                sql.Append("max(CASE WHEN p2.nzp_prm=40 THEN p2.val_prm ELSE NULL END) AS buildingTotalArea, ");
                sql.Append("max(year(CASE WHEN p2.nzp_prm=150 THEN p2.val_prm ELSE NULL END)) AS commissioningYear, ");
                sql.Append("0, ");
                sql.Append("0, ");
                sql.Append("'" + points.pref + "' ");
                sql.Append("FROM " + points.pref + "_data:s_ulica ul, ");
                sql.Append(points.pref + "_data:s_rajon sr, ");
                sql.Append(points.pref + "_data:s_town t, ");
                sql.Append(points.pref + "_data:dom d ");
                sql.Append("LEFT OUTER JOIN " + points.pref + "_data:prm_2 p2 ON d.nzp_dom=p2.nzp ");
                sql.Append("WHERE d.nzp_town=t.nzp_town ");
                sql.Append("AND d.nzp_raj=sr.nzp_raj ");
                sql.Append("AND d.nzp_ul=ul.nzp_ul ");
                sql.Append("AND d.nzp_dom=" + request.entity.nzp_dom + " ");
                sql.Append("GROUP BY d.nzp_dom; ");
                if (!ExecSQL(connectionID, sql.ToString(), true).result)
                {
                    connectionID.Close();
                    MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                    return null;
                }

                sql.Remove(0, sql.Length);
                sql.Append("UPDATE mkd ");
                sql.Append("SET (residentCount) =( ");
                sql.Append("(SELECT sum(cast(nvl(p1.val_prm,0) AS integer)) ");
                sql.Append("FROM " + points.pref + "_data:prm_1 p1, ");
                sql.Append(points.pref + "_data:kvar k ");
                sql.Append("WHERE k.nzp_kvar=p1.nzp ");
                sql.Append("AND mkd.uniqueNumber=k.nzp_dom ");
                sql.Append("AND p1.nzp_prm=5)) ");
                sql.Append("WHERE mkd.pref='" + points.pref + "' ");
                sql.Append("AND mkd.uniqueNumber=" + request.entity.nzp_dom + "; ");

                if (!ExecSQL(connectionID, sql.ToString(), true).result)
                {
                    connectionID.Close();
                    MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                    return null;
                }

                sql.Remove(0, sql.Length);
                sql.Append("UPDATE mkd ");
                sql.Append("SET personalAccountCount = ");
                sql.Append("(SELECT count(k.nzp_kvar) ");
                sql.Append("FROM " + points.pref + "_data:kvar k, ");
                sql.Append(points.pref + "_data:prm_3 p3 ");
                sql.Append("WHERE p3.nzp=k.nzp_kvar ");
                sql.Append("AND (nzp_prm=51 ");
                sql.Append("AND val_prm<>100) ");
                sql.Append("AND mkd.uniqueNumber = k.nzp_dom ");
                sql.Append("AND mkd.pref='" + points.pref + "' ");
                sql.Append("AND mkd.uniqueNumber=" + request.entity.nzp_dom + "); ");
                if (!ExecSQL(connectionID, sql.ToString(), true).result)
                {
                    connectionID.Close();
                    MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                    return null;
                }
            }
            sql.Remove(0, sql.Length);
            sql.Append("SELECT * FROM mkd");
            DataTable dt = OpenSql(connectionID, sql.ToString());
            if (dt.Rows.Count != 0)
            {
                MultiApartmentBuilding = MultiApartmentBuilding.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
            }
#endif

            t = OrmConvert.ConvertDataRows(MultiApartmentBuilding.Rows, EPaspDataConverter.toMultiApartmentBuilding)[0];

            return new IntfResultObjectType<custom_multiApartmentBuilding>(t);
        }

        //РСО 
        public IntfResultObjectType<List<custom_mabResourceSupplyOrganization>> SelectResourceSupplyOrganization(
         IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            List<custom_mabResourceSupplyOrganization> t = new List<custom_mabResourceSupplyOrganization>();
            DataTable ResourceSupplyOrganization = new DataTable();
            StringBuilder sql = new StringBuilder();
            string pref = request.entity.pref;
            int month = request.entity.month;
            int year = request.entity.year;

            sql.Remove(0, sql.Length);
            sql.Append("SELECT t.nzp_serv, "); 
            sql.Append("t.nzp_supp, ");
            sql.Append("sp.name_supp, ");
            sql.Append("(CAST (min(t.dat_s) AS CHAR(10))) as dat_s, ");
            sql.Append("(CAST (min(t.dat_po) AS CHAR(10))) as dat_po, ");

            sql.Append("(SELECT max(val_prm) ");
            sql.Append("FROM " + pref + "_data:prm_11 ");
            sql.Append("WHERE nzp_prm = 445 ");
            sql.Append("AND nzp = nzp_serv ");
            sql.Append("AND is_actual <> 100 ");
            sql.Append("AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po ");
            sql.Append("AND val_prm = 1) AS inn, ");

            sql.Append("(SELECT max(val_prm) ");
            sql.Append("FROM " + pref + "_data:prm_11 ");
            sql.Append("WHERE nzp_prm = 117 ");
            sql.Append("AND nzp = nzp_serv ");
            sql.Append("AND is_actual <> 100 ");
            sql.Append("AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po ");
            sql.Append("AND val_prm = 1) AS address, ");

            sql.Append("(SELECT max(val_prm) ");
            sql.Append("FROM " + pref + "_data:prm_11 ");
            sql.Append("WHERE nzp_prm = 870 ");
            sql.Append("AND nzp = nzp_serv ");
            sql.Append("AND is_actual <> 100 ");
            sql.Append("AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po ");
            sql.Append("AND val_prm = 1) AS kpp ");
            sql.Append("FROM " + pref + "_data: kvar kv, ");
            sql.Append(pref + "_data: tarif t, ");
            sql.Append(pref + "_kernel: supplier sp ");
            sql.Append("WHERE t.nzp_kvar = kv.nzp_kvar ");
            //sql.Append("--AND mdy(7,1,2012) BETWEEN t.dat_s AND t.dat_po ");
            sql.Append("AND t.is_actual <> 100 ");
            sql.Append("AND kv.nzp_dom = " + request.entity.nzp_dom);
            sql.Append("AND t.nzp_supp = sp.nzp_supp ");
            sql.Append("AND t.nzp_serv IN(6,8,9,10,25) ");
            sql.Append("GROUP BY t.nzp_serv, ");
            sql.Append("t.nzp_supp, ");
            sql.Append("sp.name_supp ");
            sql.Append("ORDER BY t.nzp_serv, ");
            sql.Append("t.nzp_supp;");

            DataTable dt = OpenSql(connectionID, sql.ToString());
            if (dt.Rows.Count != 0)
            {
                ResourceSupplyOrganization = ResourceSupplyOrganization.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
            }
            t = OrmConvert.ConvertDataRows(ResourceSupplyOrganization.Rows, EPaspDataConverter.toResourceSupplyOrganization);
            return new IntfResultObjectType<List<custom_mabResourceSupplyOrganization>>(t);
        }

        //Параметры, влияющие на начисления
        public IntfResultObjectType<mabAccrualParameters> SelectAccrualParameters(
           IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabAccrualParameters accParams = new mabAccrualParameters();
            DataTable dtAccParams = new DataTable();
            StringBuilder sql = new StringBuilder();
            string pref = request.entity.pref;
            int month = request.entity.month;
            int year = request.entity.year;

            sql.Remove(0, sql.Length);
            sql.Append("SELECT ");
            sql.Append("ul.ulica, ");
            sql.Append("r.rajon, ");
            sql.Append("t.town, ");
            sql.Append("d.nzp_dom, ");
            sql.Append("SUM(CASE WHEN (SELECT COUNT(*) FROM " + pref + "_data:prm_1 WHERE is_actual <> 100 AND nzp = kv.nzp_kvar AND nzp_prm = 7 AND val_prm = 1 AND mdy(10,1,2013) BETWEEN dat_s AND dat_po) > 0 then 1 else 0 end) as hasElectricRanges, ");
            sql.Append("SUM(CASE WHEN (SELECT COUNT(*) FROM " + pref + "_data:prm_1 WHERE is_actual <> 100 AND nzp = kv.nzp_kvar AND nzp_prm = 7 AND val_prm IN (3,4,5,10,12,14,16) AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po) > 0 then 1 else 0 end) as mabBathTubeType, ");
            sql.Append("SUM(CASE WHEN (SELECT COUNT(*) FROM " + pref + "_data:prm_1 WHERE is_actual <> 100 AND nzp = kv.nzp_kvar AND nzp_prm = 7 AND val_prm IN (15,17) AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po) > 0 then 1 else 0 end) as mabWashbasinType ");
            sql.Append("FROM ");
            sql.Append(pref + "_data: s_ulica ul, ");
            sql.Append(pref + "_data: dom d, ");
            sql.Append(pref + "_data: s_rajon r, ");
            sql.Append(pref + "_data: s_town t, ");
            sql.Append(pref + "_data: kvar kv "); 
            sql.Append("WHERE ");
            sql.Append("d.nzp_dom = " + request.entity.nzp_dom + " ");
            sql.Append("AND d.nzp_ul = ul.nzp_ul "); 
            sql.Append("AND ul.nzp_raj = r.nzp_raj "); 
            sql.Append("AND t.nzp_town = r.nzp_town ");
            sql.Append("AND d.nzp_dom = kv.nzp_dom ");
            sql.Append("group by 1,2,3,4 ");

            DataTable dt = OpenSql(connectionID, sql.ToString());
            if (dt.Rows.Count != 0)
            {
                dtAccParams = dtAccParams.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
            }
            accParams = OrmConvert.ConvertDataRows(dtAccParams.Rows, EPaspDataConverter.toAccrualParams)[0];
            return new IntfResultObjectType<mabAccrualParameters>(accParams);
        }

        //Квартира
        public IntfResultObjectType<List<custom_flat>> SelectFlats(
           IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            #region basic fields
            List<custom_flat> customFlats = new List<custom_flat>();
            DataTable flat = new DataTable();
            StringBuilder sql = new StringBuilder();
            string pref = request.entity.pref;
            int month = request.entity.month;
            int year = request.entity.year;

            sql.Remove(0, sql.Length);
            sql.Append("SELECT ");
            sql.Append("kv.nzp_kvar, ");
            sql.Append("kv.nkvar, ");
            sql.Append("kv.nkvar_n, ");
            sql.Append("kv.porch as entrance, ");
            sql.Append("nvl((SELECT MAX(val_prm) FROM " + pref + "_data:prm_1 WHERE is_actual <> 100 AND nzp = nzp_kvar AND nzp_prm = 2  AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po),0) as floor, ");
            sql.Append("nvl((SELECT MAX(val_prm) FROM " + pref + "_data:prm_1 WHERE is_actual <> 100 AND nzp = nzp_kvar AND nzp_prm = 4  AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po),0) as all_pl, ");
            sql.Append("nvl((SELECT MAX(val_prm) FROM " + pref + "_data:prm_1 WHERE is_actual <> 100 AND nzp = nzp_kvar AND nzp_prm = 6  AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po),0) as gil_pl, ");
            sql.Append("nvl((SELECT MAX(val_prm) FROM " + pref + "_data:prm_1 WHERE is_actual <> 100 AND nzp = nzp_kvar AND nzp_prm = 107  AND mdy(" + month + ",1," + year + ") BETWEEN dat_s AND dat_po),0) as kol_komnat ");
            sql.Append("FROM ");
            sql.Append(pref + "_data: kvar kv ");
            sql.Append("WHERE ");
            sql.Append("nzp_dom = " + request.entity.nzp_dom + ";");

            DataTable dt = OpenSql(connectionID, sql.ToString());
            if (dt.Rows.Count != 0)
            {
                flat = flat.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
            }
            customFlats = OrmConvert.ConvertDataRows(flat.Rows, EPaspDataConverter.toFlat);
            #endregion

            foreach (var customFlat in customFlats)
            {
                #region personalAccount
                request.entity.nzp_kvar = Convert.ToInt32(customFlat.uniqueNumber);
                customFlat.personalAccount = SelectPersonalAccount(request, connectionID).resultData;
                #endregion

                #region measurementReadings
                customFlat.measurementReading = SelectMeasurementReadings(request, connectionID).resultData;
                #endregion
            }
            return new IntfResultObjectType<List<custom_flat>>(customFlats);
        }

        public IntfResultObjectType<custom_personalAccount> SelectPersonalAccount(
            IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            custom_personalAccount personalAccounts = new custom_personalAccount();
            DataTable personalAccount = new DataTable();
            StringBuilder sql = new StringBuilder();
            string pref = request.entity.pref;
            int nzp_kvar = request.entity.nzp_kvar;
            DateTime d1 = new DateTime(request.entity.year, request.entity.month, 1);
            DateTime d2 = d1.AddMonths(1);

            sql.Remove(0, sql.Length);
            sql.Append("SELECT ");
            sql.Append("kt.tprp, ");
            sql.Append("kt.nzp_gil, ");
            sql.Append("kt.fam, ");
            sql.Append("kt.ima, ");
            sql.Append("kt.otch, ");
            sql.Append("kt.dat_rog, ");
            sql.Append("sd.nzp_dok, ");
            sql.Append("kt.serij, ");
            sql.Append("kt.nomer, ");
            sql.Append("kt.vid_dat, ");
            sql.Append("kt.vid_mes, ");
            sql.Append("kt.dat_ofor, ");
            sql.Append("kt.nzp_tkrt, ");
            sql.Append(pref + "_data: get_kol_gil(mdy(" + d1.Month + ",1," + d1.Year + "),mdy(" + d2.Month + ",1," + d2.Year + ") - 1 units day,15, kt.nzp_kvar,0) as residents, ");
            sql.Append(pref + "_data: get_kol_gil(mdy(" + d1.Month + ",1," + d1.Year + "),mdy(" + d2.Month + ",1," + d2.Year + ") - 1 units day,15,kt.nzp_kvar,2) as registered "); 
            sql.Append("FROM ");
            sql.Append(pref + "_data: kart kt, ");
            sql.Append(pref + "_data: s_dok sd "); 
            sql.Append("WHERE "); 
            sql.Append("kt.nzp_kvar = " + nzp_kvar + " ");
            sql.Append("AND kt.nzp_dok = sd.nzp_dok "); 
            sql.Append("AND kt.dat_sost =(SELECT MAX(k2.dat_sost) FROM " + pref + "_data: kart k2 ");
            sql.Append("WHERE k2.nzp_gil=kt.nzp_gil "); 
            sql.Append("AND k2.nzp_kvar=kt.nzp_kvar ");
            sql.Append(");");

            DataTable dt = OpenSql(connectionID, sql.ToString());
            if (dt.Rows.Count != 0)
            {
                personalAccount = personalAccount.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
            }
            List<custom_personalAccount> temp = OrmConvert.ConvertDataRows(personalAccount.Rows, EPaspDataConverter.toPersonalAccount);

            foreach (var t in temp)
            {
                foreach(var n in t.registeredPersons)
                    personalAccounts.registeredPersons.Add(n);
                foreach (var k in t.temporaryRegisteredPersons)
                    personalAccounts.temporaryRegisteredPersons.Add(k);
                personalAccounts.residentCountPeriods = t.residentCountPeriods;
                personalAccounts.registeredPersonCountPeriods = t.registeredPersonCountPeriods;
            }
            return new IntfResultObjectType<custom_personalAccount>(personalAccounts);
        }

        public IntfResultObjectType<List<measurementReading>> SelectMeasurementReadings(
    IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            List<measurementReading> measurementReadings = new List<measurementReading>();
            DataTable measurementReading = new DataTable();
            StringBuilder sql = new StringBuilder();
            string pref = request.entity.pref;
            int nzp_kvar = request.entity.nzp_kvar;
            DateTime d1 = new DateTime(request.entity.year, request.entity.month, 1);
            DateTime d2 = d1.AddMonths(1);

            sql.Remove(0, sql.Length);
            sql.Append("SELECT ");
            sql.Append("cs.nzp_serv, ");
            sql.Append("cs.num_cnt, ");
            sql.Append("sct.cnt_stage, ");
            sql.Append("sct.mmnog, ");
            sql.Append("ct.val_cnt, ");
            sql.Append("ct.dat_uchet ");
            sql.Append("FROM ");
            sql.Append("zel_data: counters_spis cs, ");
            sql.Append("zel_kernel: s_counttypes sct, ");
            sql.Append("zel_data: counters ct ");
            sql.Append("WHERE ");
            sql.Append("cs.nzp = " + request.entity.nzp_kvar + " ");
            sql.Append("AND cs.nzp_type = 3 ");
            sql.Append("AND sct.nzp_cnttype = cs.nzp_cnttype ");
            sql.Append("AND cs.is_actual <> 100 ");
            sql.Append("AND ct.is_actual <> 100 ");
            sql.Append("AND ct.dat_close IS NULL;");

            DataTable dt = OpenSql(connectionID, sql.ToString());
            if (dt.Rows.Count != 0)
            {
                measurementReading = measurementReading.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
            }
            measurementReadings = OrmConvert.ConvertDataRows(measurementReading.Rows, EPaspDataConverter.toMeasurementReading);

            return new IntfResultObjectType<List<measurementReading>>(measurementReadings);
        }

        //управляющая организация
        public IntfResultObjectType<managingOrganization> SelectManagingOrganization(
           IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            managingOrganization manOrgs = new managingOrganization();
            DataTable dtManOrgs = new DataTable();
            StringBuilder sql = new StringBuilder();
            string pref = request.entity.pref;
            int month = request.entity.month;
            int year = request.entity.year;

            List<_Point> prefixs = new List<_Point>();
            prefixs = Points.PointList;

            ExecSQL(connectionID, "DROP TABLE manorg", false);

            sql.Remove(0, sql.Length);
            sql.Append("CREATE TEMP TABLE manorg ");
            sql.Append("(nzp_area INTEGER, ");
            sql.Append("ceo CHAR(200)) WITH NO LOG;");

            if (!ExecSQL(connectionID, sql.ToString(), true).result)
            {
                connectionID.Close();
                MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                return null;
            }

            foreach (var points in prefixs)
            {
                sql.Remove(0, sql.Length);
                sql.Append("INSERT INTO manorg ");
                sql.Append("SELECT nzp_area, ");
                sql.Append("(select max(val_prm) from " + points.pref + "_data: prm_7 where nzp_prm = 576 and nzp = nzp_area and is_actual <> 100 and mdy(" + month + ",1," + year + ") between dat_s and dat_po and val_prm = 1) as ceo, ");
                sql.Append("'" + points.pref + "' ");
                sql.Append("FROM " + points.pref + "_data: s_area;");
                if (!ExecSQL(connectionID, sql.ToString(), true).result)
                {
                    connectionID.Close();
                    MonitorLog.WriteLog("Ошибка ", MonitorLog.typelog.Error, true);
                    return null;
                }
            }

            sql.Remove(0, sql.Length);
            sql.Append("SELECT * FROM manorg");
            DataTable dt = OpenSql(connectionID, sql.ToString());
            if (dt.Rows.Count != 0)
            {
                dtManOrgs = dtManOrgs.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
            }
            manOrgs = OrmConvert.ConvertDataRows(dtManOrgs.Rows, EPaspDataConverter.toManagingOrganization)[0];
            return new IntfResultObjectType<managingOrganization>(manOrgs);
        }

        public IntfResultObjectType<List<mabService>> SelectMabServiceList(
            IntfRequestType request, IDbConnection connectionID)
        {
#if PG
            string _sql =
                           " select nzp_serv, service from services";
#else
            string _sql =
                           " select nzp_serv, service from services";
#endif
            //Пример заполнение списка через DataTable
            DataTable data = new DataTable();
            {
                DataTable dt = OpenSql(connectionID, _sql);
                List<mabService> list = OrmConvert.ConvertDataRows(dt.Rows, EPaspDataConverter.ToMabService);
            }

            //Пример заполнение списка с помощью DataReader
            {
                List<mabService> list = ExecRead(connectionID, _sql, EPaspDataConverter.ToMabService);
                return new IntfResultObjectType<List<mabService>>(list);
            }
        }

        //Лицо, оказывающее услуги по содержанию и ремонту общего имущества МКД
        public IntfResultObjectType<mabHousingServiceProvider> SelectHousingServiceProvider(
       IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabHousingServiceProvider t = new mabHousingServiceProvider();

            t.serviceName = null; //Наименование услуги

            t.serviceCode = null; //Код услуги

            t.serviceProvider = new buildingServiceProvider(); //поставщик услуги

            t.supplyStartDate = null;  //Дата начала обслуживания дома

            t.supplyEndDate = null; //Дата прекращения обслуживания дома

            return new IntfResultObjectType<mabHousingServiceProvider>(t);
        }

        //Общие сведения и техническое состояние МКД
        public IntfResultObjectType<mabParameters> SelectParametrs(
       IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabParameters t = new mabParameters();

            t.nearHouseTerritoryAreaBySurfaceType = new mabNearHouseTerritorySurfaceTypeArea[0]; // Площадь придомовой территории по типу покрытия

            t.landArea = new mabLandArea(); //Информация по площади земельного участка

            t.technicalSummary = new mabTechnicalSummary(); //Техническая информация

            t.indoorArea = new mabIndoorArea(); //Данные по внутренней площади

            t.communityPropertyObjects = new mabCommunityPropertyObject[0]; //Объекты общего имущества

            t.communityPropertyRooms = new mabCommunityPropertyRoom[0]; //Помещения, относящееся к общему долевому имуществу собственников помещений, кроме мест общего пользования

            t.heatingEquipment = new mabHeatingEquipment(); //Оборудование по отоплению, размещенное на внутридомовых инженерных системах

            t.coldWaterEquipment = new mabColdWaterEquipment(); //Оборудование по холодному водоснабжению, размещенное на внутридомовых инженерных системах

            t.hotWaterEquipment = new mabHotWaterEquipment(); //Оборудование по горячему водоснабжению, размещенное на внутридомовых инженерных системах

            t.electricEquipment = new mabElectricEquipment(); //Оборудование по электроснабжению, размещенное на внутридомовых инженерных системах

            t.elevators = new mabElevator[0]; //Лифты

            t.otherEquipment = new mabOtherEquipment(); //Другое оборудование, размещенное на внутридомовых инженерных системах

            t.customEquipmentItems = new mabCustomEquipment[0]; //Иное оборудование, размещенное на внутридомовых инженерных системах

            t.premiseSummary = new mabPremiseSummary(); //Общие сведения о помещениях

            t.flatCountByOwnershipType = new mabFlatCount[0]; //Количество квартир по типу собственности

            t.energyEfficiency = new mabEnergyEfficiency(); //Данные по энергоэффективности

            t.maxEnergyConsumption = new mabMaxEnergyConsumption(); //Характеристики максимального энергопотребления

            t.energyConsumption = new mabEnergyConsumption(); //Энергопотребление


            return new IntfResultObjectType<mabParameters>(t);
        }

        /*
        //Лица, оказывающие коммунальные услуги в МКД
        public IntfResultObjectType<List<mabCommunalServiceProvider>> SelectCommunalServiceProvider(
         IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            List<mabCommunalServiceProvider> t = new List<mabCommunalServiceProvider>();
            DataTable ResourceSupplyOrganization = new DataTable();
            StringBuilder sql = new StringBuilder();
            string pref = request.entity.pref;
            int month = request.entity.month;
            int year = request.entity.year;

            sql.Remove(0, sql.Length);
            sql.Append(" ");

            DataTable dt = OpenSql(connectionID, sql.ToString());
            if (dt.Rows.Count != 0)
            {
                ResourceSupplyOrganization = ResourceSupplyOrganization.AsEnumerable().Union(dt.AsEnumerable()).CopyToDataTable();
            }
            t = OrmConvert.ConvertDataRows(ResourceSupplyOrganization.Rows, EPaspDataConverter.toCommunalServiceProvider);
            return new IntfResultObjectType<List<mabCommunalServiceProvider>>(t);
        }
        */



















        //Нежилое помещение -- typek=3 in kvar
        public IntfResultObjectType<nonresidentialPremise> SelectnonresidentialPremise(
      IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            nonresidentialPremise t = new nonresidentialPremise();

            t.uniqueNumber = 0; //Уникальный в рамках дома номер

            t.entrance = null; //Подъезд

            t.floor = 0; //Этаж

            t.totalArea = 0; //общая площадь, м2

            t.comment = null; //Комментарий

            t.resourceInputs = new resourceInput[0]; //Места ввода инженерных систем для подачи в помещение ресурсов, необходимых для предоставления коммунальных услуг -----------

            t.personalAccount = new personalAccount(); //Лицевой счет

            t.measurementReadings = new measurementReading[0]; //Показания счетчиков

            return new IntfResultObjectType<nonresidentialPremise>(t);
        }

        //место ввода инженерных систем для подачи в помещение ресурсов, необходимых для предоставления коммунальных услуг< --------------------
        public IntfResultObjectType<resourceInput> SelectresourceInput(
        IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            resourceInput t = new resourceInput();

            t.communalResource = new communalResource(); //Коммунальный ресурс

            t.description = null; //Описание

            t.controlMeasuringDevice = new controlMeasuringDevice(); //Прибор учета

            return new IntfResultObjectType<resourceInput>(t);
        }

        //Объем предоставленных жильцам коммунальных ресурсов (с учетом общедомовых нужд)     ? за месяц
        public IntfResultObjectType<mabProvidedCommunalResource> SelectmabProvidedCommunalResource(
    IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabProvidedCommunalResource t = new mabProvidedCommunalResource();

            t.communalResource = new communalResource(); //Коммунальный ресурс

            t.amount = 0; //Объем ресурса

            t.price = 0; //Оплата поступившая от жильцов

            return new IntfResultObjectType<mabProvidedCommunalResource>(t);
        }

        //Поставленный РCО объем коммунального ресурса    = предыдущему
        public IntfResultObjectType<mabConsumedCommunalResource> SelectConsumedCommunalResource(
    IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabConsumedCommunalResource t = new mabConsumedCommunalResource();

            t.communalResource = new communalResource(); //Коммунальный ресурс

            t.amount = 0; //Объем ресурса      

            return new IntfResultObjectType<mabConsumedCommunalResource>(t);
        }

        //Показания счетчика 
        public IntfResultObjectType<measurementReading> SelectmeasurementReading(
   IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            measurementReading t = new measurementReading();

            t.communalResource = new communalResource(); //Коммунальный ресурс

            t.measuringDeviceNumber = null; //Номер счетчика

            t.measuringDeviceCapacity = 0; //Разрядность счетчика

            t.transformationCoefficient = 0; // Коэффициент трансформации

            t.transformationCoefficientSpecified = false; //наличие

            t.indiccurBeginDay = 0; //Дневные показания на начало периода

            t.indiccurEndDay = 0; ; //Дневные показания на конец периода

            t.indiccurBeginNight = 0; //Ночные показания на начало периода

            t.indiccurEndNight = 0; //Ночные показания на конец периода

            t.indiccurBeginPeak = 0;//Пиковые показания на начало периода

            t.indiccurEndPeak = 0; //Пиковые показания на конец периода

            return new IntfResultObjectType<measurementReading>(t);
        }

        //Недопоставка коммунального ресурса nedop_kvar
        public IntfResultObjectType<communalShortDelivery> SelectcommunalShortDelivery(
 IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            communalShortDelivery t = new communalShortDelivery();

            t.communalResource = new communalResource(); //Коммунальный ресурс

            return new IntfResultObjectType<communalShortDelivery>(t);
        }

        //Недопоставка жилищной услуги  ndeop_kvar
        public IntfResultObjectType<mabHousingShortDelivery> SelectHousingShortDelivery(
 IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabHousingShortDelivery t = new mabHousingShortDelivery();

            t.code = null; //Код услуги

            return new IntfResultObjectType<mabHousingShortDelivery>(t);
        }

        //Общие сведения о недопоставвках по МКД ------------
        public IntfResultObjectType<mabShortDeliverySummary> SelectShortDeliverySummary(
 IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabShortDeliverySummary t = new mabShortDeliverySummary();

            t.maintenanceShortDeliveryFeeReductionCount = 0; //количество случаев снижения платы за нарушения качества содержания и
            // ремонта общего имущества в многоквартирном доме за отчетный период

            t.communalResourceShortDeliveryFeeReductionCount = 0;//количество случаев снижения платы за нарушения качества коммунальных услуг и (или)
            // за превышение установленной продолжительности перерывов в их оказании за отчетный период

            return new IntfResultObjectType<mabShortDeliverySummary>(t);
        }

        //Сведения об оказываемых коммунальных услугах по коммунальному ресурсу с разделением для населения / не для населения typek=3 - не для населения
        public IntfResultObjectType<communalResourceServices> SelectcommunalResourceServices(
IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            communalResourceServices t = new communalResourceServices();

            t.communalResource = new communalResource();  //Коммунальный ресурс

            t.forPopulation = false; //Для населения / не для населения

            t.singleTariffNormAmount = 0; //Размер нормы потребления, по которой разделяются тарифы для однотарифного счетчика. Если разделение или услуга не используется, то заполнять 0 --

            t.dayTariffNormAmount = 0; //Размер нормы потребления, по которой разделяются тарифы дневного потребления. Если разделение или услуга не используется, то заполнять 0 -- 

            t.nightTariffNormAmount = 0; //Размер нормы потребления, по которой разделяются тарифы для ночного потребления. Если разделение или услуга не используется, то заполнять 0 -- 

            t.peakTariffNormAmount = 0; //Размер нормы потребления, по которой разделяются тарифы для пикового потребления. Если разделение или услуга не используется, то заполнять 0 --

            t.communalServices = new communalService[0]; //Оказываемые коммунальные услуги 

            return new IntfResultObjectType<communalResourceServices>(t);
        }

        //Сведения об оказываемой жилищной услуге          не коммунальные 
        public IntfResultObjectType<housingService> SelecthousingService(
IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            housingService t = new housingService();
            t.name = null; //Наименование

            t.code = null; //>Код услуги<

            t.measureUnitCode = null; //>Код единицы измерения по ОКЕИ

            t.tariff = new housingServiceTariff(); //Тариф

            return new IntfResultObjectType<housingService>(t);
        }


        //Выполненная работа по аварийному ремонту ------
        public IntfResultObjectType<providedEmergencyRepairTask> SelectprovidedEmergencyRepairTask(
IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            providedEmergencyRepairTask t = new providedEmergencyRepairTask();

            t.taskCode = 0; //Код работы по аварийному ремонту

            t.taskName = null; //Наименование работы по аварийному ремонту

            t.repairTaskCategory = null; //>Категория работ по ремонту дома (элемент здания, система)<

            t.comment = null; //Комментарий

            t.price = 0; //Стоимость, руб

            return new IntfResultObjectType<providedEmergencyRepairTask>(t);
        }

        //Выполненная работа по МКД ---------
        public IntfResultObjectType<mabProvidedService> SelectmabProvidedService(
IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabProvidedService t = new mabProvidedService();
            t.serviceCode = null; //Код услуги

            t.serviceName = null; //Наименование услуги

            t.measureUnitCode = null; //Код единицы измерения по ОКЕИ

            t.amount = 0; //Объем

            t.comment = null; //Комментарий

            t.price = 0; //Стоимость, руб

            return new IntfResultObjectType<mabProvidedService>(t);
        }


        //Осмотр многоквартирного дома -----------
        public IntfResultObjectType<buildingSurvey> SelectbuildingSurvey(
IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            buildingSurvey t = new buildingSurvey();
            t.repairTaskCategory = null; //Категория работ по ремонту дома (элемент здания, система)

            t.date = null; //дата подписания акта осмотра

            t.tearPercent = 0; //процент износа

            t.buildingSurveyBasis = null; //Основание осмотра многоквартирного дома

            t.inspectors = new buildingSurveyInspector[0]; //Лица, проводящие осмотр

            t.majorRepairTasks = new mabMajorRepairTask[0]; //Работы по капитальному ремонту, выявленная по результатам осмотра

            return new IntfResultObjectType<buildingSurvey>(t);
        }

        //Работа, включенная в план работ по дому ------------
        public IntfResultObjectType<buildingMajorRepairPlanTask> SelectbuildingMajorRepairPlanTask(
IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            buildingMajorRepairPlanTask t = new buildingMajorRepairPlanTask();

            t.taskNumber = 0; //Номер работы

            t.price = 0; //Стоимость выполнения работы, руб

            t.priceSpecified = false; //наличие

            return new IntfResultObjectType<buildingMajorRepairPlanTask>(t);
        }

        //выполненная работа по кап ремонту МКД ------------
        public IntfResultObjectType<mabMajorRepairDoneTask> SelectMajorRepairDoneTask(
IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabMajorRepairDoneTask t = new mabMajorRepairDoneTask();
            t.taskNumber = 0; //Номер работы

            t.finalPrice = 0; //Сумма, за которую выполнена, руб

            t.fundingSources = new mabMajorRepairTaskFundingSource[0]; //Источники финансирования

            t.year = 0; //Год, в котором выполнена

            return new IntfResultObjectType<mabMajorRepairDoneTask>(t);
        }

        //работа по кап ремонту МКД с отказом выполнения -----------------
        public IntfResultObjectType<mabMajorRepairRefusedTask> SelectMajorRepairRefusedTask(
IntfRequestObjectType<EPaspReqPrm> request, IDbConnection connectionID)
        {
            mabMajorRepairRefusedTask t = new mabMajorRepairRefusedTask();
            t.taskNumber = 0;  //Номер работы

            return new IntfResultObjectType<mabMajorRepairRefusedTask>(t);
        }
    }
}
