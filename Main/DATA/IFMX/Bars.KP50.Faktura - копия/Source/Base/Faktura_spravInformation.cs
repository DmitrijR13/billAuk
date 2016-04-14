using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaSpravInformation
    {
        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Ссылка на схему/базу с показаниями счетчиков
        /// </summary>
        private string _baseData;

        private List<SpravInformationRow> spravList;

        private MyDataReader _reader;


        private string _pref;
        private int _month;
        private int _year;

        public string Pref
        {
            get
            {
                return _pref;
            }
            set
            {
                _baseData = value + DBManager.sDataAliasRest;
                _pref = value;
            }
        }



        public DbFakturaSpravInformation(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;
        }

        public Dictionary<int, BaseServ2> ServRename(Dictionary<int, BaseServ2> bsDictionary, int nzpdom)
        {
            var dat = new DateTime(_year, _month, 1);
            foreach (var v in bsDictionary)
            {
                BaseServ2 bs = v.Value;

                string s = " SELECT val_prm FROM " + _pref + DBManager.sDataAliasRest + "prm_2 " +
                           " WHERE nzp_prm = " + (3000000 + bs.Serv.NzpServ) +
                           " AND nzp = " + nzpdom +
                           " AND is_actual = 1 " +
                           " AND dat_s <='" + dat.AddMonths(1).ToShortDateString() + "' " +
                           " AND dat_po >='" + dat.ToShortDateString() + "' ";
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    bs.Serv.NameServ = _reader[0].ToString();
                }
            }
            return bsDictionary;
        }


        public List<SpravInformationRow> GetSpravInformation(int nzpDom)
        {
            DateTime curMonth = new DateTime(_year, _month, 1);
            try
            {
                string calcGku = _pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter +
                                 "calc_gku_" + _month.ToString("00");
                string counters = _pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter + "counters_" +
                _month.ToString("00");
                spravList = new List<SpravInformationRow>();
                string s = " CREATE TEMP TABLE t_sprav_info " +
                           " (nzp_serv integer, is_device integer, service char(50), priznak_rasch integer, " +
                           " kf307n decimal(14,7), val4 decimal(14,7), " +
                           " sum_v1_v2 decimal(14,7), kf_dpu_ls decimal(14,7)," +
                           " squ1 decimal(14,7), rash_norm_one decimal(14,7)) " + DBManager.sUnlogTempTable;
                DBManager.ExecSQL(_connection, s, true);

                s = " INSERT INTO t_sprav_info (nzp_serv, is_device, service, priznak_rasch, rash_norm_one) " +
                    " SELECT s.nzp_serv, " +
                    " MAX(CASE WHEN is_device = 9 THEN 1 ELSE is_device END) as is_device, " +
                    " MAX(service), " +
                    " MAX(CASE WHEN is_device = 0 THEN 1 ELSE 0 END) AS priznak_rasch, " +
                    " MAX(g.rash_norm_one) " +
                    " FROM  " + _pref + DBManager.sKernelAliasRest + "services s, " +
                      counters + " c " +
                    " LEFT JOIN " + calcGku + " g " +
                    " ON c.nzp_dom = g.nzp_dom " +
                    " AND c.nzp_serv = g.nzp_serv " +
                    " AND g.stek = 3 " +
                    " AND g.dat_charge is null " +
                    " WHERE s.nzp_serv = c.nzp_serv " +
                    " AND c.stek = 3 " +
                    " AND c.nzp_type = 1 " +
                    " AND c.nzp_kvar = 0 " +
                    " AND c.nzp_dom=" + nzpDom +
                    " AND c.dat_charge is null " +
                    " GROUP BY 1 ";
                DBManager.ExecSQL(_connection, s, true);

                s = " update t_sprav_info set squ1 = (" +
                    " SELECT sum(squ) " +
                    " from " + calcGku + " c  where nzp_dom=" + nzpDom +
                    " and c.nzp_serv = t_sprav_info.nzp_serv and stek=3) ";
                DBManager.ExecSQL(_connection, s, true);

                //s = " select s.nzp_serv_link,sum(case when n.rashod>0 then n.rashod else 0 end) rashod_odn " +
                //    " from " + calcGku + " n " +
                //    " left outer join " + Points.Pref + DBManager.sKernelAliasRest +
                //    "serv_odn s on n.nzp_serv=s.nzp_serv " +
                //    " and s.nzp_serv_link in (select nzp_serv_link from " + _pref + DBManager.sKernelAliasRest + "serv_odn) " +
                //    " where n.nzp_dom=" + nzpDom + 
                //    " and n.stek=3 " +
                //    " group by 1 order by 1 ";
                //DBManager.ExecRead(_connection, out _reader, s, true);
                //decimal rashod_odn = 0;
                //if (_reader.Read())
                //{
                //    if (_reader["rashod_odn"] != DBNull.Value) rashod_odn = Convert.ToDecimal(_reader["rashod_odn"]);
                //}
                //_reader.Close();

                //s = " update t_sprav_info set kf_dpu_ls = (" +
                //    " SELECT sum(case when nzp_serv in (select nzp_serv_link from " + _pref +
                //      DBManager.sKernelAliasRest + "serv_odn) then " + rashod_odn + " else dop87 end) as kf_dpu_ls " +
                //    " from " + counters + " c  where nzp_dom=" + nzpDom +
                //    " and c.nzp_serv = t_sprav_info.nzp_serv and stek=3 and nzp_type=3) ";
                //DBManager.ExecSQL(_connection, s, true);

                //s = " update t_sprav_info set sum_v1_v2 = (" +
                //    " SELECT sum(val1+val2+(case when nzp_serv in (select nzp_serv_link from " + _pref +
                //      DBManager.sKernelAliasRest + "serv_odn) then " + rashod_odn + " else dop87 end)) as sum_v1_v2 " +
                //    " from " + counters + " c  where nzp_dom=" + nzpDom +
                //    " and c.nzp_serv = t_sprav_info.nzp_serv and stek=3 and nzp_type=3) ";
                //DBManager.ExecSQL(_connection, s, true);

                s = " update t_sprav_info set kf_dpu_ls = (" +
                    " SELECT sum(dop87) as kf_dpu_ls " +
                    " from " + counters + " c  where nzp_dom=" + nzpDom +
                    " and c.nzp_serv = t_sprav_info.nzp_serv and stek=3 and nzp_type=3) ";
                DBManager.ExecSQL(_connection, s, true);

                s = " update t_sprav_info set sum_v1_v2 = (" +
                    " SELECT sum(val1+val2+dop87) as sum_v1_v2 " +
                    " from " + counters + " c  where nzp_dom=" + nzpDom +
                    " and c.nzp_serv = t_sprav_info.nzp_serv and stek=3 and nzp_type=3) ";
                DBManager.ExecSQL(_connection, s, true);

                s = " update t_sprav_info set val4 = (" +
                    " SELECT max(case when cnt_stage>0 then val4 else 0 end) as val4 " +
                    " from " + counters + " c  where nzp_dom=" + nzpDom +
                    " and c.nzp_serv = t_sprav_info.nzp_serv and stek=3 and nzp_type=1) ";
                DBManager.ExecSQL(_connection, s, true);

                s = " update t_sprav_info set kf307n = (" +
                    " SELECT kf307n " +
                    " from " + counters + " c  where nzp_dom=" + nzpDom +
                    " and c.nzp_serv = t_sprav_info.nzp_serv and stek=3 and nzp_type=1) ";
                DBManager.ExecSQL(_connection, s, true);

                s = " update t_sprav_info set  priznak_rasch = 4 where " +
                    " nzp_serv in (select nzp_serv from " + _pref + DBManager.sKernelAliasRest + "serv_odn)  " +
                    " and 0<(Select count(*) from " + _pref + "_charge_" + (_year - 2000).ToString("00") + ".counters_" + _month.ToString("00") + " c where " +
                    " c.nzp_dom=" + nzpDom + " and c.nzp_serv= t_sprav_info.nzp_serv " +
                    " and nzp_type=1 and stek in (1,2))";
                DBManager.ExecSQL(_connection, s, true);

                s = " update t_sprav_info set priznak_rasch = 3 where " +
                    " is_device in (1) and 0<( " +
                    " Select count(*) from " + _pref + "_charge_" + (_year - 2000).ToString("00") + ".counters_" +
                     _month.ToString("00") + " c where c.nzp_serv=t_sprav_info.nzp_serv and nzp_type=3 and stek in (2)) ";
                DBManager.ExecSQL(_connection, s, true);

                s = " update t_sprav_info set  priznak_rasch = 2 where " +
                    " is_device in (1) and 0<( " +
                    " Select count(*) from " + _pref + "_charge_" + (_year - 2000).ToString("00") + ".counters_" +
                     _month.ToString("00") + " c where c.nzp_serv=t_sprav_info.nzp_serv and nzp_type=3 and stek in (1)) ";
                DBManager.ExecSQL(_connection, s, true);

                s = " SELECT nzp_serv, service, " +
                    " MAX(priznak_rasch) AS priznak_rasch, " +
                    " MAX(kf307n) AS kf307n, " +
                    " MAX(val4) AS val4, " +
                    " MAX(sum_v1_v2) AS sum_v1_v2, " +
                    " MAX(kf_dpu_ls) AS kf_dpu_ls, " +
                    " MAX(squ1) AS squ1, " +
                    " SUM(rash_norm_one) AS rash_norm_one " +
                    " FROM t_sprav_info GROUP BY 1,2 ";
                DBManager.ExecRead(_connection, out _reader, s, true);
                while (_reader.Read())
                {
                    var sr = new SpravInformationRow
                    {
                        _nzp_serv = _reader["nzp_serv"] != DBNull.Value ? Int32.Parse(_reader["nzp_serv"].ToString()) : 0,
                        _serv = _reader["service"] != DBNull.Value ? _reader["service"].ToString() : "",
                        _type = _reader["priznak_rasch"] != DBNull.Value ? Int32.Parse(_reader["priznak_rasch"].ToString()) : 0,
                        _norm_indiv = _reader["rash_norm_one"] != DBNull.Value ? Decimal.Parse(_reader["rash_norm_one"].ToString()) : 0,
                        _norm_od = _reader["kf307n"] != DBNull.Value ? Decimal.Parse(_reader["kf307n"].ToString()) : 0,
                        _volume_odpu = _reader["val4"] != DBNull.Value ? Decimal.Parse(_reader["val4"].ToString()) : 0,
                        _volume_in = _reader["sum_v1_v2"] != DBNull.Value ? Decimal.Parse(_reader["sum_v1_v2"].ToString()) : 0,
                        _volume_odn = _reader["kf_dpu_ls"] != DBNull.Value ? Decimal.Parse(_reader["kf_dpu_ls"].ToString()) : 0,
                        _square = _reader["squ1"] != DBNull.Value ? Decimal.Parse(_reader["squ1"].ToString()) : 0
                    };
                    spravList.Add(sr);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения справочной информации " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                _reader.Close();
                DBManager.ExecSQL(_connection, " DROP TABLE t_sprav_info ", true);
            }

            return spravList;
        }


        public void Clear()
        {

        }

    }

    public class SpravInformationRow
    {
        /// <summary> Услуга </summary>
        public int _nzp_serv;

        /// <summary> Услуга </summary>
        public string _serv;

        /// <summary> Тип определения объема </summary>
        public int _type;

        /// <summary> Норматив потребления коммунальных услуг - индивидуальное потребление </summary>
        public decimal _norm_indiv;

        /// <summary> Норматив потребления коммунальных услуг - общедомовые нужды </summary>
        public decimal _norm_od;

        /// <summary> Суммарный объем коммунальных услуг - по ОДПУ </summary>
        public decimal _volume_odpu;

        /// <summary> Суммарный объем коммунальных услуг - в помещениях дома </summary>
        public decimal _volume_in;

        /// <summary> Суммарный объем коммунальных услуг - на ОДН </summary>
        public decimal _volume_odn;

        /// <summary> Общая площадь помещений дома для расчета </summary>
        public decimal _square;

    }

}


