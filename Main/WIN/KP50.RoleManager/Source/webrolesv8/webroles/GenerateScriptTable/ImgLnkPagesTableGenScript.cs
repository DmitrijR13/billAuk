using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace webroles.GenerateScriptTable
{
    class ImgLnkPagesTableGenScript : GenerateScript
    {
        private DataTable genImgDataTable;



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
                        var imgLnkRow = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.img_lnk );
                    var listimgLnkRows = imgLnkRow as IList<ChangedRow> ?? imgLnkRow.ToList();
                    if (listimgLnkRows.Count() != 0)
                    {
                        generateToChangeScript(listimgLnkRows, Tables.img_lnk);
                    }
                    break;
                case TypeScript.FullDeveloper:
                    generateToFullScript(); break;
                case TypeScript.FullCustomer:
                    generateToFullScript(); break;
            }
        }

        private void requestToFullScript()
        {
            string commandText = "WITH pageun AS (SELECT * FROM " + Tables.pages_intermed + " UNION SELECT * FROM " + Tables.pages_intermed_rp + ") " +
                                     "SELECT DISTINCT nzp_img, cur_page, tip, kod, img_url FROM  " + Tables.img_lnk + ", pageun WHERE cur_page=pageun.nzp_page AND tip=1; ";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);

            string commandGenImg =
                                   "SELECT DISTINCT nzp_img, cur_page, tip, kod, img_url FROM " + Tables.img_lnk + " WHERE cur_page=0 AND tip=1; ";
            genImgDataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandGenImg, Connect);
            Adapter.Fill(genImgDataTable);
        }



        private void generateToFullScript()
        {
            AddToLinesList(new List<string> { "--Добавление картинки для пункта меню" });
            generateScript(genImgDataTable);
            generateScript(DataTable);
            AddToLinesList(new List<string> { string.Empty });
        }

        private void generateScript(DataTable dataTableataTable)
        {
          var comList = (from DataRow dr in dataTableataTable.Rows
                         select "INSERT INTO " + Tables.img_lnk + " (cur_page, tip, kod, img_url) VALUES (" +
                                  (dr.Field<int?>("cur_page").HasValue ? dr.Field<int?>("cur_page").ToString() : "null") + ", " +
                                  (dr.Field<int?>("tip").HasValue ? dr.Field<int?>("tip").ToString() : "null") + ", " +
                                  (dr.Field<int?>("kod").HasValue ? dr.Field<int?>("kod").ToString() : "null") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("img_url")) ? "'" + dr.Field<string>("img_url") + "'" : "''") + ");").ToList();
            if (comList.Count == 0) return;
            AddToLinesList(comList);

        }
    }
}
