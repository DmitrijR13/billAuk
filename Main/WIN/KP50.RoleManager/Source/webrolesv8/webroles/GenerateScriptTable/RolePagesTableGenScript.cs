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
    class RolePagesTableGenScript : GenerateScript
    {
        private DataTable dtZero;




        public override void Request()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer:
                    break;
                case TypeScript.FullDeveloper: 
                    requestToFullScript(""); break;
                case TypeScript.FullCustomer:
                    const string where = " AND (p.is_dev<>1 OR p.is_dev IS NULL) ";
                    requestToFullScript(where); break;
            }
        }
        public override void GenerateScr()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: 
                       var chanRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.role_pages);
                    var listchanRows = chanRows as IList<ChangedRow> ?? chanRows.ToList();
                    if (listchanRows.Count() != 0)
                    {
                        generateToChangeScript(listchanRows, Tables.role_pages);
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
            string commandText =
               "DELETE FROM " + Tables.role_pages_intermed + ";" +
               "INSERT INTO " + Tables.role_pages_intermed + "(nzp_page, nzp_role) (SELECT DISTINCT rp.nzp_page, rp.nzp_role FROM " + Tables.role_pages + " rp, " + Tables.s_roles_intermed + " sri, " + Tables.pages_intermed_rp + " pirp " +
               "WHERE rp.nzp_role=sri.nzp_role AND rp.nzp_page=pirp.nzp_page) ORDER BY nzp_page; " +
               "INSERT into " + Tables.role_pages_intermed + " (nzp_role,nzp_page) " +
               "SELECT  a.nzp_role_to, b.nzp_page from " + Tables.t_role_merging + " a, " + Tables.role_pages_intermed + " b where a.nzp_role_from = b.nzp_role; " +
               "DELETE FROM  role_pages_intermed WHERE nzp_role in(SELECT nzp_role_from FROM " + Tables.t_role_merging + ");" +
               "SELECT DISTINCT nzp_role, nzp_page FROM " + Tables.role_pages_intermed + " ORDER BY nzp_role, nzp_page; ";

            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
            string commandTextZero = "SELECT rp.nzp_page, rp.nzp_role FROM " + Tables.role_pages + " rp, " + Tables.s_roles_intermed + " sri WHERE rp.nzp_role=sri.nzp_role AND rp.nzp_page=0; ";
            dtZero = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandTextZero, Connect);
            Adapter.Fill(dtZero);
        }


        void executeOneRequest(string where)
        {
            var comString =
                "DELETE FROM " + Tables.pages_intermed_rp + "; " +
                "WITH rol_pag AS (SELECT rp.nzp_role, rp.nzp_page FROM role_pages rp, s_roles_intermed sri, pages p " +
                "WHERE rp.nzp_role=sri.nzp_role AND (p.nzp_page=rp.nzp_page " + where + " )) " +
                "INSERT INTO " + Tables.pages_intermed_rp + " (nzp_page) SELECT DISTINCT rol_pag.nzp_page FROM rol_pag; ";
            Returns ret;
            TransferDataDb.ExecuteScalar(comString, out ret);
        }

        private void generateToFullScript()
        {
            AddToLinesList(new List<string> { "--Заполнение " + Tables.role_pages });
            generateScript(dtZero);
            generateScript(DataTable);
            AddToLinesList(new List<string> { string.Empty });
        }

        private void generateScript(DataTable DataTable)
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.role_pages + " (nzp_role, nzp_page) VALUES (" +
                               (dr.Field<int?>("nzp_role").HasValue ? dr.Field<int?>("nzp_role").ToString() : "null") + ", " +
                               (dr.Field<int?>("nzp_page").HasValue ? dr.Field<int?>("nzp_page").ToString() : "null") + ")" + ";").ToList();
            AddToLinesList(comList);
        }

    }
}
