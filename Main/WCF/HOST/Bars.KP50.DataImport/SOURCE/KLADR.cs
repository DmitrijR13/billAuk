using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using Globals.SOURCE.Utility;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbKladr : DbAdminClient
    {

        public int loadId { get; set; }

        private readonly IDbConnection conDb;

        public DbKladr(IDbConnection _conDb)
        {
            conDb = _conDb;
            loadId = -1;
        }

        /// <summary>
        /// Загрузка адресного простанства из КЛАДР 
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns UploadKLADRAddrSpace(out Returns ret, KLADRFinder finder)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            Returns retvar = STCLINE.KP50.Global.Utils.InitReturns();

            int id = 0;
            var sqlStr = "";

            decimal nzp_city = 0;
            decimal nzp_raj = 0;
            decimal nzp_town = 0;
            decimal nzp_ul = 0;

            var region = finder.regionCode;
            var district = finder.districtCode;
            var city = finder.cityCode;
            try
            {
                try
                {
                    sqlStr =
                        " insert into " + Points.Pref + "_data" + tableDelimiter +
                        "upload_progress (date_upload, progress, upload_type) " +
                        " VALUES  ( " + DBManager.sCurDateTime + " , 0, 1 ) ";
                    retvar = ExecSQL(conDb, null, sqlStr, true);
                    if (!retvar.result)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка в функции UploadKLADRAddrSpace:\n", MonitorLog.typelog.Error, true);
                    }
                    id = Convert.ToInt32(ClassDBUtils.GetSerialKey(conDb, null));
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка в функции UploadKLADRAddrSpace:\n" + ex.Message + ex.StackTrace,
                        MonitorLog.typelog.Error, true);
                }

                #region очистка адресного пространства

                if (finder.clearAddrSpace)
                {
                    sqlStr =
                        " delete from " + Points.Pref + sDataAliasRest + "s_stat" +
                        " where not exists " +
                        " (select 1 from " + Points.Pref + sDataAliasRest + "s_town t" +
                        " where t.nzp_stat = " + Points.Pref + sDataAliasRest + "s_stat.nzp_stat); ";
                    retvar = ExecSQL(conDb, sqlStr, true);
                    if (!retvar.result)
                    {
                        ret.result = false;
                        return ret;
                    }
                    sqlStr =
                        " delete from " + Points.Pref + sDataAliasRest + "s_town" +
                        " where not exists" +
                        " (select 1 from " + Points.Pref + sDataAliasRest + "s_rajon r" +
                        " where r.nzp_town = " + Points.Pref + sDataAliasRest + "s_town.nzp_town); ";
                    retvar = ExecSQL(conDb, sqlStr, true);
                    if (!retvar.result)
                    {
                        ret.result = false;
                        return ret;
                    }
                    sqlStr =
                        " delete from " + Points.Pref + sDataAliasRest + "s_rajon" +
                        " where not exists " +
                        " (select 1 from " + Points.Pref + sDataAliasRest + "s_ulica u" +
                        " where u.nzp_raj = " + Points.Pref + sDataAliasRest + "s_rajon.nzp_raj); ";
                    retvar = ExecSQL(conDb, sqlStr, true);
                    if (!retvar.result)
                    {
                        ret.result = false;
                        return ret;
                    }
                    sqlStr =
                        " delete from " + Points.Pref + sDataAliasRest + "s_ulica" +
                        " where not exists " +
                        " (select 1 from " + Points.Pref + sDataAliasRest + "dom d" +
                        " where d.nzp_ul = " + Points.Pref + sDataAliasRest + "s_ulica.nzp_ul); " +
                        "";
                    retvar = ExecSQL(conDb, sqlStr, true);
                    if (!retvar.result)
                    {
                        ret.result = false;
                        return ret;
                    }
                    sqlStr = sUpdStat + " " + Points.Pref + sDataAliasRest + "s_stat";
                    ExecSQL(conDb, sqlStr, true);
                    sqlStr = sUpdStat + " " + Points.Pref + sDataAliasRest + "s_town";
                    ExecSQL(conDb, sqlStr, true);
                    sqlStr = sUpdStat + " " + Points.Pref + sDataAliasRest + "s_rajon";
                    ExecSQL(conDb, sqlStr, true);
                    sqlStr = sUpdStat + " " + Points.Pref + sDataAliasRest + "s_ulica";
                    ExecSQL(conDb, sqlStr, true);
                    foreach (var bank in Points.PointList)
                    {
                        sqlStr =
                            " delete from " + bank.pref + sDataAliasRest + "s_stat" +
                            " where not exists " +
                            " (select 1 from " + bank.pref + sDataAliasRest + "s_town t" +
                            " where t.nzp_stat = " + bank.pref + sDataAliasRest + "s_stat.nzp_stat);";
                        retvar = ExecSQL(conDb, sqlStr, true);
                        if (!retvar.result)
                        {
                            ret.result = false;
                            return ret;
                        }
                        sqlStr =
                            " delete from " + bank.pref + sDataAliasRest + "s_town" +
                            " where not exists" +
                            " (select 1 from " + bank.pref + sDataAliasRest + "s_rajon r" +
                            " where r.nzp_town = " + bank.pref + sDataAliasRest + "s_town.nzp_town); " +
                            "";
                        retvar = ExecSQL(conDb, sqlStr, true);
                        if (!retvar.result)
                        {
                            ret.result = false;
                            return ret;
                        }
                        sqlStr =
                            " delete from " + bank.pref + sDataAliasRest + "s_rajon" +
                            " where not exists " +
                            " (select 1 from " + bank.pref + sDataAliasRest + "s_ulica u" +
                            " where u.nzp_raj = " + bank.pref + sDataAliasRest + "s_rajon.nzp_raj); " +
                            "";
                        retvar = ExecSQL(conDb, sqlStr, true);
                        if (!retvar.result)
                        {
                            ret.result = false;
                            return ret;
                        }
                        sqlStr =
                            " delete from " + bank.pref + sDataAliasRest + "s_ulica" +
                            " where not exists " +
                            " (select 1 from " + bank.pref + sDataAliasRest + "dom d" +
                            " where d.nzp_ul = " + bank.pref + sDataAliasRest + "s_ulica.nzp_ul); " +
                            "";
                        retvar = ExecSQL(conDb, sqlStr, true);
                        if (!retvar.result)
                        {
                            ret.result = false;
                            return ret;
                        }
                        sqlStr = sUpdStat + " " + bank.pref + sDataAliasRest + "s_stat";
                        ExecSQL(conDb, sqlStr, true);
                        sqlStr = sUpdStat + " " + bank.pref + sDataAliasRest + "s_town";
                        ExecSQL(conDb, sqlStr, true);
                        sqlStr = sUpdStat + " " + bank.pref + sDataAliasRest + "s_rajon";
                        ExecSQL(conDb, sqlStr, true);
                        sqlStr = sUpdStat + " " + bank.pref + sDataAliasRest + "s_ulica";
                        ExecSQL(conDb, sqlStr, true);
                    }
                }

                #endregion

                #region Для выгрузки выбрана улица

                if (finder.level == "street")
                {
                    decimal nzp_stat = SaveRegion(finder.region);
                    if (nzp_stat == -1)
                    {
                        ret.result = false;
                        return ret;
                    }

                    if (finder.district != null)
                    {
                        nzp_town = SaveDistricrt(finder.district, nzp_stat);
                        if (nzp_town == -1)
                        {
                            ret.result = false;
                            return ret;
                        }
                    }
                    else if (finder.city == null)
                    {
                        var tmpObj = new KLADRData() {code = "-", fullname = "-"};
                        nzp_town = SaveDistricrt(tmpObj, nzp_stat);
                        if (nzp_town == -1)
                        {
                            ret.result = false;
                            return ret;
                        }
                    }

                    if (finder.city != null)
                    {
                        nzp_city = SaveCity(finder.city, nzp_stat);
                        if (nzp_city == -1)
                        {
                            ret.result = false;
                            return ret;
                        }
                    }

                    if (finder.settlement != null)
                    {
                        if (nzp_city != 0)
                        {
                            //if (!cbxIgnoreCityDistrict.Checked)
                            nzp_raj = SaveSettlement(finder.settlement, nzp_city);
                            if (nzp_raj == -1)
                            {
                                ret.result = false;
                                return ret;
                            }
                        }
                        if (nzp_city == 0)
                        {
                            nzp_raj = SaveSettlement(finder.settlement, nzp_town);
                            if (nzp_raj == -1)
                            {
                                ret.result = false;
                                return ret;
                            }
                        }
                    }

                    if (nzp_raj == 0)
                    {
                        var tmpObj = new KLADRData() {code = "-", fullname = "-"};
                        if (nzp_city != 0)
                        {
                            nzp_raj = SaveSettlement(tmpObj, nzp_city);
                            if (nzp_raj == -1)
                            {
                                ret.result = false;
                                return ret;
                            }
                        }
                        else
                        {
                            nzp_raj = SaveSettlement(tmpObj, nzp_town);
                            if (nzp_raj == -1)
                            {
                                ret.result = false;
                                return ret;
                            }
                        }

                        nzp_ul = SaveStreet(finder.street, nzp_raj);
                        if (nzp_raj == -1)
                        {
                            ret.result = false;
                            return ret;
                        }
                    }
                    else
                    {
                        nzp_ul = SaveStreet(finder.street, nzp_raj);
                        if (nzp_raj == -1)
                        {
                            ret.result = false;
                            return ret;
                        }
                    }
                }

                #endregion Для выгрузки выбрана улица

                #region Для выгрузки выбран населенный пункт

                if (finder.level == "settlement")
                {
                    decimal nzp_stat = SaveRegion(finder.region);

                    if (finder.district != null)
                    {
                        nzp_town = SaveDistricrt(finder.district, nzp_stat);
                    }

                    if (finder.city != null)
                    {
                        nzp_city = SaveCity(finder.city, nzp_stat);
                    }

                    SetProgress(id, "0.7");

                    if (nzp_city != 0)
                        nzp_raj = SaveSettlement(finder.settlement, nzp_city);
                    if (nzp_city == 0)
                        nzp_raj = SaveSettlement(finder.settlement, nzp_town);

                    if (finder.loadStreets)
                    {
                        foreach (var obj in finder.streetList)
                        {
                            if (nzp_raj == 0)
                            {
                                var tmpObj = new KLADRData() {code = "-", fullname = "-"};
                                if (nzp_city != 0)
                                    nzp_raj = SaveSettlement(tmpObj, nzp_city);
                                else
                                    nzp_raj = SaveSettlement(tmpObj, nzp_town);

                                SaveStreet(obj, nzp_raj);
                            }
                            else
                                SaveStreet(obj, nzp_raj);
                        }
                    }

                    SetProgress(id, "0.9");
                }

                #endregion Для выгрузки выбран населенный пункт

                #region Для выгрузки выбран город

                if (finder.level == "city")
                {
                    decimal nzp_stat = SaveRegion(finder.region);

                    if (finder.district != null)
                    {
                        nzp_town = SaveDistricrt(finder.district, nzp_stat);
                    }

                    nzp_city = SaveCity(finder.city, nzp_stat);

                    SetProgress(id, "0.3");

                    var tmpObj = new KLADRData() {code = "-", fullname = "-"};
                    nzp_raj = SaveSettlement(tmpObj, nzp_city);

                    if (finder.loadStreets)
                    {
                        for (int i = 0; i < finder.streetList.Count; i++)
                        {
                            SaveStreet(finder.streetList[i], nzp_raj);
                        }
                    }

                    SetProgress(id, "0.5");

                    if (finder.settlement != null)
                    {
                        for (int j = 0; j < finder.settlementList.Count; j++)
                        {
                            nzp_raj = SaveSettlement(finder.settlementList[j], nzp_city);

                            if (j == finder.settlementList.Count/4)
                                SetProgress(id, "0.6");

                            if (j == finder.settlementList.Count/2)
                                SetProgress(id, "0.7");

                            if (finder.loadStreets)
                            {
                                string query = " SELECT fullname, code, name, socr FROM " + Points.Pref +
                                               DBManager.sDataAliasRest + "kladr_street WHERE SUBSTR (CODE, 1 , 11) = '" +
                                               finder.settlementList[j].code.Trim() + "'";
                                foreach (
                                    DataRow dr in
                                        ClassDBUtils.OpenSQL(query, conDb, ClassDBUtils.ExecMode.Exception)
                                            .resultData.Rows)
                                {
                                    SaveStreet(new KLADRData()
                                    {
                                        fullname = dr["FULLNAME"].ToString(),
                                        code = dr["CODE"].ToString(),
                                        name = dr["NAME"].ToString(),
                                        socr = dr["SOCR"].ToString()
                                    }, nzp_raj);
                                }
                            }
                        }
                    }
                }

                #endregion Для выгрузки выбран город

                #region Для выгрузки выбран район

                if (finder.level == "district")
                {
                    List<KLADRData> tmpSettlementList = new List<KLADRData>();
                    List<KLADRData> tmpStreetList = new List<KLADRData>();

                    decimal nzp_stat = SaveRegion(finder.region);

                    nzp_town = SaveDistricrt(finder.district, nzp_stat);

                    SetProgress(id, "0.1");

                    if (finder.cityList != null)
                    {
                        for (int c = 0; c < finder.cityList.Count; c++)
                        {
                            if (c == finder.cityList.Count/4)
                                SetProgress(id, "0.3");
                            if (c == finder.cityList.Count/2)
                                SetProgress(id, "0.4");

                            nzp_city = SaveCity(finder.cityList[c], nzp_stat);
                            city = finder.cityList[c].code.Substring(5, 3);

                            tmpSettlementList.Clear();

                            tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                            {
                                query =
                                    "SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                    "kladr WHERE SUBSTR(CODE, 1, 2) = '" + region + "' AND SUBSTR (CODE, 3, 3) = '" +
                                    district + "' " +
                                    " AND SUBSTR (CODE, 6, 3) = '" + city + "' AND SUBSTR (CODE, 9, 3) <> '000'",
                                tableName = "KLADR.DBF"
                            }).returnsData;

                            var tmpObj = new KLADRData() {code = "-", fullname = "-"};
                            nzp_raj = SaveSettlement(tmpObj, nzp_city);

                            if (finder.loadStreets)
                            {
                                tmpStreetList.Clear();
                                tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                {
                                    query =
                                        "SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                        "kladr_street WHERE SUBSTR (CODE, 1 , 11) = '" + finder.cityList[c].code.Trim() + "'",
                                    level = "street",
                                    tableName = "STREET.DBF"
                                }).returnsData;
                                for (int i = 0; i < tmpStreetList.Count; i++)
                                {
                                    SaveStreet(tmpStreetList[i], nzp_raj);
                                }
                            }

                            if (tmpSettlementList.Count != 0)
                            {
                                for (int s = 0; s < tmpSettlementList.Count; s++)
                                {
                                    nzp_raj = SaveSettlement(tmpSettlementList[s], nzp_city);
                                    if (finder.loadStreets)
                                    {
                                        tmpStreetList.Clear();
                                        tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                        {
                                            query =
                                                "SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                                "kladr_street WHERE SUBSTR(CODE, 1 , 11) = '" +
                                                tmpSettlementList[s].code.Trim() + "'",
                                            level = "street",
                                            tableName = "STREET.DBF"
                                        }).returnsData;
                                        for (int i = 0; i < tmpStreetList.Count; i++)
                                        {
                                            SaveStreet(tmpStreetList[i], nzp_raj);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    SetProgress(id, "0.5");

                    tmpSettlementList.Clear();
                    tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                    {
                        query =
                            "SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                            "kladr WHERE SUBSTR (CODE, 1, 2) = '" + region + "' AND SUBSTR(CODE, 3, 3) = '" + district +
                            "' " +
                            " AND SUBSTR(CODE, 6, 3) = '000' AND SUBSTR (CODE, 9, 3) <> '000'",
                        tableName = "KLADR.DBF"
                    }).returnsData;

                    for (int j = 0; j < tmpSettlementList.Count; j++)
                    {
                        nzp_raj = SaveSettlement(tmpSettlementList[j], nzp_town);

                        if (j == tmpSettlementList.Count/4)
                            SetProgress(id, "0.7");
                        if (j == tmpSettlementList.Count/2)
                            SetProgress(id, "0.8");

                        if (finder.loadStreets)
                        {
                            tmpStreetList.Clear();
                            tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                            {
                                query =
                                    "SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                    "kladr_street WHERE SUBSTR (CODE, 1 , 11) = '" + tmpSettlementList[j].code.Trim() + "'",
                                level = "street",
                                tableName = "STREET.DBF"
                            }).returnsData;

                            for (int i = 0; i < tmpStreetList.Count; i++)
                            {
                                SaveStreet(tmpStreetList[i], nzp_raj);
                            }
                        }
                    }
                }

                #endregion Для выгрузки выбран район

                #region Для выгрузки выбран регион

                if (finder.level == "region")
                {
                    decimal nzp_stat = SaveRegion(finder.region);

                    List<KLADRData> tmpCityList = new List<KLADRData>();
                    List<KLADRData> tmpSettlementList = new List<KLADRData>();
                    List<KLADRData> tmpStreetList = new List<KLADRData>();

                    SetProgress(id, "0.1");

                    #region сохранение улиц, принадлежащих региону

                    if (finder.loadStreets && finder.streetList != null && finder.streetList.Count != 0)
                    {
                        var tmpObj = new KLADRData() {code = "-", fullname = "-"};

                        nzp_town = SaveDistricrt(tmpObj, nzp_stat);
                        nzp_raj = SaveSettlement(tmpObj, nzp_town);

                        for (int f = 0; f < finder.streetList.Count; f++)
                        {
                            SaveStreet(finder.streetList[f], nzp_raj);
                        }
                    }

                    #endregion сохранение улиц, принадлежащих региону

                    for (int c = 0; c < finder.cityList.Count; c++)
                    {
                        nzp_city = SaveCity(finder.cityList[c], nzp_stat);
                        city = finder.cityList[c].code.Substring(5, 3);

                        tmpSettlementList.Clear();
                        tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                        {
                            query =
                                " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                "kladr WHERE SUBSTR(CODE, 1, 2) = '" + region + "' AND SUBSTR(CODE, 3, 3) = '000' " +
                                " AND SUBSTR(CODE, 6, 3) = '" + city + "' AND SUBSTR(CODE, 9, 3) <> '000'",
                            tableName = "KLADR.DBF"
                        }).returnsData;

                        var tmpObj = new KLADRData() {code = "-", fullname = "-"};
                        nzp_raj = SaveSettlement(tmpObj, nzp_city);

                        if (finder.loadStreets)
                        {
                            tmpStreetList.Clear();
                            tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                            {
                                query =
                                    " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                    "kladr_street WHERE SUBSTR(CODE, 1 , 11) = '" + finder.cityList[c].code.Trim() + "'",
                                level = "street",
                                tableName = "STREET.DBF"
                            }).returnsData;
                            for (int i = 0; i < tmpStreetList.Count; i++)
                            {
                                SaveStreet(tmpStreetList[i], nzp_raj);
                            }
                        }

                        if (tmpSettlementList.Count != 0)
                        {
                            for (int s = 0; s < tmpSettlementList.Count; s++)
                            {
                                nzp_raj = SaveSettlement(tmpSettlementList[s], nzp_city);
                                if (finder.loadStreets)
                                {
                                    tmpStreetList.Clear();
                                    tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                    {
                                        query =
                                            " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                            "kladr_street WHERE SUBSTR (CODE, 1 , 11) = '" + tmpSettlementList[s].code.Trim() +
                                            "'",
                                        level = "street",
                                        tableName = "STREET.DBF"
                                    }).returnsData;
                                    for (int i = 0; i < tmpStreetList.Count; i++)
                                    {
                                        SaveStreet(tmpStreetList[i], nzp_raj);
                                    }
                                }
                            }
                        }
                    }

                    SetProgress(id, "0.3");

                    for (int d = 0; d < finder.districtList.Count; d++)
                    {
                        if (d == finder.districtList.Count/8)
                            SetProgress(id, "0.4");
                        if (d == finder.districtList.Count/4)
                            SetProgress(id, "0.6");
                        if (d == finder.districtList.Count/2)
                            SetProgress(id, "0.7");
                        if (d == finder.districtList.Count*3/4)
                            SetProgress(id, "0.8");


                        nzp_town = SaveDistricrt(finder.districtList[d], nzp_stat);

                        district = finder.districtList[d].code.Substring(2, 3);
                        tmpCityList.Clear();
                        tmpCityList = LoadDataFromKLADR(new KLADRFinder()
                        {
                            query =
                                " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                "kladr WHERE SUBSTR (CODE, 1, 2) = '" + region + "' AND  SUBSTR (CODE, 3, 3) = '" +
                                district + "' " +
                                " AND  SUBSTR (CODE, 9, 3) = '000' AND  SUBSTR (CODE, 6, 3) <> '000' ",
                            tableName = "KLADR.DBF"
                        }).returnsData;

                        for (int c = 0; c < tmpCityList.Count; c++)
                        {
                            nzp_city = SaveCity(tmpCityList[c], nzp_stat);
                            city = tmpCityList[c].code.Substring(5, 3);

                            var tmpObj = new KLADRData() {code = "-", fullname = "-"};
                            nzp_raj = SaveSettlement(tmpObj, nzp_city);

                            if (finder.loadStreets)
                            {
                                tmpStreetList.Clear();
                                tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                {
                                    query =
                                        " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                        "kladr_street WHERE SUBSTR(CODE, 1 , 11) = '" + tmpCityList[c].code.Trim() + "'",
                                    level = "street",
                                    tableName = "STREET.DBF"
                                }).returnsData;
                                for (int i = 0; i < tmpStreetList.Count; i++)
                                {
                                    SaveStreet(tmpStreetList[i], nzp_raj);
                                }
                            }

                            tmpSettlementList.Clear();
                            tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                            {
                                query =
                                    " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                    "kladr WHERE SUBSTR(CODE, 1, 2) = '" + region + "' AND  SUBSTR(CODE, 3, 3) = '" +
                                    district + "' " +
                                    " AND  SUBSTR(CODE, 6, 3) = '" + city + "' AND  SUBSTR(CODE, 9, 3) <> '000'",
                                tableName = "KLADR.DBF"
                            }).returnsData;

                            if (tmpSettlementList.Count != 0)
                            {
                                for (int s = 0; s < tmpSettlementList.Count; s++)
                                {
                                    nzp_raj = SaveSettlement(tmpSettlementList[s], nzp_city);
                                    if (finder.loadStreets)
                                    {
                                        tmpStreetList.Clear();
                                        tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                        {
                                            query =
                                                " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                                "kladr_street WHERE SUBSTR(CODE, 1 , 11) = '" +
                                                tmpSettlementList[s].code.Trim() + "'",
                                            level = "street",
                                            tableName = "STREET.DBF"
                                        }).returnsData;
                                        for (int i = 0; i < tmpStreetList.Count; i++)
                                        {
                                            SaveStreet(tmpStreetList[i], nzp_raj);
                                        }
                                    }
                                }
                            }
                        }

                        tmpSettlementList.Clear();
                        tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                        {
                            query =
                                " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                "kladr WHERE SUBSTR(CODE, 1, 2) = '" + region + "' AND  SUBSTR(CODE, 3, 3) = '" +
                                district + "' " +
                                " AND  SUBSTR(CODE, 6, 3) = '000' AND  SUBSTR(CODE, 9, 3) <> '000'",
                            tableName = "KLADR.DBF"
                        }).returnsData;

                        for (int j = 0; j < tmpSettlementList.Count; j++)
                        {
                            nzp_raj = SaveSettlement(tmpSettlementList[j], nzp_town);

                            if (finder.loadStreets)
                            {
                                tmpStreetList.Clear();
                                tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                {
                                    query =
                                        " SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest +
                                        "kladr_street WHERE SUBSTR (CODE, 1 , 11) = '" + tmpSettlementList[j].code.Trim() + "'",
                                    level = "street",
                                    tableName = "STREET.DBF"
                                }).returnsData;

                                for (int i = 0; i < tmpStreetList.Count; i++)
                                {
                                    SaveStreet(tmpStreetList[i], nzp_raj);
                                }
                            }
                        }
                    }
                }

                #endregion регион

                //запись в базу об окончании загрузки
                SetProgress(id, "1");
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции UploadKLADRAddrSpace:\n" + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                SetProgress(id, "-0.01");
            }
            return ret;
        }

        //обновление прогресса загрузки
        private void SetProgress(int id, string progress)
        {
            var sql = " update " + Points.Pref + "_data" + DBManager.tableDelimiter + "upload_progress set progress = " +
                      progress + " where id = " + id;
            DBManager.ExecSQL(conDb, sql, true);
        }


        private decimal SaveRegion(KLADRData obj)
        {
            decimal nzp_stat = 0;
            var upper_bank = Points.Pref;
            var points = Points.PointList;
            Returns retvar = Utils.InitReturns();

            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();

            //работа с верхним банком
            string sqlString = "SELECT * FROM " + upper_bank + "_data" + tableDelimiter + "s_stat where soato = '" +
                               selectedCode + "'";

            if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
            {
                //обновить
                sqlString = " UPDATE " + upper_bank + "_data" + tableDelimiter + "s_stat SET ( stat, stat_t ) = " +
                            " ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" +
                            selectedCode + "'";
                retvar = ExecSQL(conDb, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + retvar.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                sqlString = "SELECT nzp_stat FROM " + upper_bank + "_data" + tableDelimiter + "s_stat WHERE soato = '" +
                            selectedCode + "'";
                nzp_stat = Convert.ToDecimal(ExecScalar(conDb, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                try
                {
                    sqlString = "INSERT INTO " + upper_bank + "_data" + tableDelimiter +
                                "s_stat ( stat, stat_t, nzp_land, soato ) VALUES ( '" + selectedFullname.Trim() + "', '" +
                                selectedFullname.Trim() + "' , '1' , '" + selectedCode.Trim() + "')";
                    retvar = ExecSQL(conDb, null, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + retvar.text, MonitorLog.typelog.Error,
                            true);
                        return -1;
                    }
                    nzp_stat = ClassDBUtils.GetSerialKey(conDb, null);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + ex.Message + ex.StackTrace,
                        MonitorLog.typelog.Error, true);
                    return -1;
                }
            }

            //работа с локальными банками
            foreach (var bank in points)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data" + tableDelimiter + "s_stat where soato = '" +
                               selectedCode + "'";
                if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
                {
                    //обновить
                    sqlString = " UPDATE " + bank.pref + "_data" + tableDelimiter + "s_stat SET ( stat, stat_t ) = " +
                                " ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" +
                                selectedCode + "'";
                    retvar = ExecSQL(conDb, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                   
                }
                else
                {
                    //добавить
                    try
                    {
                        sqlString = "INSERT INTO " + bank.pref + "_data" + tableDelimiter + "s_stat " +
                                    " SELECT * FROM " + Points.Pref + "_data" + tableDelimiter + "s_stat " +
                                    " WHERE soato = '" + selectedCode + "'";
                        retvar = ExecSQL(conDb, null, sqlString, true);
                        if (!retvar.result)
                        {
                            MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + retvar.text, MonitorLog.typelog.Error,
                                true);
                            return -1;
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }

            return nzp_stat;
        }

        private decimal SaveDistricrt(KLADRData obj, decimal nzp_stat)
        {
            decimal nzp_town = 0;
            var upper_bank = Points.Pref;
            var points = Points.PointList;
            Returns retvar = Utils.InitReturns();

            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();

            //работа с верхним банком
            string sqlString = "SELECT * FROM " + upper_bank + "_data" + tableDelimiter + "s_town where soato = '" +
                               selectedCode + "'";

            if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data" + tableDelimiter +
                            "s_town SET ( town, town_t, nzp_stat ) = " +
                            "( '" + selectedFullname + "' , '" + selectedFullname + "', '" + nzp_stat.ToString() +
                            "' ) where soato = '" + selectedCode + "'";
                retvar = ExecSQL(conDb, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + retvar.text, MonitorLog.typelog.Error,
                        true);
                    return -1;
                }
                sqlString = "SELECT nzp_town FROM " + upper_bank + "_data" + tableDelimiter + "s_town WHERE soato = '" +
                            selectedCode + "'";
                nzp_town = Convert.ToDecimal(ExecScalar(conDb, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                try
                {
                    sqlString = "INSERT INTO " + upper_bank + "_data" + tableDelimiter +
                                "s_town ( town, town_t, nzp_stat, soato ) VALUES " +
                                "( '" + selectedFullname.Trim() + "', '" + selectedFullname.Trim() + "' , '" + nzp_stat.ToString() +
                                "' , '" + selectedCode.Trim() + "')";
                    retvar = ExecSQL(conDb, null, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + retvar.text, MonitorLog.typelog.Error,
                            true);
                        return -1;
                    }
                    nzp_town = ClassDBUtils.GetSerialKey(conDb, null);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + ex.Message + ex.StackTrace,
                        MonitorLog.typelog.Error, true);
                    return -1;
                }
            }

            //работа с локальными банками
            foreach (var bank in points)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data" + tableDelimiter + "s_town where soato = '" +
                               selectedCode + "'";

                if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data" + tableDelimiter +
                                "s_town SET ( town, town_t, nzp_stat ) = " +
                                "( '" + selectedFullname + "' , '" + selectedFullname + "', '" + nzp_stat.ToString() +
                                "' ) where soato = '" + selectedCode + "'";
                    retvar = ExecSQL(conDb, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + retvar.text, MonitorLog.typelog.Error,
                            true);
                        return -1;
                    }
                    
                }
                else
                {
                    //добавить
                    try
                    {
                        sqlString = "INSERT INTO " + bank.pref + "_data" + tableDelimiter + "s_town " +
                                    " SELECT * FROM " + Points.Pref + "_data" + tableDelimiter + "s_town " +
                                    " WHERE soato = '" + selectedCode + "'" +
                                    " AND nzp_stat = " + nzp_stat;
                        retvar = ExecSQL(conDb, null, sqlString, true);
                        if (!retvar.result)
                        {
                            MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + retvar.text, MonitorLog.typelog.Error,
                                true);
                            return -1;
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }

            return nzp_town;
        }

        private decimal SaveCity(KLADRData obj, decimal nzp_stat)
        {
            decimal nzp_town = 0;
            var upper_bank = Points.Pref;
            var points = Points.PointList;
            Returns retvar = Utils.InitReturns();

            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data" + tableDelimiter + "s_town where soato = '" +
                               selectedCode + "'";

            if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data" + tableDelimiter +
                            "s_town SET ( town, town_t, nzp_stat ) = " +
                            " ( '" + selectedFullname + "' , '" + selectedFullname + "', '" + nzp_stat.ToString() +
                            "' ) where soato = '" + selectedCode + "'";
                retvar = ExecSQL(conDb, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + retvar.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                sqlString = "SELECT nzp_town FROM " + upper_bank + "_data" + tableDelimiter + "s_town WHERE soato = '" +
                            selectedCode + "'";
                nzp_town = Convert.ToDecimal(ExecScalar(conDb, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                try
                {
                    sqlString = "INSERT INTO " + upper_bank + "_data" + tableDelimiter +
                                "s_town ( town, town_t, nzp_stat, soato ) VALUES " +
                                " ( '" + selectedFullname.Trim() + "', '" + selectedFullname.Trim() + "' , '" + nzp_stat.ToString() +
                                "' , '" + selectedCode.Trim() + "')";
                    retvar = ExecSQL(conDb, null, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    nzp_town = ClassDBUtils.GetSerialKey(conDb, null);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + ex.Message + ex.StackTrace,
                        MonitorLog.typelog.Error, true);
                    return -1;
                }
            }

            //работа с локальными банками
            foreach (var bank in points)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data" + tableDelimiter + "s_town where soato = '" +
                               selectedCode + "'";

                if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data" + tableDelimiter +
                                "s_town SET ( town, town_t, nzp_stat ) = " +
                                " ( '" + selectedFullname + "' , '" + selectedFullname + "', '" + nzp_stat.ToString() +
                                "' ) where soato = '" + selectedCode + "'";
                    retvar = ExecSQL(conDb, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    
                }
                else
                {
                    //добавить
                    try
                    {
                        sqlString = "INSERT INTO " + bank.pref + "_data" + tableDelimiter + "s_town " +
                                    " SELECT * FROM " + Points.Pref + "_data" + tableDelimiter + "s_town " +
                                    " WHERE soato = '" + selectedCode + "'" +
                                    " AND nzp_stat = " + nzp_stat;
                        retvar = ExecSQL(conDb, null, sqlString, true);
                        if (!retvar.result)
                        {
                            MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + retvar.text, MonitorLog.typelog.Error, true);
                            return -1;
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }

            return nzp_town;
        }

        private decimal SaveSettlement(KLADRData obj, decimal nzp_town)
        {
            decimal nzp_raj = 0;
            var upper_bank = Points.Pref;
            var points = Points.PointList;
            Returns retvar = Utils.InitReturns();


            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data" + tableDelimiter + "s_rajon where soato = '" +
                               selectedCode.Trim() + "' and nzp_town = " + nzp_town.ToString();

            if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data" + tableDelimiter + "s_rajon SET ( rajon, rajon_t ) = " +
#if PG
                    "( '" + selectedFullname + "'::character(30), '" + selectedFullname +
                            "'::character(30) ) where soato = '" + selectedCode + "' and nzp_town = " +
                            nzp_town.ToString();
#else
 "( '" + selectedFullname + "', '" + selectedFullname + "' ) where soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
#endif
                retvar = ExecSQL(conDb, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + retvar.text, MonitorLog.typelog.Error,
                        true);
                    return -1;
                }
                sqlString = "SELECT nzp_raj FROM " + upper_bank + "_data" + tableDelimiter + "s_rajon WHERE soato = '" +
                            selectedCode + "' and nzp_town = " + nzp_town.ToString();
                nzp_raj = Convert.ToDecimal(ExecScalar(conDb, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                try
                {
                    sqlString = "INSERT INTO " + upper_bank + "_data" + tableDelimiter +
                                "s_rajon ( nzp_town, rajon, rajon_t, soato ) VALUES " +
#if PG
                        "(  '" + nzp_town.ToString() + "' ,'" + selectedFullname.Trim() + "'::character(30), '" +
                                selectedFullname.Trim() + "'::character(30) , '" + selectedCode.Trim() + "')";
#else
 "(  '" + nzp_town.ToString() + "' ,'" + selectedFullname + "', '" + selectedFullname + "' , '" + selectedCode + "')";
#endif
                    retvar = ExecSQL(conDb, null, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + retvar.text, MonitorLog.typelog.Error,
                            true);
                        return -1;
                    }
                    nzp_raj = ClassDBUtils.GetSerialKey(conDb, null);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + ex.Message + ex.StackTrace,
                        MonitorLog.typelog.Error, true);
                    return -1;
                }
            }

            //работа с локальными банками
            foreach (var bank in points)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data" + tableDelimiter + "s_rajon where soato = '" +
                               selectedCode.Trim() + "' and nzp_town = " + nzp_town.ToString();

                if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data" + tableDelimiter + "s_rajon SET ( rajon, rajon_t ) = " +
#if PG
 "( '" + selectedFullname + "'::character(30), '" + selectedFullname +
                                "'::character(30) ) where soato = '" + selectedCode + "' and nzp_town = " +
                                nzp_town.ToString();
#else
 "( '" + selectedFullname + "', '" + selectedFullname + "' ) where soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
#endif
                    retvar = ExecSQL(conDb, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + retvar.text, MonitorLog.typelog.Error,
                            true);
                        return -1;
                    }
                    
                }
                else
                {
                    //добавить
                    try
                    {
                        sqlString = "INSERT INTO " + bank.pref + "_data" + tableDelimiter + "s_rajon " +
                                    " SELECT * FROM " + Points.Pref + "_data" + tableDelimiter + "s_rajon " +
                                    " WHERE soato = '" + selectedCode + "' " +
                                    " AND nzp_town = " + nzp_town;
                        retvar = ExecSQL(conDb, null, sqlString, true);
                        if (!retvar.result)
                        {
                            MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + retvar.text, MonitorLog.typelog.Error,
                                true);
                            return -1;
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }

            return nzp_raj;
        }

        private decimal SaveStreet(KLADRData obj, decimal nzp_raj)
        {
            decimal nzp_ul = 0;
            var upper_bank = Points.Pref;
            var points = Points.PointList;
            Returns retvar = Utils.InitReturns();

            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();
            string selectedName = obj.name.ToUpper();
            string selectedSocr = obj.socr.ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data" + tableDelimiter + "s_ulica where soato = '" +
                               selectedCode + "'";

            if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data" + tableDelimiter +
                            "s_ulica SET ( ulica, ulicareg, nzp_raj ) = " +
                            "( '" + selectedName + "' , '" + selectedSocr + "', '" + nzp_raj + "') where soato = '" +
                            selectedCode + "'";
                retvar = ExecSQL(conDb, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + retvar.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                sqlString = "SELECT nzp_ul FROM " + upper_bank + "_data" + tableDelimiter + "s_ulica WHERE soato = '" +
                            selectedCode + "' and nzp_raj = " + nzp_raj.ToString();
                nzp_ul = Convert.ToDecimal(ExecScalar(conDb, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                try
                {
                    sqlString = "INSERT INTO " + upper_bank + "_data" + tableDelimiter +
                                "s_ulica ( ulica, nzp_raj, soato, ulicareg ) VALUES " +
                                "( '" + selectedName.Trim() + "', '" + nzp_raj + "' , '" + selectedCode.Trim() + "' , '" +
                                selectedSocr.Trim() + "')";
                    retvar = ExecSQL(conDb, null, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + retvar.text, MonitorLog.typelog.Error,
                            true);
                        return -1;
                    }
                    nzp_ul = ClassDBUtils.GetSerialKey(conDb, null);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + ex.Message + ex.StackTrace,
                        MonitorLog.typelog.Error, true);
                    return -1;
                }
            }

            //работа с локальными банками
            foreach (var bank in points)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data" + tableDelimiter + "s_ulica where soato = '" +
                               selectedCode + "'";

                if (ClassDBUtils.OpenSQL(sqlString, conDb).resultData.Rows.Count != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data" + tableDelimiter +
                                "s_ulica SET ( ulica, ulicareg, nzp_raj ) = " +
                                "( '" + selectedName + "' , '" + selectedSocr + "', '" + nzp_raj + "') where soato = '" +
                                selectedCode + "'";
                    retvar = ExecSQL(conDb, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    
                }
                else
                {
                    //добавить
                    try
                    {
                        sqlString = "INSERT INTO " + bank.pref + "_data" + tableDelimiter + "s_ulica " +
                                    " SELECT * FROM " + Points.Pref + "_data" + tableDelimiter + "s_ulica " +
                                    " WHERE soato = '" + selectedCode + "'";
                        retvar = ExecSQL(conDb, null, sqlString, true);
                        if (!retvar.result)
                        {
                            MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + retvar.text, MonitorLog.typelog.Error,
                                true);
                            return -1;
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + ex.Message + ex.StackTrace,
                            MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }

            return nzp_ul;
        }

        /// <summary>
        /// Загрузка файлов КЛАДР
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns RefreshKLADRFile(FilesImported finder, ref Returns ret)
        {
            ret = Utils.InitReturns();

            #region Разархивация файла

            string fullFileName = Path.Combine(InputOutput.GetInputDir(), finder.saved_name);
            string dir = "Source\\KLADR";

            if (InputOutput.useFtp)
            {
                if (!InputOutput.DownloadFile(finder.saved_name, fullFileName))
                {
                    ret.text = "Не удалось скопировать с ftp сервера файл " + finder.saved_name + " в файл " +
                               fullFileName;
                    ret.result = false;
                    ret.tag = -1;
                    throw new Exception("Ошибка выполнения процедуры RefreshKLADRFile: " +
                                        "Не удалось скопировать с ftp сервера файл " + finder.saved_name + " в файл " +
                                        fullFileName);
                }
            }

            //var arch = new Utility.Archive();
            //ret = arch.Decompress(fullFileName, dir, false);
            if (Utility.Archive.GetInstance(fullFileName).Decompress(fullFileName, dir).Length < 1)
            {
                ret.text = "Ошибка при обработке файла '" + fullFileName.Trim() + "' ";
                ret.tag = -1;
                throw new Exception("Ошибка при обработке файла '" + fullFileName.Trim() + "' ");
            }

            #endregion Разархивация файла

            try
            {
                string sql =
                    " insert into " + Points.Pref + "_data" + tableDelimiter +
                    " upload_progress (date_upload, progress, upload_type) " +
                    " VALUES  ( " + DBManager.sCurDateTime + " , 0, 1 ) ";
                loadId =
                    Convert.ToInt32(
                        ClassDBUtils.ExecSQL(sql, conDb, null, true, ClassDBUtils.ExecMode.Exception).resultID);

                ret = InsertKLADRIntoDb(@"Source\KLADR\KLADR.DBF", " kladr ", 11);
                SetProgress(loadId, "0.2");

                ret = InsertKLADRIntoDb(@"Source\KLADR\STREET.DBF", " kladr_street ", 15);
                SetProgress(loadId, "1");
            }
            catch (Exception ex)
            {
                SetProgress(loadId, "-0.01");
                ret.result = false;
                ret.text = "Ошибка при обработке файла '" + finder.saved_name.Trim() + "' ";
                ret.tag = -1;
                throw new Exception("Ошибка при обработке файла '" + finder.saved_name + "' " + ex.Message);
            }

            ret.result = true;
            ret.text = " Успешно загружено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// Ф-ция проверки таблиц КЛАДР и на наличие в них данных
        /// </summary>
        /// <param name="tables">Массив названий таблиц, которые необходимо проверить</param>
        /// <returns></returns>
        private bool CheckKLADRTables(string[] tables)
        {
            bool result = true;
            string sql = "";
            try
            {
#if PG
                sql = " SET search_path TO  '" + Points.Pref + "_data" + "'";
#else
                    sql = "DATABASE " + Points.Pref + "_data";
#endif
                ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                #region проверка таблиц на существование

                foreach (var tblName in tables)
                {
#if PG
                    sql =
                        "select table_name as tabname " +
                        " from information_schema.tables " +
                        " where table_schema ='" + Points.Pref + "_data" + "' and table_name ='" + tblName + "'";
#else
                    sql =
                        " select * from " + Points.Pref + DBManager.sDataAliasRest + "systables a " +
                        " where a.tabname = '" + tblName + "' and a.tabid > 99";
#endif
                    //проверка на существование
                    if (ClassDBUtils.OpenSQL(sql, conDb).resultData.Rows.Count == 0)
                    {
                        result = false;
                        if (DBManager.ExecSQL(conDb, sql, true).result)
                        {
                            sql = "CREATE  TABLE " + Points.Pref + DBManager.sDataAliasRest + tblName +
                                  " (" +
                                  " name       CHAR(40)," +
                                  " socr       CHAR(10)," +
                                  " fullname   CHAR(50)," +
                                  " code       CHAR(30)," +
                                  " index      integer," +
                                  " gninmb     CHAR(4)," +
                                  " ocatd      CHAR(11)" +
                                  " )";
                            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                            sql = "CREATE INDEX ix_" + tblName + "_code_1 ON " + Points.Pref + DBManager.sDataAliasRest +
                                  tblName + " (code)";
                            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);
                        }
                    }
                        //проверка на наличие данных
                    else
                    {
                        sql = "SELECT COUNT(*) as count from " + Points.Pref + DBManager.sDataAliasRest + tblName;
                        if (
                            Convert.ToInt32(
                                ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows[0][
                                    "count"]) < 200000)
                        {
                            result = false;
                        }
                    }
                }

                #endregion проверка таблиц на существование

                //проверка таблицы s_land
                sql = "SELECT * FROM " + Points.Pref + DBManager.sDataAliasRest + "s_land WHERE nzp_land = 1";
                if (ClassDBUtils.OpenSQL(sql, conDb).resultData.Rows.Count == 0)
                {
                    sql = "INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "s_land (nzp_land, land, land_t, soato) VALUES (1, 'РОССИЯ', 'РОССИЯ', '643');";
                    ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);
                    MonitorLog.WriteLog("В таблицу s_land добавлена строка с данными о России." + Environment.NewLine,
                        MonitorLog.typelog.Info, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CheckKLADRTables:" + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                return false;
            }
            return result;
        }

        /// <summary>
        /// Загрузка КЛАДР из dbf в БД
        /// </summary>
        /// <returns></returns>
        private Returns InsertKLADRIntoDb(string path, string tblName, int soatoLength)
        {
            var ret = new Returns();

            try
            {
                //передаем путь к файлу и кодировку '866'
                using (DataTable tbl = new exDBF("TableName").Load(path, 866))
                {
                    #region Склеить название и сокращение

                    tbl.Columns.Add("FULLNAME", typeof (String));
                    for (int i = 0; i < tbl.Rows.Count; i++)
                    {
                        tbl.Rows[i]["CODE"] = tbl.Rows[i]["CODE"].ToString().Substring(0, soatoLength);

                        string socr = "";
                        if (tbl.Rows[i]["SOCR"] != null)
                        {
                            socr = tbl.Rows[i]["SOCR"].ToString().Trim();
                            tbl.Rows[i]["SOCR"] = socr;
                        }
                        string name = "";
                        if (tbl.Rows[i]["NAME"] != null)
                        {
                            name = tbl.Rows[i]["NAME"].ToString().Trim();
                            tbl.Rows[i]["NAME"] = name;
                        }
                        tbl.Rows[i]["FULLNAME"] = name + " " + socr;
                    }

                    #endregion Склеить название и сокращение

                    string sql = "";

#if PG
                    sql = " SET search_path TO  '" + Points.Pref + "_data" + "'";
#else
                    sql = " DATABASE " + Points.Pref + "_data";
#endif
                    ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                    sql = "truncate table " + Points.Pref + DBManager.sDataAliasRest + tblName;
                    ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                    sql = DBManager.sUpdStat + " " + Points.Pref + DBManager.sDataAliasRest + tblName;
                    ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);

                    foreach (DataRow dr in tbl.Rows)
                    {
                        sql =
                            " INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + tblName +
                            " (name, socr, code, index, gninmb, ocatd,  fullname) " +
                            " VALUES" +
                            " (" +
                            " " + Utils.EStrNull(dr["NAME"].ToString().Trim()) + ", " +
                            Utils.EStrNull(dr["SOCR"].ToString().Trim()) + ", " +
                            " " + Utils.EStrNull(dr["CODE"].ToString().Trim()) + ", " +
                            Utils.EInt0(dr["INDEX"].ToString().Trim()) + ", " +
                            " " + Utils.EStrNull(dr["GNINMB"].ToString().Trim()) + ", " +
                            Utils.EStrNull(dr["OCATD"].ToString().Trim()) + ", " +
                            Utils.EStrNull(dr["FULLNAME"].ToString().Trim()) +
                            " )";
                        ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);
                    }
                    sql = DBManager.sUpdStat + " " + Points.Pref + DBManager.sDataAliasRest + tblName;
                    ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);
                }
            }
            catch (Exception ex)
            {
                ret.text = "Ошибка при загрузке файла " + System.IO.Path.GetFileName(path) + " в базу данных";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры InsertKLADRIntoDb." + ex.Message);
            }
            return ret;
        }


        /// <summary>
        /// Загрузка регионов из КЛАДР
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public ReturnsObjectType<List<KLADRData>> LoadDataFromKLADR(KLADRFinder finder)
        {
            ReturnsObjectType<List<KLADRData>> res = new ReturnsObjectType<List<KLADRData>>();
            res.returnsData = new List<KLADRData>();

            if (!CheckKLADRTables(new[] {"kladr", "kladr_street"}))
            {
                return new ReturnsObjectType<List<KLADRData>>()
                {
                    tag = -2,
                    text = "КЛАДР не загружен. Требуется его обновление.",
                    result = false
                };
            }

            //получение списка регионов
            try
            {
                // Заполняем объект данными
                foreach (
                    DataRow dr in
                        ClassDBUtils.OpenSQL(finder.query, conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows)
                {
                    res.returnsData.Add(new KLADRData()
                    {
                        fullname = dr["FULLNAME"].ToString(),
                        code = dr["CODE"].ToString(),
                        name = dr["NAME"].ToString(),
                        socr = dr["SOCR"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LoadDataFromKLADR : " + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);

                return new ReturnsObjectType<List<KLADRData>>()
                {
                    text = "Ошибка получения данных КЛАДР.",
                    result = false
                };
            }

            res.result = true;
            res.text = "Выполнено.";
            res.tag = -1;

            return res;
        }

        /// <summary>
        /// Получение прогресса загрузки
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<UploadingData> GetUploadingProgress(UploadingData finder, out Returns ret)
        {
            IDataReader reader = null;
            List<UploadingData> result = new List<UploadingData>();
            ret = Utils.InitReturns();
            try
            {
                if (!TempTableInWebCashe(conDb, Points.Pref + "_data" + tableDelimiter + "upload_progress"))
                {
                    MonitorLog.WriteLog("Таблица upload_progress не найдена", MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    ret.text = "Данные о загрузках временно не доступны";
                    ret.tag = -1;
                    return null;
                }
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " offset " + finder.skip + " limit " + finder.rows
                    : String.Empty;
                string sql = "select  * " +
                             " from " + Points.Pref + "_data.upload_progress " +
                             " where upload_type = " + finder.upload_type +
                             " order by date_upload DESC " + skip;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " skip " + finder.skip + " first " + finder.rows : String.Empty;
                string sql = "select " + skip + " * " +
                 " from " + Points.Pref + "_data" + tableDelimiter + "upload_progress " +
                 " where upload_type = " + finder.upload_type +
                " order by date_upload DESC";
#endif
                ret = ExecRead(conDb, out reader, sql, true);
                if (!ret.result)
                {
                    return null;
                }

                int i = 0;
                while (reader.Read())
                {
                    i++;
                    UploadingData file = new UploadingData();
                    file.num = (i + finder.skip).ToString();
                    file.id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : -1;
                    if (reader["date_upload"] != DBNull.Value)
                        file.date_upload = Convert.ToDateTime(reader["date_upload"]);
                    file.progress = reader["progress"] != DBNull.Value
                        ? (Convert.ToDecimal(reader["progress"])*100).ToString().Split('.')[0] + "%"
                        : "0%";
                    file.upload_type = reader["upload_type"] != DBNull.Value
                        ? Convert.ToInt32(reader["upload_type"])
                        : 0;

                    if (Convert.ToDecimal(reader["progress"]) < 0)
                        file.status = "Ошибка";
                    else if (Convert.ToDecimal(reader["progress"]) == 1)
                        file.status = "Выполнено";
                    else
                        file.status = "Выполняется";

                    result.Add(file);
                }
                sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter +
                      "upload_progress where upload_type = " + finder.upload_type;

                object count = ExecScalar(conDb, sql, out ret, true);
                if (ret.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
                return result;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetUploadingProgress\n" + ex.Message, MonitorLog.typelog.Error, 20, 201,
                    true);
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
        }

        public Returns StopRefresh()
        {
            Returns ret = new Returns();
            string sql;


            sql =
                " UPDATE " + Points.Pref + DBManager.sDataAliasRest + " upload_progress " +
                " SET progress = 1 WHERE progress >= 0 AND progress < 1 ";
            ret = ExecSQL(sql, true);

            return ret;
        }

    }

}
