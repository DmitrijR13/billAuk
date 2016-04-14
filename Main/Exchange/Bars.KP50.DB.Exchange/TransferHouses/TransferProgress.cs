using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Exchange.TransferHouses
{
    class TransferProgress
    {
        private static IDbConnection Connection;
        private static readonly string connectionString = Constants.cons_Kernel;
        /// <summary>Открыть соединение с БД</summary>
        /// <param name="inlog">Логировать да/нет</param>
        protected virtual void OpenDb(bool inlog = true)
        {
            var result = DBManager.OpenDb(Connection, inlog);
            if (!result.result)
            {
                throw new Exception(result.text);
            }
        }

        /// <summary>Открыть соединение</summary>
        /// <returns>Открытое соединение</returns>
        private static IDbConnection OpenConnection()
        {
            if (Connection == null)
            {
                Connection = DBManager.GetConnection(connectionString);
                var result = DBManager.OpenDb(Connection, true);
                if (!result.result)
                {
                    throw new Exception(result.text);
                }
            }

            return Connection;
        }


        /// <summary>Закрыть соединение с БД</summary>
        private static void CloseConnection()
        {
            try
            {
                if (Connection != null)
                {
                    Connection.Close();
                    Connection = null;
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Не удалось закрыть соединение", exc);
            }
        }

        /// <summary>
        /// Обновить поле прогресс (3 параметра)
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="transfer_id"></param>
        /// <param name="status_id"></param>
        public static void UpdateProgress(decimal progress, int transfer_id, int status_id)
        {
            try
            {
                OpenConnection();
                DBManager.ExecSQL(Connection, string.Format("update {0}transfer_data_log set(progress,status) = ({1},{2}) where transfer_id = {3}",
                    Points.Pref + DBManager.sDataAliasRest, progress.ToString(CultureInfo.InvariantCulture), status_id, transfer_id), true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Произошла ошибка при обновлении поля прогресс ", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при обновлении поля прогресс ", ex);
                }
                throw;
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Обновить поле прогресс (2 параметра)
        /// </summary>
        /// <param name="transfer_id"></param>
        /// <param name="status_id"></param>
        public static void UpdateProgress(int transfer_id, int status_id)
        {
            try
            {
                OpenConnection();
                DBManager.ExecSQL(Connection, string.Format("update {0}transfer_data_log set(status) = ({1}) where transfer_id = {2}",
                     Points.Pref + DBManager.sDataAliasRest, status_id, transfer_id), true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Произошла ошибка при обновлении поля прогресс ", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при обновлении поля прогресс ", ex);
                }
                throw;
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Добавление записи по домам в лог переноса
        /// </summary>
        /// <param name="transfer_id"></param>
        /// <param name="nzp_dom"></param>
        /// <param name="is_transfer"></param>
        public static void InsertIntoHouseLog(int transfer_id, int nzp_dom, int is_transfer, string error)
        {
            try
            {
                OpenConnection();
                DBManager.ExecSQL(Connection, string.Format("insert into {0}transfer_house_log (transfer_id,nzp_dom,is_transfer,error) values ({1},{2},{3},'{4}');",
                     Points.Pref + DBManager.sDataAliasRest, transfer_id, nzp_dom, is_transfer, error), true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Произошла ошибка при логировании результатов переноса домов ", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка при логировании результатов переноса домов  ", ex);
                }
                throw;
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        ///  Добавление записи в лог переноса
        /// </summary>
        /// <returns></returns>
        public static int InsertIntoTransferDataLog(int nzp_user)
        {
            try
            {
                OpenConnection();
                DBManager.ExecSQL(Connection, string.Format("insert into {0}transfer_data_log (progress,status,created_on,nzp_user) values ({1},{2},{3},{4})",
                Points.Pref + DBManager.sDataAliasRest, 0, 1, DBManager.sCurDateTime, nzp_user), true);
                return DBManager.GetSerialValue(Connection);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Произошла ошибка", ex);
                if (ex.InnerException != null)
                {
                    MonitorLog.WriteException("Произошла ошибка", ex);
                }
                throw;
            }
            finally
            {
                CloseConnection();
            }
        }

    }
}
