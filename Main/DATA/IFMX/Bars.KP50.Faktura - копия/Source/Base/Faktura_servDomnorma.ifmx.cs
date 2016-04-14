

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Faktura.Source.Base
{
    /// <summary>
    /// Расход по услуге
    /// </summary>
    public class ServVolumeDom
    {

        public enum VolumeType
        {
            Normativ = 0,
            Counter = 1,
            Average = 2
        }
        public int NzpServ;
        public decimal Volume;
        public decimal Odn;
        public decimal NormOdn;
        public decimal Kf307;//Коэффициент П354
        public decimal Kf307N;//Коэффициент П354 Нормативный
        public decimal AllLsExpend;//dlt_calc - Сумма начисленных расходов по ЛС
        public decimal NormExpend;//val1 - Нормативный расход
        public decimal IpuExpend;//val2 - Расход по ИПУ"
        public decimal DomBillExpend;//val3 - Расчетный расход по дому (с учетом Пост.344)
        public decimal OdpuExpend;//val4 - Расход по ОДПУ
        public decimal OdnExpend;//kf_dpu_ls - Расход ОДН
        public decimal IpuNormSum;//sum_val1_val2 - Сумма нормативного расхода, расхода по ИПУ и расхода на ОДН
        public decimal RevalExpend;//dlt_reval - Изменение расхода-перерасчет ИПУ
        public decimal RealChargeExpend;//dlt_real_charge - Изменение расхода-вручную
        public VolumeType IsPu;
        public bool HasCountersMop;
        public ServVolumeDom()
        {
            Clear();
        }

        public void Clear()
        {
            NzpServ = 0;
            Volume = 0;
            Odn = 0;
            IsPu = VolumeType.Normativ;
            HasCountersMop = false;
            Kf307 = 0;
            Kf307N = 0;
            AllLsExpend = 0;
            NormExpend = 0;
            IpuExpend = 0;
            DomBillExpend = 0;
            OdpuExpend = 0;
            OdnExpend = 0;
            IpuNormSum = 0;
            RevalExpend = 0;
            RealChargeExpend = 0;
        }

    }

    public class DbFakturaServVolumeDom
    {
        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Рассчетный месяц
        /// </summary>
        private readonly int _month;

        /// <summary>
        /// Расчетный год
        /// </summary>
        private readonly int _year;

        /// <summary>
        /// Расчетный месяц
        /// </summary>

        public decimal CalcSquareLs;

        public decimal HvsNorm;
        public decimal GvsNorm;

        public readonly List<ServVolumeDom> ListDomNormativ;

        private MyDataReader _reader;

        public string Pref { get; set; }


        public DbFakturaServVolumeDom(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;
            ListDomNormativ = new List<ServVolumeDom>();
        }


        public void AddVolume(ServVolumeDom servVolume)
        {
            if (servVolume.HasCountersMop)
            {
                servVolume.Odn = servVolume.Volume;
                servVolume.Volume = 0;
            }
            ListDomNormativ.Add(servVolume);
        }

        public List<ServVolumeDom> GetServNormativ(string pref, int nzpDom)
        {
            string tableCounters = pref + "_charge_" + (_year - 2000) + DBManager.tableDelimiter+
                "counters_" + _month.ToString("00");
            string s = " SELECT nzp_serv, a.kod_info, a.cnt_stage, " +
                       "        a.kf_dpu_kg as dpu_odn, " + //ОДН по дому
                       "        case when a.cur_zap = 1 then a.val4 -a.val1 -a.val2 -a.dlt_reval " +
                       "        -a.dlt_real_charge else a.val4 end as dpu, " +
                       "        a.vl210 as norm_odn, " + //Норматив на 1 квм. по ЛС
                       "        a.kf307,  " +
                       "        a.kf307n,  " +
                       "        a.dlt_calc,  " +
                       "        a.val1,  " +
                       "        a.val2,  " +
                       "        a.val3 as dpu_cut, " +
                       "        a.val4,  " +
                       "        a.kf_dpu_ls,  " +
                       "        a.val1 + a.val2 + a.dlt_reval + a.dlt_real_charge as sum_val1_val2,  " +
                       "        a.dlt_reval,  " +
                       "        a.dlt_real_charge,  " +
                       "        a.cur_zap as counter_mop " + //Признак счетчика ОДН
                       " FROM " + tableCounters + " a " +
                       " WHERE a.nzp_dom = " + nzpDom +
                       "        AND stek = 3 " +
                       "        AND nzp_type=1 ";
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true); 
                while (_reader.Read())
                {                
                    var servVolume = new ServVolumeDom
                    {
                        NzpServ = _reader["nzp_serv"] != DBNull.Value ? Int32.Parse(_reader["nzp_serv"].ToString()) : 0,
                        IsPu = _reader["cnt_stage"] != DBNull.Value ? (_reader["cnt_stage"].ToString() == "1"
                            ? ServVolumeDom.VolumeType.Counter
                            : ServVolumeDom.VolumeType.Normativ) : 0,
                        HasCountersMop = _reader["counter_mop"] != DBNull.Value && _reader["counter_mop"].ToString() == "1",
                        Volume = _reader["dpu"] != DBNull.Value ? Decimal.Parse(_reader["dpu"].ToString()) : 0,
                        Odn = _reader["dpu_odn"] != DBNull.Value ? Decimal.Parse(_reader["dpu_odn"].ToString()) : 0,
                        NormOdn = _reader["norm_odn"] != DBNull.Value ? Decimal.Parse(_reader["norm_odn"].ToString()) : 0,
                        Kf307 = _reader["kf307"] != DBNull.Value ? Decimal.Parse(_reader["kf307"].ToString()) : 0,
                        Kf307N = _reader["kf307n"] != DBNull.Value ? Decimal.Parse(_reader["kf307n"].ToString()) : 0,
                        AllLsExpend = _reader["dlt_calc"] != DBNull.Value ? Decimal.Parse(_reader["dlt_calc"].ToString()) : 0,
                        NormExpend = _reader["val1"] != DBNull.Value ? Decimal.Parse(_reader["val1"].ToString()) : 0,
                        IpuExpend = _reader["val2"] != DBNull.Value ? Decimal.Parse(_reader["val2"].ToString()) : 0,
                        DomBillExpend = _reader["dpu_cut"] != DBNull.Value ? Decimal.Parse(_reader["dpu_cut"].ToString()) : 0,
                        OdpuExpend = _reader["val4"] != DBNull.Value ? Decimal.Parse(_reader["val4"].ToString()) : 0,
                        OdnExpend = _reader["kf_dpu_ls"] != DBNull.Value ? Decimal.Parse(_reader["kf_dpu_ls"].ToString()) : 0,
                        IpuNormSum = _reader["sum_val1_val2"] != DBNull.Value ? Decimal.Parse(_reader["sum_val1_val2"].ToString()) : 0,
                        RevalExpend = _reader["dlt_reval"] != DBNull.Value ? Decimal.Parse(_reader["dlt_reval"].ToString()) : 0,
                        RealChargeExpend = _reader["dlt_real_charge"] != DBNull.Value ? Decimal.Parse(_reader["dlt_real_charge"].ToString()) : 0
                    };


                    //для поддержки 2.0
                    //if (((servVolume.nzpServ == 8) & (reader["kod_info"].ToString() == "24")) ||
                    //    ((servVolume.nzpServ == 9) & (reader["kod_info"].ToString() == "25")))
                    //{

                    //}
                    ListDomNormativ.Add(servVolume);

                }
            }
            catch
            {

                MonitorLog.WriteLog("Ошибка выборки домовых расходов " + s, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                _reader.Close();
            }




            return ListDomNormativ;
        }


        public ServVolumeDom GetServVolume(int nzpServ)
        {
            return ListDomNormativ.FirstOrDefault(x => x.NzpServ == nzpServ);
        }

        public void Clear()
        {
            ListDomNormativ.Clear();
        }

    }
   
}


