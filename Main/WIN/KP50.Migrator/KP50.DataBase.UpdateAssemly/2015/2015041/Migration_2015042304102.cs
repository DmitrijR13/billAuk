using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System;

namespace KP50.DataBase.UpdateAssembly._2015._2015041
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015042304102, MigrateDataBase.CentralBank)]
    public class Migration_2015042304102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Debt);

            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };

            if (!Database.TableExists(kredit))
            {
                Database.AddTable(kredit,
                    new Column("nzp_kredit", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("old_nzp_kredit", DbType.Int32),
                    new Column("pref", DbType.String.WithSize(100)),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dat_month", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_s", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_po", DbType.Date, ColumnProperty.NotNull),
                    new Column("valid", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dog_num", DbType.String.WithSize(20)),
                    new Column("dog_dat", DbType.Date),
                    new Column("sum_dolg", DbType.Decimal.WithSize(14,2), ColumnProperty.Null, 0),
                    new Column("perc", DbType.Decimal.WithSize(14, 2), ColumnProperty.Null, 0),
                    new Column("sum_real_p", DbType.Decimal.WithSize(14, 2), ColumnProperty.Null, 0)
                );
            }

            SchemaQualifiedObjectName kredit_pay = new SchemaQualifiedObjectName() { Name = "kredit_pay", Schema = CurrentSchema };
            if (!Database.TableExists(kredit_pay))
            {
                Database.AddTable(kredit_pay,
                    new Column("nzp_kredx", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_kredit", DbType.Int32, ColumnProperty.NotNull),
                   
                    new Column("old_nzp_kredit", DbType.Int32),
                    new Column("pref", DbType.String.WithSize(100)),
                    
                    new Column("calc_month", DbType.Date, ColumnProperty.NotNull),
                    
                    new Column("is_sum_pere", DbType.Int32, ColumnProperty.NotNull, 0),
                    new Column("sum_indolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_dolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_odna12", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_perc", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_charge", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_outdolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_money", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0)
                );
            }

            // -- уникальные индексы
            if (!Database.IndexExists("ix_kredit_nzp_kredit", kredit)) Database.AddIndex("ix_kredit_nzp_kredit", true, kredit, "nzp_kredit");
            if (!Database.IndexExists("ix_kredit_pay_nzp_kredx", kredit_pay)) Database.AddIndex("ix_kredit_pay_nzp_kredx", true, kredit_pay, "nzp_kredx");

            // -- простые индексы
            // kredit
            if (!Database.IndexExists("ix_kredit_1", kredit)) Database.AddIndex("ix_kredit_1", false, kredit, "nzp_kvar", "dat_month");
            
            // kredit_pay
            if (!Database.IndexExists("ix_kredit_pay_nzp_kredit", kredit_pay)) Database.AddIndex("ix_kredit_pay_nzp_kredit", false, kredit_pay, "nzp_kredit");
            if (!Database.IndexExists("ix_kredit_pay_1", kredit_pay)) Database.AddIndex("ix_kredit_pay_1", false, kredit_pay, "nzp_kvar", "nzp_serv");
            if (!Database.IndexExists("ix_kredit_pay_calc_month", kredit_pay)) Database.AddIndex("ix_kredit_pay_calc_month", false, kredit_pay, "calc_month");
            
            // -- первичные ключи
            if (!Database.ConstraintExists(kredit, "kredit_pkey")) Database.AddPrimaryKey("kredit_pkey", kredit, "nzp_kredit");
            if (!Database.ConstraintExists(kredit_pay, "kredit_pay_pkey")) Database.AddPrimaryKey("kredit_pay_pkey", kredit_pay, "nzp_kredx");

            // слить данные
            string kernel = CentralPrefix + "_kernel" + Database.TableDelimiter;
            string localPref = "";
            
            string sql = "";
            string chargeYY = "";
            string kreditMM = "";
            DateTime calcMonth;

            Database.ExecuteNonQuery("delete from kredit_pay");
            Database.ExecuteNonQuery("delete from kredit");
            
            using (IDataReader reader = Database.ExecuteReader("select trim(bd_kernel) as pref from " + kernel + "s_point where flag > 1 order by 1"))
            {
                while (reader.Read())
                {
                    localPref = (string)reader["pref"];
                    sql = "insert into kredit (old_nzp_kredit, pref, nzp_kvar, nzp_serv, dat_month, dat_s, dat_po, valid, sum_dolg, dog_num, dog_dat, sum_real_p, perc) "  +
                        " select nzp_kredit, " + "'" + localPref + "'" + ", nzp_kvar, nzp_serv, dat_month, dat_s, dat_po, valid, sum_dolg, dog_num, dog_dat, sum_real_p, perc " +
                        " from " + localPref +    "_data" + Database.TableDelimiter + "kredit";
                    Database.ExecuteNonQuery(sql);


                    for (int yy = 13; yy <= 16; yy++)
                    {
                        for (int mm = 1; mm <= 12; mm++)
                        {
                            kreditMM = "kredit_" + mm.ToString("00");
                            chargeYY = localPref + "_charge_" + yy.ToString("00");
                            calcMonth = new DateTime(yy, mm, 1);

                            Console.WriteLine("Таблица " + chargeYY + Database.TableDelimiter + kreditMM);

                            SchemaQualifiedObjectName kreditXX = new SchemaQualifiedObjectName() { Name = kreditMM, Schema = chargeYY };

                            if (Database.TableExists(kreditXX))
                            {
                                if (Database.ColumnExists(kreditXX, "sum_money"))
                                {
                                    sql = "insert into kredit_pay (nzp_kvar, nzp_serv, old_nzp_kredit, pref, calc_month, " +
                                        " sum_indolg, sum_odna12, sum_perc, sum_charge, sum_outdolg, sum_money) " +
                                        "select a.nzp_kvar, a.nzp_serv, a.nzp_kredit," + "'" + localPref + "'" + "," + "'" + calcMonth.ToShortDateString() + "'" + "," +
                                        " a.sum_indolg, a.sum_odna12, a.sum_perc, a.sum_charge, a.sum_outdolg, coalesce(a.sum_money, 0) " +
                                        " from " + chargeYY + Database.TableDelimiter + kreditMM + " a";
                                    Database.ExecuteNonQuery(sql);
                                }
                                else
                                {
                                    sql = "insert into kredit_pay (nzp_kvar, nzp_serv, old_nzp_kredit, pref, calc_month, " +
                                        " sum_indolg, sum_odna12, sum_perc, sum_charge, sum_outdolg, sum_money) " +
                                        "select a.nzp_kvar, a.nzp_serv, a.nzp_kredit," + "'" + localPref + "'" + "," + "'" + calcMonth.ToShortDateString() + "'" + "," +
                                        " a.sum_indolg, a.sum_odna12, a.sum_perc, a.sum_charge, a.sum_outdolg, 0 " +
                                        " from " + chargeYY + Database.TableDelimiter + kreditMM + " a";
                                    Database.ExecuteNonQuery(sql);
                                }
                            }
                        }
                    }
                }
            }

            Database.ExecuteNonQuery("update kredit_pay a set nzp_kredit = (select nzp_kredit from kredit b where a.old_nzp_kredit = b.old_nzp_kredit and a.pref = b.pref)");
            Database.ExecuteNonQuery("delete from kredit_pay where nzp_kredit is null");

            using (IDataReader reader = Database.ExecuteReader("select trim(bd_kernel) as pref from " + kernel + "s_point order by 1"))
            {
                while (reader.Read())
                {
                    localPref = (string)reader["pref"];
                    InsertServices(localPref);
                }
            }

            SetSchema(Bank.Kernel);
            if (!ValueExists("s_listfactura", "kind", 121))
            {
                Database.ExecuteNonQuery(@"insert into s_listfactura (name_rus, file_name, kind, townfilter, default_) values ('Счет с рассрочкой по пост. 354', '~/App_Data/std354.frx', 121, 63, 0);");           
            }

        }

        public override void Revert()
        {
            
        }

        private void InsertServices(string pref)
        { 
            string now = "now()";
            if (Database.ProviderName == "Informix") now = "current";

            Database.SetSchema(pref + "_kernel");

            // ... prm_name
            if (!ValueExists("prm_name", "nzp_prm", 1116))
            {
                Database.ExecuteNonQuery(@"insert into prm_name (nzp_prm,name_prm,type_prm,prm_num,low_,high_,digits_)
 values (1116,'Рассрочка(процент) по холодной воде   ','float',1,0,100,4);");    
            }

            if (!ValueExists("prm_name", "nzp_prm", 1118))
            {
                Database.ExecuteNonQuery(@"insert into prm_name (nzp_prm,name_prm,type_prm,prm_num,low_,high_,digits_)
 values (1118,'Рассрочка(процент) по канализации     ','float',1,0,100,4);");
            }

            if (!ValueExists("prm_name", "nzp_prm", 1119))
            {
                Database.ExecuteNonQuery(@"insert into prm_name (nzp_prm,name_prm,type_prm,prm_num,low_,high_,digits_)
 values (1119,'Рассрочка(процент) по отоплению       ','float',1,0,100,4);");
            }

            if (!ValueExists("prm_name", "nzp_prm", 1120))
            {
                Database.ExecuteNonQuery(@"insert into prm_name (nzp_prm,name_prm,type_prm,prm_num,low_,high_,digits_)
 values (1120,'Рассрочка(процент) по электроснабжению','float',1,0,100,4);");
            }

            if (!ValueExists("prm_name", "nzp_prm", 1121))
            {
                Database.ExecuteNonQuery(@"insert into prm_name (nzp_prm,name_prm,type_prm,prm_num,low_,high_,digits_)
 values (1121,'Рассрочка(процент) по газу            ','float',1,0,100,4);");
            }

            if (!ValueExists("prm_name", "nzp_prm", 1122))
            {
                Database.ExecuteNonQuery(@"insert into prm_name (nzp_prm,name_prm,type_prm,prm_num,low_,high_,digits_)
 values (1122,'Рассрочка(процент)                    ','float',5,0,100,4);");
            }

            // ... formuls
            if (!ValueExists("formuls", "nzp_frm", 1999))
            {
                Database.ExecuteNonQuery(@"insert into formuls (nzp_frm,name_frm,nzp_measure,is_device) values (1999,'Рассрочка платежа по Пост.№354',6,0);");
            }

            // ... formuls_opis
            if (!ValueExists("formuls_opis", "nzp_frm", 1999))
            {
                Database.ExecuteNonQuery(@"insert into formuls_opis (nzp_frm, nzp_frm_kod, nzp_frm_typ, nzp_frm_typrs) values (1999, 1999, 1999, 4);");
            }

            // ... services
            if (!ValueExists("services", "nzp_serv", 306))
            {
                Database.ExecuteNonQuery(@"insert into services (nzp_serv,service,service_small,service_name,ed_izmer,type_lgot,nzp_frm,ordering)
 values (306,'% за рассрочку по холодной воде   ','% за рассрочку ХВС  ','Проценты за рассрочку по холодной воде   ','с кв.в мес.',1,0,309);");
            }

            if (!ValueExists("services", "nzp_serv", 307))
            {
                Database.ExecuteNonQuery(@"insert into services (nzp_serv,service,service_small,service_name,ed_izmer,type_lgot,nzp_frm,ordering)
 values (307,'% за рассрочку по горячей воде    ','% за рассрочку ГВС  ','Проценты за рассрочку по горячей воде    ','с кв.в мес.',1,0,310);");
            }

            if (!ValueExists("services", "nzp_serv", 308))
            {
                Database.ExecuteNonQuery(@"insert into services (nzp_serv,service,service_small,service_name,ed_izmer,type_lgot,nzp_frm,ordering)
 values (308,'% за рассрочку по канализации     ','% за рассрочку Кан  ','Проценты за рассрочку по канализации     ','с кв.в мес.',1,0,311);");
            }

            if (!ValueExists("services", "nzp_serv", 309))
            {
                Database.ExecuteNonQuery(@"insert into services (nzp_serv,service,service_small,service_name,ed_izmer,type_lgot,nzp_frm,ordering)
 values (309,'% за рассрочку по отоплению       ','% за рассрочку Отопл','Проценты за рассрочку по отоплению       ','с кв.в мес.',1,0,312);");
            }

            if (!ValueExists("services", "nzp_serv", 310))
            {
                Database.ExecuteNonQuery(@"insert into services (nzp_serv,service,service_small,service_name,ed_izmer,type_lgot,nzp_frm,ordering)
 values (310,'% за рассрочку по электроснабжению','% за рассрочку ЭлЭн ','Проценты за рассрочку по электроснабжению','с кв.в мес.',1,0,313);");
            }

            if (!ValueExists("services", "nzp_serv", 311))
            {
                Database.ExecuteNonQuery(@"insert into services (nzp_serv,service,service_small,service_name,ed_izmer,type_lgot,nzp_frm,ordering)
 values (311,'% за рассрочку по газу            ','% за рассрочку Газ  ','Проценты за рассрочку по газу            ','с кв.в мес.',1,0,314);");
            }

            Database.ExecuteNonQuery(@"delete from prm_frm where nzp_frm=1999;
insert into prm_frm (nzp_frm, frm_calc, is_prm, operation, nzp_prm, frm_p1, frm_p2, frm_p3, result)
 select 1999, 0, 1, ' -FLD', nzp_prm, ' ', ' ', ' ', ' ' 
 from prm_name where nzp_prm in (1116,1117,1118,1119,1120,1121,1122);");
            
//            Database.ExecuteNonQuery(@"delete from l_foss where nzp_serv in (306,307,308,309,310,311);
//insert into l_foss (nzp_serv,nzp_supp,nzp_frm, dat_s, dat_po)
// select nzp_serv, 1999, 1999, '01.09.2012', '01.01.3000'
// from services where nzp_serv in (306,307,308,309,310,311);");

            Database.ExecuteNonQuery(@"delete from prm_tarifs where nzp_serv in (306,307,308,309,310,311);
insert into prm_tarifs (nzp_serv,nzp_frm,nzp_prm,is_edit,nzp_user,dat_when)
 select nzp_serv,1999, 1122,1,1," + now + @"
 from services where nzp_serv in (306,307,308,309,310,311);");

            //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            string sql = @"delete from prm_12 where nzp in (306,307,308,309,310,311);
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (306,123,'01.01.2003','01.01.3000','1',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (307,123,'01.01.2003','01.01.3000','1',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (308,123,'01.01.2003','01.01.3000','1',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (309,123,'01.01.2003','01.01.3000','1',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (310,123,'01.01.2003','01.01.3000','1',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (311,123,'01.01.2003','01.01.3000','1',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (306,127,'01.01.2003','01.01.3000','2',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (307,127,'01.01.2003','01.01.3000','2',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (308,127,'01.01.2003','01.01.3000','2',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (309,127,'01.01.2003','01.01.3000','2',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (310,127,'01.01.2003','01.01.3000','2',1,1,{now});
insert into prm_12 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)
 values (311,127,'01.01.2003','01.01.3000','2',1,1,{now});";
            
            Database.SetSchema(pref + "_data");
            Database.ExecuteNonQuery(sql.Replace("{now}", now));
        }

        private bool ValueExists(string tableName, string keyField, int keyValue)
        {
            return Convert.ToInt32(Database.ExecuteScalar("select count(*) from " + tableName + " where " + keyField + " = " + keyValue)) > 0;
        }
    }
}
