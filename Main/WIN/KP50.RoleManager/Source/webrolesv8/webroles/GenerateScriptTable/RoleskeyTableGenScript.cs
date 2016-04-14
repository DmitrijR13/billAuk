using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using webroles.TransferData;

namespace webroles.GenerateScriptTable
{
    class RoleskeyTableGenScript : GenerateScript
    {



        public override void Request()
        {

            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: break;
                case TypeScript.FullDeveloper: 
                    requestToFullScript(); break;
                case TypeScript.FullCustomer:
                    requestToFullScript(); break;
            }
        }

        public override void GenerateScr()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: 
                      var chanRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.roleskey);
                    var listchanRows = chanRows as IList<ChangedRow> ?? chanRows.ToList();
                    if (listchanRows.Count() != 0)
                    {
                        generateToChangeScript(listchanRows, Tables.roleskey);
                    }
                    break;
                case TypeScript.FullDeveloper:
                    generateToFullScript(); break;
                case TypeScript.FullCustomer:
                    generateToFullScript(); break;
            }
        }

        private void generateToFullScript()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.roleskey + " (nzp_role, tip, kod) VALUES (" +
                                  (dr.Field<int?>("nzp_role").HasValue ? dr.Field<int?>("nzp_role").ToString() : "null") + ", " +
                                  (dr.Field<int?>("tip").HasValue ? dr.Field<int?>("tip").ToString() : "null") + ", " +
                                  (dr.Field<int?>("kod").HasValue ? dr.Field<int?>("kod").ToString() : "null") + ")" + ";").ToList();
            comList.Insert(0, "--Заполнение таблицы " + Tables.roleskey);
            //comList.Insert(1, "DELETE FROM roleskey WHERE nzp_role < 1000;");
            comList.Add(string.Empty);
            AddToLinesList(comList);
        }

        private void requestToFullScript()
        {
            executeOneRequest();
            string commandText =
               "delete from " + Tables.roleskey_intermed + "; " +
               "INSERT INTO " + Tables.roleskey_intermed + "(nzp_role, tip, kod) (SELECT DISTINCT r.nzp_role, tip, kod " +
               "FROM " + Tables.roleskey + " r, " + Tables.s_roles_intermed + " sri " +
               "WHERE r.nzp_role=sri.nzp_role) ORDER BY nzp_role; " +
               "delete from " + Tables.roleskey_intermed + " where tip = 105 and kod < 1000 and kod not in (select nzp_role from " + Tables.s_roles_intermed + ");" +
               "insert into " + Tables.roleskey_intermed + " (nzp_role, tip, kod) " +
               "select   a.nzp_role_to, b.tip, b.kod from " + Tables.t_role_merging + " a, " + Tables.roleskey_intermed + " b where a.nzp_role_from = b.nzp_role; " +
               "DELETE FROM  roleskey_intermed WHERE nzp_role in (SELECT nzp_role_from FROM " + Tables.t_role_merging + ");" +
               "SELECT DISTINCT nzp_role, tip, kod FROM " + Tables.roleskey_intermed + " ORDER BY nzp_role; ";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
        }

        void executeOneRequest()
        {
            var comString =
                "SELECT nzp_rlsv, r.nzp_role, tip, kod FROM " + Tables.roleskey + "  r, " + Tables.s_roles_intermed + "  sri " +
                "WHERE r.nzp_role=sri.nzp_role;";
            Returns ret;
           TransferDataDb.ExecuteScalar(comString, out ret);
        }
    }
}
