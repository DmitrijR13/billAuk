using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace Bars.KP50.Faktura.Source.Base
{
    /// <summary>
    /// Класс описывающий рассрочку по 354 постановлению
    /// </summary>
    public class Instalment354
    {
        /// <summary>
        /// Код услуги
        /// </summary>
        public int NzpServ;

        /// <summary>
        /// Уникальный код рассрочки
        /// </summary>
        public int NzpKredit;

        /// <summary>
        /// Месяц, в котором заключена рассрочка
        /// </summary>
        public DateTime DatMonth; 

        /// <summary>
        /// Наименование услуги
        /// </summary>
        public string ServiceName;
        
        /// <summary>
        /// Общая сумма на которую предоставлена рассрочка
        /// </summary>
        public decimal SumPlat;


        /// <summary>
        /// Общая сумма на которую предоставлена рассрочка
        /// </summary>
        public decimal SumPreviosPlat;


        /// <summary>
        /// Проценты, под которые предоставлена рассрочка
        /// </summary>
        public decimal Percent;

        /// <summary>
        /// Рассрочка в текущем месяце
        /// </summary>
        public decimal CurrentSumInstalment;

        /// <summary>
        /// Сумма процентов в текущем месяце
        /// </summary>
        public decimal CurrentSumPercent;

        /// <summary>
        /// Сумма процентов расчетная
        /// </summary>
        public decimal SumPercent()
        {
            return SumPlat*Percent/100;
        }

        /// <summary>
        /// Сумма к плате с учетом процентов
        /// </summary>
        public decimal SumCharge()
        {
            return SumPlat + SumPercent();
        }

        public Instalment354()
        {
            Clear();
        }

        public void Clear()
        {
            NzpServ = 0;
            NzpKredit = 0;
            ServiceName = String.Empty;
            SumPlat = 0;
            Percent = 0;
            CurrentSumInstalment = 0;
            CurrentSumPercent = 0;
            DatMonth = DateTime.Now;
        }
    }

    public class DbFakturaInstalment354
    {
        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Расчетный месяц
        /// </summary>
        private readonly string _dateMonth;

        private MyDataReader _reader;

        public List<Instalment354> Instalments;
        private string Pref { get; set; }

        public bool Allow;


        public DbFakturaInstalment354(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _dateMonth = "'" + new DateTime(year, month, 1).ToShortDateString() + "'";
            Instalments = new List<Instalment354>();
        }

        public void GetInstalmentSum(string pref, int nzpKvar)
        {
            Instalments.Clear();
            Allow = false;
            Pref = pref;
            string s = " SELECT  a.nzp_serv, a.dat_s, a.dat_po, a.sum_dolg, " +
                       " a.perc, a.dat_month, a.sum_real_p, service_name, a.nzp_kredit  " +
                       " FROM " + Pref + DBManager.sDataAliasRest + "kredit a, " +
                       Pref + DBManager.sKernelAliasRest + "serv_odn d, " +
                       Points.Pref + DBManager.sKernelAliasRest + "services s" +
                       " WHERE nzp_kvar=" + nzpKvar +
                       "        AND d.nzp_serv_link=s.nzp_serv " +
                       "        AND a.nzp_serv=d.nzp_serv_repay " +
                       "        AND dat_month = " + _dateMonth;

            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                while (_reader.Read())
                {
                    Allow = true;
                    var instalment = new Instalment354
                    {
                        NzpServ = Convert.ToInt32(_reader["nzp_serv"]),
                        ServiceName = _reader["service_name"].ToString().Trim(),
                        SumPlat = Convert.ToDecimal(_reader["sum_dolg"].ToString().Trim()),
                        SumPreviosPlat = Convert.ToDecimal(_reader["sum_real_p"].ToString().Trim()),
                        Percent = Convert.ToDecimal(_reader["perc"].ToString().Trim()),
                        NzpKredit = Convert.ToInt32(_reader["nzp_kredit"]),
                        DatMonth = Convert.ToDateTime(_reader["dat_month"])
                    };
                //    GetCurrentSum(instalment);
                    
                    Instalments.Add(instalment);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки рассрочки " + s + " " +
                                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                _reader.Close();
            }
        }

        //private void GetCurrentSum(Instalment354 instalment)
        //{
        //    string tableKredit = Pref + "_charge_" + (_year/100).ToString("00") +
        //                         DBManager.tableDelimiter;

        //    string s = " SELECT  sum_odna12, sum_perc  " +
        //               " FROM " + tableKredit + "kredit a " +
        //               " WHERE nzp_kredit=" + instalment.NzpKredit +
        //               "     AND nzp_serv=" + instalment.NzpServ;
        //    try
        //    {
        //        DBManager.ExecRead(_connection, out _reader, s, true);
        //        while (_reader.Read())
        //        {
        //            instalment.CurrentSumPercent = Convert.ToDecimal(_reader["sum_perc"]);
        //            instalment.CurrentSumInstalment = Convert.ToDecimal(_reader["sum_odna12"]);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog("Ошибка выборки рассрочки из начислений " + s + " " +
        //                            ex.Message, MonitorLog.typelog.Error, 20, 201, true);
        //    }
        //    finally
        //    {
        //        _reader.Close();
        //    }
        //}


      

    }

}


