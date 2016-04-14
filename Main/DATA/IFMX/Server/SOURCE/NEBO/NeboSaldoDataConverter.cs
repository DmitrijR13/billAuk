using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using STCLINE.KP50.EPaspXsd;

namespace STCLINE.KP50.DataBase
{
    internal static class NeboSaldoDataConverter
    {
        public static NeboReestr ToReestrInfo(IDataRecord dr)
        {
            NeboReestr t = new NeboReestr();
            //данные из таблицы nebo_reestr
            t.nzp_nebo_reestr = DataConvert.FieldValue(dr, "nzp_nebo_reestr", 0);
            t.type_reestr = DataConvert.FieldValue(dr, "type_reestr", 0);
            t.dat_charge = DataConvert.FieldValue(dr, "dat_charge", new DateTime());
            t.dat_oper = DataConvert.FieldValue(dr, "dat_oper", new DateTime());
            t.is_prepare = DataConvert.FieldValue(dr, "is_prepare", 0);
            t.dat_prepare = DataConvert.FieldValue(dr, "dat_prepare", new DateTime());
            t.is_notify = DataConvert.FieldValue(dr, "is_notify", -1); //?? что возвращать в случае ошибки
            t.dat_notify = DataConvert.FieldValue(dr, "dat_notify", new DateTime());
            t.dat_nebocall = DataConvert.FieldValue(dr, "dat_nebocall", new DateTime());
            //сборные данные из nebo_rsaldo
            t.count_rows = DataConvert.FieldValue(dr, "count_rows", 0);
            t.count_pages = DataConvert.FieldValue(dr, "count_pages", 0);
            t.kontr_sum_insaldo = DataConvert.FieldValue(dr, "kontr_sum_insaldo", Convert.ToDecimal(0.00));
            t.kontr_sum_money = DataConvert.FieldValue(dr, "kontr_sum_money", Convert.ToDecimal(0.00));
            t.kontr_sum_prih = DataConvert.FieldValue(dr, "kontr_sum_prih", Convert.ToDecimal(0.00));

            return t;
        }

        public static NeboSaldo ToSaldoReestr(IDataRecord dr)
        {
            NeboSaldo t = new NeboSaldo();
            t.nzp_nebo_rsaldo = DataConvert.FieldValue(dr, "nzp_nebo_rsaldo", 0);
            t.nzp_nebo_reestr = DataConvert.FieldValue(dr, "nzp_nebo_reestr", 0);
            t.dat_charge = DataConvert.FieldValue(dr, "dat_charge", new DateTime()).ToString("dd.MM.yyyy");
            t.typek = DataConvert.FieldValue(dr, "typek", 0);
            t.num_ls = DataConvert.FieldValue(dr, "num_ls", 0);
            t.pkod = DataConvert.FieldValue(dr, "pkod", Convert.ToDecimal(0.00));
            t.nzp_dom = DataConvert.FieldValue(dr, "nzp_dom", 0);
            t.nzp_serv = DataConvert.FieldValue(dr, "nzp_serv", 0);
            t.nzp_supp = DataConvert.FieldValue(dr, "nzp_supp", 0);
            t.sum_insaldo = DataConvert.FieldValue(dr, "sum_insaldo", Convert.ToDecimal(0.00));
            t.sum_real = DataConvert.FieldValue(dr, "sum_real", Convert.ToDecimal(0.00));
            t.reval = DataConvert.FieldValue(dr, "reval", Convert.ToDecimal(0.00));
            t.izm_saldo = DataConvert.FieldValue(dr, "izm_saldo", Convert.ToDecimal(0.00));
            t.sum_money = DataConvert.FieldValue(dr, "sum_money", Convert.ToDecimal(0.00));
            t.sum_charge = DataConvert.FieldValue(dr, "sum_charge", Convert.ToDecimal(0.00));
            t.sum_outsaldo = DataConvert.FieldValue(dr, "sum_outsaldo", Convert.ToDecimal(0.00));
            t.page_number = DataConvert.FieldValue(dr, "page_number", 0);

            return t;
        }


        public static NeboSupp ToSuppReestr(IDataRecord dr)
        {
            NeboSupp t = new NeboSupp();
            t.nzp_nebo_rfnsupp = DataConvert.FieldValue(dr, "nzp_nebo_rfnsupp", 0);
            t.nzp_nebo_reestr = DataConvert.FieldValue(dr, "nzp_nebo_reestr", 0);
            t.dat_uchet = DataConvert.FieldValue(dr, "dat_uchet", new DateTime());
            t.typek = DataConvert.FieldValue(dr, "typek", 0);
            t.num_ls = DataConvert.FieldValue(dr, "num_ls", 0);
            t.pkod = DataConvert.FieldValue(dr, "pkod", Convert.ToDecimal(0.00));
            t.nzp_dom = DataConvert.FieldValue(dr, "nzp_dom", 0);
            t.nzp_serv = DataConvert.FieldValue(dr, "nzp_serv", 0);
            t.nzp_area = DataConvert.FieldValue(dr, "nzp_area", 0);
            t.nzp_supp = DataConvert.FieldValue(dr, "nzp_supp", 0);
            t.sum_prih = DataConvert.FieldValue(dr, "sum_prih", Convert.ToDecimal(0.00));           
            t.page_number = DataConvert.FieldValue(dr, "page_number", 0);

            return t;
        }

        public static NeboPaymentReestr ToNeboPaymentReestr(IDataRecord dr)
        {
            NeboPaymentReestr t = new NeboPaymentReestr();
            t.dat_uchet = DataConvert.FieldValue(dr, "dat_uchet", new DateTime()).ToString("dd.MM.yyyy");
            t.pkod = DataConvert.FieldValue(dr, "pkod", Convert.ToDecimal(0.00));
            t.nzp_dom = DataConvert.FieldValue(dr, "nzp_dom", 0);
            t.nzp_serv = DataConvert.FieldValue(dr, "nzp_serv", 0);
            t.nzp_supp = DataConvert.FieldValue(dr, "nzp_supp", 0);
            t.sum_prih = DataConvert.FieldValue(dr, "sum_prih", Convert.ToDecimal(0.00));
            return t;
        }
    }

}
