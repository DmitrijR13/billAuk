using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// 
    /// </summary>
    public partial class DbFnReval : DataBaseHead
    {
        //--------------------------------------------------------------------------
        //Конвертер данных для FnReval
        //--------------------------------------------------------------------------
        public static FnReval ToFnRevalValue(DataRow dr)
        {
            FnReval obj = new FnReval();

            obj.nzp_reval = DataConvert.FieldValue<Int32>(dr, "nzp_reval", 0);
            obj.nzp_area = DataConvert.FieldValue<Int32>(dr, "nzp_area", 0);
            obj.nzp_serv = DataConvert.FieldValue<Int32>(dr, "nzp_serv", 0); 
            obj.nzp_payer = DataConvert.FieldValue<Int32>(dr, "nzp_payer", 0);
            obj.area = DataConvert.FieldValue<string>(dr, "area", null); 
            obj.service = DataConvert.FieldValue<string>(dr, "service", null); 
            obj.payer = DataConvert.FieldValue<string>(dr, "payer", null); 
            obj.nzp_reval_2 = DataConvert.FieldValue<Int32>(dr, "nzp_reval_2", 0); 
            obj.nzp_area_2 = DataConvert.FieldValue<Int32>(dr, "nzp_area_2", 0); 
            obj.nzp_serv_2 = DataConvert.FieldValue<Int32>(dr, "nzp_serv_2", 0); 
            obj.nzp_payer_2 = DataConvert.FieldValue<Int32>(dr, "nzp_payer_2", 0); 
            obj.area_2 = DataConvert.FieldValue<string>(dr, "area_2", null); 
            obj.service_2 = DataConvert.FieldValue<string>(dr, "service_2", null); 
            obj.payer_2 = DataConvert.FieldValue<string>(dr, "payer_2", null); 
            obj.dat_oper = DataConvert.FieldValue<DateTime>(dr, "dat_oper", DateTime.MinValue); 
            obj.sum_reval = DataConvert.FieldValue<decimal>(dr, "sum_reval", 0); 
            obj.comment = DataConvert.FieldValue<string>(dr, "comment", null); 
            obj.nzp_user = DataConvert.FieldValue<Int32>(dr, "nzp_user", 0); 
            obj.user_ = DataConvert.FieldValue<string>(dr, "user_", null);
            obj.dat_when = DataConvert.FieldValue<DateTime>(dr, "dat_when", DateTime.MinValue);

            return obj;
        }

        public static FnReval ToFnRevalValueSupp(DataRow dr)
        {
            FnReval obj = new FnReval();

            obj.nzp_reval = DataConvert.FieldValue<Int32>(dr, "nzp_reval", 0);
            obj.nzp_supp = DataConvert.FieldValue<Int32>(dr, "nzp_supp", 0);
            obj.nzp_supp_2 = DataConvert.FieldValue<Int32>(dr, "nzp_supp_2", 0);
            obj.supp = DataConvert.FieldValue<string>(dr, "supp", null);
            obj.supp_2 = DataConvert.FieldValue<string>(dr, "supp_2", null);
            obj.nzp_serv = DataConvert.FieldValue<Int32>(dr, "nzp_serv", 0);
            obj.nzp_payer = DataConvert.FieldValue<Int32>(dr, "nzp_payer", 0);
            obj.service = DataConvert.FieldValue<string>(dr, "service", null);
            obj.payer = DataConvert.FieldValue<string>(dr, "payer", null);
            obj.nzp_reval_2 = DataConvert.FieldValue<Int32>(dr, "nzp_reval_2", 0);
            obj.nzp_serv_2 = DataConvert.FieldValue<Int32>(dr, "nzp_serv_2", 0);
            obj.nzp_payer_2 = DataConvert.FieldValue<Int32>(dr, "nzp_payer_2", 0);
            obj.service_2 = DataConvert.FieldValue<string>(dr, "service_2", null);
            obj.payer_2 = DataConvert.FieldValue<string>(dr, "payer_2", null);
            obj.dat_oper = DataConvert.FieldValue<DateTime>(dr, "dat_oper", DateTime.MinValue);
            obj.sum_reval = DataConvert.FieldValue<decimal>(dr, "sum_reval", 0);
            obj.comment = DataConvert.FieldValue<string>(dr, "comment", null);
            obj.nzp_user = DataConvert.FieldValue<Int32>(dr, "nzp_user", 0);
            obj.user_ = DataConvert.FieldValue<string>(dr, "user_", null);
            obj.dat_when = DataConvert.FieldValue<DateTime>(dr, "dat_when", DateTime.MinValue);

            return obj;
        }
    } //end class
} //end namespace