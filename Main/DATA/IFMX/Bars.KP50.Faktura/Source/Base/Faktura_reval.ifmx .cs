

using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Faktura.Source.Base
{
    public class FakturaReval
    {
        
        /// <summary>
        /// Код услуги
        /// </summary>
        public int NzpServ;

        /// <summary>
        /// Причина перерасчета
        /// </summary>
        public string Reason;
        
        /// <summary>
        /// Период перерасчета
        /// </summary>
        public string ReasonPeriod;
        
        /// <summary>
        /// Сумма перерасчета
        /// </summary>
        public decimal SumReval;

        /// <summary>
        /// Объем перерасчета
        /// </summary>
        public decimal CReval;

        public FakturaReval()
        {
            Clear();
        }

        public void Clear()
        {
            NzpServ = 0;
            Reason = String.Empty;
            ReasonPeriod = String.Empty;
            SumReval = 0;
            CReval = 0;
        }
    }

    public class DbFakturaReval 
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
        /// Причины перерасчета
        /// </summary>
        public enum RevalType
        {
            None = 0,

            /// <summary>
            /// Параметры
            /// </summary>
            Prm = 1,

            /// <summary>
            /// Услуги
            /// </summary>
            Service = 2,

             
            /// <summary>
            /// Недопоставка
            /// </summary>
            Nedop = 3,
             
            /// <summary>
            /// Счетчики
            /// </summary>
            Counters = 4,

            /// <summary>
            /// льготы
            /// </summary>
            Lgot = 5,

            /// <summary>
            /// Жильцы
            /// </summary>
            Gilec = 6,

            /// <summary>
            /// Вручную
            /// </summary>
            Manual = 7,

            /// <summary>
            /// Домовые счетчики ОДН
            /// </summary>
            DomCounters = 8,

            /// <summary>
            /// Расход жильца
            /// </summary>
            CalcGilec = 9,

            /// <summary>
            /// Групповые счетчики
            /// </summary>
            GroupCounters = 10,

            /// <summary>
            /// Перекидка
            /// </summary>
            Perekidka = 100
        }

        

        /// <summary>
        /// Список перерасчетов
        /// </summary>
        public List<FakturaReval> ListReval;
      
      
        private MyDataReader _reader;

        /// <summary>
        /// Расчетный месяц
        /// </summary>
        private readonly string _dateMonth;

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


        /// <summary>
        /// Учитывать ли перерасчеты за текущий месяце
        /// </summary>
        private readonly bool _curMonthReval;

        /// <summary>
        /// Периооды временного выбытия жильцов
        /// </summary>
        private string _gilPeriods;
 
        public DbFakturaReval(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;
            ListReval = new List<FakturaReval>();
            _dateMonth = "'" + new DateTime(_year, _month, 1).ToShortDateString() + "'";
            _curMonthReval = false;
        }


        /// <summary>
        /// Загрузка периода временно выбывших  жильцов
        /// </summary>
        /// <param name="nzpKvar">Код квартиры</param>
        /// <returns></returns>
        private void LoadGilVrVib(int nzpKvar)
        {
            MyDataReader reader;
            _gilPeriods = String.Empty;
            string s = String.Empty;
            if (_curMonthReval)
            {
                s = " SELECT dat_s, dat_po  " +
                    " FROM " + _baseData + " gil_periods " +
                    " WHERE nzp_kvar=" + nzpKvar + " and is_actual=1 " +
                    "        AND dat_s <= " + _dateMonth +
                    "        AND dat_po >= " + _dateMonth +
                    " UNION ";
            }
            s += " SELECT a.dat_s, a.dat_po  " +
                 " FROM " + _baseData + "gil_periods a," +
                 "      " + _baseData + "must_calc b" +
                 " WHERE MDY(b.month_,01, b.year_) = " + _dateMonth +
                 "        AND a.nzp_kvar=b.nzp_kvar " +
                 "        AND kod1 = 6 " +
                 "        AND b.nzp_kvar=" + nzpKvar +
                 "        AND a.dat_s <= b.dat_po" +
                 "        AND a.dat_po >= b.dat_s ";
            DBManager.ExecRead(_connection, out reader, s, true);
            while (reader.Read())
            {
                if ((reader["dat_s"] != DBNull.Value) & (reader["dat_po"] != DBNull.Value))
                {
                    _gilPeriods = _gilPeriods + ", " + ((DateTime)reader["dat_s"]).ToShortDateString() + "-" +
                        ((DateTime)reader["dat_po"]).ToShortDateString();
                }
            }
            if (String.IsNullOrEmpty(_gilPeriods)) _gilPeriods = _gilPeriods.TrimEnd(',') ;
            reader.Close();
        }

        /// <summary>
        /// Добавление перерасчета к списку перерасчетов
        /// </summary>
        /// <param name="nzpServ">Код услуги</param>
        /// <param name="revalType">Тип перерасчета</param>
        /// <param name="comment">Комментарий перерасчета</param>
        /// <param name="period">Период перерасчета</param>
        private void AddReval(int nzpServ, RevalType revalType, string comment, string period)
        {
            var reval = new FakturaReval {NzpServ = nzpServ, ReasonPeriod = period};
            if (String.IsNullOrEmpty(comment))
            {
                switch (revalType)
                {
                    case RevalType.CalcGilec: reval.Reason = "указан расход жильца"; break;
                    case RevalType.Counters: reval.Reason = "Показания счетчиков "; break;
                    case RevalType.DomCounters: reval.Reason = "перерасчпределение ОДН"; break;
                    case RevalType.Gilec:
                        {
                            if (String.IsNullOrEmpty(_gilPeriods))
                                reval.Reason = "изменение в списке жильцов ";
                            else
                                reval.Reason = "временное выбытие " + _gilPeriods;

                        }
                        break;

                    case RevalType.GroupCounters: reval.Reason = "Групповые счетчики"; break;
                    case RevalType.Lgot: reval.Reason = "льготы"; break;
                    case RevalType.Manual: reval.Reason = ""; break;
                    case RevalType.Prm: reval.Reason = "характеристики жилья"; break;
                    case RevalType.Service: reval.Reason = "период действия услуги"; break;
                }
            }
            else
            {
                reval.Reason = comment;
            }
            reval.SumReval = 0;
            ListReval.Add(reval);
        }

        private void AddReval(int nzpServ, int revalType, string comment, string period)
        {
            AddReval(nzpServ, (RevalType)revalType, comment, period);
        }


        /// <summary>
        /// Загрузка причин перерасчета
        /// </summary>
        /// <param name="apref">Префикс БД/схемы</param>
        /// <param name="nzpKvar">Код квартиры</param>
        public void LoadRevalReason(string apref, int nzpKvar)
        {
            Pref = apref;
            LoadGilVrVib(nzpKvar);
            //Недопоставки
            //string s = " SELECT a.nzp_serv  "+
            //           " FROM  " + baseData + "nedop_kvar a "+
            //           " WHERE month_calc ="+ DateMonth + " "+
            //           " AND is_actual=1 "+
            //           " where nzp_kvar = "+ nzp_kvar +
            //           " GROUP BY 1";
            // DBManager.ExecRead(connection, out reader, s, true);
            // while (reader.Read())
            // {
            //     AddReval((int)reader["nzp_serv"], RevalType.Nedop, "Недопоставка");
            // }
            // reader.Close();
            // s =       " SELECT a.nzp_serv " +
            //           " FROM  " + baseData + "counters a  " +
            //           " WHERE month_calc = " + DateMonth +
            //           " AND is_actual=1 AND dat_uchet< " + DateMonth +
            //           " GROUP BY 1";
            // DBManager.ExecRead(connection, out reader, s, true);
            // while (reader.Read())
            // {
            //     AddReval((int)reader["nzp_serv"], RevalType.Counters, "Показания счетчиков");
            // }
            // reader.Close();


            //Must_calc
            string s = " SELECT a.nzp_serv, dat_s, dat_po, kod1   " +
                " FROM  " + _baseData + "must_calc a  " +
                " WHERE a.nzp_kvar=" + nzpKvar +
                "        AND nzp_serv>1  " +
                "        AND month_=" + _month +
                "        AND year_=" + _year;
            DBManager.ExecRead(_connection, out _reader, s, true);
            while (_reader.Read())
            {
                AddReval((int)_reader["nzp_serv"], (int)_reader["kod1"],"",
                   ((DateTime)_reader["dat_s"]).ToShortDateString() + "-" +
                   ((DateTime)_reader["dat_po"]).ToShortDateString());
            }
            _reader.Close();

            //Новые Счетчики
            s = " SELECT a.nzp_serv, a.num_cnt, p.val_prm as dat_plomb " +
                " FROM  " + _baseData + "counters_spis a, " + _baseData + "prm_17 p  " +
                " WHERE a.nzp_type=3 AND a.nzp="+nzpKvar+" AND p.month_calc = " +_dateMonth+                
                " AND a.is_actual=1 AND p.is_actual=1 AND p.nzp_prm=2027 AND a.nzp_counter=p.nzp" +
                " GROUP BY 1,2,3 ";
            DBManager.ExecRead(_connection, out _reader, s, true);
            while (_reader.Read())
            {
                AddReval((int)_reader["nzp_serv"], RevalType.Counters," Счетчик №" + 
                    _reader["num_cnt"].ToString().Trim()
                    + " опломбирован " + _reader["dat_plomb"].ToString().Trim(),"");
            }
            _reader.Close();

            //перекидки
            s = " SELECT nzp_serv, comment  " +
               " FROM  " + Pref + "_charge_" + (_year - 2000) + DBManager.tableDelimiter +"perekidka " +
               " WHERE nzp_kvar=  " +nzpKvar+
               " AND month_=" + _month +
               " GROUP BY 1,2";
            DBManager.ExecRead(_connection, out _reader, s, true);
            while (_reader.Read())
            {
                AddReval((int)_reader["nzp_serv"], RevalType.Perekidka, _reader["comment"].ToString().Trim(),"");
            }
            _reader.Close();
        }

  

        public void Clear()
        {
            _gilPeriods = String.Empty;
            ListReval.Clear();
        }
    }
   
}


