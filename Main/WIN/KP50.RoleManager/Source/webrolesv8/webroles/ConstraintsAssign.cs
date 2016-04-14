using System.Data;
namespace webroles
{
    class TablesConstraints
    {
        public static void Assign( DataSet dataSet)
        {
            // Внешние ключи таблицы pages
            ForeignKeyConstraint constraintFk = new ForeignKeyConstraint("'pages_created_by__users_id'", dataSet.Tables["users"].Columns["id"], dataSet.Tables["pages"].Columns["created_by"]);
            constraintFk.DeleteRule = Rule.None;
            dataSet.Tables["pages"].Constraints.Add(constraintFk);
            constraintFk = new ForeignKeyConstraint("'pages_changed_by__users_id'", dataSet.Tables["users"].Columns["id"], dataSet.Tables["pages"].Columns["changed_by"]);
            constraintFk.DeleteRule = Rule.None;
            dataSet.Tables["pages"].Constraints.Add(constraintFk);
           // Внешние ключи таблицы s_actions
            constraintFk = new ForeignKeyConstraint("'s_actions_changed_by__users_id'", dataSet.Tables["users"].Columns["id"], dataSet.Tables["s_actions"].Columns["changed_by"]);
            constraintFk.DeleteRule = Rule.None;
            dataSet.Tables["s_actions"].Constraints.Add(constraintFk);
           // constraintFk = new ForeignKeyConstraint("'s_actions_created_by__users_id'", dataSet.Tables["users"].Columns["id"], dataSet.Tables["s_actions"].Columns["created_by"]);
           // constraintFk.DeleteRule = Rule.None;
            //dataSet.Tables["s_actions"].Constraints.Add(constraintFk);
           
        }

        public static void Clear( DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                if (table.Constraints == null) continue;
                if (table.Constraints.Count == 0) continue;
                table.Constraints.Clear();
            }
        }
    }
}
