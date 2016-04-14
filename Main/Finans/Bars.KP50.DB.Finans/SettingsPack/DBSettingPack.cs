using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Finans.SettingsPack
{
    /// <summary>
    /// Класс для управления настройками создания пачек
    /// </summary>
    public class DBSettingPack : DataBaseHead
    {
        private IDbConnection connection;

        public DBSettingPack()
        {
            connection = GetConnection();
            OpenDb(connection, true);
        }
        /// <summary>
        /// Получение перечня настроек создания пачек
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<SettingsPackPrms> GetSettingsPack(SettingsPackPrms finder, out Returns ret)
        {
            string additWhere = String.Empty;
            string limiOffset= String.Empty;
            if (finder.nzp_bank > 0)
            {
                additWhere = "AND sp.nzp_bank=" + finder.nzp_bank;
            }
            else
            {
                if (finder.rows > 0 || finder.skip > 0)
                {
                    limiOffset = "LIMIT " + finder.rows + " OFFSET " + finder.skip;
                }
            }

            string query = "SELECT sp.nzp_bank, b.bank, " +
                           "MAX (CASE WHEN sp.pack_type=10 THEN sp.pack_type END) as id10, " +
                           "MAX (CASE WHEN sp.pack_type=10 THEN pt.type_name END) as typename10, " +
                           "MAX (CASE WHEN sp.pack_type=20 THEN sp.pack_type END) as id20, " +
                           "MAX (CASE WHEN sp.pack_type=20 THEN pt.type_name END) as typename20 " +
                           "FROM  " + Points.Pref + sDataAliasRest + "s_packsettings sp, " + Points.Pref + sKernelAliasRest + "s_bank b, " +
                           Points.Pref + sKernelAliasRest + "pack_types pt " +
                           "WHERE sp.nzp_bank=b.nzp_bank " + additWhere + " AND sp.pack_type=pt.id " +
                           "GROUP BY sp.nzp_bank, b.bank " +
                           "ORDER BY b.bank " + limiOffset;
            IDataReader reader = null;
            List<SettingsPackPrms> list = new List<SettingsPackPrms>();
            ret = Utils.InitReturns();

            try
            {
                ret = ExecRead(connection, out reader, query, true);
                if (!ret.result)
                {
                    throw new InvalidOperationException("Ошибка получения списка настроек создания пачек");
                }

                while (reader.Read())
                {
                    SettingsPackPrms pack = new SettingsPackPrms();
                    if (reader["nzp_bank"] != DBNull.Value)
                    {
                        pack.nzp_bank = (int) reader["nzp_bank"];
                    }
                    if (reader["bank"] != DBNull.Value)
                    {
                        pack.bank = reader["bank"].ToString();
                    }
                    int idpacktype;
                    string namepacktype = String.Empty;
                    // Оплаты на счет РЦ
                    if (reader["id10"] != DBNull.Value)
                    {
                        idpacktype = (int) reader["id10"];
                        if (reader["typename10"] != DBNull.Value)
                        {
                            namepacktype = reader["typename10"].ToString();
                        }
                        pack.type_pack_list.Add(new PackTypes{id=idpacktype, type_name =namepacktype });
                    }
                    // Оплаты УК и ПУ
                    if (reader["id20"] != DBNull.Value)
                    {
                        idpacktype = (int) reader["id20"];

                        if (reader["typename20"] != DBNull.Value)
                        {
                            namepacktype = reader["typename20"].ToString();
                        }
                        pack.type_pack_list.Add(new PackTypes { id = idpacktype, type_name = namepacktype });
                    }
                    list.Add(pack);
                }
                query = "select count(DISTINCT nzp_bank) FROM " + Points.Pref + sDataAliasRest + "s_packsettings";
                object count = ExecScalar(connection, query, out ret, true);
                if (!ret.result)
                {
                    throw new InvalidOperationException("Ошибка получения количества настроек создания пачек");
                }
                int parsed_count;
                if (!Int32.TryParse(count.ToString(), out parsed_count))
                {
                    throw new InvalidOperationException("Ошибка перобразования количества настроек создания пачек в целое число");
                }
                ret.tag = parsed_count;
            }
            catch (InvalidOperationException ex)
            {
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "/n"+  ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = ex.Message;
                ret.tag = -1;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "/n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
            return list;
        }
        /// <summary>
        /// Добавление новой настройки создания пачки
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns AddSettingPack(SettingsPackPrms finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                if (finder.nzp_bank <= 0)
                {
                    throw new InvalidOperationException("Для добавляемой настройки не указана касса");
                }
                if (finder.type_pack_list == null || finder.type_pack_list.Count == 0)
                {
                    throw new InvalidOperationException("Для добавляемой настройки не указано ни одного типа пачек");
                }
                // проверка на существование
                string sql = "select exists (SELECT 1 FROM " + Points.Pref + sDataAliasRest + "s_packsettings WHERE nzp_bank=" + finder.nzp_bank + ")";
                bool isSettingExists=(bool)ExecScalar(connection, sql, out ret, true);
                if (!ret.result)
                {
                    throw new InvalidOperationException("Ошибка проверки добавляемой настройки на существование");
                }
                if (isSettingExists)
                {
                    throw new InvalidOperationException("Добавляемая настройка пачки с указанным местом формирования уже существует");
                }
                var insertedList = finder.type_pack_list.Select(t => "(" + finder.nzp_bank + "," + t.id + ")").ToList();
                sql = "INSERT INTO " +Points.Pref+ sDataAliasRest + "s_packsettings (nzp_bank, pack_type) " +
                             "VALUES " + String.Join(",", insertedList);
                return ExecSQL(connection, sql, true);
            }
            catch (InvalidOperationException ex)
            {
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "/n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = ex.Message;
                ret.tag = -1;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "/n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
            }
            return ret;
        }
        /// <summary>
        /// Редактирование настройки создания пачки
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns EditSettingPack(SettingsPackPrms finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                if (finder.nzp_bank <= 0)
                {
                    throw new InvalidOperationException("Для редактируемой настройки не указана касса");
                }
                if (finder.type_pack_list == null || finder.type_pack_list.Count == 0)
                {
                    throw new InvalidOperationException("Для редактируемой настройки не указано ни одного типа пачек");
                }
                // Сначала удалим старую запись
                ret = DeleteSettingPack(finder);
                // При успешном удалении
                if (ret.result)
                {
                    // добавим новую
                    ret = AddSettingPack(finder);
                }
            }
            catch (InvalidOperationException ex)
            {
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "/n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                ret.text = ex.Message;
                ret.tag = -1;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "/n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
            }
            return ret;
        }
        /// <summary>
        /// Удаление настройки создания пачки
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns DeleteSettingPack(SettingsPackPrms finder)
        {
            string sql = "DELETE FROM " +Points.Pref+ sDataAliasRest + "s_packsettings WHERE nzp_bank=" + finder.nzp_bank;
            return ExecSQL(connection, sql, true);
        }


    }
}
