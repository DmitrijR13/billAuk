using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015050804203, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015050804203 : Migration
    {
        List<long> maxNzpResList = new List<long>(); // коллекция max значений колонки nzp_res таблицы resolution по всем банкам
        public override void Apply()
        {
            if (Database.ProviderName != "PostgreSQL") return;
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName points = new SchemaQualifiedObjectName() {Name = "s_point", Schema = CurrentSchema};
            if (!Database.TableExists(points)) return;
            List<string> prefs = new List<string>(); // коллекция преффиксов
            // извлечь все префиксы
            string sql = "select bd_kernel from " + points.ToString();
            IDataReader reader = null;
            try
            {
                reader = Database.ExecuteReader(sql);
                while (reader.Read())
                {
                    if (reader["bd_kernel"] == null) continue;
                    prefs.Add(reader["bd_kernel"].ToString().Trim());
                }

                foreach (string pref in prefs)
                {
                    // формирование наименования таблицы resolution
                    SchemaQualifiedObjectName tableResolution = new SchemaQualifiedObjectName() {Name = "resolution", Schema = pref + "_kernel"};
                    if (!Database.TableExists(tableResolution)) continue;
                    // полное наименование со  схемой (метод ToString переопределен)
                    string tableResolutionFullName = tableResolution.ToString();
                    // проверка существования записи с nzp_res=1000000
                    string sqlBillion = "select exists (select 1 from " + tableResolutionFullName + " where nzp_res=1000000)";
                    bool isBillionExists = (bool) Database.ExecuteScalar(sqlBillion);
                    // если не существует
                    if (!isBillionExists)
                    {
                        // вставка фиктивной записи
                        string sqlInsert = "insert into " + tableResolutionFullName + "(nzp_res, name_short, name_res, is_readonly) " +
                                           " values (1000000, '-','-', 0)";
                        Database.ExecuteNonQuery(sqlInsert);
                    }
                    getMaxValNzpResColumn(tableResolutionFullName);
                }

                if (maxNzpResList.Count < 0) return;
                long max = maxNzpResList.Max();

                foreach (string pref in prefs)
                {
                    SchemaQualifiedObjectName tableResolution = new SchemaQualifiedObjectName() {Name = "resolution", Schema = pref + "_kernel"};
                     string tableResolutionFullName = tableResolution.ToString();
                    string seqName = tableResolution.Schema + Database.TableDelimiter + tableResolution.Name + "_nzp_res_seq";
                    // если последовательность не существует
                    if (!Database.SequenceExists(tableResolution.Schema, tableResolution.Name + "_nzp_res_seq"))
                    {
                        Database.AddSequence(tableResolution.Schema, tableResolution.Name + "_nzp_res_seq");
                    }
                    // изменить значение по умолчанию колонки nzp_res
                    sql = "Alter table " + tableResolutionFullName + " Alter column nzp_res set default nextval('" + seqName + "')";
                    Database.ExecuteNonQuery(sql);
                    //  делаем текущее значение последоваетельности = max
                    sql = "SELECT setval" + "('" +seqName + "', " + max + ")";
                    Database.ExecuteNonQuery(sql);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (reader!= null) reader.Close();
            }
        }

        /// <summary>
        /// добавляет максимальное значение nzp_res в коллекцию 
        /// </summary>
        /// <param name="tableName"></param>
        private void getMaxValNzpResColumn(string tableName)
        {
            string sqlMaxNzpRes = "select max(nzp_res) from " + tableName;
            object maxNzpRes = Database.ExecuteScalar(sqlMaxNzpRes);
            if (maxNzpRes == null || maxNzpRes == DBNull.Value) return;
            long parsedMaxNzpRes;
            if (!Int64.TryParse(maxNzpRes.ToString(), out parsedMaxNzpRes)) return;
            maxNzpResList.Add(parsedMaxNzpRes);
        }
    }
}
