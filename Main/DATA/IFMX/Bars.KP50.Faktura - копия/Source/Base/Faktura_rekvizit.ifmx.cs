

using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.Base
{
    public class DbFakturaRekvizit
    {
        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Ссылка на схему/базу с показаниями счетчиков
        /// </summary>
        private string _baseData;

        private readonly List<_Rekvizit> _spis;

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

        public DbFakturaRekvizit(IDbConnection connDb)
        {
            _connection = connDb;
            _spis = new List<_Rekvizit>();
        }

        public void GetListCentralUkBankRekvizit(bool local)
        {
            _spis.Clear();
            string tableData;

            if (local) tableData = _baseData;
            else tableData = Points.Pref + DBManager.sDataAliasRest;

            string s = " SELECT *  " +
                       " FROM " + tableData + "s_bankstr " +
                       " ORDER BY nzp_area, nzp_geu  ";
            try
            {
                DataTable dt = DBManager.ExecSQLToTable(_connection, s);
                foreach (DataRow dr in dt.Rows)
                {
                    var uk = new _Rekvizit();

                    if (dr["nzp_geu"] != DBNull.Value) uk.nzp_geu = Convert.ToInt32(dr["nzp_geu"]);
                    if (dr["nzp_area"] != DBNull.Value) uk.nzp_area = Convert.ToInt32(dr["nzp_area"]);
                    if (dr["sb1"] != DBNull.Value) uk.poluch = Convert.ToString(dr["sb1"]).Trim();
                    if (dr["sb2"] != DBNull.Value) uk.bank = Convert.ToString(dr["sb2"]).Trim();
                    if (dr["sb3"] != DBNull.Value) uk.rschet = Convert.ToString(dr["sb3"]).Trim();
                    if (dr["sb4"] != DBNull.Value) uk.korr_schet = Convert.ToString(dr["sb4"]).Trim();
                    if (dr["sb5"] != DBNull.Value) uk.bik = Convert.ToString(dr["sb5"]).Trim();
                    if (dr["sb6"] != DBNull.Value) uk.inn = Convert.ToString(dr["sb6"]).Trim();
                    if (dr["sb7"] != DBNull.Value) uk.phone = Convert.ToString(dr["sb7"]).Trim();
                    if (dr["sb8"] != DBNull.Value) uk.adres = Convert.ToString(dr["sb8"]).Trim();
                    if (dr["sb9"] != DBNull.Value) uk.pm_note = Convert.ToString(dr["sb9"]).Trim();
                    if (dr["sb10"] != DBNull.Value) uk.poluch2 = Convert.ToString(dr["sb10"]).Trim();
                    if (dr["sb11"] != DBNull.Value) uk.bank2 = Convert.ToString(dr["sb11"]).Trim();
                    if (dr["sb12"] != DBNull.Value) uk.rschet2 = Convert.ToString(dr["sb12"]).Trim();
                    if (dr["sb13"] != DBNull.Value) uk.korr_schet2 = Convert.ToString(dr["sb13"]).Trim();
                    if (dr["sb14"] != DBNull.Value) uk.bik2 = Convert.ToString(dr["sb14"]).Trim();
                    if (dr["sb15"] != DBNull.Value) uk.inn2 = Convert.ToString(dr["sb15"]).Trim();
                    if (dt.Columns.Count >17)
                    {
                        if (dr["sb16"] != DBNull.Value) uk.phone2 = Convert.ToString(dr["sb16"]).Trim();
                        if (dr["sb17"] != DBNull.Value) uk.adres2 = Convert.ToString(dr["sb17"]).Trim();
                        if (dr["filltext"] != DBNull.Value) uk.filltext = Convert.ToInt32(dr["filltext"]);
                    }
                    else
                    {
                        uk.filltext = 1;
                    }


                    _spis.Add(uk);

                }


                s = " SELECT *  " +
                    " FROM " + tableData + "prm_10 " +
                    " where nzp_prm=80 and is_actual<>100 order by dat_po desc";
                DBManager.ExecRead(_connection, out _reader, s, true);
                if (_reader.Read())
                {
                    foreach (_Rekvizit uk in _spis)
                    {
                        uk.ercName = _reader["val_prm"].ToString().Trim();
                    }
                }
                _reader.Close();
        
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка выборки реквизитов " + s, MonitorLog.typelog.Error, 20, 201, true);
     
            }
            finally
            {
                _reader.Close();
            }
        }

        public _Rekvizit GetRekvizit(int nzpArea, int nzpGeu, string apref)
        {
            Pref = apref;
            _Rekvizit result = null;
            if (_spis.Count == 0) GetListCentralUkBankRekvizit(false);
            if (_spis.Count == 0) GetListCentralUkBankRekvizit(true);
            
            foreach (_Rekvizit r in _spis)
            {
                if (nzpArea != 5000)//ТСЖ Казани
                {
                    if (r.nzp_area == nzpArea)
                        result = r;
                }
                else
                {
                    if ((r.nzp_area == nzpArea)&(r.nzp_geu == nzpGeu))
                        result = r;
                }
            }

            return result ?? (new _Rekvizit());
        }
   
        public void Clear()
        {
            _spis.Clear();
        }

    }
   
}


