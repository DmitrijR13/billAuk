using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using webroles.TransferData;

namespace webroles.GenerateScriptTable
{
    /// <summary>
    /// Класс для генерации скрипта таблицы s_roles
    /// </summary>
    public class SrolesTableGenScript : GenerateScript
    {
        public int ProfileIdValue { get; set; }

        public override void Request()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper:
                    break;
                case TypeScript.ChangeCustomer:
                    break;
                case TypeScript.FullDeveloper:
                    requestToFullScript("");
                    break;
                case TypeScript.FullCustomer:
                    const string where = " AND " + DBManager.sNvlWord + "(is_dev,0)=0 ";
                    requestToFullScript(where);
                    break;
            }
        }

        public override void GenerateScr()
        {

            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper:
                    break;
                case TypeScript.ChangeCustomer:
                    var chanRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.s_roles);
                    var listchanRows = chanRows as IList<ChangedRow> ?? chanRows.ToList();
                    if (listchanRows.Count() != 0)
                    {
                        generateToChangeScript(listchanRows, Tables.s_roles);
                    }
                    break;
                case TypeScript.FullDeveloper:
                    generateToFullScript("");
                    break;
                case TypeScript.FullCustomer:
                    const string where = " AND " + DBManager.sNvlWord + "(is_dev,0)=0 ";
                    generateToFullScript(where);
                    break;
            }

        }

        // запрос на создание временной таблицы с ролями для конктретного профиля
        private void requestToFullScript(string where)
        {
            var comString = " truncate " + Tables.s_roles_intermed + "; " +
                            " insert into " + Tables.s_roles_intermed +
                            " SELECT " + DBManager.sUniqueWord + " sr.nzp_role FROM " + Tables.profile_roles + "  pr ," + Tables.s_roles + " sr " +
                            " WHERE profile_id=" + ProfileIdValue + " and sr.nzp_role=pr.nzp_role " + where;
            //"WITH profile_roles_sel AS (SELECT DISTINCT nzp_role FROM " + Tables.profile_roles + " WHERE profile_id=" + ProfileIdValue + ") " +
            //"INSERT INTO " + Tables.s_roles_intermed + " (nzp_role) " +
            //"SELECT sr.nzp_role FROM " + Tables.s_roles + " sr, profile_roles_sel " + "WHERE sr.nzp_role=profile_roles_sel.nzp_role " + where + " ORDER BY sr.nzp_role;";
            Returns ret;
            TransferDataDb.ExecuteScalar(comString, out ret);
        }


        // добавление строк для генрации в общую коллекцию
        private void generateToFullScript(string where)
        {
            removeMigratedFromRoles(where);
            var comList = (from DataRow dr in DataTable.Rows
                select "INSERT INTO " + Tables.s_roles + " (nzp_role, role, page_url, sort) VALUES (" +
                       (dr.Field<int?>("nzp_role").HasValue ? dr.Field<int?>("nzp_role").ToString() : "null") + ", " +
                       (!String.IsNullOrEmpty(dr.Field<string>("role")) ? "'" + dr.Field<string>("role") + "'" : "''") + ", " +
                       (dr.Field<int?>("page_url").HasValue ? dr.Field<int?>("page_url").ToString() : "null") + ", " +
                       (dr.Field<int?>("sort").HasValue ? dr.Field<int?>("sort").ToString() : "null") + ")" + ";").ToList();
            comList.Insert(0, "--Заполнение таблицы " + Tables.s_roles);
            comList.Add(string.Empty);
            AddToLinesList(comList);
        }

        private void removeMigratedFromRoles(string where)
        {
            string commandText =
                "delete from " + Tables.s_roles_intermed + " where nzp_role  in (select nzp_role_from from " + Tables.t_role_merging + "); " +
                "SELECT DISTINCT sr.nzp_role, sr.role, sr.page_url,sr.sort FROM " +
                Tables.s_roles + " sr, " + Tables.s_roles_intermed + " sri WHERE sr.nzp_role=sri.nzp_role " + where + " ORDER BY nzp_role;";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
        }
    }
}
