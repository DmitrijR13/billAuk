using System;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.DataBase
{
    public partial class DbParameters : DataBaseHead
    {
        //--------------------------------------------------------------------------
        // Конвертер данных для Prm
        //--------------------------------------------------------------------------
        public static Prm ToPrmValue(DataRow dr)
        {
            Prm obj = new Prm();

            // num
            if (dr.Table.Columns.Contains("num"))
                obj.num = DataConvert.FieldValue<Int32>(dr, "num", 0).ToString();

            // prm_X
            obj.nzp_key = DataConvert.FieldValue<Int32>(dr, "nzp_key", 0);
            obj.nzp = DataConvert.FieldValue<Int32>(dr, "nzp", 0);
            obj.nzp_prm = DataConvert.FieldValue<Int32>(dr, "nzp_prm", 0);
            obj.dat_s = DataConvert.FieldValue<DateTime>(dr, "dat_s", DateTime.MinValue).ToString("dd.MM.yyyy");
            obj.dat_po = DataConvert.FieldValue<DateTime>(dr, "dat_po", DateTime.MinValue).ToString("dd.MM.yyyy");
            obj.val_prm = DataConvert.FieldValue<string>(dr, "val_prm", "");
            obj.is_actual = DataConvert.FieldValue<Int32>(dr, "is_actual", 0);
            obj.dat_del = DataConvert.FieldValue<DateTime>(dr, "dat_del", DateTime.MinValue).ToString("dd.MM.yyyy");
            obj.user_del = DataConvert.FieldValue<Int32>(dr, "user_del", 0);
            obj.dat_when = DataConvert.FieldValue<DateTime>(dr, "dat_when", DateTime.MinValue).ToString("dd.MM.yyyy");
            obj.nzp_user = DataConvert.FieldValue<Int32>(dr, "nzp_user", 0);
            if (dr.Table.Columns.Contains("user_name"))
                obj.user_name = DataConvert.FieldValue<string>(dr, "user_name", "");
            if (dr.Table.Columns.Contains("block"))
                obj.block = DataConvert.FieldValue<string>(dr, "block", "");

            // prm_name
            if (dr.Table.Columns.Contains("name_prm"))
                obj.name_prm = DataConvert.FieldValue<string>(dr, "name_prm", "");
            if (dr.Table.Columns.Contains("type_prm"))
                obj.type_prm = DataConvert.FieldValue<string>(dr, "type_prm", "");
            if (dr.Table.Columns.Contains("nzp_res"))
                obj.nzp_res = DataConvert.FieldValue<Int32>(dr, "nzp_res", 0);
            if (dr.Table.Columns.Contains("prm_num"))
                obj.prm_num = DataConvert.FieldValue<Int32>(dr, "prm_num", 0);

            // ls
            if (dr.Table.Columns.Contains("num_ls"))
                obj.num_ls = DataConvert.FieldValue<Int32>(dr, "num_ls", 0);
            if (dr.Table.Columns.Contains("nzp_kvar"))
                obj.nzp_kvar = DataConvert.FieldValue<Int32>(dr, "nzp_kvar", 0);
            if (dr.Table.Columns.Contains("adr"))
                obj.adr = DataConvert.FieldValue<string>(dr, "adr", "");

            return obj;
        }
    }
}