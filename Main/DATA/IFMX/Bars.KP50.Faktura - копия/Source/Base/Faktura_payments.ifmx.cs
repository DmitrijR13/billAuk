

using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaPayments
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

        /// <summary>
        /// Расчетный месяц
        /// </summary>

        public string DateOplat;//Дата последней оплаты по ЛС
        public decimal LastSumOplat;//Сумма последней оплаты по ЛС
        
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



        public DbFakturaPayments(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;
        }




        public void SetPayment(string pref, int nzpKvar)
        {
            string lastDay = new DateTime( _year, _month, DateTime.DaysInMonth(_year, _month)).ToShortDateString();
            string tablePackLs = Points.Pref + "_fin_" + (_year - 2000) + DBManager.tableDelimiter +
                "pack_ls";

            string s = " SELECT * " +
                       " FROM " + tablePackLs + " a, " + pref + DBManager.sDataAliasRest + "kvar k " +
                       " WHERE a.num_ls=k.num_ls " +
                       " AND nzp_kvar=" + nzpKvar +
                       " AND dat_uchet <= Date('" + lastDay + "') order by dat_vvod desc";

            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                    if (_reader["dat_vvod"] != DBNull.Value)
                    {
                        if ((LastSumOplat == 0) || (
                            DateTime.Parse(_reader["dat_uchet"].ToString()) >=
                            DateTime.Parse("01." + _month + "." + _year + "")))
                        {

                            if (String.IsNullOrEmpty(DateOplat))
                                DateOplat = DateTime.Parse(_reader["dat_vvod"].ToString()).ToShortDateString();
                            LastSumOplat += Decimal.Parse(_reader["g_sum_ls"].ToString());
                        }
                    }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка загрузки информации СЗ " + s + " " +
                                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                _reader.Close();
            }
        }





        public void Clear()
        {
            LastSumOplat = 0;
            DateOplat = String.Empty;
        }

    }
   
}


