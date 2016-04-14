

using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaDomPrm
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

        public int CountDomGil // количесво проживающих в доме
        {
            get { return GetCountGil(); }
            set { _countDomGil = value; }
            
        }
        public decimal DomSquare //Общая площадь дома
        {
            get { return GetDomSquare(); }
            set { _domSquare = value; }
            
        }
        public decimal MopSquare //площадь мест общего пользования дома
        {
            get { return GetMopSquare(); }
            set { _mopSquare = value; }
        }

        public int NzpArea; //Код территории, к которой принадлежит ЛС
        public int NzpGeu;//Код участка, к которой принадлежит ЛС

        public int BaseDom //Базовый дом для секции
        {
            get { return GetBaseDom(); }
            set { _baseDom = value; }
        }
        public string UpravDom //Старший по домам
        {
            get { return GetUpravDom(); }
            set { _upravDom = value; }
        }
        public string DomRemark //Примечание по Дому в счете
        {
            get { return GetDomRemark(); }
            set { _domRemark = value; }
        }
        public decimal GkalHeatNorm
        {
            get
            {
                return GetGkalHeatNorm();
            }
            set { _gkalHeatNorm = value; }
        }//Норматив на подогрев 1 куб.м. воды в Гкал
        public decimal OtopCommunalFlatNorm
        {
            get { return GetOtopCommunalFlatNorm(); }
            set { _otopCommunalFlatNorm = value; }
        }//Норма по отоплению для коммунальных квартир
        public decimal OtopOwnFlatNorm
        {
            get { return GetOtopOwnFlatNorm(); }
            set { _otopOwnFlatNorm = value; }
        }//Норма по отоплению для изолированных квартир
        public string Indecs
        {
            get { return GetDomIndecs(); }
            set { _indecs = value; }
        }//Индекс дома
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

        private string _pref;
        private decimal _gkalHeatNorm;
        private string _indecs;
        private decimal _otopOwnFlatNorm;
        private decimal _otopCommunalFlatNorm;
        private MyDataReader _reader;
        private int _nzpDom;// Код дома
        private string _domRemark;
        private int _baseDom;
        private string _upravDom;
        private decimal _mopSquare;
        private decimal _domSquare;
        private int _countDomGil;


        public DbFakturaDomPrm(IDbConnection connDb, int month, int year)
        {
            _connection = connDb;
            _month = month;
            _year = year;
            _dateMonth = "'" + new DateTime(_year, _month, 1).ToShortDateString() + "'";
            Clear();
        }


        /// <summary>
        /// Если дом секционный возращает код базовой секции 
        /// </summary>
        /// <returns></returns>
        public int GetBaseDom()
        {
            if (_baseDom >= 0) return _baseDom;
            _baseDom = 0; 
            string s = " SELECT nzp_dom_base " +
                       " FROM " + _baseData + "link_dom_lit  " +
                       " WHERE nzp_dom = " + _nzpDom;
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true); 
                if (_reader.Read())
                {
                    _baseDom = _reader["nzp_dom_base"] != DBNull.Value ? Int32.Parse(_reader["nzp_dom_base"].ToString()) : 0;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка определения базовой секции дома " + ex.Message + " " +
               ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }

            finally
            {
                _reader.Close(); 
            }
            return _baseDom;

        }

        /// <summary>
        /// Возвращает почтовый индекс дома
        /// </summary>
        /// <returns></returns>
        public string GetDomIndecs()
        {
            if (_indecs != String.Empty) return _indecs;
            _indecs = "";
            string s = " SELECT val_prm  " +
                       " FROM " + _baseData + "prm_2  " +
                       " WHERE nzp_dom= " + _nzpDom +
                       "       AND nzp_prm=68   " +
                       "       AND is_actual=1  " +
                       "       AND dat_s<=" + _dateMonth +
                       "       AND dat_po>=" + _dateMonth;
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    _indecs = _reader["val_prm"] != DBNull.Value ? _reader["val_prm"].ToString().Trim() : String.Empty;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка определения почтового индекса дома " + ex.Message + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);

            }
            finally
            {
                _reader.Close();
            }
            return _indecs;
        }


        /// <summary>
        /// Общая площадь дома
        /// </summary>
        /// <returns></returns>
        public decimal GetDomSquare()
        {
            //if (_domSquare >= 0) return _domSquare;
            _domSquare = 0;
            string s;
            if (GetBaseDom() == 0)
            {
                s = "  SELECT val_prm " +
                           "  FROM " + _baseData + "prm_2 p " +
                           "  WHERE p.nzp_prm=40 " +
                           "        AND p.is_actual=1 " +
                           "        AND p.dat_s<=" + _dateMonth +
                           "        AND p.dat_po>=" + _dateMonth +
                    "        AND p.nzp = " + _nzpDom +
                    "  AND p.dat_s = (select MAX(dat_s) " +
                    "  FROM " + _baseData + "prm_2 a " +
                    "  WHERE a.nzp_prm=40 " +
                    "        AND a.is_actual=1 " +
                    "        AND a.dat_s<=" + _dateMonth +
                    "        AND a.dat_po>=" + _dateMonth +
                    "        AND a.nzp = " + _nzpDom + ") ";
            }
            else
            {
                s = "  SELECT val_prm " +
                        "  FROM " + _baseData + "prm_2 p, " +
                         _baseData + "link_dom_lit d " +
                        "  WHERE p.nzp_prm=40 " +
                        "        AND p.is_actual=1 " +
                        "        AND p.dat_s<=" + _dateMonth + 
                        "        AND p.dat_po>=" + _dateMonth + 
                        "        AND p.nzp=d.nzp_dom " +
                    "        AND d.nzp_dom_base = " + BaseDom +
                    "  AND p.dat_s = (select MAX(dat_s) " +
                    "  FROM " + _baseData + "prm_2 a " +
                    "  WHERE a.nzp_prm=40 " +
                    "        AND a.is_actual=1 " +
                    "        AND a.dat_s<=" + _dateMonth +
                    "        AND a.dat_po>=" + _dateMonth +
                    "        AND a.nzp = " + _nzpDom + ") ";
            }
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                decimal d;
                while (_reader.Read())
                {
                    _domSquare += _reader["val_prm"] != DBNull.Value
                        ? Decimal.TryParse(_reader["val_prm"].ToString().Trim(), out d) ? d : 0
                        : 0;

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка определения площади дома " + ex.Message + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);

            }

            finally
            {
                _reader.Close();
            }
            return _domSquare;

        }


        /// <summary>
        /// Площадь мест общего пользования дома
        /// </summary>
        /// <returns></returns>
        public decimal GetMopSquare()
        {
            //if (_mopSquare >= 0) return _mopSquare;
            _mopSquare = 0;
            string s;
            if (GetBaseDom() == 0)
            {
                s = "  SELECT val_prm " +
                    "  FROM " + _baseData + "prm_2 p " +
                    "  WHERE p.nzp_prm=2049 " +
                    "        AND p.is_actual=1 " +
                    "        AND p.dat_s<=" + _dateMonth +
                    "        AND p.dat_po>=" + _dateMonth +
                    "        AND p.nzp = " + _nzpDom +
                    "  AND dat_s = (select MAX(dat_s) " +
                    "  FROM " + _baseData + "prm_2 a " +
                    "  WHERE a.nzp_prm=2049 " +
                    "        AND a.is_actual=1 " +
                    "        AND a.dat_s<=" + _dateMonth +
                    "        AND a.dat_po>=" + _dateMonth +
                    "        AND a.nzp = " + _nzpDom + ") ";
            }
            else
            {
                s = "  SELECT val_prm " +
                       "  FROM " + _baseData + "prm_2 p, " +
                        _baseData + "link_dom_lit d " +
                       "  WHERE p.nzp_prm=2049 " +
                       "        AND p.is_actual=1 " +
                       "        AND p.dat_s<=" + _dateMonth +
                       "        AND p.dat_po>=" + _dateMonth + 
                       "        AND p.nzp=d.nzp_dom " +
                    "        AND d.nzp_dom_base = " + BaseDom +
                    "  AND p.dat_s = (select MAX(dat_s) " +
                    "  FROM " + _baseData + "prm_2 a " +
                    "  WHERE a.nzp_prm=2049 " +
                    "        AND a.is_actual=1 " +
                    "        AND a.dat_s<=" + _dateMonth +
                    "        AND a.dat_po>=" + _dateMonth +
                    "        AND a.nzp = " + _nzpDom + ") ";
            }
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                decimal d;
                    while (_reader.Read())
                    {
                    _mopSquare += _reader["val_prm"] != DBNull.Value
                        ? Decimal.TryParse(_reader["val_prm"].ToString().Trim(), out d) ? d : 0
                        : 0;
                    }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка определения площади МОП дома " + ex.Message + " " +
              ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }

            finally
            {
                _reader.Close(); 
            }
            return _mopSquare;
        }


        /// <summary>
        /// Получение количества жильцов 
        /// </summary>
        /// <returns></returns>
        public int GetCountGil()
        {
            if (_countDomGil >= 0) return _countDomGil;
            _countDomGil = 0;
            string tablegil = Pref + "_charge_" + (_year - 2000) +
                DBManager.tableDelimiter + "gil_" + _month.ToString("00");
            string s;

            if (GetBaseDom() == 0)
            {
                s = " SELECT sum(cnt2-val5+val3) as count_gil " +
                       " FROM " + tablegil + " a" +
                       " WHERE stek=3  and nzp_dom = " + _nzpDom;
                       
            }
            else
            {
                s = " SELECT sum(a.cnt2 - a.val5 + a.val3) as count_gil " +
                    " FROM " + tablegil + " a, " + _baseData + "link_dom_lit k " +
                    " WHERE a.nzp_dom=k.nzp_dom  and stek=3 and nzp_dom_base = " + BaseDom;
            }
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    decimal d;
                    _countDomGil += Decimal.ToInt32(_reader["count_gil"] != DBNull.Value ?
                        Decimal.TryParse(_reader["count_gil"].ToString().Trim(), out d) ? d : 0 : 0); 
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(" Ошибка определения количестова жильцов в доме " + ex.Message + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);


            }

            finally
            {
                _reader.Close();
            }
            return _countDomGil;
        }


        /// <summary>
        ///  норматив отопления для изолированных квартир
        /// </summary>
        /// <returns></returns>
        public decimal GetOtopOwnFlatNorm()
        {
            if (_otopOwnFlatNorm >= 0) return _otopOwnFlatNorm;
            _otopOwnFlatNorm = 0;
           
            string s = " SELECT val_prm  " +
                      " FROM " + _baseData + "prm_2  " +
                      " WHERE nzp_prm = 723 " +
                      "       AND is_actual = 1 " +
                      "       AND dat_s <= " + _dateMonth + 
                      "       AND dat_po >= " + _dateMonth + 
                      "       AND nzp = " + _nzpDom;
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    decimal d;
                    _otopOwnFlatNorm = _reader["val_prm"] != DBNull.Value ?
                        Decimal.TryParse(_reader["val_prm"].ToString().Trim(), out d) ? d : 0 : 0;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("  Ошибка определения норматива отопления для изолированных квартир " + ex.Message + " " +
 ex.Message, MonitorLog.typelog.Error, 20, 201, true);

            }

            finally
            {
                _reader.Close();
            }
            return _otopOwnFlatNorm;
        }

        /// <summary>
        /// норматив отопления для коммунальных квартир
        /// </summary>
        /// <returns></returns>
        public decimal GetOtopCommunalFlatNorm()
        {
            if (_otopCommunalFlatNorm >= 0) return _otopCommunalFlatNorm;
            _otopCommunalFlatNorm = 0;
          
            string s = " SELECT val_prm  " +
                      " FROM " + _baseData + "prm_2  " +
                      " WHERE nzp_prm = 2074 " +
                      "       AND is_actual = 1 " +
                      "       AND dat_s <= " + _dateMonth +
                      "       AND dat_po >= " + _dateMonth +
                      "       AND nzp = " + _nzpDom;
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    decimal d;
                    _otopCommunalFlatNorm = _reader["val_prm"] != DBNull.Value ?
                        Decimal.TryParse(_reader["val_prm"].ToString().Trim(), out d) ? d : 0 : 0;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("   Ошибка определения норматива отопления для коммунальных квартир  " + ex.Message + " " +
ex.Message, MonitorLog.typelog.Error, 20, 201, true);

            }

            finally
            {
                _reader.Close();
            }
            return _otopCommunalFlatNorm;
        }


        /// <summary>
        /// норматив нагрева 1 куб.м воды в гкал
        /// </summary>
        /// <returns></returns>
        public decimal GetGkalHeatNorm()
        {
            if (_gkalHeatNorm >= 0) return _gkalHeatNorm;
            _gkalHeatNorm = 0;
            string s = " SELECT val_prm  " +
                      " FROM " + _baseData + "prm_2  " +
                      " WHERE nzp_prm = 436 " +
                      "       AND is_actual = 1 " +
                      "       AND dat_s <= " + _dateMonth + 
                      "       AND dat_po >= " + _dateMonth + 
                      "       AND nzp = " + _nzpDom;
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    decimal d;
                    _gkalHeatNorm = _reader["val_prm"] != DBNull.Value ?
                        Decimal.TryParse(_reader["val_prm"].ToString().Trim(), out d) ? d : 0 : 0;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("   Ошибка определения норматива на нагрев 1 куб.м.  " + ex.Message + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);

            }

            finally
            {
                _reader.Close();
            }
            return _gkalHeatNorm;
        }
         

        /// <summary>
        /// Загрузка примечания к дому
        /// </summary>
        /// <returns></returns>
        public string GetDomRemark()
        {
            if (_domRemark != String.Empty) return _domRemark;
            _domRemark = "";
            string s = " SELECT remark  " +
                          " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_remark " +
                          " WHERE nzp_dom =  " + _nzpDom;
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    _domRemark = _reader["remark"] != DBNull.Value ? _reader["remark"].ToString().Trim() : "";
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("   Ошибка загрузки примечания к дому  " + ex.Message + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }

            finally
            {
                _reader.Close();
            }

            return _domRemark;
        }


        public string GetUpravDom()
        {
            if (_upravDom != String.Empty) return _upravDom;
            _upravDom = "";
            string s = "  Select trim(fam)||' '||trim(ima)||' '||' '||trim(otch)||' '||trim(adres) as mdom " +
                       "  From " + _baseData + "h_master a," +
                                   _baseData + "h_link b " +
                       "  Where a.nzp_hm=b.nzp_hm " +
                       "        AND nzp_dom= " + _nzpDom +
                       "        AND kod=2 ";
            try
            {
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    _upravDom = _reader["val_prm"] != DBNull.Value ? _reader["val_prm"].ToString().Trim() : "";
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(" Ошибка загрузки управдома  " + ex.Message + " " +
                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                
            }

            finally
            {
                _reader.Close();
            }
            return _upravDom;
        }


        /// <summary>
        /// Загрузка домовых параметров
        /// </summary>
        /// <param name="apref"></param>
        /// <param name="nzpDom"></param>
        public void SetNzpDom(string apref, int nzpDom)
        {
            if (_nzpDom == nzpDom) return;
            _nzpDom = nzpDom;
            Pref = apref;
        }





        public void Clear()
        {
            _nzpDom = 0;
            _gkalHeatNorm = -1;
            _indecs = String.Empty;
            _otopOwnFlatNorm = -1;
            _otopCommunalFlatNorm = -1;
            _domRemark = String.Empty;
            _baseDom = -1;
            _upravDom = String.Empty;
            _mopSquare = -1;
            _domSquare = -1;
            _countDomGil = -1;
            NzpArea = 0; //Код территории, к которой принадлежит ЛС
            NzpGeu = 0; //Код участка, к которой принадлежит ЛС

        }

    }
   
}


