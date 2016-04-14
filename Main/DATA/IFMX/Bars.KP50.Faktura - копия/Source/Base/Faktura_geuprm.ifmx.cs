

using System;
using System.Data;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaGeuPrm
    {
        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Ссылка на схему/базу с показаниями счетчиков
        /// </summary>
        private string _baseData;

        public string GeuPhone;//Телефон ЖЭУ
        public string GeuAdr;//Адрес ЖЭУ
        public string GeuName;//Наименование ЖЭУ
        public string GeuPref;//Префикс наименование ЖЭУ (ТО, ЖЭУ, РЦ)
        public string GeuDatPlat;//День, до которого следует оплатить счет
        public string GeuKodErc;//Код УК для Казани

        public string GeuRemark; //Примечание УК в счете

        private MyDataReader _reader;
        private int _nzpGeu;// Код дома

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



        public DbFakturaGeuPrm(IDbConnection connDb)
        {
            _connection = connDb;
        
        }




        public void SetGeuField()
        {

            string s = " SELECT  nzp_prm, val_prm "+
                       " FROM " + _baseData + "prm_8  "+
                       " WHERE nzp=" + _nzpGeu+
                       "        AND is_actual=1 "+
                       "        AND dat_s<=" + DBManager.sCurDate+
                       "        AND dat_po>=" + DBManager.sCurDate+
                       "        AND nzp_prm  in (65, 73, 714, 716, 1210, 875) ";
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                while (_reader.Read())
                {
                    switch (Int32.Parse(_reader["nzp_prm"].ToString()))
                    {
                        case 73: GeuName = _reader["val_prm"].ToString().Trim(); break;
                        case 716: GeuPref = _reader["val_prm"].ToString().Trim(); break;
                        case 1210: GeuDatPlat = _reader["val_prm"].ToString().Trim(); break;
                        case 875: GeuAdr = _reader["val_prm"].ToString().Trim(); break;
                        case 65: GeuPhone = _reader["val_prm"].ToString().Trim(); break;
                        case 714: GeuKodErc = _reader["val_prm"].ToString().Trim(); break;
                    }


                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка параметров территории " + s +" "+
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
        public string GetGeuRemark()
        {
            string result = String.Empty;
            //норматив нагрева 1 куб.м воды в гкал
            string s = " SELECT remark  " +
                       " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_remark " +
                       " WHERE nzp_area =  " + _nzpGeu +
                       "       AND nzp_geu = 0 ";
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    result = _reader["remark"] != DBNull.Value ? _reader["remark"].ToString().Trim() : String.Empty;
                    Encoding codepage = Encoding.Default;
                    result = codepage.GetString(Convert.FromBase64String(result));
             
                }
            }
            catch (Exception ex)
            {
                throw new Exception(" Ошибка загрузки примечания к жэу" + ex.Message);
            }

            finally
            {
                _reader.Close();
            }

            return result;
        }


        public void LoadGeuPrm(string apref, int nzpArea)
        {
            if (_nzpGeu == nzpArea) return;
            _nzpGeu = nzpArea;

            SetGeuField();

            GeuRemark = GetGeuRemark();
        }



        public void Clear()
        {
            _nzpGeu = 0;
            GeuPhone =  String.Empty;//Телефон ЖЭУ
            GeuAdr = String.Empty;//Адрес ЖЭУ
            GeuName = String.Empty;//Наименование ЖЭУ
            GeuPref = String.Empty;//Префикс наименование ЖЭУ (ТО, ЖЭУ, РЦ)
            GeuDatPlat = String.Empty;//День, до которого следует оплатить счет
            GeuKodErc = String.Empty;//Код УК для Казани
            GeuRemark = String.Empty; //Примечание в счете

        }

    }
   
}


