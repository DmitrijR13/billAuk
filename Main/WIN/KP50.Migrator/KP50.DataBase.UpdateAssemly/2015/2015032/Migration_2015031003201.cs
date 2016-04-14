using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015032
{
    [Migration(2015031003201, MigrateDataBase.Web)]
    public class Migration_2015031003201_Web : Migration
    {
        public override void Apply()
        {
            var Queue = new SchemaQualifiedObjectName() { Name = "queue" };
            var QueueRatio = new SchemaQualifiedObjectName() { Name = "queueratio" };
            if(!Database.TableExists(Queue))
            {
                Database.AddTable(Queue,
                    new Column("taskid", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("tasktype", DbType.Int32, ColumnProperty.NotNull),
                    new Column("taskpriority", DbType.Int32, ColumnProperty.NotNull, 0),
                    new Column("taskstate", DbType.Int32, ColumnProperty.NotNull, 0),
                    new Column("taskcalculationdate", DbType.DateTime2, ColumnProperty.NotNull),
                    new Column("taskrunafter", DbType.DateTime2),
                    new Column("taskqueued", DbType.DateTime2),
                    new Column("taskperform", DbType.DateTime2),
                    new Column("taskcompleated", DbType.DateTime2),
                    new Column("taskprogress", DbType.Decimal.WithSize(6, 4), ColumnProperty.None, 0),
                    new Column("taskparameter", DbType.String.WithSize(5000)),
                    new Column("queuepublisher", DbType.String.WithSize(100)),
                    new Column("queuepublisheraddress", DbType.String.WithSize(15)),
                    new Column("queueprocessor", DbType.String.WithSize(100)));

                Database.AddPrimaryKey("TaskIdPKey", Queue, "taskid");
                Database.AddIndex("idx_queue", false, Queue, "taskid", "taskpriority", "taskstate", "taskprogress");
            }

            if (!Database.TableExists(QueueRatio))
            {
                Database.AddTable(QueueRatio,
                    new Column("queueratioid", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("calcfontasktypeid", DbType.Int32, ColumnProperty.NotNull),
                    new Column("qtasktypeid", DbType.Int32, ColumnProperty.NotNull),
                    new Column("queueratioisactual", DbType.Boolean, ColumnProperty.NotNull, false));

                Database.AddIndex("idx_queueratio", true, QueueRatio, "calcfontasktypeid", "qtasktypeid");
            }
        }

        public override void Revert()
        {
            var Queue = new SchemaQualifiedObjectName() { Name = "queue" };
            var QueueRatio = new SchemaQualifiedObjectName() { Name = "queueratio" };

            if (Database.TableExists(Queue)) Database.RemoveTable(Queue);
            if (Database.TableExists(QueueRatio)) Database.RemoveTable(QueueRatio);
        }
    }
}
