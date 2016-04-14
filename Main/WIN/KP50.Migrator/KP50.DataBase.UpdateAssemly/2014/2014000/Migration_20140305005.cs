using KP50.DataBase.Migrator.Framework;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305005, MigrateDataBase.CentralBank)]
    public class Migration_20140305005_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            
            #warning Индусский код detected.
            SchemaQualifiedObjectName s_remark = new SchemaQualifiedObjectName() { Name = "s_remark", Schema = CurrentSchema };
            if (!Database.ColumnExists(s_remark, "remark")) Database.AddColumn(s_remark, new Column("remark", DbType.String.WithSize(2000)));

            SchemaQualifiedObjectName sys_dictionary_values = new SchemaQualifiedObjectName() { Name = "sys_dictionary_values", Schema = CurrentSchema };
            Database.Delete(sys_dictionary_values, "nzp_dict = 7427");
            Database.Insert(sys_dictionary_values, new string[] { "nzp_dict", "name", "nzp_dict_parent", "nzp_tdict", "code", "note" }, new string[] { "7427", "Пересчёт сальдо перечисления", null, "101", null, null });

            SchemaQualifiedObjectName sys_event_detail = new SchemaQualifiedObjectName() { Name = "sys_event_detail", Schema = CurrentSchema };
            if (!Database.TableExists(sys_event_detail))
            {
                Database.AddTable(sys_event_detail,
                    new Column("nzp_ev_det", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_event", DbType.Int32),
                    new Column("table_", DbType.String.WithSize(100)),
                    new Column("nzp", DbType.Int32));
                Database.AddIndex("ixsys_even_d_1", false, sys_event_detail, "nzp_event");
            }

            SchemaQualifiedObjectName kvar_pkodes = new SchemaQualifiedObjectName() { Name = "kvar_pkodes", Schema = CurrentSchema };
            if (!Database.TableExists(kvar_pkodes))
            {
                Database.AddTable(kvar_pkodes,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("area_code", DbType.Int32),
                    new Column("pkod10", DbType.Int32),
                    new Column("pkod", DbType.Decimal.WithSize(13), ColumnProperty.NotNull),
                    new Column("dat_s", DbType.Date),
                    new Column("dat_po", DbType.Date),
                    new Column("is_actual", DbType.Int32),
                    new Column("changed_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_on", DbType.DateTime));

                Database.AddIndex("ix_kvar_pkodes_1", true, kvar_pkodes, "id");
                Database.AddIndex("ix_kvar_pkodes_2", true, kvar_pkodes, "pkod");
                Database.AddIndex("ix_kvar_pkodes_3", false, kvar_pkodes, "nzp_kvar");
                Database.AddIndex("ix_kvar_pkodes_4", false, kvar_pkodes, "area_code");
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_remark = new SchemaQualifiedObjectName() { Name = "s_remark", Schema = CurrentSchema };
            if (Database.ColumnExists(s_remark, "remark")) Database.RemoveColumn(s_remark, "remark");

            SchemaQualifiedObjectName sys_event_detail = new SchemaQualifiedObjectName() { Name = "sys_event_detail", Schema = CurrentSchema };
            if (Database.TableExists(sys_event_detail)) Database.RemoveTable(sys_event_detail);

            SchemaQualifiedObjectName kvar_pkodes = new SchemaQualifiedObjectName() { Name = "kvar_pkodes", Schema = CurrentSchema };
            if (Database.TableExists(kvar_pkodes)) Database.RemoveTable(kvar_pkodes);
        }
    }

    [Migration(20140305005, MigrateDataBase.Fin)]
    public class Migration_20140305005_Fin : Migration
    {
        public override void Apply()
        {
            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName fn_distrib_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_distrib_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                SchemaQualifiedObjectName fn_pa_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_pa_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                string IndexDistribDomTemplate = string.Format("ix_fnd_dom_{0}_", i.ToString("00"));

                if (!Database.TableExists(fn_distrib_dom))
                {
                    Database.AddTable(fn_distrib_dom,
                        new Column("nzp_dis", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                        new Column("nzp_payer", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_area", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_dom", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                        new Column("dat_oper", DbType.Date, ColumnProperty.NotNull),
                        new Column("sum_in", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                        new Column("sum_rasp", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                        new Column("sum_ud", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                        new Column("sum_naud", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                        new Column("sum_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                        new Column("sum_charge", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                        new Column("sum_send", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                        new Column("sum_out", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                        new Column("nzp_bank", DbType.Int32, ColumnProperty.None, -1));

                    Database.AddIndex(string.Format("{0}_1", IndexDistribDomTemplate), true, fn_distrib_dom, "nzp_dis");
                    Database.AddIndex(string.Format("{0}_2", IndexDistribDomTemplate), false, fn_distrib_dom, "nzp_payer");
                    Database.AddIndex(string.Format("{0}_3", IndexDistribDomTemplate), false, fn_distrib_dom, "dat_oper", "nzp_area", "nzp_serv", "nzp_payer");
                    Database.AddIndex(string.Format("{0}_4", IndexDistribDomTemplate), false, fn_distrib_dom, "nzp_area", "nzp_serv");
                    Database.AddIndex(string.Format("{0}_5", IndexDistribDomTemplate), false, fn_distrib_dom, "nzp_serv");
                    Database.AddIndex(string.Format("{0}_6", IndexDistribDomTemplate), false, fn_distrib_dom, "nzp_bank");
                    Database.AddIndex(string.Format("{0}_7", IndexDistribDomTemplate), false, fn_distrib_dom, "nzp_dom");
                    Database.AddIndex(string.Format("{0}_8", IndexDistribDomTemplate), false, fn_distrib_dom, "nzp_area", "nzp_dom", "nzp_serv");
                }

                if (!Database.TableExists(fn_pa_dom))
                {
                    Database.AddTable(fn_pa_dom,
                        new Column("nzp_pk", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                        new Column("nzp_dom", DbType.Int32),
                        new Column("nzp_supp", DbType.Int32),
                        new Column("nzp_serv", DbType.Int32),
                        new Column("nzp_area", DbType.Int32),
                        new Column("nzp_geu", DbType.Int32),
                        new Column("sum_prih", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                        new Column("sum_prih_r", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                        new Column("sum_prih_g", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                        new Column("dat_oper", DbType.Date),
                        new Column("nzp_bl", DbType.Int32),
                        new Column("nzp_supp_w", DbType.Int32, ColumnProperty.None, 0),
                        new Column("nzp_area_w", DbType.Int32, ColumnProperty.None, 0),
                        new Column("nzp_bank", DbType.Int32, ColumnProperty.None, -1));

                    Database.AddIndex(string.Format("ix_fnp_dom_{0}_1", i.ToString("00")), true, fn_pa_dom, "nzp_pk");
                    Database.AddIndex(string.Format("ix_fnp_dom_{0}_6", i.ToString("00")), false, fn_pa_dom, "nzp_dom", "dat_oper", "nzp_area", "nzp_serv", "nzp_supp");
                    Database.AddIndex(string.Format("ix_fpt_dom_{0}_1", i.ToString("00")), false, fn_pa_dom, "nzp_supp", "nzp_serv");
                    Database.AddIndex(string.Format("ix_fpt_dom_{0}_2", i.ToString("00")), false, fn_pa_dom, "nzp_serv");
                    Database.AddIndex(string.Format("ix_fpt_dom_{0}_3", i.ToString("00")), false, fn_pa_dom, "dat_oper", "nzp_area", "nzp_geu", "nzp_supp", "nzp_serv");
                    Database.AddIndex(string.Format("ix_fpt_dom_{0}_4", i.ToString("00")), false, fn_pa_dom, "dat_oper", "nzp_serv");
                }
            }

            SchemaQualifiedObjectName fn_perc_dom = new SchemaQualifiedObjectName() { Name = "fn_perc_dom", Schema = CurrentSchema };
            if (!Database.TableExists(fn_perc_dom))
            {
                Database.AddTable(fn_perc_dom,
                    new Column("nzp_pr", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_area", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32),
                    new Column("sum_prih", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                    new Column("sum_perc", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                    new Column("perc_ud", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                    new Column("dat_oper", DbType.Date),
                    new Column("nzp_bl", DbType.Int32),
                    new Column("nzp_bank", DbType.Int32, ColumnProperty.None, 0));

                Database.AddIndex("ix_fnpr_dom_1", true, fn_perc_dom, "nzp_pr");
                Database.AddIndex("ix_fnpr_dom_2", false, fn_perc_dom, "dat_oper", "nzp_bank");
                Database.AddIndex("ix_fnpr_dom_3", false, fn_perc_dom, "dat_oper", "nzp_supp", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix_fnpr_dom_4", false, fn_perc_dom, "dat_oper", "nzp_payer");
                Database.AddIndex("ix_fnpr_dom_5", false, fn_perc_dom, "nzp_bank");
            }

            SchemaQualifiedObjectName fn_naud_dom = new SchemaQualifiedObjectName() { Name = "fn_naud_dom", Schema = CurrentSchema };
            if (!Database.TableExists(fn_naud_dom))
            {
                Database.AddTable(fn_naud_dom,
                    new Column("nzp_naud", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("dat_oper", DbType.Date),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_payer_2", DbType.Int32),
                    new Column("sum_ud", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_ud_r", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_ud_g", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_naud", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_naud_r", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_naud_g", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("nzp_bl", DbType.Int32),
                    new Column("nzp_area", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32),
                    new Column("nzp_bank", DbType.Int32, ColumnProperty.None, 0));

                Database.AddIndex("ix_naud_dom_0", true, fn_naud_dom, "nzp_naud");
                Database.AddIndex("ix_naudt_dom_1", false, fn_naud_dom, "dat_oper", "nzp_area", "nzp_geu", "nzp_payer", "nzp_serv");
                Database.AddIndex("ix_naudt_dom_2", false, fn_naud_dom, "dat_oper", "nzp_area", "nzp_geu", "nzp_payer_2", "nzp_serv");
                Database.AddIndex("ix_naudt_dom_3", false, fn_naud_dom, "nzp_payer", "nzp_serv");
                Database.AddIndex("ix_naudt_dom_4", false, fn_naud_dom, "nzp_payer_2", "nzp_serv");
            }

            SchemaQualifiedObjectName fn_sended_dom = new SchemaQualifiedObjectName() { Name = "fn_sended_dom", Schema = CurrentSchema };
            if (!Database.TableExists(fn_sended_dom))
            {
                Database.AddTable(fn_sended_dom,
                    new Column("nzp_snd", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("dat_oper", DbType.Date, ColumnProperty.NotNull),
                    new Column("nzp_area", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_dom", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_payer", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_fd", DbType.Int32, ColumnProperty.NotNull),
                    new Column("num_pp", DbType.Int32, ColumnProperty.None, 0),
                    new Column("dat_pp", DbType.Date),
                    new Column("sum_send", DbType.Decimal.WithSize(13,2)),
                    new Column("nzp_send", DbType.Int32),
                    new Column("nzp_user", DbType.Int32),
                    new Column("dat_when", DbType.Date),
                    new Column("nzp_snd_ret", DbType.Int32, ColumnProperty.None, 0),
                    new Column("id_bc_file", DbType.Int32));

                Database.AddIndex("ix_fs_dom_1", true, fn_sended_dom, "nzp_snd");
                Database.AddIndex("ix_fs_dom_2", false, fn_sended_dom, "dat_oper", "nzp_area", "nzp_serv", "nzp_payer");
                Database.AddIndex("ix_fs_dom_3", false, fn_sended_dom, "nzp_area", "nzp_dom","nzp_serv", "nzp_payer");
                Database.AddIndex("ix_fs_dom_4", false, fn_sended_dom, "nzp_serv", "nzp_payer");
                Database.AddIndex("ix_fs_dom_5", false, fn_sended_dom, "nzp_payer", "nzp_fd");
                Database.AddIndex("ix_fs_dom_6", false, fn_sended_dom, "nzp_fd");
                Database.AddIndex("ix_fs_dom_7", false, fn_sended_dom, "num_pp", "dat_pp");
            }

            SchemaQualifiedObjectName fn_reval_dom = new SchemaQualifiedObjectName() { Name = "fn_reval_dom", Schema = CurrentSchema };
            if (!Database.TableExists(fn_reval_dom))
            {
                Database.AddTable(fn_reval_dom,
                    new Column("nzp_reval", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("dat_oper", DbType.Date),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_payer_2", DbType.Int32),
                    new Column("nzp_reval_2", DbType.Int32),
                    new Column("sum_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_reval_r", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_reval_g", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("comment", DbType.StringFixedLength.WithSize(60)),
                    new Column("nzp_bl", DbType.Int32),
                    new Column("nzp_area", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32),
                    new Column("nzp_user", DbType.Int32),
                    new Column("dat_when", DbType.Date),
                    new Column("nzp_bank", DbType.Int32, ColumnProperty.None, 0));

                Database.AddIndex("ix_reval_dom_01", true, fn_reval_dom, "nzp_reval");
                Database.AddIndex("ix_revt_dom_1", false, fn_reval_dom, "dat_oper", "nzp_area", "nzp_geu", "nzp_payer", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix_revt_dom_2", false, fn_reval_dom, "dat_oper", "nzp_area", "nzp_geu", "nzp_payer_2", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix_revt_dom_3", false, fn_reval_dom, "nzp_payer", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix_revt_dom_4", false, fn_reval_dom, "nzp_payer_2", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix110_dom_5", false, fn_reval_dom, "nzp_reval_2");
            }
        }

        public override void Revert()
        {
            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName fn_distrib_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_distrib_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                SchemaQualifiedObjectName fn_pa_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_pa_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                if (Database.TableExists(fn_distrib_dom)) Database.RemoveTable(fn_distrib_dom);
                if (Database.TableExists(fn_pa_dom)) Database.RemoveTable(fn_pa_dom);
            }

            SchemaQualifiedObjectName fn_perc_dom = new SchemaQualifiedObjectName() { Name = "fn_perc_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_naud_dom = new SchemaQualifiedObjectName() { Name = "fn_naud_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_sended_dom = new SchemaQualifiedObjectName() { Name = "fn_sended_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_reval_dom = new SchemaQualifiedObjectName() { Name = "fn_reval_dom", Schema = CurrentSchema };
            if (Database.TableExists(fn_perc_dom)) Database.RemoveTable(fn_perc_dom);
            if (Database.TableExists(fn_naud_dom)) Database.RemoveTable(fn_naud_dom);
            if (Database.TableExists(fn_sended_dom)) Database.RemoveTable(fn_sended_dom);
            if (Database.TableExists(fn_reval_dom)) Database.RemoveTable(fn_reval_dom);
        }
    }
}
