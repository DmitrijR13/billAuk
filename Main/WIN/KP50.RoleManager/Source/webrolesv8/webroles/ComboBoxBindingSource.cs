using System;
using System.Windows.Forms;
using Npgsql;
using System.Data;
using NpgsqlTypes;

namespace webroles
{
    public  class ComboBoxBindingSource:IObserver
    {
        private readonly DataGridViewColumn dgvColumn;
        private readonly bool isNezadanoNeeded;
        private readonly string commandString;
        private readonly string additionalConditionString;
        private readonly bool additionalCondition ;
        private readonly bool isZeroNeeded;

        public ComboBoxBindingSource(DataGridViewColumn dgvColumn, bool isNezadanoNeeded, string additionalConditionString = "", bool additionalCondition = false, bool isZeroNeeded = false) 
        {
            this.dgvColumn = dgvColumn;
            this.isNezadanoNeeded = isNezadanoNeeded;
            this.additionalConditionString = additionalConditionString;
            this.additionalCondition = additionalCondition;
            this.isZeroNeeded = isZeroNeeded;
        }
        public ComboBoxBindingSource(String commandString, DataGridViewColumn dgvColumn, bool isNezadanoNeeded)
        {
            this.dgvColumn = dgvColumn;
            this.isNezadanoNeeded = isNezadanoNeeded;
            this.commandString = commandString;
            
        }

        
        /// <summary>
        /// Возвращает объект BindingSource, у которого свойство DataSource ссылается на таблицу, столбцы которой буду присвоены свойствам DataSource, DisplayMember, DisplayValue колонки "page_type" типа ComboBoxColumn в таблице pages dataGridView.
        /// </summary>
        /// <returns></returns>
        /// 
        
    public void update(DataTable dataTable)
     {
         if (isNezadanoNeeded)
         {
             var dt = dataTable.Copy();
             if (isZeroNeeded)
             {
                 DataRow rownz = dt.NewRow();
                 rownz["id"] = 0;
                 rownz["text"] = " ";
                 dt.Rows.Add(rownz);
             }
             DataRow row = dt.NewRow();
             row["text"] = "незадано";
             dt.Rows.Add(row);
             ((DataGridViewComboBoxColumn)dgvColumn).DataSource = dt.Copy();
            // ((DataGridViewComboBoxColumn)dgvColumn).
         }
         else
             ((DataGridViewComboBoxColumn)dgvColumn).DataSource = dataTable.Copy(); ;
     }

    public void update(bool isZeroNeeded=false)
    {
        NpgsqlConnection connect = ConnectionToPostgreSqlDb.GetConnection();
        DataTable dataTable = new DataTable();
        dataTable = new DataTable();
        dataTable.Columns.Add("id");
        dataTable.Columns.Add("text");
        dataTable.Columns["id"].DataType = typeof(int);
        dataTable.Columns["text"].DataType = typeof(string);
        try
        {
            connect.Open();
            NpgsqlCommand selectCommand = new NpgsqlCommand(commandString, connect);
            NpgsqlDataReader reader = selectCommand.ExecuteReader();
            while (reader.Read())
            {
                DataRow row = dataTable.NewRow();
                row["id"] = (int)reader["id"];
                row["text"] = reader["text"].ToString();
                dataTable.Rows.Add(row);
            }
            // добавление строки "незадано"
            if (isNezadanoNeeded)
            {
                
                if (isZeroNeeded)
                {
                    DataRow rownz = dataTable.NewRow();
                    rownz["id"] = 0;
                    rownz["text"] = " ";
                    dataTable.Rows.Add(rownz);
                }
                DataRow row = dataTable.NewRow();
                row = dataTable.NewRow();
                row["text"] = "незадано";
                dataTable.Rows.Add(row);

            }
            reader.Close();
        }

        catch (NpgsqlException exc)
        {
            MessageBox.Show(exc.Message);
        }
        finally
        {
            connect.Close();
            ((DataGridViewComboBoxColumn)dgvColumn).DataSource = dataTable;
        }
    }

    public bool AdditionalContition
    {
        get { return additionalCondition; }
    }

    public string AdditionalContotionString
    {
        get { return additionalConditionString; }
    }

    public static DataTable Update(string commandString)
    {
        NpgsqlConnection connect = ConnectionToPostgreSqlDb.GetConnection();
        DataTable dataTable = new DataTable();
        IDataReader reader=null;
        dataTable = new DataTable();
        dataTable.Columns.Add("id");
        dataTable.Columns.Add("text");
        dataTable.Columns["id"].DataType = typeof(int);
        dataTable.Columns["text"].DataType = typeof(string);
        try
        {
            connect.Open();
            NpgsqlCommand selectCommand = new NpgsqlCommand(commandString, connect);
            reader = selectCommand.ExecuteReader();
            while (reader.Read())
            {
                DataRow row = dataTable.NewRow();
                row["id"] = (int)reader["id"];
                row["text"] = reader["text"].ToString();
                dataTable.Rows.Add(row);
            }
            // добавление строки "незадано"
         
            
        }

        catch (NpgsqlException exc)
        {
            MessageBox.Show(exc.Message);
        }
        finally
        {
            if (reader != null) reader.Close();
            connect.Close();
        }
        return dataTable;
    }

    public static  DataTable GetEmptyTable ()
    {
        DataTable dataTable = new DataTable();
        dataTable = new DataTable();
        dataTable.Columns.Add("id");
        dataTable.Columns.Add("text");
        dataTable.Columns["id"].DataType = typeof(int);
        dataTable.Columns["text"].DataType = typeof(string);
        DataRow rownz = dataTable.NewRow();
        rownz["text"] = "незадано";
        dataTable.Rows.Add(rownz);
        return dataTable;
    }
    }

  }

   

        


