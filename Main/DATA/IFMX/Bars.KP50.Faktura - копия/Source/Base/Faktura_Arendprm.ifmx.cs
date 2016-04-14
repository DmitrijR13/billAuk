

using System;
using System.Data;
using System.Globalization;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaArendPrm
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

        public string ArendNumAct; //Последовательный номер акта для Арендаторов
        public string ArendNumDog; //Номер договора с Арендатором
        public string ArendDatDog; //Дата договора с Арендатором
        public string ArendInnDog; //ИНН Арендатора
        public string ArendKppDog; //КПП Арендатора
        public string ArendFullName; //Полное наименование Арендатора
        public string ArendUrAdr; //Юридический адрес Арендатора


        private MyDataReader _reader;
        private int _nzpKvar;// Код дома

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



        public DbFakturaArendPrm(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;

        }




        public void SetArendField()
        {

            string s = " Select nzp_prm, val_prm From " + _baseData + "prm_1 " +
                       " WHERE nzp=" + _nzpKvar + " AND nzp_prm in (883, 884, 886, 1014, 965, 1013)";
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                while (_reader.Read())
                {
                    switch (Int32.Parse(_reader["nzp_prm"].ToString()))
                    {
                        case 883: ArendNumDog = _reader["val_prm"].ToString().Trim(); break;
                        case 884: ArendDatDog = _reader["val_prm"].ToString().Trim(); break;
                        case 886: ArendInnDog = _reader["val_prm"].ToString().Trim(); break;
                        case 1014: ArendKppDog = _reader["val_prm"].ToString().Trim(); break;
                        case 965: ArendFullName = _reader["val_prm"].ToString().Trim(); break;
                        case 1013: ArendUrAdr = _reader["val_prm"].ToString().Trim(); break;
                    }


                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка параметров арендаторов " + s + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                _reader.Close();
            }
        }


        public string GetNumAct()
        {
            MyDataReader reader = null;
            string result = String.Empty;
            //Первая часть арендаторов
            string lastDay = DateTime.DaysInMonth(_year, _month).ToString("00") + "." + _month.ToString("00") + "." + _year;
            string s = " SELECT * " +
                       " FROM " + Points.Pref + DBManager.sDataAliasRest + "num_acts s " +
                       " WHERE dat_act='" + lastDay + "'" +
                       " AND num_ls= " + _nzpKvar;
            try
            {
                DBManager.ExecRead(_connection, out reader, s, true);
                if (reader.Read()) // Такой акт уже есть
                {
                    result = reader["num_act"].ToString();

                }
                else
                {
                    reader.Close();
                    IDbTransaction tr = _connection.BeginTransaction();
                    try
                    {


                        s = " Select max(num_act) as num_act From " + Points.Pref + DBManager.sDataAliasRest + "num_acts  " +
                            " WHERE year_= " + _year;
                        DBManager.ExecRead(_connection, tr, out reader, s, true);
                        if (reader.Read())
                        {
                            result = (Int32.Parse(reader["num_act"].ToString()) + 1).ToString(CultureInfo.InvariantCulture);
                        }
                        reader.Close();

                        s = "insert into " + Points.Pref + DBManager.sDataAliasRest + "num_acts(num_ls,dat_act,year_,num_act)" +
                            "values(" + _nzpKvar + "','" + lastDay + "'," + _year + "," + result + ")";
                        DBManager.ExecRead(_connection, tr, out reader, s, true);

                        ArendDatDog = lastDay;//можно ли??

                        if (tr != null) tr.Commit();

                    }
                    catch
                    {
                        if (tr != null) tr.Rollback();
                        MonitorLog.WriteLog("Ошибка выборки " + s, MonitorLog.typelog.Error, 20, 201, true);
                        result = String.Empty;
                    }
                }
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            return result;

        }

        public void LoadArendPrm(string apref, int nzpArea, int nzpKvar)
        {
            Pref = apref;
            _nzpKvar = nzpKvar;

            SetArendField();

            ArendNumAct = GetNumAct();
        }



        public void Clear()
        {
            _nzpKvar = 0;
            ArendNumAct = String.Empty; //Последовательный номер акта для Арендаторов
            ArendNumDog = String.Empty; //Номер договора с Арендатором
            ArendDatDog = String.Empty; //Дата договора с Арендатором
            ArendInnDog = String.Empty; //ИНН Арендатора
            ArendKppDog = String.Empty; //КПП Арендатора
            ArendFullName = String.Empty; //Полное наименование Арендатора
            ArendUrAdr = String.Empty; //Юридический адрес Арендатора

        }

    }

}


