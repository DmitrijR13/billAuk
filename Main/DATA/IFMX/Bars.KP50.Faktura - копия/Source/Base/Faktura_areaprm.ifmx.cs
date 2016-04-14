

using System;
using System.Data;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaAreaPrm
    {
        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Расчетный месяц
        /// </summary>
        private readonly string _dateMonth;
        
        public string AreaName;//Наименование УК
        public string AreaDirectorFio;//Фио руководителя УК
        public string AreaDirectorPost;//Должность руководителя УК
        public string AreaAdsPhone;//Телефон аварийно диспетчерской службы УК
        public string AreaPhone;//Телефон УК
        public string AreaAdr;//Адрес УК
        public string AreaEmail;//Адрес email УК
        public string AreaWeb;//Адрес вебсайта УК

        //Реквизиты управляющей организации
        public string Reciever;//Получатель
        public string Bank;//Банк
        public string CurrentAccount;//Расчетный счет
        public string CorrelationAccount;//Корреляционный счет
        public string Bik;//БИК
        public string Inn;//ИНН/КПП
        public string Phone;//Телефон
        public string Address;//Адрес


        public string AreaRemark; //Примечание УК в счете

        private MyDataReader _reader;
        private int _nzpArea;// Код дома

        public string Pref { get; set; }


        public DbFakturaAreaPrm(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _dateMonth = "'" + new DateTime(year, month, 1).ToShortDateString() + "'";
        }




        private void SetAreaField()
        {

            string s = " SELECT   nzp_prm, val_prm " +
                       " FROM " + Points.Pref + DBManager.sDataAliasRest + "prm_7  " +
                       " WHERE nzp=" + _nzpArea +
                       "        AND is_actual=1 " +
                       "        AND dat_s<=" + _dateMonth +
                       "        AND dat_po>=" + _dateMonth +
                       "        AND nzp_prm  in (576, 294, " +
                       "        296, 581, 582, 583, 584) ";
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                while (_reader.Read())
                {
                    switch (Int32.Parse(_reader["nzp_prm"].ToString()))
                    {
                        case 294: AreaAdsPhone = _reader["val_prm"].ToString().Trim(); break;
                        case 296: AreaAdr = _reader["val_prm"].ToString().Trim(); break;
                        case 576: AreaDirectorFio = _reader["val_prm"].ToString().Trim(); break;
                        case 581: AreaDirectorPost = _reader["val_prm"].ToString().Trim(); break;
                        case 582: AreaEmail = _reader["val_prm"].ToString().Trim(); break;
                        case 583: AreaWeb = _reader["val_prm"].ToString().Trim(); break;
                        case 584: AreaPhone = _reader["val_prm"].ToString().Trim(); break;
                    }

                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка параметров территории " + s + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                _reader.Close();
            }
        }

        /// <summary>
        /// Загрузка примечания по УК
        /// </summary>
        private void SetAreaByBankStr()
        {

            string s = " SELECT sb10, sb11, sb12, sb13, sb14, sb15, sb16, sb17 " +
                       " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_bankstr  " +
                       " WHERE nzp_area =" + _nzpArea;
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    if (_reader["sb10"] != DBNull.Value) Reciever = _reader["sb10"].ToString().Trim();
                    if (_reader["sb11"] != DBNull.Value) Bank = _reader["sb11"].ToString().Trim();
                    if (_reader["sb12"] != DBNull.Value) CurrentAccount = _reader["sb12"].ToString().Trim();
                    if (_reader["sb13"] != DBNull.Value) CorrelationAccount = _reader["sb13"].ToString().Trim();
                    if (_reader["sb14"] != DBNull.Value) Bik = _reader["sb14"].ToString().Trim();
                    if (_reader["sb15"] != DBNull.Value) Inn = _reader["sb15"].ToString().Trim();
                    if (_reader["sb16"] != DBNull.Value) Phone = _reader["sb16"].ToString().Trim();
                    if (_reader["sb17"] != DBNull.Value) Address = _reader["sb17"].ToString().Trim();
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка параметров УК " + s + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                _reader.Close();
            }
        }


        /// <summary>
        /// Загрузка примечания к дому
        /// </summary>
        /// <returns></returns>
        private string GetAreaRemark()
        {
            string result = String.Empty;
            //норматив нагрева 1 куб.м воды в гкал
            string s = " SELECT remark  " +
                       " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_remark " +
                       " WHERE nzp_area =  " + _nzpArea +
                       "       AND nzp_geu = 0 ";
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    result = _reader["remark"] != DBNull.Value ? _reader["remark"].ToString().Trim() : String.Empty;
                    Encoding codepage = Encoding.Default;

                    try
                    {

                        result = codepage.GetString(Convert.FromBase64String(result));
                    }
                    catch
                    {

                        result = _reader["remark"].ToString().Trim();
                    }


                }
            }
            catch (Exception ex)
            {
                throw new Exception(" Ошибка загрузки примечания к дому" + ex.Message);
            }

            finally
            {
                _reader.Close();
            }

            return result;
        }


        public void LoadAreaPrm(string apref, int nzpArea)
        {
            if (_nzpArea == nzpArea) return;
            _nzpArea = nzpArea;

            SetAreaField();
            SetAreaByBankStr();

            AreaRemark = GetAreaRemark();
        }



        public void Clear()
        {
            _nzpArea = 0;
            AreaDirectorFio =  String.Empty;//Фио руководителя УК
            AreaDirectorPost = String.Empty;//Должность руководителя УК
            AreaAdsPhone = String.Empty;//Телефон аварийно диспетчерской службы УК
            AreaPhone = String.Empty;//Телефон УК
            AreaAdr = String.Empty;//Адрес УК
            AreaEmail = String.Empty;//Адрес email УК
            AreaWeb = String.Empty;//Адрес вебсайта УК
            AreaRemark = String.Empty;//Адрес вебсайта УК

        }

    }
   
}


