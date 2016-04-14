

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
    public class ServVolume2
    {
        
        public enum VolumeType
        {
            Normativ = 0,
            Counter = 1,
            Average = 2
        }
        public int NzpServ;
        public decimal Volume;
        public decimal Normativ;
        public decimal FullNormativ;
        public decimal Odn;
        //public decimal kf307;
        public VolumeType IsPu;
        public ServVolume2()
        {
            Clear();
        }

        public void Clear()
        {
            NzpServ = 0;
            Volume = 0;
            Normativ = 0;
            FullNormativ = 0;
            Odn =0;
            IsPu = VolumeType.Normativ;
            //kf307 = 0;
        }

    }
    
    public class DbFakturaServVolumeLs
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

        private readonly List<ServVolume2> _listLsNormativ;

        private MyDataReader _reader;

        public string Pref { get; set; }


        public DbFakturaServVolumeLs(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;
            _listLsNormativ = new List<ServVolume2>();
        }


        public void AddVolumeLs(ServVolume2 servVolume)
        {
            if (servVolume.Odn<0)
            {
                if (servVolume.Volume > 0)
                {
                    if (-1 * servVolume.Odn > servVolume.Volume)
                        servVolume.Odn = -servVolume.Volume;
                }
                else
                {
                    servVolume.Odn = 0;
                }
            }
            _listLsNormativ.Add(servVolume);
        }

        public List<ServVolume2> GetLsServNormativ(string pref, int nzpKvar, bool calcKan)
        {

            string tableRashNorm = pref + "_charge_" + (_year - 2000) +
                DBManager.tableDelimiter + "calc_gku_" + _month.ToString("00");
            string s = " SELECT a.nzp_serv, " +
                        "        dlt_reval, " +
                        "        valm, " +
                        "        rashod_norm , " +
                        "        dop87  , " +
                        "        is_device, " +//0-норматив 1 по счетчику 9 по среднему
                        "        gil, " +
                        "        valm, " +
                        "        squ, " +
                        "        rashod , " +
                        "        rsh2, " +//Нормативный расход при нулевом расходе
                        "        rash_norm_one, " +//Нормативный расход на одного проживающего
                        "        tarif " +//Количество проживающих
                        " FROM " + tableRashNorm + " a " +
                        " WHERE nzp_kvar=" + nzpKvar +
                        " AND a.stek = 3 ";

            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true); 
                while (_reader.Read())
                {
                    if (Decimal.Parse(_reader["tarif"].ToString()) > 0)
                    {

                        var servVolume = new ServVolume2
                        {
                            NzpServ = Int32.Parse(_reader["nzp_serv"].ToString()),
                            Volume =
                                Decimal.Parse(_reader["valm"].ToString()) +
                                Decimal.Parse(_reader["dlt_reval"].ToString()),
                            Odn = Decimal.Parse(_reader["dop87"].ToString()),
                            Normativ = Convert.ToDecimal(_reader["rash_norm_one"]),
                            FullNormativ = Convert.ToDecimal(_reader["rashod_norm"]),
                            IsPu = (ServVolume2.VolumeType) int.Parse(_reader["is_device"].ToString())
                        };
                        CalcSquareLs = Decimal.Parse(_reader["squ"].ToString());
                        AddVolumeLs(servVolume);
                    }

                }
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выборки " + s, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                _reader.Close();
            }

            return _listLsNormativ;
        }

        public ServVolume2 GetServVolume(int nzpServ)
        {
            return _listLsNormativ.FirstOrDefault(x => x.NzpServ == nzpServ);
        }

        public void Clear()
        {
            _listLsNormativ.Clear();
        }

    }
   
}


