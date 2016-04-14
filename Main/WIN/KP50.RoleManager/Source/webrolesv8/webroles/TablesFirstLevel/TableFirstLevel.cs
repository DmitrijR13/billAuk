using System.Data;
using System.Windows.Forms;
using Npgsql;
using webroles.TransferData;

namespace webroles.TablesFirstLevel
{ 
   public abstract class TableFirstLevel
   {
      //  public List<TableSecondLevel> Childs;
       /// <summary>
       /// Текст для узла TreeView
       /// </summary>
        public abstract string NodeTreeViewText { get;  }
       /// <summary>
       /// Название таблицы
       /// </summary>
        public abstract string TableName { get; }
       /// <summary>
       /// Текст команды SELECT
       /// </summary>
        public abstract string SelectCommand { get; }
       /// <summary>
       /// Колонки для DataGridView
       /// </summary>
        public abstract DataGridViewColumn[] GetTableColumns { get; set; }
       /// <summary>
       /// Создает колонки соответствующей таблицы
       /// </summary>
        public abstract void CreateColumns();
       /// <summary>
       /// Сохраняет изменения
       /// </summary>
       /// <param name="connect"></param>
       /// <param name="adapter"></param>
       /// <param name="dataSet"></param>
        public abstract bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt);
       /// <summary>
       /// Добавляет значения по умолчанию, после добавления новой строки
       /// </summary>
       /// <param name="dgv">dataGridView</param>
       /// <param name="index">индекс новой строки</param>
        public abstract void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index);
       /// <summary>
       /// Добавляет значения после изменения ячейки
       /// </summary>
        /// <param name="dgv">dataGridView</param>
       /// <param name="row">индекс строки в которой находится измененая ячейка</param>
       /// <param name="column">индекс колонки, в которой находится измененная ячейка</param>
        public abstract void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column);

       public virtual DataTable GetDataSource(NpgsqlConnection connect, NpgsqlDataAdapter adapter)
       {
           DataTable dt =new DataTable();
           adapter.SelectCommand.CommandText = SelectCommand;
           TransferDataDb.Fill(adapter, dt);
           return dt;
       }
   }
}

