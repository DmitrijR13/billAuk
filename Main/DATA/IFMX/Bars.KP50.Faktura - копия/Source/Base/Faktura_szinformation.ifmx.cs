

using System;
using System.Data;
using Bars.KP50.DB.Faktura;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaSzInformation
    {
        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Ссылка на схему/базу с показаниями счетчиков
        /// </summary>
        private string _baseData;

        /// <summary>
        /// Рассчетный месяц
        /// </summary>
        private readonly int _month;

        /// <summary>
        /// Расчетный год
        /// </summary>
        private readonly int _year;

        private MyDataReader _reader;


        private string _pref;

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



        public DbFakturaSzInformation(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;
        }


        public SzInformation GetSzInformation(int nzpKvar)
        {

            var result = new SzInformation();
            string tableSz = Pref + "_charge_" + (_year - 2000) + DBManager.tableDelimiter +
                "calc_sz_fin_" + _month.ToString("00");
                string s = " Select fam, ima, otch, drog, sum(case when nzp_exp in (6,120) then sum_must else 0 end) as ls_smo, " +
                           "         sum(case when ((nzp_exp>10) AND (nzp_exp<20))or(nzp_exp=128) " +
                           " then sum_must else 0 end) as ls_edv, " +
                           "         sum(case when ((nzp_exp<=10)and(nzp_exp<>6))or(nzp_exp=21) " +
                           " then sum_must else 0 end) as ls_lgota, " +
                           "         sum(case when nzp_exp=28 then sum_must else 0 end) as ls_tepl, " +
                           "         sum(case when nzp_exp=3 then sum_must else 0 end) as ls_sv " +
                           "  From " + tableSz + " a, " + _baseData + "kvar k " +
                           "  Where a.num_ls=k.num_ls " +
                           "    AND nzp_exp>0 " +
                           " AND k.nzp_kvar=" + nzpKvar +
                           " group by 1,2,3,4";
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                while (_reader.Read())
                {
                    var sg = new SzGilec
                    {
                        Fam = _reader["fam"] != DBNull.Value ? _reader["fam"].ToString().Trim() : "",
                        Ima = _reader["ima"] != DBNull.Value ? _reader["ima"].ToString().Trim() : "",
                        Otch = _reader["otch"] != DBNull.Value ? _reader["otch"].ToString().Trim() : "",
                        DatRog = _reader["drog"] != DBNull.Value ? _reader["drog"].ToString().Trim() : ""
                    };

                    Decimal.TryParse(_reader["ls_edv"].ToString(), out sg.SumEdv);
                    Decimal.TryParse(_reader["ls_smo"].ToString(), out sg.SumSubs);
                    Decimal.TryParse(_reader["ls_tepl"].ToString(), out sg.SumTepl);
                    Decimal.TryParse(_reader["ls_lgota"].ToString(), out sg.SumLgota);
                    Decimal.TryParse(_reader["ls_sv"].ToString(), out sg.SumSv);
                    result.AddGilec(sg);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка загрузки информации СЗ " + s +" "+
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                _reader.Close();
            }
    
            return result;
        }


        public void Clear()
        {
            
        }

    }
   
}


