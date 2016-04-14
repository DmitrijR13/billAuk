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
    internal class RoleActionsTableGenScript : GenerateScript
    {


        public override void Request()
        {
            string where = "";
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: break;
                case TypeScript.FullDeveloper:
                    where = ""; 
                    requestToFullScript(where); break;
                case TypeScript.FullCustomer:
                     where = " and "+DBManager.sNvlWord+"(is_dev,0)=0 ";
                    requestToFullScript(where); break;
            }
            
        }
        public override void GenerateScr()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: 

                      var chanRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.role_actions);
                    var listchanRows = chanRows as IList<ChangedRow> ?? chanRows.ToList();
                    if (listchanRows.Count() != 0)
                    {
                        generateToChangeScript(listchanRows, Tables.role_actions);
                    }
                    break;
                case TypeScript.FullDeveloper:
                    generateToFullScript(); break;
                case TypeScript.FullCustomer:
                    generateToFullScript(); break;
            }
        }

        private void requestToFullScript(string where)
        {
            executeOneRequest(where);
            Returns ret = new Returns(true, "");
            string commandText = "DELETE FROM " + Tables.role_actions_intermed;
            ret = TransferDataDb.ExecSQL(commandText);
            // сначала заполняем промежуточную таблицу с действиями ролей (role_actions_intermed) на основе pages_intermed и s_actions_intermed
            commandText = "INSERT INTO " + Tables.role_actions_intermed + "(nzp_role, nzp_page, nzp_act, mod_act, sign)" +
                          " (SELECT DISTINCT ra.nzp_role, ra.nzp_page,ra.nzp_act, ra.mod_act, ra.sign " +
                          "FROM " + Tables.role_actions + " ra, " + Tables.s_roles_intermed + " sri, " + Tables.pages_intermed + " pi, " + Tables.s_actions_intermed + " sai " +
                          "WHERE ra.nzp_role=sri.nzp_role AND ra.nzp_page=pi.nzp_page AND ra.nzp_act=sai.nzp_act) ORDER BY nzp_page";
            ret = TransferDataDb.ExecSQL(commandText);
            // затем вставляем роли из t_role_merging
            commandText = "insert into " + Tables.role_actions_intermed + " (nzp_role, nzp_page, nzp_act, mod_act) " +
                          "select   a.nzp_role_to, b.nzp_page, b.nzp_act, b.mod_act FROM " + Tables.t_role_merging + " a, "
                          + Tables.role_actions_intermed + " b where a.nzp_role_from = b.nzp_role";
            ret = TransferDataDb.ExecSQL(commandText);
            // Удаление из  временной role_actions_intermed тех записей, роли которых равны колонке nzp_role_from таблицы t_role_merging
            commandText = "Delete FROM  " + Tables.role_actions_intermed + " WHERE nzp_role in (SELECT nzp_role_from FROM " + Tables.t_role_merging + ")";
            ret = TransferDataDb.ExecSQL(commandText);
            // Извлекаем сформированную таблицу role_actions_intermed, для последующей выгрузки строк
            commandText = "SELECT DISTINCT nzp_role, nzp_page, nzp_act, mod_act, sign FROM " + Tables.role_actions_intermed +
                          " ORDER BY nzp_role, nzp_page, nzp_act";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
        }

        private void generateToFullScript()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.role_actions + " (nzp_role, nzp_page, nzp_act, mod_act) VALUES (" +
                       (dr.Field<int?>("nzp_role").HasValue ? dr.Field<int?>("nzp_role").ToString() : "null") + ", " +
                       (dr.Field<int?>("nzp_page").HasValue ? dr.Field<int?>("nzp_page").ToString() : "null") + ", " +
                       (dr.Field<int?>("nzp_act").HasValue ? dr.Field<int?>("nzp_act").ToString() : "null") + ", " +
                       (dr.Field<int?>("mod_act").HasValue ? dr.Field<int?>("mod_act").ToString() : "null") + ");").ToList();
            //  (!String.IsNullOrEmpty(dr.Field<string>("sign")) ? "'" + dr.Field<string>("sign") + "'" : "''") + 

            comList.Insert(0, "--Заполнение таблицы " + Tables.role_actions);
            comList.Add(string.Empty);
            AddToLinesList(comList);
        }

        void executeOneRequest(string where)
        {
            Returns ret;
            // заполняем во временную таблицу страницы из доступных  ролей и действий на основе role_actions
            var comString =
                "DELETE FROM " + Tables.pages_intermed + "; " +
                " insert into " + Tables.pages_intermed + "(nzp_page) " +
                " select " + DBManager.sUniqueWord + " p.nzp_page from " + Tables.pages + " p, " + Tables.role_actions + " r, " + Tables.s_roles_intermed + " sri " +
                " where p.nzp_page= r.nzp_page and sri.nzp_role=r.nzp_role " + where;
            TransferDataDb.ExecuteScalar(comString, out ret);
                //"WITH sel AS (SELECT DISTINCT ra.nzp_page, ra.nzp_act FROM role_actions ra , pages p, s_actions sa, " +Tables.s_roles_intermed+" sri "+
                //"WHERE " + where + " ), " +
                //"pages_intermed_insert AS (INSERT INTO " + Tables.pages_intermed + "(nzp_page) SELECT DISTINCT sel.nzp_page FROM sel RETURNING *) " +
                //"INSERT INTO " + Tables.s_actions_intermed + "(nzp_act) SELECT DISTINCT sel.nzp_act FROM sel ORDER BY nzp_act;";

            comString = "DELETE FROM " + Tables.s_actions_intermed + "; " +
                        " insert into "+Tables.s_actions_intermed+" (nzp_act) " +
                        " select " + DBManager.sUniqueWord + " s.nzp_act from "+Tables.s_actions+" s, "+Tables.role_actions+" r, "+Tables.s_roles_intermed+" sri " +
                        " where s.nzp_act=r.nzp_act and r.nzp_role= sri.nzp_role " + where;
                //"SELECT role_actions.id, role_actions.nzp_role, role_actions.nzp_page, role_actions.nzp_act, role_actions.mod_act " +
                //"FROM role_actions, pages_intermed, s_actions_intermed, s_roles_intermed " +
                //"WHERE role_actions.nzp_role=s_roles_intermed.nzp_role AND role_actions.nzp_page=pages_intermed.nzp_page AND role_actions.nzp_act=s_actions_intermed.nzp_act ORDER BY id; " ;
             TransferDataDb.ExecuteScalar(comString, out ret);
        }
    }
}
