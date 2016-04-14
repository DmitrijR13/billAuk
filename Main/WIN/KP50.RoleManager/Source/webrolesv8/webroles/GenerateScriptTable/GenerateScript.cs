using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using webroles.TablesFirstLevel;
using webroles.Windows;

namespace webroles.GenerateScriptTable
{
    public class ScriptCollectionEventArgs : EventArgs
    {
        private int count;
        public ScriptCollectionEventArgs(int count)
        {
            this.count = count;
        }

        public int Count
        {
            get { return count; }
        }
    }
    
    public abstract class GenerateScript
    {
       
        public static bool NotRepeatCreateForScript ;
        public static List<ChangedRow> ChangedRowCollection= new List<ChangedRow>();
        public static List<string> DropTriggersList;
        public static List<List<string>> LinesList;
        public static TypeScript TypeScr;
        public static event EventHandler<ScriptCollectionEventArgs> ScriptCollectionChanged;

        static void onRiseEvent()
        {
            if (ScriptCollectionChanged!=null)
                ScriptCollectionChanged(new object(),new ScriptCollectionEventArgs(ChangedRowCollection.Count));
        }
        public static void AddToLinesList(List<string> lines)
        {
            if (LinesList== null) LinesList = new List<List<string>>();
            LinesList.Add(lines);
        }

        public static void ClearChangedRowCollection()
        {
            ChangedRowCollection.Clear();
            onRiseEvent();
        }

        public static void AddToDropTriggersList(List<string> lines)
        {
            if (DropTriggersList == null) DropTriggersList = new List<string>();
            DropTriggersList.AddRange(lines);
        }

        public static void ClearLineList ()
        {
             if (LinesList!= null) LinesList.Clear();
             if (DropTriggersList != null)DropTriggersList.Clear();
        }
        public static bool CheckCellToNullValue(DataGridViewCell cell)
        {
            if (cell.ValueType == typeof(int))
            {
                if (cell.Value == DBNull.Value)
                {
                    return true;
                }
            }

            if (cell.ValueType == typeof(string))
            {
                if (cell.Value == DBNull.Value)
                {
                    return true;
                }
            }
            return false;
        }
        public static string GetCellValue(DataGridViewCell cell)
        {
            if (cell.Value == DBNull.Value)
            {
                return "null";
            }
            if (cell.ValueType == typeof(int))
            {
                return cell.Value.ToString();
            }

            if (cell.ValueType == typeof(string))
            {
                return "'" + cell.Value + "'";
            }
            return "null";
        }
        public static void CheckOnSameData(string tableName, int id =0) 
        {
           if  (ChangedRowCollection.Count==0) return;
           IEnumerable<ChangedRow> chr = new List<ChangedRow>();
            chr = ChangedRowCollection.Select(rowChan => rowChan)
                       .Where(
                           rowChan =>
                                rowChan.TableName == tableName &&
                                (rowChan.IdNum == id));

            while (chr.Count() != 0)
            {
                ChangedRowCollection.Remove(chr.First());
            }
            onRiseEvent();
        }
        public static object checkTypeCell(DataGridViewCell cell)
        {
            if (cell.ValueType == typeof (int))
                return cell.Value;
            if (cell.ValueType == typeof(string))
                return "'"+ cell.Value+"'";
            return "null";
        }
        public static ChangedRow AddVal(string tableName, Dictionary<string, string> valueColumns, Dictionary<string, string> whereColumns, Dictionary<string, int> idColValue, ScriptGenerateOper typeScr, int valueBaseColumn = -1)
        {
            // проверка на повторяемость
            CheckOnSameData(tableName,  idColValue.FirstOrDefault().Value);
            onRiseEvent();
            ChangedRow chRow = new ChangedRow(tableName, idColValue.FirstOrDefault().Key, idColValue.FirstOrDefault().Value, valueBaseColumn);
            switch (typeScr)
            {
                case ScriptGenerateOper.Insert:
                    chRow.State= DataRowState.Added;
                    foreach (KeyValuePair<string,string> valueCol in valueColumns)
                    {
                        chRow.AddToValuesDictionaty(valueCol.Key, valueCol.Value);
                    }
                    ChangedRowCollection.Add(chRow);
                    onRiseEvent();
                    return chRow;
                case ScriptGenerateOper.Delete:
                    chRow.State = DataRowState.Deleted;
                    foreach (KeyValuePair<string, string> whereCol in whereColumns)
                    {
                        chRow.AddToWhereDictionaty(whereCol.Key, whereCol.Value);
                    }
                    ChangedRowCollection.Add(chRow);
                    onRiseEvent();
                    return chRow;
                case ScriptGenerateOper.Update:
                    UpdateRowWindow urw = new UpdateRowWindow(valueColumns, tableName, idColValue);
                    if (DialogResult.OK == urw.ShowDialog())
                    {
                        foreach (KeyValuePair<string, string> whereCol in whereColumns)
                        {
                            urw.UpdateChangedRow.AddToWhereDictionaty(whereCol.Key,whereCol.Value);
                        }
                        urw.UpdateChangedRow.ValueBaseColumn = valueBaseColumn;
                        ChangedRowCollection.Add(urw.UpdateChangedRow);
                    }
                    urw.Close();
                    onRiseEvent();
                    return urw.UpdateChangedRow;
                default:
                    onRiseEvent();
                    return chRow;
            }
            
        }
        public static void InsertPrevValuesForScript(string tableName, DataGridView dgv, int position=-1)
        {
            string tabName = tableName;
            if (tableName.Length > Tables.img_lnk.Length)
            {
                string img_lnkTable = tableName.Substring(0, Tables.img_lnk.Length);
                if (img_lnkTable == Tables.img_lnk)
                {
                    tabName = Tables.img_lnk;
                }
            }
            if (ChangedRowCollection.Count==0) return;
            var chRows = ChangedRowCollection.Select(row=>row).Where(row => row.TableName == tabName);
            var changedRows = chRows as ChangedRow[] ?? chRows.ToArray();
            if (!changedRows.Any()) return;
            // Если таблица dataGridView пустая, то удаляем все экземпляры связанные с этой таблицей 
            if (dgv.Rows.Count == 0)
            {
                if (position != -1)
                {
                    var arr = changedRows.Select(row => row).Where(row => position == row.ValueBaseColumn);
                                                                
                    foreach (var ar in arr)
                    {
                        ChangedRowCollection.Remove(ar);
                    }
                    onRiseEvent();
                    return;
                }
                for (int i=0;i<changedRows.Length;i++)
                    ChangedRowCollection.Remove(changedRows[i]);
                onRiseEvent();
                return;
            }
    
            foreach (var row in changedRows)
            {
                bool continueCycle = false;
               
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    // Если вызов пришел при входе или сохранения таблицы второго уровня,

                    if (position != -1)
                    {
                        // Если значение базовой колонки не совпадает, выйти
                        // Комментарий: Для таблиц второго уровня нужно сравнивать Position cо значением базовой колонки, т.к. удалять строки только по IdColumn 
                        // может удалить все.
                        if (position !=
                            (row.ValueBaseColumn))
                        {
                            continueCycle = true;
                            break;
                        }
                    }
                   
                    if ((int) dgv.Rows[i].Cells[row.NameIdColumn].Value == row.IdNum)
                    {
                        // Для таблиц role_actions и role_pages не перепроверять записи
                        if (tabName != Tables.role_actions & tableName != Tables.role_pages)
                        {
                            updateValduringInsertOrUpdate(dgv, i, row.ColValuesDictionary);
                            updateValduringInsertOrUpdate(dgv, i, row.WhereColValuesDictionary);
                        }
                        var forscriptValue = dgv.Rows[i].Cells["forscript"].Value;
                        switch (row.State)
                        { 
                            case DataRowState.Added:
                                if ((forscriptValue == null || forscriptValue == DBNull.Value) || (int)forscriptValue!=1)
                                {
                                    NotRepeatCreateForScript = true;
                                }
                                 dgv.Rows[i].Cells["forscript"].Value = 1;
                                    continueCycle = true;
                                    break;
                            case DataRowState.Modified:
                                    if ((forscriptValue == null || forscriptValue == DBNull.Value) || (int)forscriptValue != 2)
                                    {
                                        NotRepeatCreateForScript = true;
                                    }
                                dgv.Rows[i].Cells["forscript"].Value = 2;
                                continueCycle = true;
                                break;
                            case DataRowState.Deleted:
                                if ((forscriptValue == null || forscriptValue == DBNull.Value) || (int)forscriptValue != 3)
                                {
                                    NotRepeatCreateForScript = true;
                                }
                                dgv.Rows[i].Cells["forscript"].Value = 3;
                                continueCycle = true;
                                break;
                            default:
                                if ((forscriptValue != null && forscriptValue != DBNull.Value))
                                {
                                    NotRepeatCreateForScript = true;
                                }
                                continueCycle = true;
                                dgv.Rows[i].Cells["forscript"].Value = DBNull.Value;
                                break;
                        }
                    }
                   // if (exitCycle) break;
                }
                if (continueCycle) continue;
                
                ChangedRowCollection.Remove(row);
                onRiseEvent();
            }
        }
        private static void updateValduringInsertOrUpdate( DataGridView dgv, int i, Dictionary<string, string> colValuesDictionary)
        {
                Dictionary<string, string> d = new Dictionary<string, string>();
                foreach (var val in colValuesDictionary)
                {
                    if (dgv.Rows[i].Cells[val.Key].Value == null && val.Value == "null") continue;
                    if (dgv.Rows[i].Cells[val.Key].Value == DBNull.Value || dgv.Rows[i].Cells[val.Key].Value == null)
                    {
                        d.Add(val.Key, "null");
                        continue;
                    }
                    var value = dgv.Rows[i].Cells[val.Key].Value;
                    if (dgv.Rows[i].Cells[val.Key].ValueType == typeof (string))
                    {
                        value = "'" + value + "'";
                    }
                    if (value != null && value.ToString() != val.Value)
                    {
                        d.Add(val.Key, value.ToString());
                    }
                }

                foreach (var pair in d)
                {
                    colValuesDictionary[pair.Key] = pair.Value;
                }
        }
        public void generateToChangeScript(IEnumerable<ChangedRow> listPagesRows, string tableName)
        {
            List<string> ls = new List<string>();
            var pagesRows = listPagesRows as ChangedRow[] ?? listPagesRows.ToArray();
            var insertRows = pagesRows.Select(row => row).Where(row => row.State == DataRowState.Added);
            IEnumerable<ChangedRow> changedRows = insertRows as ChangedRow[] ?? insertRows.ToArray();
            if (changedRows.Count() != 0)
            {
               // AddToLinesList(beforeInsert());
                foreach (var insertRow in changedRows)
                {
                    ls = new List<string>();
                    int i = 1;
                    string cols = "";
                    string values = "";
                    foreach (var changedRow in insertRow.ColValuesDictionary)
                    {
                        cols += ((i == 1) ? " (" : "") + changedRow.Key + (i == insertRow.ColValuesDictionary.Count ? ")" : ", ");
                        i++;
                    }
                    i = 1;
                    foreach (var changedRow in insertRow.ColValuesDictionary)
                    {
                        values += ((i == 1) ? " (" : "") + changedRow.Value + (i == insertRow.ColValuesDictionary.Count ? ");" : ", ");
                        i++;
                    }
                    string s = "INSERT INTO " + tableName + cols + " VALUES " + values;

                    ls.Add(s);
                    AddToLinesList(ls);
                }
                ls.Add(string.Empty);
            }

            var updateRows = pagesRows.Select(row => row).Where(row => row.State == DataRowState.Modified);
            var rows = updateRows as ChangedRow[] ?? updateRows.ToArray();
            if (rows.Count() != 0)
            {


                foreach (var updateRow in rows)
                {
                    if (updateRow.ColValuesDictionary.Count == 0) continue;
                    ls = new List<string>();
                    string set = "";
                    string where = "";
                    int i = 1;
                    foreach (var changedRow in updateRow.ColValuesDictionary)
                    {
                        set += changedRow.Key + "=" + changedRow.Value + (updateRow.ColValuesDictionary.Count == i ? " " : ", ");
                        i++;
                    }

                    i = 1;
                    foreach (var changedRow in updateRow.WhereColValuesDictionary)
                    {
                        if (changedRow.Value == "null")
                        {
                            where += changedRow.Key + " is " + changedRow.Value + (updateRow.WhereColValuesDictionary.Count == i ? ";" : " and ");
                            i++;
                            continue;
                        }
                        where += changedRow.Key + "=" + changedRow.Value + (updateRow.WhereColValuesDictionary.Count == i ? ";" : " and ");
                        i++;
                    }

                    string s = "UPDATE " + tableName + " Set " + set + "WHERE " + where;
                    ls.Add(s);
                    AddToLinesList(ls);
                }
                ls.Add(string.Empty);
            }

            var deleteRows = pagesRows.Select(row => row).Where(row => row.State == DataRowState.Deleted);
           // var enumerable = deleteRows as ChangedRow[] ?? deleteRows.ToArray();
            var enumerable = deleteRows as ChangedRow[] ?? deleteRows.ToArray();
            if (enumerable.Count() != 0)
            {
              //  AddToLinesList(beforeDelete());
                foreach (var deleteRow in enumerable)
                {
                    ls = new List<string>();
                    string where = "";
                    int i = 1;
                    foreach (var changedRow in deleteRow.WhereColValuesDictionary)
                    {
                        if (changedRow.Value == "null")
                        {
                            where += changedRow.Key + " is " + changedRow.Value + (deleteRow.WhereColValuesDictionary.Count == i ? ";" : deleteRow.WhereDelimeter);
                            i++;
                            continue;
                        }

                        where += changedRow.Key + "=" + changedRow.Value + (deleteRow.WhereColValuesDictionary.Count == i ? ";" : deleteRow.WhereDelimeter);
                        i++;
                    }
                    string s = "DELETE FROM " + tableName + " WHERE " + where;
                    ls.Add(s);
                    AddToLinesList(ls);
                }
                ls.Add(string.Empty);
            }
        }
        // Этот метод вызывается только с классов RolePagesTable и RoleActionsTAble. Для этих таблиц метод нужен отдельный т.к. возможно слияние одной роли в несколько ролей.
        public static ChangedRow AddValSpecialForRolePagesAndRoleActions(string tableName, Dictionary<string, string> whereColumns, Dictionary<string, string> valColumns, Dictionary<string, int> idColValue, ScriptGenerateOper typeScr, int valueBaseColumn, bool isSroles=false)
        {
            CheckOnSameData(tableName, idColValue.FirstOrDefault().Value);
            Dictionary<int, string> rolesList = new Dictionary<int, string>();
            ChangedRow chRow = null;
            switch (typeScr)
            {
                case ScriptGenerateOper.Insert:
                    // CheckOnSameData(tableName, id);
                    rolesList = SrolesTable.CheckRoles(valueBaseColumn, isSroles);
                    if (rolesList == null)
                    {
                        return null;
                    }
                    if (rolesList.Count > 0)
                    {
                        // Формирование словаря значений
                        foreach (var role in rolesList)
                        {
                            chRow = new ChangedRow(tableName, idColValue.FirstOrDefault().Key, idColValue.FirstOrDefault().Value, valueBaseColumn);
                            chRow.State = DataRowState.Added;
                            foreach (KeyValuePair<string, string> valueCol in valColumns)
                            {
                                if (valueCol.Key == "nzp_role")
                                {
                                    chRow.AddToValuesDictionaty(valueCol.Key, role.Key.ToString());
                                }
                                else if (valueCol.Key == "role")
                                {
                                    chRow.AddToValuesDictionaty(valueCol.Key, "'" + role.Value + "'");
                                }
                                else
                                {
                                    chRow.AddToValuesDictionaty(valueCol.Key, valueCol.Value);
                                }
                            }
                            ChangedRowCollection.Add(chRow);
                        }

                    }
                    break;
                case ScriptGenerateOper.Delete:

                    rolesList = SrolesTable.CheckRoles(valueBaseColumn, isSroles);
                    if (rolesList == null)
                    {
                        return null;
                    }
                    if (rolesList.Count > 0)
                    {
                        foreach (var role in rolesList)
                        {
                            chRow = new ChangedRow(tableName, idColValue.FirstOrDefault().Key, idColValue.FirstOrDefault().Value, valueBaseColumn);
                            chRow.State = DataRowState.Deleted;
                            foreach (KeyValuePair<string, string> whereCol in whereColumns)
                            {
                                if (whereCol.Key == "nzp_role")
                                {
                                    chRow.AddToWhereDictionaty(whereCol.Key, role.Key.ToString());
                                }
                                else
                                {
                                    chRow.AddToWhereDictionaty(whereCol.Key, whereCol.Value);
                                }
                            }
                            ChangedRowCollection.Add(chRow);
                        }
                    }
                    break;
                case ScriptGenerateOper.Update:
                    rolesList = SrolesTable.CheckRoles(valueBaseColumn, isSroles);
                    if (rolesList == null)
                    {
                        return null;
                    }
                    if (rolesList.Count > 0)
                    {
                        UpdateRowWindow urw = new UpdateRowWindow(valColumns, tableName, idColValue);
                        if (DialogResult.OK == urw.ShowDialog())
                        {
                            foreach (var role in rolesList)
                            {
                                chRow = new ChangedRow(tableName, idColValue.FirstOrDefault().Key, idColValue.FirstOrDefault().Value, valueBaseColumn);
                                chRow.State = urw.UpdateChangedRow.State;
                                foreach (
                                    KeyValuePair<string, string> kvp in urw.UpdateChangedRow.ColValuesDictionary)
                                {
                                    chRow.AddToValuesDictionaty(kvp.Key, kvp.Value);
                                }

                                foreach (KeyValuePair<string, string> whereCol in whereColumns)
                                {
                                    if (whereCol.Key == "nzp_role")
                                    {
                                        chRow.AddToWhereDictionaty(whereCol.Key, role.Key.ToString());
                                    }
                                    else
                                    {
                                        chRow.AddToWhereDictionaty(whereCol.Key, whereCol.Value);
                                    }
                                }
                                ChangedRowCollection.Add(chRow);
                            }
                        }
                        urw.Close();
                    }
                    break;
            }
            onRiseEvent();
            return chRow;
        }

        public static List<WatchGeneratedRows> GetGeneratedRows()
        {
            string[] tables =
            {
                Tables.pages,Tables.s_actions, Tables.actions_show, Tables.actions_lnk, Tables.img_lnk, Tables.page_links,Tables.s_roles,Tables.roleskey,
                 Tables.role_actions, Tables.role_pages, Tables.report };

            List<WatchGeneratedRows> wgr = new List<WatchGeneratedRows>();
            for (int j = 0; j < tables.Length; j++)
            {
                int j1 = j;
                var insertRows = ChangedRowCollection.Select(row => row).Where(row =>row.TableName==tables[j1] && row.State == DataRowState.Added);
                IEnumerable<ChangedRow> changedRows = insertRows as ChangedRow[] ?? insertRows.ToArray();
                if (changedRows.Count() != 0)
                {
                    // AddToLinesList(beforeInsert());
                    foreach (var insertRow in changedRows)
                    {
                       // wgr = new List<WatchGeneratedRows>();
                        int i = 1;
                        string cols = "";
                        string values = "";
                        foreach (var changedRow in insertRow.ColValuesDictionary)
                        {
                            cols += ((i == 1) ? " (" : "") + changedRow.Key +
                                    (i == insertRow.ColValuesDictionary.Count ? ")" : ", ");
                            i++;
                        }
                        i = 1;
                        foreach (var changedRow in insertRow.ColValuesDictionary)
                        {
                            values += ((i == 1) ? " (" : "") + changedRow.Value +
                                      (i == insertRow.ColValuesDictionary.Count ? ");" : ", ");
                            i++;
                        }
                        string s = "INSERT INTO " + tables[j] + cols + " VALUES " + values;
                        // Вставка комментария
                        string comment = getCommentToWatchList(tables[j], insertRow);
                        wgr.Add(new WatchGeneratedRows(tables[j], s, insertRow.IdNum, comment));
                    }
                }


                var updateRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == tables[j1] && row.State == DataRowState.Modified);
                var rows = updateRows as ChangedRow[] ?? updateRows.ToArray();
                if (rows.Count() != 0)
                {


                    foreach (var updateRow in rows)
                    {
                        if (updateRow.ColValuesDictionary.Count == 0) continue;
                       // wgr = new List<WatchGeneratedRows>();
                        string set = "";
                        string where = "";
                        int i = 1;
                        foreach (var changedRow in updateRow.ColValuesDictionary)
                        {
                            set += changedRow.Key + "=" + changedRow.Value +
                                   (updateRow.ColValuesDictionary.Count == i ? " " : ", ");
                            i++;
                        }

                        i = 1;
                        foreach (var changedRow in updateRow.WhereColValuesDictionary)
                        {
                            if (changedRow.Value == "null")
                            {
                                where += changedRow.Key + " is " + changedRow.Value +
                                         (updateRow.WhereColValuesDictionary.Count == i ? ";" : " and ");
                                i++;
                                continue;
                            }
                            where += changedRow.Key + "=" + changedRow.Value +
                                     (updateRow.WhereColValuesDictionary.Count == i ? ";" : " and ");
                            i++;
                        }

                        string s = "UPDATE " + tables[j] + " Set " + set + "WHERE " + where;
                        string comment = getCommentToWatchList(tables[j1], updateRow);
                        wgr.Add(new WatchGeneratedRows(tables[j], s, updateRow.IdNum, comment));
                    }
                }

                var deleteRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == tables[j1] && row.State == DataRowState.Deleted);
                // var enumerable = deleteRows as ChangedRow[] ?? deleteRows.ToArray();
                var enumerable = deleteRows as ChangedRow[] ?? deleteRows.ToArray();
                if (enumerable.Count() != 0)
                {
                    //  AddToLinesList(beforeDelete());
                    foreach (var deleteRow in enumerable)
                    {
                      //  wgr = new List<WatchGeneratedRows>();
                        string where = "";
                        int i = 1;
                        foreach (var changedRow in deleteRow.WhereColValuesDictionary)
                        {
                            if (changedRow.Value == "null")
                            {
                                where += changedRow.Key + " is " + changedRow.Value +
                                         (deleteRow.WhereColValuesDictionary.Count == i ? ";" : " and ");
                                i++;
                                continue;
                            }

                            where += changedRow.Key + "=" + changedRow.Value +
                                     (deleteRow.WhereColValuesDictionary.Count == i ? ";" : " and ");
                            i++;
                        }
                        string s = "DELETE FROM " + tables[j] + " WHERE " + where;
                        string comment = getCommentToWatchList(tables[j], deleteRow);
                        wgr.Add(new WatchGeneratedRows(tables[j], s, deleteRow.IdNum, comment));
                    }
                }
            }
            return wgr;
        }
        private static string getCommentToWatchList(string tableName, ChangedRow chr)
        {
            if (tableName == Tables.role_actions || tableName == Tables.role_pages || tableName== Tables.s_roles)
            {
                if (chr.ColValuesDictionary != null && chr.ColValuesDictionary.Count != 0 & chr.ColValuesDictionary.ContainsKey("nzp_role"))
                {
                    if (chr.ColValuesDictionary["nzp_role"] != chr.ValueBaseColumn.ToString())
                    {
                        return "Роль " + chr.ValueBaseColumn + " вливается в роль " +
                               chr.ColValuesDictionary["nzp_role"];
                    }
                }
                else if (chr.WhereColValuesDictionary != null && chr.WhereColValuesDictionary.Count != 0 & chr.WhereColValuesDictionary.ContainsKey("nzp_role"))
                {
                    if (chr.WhereColValuesDictionary["nzp_role"] != chr.ValueBaseColumn.ToString())
                    {
                        return "Роль " + chr.ValueBaseColumn + " вливается в роль " +
                               chr.WhereColValuesDictionary["nzp_role"];
                    }
                }
                    
            }
            return "";
        }
        public static NpgsqlConnection Connect = ConnectionToPostgreSqlDb.GetConnection();
        public static string FileName { get; set; }
        protected NpgsqlDataAdapter Adapter;
        protected DataTable DataTable { get; set; }
        public abstract void Request();
        public abstract void GenerateScr();
    }
}
