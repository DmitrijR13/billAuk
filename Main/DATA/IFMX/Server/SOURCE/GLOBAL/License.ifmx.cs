using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using System.Data;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Модель лицензии
    /// </summary>
    public class License
    {
        public int nzp_payer { get; set; }
        public string key_value { get; set; }
        public string license_number { get; set; }
    }

    /// <summary>
    /// Класс, отвечающий за работу с лицензией на уровне БД
    /// </summary>
    public class DbLicense : DataBaseHead
    {
        /// <summary>
        /// Получение лицензии из базы
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public License GetLicense(int nzp_user, out Returns ret)
        {
            #region Открытие соединения
            var conn = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn, true);
            if (!ret.result) 
                return null;
            #endregion

            #region Проверка существования таблицы
            if (!TableInWebCashe(conn, "keys"))
            {
                ret.text = "Отсутствует таблица с ключами.";
                ret.result = false;
                return null;
            }
            #endregion

            #region SQL
#if PG
  StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT key_value, license_number ");
            sql.Append(" FROM keys ");
            sql.Append(" WHERE nzp_payer = " + nzp_user);
#else
            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT key_value, license_number ");
            sql.Append(" FROM keys ");
            sql.Append(" WHERE nzp_payer = " + nzp_user);
#endif
            #endregion

            try
            {
                #region Считывание данных о лицензии
                IDataReader reader;
                ret = ExecRead(conn, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    return null;
                }

                if (!reader.Read())
                {
                    ret.text = "Не найдена информация о лицензии.";
                    ret.result = false;
                    return null;
                }

                License lic = new License();
                lic.key_value = Convert.ToString(reader["key_value"]);
                lic.license_number = Convert.ToString(reader["license_number"]);
                lic.nzp_payer = nzp_user;
                return lic;
                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка считывания информации о лицензии: " + ex.ToString(), MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка считывания информации о лицензии.";
                return null;
            }
        }

        /// <summary>
        /// Получение номера лицензии
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string GetLicenseNumber(int nzp_user, out Returns ret)
        {
            #region Открытие соединения
            var conn = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn, true);
            if (!ret.result)
                return null;
            #endregion

            #region Получение nzp_payer
#if PG
string sql = " SELECT nzp_payer FROM users WHERE nzp_user = " + nzp_user;
#else
            string sql = " SELECT nzp_payer FROM users WHERE nzp_user = " + nzp_user;
#endif
            int nzp_payer = 0;
            try
            {
                nzp_payer = Convert.ToInt32(ExecScalar(conn, sql, out ret, true));
                if (!ret.result)
                    return null;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения nzp_payer для проверки лицензии: " + ex.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка получения nzp_payer для проверки лицензии. Подробности в журнале.";
                ret.result = false;
                return null;
            }
            #endregion

            #region Получние номера лицензии из prm
            Prm finder = new Prm()
            {
                nzp_user = nzp_user,
                pref = Points.Pref,
                nzp_prm = NzpParams.licenseNumber.GetHashCode(),
                prm_num = 20,
                nzp = nzp_payer,
                month_ = DateTime.Now.Month,
                year_ = DateTime.Now.Year
            };
            DbParameters db = new DbParameters();
            finder = db.FindSimplePrmValue(conn, finder, out ret);
            if (!ret.result)
                return null;
            #endregion

            return finder.val_prm;
        }

        /// <summary>
        /// Перечислитель для номеров параметров
        /// </summary>
        public enum NzpParams
        {
            /// <summary>
            /// Номер лицензии для prm_20
            /// </summary>
            licenseNumber = 1250
        }
    }
}