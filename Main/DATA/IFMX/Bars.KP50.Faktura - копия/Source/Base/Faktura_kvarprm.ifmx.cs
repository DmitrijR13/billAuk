

using System;
using System.Data;
using System.Globalization;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaKvarPrm
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
        private readonly string _dateMonth;


        public int CountGil; // количесво фактически проживающих
        public int CountRegisterGil; // количесво проживающих по прописке
        public int CountDepartureGil; // количесво верменно выбывших
        public int CountArriveGil; // количесво верменно прибывших
        public int Rooms; // количесво комнат в квартире

        public decimal FullSquare; //Общая площадь
        public decimal LiveSquare; //Жилая площадь
        public decimal HeatSquare; //Отопительная площадь
        public string Stage; //этаж

        public bool Ownflat; // Приватизированная квартира ( Истина - да, ложь - нет)
        public bool IsolateFlat; //Изолированная квартира ( Истина - да, ложь - нет)
        public string PayerFio; // Фамилия квартиросъёмщика, собственника
        public string StateLs; //Состояние ЛС (1 - открыт, 2 - закрыт, 3 - неопределено) 

        private MyDataReader _reader;

        private bool _isPasp;//Считать жильцов по паспортистке


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



        public DbFakturaKvarPrm(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;
            _dateMonth = "'" + new DateTime(_year, _month, 1).ToShortDateString() + "'";
        }

        private void LoadGil(int nzpKvar)
        {
            string s = " SELECT sum(cnt2) as countGil, sum(val5) as countDepartureGil," +
                       " sum(val3) as countArriveGil, sum(val4) as paspgil " +
                       " FROM " + Pref + "_charge_" + (_year - 2000) + DBManager.tableDelimiter +
                       "gil_" + _month.ToString("00") +
                       " WHERE stek=3 and nzp_kvar=" + nzpKvar;
            DBManager.ExecRead(_connection, out _reader, s, true);
            while (_reader.Read())
            {
                if (_reader["countGil"] != DBNull.Value)
                {
                    Int32.TryParse(_reader["countGil"].ToString(), out CountGil);
                    CountArriveGil = Convert.ToInt32(
                        Math.Round(Decimal.Parse(_reader["countArriveGil"].ToString())));

                    CountDepartureGil = Convert.ToInt32(
                        Math.Round(Decimal.Parse(_reader["countDepartureGil"].ToString())));
                    if (_isPasp) CountRegisterGil = CountArriveGil + Convert.ToInt32(
                        Math.Round(Decimal.Parse(_reader["paspgil"].ToString())));
                }
            }
            _reader.Close();


        }


        /// <summary>
        /// Загрузка причин перерасчета
        /// </summary>
        /// <param name="apref">Префикс БД/схемы</param>
        /// <param name="nzpKvar">Код квартиры</param>
        public void LoadKvarPrm(string apref, int nzpKvar)
        {
            Pref = apref;
            LoadGil(nzpKvar);
            string s = " SELECT * from   " + _baseData + "prm_1 " +
                       " WHERE nzp_prm in (2,3,4,5,6,8,107,130,2005)" +
                       "        AND nzp=" + nzpKvar +
                       "        AND is_actual=1  " +
                       "        AND dat_s<=" + _dateMonth +
                       "        AND dat_po>=" + _dateMonth;
            DBManager.ExecRead(_connection, out _reader, s, true);
            while (_reader.Read())
            {
                int i;
                decimal d;
                switch ((int)_reader["nzp_prm"])
                {
                    case 2: Stage = Int32.TryParse(_reader["val_prm"].ToString(), out i) ? i.ToString(CultureInfo.InvariantCulture) : ""; break;
                    case 3: IsolateFlat = (_reader["val_prm"].ToString().Trim() != "2"); break;
                    case 4: FullSquare = Decimal.TryParse(_reader["val_prm"].ToString(), out d) ? d : 0; break;
                    case 5: CountGil = Int32.TryParse(_reader["val_prm"].ToString(), out i) ? i : 0; break;
                    case 6: LiveSquare = Decimal.TryParse(_reader["val_prm"].ToString(), out d) ? d : 0; break;
                    case 8: Ownflat = true; break;
                    case 107: Rooms = Int32.TryParse(_reader["val_prm"].ToString(), out i) ? i : 0; break;
                    case 130: _isPasp = true; break;
                    case 2005: CountRegisterGil = Int32.TryParse(_reader["val_prm"].ToString(), out i) ? i : 0; break;
                }
            }
            _reader.Close();

            //ФИО Квартиросъемщика
            s = " SELECT * from   " + _baseData + "prm_3 " +
                       " WHERE nzp_prm in (46,51)" +
                       "        AND nzp=" + nzpKvar +
                       "        AND is_actual=1  " +
                       "        AND dat_s<=" + _dateMonth +
                       "        AND dat_po>=" + _dateMonth;
            DBManager.ExecRead(_connection, out _reader, s, true);
            while (_reader.Read())
            {
                switch ((int)_reader["nzp_prm"])
                {
                    case 46: PayerFio = _reader["val_prm"].ToString().Trim(); break;
                    case 51: StateLs = _reader["val_prm"].ToString().Trim(); break;
                }
            }
            _reader.Close();
        }



        public void Clear()
        {
            CountGil = 0;
            CountRegisterGil = 0; // количесво проживающих по прописке
            CountDepartureGil = 0; // количесво верменно выбывших
            CountArriveGil = 0; // количесво верменно прибывших

            FullSquare = 0; //Общая площадь
            LiveSquare = 0; //Жилая площадь
            HeatSquare = 0; //Отопительная площадь
            Stage = ""; //этаж

            Ownflat = false; // Приватизированная квартира ( Истина - да, ложь - нет)
            IsolateFlat = true; //Изолированная квартира ( Истина - да, ложь - нет)
            PayerFio = ""; // Фамилия квартиросъёмщика, собственника
            StateLs = "1";
        }

    }
   
}


